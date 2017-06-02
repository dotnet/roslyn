// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal sealed class InternalsVisibleToCompletionProvider : AbstractInternalsVisibleToCompletionProvider
    {
        internal override bool IsInsertionTrigger(SourceText text, int insertedCharacterPosition, OptionSet options)
        {
            var ch = text[insertedCharacterPosition];
            return ch == '\"';
        }

        protected override bool IsPositionEntirelyWithinStringLiteral(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
            => syntaxTree.IsEntirelyWithinStringLiteral(position, cancellationToken);
    }
}
