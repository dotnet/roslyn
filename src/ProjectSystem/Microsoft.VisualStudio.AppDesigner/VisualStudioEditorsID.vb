' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Design

'*************************
'
'  These values must match those in VisualStudioEditorsID.h
'
'*************************

'Note: Common shell ID's taken from public\internal\VSCommon\inc\vsshlids.h and stdidcmd.h

Namespace Microsoft.VisualStudio.Editors

    Public Partial Class Constants
        Friend NotInheritable Class MenuConstants

            ' Constants for menu command IDs and GUIDs. 
            ' *** These must match the constants in designerui\VisualStudioEditorsID.h *****


            ' *********************************************************************

            'Some common stuff
            Private Shared ReadOnly s_CMDSETID_StandardCommandSet97 As New Guid("5efc7975-14bc-11cf-9b2b-00aa00573819")
            Private Shared ReadOnly s_CMDSETID_StandardCommandSet2K As New Guid("1496A755-94DE-11D0-8C3F-00C04FC2AAE2")

            Friend Shared ReadOnly guidVSStd97 As Guid = s_CMDSETID_StandardCommandSet97
            Friend Shared ReadOnly guidVSStd2K As Guid = s_CMDSETID_StandardCommandSet2K
            Private Const s_cmdidCopy As Integer = 15
            Private Const s_cmdidCut As Integer = 16
            Friend Const cmdidFileClose As Integer = 223
            Friend Const cmdidSave As Integer = 110
            Friend Const cmdidSaveAs As Integer = 111
            Friend Const cmdidSaveProjectItemAs As Integer = 226
            Friend Const cmdidSaveProjectItem As Integer = 331

            Friend Shared ReadOnly CommandIDVSStd97cmdidCut As New CommandID(guidVSStd97, s_cmdidCut)

            ' GUID constants.

        End Class
    End Class

End Namespace

