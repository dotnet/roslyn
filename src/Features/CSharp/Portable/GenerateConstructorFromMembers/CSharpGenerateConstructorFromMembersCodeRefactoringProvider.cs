// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        protected override bool PrefersThrowExpression(DocumentOptionSet options)
            => options.GetOption(CSharpCodeStyleOptions.PreferThrowExpression).Value;
    }
}
