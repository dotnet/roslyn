// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.Interactive;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Interactive;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.InteractiveWindow.Commands;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Interactive
{
    internal sealed class CSharpInteractiveEvaluator : InteractiveEvaluator
    {
        private static readonly CSharpParseOptions s_parseOptions =
            new CSharpParseOptions(languageVersion: LanguageVersion.CSharp6, kind: SourceCodeKind.Script);

        private const string InteractiveResponseFile = "CSharpInteractive.rsp";

        public CSharpInteractiveEvaluator(
            HostServices hostServices,
            IViewClassifierAggregatorService classifierAggregator,
            IInteractiveWindowCommandsFactory commandsFactory,
            ImmutableArray<IInteractiveWindowCommand> commands,
            IContentTypeRegistryService contentTypeRegistry,
            string responseFileDirectory,
            string initialWorkingDirectory)
            : base(
                contentTypeRegistry.GetContentType(ContentTypeNames.CSharpContentType),
                hostServices,
                classifierAggregator,
                commandsFactory,
                commands,
                (responseFileDirectory != null) ? Path.Combine(responseFileDirectory, InteractiveResponseFile) : null,
                initialWorkingDirectory,
                typeof(InteractiveHostEntryPoint).Assembly.Location,
                typeof(CSharpReplServiceProvider))
        {
        }

        protected override string LanguageName
        {
            get { return LanguageNames.CSharp; }
        }

        protected override ParseOptions ParseOptions
        {
            get { return s_parseOptions; }
        }

        protected override CompilationOptions GetSubmissionCompilationOptions(string name, MetadataReferenceResolver metadataReferenceResolver, SourceReferenceResolver sourceReferenceResolver, ImmutableArray<string> imports)
        {
            return new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                scriptClassName: name,
                allowUnsafe: true,
                xmlReferenceResolver: null, // no support for permission set and doc includes in interactive
                usings: imports,
                sourceReferenceResolver: sourceReferenceResolver,
                metadataReferenceResolver: metadataReferenceResolver,
                assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default);
        }

        public override bool CanExecuteCode(string text)
        {
            if (base.CanExecuteCode(text))
            {
                return true;
            }

            return SyntaxFactory.IsCompleteSubmission(SyntaxFactory.ParseSyntaxTree(text, options: s_parseOptions));
        }

        protected override CommandLineParser CommandLineParser
        {
            get { return CSharpCommandLineParser.ScriptRunner; }
        }
    }
}
