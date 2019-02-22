// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.GenerateConstructorFromMembers;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PickMembers;

namespace Microsoft.CodeAnalysis.CSharp.GenerateConstructorFromMembers
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.GenerateConstructorFromMembers), Shared]
    class CSharpGenerateConstructorFromMembersCodeRefactoringProvider : AbstractGenerateConstructorFromMembersCodeRefactoringProvider
    {
        [ImportingConstructor]
        public CSharpGenerateConstructorFromMembersCodeRefactoringProvider(IPickMembersService membersService)
            : base(membersService)
        {
        }

        internal override bool IsPreferThrowExpressionEnabled(OptionSet options)
            => options.GetOption(CSharpCodeStyleOptions.PreferThrowExpression).Value;
    }
}
