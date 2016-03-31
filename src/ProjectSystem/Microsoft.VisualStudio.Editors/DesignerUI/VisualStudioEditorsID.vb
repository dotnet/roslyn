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

            Private Const IDM_CTX_RESX_ContextMenu As Integer = &H100
            Private Const IDM_VS_MENU_Resources As Integer = &H105
            Private Const IDM_CTX_SETTINGSDESIGNER_ContextMenu As Integer = &H110
            Private Const IDM_VS_MNUCTRL_NAVIGATE As Integer = &H106
            Public Const IDM_VS_TOOLBAR_Settings As Integer = &H210
            Public Const IDM_VS_TOOLBAR_Resources As Integer = &H211
            Public Const IDM_VS_TOOLBAR_Resources_ResW As Integer = &H212


            ' *********************************************************************
            ' Command Group IDs (0x1???)
            ' *********************************************************************


            ' *********************************************************************
            ' Command IDs (0x2???)
            ' *********************************************************************

            Private Const cmdidRESXImport As Integer = &H2003
            Private Const cmdidRESXExport As Integer = &H2004
            Private Const cmdidRESXPlay As Integer = &H2005

            Private Const cmdidRESXAddFixedMenuCommand As Integer = &H2019
            Private Const cmdidRESXAddExistingFile As Integer = &H2020
            Private Const cmdidRESXAddNewString As Integer = &H2021
            Private Const cmdidRESXAddNewImagePNG As Integer = &H2022
            Private Const cmdidRESXAddNewImageBMP As Integer = &H2023
            Private Const cmdidRESXAddNewImageGIF As Integer = &H2024
            Private Const cmdidRESXAddNewImageJPEG As Integer = &H2025
            Private Const cmdidRESXAddNewImageTIFF As Integer = &H2026
            Private Const cmdidRESXAddNewIcon As Integer = &H2027
            Private Const cmdidRESXAddNewTextFile As Integer = &H2028
            Private Const cmdidRESXAddDefaultResource As Integer = &H2030

            Private Const cmdidRESXResTypeStrings As Integer = &H2040
            Private Const cmdidRESXResTypeImages As Integer = &H2041
            Private Const cmdidRESXResTypeIcons As Integer = &H2042
            Private Const cmdidRESXResTypeAudio As Integer = &H2043
            Private Const cmdidRESXResTypeFiles As Integer = &H2044
            Private Const cmdidRESXResTypeOther As Integer = &H2045

            Private Const cmdidIDRESXViewsFixedMenuCommand As Integer = &H2018
            Private Const cmdidRESXViewsList As Integer = &H2050
            Private Const cmdidRESXViewsDetails As Integer = &H2051
            Private Const cmdidRESXViewsThumbnails As Integer = &H2052

            Private Const cmdidRESXGenericRemove As Integer = &H2060

            Private Const cmdidRESXAccessModifierCombobox As Integer = &H2061
            Private Const cmdidRESXGetAccessModifierOptions As Integer = &H2062

            Private Const cmdidSETTINGSDESIGNERViewCode As Integer = &H2104
            Private Const cmdidSETTINGSDESIGNERSynchronize As Integer = &H2105
            Private Const cmdidSETTINGSDESIGNERAccessModifierCombobox As Integer = &H2106
            Private Const cmdidSETTINGSDESIGNERGetAccessModifierOptions As Integer = &H2107
            Private Const cmdidSETTINGSDESIGNERLoadWebSettings As Integer = &H2108

            Private Const cmdidCurrentProfile As Integer = &H2201
            Private Const cmdidCurrentProfileListGet As Integer = &H2202

            Private Const cmdidCOMMONEditCell As Integer = &H2F00
            Private Const cmdidCOMMONAddRow As Integer = &H2F01
            Private Const cmdidCOMMONRemoveRow As Integer = &H2F02

            ' *********************************************************************



            'Some common stuff
            Private Shared ReadOnly CMDSETID_StandardCommandSet97 As New Guid("5efc7975-14bc-11cf-9b2b-00aa00573819")
            Private Shared ReadOnly CMDSETID_StandardCommandSet2K As New Guid("1496A755-94DE-11D0-8C3F-00C04FC2AAE2")
            Public Shared ReadOnly guidVSStd97 As Guid = CMDSETID_StandardCommandSet97
            Public Shared ReadOnly guidVSStd2K As Guid = CMDSETID_StandardCommandSet2K
            Private Const cmdidCopy As Integer = 15
            Public Const cmdidCut As Integer = 16
            Private Const cmdidDelete As Integer = 17
            Public Const cmdidRedo As Integer = 29
            Public Const cmdidMultiLevelRedo As Integer = 30
            Public Const cmdidMultiLevelRedoList As Integer = 299
            Public Const cmdidUndo As Integer = 43
            Public Const cmdidMultiLevelUndo As Integer = 44
            Public Const cmdidMultiLevelUndoList As Integer = 299
            Private Const cmdidRemove As Integer = 168
            Private Const cmdidPaste As Integer = 26
            Private Const cmdidOpen As Integer = 261
            Private Const cmdidOpenWith As Integer = 199
            Private Const cmdidRename As Integer = 150
            Private Const cmdidSelectAll As Integer = 31
            Public Const cmdidFileClose As Integer = 223
            Public Const cmdidSave As Integer = 110
            Public Const cmdidSaveAs As Integer = 111
            Public Const cmdidSaveProjectItemAs As Integer = 226
            Public Const cmdidSaveProjectItem As Integer = 331
            Public Const cmdidViewCode As Integer = 333
            Public Const cmdidEditLabel As Integer = 338
            Public Const ECMD_CANCEL As Integer = 103


            Public Shared ReadOnly CommandIDVSStd97cmdidCut As New CommandID(guidVSStd97, cmdidCut)
            Public Shared ReadOnly CommandIDVSStd97cmdidCopy As New CommandID(guidVSStd97, cmdidCopy)
            Public Shared ReadOnly CommandIDVSStd97cmdidPaste As New CommandID(guidVSStd97, cmdidPaste)
            Public Shared ReadOnly CommandIDVSStd97cmdidDelete As New CommandID(guidVSStd97, cmdidDelete)
            Public Shared ReadOnly CommandIDVSStd97cmdidRemove As New CommandID(guidVSStd97, cmdidRemove)
            Public Shared ReadOnly CommandIDVSStd97cmdidRename As New CommandID(guidVSStd97, cmdidRename)
            Public Shared ReadOnly CommandIDVSStd97cmdidSelectAll As New CommandID(guidVSStd97, cmdidSelectAll)
            Public Shared ReadOnly CommandIDVSStd97cmdidEditLabel As New CommandID(guidVSStd97, cmdidEditLabel)
            Public Shared ReadOnly CommandIDVSStd97cmdidViewCode As New CommandID(guidVSStd97, cmdidViewCode)
            Public Shared ReadOnly CommandIDVSStd2kECMD_CANCEL As New CommandID(guidVSStd2K, ECMD_CANCEL)

            ' GUID constants.
            Private Shared ReadOnly GUID_RESX_CommandID As New Guid("66BD4C1D-3401-4bcc-A942-E4990827E6F7")
            'The Command GUID for the resource editor.  It is required for us to correctly hook up key bindings,
            '  and must be returned from the editor factory.
            Public Shared ReadOnly GUID_RESXEditorCommandUI As New Guid("fea4dcc9-3645-44cd-92e7-84b55a16465c")

            Public Shared ReadOnly GUID_RESX_MenuGroup As New Guid("54869924-25F5-4878-A9C9-1C7198D99A8A")
            Public Shared ReadOnly GUID_SETTINGSDESIGNER_MenuGroup As New Guid("42b7a61f-81fd-4283-9678-6c448a827e56")
            Private Shared ReadOnly GUID_SETTINGSDESIGNER_CommandID As New Guid("c2013470-51ac-4278-9ac5-389c72a1f926")
            'The Command GUID for the settings designer.  It is required for us to correctly hook up key bindings,
            '  and must be returned from the editor factory.
            Public Shared ReadOnly GUID_SETTINGSDESIGNER_CommandUI As New Guid("515231ad-c9dc-4aa3-808f-e1b65e72081c")


            Private Shared ReadOnly GUID_MS_VS_Editors_CommandId As New Guid("E4B9BB05-1963-4774-8CFC-518359E3FCE3")

            ' Command ID = GUID + cmdid.
            Public Shared ReadOnly CommandIDVSStd97Open As New CommandID(guidVSStd97, cmdidOpen)
            Public Shared ReadOnly CommandIDVSStd97OpenWith As New CommandID(guidVSStd97, cmdidOpenWith)
            Public Shared ReadOnly CommandIDResXImport As New CommandID(GUID_RESX_CommandID, cmdidRESXImport)
            Public Shared ReadOnly CommandIDResXExport As New CommandID(GUID_RESX_CommandID, cmdidRESXExport)
            Public Shared ReadOnly CommandIDResXPlay As New CommandID(GUID_RESX_CommandID, cmdidRESXPlay)

            Public Shared ReadOnly CommandIDRESXAddFixedMenuCommand As New CommandID(GUID_RESX_CommandID, cmdidRESXAddFixedMenuCommand)
            Public Shared ReadOnly CommandIDRESXAddExistingFile As New CommandID(GUID_RESX_CommandID, cmdidRESXAddExistingFile)
            Public Shared ReadOnly CommandIDRESXAddNewString As New CommandID(GUID_RESX_CommandID, cmdidRESXAddNewString)
            Public Shared ReadOnly CommandIDRESXAddNewImagePNG As New CommandID(GUID_RESX_CommandID, cmdidRESXAddNewImagePNG)
            Public Shared ReadOnly CommandIDRESXAddNewImageBMP As New CommandID(GUID_RESX_CommandID, cmdidRESXAddNewImageBMP)
            Public Shared ReadOnly CommandIDRESXAddNewImageGIF As New CommandID(GUID_RESX_CommandID, cmdidRESXAddNewImageGIF)
            Public Shared ReadOnly CommandIDRESXAddNewImageJPEG As New CommandID(GUID_RESX_CommandID, cmdidRESXAddNewImageJPEG)
            Public Shared ReadOnly CommandIDRESXAddNewImageTIFF As New CommandID(GUID_RESX_CommandID, cmdidRESXAddNewImageTIFF)
            Public Shared ReadOnly CommandIDRESXAddNewIcon As New CommandID(GUID_RESX_CommandID, cmdidRESXAddNewIcon)
            Public Shared ReadOnly CommandIDRESXAddNewTextFile As New CommandID(GUID_RESX_CommandID, cmdidRESXAddNewTextFile)
            Public Shared ReadOnly CommandIDRESXAddDefaultResource As New CommandID(GUID_RESX_CommandID, cmdidRESXAddDefaultResource)

            Public Shared ReadOnly CommandIDRESXResTypeStrings As New CommandID(GUID_RESX_CommandID, cmdidRESXResTypeStrings)
            Public Shared ReadOnly CommandIDRESXResTypeImages As New CommandID(GUID_RESX_CommandID, cmdidRESXResTypeImages)
            Public Shared ReadOnly CommandIDRESXResTypeIcons As New CommandID(GUID_RESX_CommandID, cmdidRESXResTypeIcons)
            Public Shared ReadOnly CommandIDRESXResTypeAudio As New CommandID(GUID_RESX_CommandID, cmdidRESXResTypeAudio)
            Public Shared ReadOnly CommandIDRESXResTypeFiles As New CommandID(GUID_RESX_CommandID, cmdidRESXResTypeFiles)
            Public Shared ReadOnly CommandIDRESXResTypeOther As New CommandID(GUID_RESX_CommandID, cmdidRESXResTypeOther)
            Public Shared ReadOnly CommandIDRESXViewsFixedMenuCommand As New CommandID(GUID_RESX_CommandID, cmdidIDRESXViewsFixedMenuCommand)
            Public Shared ReadOnly CommandIDRESXViewsList As New CommandID(GUID_RESX_CommandID, cmdidRESXViewsList)
            Public Shared ReadOnly CommandIDRESXViewsDetails As New CommandID(GUID_RESX_CommandID, cmdidRESXViewsDetails)
            Public Shared ReadOnly CommandIDRESXViewsThumbnails As New CommandID(GUID_RESX_CommandID, cmdidRESXViewsThumbnails)

            Public Shared ReadOnly CommandIDRESXGenericRemove As New CommandID(GUID_RESX_CommandID, cmdidRESXGenericRemove)
            Public Shared ReadOnly CommandIDRESXAccessModifierCombobox As New CommandID(GUID_RESX_CommandID, cmdidRESXAccessModifierCombobox)
            Public Shared ReadOnly CommandIDRESXGetAccessModifierOptions As New CommandID(GUID_RESX_CommandID, cmdidRESXGetAccessModifierOptions)

            Public Shared ReadOnly ResXContextMenuID As New CommandID(GUID_RESX_MenuGroup, IDM_CTX_RESX_ContextMenu)
            Public Shared ReadOnly SettingsDesignerContextMenuID As New CommandID(GUID_SETTINGSDESIGNER_MenuGroup, IDM_CTX_SETTINGSDESIGNER_ContextMenu)
            Public Shared ReadOnly SettingsDesignerToolbar As New CommandID(GUID_SETTINGSDESIGNER_MenuGroup, IDM_VS_TOOLBAR_Settings)

            Public Shared ReadOnly CommandIDSettingsDesignerViewCode As New CommandID(GUID_SETTINGSDESIGNER_CommandID, cmdidSETTINGSDESIGNERViewCode)
            Public Shared ReadOnly CommandIDSettingsDesignerSynchronize As New CommandID(GUID_SETTINGSDESIGNER_CommandID, cmdidSETTINGSDESIGNERSynchronize)
            Public Shared ReadOnly CommandIDSettingsDesignerAccessModifierCombobox As New CommandID(GUID_SETTINGSDESIGNER_CommandID, cmdidSETTINGSDESIGNERAccessModifierCombobox)
            Public Shared ReadOnly CommandIDSettingsDesignerGetAccessModifierOptions As New CommandID(GUID_SETTINGSDESIGNER_CommandID, cmdidSETTINGSDESIGNERGetAccessModifierOptions)
            Public Shared ReadOnly CommandIDSettingsDesignerLoadWebSettings As New CommandID(GUID_SETTINGSDESIGNER_CommandID, cmdidSETTINGSDESIGNERLoadWebSettings)

            ' Shared commands
            Public Shared ReadOnly CommandIDCOMMONEditCell As New CommandID(GUID_MS_VS_Editors_CommandId, cmdidCOMMONEditCell)
            Public Shared ReadOnly CommandIDCOMMONAddRow As New CommandID(GUID_MS_VS_Editors_CommandId, cmdidCOMMONAddRow)
            Public Shared ReadOnly CommandIDCOMMONRemoveRow As New CommandID(GUID_MS_VS_Editors_CommandId, cmdidCOMMONRemoveRow)

#Region "My Extension feature menus"
            ' GUID for My Extension feature menus.
            Private Shared ReadOnly GUID_MYEXTENSION_Menu As New Guid("6C37AED7-D987-4fdf-ADF5-B71EB3F7236C")
            ' ID for My Extension context menu.
            Private Const IDM_CTX_MYEXTENSION_ContextMenu As Integer = &H110
            ' ID for My Extension context menu's only group.
            Private Const IDG_MYEXTENSION_CTX_AddRemove As Integer = &H1101
            ' ID for My Extension menu buttons.
            Private Const cmdidMYEXTENSIONAddExtension As Integer = &H2001
            Private Const cmdidMYEXTENSIONRemoveExtension As Integer = &H2002
            ' Command IDs to use in My Extension Property Page
            Public Shared ReadOnly CommandIDMYEXTENSIONContextMenu As New CommandID(GUID_MYEXTENSION_Menu, IDM_CTX_MYEXTENSION_ContextMenu)
            Public Shared ReadOnly CommandIDMyEXTENSIONAddExtension As New CommandID(GUID_MYEXTENSION_Menu, cmdidMYEXTENSIONAddExtension)
            Public Shared ReadOnly CommandIDMyEXTENSIONRemoveExtension As New CommandID(GUID_MYEXTENSION_Menu, cmdidMYEXTENSIONRemoveExtension)
#End Region

        End Class
    End Class

End Namespace
