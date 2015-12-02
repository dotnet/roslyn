Imports System
Imports System.ComponentModel.Design
Imports System.Runtime.InteropServices

'*************************
'
'  These values must match those in VisualStudioEditorsID.h
'
'*************************

'Note: Common shell ID's taken from public\internal\VSCommon\inc\vsshlids.h and stdidcmd.h

Namespace Microsoft.VisualStudio.Editors

    Partial Class Constants
        Friend NotInheritable Class MenuConstants

            ' Constants for menu command IDs and GUIDs. 
            ' *** These must match the constants in designerui\VisualStudioEditorsID.h *****


            ' *********************************************************************

            'Some common stuff
            Private Shared ReadOnly CMDSETID_StandardCommandSet97 As New Guid("5efc7975-14bc-11cf-9b2b-00aa00573819")
            Private Shared ReadOnly CMDSETID_StandardCommandSet2K As New Guid("1496A755-94DE-11D0-8C3F-00C04FC2AAE2")

            Friend Shared ReadOnly guidVSStd97 As Guid = CMDSETID_StandardCommandSet97
            Friend Shared ReadOnly guidVSStd2K As Guid = CMDSETID_StandardCommandSet2K
            Private Const cmdidCopy As Integer = 15
            Private Const cmdidCut As Integer = 16
            Friend Const cmdidFileClose As Integer = 223
            Friend Const cmdidSave As Integer = 110
            Friend Const cmdidSaveAs As Integer = 111
            Friend Const cmdidSaveProjectItemAs As Integer = 226
            Friend Const cmdidSaveProjectItem As Integer = 331

            Friend Shared ReadOnly CommandIDVSStd97cmdidCut As New CommandID(guidVSStd97, cmdidCut)

            ' GUID constants.

        End Class
    End Class

End Namespace

