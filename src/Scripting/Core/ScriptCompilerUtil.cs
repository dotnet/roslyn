// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Scripting
{
    internal static class ScriptCompilerUtil
    {
        /// <summary>
        /// csi.exe and vbi.exe entry point.
        /// </summary>
        internal static int RunInteractive(CommonCompiler compiler, TextWriter consoleOutput)
        {
            ErrorLogger errorLogger = null;
            if (compiler.Arguments.ErrorLogPath != null)
            {
                errorLogger = compiler.GetErrorLogger(consoleOutput, CancellationToken.None);
                if (errorLogger == null)
                {
                    return CommonCompiler.Failed;
                }
            }

            using (errorLogger)
            {
                return RunInteractiveCore(compiler, consoleOutput, errorLogger);
            }
        }

        /// <summary>
        /// csi.exe and vbi.exe entry point.
        /// </summary>
        private static int RunInteractiveCore(CommonCompiler compiler, TextWriter consoleOutput, ErrorLogger errorLogger)
        {
            Debug.Assert(compiler.Arguments.IsInteractive);

            var hasScriptFiles = compiler.Arguments.SourceFiles.Any(file => file.IsScript);

            if (compiler.Arguments.DisplayLogo && !hasScriptFiles)
            {
                compiler.PrintLogo(consoleOutput);
            }

            if (compiler.Arguments.DisplayHelp)
            {
                compiler.PrintHelp(consoleOutput);
                return 0;
            }

            // TODO (tomat):
            // When we have command line REPL enabled we'll launch it if there are no input files. 
            IEnumerable<Diagnostic> errors = compiler.Arguments.Errors;
            if (!hasScriptFiles)
            {
                errors = errors.Concat(new[] { Diagnostic.Create(compiler.MessageProvider, compiler.MessageProvider.ERR_NoScriptsSpecified) });
            }

            if (compiler.ReportErrors(errors, consoleOutput, errorLogger))
            {
                return CommonCompiler.Failed;
            }

            // arguments are always available when executing script code:
            Debug.Assert(compiler.Arguments.ScriptArguments != null);

            var compilation = compiler.CreateCompilation(consoleOutput, touchedFilesLogger: null, errorLogger: errorLogger);
            if (compilation == null)
            {
                return CommonCompiler.Failed;
            }

            byte[] compiledAssembly;
            using (MemoryStream output = new MemoryStream())
            {
                EmitResult emitResult = compilation.Emit(output);
                if (compiler.ReportErrors(emitResult.Diagnostics, consoleOutput, errorLogger))
                {
                    return CommonCompiler.Failed;
                }

                compiledAssembly = output.ToArray();
            }

            var assembly = Assembly.Load(compiledAssembly);

            return Execute(assembly, compiler.Arguments.ScriptArguments.ToArray());
        }

        private static int Execute(Assembly assembly, string[] scriptArguments)
        {
            var parameters = assembly.EntryPoint.GetParameters();
            object[] arguments;

            if (parameters.Length == 0)
            {
                arguments = SpecializedCollections.EmptyObjects;
            }
            else
            {
                Debug.Assert(parameters.Length == 1);
                arguments = new object[] { scriptArguments };
            }

            object result = assembly.EntryPoint.Invoke(null, arguments);
            return result is int ? (int)result : CommonCompiler.Succeeded;
        }
    }
}
