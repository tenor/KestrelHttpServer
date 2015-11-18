// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public class Frame<THttpContext> : Frame
    {
        private readonly IHttpApplication<THttpContext> _application;

        public Frame(IHttpApplication<THttpContext> application,
                     ConnectionContext context)
            : this(application, context, remoteEndPoint: null, localEndPoint: null)
        {
        }

        public Frame(IHttpApplication<THttpContext> application,
                     ConnectionContext context,
                     IPEndPoint remoteEndPoint,
                     IPEndPoint localEndPoint)
            : base(context, remoteEndPoint, localEndPoint)
        {
            _application = application;
        }

        public override async Task RequestProcessingAsync()
        {
            try
            {
                var terminated = false;
                while (!terminated && !_requestProcessingStopping)
                {
                    while (!terminated && !_requestProcessingStopping && !TakeStartLine(SocketInput))
                    {
                        terminated = SocketInput.RemoteIntakeFin;
                        if (!terminated)
                        {
                            await SocketInput;
                        }
                    }

                    while (!terminated && !_requestProcessingStopping && !TakeMessageHeaders(SocketInput, _requestHeaders))
                    {
                        terminated = SocketInput.RemoteIntakeFin;
                        if (!terminated)
                        {
                            await SocketInput;
                        }
                    }

                    if (!terminated && !_requestProcessingStopping)
                    {
                        MessageBody = MessageBody.For(HttpVersion, _requestHeaders, this);
                        _keepAlive = MessageBody.RequestKeepAlive;
                        RequestBody = new FrameRequestStream(MessageBody);
                        ResponseBody = new FrameResponseStream(this);
                        DuplexStream = new FrameDuplexStream(RequestBody, ResponseBody);

                        var context = _application.CreateContext(this);
                        try
                        {
                            await _application.ProcessRequestAsync(context).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            ReportApplicationError(ex);
                            _application.DisposeContext(context, ex);
                        }
                        finally
                        {
                            // Trigger OnStarting if it hasn't been called yet and the app hasn't
                            // already failed. If an OnStarting callback throws we can go through
                            // our normal error handling in ProduceEnd.
                            // https://github.com/aspnet/KestrelHttpServer/issues/43
                            if (!_responseStarted && _applicationException == null)
                            {
                                await FireOnStarting();
                            }

                            await FireOnCompleted();

                            if (_applicationException == null)
                            {
                                _application.DisposeContext(context, null);
                            }

                            await ProduceEnd();

                            // Finish reading the request body in case the app did not.
                            await MessageBody.Consume();
                        }

                        terminated = !_keepAlive;
                    }

                    Reset();
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning("Connection processing ended abnormally", ex);
            }
            finally
            {
                try
                {
                    // Inform client no more data will ever arrive
                    ConnectionControl.End(ProduceEndType.SocketShutdownSend);

                    // Wait for client to either disconnect or send unexpected data
                    await SocketInput;

                    // Dispose socket
                    ConnectionControl.End(ProduceEndType.SocketDisconnect);
                }
                catch (Exception ex)
                {
                    Log.LogWarning("Connection shutdown abnormally", ex);
                }
            }
        }
    }
}
