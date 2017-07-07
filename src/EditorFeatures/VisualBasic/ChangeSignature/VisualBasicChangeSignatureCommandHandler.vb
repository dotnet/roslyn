' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.Implementation.ChangeSignature

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.ChangeSignature
    <ExportCommandHandler(PredefinedCommandHandlerNames.ChangeSignature, ContentTypeNames.VisualBasicContentType)>
    Friend Class VisualBasicChangeSignatureCommandHandler
        Inherits AbstractChangeSignatureCommandHandler

        <ImportingConstructor>
        Public Sub New(waitIndicator As IWaitIndicator)
            MyBase.New(waitIndicator)
        End Sub
    End Class
End Namespace
