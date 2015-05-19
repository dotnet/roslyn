// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Scripting.CSharp
{
    /// <summary>
    /// A factory for creating and running csharp scripts.
    /// </summary>
    public sealed class CSharpScript : Script
    {
        private CSharpScript(string code, string path, ScriptOptions options, Type globalsType, Type returnType, ScriptBuilder builder, Script previous)
            : base(code, path, options, globalsType, returnType, builder, previous)
        {
        }

        internal override Script Make(string code, string path, ScriptOptions options, Type globalsType, Type returnType, ScriptBuilder builder, Script previous)
        {
            return new CSharpScript(code, path, options, globalsType, returnType, builder, previous);
        }

        /// <summary>
        /// Create a new C# script.
        /// <param name="code">The source code of the script.</param>
        /// <param name="options">The script options.</param>
        /// </summary>
        public static Script Create(string code, ScriptOptions options)
        {
            return new CSharpScript(code, null, options, null, typeof(object), null, null);
        }

        /// <summary>
        /// Create a new C# script.
        /// <param name="code">The source code of the script.</param>
        /// </summary>
        public static Script Create(string code)
        {
            return Create(code, null);
        }

        /// <summary>
        /// Run a C# script.
        /// </summary>
        /// <param name="code">The source code of the script.</param>
        /// <param name="options">The script options.</param>
        /// <param name="globals">An object instance whose members can be accessed by the script as global variables, 
        /// or a <see cref="ScriptState"/> instance that was the output from a previously run script.</param>
        public static ScriptState Run(string code, ScriptOptions options, object globals)
        {
            return Create(code, options).Run(globals);
        }

        /// <summary>
        /// Run a C# script.
        /// </summary>
        /// <param name="code">The source code of the script.</param>
        /// <param name="options">The script options.</param>
        public static ScriptState Run(string code, ScriptOptions options)
        {
            return Run(code, options, globals: null);
        }

        /// <summary>
        /// Run a C# script.
        /// </summary>
        /// <param name="code">The source code of the script.</param>
        /// <param name="globals">An object instance whose members can be accessed by the script as global variables, 
        /// or a <see cref="ScriptState"/> instance that was the output from a previously run script.</param>
        public static ScriptState Run(string code, object globals)
        {
            return Run(code, options: null, globals: globals);
        }

        /// <summary>
        /// Run a C# script.
        /// </summary>
        /// <param name="code">The source code of the script.</param>
        public static ScriptState Run(string code)
        {
            return Run(code, null, null);
        }

        /// <summary>
        /// Run a C# script and return its resulting value.
        /// </summary>
        /// <param name="code">The source code of the script.</param>
        /// <param name="options">The script options.</param>
        /// <param name="globals">An object instance whose members can be accessed by the script as global variables, 
        /// or a <see cref="ScriptState"/> instance that was the output from a previously run script.</param>
        /// <return>Returns the value returned by running the script.</return>
        public static object Eval(string code, ScriptOptions options, object globals)
        {
            return Run(code, options, globals).ReturnValue;
        }

        /// <summary>
        /// Run a C# script and return its resulting value.
        /// </summary>
        /// <param name="code">The source code of the script.</param>
        /// <param name="options">The script options.</param>
        /// <return>Returns the value returned by running the script.</return>
        public static object Eval(string code, ScriptOptions options)
        {
            return Run(code, options).ReturnValue;
        }

        /// <summary>
        /// Run a C# script and return its resulting value.
        /// </summary>
        /// <param name="code">The source code of the script.</param>
        /// <param name="globals">An object instance whose members can be accessed by the script as global variables, 
        /// or a <see cref="ScriptState"/> instance that was the output from a previously run script.</param>
        /// <return>Returns the value returned by running the script.</return>
        public static object Eval(string code, object globals)
        {
            return Run(code, globals).ReturnValue;
        }

        /// <summary>
        /// Run a C# script and return its resulting value.
        /// </summary>
        /// <param name="code">The source code of the script.</param>
        /// <return>Returns the value returned by running the script.</return>
        public static object Eval(string code)
        {
            return Run(code).ReturnValue;
        }

        #region Compilation
        private static readonly CSharpParseOptions s_defaultInteractive = new CSharpParseOptions(languageVersion: LanguageVersion.CSharp6, kind: SourceCodeKind.Interactive, documentationMode: DocumentationMode.Parse, preprocessorSymbols: ImmutableArray<string>.Empty);
        private static readonly CSharpParseOptions s_defaultScript = new CSharpParseOptions(languageVersion: LanguageVersion.CSharp6, kind: SourceCodeKind.Script, documentationMode: DocumentationMode.Parse, preprocessorSymbols: ImmutableArray<string>.Empty);

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

