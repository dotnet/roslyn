' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.Wrapping.BinaryExpression
Imports Microsoft.CodeAnalysis.VisualBasic.Wrapping.ChainedExpression
Imports Microsoft.CodeAnalysis.VisualBasic.Wrapping.SeparatedSyntaxList
Imports Microsoft.CodeAnalysis.Wrapping

Namespace Microsoft.CodeAnalysis.VisualBasic.Wrapping
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.Wrapping), [Shared]>
    Friend Class VisualBasicWrappingCodeRefactoringProvider
        Inherits AbstractWrappingCodeRefactoringProvider

        Private Shared ReadOnly s_wrappers As ImmutableArray(Of ISyntaxWrapper) =
            ImmutableArray.Create(Of ISyntaxWrapper)(
                New VisualBasicArgumentWrapper(),
                New VisualBasicParameterWrapper(),
                New VisualBasicBinaryExpressionWrapper(),
                New VisualBasicChainedExpressionWrapper(),
                New VisualBasicCollectionCreationExpressionWrapper())

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
            MyBase.New(s_wrappers)
        End Sub

        Protected Overrides Function GetWrappingOptions(options As IOptionsReader) As SyntaxWrappingOptions
            Return VisualBasicSyntaxWrappingOptions.Create(options)
        End Function
    End Class
End Namespace
