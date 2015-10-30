' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class VisualBasicSyntaxFactsServiceTests

        <Fact>
        Public Sub IsMethodLevelMember_Field()
            Assert.True(IsMethodLevelMember("
Class C
    [|Dim x As Integer|]
End Class"))
        End Sub

        <Fact>
        Public Sub IsMethodLevelMember_AutoProperty()
            Assert.True(IsMethodLevelMember("
Class C
    [|Property x As Integer|]
End Class"))
        End Sub

        <Fact>
        Public Sub IsMethodLevelMember_NormalProperty()
            Assert.True(IsMethodLevelMember("
Class C
    [|Property x As Integer
        Get
            Return 42
        End Get
        Set (value As Integer)
        End Set
    End Property|]
End Class"))
        End Sub

        <Fact>
        Public Sub IsMethodLevelMember_NotPropertyStatementInBlock()
            Assert.False(IsMethodLevelMember("
Class C
    [|Property x As Integer|]
        Get
            Return 42
        End Get
        Set (value As Integer)
        End Set
    End Property
End Class"))
        End Sub

        <Fact>
        Public Sub IsMethodLevelMember_FieldLikeEvent()
            Assert.True(IsMethodLevelMember("
Class C
    [|Event x As EventHandler|]
End Class"))
        End Sub

        <Fact>
        Public Sub IsMethodLevelMember_SimpleEvent()
            Assert.True(IsMethodLevelMember("
Class C
    [|Event E(i As Integer)|]
End Class"))
        End Sub

        <Fact>
        Public Sub IsMethodLevelMember_CustomEvent()
            Assert.True(IsMethodLevelMember("
Class C
        [|Custom Event x As EventHandler
            AddHandler(value As EventHandler)

            End AddHandler
            RemoveHandler(value As EventHandler)

            End RemoveHandler
            RaiseEvent(sender As Object, e As EventArgs)

            End RaiseEvent
        End Event|]
End Class"))
        End Sub

        <Fact>
        Public Sub IsMethodLevelMember_NotEvenStatementInBlock()
            Assert.False(IsMethodLevelMember("
Class C
        [|Custom Event x As EventHandler|]
            AddHandler(value As EventHandler)

            End AddHandler
            RemoveHandler(value As EventHandler)

            End RemoveHandler
            RaiseEvent(sender As Object, e As EventArgs)

            End RaiseEvent
        End Event
End Class"))
        End Sub

        <Fact>
        Public Sub IsMethodLevelMember_MustInheritMethod()
            Assert.True(IsMethodLevelMember("
Class C
    [|Public MustInherit Sub M()|]
End Class"))
        End Sub

        <Fact>
        Public Sub IsMethodLevelMember_Method()
            Assert.True(IsMethodLevelMember("
Class C
    [|Sub M()
    End Sub|]
End Class"))
        End Sub

        <Fact>
        Public Sub IsMethodLevelMember_NotMethodStatementInBlock()
            Assert.False(IsMethodLevelMember("
Class C
    [|Sub M()|]
    End Sub
End Class"))
        End Sub

        <Fact>
        Public Sub IsMethodLevelMember_Constructor()
            Assert.True(IsMethodLevelMember("
Class C
    [|Sub New()
    End Sub|]
End Class"))
        End Sub

        <Fact>
        Public Sub IsMethodLevelMember_NotSubNewStatementInBlock()
            Assert.False(IsMethodLevelMember("
Class C
    [|Sub New()|]
    End Sub
End Class"))
        End Sub

        <Fact>
        Public Sub IsMethodLevelMember_Operator()
            Assert.True(IsMethodLevelMember("
Class C
    [|Public Shared Operator +(left As C, right As C) As C
    End Operator|]
End Class"))
        End Sub

        <Fact>
        Public Sub IsMethodLevelMember_NotOperatorStatmentInBlock()
            Assert.False(IsMethodLevelMember("
Class C
    [|Public Shared Operator +(left As C, right As C) As C|]
    End Operator
End Class"))
        End Sub

        <Fact>
        Public Sub IsMethodLevelMember_EnumMember()
            Assert.True(IsMethodLevelMember("
Enum E
    [|X|]
End Enum"))
        End Sub


        <Fact>
        Public Sub IsMethodLevelMember_DeclareStatement()
            Assert.True(IsMethodLevelMember("
Class C
        [|Declare Sub M Lib ""l"" ()|]
End Class"))
        End Sub

        <Fact>
        Public Sub IsMethodLevelMember_NotAccessor()
            Assert.False(IsMethodLevelMember("
Class C
    Property x As Integer
        [|Get
            Return 42
        End Get|]
        Set (value As Integer)
        End Set
    End Property
End Class"))
        End Sub

        <Fact>
        Public Sub IsMethodLevelMember_NotDelegate()
            Assert.False(IsMethodLevelMember("
Class C
    [|Delegate Sub M()|]
End Class"))
        End Sub

        <Fact>
        Public Sub IsMethodLevelMember_NotLambdaHeader()
            Assert.False(IsMethodLevelMember("
Class C
    Sub M()
        Dim x As Action = [|Sub ()|]
                          End Sub
    End Sub
End Class"))
        End Sub

        Private Function IsMethodLevelMember(markup As String) As Boolean
            Dim code As String = Nothing
            Dim span As TextSpan
            MarkupTestFile.GetSpan(markup, code, span)
            Dim tree = SyntaxFactory.ParseSyntaxTree(code)
            Dim node = tree.GetRoot().FindNode(span)
            Dim service As New VisualBasicSyntaxFactsService()
            Return service.IsMethodLevelMember(node)
        End Function

    End Class
End Namespace
