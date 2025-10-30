// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

internal static partial class SyntaxTreeExtensions
{
    public static bool IsPreProcessorDirectiveContext(this SyntaxTree syntaxTree, int position, SyntaxToken preProcessorTokenOnLeftOfPosition, CancellationToken cancellationToken)
    {
        var token = preProcessorTokenOnLeftOfPosition;
        var directive = token.GetAncestor<DirectiveTriviaSyntax>();

        // Directives contain the EOL, so if the position is within the full span of the
        // directive, then it is on that line, the only exception is if the directive is on the
        // last line, the position at the end if technically not contained by the directive but
        // its also not on a new line, so it should be considered part of the preprocessor
        // context.
        if (directive == null)
        {
            return false;
        }

        return
            directive.FullSpan.Contains(position) ||
            directive.FullSpan.End == syntaxTree.GetRoot(cancellationToken).FullSpan.End;
    }
}
