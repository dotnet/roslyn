// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
#if MEF
    [ExportLanguageServiceFactory(typeof(ISyntaxTreeFactoryService), LanguageNames.CSharp)]
#endif
    internal partial class CSharpSyntaxTreeFactoryServiceFactory : ILanguageServiceFactory
    {
        public ILanguageService CreateLanguageService(ILanguageServiceProvider provider)
        {
            return new CSharpSyntaxTreeFactoryService(provider);
        }

        private partial class CSharpSyntaxTreeFactoryService : AbstractSyntaxTreeFactoryService
        {
            public CSharpSyntaxTreeFactoryService(ILanguageServiceProvider languageServices) : base(languageServices)
            {
            }

            public override ParseOptions GetDefaultParseOptions()
            {
                return CSharpParseOptions.Default;
            }

            public override SyntaxTree CreateSyntaxTree(string fileName, ParseOptions options, SyntaxNode node)
            {
                options = options ?? GetDefaultParseOptions();
                return SyntaxFactory.SyntaxTree(node, fileName, options);
            }

            public override SyntaxTree ParseSyntaxTree(string fileName, ParseOptions options, SourceText text, CancellationToken cancellationToken)
            {
                options = options ?? GetDefaultParseOptions();
                return SyntaxFactory.ParseSyntaxTree(text, fileName, options, cancellationToken: cancellationToken);
            }

            public override SyntaxNode DeserializeNodeFrom(Stream stream, CancellationToken cancellationToken)
            {
                return CSharpSyntaxNode.DeserializeFrom(stream, cancellationToken);
            }

            public override SyntaxTree CreateRecoverableTree(string filePath, ParseOptions options, ValueSource<TextAndVersion> text, SyntaxNode root, bool reparse)
            {
                options = options ?? GetDefaultParseOptions();

                if (reparse)
                {
                    return new ReparsedSyntaxTree(this, filePath, (CSharpParseOptions)options, text, (CompilationUnitSyntax)root);
                }
                else
                {
                    return new SerializedSyntaxTree(this, filePath, (CSharpParseOptions)options, text, (CompilationUnitSyntax)root);
                }
            }
        }
    }
}