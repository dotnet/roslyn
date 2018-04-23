' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeFixes.NamingStyles
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Diagnostics.Analyzers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.NamingStyles
    Partial Public Class NamingStylesTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicNamingStyleDiagnosticAnalyzer(), New NamingStyleCodeFixProvider())
        End Function

        ' TODO: everything else apart from locals

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Async Function TestCamelCaseLocals_LocalDeclaration1() As Task
            Await TestInRegularAndScriptAsync(
"module C
    sub M()
        dim [|X|] = 0
    end sub
end module",
"module C
    sub M()
        dim x = 0
    end sub
end module",
                options:=LocalNamesAreCamelCase)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Async Function TestCamelCaseLocals_LocalDeclaration2() As Task
            Await TestInRegularAndScriptAsync(
"module C
    sub M()
        dim X as integer, [|Y|], Z as string
    end sub
end module",
"module C
    sub M()
        dim X as integer, y, Z as string
    end sub
end module",
                options:=LocalNamesAreCamelCase)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Async Function TestCamelCaseLocals_LocalDeclaration3() As Task
            Await TestInRegularAndScriptAsync(
"module C
    sub M()
        dim [|X|](0 to 4) as integer, Y as new object(), Z%? as integer
    end sub
end module",
"module C
    sub M()
        dim x(0 to 4) as integer, Y as new object(), Z%? as integer
    end sub
end module",
                options:=LocalNamesAreCamelCase)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Async Function TestCamelCaseLocals_LocalDeclaration4() As Task
            Await TestInRegularAndScriptAsync(
"module C
    sub M()
        dim X(0 to 4) as integer, [|Y|] as new object(), Z%? as integer
    end sub
end module",
"module C
    sub M()
        dim X(0 to 4) as integer, y as new object(), Z%? as integer
    end sub
end module",
                options:=LocalNamesAreCamelCase)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Async Function TestCamelCaseLocals_LocalDeclaration5() As Task
            Await TestInRegularAndScriptAsync(
"module C
    sub M()
        dim X(0 to 4) as integer, Y as new object(), [|Z%|]? as integer
    end sub
end module",
"module C
    sub M()
        dim X(0 to 4) as integer, Y as new object(), z%? as integer
    end sub
end module",
                options:=LocalNamesAreCamelCase)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Async Function TestCamelCaseLocals_UsingVariable1() As Task
            Await TestInRegularAndScriptAsync(
"module C
    sub M()
        using [|A|] = nothing
        end using
    end sub
end module",
"module C
    sub M()
        using a = nothing
        end using
    end sub
end module",
                options:=LocalNamesAreCamelCase)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Async Function TestCamelCaseLocals_UsingVariable2() As Task
            Await TestInRegularAndScriptAsync(
"module C
    sub M()
        using A = nothing, [|B|] as new object()
        end using
    end sub
end module",
"module C
    sub M()
        using A = nothing, b as new object()
        end using
    end sub
end module",
                options:=LocalNamesAreCamelCase)
        End Function

        <Fact(Skip:="Implicit declarations cannot be found by syntax. Requires https://github.com/dotnet/roslyn/issues/14061")>
        <Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Async Function TestCamelCaseLocals_ForVariable1_ImplicitlyDeclared() As Task
            Await TestInRegularAndScriptAsync(
"module C
    sub M()
        for [|I|] = 1 to 10
        next
    end sub
end module",
"module C
    sub M()
        for i = 1 to 10
        next
    end sub
end module",
                options:=LocalNamesAreCamelCase)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Async Function TestCamelCaseLocals_ForVariable1_NotWhenDeclaredPreviously() As Task
            Await TestMissingInRegularAndScriptAsync(
"module C
    sub M()
        dim I as integer
        for [|I|] = 1 to 10
        next
    end sub
end module", New TestParameters(options:=LocalNamesAreCamelCase))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Async Function TestCamelCaseLocals_ForVariable2() As Task
            Await TestInRegularAndScriptAsync(
"module C
    sub M()
        for [|I|]? as integer = 1 to 10
        next
    end sub
end module",
"module C
    sub M()
        for i? as integer = 1 to 10
        next
    end sub
end module",
                options:=LocalNamesAreCamelCase)
        End Function

        <Fact(Skip:="Implicit declarations cannot be found by syntax. Requires https://github.com/dotnet/roslyn/issues/14061")>
        <Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Async Function TestCamelCaseLocals_ForEachVariable1_ImplicitlyDeclared() As Task
            Await TestInRegularAndScriptAsync(
"module C
    sub M()
        for each [|X|] in {}
        next
    end sub
end module",
"module C
    sub M()
        for each x in {}
        next
    end sub
end module",
                options:=LocalNamesAreCamelCase)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Async Function TestCamelCaseLocals_ForEachVariable1_NotWhenDeclaredPreviously() As Task
            Await TestMissingInRegularAndScriptAsync(
"module C
    sub M()
        dim X
        for each [|X|] in {}
        next
    end sub
end module", New TestParameters(options:=LocalNamesAreCamelCase))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Async Function TestCamelCaseLocals_ForEachVariable2() As Task
            Await TestInRegularAndScriptAsync(
"module C
    sub M()
        for each [|X|]? as integer in {}
        next
    end sub
end module",
"module C
    sub M()
        for each x? as integer in {}
        next
    end sub
end module",
                options:=LocalNamesAreCamelCase)
        End Function

        <Fact(Skip:="Implicit declarations cannot be found by syntax. Requires https://github.com/dotnet/roslyn/issues/14061")>
        <Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Async Function TestCamelCaseLocals_ImplicitlyDeclaredLocal() As Task
            Await TestInRegularAndScriptAsync(
"module C
    sub M()
        [|Value|] = 0
        System.Console.WriteLine(Value)
    end sub
end module",
"module C
    sub M()
        value = 0
        System.Console.WriteLine(value)
    end sub
end module",
                options:=LocalNamesAreCamelCase)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Async Function TestCamelCaseLocals_ImplicitlyDeclaredLocal_NotOnSecondUse() As Task
            Await TestMissingInRegularAndScriptAsync(
"module C
    sub M()
        Value = 0
        System.Console.WriteLine([|Value|])
    end sub
end module", New TestParameters(options:=LocalNamesAreCamelCase))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Async Function TestCamelCaseLocals_NotWhenLocalDeclaredPreviously() As Task
            Await TestMissingInRegularAndScriptAsync(
"module C
    sub M()
        dim Value as integer
        [|Value|] = 0
        System.Console.WriteLine(Value)
    end sub
end module", New TestParameters(options:=LocalNamesAreCamelCase))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Async Function TestCamelCaseLocals_QueryFromClauseIgnored() As Task
            ' This is an IRangeVariableSymbol, not ILocalSymbol
            Await TestMissingInRegularAndScriptAsync(
"module C
    sub M()
        dim squares =
            from [|STR|] in {string.Empty}
            let Number = integer.Parse(STR)
            select Number * Number
    end sub
end module", New TestParameters(options:=LocalNamesAreCamelCase))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Async Function TestCamelCaseLocals_QueryLetClauseIgnored() As Task
            ' This is an IRangeVariableSymbol, not ILocalSymbol
            Await TestMissingInRegularAndScriptAsync(
"module C
    sub M()
        dim squares =
            from STR in {string.Empty}
            let [|Number|] = integer.Parse(STR)
            select Number * Number
    end sub
end module", New TestParameters(options:=LocalNamesAreCamelCase))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Async Function TestCamelCaseLocals_ParameterIgnored() As Task
            Await TestMissingInRegularAndScriptAsync(
"module C
    sub M([|X|] as integer)
    end sub
end module", New TestParameters(options:=LocalNamesAreCamelCase))
        End Function

    End Class
End Namespace
