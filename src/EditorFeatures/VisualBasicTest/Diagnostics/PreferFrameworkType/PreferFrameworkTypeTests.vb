﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeFixes.PreferFrameworkType
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.Diagnostics.Analyzers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.PreferFrameworkTypeTests
    Partial Public Class PreferFrameworkTypeTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Private ReadOnly onWithInfo = New CodeStyleOption(Of Boolean)(True, NotificationOption.Suggestion)
        Private ReadOnly offWithInfo = New CodeStyleOption(Of Boolean)(False, NotificationOption.Suggestion)

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(New VisualBasicPreferFrameworkTypeDiagnosticAnalyzer(), New PreferFrameworkTypeCodeFixProvider())
        End Function

        Private ReadOnly Property NoFrameworkType As IDictionary(Of OptionKey, Object)
            Get
                Return [Option](CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, True, NotificationOption.Suggestion).With(
                 CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, Me.onWithInfo, GetLanguage())
            End Get
        End Property

        Private ReadOnly Property FrameworkTypeEverywhere As IDictionary(Of OptionKey, Object)
            Get
                Return [Option](CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, False, NotificationOption.Suggestion).With(
                 CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, Me.offWithInfo, GetLanguage())
            End Get
        End Property

        Private ReadOnly Property FrameworkTypeInDeclaration As IDictionary(Of OptionKey, Object)
            Get
                Return [Option](CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, False, NotificationOption.Suggestion).With(
                 CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, Me.onWithInfo, GetLanguage())
            End Get
        End Property

        Private ReadOnly Property FrameworkTypeInMemberAccess As IDictionary(Of OptionKey, Object)
            Get
                Return [Option](CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, False, NotificationOption.Suggestion).With(
                 CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, Me.onWithInfo, GetLanguage())
            End Get
        End Property

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        Public Async Function NotWhenOptionsAreNotSet() As Task
            Await TestMissingAsync("
Class C
    Protected i As [|Integer|]
End Class
", options:=NoFrameworkType)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        Public Async Function NotOnUserdefinedType() As Task
            Await TestMissingAsync("
Class C
    Protected i As [|C|]
End Class
", options:=FrameworkTypeEverywhere)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        Public Async Function NotOnFrameworkType() As Task
            Await TestMissingAsync("
Imports System
Class C
    Protected i As [|Int32|]
End Class
", options:=FrameworkTypeEverywhere)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        Public Async Function NotOnQualifiedTypeSyntax() As Task
            Await TestMissingAsync("
Class C
    Protected i As [|System.Int32|]
End Class
", options:=FrameworkTypeEverywhere)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        Public Async Function NotOnFrameworkTypeWithNoPredefinedKeywordEquivalent() As Task
            Await TestMissingAsync("
Class C
    Protected i As [|List|](Of Integer)
End Class
", options:=FrameworkTypeEverywhere)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        Public Async Function NotOnIdentifierThatIsNotTypeSyntax() As Task
            Await TestMissingAsync("
Class C
    Protected [|i|] As Integer
End Class
", options:=FrameworkTypeEverywhere)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        Public Async Function NotOnBoolean_KeywordMatchesTypeName() As Task
            Await TestMissingAsync("
Class C
    Protected x As [|Boolean|]
End Class
", options:=FrameworkTypeEverywhere)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        Public Async Function NotOnByte_KeywordMatchesTypeName() As Task
            Await TestMissingAsync("
Class C
    Protected x As [|Byte|]
End Class
", options:=FrameworkTypeEverywhere)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        Public Async Function NotOnChar_KeywordMatchesTypeName() As Task
            Await TestMissingAsync("
Class C
    Protected x As [|Char|]
End Class
", options:=FrameworkTypeEverywhere)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        Public Async Function NotOnObject_KeywordMatchesTypeName() As Task
            Await TestMissingAsync("
Class C
    Protected x As [|Object|]
End Class
", options:=FrameworkTypeEverywhere)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        Public Async Function NotOnSByte_KeywordMatchesTypeName() As Task
            Await TestMissingAsync("
Class C
    Protected x As [|SByte|]
End Class
", options:=FrameworkTypeEverywhere)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        Public Async Function NotOnString_KeywordMatchesTypeName() As Task
            Await TestMissingAsync("
Class C
    Protected x As [|String|]
End Class
", options:=FrameworkTypeEverywhere)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        Public Async Function NotOnSingle_KeywordMatchesTypeName() As Task
            Await TestMissingAsync("
Class C
    Protected x As [|Single|]
End Class
", options:=FrameworkTypeEverywhere)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        Public Async Function NotOnDecimal_KeywordMatchesTypeName() As Task
            Await TestMissingAsync("
Class C
    Protected x As [|Decimal|]
End Class
", options:=FrameworkTypeEverywhere)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        Public Async Function NotOnDouble_KeywordMatchesTypeName() As Task
            Await TestMissingAsync("
Class C
    Protected x As [|Double|]
End Class
", options:=FrameworkTypeEverywhere)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        Public Async Function FieldDeclaration() As Task
            Await TestAsync(
"Imports System
Class C
    Protected i As [|Integer|]
End Class
",
"Imports System
Class C
    Protected i As Int32
End Class", options:=FrameworkTypeInDeclaration)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        Public Async Function FieldDeclarationWithInitializer() As Task
            Await TestAsync(
"Imports System
Class C
    Protected i As [|Integer|] = 5
End Class
",
"Imports System
Class C
    Protected i As Int32 = 5
End Class", options:=FrameworkTypeInDeclaration)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        Public Async Function DelegateDeclaration() As Task
            Await TestAsync(
"Imports System
Class C
    Public Delegate Function PerformCalculation(x As Integer, y As Integer) As [|Integer|]
End Class
",
"Imports System
Class C
    Public Delegate Function PerformCalculation(x As Integer, y As Integer) As Int32
End Class", options:=FrameworkTypeInDeclaration)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        Public Async Function PropertyDeclaration() As Task
            Await TestAsync(
"Imports System
Class C
    Public Property X As [|Long|]
End Class
",
"Imports System
Class C
    Public Property X As Int64
End Class", options:=FrameworkTypeInDeclaration)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        Public Async Function GenericPropertyDeclaration() As Task
            Await TestAsync(
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
End Class", options:=FrameworkTypeInDeclaration)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        Public Async Function FunctionDeclarationReturnType() As Task
            Await TestAsync(
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
End Class", options:=FrameworkTypeInDeclaration)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        Public Async Function MethodDeclarationParameters() As Task
            Await TestAsync(
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
End Class", options:=FrameworkTypeInDeclaration)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        Public Async Function GenericMethodInvocation() As Task
            Await TestAsync(
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
End Class", options:=FrameworkTypeInDeclaration)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        Public Async Function LocalDeclaration() As Task
            Await TestAsync(
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
End Class", options:=FrameworkTypeInDeclaration)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        Public Async Function MemberAccess() As Task
            Await TestAsync(
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
End Class", options:=FrameworkTypeInMemberAccess)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        Public Async Function MemberAccess2() As Task
            Await TestAsync(
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
End Class", options:=FrameworkTypeInMemberAccess)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        Public Async Function DocCommentTriviaCrefExpression() As Task
            Await TestAsync(
"Imports System
Class C
    ''' <see cref=""[|Integer|].MaxValue""/>
    Public Sub Test()
    End Sub
End Class
",
"Imports System
Class C
    ''' <see cref=""Int32.MaxValue""/>
    Public Sub Test()
    End Sub
End Class", options:=FrameworkTypeInMemberAccess)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        Public Async Function GetTypeExpression() As Task
            Await TestAsync(
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
End Class", options:=FrameworkTypeInDeclaration)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        Public Async Function FormalParametersWithinLambdaExression() As Task
            Await TestAsync(
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
End Class", options:=FrameworkTypeInDeclaration)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        Public Async Function ObjectCreationExpression() As Task
            Await TestAsync(
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
End Class", options:=FrameworkTypeInDeclaration)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        Public Async Function ArrayDeclaration() As Task
            Await TestAsync(
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
End Class", options:=FrameworkTypeInDeclaration)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        Public Async Function ArrayInitializer() As Task
            Await TestAsync(
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
End Class", options:=FrameworkTypeInDeclaration)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        Public Async Function MultiDimentionalArrayAsGenericTypeParameter() As Task
            Await TestAsync(
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
End Class", options:=FrameworkTypeInDeclaration)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        Public Async Function ForStatement() As Task
            Await TestAsync(
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
End Class", options:=FrameworkTypeInDeclaration)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        Public Async Function ForeachStatement() As Task
            Await TestAsync(
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
End Class", options:=FrameworkTypeInDeclaration)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        Public Async Function LeadingTrivia() As Task
            Await TestAsync(
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
End Class", options:=FrameworkTypeInDeclaration, compareTokens:=False)
        End Function

    End Class
End Namespace
