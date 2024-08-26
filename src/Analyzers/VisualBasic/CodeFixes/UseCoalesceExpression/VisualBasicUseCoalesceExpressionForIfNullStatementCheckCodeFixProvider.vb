' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.UseCoalesceExpression

Namespace Microsoft.CodeAnalysis.VisualBasic.UseCoalesceExpression
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.UseCoalesceExpressionForIfNullStatementCheck), [Shared]>
    <ExtensionOrder(Before:=PredefinedCodeFixProviderNames.AddBraces)>
    Friend Class VisualBasicUseCoalesceExpressionForIfNullStatementCheckCodeFixProvider
        Inherits AbstractUseCoalesceExpressionForIfNullStatementCheckCodeFixProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub
    End Class
End Namespace
