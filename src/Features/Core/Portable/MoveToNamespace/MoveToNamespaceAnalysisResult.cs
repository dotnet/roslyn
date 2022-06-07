// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.MoveToNamespace
{
    internal partial class MoveToNamespaceAnalysisResult
    {
        public static readonly MoveToNamespaceAnalysisResult Invalid = new();

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
            => CanPerform = false;

    }
}
