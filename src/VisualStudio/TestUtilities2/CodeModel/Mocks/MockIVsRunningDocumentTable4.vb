' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports Microsoft.VisualStudio.Shell.Interop

<PartNotDiscoverable>
<Export(GetType(IVsRunningDocumentTable4))>
Public Class MockIVsRunningDocumentTable4
    Implements IVsRunningDocumentTable4, IVsRunningDocumentTable

    Public Function GetRelatedSaveTreeItems(cookie As UInteger, grfSave As UInteger, celt As UInteger, rgSaveTreeItems() As VSSAVETREEITEM) As UInteger Implements IVsRunningDocumentTable4.GetRelatedSaveTreeItems
        Throw New NotImplementedException()
    End Function

    Public Sub NotifyDocumentChangedEx(cookie As UInteger, attributes As UInteger) Implements IVsRunningDocumentTable4.NotifyDocumentChangedEx
        Throw New NotImplementedException()
    End Sub

    Public Function IsDocumentDirty(cookie As UInteger) As Boolean Implements IVsRunningDocumentTable4.IsDocumentDirty
        Throw New NotImplementedException()
    End Function

    Public Function IsDocumentReadOnly(cookie As UInteger) As Boolean Implements IVsRunningDocumentTable4.IsDocumentReadOnly
        Throw New NotImplementedException()
    End Function

    Public Sub UpdateDirtyState(cookie As UInteger) Implements IVsRunningDocumentTable4.UpdateDirtyState
        Throw New NotImplementedException()
    End Sub

    Public Sub UpdateReadOnlyState(cookie As UInteger) Implements IVsRunningDocumentTable4.UpdateReadOnlyState
        Throw New NotImplementedException()
    End Sub

    Public Function IsMonikerValid(moniker As String) As Boolean Implements IVsRunningDocumentTable4.IsMonikerValid
        Throw New NotImplementedException()
    End Function

    Public Function IsCookieValid(cookie As UInteger) As Boolean Implements IVsRunningDocumentTable4.IsCookieValid
        Throw New NotImplementedException()
    End Function

    Public Function GetDocumentCookie(moniker As String) As UInteger Implements IVsRunningDocumentTable4.GetDocumentCookie
        Throw New NotImplementedException()
    End Function

    Public Function GetDocumentFlags(cookie As UInteger) As UInteger Implements IVsRunningDocumentTable4.GetDocumentFlags
        Throw New NotImplementedException()
    End Function

    Public Function GetDocumentReadLockCount(cookie As UInteger) As UInteger Implements IVsRunningDocumentTable4.GetDocumentReadLockCount
        Throw New NotImplementedException()
    End Function

    Public Function GetDocumentEditLockCount(cookie As UInteger) As UInteger Implements IVsRunningDocumentTable4.GetDocumentEditLockCount
        Throw New NotImplementedException()
    End Function

    Public Function GetDocumentMoniker(cookie As UInteger) As String Implements IVsRunningDocumentTable4.GetDocumentMoniker
        Throw New NotImplementedException()
    End Function

    Public Sub GetDocumentHierarchyItem(cookie As UInteger, ByRef hierarchy As IVsHierarchy, ByRef itemID As UInteger) Implements IVsRunningDocumentTable4.GetDocumentHierarchyItem
        Throw New NotImplementedException()
    End Sub

    Public Function GetDocumentData(cookie As UInteger) As Object Implements IVsRunningDocumentTable4.GetDocumentData
        Throw New NotImplementedException()
    End Function

    Public Function GetDocumentProjectGuid(cookie As UInteger) As Guid Implements IVsRunningDocumentTable4.GetDocumentProjectGuid
        Throw New NotImplementedException()
    End Function

    Private Function IVsRunningDocumentTable3_GetRelatedSaveTreeItems(cookie As UInteger, grfSave As UInteger, celt As UInteger, rgSaveTreeItems() As VSSAVETREEITEM) As UInteger Implements IVsRunningDocumentTable3.GetRelatedSaveTreeItems
        Throw New NotImplementedException()
    End Function

    Private Sub IVsRunningDocumentTable3_NotifyDocumentChangedEx(cookie As UInteger, attributes As UInteger) Implements IVsRunningDocumentTable3.NotifyDocumentChangedEx
        Throw New NotImplementedException()
    End Sub

    Private Function IVsRunningDocumentTable3_IsDocumentDirty(cookie As UInteger) As Boolean Implements IVsRunningDocumentTable3.IsDocumentDirty
        Throw New NotImplementedException()
    End Function

    Private Function IVsRunningDocumentTable3_IsDocumentReadOnly(cookie As UInteger) As Boolean Implements IVsRunningDocumentTable3.IsDocumentReadOnly
        Throw New NotImplementedException()
    End Function

    Private Sub IVsRunningDocumentTable3_UpdateDirtyState(cookie As UInteger) Implements IVsRunningDocumentTable3.UpdateDirtyState
        Throw New NotImplementedException()
    End Sub

    Private Sub IVsRunningDocumentTable3_UpdateReadOnlyState(cookie As UInteger) Implements IVsRunningDocumentTable3.UpdateReadOnlyState
        Throw New NotImplementedException()
    End Sub

    Public Function RegisterAndLockDocument(grfRDTLockType As UInteger, pszMkDocument As String, pHier As IVsHierarchy, itemid As UInteger, punkDocData As IntPtr, ByRef pdwCookie As UInteger) As Integer Implements IVsRunningDocumentTable.RegisterAndLockDocument
        Throw New NotImplementedException()
    End Function

    Public Function LockDocument(grfRDTLockType As UInteger, dwCookie As UInteger) As Integer Implements IVsRunningDocumentTable.LockDocument
        Throw New NotImplementedException()
    End Function

    Public Function UnlockDocument(grfRDTLockType As UInteger, dwCookie As UInteger) As Integer Implements IVsRunningDocumentTable.UnlockDocument
        Throw New NotImplementedException()
    End Function

    Public Function FindAndLockDocument(dwRDTLockType As UInteger, pszMkDocument As String, ByRef ppHier As IVsHierarchy, ByRef pitemid As UInteger, ByRef ppunkDocData As IntPtr, ByRef pdwCookie As UInteger) As Integer Implements IVsRunningDocumentTable.FindAndLockDocument
        Throw New NotImplementedException()
    End Function

    Public Function RenameDocument(pszMkDocumentOld As String, pszMkDocumentNew As String, pHier As IntPtr, itemidNew As UInteger) As Integer Implements IVsRunningDocumentTable.RenameDocument
        Throw New NotImplementedException()
    End Function

    Public Function AdviseRunningDocTableEvents(pSink As IVsRunningDocTableEvents, ByRef pdwCookie As UInteger) As Integer Implements IVsRunningDocumentTable.AdviseRunningDocTableEvents
        Return 0
    End Function

    Public Function UnadviseRunningDocTableEvents(dwCookie As UInteger) As Integer Implements IVsRunningDocumentTable.UnadviseRunningDocTableEvents
        Return 0
    End Function

    Public Function GetDocumentInfo(docCookie As UInteger, ByRef pgrfRDTFlags As UInteger, ByRef pdwReadLocks As UInteger, ByRef pdwEditLocks As UInteger, ByRef pbstrMkDocument As String, ByRef ppHier As IVsHierarchy, ByRef pitemid As UInteger, ByRef ppunkDocData As IntPtr) As Integer Implements IVsRunningDocumentTable.GetDocumentInfo
        Throw New NotImplementedException()
    End Function

    Public Function NotifyDocumentChanged(dwCookie As UInteger, grfDocChanged As UInteger) As Integer Implements IVsRunningDocumentTable.NotifyDocumentChanged
        Throw New NotImplementedException()
    End Function

    Public Function NotifyOnAfterSave(dwCookie As UInteger) As Integer Implements IVsRunningDocumentTable.NotifyOnAfterSave
        Throw New NotImplementedException()
    End Function

    Public Function GetRunningDocumentsEnum(ByRef ppenum As IEnumRunningDocuments) As Integer Implements IVsRunningDocumentTable.GetRunningDocumentsEnum
        Throw New NotImplementedException()
    End Function

    Public Function SaveDocuments(grfSaveOpts As UInteger, pHier As IVsHierarchy, itemid As UInteger, docCookie As UInteger) As Integer Implements IVsRunningDocumentTable.SaveDocuments
        Throw New NotImplementedException()
    End Function

    Public Function NotifyOnBeforeSave(dwCookie As UInteger) As Integer Implements IVsRunningDocumentTable.NotifyOnBeforeSave
        Throw New NotImplementedException()
    End Function

    Public Function RegisterDocumentLockHolder(grfRDLH As UInteger, dwCookie As UInteger, pLockHolder As IVsDocumentLockHolder, ByRef pdwLHCookie As UInteger) As Integer Implements IVsRunningDocumentTable.RegisterDocumentLockHolder
        Throw New NotImplementedException()
    End Function

    Public Function UnregisterDocumentLockHolder(dwLHCookie As UInteger) As Integer Implements IVsRunningDocumentTable.UnregisterDocumentLockHolder
        Throw New NotImplementedException()
    End Function

    Public Function ModifyDocumentFlags(docCookie As UInteger, grfFlags As UInteger, fSet As Integer) As Integer Implements IVsRunningDocumentTable.ModifyDocumentFlags
        Throw New NotImplementedException()
    End Function
End Class
