// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
#if NETCOREAPP2_2
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class H2SpecTests : TestApplicationErrorLoggerLoggedTest
    {
        [ConditionalTheory]
        [MemberData(nameof(H2SpecTestCases))]
        public async Task RunIndividualTestCase(H2SpecTestCase testCase)
        {
            var hostBuilder = TransportSelector.GetWebHostBuilder()
                .UseKestrel(options =>
                {
                    options.Listen(IPAddress.Loopback, 0, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http2;
                        if (testCase.Https)
                        {
                            listenOptions.UseHttps(TestResources.TestCertificatePath, "testPassword");
                        }
                    });
                })
                .ConfigureServices(AddTestLogging)
                .Configure(ConfigureHelloWorld);

            using (var host = hostBuilder.Build())
            {
                await host.StartAsync();

                H2SpecCommands.RunTest(testCase.Id, host.GetPort(), testCase.Https, Logger);
            }
        }

        public static TheoryData<H2SpecTestCase> H2SpecTestCases
        {
            get
            {
                var dataset = new TheoryData<H2SpecTestCase>();
                var toSkip = new[] { "hpack/4.2/1", "http2/5.4.1/2", "http2/6.5.3/1", "http2/6.9.1/1", "http2/6.9.1/2",
                    "http2/6.9.1/3", "http2/6.9.2/1", "http2/6.9.2/2", "http2/8.1.2.3/1" };

                foreach (var testcase in H2SpecCommands.EnumerateTestCases())
                {
                    string skip = null;
                    if (toSkip.Contains(testcase.Item1))
                    {
                        skip = "https://github.com/aspnet/KestrelHttpServer/issues/2154";
                    }
                    
                    dataset.Add(new H2SpecTestCase()
                    {
                        Id = testcase.Item1,
                        Description = testcase.Item2,
                        Https = false,
                        Skip = skip,
                    });
                    
                    dataset.Add(new H2SpecTestCase()
                    {
                        Id = testcase.Item1,
                        Description = testcase.Item2,
                        Https = true,
                        Skip = skip,
                    });
                }

                return dataset;
            }
        }

        public class H2SpecTestCase : IXunitSerializable
        {
            // For the serializer
            public H2SpecTestCase()
            {
            }

            public string Id { get; set; }
            public string Description { get; set; }
            public bool Https { get; set; }
            public string Skip { get; set; }

            public void Deserialize(IXunitSerializationInfo info)
            {
                Id = info.GetValue<string>(nameof(Id));
                Description = info.GetValue<string>(nameof(Description));
                Https = info.GetValue<bool>(nameof(Https));
                Skip = info.GetValue<string>(nameof(Skip));
            }

            public void Serialize(IXunitSerializationInfo info)
            {
                info.AddValue(nameof(Id), Id, typeof(string));
                info.AddValue(nameof(Description), Description, typeof(string));
                info.AddValue(nameof(Https), Https, typeof(bool));
                info.AddValue(nameof(Skip), Skip, typeof(string));
            }

            public override string ToString()
            {
                return $"{Id}, HTTPS:{Https}, {Description}";
            }
        }

        private void ConfigureHelloWorld(IApplicationBuilder app)
        {
            app.Run(context =>
            {
                return context.Response.WriteAsync("Hello World");
            });
        }
    }
}
#elif NET461 // HTTP/2 is not supported
#else
#error TFMs need updating
#endif
