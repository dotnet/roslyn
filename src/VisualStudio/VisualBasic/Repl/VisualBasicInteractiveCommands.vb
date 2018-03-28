' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Interactive
    Friend Module VisualBasicInteractiveCommands
        Public Const InteractiveToolWindow = &H1
        Public Const ResetInteractiveFromProject = &H2

        Public Const InteractiveCommandSetIdString As String = "93DF185E-D75B-4FDB-9D47-E90F111971C5"
        Public ReadOnly InteractiveCommandSetId As Guid = New Guid(InteractiveCommandSetIdString)
    End Module
End Namespace
