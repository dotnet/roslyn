' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.AddConstructorParametersFromMembers
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.VisualBasic.AddConstructorParametersFromMembers
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.AddConstructorParametersFromMembers), [Shared]>
    Friend Class VisualBasicAddConstructorParametersFromMembersCodeRefactoringProvider
        Inherits AbstractAddConstructorParametersFromMembersCodeRefactoringProvider

        Friend Overrides Function IsPreferThrowExpressionEnabled(optionSet As OptionSet) As Boolean
            Return False
        End Function
    End Class
End Namespace
