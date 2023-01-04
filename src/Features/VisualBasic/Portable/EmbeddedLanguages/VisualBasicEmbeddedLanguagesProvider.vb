' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.EmbeddedLanguages
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.EmbeddedLanguages.VirtualChars
Imports Microsoft.CodeAnalysis.VisualBasic.Features.EmbeddedLanguages
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService

Namespace Microsoft.CodeAnalysis.VisualBasic.EmbeddedLanguages.LanguageServices
    <ExportLanguageService(GetType(IEmbeddedLanguagesProvider), LanguageNames.VisualBasic, ServiceLayer.Default), [Shared]>
    Friend Class VisualBasicEmbeddedLanguagesProvider
        Inherits AbstractEmbeddedLanguagesProvider

        Public Shared ReadOnly Info As New EmbeddedLanguageInfo(
            VisualBasicSyntaxFacts.Instance,
            VisualBasicSemanticFactsService.Instance,
            VisualBasicVirtualCharService.Instance)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
            MyBase.New(Info)
        End Sub

        Public Overrides Function EscapeText(text As String, token As SyntaxToken) As String
            Return EmbeddedLanguageUtilities.EscapeText(text)
        End Function
    End Class
End Namespace
