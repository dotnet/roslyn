// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Tools.Logging;
using Microsoft.CodeAnalysis.Tools.MSBuild;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.Tools.CodeFormatter
{
    class Program
    {
        static readonly string[] _verbosityLevels = new[] { "q", "quiet", "m", "minimal", "n", "normal", "d", "detailed", "diag", "diagnostic" };

        static async Task<int> Main(string[] args)
        {
            var parser = new CommandLineBuilder()
                .UseParseDirective()
                .UseHelp()
                .UseSuggestDirective()
                .UseParseErrorReporting()
                .UseExceptionHandler()
                .AddOption(new[] { "-w", "--workspace" }, "The solution or project file to operate on. If a file is not specified, the command will search the current directory for one.", a => a.WithDefaultValue(() => null).ParseArgumentsAs<string>())
                .AddOption(new[] { "-v", "--verbosity" }, "Set the verbosity level. Allowed values are q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic]", a => a.FromAmong(_verbosityLevels).ExactlyOne())
                .AddVersionOption()
                .OnExecute(typeof(Program).GetMethod(nameof(Run)))
                .Build();

            return await parser.InvokeAsync(args);
        }

        public static async Task<int> Run(string workspace, string verbosity, IConsole console = null)
        {
            var serviceCollection = new ServiceCollection();
            var logLevel = GetLogLevel(verbosity);
            ConfigureServices(serviceCollection, console, logLevel);

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var logger = serviceProvider.GetService<ILogger<Program>>();

            try
            {
                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    var workingDirectory = Directory.GetCurrentDirectory();
                    var (isSolution, workspacePath) = MSBuildWorkspaceFinder.FindWorkspace(workingDirectory, workspace);

                    MSBuildEnvironment.ApplyEnvironmentVariables();
                    MSBuildCoreLoader.LoadDotnetInstance();

                    return await CodeFormatter.FormatWorkspaceAsync(logger, workspacePath, isSolution, cancellationTokenSource.Token);
                }
            }
            catch (FileNotFoundException fex)
            {
                logger.LogError(fex.Message);
                return 1;
            }
            catch (OperationCanceledException)
            {
                return 0;
            }
        }

        private static LogLevel GetLogLevel(string verbosity)
        {
            switch (verbosity)
            {
                case "q":
                case "quiet":
                    return LogLevel.Error;
                case "m":
                case "minimal":
                    return LogLevel.Warning;
                case "n":
                case "normal":
                    return LogLevel.Information;
                case "d":
                case "detailed":
                    return LogLevel.Debug;
                case "diag":
                case "diagnostic":
                    return LogLevel.Trace;
                default:
                    return LogLevel.Information;
            }
        }

        private static void ConfigureServices(ServiceCollection serviceCollection, IConsole console, LogLevel logLevel)
        {
            serviceCollection.AddSingleton(new LoggerFactory().AddSimpleConsole(console, logLevel));
            serviceCollection.AddLogging();
        }
    }
}
