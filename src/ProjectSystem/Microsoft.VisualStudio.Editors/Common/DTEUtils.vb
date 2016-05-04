' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports EnvDTE
Imports Microsoft.VisualStudio.Shell.Interop
Imports System.IO


Namespace Microsoft.VisualStudio.Editors.Common

    ''' <summary>
    ''' Utilities related to DTE projects and project items
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class DTEUtils


        'The relevant project property names
        Public Const PROJECTPROPERTY_CUSTOMTOOL As String = "CustomTool"
        Public Const PROJECTPROPERTY_CUSTOMTOOLNAMESPACE As String = "CustomToolNamespace"

        Private Const s_PROJECTPROPERTY_MSBUILD_ITEMTYPE As String = "ItemType"
        Private Const s_PROJECTPROPERTY_BUILDACTION As String = "BuildAction"

        ''' <summary>
        ''' This is a shared class - disallow instantation.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub New()
        End Sub


        ''' <summary>
        ''' Given a collection of ProjectItem ("ProjectItems"), queries it for the ProjectItem
        '''   of a given key.  If not found, returns Nothing.
        ''' </summary>
        ''' <param name="ProjectItems">The collection of ProjectItem to check</param>
        ''' <param name="Name">The key to check for.</param>
        ''' <returns>The ProjectItem for the given key, if found, else Nothing.  Throws exceptions only in unexpected cases.</returns>
        ''' <remarks></remarks>
        Public Shared Function QueryProjectItems(ByVal ProjectItems As ProjectItems, ByVal Name As String) As ProjectItem
            Return ResourceEditor.ResourcesFolderService.QueryProjectItems(ProjectItems, Name)
        End Function


        ''' <summary>
        ''' Given a DTE project, return the directory on disk where templates for that project system
        '''   are stored.
        ''' </summary>
        ''' <param name="Project">The project.</param>
        ''' <returns>The full path of the templates directory for that project.</returns>
        ''' <remarks></remarks>
        Public Shared Function GetProjectTemplateDirectory(ByVal Project As Project) As String
            Return Project.DTE.Solution.ProjectItemsTemplatePath(Project.Kind)
        End Function


        ''' <summary>
        ''' Retrieves the directory name on disk for a ProjectItems collection.
        ''' </summary>
        ''' <param name="ProjectItems">The ProjectItems collection to check.  Must refer to a physical folder on disk.</param>
        ''' <returns>The directory name of the collection on disk.</returns>
        ''' <remarks></remarks>
        Public Shared Function GetFolderNameFromProjectItems(ByVal ProjectItems As ProjectItems) As String
            Return ResourceEditor.ResourcesFolderService.GetFolderNameFromProjectItems(ProjectItems)
        End Function

        ''' <summary>
        ''' Get the current EnvDTE.Project instance for the project containing the .settings
        ''' file
        ''' </summary>
        ''' <remarks></remarks>
        Friend Shared Function EnvDTEProject(ByVal VsHierarchy As IVsHierarchy) As EnvDTE.Project
            Dim ProjectObj As Object = Nothing
            VSErrorHandler.ThrowOnFailure(VsHierarchy.GetProperty(VSITEMID.ROOT, __VSHPROPID.VSHPROPID_ExtObject, ProjectObj))
            Return CType(ProjectObj, EnvDTE.Project)
        End Function

        ''' <summary>
        ''' Get the current EnvDTE.Project instance for the project associated with the ProjectUniqueName
        ''' </summary>
        ''' <remarks></remarks>
        Friend Shared Function EnvDTEProjectFromProjectUniqueName(ByVal ProjectUniqueName As String) As EnvDTE.Project

            Dim SolutionService As IVsSolution = TryCast(VBPackage.GetGlobalService(GetType(IVsSolution)), IVsSolution)

            If SolutionService IsNot Nothing Then
                Dim Hierarchy As IVsHierarchy = Nothing

                If VSErrorHandler.Succeeded(SolutionService.GetProjectOfUniqueName(ProjectUniqueName, Hierarchy)) Then
                    Return DTEUtils.EnvDTEProject(Hierarchy)
                End If
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Get EnvDTE.ProjectItem from hierarchy and itemid
        ''' </summary>
        ''' <param name="VsHierarchy"></param>
        ''' <param name="ItemId"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function ProjectItemFromItemId(ByVal VsHierarchy As IVsHierarchy, ByVal ItemId As UInteger) As ProjectItem
            Dim ExtensibilityObject As Object = Nothing
            VSErrorHandler.ThrowOnFailure(VsHierarchy.GetProperty(CUInt(ItemId), CInt(__VSHPROPID.VSHPROPID_ExtObject), ExtensibilityObject))
            Debug.Assert(ExtensibilityObject IsNot Nothing AndAlso TypeOf ExtensibilityObject Is ProjectItem)
            Return DirectCast(ExtensibilityObject, ProjectItem)
        End Function


        ''' <summary>
        ''' Finds all files within a given ProjectItem that contain the given extension
        ''' </summary>
        ''' <param name="ProjectItems">The ProjectItems node to search through</param>
        ''' <param name="Extension">The extension to search for, including the period.  E.g. ".resx"</param>
        ''' <param name="SearchChildren">If True, the search will continue to children.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function FindAllFilesWithExtension(ByVal ProjectItems As ProjectItems, ByVal Extension As String, ByVal SearchChildren As Boolean) As List(Of ProjectItem)
            Dim ResXFiles As New List(Of ProjectItem)
            For Each Item As ProjectItem In ProjectItems
                If IO.Path.GetExtension(Item.FileNames(1)).Equals(Extension, StringComparison.OrdinalIgnoreCase) Then
                    ResXFiles.Add(Item)
                End If

                If SearchChildren AndAlso Item.ProjectItems.Count > 0 Then
                    ResXFiles.AddRange(FindAllFilesWithExtension(Item.ProjectItems, Extension, SearchChildren))
                End If
            Next

            Return ResXFiles
        End Function


        ''' <summary>
        ''' Get the file name from a project item.
        ''' </summary>
        ''' <param name="ProjectItem"></param>
        ''' <returns></returns>
        ''' <remarks>If the item contains of multiple files, the first one is returned</remarks>
        Public Shared Function FileNameFromProjectItem(ByVal ProjectItem As EnvDTE.ProjectItem) As String
            If ProjectItem Is Nothing Then
                System.Diagnostics.Debug.Fail("Can't get file name for NULL project item!")
                Throw New System.ArgumentNullException()
            End If

            If ProjectItem.FileCount <= 0 Then
                Debug.Fail("No file associated with ProjectItem (filecount <= 0)")
                Return Nothing
            End If

            ' The ProjectItem.FileNames collection is 1 based...
            Return ProjectItem.FileNames(1)
        End Function


        ''' <summary>
        ''' Retrieves the given project item's property, if it exists, else Nothing
        ''' </summary>
        ''' <param name="PropertyName">The name of the property to retrieve.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function GetProjectItemProperty(ByVal ProjectItem As ProjectItem, ByVal PropertyName As String) As [Property]
            If ProjectItem.Properties Is Nothing Then
                Return Nothing
            End If

            For Each Prop As [Property] In ProjectItem.Properties
                If Prop.Name.Equals(PropertyName, StringComparison.OrdinalIgnoreCase) Then
                    Return Prop
                End If
            Next

            Return Nothing
        End Function


        ''' <summary>
        ''' Retrieves the given project's property, if it exists, else Nothing
        ''' </summary>
        ''' <param name="PropertyName">The name of the property to retrieve.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function GetProjectProperty(ByVal Project As Project, ByVal PropertyName As String) As [Property]
            If Project.Properties Is Nothing Then
                Return Nothing
            End If

            For Each Prop As [Property] In Project.Properties
                If Prop.Name.Equals(PropertyName, StringComparison.OrdinalIgnoreCase) Then
                    Return Prop
                End If
            Next

            Return Nothing
        End Function


        ''' <summary>
        ''' Given a DTE project, returns the active IVsCfg configuration for it
        ''' </summary>
        ''' <param name="Project">The DTE project</param>
        ''' <param name="VsCfgProvider">The IVsCfgProvider2 interface instance to look up the active configuration from</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function GetActiveConfiguration(ByVal Project As Project, ByVal VsCfgProvider As IVsCfgProvider2) As IVsCfg
            Dim VsCfg As IVsCfg = Nothing
            With GetActiveDTEConfiguration(Project)
                VSErrorHandler.ThrowOnFailure(VsCfgProvider.GetCfgOfName(.ConfigurationName, .PlatformName, VsCfg))
            End With
            Return VsCfg
        End Function


        ''' <summary>
        ''' Given a DTE project, returns the active DTE configuration object for it
        ''' </summary>
        ''' <param name="Project">The DTE project</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function GetActiveDTEConfiguration(ByVal Project As Project) As EnvDTE.Configuration
            Try
                Return Project.ConfigurationManager.ActiveConfiguration
            Catch ex As ArgumentException
                'If there are no configurations defined in the project, this call can fail.  In that case, just return
                '  the first config (there should be a single Debug configuration automatically defined and available).
                Return Project.ConfigurationManager.Item(1) '1-indexed
            Catch ex As Exception
                Common.RethrowIfUnrecoverable(ex)
                Debug.Fail("Unexpected exception trying to get the active configuration")

                Return Project.ConfigurationManager.Item(1) '1-indexed
            End Try
        End Function


        ''' <summary>
        ''' Tries to set the Build Action property of the given project item to the given build action (enumation).  
        '''   If this project system doesn't have that property, this call is a NOP.
        ''' </summary>
        ''' <param name="Item">The ProjectItem on which to set the property</param>
        ''' <remarks></remarks>
        Public Shared Sub SetBuildAction(ByVal Item As ProjectItem, ByVal BuildAction As VSLangProj.prjBuildAction)
            Dim BuildActionProperty As [Property] = GetProjectItemProperty(Item, s_PROJECTPROPERTY_BUILDACTION)
            If BuildActionProperty IsNot Nothing Then
                BuildActionProperty.Value = BuildAction
            End If
        End Sub

        ''' <summary>
        ''' Tries to get the Build Action property of the given project item to the given build action (enumation).  
        '''   If this project system doesn't have that property, returns prjBuildActionNone.
        ''' </summary>
        ''' <param name="Item">The ProjectItem on which to set the property</param>
        ''' <remarks></remarks>
        Public Shared Function GetBuildAction(ByVal Item As ProjectItem) As VSLangProj.prjBuildAction
            Dim BuildActionProperty As [Property] = GetProjectItemProperty(Item, s_PROJECTPROPERTY_BUILDACTION)
            If BuildActionProperty IsNot Nothing Then
                Return CType(BuildActionProperty.Value, VSLangProj.prjBuildAction)
            End If

            Return VSLangProj.prjBuildAction.prjBuildActionNone
        End Function

        ''' <summary>
        ''' Tries to set the Build Action property of the given project item to the given build action (string).  
        '''   If this project system doesn't have that property, this call is a NOP.
        ''' </summary>
        ''' <param name="Item">The ProjectItem on which to set the property</param>
        ''' <remarks>
        ''' This version of the function uses newer functionality in Visual Studio, and is necessary for more
        '''   recent build actions, such as the WPF build actions, that weren't available in the original enumeration.
        ''' </remarks>
        Public Shared Sub SetBuildActionAsString(ByVal item As ProjectItem, ByVal buildAction As String)

            Dim BuildActionProperty As [Property] = GetProjectItemProperty(item, s_PROJECTPROPERTY_MSBUILD_ITEMTYPE)
            If BuildActionProperty IsNot Nothing Then
                BuildActionProperty.Value = buildAction
            End If
        End Sub

        ''' <summary>
        ''' Tries to get the Build Action property of the given project item to the given build action (enumation).  
        '''   If this project system doesn't have that property, returns "".
        ''' </summary>
        ''' <param name="Item">The ProjectItem on which to set the property</param>
        ''' <remarks></remarks>
        Public Shared Function GetBuildActionAsString(ByVal Item As ProjectItem) As String
            Dim BuildActionProperty As [Property] = GetProjectItemProperty(Item, s_PROJECTPROPERTY_MSBUILD_ITEMTYPE)
            If BuildActionProperty IsNot Nothing Then
                Return CType(BuildActionProperty.Value, String)
            End If

            Return String.Empty
        End Function

        ''' ;FindProjectItem
        ''' <summary>
        ''' Given ProjectItems and a file name, find and return a ProjectItem in this ProjectItems 
        ''' with the same file name. If none is found, return NULL.
        ''' </summary>
        Public Shared Function FindProjectItem(ByVal projectItems As ProjectItems, ByVal fileName As String) As ProjectItem
            For Each projectItem As ProjectItem In projectItems
                If projectItem.Kind.Equals( _
                    EnvDTE.Constants.vsProjectItemKindPhysicalFile, StringComparison.OrdinalIgnoreCase) AndAlso _
                    projectItem.FileCount > 0 Then

                    Dim itemFileName As String = Path.GetFileName(projectItem.FileNames(1))
                    If String.Compare(fileName, itemFileName, StringComparison.OrdinalIgnoreCase) = 0 Then
                        Return projectItem
                    End If
                End If
            Next

            Return Nothing
        End Function

        ''' ;ItemIdOfProjectItem
        ''' <summary>
        ''' From a hierarchy and projectitem, return the item id
        ''' </summary>
        Public Shared Function ItemIdOfProjectItem(ByVal Hierarchy As IVsHierarchy, ByVal ProjectItem As EnvDTE.ProjectItem) As UInteger
            Dim FoundItemId As UInteger
            VSErrorHandler.ThrowOnFailure(Hierarchy.ParseCanonicalName(ProjectItem.FileNames(1), FoundItemId))
            Return FoundItemId
        End Function

        Public Shared Sub ApplyTreeViewThemeStyles(ByVal handle As IntPtr)

            Dim ShellUIService As IVsUIShell5 = TryCast(VBPackage.GetGlobalService(GetType(SVsUIShell)), IVsUIShell5)

            If ShellUIService IsNot Nothing Then
                ShellUIService.ThemeWindow(handle)
            Else
                ' set window long
                Dim newStyle As Integer = Interop.NativeMethods.GetWindowLong(handle, Interop.NativeMethods.GWL_STYLE).ToInt32
                newStyle = newStyle Or Interop.NativeMethods.TVS_TRACKSELECT
                newStyle = newStyle And Not Interop.NativeMethods.TVS_HASLINES
                Interop.NativeMethods.SetWindowLong(handle, Interop.NativeMethods.GWL_STYLE, New IntPtr(newStyle))

                ' set window theme
                Interop.NativeMethods.SetWindowTheme(handle, "Explorer", Nothing)

                ' set extended style
                newStyle = Interop.NativeMethods.SendMessage(handle, Interop.NativeMethods.TVM_GETEXTENDEDSTYLE, IntPtr.Zero, IntPtr.Zero).ToInt32
                newStyle = newStyle Or Interop.NativeMethods.TVS_EX_DOUBLEBUFFER
                newStyle = newStyle Or Interop.NativeMethods.TVS_EX_FADEINOUTEXPANDOS
                Interop.NativeMethods.SendMessage(handle, Interop.NativeMethods.TVM_SETEXTENDEDSTYLE, IntPtr.Zero, New IntPtr(newStyle))
            End If
        End Sub

        Public Shared Sub ApplyListViewThemeStyles(ByVal handle As IntPtr)
            ' set window theme
            Interop.NativeMethods.SetWindowTheme(handle, "Explorer", Nothing)

            ' set extended style
            Dim newStyle As Integer = Interop.NativeMethods.SendMessage(handle, Interop.NativeMethods.LVM_GETEXTENDEDLISTVIEWSTYLE, IntPtr.Zero, IntPtr.Zero).ToInt32
            newStyle = newStyle Or Interop.NativeMethods.LVS_EX_DOUBLEBUFFER
            Interop.NativeMethods.SendMessage(handle, Interop.NativeMethods.LVM_SETEXTENDEDLISTVIEWSTYLE, IntPtr.Zero, New IntPtr(newStyle))
        End Sub

    End Class

End Namespace

