' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Option Strict On

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
Imports Microsoft.CodeAnalysis.PreferFrameworkType
Imports Microsoft.CodeAnalysis.VisualBasic.Diagnostics.Analyzers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.PreferFrameworkTypeTests
    <Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
    Partial Public Class PreferFrameworkTypeTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest_NoEditor

        Private ReadOnly onWithInfo As New CodeStyleOption2(Of Boolean)(True, NotificationOption2.Suggestion)
        Private ReadOnly offWithInfo As New CodeStyleOption2(Of Boolean)(False, NotificationOption2.Suggestion)

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicPreferFrameworkTypeDiagnosticAnalyzer(),
                    New PreferFrameworkTypeCodeFixProvider())
        End Function

        Private ReadOnly Property NoFrameworkType As OptionsCollection
            Get
                Return New OptionsCollection(GetLanguage()) From {
                    {CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, True, NotificationOption2.Suggestion},
                    {CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, Me.onWithInfo}
                    }
            End Get
        End Property

        Private ReadOnly Property FrameworkTypeEverywhere As OptionsCollection
            Get
                Return New OptionsCollection(GetLanguage()) From {
                    {CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, False, NotificationOption2.Suggestion},
                    {CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, Me.offWithInfo}
                    }
            End Get
        End Property

        Private ReadOnly Property FrameworkTypeInDeclaration As OptionsCollection
            Get
                Return New OptionsCollection(GetLanguage()) From {
                    {CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, False, NotificationOption2.Suggestion},
                    {CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, Me.onWithInfo}
                    }
            End Get
        End Property

        Private ReadOnly Property FrameworkTypeInMemberAccess As OptionsCollection
            Get
                Return New OptionsCollection(GetLanguage()) From {
                    {CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, False, NotificationOption2.Suggestion},
                    {CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, Me.onWithInfo}
                    }
            End Get
        End Property

        <Fact>
        Public Async Function NotWhenOptionsAreNotSet() As Task
            Await TestMissingInRegularAndScriptAsync("
Class C
    Protected i As [|Integer|]
End Class
", New TestParameters(options:=NoFrameworkType))
        End Function

        <Fact>
        Public Async Function NotOnUserdefinedType() As Task
            Await TestMissingInRegularAndScriptAsync("
Class C
    Protected i As [|C|]
End Class
", New TestParameters(options:=FrameworkTypeEverywhere))
        End Function

        <Fact>
        Public Async Function NotOnFrameworkType() As Task
            Await TestMissingInRegularAndScriptAsync("
Imports System
Class C
    Protected i As [|Int32|]
End Class
", New TestParameters(options:=FrameworkTypeEverywhere))
        End Function

        <Fact>
        Public Async Function NotOnQualifiedTypeSyntax() As Task
            Await TestMissingInRegularAndScriptAsync("
Class C
    Protected i As [|System.Int32|]
End Class
", New TestParameters(options:=FrameworkTypeEverywhere))
        End Function

        <Fact>
        Public Async Function NotOnFrameworkTypeWithNoPredefinedKeywordEquivalent() As Task
            Await TestMissingInRegularAndScriptAsync("
Class C
    Protected i As [|List|](Of Integer)
End Class
", New TestParameters(options:=FrameworkTypeEverywhere))
        End Function

        <Fact>
        Public Async Function NotOnIdentifierThatIsNotTypeSyntax() As Task
            Await TestMissingInRegularAndScriptAsync("
Class C
    Protected [|i|] As Integer
End Class
", New TestParameters(options:=FrameworkTypeEverywhere))
        End Function

        <Fact>
        Public Async Function NotOnBoolean_KeywordMatchesTypeName() As Task
            Await TestMissingInRegularAndScriptAsync("
Class C
    Protected x As [|Boolean|]
End Class
", New TestParameters(options:=FrameworkTypeEverywhere))
        End Function

        <Fact>
        Public Async Function NotOnByte_KeywordMatchesTypeName() As Task
            Await TestMissingInRegularAndScriptAsync("
Class C
    Protected x As [|Byte|]
End Class
", New TestParameters(options:=FrameworkTypeEverywhere))
        End Function

        <Fact>
        Public Async Function NotOnChar_KeywordMatchesTypeName() As Task
            Await TestMissingInRegularAndScriptAsync("
Class C
    Protected x As [|Char|]
End Class
", New TestParameters(options:=FrameworkTypeEverywhere))
        End Function

        <Fact>
        Public Async Function NotOnObject_KeywordMatchesTypeName() As Task
            Await TestMissingInRegularAndScriptAsync("
Class C
    Protected x As [|Object|]
End Class
", New TestParameters(options:=FrameworkTypeEverywhere))
        End Function

        <Fact>
        Public Async Function NotOnSByte_KeywordMatchesTypeName() As Task
            Await TestMissingInRegularAndScriptAsync("
Class C
    Protected x As [|SByte|]
End Class
", New TestParameters(options:=FrameworkTypeEverywhere))
        End Function

        <Fact>
        Public Async Function NotOnString_KeywordMatchesTypeName() As Task
            Await TestMissingInRegularAndScriptAsync("
Class C
    Protected x As [|String|]
End Class
", New TestParameters(options:=FrameworkTypeEverywhere))
        End Function

        <Fact>
        Public Async Function NotOnSingle_KeywordMatchesTypeName() As Task
            Await TestMissingInRegularAndScriptAsync("
Class C
    Protected x As [|Single|]
End Class
", New TestParameters(options:=FrameworkTypeEverywhere))
        End Function

        <Fact>
        Public Async Function NotOnDecimal_KeywordMatchesTypeName() As Task
            Await TestMissingInRegularAndScriptAsync("
Class C
    Protected x As [|Decimal|]
End Class
", New TestParameters(options:=FrameworkTypeEverywhere))
        End Function

        <Fact>
        Public Async Function NotOnDouble_KeywordMatchesTypeName() As Task
            Await TestMissingInRegularAndScriptAsync("
Class C
    Protected x As [|Double|]
End Class
", New TestParameters(options:=FrameworkTypeEverywhere))
        End Function

        <Fact>
        Public Async Function FieldDeclaration() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Class C
    Protected i As [|Integer|]
End Class
",
"Imports System
Class C
    Protected i As Int32
End Class
", options:=FrameworkTypeInDeclaration)
        End Function

        <Fact>
        Public Async Function FieldDeclarationWithInitializer() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Class C
    Protected i As [|Integer|] = 5
End Class
",
"Imports System
Class C
    Protected i As Int32 = 5
End Class
", options:=FrameworkTypeInDeclaration)
        End Function

        <Fact>
        Public Async Function DelegateDeclaration() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Class C
    Public Delegate Function PerformCalculation(x As Integer, y As Integer) As [|Integer|]
End Class
",
"Imports System
Class C
    Public Delegate Function PerformCalculation(x As Integer, y As Integer) As Int32
End Class
", options:=FrameworkTypeInDeclaration)
        End Function

        <Fact>
        Public Async Function PropertyDeclaration() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Class C
    Public Property X As [|Long|]
End Class
",
"Imports System
Class C
    Public Property X As Int64
End Class
", options:=FrameworkTypeInDeclaration)
        End Function

        <Fact>
        Public Async Function GenericPropertyDeclaration() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Class C
    Public Property X As List(Of [|Long|])
End Class
",
"Imports System
Imports System.Collections.Generic
Class C
    Public Property X As List(Of Int64)
End Class
", options:=FrameworkTypeInDeclaration)
        End Function

        <Fact>
        Public Async Function FunctionDeclarationReturnType() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Class C
    Public Function F() As [|Integer|]
    End Function
End Class
",
"Imports System
Class C
    Public Function F() As Int32
    End Function
End Class
", options:=FrameworkTypeInDeclaration)
        End Function

        <Fact>
        Public Async Function MethodDeclarationParameters() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Class C
    Public Sub F(x As [|Integer|])
    End Sub
End Class
",
"Imports System
Class C
    Public Sub F(x As Int32)
    End Sub
End Class
", options:=FrameworkTypeInDeclaration)
        End Function

        <Fact>
        Public Async Function GenericMethodInvocation() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Class C
    Public Sub Method(Of T)()
    End Sub
    Public Sub Test()
        Method(Of [|Integer|])()
    End Sub
End Class
",
"Imports System
Class C
    Public Sub Method(Of T)()
    End Sub
    Public Sub Test()
        Method(Of Int32)()
    End Sub
End Class
", options:=FrameworkTypeInDeclaration)
        End Function

        <Fact>
        Public Async Function LocalDeclaration() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Class C
    Public Sub Test()
        Dim x As [|Integer|] = 5
    End Sub
End Class
",
"Imports System
Class C
    Public Sub Test()
        Dim x As Int32 = 5
    End Sub
End Class
", options:=FrameworkTypeInDeclaration)
        End Function

        <Fact>
        Public Async Function MemberAccess() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Class C
    Public Sub Test()
        Dim x = [|Integer|].MaxValue
    End Sub
End Class
",
"Imports System
Class C
    Public Sub Test()
        Dim x = Int32.MaxValue
    End Sub
End Class
", options:=FrameworkTypeInMemberAccess)
        End Function

        <Fact>
        Public Async Function MemberAccess2() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Class C
    Public Sub Test()
        Dim x = [|Integer|].Parse(""1"")
    End Sub
End Class
",
"Imports System
Class C
    Public Sub Test()
        Dim x = Int32.Parse(""1"")
    End Sub
End Class
", options:=FrameworkTypeInMemberAccess)
        End Function

        <Fact>
        Public Async Function DocCommentTriviaCrefExpression() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Class C
    ''' <see cref=""[|Integer|].MaxValue""/>
    Public Sub Test()
    End Sub
End Class
",
"Imports System
Class C
    ''' <see cref=""Integer.MaxValue""/>
    Public Sub Test()
    End Sub
End Class
", options:=FrameworkTypeInMemberAccess)
        End Function

        <Fact>
        Public Async Function GetTypeExpression() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Class C
    Public Sub Test()
         Dim x = GetType([|Integer|])
    End Sub
End Class
",
"Imports System
Class C
    Public Sub Test()
         Dim x = GetType(Int32)
    End Sub
End Class
", options:=FrameworkTypeInDeclaration)
        End Function

        <Fact>
        Public Async Function FormalParametersWithinLambdaExression() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Class C
    Public Sub Test()
        Dim func3 As Func(Of Integer, Integer) = Function(z As [|Integer|]) z + 1
    End Sub
End Class
",
"Imports System
Class C
    Public Sub Test()
        Dim func3 As Func(Of Integer, Integer) = Function(z As Int32) z + 1
    End Sub
End Class
", options:=FrameworkTypeInDeclaration)
        End Function

        <Fact>
        Public Async Function ObjectCreationExpression() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Class C
    Public Sub Test()
        Dim z = New [|Date|](2016, 8, 23)
    End Sub
End Class
",
"Imports System
Class C
    Public Sub Test()
        Dim z = New DateTime(2016, 8, 23)
    End Sub
End Class
", options:=FrameworkTypeInDeclaration)
        End Function

        <Fact>
        Public Async Function ArrayDeclaration() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Class C
    Public Sub Test()
        Dim k As [|Integer|]() = New Integer(3) {}
    End Sub
End Class
",
"Imports System
Class C
    Public Sub Test()
        Dim k As Int32() = New Integer(3) {}
    End Sub
End Class
", options:=FrameworkTypeInDeclaration)
        End Function

        <Fact>
        Public Async Function ArrayInitializer() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Class C
    Public Sub Test()
        Dim k As Integer() = New [|Integer|](3) {0, 1, 2, 3}
    End Sub
End Class
",
"Imports System
Class C
    Public Sub Test()
        Dim k As Integer() = New Int32(3) {0, 1, 2, 3}
    End Sub
End Class
", options:=FrameworkTypeInDeclaration)
        End Function

        <Fact>
        Public Async Function MultiDimentionalArrayAsGenericTypeParameter() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Class C
    Public Sub Test()
        Dim a As List(Of [|Integer|]()(,)(,,,))
    End Sub
End Class
",
"Imports System
Imports System.Collections.Generic
Class C
    Public Sub Test()
        Dim a As List(Of Int32()(,)(,,,))
    End Sub
End Class
", options:=FrameworkTypeInDeclaration)
        End Function

        <Fact>
        Public Async Function ForStatement() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Class C
    Public Sub Test()
        For j As [|Integer|] = 0 To 3
        Next
    End Sub
End Class
",
"Imports System
Class C
    Public Sub Test()
        For j As Int32 = 0 To 3
        Next
    End Sub
End Class
", options:=FrameworkTypeInDeclaration)
        End Function

        <Fact>
        Public Async Function ForeachStatement() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Class C
    Public Sub Test()
        For Each item As [|Integer|] In New Integer() {1, 2, 3}
        Next
    End Sub
End Class
",
"Imports System
Class C
    Public Sub Test()
        For Each item As Int32 In New Integer() {1, 2, 3}
        Next
    End Sub
End Class
", options:=FrameworkTypeInDeclaration)
        End Function

        <Fact>
        Public Async Function LeadingTrivia() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Class C
    Public Sub Test()
        ' This is a comment
        Dim x As [|Integer|]
    End Sub
End Class",
"Imports System
Class C
    Public Sub Test()
        ' This is a comment
        Dim x As Int32
    End Sub
End Class", options:=FrameworkTypeInDeclaration)
        End Function
    End Class
End Namespace
