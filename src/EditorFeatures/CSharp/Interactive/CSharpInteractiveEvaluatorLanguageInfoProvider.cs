// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using Microsoft.CodeAnalysis.Interactive;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Interactive
{
    internal sealed class CSharpInteractiveEvaluatorLanguageInfoProvider : InteractiveEvaluatorLanguageInfoProvider
    {
        public static readonly CSharpInteractiveEvaluatorLanguageInfoProvider Instance = new();

        private CSharpInteractiveEvaluatorLanguageInfoProvider()
        {
        }

        private static readonly CSharpParseOptions s_parseOptions =
            new(languageVersion: LanguageVersion.Latest, kind: SourceCodeKind.Script);

        public override string LanguageName
            => LanguageNames.CSharp;

        public override ParseOptions ParseOptions
            => s_parseOptions;

        public override CommandLineParser CommandLineParser
            => CSharpCommandLineParser.Script;

        public override CompilationOptions GetSubmissionCompilationOptions(string name, MetadataReferenceResolver metadataReferenceResolver, SourceReferenceResolver sourceReferenceResolver, ImmutableArray<string> imports)
            => CSharpScriptCompiler.WithTopLevelBinderFlags(
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    scriptClassName: name,
                    allowUnsafe: true,
                    xmlReferenceResolver: null, // no support for permission set and doc includes in interactive
                    usings: imports,
                    sourceReferenceResolver: sourceReferenceResolver,
                    metadataReferenceResolver: metadataReferenceResolver,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default));

        public override bool IsCompleteSubmission(string text)
            => SyntaxFactory.IsCompleteSubmission(SyntaxFactory.ParseSyntaxTree(text, options: s_parseOptions));

        public override string InteractiveResponseFileName
            => "CSharpInteractive.rsp";

        public override Type ReplServiceProviderType
            => typeof(CSharpReplServiceProvider);
    }
}
