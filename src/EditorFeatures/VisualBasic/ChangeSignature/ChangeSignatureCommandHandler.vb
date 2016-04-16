' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Implementation.ChangeSignature

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.ChangeSignature
    <ExportCommandHandler(PredefinedCommandHandlerNames.ChangeSignature, ContentTypeNames.VisualBasicContentType)>
    Friend Class ChangeSignatureCommandHandler
        Inherits AbstractChangeSignatureCommandHandler
    End Class
End Namespace
