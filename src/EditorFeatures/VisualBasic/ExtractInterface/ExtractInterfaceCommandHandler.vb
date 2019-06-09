﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Editor.Implementation.ExtractInterface
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.VisualStudio.Utilities
Imports VSCommanding = Microsoft.VisualStudio.Commanding

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.ExtractInterface
    <Export(GetType(VSCommanding.ICommandHandler))>
    <ContentType(ContentTypeNames.VisualBasicContentType)>
    <Name(PredefinedCommandHandlerNames.ExtractInterface)>
    Friend Class ExtractInterfaceCommandHandler
        Inherits AbstractExtractInterfaceCommandHandler

        <ImportingConstructor>
        Public Sub New(threadingContext As IThreadingContext)
            MyBase.New(threadingContext)
        End Sub
    End Class
End Namespace
