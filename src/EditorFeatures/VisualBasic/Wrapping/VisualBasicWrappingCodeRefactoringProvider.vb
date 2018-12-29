﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Wrapping.BinaryExpression
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Wrapping.SeparatedSyntaxList
Imports Microsoft.CodeAnalysis.Editor.Wrapping

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Wrapping
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.Wrapping), [Shared]>
    Friend Class VisualBasicWrappingCodeRefactoringProvider
        Inherits AbstractWrappingCodeRefactoringProvider

        Private Shared ReadOnly s_wrappers As ImmutableArray(Of ISyntaxWrapper) =
            ImmutableArray.Create(Of ISyntaxWrapper)(
                New VisualBasicArgumentWrapper(),
                New VisualBasicParameterWrapper(),
                New VisualBasicBinaryExpressionWrapper())

        Public Sub New()
            MyBase.New(s_wrappers)
        End Sub
    End Class
End Namespace
