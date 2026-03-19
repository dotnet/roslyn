' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.MakeTypePartial

Namespace Microsoft.CodeAnalysis.VisualBasic.MakeTypePartial
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.MakeTypePartial), [Shared]>
    Friend Class VisualBasicMakeTypePartialCodeFixProvider
        Inherits AbstractMakeTypePartialCodeFixProvider

        Private Const BC40046 As String = NameOf(BC40046) ' Type 'D' and partial type 'D' conflict in container 'C', but are being merged because one of them is declared partial

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(BC40046)
    End Class
End Namespace
