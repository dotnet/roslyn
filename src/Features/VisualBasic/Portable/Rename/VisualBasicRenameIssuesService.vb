' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.Rename
    <ExportLanguageService(GetType(IRenameIssuesService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicRenameIssuesService
        Implements IRenameIssuesService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Function CheckLanguageSpecificIssues(semantic As SemanticModel, symbol As ISymbol, triggerToken As SyntaxToken, <NotNullWhen(True)> ByRef langError As String) As Boolean Implements IRenameIssuesService.CheckLanguageSpecificIssues
            Return False
        End Function
    End Class
End Namespace
