' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Implementation.ExtractInterface

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.ExtractInterface
    <ExportCommandHandler(PredefinedCommandHandlerNames.ExtractInterface, ContentTypeNames.VisualBasicContentType)>
    Friend Class ExtractInterfaceCommandHandler
        Inherits AbstractExtractInterfaceCommandHandler

    End Class
End Namespace
