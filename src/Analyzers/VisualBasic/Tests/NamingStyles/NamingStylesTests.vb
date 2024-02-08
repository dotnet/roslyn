' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeFixes.NamingStyles
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.NamingStyles
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.VisualBasic.Diagnostics.Analyzers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.NamingStyles
    <Trait(Traits.Feature, Traits.Features.NamingStyle)>
    Public Class NamingStylesTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest_NoEditor

        Private Shared ReadOnly s_options As NamingStylesTestOptionSets = New NamingStylesTestOptionSets(LanguageNames.VisualBasic)

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicNamingStyleDiagnosticAnalyzer(), New NamingStyleCodeFixProvider())
        End Function

        Protected Overrides Function GetComposition() As TestComposition
            Return MyBase.GetComposition().AddParts(GetType(TestSymbolRenamedCodeActionOperationFactoryWorkspaceService))
        End Function

        ' TODO: everything else apart from locals

        <Fact>
        Public Async Function TestCamelCaseParameters() As Task
            Await TestInRegularAndScriptAsync(
"module C
    sub M([|X|] as integer)
    end sub
end module",
"module C
    sub M(x as integer)
    end sub
end module",
                options:=s_options.ParameterNamesAreCamelCase)
        End Function

        <Fact>
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
                options:=s_options.LocalNamesAreCamelCase)
        End Function

        <Fact>
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
                options:=s_options.LocalNamesAreCamelCase)
        End Function

        <Fact>
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
                options:=s_options.LocalNamesAreCamelCase)
        End Function

        <Fact>
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
                options:=s_options.LocalNamesAreCamelCase)
        End Function

        <Fact>
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
                options:=s_options.LocalNamesAreCamelCase)
        End Function

        <Fact>
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
                options:=s_options.LocalNamesAreCamelCase)
        End Function

        <Fact>
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
                options:=s_options.LocalNamesAreCamelCase)
        End Function

        <Fact(Skip:="Implicit declarations cannot be found by syntax. Requires https://github.com/dotnet/roslyn/issues/14061")>
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
                options:=s_options.LocalNamesAreCamelCase)
        End Function

        <Fact>
        Public Async Function TestCamelCaseLocals_ForVariable1_NotWhenDeclaredPreviously() As Task
            Await TestMissingInRegularAndScriptAsync(
"module C
    sub M()
        dim I as integer
        for [|I|] = 1 to 10
        next
    end sub
end module", New TestParameters(options:=s_options.LocalNamesAreCamelCase))
        End Function

        <Fact>
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
                options:=s_options.LocalNamesAreCamelCase)
        End Function

        <Fact(Skip:="Implicit declarations cannot be found by syntax. Requires https://github.com/dotnet/roslyn/issues/14061")>
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
                options:=s_options.LocalNamesAreCamelCase)
        End Function

        <Fact>
        Public Async Function TestCamelCaseLocals_ForEachVariable1_NotWhenDeclaredPreviously() As Task
            Await TestMissingInRegularAndScriptAsync(
"module C
    sub M()
        dim X
        for each [|X|] in {}
        next
    end sub
end module", New TestParameters(options:=s_options.LocalNamesAreCamelCase))
        End Function

        <Fact>
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
                options:=s_options.LocalNamesAreCamelCase)
        End Function

        <Fact>
        Public Async Function TestCamelCaseLocals_CatchVariable() As Task
            Await TestInRegularAndScriptAsync(
"imports System
module C
    sub M()
        try
        catch [|Exception|] as Exception
        end try
    end sub
end module",
"imports System
module C
    sub M()
        try
        catch exception as Exception
        end try
    end sub
end module",
                options:=s_options.LocalNamesAreCamelCase)
        End Function

        <Fact>
        Public Async Function TestCamelCaseLocals_CatchWithoutDeclarationIgnored() As Task
            Await TestMissingInRegularAndScriptAsync(
"imports System
module C
    sub M()
        try
        [|catch|]
        end try
    end sub
end module", New TestParameters(options:=s_options.LocalNamesAreCamelCase))
        End Function

        <Fact(Skip:="Implicit declarations cannot be found by syntax. Requires https://github.com/dotnet/roslyn/issues/14061")>
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
                options:=s_options.LocalNamesAreCamelCase)
        End Function

        <Fact>
        Public Async Function TestCamelCaseLocals_ImplicitlyDeclaredLocal_NotOnSecondUse() As Task
            Await TestMissingInRegularAndScriptAsync(
"module C
    sub M()
        Value = 0
        System.Console.WriteLine([|Value|])
    end sub
end module", New TestParameters(options:=s_options.LocalNamesAreCamelCase))
        End Function

        <Fact>
        Public Async Function TestCamelCaseLocals_NotWhenLocalDeclaredPreviously() As Task
            Await TestMissingInRegularAndScriptAsync(
"module C
    sub M()
        dim Value as integer
        [|Value|] = 0
        System.Console.WriteLine(Value)
    end sub
end module", New TestParameters(options:=s_options.LocalNamesAreCamelCase))
        End Function

        <Fact>
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
end module", New TestParameters(options:=s_options.LocalNamesAreCamelCase))
        End Function

        <Fact>
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
end module", New TestParameters(options:=s_options.LocalNamesAreCamelCase))
        End Function

        <Fact>
        Public Async Function TestCamelCaseLocals_ParameterIgnored() As Task
            Await TestMissingInRegularAndScriptAsync(
"module C
    sub M([|X|] as integer)
    end sub
end module", New TestParameters(options:=s_options.LocalNamesAreCamelCase))
        End Function

        <Fact>
        Public Async Function TestCamelCaseLocals_TupleTypeElementNameIgnored1() As Task
            Await TestMissingInRegularAndScriptAsync(
"module C
    sub M()
        dim tuple as ([|A|] as integer, B as string)
    end sub
end module", New TestParameters(options:=s_options.LocalNamesAreCamelCase))
        End Function

        <Fact>
        Public Async Function TestCamelCaseLocals_TupleTypeElementNameIgnored2() As Task
            Await TestMissingInRegularAndScriptAsync(
"module C
    sub M()
        dim tuple as (A as integer, ([|B|] as string, C as string)) = (0, (string.Empty, string.Empty))
    end sub
end module", New TestParameters(options:=s_options.LocalNamesAreCamelCase))
        End Function

        <Fact>
        Public Async Function TestCamelCaseLocals_TupleExpressionElementNameIgnored() As Task
            Await TestMissingInRegularAndScriptAsync(
"module C
    sub M()
        dim tuple = ([|A|]:=0, B:=0)
    end sub
end module", New TestParameters(options:=s_options.LocalNamesAreCamelCase))
        End Function

        <Fact>
        Public Async Function TestUpperCaseConstants_ConstField() As Task
            Await TestInRegularAndScriptAsync(
"module C
    const [|field|] = 0
end module",
"module C
    const FIELD = 0
end module",
                options:=s_options.ConstantsAreUpperCase)
        End Function

        <Fact>
        Public Async Function TestUpperCaseConstants_ConstLocal() As Task
            Await TestInRegularAndScriptAsync(
"module C
    sub M()
        const local1 = 0, [|local2|] as integer = 0
    end sub
end module",
"module C
    sub M()
        const local1 = 0, LOCAL2 as integer = 0
    end sub
end module",
                options:=s_options.ConstantsAreUpperCase)
        End Function

        <Fact>
        Public Async Function TestUpperCaseConstants_NonConstFieldIgnored() As Task
            Await TestMissingInRegularAndScriptAsync(
"module C
    readonly [|field|] = 0
end module", New TestParameters(options:=s_options.ConstantsAreUpperCase))
        End Function

        <Fact>
        Public Async Function TestUpperCaseConstants_NonConstLocalIgnored() As Task
            Await TestMissingInRegularAndScriptAsync(
"module C
    sub M()
        dim local1 = 0, [|local2|] as integer = 0
    end sub
end module", New TestParameters(options:=s_options.ConstantsAreUpperCase))
        End Function

        <Fact>
        Public Async Function TestCamelCaseLocalsUpperCaseConstants_ConstLocal() As Task
            Await TestInRegularAndScriptAsync(
"module C
    sub M()
        const [|PascalCase|] = 0
    end sub
end module",
"module C
    sub M()
        const PASCALCASE = 0
    end sub
end module",
                options:=s_options.LocalsAreCamelCaseConstantsAreUpperCase)
        End Function

        <Fact>
        Public Async Function TestCamelCaseLocalsUpperCaseConstants_NonConstLocal() As Task
            Await TestInRegularAndScriptAsync(
"module C
    sub M()
        dim [|PascalCase|] = 0
    end sub
end module",
"module C
    sub M()
        dim pascalCase = 0
    end sub
end module",
                options:=s_options.LocalsAreCamelCaseConstantsAreUpperCase)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38513")>
        Public Async Function TestInterfaceNamesStartWithI() As Task
            Await TestInRegularAndScriptAsync(
"Interface [|test|]
End Interface",
"Interface ITest
End Interface",
                options:=s_options.InterfaceNamesStartWithI)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38513")>
        Public Async Function TestTypeParameterNamesStartWithT() As Task
            Await TestInRegularAndScriptAsync(
"Public Class classHolder(Of [|type|])
End Class",
"Public Class classHolder(Of TType)
End Class",
                options:=s_options.TypeParameterNamesStartWithT)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51727")>
        Public Async Function TestExternMethod() As Task
            Await TestMissingInRegularAndScriptAsync(
"Public Class C
    Declare Sub [|some_p_invoke|] Lib ""some""()
End Class",
                New TestParameters(options:=s_options.MethodNamesArePascalCase))
        End Function
    End Class
End Namespace
