' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeFixVerifier(
    Of Microsoft.CodeAnalysis.VisualBasic.RemoveUnusedMembers.VisualBasicRemoveUnusedMembersDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.VisualBasic.RemoveUnusedMembers.VisualBasicRemoveUnusedMembersCodeFixProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.RemoveUnusedMembers
    Public Class RemoveUnusedMembersTests

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Sub TestStandardProperties()
            VerifyVB.VerifyStandardProperties()
        End Sub

        <Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        <InlineData("Public")>
        <InlineData("Friend")>
        <InlineData("Protected")>
        <InlineData("Protected Friend")>
        Public Async Function NonPrivateField(accessibility As String) As Task
            Dim code =
$"Class C
    {accessibility} _goo As Integer
End Class"

            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        <InlineData("Public")>
        <InlineData("Friend")>
        <InlineData("Protected")>
        <InlineData("Protected Friend")>
        Public Async Function NonPrivateFieldWithConstantInitializer(accessibility As String) As Task
            Dim code =
$"Class C
    {accessibility} _goo As Integer = 0
End Class"

            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        <InlineData("Public")>
        <InlineData("Friend")>
        <InlineData("Protected")>
        <InlineData("Protected Friend")>
        Public Async Function NonPrivateFieldWithNonConstantInitializer(accessibility As String) As Task
            Dim code =
$"Class C
    {accessibility} _goo As Integer = _goo2
    Private Shared ReadOnly _goo2 As Integer = 0
End Class"

            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        <InlineData("Public")>
        <InlineData("Friend")>
        <InlineData("Protected")>
        <InlineData("Protected Friend")>
        Public Async Function NonPrivateMethod(accessibility As String) As Task
            Dim code =
$"Class C
    {accessibility} Sub M
    End Sub
End Class"

            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        <InlineData("Public")>
        <InlineData("Friend")>
        <InlineData("Protected")>
        <InlineData("Protected Friend")>
        Public Async Function NonPrivateProperty(accessibility As String) As Task
            Dim code =
$"Class C
    {accessibility} Property P As Integer
End Class"

            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        <InlineData("Public")>
        <InlineData("Friend")>
        <InlineData("Protected")>
        <InlineData("Protected Friend")>
        Public Async Function NonPrivateIndexer(accessibility As String) As Task
            Dim code =
$"Class C
    {accessibility} Property P(i As Integer) As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class"

            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        <InlineData("Public")>
        <InlineData("Friend")>
        <InlineData("Protected")>
        <InlineData("Protected Friend")>
        Public Async Function NonPrivateEvent(accessibility As String) As Task
            Dim code =
$"Imports System
Class C
    {accessibility} Event E As EventHandler
End Class"

            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldIsUnused() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Class C
    Private [|_goo|] As Integer
End Class",
"Class C
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function MethodIsUnused() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Class C
    Private Sub [|M|]()
    End Sub
End Class",
"Class C
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function GenericMethodIsUnused() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Class C
    Private Sub [|M|](Of T)()
    End Sub
End Class",
"Class C
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function MethodInGenericTypeIsUnused() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Class C(Of T)
    Private Sub [|M|]()
    End Sub
End Class",
"Class C(Of T)
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function InstanceConstructorIsUnused_NoArguments() As Task
            ' We only flag constructors with arguments.
            Dim code =
"Class C
    Private Sub New()
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function InstanceConstructorIsUnused_WithArguments() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Class C
    Private Sub [|New|](i As Integer)
    End Sub
End Class",
"Class C
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function StaticConstructorIsNotFlagged() As Task
            Dim code =
"Class C
    Shared Sub New()
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function PropertyIsUnused() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Class C
    Private Property [|P|] As Integer
End Class",
"Class C
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function IndexerIsUnused() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Class C
    Private Property [|P|](i As Integer) As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class",
"Class C
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function EventIsUnused() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Class C
    Private Event [|E|] As System.EventHandler
End Class",
"Class C
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldIsUnused_ReadOnly() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Class C
    Private ReadOnly [|_goo|] As Integer
End Class",
"Class C
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function PropertyIsUnused_ReadOnly() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Class C
    Private ReadOnly Property [|P|] As Integer
End Class",
"Class C
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function EventIsUnused_ReadOnly() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Class C
    Private Event [|E|] As System.EventHandler
End Class",
"Class C
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldIsUnused_Shared() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Class C
    Private Shared [|_goo|] As Integer
End Class",
"Class C
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function MethodIsUnused_Shared() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Class C
    Private Shared Sub [|M|]()
    End Sub
End Class",
"Class C
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function PropertyIsUnused_Shared() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Class C
    Private Shared Property [|P|] As Integer
End Class",
"Class C
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function IndexerIsUnused_Shared() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Class C
    Private Shared Property [|P|](i As Integer) As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class",
"Class C
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function EventIsUnused_Shared() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Class C
    Private Shared Event [|E|] As System.EventHandler
End Class",
"Class C
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function EventIsUnused_Custom() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Imports System

Class C
    Private Custom Event [|E|] As EventHandler
        AddHandler(value As EventHandler)
        End AddHandler
        RemoveHandler(value As EventHandler)
        End RemoveHandler
        RaiseEvent(sender As Object, e As EventArgs)
        End RaiseEvent
    End Event
End Class",
"Imports System

Class C
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldIsUnused_Const() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Class C
    Private Const [|_goo|] As Integer = 0
End Class",
"Class C
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldIsRead() As Task
            Dim code =
"Class C
    Dim _goo As Integer
    Public Function M() As Integer
        Return _goo
    End Function
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldIsRead_Lambda() As Task
            Dim code =
"Imports System
Class C
    Dim _goo As Integer
    Public Function M() As Integer
        Dim getGoo As Func(Of Integer) = Function() _goo
    End Function
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldIsRead_Accessor() As Task
            Dim code =
"Class C
    Dim _goo As Integer
    Public ReadOnly Property P As Integer
        Get
            Return _goo
        End Get
    End Property
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldIsRead_DifferentInstance() As Task
            Dim code =
"Class C
    Dim _goo As Integer
    Public Function M() As Integer
        Return New C()._goo
    End Function
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldIsRead_ObjectInitializer() As Task
            Dim code =
"Class C
    Dim _goo As Integer
    Public Function M() As C2
        Return New C2() With {.F = _goo}
    End Function
End Class

Class C2
    Public F As Integer
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldIsRead_ObjectInitializer_02() As Task
            Dim code =
"Class C
    Dim _goo As Integer
    Dim {|IDE0052:_goo2|} As Integer
    Public Function M() As C
        Return New C() With {._goo = 0, ._goo2 = ._goo}
    End Function
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldIsRead_MeInstance() As Task
            Dim code =
"Class C
    Dim _goo As Integer
    Public Function M() As Integer
        Return Me._goo
    End Function
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldIsRead_Attribute() As Task
            Dim code =
"Class C
    Const _goo As String = """"

    <System.Obsolete(_goo)>
    Public Sub M()
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function MethodIsInvoked() As Task
            Dim code =
"Class C
    Private Sub M()
    End Sub

    Public Sub M2()
        M()
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function MethodIsAddressTaken() As Task
            Dim code =
"Class C
    Private Sub M()
    End Sub

    Public Sub M2()
        Dim x As System.Action = AddressOf M
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function GenericMethodIsInvoked_ExplicitTypeArguments() As Task
            Dim code =
"Class C
    Private Sub M1(Of T)()
    End Sub

    Public Sub M2()
        M1(Of Integer)()
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function GenericMethodIsInvoked_ImplicitTypeArguments() As Task
            Dim code =
"Class C
    Private Sub M1(Of T)(t1 As T)
    End Sub

    Public Sub M2()
        M1(0)
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function MethodInGenericTypeIsInvoked_NoTypeArguments() As Task
            Dim code =
"Class C(Of T)
    Private Sub M1()
    End Sub

    Public Sub M2()
        M1()
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function MethodInGenericTypeIsInvoked_NonConstructedType() As Task
            Dim code =
"Class C(Of T)
    Private Sub M1()
    End Sub

    Public Sub M2(m As C(Of T))
        m.M1()
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function MethodInGenericTypeIsInvoked_ConstructedType() As Task
            Dim code =
"Class C(Of T)
    Private Sub M1()
    End Sub

    Public Sub M2(m As C(Of Integer))
        m.M1()
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function InstanceConstructorIsUsed_NoArguments() As Task
            Dim code =
"Class C
    Private Sub New()
    End Sub

    Public Shared ReadOnly Instance As C = New C()
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function InstanceConstructorIsUsed_NoArguments_AsNew() As Task
            Dim code =
"Class C
    Private Sub New()
    End Sub

    Public Shared ReadOnly Instance As New C()
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function InstanceConstructorIsUsed_WithArguments() As Task
            Dim code =
"Class C
    Private Sub New(i As Integer)
    End Sub

    Public Shared ReadOnly Instance As C = New C(0)
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function InstanceConstructorIsUsed_WithArguments_AsNew() As Task
            Dim code =
"Class C
    Private Sub New(i As Integer)
    End Sub

    Public Shared ReadOnly Instance As New C(0)
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function PropertyIsRead() As Task
            Dim code =
"Class C
    Private ReadOnly Property P As Integer
    Public Function M() As Integer
        Return P
    End Function
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function IndexerIsRead() As Task
            Dim code =
"Class C
    Private Shared Property P(i As Integer) As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property

    Public Function M(x As Integer) As Integer
        Return P(x)
    End Function
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function EventIsRead() As Task
            Dim code =
"Class C
    Private Event E As System.EventHandler

    Public Function M() As System.EventHandler
        Return {|BC32022:E|}
    End Function
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function EventIsSubscribed() As Task
            Dim code =
"Class C
    Private Event E As System.EventHandler

    Public Function M(e2 As System.EventHandler) As System.EventHandler
        AddHandler E, e2
    End Function
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function EventIsRaised() As Task
            Dim code =
"Imports System

Class C
    Private Event _eventHandler As System.EventHandler

    Public Sub RaiseAnEvent(args As EventArgs)
        RaiseEvent _eventHandler(Me, args)
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        <WorkItem(32488, "https://github.com/dotnet/roslyn/issues/32488")>
        Public Async Function FieldInNameOf() As Task
            Dim code =
"Class C
    Private _goo As Integer
    Public _goo2 As String = NameOf(_goo)
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        <WorkItem(31581, "https://github.com/dotnet/roslyn/issues/31581")>
        Public Async Function MethodInNameOf() As Task
            Dim code =
"Class C
    Private Sub M()
    End Sub
    Public _goo2 As String = NameOf(M)
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        <WorkItem(31581, "https://github.com/dotnet/roslyn/issues/31581")>
        Public Async Function PropertyInNameOf() As Task
            Dim code =
"Class C
    Private ReadOnly Property P As Integer
    Public _goo2 As String = NameOf(P)
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldInDocComment() As Task
            Dim code =
"
''' <summary>
''' <see cref=""C._goo""/>
''' </summary>
Class C
    Private Shared {|IDE0052:_goo|} As Integer
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldInDocComment_02() As Task
            Dim code =
"
Class C
    ''' <summary>
    ''' <see cref=""_goo""/>
    ''' </summary>
    Private Shared {|IDE0052:_goo|} As Integer
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldInDocComment_03() As Task
            Dim code =
"
Class C
    ''' <summary>
    ''' <see cref=""_goo""/>
    ''' </summary>
    Public Sub M()
    End Sub

    Private Shared {|IDE0052:_goo|} As Integer
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldIsOnlyWritten() As Task
            Dim code =
"Class C
    Private {|IDE0052:_goo|} As Integer
    Public Sub M()
        _goo = 0
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function PropertyIsOnlyWritten() As Task
            Dim code =
"Class C
    Private Property {|IDE0052:P|} As Integer
    Public Sub M()
        P = 0
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function IndexerIsOnlyWritten() As Task
            Dim code =
"Class C
    Private Property {|IDE0052:P|}(x As Integer) As Integer
        Get
            Return 0
        End Get
        Set
        End Set
    End Property
    Public Sub M(x As Integer)
        P(x) = 0
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function EventIsOnlyWritten() As Task
            Dim code =
"Imports System

Class C
    Private Custom Event {|IDE0052:E|} As EventHandler
        AddHandler(value As EventHandler)
        End AddHandler
        RemoveHandler(value As EventHandler)
        End RemoveHandler
        RaiseEvent(sender As Object, e As EventArgs)
        End RaiseEvent
    End Event
    Public Sub M()
        ' BC32022: 'Private Event E As EventHandler' is an event, and cannot be called directly. Use a 'RaiseEvent' statement to raise an event.
        {|BC32022:E|} = Nothing
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldIsOnlyInitialized_NonConstant() As Task
            Dim code =
"Class C
    Private {|IDE0052:_goo|} As Integer = M()
    Public Shared Function M() As Integer
        Return 0
    End Function
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldIsOnlyInitialized_NonConstant_02() As Task
            Dim code =
"Class C
    Private {|IDE0052:_goo|} = 0 ' Implicit conversion to Object type in the initializer, hence it is a non constant initializer.
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldIsOnlyWritten_ObjectInitializer() As Task
            Dim code =
"Class C
    Private {|IDE0052:_goo|} As Integer
    Public Sub M()
        Dim x = New C() With { ._goo = 0 }
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldIsOnlyWritten_InProperty() As Task
            Dim code =
"Class C
    Private {|IDE0052:_goo|} As Integer
    Public Property P As Integer
        Get 
            Return 0
        End Get
        Set
            _goo = value
        End Set
    End Property
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldIsReadAndWritten() As Task
            Dim code =
"Class C
    Dim _goo As Integer
    Public Sub M()
        _goo = 0
        System.Console.WriteLine(_goo)
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function PropertyIsReadAndWritten() As Task
            Dim code =
"Class C
    Private Property P As Integer
    Public Sub M()
        P = 0
        System.Console.WriteLine(P)
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function IndexerIsReadAndWritten() As Task
            Dim code =
"Class C
    Private Property P(i As Integer) As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property

    Public Function M(x As Integer) As Integer
        P(x) = 0
        System.Console.WriteLine(P(x))
    End Function
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldIsTargetOfCompoundAssignment() As Task
            Dim code =
"Class C
    Dim {|IDE0052:_goo|} As Integer
    Public Sub M()
        _goo += 1
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function PropertyIsTargetOfCompoundAssignment() As Task
            Dim code =
"Class C
    Private Property {|IDE0052:P|} As Integer
    Public Sub M()
        P += 1
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function IndexerIsTargetOfCompoundAssignment() As Task
            Dim code =
"Class C
    Private Property {|IDE0052:P|}(i As Integer) As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property

    Public Sub M(x As Integer)
        P(x) += 1
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldIsArg() As Task
            Dim code =
"Class C
    Dim _goo As Integer
    Public Sub M1()
        M2(_goo)
    End Sub
    Public Sub M2(x As Integer)
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldIsByRefArg() As Task
            Dim code =
"Class C
    Dim _goo As Integer
    Public Sub M1()
        M2(_goo)
    End Sub
    Public Sub M2(ByRef x As Integer)
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function MethodIsArg() As Task
            Dim code =
"Class C
    Private Sub M()
    End Sub

    Public Sub M1()
        M2(AddressOf M)
    End Sub
    Public Sub M2(x As System.Action)
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        <WorkItem(30895, "https://github.com/dotnet/roslyn/issues/30895")>
        Public Async Function MethodWithHandlesClause() As Task
            Dim code =
"Public Interface I
    Event M()
End Interface

Public Class C
    Private WithEvents _field1 As I

    Private Sub M() Handles _field1.M
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        <WorkItem(30895, "https://github.com/dotnet/roslyn/issues/30895")>
        Public Async Function FieldReferencedInHandlesClause() As Task
            Dim code =
"Public Interface I
    Event M()
End Interface

Public Class C
    Private WithEvents _field1 As I

    Private Sub M() Handles _field1.M
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        <WorkItem(30895, "https://github.com/dotnet/roslyn/issues/30895")>
        Public Async Function FieldReferencedInHandlesClause_02() As Task
            Dim code =
"Public Interface I
    Event M()
End Interface

Public Class C
    Private WithEvents _field1 As I
    Private WithEvents _field2 As I

    Private Sub M() Handles _field1.M, _field2.M
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        <WorkItem(30895, "https://github.com/dotnet/roslyn/issues/30895")>
        Public Async Function EventReferencedInHandlesClause() As Task
            Dim code =
"Public Class B
    Private Event M()

    Public Class C
        Private WithEvents _field1 As B

        Private Sub M() Handles _field1.M
        End Sub
    End Class
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function PropertyIsArg() As Task
            Dim code =
"Class C
    Private ReadOnly Property P As Integer
    Public Sub M1()
        M2(P)
    End Sub
    Public Sub M2(x As Integer)
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function IndexerIsArg() As Task
            Dim code =
"Class C
    Private Property P(i As Integer) As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property

    Public Sub M1(x As Integer)
        M2(P(x))
    End Sub
    Public Sub M2(x As Integer)
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function EventIsArg() As Task
            Dim code =
"Class C
    Private Event _goo As System.EventHandler
    Public Sub M1()
        M2({|BC32022:_goo|})
    End Sub
    Public Sub M2(x As System.EventHandler)
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function MultipleFields_AllUnused() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Class C
    Private [|_goo|], [|_goo2|] As Integer, [|_goo3|], [|_goo4|] As String
End Class",
"Class C
End Class")
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        <CombinatorialData>
        Public Async Function MultipleFields_AllUnused_FixOne(
            <CombinatorialValues("[|_goo|]")> firstField As String,
            <CombinatorialValues("[|_bar|]")> secondField As String,
            <CombinatorialValues(0, 1)> diagnosticIndex As Integer) As Task

            Dim code =
$"Class C
    Private {firstField}, {secondField} As Integer
End Class"

            Dim fixedCode =
$"Class C
    Private {If(diagnosticIndex = 0, secondField, firstField)} As Integer
End Class"

            Dim batchFixedCode =
"Class C
End Class"

            Await VerifyVB.VerifyFixOneAndFixBatchAsync(code, fixedCode, batchFixedCode,
                diagnosticSelector:=Function(diagnostics) diagnostics(diagnosticIndex))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function MultipleFields_AllUnused_02() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Class C
    Private [|_goo|], [|_goo2|] As Integer, [|_goo3|] As Integer = 0, [|_goo4|] As String
End Class",
"Class C
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function MultipleFields_SomeUnused() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Class C
    Private [|_goo|] As Integer = 0, _goo2 As Integer = 0
    Public Function M() As Integer
        Return _goo2
    End Function
End Class",
"Class C
    Private _goo2 As Integer = 0
    Public Function M() As Integer
        Return _goo2
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function MultipleFields_SomeUnused_02() As Task
            Dim code =
"Class C
    Private _goo = 0, {|IDE0052:_goo2|} = 0
    Public Function M() As Integer
        Return _goo
    End Function
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldIsRead_InNestedType() As Task
            Dim code =
"Class C
    Private _goo As Integer
    Private Class Nested
        Public Function M(c As C) As Integer
            Return c._goo
        End Function
    End Class
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function MethodIsInvoked_InNestedType() As Task
            Dim code =
"Class C
    Private Sub M1()
    End Sub

    Private Class Nested
        Public Sub M2(c As C)
            c.M1()
        End Sub
    End Class
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldOfNestedTypeIsUnused() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Class C
    Private Class Nested
        Private [|_goo|] As Integer
    End Class
End Class",
"Class C
    Private Class Nested
    End Class
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldOfNestedTypeIsRead() As Task
            Dim code =
"Class C
    Private Class Nested
        Private _goo As Integer
        Public Function M() As Integer
            Return _goo
        End Function
    End Class
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldIsUnused_PartialClass() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Partial Class C
    Private [|_goo|] As Integer
End Class",
"Partial Class C
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldIsRead_PartialClass() As Task
            Dim code =
"Partial Class C
    Private _goo As Integer
End Class

Partial Class C
    Public Function M() As Integer
        Return _goo
    End Function
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldIsRead_PartialClass_DifferentFile() As Task
            Dim source1 =
"Partial Class C
    Private _goo As Integer
End Class"
            Dim source2 =
"Partial Class C
    Public Function M() As Integer
        Return _goo
    End Function
End Class"

            Await VerifyVB.VerifyCodeFixAsync(sources:=(source1, source2), fixedSources:=(source1, source2), numberOfFixAllIterations:=0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldIsOnlyWritten_PartialClass_DifferentFile() As Task
            Dim source1 =
"Partial Class C
    Private {|IDE0052:_goo|} As Integer
End Class"
            Dim source2 =
"Partial Class C
    Public Sub M()
        _goo = 0
    End Sub
End Class"

            Await VerifyVB.VerifyCodeFixAsync(sources:=(source1, source2), fixedSources:=(source1, source2), numberOfFixAllIterations:=0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldIsRead_InParens() As Task
            Dim code =
"Class C
    Private _goo As Integer
    Public Function M() As Integer
        Return (_goo)
    End Function
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldIsWritten_InParens() As Task
            Dim code =
"Class C
    Private _goo As Integer
    Public Sub M()
        ' Below is a syntax error, _goo is parsed as skipped trivia
        {|BC30035:(|}_goo) = 0
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldIsUnusedInType_SyntaxError() As Task
            Dim code =
"Class C
    Private _goo As Integer
    Public Sub M()
        {|BC30647:Return {|BC30201:|}={|BC30201:|}|}
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldIsUnusedInType_SemanticError() As Task
            Dim code =
"Class C
    Private _goo As Integer
    Public Sub M()
        ' _goo2 is undefined
        {|BC30647:Return {|BC30451:_goo2|}|}
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldIsUnusedInType_SemanticErrorInDifferentType() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Class C
    Private [|_goo|] As Integer
End Class

Class C2
    Public Sub M()
        ' _goo2 is undefined
        {|BC30647:Return {|BC30451:_goo2|}|}
    End Sub
End Class",
"Class C
End Class

Class C2
    Public Sub M()
        ' _goo2 is undefined
        {|BC30647:Return {|BC30451:_goo2|}|}
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldInTypeWithGeneratedCode() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Class C
    Private [|i|] As Integer

    <System.CodeDom.Compiler.GeneratedCodeAttribute("""", """")>
    Private j As Integer

    Public Sub M()
    End Sub
End Class",
"Class C
    <System.CodeDom.Compiler.GeneratedCodeAttribute("""", """")>
    Private j As Integer

    Public Sub M()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldIsGeneratedCode() As Task
            Dim code =
"Class C
    <System.CodeDom.Compiler.GeneratedCodeAttribute("""", """")>
    Private i As Integer

    Public Sub M()
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldUsedInGeneratedCode() As Task
            Dim code =
"Class C
    Private i As Integer

    <System.CodeDom.Compiler.GeneratedCodeAttribute("""", """")>
    Public Function M() As Integer
        Return i
    End Function
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FixAllFields_Document() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Class C
    Private [|_goo|], [|_goo2|] As Integer, [|_goo3|] As Integer = 0, _goo4, [|_goo5|] As Char
    Private [|_goo6|], [|_goo7|] As Integer, [|_goo8|] As Integer = 0
    Private {|IDE0052:_goo9|}, {|IDE0052:_goo10|} As New String("""") ' Non constant initializer
    Private {|IDE0052:_goo11|} = 0  ' Implicit conversion to Object type in the initializer, hence it is a non constant initializer.

    Public Sub M()
        Dim x = _goo4
    End Sub
End Class",
"Class C
    Private _goo4 As Char
    Private {|IDE0052:_goo9|}, {|IDE0052:_goo10|} As New String("""") ' Non constant initializer
    Private {|IDE0052:_goo11|} = 0  ' Implicit conversion to Object type in the initializer, hence it is a non constant initializer.

    Public Sub M()
        Dim x = _goo4
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FixAllMethods_Document() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Class C
    Private Sub [|M|]()
    End Sub

    Private Sub [|M2|]()
    End Sub

    Private Shared Sub [|M3|]()
    End Sub

    Private Class NestedClass
        Private Sub [|M4|]()
        End Sub
    End Class
End Class",
"Class C
    Private Class NestedClass
    End Class
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FixAllProperties_Document() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Class C
    Private Property [|P|] As Integer

    Private ReadOnly Property [|P2|] As Integer

    Private Property [|P3|] As Integer
        Get
            Return 0
        End Get
        Set
        End Set
    End Property

    Private Property [|P4|](x As Integer) As Integer
        Get
            Return 0
        End Get
        Set
        End Set
    End Property
End Class",
"Class C
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FixAllEvents_Document() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Imports System

Class C
    Private Event [|E1|] As EventHandler
    Private Event E2 As EventHandler
    Private Shared Event [|E3|] As EventHandler

    Private Custom Event [|E4|] As EventHandler
        AddHandler(value As EventHandler)
        End AddHandler
        RemoveHandler(value As EventHandler)
        End RemoveHandler
        RaiseEvent(sender As Object, e As EventArgs)
        End RaiseEvent
    End Event

    Public Sub M()
        Dim x1 = {|BC32022:E2|}
    End Sub
End Class",
"Imports System

Class C
    Private Event E2 As EventHandler

    Public Sub M()
        Dim x1 = {|BC32022:E2|}
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FixAllMembers_Project() As Task
            Dim source1 =
"Partial Class C
    Private [|_goo|] As Integer, _goo2 = 0, [|_goo3|] As Integer
    Private Sub [|M1|]()
    End Sub
    Private Property [|P1|] As Integer
    Private Property [|P2|](x As Integer) As Integer
        Get
            Return 0
        End Get
        Set
        End Set
    End Property
    Private Event [|E1|] As System.EventHandler
End Class

Class C2
    Private Sub [|M2|]()
    End Sub
End Class"
            Dim source2 =
"Partial Class C
    Private Sub [|M3|]()
    End Sub
    Public Function M4() As Integer
        Return _goo2
    End Function
End Class

Module C3
    Private Sub [|M5|]()
    End Sub
End Module"
            Dim fixedSource1 =
"Partial Class C
    Private _goo2 = 0
End Class

Class C2
End Class"
            Dim fixedSource2 =
"Partial Class C
    Public Function M4() As Integer
        Return _goo2
    End Function
End Class

Module C3
End Module"
            Await VerifyVB.VerifyCodeFixAsync(sources:=(source1, source2), fixedSources:=(fixedSource1, fixedSource2), numberOfFixAllIterations:=2)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function FieldIsUnused_Module() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Module C
    Private [|_goo|] As Integer
End Module",
"Module C
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function RedimStatement_NoPreserve() As Task
            Dim code =
"Public Class C
    Private {|IDE0052:intArray|}(10, 10, 10) As Integer

    Public Sub M()
        ReDim intArray(10, 10, 20)
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        Public Async Function RedimStatement_Preserve() As Task
            Dim code =
"Public Class C
    Private intArray(10, 10, 10) As Integer

    Public Sub M()
        ReDim Preserve intArray(10, 10, 20)
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        <WorkItem(37213, "https://github.com/dotnet/roslyn/issues/37213")>
        Public Async Function UsedPrivateExtensionMethod() As Task
            Dim code =
"Imports System.Runtime.CompilerServices

Public Module B
    <Extension()>
    Public Sub PublicExtensionMethod(s As String)
        s.PrivateExtensionMethod()
    End Sub

    <Extension()>
    Private Sub PrivateExtensionMethod(s As String)
    End Sub
End Module"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        <WorkItem(33142, "https://github.com/dotnet/roslyn/issues/33142")>
        Public Async Function XmlLiteral_NoDiagnostic() As Task
            Dim code =
"Public Class C
    Public Sub Foo()
        Dim xml = <tag><%= Me.M() %></tag>
    End Sub

    Private Function M() As Integer
        Return 42
    End Function
End Class"
            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)>
        <WorkItem(33142, "https://github.com/dotnet/roslyn/issues/33142")>
        Public Async Function Attribute_Diagnostic() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Public Class C
    <MyAttribute>
    Private Function [|M|]() As Integer
        Return 42
    End Function
End Class

Public Class MyAttribute
    Inherits System.Attribute
End Class",
"Public Class C
End Class

Public Class MyAttribute
    Inherits System.Attribute
End Class")
        End Function
    End Class
End Namespace
