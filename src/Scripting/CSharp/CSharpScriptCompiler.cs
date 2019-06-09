// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Scripting
{
    internal sealed class CSharpScriptCompiler : ScriptCompiler
    {
        public static readonly ScriptCompiler Instance = new CSharpScriptCompiler();

        internal static readonly CSharpParseOptions DefaultParseOptions = new CSharpParseOptions(kind: SourceCodeKind.Script, languageVersion: LanguageVersion.Latest);

        private CSharpScriptCompiler()
        {
        }

        public override DiagnosticFormatter DiagnosticFormatter => CSharpDiagnosticFormatter.Instance;

        public override StringComparer IdentifierComparer => StringComparer.Ordinal;

        public override bool IsCompleteSubmission(SyntaxTree tree) => SyntaxFactory.IsCompleteSubmission(tree);

        public override SyntaxTree ParseSubmission(SourceText text, ParseOptions parseOptions, CancellationToken cancellationToken) =>
            SyntaxFactory.ParseSyntaxTree(text, parseOptions ?? DefaultParseOptions, cancellationToken: cancellationToken);

        public override Compilation CreateSubmission(Script script)
        {
            CSharpCompilation previousSubmission = null;
            if (script.Previous != null)
            {
                previousSubmission = (CSharpCompilation)script.Previous.GetCompilation();
            }

            var diagnostics = DiagnosticBag.GetInstance();
            var references = script.GetReferencesForCompilation(MessageProvider.Instance, diagnostics);

            // TODO: report diagnostics
            diagnostics.Free();

            var tree = SyntaxFactory.ParseSyntaxTree(script.SourceText, script.Options.ParseOptions ?? DefaultParseOptions, script.Options.FilePath);

            string assemblyName, submissionTypeName;
            script.Builder.GenerateSubmissionId(out assemblyName, out submissionTypeName);

            var compilation = CSharpCompilation.CreateScriptCompilation(
                assemblyName,
                tree,
                references,
                new CSharpCompilationOptions(
                    outputKind: OutputKind.DynamicallyLinkedLibrary,
                    mainTypeName: null,
                    scriptClassName: submissionTypeName,
                    usings: script.Options.Imports,
                    optimizationLevel: script.Options.OptimizationLevel,
                    checkOverflow: script.Options.CheckOverflow,
                    allowUnsafe: script.Options.AllowUnsafe,
                    platform: Platform.AnyCpu,
                    warningLevel: script.Options.WarningLevel,
                    xmlReferenceResolver: null, // don't support XML file references in interactive (permissions & doc comment includes)
                    sourceReferenceResolver: script.Options.SourceResolver,
                    metadataReferenceResolver: script.Options.MetadataResolver,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default
                ).WithTopLevelBinderFlags(BinderFlags.IgnoreCorLibraryDuplicatedTypes),
                previousSubmission,
                script.ReturnType,
                script.GlobalsType
            );

            return compilation;
        }
    }
}
