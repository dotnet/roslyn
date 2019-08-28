' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.EmbeddedLanguages.VirtualChars

Namespace Microsoft.CodeAnalysis.VisualBasic.EmbeddedLanguages.LanguageServices
    <ExportLanguageService(GetType(IEmbeddedLanguagesProvider), LanguageNames.VisualBasic, ServiceLayer.Default), [Shared]>
    Friend Class VisualBasicEmbeddedLanguagesProvider
        Inherits AbstractEmbeddedLanguagesProvider

        Public Shared Info As New EmbeddedLanguageInfo(
            SyntaxKind.StringLiteralToken,
            SyntaxKind.InterpolatedStringTextToken,
            VisualBasicSyntaxFactsService.Instance,
            VisualBasicSemanticFactsService.Instance,
            VisualBasicVirtualCharService.Instance)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
            MyBase.New(Info)
        End Sub
    End Class
End Namespace
