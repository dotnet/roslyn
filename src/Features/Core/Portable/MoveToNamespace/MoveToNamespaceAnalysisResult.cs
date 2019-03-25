// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.MoveToNamespace
{
    internal partial class MoveToNamespaceAnalysisResult
    {
        public static MoveToNamespaceAnalysisResult Invalid = new MoveToNamespaceAnalysisResult();

        public bool CanPerform { get; }
        public Document Document { get; }
        public SyntaxNode Container { get; }
        public string OriginalNamespace { get; }
        public ContainerType Type { get; }
        public ImmutableArray<string> Namespaces { get; }

        public MoveToNamespaceAnalysisResult(
            Document document,
            SyntaxNode container,
            string originalNamespace,
            ImmutableArray<string> namespaces,
            ContainerType containerType)
        {
            CanPerform = true;
            Document = document;
            Container = container;
            OriginalNamespace = originalNamespace;
            Type = containerType;
            Namespaces = namespaces;
        }

        private MoveToNamespaceAnalysisResult()
        {
            CanPerform = false;
        }

    }
}
