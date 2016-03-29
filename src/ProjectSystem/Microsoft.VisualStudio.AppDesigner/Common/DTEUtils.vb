'******************************************************************************
'* DTEUtils.vb
'*
'* Copyright (C) 1999-2003 Microsoft Corporation. All Rights Reserved.
'* Information Contained Herein Is Proprietary and Confidential.
'******************************************************************************

Imports EnvDTE
Imports Microsoft.VisualStudio.Shell.Interop
Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Drawing
Imports System.IO
Imports System.Threading


Namespace Microsoft.VisualStudio.Editors.AppDesCommon

    ''' <summary>
    ''' Utilities related to DTE projects and project items
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class DTEUtils


        'The relevant project property names

        Private Const PROJECTPROPERTY_MSBUILD_ITEMTYPE As String = "ItemType"
        Private Const PROJECTPROPERTY_BUILDACTION As String = "BuildAction"

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
            Try
                Return ProjectItems.Item(Name)
            Catch ex As ArgumentException
                'This is the expected exception if the key could not be found.
            Catch ex As OutOfMemoryException
                Throw
            Catch ex As ThreadAbortException
                Throw
            Catch ex As StackOverflowException
                Throw
            Catch ex As Exception
                'Any other error - shouldn't be the case, but it might depend on the project implementation
                Debug.Fail("Unexpected exception searching for an item in ProjectItems: " & ex.Message)
            End Try

            Return Nothing
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
                AppDesCommon.RethrowIfUnrecoverable(ex)
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
            Dim BuildActionProperty As [Property] = GetProjectItemProperty(Item, PROJECTPROPERTY_BUILDACTION)
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
            Dim BuildActionProperty As [Property] = GetProjectItemProperty(Item, PROJECTPROPERTY_BUILDACTION)
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

            Dim BuildActionProperty As [Property] = GetProjectItemProperty(item, PROJECTPROPERTY_MSBUILD_ITEMTYPE)
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
            Dim BuildActionProperty As [Property] = GetProjectItemProperty(Item, PROJECTPROPERTY_MSBUILD_ITEMTYPE)
            If BuildActionProperty IsNot Nothing Then
                Return CType(BuildActionProperty.Value, String)
            End If

            Return String.Empty
        End Function

    End Class

End Namespace

