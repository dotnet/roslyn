// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    internal abstract partial class AbstractSyntaxTreeFactoryService : ISyntaxTreeFactoryService
    {
        private readonly HostLanguageServices languageServices;

        public AbstractSyntaxTreeFactoryService(HostLanguageServices languageServices)
        {
            this.languageServices = languageServices;
        }

        public abstract ParseOptions GetDefaultParseOptions();
        public abstract SyntaxTree CreateSyntaxTree(string filePath, ParseOptions options, SyntaxNode node, Encoding encoding);
        public abstract SyntaxTree ParseSyntaxTree(string filePath, ParseOptions options, SourceText text, CancellationToken cancellationToken);
        public abstract SyntaxTree CreateRecoverableTree(string filePath, ParseOptions options, ValueSource<TextAndVersion> text, SyntaxNode root, bool reparse);
        public abstract SyntaxNode DeserializeNodeFrom(Stream stream, CancellationToken cancellationToken);

        protected static SyntaxNode RecoverNode(SyntaxTree tree, TextSpan textSpan, int kind)
        {
            var token = tree.GetRoot().FindToken(textSpan.Start, findInsideTrivia: true);
            var node = token.Parent;

            while (node != null)
            {
                if (node.Span == textSpan && node.RawKind == kind)
                {
                    return node;
                }

                var structuredTrivia = node as IStructuredTriviaSyntax;
                if (structuredTrivia != null)
                {
                    node = structuredTrivia.ParentTrivia.Token.Parent;
                }
                else
                {
                    node = node.Parent;
                }
            }

            throw ExceptionUtilities.Unreachable;
        }
    }
}