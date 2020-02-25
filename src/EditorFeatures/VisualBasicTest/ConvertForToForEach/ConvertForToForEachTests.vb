﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.ConvertForToForEach

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ConvertForToForEach
    Public Class ConvertForToForEachTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicConvertForToForEachCodeRefactoringProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)>
        Public Async Function TestArray1() As Task
            Await TestInRegularAndScript1Async(
"imports System

class C
    sub Test(array as string())
        [||]For i = 0 to array.Length - 1
            Console.WriteLine(array(i))
        next
    end sub
end class",
"imports System

class C
    sub Test(array as string())
        For Each {|Rename:v|} In array
            Console.WriteLine(v)
        next
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)>
        Public Async Function TestForSelected() As Task
            Await TestInRegularAndScript1Async(
"imports System

class C
    sub Test(array as string())
        [|For|] i = 0 to array.Length - 1
            Console.WriteLine(array(i))
        next
    end sub
end class",
"imports System

class C
    sub Test(array as string())
        For Each {|Rename:v|} In array
            Console.WriteLine(v)
        next
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)>
        Public Async Function TestAtEndOfFor() As Task
            Await TestInRegularAndScript1Async(
"imports System

class C
    sub Test(array as string())
        For i = 0 to array.Length - 1[||]
            Console.WriteLine(array(i))
        next
    end sub
end class",
"imports System

class C
    sub Test(array as string())
        For Each {|Rename:v|} In array
            Console.WriteLine(v)
        next
    end sub
end class")
        End Function

        <WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)>
        Public Async Function TestBeforeFor() As Task
            Await TestInRegularAndScript1Async(
"imports System

class C
    sub Test(array as string())
       [||] For i = 0 to array.Length - 1
            Console.WriteLine(array(i))
        next
    end sub
end class",
"imports System

class C
    sub Test(array as string())
        For Each {|Rename:v|} In array
            Console.WriteLine(v)
        next
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)>
        Public Async Function TestMissingAfterFor() As Task
            Await TestMissingInRegularAndScriptAsync(
"imports System

class C
    sub Test(array as string())
        For [||]i = 0 to array.Length - 1
            Console.WriteLine(array(i))
        next
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)>
        Public Async Function TestArrayPlusStep1() As Task
            Await TestInRegularAndScript1Async(
"imports System

class C
    sub Test(array as string())
        [||]For i = 0 to array.Length - 1 step 1
            Console.WriteLine(array(i))
        next
    end sub
end class",
"imports System

class C
    sub Test(array as string())
        For Each {|Rename:v|} In array
            Console.WriteLine(v)
        next
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)>
        Public Async Function TestMissingWithWrongStep() As Task
            Await TestMissingInRegularAndScriptAsync(
"imports System

class C
    sub Test(array as string())
        [||]For i = 0 to array.Length - 1 step 2
            Console.WriteLine(array(i))
        next
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)>
        Public Async Function TestMissingIfReferencingNotDeclaringVariable() As Task
            Await TestMissingInRegularAndScriptAsync(
"imports System

class C
    sub Test(array as string())
        dim i as integer
        [||]For i = 0 to array.Length - 1 step 2
            Console.WriteLine(array(i))
        next
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)>
        Public Async Function TestMissingWithIncorrectCondition1() As Task
            Await TestMissingInRegularAndScriptAsync(
"imports System

class C
    sub Test(array as string())
        [||]For i = 0 to array.Length
        {
            Console.WriteLine(array(i))
        next
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)>
        Public Async Function TestMissingWithIncorrectCondition2() As Task
            Await TestMissingInRegularAndScriptAsync(
"imports System

class C
    sub Test(array as string())
        [||]For i = 0 to GetLength(array) - 1 
        {
            Console.WriteLine(array(i))
        next
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)>
        Public Async Function TestMissingWithIncorrectCondition3() As Task
            Await TestMissingInRegularAndScriptAsync(
"imports System

class C
    sub Test(array as string())
        [||]For i = 0 to array.Length - 2
        {
            Console.WriteLine(array(i))
        next
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)>
        Public Async Function TestNotStartingAtZero() As Task
            Await TestMissingInRegularAndScriptAsync(
"imports System

class C
    sub Test(array as string())
        [||]For i = 1 to array.Length - 1
        {
            Console.WriteLine(array(i))
        next
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)>
        Public Async Function TestList1() As Task
            Await TestInRegularAndScript1Async(
"imports System
imports System.Collections.Generic

class C
    sub Test(list as IList(of string))
        [||]For i = 0 to list.Count - 1
            Console.WriteLine(list(i))
        next
    end sub
end class",
"imports System
imports System.Collections.Generic

class C
    sub Test(list as IList(of string))
        For Each {|Rename:v|} In list
            Console.WriteLine(v)
        next
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)>
        Public Async Function TestChooseNameFromDeclarationStatement() As Task
            Await TestInRegularAndScript1Async(
"imports System
imports System.Collections.Generic

class C
    sub Test(list as IList(of string))
        [||]For i = 0 to list.Count - 1
            dim val = list(i)
            Console.WriteLine(list(i))
        next
    end sub
end class",
"imports System
imports System.Collections.Generic

class C
    sub Test(list as IList(of string))
        For Each val In list
            Console.WriteLine(val)
        next
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)>
        Public Async Function TestChooseNameAndTypeFromDeclarationStatement() As Task
            Await TestInRegularAndScript1Async(
"imports System
imports System.Collections.Generic

class C
    sub Test(list as IList(of string))
        [||]For i = 0 to list.Count - 1
            dim val As Object = list(i)
            Console.WriteLine(list(i))
        next
    end sub
end class",
"imports System
imports System.Collections.Generic

class C
    sub Test(list as IList(of string))
        For Each val As Object In list
            Console.WriteLine(val)
        next
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)>
        Public Async Function TestChooseNameFromDeclarationStatement_PreserveComments() As Task
            Await TestInRegularAndScript1Async(
"imports System
imports System.Collections.Generic

class C
    sub Test(list as IList(of string))
        [||]For i = 0 to list.Count - 1
            ' loop comment

            dim val = list(i)
            Console.WriteLine(list(i))
        next
    end sub
end class",
"imports System
imports System.Collections.Generic

class C
    sub Test(list as IList(of string))
        For Each val In list
            ' loop comment

            Console.WriteLine(val)
        next
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)>
        Public Async Function TestChooseNameFromDeclarationStatement_PreserveDirectives() As Task
            Await TestInRegularAndScript1Async(
"imports System
imports System.Collections.Generic

class C
    sub Test(list as IList(of string))
        [||]For i = 0 to list.Count - 1
#if true

            dim val = list(i)
            Console.WriteLine(list(i))

#end if
        next
    end sub
end class",
"imports System
imports System.Collections.Generic

class C
    sub Test(list as IList(of string))
        For Each val In list
#if true

            Console.WriteLine(val)

#end if
        next
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)>
        Public Async Function TestMissingIfVariableUsedNotForIndexing() As Task
            Await TestMissingInRegularAndScriptAsync(
"imports System

class C
    sub Test(array as string())
        [||]For i = 0 to array.Length - 1
            Console.WriteLine(i)
        next
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)>
        Public Async Function TestMissingIfVariableUsedForIndexingNonCollection() As Task
            Await TestMissingInRegularAndScriptAsync(
"imports System

class C
    sub Test(array as string())
        [||]For i = 0 to array.Length - 1
            Console.WriteLine(other(i))
        next
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)>
        Public Async Function TestWarningIfCollectionWrittenTo() As Task
            Await TestInRegularAndScript1Async(
"imports System

class C
    sub Test(array as string())
        [||]For i = 0 to array.Length - 1
            array(i) = 1
        next
    end sub
end class",
"imports System

class C
    sub Test(array as string())
        For Each {|Rename:v|} In array
            {|Warning:v|} = 1
        next
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)>
        Public Async Function TestDifferentIndexerAndEnumeratorType() As Task
            Await TestInRegularAndScriptAsync(
"imports System

class MyList
  default public readonly property Item(i as integer) as string

  public function GetEnumerator() as Enumerator
  end function

  public structure Enumerator
    public readonly property Current as object
  end structure
end class

class C
    sub Test(list as MyList)
        ' need to use 'string' here to preserve original index semantics.
        [||]For i = 0 to list.Length - 1
            Console.WriteLine(list(i))
        next
    end sub
end class",
"imports System

class MyList
  default public readonly property Item(i as integer) as string

  public function GetEnumerator() as Enumerator
  end function

  public structure Enumerator
    public readonly property Current as object
  end structure
end class

class C
    sub Test(list as MyList)
        ' need to use 'string' here to preserve original index semantics.
        For Each {|Rename:v|} As String In list
            Console.WriteLine(v)
        next
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)>
        Public Async Function TestSameIndexerAndEnumeratorType() As Task
            Await TestInRegularAndScriptAsync(
"imports System

class MyList
  default public readonly property Item(i as integer) as object

  public function GetEnumerator() as Enumerator
  end function

  public structure Enumerator
    public readonly property Current as object
  end structure
end class

class C
    sub Test(list as MyList)
        ' can omit type here since the type stayed the same.
        [||]For i = 0 to list.Count - 1
            Console.WriteLine(list(i))
        next
    end sub
end class",
"imports System

class MyList
  default public readonly property Item(i as integer) as object

  public function GetEnumerator() as Enumerator
  end function

  public structure Enumerator
    public readonly property Current as object
  end structure
end class

class C
    sub Test(list as MyList)
        ' can omit type here since the type stayed the same.
        For Each {|Rename:v|} In list
            Console.WriteLine(v)
        next
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)>
        Public Async Function TestTrivia() As Task
            Await TestInRegularAndScript1Async(
"imports System

class C
    sub Test(array as string())
        ' trivia 1
        [||]For i = 0 to array.Length - 1 ' trivia 2
            Console.WriteLine(array(i))
        next ' trivia 3
    end sub
end class",
"imports System

class C
    sub Test(array as string())
        ' trivia 1
        For Each {|Rename:v|} In array ' trivia 2
            Console.WriteLine(v)
        next ' trivia 3
    end sub
end class")
        End Function
    End Class
End Namespace
