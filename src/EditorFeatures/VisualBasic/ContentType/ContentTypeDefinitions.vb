' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
