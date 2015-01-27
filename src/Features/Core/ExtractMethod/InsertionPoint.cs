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
        private readonly SyntaxAnnotation annotation;
        private readonly Lazy<SyntaxNode> context;

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

            this.SemanticDocument = document;
            this.annotation = annotation;
            this.context = CreateLazyContextNode();
        }

        public SemanticDocument SemanticDocument { get; private set; }

        public SyntaxNode GetRoot()
        {
            return this.SemanticDocument.Root;
        }

        public SyntaxNode GetContext()
        {
            return this.context.Value;
        }

        public InsertionPoint With(SemanticDocument document)
        {
            return new InsertionPoint(document, this.annotation);
        }

        private Lazy<SyntaxNode> CreateLazyContextNode()
        {
            return new Lazy<SyntaxNode>(ComputeContextNode, isThreadSafe: true);
        }

        private SyntaxNode ComputeContextNode()
        {
            var root = this.SemanticDocument.Root;
            return root.GetAnnotatedNodesAndTokens(this.annotation).Single().AsNode();
        }
    }
}
