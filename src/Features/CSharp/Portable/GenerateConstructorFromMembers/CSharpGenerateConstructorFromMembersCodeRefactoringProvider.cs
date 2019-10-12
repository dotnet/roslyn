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
    [ExtensionOrder(Before = PredefinedCodeRefactoringProviderNames.GenerateEqualsAndGetHashCodeFromMembers)]
    internal sealed class CSharpGenerateConstructorFromMembersCodeRefactoringProvider
        : AbstractGenerateConstructorFromMembersCodeRefactoringProvider
    {
        [ImportingConstructor]
        public CSharpGenerateConstructorFromMembersCodeRefactoringProvider()
        {
        }

        /// <summary>
        /// For testing purposes only.
        /// </summary>
        internal CSharpGenerateConstructorFromMembersCodeRefactoringProvider(IPickMembersService pickMembersService_forTesting)
            : base(pickMembersService_forTesting)
        {
        }

        protected override bool GetPreferThrowExpressionOptionValue(DocumentOptionSet options)
        {
            return options.GetOption(CSharpCodeStyleOptions.PreferThrowExpression).Value;
        }
    }
}
