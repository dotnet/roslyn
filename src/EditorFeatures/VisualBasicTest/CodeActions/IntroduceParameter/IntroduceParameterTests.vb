' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.IntroduceVariable

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings.IntroduceParameter
    Public Class IntroduceParameterTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicIntroduceParameterCodeRefactoringProvider()
        End Function

        Protected Overrides Function MassageActions(actions As ImmutableArray(Of CodeAction)) As ImmutableArray(Of CodeAction)
            Return GetNestedActions(actions)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)>
        Public Async Function TestExpressionWithNoMethodCallsCase() As Task
            Dim source =
"Class Program
    Sub M(x As Integer, y As Integer, z As Integer)
        Dim num As Integer = [|x * y * z|]
    End Sub
End Class"
            Dim expected =
"Class Program
    Sub M(x As Integer, y As Integer, z As Integer, num As Integer)
    End Sub
End Class"
            Await TestInRegularAndScriptAsync(source, expected, index:=0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)>
        Public Async Function TestExpressionCaseWithLocal() As Task
            Dim source =
"Class Program
    Sub M(x As Integer, y As Integer, z As Integer)
        Dim l As Integer = 5
        Dim num As Integer = [|l * x * y * z|]
    End Sub

    Sub M1(x As Integer, y As Integer, z As Integer)
        M(z, y, x)
    End Sub
End Class"
            Await TestMissingInRegularAndScriptAsync(source)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)>
        Public Async Function TestBasicComplexExpressionCase() As Task
            Dim source =
"Class Program
    Sub M(x As String, y As Integer, z As Integer)
        Dim num As Integer = [|x.Length * y * z|]
    End Sub

    Sub M1(y As String)
        M(y, 5, 2)
    End Sub
End Class"
            Dim expected =
"Class Program
    Sub M(x As String, y As Integer, z As Integer, num As Integer)
    End Sub

    Sub M1(y As String)
        M(y, 5, 2, y.Length * 5 * 2)
    End Sub
End Class"
            Await TestInRegularAndScriptAsync(source, expected, index:=0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)>
        Public Async Function TestExpressionCaseWithSingleMethodCall() As Task
            Dim source =
"Class Program
    Sub M(x As Integer, y As Integer, z As Integer)
        Dim num As Integer = [|x * y * z|]
    End Sub

    Sub M1(x As Integer, y As Integer, z As Integer)
        M(z, y, x)
    End Sub
End Class"
            Dim expected =
"Class Program
    Sub M(x As Integer, y As Integer, z As Integer, num As Integer)
    End Sub

    Sub M1(x As Integer, y As Integer, z As Integer)
        M(z, y, x, z * y * x)
    End Sub
End Class"
            Await TestInRegularAndScriptAsync(source, expected, index:=0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)>
        Public Async Function TestExpressionCaseWithSingleMethodCallMultipleDeclarators() As Task
            Dim source =
"Class Program
    Sub M(x As Integer, y As Integer, z As Integer)
        Dim num = [|x * y * z|], y = 0
    End Sub

    Sub M1(x As Integer, y As Integer, z As Integer)
        M(z, y, x)
    End Sub
End Class"
            Await TestMissingInRegularAndScriptAsync(source)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)>
        Public Async Function TestHighlightIncompleteExpressionCaseWithSingleMethodCall() As Task
            Dim source =
"Class Program
    Sub M(x As Integer, y As Integer, z As Integer)
        Dim num As Integer = 5 * [|x * y * z|]
    End Sub

    Sub M1(x As Integer, y As Integer, z As Integer)
        M(z, y, x)
    End Sub
End Class"
            Await TestMissingInRegularAndScriptAsync(source)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)>
        Public Async Function TestExpressionCaseWithMultipleMethodCall() As Task
            Dim source =
"Class Program
    Sub M(x As Integer, y As Integer, z As Integer)
        Dim num As Integer = [|x * y * z|]
    End Sub

    Sub M1(x As Integer, y As Integer, z As Integer)
        M(z, y, x)
        M(a + b, 5, x)
    End Sub
End Class"
            Dim expected =
"Class Program
    Sub M(x As Integer, y As Integer, z As Integer, num As Integer)
    End Sub

    Sub M1(x As Integer, y As Integer, z As Integer)
        M(z, y, x, z * y * x)
        M(a + b, 5, x, (a + b) * 5 * x)
    End Sub
End Class"
            Await TestInRegularAndScriptAsync(source, expected, index:=0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)>
        Public Async Function TestExpressionAllOccurrences() As Task
            Dim source =
"Class Program
    Sub M(x As Integer, y As Integer, z As Integer)
        Dim num2 As Integer = x * y * z
        Dim num As Integer = [|x * y * z|]
    End Sub

    Sub M1(x As Integer, y As Integer, z As Integer)
        M(z, y, x)
    End Sub
End Class"
            Dim expected =
"Class Program
    Sub M(x As Integer, y As Integer, z As Integer, num As Integer)
        Dim num2 As Integer = num
    End Sub

    Sub M1(x As Integer, y As Integer, z As Integer)
        M(z, y, x, z * y * x)
    End Sub
End Class"
            Await TestInRegularAndScriptAsync(source, expected, index:=3)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)>
        Public Async Function TestExpressionWithNoMethodCallsTrampoline() As Task
            Dim source =
"Class Program
    Sub M(x As Integer, y As Integer, z As Integer)
        Dim num As Integer = [|x * y * z|]
    End Sub
End Class"
            Dim expected =
"Class Program
    Public Function GetNum(x As Integer, y As Integer, z As Integer) As Integer
        Return x * y * z
    End Function

    Sub M(x As Integer, y As Integer, z As Integer, num As Integer)
    End Sub
End Class"
            Await TestInRegularAndScriptAsync(source, expected, index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)>
        Public Async Function TestExpressionWithSingleMethodCallTrampoline() As Task
            Dim source =
"Class Program
    Sub M(x As Integer, y As Integer, z As Integer)
        Dim num As Integer = [|x * y * z|]
    End Sub

    Sub M1(x As Integer, y As Integer, z As Integer)
        M(z, y, x)
    End Sub
End Class"
            Dim expected =
"Class Program
    Public Function GetNum(x As Integer, y As Integer, z As Integer) As Integer
        Return x * y * z
    End Function

    Sub M(x As Integer, y As Integer, z As Integer, num As Integer)
    End Sub

    Sub M1(x As Integer, y As Integer, z As Integer)
        M(z, y, x, GetNum(z, y, x))
    End Sub
End Class"
            Await TestInRegularAndScriptAsync(source, expected, index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)>
        Public Async Function TestExpressionWithSingleMethodCallTrampolineAllOccurrences() As Task
            Dim source =
"Class Program
    Sub M(x As Integer, y As Integer, z As Integer)
        Dim num As Integer = [|x * y * z|]
        Dim num2 As Integer = x * y * z
    End Sub

    Sub M1(x As Integer, y As Integer, z As Integer)
        M(z, y, x)
    End Sub
End Class"
            Dim expected =
"Class Program
    Public Function GetNum(x As Integer, y As Integer, z As Integer) As Integer
        Return x * y * z
    End Function

    Sub M(x As Integer, y As Integer, z As Integer, num As Integer)
        Dim num2 As Integer = num
    End Sub

    Sub M1(x As Integer, y As Integer, z As Integer)
        M(z, y, x, GetNum(z, y, x))
    End Sub
End Class"

            Await TestInRegularAndScriptAsync(source, expected, index:=4)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)>
        Public Async Function TestExpressionWithSingleMethodCallAndAccessorsTrampoline() As Task
            Dim source =
"Class Program
    Sub M(x As Integer, y As Integer, z As Integer)
        Dim num As Integer = [|x * y * z|]
    End Sub

    Sub M1(x As Integer, y As Integer, z As Integer)
        Me.M(z, y, x)
    End Sub
End Class"
            Dim expected =
"Class Program
    Public Function GetNum(x As Integer, y As Integer, z As Integer) As Integer
        Return x * y * z
    End Function

    Sub M(x As Integer, y As Integer, z As Integer, num As Integer)
    End Sub

    Sub M1(x As Integer, y As Integer, z As Integer)
        Me.M(z, y, x, GetNum(z, y, x))
    End Sub
End Class"
            Await TestInRegularAndScriptAsync(source, expected, index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)>
        Public Async Function TestExpressionWithSingleMethodCallAndAccessorsConditionalTrampoline() As Task
            Dim source =
"Class Program
    Sub M(x As Integer, y As Integer, z As Integer)
        Dim num As Integer = [|x * y * z|]
    End Sub

    Sub M1(x As Integer, y As Integer, z As Integer)
        Me?.M(z, y, x)
    End Sub
End Class"
            Dim expected =
"Class Program
    Public Function GetNum(x As Integer, y As Integer, z As Integer) As Integer
        Return x * y * z
    End Function

    Sub M(x As Integer, y As Integer, z As Integer, num As Integer)
    End Sub

    Sub M1(x As Integer, y As Integer, z As Integer)
        Me?.M(z, y, x, Me?.GetNum(z, y, x))
    End Sub
End Class"
            Await TestInRegularAndScriptAsync(source, expected, index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)>
        Public Async Function TestExpressionWithSingleMethodCallMultipleAccessorsTrampoline() As Task
            Dim source =
"Class TestClass
    Sub Main(args As String())
        Dim a = New A()
        a.Prop.ComputeAge(5, 5)
    End Sub
End Class

Class A
    Public Prop As B
End Class

Class B
    Function ComputeAge(x As Integer, y As Integer) As Integer
        Dim age = [|x + y|]
        Return age
    End Function
End Class"
            Dim expected =
"Class TestClass
    Sub Main(args As String())
        Dim a = New A()
        a.Prop.ComputeAge(5, 5, a.Prop.GetAge(5, 5))
    End Sub
End Class

Class A
    Public Prop As B
End Class

Class B
    Public Function GetAge(x As Integer, y As Integer) As Integer
        Return x + y
    End Function

    Function ComputeAge(x As Integer, y As Integer, age As Integer) As Integer
        Return age
    End Function
End Class"
            Await TestInRegularAndScriptAsync(source, expected, index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)>
        Public Async Function TestExpressionWithSingleMethodCallMultipleAccessorsConditionalTrampoline() As Task
            Dim source =
"Class TestClass
    Sub Main(args As String())
        Dim a = New A()
        a?.Prop?.ComputeAge(5, 5)
    End Sub
End Class

Class A
    Public Prop As B
End Class

Class B
    Function ComputeAge(x As Integer, y As Integer) As Integer
        Dim age = [|x + y|]
        Return age
    End Function
End Class"
            Dim expected =
"Class TestClass
    Sub Main(args As String())
        Dim a = New A()
        a?.Prop?.ComputeAge(5, 5, a?.Prop?.GetAge(5, 5))
    End Sub
End Class

Class A
    Public Prop As B
End Class

Class B
    Public Function GetAge(x As Integer, y As Integer) As Integer
        Return x + y
    End Function

    Function ComputeAge(x As Integer, y As Integer, age As Integer) As Integer
        Return age
    End Function
End Class"
            Await TestInRegularAndScriptAsync(source, expected, index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)>
        Public Async Function TestExpressionWithSingleMethodCallAccessorsMixedConditionalTrampoline() As Task
            Dim source =
"Class TestClass
    Sub Main(args As String())
        Dim a = New A()
        a.Prop?.ComputeAge(5, 5)
    End Sub
End Class

Class A
    Public Prop As B
End Class

Class B
    Function ComputeAge(x As Integer, y As Integer) As Integer
        Dim age = [|x + y|]
        Return age
    End Function
End Class"
            Dim expected =
"Class TestClass
    Sub Main(args As String())
        Dim a = New A()
        a.Prop?.ComputeAge(5, 5, a.Prop?.GetAge(5, 5))
    End Sub
End Class

Class A
    Public Prop As B
End Class

Class B
    Public Function GetAge(x As Integer, y As Integer) As Integer
        Return x + y
    End Function

    Function ComputeAge(x As Integer, y As Integer, age As Integer) As Integer
        Return age
    End Function
End Class"
            Await TestInRegularAndScriptAsync(source, expected, index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)>
        Public Async Function TestExpressionWithSingleMethodCallAccessorsMixedConditionalTrampoline2() As Task
            Dim source =
"Class TestClass
    Sub Main(args As String())
        Dim a = New A()
        a?.Prop.ComputeAge(5, 5)
    End Sub
End Class

Class A
    Public Prop As B
End Class

Class B
    Function ComputeAge(x As Integer, y As Integer) As Integer
        Dim age = [|x + y|]
        Return age
    End Function
End Class"
            Dim expected =
"Class TestClass
    Sub Main(args As String())
        Dim a = New A()
        a?.Prop.ComputeAge(5, 5, a?.Prop.GetAge(5, 5))
    End Sub
End Class

Class A
    Public Prop As B
End Class

Class B
    Public Function GetAge(x As Integer, y As Integer) As Integer
        Return x + y
    End Function

    Function ComputeAge(x As Integer, y As Integer, age As Integer) As Integer
        Return age
    End Function
End Class"
            Await TestInRegularAndScriptAsync(source, expected, index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)>
        Public Async Function TestExpressionWithNoMethodCallOverload() As Task
            Dim source =
"Class Program
    Sub M(x As Integer, y As Integer, z As Integer)
        Dim num As Integer = [|x * y * z|]
    End Sub

    Sub M1(x As Integer, y As Integer, z As Integer)
        M(z, y, x)
    End Sub
End Class"
            Dim expected =
"Class Program
    Public Sub M(x As Integer, y As Integer, z As Integer)
        M(x, y, z, x * y * z)
    End Sub

    Sub M(x As Integer, y As Integer, z As Integer, num As Integer)
    End Sub

    Sub M1(x As Integer, y As Integer, z As Integer)
        M(z, y, x)
    End Sub
End Class"

            Await TestInRegularAndScriptAsync(source, expected, index:=2)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)>
        Public Async Function TestExpressionCaseWithRecursiveCall() As Task
            Dim source =
"Class Program
    Sub M(x As Integer, y As Integer, z As Integer)
        Dim num As Integer = [|x * y * z|]
        M(x, x, z)
    End Sub
End Class"
            Dim expected =
"Class Program
    Sub M(x As Integer, y As Integer, z As Integer, num As Integer)
        M(x, x, z, x * x * z)
    End Sub
End Class"

            Await TestInRegularAndScriptAsync(source, expected, index:=0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)>
        Public Async Function TestExpressionCaseWithNestedRecursiveCall() As Task
            Dim source =
"Class Program
    Function M(x As Integer, y As Integer, z As Integer) As Integer
        Dim num As Integer = [|x * y * z|]
        return M(x, x, M(x, y, z))
    End Function
End Class"
            Dim expected =
"Class Program
    Function M(x As Integer, y As Integer, z As Integer, num As Integer) As Integer
        return M(x, x, M(x, y, z, x * y * z), x * x * M(x, y, z, x * y * z))
    End Function
End Class"

            Await TestInRegularAndScriptAsync(source, expected, index:=0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)>
        Public Async Function TestExpressionCaseWithParamsArg() As Task
            Dim source =
"Class Program
    Function M(ParamArray args() As Integer) As Integer
        Dim num As Integer = [|args(0) + args(1)|]
        Return num
    End Function
End Class"

            Await TestMissingInRegularAndScriptAsync(source)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)>
        Public Async Function TestExpressionCaseWithOptionalParameters() As Task
            Dim source =
"Class Program
    Sub M(x As Integer, Optional y As Integer = 5)
        Dim num As Integer = [|x * y|]
    End Sub

    Sub M1()
        M(7, 2)
    End Sub
End Class"
            Dim expected =
"Class Program
    Sub M(x As Integer, num As Integer, Optional y As Integer = 5)
    End Sub

    Sub M1()
        M(7, 7 * 2, 2)
    End Sub
End Class"

            Await TestInRegularAndScriptAsync(source, expected, index:=0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)>
        Public Async Function TestExpressionCaseWithOptionalParametersUsed() As Task
            Dim source =
"Class Program
    Sub M(x As Integer, Optional y As Integer = 5)
        Dim num As Integer = [|x * y|]
    End Sub

    Sub M1()
        M(7)
    End Sub
End Class"
            Dim expected =
"Class Program
    Sub M(x As Integer, num As Integer, Optional y As Integer = 5)
    End Sub

    Sub M1()
        M(7, 7 * 5)
    End Sub
End Class"

            Await TestInRegularAndScriptAsync(source, expected, index:=0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)>
        Public Async Function TestExpressionCaseWithCancellationToken() As Task
            Dim source =
"Imports System.Threading
Class Program
    Sub M(x As Integer, cancellationToken As CancellationToken)
        Dim num As Integer = [|x * x|]
    End Sub

    Sub M1(cancellationToken As CancellationToken)
        M(7, cancellationToken)
    End Sub
End Class"
            Dim expected =
"Imports System.Threading
Class Program
    Sub M(x As Integer, num As Integer, cancellationToken As CancellationToken)
    End Sub

    Sub M1(cancellationToken As CancellationToken)
        M(7, 7 * 7, cancellationToken)
    End Sub
End Class"

            Await TestInRegularAndScriptAsync(source, expected, index:=0)
        End Function
    End Class
End Namespace

