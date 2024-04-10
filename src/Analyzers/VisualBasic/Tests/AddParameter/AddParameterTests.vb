' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.AddParameter

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.AddParameter
    <Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)>
    Public Class AddParameterTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest_NoEditor

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (Nothing, New VisualBasicAddParameterCodeFixProvider())
        End Function

        Protected Overrides Function MassageActions(actions As ImmutableArray(Of CodeAction)) As ImmutableArray(Of CodeAction)
            Return FlattenActions(actions)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23927")>
        Public Async Function TestMissingOnOmittedArgument() As Task
            Await TestMissingAsync(
"Public Module Module1
    Private Class C
        Public Sub New(Arg1 As Integer)
        End Sub

        Public Sub New(Arg1 As Integer, Arg2 As Integer)
        End Sub
    End Class

    Public Sub Main()
        Dim x = New [|C|](, 0)
    End Sub
End Module")
        End Function

        <Fact>
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

        <Fact>
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

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20973")>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20708")>
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

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20708")>
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

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20708")>
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

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20708")>
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

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20708")>
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

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20708")>
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

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")>
        Public Async Function TestInvocation_InstanceMethod1() As Task
            ' error BC30057: Too many arguments to 'Private Sub M1()'.
            Await TestInRegularAndScriptAsync(
"
Class C
    Private Sub M1()
    End Sub

    Private Sub M2()
        Dim i As Integer = 0
        M1([|i|])
    End Sub
End Class
",
"
Class C
    Private Sub M1(i As Integer)
    End Sub

    Private Sub M2()
        Dim i As Integer = 0
        M1(i)
    End Sub
End Class
")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")>
        Public Async Function TestInvocation_StaticMethod() As Task
            ' error BC30057: Too many arguments to 'Private Shared Sub M1()'.
            Await TestInRegularAndScriptAsync(
"
Friend Class C1
    Private Shared Sub M1()
    End Sub

    Private Sub M2()
        C1.M1([|1|])
    End Sub
End Class
",
"
Friend Class C1
    Private Shared Sub M1(v As Integer)
    End Sub

    Private Sub M2()
        C1.M1(1)
    End Sub
End Class
")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")>
        Public Async Function TestInvocation_ExtensionMethod() As Task
            ' error BC36582: Too many arguments to extension method 'Public Sub ExtensionM1()' defined in 'Extensions'.
            Await TestInRegularAndScriptAsync(
"
Imports System.Runtime.CompilerServices

Namespace N
    Module Extensions
        <Extension()>
        Public Sub ExtensionM1(o As Integer)
        End Sub
    End Module

    Class C1
        Private Sub M1()
        Dim i as Integer = 5
        i.ExtensionM1([|1|])
        End Sub
    End Class
End Namespace
",
"
Imports System.Runtime.CompilerServices

Namespace N
    Module Extensions
        <Extension()>
        Public Sub ExtensionM1(o As Integer, v As Integer)
        End Sub
    End Module

    Class C1
        Private Sub M1()
        Dim i as Integer = 5
        i.ExtensionM1(1)
        End Sub
    End Class
End Namespace
")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")>
        Public Async Function TestInvocationExtensionMethod_StaticInvocationStyle() As Task
            ' error BC30057: Too many arguments to 'Public Sub ExtensionM1(i As Integer)'.
            Await TestInRegularAndScriptAsync(
"
Namespace N
    Friend Module Extensions
        <System.Runtime.CompilerServices.ExtensionAttribute()>
        Public Sub ExtensionM1(i As Integer)
        End Sub
    End Module

    Friend Class C1
        Private Sub M1()
        	Extensions.ExtensionM1(5, [|1|])
        End Sub
    End Class
End Namespace
",
"
Namespace N
    Friend Module Extensions
        <System.Runtime.CompilerServices.ExtensionAttribute()>
        Public Sub ExtensionM1(i As Integer, v As Integer)
        End Sub
    End Module

    Friend Class C1
        Private Sub M1()
        	Extensions.ExtensionM1(5, 1)
        End Sub
    End Class
End Namespace
")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")>
        Public Async Function TestInvocationOverride() As Task
            ' error BC30516: Overload resolution failed because no accessible 'M1' accepts this number of arguments.
            Dim code =
"
Friend Class Base
    Protected Overridable Sub M1()
    End Sub
End Class
Friend Class C1
    Inherits Base

    Protected Overrides Sub M1()
    End Sub

    Private Sub M2()
        Me.[|M1|](1)
    End Sub
End Class
"
            Dim fixDeclarationOnly =
"
Friend Class Base
    Protected Overridable Sub M1()
    End Sub
End Class
Friend Class C1
    Inherits Base

    Protected Overrides Sub M1(v As Integer)
    End Sub

    Private Sub M2()
        Me.M1(1)
    End Sub
End Class
"
            Dim fixCascading =
"
Friend Class Base
    Protected Overridable Sub M1(v As Integer)
    End Sub
End Class
Friend Class C1
    Inherits Base

    Protected Overrides Sub M1(v As Integer)
    End Sub

    Private Sub M2()
        Me.M1(1)
    End Sub
End Class
"
            Await TestInRegularAndScriptAsync(code, fixDeclarationOnly, index:=0)
            Await TestInRegularAndScriptAsync(code, fixCascading, index:=1)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")>
        Public Async Function TestInvocationInterface() As Task
            ' error BC30057: Too many arguments to 'Public Sub M1()'.
            Dim code =
"
Friend Interface I1
    Sub M1()
End Interface

Friend Class C1
    Implements I1

    Public Sub M1() Implements I1.M1
    End Sub

    Private Sub M2()
        Me.M1([|1|])
    End Sub
End Class
"
            Dim fixDeclarationOnly =
"
Friend Interface I1
    Sub M1()
End Interface

Friend Class C1
    Implements I1

    Public Sub M1(v As Integer) Implements I1.M1
    End Sub

    Private Sub M2()
        Me.M1(1)
    End Sub
End Class
"
            Dim fixCascading =
"
Friend Interface I1
    Sub M1(v As Integer)
End Interface

Friend Class C1
    Implements I1

    Public Sub M1(v As Integer) Implements I1.M1
    End Sub

    Private Sub M2()
        Me.M1(1)
    End Sub
End Class
"
            Await TestInRegularAndScriptAsync(code, fixDeclarationOnly, index:=0)
            Await TestInRegularAndScriptAsync(code, fixCascading, index:=1)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")>
        Public Async Function TestInvocationRecursion() As Task
            ' error BC30057: Too many arguments to 'Private Sub M1()'.
            Dim code =
"
Friend Class C1
    Private Sub M1()
        Me.M1([|1|])
    End Sub
End Class"
            Dim fix =
"
Friend Class C1
    Private Sub M1(v As Integer)
        Me.M1(1)
    End Sub
End Class"
            Await TestInRegularAndScriptAsync(code, fix)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")>
        Public Async Function TestInvocationTuple1() As Task
            ' error BC30057: Too many arguments to 'Private Sub M1(t1 As (Integer, Integer))'.
            Dim code =
"
Friend Class C1
    Private Sub M1(t1 As (Integer, Integer))
        Me.M1((1,1), [|(1,""1"")|])
    End Sub
End Class"
            Dim fix =
"
Friend Class C1
    Private Sub M1(t1 As (Integer, Integer), value As (Integer, String))
        Me.M1((1,1), (1,""1""))
    End Sub
End Class"
            Await TestInRegularAndScriptAsync(code, fix)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")>
        Public Async Function TestInvocationTuple2() As Task
            ' error BC30057: Too many arguments to 'Private Sub M1(t1 As (Integer, Integer))'.
            Dim code =
"
Friend Class C1
    Private Sub M1(t1 As (Integer, Integer))
        Dim tup=(1, ""1"")
        Me.M1((1,1), [|tup|])
    End Sub
End Class"
            Dim fix =
"
Friend Class C1
    Private Sub M1(t1 As (Integer, Integer), tup As (Integer, String))
        Dim tup=(1, ""1"")
        Me.M1((1,1), tup)
    End Sub
End Class"
            Await TestInRegularAndScriptAsync(code, fix)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")>
        Public Async Function TestInvocationTuple3() As Task
            ' error BC30057: Too many arguments to 'Private Sub M1(t1 As (Integer, Integer))'.
            Dim code =
"
Friend Class C1
    Private Sub M1(t1 As (Integer, Integer))
        Dim tup=(i:=1, s:=""1"")
        Me.M1((1,1), [|tup|])
    End Sub
End Class"
            Dim fix =
"
Friend Class C1
    Private Sub M1(t1 As (Integer, Integer), tup As (i As Integer, s As String))
        Dim tup=(i:=1, s:=""1"")
        Me.M1((1,1), tup)
    End Sub
End Class"
            Await TestInRegularAndScriptAsync(code, fix)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")>
        Public Async Function TestInvocation_OverloadResolution_Missing() As Task
            ' error BC30311: Value of type 'Exception' cannot be converted to 'String'.
            Dim code =
"
Public Class C
    
    Public Sub M(i1 As String, i2 As String)
    End Sub
    Public Sub M(ex As System.Exception)
    End Sub

    Public Sub Test()
        M([|new System.Exception()|], 2)
    End Sub
End Class"
            Dim fix =
"
Public Class C
    
    Public Sub M(i1 As String, i2 As String)
    End Sub
    Public Sub M(ex As System.Exception, v As Integer)
    End Sub

    Public Sub Test()
        M([|new System.Exception()|], 2)
    End Sub
End Class"
            Await TestInRegularAndScriptAsync(code, fix)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")>
        Public Async Function TestInvocation_OverloadResolution_WrongNumberOfArguments() As Task
            ' error BC30516: Overload resolution failed because no accessible 'M' accepts this number of arguments.
            Dim code =
"
Public Class C
    
    Public Sub M(i1 As String, i2 As String, i3 As String)
    End Sub
    Public Sub M(ex As System.Exception)
    End Sub

    Public Sub Test()
        [|M|](new System.Exception(), 2)
    End Sub
End Class"
            Dim fix =
"
Public Class C
    
    Public Sub M(i1 As String, i2 As String, i3 As String)
    End Sub
    Public Sub M(ex As System.Exception, v As Integer)
    End Sub

    Public Sub Test()
        M(new System.Exception(), 2)
    End Sub
End Class"
            Await TestInRegularAndScriptAsync(code, fix)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")>
        Public Async Function TestInvocation_LambdaExpressionParameter() As Task
            ' error BC36625: Lambda expression cannot be converted to 'Integer' because 'Integer' is not a delegate type.
            Dim code =
"
Public Class C
    
    Public Sub M(i1 As Integer, i2 As Integer)
    End Sub
    Public Sub M(a As System.Action)
    End Sub

    Public Sub Test()
        M([|Sub() System.Console.Write(0)|], 1)
    End Sub
End Class"
            Dim fix =
"
Public Class C
    
    Public Sub M(i1 As Integer, i2 As Integer)
    End Sub
    Public Sub M(a As System.Action, v As Integer)
    End Sub

    Public Sub Test()
        M(Sub() System.Console.Write(0), 1)
    End Sub
End Class"
            Await TestInRegularAndScriptAsync(code, fix)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")>
        Public Async Function TestInvocation_NamedParameter() As Task
            ' error BC30272: 'i2' is not a parameter of 'Private Sub M1(i1 As Integer)'.
            Dim code =
"
Friend Class C1
    Private Sub M1(i1 As Integer)
    End Sub

    Private Sub M2()
        Me.M1([|i2|]:=2, i1:=1)
    End Sub
End Class"
            Dim fix =
"
Friend Class C1
    Private Sub M1(i1 As Integer, i2 As Integer)
    End Sub

    Private Sub M2()
        Me.M1(i2:=2, i1:=1)
    End Sub
End Class"
            Await TestInRegularAndScriptAsync(code, fix)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")>
        Public Async Function TestInvocation_AddParameterToMethodWithParams() As Task
            ' error BC30311: Value of type 'String' cannot be converted to 'Exception'.
            Dim code =
"
Friend Class C1
    Private Shared Sub M1(ParamArray exceptions As System.Exception())
    End Sub

    Private Shared Sub M2()
        M1([|""Test""|], New System.Exception())
    End Sub
End Class"
            Dim fix =
"
Friend Class C1
    Private Shared Sub M1(v As String, ParamArray exceptions As System.Exception())
    End Sub

    Private Shared Sub M2()
        M1(""Test"", New System.Exception())
    End Sub
End Class"
            Await TestInRegularAndScriptAsync(code, fix)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")>
        Public Async Function TestInvocation_PartialMethodsInOneDocument() As Task
            ' error BC30057: Too many arguments to 'Private Sub PartialM()'.
            Dim code =
"
Namespace N1
	Partial Class C1
    	Private Partial Sub PartialM()
        End Sub
	End Class
End Namespace

Namespace N1
	Partial Class C1
		Private Sub PartialM()
		End Sub
        Private Sub M1()
            Me.PartialM([|1|])
        End Sub
    End Class
End Namespace"
            Dim fix =
"
Namespace N1
	Partial Class C1
    	Private Partial Sub PartialM(v As Integer)
        End Sub
	End Class
End Namespace

Namespace N1
	Partial Class C1
		Private Sub PartialM(v As Integer)
		End Sub
        Private Sub M1()
            Me.PartialM(1)
        End Sub
    End Class
End Namespace"
            Await TestInRegularAndScriptAsync(code, fix)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")>
        Public Async Function TestInvocation_InvocationStyles_Positional_WithOptionalParam() As Task
            ' error BC30057: Too many arguments to 'Private Sub M([i As Integer = 1])'.
            Dim code =
"
Friend Class C
    Private Sub M(Optional i As Integer=1)
    End Sub

    Private Sub Test()
        Me.M(1, [|2|])
    End Sub
End Class"
            Dim fix =
"
Friend Class C
    Private Sub M(Optional i As Integer=1, Optional v As Integer = Nothing)
    End Sub

    Private Sub Test()
        Me.M(1, 2)
    End Sub
End Class"
            Await TestInRegularAndScriptAsync(code, fix)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")>
        Public Async Function TestInvocation_InvocationStyles_Named_WithOptionalParam() As Task
            ' error BC30272: 'i3' is not a parameter of 'Private Sub M(i1 As Integer, [i2 As Integer = 2])'.
            Dim code =
"
Friend Class C
    Private Sub M(i1 As Integer, Optional i2 As Integer = 2)
    End Sub

    Private Sub Test()
        Me.M(1, i2:=2, [|i3|]:=3)
    End Sub
End Class"
            Dim fix =
"
Friend Class C
    Private Sub M(i1 As Integer, Optional i2 As Integer = 2, Optional i3 As Integer = Nothing)
    End Sub

    Private Sub Test()
        Me.M(1, i2:=2, i3:=3)
    End Sub
End Class"
            Await TestInRegularAndScriptAsync(code, fix)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")>
        Public Async Function TestInvocation_Indexer_NotSupported() As Task
            ' error BC30057: Too many arguments to 'Public Default Property Item(i1 As Integer) As Integer'.
            ' Could be fixed as Public Default Property Item(i1 As Integer, v As Integer) As Integer
            Dim code =
"
Public Class C
    Public Default Property Item(i1 As Integer) As Integer
        Get
            Return 1
        End Get
        Set(value As Integer)
        End Set
    End Property

    Public Sub Test()
        Dim i As Integer = Me(0, [|0|])
    End Sub
End Class
"
            Await TestMissingAsync(code)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29061")>
        Public Async Function TestConstructorInitializer_DoNotOfferFixForConstructorWithDiagnostic() As Task
            ' Error BC30057: Too many arguments to 'Public Sub New()'.
            Dim code =
"
Public Class C
    Public Sub New()
        Me.New([|1|])
    End Sub
End Class
"
            Await TestMissingAsync(code)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29061")>
        Public Async Function TestConstructorInitializer_OfferFixForOtherConstructors() As Task
            ' Error BC30516: Overload resolution failed because no accessible 'New' accepts this number of arguments.
            Dim code =
"
Public Class C
    Public Sub New(i As Integer)
    End Sub
    
    Public Sub New()
        Me.[|New|](1,1)
    End Sub
End Class
"
            Dim fix0 =
"
Public Class C
    Public Sub New(i As Integer, v As Integer)
    End Sub
    
    Public Sub New()
        Me.New(1,1)
    End Sub
End Class
"
            Await TestInRegularAndScriptAsync(code, fix0, index:=0)
            Await TestActionCountAsync(code, 1)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29061")>
        Public Async Function TestConstructorInitializer_OfferFixForBaseConstrcutors() As Task
            ' error BC30057: Too many arguments to 'Public Sub New()'.
            Dim code =
"
Public Class B
    Public Sub New()
    End Sub
End Class

Public Class C
    Inherits B
    Public Sub New()
        MyBase.New([|1|])
    End Sub
End Class
"
            Dim fix0 =
"
Public Class B
    Public Sub New(v As Integer)
    End Sub
End Class

Public Class C
    Inherits B
    Public Sub New()
        MyBase.New(1)
    End Sub
End Class
"
            Await TestInRegularAndScriptAsync(code, fix0, index:=0)
        End Function
    End Class
End Namespace
