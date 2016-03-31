Imports System.CodeDom
Imports System.CodeDom.Compiler
Imports System.ComponentModel
Imports System.ComponentModel.Design

Imports Microsoft.VisualStudio.Designer.Interfaces
Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Shell.Interop


Namespace Microsoft.VisualStudio.Editors.DesignerFramework

    ''' <summary>
    ''' Gets the language-dependent terminology for Public/Friend
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class AccessModifierConverter
        Private _converter As TypeConverter

        Public Enum Access
            [Public]
            [Friend]
        End Enum

        Public Sub New(ByVal provider As CodeDomProvider)
            If provider IsNot Nothing Then
                Dim converter As TypeConverter = provider.GetConverter(GetType(MemberAttributes))

                'If the convert we got is just the standard converter, the codedom provider 
                '  must not support this converter.  We're better off using defaults.
                If converter.GetType() IsNot GetType(TypeConverter) OrElse Not converter.CanConvertTo(GetType(String)) Then
                    _converter = converter
                End If
            End If
        End Sub

        ''' <summary>
        ''' Gets the language-dependent terminology for Public/Friend
        ''' </summary>
        ''' <param name="accessibility"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function ConvertToString(ByVal accessibility As AccessModifierConverter.Access) As String
            Select Case accessibility
                Case AccessModifierConverter.Access.Friend
                    If _converter IsNot Nothing Then
                        Return _converter.ConvertToString(MemberAttributes.Assembly)
                    Else
                        Return "Internal"
                    End If
                Case AccessModifierConverter.Access.Public
                    If _converter IsNot Nothing Then
                        Return _converter.ConvertToString(MemberAttributes.Public)
                    Else
                        Return "Public"
                    End If
                Case Else
                    Throw Common.CreateArgumentException("AccessModifier")
            End Select
        End Function
    End Class

    Friend MustInherit Class AccessModifierCombobox
        Implements IDisposable

        Private m_isDisposed As Boolean = False
        Private m_rootDesigner As BaseRootDesigner
        Private m_projectItem As EnvDTE.ProjectItem
        Private m_serviceProvider As IServiceProvider
        Private m_namespaceToOverrideIfCustomToolIsEmpty As String
        Private m_codeGeneratorEntries As New List(Of CodeGenerator)
        Private m_recognizedCustomToolValues As New List(Of String)

        Private m_designerCommandBarComboBoxCommand As DesignerCommandBarComboBox
        Private m_commandIdCombobox As CommandID

        ' Cached flag to indicate if the custom tools associated with this combobox are
        ' registered for the current project type.
        ' The states are True (registered), False (not registerd) or Missing (we haven't 
        ' checked the project system yet)
        ' This field should only be accessed through the CustomToolsRegistered property.
        Private m_customToolsRegistered As Nullable(Of Boolean)

        Enum Access
            [Public]
            [Friend]
        End Enum

#Region "Nested class CodeGenerator"

        Private MustInherit Class CodeGenerator
            Private m_customToolValue As String

            Public Sub New(ByVal customToolValue As String)
                If customToolValue Is Nothing Then
                    Throw New ArgumentNullException("customToolValue")
                End If
                m_customToolValue = customToolValue
            End Sub

            Public MustOverride ReadOnly Property DisplayName() As String
            Public ReadOnly Property CustomToolValue() As String
                Get
                    Return m_customToolValue
                End Get
            End Property
        End Class

        Private Class CodeGeneratorWithName
            Inherits CodeGenerator

            Private m_displayName As String

            Public Sub New(ByVal displayName As String, ByVal customToolValue As String)
                MyBase.New(customToolValue)

                If displayName Is Nothing Then
                    Throw New ArgumentNullException(displayName)
                End If
                m_displayName = displayName
            End Sub

            Public Overrides ReadOnly Property DisplayName() As String
                Get
                    Return m_displayName
                End Get
            End Property
        End Class

        Private Class CodeGeneratorWithDelayedName
            Inherits CodeGenerator

            Private m_accessibility As AccessModifierConverter.Access
            Private m_serviceProvider As IServiceProvider

            Public Sub New(ByVal accessibility As AccessModifierConverter.Access, ByVal serviceProvider As IServiceProvider, ByVal customToolValue As String)
                MyBase.New(customToolValue)

                If serviceProvider Is Nothing Then
                    Throw New ArgumentNullException("serviceProvider")
                End If

                m_accessibility = accessibility
                m_serviceProvider = serviceProvider
            End Sub

            Public Overrides ReadOnly Property DisplayName() As String
                Get
                    Dim codeDomProvider As CodeDomProvider = Nothing
                    Dim vsmdCodeDomProvider As IVSMDCodeDomProvider = TryCast(m_serviceProvider.GetService(GetType(IVSMDCodeDomProvider)), IVSMDCodeDomProvider)
                    If vsmdCodeDomProvider IsNot Nothing Then
                        codeDomProvider = TryCast(vsmdCodeDomProvider.CodeDomProvider(), CodeDomProvider)
                    End If

                    Return New AccessModifierConverter(codeDomProvider).ConvertToString(m_accessibility)
                End Get
            End Property
        End Class

#End Region

#Region "Nested class "

        ''' <summary>
        ''' This class registers/unregisters a DesigerMenuCommand with the package.
        ''' This is needed for the access modifier combobox because we want the
        '''   combobox to remain enabled when the user clicks away from the designer
        '''   and onto, say, the solution explorer.
        ''' We can get this effect as long as we don't use DefaultDisabled in the
        '''   .vsct file.  However, when the user clicks away from the editor, the
        '''   shell will keep the combobox enabled, but it will remove the selected
        '''   text, so it goes blank.  This all is confusing to the user.  Unfortunately,
        '''   command bars weren't really designed for editors (we're using them because
        '''   historical reasons forced on us by DTP).
        ''' To keep the text from going blank when a designer doesn't have the focus,
        '''   we need to register a command handler with our package.  This class keeps
        '''   track of the last command registered with the package for a given
        '''   commandID.
        ''' When a designer is activated, it should register its command here for the
        '''   access modifier combobox.  It should only unregister it when the designer
        '''   is closed.  The last designer to register here will control the case
        '''   when the designer is not focused and the command gets routed through 
        '''   the package.
        ''' </summary>
        ''' <remarks></remarks>
        Friend Class DesignerMenuCommandForwarder


            '
            ' Map from command ID to LIFO list of command handlers. The item at the head of the list is the item 
            ' that is currently registered with the shell's MenuCommandService
            ' 
            Private Shared s_packageCommandForwarderLists As New Dictionary(Of CommandID, LinkedList(Of DesignerMenuCommand))

            Public Shared Sub RegisterMenuCommandForwarder(ByVal commandID As CommandID, ByVal forwarder As DesignerMenuCommand)
                Dim menuCommandService As IMenuCommandService = VBPackage.Instance.MenuCommandService
                If menuCommandService IsNot Nothing Then
                    ' Remove previous active command (if any) and tell the shell that this is no longer the active 
                    ' command...
                    Dim previousCommand As DesignerMenuCommand = GetMenuCommandAtHeadOfInternalList(commandID)
                    If previousCommand IsNot Nothing Then
                        menuCommandService.RemoveCommand(previousCommand)
                    End If

                    ' Add the command to our internal list of commands...
                    AddMenuCommandForwarderToInternalList(commandID, forwarder)

                    menuCommandService.AddCommand(forwarder)
                Else
                    Debug.Fail("No package menu command service?")
                End If
            End Sub

            Public Shared Sub UnregisterMenuCommandForwarder(ByVal commandID As CommandID, ByVal forwarder As DesignerMenuCommand)
                Dim menuCommandService As IMenuCommandService = VBPackage.Instance.MenuCommandService
                If menuCommandService IsNot Nothing Then
                    ' Remove the currently active command (if any) from the MenuCommandService
                    Dim previousCommand As DesignerMenuCommand = GetMenuCommandAtHeadOfInternalList(commandID)
                    If previousCommand IsNot Nothing Then
                        menuCommandService.RemoveCommand(previousCommand)
                    End If

                    ' Update our internal list of commands
                    RemoveMenuCommandForwarderFromInternalList(commandID, forwarder)

                    ' Re-register the new command that is supposed to be active
                    Dim newCommand As DesignerMenuCommand = GetMenuCommandAtHeadOfInternalList(commandID)
                    If newCommand IsNot Nothing Then
                        menuCommandService.AddCommand(newCommand)
                    Else
                        ' Add an imposter command to keep an handler around when the UI is closed
                        Dim imposterCommand As ImposterDesignerMenuCommand = New ImposterDesignerMenuCommand(commandID)
                        AddMenuCommandForwarderToInternalList(commandID, imposterCommand)
                        menuCommandService.AddCommand(imposterCommand)
                    End If
                Else
                    Debug.Fail("No package menu command service?")
                End If
            End Sub

            ''' <summary>
            ''' Get the command at the head of the queue for the given command ID
            ''' </summary>
            ''' <returns>
            ''' The first command at the head of the queue, or NULL if no the queue is emty
            ''' </returns>
            ''' <remarks></remarks>
            Protected Shared Function GetMenuCommandAtHeadOfInternalList(ByVal cmdId As CommandId) As DesignerMenuCommand
                Dim list As LinkedList(Of DesignerMenuCommand) = Nothing
                If (Not s_packageCommandForwarderLists.TryGetValue(cmdId, list)) OrElse list Is Nothing OrElse list.Count = 0 Then
                    Return Nothing
                Else
                    Return list.First.Value
                End If
            End Function

            ''' <summary>
            ''' Add a menu command forwarder to our internal LIFO queue. 
            ''' If the command is in the list, but isn't the first command, we move it to the head of the list
            ''' </summary>
            ''' <remarks></remarks>
            Protected Shared Sub AddMenuCommandForwarderToInternalList(ByVal cmdId As CommandId, ByVal command As DesignerMenuCommand)
                Dim list As LinkedList(Of DesignerMenuCommand) = Nothing

                ' Demand create the list corresponding to this cmdId
                If Not s_packageCommandForwarderLists.TryGetValue(cmdId, list) Then
                    list = New LinkedList(Of DesignerMenuCommand)
                    s_packageCommandForwarderLists(cmdId) = list
                End If

                ' Move the command to the head of the queue...
                list.Remove(command)
                list.AddFirst(command)

                Debug.Assert(list.Count > 0, "We just added a menu command to the list - how come it is empty!")
            End Sub


            ''' <summary>
            ''' Remove a menu command forwarder from our internal LIFO queue. 
            ''' </summary>
            ''' <remarks></remarks>
            Protected Shared Sub RemoveMenuCommandForwarderFromInternalList(ByVal cmdId As CommandId, ByVal command As DesignerMenuCommand)
                Dim list As LinkedList(Of DesignerMenuCommand) = Nothing
                If s_packageCommandForwarderLists.TryGetValue(cmdId, list) Then
                    list.Remove(command)
                End If

                If list IsNot Nothing AndAlso list.Count = 0 Then
                    s_packageCommandForwarderLists.Remove(cmdId)
                End If
            End Sub

        End Class

#End Region

        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="rootDesigner"></param>
        ''' <param name="serviceProvider"></param>
        ''' <param name="projectItem"></param>
        ''' <param name="namespaceToOverrideIfCustomToolIsEmpty">
        ''' If this is not Nothing, then setting a new custom tool value will also change the
        '''   custom tool namespace to this value, if the current custom tool is not empty.
        ''' 
        ''' This is currently used for the VB scenario - if the custom tool has been yet been set, and
        '''   the user turns on code generation, we want to also set the custom tool namespace to the
        '''   default for VB (My.Resources).
        ''' </param>
        ''' <remarks></remarks>
        Public Sub New(ByVal rootDesigner As BaseRootDesigner, ByVal serviceProvider As IServiceProvider, ByVal projectItem As EnvDTE.ProjectItem, ByVal namespaceToOverrideIfCustomToolIsEmpty As String)
            If rootDesigner Is Nothing Then
                Throw New ArgumentNullException("rootDesigner")
            End If
            If projectItem Is Nothing Then
                Throw New ArgumentNullException("projectItem")
            End If
            If serviceProvider Is Nothing Then
                Throw New ArgumentNullException("serviceProvider")
            End If

            m_rootDesigner = rootDesigner
            m_projectItem = projectItem
            m_serviceProvider = serviceProvider
            m_namespaceToOverrideIfCustomToolIsEmpty = namespaceToOverrideIfCustomToolIsEmpty
        End Sub

        ''' <summary>
        ''' Adds the given code generator entry, using a language-dependent version of the accessibility as the display name
        ''' </summary>
        ''' <param name="accessibility"></param>
        ''' <param name="customToolValue"></param>
        ''' <remarks></remarks>
        Public Sub AddCodeGeneratorEntry(ByVal accessibility As AccessModifierConverter.Access, ByVal customToolValue As String)
            Debug.Assert(System.Enum.IsDefined(GetType(AccessModifierConverter.Access), accessibility))

            Dim entry As New CodeGeneratorWithDelayedName(accessibility, m_serviceProvider, customToolValue)
            m_codeGeneratorEntries.Add(entry)
            AddRecognizedCustomToolValue(entry.CustomToolValue)
        End Sub

        ''' <summary>
        ''' Add a mapping entry for a custom tool generator that we will show in the dropdown of available
        '''   choices
        ''' </summary>
        ''' <param name="displayName"></param>
        ''' <param name="customToolValue"></param>
        ''' <remarks></remarks>
        Public Sub AddCodeGeneratorEntry(ByVal displayName As String, ByVal customToolValue As String)
            Dim entry As New CodeGeneratorWithName(displayName, customToolValue)
            m_codeGeneratorEntries.Add(entry)
            AddRecognizedCustomToolValue(entry.CustomToolValue)
        End Sub

        ''' <summary>
        ''' Add an entry for a custom tool generator that we recognize.  Adding it here does *not* mean
        '''   it will show up in the dropdown of available values.  Rather, it simply means that if this
        '''   value is found in the custom tool value, we won't disable the accessibility combobox.
        ''' It is okay to make multiple calls with the same value.  In fact, any generator added through
        '''   AddCodeGeneratorEntry will automatically be added here, too.
        ''' </summary>
        ''' <param name="customToolValue"></param>
        ''' <remarks></remarks>
        Public Sub AddRecognizedCustomToolValue(ByVal customToolValue As String)
            If Not m_recognizedCustomToolValues.Contains(customToolValue) Then
                m_recognizedCustomToolValues.Add(customToolValue)
                ' We also make sure to reset the cached value for if the custom tool(s)
                ' is/are registered...
                m_customToolsRegistered = New Nullable(Of Boolean)
            End If
        End Sub

        Protected ReadOnly Property RootDesigner() As BaseRootDesigner
            Get
                Return m_rootDesigner
            End Get
        End Property

        Protected Function GetMenuCommandsToRegister(ByVal commandIdCombobox As CommandID, ByVal commandIdGetDropdownValues As CommandID) As ICollection
            ' For a dynamic combobox, we need to add two commands, one to handle the combobox, and one to fill
            ' it with items...
            Dim MenuCommands As New List(Of MenuCommand)
            m_designerCommandBarComboBoxCommand = New DesignerCommandBarComboBox(m_rootDesigner, commandIdCombobox, AddressOf GetCurrentValue, AddressOf SetCurrentValue, AddressOf EnabledHandler)
            m_commandIdCombobox = commandIdCombobox
            MenuCommands.Add(m_designerCommandBarComboBoxCommand)
            MenuCommands.Add(New DesignerCommandBarComboBoxFiller(m_rootDesigner, commandIdGetDropdownValues, AddressOf GetDropdownValues))

            RegisterMenuCommandForwarder()

            Return MenuCommands
        End Function

        ''' <summary>
        ''' Tries to retrieve the value of the "Custom Tool" property.  If there is no such
        '''   property in this project, returns False.
        ''' </summary>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function TryGetCustomToolPropertyValue(ByRef value As String) As Boolean
            value = Nothing

            Dim ToolProperty As EnvDTE.Property = DTEUtils.GetProjectItemProperty(m_projectItem, DTEUtils.PROJECTPROPERTY_CUSTOMTOOL)
            If ToolProperty IsNot Nothing Then
                Dim CurrentCustomToolValue As String = TryCast(ToolProperty.Value, String)
                value = CurrentCustomToolValue
                Return True
            End If

            Return False
        End Function

        ''' <summary>
        ''' Return the current accessibility value
        ''' </summary>
        Private Function GetCurrentValue() As String
            Dim currentValue As String
            Dim matchingEntry As CodeGenerator = GetCurrentMatchingGenerator()
            If matchingEntry IsNot Nothing Then
                currentValue = matchingEntry.DisplayName
            Else
                currentValue = SR.GetString(SR.RSE_AccessModifier_Custom)
            End If

            Switches.TracePDAccessModifierCombobox(TraceLevel.Verbose, "GetCurrentValue: " & Me.GetType.Name & ": " & currentValue)
            Return currentValue
        End Function

        ''' <summary>
        ''' Searches the current custom tool value for a matching generator entry.
        ''' </summary>
        Private Function GetCurrentMatchingGenerator() As CodeGenerator
            Dim customToolValue As String = Nothing
            If TryGetCustomToolPropertyValue(customToolValue) Then
                For Each entry As CodeGenerator In m_codeGeneratorEntries
                    If entry.CustomToolValue.Equals(customToolValue, StringComparison.OrdinalIgnoreCase) Then
                        Return entry
                    End If
                Next
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Set the current accessibility value
        ''' </summary>
        ''' <param name="value"></param>
        Private Sub SetCurrentValue(ByVal value As String)
            Switches.TracePDAccessModifierCombobox(TraceLevel.Verbose, "SetCurrentValue: " & Me.GetType.Name & ": " & value)

            For Each entry As CodeGenerator In m_codeGeneratorEntries
                If entry.DisplayName.Equals(value, StringComparison.CurrentCultureIgnoreCase) Then
                    TrySetCustomToolValue(entry.CustomToolValue)
                    Return
                End If
            Next

            'Couldn't find the expected entry.  Do nothing.
        End Sub

        ''' <summary>
        ''' Try to set the Custom Tool property to the given value.  Show an error dialog if
        '''   there's an error.
        ''' </summary>
        ''' <param name="value"></param>
        ''' <remarks></remarks>
        Private Sub TrySetCustomToolValue(ByVal value As String)
            Try
                Dim ToolProperty As EnvDTE.Property = DTEUtils.GetProjectItemProperty(m_projectItem, DTEUtils.PROJECTPROPERTY_CUSTOMTOOL)
                Dim ToolNamespaceProperty As EnvDTE.Property = DTEUtils.GetProjectItemProperty(m_projectItem, DTEUtils.PROJECTPROPERTY_CUSTOMTOOLNAMESPACE)

                If ToolProperty IsNot Nothing Then
                    Dim previousToolValue As String = TryCast(ToolProperty.Value, String)
                    If ToolNamespaceProperty IsNot Nothing Then
                        Dim previousToolNamespaceValue As String = Nothing
                        previousToolNamespaceValue = TryCast(ToolProperty.Value, String)
                    End If

                    ToolProperty.Value = value

                    If ToolNamespaceProperty IsNot Nothing _
                    AndAlso m_namespaceToOverrideIfCustomToolIsEmpty IsNot Nothing _
                    AndAlso previousToolValue = "" Then
                        ' This is currently used for the VB scenario - if the custom tool has been yet been set, and
                        '   the user turns on code generation, we want to also set the custom tool namespace to the
                        '   default for VB (My.Resources).
                        ToolNamespaceProperty.Value = m_namespaceToOverrideIfCustomToolIsEmpty
                    End If

                    m_rootDesigner.RefreshMenuStatus()
                Else
                    Debug.Fail("Couldn't find CustomTool property.  Dropdown shouldn't have been enabled.")
                End If
            Catch ex As Exception
                DesignerFramework.DesignerMessageBox.Show( _
                    m_rootDesigner, _
                    SR.GetString(SR.RSE_Task_CantChangeCustomToolOrNamespace), _
                    ex, _
                    Nothing) 'Note: when we integrate the changes to DesignerMessageBox.Show, the caption property can be removed)
            End Try
        End Sub

        ''' <summary>
        ''' Gets the set of entries for the AccessModifier dropdown on the toolbar
        ''' </summary>
        Friend Function GetDropdownValues() As String()
            Dim Values As New List(Of String)

            For Each entry As CodeGenerator In m_codeGeneratorEntries
                Values.Add(entry.DisplayName)
            Next

            Return Values.ToArray()
        End Function

        Protected MustOverride Function IsDesignerEditable() As Boolean

        Private Function EnabledHandler(ByVal MenuCommand As DesignerMenuCommand) As Boolean
            Try
                Dim shouldBeEnabled As Boolean = Me.ShouldBeEnabled()
                Switches.TracePDAccessModifierCombobox(TraceLevel.Verbose, "EnabledHandler: " & Me.GetType.Name & ": Enabled=" & shouldBeEnabled)
            Catch e As Exception
                Debug.Fail("Failed to determine if the access modifier combobox should be enabled: " & e.ToString())
                Throw
            End Try
            Return shouldBeEnabled
        End Function

        ''' <summary>
        ''' Is the AccessModifier combobox on the settings designer toolbar enabled?
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overridable Function ShouldBeEnabled() As Boolean
            If Not IsDesignerEditable() Then
                Return False
            End If

            ' If the custom tool(s) aren't registered, we don't enable the combobox...
            If Not CustomToolRegistered Then
                Return False
            End If

            Dim customToolValue As String = Nothing
            If Not TryGetCustomToolPropertyValue(customToolValue) Then
                'This project has no Custom Tool property, so don't enable the dropdown.
                Return False
            End If


            'If the current custom tool is set to a (non-empty) single file generator that we don't
            '  recognize, then disable the combobox.  Otherwise the user might accidentally change
            '  it and won't easily be able to get back the original value.  This is an advanced 
            '  scenario, and the advanced user can change this value directly in the property sheet
            '  if really needed.
            If customToolValue <> "" AndAlso Not m_recognizedCustomToolValues.Contains(customToolValue) Then
                Return False
            End If

            'Otherwise, we can enable it.
            Return True
        End Function

        ''' <summary>
        ''' Demand check if the custom tools that we know about are registered for the current project system.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overridable ReadOnly Property CustomToolRegistered() As Boolean
            Get
                If Not m_customToolsRegistered.HasValue Then
                    ' If one or more of the custom tools in the drop-down are not registered for the current
                    '  project type, we disable the combobox...
                    For Each generator As CodeGenerator In m_codeGeneratorEntries
                        If Not ShellUtil.IsCustomToolRegistered(Hierarchy, generator.CustomToolValue) Then
                            m_customToolsRegistered = False
                            Return m_customToolsRegistered.Value
                        End If
                    Next

                    m_customToolsRegistered = True
                End If

                Return m_customToolsRegistered.Value
            End Get
        End Property

        ''' <summary>
        ''' Get the hierarchy from the associated project item
        ''' </summary>
        Protected Overridable ReadOnly Property Hierarchy() As IVsHierarchy
            Get
                Return Common.ShellUtil.VsHierarchyFromDTEProject(m_serviceprovider, m_projectItem.ContainingProject)
            End Get
        End Property


#Region "IDisposable"

        Protected Overridable Sub Dispose(ByVal disposing As Boolean)
            If Not m_isDisposed Then
                If disposing Then
                    UnregisterMenuCommandForwarder()
                End If
            End If

            m_isDisposed = True
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

#End Region

#Region "Menu command forwarding to the package.  See comments in DesignerMenuCommandForwarder"

        Friend Sub OnDesignerWindowActivated(ByVal activated As Boolean)
            If activated Then
                RegisterMenuCommandForwarder()
                'Note: we don't unregister it until we are Disposed.  This allow us
                '  to keep supplying the current text value of the combobox until
                '  another like editor gets activated or until our editor is closed.
            End If
        End Sub

        Protected Overridable Sub RegisterMenuCommandForwarder()
            DesignerMenuCommandForwarder.RegisterMenuCommandForwarder(m_commandIdCombobox, m_designerCommandBarComboBoxCommand)
        End Sub

        Protected Overridable Sub UnregisterMenuCommandForwarder()
            DesignerMenuCommandForwarder.UnregisterMenuCommandForwarder(m_commandIdCombobox, m_designerCommandBarComboBoxCommand)
        End Sub

#End Region

    End Class

End Namespace
