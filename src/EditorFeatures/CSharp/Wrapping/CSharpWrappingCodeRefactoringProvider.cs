// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Editor.Wrapping.SeparatedSyntaxList;
using Microsoft.CodeAnalysis.Editor.Wrapping;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Wrapping
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.Wrapping), Shared]
    internal class CSharpWrappingCodeRefactoringProvider : AbstractWrappingCodeRefactoringProvider
    {
        private static readonly ImmutableArray<IWrapper> s_wrappers =
            ImmutableArray.Create<IWrapper>(
                new CSharpArgumentWrapper(),
                new CSharpParameterWrapper());

        public CSharpWrappingCodeRefactoringProvider()
            : base(s_wrappers)
        {
        }
    }
}
