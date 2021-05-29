// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundSequencePoint
    {
        public static BoundStatement Create(SyntaxNode? syntax, TextSpan? part, BoundStatement statement, bool hasErrors = false)
        {
            if (part.HasValue)
            {
                // A bound sequence point is permitted to have a null syntax to make a hidden sequence point.
                return new BoundSequencePointWithSpan(syntax!, statement, part.Value, hasErrors);
            }
            else
            {
                // A bound sequence point is permitted to have a null syntax to make a hidden sequence point.
                return new BoundSequencePoint(syntax!, statement, hasErrors);
            }
        }

        public static BoundStatement Create(SyntaxNode? syntax, BoundStatement? statementOpt, bool hasErrors = false, bool wasCompilerGenerated = false)
        {
            // A bound sequence point is permitted to have a null syntax to make a hidden sequence point.
            return new BoundSequencePoint(syntax!, statementOpt, hasErrors) { WasCompilerGenerated = wasCompilerGenerated };
        }

        public static BoundStatement CreateHidden(BoundStatement? statementOpt = null, bool hasErrors = false)
        {
            // A bound sequence point is permitted to have a null syntax to make a hidden sequence point.
            return new BoundSequencePoint(null!, statementOpt, hasErrors) { WasCompilerGenerated = true };
        }
    }
}
