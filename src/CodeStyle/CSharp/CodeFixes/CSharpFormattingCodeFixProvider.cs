// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.FixFormatting)]
    [Shared]
    internal class CSharpFormattingCodeFixProvider : AbstractFormattingCodeFixProvider
    {
        protected override ISyntaxFormattingService SyntaxFormattingService => new CSharpSyntaxFormattingService();
    }
}
