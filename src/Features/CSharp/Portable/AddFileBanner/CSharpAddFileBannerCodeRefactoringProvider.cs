// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.AddFileBanner;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.CSharp.AddFileBanner
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp,
        Name = PredefinedCodeRefactoringProviderNames.AddFileBanner), Shared]
    internal class CSharpAddFileBannerCodeRefactoringProvider : AbstractAddFileBannerCodeRefactoringProvider
    {
        protected override bool IsCommentStartCharacter(char ch)
            => ch == '/';
    }
}
