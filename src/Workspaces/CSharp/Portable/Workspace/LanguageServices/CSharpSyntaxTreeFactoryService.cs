// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpSyntaxTreeFactoryServiceFactory()
        {
        }

        public ILanguageService CreateLanguageService(HostLanguageServices provider)
            => new CSharpSyntaxTreeFactoryService(provider);

        private partial class CSharpSyntaxTreeFactoryService : AbstractSyntaxTreeFactoryService
        {
            public CSharpSyntaxTreeFactoryService(HostLanguageServices languageServices) : base(languageServices)
            {
            }

            public override ParseOptions GetDefaultParseOptions()
                => CSharpParseOptions.Default;

            public override ParseOptions GetDefaultParseOptionsWithLatestLanguageVersion()
                => _parseOptionWithLatestLanguageVersion;

            public override SyntaxTree CreateSyntaxTree(string filePath, ParseOptions options, Encoding encoding, SyntaxNode root)
            {
                options ??= GetDefaultParseOptions();
                return CSharpSyntaxTree.Create((CSharpSyntaxNode)root, (CSharpParseOptions)options, filePath, encoding);
            }

            public override SyntaxTree ParseSyntaxTree(string filePath, ParseOptions options, SourceText text, CancellationToken cancellationToken)
            {
                options ??= GetDefaultParseOptions();
                return SyntaxFactory.ParseSyntaxTree(text, options, filePath, cancellationToken: cancellationToken);
            }

            public override SyntaxNode DeserializeNodeFrom(Stream stream, CancellationToken cancellationToken)
                => CSharpSyntaxNode.DeserializeFrom(stream, cancellationToken);

            public override bool CanCreateRecoverableTree(SyntaxNode root)
                => base.CanCreateRecoverableTree(root) && root is CompilationUnitSyntax cu && cu.AttributeLists.Count == 0;

            public override SyntaxTree CreateRecoverableTree(
                ProjectId cacheKey,
                string filePath,
                ParseOptions options,
                ValueSource<TextAndVersion> text,
                Encoding encoding,
                SyntaxNode root)
            {
                System.Diagnostics.Debug.Assert(CanCreateRecoverableTree(root));
                return RecoverableSyntaxTree.CreateRecoverableTree(
                    this,
                    cacheKey,
                    filePath,
                    options ?? GetDefaultParseOptions(),
                    text,
                    encoding,
                    (CompilationUnitSyntax)root);
            }
        }
    }
}
