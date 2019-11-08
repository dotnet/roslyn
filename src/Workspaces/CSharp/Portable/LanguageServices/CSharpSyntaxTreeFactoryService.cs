// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    [ExportLanguageServiceFactory(typeof(ISyntaxTreeFactoryService), LanguageNames.CSharp), Shared]
    internal partial class CSharpSyntaxTreeFactoryServiceFactory : ILanguageServiceFactory
    {
        private static readonly CSharpParseOptions _parseOptionWithLatestLanguageVersion = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);

        [ImportingConstructor]
        public CSharpSyntaxTreeFactoryServiceFactory()
        {
        }

        public ILanguageService CreateLanguageService(HostLanguageServices provider)
        {
            return new CSharpSyntaxTreeFactoryService(provider);
        }

        private partial class CSharpSyntaxTreeFactoryService : AbstractSyntaxTreeFactoryService
        {
            public CSharpSyntaxTreeFactoryService(HostLanguageServices languageServices) : base(languageServices)
            {
            }

            public override ParseOptions GetDefaultParseOptions()
            {
                return CSharpParseOptions.Default;
            }

            public override ParseOptions GetDefaultParseOptionsWithLatestLanguageVersion()
            {
                return _parseOptionWithLatestLanguageVersion;
            }

            public override SyntaxTree CreateSyntaxTree(string filePath, ParseOptions options, Encoding encoding, SyntaxNode root, ImmutableDictionary<string, ReportDiagnostic> treeDiagnosticReportingOptionsOpt)
            {
                options ??= GetDefaultParseOptions();
                return CSharpSyntaxTree.Create((CSharpSyntaxNode)root, (CSharpParseOptions)options, filePath, encoding, treeDiagnosticReportingOptionsOpt);
            }

            public override SyntaxTree ParseSyntaxTree(string filePath, ParseOptions options, SourceText text, ImmutableDictionary<string, ReportDiagnostic> treeDiagnosticReportingOptionsOpt, CancellationToken cancellationToken)
            {
                options ??= GetDefaultParseOptions();
                return SyntaxFactory.ParseSyntaxTree(text, options, filePath, treeDiagnosticReportingOptionsOpt, cancellationToken: cancellationToken);
            }

            public override SyntaxNode DeserializeNodeFrom(Stream stream, CancellationToken cancellationToken)
                => CSharpSyntaxNode.DeserializeFrom(stream, cancellationToken);

            public override bool CanCreateRecoverableTree(SyntaxNode root)
            {
                return base.CanCreateRecoverableTree(root) && root is CompilationUnitSyntax { AttributeLists: { Count: 0 } } cu;
            }

            public override SyntaxTree CreateRecoverableTree(
                ProjectId cacheKey,
                string filePath,
                ParseOptions options,
                ValueSource<TextAndVersion> text,
                Encoding encoding,
                SyntaxNode root,
                ImmutableDictionary<string, ReportDiagnostic> treeDiagnosticReportingOptionsOpt)
            {
                System.Diagnostics.Debug.Assert(CanCreateRecoverableTree(root));
                return RecoverableSyntaxTree.CreateRecoverableTree(
                    this,
                    cacheKey,
                    filePath,
                    options ?? GetDefaultParseOptions(),
                    text,
                    encoding,
                    (CompilationUnitSyntax)root,
                    treeDiagnosticReportingOptionsOpt);
            }
        }
    }
}
