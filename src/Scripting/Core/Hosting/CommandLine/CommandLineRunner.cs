// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#pragma warning disable 436 // The type 'RelativePathResolver' conflicts with imported type

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    internal sealed class CommandLineRunner
    {
        private readonly ConsoleIO _console;
        private readonly CommonCompiler _compiler;
        private readonly ScriptCompiler _scriptCompiler;
        private readonly ObjectFormatter _objectFormatter;

        internal CommandLineRunner(ConsoleIO console, CommonCompiler compiler, ScriptCompiler scriptCompiler, ObjectFormatter objectFormatter)
        {
            Debug.Assert(console != null);
            Debug.Assert(compiler != null);
            Debug.Assert(scriptCompiler != null);
            Debug.Assert(objectFormatter != null);

            _console = console;
            _compiler = compiler;
            _scriptCompiler = scriptCompiler;
            _objectFormatter = objectFormatter;
        }

        // for testing:
        internal ConsoleIO Console => _console;
        internal CommonCompiler Compiler => _compiler;

        /// <summary>
        /// csi.exe and vbi.exe entry point.
        /// </summary>
        internal int RunInteractive()
        {
            ErrorLogger errorLogger = null;
            if (_compiler.Arguments.ErrorLogPath != null)
            {
                errorLogger = _compiler.GetErrorLogger(_console.Out, CancellationToken.None);
                if (errorLogger == null)
                {
                    return CommonCompiler.Failed;
                }
            }

            using (errorLogger)
            {
                return RunInteractiveCore(errorLogger);
            }
        }

        /// <summary>
        /// csi.exe and vbi.exe entry point.
        /// </summary>
        private int RunInteractiveCore(ErrorLogger errorLogger)
        {
            Debug.Assert(_compiler.Arguments.IsScriptRunner);

            var sourceFiles = _compiler.Arguments.SourceFiles;

            if (sourceFiles.IsEmpty && _compiler.Arguments.DisplayLogo)
            {
                _compiler.PrintLogo(_console.Out);

                if (!_compiler.Arguments.DisplayHelp)
                {
                    _console.Out.WriteLine(ScriptingResources.HelpPrompt);
                }
            }

            if (_compiler.Arguments.DisplayHelp)
            {
                _compiler.PrintHelp(_console.Out);
                return 0;
            }

            SourceText code = null;

            var diagnosticsInfos = new List<DiagnosticInfo>();

            if (!sourceFiles.IsEmpty)
            {
                if (sourceFiles.Length > 1 || !sourceFiles[0].IsScript)
                {
                    diagnosticsInfos.Add(new DiagnosticInfo(_compiler.MessageProvider, _compiler.MessageProvider.ERR_ExpectedSingleScript));
                }
                else
                {
                    code = _compiler.ReadFileContent(sourceFiles[0], diagnosticsInfos);
                }
            }

            var scriptPathOpt = sourceFiles.IsEmpty ? null : sourceFiles[0].Path;
            var scriptOptions = GetScriptOptions(_compiler.Arguments, scriptPathOpt, _compiler.MessageProvider, diagnosticsInfos);

            var errors = _compiler.Arguments.Errors.Concat(diagnosticsInfos.Select(Diagnostic.Create));
            if (_compiler.ReportErrors(errors, _console.Out, errorLogger))
            {
                return CommonCompiler.Failed;
            }

            var cancellationToken = new CancellationToken();

            if (_compiler.Arguments.InteractiveMode)
            {
                RunInteractiveLoop(scriptOptions, code?.ToString(), cancellationToken);
                return CommonCompiler.Succeeded;
            }
            else
            {
                return RunScript(scriptOptions, code.ToString(), errorLogger, cancellationToken);
            }
        }

        private static ScriptOptions GetScriptOptions(CommandLineArguments arguments, string scriptPathOpt, CommonMessageProvider messageProvider, List<DiagnosticInfo> diagnostics)
        {
            var touchedFilesLoggerOpt = (arguments.TouchedFilesPath != null) ? new TouchedFileLogger() : null;

            var metadataResolver = GetMetadataReferenceResolver(arguments, touchedFilesLoggerOpt);
            var sourceResolver = GetSourceReferenceResolver(arguments, touchedFilesLoggerOpt);

            var resolvedReferences = new List<MetadataReference>();
            if (!arguments.ResolveMetadataReferences(metadataResolver, diagnostics, messageProvider, resolvedReferences))
            {
                // can't resolve some references
                return null;
            }

            return new ScriptOptions(
                filePath: scriptPathOpt ?? "", 
                references: ImmutableArray.CreateRange(resolvedReferences),
                namespaces: CommandLineHelpers.GetImports(arguments),
                metadataResolver: metadataResolver,
                sourceResolver: sourceResolver);
        }

        internal static MetadataReferenceResolver GetMetadataReferenceResolver(CommandLineArguments arguments, TouchedFileLogger loggerOpt)
        {
            return new RuntimeMetadataReferenceResolver(
                new RelativePathResolver(arguments.ReferencePaths, arguments.BaseDirectory),
                null,
                GacFileResolver.IsAvailable ? new GacFileResolver(preferredCulture: CultureInfo.CurrentCulture) : null,
                (path, properties) =>
                {
                    loggerOpt?.AddRead(path);
                    return MetadataReference.CreateFromFile(path, properties);
                });
        }

        internal static SourceReferenceResolver GetSourceReferenceResolver(CommandLineArguments arguments, TouchedFileLogger loggerOpt)
        {
            return new CommonCompiler.LoggingSourceFileResolver(arguments.SourcePaths, arguments.BaseDirectory, ImmutableArray<KeyValuePair<string, string>>.Empty, loggerOpt);
        }

        private int RunScript(ScriptOptions options, string code, ErrorLogger errorLogger, CancellationToken cancellationToken)
        {
            var globals = new CommandLineScriptGlobals(_console.Out, _objectFormatter);
            globals.Args.AddRange(_compiler.Arguments.ScriptArguments);

            var script = Script.CreateInitialScript<int>(_scriptCompiler, code, options, globals.GetType(), assemblyLoaderOpt: null);
            try
            {
                return script.RunAsync(globals, cancellationToken).Result.ReturnValue;
            }
            catch (CompilationErrorException e)
            {
                _compiler.ReportErrors(e.Diagnostics, _console.Out, errorLogger);
                return CommonCompiler.Failed;
            }
        }

        private void RunInteractiveLoop(ScriptOptions options, string initialScriptCodeOpt, CancellationToken cancellationToken)
        {
            var globals = new InteractiveScriptGlobals(_console.Out, _objectFormatter);
            globals.Args.AddRange(_compiler.Arguments.ScriptArguments);

            ScriptState<object> state = null;

            if (initialScriptCodeOpt != null)
            {
                var script = Script.CreateInitialScript<object>(_scriptCompiler, initialScriptCodeOpt, options, globals.GetType(), assemblyLoaderOpt: null);
                TryBuildAndRun(script, globals, ref state, ref options, cancellationToken);
            }

            while (true)
            {
                _console.Out.Write("> ");
                var input = new StringBuilder();
                string line;
                bool cancelSubmission = false;

                while (true)
                {
                    line = _console.In.ReadLine();
                    if (line == null)
                    {
                        if (input.Length == 0)
                        {
                            return;
                        }

                        cancelSubmission = true;
                        break;
                    }

                    input.AppendLine(line);

                    var tree = _scriptCompiler.ParseSubmission(SourceText.From(input.ToString()), cancellationToken);
                    if (_scriptCompiler.IsCompleteSubmission(tree))
                    {
                        break;
                    }

                    _console.Out.Write(". ");
                }

                if (cancelSubmission)
                {
                    continue;
                }

                string code = input.ToString();

                if (IsHelpCommand(code))
                {
                    DisplayHelpText();
                    continue;
                }

                Script<object> newScript;
                if (state == null)
                {
                    newScript = Script.CreateInitialScript<object>(_scriptCompiler, code, options, globals.GetType(), assemblyLoaderOpt: null);
                }
                else
                {
                    newScript = state.Script.ContinueWith(code, options);
                }

                if (!TryBuildAndRun(newScript, globals, ref state, ref options, cancellationToken))
                {
                    continue;
                }

                if (newScript.HasReturnValue())
                {
                    globals.Print(state.ReturnValue);
                }
            }
        }

        private bool TryBuildAndRun(Script<object> newScript, InteractiveScriptGlobals globals, ref ScriptState<object> state, ref ScriptOptions options, CancellationToken cancellationToken)
        {
            var diagnostics = newScript.Compile(cancellationToken);
            DisplayDiagnostics(diagnostics);
            if (diagnostics.HasAnyErrors())
            {
                return false;
            }

            try
            {
                var task = (state == null) ?
                    newScript.RunAsync(globals, cancellationToken) :
                    newScript.ContinueAsync(state, cancellationToken);

                state = task.GetAwaiter().GetResult();
            }
            catch (FileLoadException e) when (e.InnerException is InteractiveAssemblyLoaderException)
            {
                _console.ForegroundColor = ConsoleColor.Red;
                _console.Out.WriteLine(e.InnerException.Message);
                _console.ResetColor();

                return false;
            }
            catch (Exception e)
            {
                DisplayException(e);
                return false;
            }

            options = UpdateOptions(options, globals);

            return true;
        }

        private static ScriptOptions UpdateOptions(ScriptOptions options, InteractiveScriptGlobals globals)
        {
            var currentMetadataResolver = (RuntimeMetadataReferenceResolver)options.MetadataResolver;
            var currentSourceResolver = (CommonCompiler.LoggingSourceFileResolver)options.SourceResolver;

            string newWorkingDirectory = Directory.GetCurrentDirectory();
            var newReferenceSearchPaths = ImmutableArray.CreateRange(globals.ReferencePaths);
            var newSourceSearchPaths = ImmutableArray.CreateRange(globals.SourcePaths);

            // remove references and imports from the options, they have been applied and will be inherited from now on:
            return options.
                RemoveImportsAndReferences().
                WithMetadataResolver(currentMetadataResolver.
                    WithRelativePathResolver(
                        currentMetadataResolver.PathResolver.
                            WithBaseDirectory(newWorkingDirectory).
                            WithSearchPaths(newReferenceSearchPaths))).
                WithSourceResolver(currentSourceResolver.
                        WithBaseDirectory(newWorkingDirectory).
                        WithSearchPaths(newSourceSearchPaths));
        }

        private void DisplayException(Exception e)
        {
            try
            {
                _console.ForegroundColor = ConsoleColor.Red;
                _console.Out.Write(_objectFormatter.FormatUnhandledException(e));
            }
            finally
            {
                _console.ResetColor();
            }
        }

        private static bool IsHelpCommand(string text)
        {
            const string helpCommand = "#help";
            Debug.Assert(text != null);
            return text.Trim() == helpCommand;
        }

        private void DisplayHelpText()
        {
            _console.Out.Write(ScriptingResources.HelpText);
            _console.Out.WriteLine();
        }

        private void DisplayDiagnostics(IEnumerable<Diagnostic> diagnostics)
        {
            const int MaxDisplayCount = 5;

            var errorsAndWarnings = diagnostics.ToArray();
           
            // by severity, then by location
            var ordered = errorsAndWarnings.OrderBy((d1, d2) =>
            {
                int delta = (int)d2.Severity - (int)d1.Severity;
                return (delta != 0) ? delta : d1.Location.SourceSpan.Start - d2.Location.SourceSpan.Start;
            });

            try
            {
                foreach (var diagnostic in ordered.Take(MaxDisplayCount))
                {
                    _console.ForegroundColor = (diagnostic.Severity == DiagnosticSeverity.Error) ? ConsoleColor.Red : ConsoleColor.Yellow;
                    _console.Out.WriteLine(diagnostic.ToString());
                }

                if (errorsAndWarnings.Length > MaxDisplayCount)
                {
                    int notShown = errorsAndWarnings.Length - MaxDisplayCount;
                    _console.ForegroundColor = ConsoleColor.DarkRed;
                    _console.Out.WriteLine(string.Format((notShown == 1) ? ScriptingResources.PlusAdditionalError : ScriptingResources.PlusAdditionalError, notShown));
                }
            }
            finally
            {
                _console.ResetColor();
            }
        }
    }
}
