' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ObjectBrowser.Mocks
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ObjectBrowser
    Friend Module Helpers

        Public Class TestState
            Implements IDisposable

            Private ReadOnly _workspace As TestWorkspace
            Private ReadOnly _libraryManager As AbstractObjectBrowserLibraryManager

            Sub New(workspace As TestWorkspace, libraryManager As AbstractObjectBrowserLibraryManager)
                _workspace = workspace
                _libraryManager = libraryManager
            End Sub

            Public Sub Dispose() Implements IDisposable.Dispose
                _libraryManager.Dispose()
                _workspace.Dispose()
            End Sub

            Public Function GetLibrary() As IVsSimpleLibrary2
                Dim vsLibraryManager = TryCast(_libraryManager, IVsLibraryMgr)

                Dim count As UInteger
                IsOK(Function() vsLibraryManager.GetCount(count))
                Assert.Equal(1UI, count)

                Dim vsLibrary As IVsLibrary = Nothing
                IsOK(Function() vsLibraryManager.GetLibraryAt(0, vsLibrary))
                Assert.NotNull(vsLibrary)

                Dim vsSimpleLibrary = TryCast(vsLibrary, IVsSimpleLibrary2)
                Assert.NotNull(vsSimpleLibrary)

                Return vsSimpleLibrary
            End Function
        End Class

        Friend Sub IsOK(comAction As Func(Of Integer))
            Assert.Equal(VSConstants.S_OK, comAction())
        End Sub

        <Extension>
        Friend Sub VerifyContents(
            list As IVsSimpleObjectList2,
            itemPredicate As Func(Of IVsSimpleObjectList2, UInteger, Boolean),
            ParamArray verificationActions As Action(Of IVsSimpleObjectList2, UInteger)()
        )
            Dim count As UInteger
            IsOK(Function() list.GetItemCount(count))

            Dim itemIndices = New List(Of UInteger)
            For i = 0UI To CUInt(count - 1)
                If itemPredicate(list, i) Then
                    itemIndices.Add(i)
                End If
            Next

            Assert.Equal(verificationActions.Length, itemIndices.Count)

            For i = 0 To verificationActions.Length - 1
                Dim index = itemIndices(i)
                verificationActions(i)(list, index)
            Next
        End Sub

        <Extension>
        Friend Sub VerifyEmpty(list As IVsSimpleObjectList2)
            Dim count As UInteger
            IsOK(Function() list.GetItemCount(count))
            Assert.Equal(CUInt(0), count)
        End Sub

        <Extension>
        Friend Sub VerifyNames(
            list As IVsSimpleObjectList2,
            itemPredicate As Func(Of IVsSimpleObjectList2, UInteger, Boolean),
            ParamArray names As String()
        )
            Dim verificationActions = New Action(Of IVsSimpleObjectList2, UInteger)(names.Length - 1) {}

            For i = 0 To names.Length - 1
                Dim name = names(i)
                verificationActions(i) = Sub(l, index)
                                             Dim text As String = Nothing
                                             IsOK(Function() l.GetTextWithOwnership(index, VSTREETEXTOPTIONS.TTO_DEFAULT, text))
                                             Assert.Equal(name, text)
                                         End Sub
            Next

            VerifyContents(list, itemPredicate, verificationActions)
        End Sub

        <Extension>
        Friend Sub VerifyNames(list As IVsSimpleObjectList2, ParamArray names As String())
            VerifyNames(list, Function(x, y) True, names)
        End Sub

        <Extension>
        Friend Sub VerifyHelpKeywords(list As IVsSimpleObjectList2, ParamArray helpKeywords As String())
            Dim verificationActions = New Action(Of IVsSimpleObjectList2, UInteger)(helpKeywords.Length - 1) {}

            For i = 0 To helpKeywords.Length - 1
                Dim helpKeyword = helpKeywords(i)
                verificationActions(i) = Sub(l, index)
                                             Dim pvar As Object = Nothing
                                             IsOK(Function() l.GetProperty(index, _VSOBJLISTELEMPROPID.VSOBJLISTELEMPROPID_HELPKEYWORD, pvar))
                                             Assert.Equal(helpKeyword, pvar)
                                         End Sub
            Next

            VerifyContents(list, AddressOf IsImmediateMember, verificationActions)
        End Sub

        <Extension>
        Friend Sub VerifyDescriptions(
            list As IVsSimpleObjectList2,
            itemPredicate As Func(Of IVsSimpleObjectList2, UInteger, Boolean),
            ParamArray descriptions As String()
        )
            Dim verificationActions = New Action(Of IVsSimpleObjectList2, UInteger)(descriptions.Length - 1) {}

            For i = 0 To descriptions.Length - 1
                Dim description = descriptions(i)
                verificationActions(i) = Sub(l, index)
                                             Dim mockDescription = New MockObjectBrowserDescription()
                                             IsOK(Function() list.FillDescription2(index, 0, mockDescription))
                                             Assert.Equal(description.Trim(), mockDescription.ToString().Trim())
                                         End Sub
            Next

            VerifyContents(list, itemPredicate, verificationActions)
        End Sub

        <Extension>
        Friend Sub VerifyDescriptions(list As IVsSimpleObjectList2, ParamArray descriptions As String())
            VerifyDescriptions(list, Function(x, y) True, descriptions)
        End Sub

        Friend Function IsImmediateMember(list As IVsSimpleObjectList2, index As UInteger) As Boolean
            Dim categoryField As UInteger
            IsOK(Function() list.GetCategoryField2(index, _LIB_CATEGORY2.LC_MEMBERINHERITANCE, categoryField))

            Return categoryField = _LIBCAT_MEMBERINHERITANCE.LCMI_IMMEDIATE
        End Function

        <Extension>
        Friend Sub VerifyImmediateMemberDescriptions(list As IVsSimpleObjectList2, ParamArray descriptions As String())
            VerifyDescriptions(list, AddressOf IsImmediateMember, descriptions)
        End Sub

        <Extension>
        Friend Sub VerifyCanonicalNodes(list As IVsSimpleObjectList2, index As UInteger, ParamArray nodeDescriptors() As NavInfoNodeDescriptor)
            Dim navInfo As IVsNavInfo = Nothing
            IsOK(Function() list.GetNavInfo(index, navInfo))

            Dim enumNavInfoNodes As IVsEnumNavInfoNodes = Nothing
            IsOK(Function() navInfo.EnumCanonicalNodes(enumNavInfoNodes))

            For Each nodeDescriptor In nodeDescriptors
                Dim node = New IVsNavInfoNode(1) {}
                Dim fetched As UInteger
                IsOK(Function() enumNavInfoNodes.Next(1, node, fetched))

                Assert.Equal(CUInt(1), fetched)
                Assert.NotNull(node(0))

                Dim name As String = Nothing
                IsOK(Function() node(0).get_Name(name))

                Assert.Equal(nodeDescriptor.Name, name)

                Dim type As UInteger
                IsOK(Function() node(0).get_Type(type))

                Assert.Equal(ListType(nodeDescriptor.Kind), type)
            Next
        End Sub

        Private Function ListType(kind As ObjectListKind) As UInteger
            Return Implementation.Library.ObjectBrowser.Helpers.ObjectListKindToListType(kind)
        End Function

        Private ReadOnly Property ClassViewFlags As UInteger
            Get
                Return CUInt(Implementation.Library.ObjectBrowser.Helpers.ClassView)
            End Get
        End Property

        Private Function GetList(simpleLibrary As IVsSimpleLibrary2, kind As ObjectListKind) As IVsSimpleObjectList2
            Dim list As IVsSimpleObjectList2 = Nothing
            Dim searchCriteria As VSOBSEARCHCRITERIA2() = Nothing
            IsOK(Function() simpleLibrary.GetList2(ListType(kind), ClassViewFlags, searchCriteria, list))
            Assert.NotNull(list)

            Return list
        End Function

        Private Function GetList(simpleObjectList As IVsSimpleObjectList2, index As Integer, kind As ObjectListKind) As IVsSimpleObjectList2
            Dim list As IVsSimpleObjectList2 = Nothing
            Dim searchCriteria As VSOBSEARCHCRITERIA2() = Nothing
            IsOK(Function() simpleObjectList.GetList2(CUInt(index), ListType(kind), ClassViewFlags, searchCriteria, list))
            Assert.NotNull(list)

            Return list
        End Function

        <Extension>
        Friend Function GetProjectList(simpleLibrary As IVsSimpleLibrary2) As IVsSimpleObjectList2
            Return GetList(simpleLibrary, ObjectListKind.Projects)
        End Function

        <Extension>
        Friend Function GetProjectList(simpleObjectList As IVsSimpleObjectList2, index As Integer) As IVsSimpleObjectList2
            Return GetList(simpleObjectList, index, ObjectListKind.Projects)
        End Function

        <Extension>
        Friend Function GetNamespaceList(simpleLibrary As IVsSimpleLibrary2) As IVsSimpleObjectList2
            Return GetList(simpleLibrary, ObjectListKind.Namespaces)
        End Function

        <Extension>
        Friend Function GetNamespaceList(simpleObjectList As IVsSimpleObjectList2, index As Integer) As IVsSimpleObjectList2
            Return GetList(simpleObjectList, index, ObjectListKind.Namespaces)
        End Function

        <Extension>
        Friend Function GetTypeList(simpleLibrary As IVsSimpleLibrary2) As IVsSimpleObjectList2
            Return GetList(simpleLibrary, ObjectListKind.Types)
        End Function

        <Extension>
        Friend Function GetTypeList(simpleObjectList As IVsSimpleObjectList2, index As Integer) As IVsSimpleObjectList2
            Return GetList(simpleObjectList, index, ObjectListKind.Types)
        End Function

        <Extension>
        Friend Function GetMemberList(simpleLibrary As IVsSimpleLibrary2) As IVsSimpleObjectList2
            Return GetList(simpleLibrary, ObjectListKind.Members)
        End Function

        <Extension>
        Friend Function GetMemberList(simpleObjectList As IVsSimpleObjectList2, index As Integer) As IVsSimpleObjectList2
            Return GetList(simpleObjectList, index, ObjectListKind.Members)
        End Function

        <Extension>
        Friend Function GetReferenceList(simpleLibrary As IVsSimpleLibrary2) As IVsSimpleObjectList2
            Return GetList(simpleLibrary, ObjectListKind.References)
        End Function

        <Extension>
        Friend Function GetReferenceList(simpleObjectList As IVsSimpleObjectList2, index As Integer) As IVsSimpleObjectList2
            Return GetList(simpleObjectList, index, ObjectListKind.References)
        End Function

    End Module
End Namespace
