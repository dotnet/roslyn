// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.UseInferredMemberName;

namespace Microsoft.CodeAnalysis.CSharp.UseInferredMemberName
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal sealed class CSharpUseInferredMemberNameCodeFixProvider : AbstractUseInferredMemberNameCodeFixProvider
    {
#pragma warning disable RS0033 // Importing constructor should be [Obsolete]
        [ImportingConstructor]
#pragma warning restore RS0033 // Importing constructor should be [Obsolete]
        public CSharpUseInferredMemberNameCodeFixProvider()
        {
        }

        protected override void LanguageSpecificRemoveSuggestedNode(SyntaxEditor editor, SyntaxNode node)
        {
            editor.RemoveNode(node, SyntaxRemoveOptions.KeepExteriorTrivia | SyntaxRemoveOptions.AddElasticMarker);
        }
    }
}
