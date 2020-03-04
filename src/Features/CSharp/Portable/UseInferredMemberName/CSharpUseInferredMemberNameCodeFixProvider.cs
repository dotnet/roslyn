﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.UseInferredMemberName;

namespace Microsoft.CodeAnalysis.CSharp.UseInferredMemberName
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal sealed class CSharpUseInferredMemberNameCodeFixProvider : AbstractUseInferredMemberNameCodeFixProvider
    {
        [ImportingConstructor]
        public CSharpUseInferredMemberNameCodeFixProvider()
        {
        }

        protected override void LanguageSpecificRemoveSuggestedNode(SyntaxEditor editor, SyntaxNode node)
        {
            editor.RemoveNode(node, SyntaxRemoveOptions.KeepExteriorTrivia | SyntaxRemoveOptions.AddElasticMarker);
        }
    }
}
