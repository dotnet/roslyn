' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.MoveDeclarationNearReference

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.MoveDeclarationNearReference
    <Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)>
    Public Class MoveDeclarationNearReferenceTests
        Inherits AbstractVisualBasicCodeActionTest_NoEditor

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As TestWorkspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicMoveDeclarationNearReferenceCodeRefactoringProvider()
        End Function

        <Fact>
        Public Async Function TestMove1() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M()
        dim [||]x as integer
        if true
            Console.WriteLine(x)
        end if
    end sub
end class",
"class C
    sub M()
        if true
            dim x as integer
            Console.WriteLine(x)
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestMove2() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M()
        dim [||]x as integer
        Console.WriteLine()
        Console.WriteLine(x)
    end sub
end class",
"class C
    sub M()
        Console.WriteLine()
        dim x as integer
        Console.WriteLine(x)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestMove3() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M()

        dim [||]x as integer
        Console.WriteLine()
        if true
            Console.WriteLine(x)
        end if

        if true
            Console.WriteLine(x)
        end if
    end sub
end class",
"class C
    sub M()
        Console.WriteLine()

        dim x as integer
        if true
            Console.WriteLine(x)
        end if

        if true
            Console.WriteLine(x)
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestMove4() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M()
        dim [||]x as integer
        Console.WriteLine()
        if true
            Console.WriteLine(x)
        end if
    end sub
end class",
"class C
    sub M()
        Console.WriteLine()
        if true
            dim x as integer
            Console.WriteLine(x)
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestAssign1() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M()
        dim [||]x as integer
        if true
            x = 5
            Console.WriteLine(x)
        end if
    end sub
end class",
"class C
    sub M()
        if true
            dim x as integer = 5
            Console.WriteLine(x)
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestAssign2() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M()
        dim [||]x as integer = 0
        if true
            x = 5
            Console.WriteLine(x)
        end if
    end sub
end class",
"class C
    sub M()
        if true
            dim x as integer = 5
            Console.WriteLine(x)
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestAssign3() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M()
        dim [||]x = ctype(0, integer)
        if true
            x = 5
            Console.WriteLine(x)
        end if
    end sub
end class",
"class C
    sub M()
        if true
            dim x = ctype(0, integer)
            x = 5
            Console.WriteLine(x)
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestMissing1() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M()
        dim [||]x as integer
        Console.WriteLine(x)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestMissingWhenReferencedInDeclaration() As Task
            Await TestMissingInRegularAndScriptAsync(
"class Program
    sub M()
        dim [||]x as object() = { x }
        x.ToString()
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestMissingWhenInDeclarationGroup() As Task
            Await TestMissingInRegularAndScriptAsync(
"class Program
    sub M()
        dim [||]i as integer = 5
        dim j as integer = 10
        Console.WriteLine(i)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestWarnOnChangingScopes1() As Task
            Await TestInRegularAndScriptAsync(
"imports System.Linq

class Program
    sub M()
        dim [||]gate = new object()
        dim x = sub()
                    Console.WriteLine(gate)
                end sub()
    end sub
end class",
"imports System.Linq

class Program
    sub M()
        dim x = sub()
                    {|Warning:dim gate = new object()|}
                    Console.WriteLine(gate)
                end sub()
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestWarnOnChangingScopes2() As Task
            Await TestAsync(
"using System
using System.Linq

class Program
    sub M()
        dim [||]i = 0
        for each (v in x)
            Console.Write(i)
            i = i + 1
        next
    end sub
end class",
"using System
using System.Linq

class Program
    sub M()
        for each (v in x)
        {|Warning:dim i = CInt(0)|}
            Console.Write(i)
            i = i + 1
        next
    end sub
end class", parseOptions:=Nothing)
        End Function

        <Fact>
        Public Async Function MissingIfNotInDeclarationSpan() As Task
            Await TestMissingInRegularAndScriptAsync(
"using System
using System.Collections.Generic
using System.Linq

class Program
    sub M()
        ' Comment [||]about goo!
        ' Comment about goo!
        ' Comment about goo!
        ' Comment about goo!
        ' Comment about goo!
        ' Comment about goo!
        ' Comment about goo!
        dim goo = 0
        Console.WriteLine()
        Console.WriteLine(goo)
    end sub
end class")
        End Function
    End Class
End Namespace
