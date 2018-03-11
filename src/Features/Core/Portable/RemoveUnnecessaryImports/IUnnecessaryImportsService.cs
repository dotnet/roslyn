// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryImports
{
    internal interface IUnnecessaryImportsService : ILanguageService
    {
        ImmutableArray<SyntaxNode> GetUnnecessaryImports(SemanticModel semanticModel, CancellationToken cancellationToken);
    }
}
