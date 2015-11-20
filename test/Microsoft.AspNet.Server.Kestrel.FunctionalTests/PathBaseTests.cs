﻿using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Testing.xunit;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNet.Server.Kestrel.FunctionalTests
{
    public class PathBaseTests
    {
        [ConditionalTheory]
        [InlineData("http://localhost:8791/base", "http://localhost:8791/base", "/base", "")]
        [InlineData("http://localhost:8791/base", "http://localhost:8791/base/", "/base", "/")]
        [InlineData("http://localhost:8791/base", "http://localhost:8791/base/something", "/base", "/something")]
        [InlineData("http://localhost:8791/base/more", "http://localhost:8791/base/more", "/base/more", "")]
        [InlineData("http://localhost:8791/base/more", "http://localhost:8791/base/more/something", "/base/more", "/something")]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono, SkipReason = "Test hangs after execution on Mono.")]
        public Task ServerPathIsRequestPathBase(string registerAddress, string requestAddress, string expectedPathBase, string expectedPath)
        {
            return TestPathBase(registerAddress, requestAddress, expectedPathBase, expectedPath);
        }

        [ConditionalTheory]
        [InlineData("http://localhost:8791", "http://localhost:8791", "", "/")]
        [InlineData("http://localhost:8791", "http://localhost:8791/", "", "/")]
        [InlineData("http://localhost:8791", "http://localhost:8791/something", "", "/something")]
        [InlineData("http://localhost:8791/base", "http://localhost:8791", "", "/")]
        [InlineData("http://localhost:8791/base", "http://localhost:8791/", "", "/")]
        [InlineData("http://localhost:8791/base", "http://localhost:8791/something", "", "/something")]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono, SkipReason = "Test hangs after execution on Mono.")]
        public Task DefaultPathBaseIsEmpty(string registerAddress, string requestAddress, string expectedPathBase, string expectedPath)
        {
            return TestPathBase(registerAddress, requestAddress, expectedPathBase, expectedPath);
        }

        [ConditionalTheory]
        [InlineData("http://localhost:8791", "http://localhost:8791", "", "/")]
        [InlineData("http://localhost:8791", "http://localhost:8791/", "", "/")]
        [InlineData("http://localhost:8791/", "http://localhost:8791", "", "/")]
        [InlineData("http://localhost:8791/", "http://localhost:8791/", "", "/")]
        [InlineData("http://localhost:8791/base", "http://localhost:8791/base/", "/base", "/")]
        [InlineData("http://localhost:8791/base/", "http://localhost:8791/base", "/base", "")]
        [InlineData("http://localhost:8791/base/", "http://localhost:8791/base/", "/base", "/")]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono, SkipReason = "Test hangs after execution on Mono.")]
        public Task PathBaseNeverEndsWithSlash(string registerAddress, string requestAddress, string expectedPathBase, string expectedPath)
        {
            return TestPathBase(registerAddress, requestAddress, expectedPathBase, expectedPath);
        }

        private async Task TestPathBase(string registerAddress, string requestAddress, string expectedPathBase, string expectedPath)
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string> {
                    { "server.urls", registerAddress }
                }).Build();

            var builder = new WebHostBuilder(config)
                .UseServerFactory("Microsoft.AspNet.Server.Kestrel")
                .UseStartup(app =>
                {
                    app.Run(async context =>
                    {
                        await context.Response.WriteAsync(JsonConvert.SerializeObject(new
                        {
                            PathBase = context.Request.PathBase.Value,
                            Path = context.Request.Path.Value
                        }));
                    });
                });

            using (var app = builder.Build().Start())
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(requestAddress);
                    response.EnsureSuccessStatusCode();

                    var responseText = await response.Content.ReadAsStringAsync();
                    Assert.NotEmpty(responseText);

                    var pathFacts = JsonConvert.DeserializeObject<JObject>(responseText);
                    Assert.Equal(expectedPathBase, pathFacts["PathBase"].Value<string>());
                    Assert.Equal(expectedPath, pathFacts["Path"].Value<string>());
                }
            }
        }
    }
}
