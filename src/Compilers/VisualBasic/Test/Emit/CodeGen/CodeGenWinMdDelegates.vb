' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities
Imports System.Xml.Linq
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class CodeGenWinMdDelegates
        Inherits BasicTestBase

        ''' <summary>
        ''' When the output type is .winmdobj, delegate types shouldn't output Begin/End invoke 
        ''' members.
        ''' </summary>
        <Fact()>
        Public Sub SimpleDelegateMembersTest()
            Dim libSrc =
                <compilation>
                    <file name="c.vb">
                        <![CDATA[
Namespace Test
  Public Delegate Sub SubDelegate()
End Namespace]]>
                    </file>
                </compilation>

            Dim getValidator =
                Function(expectedMembers As String())
                    Return Sub(m As ModuleSymbol)
                               Dim actualMembers = m.GlobalNamespace.GetMember(Of NamespaceSymbol)("Test").
                                   GetMember(Of NamedTypeSymbol)("SubDelegate").GetMembers().ToArray()

                               AssertEx.SetEqual((From s In actualMembers Select s.Name), expectedMembers)
                           End Sub
                End Function

            Dim verify =
                Sub(winmd As Boolean, expected As String())
                    Dim validator = getValidator(expected)

                    ' We should see the same members from both source and metadata
                    Dim verifier = CompileAndVerify(
                        libSrc,
                        sourceSymbolValidator:=validator,
                        symbolValidator:=validator,
                        options:=If(winmd, TestOptions.ReleaseWinMD, TestOptions.ReleaseDll))
                    verifier.VerifyDiagnostics()
                End Sub

            ' Test winmd
            verify(True, New String() {
                WellKnownMemberNames.InstanceConstructorName,
                WellKnownMemberNames.DelegateInvokeName})

            ' Test normal
            verify(False, New String() {
                WellKnownMemberNames.InstanceConstructorName,
                WellKnownMemberNames.DelegateInvokeName,
                WellKnownMemberNames.DelegateBeginInvokeName,
                WellKnownMemberNames.DelegateEndInvokeName})
        End Sub

        <Fact()>
        Public Sub AnonymousDelegate()
            Dim src =
                <compilation>
                    <file name="c.vb">
                        <![CDATA[
Class C
    Public Sub S()
        Dim x = Function(y As Integer) y
    End Sub

    Public F = Function(x As Integer) x
End Class]]>
                    </file>
                </compilation>

            Dim srcValidator =
                Sub(m As ModuleSymbol)
                    Dim comp = m.DeclaringCompilation
                    Dim tree = comp.SyntaxTrees.Single()
                    Dim model = comp.GetSemanticModel(tree)
                    Dim node = tree.GetRoot().DescendantNodes().OfType(Of ModifiedIdentifierSyntax).First()
                    Dim nodeSymbol = DirectCast(model.GetDeclaredSymbol(node), LocalSymbol).Type

                    Assert.True(nodeSymbol.IsAnonymousType)
                    AssertEx.SetEqual((From member In nodeSymbol.GetMembers() Select member.Name),
                                      {WellKnownMemberNames.InstanceConstructorName,
                                       "Invoke"})
                End Sub

            Dim mdValidator =
                Sub(m As ModuleSymbol)
                    Dim members = m.GlobalNamespace
                End Sub

            Dim verifier = CompileAndVerify(
                src,
                sourceSymbolValidator:=srcValidator,
                symbolValidator:=mdValidator,
                options:=TestOptions.ReleaseWinMD)
        End Sub

        <Fact()>
        Public Sub TestAllDelegates()
            Dim winRtDelegateLibrarySrc =
                <compilation>
                    <file name="WinRTDelegateLibrary.vb"><![CDATA[
Namespace WinRTDelegateLibrary
    Public Structure S1
    End Structure  

    Public Enum E1
        alpha = 1
        bravo
        charlie
        delta
    End Enum

    Public Class C1
    End Class

    Public Interface I1
    End Interface

    ''' 
    ''' These are the interesting types
    ''' 

    Public Delegate Sub SubSubDelegate()

    Public Delegate Function intintDelegate(a As Integer) As Integer
           
    Public Delegate Function structDelegate(s As S1) As S1
           
    Public Delegate Function enumDelegate(e As E1) As E1
           
    Public Delegate Function classDelegate(c As C1) As C1
           
    Public Delegate Function stringDelegate(s As string) As string
           
    Public Delegate Function decimalDelegate(d As Decimal) As Decimal
           
    Public Delegate Function WinRTDelegate(d As SubSubDelegate) as SubSubDelegate
           
    Public Delegate Function nullableDelegate(a As Integer?) As Integer?
           
    Public Delegate Function genericDelegate(Of T)(t As T) As T
    Public Delegate Function genericDelegate2(Of T As New)(t As T) As T
    Public Delegate Function genericDelegate3(Of T As Class)(t As T) As T
    Public Delegate Function genericDelegate4(Of T As Structure)(t As T) As T
    Public Delegate Function genericDelegate5(Of T As I1)(t As T) As T
           
    Public Delegate Function arrayDelegate(arr as Integer()) As Integer()
           
    Public Delegate Function interfaceDelegate(i As I1) As I1
End Namespace
]]>
                    </file>
                </compilation>

            ' We need the 4.5 refs here
            Dim coreRefs45 = {
            MscorlibRef_v4_0_30316_17626,
            SystemCoreRef_v4_0_30319_17929}

            Dim winRtDelegateLibrary = CompilationUtils.CreateEmptyCompilationWithReferences(
                winRtDelegateLibrarySrc,
                references:=coreRefs45,
                options:=TestOptions.ReleaseWinMD).EmitToImageReference()

            Dim fileElement = winRtDelegateLibrarySrc.<file>.Single()
            fileElement.ReplaceAttributes(New XAttribute("name", "NonWinRTDelegateLibrary.vb"))
            fileElement.SetValue(fileElement.Value.Replace("WinRTDelegateLibrary", "NonWinRTDelegateLibrary"))

            Dim nonWinRtDelegateLibrary = CompilationUtils.CreateEmptyCompilationWithReferences(
                winRtDelegateLibrarySrc,
                references:=coreRefs45,
                options:=TestOptions.ReleaseDll).EmitToImageReference()

            Dim allDelegates =
                <compilation>
                    <file name="c.vb">
                        <![CDATA[
Imports WinRT = WinRTDelegateLibrary
Imports NonWinRT = NonWinRTDelegateLibrary

Class Test
    Public d001 As WinRT.SubSubDelegate
    Public d101 As NonWinRT.SubSubDelegate

    Public d002 As WinRT.intintDelegate
    Public d102 As NonWinRT.intintDelegate

    Public d003 As WinRT.structDelegate
    Public d103 As NonWinRT.structDelegate

    Public d004 As WinRT.enumDelegate
    Public d104 As NonWinRT.enumDelegate

    Public d005 As WinRT.classDelegate
    Public d105 As NonWinRT.classDelegate

    Public d006 As WinRT.stringDelegate
    Public d106 As NonWinRT.stringDelegate

    Public d007 As WinRT.decimalDelegate
    Public d107 As NonWinRT.decimalDelegate

    Public d008 As WinRT.WinRTDelegate
    Public d108 As NonWinRT.WinRTDelegate

    Public d009 As WinRT.nullableDelegate
    Public d109 As NonWinRT.nullableDelegate

    Public d010 As WinRT.genericDelegate(Of Single)
    Public d110 As NonWinRT.genericDelegate(Of Single)

    Public d011 As WinRT.genericDelegate2(Of Object)
    Public d111 As NonWinRT.genericDelegate2(Of Object)

    Public d012 As WinRT.genericDelegate3(Of WinRT.C1)
    Public d112 As NonWinRT.genericDelegate3(Of NonWinRT.C1)

    Public d013 As WinRT.genericDelegate4(Of WinRT.S1)
    Public d113 As NonWinRT.genericDelegate4(Of NonWinRT.S1)

    Public d014 As WinRT.genericDelegate5(Of WinRT.I1)
    Public d114 As NonWinRT.genericDelegate5(Of NonWinRT.I1)

    Public d015 As WinRT.arrayDelegate
    Public d115 As NonWinRT.arrayDelegate

    Public d016 As WinRT.interfaceDelegate
    Public d116 As NonWinRT.interfaceDelegate
End Class
]]>
                    </file>
                </compilation>

            Dim isWinRt = Function(field As FieldSymbol)
                              Dim fieldType = field.Type
                              If DirectCast(fieldType, Object) Is Nothing Then
                                  Return False
                              End If

                              If Not fieldType.IsDelegateType() Then
                                  Return False
                              End If

                              For Each member In fieldType.GetMembers()
                                  Select Case member.Name
                                      Case WellKnownMemberNames.DelegateBeginInvokeName
                                      Case WellKnownMemberNames.DelegateEndInvokeName
                                          Return False
                                  End Select
                              Next

                              Return True
                          End Function

            Dim validator As Action(Of ModuleSymbol) =
                Sub(m As ModuleSymbol)
                    Dim type = m.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Test")
                    Dim fields = type.GetMembers()

                    For Each field In fields
                        Dim fieldSymbol = TryCast(field, FieldSymbol)
                        If DirectCast(fieldSymbol, Object) IsNot Nothing Then
                            If fieldSymbol.Name.Contains("d1") Then
                                Assert.False(isWinRt(fieldSymbol))
                            Else
                                Assert.True(isWinRt(fieldSymbol))
                            End If
                        End If
                    Next
                End Sub

            Dim verifier = CompileAndVerify(
                allDelegates,
                references:={
                    winRtDelegateLibrary,
                    nonWinRtDelegateLibrary},
                symbolValidator:=validator)

            verifier.VerifyDiagnostics()
        End Sub
    End Class
End Namespace
