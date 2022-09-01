' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.FileHeaders
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic.FileHeaders
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.FileHeader)>
    <[Shared]>
    Friend Class VisualBasicFileHeaderCodeFixProvider
        Inherits AbstractFileHeaderCodeFixProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides ReadOnly Property FileHeaderHelper As AbstractFileHeaderHelper
            Get
                Return VisualBasicFileHeaderHelper.Instance
            End Get
        End Property
    End Class
End Namespace
