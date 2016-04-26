' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.VisualStudio.Editors.DesignerFramework
Imports System.Windows.Forms
Imports System.Drawing
Imports System.Reflection
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.PlatformUI

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner

    ''' <summary>
    ''' Show a dialog allowing the user to pick a type
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class TypePickerDialog
        'Inherits System.Windows.Forms.Form
        Inherits BaseDialog

        Private Shared s_previousSize As System.Drawing.Size = System.Drawing.Size.Empty

        Private _projectItemid As UInteger
        Private _vsHierarchy As IVsHierarchy

        Public Sub New(ByVal ServiceProvider As IServiceProvider, ByVal vsHierarchy As IVsHierarchy, ByVal ItemId As UInteger)
            MyBase.New(ServiceProvider)

            _vsHierarchy = vsHierarchy
            _projectItemid = ItemId

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            If Not s_previousSize.IsEmpty Then
                Me.Size = New System.Drawing.Size(Math.Min(CInt(Me.MinimumSize.Width * 1.5), s_previousSize.Width), _
                                                  Math.Min(CInt(Me.MinimumSize.Height * 1.5), s_previousSize.Height))

            End If

            'Add any initialization after the InitializeComponent() call
            _typeTreeView = New TypeTV
            _typeTreeView.AccessibleName = SR.GetString(SR.SD_SelectATypeTreeView_AccessibleName)
            _typeTreeView.Dock = DockStyle.Fill
            AddHandler _typeTreeView.AfterSelect, AddressOf Me.TypeTreeViewAfterSelectHandler
            AddHandler _typeTreeView.BeforeExpand, AddressOf Me.TypeTreeViewBeforeExpandHandler
            TreeViewPanel.Controls.Add(_typeTreeView)
            F1Keyword = HelpIDs.Dlg_PickType
        End Sub

#Region " Windows Form Designer generated code "

        Public Sub New()
            MyBase.New()

            'This call is required by the Windows Form Designer.
            InitializeComponent()
        End Sub

        'Form overrides dispose to clean up the component list.
        Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
            If disposing Then
                If Not (_components Is Nothing) Then
                    _components.Dispose()
                End If
            End If
            MyBase.Dispose(disposing)
        End Sub
        Friend WithEvents TypeTextBox As System.Windows.Forms.TextBox
        Friend WithEvents m_CancelButton As System.Windows.Forms.Button
        Friend WithEvents m_OkButton As System.Windows.Forms.Button
        Friend WithEvents TreeViewPanel As System.Windows.Forms.Panel
        Friend WithEvents SelectedTypeLabel As System.Windows.Forms.Label
        Friend WithEvents okCancelTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents overarchingTableLayoutPanel As System.Windows.Forms.TableLayoutPanel

        'Required by the Windows Form Designer
        Private _components As System.ComponentModel.IContainer

        'NOTE: The following procedure is required by the Windows Form Designer
        'It can be modified using the Windows Form Designer.  
        'Do not modify it using the code editor.
        <System.Diagnostics.DebuggerNonUserCode()> Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(TypePickerDialog))
            Me.TypeTextBox = New System.Windows.Forms.TextBox
            Me.m_CancelButton = New System.Windows.Forms.Button
            Me.m_OkButton = New System.Windows.Forms.Button
            Me.TreeViewPanel = New System.Windows.Forms.Panel
            Me.SelectedTypeLabel = New System.Windows.Forms.Label
            Me.okCancelTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.overarchingTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.okCancelTableLayoutPanel.SuspendLayout()
            Me.overarchingTableLayoutPanel.SuspendLayout()
            Me.SuspendLayout()
            '
            'TypeTextBox
            '
            resources.ApplyResources(Me.TypeTextBox, "TypeTextBox")
            Me.TypeTextBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend
            Me.TypeTextBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource
            Me.TypeTextBox.Margin = New System.Windows.Forms.Padding(3, 3, 0, 3)
            Me.TypeTextBox.Name = "TypeTextBox"
            '
            'm_CancelButton
            '
            resources.ApplyResources(Me.m_CancelButton, "m_CancelButton")
            Me.m_CancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel
            Me.m_CancelButton.Margin = New System.Windows.Forms.Padding(3, 0, 0, 0)
            Me.m_CancelButton.Name = "m_CancelButton"
            '
            'm_OkButton
            '
            resources.ApplyResources(Me.m_OkButton, "m_OkButton")
            Me.m_OkButton.Margin = New System.Windows.Forms.Padding(0, 0, 3, 0)
            Me.m_OkButton.Name = "m_OkButton"
            '
            'TreeViewPanel
            '
            resources.ApplyResources(Me.TreeViewPanel, "TreeViewPanel")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.TreeViewPanel, 2)
            Me.TreeViewPanel.Margin = New System.Windows.Forms.Padding(0, 0, 0, 3)
            Me.TreeViewPanel.Name = "TreeViewPanel"
            '
            'SelectedTypeLabel
            '
            resources.ApplyResources(Me.SelectedTypeLabel, "SelectedTypeLabel")
            Me.SelectedTypeLabel.Margin = New System.Windows.Forms.Padding(0, 3, 3, 3)
            Me.SelectedTypeLabel.Name = "SelectedTypeLabel"
            '
            'okCancelTableLayoutPanel
            '
            resources.ApplyResources(Me.okCancelTableLayoutPanel, "okCancelTableLayoutPanel")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.okCancelTableLayoutPanel, 2)
            Me.okCancelTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
            Me.okCancelTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
            Me.okCancelTableLayoutPanel.Controls.Add(Me.m_OkButton, 0, 0)
            Me.okCancelTableLayoutPanel.Controls.Add(Me.m_CancelButton, 1, 0)
            Me.okCancelTableLayoutPanel.Margin = New System.Windows.Forms.Padding(0, 3, 0, 0)
            Me.okCancelTableLayoutPanel.Name = "okCancelTableLayoutPanel"
            Me.okCancelTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            '
            'overarchingTableLayoutPanel
            '
            resources.ApplyResources(Me.overarchingTableLayoutPanel, "overarchingTableLayoutPanel")
            Me.overarchingTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
            Me.overarchingTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            Me.overarchingTableLayoutPanel.Controls.Add(Me.TreeViewPanel, 0, 0)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.okCancelTableLayoutPanel, 0, 2)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.SelectedTypeLabel, 0, 1)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.TypeTextBox, 1, 1)
            Me.overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel"
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            '
            'TypePickerDialog
            '
            Me.AcceptButton = Me.m_OkButton
            resources.ApplyResources(Me, "$this")
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.CancelButton = Me.m_CancelButton
            Me.Controls.Add(Me.overarchingTableLayoutPanel)
            Me.HelpButton = True
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.Name = "TypePickerDialog"
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.ShowIcon = False
            Me.okCancelTableLayoutPanel.ResumeLayout(False)
            Me.okCancelTableLayoutPanel.PerformLayout()
            Me.overarchingTableLayoutPanel.ResumeLayout(False)
            Me.overarchingTableLayoutPanel.PerformLayout()
            Me.ResumeLayout(False)

        End Sub

#End Region

        Private _typeTreeView As TypeTV

        Private Sub TypeTreeViewAfterSelectHandler(ByVal sender As Object, ByVal e As TreeViewEventArgs)
            If e.Node IsNot Nothing Then
                Dim Node As TypeTVNode = TryCast(e.Node, TypeTVNode)
                If Node IsNot Nothing Then
                    If Node.IsTypeNode Then
                        TypeTextBox.Text = Node.Parent.Text + "." + Node.Text
                    End If
                End If
            End If
        End Sub

        Private Sub TypeTreeViewBeforeExpandHandler(ByVal sender As Object, ByVal e As TreeViewCancelEventArgs)
            If e.Node IsNot Nothing Then
                Dim Node As TypeTVNode = TryCast(e.Node, TypeTVNode)
                If Node IsNot Nothing AndAlso Node.IsAssemblyNode AndAlso Node.HasDummyNode Then
                    Node.RemoveDummyNode()
                    Using mtsrv As New Microsoft.VSDesigner.MultiTargetService(_vsHierarchy, _projectItemid, False)
                        If (mtsrv IsNot Nothing) Then
                            Dim typs As System.Type() = mtsrv.GetSupportedTypes(Node.Text, AddressOf GetAssemblyCallback)
                            For Each availableType As System.Type In typs
                                'TypeTextBox.AutoCompleteCustomSource.Add(availableType.FullName)
                                If availableType.FullName.Contains(".") AndAlso SettingTypeValidator.IsValidSettingType(mtsrv.GetRuntimeType(availableType)) Then
                                    _typeTreeView.AddTypeNode(Node, availableType.FullName)
                                End If
                            Next
                        End If
                    End Using
                End If
            End If
        End Sub

        Private Function GetAssemblyCallback(ByVal an As AssemblyName) As Assembly
            Dim Resolver As SettingsTypeCache = DirectCast(GetService(GetType(SettingsTypeCache)), SettingsTypeCache)
            If Resolver IsNot Nothing Then
                'Return Resolver.GetAssembly(an, False)
            End If
            Return Nothing
        End Function


        ''' <summary>
        ''' Get whatever type name the user selected
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property TypeName() As String
            Get
                Return TypeTextBox.Text.Trim()
            End Get
            Set(ByVal Value As String)
                TypeTextBox.Text = Value
            End Set
        End Property

        Public Sub SetServiceProvider(ByVal Provider As IServiceProvider)
            ServiceProvider = Provider
        End Sub

        ''' <summary>
        ''' A collection of available types
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private ReadOnly Property AvailableTypes() As AutoCompleteStringCollection
            Get
                Return TypeTextBox.AutoCompleteCustomSource
            End Get
        End Property

        Private Sub SetAvailableTypes(ByVal types As IEnumerable(Of System.Type))
            TypeTextBox.AutoCompleteCustomSource.Clear()
            If types IsNot Nothing Then
                For Each availableType As System.Type In types
                    TypeTextBox.AutoCompleteCustomSource.Add(availableType.FullName)
                    If availableType.FullName.Contains(".") Then
                        _typeTreeView.AddTypeNode(Nothing, availableType.FullName)
                    End If
                Next
            End If
        End Sub

        Public Sub SetProjectReferencedAssemblies()
            TypeTextBox.AutoCompleteCustomSource.Clear()

            Dim envDTE As EnvDTE.Project = DTEUtils.EnvDTEProject(_vsHierarchy)
            Dim VSProject As VSLangProj.VSProject = DirectCast(envDTE.Object, VSLangProj.VSProject)
            Dim References As VSLangProj.References = Nothing

            If VSProject IsNot Nothing Then
                References = VSProject.References()
            End If

            If References IsNot Nothing Then
                Using mtsrv As New Microsoft.VSDesigner.MultiTargetService(_vsHierarchy, _projectItemid, False)
                    For ReferenceNo As Integer = 1 To References.Count()
                        Dim reference As String = References.Item(ReferenceNo).Name()
                        If mtsrv Is Nothing OrElse mtsrv.IsSupportedAssembly(reference) Then
                            _typeTreeView.AddAssemblyNode(reference)
                        End If
                    Next
                End Using
            Else
                Dim Resolver As SettingsTypeCache = DirectCast(GetService(GetType(SettingsTypeCache)), SettingsTypeCache)
                If Resolver IsNot Nothing Then
                    SetAvailableTypes(Resolver.GetWellKnownTypes())
                End If
            End If
        End Sub


        ''' <summary>
        ''' Try to validate the current type name, giving the user a chance to cancel the close
        ''' if validation fails
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub m_OkButton_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles m_OkButton.Click
            If QueryClose() Then
                Me.DialogResult = System.Windows.Forms.DialogResult.OK
                Me.Hide()
            End If
        End Sub

        ''' <summary>
        ''' Validate the current type name, prompting the user if the validation fails
        ''' </summary>
        ''' <returns>
        ''' True if validation successful OR validation unsuccessful, but user chooses to save type anyway
        '''</returns>
        ''' <remarks></remarks>
        Private Function QueryClose() As Boolean
            Dim ShouldClose As Boolean

            Dim NormalizedTypeName As String
            Try
                NormalizedTypeName = NormalizeTypeName(TypeName)

                Dim Resolver As SettingsTypeCache = DirectCast(GetService(GetType(SettingsTypeCache)), SettingsTypeCache)
                Debug.Assert(Resolver IsNot Nothing, "Couldn't find a SettingsTypeCache")
                Dim resolvedType As Type = Resolver.GetSettingType(NormalizedTypeName)
                If resolvedType Is Nothing Then
                    ' This shouldn't normally happen - if we were able to figure out what the
                    ' display name is, we should be able to figure out what the type is...
                    ' We failed to resolve the type...
                    ReportError(SR.GetString(SR.SD_UnknownType, TypeName), TypeName)
                    ShouldClose = False
                ElseIf resolvedType.IsGenericType Then
                    ReportError(SR.GetString(SR.SD_ERR_GenericTypesNotSupported_1Arg, TypeName))
                    ShouldClose = False
                ElseIf resolvedType.IsAbstract Then
                    ReportError(SR.GetString(SR.SD_ERR_AbstractTypesNotSupported_1Arg, TypeName))
                    ShouldClose = False
                ElseIf Not SettingTypeValidator.IsValidSettingType(resolvedType) Then
                    ReportError(SR.GetString(SR.SD_UnknownType, TypeName), TypeName)
                    ShouldClose = False
                Else
                    ' Everything is cool'n froody!
                    TypeName = NormalizedTypeName
                    ShouldClose = True
                End If

            Catch ex As ArgumentException
                ' The type resolution may throw an argument exception if the type name was invalid...
                ' Let's report the error and keep the dialog open!
                ReportError(SR.GetString(SR.SD_ERR_InvalidTypeName_1Arg, TypeName))
                Return False
            Catch ex As System.IO.FileLoadException
                ' The type resolution may throw an argument exception if the type name contains an invalid assembly name 
                ' (i.e. Foo,,)
                ' Let's report the error and keep the dialog open!
                ReportError(SR.GetString(SR.SD_ERR_InvalidTypeName_1Arg, TypeName))
                Return False
            Catch ex As Exception
                ' We don't know what happened here - let's assume that the type name was bad...
                ' Let's report the error and keep the dialog open!
                Debug.Fail(String.Format("Unexpected {0} caught when resolving type {1}: {2}", ex.GetType().Name, TypeName, ex))
                ReportError(SR.GetString(SR.SD_ERR_InvalidTypeName_1Arg, TypeName))
                Return False
            End Try
            Return ShouldClose
        End Function

        ''' <summary>
        ''' Get the correct type name from what the user typed in the text box. The textbox accepts language specific
        ''' type names (i.e. int for System.Int32) as well as type names in imported namespaces
        ''' </summary>
        ''' <param name="displayName"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function NormalizeTypeName(ByVal displayName As String) As String
            Dim typeNameResolutionService As SettingTypeNameResolutionService = _
                DirectCast(GetService(GetType(SettingTypeNameResolutionService)), SettingTypeNameResolutionService)

            Debug.Assert(typeNameResolutionService IsNot Nothing, "The settingsdesignerloader should have added a typenameresolutioncomponent service!")
            If typeNameResolutionService IsNot Nothing Then
                displayName = typeNameResolutionService.TypeDisplayNameToPersistedSettingTypeName(displayName)
            End If

            Dim typeNameCache As System.Collections.Generic.Dictionary(Of String, Object) = Nothing
            If Not typeNameResolutionService.IsCaseSensitive Then
                typeNameCache = New System.Collections.Generic.Dictionary(Of String, Object)(StringComparison.OrdinalIgnoreCase)
                For Each typeName As String In AvailableTypes
                    typeNameCache(typeName) = Nothing
                Next
            End If

            If typeNameCache IsNot Nothing Then
                If typeNameCache.ContainsKey(displayName) Then
                    Return displayName
                End If
            Else
                If AvailableTypes.Contains(displayName) Then
                    Return displayName
                End If
            End If

            For Each import As String In GetProjectImports()
                Dim probeName As String = String.Format("{0}.{1}", import, displayName)
                If typeNameCache IsNot Nothing Then
                    If typeNameCache.ContainsKey(probeName) Then
                        Return probeName
                    End If
                Else
                    If AvailableTypes.Contains(probeName) Then
                        Return probeName
                    End If
                End If
            Next
            Return displayName
        End Function

        ''' <summary>
        ''' Get the project level imports
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetProjectImports() As List(Of String)
            Dim Result As New List(Of String)
            Dim vsProject As VSLangProj.VSProject
            vsProject = TryCast(GetService(GetType(VSLangProj.VSProject)), VSLangProj.VSProject)
            If vsProject IsNot Nothing AndAlso vsProject.Imports() IsNot Nothing Then
                For Index As Integer = 1 To vsProject.Imports.Count
                    Result.Add(vsProject.Imports.Item(Index))
                Next
            Else
                ' CONSIDER: add some default "imports" here (like "System")
            End If
            Return Result
        End Function

        Private Class TypeTV
            Inherits TreeView

            Public Sub New()
                Me.PathSeparator = "."
                Me.Sorted = True

                Dim assemblyImage As Bitmap = Utils.GetManifestBitmapTransparent("assembly.bmp", Color.FromArgb(255, 0, 255))
                Dim namespaceImage As Bitmap = Utils.GetManifestBitmapTransparent("namespace.bmp", Color.FromArgb(255, 0, 255))
                Dim objectImage As Bitmap = Utils.GetManifestBitmapTransparent("object.bmp", Color.FromArgb(255, 0, 255))

                Dim treeViewIcons As ImageList = New ImageList()
                treeViewIcons.Images.Add(assemblyImage)
                treeViewIcons.Images.Add(namespaceImage)
                treeViewIcons.Images.Add(objectImage)

                'Scale the imagelist for High DPI
                DpiHelper.LogicalToDeviceUnits(treeViewIcons)

                Me.ImageList = treeViewIcons

                Common.DTEUtils.ApplyTreeViewThemeStyles(Me.Handle)
            End Sub

            Public Sub AddAssemblyNode(ByVal assemblyName As String)
                If Not String.IsNullOrEmpty(assemblyName) AndAlso Not Me.Nodes.ContainsKey(assemblyName) Then
                    Dim asNode As TypeTVNode = New TypeTVNode(NodeType.ASSEMBLY_NODE)
                    asNode.Text = assemblyName
                    asNode.Name = assemblyName
                    Me.Nodes.Add(asNode)
                    asNode.AddDummyNode()
                End If
            End Sub

            Public Sub AddTypeNode(ByVal asNode As TypeTVNode, ByVal typeFullName As String)
                Dim nodes As TreeNodeCollection = Me.Nodes
                If asNode IsNot Nothing Then
                    nodes = asNode.Nodes
                End If
                Dim nsName As String = TypeTVNode.ExtractName(typeFullName)
                Dim typName As String = TypeTVNode.ExtractChildPath(typeFullName)
                If Not String.IsNullOrEmpty(nsName) Then
                    Dim nsNode As TypeTVNode

                    If Not nodes.ContainsKey(nsName) Then
                        nsNode = New TypeTVNode(NodeType.NAMESPACE_NODE)
                        nsNode.Text = nsName
                        nsNode.Name = nsName
                        nodes.Add(nsNode)
                    Else
                        nsNode = DirectCast(nodes(nsName), TypeTVNode)
                    End If
                    If Not String.IsNullOrEmpty(typName) AndAlso Not nsNode.Nodes.ContainsKey(typName) Then
                        Dim typNode As TypeTVNode = New TypeTVNode(NodeType.TYPE_NODE)
                        typNode.Text = typName
                        typNode.Name = typName
                        nsNode.Nodes.Add(typNode)
                    End If
                End If
            End Sub

        End Class

        Private Enum NodeType
            ASSEMBLY_NODE
            NAMESPACE_NODE
            TYPE_NODE
        End Enum


        Private Class TypeTVNode
            Inherits TreeNode

            Private _nodeType As NodeType

            Private Const s_DUMMY_ITEM_TEXT As String = " THIS IS THE DUMMY ITEM "
            Private Const s_assemblyImageIndex As Integer = 0
            Private Const s_selectedAssemblyImageIndex As Integer = 0
            Private Const s_namespaceImageIndex As Integer = 1
            Private Const s_selectedNamespaceImageIndex As Integer = 1
            Private Const s_typeImageIndex As Integer = 2
            Private Const s_selectedTypeImageIndex As Integer = 2

            Public Sub New(ByVal nodeType As NodeType)
                Me._nodeType = nodeType

                Select Case nodeType
                    Case NodeType.ASSEMBLY_NODE
                        ImageIndex = s_assemblyImageIndex
                        SelectedImageIndex = s_selectedAssemblyImageIndex
                    Case NodeType.NAMESPACE_NODE
                        ImageIndex = s_namespaceImageIndex
                        SelectedImageIndex = s_selectedNamespaceImageIndex
                    Case NodeType.TYPE_NODE
                        ImageIndex = s_typeImageIndex
                        SelectedImageIndex = s_selectedTypeImageIndex
                End Select

            End Sub

            Public ReadOnly Property IsAssemblyNode() As Boolean
                Get
                    Return _nodeType = NodeType.ASSEMBLY_NODE
                End Get
            End Property

            Public ReadOnly Property HasDummyNode() As Boolean
                Get
                    Return Me.Nodes.ContainsKey(s_DUMMY_ITEM_TEXT)
                End Get
            End Property


            Public ReadOnly Property IsNameSpaceNode() As Boolean
                Get
                    Return _nodeType = NodeType.NAMESPACE_NODE
                End Get
            End Property

            Public ReadOnly Property IsTypeNode() As Boolean
                Get
                    Return _nodeType = NodeType.TYPE_NODE
                End Get
            End Property

            Public Sub AddDummyNode()
                If IsAssemblyNode() AndAlso Me.Nodes.Count = 0 Then
                    Me.Nodes.Add(s_DUMMY_ITEM_TEXT, s_DUMMY_ITEM_TEXT)
                End If
            End Sub

            Public Sub RemoveDummyNode()
                If IsAssemblyNode() AndAlso Me.Nodes.ContainsKey(s_DUMMY_ITEM_TEXT) Then
                    Me.Nodes.RemoveByKey(s_DUMMY_ITEM_TEXT)
                End If
            End Sub

#If DRILL_DOWN_NAMESPACES Then
            Friend Shared Function ExtractName(ByVal Path As String) As String
                Dim PointPos As Integer = Path.IndexOf(".")
                If PointPos <> -1 Then
                    Return Path.Substring(0, PointPos)
                Else
                    Return Path
                End If
            End Function

            Friend Shared Function ExtractChildPath(ByVal Path As String) As String
                Dim PointPos As Integer = Path.IndexOf(".")
                If PointPos <> -1 Then
                    Return Path.Substring(PointPos + 1)
                Else
                    Return ""
                End If
            End Function
#Else
            Friend Shared Function ExtractName(ByVal Path As String) As String
                Dim PointPos As Integer = Path.LastIndexOf(".")
                If PointPos <> -1 Then
                    Return Path.Substring(0, PointPos)
                Else
                    Return Path
                End If
            End Function

            Friend Shared Function ExtractChildPath(ByVal Path As String) As String
                Dim PointPos As Integer = Path.LastIndexOf(".")
                If PointPos <> -1 Then
                    Return Path.Substring(PointPos + 1)
                Else
                    Return ""
                End If
            End Function
#End If
        End Class

        ''' <summary>
        ''' We want to preserve the size of the dialog for the next time the user selects it...
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub TypePickerDialog_FormClosed(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosedEventArgs) Handles Me.FormClosed
            s_previousSize = Me.Size
        End Sub

        Private Sub TypePickerDialog_HelpButtonClicked(ByVal sender As System.Object, ByVal e As System.ComponentModel.CancelEventArgs) Handles MyBase.HelpButtonClicked
            e.Cancel = True
            ShowHelp()
        End Sub
    End Class
End Namespace
