﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Editor.Implementation.ExtractInterface
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.ExtractInterface
    <Export(GetType(ICommandHandler))>
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
