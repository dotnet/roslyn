// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal class InsertionPoint
    {
        private readonly SyntaxAnnotation _annotation;
        private readonly Lazy<SyntaxNode> _context;

        public static async Task<InsertionPoint> CreateAsync(SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken)
        {
            var root = document.Root;
            var annotation = new SyntaxAnnotation();
            var newRoot = root.AddAnnotations(SpecializedCollections.SingletonEnumerable(Tuple.Create(node, annotation)));
            return new InsertionPoint(await document.WithSyntaxRootAsync(newRoot, cancellationToken).ConfigureAwait(false), annotation);
        }

        private InsertionPoint(SemanticDocument document, SyntaxAnnotation annotation)
        {
            Contract.ThrowIfNull(document);
            Contract.ThrowIfNull(annotation);

            SemanticDocument = document;
            _annotation = annotation;
            _context = CreateLazyContextNode();
        }

        public SemanticDocument SemanticDocument { get; }

        public SyntaxNode GetRoot()
        {
            return SemanticDocument.Root;
        }

        public SyntaxNode GetContext()
        {
            return _context.Value;
        }

        public InsertionPoint With(SemanticDocument document)
        {
            return new InsertionPoint(document, _annotation);
        }

        private Lazy<SyntaxNode> CreateLazyContextNode()
        {
            return new Lazy<SyntaxNode>(ComputeContextNode, isThreadSafe: true);
        }

        private SyntaxNode ComputeContextNode()
        {
            var root = SemanticDocument.Root;
            return root.GetAnnotatedNodesAndTokens(_annotation).Single().AsNode();
        }
    }
}
