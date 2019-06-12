' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.ConvertLinq

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeActions.ConvertLinq

    Public Class ConvertLinqMethodToLinqQueryTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicConvertLinqMethodToLinqQueryProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinqMethodToQuery)>
        Public Async Function Conversion_WhereOrderByTrivialSelect() As Task
            Await Test(
"From num In New Integer() {0, 1, 2} Where num Mod 2 = 0 Order By num Select num",
"New Integer() {0, 1, 2}.Where(Function(num) num Mod 2 = 0).OrderBy(Function(num) num)")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinqMethodToQuery)>
        Public Async Function Conversion_WhereSelect() As Task
            Await Test(
"From x in New Integer() {0, 1, 2} Where x<5 Select x* x",
"New Integer() {0, 1, 2}.Where(Function(x) x < 5).Select(Function(x) x * x)")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinqMethodToQuery)>
        Public Async Function Conversion_MultipleFrom() As Task
            Await Test(
"From x In New Integer() {0} From y In New Integer() {1} From z In New Integer() {2} Where x + y + z < 5 Select x * x",
"New Integer() {0}.SelectMany(Function(x) New Integer() {1}, Function(x, y) New With {x, y
       }).SelectMany(Function(VBIt) New Integer() {2}, Function(VBIt1, z) New With {VBIt1, z
       }).Where(Function(VBIt) VBIt.VBIt1.x + VBIt.VBIt1.y + VBIt.z < 5).Select(Function(VBIt) VBIt.VBIt1.x * VBIt.VBIt1.x)")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinqMethodToQuery)>
        Public Async Function Conversion_SelectWithType() As Task
            Await Test("From x As String In New Object() {""1""} Select x", "New Object() {""1""}.Cast(Of String)()")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinqMethodToQuery)>
        Public Async Function Conversion_ReduceUnnecessarySelect() As Task
            Await Test("From a In New Integer() {1, 2, 3} Select a", "New Integer() {1, 2, 3}")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinqMethodToQuery)>
        Public Async Function Conversion_DoubleFrom() As Task
            Await Test(
"From w In ""aaa bbb ccc"".Split("" "") From c In w Select c",
"""aaa bbb ccc"".Split("" "").SelectMany(Function(w) w, Function(w, c) c)")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinqMethodToQuery)>
        Public Async Function NoDiagnostics_Group() As Task
            Await TestNoDiagnostics("From w In New String() { ""apples"", ""blueberries"", ""oranges"", ""bananas"", ""apricots"" }
Group w By FirstLetter = w(0) Into fruitGroup = Group
Where fruitGroup.Count >= 2
Select Words = fruitGroup.Count(), FirstLetter")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinqMethodToQuery)>
        Public Async Function NoDiagnostics_Join() As Task
            Await TestNoDiagnostics("From a In New Integer() {1, 2, 3} Join b In New Integer(){4} On a Equals b")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinqMethodToQuery)>
        Public Async Function NoDiagnostics_Let1() As Task
            Await TestNoDiagnostics("From sentence In new String() { ""aa bb"", ""ee ff"", ""ii"" }
let words = sentence.Split("" "")
from word in words
let w = word.ToLower()
where w(0) = ""a""
select word")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinqMethodToQuery)>
        Public Async Function NoDiagnostics_Let2() As Task
            Await TestNoDiagnostics("From x in ""123""
Let z = x.ToString()
Select Integer.Parse(z)")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinqMethodToQuery)>
        Public Async Function NoDiagnostics_GroupBy() As Task
            Await TestNoDiagnostics("From x In New Integer() {1} Group By year = x * 2 Into g = Group, Count()")
        End Function

        Private Async Function Test(input As String, expectedOutput As String) As Task
            Const code As String = "
Imports System.Collections.Generic
Imports System.Linq
Class C
    Function M() Of IEnumerable(Of Object)
       Return ##
    End Function
End Class
"
            Await TestInRegularAndScriptAsync(code.Replace("##", "[||]" + input), code.Replace("##", expectedOutput))
        End Function

        Private Async Function TestNoDiagnostics(input As String) As Task
            Const code As String = "
Imports System.Collections.Generic
Imports System.Linq
Class C
    Function M() Of IEnumerable(Of Object)
       Return ##
    End Function
End Class    
"
            Await TestMissingInRegularAndScriptAsync(code.Replace("##", "[||]" + input))
        End Function
    End Class
End Namespace

