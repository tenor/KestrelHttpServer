// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Server.Kestrel;

namespace Microsoft.AspNet.Server.KestrelTests
{
    public class TestServiceContext : ServiceContext
    {
        public TestServiceContext()
        {
            AppLifetime = new LifetimeNotImplemented();
            Log = new TestKestrelTrace();
            DateHeaderValueManager = new TestDateHeaderValueManager();
        }
    }
}
