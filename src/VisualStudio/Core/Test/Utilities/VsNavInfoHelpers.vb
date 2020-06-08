' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Utilities.VsNavInfo
    Friend Module VsNavInfoHelpers

        Public Sub IsOK(result As Integer)
            Assert.Equal(VSConstants.S_OK, result)
        End Sub

        Public Delegate Sub NodeVerifier(vsNavInfoNode As IVsNavInfoNode)

        Private Function Node(expectedListType As _LIB_LISTTYPE, expectedName As String) As NodeVerifier
            Return Sub(vsNavInfoNode)
                       Dim listType As UInteger
                       IsOK(vsNavInfoNode.get_Type(listType))
                       Assert.Equal(CUInt(expectedListType), listType)

                       Dim actualName As String = Nothing
                       IsOK(vsNavInfoNode.get_Name(actualName))
                       Assert.Equal(expectedName, actualName)
                   End Sub
        End Function

        Public Function Package(expectedName As String) As NodeVerifier
            Return Node(_LIB_LISTTYPE.LLT_PACKAGE, expectedName)
        End Function

        Public Function [Namespace](expectedName As String) As NodeVerifier
            Return Node(_LIB_LISTTYPE.LLT_NAMESPACES, expectedName)
        End Function

        Public Function [Class](expectedName As String) As NodeVerifier
            Return Node(_LIB_LISTTYPE.LLT_CLASSES, expectedName)
        End Function

        Public Function Member(expectedName As String) As NodeVerifier
            Return Node(_LIB_LISTTYPE.LLT_MEMBERS, expectedName)
        End Function

        Public Function Hierarchy(expectedName As String) As NodeVerifier
            Return Node(_LIB_LISTTYPE.LLT_HIERARCHY, expectedName)
        End Function

        <Extension>
        Public Sub VerifyNodes(enumerator As IVsEnumNavInfoNodes, verifiers() As NodeVerifier)
            Dim index = 0
            Dim actualNode = New IVsNavInfoNode(0) {}
            Dim fetched As UInteger
            While enumerator.Next(1, actualNode, fetched) = VSConstants.S_OK
                Assert.True(index < verifiers.Length)

                Dim verifier = verifiers(index)
                index += 1

                verifier(actualNode(0))
            End While
        End Sub

    End Module
End Namespace
