// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.AddConstructorParametersFromMembers;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.CSharp.AddConstructorParametersFromMembers
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.AddConstructorParametersFromMembers), Shared]
    class CSharpAddConstructorParametersFromMembersCodeRefactoringProvider : AbstractAddConstructorParametersFromMembersCodeRefactoringProvider
    {
        internal override bool IsPreferThrowExpressionEnabled(OptionSet optionSet)
            => optionSet.GetOption(CSharpCodeStyleOptions.PreferThrowExpression).Value;
    }
}
