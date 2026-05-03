' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.SpellCheck

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SpellCheck
    <UseExportProvider>
    Public Class SpellCheckSpanTests
        Inherits AbstractSpellCheckSpanTests

        Protected Overrides Function CreateWorkspace(content As String) As EditorTestWorkspace
            Return EditorTestWorkspace.CreateVisualBasic(content)
        End Function

        <Fact>
        Public Async Function TestComment1() As Task
            Await TestAsync("{|Comment:' Goo |}")
        End Function

        <Fact>
        Public Async Function TestComment2() As Task
            Await TestAsync("
{|Comment:' Goo |}")
        End Function

        <Fact>
        Public Async Function TestDocComment1() As Task
            Await TestAsync("
'''{|Comment:goo bar baz|}
class {|Identifier:C|}
end class")
        End Function

        <Fact>
        Public Async Function TestDocComment2() As Task
            Await TestAsync("
'''{|Comment:goo bar baz|}
'''{|Comment:goo bar baz|}
class {|Identifier:C|}
end class")
        End Function

        <Fact>
        Public Async Function TestDocComment3() As Task
            Await TestAsync("
'''{|Comment: |}<summary>{|Comment: goo bar baz |}</summary>
class {|Identifier:C|}
end class")
        End Function

        <Fact>
        Public Async Function TestString1() As Task
            Await TestAsync("
dim {|Identifier:x|} = {|String:"" goo ""|}")
        End Function

        <Fact>
        Public Async Function TestString2() As Task
            Await TestAsync("
dim {|Identifier:x|} = "" goo ")
        End Function

        <Fact>
        Public Async Function TestString3() As Task
            Await TestAsync("
dim {|Identifier:x|} = {|String:""
    goo
""|}")
        End Function

        <Fact>
        Public Async Function TestString4() As Task
            Await TestAsync("
dim {|Identifier:x|} = ""
    goo
")
        End Function

        <Fact>
        Public Async Function TestString5() As Task
            Await TestAsync("
dim {|Identifier:x|} = $""{|String: goo |}""")
        End Function

        <Fact>
        Public Async Function TestString6() As Task
            Await TestAsync("
dim {|Identifier:x|} = $""{|String:
    goo
|}""")
        End Function

        <Fact>
        Public Async Function TestString7() As Task
            Await TestAsync("
dim {|Identifier:x|} = $""{|String: goo |}{0}{|String: bar |}""")
        End Function

        <Fact>
        Public Async Function TestIdentifier1() As Task

            Await TestAsync("
class {|Identifier:C|}
end class")
        End Function

        <Fact>
        Public Async Function TestIdentifier4() As Task

            Await TestAsync("
delegate sub {|Identifier:C|}()")
        End Function

        <Fact>
        Public Async Function TestIdentifier5() As Task

            Await TestAsync("
enum {|Identifier:C|}
end enum")
        End Function

        <Fact>
        Public Async Function TestIdentifier6() As Task

            Await TestAsync("
enum {|Identifier:C|}
    {|Identifier:D|}
end enum")
        End Function

        <Fact>
        Public Async Function TestIdentifier7() As Task

            Await TestAsync("
enum {|Identifier:C|}
    {|Identifier:D|}
    {|Identifier:E|}
end enum")
        End Function

        <Fact>
        Public Async Function TestIdentifier8() As Task

            Await TestAsync("
interface {|Identifier:C|}
end interface")
        End Function

        <Fact>
        Public Async Function TestIdentifier9() As Task

            Await TestAsync("
structure {|Identifier:C|}
end structure")
        End Function

        <Fact>
        Public Async Function TestIdentifier11() As Task

            Await TestAsync("
class {|Identifier:C|}(of {|Identifier:T|}) { }")
        End Function

        <Fact>
        Public Async Function TestIdentifier12() As Task

            Await TestAsync("
class {|Identifier:C|}
    dim {|Identifier:X|} as integer
end class")
        End Function

        <Fact>
        Public Async Function TestIdentifier13() As Task

            Await TestAsync("
class {|Identifier:C|}
    dim {|Identifier:X|}, {|Identifier:Y|} as integer
end class")
        End Function

        <Fact>
        Public Async Function TestIdentifier13b() As Task

            Await TestAsync("
class {|Identifier:C|}
    dim {|Identifier:X|} as integer, {|Identifier:Y|} as boolean
end class")
        End Function

        <Fact>
        Public Async Function TestIdentifier16() As Task

            Await TestAsync("
class {|Identifier:C|}
    private property {|Identifier:X|} as integer
end class")
        End Function

        <Fact>
        Public Async Function TestIdentifier19() As Task

            Await TestAsync("
class {|Identifier:C|}
{
    private event {|Identifier:X|} as Action
        add
        end add
        remove
        end remove
    end event
end class")
        End Function

        <Fact>
        Public Async Function TestIdentifier20() As Task

            Await TestAsync("
class {|Identifier:C|}
{
    sub {|Identifier:D|}()
        dim {|Identifier:E|} as integer
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestIdentifier21() As Task

            Await TestAsync("
class {|Identifier:C|}
    sub {|Identifier:D|}()
        dim {|Identifier:E|}, {|Identifier:F|} as integer
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestIdentifier21b() As Task

            Await TestAsync("
class {|Identifier:C|}
    sub {|Identifier:D|}()
        dim {|Identifier:E|} as integer, {|Identifier:F|} as boolean
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestIdentifier22() As Task

            Await TestAsync("
class {|Identifier:C|}
    sub {|Identifier:D|}()
{|Identifier:E|}:
        return
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestIdentifier23() As Task

            Await TestAsync("
class {|Identifier:C|}
    sub {|Identifier:D|}({|Identifier:E|} as integer)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestIdentifier24() As Task

            Await TestAsync("
class {|Identifier:C|}
    sub {|Identifier:D|}({|Identifier:E|} as integer)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestIdentifier25() As Task

            Await TestAsync("
class {|Identifier:C|}
    sub {|Identifier:D|}({|Identifier:E|} as integer, {|Identifier:F|} as integer)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestIdentifier26() As Task

            Await TestAsync("
module {|Identifier:C|}
    sub {|Identifier:D|}({|Identifier:E|} as integer)
    end sub
end module")
        End Function

        <Fact>
        Public Async Function TestIdentifier27() As Task

            Await TestAsync("
namespace {|Identifier:C|}
end namespace")
        End Function

        <Fact>
        Public Async Function TestIdentifier28() As Task

            Await TestAsync("
namespace {|Identifier:C|}.{|Identifier:D|}
end namespace")
        End Function
    End Class
End Namespace
