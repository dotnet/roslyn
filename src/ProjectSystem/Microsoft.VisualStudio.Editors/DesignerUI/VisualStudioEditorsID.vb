' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Design

'*************************
'
'  These values must match those in VisualStudioEditorsID.h
'
'*************************

'Note: Common shell ID's taken from public\internal\VSCommon\inc\vsshlids.h and stdidcmd.h

Namespace Microsoft.VisualStudio.Editors

    Partial Friend NotInheritable Class Constants
        Friend NotInheritable Class MenuConstants

            ' Constants for menu command IDs and GUIDs. 
            ' *** These must match the constants in designerui\VisualStudioEditorsID.h *****


            ' *********************************************************************
            ' Menu IDs (0x01??)
            ' *********************************************************************

            Private Const s_IDM_CTX_RESX_ContextMenu As Integer = &H100
            Private Const s_IDM_VS_MENU_Resources As Integer = &H105
            Private Const s_IDM_CTX_SETTINGSDESIGNER_ContextMenu As Integer = &H110
            Private Const s_IDM_VS_MNUCTRL_NAVIGATE As Integer = &H106
            Public Const IDM_VS_TOOLBAR_Settings As Integer = &H210
            Public Const IDM_VS_TOOLBAR_Resources As Integer = &H211
            Public Const IDM_VS_TOOLBAR_Resources_ResW As Integer = &H212


            ' *********************************************************************
            ' Command Group IDs (0x1???)
            ' *********************************************************************


            ' *********************************************************************
            ' Command IDs (0x2???)
            ' *********************************************************************

            Private Const s_cmdidRESXImport As Integer = &H2003
            Private Const s_cmdidRESXExport As Integer = &H2004
            Private Const s_cmdidRESXPlay As Integer = &H2005

            Private Const s_cmdidRESXAddFixedMenuCommand As Integer = &H2019
            Private Const s_cmdidRESXAddExistingFile As Integer = &H2020
            Private Const s_cmdidRESXAddNewString As Integer = &H2021
            Private Const s_cmdidRESXAddNewImagePNG As Integer = &H2022
            Private Const s_cmdidRESXAddNewImageBMP As Integer = &H2023
            Private Const s_cmdidRESXAddNewImageGIF As Integer = &H2024
            Private Const s_cmdidRESXAddNewImageJPEG As Integer = &H2025
            Private Const s_cmdidRESXAddNewImageTIFF As Integer = &H2026
            Private Const s_cmdidRESXAddNewIcon As Integer = &H2027
            Private Const s_cmdidRESXAddNewTextFile As Integer = &H2028
            Private Const s_cmdidRESXAddDefaultResource As Integer = &H2030

            Private Const s_cmdidRESXResTypeStrings As Integer = &H2040
            Private Const s_cmdidRESXResTypeImages As Integer = &H2041
            Private Const s_cmdidRESXResTypeIcons As Integer = &H2042
            Private Const s_cmdidRESXResTypeAudio As Integer = &H2043
            Private Const s_cmdidRESXResTypeFiles As Integer = &H2044
            Private Const s_cmdidRESXResTypeOther As Integer = &H2045

            Private Const s_cmdidIDRESXViewsFixedMenuCommand As Integer = &H2018
            Private Const s_cmdidRESXViewsList As Integer = &H2050
            Private Const s_cmdidRESXViewsDetails As Integer = &H2051
            Private Const s_cmdidRESXViewsThumbnails As Integer = &H2052

            Private Const s_cmdidRESXGenericRemove As Integer = &H2060

            Private Const s_cmdidRESXAccessModifierCombobox As Integer = &H2061
            Private Const s_cmdidRESXGetAccessModifierOptions As Integer = &H2062

            Private Const s_cmdidSETTINGSDESIGNERViewCode As Integer = &H2104
            Private Const s_cmdidSETTINGSDESIGNERSynchronize As Integer = &H2105
            Private Const s_cmdidSETTINGSDESIGNERAccessModifierCombobox As Integer = &H2106
            Private Const s_cmdidSETTINGSDESIGNERGetAccessModifierOptions As Integer = &H2107
            Private Const s_cmdidSETTINGSDESIGNERLoadWebSettings As Integer = &H2108

            Private Const s_cmdidCurrentProfile As Integer = &H2201
            Private Const s_cmdidCurrentProfileListGet As Integer = &H2202

            Private Const s_cmdidCOMMONEditCell As Integer = &H2F00
            Private Const s_cmdidCOMMONAddRow As Integer = &H2F01
            Private Const s_cmdidCOMMONRemoveRow As Integer = &H2F02

            ' *********************************************************************



            'Some common stuff
            Private Shared ReadOnly s_CMDSETID_StandardCommandSet97 As New Guid("5efc7975-14bc-11cf-9b2b-00aa00573819")
            Private Shared ReadOnly s_CMDSETID_StandardCommandSet2K As New Guid("1496A755-94DE-11D0-8C3F-00C04FC2AAE2")
            Public Shared ReadOnly guidVSStd97 As Guid = s_CMDSETID_StandardCommandSet97
            Public Shared ReadOnly guidVSStd2K As Guid = s_CMDSETID_StandardCommandSet2K
            Private Const s_cmdidCopy As Integer = 15
            Public Const cmdidCut As Integer = 16
            Private Const s_cmdidDelete As Integer = 17
            Public Const cmdidRedo As Integer = 29
            Public Const cmdidMultiLevelRedo As Integer = 30
            Public Const cmdidMultiLevelRedoList As Integer = 299
            Public Const cmdidUndo As Integer = 43
            Public Const cmdidMultiLevelUndo As Integer = 44
            Public Const cmdidMultiLevelUndoList As Integer = 299
            Private Const s_cmdidRemove As Integer = 168
            Private Const s_cmdidPaste As Integer = 26
            Private Const s_cmdidOpen As Integer = 261
            Private Const s_cmdidOpenWith As Integer = 199
            Private Const s_cmdidRename As Integer = 150
            Private Const s_cmdidSelectAll As Integer = 31
            Public Const cmdidFileClose As Integer = 223
            Public Const cmdidSave As Integer = 110
            Public Const cmdidSaveAs As Integer = 111
            Public Const cmdidSaveProjectItemAs As Integer = 226
            Public Const cmdidSaveProjectItem As Integer = 331
            Public Const cmdidViewCode As Integer = 333
            Public Const cmdidEditLabel As Integer = 338
            Public Const ECMD_CANCEL As Integer = 103


            Public Shared ReadOnly CommandIDVSStd97cmdidCut As New CommandID(guidVSStd97, cmdidCut)
            Public Shared ReadOnly CommandIDVSStd97cmdidCopy As New CommandID(guidVSStd97, s_cmdidCopy)
            Public Shared ReadOnly CommandIDVSStd97cmdidPaste As New CommandID(guidVSStd97, s_cmdidPaste)
            Public Shared ReadOnly CommandIDVSStd97cmdidDelete As New CommandID(guidVSStd97, s_cmdidDelete)
            Public Shared ReadOnly CommandIDVSStd97cmdidRemove As New CommandID(guidVSStd97, s_cmdidRemove)
            Public Shared ReadOnly CommandIDVSStd97cmdidRename As New CommandID(guidVSStd97, s_cmdidRename)
            Public Shared ReadOnly CommandIDVSStd97cmdidSelectAll As New CommandID(guidVSStd97, s_cmdidSelectAll)
            Public Shared ReadOnly CommandIDVSStd97cmdidEditLabel As New CommandID(guidVSStd97, cmdidEditLabel)
            Public Shared ReadOnly CommandIDVSStd97cmdidViewCode As New CommandID(guidVSStd97, cmdidViewCode)
            Public Shared ReadOnly CommandIDVSStd2kECMD_CANCEL As New CommandID(guidVSStd2K, ECMD_CANCEL)

            ' GUID constants.
            Private Shared ReadOnly s_GUID_RESX_CommandID As New Guid("66BD4C1D-3401-4bcc-A942-E4990827E6F7")
            'The Command GUID for the resource editor.  It is required for us to correctly hook up key bindings,
            '  and must be returned from the editor factory.
            Public Shared ReadOnly GUID_RESXEditorCommandUI As New Guid("fea4dcc9-3645-44cd-92e7-84b55a16465c")

            Public Shared ReadOnly GUID_RESX_MenuGroup As New Guid("54869924-25F5-4878-A9C9-1C7198D99A8A")
            Public Shared ReadOnly GUID_SETTINGSDESIGNER_MenuGroup As New Guid("42b7a61f-81fd-4283-9678-6c448a827e56")
            Private Shared ReadOnly s_GUID_SETTINGSDESIGNER_CommandID As New Guid("c2013470-51ac-4278-9ac5-389c72a1f926")
            'The Command GUID for the settings designer.  It is required for us to correctly hook up key bindings,
            '  and must be returned from the editor factory.
            Public Shared ReadOnly GUID_SETTINGSDESIGNER_CommandUI As New Guid("515231ad-c9dc-4aa3-808f-e1b65e72081c")


            Private Shared ReadOnly s_GUID_MS_VS_Editors_CommandId As New Guid("E4B9BB05-1963-4774-8CFC-518359E3FCE3")

            ' Command ID = GUID + cmdid.
            Public Shared ReadOnly CommandIDVSStd97Open As New CommandID(guidVSStd97, s_cmdidOpen)
            Public Shared ReadOnly CommandIDVSStd97OpenWith As New CommandID(guidVSStd97, s_cmdidOpenWith)
            Public Shared ReadOnly CommandIDResXImport As New CommandID(s_GUID_RESX_CommandID, s_cmdidRESXImport)
            Public Shared ReadOnly CommandIDResXExport As New CommandID(s_GUID_RESX_CommandID, s_cmdidRESXExport)
            Public Shared ReadOnly CommandIDResXPlay As New CommandID(s_GUID_RESX_CommandID, s_cmdidRESXPlay)

            Public Shared ReadOnly CommandIDRESXAddFixedMenuCommand As New CommandID(s_GUID_RESX_CommandID, s_cmdidRESXAddFixedMenuCommand)
            Public Shared ReadOnly CommandIDRESXAddExistingFile As New CommandID(s_GUID_RESX_CommandID, s_cmdidRESXAddExistingFile)
            Public Shared ReadOnly CommandIDRESXAddNewString As New CommandID(s_GUID_RESX_CommandID, s_cmdidRESXAddNewString)
            Public Shared ReadOnly CommandIDRESXAddNewImagePNG As New CommandID(s_GUID_RESX_CommandID, s_cmdidRESXAddNewImagePNG)
            Public Shared ReadOnly CommandIDRESXAddNewImageBMP As New CommandID(s_GUID_RESX_CommandID, s_cmdidRESXAddNewImageBMP)
            Public Shared ReadOnly CommandIDRESXAddNewImageGIF As New CommandID(s_GUID_RESX_CommandID, s_cmdidRESXAddNewImageGIF)
            Public Shared ReadOnly CommandIDRESXAddNewImageJPEG As New CommandID(s_GUID_RESX_CommandID, s_cmdidRESXAddNewImageJPEG)
            Public Shared ReadOnly CommandIDRESXAddNewImageTIFF As New CommandID(s_GUID_RESX_CommandID, s_cmdidRESXAddNewImageTIFF)
            Public Shared ReadOnly CommandIDRESXAddNewIcon As New CommandID(s_GUID_RESX_CommandID, s_cmdidRESXAddNewIcon)
            Public Shared ReadOnly CommandIDRESXAddNewTextFile As New CommandID(s_GUID_RESX_CommandID, s_cmdidRESXAddNewTextFile)
            Public Shared ReadOnly CommandIDRESXAddDefaultResource As New CommandID(s_GUID_RESX_CommandID, s_cmdidRESXAddDefaultResource)

            Public Shared ReadOnly CommandIDRESXResTypeStrings As New CommandID(s_GUID_RESX_CommandID, s_cmdidRESXResTypeStrings)
            Public Shared ReadOnly CommandIDRESXResTypeImages As New CommandID(s_GUID_RESX_CommandID, s_cmdidRESXResTypeImages)
            Public Shared ReadOnly CommandIDRESXResTypeIcons As New CommandID(s_GUID_RESX_CommandID, s_cmdidRESXResTypeIcons)
            Public Shared ReadOnly CommandIDRESXResTypeAudio As New CommandID(s_GUID_RESX_CommandID, s_cmdidRESXResTypeAudio)
            Public Shared ReadOnly CommandIDRESXResTypeFiles As New CommandID(s_GUID_RESX_CommandID, s_cmdidRESXResTypeFiles)
            Public Shared ReadOnly CommandIDRESXResTypeOther As New CommandID(s_GUID_RESX_CommandID, s_cmdidRESXResTypeOther)
            Public Shared ReadOnly CommandIDRESXViewsFixedMenuCommand As New CommandID(s_GUID_RESX_CommandID, s_cmdidIDRESXViewsFixedMenuCommand)
            Public Shared ReadOnly CommandIDRESXViewsList As New CommandID(s_GUID_RESX_CommandID, s_cmdidRESXViewsList)
            Public Shared ReadOnly CommandIDRESXViewsDetails As New CommandID(s_GUID_RESX_CommandID, s_cmdidRESXViewsDetails)
            Public Shared ReadOnly CommandIDRESXViewsThumbnails As New CommandID(s_GUID_RESX_CommandID, s_cmdidRESXViewsThumbnails)

            Public Shared ReadOnly CommandIDRESXGenericRemove As New CommandID(s_GUID_RESX_CommandID, s_cmdidRESXGenericRemove)
            Public Shared ReadOnly CommandIDRESXAccessModifierCombobox As New CommandID(s_GUID_RESX_CommandID, s_cmdidRESXAccessModifierCombobox)
            Public Shared ReadOnly CommandIDRESXGetAccessModifierOptions As New CommandID(s_GUID_RESX_CommandID, s_cmdidRESXGetAccessModifierOptions)

            Public Shared ReadOnly ResXContextMenuID As New CommandID(GUID_RESX_MenuGroup, s_IDM_CTX_RESX_ContextMenu)
            Public Shared ReadOnly SettingsDesignerContextMenuID As New CommandID(GUID_SETTINGSDESIGNER_MenuGroup, s_IDM_CTX_SETTINGSDESIGNER_ContextMenu)
            Public Shared ReadOnly SettingsDesignerToolbar As New CommandID(GUID_SETTINGSDESIGNER_MenuGroup, IDM_VS_TOOLBAR_Settings)

            Public Shared ReadOnly CommandIDSettingsDesignerViewCode As New CommandID(s_GUID_SETTINGSDESIGNER_CommandID, s_cmdidSETTINGSDESIGNERViewCode)
            Public Shared ReadOnly CommandIDSettingsDesignerSynchronize As New CommandID(s_GUID_SETTINGSDESIGNER_CommandID, s_cmdidSETTINGSDESIGNERSynchronize)
            Public Shared ReadOnly CommandIDSettingsDesignerAccessModifierCombobox As New CommandID(s_GUID_SETTINGSDESIGNER_CommandID, s_cmdidSETTINGSDESIGNERAccessModifierCombobox)
            Public Shared ReadOnly CommandIDSettingsDesignerGetAccessModifierOptions As New CommandID(s_GUID_SETTINGSDESIGNER_CommandID, s_cmdidSETTINGSDESIGNERGetAccessModifierOptions)
            Public Shared ReadOnly CommandIDSettingsDesignerLoadWebSettings As New CommandID(s_GUID_SETTINGSDESIGNER_CommandID, s_cmdidSETTINGSDESIGNERLoadWebSettings)

            ' Shared commands
            Public Shared ReadOnly CommandIDCOMMONEditCell As New CommandID(s_GUID_MS_VS_Editors_CommandId, s_cmdidCOMMONEditCell)
            Public Shared ReadOnly CommandIDCOMMONAddRow As New CommandID(s_GUID_MS_VS_Editors_CommandId, s_cmdidCOMMONAddRow)
            Public Shared ReadOnly CommandIDCOMMONRemoveRow As New CommandID(s_GUID_MS_VS_Editors_CommandId, s_cmdidCOMMONRemoveRow)

#Region "My Extension feature menus"
            ' GUID for My Extension feature menus.
            Private Shared ReadOnly s_GUID_MYEXTENSION_Menu As New Guid("6C37AED7-D987-4fdf-ADF5-B71EB3F7236C")
            ' ID for My Extension context menu.
            Private Const s_IDM_CTX_MYEXTENSION_ContextMenu As Integer = &H110
            ' ID for My Extension context menu's only group.
            Private Const s_IDG_MYEXTENSION_CTX_AddRemove As Integer = &H1101
            ' ID for My Extension menu buttons.
            Private Const s_cmdidMYEXTENSIONAddExtension As Integer = &H2001
            Private Const s_cmdidMYEXTENSIONRemoveExtension As Integer = &H2002
            ' Command IDs to use in My Extension Property Page
            Public Shared ReadOnly CommandIDMYEXTENSIONContextMenu As New CommandID(s_GUID_MYEXTENSION_Menu, s_IDM_CTX_MYEXTENSION_ContextMenu)
            Public Shared ReadOnly CommandIDMyEXTENSIONAddExtension As New CommandID(s_GUID_MYEXTENSION_Menu, s_cmdidMYEXTENSIONAddExtension)
            Public Shared ReadOnly CommandIDMyEXTENSIONRemoveExtension As New CommandID(s_GUID_MYEXTENSION_Menu, s_cmdidMYEXTENSIONRemoveExtension)
#End Region

        End Class
    End Class

End Namespace
