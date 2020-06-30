// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SemanticModelReuse;

namespace Microsoft.CodeAnalysis.CSharp.SemanticModelReuse
{
    internal class CSharpSemanticModelReuseLanguageService : ISemanticModelReuseLanguageService
    {
        public SyntaxNode? TryGetContainingMethodBodyForSpeculation(SyntaxNode node)
        {
            throw new NotImplementedException();
        }

        public Task<SemanticModel?> TryGetSpeculativeSemanticModelAsync(
            SemanticModel previousSemanticModel, SyntaxNode currentBodyNode, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
