// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CSharpSyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using VisualBasicSyntaxKind = Microsoft.CodeAnalysis.VisualBasic.SyntaxKind;

namespace Microsoft.CodeAnalysis.Testing.TestAnalyzers
{
    public abstract class AbstractHighlightBracesAnalyzer : AbstractHighlightTokensAnalyzer
    {
        protected AbstractHighlightBracesAnalyzer(string id = "Brace", string[]? customTags = null)
            : base(id, customTags ?? new string[0], (int)CSharpSyntaxKind.OpenBraceToken, (int)VisualBasicSyntaxKind.OpenBraceToken)
        {
        }
    }
}
