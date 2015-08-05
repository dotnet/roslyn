// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Scripting.CSharp
{
    internal sealed class CSharpScriptCompiler : ScriptCompiler
    {
        public static readonly ScriptCompiler Instance = new CSharpScriptCompiler();
        private static readonly CSharpParseOptions s_defaultInteractive = new CSharpParseOptions(languageVersion: LanguageVersion.CSharp6, kind: SourceCodeKind.Interactive);
        private static readonly CSharpParseOptions s_defaultScript = new CSharpParseOptions(languageVersion: LanguageVersion.CSharp6, kind: SourceCodeKind.Script);

        private CSharpScriptCompiler()
        {
        }

        public override DiagnosticFormatter DiagnosticFormatter => CSharpDiagnosticFormatter.Instance;

        public override Compilation CreateSubmission(Script script)
        {
            Compilation previousSubmission = null;
            if (script.Previous != null)
            {
                previousSubmission = script.Previous.GetCompilation();
            }

            var references = script.GetReferencesForCompilation();

            var parseOptions = script.Options.IsInteractive ? s_defaultInteractive : s_defaultScript;
            var tree = SyntaxFactory.ParseSyntaxTree(script.Code, parseOptions, script.Options.Path);

            string assemblyName, submissionTypeName;
            script.Builder.GenerateSubmissionId(out assemblyName, out submissionTypeName);

            var compilation = CSharpCompilation.CreateSubmission(
                assemblyName,
                tree,
                references,
                new CSharpCompilationOptions(
                    outputKind: OutputKind.DynamicallyLinkedLibrary,
                    mainTypeName: null,
                    scriptClassName: submissionTypeName,
                    usings: script.Options.Namespaces,
                    optimizationLevel: OptimizationLevel.Debug, // TODO
                    checkOverflow: false,                       // TODO
                    allowUnsafe: true,                          // TODO
                    platform: Platform.AnyCpu,
                    warningLevel: 4,
                    xmlReferenceResolver: null, // don't support XML file references in interactive (permissions & doc comment includes)
                    sourceReferenceResolver: LoadDirectiveResolver.Default,
                    metadataReferenceResolver: script.Options.ReferenceResolver,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default
                ),
                previousSubmission,
                script.ReturnType,
                script.GlobalsType
            );

            return compilation;
        }

        private class LoadDirectiveResolver : SourceFileResolver
        {
            public static new LoadDirectiveResolver Default { get; } = new LoadDirectiveResolver();

            private LoadDirectiveResolver()
                : base(ImmutableArray<string>.Empty, baseDirectory: null)
            {
            }

            public override SourceText ReadText(string resolvedPath)
            {
                string unused;
                return CommonCompiler.ReadFileContentHelper(
                    resolvedPath,
                    encoding: null,
                    checksumAlgorithm: SourceHashAlgorithm.Sha1, // TODO: Should we be fetching the checksum algorithm from somewhere?
                    normalizedFilePath: out unused);
            }
        }
    }
}
