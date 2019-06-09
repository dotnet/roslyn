// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.MoveToNamespace
{
    internal partial class MoveToNamespaceAnalysisResult
    {
        public static readonly MoveToNamespaceAnalysisResult Invalid = new MoveToNamespaceAnalysisResult();

        public bool CanPerform { get; }
        public Document Document { get; }
        public SyntaxNode SyntaxNode { get; }
        public string OriginalNamespace { get; }
        public ContainerType Container { get; }
        public ImmutableArray<string> Namespaces { get; }

        public MoveToNamespaceAnalysisResult(
            Document document,
            SyntaxNode syntaxNode,
            string originalNamespace,
            ImmutableArray<string> namespaces,
            ContainerType container)
        {
            CanPerform = true;
            Document = document;
            SyntaxNode = syntaxNode;
            OriginalNamespace = originalNamespace;
            Container = container;
            Namespaces = namespaces;
        }

        private MoveToNamespaceAnalysisResult()
        {
            CanPerform = false;
        }

    }
}
