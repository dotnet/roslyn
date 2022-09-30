// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    internal abstract partial class AbstractSyntaxTreeFactoryService : ISyntaxTreeFactoryService
    {
        private readonly Lazy<int> _minimumLengthForRecoverableTree;

        internal SolutionServices SolutionServices { get; }

        public AbstractSyntaxTreeFactoryService(SolutionServices services)
        {
            SolutionServices = services;

            // Create this lazily; this ultimately needs to read options under the covers, which isn't necessary until
            // we are actually parsing stuff. This moves some extra loading of MEF parts and services out of the solution load
            // path.
            _minimumLengthForRecoverableTree = new Lazy<int>(() =>
            {
                var cacheService = SolutionServices.GetService<IProjectCacheHostService>();
                return (cacheService != null) ? cacheService.MinimumLengthForRecoverableTree : int.MaxValue;
            });
        }

        public abstract ParseOptions GetDefaultParseOptions();
        public abstract ParseOptions GetDefaultParseOptionsWithLatestLanguageVersion();
        public abstract bool OptionsDifferOnlyByPreprocessorDirectives(ParseOptions options1, ParseOptions options2);
        public abstract ParseOptions TryParsePdbParseOptions(IReadOnlyDictionary<string, string> metadata);
        public abstract SyntaxTree CreateSyntaxTree(string filePath, ParseOptions options, Encoding encoding, SyntaxNode root);
        public abstract SyntaxTree ParseSyntaxTree(string filePath, ParseOptions options, SourceText text, CancellationToken cancellationToken);
        public abstract SyntaxTree CreateRecoverableTree(ProjectId cacheKey, string filePath, ParseOptions options, ValueSource<TextAndVersion> text, Encoding encoding, SyntaxNode root);
        public abstract SyntaxNode DeserializeNodeFrom(Stream stream, CancellationToken cancellationToken);

        public virtual bool CanCreateRecoverableTree(SyntaxNode root)
            => root.FullSpan.Length > _minimumLengthForRecoverableTree.Value;

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

                if (node is IStructuredTriviaSyntax structuredTrivia)
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
