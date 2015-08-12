using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using OmniSharp.Services;
using OmniSharp.Stdio.Services;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.Framework.Configuration;
using Microsoft.Dnx.Runtime;
#if DNXCORE50
using System.Threading.Tasks;
#endif

namespace OmniSharp
{
    public class Program
    {
        private readonly IServiceProvider _serviceProvider;

        public static OmnisharpEnvironment Environment { get; set; }

        public Program(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Main(string[] args)
        {
            var applicationRoot = Directory.GetCurrentDirectory();
            var serverPort = 2000;
            var logLevel = LogLevel.Information;
            var hostPID = -1;
            var transportType = TransportType.Http;
            var otherArgs = new List<string>();

            var enumerator = args.GetEnumerator();

            while (enumerator.MoveNext())
            {
                var arg = (string)enumerator.Current;
                if (arg == "-s")
                {
                    enumerator.MoveNext();
                    applicationRoot = Path.GetFullPath((string)enumerator.Current);
                }
                else if (arg == "-p")
                {
                    enumerator.MoveNext();
                    serverPort = int.Parse((string)enumerator.Current);
                }
                else if (arg == "-v")
                {
                    logLevel = LogLevel.Verbose;
                }
                else if (arg == "--hostPID")
                {
                    enumerator.MoveNext();
                    hostPID = int.Parse((string)enumerator.Current);
                }
                else if (arg == "--stdio")
                {
                    transportType = TransportType.Stdio;
                }
                else
                {
                    otherArgs.Add((string)enumerator.Current);
                }
            }

            Environment = new OmnisharpEnvironment(applicationRoot, serverPort, hostPID, logLevel, transportType, otherArgs.ToArray());

            var writer = new SharedConsoleWriter();

            var hostingServices = new ServiceCollection();
            hostingServices.AddInstance<IOmnisharpEnvironment>(Environment);
            hostingServices.AddInstance<ISharedTextWriter>(writer);

            var hostingContainer = hostingServices.BuildServiceProvider();

            var startupLoader = hostingContainer.GetRequiredService<IStartupLoader>();

            var config = new ConfigurationBuilder()
                .AddCommandLine(new[] { "--server.urls", "http://localhost:" + serverPort })
                .Build();

            config.Set(WebHostBuilder.ServerKey, "Kestrel");

            var engineBuilder = new WebHostBuilder(_serviceProvider, config);

            if (transportType == TransportType.Stdio)
            {
                engineBuilder.UseServer(new Stdio.StdioServerFactory(Console.In, writer));
            }

            var engine = engineBuilder.Build();

            var serverShutdown = engine.Start();

            var appShutdownService = _serviceProvider.GetRequiredService<IApplicationShutdown>();
            var shutdownHandle = new ManualResetEvent(false);

            appShutdownService.ShutdownRequested.Register(() =>
            {
                serverShutdown.Dispose();
                shutdownHandle.Set();
            });

#if DNXCORE50
            var ignored = Task.Run(() =>
            {
                Console.WriteLine("Started");
                Console.ReadLine();
                appShutdownService.RequestShutdown();
            });
#else
            Console.CancelKeyPress += (sender, e) =>
            {
                appShutdownService.RequestShutdown();
            };
#endif

            if (hostPID != -1)
            {
                try
                {
                    var hostProcess = Process.GetProcessById(hostPID);
                    hostProcess.EnableRaisingEvents = true;
                    hostProcess.OnExit(() => appShutdownService.RequestShutdown());
                }
                catch
                {
                    // If the process dies before we get here then request shutdown
                    // immediately
                    appShutdownService.RequestShutdown();
                }
            }

            shutdownHandle.WaitOne();
        }
    }

    public class OmniSharpServiceProvider : IServiceProvider
    {
        public object GetService(Type serviceType)
        {
            throw new NotImplementedException();
        }
    }
}
