' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.InlineRename
    <ExportLanguageService(GetType(IEditorInlineRenameService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicEditorInlineRenameService
        Inherits AbstractEditorInlineRenameService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(
            <ImportMany> refactorNotifyServices As IEnumerable(Of IRefactorNotifyService), globalOptions As IGlobalOptionService)
            MyBase.New(refactorNotifyServices, globalOptions)
        End Sub

        Protected Overrides Function CheckLanguageSpecificIssues(semanticModel As SemanticModel, symbol As ISymbol, triggerToken As SyntaxToken, ByRef langError As String) As Boolean
            Return False
        End Function
    End Class
End Namespace
