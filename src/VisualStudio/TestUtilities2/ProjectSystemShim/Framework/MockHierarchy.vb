' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.OLE.Interop
Imports System.Runtime.InteropServices
Imports Microsoft.VisualStudio.Shell
Imports Roslyn.Utilities
Imports System.IO
Imports Moq

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
    Public NotInheritable Class MockHierarchy
        Implements IVsHierarchy
        Implements IVsProject3
        Implements IVsAggregatableProject
        Implements IVsBuildPropertyStorage

        Public Shared ReadOnly ReferencesNodeItemId As UInteger = 123456

        Private _projectName As String
        Private _projectBinPath As String
        Private _maxSupportedLangVer As String
        Private ReadOnly _projectRefPath As String
        Private ReadOnly _projectCapabilities As String
        Private ReadOnly _projectMock As Mock(Of EnvDTE.Project) = New Mock(Of EnvDTE.Project)(MockBehavior.Strict)

        Private ReadOnly _eventSinks As New Dictionary(Of UInteger, IVsHierarchyEvents)
        Private ReadOnly _hierarchyItems As New Dictionary(Of UInteger, String)

        Public Sub New(projectName As String,
                       projectFilePath As String,
                       projectBinPath As String,
                       projectRefPath As String,
                       projectCapabilities As String)
            _projectName = projectName
            _projectBinPath = projectBinPath
            _projectRefPath = projectRefPath
            _projectCapabilities = projectCapabilities
            _hierarchyItems.Add(CType(VSConstants.VSITEMID.Root, UInteger), projectFilePath)
        End Sub

        Public Sub RenameProject(projectName As String)
            If _projectName = projectName Then
                Return
            End If

            _projectName = projectName

            For Each sink In _eventSinks.Values
                sink.OnPropertyChanged(VSConstants.VSITEMID.Root, __VSHPROPID.VSHPROPID_Name, 0)
            Next
        End Sub

        Public Function AddItem(<ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")> itemidLoc As UInteger, <ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSADDITEMOPERATION")> dwAddItemOperation As VSADDITEMOPERATION, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")> pszItemName As String, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.ULONG")> cFilesToOpen As UInteger, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")> rgpszFilesToOpen() As String, hwndDlgOwner As IntPtr, <ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSADDRESULT")> pResult() As VSADDRESULT) As Integer Implements IVsProject3.AddItem
            _hierarchyItems.Add(itemidLoc, pszItemName)
            Return VSConstants.S_OK
        End Function

        Public Function AddItemWithSpecific(<ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")> itemidLoc As UInteger, <ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSADDITEMOPERATION")> dwAddItemOperation As VSADDITEMOPERATION, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")> pszItemName As String, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.ULONG")> cFilesToOpen As UInteger, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")> rgpszFilesToOpen() As String, hwndDlgOwner As IntPtr, <ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSSPECIFICEDITORFLAGS")> grfEditorFlags As UInteger, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.REFGUID")> ByRef rguidEditorType As Guid, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")> pszPhysicalView As String, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.REFGUID")> ByRef rguidLogicalView As Guid, <ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSADDRESULT")> pResult() As VSADDRESULT) As Integer Implements IVsProject3.AddItemWithSpecific
            _hierarchyItems.Add(itemidLoc, pszItemName)
            Return VSConstants.S_OK
        End Function

        Public Function AdviseHierarchyEvents(pEventSink As IVsHierarchyEvents, <ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSCOOKIE")> ByRef pdwCookie As UInteger) As Integer Implements IVsHierarchy.AdviseHierarchyEvents
            pdwCookie = _eventSinks.Keys.Concat({0}).Max() + 1UI
            _eventSinks.Add(pdwCookie, pEventSink)
            Return VSConstants.S_OK
        End Function

        Public Function Close() As Integer Implements IVsHierarchy.Close
            _hierarchyItems.Clear()
            Return VSConstants.S_OK
        End Function

        Public Function GenerateUniqueItemName(<ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")> itemidLoc As UInteger, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")> pszExt As String, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")> pszSuggestedRoot As String, ByRef pbstrItemName As String) As Integer Implements IVsProject3.GenerateUniqueItemName
            Throw New NotImplementedException()
        End Function

        Public Function GetCanonicalName(itemid As UInteger, ByRef pbstrName As String) As Integer Implements IVsHierarchy.GetCanonicalName
            If _hierarchyItems.TryGetValue(itemid, pbstrName) Then
                Return VSConstants.S_OK
            Else
                Return VSConstants.E_FAIL
            End If
        End Function

        Public Function GetGuidProperty(itemid As UInteger, propid As Integer, ByRef pguid As Guid) As Integer Implements IVsHierarchy.GetGuidProperty
            If itemid = VSConstants.VSITEMID_ROOT And propid = CType(__VSHPROPID.VSHPROPID_ProjectIDGuid, Integer) Then
                pguid = Guid.NewGuid()

                Return VSConstants.S_OK
            End If

            Throw New NotImplementedException()
        End Function

        Public Function GetItemContext(<ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")> itemid As UInteger, ByRef ppSP As IServiceProvider) As Integer Implements IVsProject3.GetItemContext
            Throw New NotImplementedException()
        End Function

        Public Function GetMkDocument(<ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")> itemid As UInteger, ByRef pbstrMkDocument As String) As Integer Implements IVsProject3.GetMkDocument
            _hierarchyItems.TryGetValue(itemid, pbstrMkDocument)
            Return VSConstants.S_OK
        End Function

        Public Function GetNestedHierarchy(itemid As UInteger, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.REFIID")> ByRef iidHierarchyNested As Guid, ByRef ppHierarchyNested As IntPtr, ByRef pitemidNested As UInteger) As Integer Implements IVsHierarchy.GetNestedHierarchy
            Throw New NotImplementedException()
        End Function

        Public Function GetProperty(itemid As UInteger, propid As Integer, ByRef pvar As Object) As Integer Implements IVsHierarchy.GetProperty

            If propid = __VSHPROPID.VSHPROPID_ProjectName Then
                pvar = _projectName
                Return VSConstants.S_OK
            ElseIf propid = __VSHPROPID5.VSHPROPID_ProjectCapabilities Then
                pvar = _projectCapabilities
                Return VSConstants.S_OK
            ElseIf propid = __VSHPROPID7.VSHPROPID_ProjectTreeCapabilities Then
                If itemid = ReferencesNodeItemId Then
                    pvar = "References"
                    Return VSConstants.S_OK
                End If
            ElseIf propid = __VSHPROPID.VSHPROPID_ExtObject Then
                Dim projectItemMock As Mock(Of EnvDTE.ProjectItem) = New Mock(Of EnvDTE.ProjectItem)(MockBehavior.Strict)
                projectItemMock.SetupGet(Function(m) m.ContainingProject).Returns(_projectMock.Object)
                projectItemMock.SetupGet(Function(m) m.FileNames(1)).Returns(_hierarchyItems(itemid))

                pvar = projectItemMock.Object
                Return VSConstants.S_OK
            End If

            Return VSConstants.E_NOTIMPL
        End Function

        Public Function GetSite(ByRef ppSP As IServiceProvider) As Integer Implements IVsHierarchy.GetSite
            Throw New NotImplementedException()
        End Function

        Public Function IsDocumentInProject(<ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")> pszMkDocument As String, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.BOOL")> ByRef pfFound As Integer, <ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSDOCUMENTPRIORITY")> pdwPriority() As VSDOCUMENTPRIORITY, <ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")> ByRef pitemid As UInteger) As Integer Implements IVsProject3.IsDocumentInProject
            pfFound = 0
            For Each kvp In _hierarchyItems
                If kvp.Value = pszMkDocument Then
                    pfFound = 1
                    pitemid = kvp.Key
                    Exit For
                End If
            Next

            Return VSConstants.S_OK
        End Function

        Public Function OpenItem(<ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")> itemid As UInteger, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.REFGUID")> ByRef rguidLogicalView As Guid, punkDocDataExisting As IntPtr, ByRef ppWindowFrame As IVsWindowFrame) As Integer Implements IVsProject3.OpenItem
            Throw New NotImplementedException()
        End Function

        Public Function OpenItemWithSpecific(<ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")> itemid As UInteger, <ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSSPECIFICEDITORFLAGS")> grfEditorFlags As UInteger, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.REFGUID")> ByRef rguidEditorType As Guid, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")> pszPhysicalView As String, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.REFGUID")> ByRef rguidLogicalView As Guid, punkDocDataExisting As IntPtr, ByRef ppWindowFrame As IVsWindowFrame) As Integer Implements IVsProject3.OpenItemWithSpecific
            Throw New NotImplementedException()
        End Function

        Public Function ParseCanonicalName(<ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")> pszName As String, ByRef pitemid As UInteger) As Integer Implements IVsHierarchy.ParseCanonicalName
            For Each kvp In _hierarchyItems
                If kvp.Value = pszName Then
                    pitemid = kvp.Key
                    Exit For
                End If
            Next

            Return VSConstants.S_OK
        End Function

        Public Function QueryClose(<ComAliasName("Microsoft.VisualStudio.OLE.Interop.BOOL")> ByRef pfCanClose As Integer) As Integer Implements IVsHierarchy.QueryClose
            Throw New NotImplementedException()
        End Function

        Public Function RemoveItem(<ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD")> dwReserved As UInteger, <ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")> itemid As UInteger, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.BOOL")> ByRef pfResult As Integer) As Integer Implements IVsProject3.RemoveItem
            _hierarchyItems.Remove(itemid)
            Return VSConstants.S_OK
        End Function

        Public Function ReopenItem(<ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")> itemid As UInteger, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.REFGUID")> ByRef rguidEditorType As Guid, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")> pszPhysicalView As String, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.REFGUID")> ByRef rguidLogicalView As Guid, punkDocDataExisting As IntPtr, ByRef ppWindowFrame As IVsWindowFrame) As Integer Implements IVsProject3.ReopenItem
            Throw New NotImplementedException()
        End Function

        Public Function SetGuidProperty(itemid As UInteger, propid As Integer, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.REFGUID")> ByRef rguid As Guid) As Integer Implements IVsHierarchy.SetGuidProperty
            Throw New NotImplementedException()
        End Function

        Public Function SetProperty(itemid As UInteger, propid As Integer, var As Object) As Integer Implements IVsHierarchy.SetProperty
            If propid = __VSHPROPID.VSHPROPID_ProjectName Then
                _projectName = If(TryCast(var, String), _projectName)
                Return VSConstants.S_OK
            End If

            Throw New NotImplementedException()
        End Function

        Public Function SetSite(psp As IServiceProvider) As Integer Implements IVsHierarchy.SetSite
            Throw New NotImplementedException()
        End Function

        Public Function TransferItem(<ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")> pszMkDocumentOld As String, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")> pszMkDocumentNew As String, punkWindowFrame As IVsWindowFrame) As Integer Implements IVsProject3.TransferItem
            For Each kvp In _hierarchyItems
                If kvp.Value = pszMkDocumentOld Then
                    _hierarchyItems(kvp.Key) = pszMkDocumentNew
                    Exit For
                End If
            Next

            Return VSConstants.S_OK
        End Function

        Public Function UnadviseHierarchyEvents(<ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSCOOKIE")> dwCookie As UInteger) As Integer Implements IVsHierarchy.UnadviseHierarchyEvents
            If Not _eventSinks.Remove(dwCookie) Then
                Throw New InvalidOperationException("The cookie was not subscribed.")
            End If

            Return VSConstants.S_OK
        End Function

        Public Function Unused0() As Integer Implements IVsHierarchy.Unused0
            Throw New NotImplementedException()
        End Function

        Public Function Unused1() As Integer Implements IVsHierarchy.Unused1
            Throw New NotImplementedException()
        End Function

        Public Function Unused2() As Integer Implements IVsHierarchy.Unused2
            Throw New NotImplementedException()
        End Function

        Public Function Unused3() As Integer Implements IVsHierarchy.Unused3
            Throw New NotImplementedException()
        End Function

        Public Function Unused4() As Integer Implements IVsHierarchy.Unused4
            Throw New NotImplementedException()
        End Function

        Private Function IVsProject2_AddItem(<ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")> itemidLoc As UInteger, <ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSADDITEMOPERATION")> dwAddItemOperation As VSADDITEMOPERATION, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")> pszItemName As String, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.ULONG")> cFilesToOpen As UInteger, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")> rgpszFilesToOpen() As String, hwndDlgOwner As IntPtr, <ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSADDRESULT")> pResult() As VSADDRESULT) As Integer Implements IVsProject2.AddItem
            Throw New NotImplementedException()
        End Function

        Private Function IVsProject2_GenerateUniqueItemName(<ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")> itemidLoc As UInteger, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")> pszExt As String, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")> pszSuggestedRoot As String, ByRef pbstrItemName As String) As Integer Implements IVsProject2.GenerateUniqueItemName
            Throw New NotImplementedException()
        End Function

        Private Function IVsProject2_GetItemContext(<ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")> itemid As UInteger, ByRef ppSP As IServiceProvider) As Integer Implements IVsProject2.GetItemContext
            Throw New NotImplementedException()
        End Function

        Private Function IVsProject2_GetMkDocument(<ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")> itemid As UInteger, ByRef pbstrMkDocument As String) As Integer Implements IVsProject2.GetMkDocument
            Throw New NotImplementedException()
        End Function

        Private Function IVsProject2_IsDocumentInProject(<ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")> pszMkDocument As String, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.BOOL")> ByRef pfFound As Integer, <ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSDOCUMENTPRIORITY")> pdwPriority() As VSDOCUMENTPRIORITY, <ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")> ByRef pitemid As UInteger) As Integer Implements IVsProject2.IsDocumentInProject
            Throw New NotImplementedException()
        End Function

        Private Function IVsProject2_OpenItem(<ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")> itemid As UInteger, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.REFGUID")> ByRef rguidLogicalView As Guid, punkDocDataExisting As IntPtr, ByRef ppWindowFrame As IVsWindowFrame) As Integer Implements IVsProject2.OpenItem
            Throw New NotImplementedException()
        End Function

        Private Function IVsProject2_RemoveItem(<ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD")> dwReserved As UInteger, <ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")> itemid As UInteger, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.BOOL")> ByRef pfResult As Integer) As Integer Implements IVsProject2.RemoveItem
            Throw New NotImplementedException()
        End Function

        Private Function IVsProject2_ReopenItem(<ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")> itemid As UInteger, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.REFGUID")> ByRef rguidEditorType As Guid, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")> pszPhysicalView As String, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.REFGUID")> ByRef rguidLogicalView As Guid, punkDocDataExisting As IntPtr, ByRef ppWindowFrame As IVsWindowFrame) As Integer Implements IVsProject2.ReopenItem
            Throw New NotImplementedException()
        End Function

        Private Function IVsProject_AddItem(<ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")> itemidLoc As UInteger, <ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSADDITEMOPERATION")> dwAddItemOperation As VSADDITEMOPERATION, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")> pszItemName As String, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.ULONG")> cFilesToOpen As UInteger, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")> rgpszFilesToOpen() As String, hwndDlgOwner As IntPtr, <ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSADDRESULT")> pResult() As VSADDRESULT) As Integer Implements IVsProject.AddItem
            Throw New NotImplementedException()
        End Function

        Private Function IVsProject_GenerateUniqueItemName(<ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")> itemidLoc As UInteger, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")> pszExt As String, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")> pszSuggestedRoot As String, ByRef pbstrItemName As String) As Integer Implements IVsProject.GenerateUniqueItemName
            Throw New NotImplementedException()
        End Function

        Private Function IVsProject_GetItemContext(<ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")> itemid As UInteger, ByRef ppSP As IServiceProvider) As Integer Implements IVsProject.GetItemContext
            Throw New NotImplementedException()
        End Function

        Private Function IVsProject_GetMkDocument(<ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")> itemid As UInteger, ByRef pbstrMkDocument As String) As Integer Implements IVsProject.GetMkDocument
            Throw New NotImplementedException()
        End Function

        Private Function IVsProject_IsDocumentInProject(<ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")> pszMkDocument As String, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.BOOL")> ByRef pfFound As Integer, <ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSDOCUMENTPRIORITY")> pdwPriority() As VSDOCUMENTPRIORITY, <ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")> ByRef pitemid As UInteger) As Integer Implements IVsProject.IsDocumentInProject
            Throw New NotImplementedException()
        End Function

        Private Function IVsProject_OpenItem(<ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")> itemid As UInteger, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.REFGUID")> ByRef rguidLogicalView As Guid, punkDocDataExisting As IntPtr, ByRef ppWindowFrame As IVsWindowFrame) As Integer Implements IVsProject.OpenItem
            Throw New NotImplementedException()
        End Function

        Public Function SetInnerProject(punkInner As Object) As Integer Implements IVsAggregatableProject.SetInnerProject
            Throw New NotImplementedException()
        End Function

        Public Function InitializeForOuter(<ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")> pszFilename As String, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")> pszLocation As String, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")> pszName As String, <ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSCREATEPROJFLAGS")> grfCreateFlags As UInteger, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.REFIID")> ByRef iidProject As Guid, ByRef ppvProject As IntPtr, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.BOOL")> ByRef pfCanceled As Integer) As Integer Implements IVsAggregatableProject.InitializeForOuter
            Throw New NotImplementedException()
        End Function

        Public Function OnAggregationComplete() As Integer Implements IVsAggregatableProject.OnAggregationComplete
            Throw New NotImplementedException()
        End Function

        Public Function GetAggregateProjectTypeGuids(ByRef pbstrProjTypeGuids As String) As Integer Implements IVsAggregatableProject.GetAggregateProjectTypeGuids
            If _projectCapabilities = "VB" Then
                pbstrProjTypeGuids = Guids.VisualBasicProjectIdString
                Return VSConstants.S_OK
            ElseIf _projectCapabilities = "CSharp" Then
                pbstrProjTypeGuids = Guids.CSharpProjectIdString
                Return VSConstants.S_OK
            End If

            Throw New NotImplementedException()
        End Function

        Public Function SetAggregateProjectTypeGuids(<ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCOLESTR")> lpstrProjTypeGuids As String) As Integer Implements IVsAggregatableProject.SetAggregateProjectTypeGuids
            Throw New NotImplementedException()
        End Function

        Public Function GetPropertyValue(pszPropName As String, pszConfigName As String, storage As UInteger, ByRef pbstrPropValue As String) As Integer Implements IVsBuildPropertyStorage.GetPropertyValue
            If pszPropName = "OutDir" Then
                pbstrPropValue = _projectBinPath
                Return VSConstants.S_OK
            ElseIf pszPropName = "TargetFileName" Then
                pbstrPropValue = PathUtilities.ChangeExtension(_projectName, "dll")
                Return VSConstants.S_OK
            ElseIf pszPropName = "TargetRefPath" Then
                pbstrPropValue = _projectRefPath
                Return VSConstants.S_OK
            ElseIf pszPropName = "MaxSupportedLangVersion" Then
                pbstrPropValue = _maxSupportedLangVer
                Return VSConstants.S_OK
            End If

            Throw New NotSupportedException($"{NameOf(MockHierarchy)}.{NameOf(GetPropertyValue)} does not support reading {pszPropName}.")
        End Function

        Public Function SetPropertyValue(pszPropName As String, pszConfigName As String, storage As UInteger, pszPropValue As String) As Integer Implements IVsBuildPropertyStorage.SetPropertyValue
            If pszPropName = "OutDir" Then
                _projectBinPath = pszPropValue
                Return VSConstants.S_OK
            ElseIf pszPropName = "TargetFileName" Then
                _projectName = PathUtilities.GetFileName(pszPropValue, includeExtension:=False)
                Return VSConstants.S_OK
            ElseIf pszPropName = "MaxSupportedLangVersion" Then
                _maxSupportedLangVer = pszPropValue
                Return VSConstants.S_OK
            End If

            Throw New NotImplementedException()
        End Function

        Public Function RemoveProperty(pszPropName As String, pszConfigName As String, storage As UInteger) As Integer Implements IVsBuildPropertyStorage.RemoveProperty
            Throw New NotImplementedException()
        End Function

        Public Function GetItemAttribute(item As UInteger, pszAttributeName As String, ByRef pbstrAttributeValue As String) As Integer Implements IVsBuildPropertyStorage.GetItemAttribute
            Throw New NotImplementedException()
        End Function

        Public Function SetItemAttribute(item As UInteger, pszAttributeName As String, pszAttributeValue As String) As Integer Implements IVsBuildPropertyStorage.SetItemAttribute
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace
