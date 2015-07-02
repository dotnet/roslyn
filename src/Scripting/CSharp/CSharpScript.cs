// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.Scripting.CSharp
{
    /// <summary>
    /// A factory for creating and running csharp scripts.
    /// </summary>
    public static class CSharpScript
    {
        /// <summary>
        /// Create a new C# script.
        /// <param name="code">The source code of the script.</param>
        /// <param name="options">The script options.</param>
        /// </summary>
        public static Script<T> Create<T>(string code, ScriptOptions options)
        {
            return new CSharpScript<T>(code, null, options, null, null, null);
        }

        /// <summary>
        /// Create a new C# script.
        /// <param name="code">The source code of the script.</param>
        /// </summary>
        public static Script<T> Create<T>(string code)
        {
            return Create<T>(code, null);
        }

        /// <summary>
        /// Run a C# script.
        /// </summary>
        /// <param name="code">The source code of the script.</param>
        /// <param name="options">The script options.</param>
        /// <param name="globals">An object instance whose members can be accessed by the script as global variables, 
        /// or a <see cref="ScriptState{T}"/> instance that was the output from a previously run script.</param>
        public static ScriptState<T> Run<T>(string code, ScriptOptions options, object globals)
        {
            return Create<T>(code, options).Run(globals);
        }

        /// <summary>
        /// Run a C# script.
        /// </summary>
        /// <param name="code">The source code of the script.</param>
        /// <param name="options">The script options.</param>
        public static ScriptState<T> Run<T>(string code, ScriptOptions options)
        {
            return Run<T>(code, options, globals: null);
        }

        /// <summary>
        /// Run a C# script.
        /// </summary>
        /// <param name="code">The source code of the script.</param>
        /// <param name="globals">An object instance whose members can be accessed by the script as global variables, 
        /// or a <see cref="ScriptState{T}"/> instance that was the output from a previously run script.</param>
        public static ScriptState<T> Run<T>(string code, object globals)
        {
            return Run<T>(code, options: null, globals: globals);
        }

        /// <summary>
        /// Run a C# script.
        /// </summary>
        /// <param name="code">The source code of the script.</param>
        public static ScriptState<T> Run<T>(string code)
        {
            return Run<T>(code, null, null);
        }

        /// <summary>
        /// Run a C# script and return its resulting value.
        /// </summary>
        /// <param name="code">The source code of the script.</param>
        /// <param name="options">The script options.</param>
        /// <param name="globals">An object instance whose members can be accessed by the script as global variables, 
        /// or a <see cref="ScriptState{T}"/> instance that was the output from a previously run script.</param>
        /// <return>Returns the value returned by running the script.</return>
        public static Task<T> Eval<T>(string code, ScriptOptions options, object globals)
        {
            return Run<T>(code, options, globals).ReturnValue;
        }

        /// <summary>
        /// Run a C# script and return its resulting value.
        /// </summary>
        /// <param name="code">The source code of the script.</param>
        /// <param name="options">The script options.</param>
        /// <return>Returns the value returned by running the script.</return>
        public static Task<T> Eval<T>(string code, ScriptOptions options)
        {
            return Run<T>(code, options).ReturnValue;
        }

        /// <summary>
        /// Run a C# script and return its resulting value.
        /// </summary>
        /// <param name="code">The source code of the script.</param>
        /// <param name="globals">An object instance whose members can be accessed by the script as global variables, 
        /// or a <see cref="ScriptState{T}"/> instance that was the output from a previously run script.</param>
        /// <return>Returns the value returned by running the script.</return>
        public static Task<T> Eval<T>(string code, object globals)
        {
            return Run<T>(code, globals).ReturnValue;
        }

        /// <summary>
        /// Run a C# script and return its resulting value.
        /// </summary>
        /// <param name="code">The source code of the script.</param>
        /// <return>Returns the value returned by running the script.</return>
        public static Task<T> Eval<T>(string code)
        {
            return Run<T>(code).ReturnValue;
        }
    }

    internal sealed class CSharpScript<T> : Script<T>
    {
        internal CSharpScript(string code, string path, ScriptOptions options, Type globalsType, ScriptBuilder builder, Script previous)
            : base(code, path, options, globalsType, builder, previous)
        {
        }

        internal override Script Make(string code, string path, ScriptOptions options, Type globalsType, ScriptBuilder builder, Script previous)
        {
            return new CSharpScript<T>(code, path, options, globalsType, builder, previous);
        }

        #region Compilation
        private static readonly CSharpParseOptions s_defaultInteractive = new CSharpParseOptions(languageVersion: LanguageVersion.CSharp6, kind: SourceCodeKind.Interactive);
        private static readonly CSharpParseOptions s_defaultScript = new CSharpParseOptions(languageVersion: LanguageVersion.CSharp6, kind: SourceCodeKind.Script);

        protected override Compilation CreateCompilation()
        {
            Compilation previousSubmission = null;
            if (this.Previous != null)
            {
                previousSubmission = this.Previous.GetCompilation();
            }

            var references = this.GetReferencesForCompilation();

            var parseOptions = this.Options.IsInteractive ? s_defaultInteractive : s_defaultScript;
            var tree = SyntaxFactory.ParseSyntaxTree(this.Code, parseOptions, path: this.Path);

            string assemblyName, submissionTypeName;
            this.Builder.GenerateSubmissionId(out assemblyName, out submissionTypeName);

            var compilation = CSharpCompilation.CreateSubmission(
                assemblyName,
                tree,
                references,
                new CSharpCompilationOptions(
                    outputKind: OutputKind.DynamicallyLinkedLibrary,
                    mainTypeName: null,
                    scriptClassName: submissionTypeName,
                    usings: this.Options.Namespaces,
                    optimizationLevel: OptimizationLevel.Debug, // TODO
                    checkOverflow: false,                  // TODO
                    allowUnsafe: true,                     // TODO
                    platform: Platform.AnyCpu,
                    warningLevel: 4,
                    xmlReferenceResolver: null, // don't support XML file references in interactive (permissions & doc comment includes)
                    sourceReferenceResolver: SourceFileResolver.Default, // TODO
                    metadataReferenceResolver: this.Options.ReferenceResolver,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default
                ),
                previousSubmission,
                this.ReturnType,
                this.GlobalsType
            );

            return compilation;
        }

        protected override string FormatDiagnostic(Diagnostic diagnostic, CultureInfo culture)
        {
            return CSharpDiagnosticFormatter.Instance.Format(diagnostic, culture);
        }
        #endregion
    }
}

