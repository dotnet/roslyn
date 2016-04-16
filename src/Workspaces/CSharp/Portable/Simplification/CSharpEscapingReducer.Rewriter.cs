// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    internal partial class CSharpEscapingReducer
    {
        private class Rewriter : AbstractExpressionRewriter
        {
            public Rewriter(CSharpEscapingReducer escapingSimplifierService, OptionSet optionSet, CancellationToken cancellationToken)
                : base(optionSet, cancellationToken)
            {
                _escapingSimplifierService = escapingSimplifierService;
            }

            private readonly CSharpEscapingReducer _escapingSimplifierService;

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                var newToken = base.VisitToken(token);
                return SimplifyToken(newToken, _escapingSimplifierService.SimplifyIdentifierToken);
            }
        }
    }
}
