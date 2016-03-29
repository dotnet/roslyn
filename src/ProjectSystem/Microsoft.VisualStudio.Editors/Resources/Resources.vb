'------------------------------------------------------------------------------
' <copyright company='Microsoft Corporation'>           
'    Copyright (c) Microsoft Corporation. All Rights Reserved.                
'    Information Contained Herein is Proprietary and Confidential.            
' </copyright>                                                                
'------------------------------------------------------------------------------
'
Namespace My.Resources

    'Hide the Microsoft_VisualStudio_Editors_Designer class.  To keep the .resources file
    '  with the same fully-qualified name in the assembly manifest, we need to have the
    '  Designer.resx file actually named "Microsoft.VisualStudio.Editors.Designer.resx",
    '  or else change the project's root namespace which I don't want to do at this point.
    '  But then the class name gets generated as "Microsoft_VisualStudio_Editors_Designer".
    'So hide that one and introduce a "Designer" class instead.
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)> _
        Partial Friend Class Microsoft_VisualStudio_Editors_Designer
    End Class

    ''' <summary>
    ''' String resource values for MS.VS.Editors.dll.  To edit the strings in this class,
    '''   edit the Microsoft.VisualStudio.Editors.resx file.
    ''' </summary>
    ''' <remarks>
    ''' </remarks>
    Friend Class Designer
        Inherits Global.My.Resources.Microsoft_VisualStudio_Editors_Designer

        ''' <summary>
        ''' These are some string resource IDs (just the resource ID name, not the 
        '''   actual string value).  These are not automatically kept up to date from
        '''   the .resx file, so they must be edited manually.
        ''' </summary>
        ''' <remarks></remarks>
        Friend Class ConstantResourceIDs

            'IMPORTANT: These must be kept manually up to date, they are not automatically
            '  synchronized with the .resx file.

            Friend Const PPG_WebReferenceNameDescription As String = "PPG_WebReferenceNameDescription"
            Friend Const PPG_ServiceReferenceNamespaceDescription As String = "PPG_ServiceReferenceNamespaceDescription"
            Friend Const PPG_UrlBehaviorName As String = "PPG_UrlBehaviorName"
            Friend Const PPG_UrlBehaviorDescription As String = "PPG_UrlBehaviorDescription"
            Friend Const PPG_WebReferenceUrlName As String = "PPG_WebReferenceUrlName"
            Friend Const PPG_WebReferenceUrlDescription As String = "PPG_WebReferenceUrlDescription"
            Friend Const PPG_ServiceReferenceUrlName As String = "PPG_ServiceReferenceUrlName"
            Friend Const PPG_ServiceReferenceUrlDescription As String = "PPG_ServiceReferenceUrlDescription"

        End Class
    End Class
End Namespace

Namespace Microsoft.VisualStudio.Editors

    ''' <summary>
    ''' Compatibility-only class for string resources.  Newer code should use My.Resources.Designer instead,
    '''   along with String.Format when needed.
    ''' 
    ''' </summary>
    ''' <remarks>
    ''' 
    ''' IMPORTANT: The old SR constants were simply string constants to the IDs.  You still had to call
    '''   SR.GetString() to get the actual string value.
    ''' The auto-generated My.Resources properties, on the other hand, return the actual string value,
    '''   and never the ID constant.
    ''' This compatibility class does *not* return the string IDs, but rather the string values.
    '''   Therefore, GetString() has been changed to simply return the string value unless there are
    '''   arguments passed in, in which case String.Format() is called.
    ''' 
    ''' </remarks>
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)> _
    Friend Class SR
        Inherits Global.My.Resources.Microsoft_VisualStudio_Editors_Designer

        ''' <summary>
        ''' Temporary compatibility function to make converting from Designer.txt to Designer.resx easier.
        ''' Just returns the input string unless there are arguments, in which case it calls String.Format.
        ''' </summary>
        ''' <param name="s"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)> _
        Friend Shared Function GetString(ByVal s As String, ByVal ParamArray Arguments() As Object) As String
            If Arguments Is Nothing OrElse Arguments.Length = 0 Then
                Return s
            Else
                Return String.Format(s, Arguments)
            End If
        End Function

    End Class

End Namespace



#If False Then

'FOR REFERENCE ONLY:   Old auto-generated SR class definition, when we were using Designer.txt

'Imports System
'Imports System.Reflection
'Imports System.Globalization
'Imports System.Resources
'Imports System.Text
'Imports System.Threading
'Imports System.ComponentModel
'Imports System.Security.Permissions

'Namespace Microsoft.VisualStudio.Editors

'    <AttributeUsage(AttributeTargets.All)> _
'    Friend NotInheritable Class SRDescriptionAttribute
'        Inherits DescriptionAttribute

'        Private replaced As Boolean = False

'        '/// <summary>
'        '///     Constructs a new sys description.
'        '/// </summary>
'        '/// <param name='description'>
'        '///     description text.
'        '/// </param>
'        Public Sub New(ByVal description As String)
'            MyBase.New(description)
'        End Sub


'        '/// <summary>
'        '///     Retrieves the description text.
'        '/// </summary>
'        '/// <returns>
'        '///     description
'        '/// </returns>
'        Public Overrides ReadOnly Property Description() As String
'            Get
'                If (Not replaced) Then
'                    replaced = True
'                    DescriptionValue = SR.GetString(SR.Culture, MyBase.Description)
'                End If
'                Return MyBase.Description
'            End Get
'        End Property
'    End Class

'    <AttributeUsage(AttributeTargets.All)> _
'    Friend NotInheritable Class SRCategoryAttribute
'        Inherits CategoryAttribute

'        Public Sub New(ByVal category As String)
'            MyBase.New(category)
'        End Sub

'        Protected Overrides Function GetLocalizedString(ByVal value As String) As String
'            Return SR.GetString(SR.Culture, value)
'        End Function
'    End Class

'    '/// <summary>
'    '///    AutoGenerated resource class. Usage:
'    '///
'    '///        string s = SR.GetString(SR.MyIdenfitier)
'    '/// </summary>
'     _
'    Friend NotInheritable Class SR
'        Friend Const CMN_AllFilesFilter As String = "CMN_AllFilesFilter"
'        Friend Const PPG_PropertyPageControlName As String = "PPG_PropertyPageControlName"
'        Friend Const PPG_ReferencesTitle As String = "PPG_ReferencesTitle"
'        Friend Const PPG_VersionTitle As String = "PPG_VersionTitle"
'        Friend Const PPG_SigningTitle As String = "PPG_SigningTitle"
'        Friend Const PPG_ApplicationTitle As String = "PPG_ApplicationTitle"
'        Friend Const PPG_CompileTitle As String = "PPG_CompileTitle"
'        Friend Const PPG_DebugTitle As String = "PPG_DebugTitle"
'        Friend Const PPG_DeployTitle As String = "PPG_DeployTitle"
'        Friend Const PPG_VdtGeneralTitle As String = "PPG_VdtGeneralTitle"
'        Friend Const PPG_Security As String = "PPG_Security"
'        Friend Const PPG_BuildTitle As String = "PPG_BuildTitle"
'        Friend Const PPG_BuildEventsTitle As String = "PPG_BuildEventsTitle"
'        Friend Const PPG_ReferencePathsTitle As String = "PPG_ReferencePathsTitle"
'        Friend Const PPG_PreBuildCommandLineTitle As String = "PPG_PreBuildCommandLineTitle"
'        Friend Const PPG_PostBuildCommandLineTitle As String = "PPG_PostBuildCommandLineTitle"
'        Friend Const PPG_OutputPathNotSecure As String = "PPG_OutputPathNotSecure"
'        Friend Const PPG_BrowseText As String = "PPG_BrowseText"
'        Friend Const PPG_StartupObjectNone As String = "PPG_StartupObjectNone"
'        Friend Const PPG_KeyFileNewText As String = "PPG_KeyFileNewText"
'        Friend Const PPG_KeyFileBrowseText As String = "PPG_KeyFileBrowseText"
'        Friend Const PPG_ComboBoxSelect_None As String = "PPG_ComboBoxSelect_None"
'        Friend Const PPG_ActiveConfigOrPlatformFormatString_1Arg As String = "PPG_ActiveConfigOrPlatformFormatString_1Arg"
'        Friend Const PPG_AllConfigurations As String = "PPG_AllConfigurations"
'        Friend Const PPG_AllPlatforms As String = "PPG_AllPlatforms"
'        Friend Const PPG_NotApplicable As String = "PPG_NotApplicable"
'        Friend Const PPG_ConfigNotFound_2Args As String = "PPG_ConfigNotFound_2Args"
'        Friend Const PPG_NeutralLanguage_None As String = "PPG_NeutralLanguage_None"
'        Friend Const PPG_SelectFileTitle As String = "PPG_SelectFileTitle"
'        Friend Const PPG_AddExistingFilesTitle As String = "PPG_AddExistingFilesTitle"
'        Friend Const PPG_AddIconFilesFilter As String = "PPG_AddIconFilesFilter"
'        Friend Const PPG_ExeFilesFilter As String = "PPG_ExeFilesFilter"
'        Friend Const PPG_SelectWorkingDirectoryTitle As String = "PPG_SelectWorkingDirectoryTitle"
'        Friend Const PPG_SelectOutputPathTitle As String = "PPG_SelectOutputPathTitle"
'        Friend Const PPG_SelectReferencePath As String = "PPG_SelectReferencePath"
'        Friend Const PPG_AddWin32ResourceFilter As String = "PPG_AddWin32ResourceFilter"
'        Friend Const PPG_AddWin32ResourceTitle As String = "PPG_AddWin32ResourceTitle"
'        Friend Const PPG_InvalidFolderPath As String = "PPG_InvalidFolderPath"
'        Friend Const PPG_AdvancedCompilerSettings_Title As String = "PPG_AdvancedCompilerSettings_Title"
'        Friend Const PPG_AdvancedBuildSettings_Title As String = "PPG_AdvancedBuildSettings_Title"
'        Friend Const PPG_CompilerWarnings_Title As String = "PPG_CompilerWarnings_Title"
'        Friend Const PPG_ReferencePaths_Title As String = "PPG_ReferencePaths_Title"
'        Friend Const PPG_CompatibleSettings_Title As String = "PPG_CompatibleSettings_Title"
'        Friend Const PPG_AssemblyInfo_Title As String = "PPG_AssemblyInfo_Title"
'        Friend Const PPG_AssemblyInfo_InvalidVersion As String = "PPG_AssemblyInfo_InvalidVersion"
'        Friend Const PPG_AssemblyInfo_BadWildcard As String = "PPG_AssemblyInfo_BadWildcard"
'        Friend Const PPG_AssemblyInfo_VersionOutOfRange_2Args As String = "PPG_AssemblyInfo_VersionOutOfRange_2Args"
'        Friend Const PPG_Application_DefaultIconText As String = "PPG_Application_DefaultIconText"
'        Friend Const PPG_Application_InvalidSubMainStartup As String = "PPG_Application_InvalidSubMainStartup"
'        Friend Const PPG_Application_StartupObjectNotSet As String = "PPG_Application_StartupObjectNotSet"
'        Friend Const PPG_Application_CantAddIcon As String = "PPG_Application_CantAddIcon"
'        Friend Const PPG_Application_BadIcon As String = "PPG_Application_BadIcon"
'        Friend Const PPG_Application_BadIcon_1Arg As String = "PPG_Application_BadIcon_1Arg"
'        Friend Const PPG_Application_BadGuid As String = "PPG_Application_BadGuid"
'        Friend Const PPG_Application_MyAppCommentLine1 As String = "PPG_Application_MyAppCommentLine1"
'        Friend Const PPG_Application_MyAppCommentLine2 As String = "PPG_Application_MyAppCommentLine2"
'        Friend Const PPG_Application_MyAppCommentLine3 As String = "PPG_Application_MyAppCommentLine3"
'        Friend Const PPG_Application_MyAppCommentLine4 As String = "PPG_Application_MyAppCommentLine4"
'        Friend Const PPG_Application_MyAppCommentLine5 As String = "PPG_Application_MyAppCommentLine5"
'        Friend Const PPG_Application_InvalidIdentifierStartupForm_1Arg As String = "PPG_Application_InvalidIdentifierStartupForm_1Arg"
'        Friend Const PPG_Application_InvalidIdentifierSplashScreenForm_1Arg As String = "PPG_Application_InvalidIdentifierSplashScreenForm_1Arg"
'        Friend Const PPG_Application_SplashSameAsStart As String = "PPG_Application_SplashSameAsStart"
'        Friend Const PPG_Application_StartupFormLabelText As String = "PPG_Application_StartupFormLabelText"
'        Friend Const PPG_Application_AppEventsCommentLine1 As String = "PPG_Application_AppEventsCommentLine1"
'        Friend Const PPG_Application_AppEventsCommentLine2 As String = "PPG_Application_AppEventsCommentLine2"
'        Friend Const PPG_Application_AppEventsCommentLine3 As String = "PPG_Application_AppEventsCommentLine3"
'        Friend Const PPG_Application_AppEventsCommentLine4 As String = "PPG_Application_AppEventsCommentLine4"
'        Friend Const PPG_Application_AppEventsCommentLine5 As String = "PPG_Application_AppEventsCommentLine5"
'        Friend Const PPG_Application_AppEventsCommentLine6 As String = "PPG_Application_AppEventsCommentLine6"
'        Friend Const PPG_Application_AppEventsCommentLine7 As String = "PPG_Application_AppEventsCommentLine7"
'        Friend Const PPG_AdvancedBuildSettings_InvalidBaseAddress As String = "PPG_AdvancedBuildSettings_InvalidBaseAddress"
'        Friend Const PPG_Compile_Notification_None As String = "PPG_Compile_Notification_None"
'        Friend Const PPG_Compile_Notification_Warning As String = "PPG_Compile_Notification_Warning"
'        Friend Const PPG_Compile_Notification_Error As String = "PPG_Compile_Notification_Error"
'        Friend Const PPG_Compile_OptionStrict_Custom As String = "PPG_Compile_OptionStrict_Custom"
'        Friend Const PPG_Compile_42016 As String = "PPG_Compile_42016"
'        Friend Const PPG_Compile_42017_42018_42019 As String = "PPG_Compile_42017_42018_42019"
'        Friend Const PPG_Compile_42020 As String = "PPG_Compile_42020"
'        Friend Const PPG_Compile_42104 As String = "PPG_Compile_42104"
'        Friend Const PPG_Compile_42105_42106_42107 As String = "PPG_Compile_42105_42106_42107"
'        Friend Const PPG_Compile_42353_42354_42355 As String = "PPG_Compile_42353_42354_42355"
'        Friend Const PPG_Compile_42024 As String = "PPG_Compile_42024"
'        Friend Const PPG_Compile_42025 As String = "PPG_Compile_42025"
'        Friend Const PPG_Compile_42004 As String = "PPG_Compile_42004"
'        Friend Const PPG_Compile_42029 As String = "PPG_Compile_42029"
'        Friend Const PPG_Compile_ResetIndeterminateWarningLevels As String = "PPG_Compile_ResetIndeterminateWarningLevels"
'        Friend Const PPG_MyApplication_StartupMode_FormCloses As String = "PPG_MyApplication_StartupMode_FormCloses"
'        Friend Const PPG_MyApplication_StartupMode_AppExits As String = "PPG_MyApplication_StartupMode_AppExits"
'        Friend Const PPG_MyApplication_AuthenMode_Windows As String = "PPG_MyApplication_AuthenMode_Windows"
'        Friend Const PPG_MyApplication_AuthenMode_ApplicationDefined As String = "PPG_MyApplication_AuthenMode_ApplicationDefined"
'        Friend Const PPG_SecurityZone_LockedMessage As String = "PPG_SecurityZone_LockedMessage"
'        Friend Const PPG_SecurityPage_CustomPermissionSet As String = "PPG_SecurityPage_CustomPermissionSet"
'        Friend Const PPG_SecurityPage_Cancel As String = "PPG_SecurityPage_Cancel"
'        Friend Const PPG_SecurityPage_Calculate As String = "PPG_SecurityPage_Calculate"
'        Friend Const PPG_MgdStatus_Stopped As String = "PPG_MgdStatus_Stopped"
'        Friend Const PPG_MgdStatus_Starting As String = "PPG_MgdStatus_Starting"
'        Friend Const PPG_MgdStatus_Building As String = "PPG_MgdStatus_Building"
'        Friend Const PPG_MgdStatus_Analyzing As String = "PPG_MgdStatus_Analyzing"
'        Friend Const PPG_MgdStatus_AnalyzeFailed As String = "PPG_MgdStatus_AnalyzeFailed"
'        Friend Const PPG_MgdStatus_Aborting As String = "PPG_MgdStatus_Aborting"
'        Friend Const PPG_MgdStatus_Cancelling As String = "PPG_MgdStatus_Cancelling"
'        Friend Const PPG_SecurityPage_InternetZone As String = "PPG_SecurityPage_InternetZone"
'        Friend Const PPG_SecurityPage_LocalIntranetZone As String = "PPG_SecurityPage_LocalIntranetZone"
'        Friend Const PPG_SecurityPage_CustomZone As String = "PPG_SecurityPage_CustomZone"
'        Friend Const PPG_SecurityPage_ZoneDefault As String = "PPG_SecurityPage_ZoneDefault"
'        Friend Const PPG_SecurityPage_Included As String = "PPG_SecurityPage_Included"
'        Friend Const PPG_SecurityPage_Excluded As String = "PPG_SecurityPage_Excluded"
'        Friend Const PPG_SecurityPage_FullTrustToolTip As String = "PPG_SecurityPage_FullTrustToolTip"
'        Friend Const PPG_SecurityPage_BadPermissionToolTip As String = "PPG_SecurityPage_BadPermissionToolTip"
'        Friend Const PPG_SecurityPage_HeaderIncluded As String = "PPG_SecurityPage_HeaderIncluded"
'        Friend Const PPG_SecurityPage_HeaderPermission As String = "PPG_SecurityPage_HeaderPermission"
'        Friend Const PPG_SecurityPage_HeaderSetting As String = "PPG_SecurityPage_HeaderSetting"
'        Friend Const PPG_SecurityPage_HelpLabelText As String = "PPG_SecurityPage_HelpLabelText"
'        Friend Const PPG_SecurityPage_HelpLabelLink As String = "PPG_SecurityPage_HelpLabelLink"
'        Friend Const PPG_SecurityPage_BadDropDownValue As String = "PPG_SecurityPage_BadDropDownValue"
'        Friend Const PPG_SecurityPage_SecurityPermissionCannotBeExcluded As String = "PPG_SecurityPage_SecurityPermissionCannotBeExcluded"
'        Friend Const PPG_SecurityPage_ExecuteCannotBeExcluded As String = "PPG_SecurityPage_ExecuteCannotBeExcluded"
'        Friend Const PPG_SecurityPage_SwitchToFullTrustDialog As String = "PPG_SecurityPage_SwitchToFullTrustDialog"
'        Friend Const PPG_SecurityPage_SwitchToFullTrustDialogTitle As String = "PPG_SecurityPage_SwitchToFullTrustDialogTitle"
'        Friend Const PPG_SecurityPage_CouldNotSaveManifest As String = "PPG_SecurityPage_CouldNotSaveManifest"
'        Friend Const PPG_SecurityPage_CouldNotLoadManifest As String = "PPG_SecurityPage_CouldNotLoadManifest"
'        Friend Const PPG_SecurityPage_CheckAccessibilityName As String = "PPG_SecurityPage_CheckAccessibilityName"
'        Friend Const PPG_SecurityPage_BlankAccessibilityName As String = "PPG_SecurityPage_BlankAccessibilityName"
'        Friend Const PPG_SecurityPage_BangAccessibilityName As String = "PPG_SecurityPage_BangAccessibilityName"
'        Friend Const PPG_SecurityPage_PermCalcFailed As String = "PPG_SecurityPage_PermCalcFailed"
'        Friend Const PPG_SecurityPage_PermCalcFailedCaption As String = "PPG_SecurityPage_PermCalcFailedCaption"
'        Friend Const PPG_SecurityAdvancedPage_Title As String = "PPG_SecurityAdvancedPage_Title"
'        Friend Const PPG_MgdStatus_BuildFailed As String = "PPG_MgdStatus_BuildFailed"
'        Friend Const PPG_MgdStatus_BuildComplete As String = "PPG_MgdStatus_BuildComplete"
'        Friend Const PPG_MgdStatus_BuildUnableToStart As String = "PPG_MgdStatus_BuildUnableToStart"
'        Friend Const PPG_MgdStatus_BuildFailedToStart As String = "PPG_MgdStatus_BuildFailedToStart"
'        Friend Const PPG_CustomPermissionSet As String = "PPG_CustomPermissionSet"
'        Friend Const PPG_NonePermissionSet As String = "PPG_NonePermissionSet"
'        Friend Const PPG_Signing_KeyFileBrowse_Title As String = "PPG_Signing_KeyFileBrowse_Title"
'        Friend Const PPG_Signing_KeyFileBrowse_Filter As String = "PPG_Signing_KeyFileBrowse_Filter"
'        Friend Const PPG_Signing_KeyFileNew_Title As String = "PPG_Signing_KeyFileNew_Title"
'        Friend Const PPG_Signing_OldPassWrong As String = "PPG_Signing_OldPassWrong"
'        Friend Const PPG_Signing_OldPassEmpty As String = "PPG_Signing_OldPassEmpty"
'        Friend Const PPG_Signing_NewPassEmpty As String = "PPG_Signing_NewPassEmpty"
'        Friend Const PPG_Signing_NewPassMismatch As String = "PPG_Signing_NewPassMismatch"
'        Friend Const PPG_Signing_NewPassTooShort As String = "PPG_Signing_NewPassTooShort"
'        Friend Const PPG_Signing_BrowseCertTitle As String = "PPG_Signing_BrowseCertTitle"
'        Friend Const PPG_Signing_BrowseCertStorePrompt As String = "PPG_Signing_BrowseCertStorePrompt"
'        Friend Const PPG_Signing_BrowseCertFileFilter As String = "PPG_Signing_BrowseCertFileFilter"
'        Friend Const PPG_Signing_IssuedTo As String = "PPG_Signing_IssuedTo"
'        Friend Const PPG_Signing_IssuedBy As String = "PPG_Signing_IssuedBy"
'        Friend Const PPG_Signing_Purpose As String = "PPG_Signing_Purpose"
'        Friend Const PPG_Signing_ExpirationDate As String = "PPG_Signing_ExpirationDate"
'        Friend Const PPG_Signing_More As String = "PPG_Signing_More"
'        Friend Const PPG_Signing_InvalidPassword As String = "PPG_Signing_InvalidPassword"
'        Friend Const PPG_Signing_InvalidPasswordTitle As String = "PPG_Signing_InvalidPasswordTitle"
'        Friend Const PPG_Signing_NoPrivateKey As String = "PPG_Signing_NoPrivateKey"
'        Friend Const PPG_Signing_NoPrivateKeyTitle As String = "PPG_Signing_NoPrivateKeyTitle"
'        Friend Const PPG_Signing_CertCreationError As String = "PPG_Signing_CertCreationError"
'        Friend Const PPG_Signing_AllPurposes As String = "PPG_Signing_AllPurposes"
'        Friend Const PPG_Signing_PasswordMismatch As String = "PPG_Signing_PasswordMismatch"
'        Friend Const PPG_Signing_NoPassword As String = "PPG_Signing_NoPassword"
'        Friend Const PPG_Signing_NoConfirm As String = "PPG_Signing_NoConfirm"
'        Friend Const PPG_Signing_NoData As String = "PPG_Signing_NoData"
'        Friend Const PPG_Signing_OpenExistingKeyPasswordTitle As String = "PPG_Signing_OpenExistingKeyPasswordTitle"
'        Friend Const PPG_Signing_OpenExistingKeyPasswordPrompt As String = "PPG_Signing_OpenExistingKeyPasswordPrompt"
'        Friend Const PPG_Signing_CreateNewKeyPasswordPrompt As String = "PPG_Signing_CreateNewKeyPasswordPrompt"
'        Friend Const PPG_Signing_ProjectAlreadyContainsFile As String = "PPG_Signing_ProjectAlreadyContainsFile"
'        Friend Const PPG_Signing_CouldNotImportFile As String = "PPG_Signing_CouldNotImportFile"
'        Friend Const PPG_Signing_KeyFileNameSuffix As String = "PPG_Signing_KeyFileNameSuffix"
'        Friend Const PPG_Signing_CertificateNotCodeSigning As String = "PPG_Signing_CertificateNotCodeSigning"
'        Friend Const PPG_UndoTransaction As String = "PPG_UndoTransaction"
'        Friend Const PPG_WindowsApp As String = "PPG_WindowsApp"
'        Friend Const PPG_WindowsService As String = "PPG_WindowsService"
'        Friend Const PPG_WindowsClassLib As String = "PPG_WindowsClassLib"
'        Friend Const PPG_CommandLineApp As String = "PPG_CommandLineApp"
'        Friend Const PPG_WebControlLib As String = "PPG_WebControlLib"
'        Friend Const PPG_Reference_CanNotRemoveReference As String = "PPG_Reference_CanNotRemoveReference"
'        Friend Const PPG_Reference_RemoveImportsFailUnexpected As String = "PPG_Reference_RemoveImportsFailUnexpected"
'        Friend Const PPG_Reference_AddWebReference As String = "PPG_Reference_AddWebReference"
'        Friend Const PPG_Reference_FailedToUpdateWebReference As String = "PPG_Reference_FailedToUpdateWebReference"
'        Friend Const PPG_WebReferenceTypeName As String = "PPG_WebReferenceTypeName"
'        Friend Const PPG_UrlBehavior_Static As String = "PPG_UrlBehavior_Static"
'        Friend Const PPG_UrlBehavior_Dynamic As String = "PPG_UrlBehavior_Dynamic"
'        Friend Const PPG_WebReferenceNameDescription As String = "PPG_WebReferenceNameDescription"
'        Friend Const PPG_UrlBehaviorName As String = "PPG_UrlBehaviorName"
'        Friend Const PPG_UrlBehaviorDescription As String = "PPG_UrlBehaviorDescription"
'        Friend Const PPG_WebReferenceUrlName As String = "PPG_WebReferenceUrlName"
'        Friend Const PPG_WebReferenceUrlDescription As String = "PPG_WebReferenceUrlDescription"
'        Friend Const PPG_ReferenceDetail_Title As String = "PPG_ReferenceDetail_Title"
'        Friend Const PropPage_RemoteMachineBlankError As String = "PropPage_RemoteMachineBlankError"
'        Friend Const PropPage_ProgramNotExist As String = "PropPage_ProgramNotExist"
'        Friend Const PropPage_NeedExternalProgram As String = "PropPage_NeedExternalProgram"
'        Friend Const PropPage_NotAnExeError As String = "PropPage_NotAnExeError"
'        Friend Const PropPage_NeedURL As String = "PropPage_NeedURL"
'        Friend Const PropPage_InvalidURL As String = "PropPage_InvalidURL"
'        Friend Const PropPage_WorkingDirError As String = "PropPage_WorkingDirError"
'        Friend Const PropPage_ReferenceNotFound As String = "PropPage_ReferenceNotFound"
'        Friend Const PropPage_UnusedReferenceTitle As String = "PropPage_UnusedReferenceTitle"
'        Friend Const PropPage_UnusedReferenceRemoveButton As String = "PropPage_UnusedReferenceRemoveButton"
'        Friend Const PropPage_UnusedReferenceNoUnusedReferences As String = "PropPage_UnusedReferenceNoUnusedReferences"
'        Friend Const PropPage_UnusedReferenceCompileFail As String = "PropPage_UnusedReferenceCompileFail"
'        Friend Const PropPage_UnusedReferenceCompileWaiting As String = "PropPage_UnusedReferenceCompileWaiting"
'        Friend Const PropPage_UnusedReferenceError As String = "PropPage_UnusedReferenceError"
'        Friend Const PPG_InvalidHexString As String = "PPG_InvalidHexString"
'        Friend Const PropPage_NeedResFile As String = "PropPage_NeedResFile"
'        Friend Const PropPage_ResourceFileNotExist As String = "PropPage_ResourceFileNotExist"
'        Friend Const APPDES_Title As String = "APPDES_Title"
'        Friend Const APPDES_SettingsTabTitle As String = "APPDES_SettingsTabTitle"
'        Friend Const APPDES_ResourceTabTitle As String = "APPDES_ResourceTabTitle"
'        Friend Const APPDES_ErrorLoading_Msg As String = "APPDES_ErrorLoading_Msg"
'        Friend Const APPDES_ErrorLoadingPropPage As String = "APPDES_ErrorLoadingPropPage"
'        Friend Const APPDES_DesignerLoader_NotDeferred As String = "APPDES_DesignerLoader_NotDeferred"
'        Friend Const APPDES_ClickHereCreateResx As String = "APPDES_ClickHereCreateResx"
'        Friend Const APPDES_ClickHereCreateSettings As String = "APPDES_ClickHereCreateSettings"
'        Friend Const APPDES_FileNotFound_1Arg As String = "APPDES_FileNotFound_1Arg"
'        Friend Const APPDES_EditorAlreadyOpen_1Arg As String = "APPDES_EditorAlreadyOpen_1Arg"
'        Friend Const APPDES_OverflowButton_AccessibilityName As String = "APPDES_OverflowButton_AccessibilityName"
'        Friend Const APPDES_OverflowButton_Tooltip As String = "APPDES_OverflowButton_Tooltip"
'        Friend Const APPDES_SpecialFileNotSupported As String = "APPDES_SpecialFileNotSupported"
'        Friend Const PPG_Property_AssemblyName As String = "PPG_Property_AssemblyName"
'        Friend Const PPG_Property_RootNamespace As String = "PPG_Property_RootNamespace"
'        Friend Const PPG_Property_StartupObject As String = "PPG_Property_StartupObject"
'        Friend Const PPG_Property_ApplicationIcon As String = "PPG_Property_ApplicationIcon"
'        Friend Const PPG_Property_AssemblyVersion As String = "PPG_Property_AssemblyVersion"
'        Friend Const PPG_Property_AssemblyFileVersion As String = "PPG_Property_AssemblyFileVersion"
'        Friend Const PPG_Property_AssemblyGuid As String = "PPG_Property_AssemblyGuid"
'        Friend Const PPG_Property_CustomSubMain As String = "PPG_Property_CustomSubMain"
'        Friend Const PPG_Property_StartProgram As String = "PPG_Property_StartProgram"
'        Friend Const PPG_Property_StartURL As String = "PPG_Property_StartURL"
'        Friend Const PPG_Property_StartWorkingDirectory As String = "PPG_Property_StartWorkingDirectory"
'        Friend Const PPG_Property_RemoteDebugMachine As String = "PPG_Property_RemoteDebugMachine"
'        Friend Const APPDES_HostingPanelName As String = "APPDES_HostingPanelName"
'        Friend Const APPDES_TabButtonDefaultAction As String = "APPDES_TabButtonDefaultAction"
'        Friend Const APPDES_TabListDescription As String = "APPDES_TabListDescription"
'        Friend Const APPDES_PageName As String = "APPDES_PageName"
'        Friend Const DFX_DesignerReadOnlyCaption As String = "DFX_DesignerReadOnlyCaption"
'        Friend Const DFX_DesignerLoaderIVsTextStreamNotFound As String = "DFX_DesignerLoaderIVsTextStreamNotFound"
'        Friend Const DFX_DesignerLoaderIVsTextStreamNotFoundNoFile As String = "DFX_DesignerLoaderIVsTextStreamNotFoundNoFile"
'        Friend Const DFX_EditorNoDesignerService As String = "DFX_EditorNoDesignerService"
'        Friend Const DFX_InvalidPhysicalViewName As String = "DFX_InvalidPhysicalViewName"
'        Friend Const DFX_UnableCreateTextBuffer As String = "DFX_UnableCreateTextBuffer"
'        Friend Const DFX_NoLocalRegistry As String = "DFX_NoLocalRegistry"
'        Friend Const DFX_ReplaceTextStreamFailed As String = "DFX_ReplaceTextStreamFailed"
'        Friend Const DFX_BufferReadOnly As String = "DFX_BufferReadOnly"
'        Friend Const DFX_IncompatibleBuffer As String = "DFX_IncompatibleBuffer"
'        Friend Const DFX_NotSupported As String = "DFX_NotSupported"
'        Friend Const DFX_WindowPane_UnknownError As String = "DFX_WindowPane_UnknownError"
'        Friend Const DFX_CreateEditorInstanceFailed_Ex As String = "DFX_CreateEditorInstanceFailed_Ex"
'        Friend Const DFX_UnableToCheckout As String = "DFX_UnableToCheckout"
'        Friend Const DFX_Error_Default_Caption As String = "DFX_Error_Default_Caption"
'        Friend Const OptionPage_Editor_InvalidTabSize As String = "OptionPage_Editor_InvalidTabSize"
'        Friend Const OptionPage_Editor_InvalidIndentSize As String = "OptionPage_Editor_InvalidIndentSize"
'        Friend Const OptionPage_Environment_StartUpShowEmptyEnvironment As String = "OptionPage_Environment_StartUpShowEmptyEnvironment"
'        Friend Const OptionPage_Environment_StartUpLoadLastLoadedSolution As String = "OptionPage_Environment_StartUpLoadLastLoadedSolution"
'        Friend Const OptionPage_Environment_StartUpShowNewProjectDialogBox As String = "OptionPage_Environment_StartUpShowNewProjectDialogBox"
'        Friend Const OptionPage_Environment_StartUpShowOpenProjectDialogBox As String = "OptionPage_Environment_StartUpShowOpenProjectDialogBox"
'        Friend Const OptionPage_Environment_ShowHelpOptionRequiresRestartOfIde As String = "OptionPage_Environment_ShowHelpOptionRequiresRestartOfIde"
'        Friend Const OptionPage_Project_IllegalDefaultProjectDirectory As String = "OptionPage_Project_IllegalDefaultProjectDirectory"
'        Friend Const PermissionSet_Requires As String = "PermissionSet_Requires"
'        Friend Const RSE_ResourceNameColumn As String = "RSE_ResourceNameColumn"
'        Friend Const RSE_TypeColumn As String = "RSE_TypeColumn"
'        Friend Const RSE_ResourceColumn As String = "RSE_ResourceColumn"
'        Friend Const RSE_CommentColumn As String = "RSE_CommentColumn"
'        Friend Const SingleFileGenerator_FailedToGenerateFile_1Arg As String = "SingleFileGenerator_FailedToGenerateFile_1Arg"
'        Friend Const RSE_Err_CantEditEmbeddedResource As String = "RSE_Err_CantEditEmbeddedResource"
'        Friend Const RSE_Err_RenameNotSupported As String = "RSE_Err_RenameNotSupported"
'        Friend Const RSE_Err_CantFindResourceFile_1Arg As String = "RSE_Err_CantFindResourceFile_1Arg"
'        Friend Const RSE_Err_LoadingResource_1Arg As String = "RSE_Err_LoadingResource_1Arg"
'        Friend Const RSE_Err_NameBlank As String = "RSE_Err_NameBlank"
'        Friend Const RSE_Err_DuplicateName_1Arg As String = "RSE_Err_DuplicateName_1Arg"
'        Friend Const RSE_Err_UnexpectedResourceType As String = "RSE_Err_UnexpectedResourceType"
'        Friend Const RSE_Err_CantCreateNewResource_2Args As String = "RSE_Err_CantCreateNewResource_2Args"
'        Friend Const RSE_Err_CantPlay_1Arg As String = "RSE_Err_CantPlay_1Arg"
'        Friend Const RSE_Err_CantSaveResource_1Arg As String = "RSE_Err_CantSaveResource_1Arg"
'        Friend Const RSE_Err_UserCancel As String = "RSE_Err_UserCancel"
'        Friend Const RSE_Err_BadData As String = "RSE_Err_BadData"
'        Friend Const RSE_Err_BadIdentifier_2Arg As String = "RSE_Err_BadIdentifier_2Arg"
'        Friend Const RSE_Err_MaxFilesLimitation As String = "RSE_Err_MaxFilesLimitation"
'        Friend Const RSE_Err_InternalException As String = "RSE_Err_InternalException"
'        Friend Const RSE_Err_Unexpected_NoResource As String = "RSE_Err_Unexpected_NoResource"
'        Friend Const RSE_Err_CantConvertFromString_2Args As String = "RSE_Err_CantConvertFromString_2Args"
'        Friend Const RSE_Err_CantUseEmptyValue As String = "RSE_Err_CantUseEmptyValue"
'        Friend Const RSE_Err_CantBeEmpty As String = "RSE_Err_CantBeEmpty"
'        Friend Const RSE_Err_CantEditInDebugMode As String = "RSE_Err_CantEditInDebugMode"
'        Friend Const RSE_Err_UpdateADependentFile As String = "RSE_Err_UpdateADependentFile"
'        Friend Const RSE_Err_CantAddUnsupportedResource_1Arg As String = "RSE_Err_CantAddUnsupportedResource_1Arg"
'        Friend Const RSE_Err_CantAddFileToDeviceProject_1Arg As String = "RSE_Err_CantAddFileToDeviceProject_1Arg"
'        Friend Const RSE_Err_TypeIsNotSupported_1Arg As String = "RSE_Err_TypeIsNotSupported_1Arg"
'        Friend Const RSE_Err_CantSaveResouce_1Arg As String = "RSE_Err_CantSaveResouce_1Arg"
'        Friend Const RSE_Err_Name As String = "RSE_Err_Name"
'        Friend Const RSE_Err_NameList As String = "RSE_Err_NameList"
'        Friend Const RSE_NothingValue As String = "RSE_NothingValue"
'        Friend Const RSE_UnknownType As String = "RSE_UnknownType"
'        Friend Const RSE_NonEditableValue As String = "RSE_NonEditableValue"
'        Friend Const RSE_DetailsCol_Name As String = "RSE_DetailsCol_Name"
'        Friend Const RSE_DetailsCol_Filename As String = "RSE_DetailsCol_Filename"
'        Friend Const RSE_DetailsCol_ImageType As String = "RSE_DetailsCol_ImageType"
'        Friend Const RSE_DetailsCol_Size As String = "RSE_DetailsCol_Size"
'        Friend Const RSE_DetailsCol_Comment As String = "RSE_DetailsCol_Comment"
'        Friend Const RSE_DetailsCol_Encoding As String = "RSE_DetailsCol_Encoding"
'        Friend Const RSE_GraphicSizeFormat As String = "RSE_GraphicSizeFormat"
'        Friend Const RSE_FileSizeFormatKB As String = "RSE_FileSizeFormatKB"
'        Friend Const RSE_FileSizeFormatBytes_1Arg As String = "RSE_FileSizeFormatBytes_1Arg"
'        Friend Const RSE_Cat_Strings As String = "RSE_Cat_Strings"
'        Friend Const RSE_Cat_Images As String = "RSE_Cat_Images"
'        Friend Const RSE_Cat_Icons As String = "RSE_Cat_Icons"
'        Friend Const RSE_Cat_Audio As String = "RSE_Cat_Audio"
'        Friend Const RSE_Cat_Files As String = "RSE_Cat_Files"
'        Friend Const RSE_Cat_Other As String = "RSE_Cat_Other"
'        Friend Const RSE_Btn_Add As String = "RSE_Btn_Add"
'        Friend Const RSE_Btn_Remove As String = "RSE_Btn_Remove"
'        Friend Const RSE_Btn_Views As String = "RSE_Btn_Views"
'        Friend Const RSE_Btn_Add_ExistingFile As String = "RSE_Btn_Add_ExistingFile"
'        Friend Const RSE_Btn_Add_String As String = "RSE_Btn_Add_String"
'        Friend Const RSE_Btn_Add_Image As String = "RSE_Btn_Add_Image"
'        Friend Const RSE_Btn_Add_Image_PNG As String = "RSE_Btn_Add_Image_PNG"
'        Friend Const RSE_Btn_Add_Image_BMP As String = "RSE_Btn_Add_Image_BMP"
'        Friend Const RSE_Btn_Add_Image_GIF As String = "RSE_Btn_Add_Image_GIF"
'        Friend Const RSE_Btn_Add_Image_JPEG As String = "RSE_Btn_Add_Image_JPEG"
'        Friend Const RSE_Btn_Add_Image_TIFF As String = "RSE_Btn_Add_Image_TIFF"
'        Friend Const RSE_Btn_Add_Icon As String = "RSE_Btn_Add_Icon"
'        Friend Const RSE_Btn_Add_TextFile As String = "RSE_Btn_Add_TextFile"
'        Friend Const RSE_Type_BMP As String = "RSE_Type_BMP"
'        Friend Const RSE_Type_EXIF As String = "RSE_Type_EXIF"
'        Friend Const RSE_Type_GIF As String = "RSE_Type_GIF"
'        Friend Const RSE_Type_JPEG As String = "RSE_Type_JPEG"
'        Friend Const RSE_Type_MEMBMP As String = "RSE_Type_MEMBMP"
'        Friend Const RSE_Type_PNG As String = "RSE_Type_PNG"
'        Friend Const RSE_Type_TIFF As String = "RSE_Type_TIFF"
'        Friend Const RSE_Type_Icon As String = "RSE_Type_Icon"
'        Friend Const RSE_Type_TextFile As String = "RSE_Type_TextFile"
'        Friend Const RSE_Type_BinaryFile As String = "RSE_Type_BinaryFile"
'        Friend Const RSE_Type_Wave As String = "RSE_Type_Wave"
'        Friend Const RES_PersistenceMode_Linked As String = "RES_PersistenceMode_Linked"
'        Friend Const RES_PersistenceMode_Embeded As String = "RES_PersistenceMode_Embeded"
'        Friend Const RSE_Filter_Bitmap As String = "RSE_Filter_Bitmap"
'        Friend Const RSE_Filter_Icon As String = "RSE_Filter_Icon"
'        Friend Const RSE_Filter_Audio As String = "RSE_Filter_Audio"
'        Friend Const RSE_Filter_Text As String = "RSE_Filter_Text"
'        Friend Const RSE_Filter_All As String = "RSE_Filter_All"
'        Friend Const RSE_FilterSave_BMP As String = "RSE_FilterSave_BMP"
'        Friend Const RSE_FilterSave_PNG As String = "RSE_FilterSave_PNG"
'        Friend Const RSE_FilterSave_GIF As String = "RSE_FilterSave_GIF"
'        Friend Const RSE_FilterSave_JPEG As String = "RSE_FilterSave_JPEG"
'        Friend Const RSE_FilterSave_TIFF As String = "RSE_FilterSave_TIFF"
'        Friend Const RSE_FilterSave_Icon As String = "RSE_FilterSave_Icon"
'        Friend Const RSE_Btn_Views_List As String = "RSE_Btn_Views_List"
'        Friend Const RSE_Btn_Views_Details As String = "RSE_Btn_Views_Details"
'        Friend Const RSE_Btn_Views_Thumbnail As String = "RSE_Btn_Views_Thumbnail"
'        Friend Const RSE_ResourceEditor As String = "RSE_ResourceEditor"
'        Friend Const RSE_DlgTitle_AddExisting As String = "RSE_DlgTitle_AddExisting"
'        Friend Const RSE_DlgTitle_Import_1Arg As String = "RSE_DlgTitle_Import_1Arg"
'        Friend Const RSE_DlgTitle_Export_1Arg As String = "RSE_DlgTitle_Export_1Arg"
'        Friend Const RSE_DlgTitle_AddNew As String = "RSE_DlgTitle_AddNew"
'        Friend Const RSE_Dlg_ReplaceExistingFile As String = "RSE_Dlg_ReplaceExistingFile"
'        Friend Const RSE_Dlg_ReplaceExistingFiles As String = "RSE_Dlg_ReplaceExistingFiles"
'        Friend Const RSE_Dlg_ExportMultiple As String = "RSE_Dlg_ExportMultiple"
'        Friend Const RSE_Dlg_ContinueAnyway As String = "RSE_Dlg_ContinueAnyway"
'        Friend Const RSE_Dlg_SetCustomTool As String = "RSE_Dlg_SetCustomTool"
'        Friend Const RSE_Task_BadLink_2Args As String = "RSE_Task_BadLink_2Args"
'        Friend Const RSE_Task_CantInstantiate_2Args As String = "RSE_Task_CantInstantiate_2Args"
'        Friend Const RSE_Task_NonrecommendedName_1Arg As String = "RSE_Task_NonrecommendedName_1Arg"
'        Friend Const RSE_Task_CantChangeCustomToolOrNamespace As String = "RSE_Task_CantChangeCustomToolOrNamespace"
'        Friend Const RSE_Task_WarningCustomToolNotSet As String = "RSE_Task_WarningCustomToolNotSet"
'        Friend Const RSE_Task_InvalidName_1Arg As String = "RSE_Task_InvalidName_1Arg"
'        Friend Const RSE_EncodingDisplayName As String = "RSE_EncodingDisplayName"
'        Friend Const RSE_DefaultEncoding As String = "RSE_DefaultEncoding"
'        Friend Const RSE_Undo_ChangeName As String = "RSE_Undo_ChangeName"
'        Friend Const RSE_Undo_AddResources_1Arg As String = "RSE_Undo_AddResources_1Arg"
'        Friend Const RSE_Undo_RemoveResources_1Arg As String = "RSE_Undo_RemoveResources_1Arg"
'        Friend Const RSE_Undo_DeleteResourceCell As String = "RSE_Undo_DeleteResourceCell"
'        Friend Const RSE_Font_MenuStrip As String = "RSE_Font_MenuStrip"
'        Friend Const RSE_Font_StringTable As String = "RSE_Font_StringTable"
'        Friend Const RSE_Font_ListView As String = "RSE_Font_ListView"
'        Friend Const RSE_NoBoldFontsInCategoryButtons As String = "RSE_NoBoldFontsInCategoryButtons"
'        Friend Const RSE_CategiesLabel As String = "RSE_CategiesLabel"
'        Friend Const RSE_PropDesc_Name As String = "RSE_PropDesc_Name"
'        Friend Const RSE_PropDesc_Comment As String = "RSE_PropDesc_Comment"
'        Friend Const RSE_PropDesc_Encoding As String = "RSE_PropDesc_Encoding"
'        Friend Const RSE_PropDesc_Filename As String = "RSE_PropDesc_Filename"
'        Friend Const RSE_PropDesc_FileType As String = "RSE_PropDesc_FileType"
'        Friend Const RSE_PropDesc_Persistence As String = "RSE_PropDesc_Persistence"
'        Friend Const RSE_PropDesc_Type As String = "RSE_PropDesc_Type"
'        Friend Const RSE_PropDesc_Value As String = "RSE_PropDesc_Value"
'        Friend Const RFS_CantCreateResourcesFolder_Folder_ExMsg As String = "RFS_CantCreateResourcesFolder_Folder_ExMsg"
'        Friend Const RFS_CantAddFileToProject_File_ExMsg As String = "RFS_CantAddFileToProject_File_ExMsg"
'        Friend Const RFS_CantAddFileToProject_File As String = "RFS_CantAddFileToProject_File"
'        Friend Const RFS_QueryReplaceFile_File As String = "RFS_QueryReplaceFile_File"
'        Friend Const RFS_QueryReplaceFileTitle_Editor As String = "RFS_QueryReplaceFileTitle_Editor"
'        Friend Const RFS_QueryRemoveLink_Folder_Link As String = "RFS_QueryRemoveLink_Folder_Link"
'        Friend Const RFS_QueryRemoveLinkTitle_Editor As String = "RFS_QueryRemoveLinkTitle_Editor"
'        Friend Const RFS_FindNotFound_File As String = "RFS_FindNotFound_File"
'        Friend Const SD_ComboBoxItem_ConnectionStringType As String = "SD_ComboBoxItem_ConnectionStringType"
'        Friend Const SD_ComboBoxItem_WebReferenceType As String = "SD_ComboBoxItem_WebReferenceType"
'        Friend Const SD_ComboBoxItem_BrowseType As String = "SD_ComboBoxItem_BrowseType"
'        Friend Const SD_ComboBoxItem_ApplicationScope As String = "SD_ComboBoxItem_ApplicationScope"
'        Friend Const SD_ComboBoxItem_UserScope As String = "SD_ComboBoxItem_UserScope"
'        Friend Const SD_GridViewNameColumnHeaderText As String = "SD_GridViewNameColumnHeaderText"
'        Friend Const SD_GridViewTypeColumnHeaderText As String = "SD_GridViewTypeColumnHeaderText"
'        Friend Const SD_GridViewScopeColumnHeaderText As String = "SD_GridViewScopeColumnHeaderText"
'        Friend Const SD_GridViewValueColumnHeaderText As String = "SD_GridViewValueColumnHeaderText"
'        Friend Const SD_UnknownType As String = "SD_UnknownType"
'        Friend Const SD_UndoTran_NameChanged As String = "SD_UndoTran_NameChanged"
'        Friend Const SD_UndoTran_TypeChanged As String = "SD_UndoTran_TypeChanged"
'        Friend Const SD_UndoTran_ScopeChanged As String = "SD_UndoTran_ScopeChanged"
'        Friend Const SD_UndoTran_RoamingChanged As String = "SD_UndoTran_RoamingChanged"
'        Friend Const SD_UndoTran_DescriptionChanged As String = "SD_UndoTran_DescriptionChanged"
'        Friend Const SD_UndoTran_ProviderChanged As String = "SD_UndoTran_ProviderChanged"
'        Friend Const SD_UndoTran_SerializedValueChanged As String = "SD_UndoTran_SerializedValueChanged"
'        Friend Const SD_UndoTran_GenerateDefaultValueInCode As String = "SD_UndoTran_GenerateDefaultValueInCode"
'        Friend Const SD_UndoTran_RemoveMultipleSettings_1Arg As String = "SD_UndoTran_RemoveMultipleSettings_1Arg"
'        Friend Const SD_Err_CantLoadSettingsFile As String = "SD_Err_CantLoadSettingsFile"
'        Friend Const SD_NewValuesAdded As String = "SD_NewValuesAdded"
'        Friend Const SD_ReplaceValueWithAppConfigValueTitle As String = "SD_ReplaceValueWithAppConfigValueTitle"
'        Friend Const SD_ReplaceValueWithAppConfigValue As String = "SD_ReplaceValueWithAppConfigValue"
'        Friend Const SD_FailedToLoadAppConfigValues As String = "SD_FailedToLoadAppConfigValues"
'        Friend Const SD_FailedToSaveAppConfigValues As String = "SD_FailedToSaveAppConfigValues"
'        Friend Const SD_ERR_DuplicateName_1Arg As String = "SD_ERR_DuplicateName_1Arg"
'        Friend Const SD_ERR_InvalidIdentifier_1Arg As String = "SD_ERR_InvalidIdentifier_1Arg"
'        Friend Const SD_ERR_InvalidTypeName_1Arg As String = "SD_ERR_InvalidTypeName_1Arg"
'        Friend Const SD_ERR_NameEmpty As String = "SD_ERR_NameEmpty"
'        Friend Const SD_ERR_InvalidValue_2Arg As String = "SD_ERR_InvalidValue_2Arg"
'        Friend Const SD_ERR_GenericTypesNotSupported_1Arg As String = "SD_ERR_GenericTypesNotSupported_1Arg"
'        Friend Const SD_ERR_AbstractTypesNotSupported_1Arg As String = "SD_ERR_AbstractTypesNotSupported_1Arg"
'        Friend Const SD_ERR_RenameNotSupported As String = "SD_ERR_RenameNotSupported"
'        Friend Const SD_ERR_ModifyParamsNotSupported As String = "SD_ERR_ModifyParamsNotSupported"
'        Friend Const SD_ERR_CantEditInDebugMode As String = "SD_ERR_CantEditInDebugMode"
'        Friend Const SD_MNU_AddSettingText As String = "SD_MNU_AddSettingText"
'        Friend Const SD_MNU_RemoveSettingText As String = "SD_MNU_RemoveSettingText"
'        Friend Const SD_FullDescriptionText As String = "SD_FullDescriptionText"
'        Friend Const SD_LinkPartOfDescriptionText As String = "SD_LinkPartOfDescriptionText"
'        Friend Const SD_IncludeSensitiveInfoInConnectionStringWarning As String = "SD_IncludeSensitiveInfoInConnectionStringWarning"
'        Friend Const SD_SelectATypeTreeView_AccessibleName As String = "SD_SelectATypeTreeView_AccessibleName"
'        Friend Const SD_CODEGENCMT_COMMON1 As String = "SD_CODEGENCMT_COMMON1"
'        Friend Const SD_CODEGENCMT_COMMON2 As String = "SD_CODEGENCMT_COMMON2"
'        Friend Const SD_CODEGENCMT_COMMON3 As String = "SD_CODEGENCMT_COMMON3"
'        Friend Const SD_CODEGENCMT_COMMON4 As String = "SD_CODEGENCMT_COMMON4"
'        Friend Const SD_CODEGENCMT_COMMON5 As String = "SD_CODEGENCMT_COMMON5"
'        Friend Const SD_CODEGENCMT_HOWTO_ATTACHEVTS As String = "SD_CODEGENCMT_HOWTO_ATTACHEVTS"
'        Friend Const SD_CODEGENCMT_HANDLE_CHANGING As String = "SD_CODEGENCMT_HANDLE_CHANGING"
'        Friend Const SD_CODEGENCMT_HANDLE_SAVING As String = "SD_CODEGENCMT_HANDLE_SAVING"
'        Friend Const SD_CODEGEN_FAILEDOPENCREATEEXTENDINGFILE As String = "SD_CODEGEN_FAILEDOPENCREATEEXTENDINGFILE"
'        Friend Const SD_DESCR_Description As String = "SD_DESCR_Description"
'        Friend Const SD_DESCR_GenerateDefaultValueInCode As String = "SD_DESCR_GenerateDefaultValueInCode"
'        Friend Const SD_DESCR_Group As String = "SD_DESCR_Group"
'        Friend Const SD_DESCR_GroupDescription As String = "SD_DESCR_GroupDescription"
'        Friend Const SD_DESCR_Roaming As String = "SD_DESCR_Roaming"
'        Friend Const SD_DESCR_Name As String = "SD_DESCR_Name"
'        Friend Const SD_DESCR_Provider As String = "SD_DESCR_Provider"
'        Friend Const SD_DESCR_Scope As String = "SD_DESCR_Scope"
'        Friend Const SD_DESCR_SerializedSettingType As String = "SD_DESCR_SerializedSettingType"
'        Friend Const SD_DESCR_Value As String = "SD_DESCR_Value"
'        Friend Const SD_SyncFiles_1Arg As String = "SD_SyncFiles_1Arg"
'        Friend Const SD_SyncFilesNoFilesFound_1Arg As String = "SD_SyncFilesNoFilesFound_1Arg"
'        Friend Const SD_SyncFilesOneOrMoreFailed As String = "SD_SyncFilesOneOrMoreFailed"
'        Friend Const SD_SFG_AutoSaveRegionText As String = "SD_SFG_AutoSaveRegionText"
'        Friend Const General_MissingService As String = "General_MissingService"
'        Friend Const PPG_Application_RootNamespaceJSharp As String = "PPG_Application_RootNamespaceJSharp"

'        Shared loader As SR = Nothing
'        Dim resources As ResourceManager

'        Friend Sub New()
'            resources = New System.Resources.ResourceManager("Microsoft.VisualStudio.Editors.Designer", Me.GetType().Module.Assembly)
'        End Sub

'        Private Shared Function GetLoader() As SR
'            If (loader Is Nothing) Then
'                SyncLock (GetType(SR))
'                    If (loader Is Nothing) Then
'                        loader = New SR()
'                    End If
'                End SyncLock
'            End If

'            Return loader
'        End Function

'        Public Shared ReadOnly Property Culture() As CultureInfo
'            Get
'                Return Nothing '/*use ResourceManager default, CultureInfo.CurrentUICulture*/
'            End Get
'        End Property

'        Public Shared Function GetString(ByVal name As String, ByVal ParamArray args As Object()) As String
'            '// null CultureInfo: let ResouceManager determine the culture
'            '// fxcop complains about not suppling a culture
'            Return GetString(SR.Culture, name, args)
'        End Function

'        Public Shared Function GetString(ByVal culture As CultureInfo, ByVal name As String, ByVal ParamArray args As Object()) As String
'            Dim sys As SR = GetLoader()
'            If (sys Is Nothing) Then
'                Return Nothing
'            End If
'            Dim res As String = sys.resources.GetString(name, culture)

'            If (args IsNot Nothing AndAlso args.Length > 0) Then
'                Return String.Format(res, args)
'            Else
'                Return res
'            End If
'        End Function

'        Public Shared Function GetString(ByVal name As String) As String
'            Return GetString(SR.Culture, name)
'        End Function

'        Public Shared Function GetString(ByVal culture As CultureInfo, ByVal name As String) As String
'            Dim sys As SR = GetLoader()
'            If (sys Is Nothing) Then
'                Return Nothing
'            End If
'            Return sys.resources.GetString(name, culture)
'        End Function

'        Public Shared Function GetBoolean(ByVal name As String) As Boolean
'            Return GetBoolean(SR.Culture, name)
'        End Function

'        Public Shared Function GetBoolean(ByVal culture As CultureInfo, ByVal name As String) As Boolean
'            Dim val As Boolean = False

'            Dim sys As SR = GetLoader()
'            If (sys IsNot Nothing) Then
'                Dim res As Object = sys.resources.GetObject(name, culture)
'                If (TypeOf res Is Boolean) Then
'                    val = DirectCast(res, Boolean)
'                End If
'            End If
'            Return val
'        End Function

'        Public Shared Function GetChar(ByVal name As String) As Char
'            Return GetChar(SR.Culture, name)
'        End Function

'        Public Shared Function GetChar(ByVal culture As CultureInfo, ByVal name As String) As Char
'            Dim val As Char

'            Dim sys As SR = GetLoader()
'            If (sys IsNot Nothing) Then
'                Dim res As Object = sys.resources.GetObject(name, culture)
'                If (TypeOf res Is Char) Then
'                    val = DirectCast(res, Char)
'                End If
'            End If
'            Return val
'        End Function

'        Public Shared Function GetByte(ByVal name As String) As Byte
'            Return GetByte(SR.Culture, name)
'        End Function

'        Public Shared Function GetByte(ByVal culture As CultureInfo, ByVal name As String) As Byte
'            Dim val As Byte = 0

'            Dim sys As SR = GetLoader()
'            If (sys IsNot Nothing) Then
'                Dim res As Object = sys.resources.GetObject(name, culture)
'                If (TypeOf res Is Byte) Then
'                    val = DirectCast(res, Byte)
'                End If
'            End If
'            Return val
'        End Function

'        Public Shared Function GetShort(ByVal name As String) As Short
'            Return GetShort(SR.Culture, name)
'        End Function

'        Public Shared Function GetShort(ByVal culture As CultureInfo, ByVal name As String) As Short
'            Dim val As Short = 0

'            Dim sys As SR = GetLoader()
'            If (sys IsNot Nothing) Then
'                Dim res As Object = sys.resources.GetObject(name, culture)
'                If (TypeOf res Is Short) Then
'                    val = DirectCast(res, Short)
'                End If
'            End If
'            Return val
'        End Function

'        Public Shared Function GetInt(ByVal name As String) As Integer
'            Return GetInt(SR.Culture, name)
'        End Function

'        Public Shared Function GetInt(ByVal culture As CultureInfo, ByVal name As String) As Integer
'            Dim val As Integer = 0

'            Dim sys As SR = GetLoader()
'            If (sys IsNot Nothing) Then
'                Dim res As Object = sys.resources.GetObject(name, culture)
'                If (TypeOf res Is Integer) Then
'                    val = DirectCast(res, Integer)
'                End If
'            End If
'            Return val
'        End Function

'        Public Shared Function GetLong(ByVal name As String) As Long
'            Return GetLong(SR.Culture, name)
'        End Function

'        Public Shared Function GetLong(ByVal culture As CultureInfo, ByVal name As String) As Long
'            Dim val As Long = 0

'            Dim sys As SR = GetLoader()
'            If (sys IsNot Nothing) Then
'                Dim res As Object = sys.resources.GetObject(name, culture)
'                If (TypeOf res Is Long) Then
'                    val = DirectCast(res, Long)
'                End If
'            End If
'            Return val
'        End Function

'        Public Shared Function GetFloat(ByVal name As String) As Single
'            Return GetFloat(SR.Culture, name)
'        End Function

'        Public Shared Function GetFloat(ByVal culture As CultureInfo, ByVal name As String) As Single
'            Dim val As Single = 0

'            Dim sys As SR = GetLoader()
'            If (sys Is Nothing) Then
'                Dim res As Object = sys.resources.GetObject(name, culture)
'                If (TypeOf res Is Single) Then
'                    val = DirectCast(res, Single)
'                End If
'            End If
'            Return val
'        End Function

'        Public Shared Function GetDouble(ByVal name As String) As Double
'            Return GetDouble(SR.Culture, name)
'        End Function

'        Public Shared Function GetDouble(ByVal culture As CultureInfo, ByVal name As String) As Double
'            Dim val As Double = 0.0

'            Dim sys As SR = GetLoader()
'            If (sys Is Nothing) Then
'                Dim res As Object = sys.resources.GetObject(name, culture)
'                If (TypeOf res Is Double) Then
'                    val = DirectCast(res, Double)
'                End If
'            End If
'            Return val
'        End Function

'        Public Shared Function GetObject(ByVal name As String) As Object
'            Return GetObject(SR.Culture, name)
'        End Function

'        Public Shared Function GetObject(ByVal culture As CultureInfo, ByVal name As String) As Object
'            Dim sys As SR = GetLoader()
'            If (sys Is Nothing) Then
'                Return Nothing
'            End If
'            Return sys.resources.GetObject(name, culture)
'        End Function
'    End Class
'End Namespace

#End If
