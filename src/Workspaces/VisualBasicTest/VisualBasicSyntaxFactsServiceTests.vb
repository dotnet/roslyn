' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
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

        <Fact>
        Public Sub IsQueryKeyword_From()
            Assert.True(IsQueryKeyword(WrapInMethod("
Dim result = $$From var1 In collection1")))
        End Sub

        <Fact>
        Public Sub IsQueryKeyword_In()
            Assert.True(IsQueryKeyword(WrapInMethod("
Dim result = From var1 $$In collection1")))
        End Sub

        <Fact>
        Public Sub IsQueryKeyword_Where()
            Assert.True(IsQueryKeyword(WrapInMethod("
Dim customerOrders = From cust In customers, ord In orders
                        $$Where cust.CustomerID = ord.CustomerID")))
        End Sub

        <Fact>
        Public Sub IsQueryKeyword_Select()
            Assert.True(IsQueryKeyword(WrapInMethod("
Dim customerOrders = From cust In customers, ord In orders
                     Where cust.CustomerID = ord.CustomerID
                     $$Select cust.CompanyName, ord.OrderDate")))
        End Sub

        <Fact>
        Public Sub IsQueryKeyword_Distinct()
            Assert.True(IsQueryKeyword(WrapInMethod("
Dim customerOrders = From cust In customers, ord In orders
                     Where cust.CustomerID = ord.CustomerID
                     Select cust.CompanyName, ord.OrderDate
                     $$Distinct")))
        End Sub

        <Fact>
        Public Sub IsQueryKeyword_Aggregate()
            Assert.True(IsQueryKeyword(WrapInMethod("
Dim customerMaxOrder = $$Aggregate order In orders
                       Into MaxOrder = Max(order.Total)")))
        End Sub

        <Fact>
        Public Sub IsQueryKeyword_GroupBy_Group()
            Assert.True(IsQueryKeyword(WrapInMethod("
Dim customersByCountry = From cust In customers
                         $$Group By CountryName = cust.Country
                         Into RegionalCustomers = Group, Count()")))
        End Sub

        <Fact>
        Public Sub IsQueryKeyword_GroupBy_By()
            Assert.True(IsQueryKeyword(WrapInMethod("
Dim customersByCountry = From cust In customers
                         Group $$By CountryName = cust.Country
                         Into RegionalCustomers = Group, Count()")))
        End Sub

        <Fact>
        Public Sub IsQueryKeyword_Into()
            Assert.True(IsQueryKeyword(WrapInMethod("
Dim customersByCountry = From cust In customers
                         Group By CountryName = cust.Country
                         $$Into RegionalCustomers = Group, Count()")))
        End Sub

        <Fact>
        Public Sub IsQueryKeyword_IntoAliasGroup()
            Assert.True(IsQueryKeyword(WrapInMethod("
Dim customersByCountry = From cust In customers
                         Group By CountryName = cust.Country
                         Into RegionalCustomers = $$Group, Count()")))
        End Sub

        <Fact>
        Public Sub IsQueryKeyword_GroupJoin_Group1()
            Assert.True(IsQueryKeyword(WrapInMethod("
Dim customerList = From cust In customers
                   $$Group Join ord In orders On
                   cust.CustomerID Equals ord.CustomerID
                   Into CustomerOrders = Group,
                        OrderTotal = Sum(ord.Total)
                   Select cust.CompanyName, cust.CustomerID,
                          CustomerOrders, OrderTotal")))
        End Sub

        <Fact>
        Public Sub IsQueryKeyword_GroupJoin_Join()
            Assert.True(IsQueryKeyword(WrapInMethod("
Dim customerList = From cust In customers
                   Group $$Join ord In orders On
                   cust.CustomerID Equals ord.CustomerID
                   Into CustomerOrders = Group,
                        OrderTotal = Sum(ord.Total)
                   Select cust.CompanyName, cust.CustomerID,
                          CustomerOrders, OrderTotal")))
        End Sub

        <Fact>
        Public Sub IsQueryKeyword_GroupJoin_On()
            Assert.True(IsQueryKeyword(WrapInMethod("
Dim customerList = From cust In customers
                   Group Join ord In orders $$On
                   cust.CustomerID Equals ord.CustomerID
                   Into CustomerOrders = Group,
                        OrderTotal = Sum(ord.Total)
                   Select cust.CompanyName, cust.CustomerID,
                          CustomerOrders, OrderTotal")))
        End Sub

        <Fact>
        Public Sub IsQueryKeyword_GroupJoin_Equals()
            Assert.True(IsQueryKeyword(WrapInMethod("
Dim customerList = From cust In customers
                   Group Join ord In orders On
                   cust.CustomerID $$Equals ord.CustomerID
                   Into CustomerOrders = Group,
                        OrderTotal = Sum(ord.Total)
                   Select cust.CompanyName, cust.CustomerID,
                          CustomerOrders, OrderTotal")))
        End Sub

        <Fact>
        Public Sub IsQueryKeyword_GroupJoin_Group2()
            Assert.True(IsQueryKeyword(WrapInMethod("
Dim customerList = From cust In customers
                   Group Join ord In orders On
                   cust.CustomerID Equals ord.CustomerID
                   Into CustomerOrders = $$Group,
                        OrderTotal = Sum(ord.Total)
                   Select cust.CompanyName, cust.CustomerID,
                          CustomerOrders, OrderTotal")))
        End Sub

        <Fact>
        Public Sub IsQueryKeyword_Join_Join()
            Assert.True(IsQueryKeyword(WrapInMethod("
Dim processes = From proc In Process.GetProcesses
                $$Join desc In processDescriptions
                On proc.ProcessName Equals desc.ProcessName
                Select proc.ProcessName, proc.Id, desc.Description")))
        End Sub

        <Fact>
        Public Sub IsQueryKeyword_Join_In()
            Assert.True(IsQueryKeyword(WrapInMethod("
Dim processes = From proc In Process.GetProcesses
                Join desc $$In processDescriptions
                On proc.ProcessName Equals desc.ProcessName
                Select proc.ProcessName, proc.Id, desc.Description")))
        End Sub

        <Fact>
        Public Sub IsQueryKeyword_Join_On()
            Assert.True(IsQueryKeyword(WrapInMethod("
Dim processes = From proc In Process.GetProcesses
                Join desc In processDescriptions
                $$On proc.ProcessName Equals desc.ProcessName
                Select proc.ProcessName, proc.Id, desc.Description")))
        End Sub

        <Fact>
        Public Sub IsQueryKeyword_Join_Equals()
            Assert.True(IsQueryKeyword(WrapInMethod("
Dim processes = From proc In Process.GetProcesses
                Join desc In processDescriptions
                On proc.ProcessName $$Equals desc.ProcessName
                Select proc.ProcessName, proc.Id, desc.Description")))
        End Sub

        <Fact>
        Public Sub IsQueryKeyword_Let()
            Assert.True(IsQueryKeyword(WrapInMethod("
Dim discountedProducts = From prod In products
                         $$Let Discount = prod.UnitPrice * 0.1
                         Where Discount >= 50
                         Select prod.ProductName, prod.UnitPrice, Discount")))
        End Sub

        <Fact>
        Public Sub IsQueryKeyword_OrderBy_Order()
            Assert.True(IsQueryKeyword(WrapInMethod("
Dim titlesDescendingPrice = From book In books
                            $$Order By book.Price Descending, book.Title Ascending, book.Author
                            Select book.Title, book.Price")))
        End Sub

        <Fact>
        Public Sub IsQueryKeyword_OrderBy_By()
            Assert.True(IsQueryKeyword(WrapInMethod("
Dim titlesDescendingPrice = From book In books
                            Order $$By book.Price Descending, book.Title Ascending, book.Author
                            Select book.Title, book.Price")))
        End Sub

        <Fact>
        Public Sub IsQueryKeyword_OrderBy_Descending()
            Assert.True(IsQueryKeyword(WrapInMethod("
Dim titlesDescendingPrice = From book In books
                            Order By book.Price $$Descending, book.Title Ascending, book.Author
                            Select book.Title, book.Price")))
        End Sub

        <Fact>
        Public Sub IsQueryKeyword_OrderBy_Ascending()
            Assert.True(IsQueryKeyword(WrapInMethod("
Dim titlesDescendingPrice = From book In books
                            Order By book.Price Descending, book.Title $$Ascending, book.Author
                            Select book.Title, book.Price")))
        End Sub

        <Fact>
        Public Sub IsQueryKeyword_Skip_Skip()
            Assert.True(IsQueryKeyword(WrapInMethod("
Dim returnCustomers = From cust In customers
                      $$Skip startIndex Take pageSize")))
        End Sub

        <Fact>
        Public Sub IsQueryKeyword_Skip_Take()
            Assert.True(IsQueryKeyword(WrapInMethod("
Dim returnCustomers = From cust In customers
                      Skip startIndex $$Take pageSize")))
        End Sub

        <Fact>
        Public Sub IsQueryKeyword_SkipWhile_Skip()
            Assert.True(IsQueryKeyword(WrapInMethod("
Dim customerList = From cust In customers
                   Order By cust.Country
                   $$Skip While IsInternationalCustomer(cust)")))
        End Sub

        <Fact>
        Public Sub IsQueryKeyword_SkipWhile_While()
            Assert.True(IsQueryKeyword(WrapInMethod("
Dim customerList = From cust In customers
                   Order By cust.Country
                   Skip $$While IsInternationalCustomer(cust)")))
        End Sub

        <Fact>
        Public Sub IsQueryKeyword_TakeWhile_Take()
            Assert.True(IsQueryKeyword(WrapInMethod("
Dim customersWithOrders = From cust In customers
                          Order By cust.Orders.Count Descending
                          $$Take While HasOrders(cust)")))
        End Sub

        <Fact>
        Public Sub IsQueryKeyword_Not_WhileStatement_1()
            Assert.False(IsQueryKeyword(WrapInMethod("
Dim index As Integer = 0
$$While index <= 10
    index += 1
End While")))
        End Sub

        <Fact>
        Public Sub IsQueryKeyword_Not_WhileStatement_2()
            Assert.False(IsQueryKeyword(WrapInMethod("
Dim index As Integer = 0
While index <= 10
    index += 1
End $$While")))
        End Sub

        <Fact>
        Public Sub IsQueryKeyword_Not_DoWhileLoop()
            Assert.False(IsQueryKeyword(WrapInMethod("
Do $$While index <= 10
    index += 1
Loop")))
        End Sub

        <Fact>
        Public Sub IsQueryKeyword_Not_DoLoopWhile()
            Assert.False(IsQueryKeyword(WrapInMethod("
Dim index As Integer = 0
Do
    index += 1
Loop $$While index < 10")))
        End Sub

        Private Shared Function IsMethodLevelMember(markup As String) As Boolean
            Dim code As String = Nothing
            Dim span As TextSpan
            MarkupTestFile.GetSpan(markup, code, span)
            Dim tree = SyntaxFactory.ParseSyntaxTree(code)
            Dim node = tree.GetRoot().FindNode(span)
            Return VisualBasicSyntaxFacts.Instance.IsMethodLevelMember(node)
        End Function

        Private Shared Function WrapInMethod(methodBody As String) As String
            Return $"
Class C
    Sub M()
        { methodBody }
    End Sub
End Class"
        End Function

        Private Shared Function IsQueryKeyword(markup As String) As Boolean
            Dim code As String = Nothing
            Dim position As Integer
            MarkupTestFile.GetPosition(markup, code, position)
            Dim tree = SyntaxFactory.ParseSyntaxTree(code)
            Dim token = tree.GetRoot().FindToken(position)
            Return VisualBasicSyntaxFacts.Instance.IsQueryKeyword(token)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40917")>
        Public Sub IsLeftSideOfCompoundAssignment()
            Assert.True(IsLeftSideOfCompoundAssignment(WrapInMethod("
Dim index As Integer = 0
$$index += 1")))
        End Sub

        Private Shared Function IsLeftSideOfCompoundAssignment(markup As String) As Boolean
            Dim code As String = Nothing
            Dim position As Integer
            MarkupTestFile.GetPosition(markup, code, position)
            Dim tree = SyntaxFactory.ParseSyntaxTree(code)
            Dim node = tree.GetRoot().FindToken(position).Parent
            Return VisualBasicSyntaxFacts.Instance.IsLeftSideOfCompoundAssignment(node)
        End Function
    End Class

End Namespace
