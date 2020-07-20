' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.FlowAnalysis

    Public Class ImplicitVariableTests : Inherits FlowTestBase

        <Fact>
        Public Sub AnalyzeImplicitVariable()
            VerifyImplicitDeclarationDataFlowAnalysis(<![CDATA[
                [|
                Console.WriteLine(x)
                |]
            ]]>,
            dataFlowsIn:={"x"},
            definitelyAssignedOnEntry:={},
            definitelyAssignedOnExit:={},
            readInside:={"x"})
        End Sub

        <Fact>
        Public Sub AnalyzeImplicitVariableAsByRefMethodArgument()
            VerifyImplicitDeclarationDataFlowAnalysis(<![CDATA[
                [|
                System.Int32.TryParse("6", CInt(x))
                |]
            ]]>,
            dataFlowsIn:={"x"},
            definitelyAssignedOnEntry:={},
            definitelyAssignedOnExit:={},
            readInside:={"x"})
        End Sub

        <Fact>
        Public Sub AnalyzeImplicitVariableDeclarationInLambda()
            VerifyImplicitDeclarationDataFlowAnalysis(<![CDATA[
                [|
                Dim f As Func(Of Object) = Function() x
                x = 1|]
            ]]>,
            alwaysAssigned:={"f", "x"},
            captured:={"x"},
            capturedInside:={"x"},
            variablesDeclared:={"f"},
            dataFlowsIn:={"x"},
            definitelyAssignedOnEntry:={},
            definitelyAssignedOnExit:={"f", "x"},
            readInside:={"x"},
            writtenInside:={"f", "x"})
        End Sub

        <Fact>
        Public Sub AnalyzeImplicitVariableDeclarationInOuterScope1()
            VerifyImplicitDeclarationDataFlowAnalysis(<![CDATA[
                [|
                If True Then
                    x = x
                End If|]
                x = 1
            ]]>,
            alwaysAssigned:={"x"},
            dataFlowsIn:={"x"},
            definitelyAssignedOnEntry:={},
            definitelyAssignedOnExit:={"x"},
            readInside:={"x"},
            writtenInside:={"x"},
            writtenOutside:={"x"})
        End Sub

        <Fact>
        Public Sub AnalyzeImplicitVariableDeclarationInOuterScope2()
            VerifyImplicitDeclarationDataFlowAnalysis(<![CDATA[
                If True Then
                    x = x
                End If
              [|x = 1|]
            ]]>,
            alwaysAssigned:={"x"},
            definitelyAssignedOnEntry:={"x"},
            definitelyAssignedOnExit:={"x"},
            readOutside:={"x"},
            writtenInside:={"x"},
            writtenOutside:={"x"})
        End Sub

#Region "Helpers"

        Private Sub VerifyImplicitDeclarationDataFlowAnalysis(
                code As XCData,
                Optional alwaysAssigned() As String = Nothing,
                Optional captured() As String = Nothing,
                Optional dataFlowsIn() As String = Nothing,
                Optional dataFlowsOut() As String = Nothing,
                Optional definitelyAssignedOnEntry() As String = Nothing,
                Optional definitelyAssignedOnExit() As String = Nothing,
                Optional readInside() As String = Nothing,
                Optional readOutside() As String = Nothing,
                Optional variablesDeclared() As String = Nothing,
                Optional writtenInside() As String = Nothing,
                Optional writtenOutside() As String = Nothing,
                Optional capturedInside() As String = Nothing,
                Optional capturedOutside() As String = Nothing)
            VerifyDataFlowAnalysis(Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit.ImplicitVariableTests.GetSourceXElementFromTemplate(code),
                                   alwaysAssigned,
                                   captured,
                                   dataFlowsIn,
                                   dataFlowsOut,
                                   definitelyAssignedOnEntry,
                                   definitelyAssignedOnExit,
                                   readInside,
                                   readOutside,
                                   variablesDeclared,
                                   writtenInside,
                                   writtenOutside,
                                   capturedInside,
                                   capturedOutside)
        End Sub

#End Region

    End Class

End Namespace
