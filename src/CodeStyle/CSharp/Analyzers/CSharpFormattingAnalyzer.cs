// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpFormattingAnalyzer : AbstractFormattingAnalyzer
    {
        protected override ISyntaxFormatting SyntaxFormatting
            => CSharpSyntaxFormatting.Instance;
    }
}
