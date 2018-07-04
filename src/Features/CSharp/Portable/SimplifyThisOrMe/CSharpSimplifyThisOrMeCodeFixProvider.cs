// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.SimplifyThisOrMe;

namespace Microsoft.CodeAnalysis.CSharp.SimplifyThisOrMe
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.SimplifyThisOrMe), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.RemoveUnnecessaryCast)]
    internal partial class CSharpSimplifyThisOrMeCodeFixProvider 
        : AbstractSimplifyThisOrMeCodeFixProvider<MemberAccessExpressionSyntax>
    {
        public CSharpSimplifyThisOrMeCodeFixProvider() 
        {
        }

        protected override string GetTitle()
            => CSharpFeaturesResources.Remove_this_qualification;

        protected override SyntaxNode GetNameWithTriviaMoved(
            SemanticModel semantic, MemberAccessExpressionSyntax memberAccess)
        {
            return memberAccess.GetNameWithTriviaMoved();
        }
    }
}
