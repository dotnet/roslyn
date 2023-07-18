' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeFixVerifier(Of
    Microsoft.CodeAnalysis.UseExplicitTupleName.UseExplicitTupleNameDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.UseExplicitTupleName.UseExplicitTupleNameCodeFixProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.UseExplicitTupleName
    <Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTupleName)>
    Public Class UseExplicitTupleNameTests
        <Fact>
        Public Async Function TestNamedTuple1() As Task
            Await VerifyVB.VerifyCodeFixAsync("
class C
    Sub M()
        dim v1 as (i as integer, s as string)
        dim v2 = v1.[|Item1|]
    end sub
end class", "
class C
    Sub M()
        dim v1 as (i as integer, s as string)
        dim v2 = v1.i
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestInArgument() As Task
            Await VerifyVB.VerifyCodeFixAsync("
class C
    Sub M()
        dim v1 as (i as integer, s as string)
        Goo(v1.[|Item1|])
    end sub

    Sub Goo(i as integer)
    end sub
end class", "
class C
    Sub M()
        dim v1 as (i as integer, s as string)
        Goo(v1.i)
    end sub

    Sub Goo(i as integer)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestNamedTuple2() As Task
            Await VerifyVB.VerifyCodeFixAsync("
class C
    Sub M()
        dim v1 as (i as integer, s as string)
        dim v2 = v1.[|Item2|]
    end sub
end class", "
class C
    Sub M()
        dim v1 as (i as integer, s as string)
        dim v2 = v1.s
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestMissingOnMatchingName1() As Task
            Dim code = "
class C
    Sub M()
        dim v1 as (integer, s as string)
        dim v2 = v1.Item1
    end sub
end class"

            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact>
        Public Async Function TestMissingOnMatchingName2() As Task
            Dim code = "
class C
    Sub M()
        dim v1 as (Item1 as integer, s as string)
        dim v2 = v1.Item1
    end sub
end class"

            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact>
        Public Async Function TestWrongCasing() As Task
            Dim code = "
class C
    Sub M()
        dim v1 as (item1 as integer, s as string)
        dim v2 = v1.Item1
    end sub
end class"

            Await VerifyVB.VerifyCodeFixAsync(code, code)
        End Function

        <Fact>
        Public Async Function TestFixAll1() As Task
            Await VerifyVB.VerifyCodeFixAsync("
class C
    Sub M()
        dim v1 as (i as integer, s as string)
        dim v2 = v1.[|Item1|]
        dim v3 = v1.[|Item2|]
    end sub
end class", "
class C
    Sub M()
        dim v1 as (i as integer, s as string)
        dim v2 = v1.i
        dim v3 = v1.s
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestFixAll2() As Task
            Await VerifyVB.VerifyCodeFixAsync("
class C
    Sub M()
        dim v1 as (i as integer, s as integer) 
        v1.[|Item1|] = v1.[|Item2|]
    end sub
end class", "
class C
    Sub M()
        dim v1 as (i as integer, s as integer) 
        v1.i = v1.s
    end sub
end class")
        End Function
    End Class
End Namespace
