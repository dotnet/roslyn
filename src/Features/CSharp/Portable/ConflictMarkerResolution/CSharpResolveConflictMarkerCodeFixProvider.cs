// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.ConflictMarkerResolution;

namespace Microsoft.CodeAnalysis.CSharp.ConflictMarkerResolution
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpResolveConflictMarkerCodeFixProvider : AbstractResolveConflictMarkerCodeFixProvider
    {
        private const string CS8300 = nameof(CS8300); // Merge conflict marker encountered

        [ImportingConstructor]
        public CSharpResolveConflictMarkerCodeFixProvider()
            : base(CS8300)
        {
        }

        protected override bool IsConflictMarker(SyntaxTrivia trivia)
            => trivia.Kind() == SyntaxKind.ConflictMarkerTrivia;

        protected override bool IsDisabledText(SyntaxTrivia trivia)
            => trivia.Kind() == SyntaxKind.DisabledTextTrivia;

        protected override bool IsEndOfLine(SyntaxTrivia trivia)
            => trivia.Kind() == SyntaxKind.EndOfLineTrivia;
    }
}
