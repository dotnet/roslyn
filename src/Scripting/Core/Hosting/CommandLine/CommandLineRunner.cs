// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#pragma warning disable 436 // The type 'RelativePathResolver' comflicts with imported type

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
            Debug.Assert(_compiler.Arguments.IsInteractive);

            var sourceFiles = _compiler.Arguments.SourceFiles;

            if (sourceFiles.IsEmpty && _compiler.Arguments.DisplayLogo)
            {
                _compiler.PrintLogo(_console.Out);
            }

            if (_compiler.Arguments.DisplayHelp)
            {
                _compiler.PrintHelp(_console.Out);
                return 0;
            }

            SourceText code = null;
            IEnumerable<Diagnostic> errors = _compiler.Arguments.Errors;
            if (!sourceFiles.IsEmpty)
            {
                if (sourceFiles.Length > 1 || !sourceFiles[0].IsScript)
                {
                    errors = errors.Concat(new[] { Diagnostic.Create(_compiler.MessageProvider, _compiler.MessageProvider.ERR_ExpectedSingleScript) });
                }
                else
                {
                    var diagnostics = new List<DiagnosticInfo>();
                    code = _compiler.ReadFileContent(sourceFiles[0], diagnostics);
                    errors = errors.Concat(diagnostics.Select(Diagnostic.Create));
                }
            }

            if (_compiler.ReportErrors(errors, _console.Out, errorLogger))
            {
                return CommonCompiler.Failed;
            }

            var cancellationToken = new CancellationToken();
            
            var scriptOptions = GetScriptOptions(_compiler.Arguments);

            if (sourceFiles.IsEmpty)
            {
                RunInteractiveLoop(scriptOptions, cancellationToken);
                return CommonCompiler.Succeeded;
            }
            else
            {
                return RunScript(scriptOptions, code.ToString(), sourceFiles[0].Path, errorLogger, cancellationToken);
            }
        }

        private static ScriptOptions GetScriptOptions(CommandLineArguments arguments)
        {
            // TODO: reference paths, usings from arguments (https://github.com/dotnet/roslyn/issues/5277)
            // TODO: auto -add facades
            return ScriptOptions.Default.
                AddReferences("System", "System.Core", "System.Runtime", "System.IO.FileSystem", "System.IO.FileSystem.Primitives", "System.Collections").
                AddNamespaces("System", "System.IO", "System.Threading.Tasks", "System.Linq");
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
                    return MetadataReference.CreateFromFile(path);
                });
        }

        private int RunScript(ScriptOptions options, string code, string scriptPath, ErrorLogger errorLogger, CancellationToken cancellationToken)
        {
            var globals = new CommandLineScriptGlobals(_console.Out, _objectFormatter);
            globals.Args.AddRange(_compiler.Arguments.ScriptArguments);

            options = options.WithPath(scriptPath).WithIsInteractive(false);
            var script = Script.CreateInitialScript<object>(_scriptCompiler, code, options, globals.GetType(), assemblyLoaderOpt: null);
            try
            {
                script.RunAsync(globals, cancellationToken).Wait();

                // TODO: use the return value of the script https://github.com/dotnet/roslyn/issues/5773
                return CommonCompiler.Succeeded;
            }
            catch (CompilationErrorException e)
            {
                _compiler.ReportErrors(e.Diagnostics, _console.Out, errorLogger);
                return CommonCompiler.Failed;
            }
        }

        private void RunInteractiveLoop(ScriptOptions options, CancellationToken cancellationToken)
        {
            var globals = new InteractiveScriptGlobals(_console.Out, _objectFormatter);
            globals.Args.AddRange(_compiler.Arguments.ScriptArguments);

            ScriptState <object> state = null;

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

                Script<object> newScript;
                if (state == null)
                {
                    newScript = Script.CreateInitialScript<object>(_scriptCompiler, code, options, globals.GetType(), assemblyLoaderOpt: null);
                }
                else
                {
                    newScript = state.Script.ContinueWith(code, options);
                }

                var newCompilation = newScript.GetCompilation();

                try
                {
                    newScript.Build(cancellationToken);

                    // display warnings:
                    DisplayDiagnostics(newCompilation.GetDiagnostics(cancellationToken).Where(d => d.Severity == DiagnosticSeverity.Warning));
                }
                catch (CompilationErrorException e)
                {
                    DisplayDiagnostics(e.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error || d.Severity == DiagnosticSeverity.Warning));
                    continue;
                }

                try
                {
                    var task = (state == null) ?
                        newScript.RunAsync(globals, cancellationToken) :
                        newScript.ContinueAsync(state, cancellationToken);

                    state = task.GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    DisplayException(e);
                    continue;
                }

                bool hasValue;
                ITypeSymbol resultType = newCompilation.GetSubmissionResultType(out hasValue);
                if (hasValue)
                {
                    if (resultType != null && resultType.SpecialType == SpecialType.System_Void)
                    {
                        _console.Out.WriteLine(_objectFormatter.VoidDisplayString);
                    }
                    else
                    {
                        globals.Print(state.ReturnValue);
                    }
                }
            }
        }

        private void DisplayException(Exception e)
        {
            var oldColor = _console.ForegroundColor;
            try
            {
                _console.ForegroundColor = ConsoleColor.Red;
                _console.Out.WriteLine(e.Message);

                _console.ForegroundColor = ConsoleColor.DarkRed;

                var trace = new StackTrace(e, needFileInfo: true);
                foreach (var frame in trace.GetFrames())
                {
                    if (!frame.HasMethod())
                    {
                        continue;
                    }

                    var method = frame.GetMethod();
                    var type = method.DeclaringType;

                    if (type == typeof(CommandLineRunner))
                    {
                        break;
                    }

                    string methodDisplay = _objectFormatter.FormatMethodSignature(method);

                    // TODO: we don't want to include awaiter helpers, shouldn't they be marked by DebuggerHidden in FX?
                    if (methodDisplay == null || IsTaskAwaiter(type) || IsTaskAwaiter(type.DeclaringType))
                    {
                        continue;
                    }

                    _console.Out.Write("  + ");
                    _console.Out.Write(methodDisplay);

                    if (frame.HasSource())
                    {
                        _console.Out.Write(string.Format(CultureInfo.CurrentUICulture, ScriptingResources.AtFileLine, frame.GetFileName(), frame.GetFileLineNumber()));
                    }

                    _console.Out.WriteLine();
                }
            }
            finally
            {
                _console.ForegroundColor = oldColor;
            }
        }

        private static bool IsTaskAwaiter(Type type)
        {
            if (type == typeof(TaskAwaiter) || type == typeof(ConfiguredTaskAwaitable))
            {
                return true;
            }

            if (type?.GetTypeInfo().IsGenericType == true)
            {
                var genericDef = type.GetTypeInfo().GetGenericTypeDefinition();
                return genericDef == typeof(TaskAwaiter<>) || type == typeof(ConfiguredTaskAwaitable<>);
            }

            return false;
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

            var oldColor = _console.ForegroundColor;
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
                _console.ForegroundColor = oldColor;
            }
        }
    }
}
