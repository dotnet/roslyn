' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.MakeDeclarationPartial

Namespace Microsoft.CodeAnalysis.VisualBasic.MakeDeclarationPartial
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.MakeDeclarationPartial), [Shared]>
    Friend Class VisualBasicMakeDeclarationPartialCodeFixProvider
        Inherits AbstractMakeDeclarationPartialCodeFixProvider

        Private Const BC40046 As String = NameOf(BC40046)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(BC40046)
    End Class
End Namespace
