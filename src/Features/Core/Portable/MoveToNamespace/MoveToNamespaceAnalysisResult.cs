// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Microsoft.CodeAnalysis.MoveToNamespace
{
    internal class MoveToNamespaceAnalysisResult
    {
        public bool CanPerform { get; }
        public Document Document { get; }
        public string ErrorMessage { get; }
        public SyntaxNode Container { get; }
        public string OriginalNamespace { get; }
        public ContainerType Type { get; }

        public MoveToNamespaceAnalysisResult(
            Document document,
            SyntaxNode container,
            string originalNamespace,
            ContainerType containerType)
        {
            CanPerform = true;
            Document = document;
            Container = container;
            OriginalNamespace = originalNamespace;
            Type = containerType;
        }

        public MoveToNamespaceAnalysisResult(string errorMessage)
        {
            CanPerform = false;
            ErrorMessage = errorMessage;
        }

        public enum ContainerType
        {
            Namespace,
            NamedType
        }

    }
}
