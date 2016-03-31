Imports Common = Microsoft.VisualStudio.Editors.AppDesCommon
Imports Microsoft.VisualStudio.Shell.Interop
Imports VSITEMID = Microsoft.VisualStudio.Editors.VSITEMIDAPPDES

Namespace Microsoft.VisualStudio.Editors.PropPageDesigner

    ''' <summary>
    ''' A class that is shared among all the property page designers and stores the currently
    '''   selected configuration and platform that the user has selected, so that all pages
    '''   are on, um, the same page.
    ''' Also listens for changes to the configurations that the user makes outside of the
    '''   project designer and allows the individual pages to get notifications on that.
    ''' </summary>
    ''' <remarks></remarks>
    Public NotInheritable Class ConfigurationState
        Implements IDisposable
        Implements IVsUpdateSolutionEvents
        Implements IVsCfgProviderEvents



#Region "Fields/constants"

        'The active project/hierarchy (with the current design, this can never change for this instance of the 
        '  project designer/configuration state
        Private m_Project As EnvDTE.Project
        Private m_ProjectHierarchy As IVsHierarchy

        'The configuration provider for the project
        Private m_VsCfgProvider As IVsCfgProvider2

        'Pointer to the parent application designer view
        Private m_View As ApplicationDesigner.ApplicationDesignerView

        'The current selections in the configuration/platform comboboxes
        Private m_SelectedConfigIndex As Integer
        Private m_SelectedPlatformIndex As Integer

        'The current list of entries to be displayed in the comboboxes of all config-dependent
        '  property pages
        Private m_ConfigurationDropdownEntries As DropdownItem()
        Private m_PlatformDropdownEntries As DropdownItem()

        'Solution build manager for this solution
        Private m_VsSolutionBuildManager As IVsSolutionBuildManager

        'Event-listening cookies
        Private m_UpdateSolutionEventsCookie As UInteger
        Private m_CfgProviderEventsCookie As UInteger

        'Last value of SimplifiedConfigMode, so we know when it changes
        Private m_SimplifiedConfigModeLastKnownValue As Boolean

#End Region


#Region "Enums"

        ''' <summary>
        ''' Selection types for the configuration and platform comboboxes
        ''' </summary>
        ''' <remarks></remarks>
        Public Enum SelectionTypes
            Normal    'Just a normal entry
            Active    'This is the entry for the currently-active configuration or platform
            All       'This is the All Configurations/All Platforms entry
        End Enum

#End Region


#Region "Events"

        ''' <summary>
        ''' Raised when the configuration or platform selected by the user has changed
        ''' </summary>
        ''' <remarks>
        ''' Listener must update their selection state by querying SelectedConfigIndex 
        '''   and SelectedPlatformIndex.
        ''' </remarks>>
        Public Event SelectedConfigurationChanged()

        ''' <summary>
        ''' Raised when the configuration/platform lists have changed.  Note that this
        '''   will *not* be followed by a SelectedConfigurationChanged event, but the
        '''   listener should still update the selection as well as their lists.
        ''' </summary>
        ''' <remarks>
        ''' Listener must update their selection state as well as their lists, by 
        '''   querying ConfigurationDropdownEntries, PlatformDropdownEntries, 
        '''   SelectedConfigIndex and SelectedPlatformIndex.
        ''' </remarks>
        Public Event ConfigurationListAndSelectionChanged()

        ''' <summary>
        ''' Raised when the undo/redo stack of a property page should be cleared because of
        '''   changes to configurations/platforms that are not currently supported by our
        '''   undo/redo story.
        ''' </summary>
        ''' <remarks></remarks>
        Public Event ClearConfigPageUndoRedoStacks()

        ''' <summary>
        ''' Raised when the value of the SimplifiedConfigMode property changes.
        ''' </summary>
        ''' <remarks></remarks>
        Public Event SimplifiedConfigModeChanged()

#End Region



        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub New(ByVal Project As EnvDTE.Project, ByVal ProjectHierarchy As IVsHierarchy, ByVal View As ApplicationDesigner.ApplicationDesignerView)
            If Project Is Nothing OrElse ProjectHierarchy Is Nothing OrElse View Is Nothing Then
                Debug.Fail("")
                Throw New ArgumentNullException()
            End If

            m_Project = Project
            m_ProjectHierarchy = ProjectHierarchy
            m_View = View

            Dim ConfigProvider As Object = Nothing
            VSErrorHandler.ThrowOnFailure(ProjectHierarchy.GetProperty(VSITEMID.ROOT, __VSHPROPID.VSHPROPID_ConfigurationProvider, ConfigProvider))
            m_VsCfgProvider = DirectCast(ConfigProvider, IVsCfgProvider2)

            'Initialize m_SimplifiedConfigModeLastKnownValue
            m_SimplifiedConfigModeLastKnownValue = IsSimplifiedConfigMode

            AdviseEventHandling()
        End Sub


        ''' <summary>
        ''' Dispose
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub Dispose() Implements System.IDisposable.Dispose
            UnadviseEventHandling()
            Me.m_Project = Nothing
            Me.m_VsCfgProvider = Nothing
            m_VsSolutionBuildManager = Nothing
        End Sub


        ''' <summary>
        ''' Changes the selected configuration and platform by name and selection type search, and notifies the pages of the update.
        ''' </summary>
        ''' <param name="ConfigName">The config name to search for.</param>
        ''' <param name="ConfigSelectionType">The configuration selection type to search for.  For more information, see comments for FindItemToSelect.</param>
        ''' <param name="PlatformName">The platform name to search for.</param>
        ''' <param name="PlatformSelectionType">The platform selection type to search for.  For more information, see comments for FindItemToSelect.</param>
        ''' <param name="PreferExactMatch">For more information, see comments for FindItemToSelect.</param>
        ''' <param name="FireNotifications">If true, notifications are sent to the pages (but only if the selection actually changed)</param>
        ''' <remarks></remarks>
        Public Sub ChangeSelection(ByVal ConfigName As String, ByVal ConfigSelectionType As SelectionTypes, ByVal PlatformName As String, ByVal PlatformSelectionType As SelectionTypes, ByVal PreferExactMatch As Boolean, ByVal FireNotifications As Boolean)
            Dim NewSelectedConfigIndex As Integer = FindItemToSelect(m_ConfigurationDropdownEntries, m_SelectedConfigIndex, ConfigName, ConfigSelectionType, PreferExactMatch)
            Dim NewSelectedPlatformIndex As Integer = FindItemToSelect(m_PlatformDropdownEntries, m_SelectedPlatformIndex, PlatformName, PlatformSelectionType, PreferExactMatch)
            Debug.Assert(NewSelectedConfigIndex >= 0 AndAlso NewSelectedConfigIndex < m_ConfigurationDropdownEntries.Length)
            Debug.Assert(NewSelectedPlatformIndex >= 0 AndAlso NewSelectedPlatformIndex < m_PlatformDropdownEntries.Length)

            ChangeSelection(NewSelectedConfigIndex, NewSelectedPlatformIndex, FireNotifications)
        End Sub


        ''' <summary>
        ''' Changes the selected configuration and platform to the given indices, and notifies the pages of the update.
        ''' </summary>
        ''' <param name="ConfigIndex">The index to select in the configuration list</param>
        ''' <param name="PlatformIndex">The index to select in the platform list</param>
        ''' <param name="FireNotifications">If true, notifications are sent to the pages (but only if the selection actually changed)</param>
        ''' <remarks></remarks>
        Public Sub ChangeSelection(ByVal ConfigIndex As Integer, ByVal PlatformIndex As Integer, ByVal FireNotifications As Boolean)
            Debug.Assert(ConfigIndex >= 0 AndAlso ConfigIndex < m_ConfigurationDropdownEntries.Length)
            Debug.Assert(PlatformIndex >= 0 AndAlso PlatformIndex < m_PlatformDropdownEntries.Length)

            If m_SelectedConfigIndex <> ConfigIndex OrElse m_SelectedPlatformIndex <> PlatformIndex Then
                m_SelectedConfigIndex = ConfigIndex
                m_SelectedPlatformIndex = PlatformIndex

                'Notify the pages to update their selection
                If FireNotifications Then
                    RaiseEvent SelectedConfigurationChanged()
                End If
            End If
        End Sub


        ''' <summary>
        ''' Returns the index of the item to be the currently selected item in the configuration dropdown of all
        '''   config-dependent property pages
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property SelectedConfigIndex() As Integer
            Get
                Return m_SelectedConfigIndex
            End Get
        End Property


        ''' <summary>
        ''' Returns the index of the item to be the currently selected item in the platform dropdown of all
        '''   config-dependent property pages        
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property SelectedPlatformIndex() As Integer
            Get
                Return m_SelectedPlatformIndex
            End Get
        End Property


        'Note: currently, this class keeps a list of the configuration and platforms to be displayed, and the
        '  PropPageDesigner contains the comboboxes (i.e., each page's PropPageDesignerView has comboboxes, so there
        '  are multiple of them.  It would be easier to move the comboboxes to the application designer, except:
        '  a) This keeps more separation between PropPageDesigner and ApplicationDesigner
        '  b) Control tabbing needs to move between the page and the comboboxes, and that is currently not possible
        '       if the comboboxes are in the app designer (because the app designer is the docview parent and the
        '       shell doesn't currently allow it to participate in message pre-processing)
        '  c) I didn't want to change the UI that much at this point in the product cycle


        ''' <summary>
        ''' All current entries to be displayed in each pages' configuration combobox
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property ConfigurationDropdownEntries() As DropdownItem()
            Get
                If m_PlatformDropdownEntries Is Nothing OrElse m_ConfigurationDropdownEntries Is Nothing Then
                    UpdateDropdownEntries()
                End If
                Return m_ConfigurationDropdownEntries
            End Get
        End Property


        ''' <summary>
        ''' All current entries to be displayed in each pages' platform combobox
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property PlatformDropdownEntries() As DropdownItem()
            Get
                If m_PlatformDropdownEntries Is Nothing OrElse m_ConfigurationDropdownEntries Is Nothing Then
                    UpdateDropdownEntries()
                End If
                Return m_PlatformDropdownEntries
            End Get
        End Property


        ''' <summary>
        ''' Updates the configuration and platform dropdown entries based on the current
        '''   configurations in the project
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub UpdateDropdownEntries()
            'Populate the dropdowns
            Dim ActiveConfiguration As EnvDTE.Configuration = Common.DTEUtils.GetActiveDTEConfiguration(Project)
            Dim ConfigNames() As String = GetAllConfigNames()
            Dim PlatformNames() As String = GetAllPlatformNames()
            Debug.Assert(GetAllConfigs().Length = ConfigNames.Length * PlatformNames.Length, "Isn't there one config for each config name/platform combination?")

            'Populate the configuration entries
            m_ConfigurationDropdownEntries = New DropdownItem(ConfigNames.Length + 2) {} 'Max possible size (+2: "active" and "all configs")
            m_PlatformDropdownEntries = New DropdownItem(PlatformNames.Length + 2) {} 'Max possible size (+2: "active" and "all platforms")

            'Populate the configuration entries

            '... Add the "Active" item at the top
            Dim ConfigIndex As Integer = 0
            m_ConfigurationDropdownEntries(ConfigIndex) = New DropdownItem(ActiveConfiguration.ConfigurationName, SelectionTypes.Active)
            ConfigIndex += 1

            '... followed by all individual items (but only if more than one)
            If ConfigNames.Length > 1 Then
                For Each ConfigName As String In ConfigNames
                    m_ConfigurationDropdownEntries(ConfigIndex) = New DropdownItem(ConfigName, SelectionTypes.Normal)
                    ConfigIndex += 1
                Next

                '... followed by "All Configurations" (but only if there's more than one)
                m_ConfigurationDropdownEntries(ConfigIndex) = New DropdownItem(SR.GetString(SR.PPG_AllConfigurations), SelectionTypes.All)
                ConfigIndex += 1
            End If
            ReDim Preserve m_ConfigurationDropdownEntries(ConfigIndex - 1)

            'Populate the platform entries

            '... Add the "Active" item at the top
            Dim PlatformIndex As Integer = 0
            m_PlatformDropdownEntries(PlatformIndex) = New DropdownItem(ActiveConfiguration.PlatformName, SelectionTypes.Active)
            PlatformIndex += 1

            '... followed by all individual items (but only if more than one)
            If PlatformNames.Length > 1 Then
                For Each PlatformName As String In PlatformNames
                    m_PlatformDropdownEntries(PlatformIndex) = New DropdownItem(PlatformName, SelectionTypes.Normal)
                    PlatformIndex += 1
                Next

                '... followed by "All platforms" (but only if there's more than one)
                m_PlatformDropdownEntries(PlatformIndex) = New DropdownItem(SR.GetString(SR.PPG_AllPlatforms), SelectionTypes.All)
                PlatformIndex += 1
            End If
            ReDim Preserve m_PlatformDropdownEntries(PlatformIndex - 1)

#If DEBUG Then
            For i As Integer = 0 To m_ConfigurationDropdownEntries.Length - 1
                Debug.Assert(m_ConfigurationDropdownEntries(i) IsNot Nothing)
            Next
            For i As Integer = 0 To m_PlatformDropdownEntries.Length - 1
                Debug.Assert(m_PlatformDropdownEntries(i) IsNot Nothing)
            Next
            Debug.Assert(m_ConfigurationDropdownEntries.Length > 0)
            Debug.Assert(m_PlatformDropdownEntries.Length > 0)
#End If

            m_SelectedConfigIndex = 0
            m_SelectedPlatformIndex = 0
        End Sub


        ''' <summary>
        ''' Retrieves the project associated with this configuration state
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property Project() As EnvDTE.Project
            Get
                Debug.Assert(m_Project IsNot Nothing)
                Return m_Project
            End Get
        End Property


        ''' <summary>
        ''' Configuration provider for the project (IVsCfgProvider2)
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property VsCfgProvider() As IVsCfgProvider2
            Get
                Debug.Assert(m_VsCfgProvider IsNot Nothing)
                Return m_VsCfgProvider
            End Get
        End Property


        ''' <summary>
        ''' Returns whether or not we're in simplified config mode for this project, which means that
        '''   we hide the configuration/platform comboboxes.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property IsSimplifiedConfigMode() As Boolean
            Get
                Return AppDesCommon.ShellUtil.GetIsSimplifiedConfigMode(m_ProjectHierarchy)
            End Get
        End Property


        ''' <summary>
        ''' Since there's no notification for the Tools.Options "Show Advanced Build Configurations" property when it
        '''   has changed, we check manually on WM_SETFOCUS in the property page designer.  The designer calls us on this
        '''   method for us to do the check.
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub CheckForModeChanges()
            Common.Switches.TracePDConfigs("CheckForModeChanges")
            Dim SimplifiedConfigModeCurrent As Boolean = Me.IsSimplifiedConfigMode
            If SimplifiedConfigModeCurrent <> m_SimplifiedConfigModeLastKnownValue Then
                Common.Switches.TracePDConfigs("Simplified Config Mode has changed")
                m_SimplifiedConfigModeLastKnownValue = SimplifiedConfigModeCurrent
                RaiseEvent SimplifiedConfigModeChanged()
            End If
        End Sub


        ''' <summary>
        ''' Retrieves the current list of all configurations for the project
        ''' </summary>
        ''' <remarks></remarks>
        Public Function GetAllConfigs() As IVsCfg()
            Dim ConfigCount As UInteger() = New UInteger(0) {} 'Interop declaration requires us to use an array
            VSErrorHandler.ThrowOnFailure(VsCfgProvider.GetCfgs(0, Nothing, ConfigCount, Nothing))
            Debug.Assert(ConfigCount(0) > 0, "Why no configs?")

            Dim Configs() As IVsCfg = New IVsCfg(CInt(ConfigCount(0)) - 1) {}
            Dim ActualCount As UInteger() = New UInteger(0) {}
            VSErrorHandler.ThrowOnFailure(VsCfgProvider.GetCfgs(CUInt(Configs.Length), Configs, ActualCount, Nothing))
            Debug.Assert(ActualCount(0) = ConfigCount(0), "Unexpected # of configs returned")
            Return Configs
        End Function


        ''' <summary>
        ''' Retrieves the current list of all configuration names for the project
        ''' </summary>
        ''' <remarks></remarks>
        Private Function GetAllConfigNames() As String()
            Dim NameObject As Object = Project.ConfigurationManager.ConfigurationRowNames()
            Dim NameArray As Array = DirectCast(NameObject, Array)
            Dim Names(NameArray.Length - 1) As String
            For i As Integer = 0 To NameArray.Length - 1
                Names(i) = DirectCast(NameArray.GetValue(i), String)
            Next

            Return Names
        End Function


        ''' <summary>
        ''' Retrieves the current list of all configuration platform names for the project
        ''' </summary>
        ''' <remarks></remarks>
        Private Function GetAllPlatformNames() As String()
            Dim NameObject As Object = Project.ConfigurationManager.PlatformNames()
            Dim NameArray As Array = DirectCast(NameObject, Array)
            Dim Names(NameArray.Length - 1) As String
            For i As Integer = 0 To NameArray.Length - 1
                Names(i) = DirectCast(NameArray.GetValue(i), String)
            Next

            Return Names
        End Function


        ''' <summary>
        ''' Selects an item in the configuration or platform combobox by configuration/platform name and selection type.
        ''' </summary>
        ''' <param name="ExistingItems">The set of existing DropdownItems to select from</param>
        ''' <param name="CurrentIndex">The current index into the ExistingItems array.</param>
        ''' <param name="DesiredName">The desired name to find.</param>
        ''' <param name="DesiredSelectionType">The desired selection type.  
        '''   If this is "All", then the "All Configurations/Platforms" item will be selected.  
        '''   If this is "Active", then the "Active" item will be selected, and the Name argument ignored.  
        '''   If this is "Normal", then the list will be searched to find a match for Name.  There may be two entries 
        '''     in the list with a given name (one "active", one not).  Preference will be given to the current item, 
        '''     unless PreferExactMatch is True.</param>
        ''' <param name="PreferExactMatch">If true, and DesiredSelectionType is "Normal", then the search will prefer
        '''     an exact matching of Name and selection type (i.e., the non-active item will be preferred, even if the current
        '''     item is the active version of the same config/platform name).  If false, preference is given to the current item
        '''     when searching for a "Normal" selection type.</param>
        ''' <returns>The new index into the ExistingItems array of the found item.</returns>
        ''' <remarks></remarks>
        Private Function FindItemToSelect(ByVal ExistingItems() As DropdownItem, ByVal CurrentIndex As Integer, ByVal DesiredName As String, ByVal DesiredSelectionType As SelectionTypes, _
            ByVal PreferExactMatch As Boolean _
        ) As Integer
            If DesiredSelectionType = SelectionTypes.All Then
                'All configurations or platforms
                For i As Integer = 0 To ExistingItems.Length - 1
                    If ExistingItems(i).SelectionType = ConfigurationState.SelectionTypes.All Then
                        'Found it
                        Return i
                        Exit Function
                    End If
                Next

                'We don't add an All Items entry to the combobox if there's only a single entry, so if we didn't find it,
                '  that's probably what happened, and we can just select the single entry in the list.
                If ExistingItems.Length = 1 Then
                    Return 0
                Else
                    Debug.Fail("Couldn't find the All Configurations or Platforms entry in the combobox")
                    Return 0
                End If
            Else
                Debug.Assert(DesiredSelectionType = SelectionTypes.Active OrElse DesiredSelectionType = SelectionTypes.Normal, "Unexpected selection type")

                Dim SelectedItem As DropdownItem = ExistingItems(CurrentIndex)
                Debug.Assert(SelectedItem IsNot Nothing)

                If DesiredSelectionType = SelectionTypes.Active Then
                    'If we desired selection type is "Active", then we choose the active configuration, whatever it
                    '  currently is.
                    For i As Integer = 0 To ExistingItems.Length - 1
                        If ExistingItems(i).SelectionType = SelectionTypes.Active Then
                            Return i
                        End If
                    Next

                    Debug.Fail("No ""active"" config/platform type found in the list")
                End If

                If SelectedItem IsNot Nothing AndAlso SelectedItem.Name.Equals(DesiredName, StringComparison.CurrentCultureIgnoreCase) Then
                    'The name selected in the combobox is already the same as the name we're trying to select
                    If SelectedItem.SelectionType = DesiredSelectionType Then
                        'The selection type is exactly the same, so the current index is correct
                        Return CurrentIndex
                    End If

                    If Not PreferExactMatch Then
                        'The selection type is not the same (i.e., one of them is "Active" and the other is not), but 
                        '  we were told not to force the selection type to be the same, so the current index is fine
                        Return CurrentIndex
                    End If
                End If

                'Search through until we find a matching name and (if ForceSelectionType) matching selection type
                If PreferExactMatch Then
                    For i As Integer = 0 To ExistingItems.Length - 1
                        If ExistingItems(i).Name.Equals(DesiredName, StringComparison.CurrentCultureIgnoreCase) Then
                            If ExistingItems(i).SelectionType = DesiredSelectionType Then
                                'Exact match
                                Return i
                            End If
                        End If
                    Next
                End If

                'If that didn't work, try matching against only the name (this is a valid scenario - for instance,
                '  if there's only one config/platform, the only entry we'll have is the "Active" one
                For i As Integer = 0 To ExistingItems.Length - 1
                    If ExistingItems(i).Name.Equals(DesiredName, StringComparison.CurrentCultureIgnoreCase) Then
                        Return i
                    End If
                Next
            End If

            Debug.Fail("Couldn't find an item to select")
            Return 0
        End Function




#Region "Configuration change handling"

        ''' <summary>
        ''' Start listening to configuration change events
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub AdviseEventHandling()
            Debug.Assert(m_UpdateSolutionEventsCookie = 0)
            Debug.Assert(m_CfgProviderEventsCookie = 0)
            m_VsSolutionBuildManager = DirectCast(m_View.GetService(GetType(IVsSolutionBuildManager)), IVsSolutionBuildManager)
            VSErrorHandler.ThrowOnFailure(m_VsSolutionBuildManager.AdviseUpdateSolutionEvents(Me, m_UpdateSolutionEventsCookie))
            VSErrorHandler.ThrowOnFailure(VsCfgProvider.AdviseCfgProviderEvents(Me, m_CfgProviderEventsCookie))
        End Sub


        ''' <summary>
        ''' Stoplistening to configuration change events
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub UnadviseEventHandling()
            If m_UpdateSolutionEventsCookie <> 0 AndAlso m_VsSolutionBuildManager IsNot Nothing Then
                VSErrorHandler.ThrowOnFailure(m_VsSolutionBuildManager.UnadviseUpdateSolutionEvents(m_UpdateSolutionEventsCookie))
                m_UpdateSolutionEventsCookie = 0
            End If

            If m_CfgProviderEventsCookie <> 0 AndAlso m_VsCfgProvider IsNot Nothing Then
                VSErrorHandler.ThrowOnFailure(m_VsCfgProvider.UnadviseCfgProviderEvents(m_CfgProviderEventsCookie))
                m_CfgProviderEventsCookie = 0
            End If
        End Sub


        ''' <summary>
        ''' Update the current set of entries and fire the notification events to the property pages
        ''' </summary>
        ''' <param name="KeepCurrentSelection">If True, then attempts to keep the current selection after the list is updated.  If false, then
        '''   the current "active" configuration and platform will be selected.</param>
        ''' <remarks></remarks>
        Private Sub UpdateEntriesAndNotifyPages(ByVal KeepCurrentSelection As Boolean)
            Dim CurrentConfigName As String = ConfigurationDropdownEntries(m_SelectedConfigIndex).Name
            Dim CurrentConfigSelectionType As SelectionTypes = ConfigurationDropdownEntries(m_SelectedConfigIndex).SelectionType
            Dim CurrentPlatformName As String = PlatformDropdownEntries(m_SelectedPlatformIndex).Name
            Dim CurrentPlatformSelectionType As SelectionTypes = PlatformDropdownEntries(m_SelectedPlatformIndex).SelectionType

            UpdateDropdownEntries()

            If KeepCurrentSelection Then
                ChangeSelection(CurrentConfigName, CurrentConfigSelectionType, CurrentPlatformName, CurrentPlatformSelectionType, PreferExactMatch:=True, FireNotifications:=False)
            Else
                'Just select the active config/platform
                ChangeSelection("", SelectionTypes.Active, "", SelectionTypes.Active, PreferExactMatch:=True, FireNotifications:=False)
            End If

            'Notify pages to update their lists and selections
            RaiseEvent ConfigurationListAndSelectionChanged()
        End Sub


        ''' <summary>
        ''' A configuration name has been added.
        ''' </summary>
        ''' <param name="pszCfgName"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function OnCfgNameAdded(ByVal pszCfgName As String) As Integer Implements Shell.Interop.IVsCfgProviderEvents.OnCfgNameAdded
            Try
                Common.Switches.TracePDConfigs("OnCfgNameAdded: Updating list")
                UpdateEntriesAndNotifyPages(KeepCurrentSelection:=True)
                CheckForModeChanges()
                Return VSConstants.S_OK
            Catch ex As Exception
                Debug.Fail("Exception during OnCfgNameAdded: " & ex.ToString)
                Throw
            End Try
        End Function


        ''' <summary>
        ''' A configuration name has been deleted.
        ''' </summary>
        ''' <param name="CfgName"></param>
        ''' <remarks></remarks>
        Public Function OnCfgNameDeleted(ByVal CfgName As String) As Integer Implements Shell.Interop.IVsCfgProviderEvents.OnCfgNameDeleted
            Try
                Common.Switches.TracePDConfigs("OnCfgNameDeleted: Clearing undo/redo stack")

                'CONSIDER: It would be nice to keep a map with config/platforms and index by integer in MultipleValueStore, updating the names
                '  as they are renamed/deleted, so that we can properly handle undo/redo after a rename/delete.
                RaiseEvent ClearConfigPageUndoRedoStacks()
                UpdateEntriesAndNotifyPages(KeepCurrentSelection:=False)
                CheckForModeChanges()
                Return VSConstants.S_OK
            Catch ex As Exception
                Debug.Fail("Exception during OnCfgNameAdded: " & ex.ToString)
                Throw
            End Try
        End Function


        ''' <summary>
        ''' A configuration name has been renamed
        ''' </summary>
        ''' <param name="OldName"></param>
        ''' <param name="NewName"></param>
        ''' <remarks></remarks>
        Public Function OnCfgNameRenamed(ByVal OldName As String, ByVal NewName As String) As Integer Implements Shell.Interop.IVsCfgProviderEvents.OnCfgNameRenamed
            Try
                Common.Switches.TracePDConfigs("OnCfgNameRenamed: Clearing undo/redo stack")

                'CONSIDER: It would be nice to keep a map with config/platforms and index by integer in MultipleValueStore, updating the names
                '  as they are renamed/deleted, so that we can properly handle undo/redo after a rename/delete.
                RaiseEvent ClearConfigPageUndoRedoStacks()
                UpdateEntriesAndNotifyPages(KeepCurrentSelection:=False)
                CheckForModeChanges()
                Return VSConstants.S_OK
            Catch ex As Exception
                Debug.Fail("Exception during OnCfgNameAdded: " & ex.ToString)
                Throw
            End Try
        End Function


        ''' <summary>
        ''' A platform name has been added
        ''' </summary>
        ''' <param name="pszPlatformName"></param>
        ''' <remarks></remarks>
        Public Function OnPlatformNameAdded(ByVal pszPlatformName As String) As Integer Implements Shell.Interop.IVsCfgProviderEvents.OnPlatformNameAdded
            Try
                Common.Switches.TracePDConfigs("OnPlatformNameAdded: Updating list")
                UpdateEntriesAndNotifyPages(KeepCurrentSelection:=True)
                CheckForModeChanges()
                Return VSConstants.S_OK
            Catch ex As Exception
                Debug.Fail("Exception during OnCfgNameAdded: " & ex.ToString)
                Throw
            End Try
        End Function


        ''' <summary>
        ''' A platform name has been deleted
        ''' </summary>
        ''' <param name="pszPlatformName"></param>
        ''' <remarks></remarks>
        Public Function OnPlatformNameDeleted(ByVal pszPlatformName As String) As Integer Implements Shell.Interop.IVsCfgProviderEvents.OnPlatformNameDeleted
            Try
                Common.Switches.TracePDConfigs("OnPlatformNameDeleted: Clearing undo/redo stack")

                'CONSIDER: It would be nice to keep a map with config/platforms and index by integer in MultipleValueStore, updating the names
                '  as they are renamed/deleted, so that we can properly handle undo/redo after a rename/delete.
                RaiseEvent ClearConfigPageUndoRedoStacks()
                UpdateEntriesAndNotifyPages(KeepCurrentSelection:=False)
                CheckForModeChanges()
                Return VSConstants.S_OK
            Catch ex As Exception
                Debug.Fail("Exception during OnCfgNameAdded: " & ex.ToString)
                Throw
            End Try
        End Function


        ''' <summary>
        ''' Fired after the active project configuration for a project in the solution has been
        '''   changed.  If NULL is passed for pIVsHierarchy, sinks for this event have to assume that
        '''   every project in the solution may have changed, even if there is only one project
        '''   active in the solution.
        ''' </summary>
        ''' <param name="pIVsHierarchy"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function OnActiveProjectCfgChange(ByVal pIVsHierarchy As Shell.Interop.IVsHierarchy) As Integer Implements Shell.Interop.IVsUpdateSolutionEvents.OnActiveProjectCfgChange
            Try
                If pIVsHierarchy Is Nothing OrElse pIVsHierarchy Is m_ProjectHierarchy Then
                    Common.Switches.TracePDConfigs("OnActiveProjectCfgChange: Hierarchy matches or is Nothing, changing configs")

                    'Note that with KeepCurrentSelection:=True, if the currently-selected config/platform is the "Active" one, then the selection
                    '  will continue to be the "Active" one, which means it will "follow" the change in selection.  If the current selection is not
                    '  the active will, then it will stay where it was.  This is the desired behavior.
                    UpdateEntriesAndNotifyPages(KeepCurrentSelection:=True)
                Else
                    Common.Switches.TracePDConfigs("OnActiveProjectCfgChange: Not our hierarchy, ignoring change")
                End If

                CheckForModeChanges()
                Return VSConstants.S_OK
            Catch ex As Exception
                Debug.Fail("Exception during OnCfgNameAdded: " & ex.ToString)
                Throw
            End Try
        End Function

        Public Function UpdateSolution_Begin(ByRef pfCancelUpdate As Integer) As Integer Implements Shell.Interop.IVsUpdateSolutionEvents.UpdateSolution_Begin
            Return VSConstants.S_OK
        End Function

        Public Function UpdateSolution_Cancel() As Integer Implements Shell.Interop.IVsUpdateSolutionEvents.UpdateSolution_Cancel
            Return VSConstants.S_OK
        End Function

        Public Function UpdateSolution_Done(ByVal fSucceeded As Integer, ByVal fModified As Integer, ByVal fCancelCommand As Integer) As Integer Implements Shell.Interop.IVsUpdateSolutionEvents.UpdateSolution_Done
            CheckForModeChanges()
            Return VSConstants.S_OK
        End Function

        Public Function UpdateSolution_StartUpdate(ByRef pfCancelUpdate As Integer) As Integer Implements Shell.Interop.IVsUpdateSolutionEvents.UpdateSolution_StartUpdate
            Return VSConstants.S_OK
        End Function

#End Region


        '**************************************



        ''' <summary>
        ''' This nested class represents an item in the configuration or platform dropdown listbox
        ''' </summary>
        ''' <remarks></remarks>
        Public Class DropdownItem

            Public Name As String
            Public SelectionType As SelectionTypes

            ''' <summary>
            ''' Constructor.
            ''' </summary>
            ''' <param name="Name">The configuration or platform name</param>
            ''' <param name="SelectionType">Type of entry</param>
            ''' <remarks></remarks>
            Public Sub New(ByVal Name As String, ByVal SelectionType As SelectionTypes)
                Debug.Assert(Name <> "")
                Debug.Assert([Enum].IsDefined(GetType(SelectionTypes), SelectionType))

                Me.Name = Name
                Me.SelectionType = SelectionType
            End Sub


            ''' <summary>
            ''' Returns the display string to show in the combobox
            ''' </summary>
            ''' <returns></returns>
            ''' <remarks></remarks>
            Public ReadOnly Property DisplayName() As String
                Get
                    Select Case Me.SelectionType
                        Case SelectionTypes.Active
                            Return SR.GetString(SR.PPG_ActiveConfigOrPlatformFormatString_1Arg, Name)
                        Case SelectionTypes.All, SelectionTypes.Normal
                            Return Name
                        Case Else
                            Debug.Fail("Unexpected EntryType")
                            Return Name
                    End Select
                End Get
            End Property

        End Class

    End Class

End Namespace
