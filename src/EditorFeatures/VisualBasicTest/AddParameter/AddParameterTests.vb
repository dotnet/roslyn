' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.AddParameter

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.AddParameter
    Public Class AddParameterTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (Nothing, New VisualBasicAddParameterCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)>
        Public Async Function TestMissingWithImplicitConstructor() As Task
            Await TestMissingAsync(
"
class C
end class

class D
    sub M()
        dim a = new [|C|](1)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)>
        Public Async Function TestOnEmptyConstructor() As Task
            Await TestInRegularAndScriptAsync(
"
class C
    public sub new()
    end sub
end class

class D
    sub M()
        dim a = new C([|1|])
    end sub
end class",
"
class C
    public sub new(v As Integer)
    end sub
end class

class D
    sub M()
        dim a = new C(1)
    end sub
end class")
        End Function

        <WorkItem(20973, "https://github.com/dotnet/roslyn/issues/20973")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)>
        Public Async Function TestNothingArgument1() As Task
            Await TestInRegularAndScriptAsync(
"
class C
    public sub new(i as integer)
    end sub
end class

class D
    sub M()
        dim a = new C(nothing, [|1|])
    end sub
end class",
"
class C
    public sub new(i as integer, v As Integer)
    end sub
end class

class D
    sub M()
        dim a = new C(nothing, 1)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)>
        Public Async Function TestNamedArg() As Task
            Await TestInRegularAndScriptAsync(
"
class C
    public sub new()
    end sub
end class

class D
    sub M()
        dim a = new C([|p|]:=1)
    end sub
end class",
"
class C
    public sub new(p As Integer)
    end sub
end class

class D
    sub M()
        dim a = new C(p:=1)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)>
        Public Async Function TestMissingWithConstructorWithSameNumberOfParams() As Task
            Await TestMissingAsync(
"
class C
    public sub new(b As Boolean)
    end sub
end class

class D
    sub M()
        dim a = new [|C|](1)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)>
        Public Async Function TestAddBeforeMatchingArg() As Task
            Await TestInRegularAndScriptAsync(
"
class C
    public sub new(i as integer)
    end sub
end class

class D
    sub M()
        dim a = new C(true, [|1|])
    end sub
end class
",
"
class C
    public sub new(v As Boolean, i as integer)
    end sub
end class

class D
    sub M()
        dim a = new C(true, 1)
    end sub
end class
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)>
        Public Async Function TestAddAfterMatchingConstructorParam() As Task
            Await TestInRegularAndScriptAsync(
"
class C
    public sub new(i as integer)
    end sub
end class

class D
    sub M()
        dim a = new C(1, [|true|])
    end sub
end class
",
"
class C
    public sub new(i as integer, v As Boolean)
    end sub
end class

class D
    sub M()
        dim a = new C(1, true)
    end sub
end class
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)>
        Public Async Function TestParams1() As Task
            Await TestInRegularAndScriptAsync(
"
option strict on
class C
    public sub new(paramarray i as integer())
    end sub
end class

class D
    sub M()
        dim a = new C([|true|], 1)
    end sub
end class
",
"
option strict on
class C
    public sub new(v As Boolean, paramarray i as integer())
    end sub
end class

class D
    sub M()
        dim a = new C(true, 1)
    end sub
end class
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)>
        Public Async Function TestParams2() As Task
            Await TestMissingAsync(
"
class C
    public sub new(paramarray i as integer())
    end sub
end class

class D
    sub M()
        dim a = new [|C|](1, true)
    end sub
end class
")
        End Function

        <WorkItem(20708, "https://github.com/dotnet/roslyn/issues/20708")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)>
        Public Async Function TestMultiLineParameters1() As Task
            Await TestInRegularAndScriptAsync(
"
class C
    public sub new(i as integer,
                   j as integer)
    end sub

    private sub Goo()
        dim x = new C(true, 0, [|0|])
    end sub
end class",
"
class C
    public sub new(v As Boolean,
                   i as integer,
                   j as integer)
    end sub

    private sub Goo()
        dim x = new C(true, 0, 0)
    end sub
end class")
        End Function

        <WorkItem(20708, "https://github.com/dotnet/roslyn/issues/20708")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)>
        Public Async Function TestMultiLineParameters2() As Task
            Await TestInRegularAndScriptAsync(
"
class C
    public sub new(i as integer,
                   j as integer)
    end sub

    private sub Goo()
        dim x = new C(0, true, [|0|])
    end sub
end class",
"
class C
    public sub new(i as integer,
                   v As Boolean,
                   j as integer)
    end sub

    private sub Goo()
        dim x = new C(0, true, 0)
    end sub
end class")
        End Function

        <WorkItem(20708, "https://github.com/dotnet/roslyn/issues/20708")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)>
        Public Async Function TestMultiLineParameters3() As Task
            Await TestInRegularAndScriptAsync(
"
class C
    public sub new(i as integer,
                   j as integer)
    end sub

    private sub Goo()
        dim x = new C(0, 0, [|true|])
    end sub
end class",
"
class C
    public sub new(i as integer,
                   j as integer,
                   v As Boolean)
    end sub

    private sub Goo()
        dim x = new C(0, 0, true)
    end sub
end class")
        End Function

        <WorkItem(20708, "https://github.com/dotnet/roslyn/issues/20708")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)>
        Public Async Function TestMultiLineParameters4() As Task
            Await TestInRegularAndScriptAsync(
"
class C
    public sub new(
        i as integer,
        j as integer)
    end sub

    private sub Goo()
        dim x = new C(true, 0, [|0|])
    end sub
end class",
"
class C
    public sub new(
        v As Boolean,
        i as integer,
        j as integer)
    end sub

    private sub Goo()
        dim x = new C(true, 0, 0)
    end sub
end class")
        End Function

        <WorkItem(20708, "https://github.com/dotnet/roslyn/issues/20708")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)>
        Public Async Function TestMultiLineParameters5() As Task
            Await TestInRegularAndScriptAsync(
"
class C
    public sub new(
        i as integer,
        j as integer)
    end sub

    private sub Goo()
        dim x = new C(0, true, [|0|])
    end sub
end class",
"
class C
    public sub new(
        i as integer,
        v As Boolean,
        j as integer)
    end sub

    private sub Goo()
        dim x = new C(0, true, 0)
    end sub
end class")
        End Function

        <WorkItem(20708, "https://github.com/dotnet/roslyn/issues/20708")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)>
        Public Async Function TestMultiLineParameters6() As Task
            Await TestInRegularAndScriptAsync(
"
class C
    public sub new(
        i as integer,
        j as integer)
    end sub

    private sub Goo()
        dim x = new C(0, 0, [|true|])
    end sub
end class",
"
class C
    public sub new(
        i as integer,
        j as integer,
        v As Boolean)
    end sub

    private sub Goo()
        dim x = new C(0, 0, true)
    end sub
end class")
        End Function
    End Class
End Namespace
