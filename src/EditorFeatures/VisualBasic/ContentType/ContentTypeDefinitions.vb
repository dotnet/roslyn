' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.ContentType

    Friend Module ContentTypeDefinitions

        ''' <summary>
        ''' Definition of the primary VB content type.
        ''' Also adds the LSP base content type to ensure the LSP client activates On VB files.
        ''' From Microsoft.VisualStudio.LanguageServer.Client.CodeRemoteContentDefinition.CodeRemoteBaseTypeName
        ''' We cannot directly reference the LSP client package in EditorFeatures as it is a VS dependency.
        ''' </summary>
        <Export()>
        <Name(ContentTypeNames.VisualBasicContentType)>
        <BaseDefinition(ContentTypeNames.RoslynContentType)>
        <BaseDefinition("code-languageserver-base")>
        Public ReadOnly VisualBasicContentTypeDefinition As ContentTypeDefinition

        <Export()>
        <Name(ContentTypeNames.VisualBasicSignatureHelpContentType)>
        <BaseDefinition("sighelp")>
        Public ReadOnly SignatureHelpContentTypeDefinition As ContentTypeDefinition

    End Module
End Namespace
