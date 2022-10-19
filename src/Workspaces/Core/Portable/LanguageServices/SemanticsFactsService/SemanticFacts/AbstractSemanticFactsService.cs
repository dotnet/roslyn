// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal abstract partial class AbstractSemanticFactsService : ISemanticFacts
    {
        public string GenerateNameForExpression(SemanticModel semanticModel, SyntaxNode expression, bool capitalize, CancellationToken cancellationToken)
            => SemanticFacts.GenerateNameForExpression(semanticModel, expression, capitalize, cancellationToken);
    }
}
