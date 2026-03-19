// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    partial class SubpatternSyntax
    {
        public NameColonSyntax? NameColon => ExpressionColon as NameColonSyntax;

        public SubpatternSyntax WithNameColon(NameColonSyntax? nameColon)
            => WithExpressionColon(nameColon);

        public SubpatternSyntax Update(NameColonSyntax? nameColon, PatternSyntax pattern)
            => Update((BaseExpressionColonSyntax?)nameColon, pattern);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static SubpatternSyntax Subpattern(NameColonSyntax? nameColon, PatternSyntax pattern)
            => Subpattern((BaseExpressionColonSyntax?)nameColon, pattern);
    }
}
