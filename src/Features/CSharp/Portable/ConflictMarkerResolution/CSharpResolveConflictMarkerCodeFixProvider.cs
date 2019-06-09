// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
