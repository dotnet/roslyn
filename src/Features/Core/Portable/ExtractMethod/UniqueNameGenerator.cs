// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod;

internal sealed class UniqueNameGenerator
{
    private readonly SemanticModel _semanticModel;

    public UniqueNameGenerator(SemanticModel semanticModel)
    {
        Contract.ThrowIfNull(semanticModel);
        _semanticModel = semanticModel;
    }

    public string CreateUniqueMethodName(SyntaxNode contextNode, string baseName)
    {
        Contract.ThrowIfNull(contextNode);
        Contract.ThrowIfNull(baseName);

        return NameGenerator.GenerateUniqueName(baseName, string.Empty,
            n => _semanticModel.LookupSymbols(contextNode.SpanStart, container: null, n).Length == 0);
    }
}
