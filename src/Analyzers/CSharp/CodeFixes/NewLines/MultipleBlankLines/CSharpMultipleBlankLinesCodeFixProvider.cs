// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.NewLines.MultipleBlankLines;

namespace Microsoft.CodeAnalysis.CSharp.NewLines.MultipleBlankLines
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal sealed class CSharpMultipleBlankLinesCodeFixProvider : AbstractMultipleBlankLinesCodeFixProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpMultipleBlankLinesCodeFixProvider()
        {
        }

        protected override bool IsEndOfLine(SyntaxTrivia trivia)
            => trivia.IsKind(SyntaxKind.EndOfLineTrivia);
    }
}
