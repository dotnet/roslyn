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
    internal class Program
    {
        private static readonly string[] _verbosityLevels = new[] { "q", "quiet", "m", "minimal", "n", "normal", "d", "detailed", "diag", "diagnostic" };

        private static async Task<int> Main(string[] args)
        {
            var parser = new CommandLineBuilder(new Command("dotnet-format", handler: CommandHandler.Create(typeof(Program).GetMethod(nameof(Run)))))
                .UseParseDirective()
                .UseHelp()
                .UseDebugDirective()
                .UseSuggestDirective()
                .RegisterWithDotnetSuggest()
                .UseParseErrorReporting()
                .UseExceptionHandler()
                .AddOption(new Option(new[] { "-w", "--workspace" }, Resources.The_solution_or_project_file_to_operate_on_If_a_file_is_not_specified_the_command_will_search_the_current_directory_for_one, new Argument<string>(() => null)))
                .AddOption(new Option(new[] { "-v", "--verbosity" }, Resources.Set_the_verbosity_level_Allowed_values_are_quiet_minimal_normal_detailed_and_diagnostic, new Argument<string>() { Arity = ArgumentArity.ExactlyOne }.FromAmong(_verbosityLevels)))
                .AddOption(new Option(new[] { "--dry-run" }, Resources.Format_files_but_do_not_save_changes_to_disk, new Argument<bool>()))
                .UseVersionOption()
                .Build();

            return await parser.InvokeAsync(args).ConfigureAwait(false);
        }

        public static async Task<int> Run(string workspace, string verbosity, bool dryRun, IConsole console = null)
        {
            var serviceCollection = new ServiceCollection();
            var logLevel = GetLogLevel(verbosity);
            ConfigureServices(serviceCollection, console, logLevel);

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var logger = serviceProvider.GetService<ILogger<Program>>();

            var cancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cancellationTokenSource.Cancel();
            };

            try
            {
                var workingDirectory = Directory.GetCurrentDirectory();
                var (isSolution, workspacePath) = MSBuildWorkspaceFinder.FindWorkspace(workingDirectory, workspace);

                // To ensure we get the version of MSBuild packaged with the dotnet SDK used by the
                // workspace, use its directory as our working directory which will take into account
                // a global.json if present.
                var workspaceDirectory = Path.GetDirectoryName(workspacePath);
                MSBuildEnvironment.ApplyEnvironmentVariables(workspaceDirectory);
                MSBuildCoreLoader.LoadDotnetInstance(workspaceDirectory);

                return await CodeFormatter.FormatWorkspaceAsync(
                    logger,
                    workspacePath,
                    isSolution,
                    logAllWorkspaceWarnings: logLevel == LogLevel.Trace,
                    saveFormattedFiles: !dryRun,
                    cancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (FileNotFoundException fex)
            {
                logger.LogError(fex.Message);
                return 1;
            }
            catch (OperationCanceledException)
            {
                return 1;
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
