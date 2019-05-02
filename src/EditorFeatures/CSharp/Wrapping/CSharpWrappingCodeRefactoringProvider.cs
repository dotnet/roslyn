// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Editor.Wrapping.SeparatedSyntaxList;
using Microsoft.CodeAnalysis.Editor.CSharp.Wrapping.BinaryExpression;
using Microsoft.CodeAnalysis.Editor.Wrapping;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Wrapping
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.Wrapping), Shared]
    internal class CSharpWrappingCodeRefactoringProvider : AbstractWrappingCodeRefactoringProvider
    {
        private static readonly ImmutableArray<ISyntaxWrapper> s_wrappers =
            ImmutableArray.Create<ISyntaxWrapper>(
                new CSharpArgumentWrapper(),
                new CSharpParameterWrapper(),
                new CSharpBinaryExpressionWrapper());

#pragma warning disable RS0033 // Importing constructor should be [Obsolete]
        [ImportingConstructor]
#pragma warning restore RS0033 // Importing constructor should be [Obsolete]
        public CSharpWrappingCodeRefactoringProvider()
            : base(s_wrappers)
        {
        }
    }
}
