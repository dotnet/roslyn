// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal sealed class InternalsVisibleToCompletionProvider : AbstractInternalsVisibleToCompletionProvider
    {
        protected override bool IsPositionEntirelyWithinStringLiteral(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
            => syntaxTree.IsEntirelyWithinStringLiteral(position, cancellationToken);
    }
}
