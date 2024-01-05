' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.Formatting

Namespace Microsoft.CodeAnalysis.CodeStyle
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.FixFormatting)>
    <[Shared]>
    Friend Class VisualBasicFormattingCodeFixProvider
        Inherits AbstractFormattingCodeFixProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides ReadOnly Property SyntaxFormatting As ISyntaxFormatting
            Get
                Return VisualBasicSyntaxFormatting.Instance
            End Get
        End Property
    End Class
End Namespace

