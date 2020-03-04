' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.ContentType

    Friend Module ContentTypeDefinitions

        ''' <summary>
        ''' Definition of the primary VB content type.
        ''' </summary>
        <Export()>
        <Name(ContentTypeNames.VisualBasicContentType)>
        <BaseDefinition(ContentTypeNames.RoslynContentType)>
        Public ReadOnly VisualBasicContentTypeDefinition As ContentTypeDefinition

        <Export()>
        <Name(ContentTypeNames.VisualBasicSignatureHelpContentType)>
        <BaseDefinition("sighelp")>
        Public ReadOnly SignatureHelpContentTypeDefinition As ContentTypeDefinition

    End Module
End Namespace
