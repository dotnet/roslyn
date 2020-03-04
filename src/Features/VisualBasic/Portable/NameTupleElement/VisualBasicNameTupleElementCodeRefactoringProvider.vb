﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.NameTupleElement
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.NameTupleElement
    <ExtensionOrder(After:=PredefinedCodeRefactoringProviderNames.IntroduceVariable)>
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicNameTupleElementCodeRefactoringProvider)), [Shared]>
    Friend Class VisualBasicNameTupleElementCodeRefactoringProvider
        Inherits AbstractNameTupleElementCodeRefactoringProvider(Of SimpleArgumentSyntax, TupleExpressionSyntax)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Function WithName(argument As SimpleArgumentSyntax, name As String) As SimpleArgumentSyntax
            Return argument.WithNameColonEquals(SyntaxFactory.NameColonEquals(name.ToIdentifierName()))
        End Function
    End Class
End Namespace
