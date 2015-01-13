// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public ILanguageService CreateLanguageService(HostLanguageServices provider)
        {
            return new CSharpSyntaxTreeFactoryService(provider);
        }

        internal partial class CSharpSyntaxTreeFactoryService : AbstractSyntaxTreeFactoryService
        {
            public CSharpSyntaxTreeFactoryService(HostLanguageServices languageServices) : base(languageServices)
            {
            }

            public override ParseOptions GetDefaultParseOptions()
            {
                return CSharpParseOptions.Default;
            }

            public override SyntaxTree CreateSyntaxTree(string fileName, ParseOptions options, SyntaxNode node, Encoding encoding)
            {
                options = options ?? GetDefaultParseOptions();
                return SyntaxFactory.SyntaxTree(node, options, fileName, encoding);
            }

            public override SyntaxTree ParseSyntaxTree(string fileName, ParseOptions options, SourceText text, CancellationToken cancellationToken)
            {
                options = options ?? GetDefaultParseOptions();
                return SyntaxFactory.ParseSyntaxTree(text, options, fileName, cancellationToken: cancellationToken);
            }

            public override SyntaxNode DeserializeNodeFrom(Stream stream, CancellationToken cancellationToken)
            {
                return CSharpSyntaxNode.DeserializeFrom(stream, cancellationToken);
            }

            public override SyntaxTree CreateRecoverableTree(ProjectId cacheKey, string filePath, ParseOptions optionsOpt, ValueSource<TextAndVersion> text, SyntaxNode root)
            {
                return RecoverableSyntaxTree.CreateRecoverableTree(this, cacheKey, filePath, optionsOpt ?? GetDefaultParseOptions(), text, (CompilationUnitSyntax)root);
            }
        }
    }
}