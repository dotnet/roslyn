// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal class UniqueNameGenerator
    {
        private readonly SemanticModel _semanticModel;

        public UniqueNameGenerator(SemanticModel semanticModel)
        {
            Contract.ThrowIfNull(semanticModel);
            _semanticModel = semanticModel;
        }

        public string CreateUniqueMethodName(SyntaxNode contextNode, string baseName, bool generateLocalFunction = false)
        {
            Contract.ThrowIfNull(contextNode);
            Contract.ThrowIfNull(baseName);

            if (generateLocalFunction)
            {
                // When generating local functions, we also want to take into account the names of local variables.
                var childNodes = contextNode.ChildNodes().AsArray();
                if (childNodes.Length > 0)
                {
                    // If we take the first child node, we may end up with part of the method header.
                    // To be safe, we use the last child node, which is guaranteed to be part of the context method's body.
                    contextNode = (SyntaxNode)childNodes.GetValue(childNodes.Length - 1);
                }
            }

            return NameGenerator.GenerateUniqueName(baseName, string.Empty,
                n => _semanticModel.LookupSymbols(contextNode.SpanStart, container: null, n).Length == 0);
        }
    }
}
