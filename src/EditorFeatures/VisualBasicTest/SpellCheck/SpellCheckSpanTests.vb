' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.SpellCheck
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SpellCheck
    <UseExportProvider>
    Public Class SpellCheckSpanTests
        Inherits AbstractSpellCheckSpanTests

        Protected Overrides Function CreateWorkspace(content As String) As TestWorkspace
            Return TestWorkspace.CreateVisualBasic(content)
        End Function

        <Fact>
        Public Async Function TestComment1() As Task
            Await TestAsync("'{|Comment: Goo |}")
        End Function

        <Fact>
        Public Async Function TestComment2() As Task
            Await TestAsync("
'{|Comment: Goo |}")
        End Function

        <Fact>
        Public Async Function TestDocComment1() As Task
            Await TestAsync("
'''{|Comment:goo bar Comment:baz|}
class {|Identifier:C|}
end class")
        End Function

        <Fact>
        Public Async Function TestDocComment2() As Task
            Await TestAsync("
'''{|Comment: |}<summary>{|Comment: goo bar baz |}</summary>
class {|Identifier:C|}
end class")
        End Function

        <Fact>
        Public Async Function TestString1() As Task
            Await TestAsync("
dim {|Identifier:x|} = ""{|String: goo |}""")
        End Function

        <Fact>
        Public Async Function TestString2() As Task
            Await TestAsync("
dim {|Identifier:x|} = ""{|String: goo |}")
        End Function

        <Fact>
        Public Async Function TestString3() As Task
            Await TestAsync("
dim {|Identifier:x|} = ""{|String:
    goo
|}""")
        End Function

        <Fact>
        Public Async Function TestString4() As Task
            Await TestAsync("
dim {|Identifier:x|} = ""{|String:
    goo
|}")
        End Function
    End Class
End Namespace
