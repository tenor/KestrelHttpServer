// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Http.Features;

namespace Microsoft.AspNet.Server.KestrelTests
{
    public class DummyApplication : IHttpApplication<HttpContext>
    {
        private readonly RequestDelegate _application;

        public DummyApplication(RequestDelegate application)
        {
            _application = application;
        }

        private readonly IHttpContextFactory _httpContextFactory = new HttpContextFactory(new HttpContextAccessor());
        public HttpContext CreateContext(IFeatureCollection contextFeatures)
        {
            return _httpContextFactory.Create(contextFeatures);
        }

        public void DisposeContext(HttpContext context, Exception exception)
        {
            _httpContextFactory.Dispose(context);
        }

        public async Task ProcessRequestAsync(HttpContext context)
        {
            await _application(context);
        }
    }
}
