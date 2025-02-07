// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Analyzer.Utilities.Extensions
{
    internal static class SyntaxTreeExtensions
    {
        public static bool OverlapsHiddenPosition([NotNullWhen(returnValue: true)] this SyntaxTree? tree, TextSpan span, CancellationToken cancellationToken)
        {
            if (tree == null)
            {
                return false;
            }

            var text = tree.GetText(cancellationToken);

            return text.OverlapsHiddenPosition(
                span,
                (position, cancellationToken2) =>
                {
                    // implements the ASP.NET IsHidden rule
                    var lineVisibility = tree.GetLineVisibility(position, cancellationToken2);
                    return lineVisibility is LineVisibility.Hidden or LineVisibility.BeforeFirstLineDirective;
                },
                cancellationToken);
        }
    }
}
