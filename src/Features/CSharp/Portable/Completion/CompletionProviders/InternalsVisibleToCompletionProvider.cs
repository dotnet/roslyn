// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal sealed class InternalsVisibleToCompletionProvider : AbstractInternalsVisibleToCompletionProvider
    {
        internal override bool IsInsertionTrigger(SourceText text, int insertedCharacterPosition, OptionSet options)
        {
            var ch = text[insertedCharacterPosition];
            if (ch == '\"')
            {
                return true;
            }

            return false;
        }

        protected override bool IsPositionEntirelyWithinStringLiteral(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
            => syntaxTree.IsEntirelyWithinStringLiteral(position, cancellationToken);

        protected override string GetAttributeNameOfAttributeSyntaxNode(SyntaxNode attributeSyntaxNode)
        {
            if (attributeSyntaxNode is AttributeSyntax attributeSyntax)
            {
                if (attributeSyntax.Name.TryGetNameParts(out var nameParts) && nameParts.Count > 0)
                {
                    var lastName = nameParts[nameParts.Count - 1];
                    return lastName;
                }
            }
            return string.Empty;
        }
    }
}
