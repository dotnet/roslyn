' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class DelegateTests
        Inherits BasicTestBase

        <Fact>
        Public Sub MissingTypes()
            VisualBasicCompilation.Create("test", syntaxTrees:={Parse("Delegate Sub A()")}, options:=TestOptions.ReleaseDll).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_UndefinedType1, "Delegate Sub A()").WithArguments("System.Void"),
                Diagnostic(ERRID.ERR_UndefinedType1, "Delegate Sub A()").WithArguments("System.Void"),
                Diagnostic(ERRID.ERR_UndefinedType1, "Delegate Sub A()").WithArguments("System.IAsyncResult"),
                Diagnostic(ERRID.ERR_UndefinedType1, "Delegate Sub A()").WithArguments("System.Object"),
                Diagnostic(ERRID.ERR_UndefinedType1, "Delegate Sub A()").WithArguments("System.IntPtr"),
                Diagnostic(ERRID.ERR_UndefinedType1, "Delegate Sub A()").WithArguments("System.AsyncCallback"),
                Diagnostic(ERRID.ERR_TypeRefResolutionError3, "A").WithArguments("System.MulticastDelegate", "test.dll"))
        End Sub

        <Fact>
        Public Sub DelegateSymbolTest()
            Dim source = <compilation name="C">
                             <file name="a.vb">
Imports System        
    ' delegate as type
    Delegate Sub SubDel(param1 as Integer, ByRef param2 as String)

Interface I1
End Interface

Class C2
implements I1
    public intMember as Integer
End Class

Class C1
    ' delegate as nested type
    Delegate Function FuncDel(param1 as Integer, param2 as String) as Char

    Delegate Sub SubGenDel(Of T)(param1 as T)
    Delegate Function FuncGenDel(Of T As I1)(param1 as integer) as T

    Sub Main()
    End Sub
End Class
                    </file>
                         </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

            ' --- test sub delegate ----------------------------------------------------------------------------

            Dim subDel As NamedTypeSymbol = CType(compilation.SourceModule.GlobalNamespace.GetTypeMembers("SubDel").Single(), NamedTypeSymbol)
            Assert.Equal("System.MulticastDelegate", subDel.BaseType.ToDisplayString(SymbolDisplayFormat.TestFormat))

            Dim delegateMembers = subDel.GetMembers()
            Assert.Equal(4, delegateMembers.Length())

            Dim delegateCtor = CType((From delegateMethod In delegateMembers Where delegateMethod.Name = ".ctor").Single(), SourceDelegateMethodSymbol)
            Assert.True(delegateCtor.IsImplicitlyDeclared())
            Assert.True(delegateCtor.IsSub())
            Assert.Equal(2, delegateCtor.Parameters.Length())
            Assert.Equal("System.Void", delegateCtor.ReturnType.ToDisplayString(SymbolDisplayFormat.TestFormat))
            Assert.Equal("TargetObject", delegateCtor.Parameters(0).Name)
            Assert.Equal("System.Object", delegateCtor.Parameters(0).Type.ToDisplayString(SymbolDisplayFormat.TestFormat))
            Assert.Equal("TargetMethod", delegateCtor.Parameters(1).Name)
            Assert.Equal("System.IntPtr", delegateCtor.Parameters(1).Type.ToDisplayString(SymbolDisplayFormat.TestFormat))
            Assert.True(delegateCtor.IsRuntimeImplemented())

            Dim delegateInvoke = CType((From delegateMethod In delegateMembers Where delegateMethod.Name = "Invoke").Single(), SourceDelegateMethodSymbol)
            Assert.True(delegateInvoke.IsImplicitlyDeclared())
            Assert.True(delegateInvoke.IsSub())
            Assert.Equal(2, delegateInvoke.Parameters.Length())
            Assert.Equal("System.Void", delegateInvoke.ReturnType.ToDisplayString(SymbolDisplayFormat.TestFormat))
            Assert.Equal("param1", delegateInvoke.Parameters(0).Name)
            Assert.False(delegateInvoke.Parameters(0).IsByRef())
            Assert.Equal("System.Int32", delegateInvoke.Parameters(0).Type.ToDisplayString(SymbolDisplayFormat.TestFormat))
            Assert.Equal("param2", delegateInvoke.Parameters(1).Name)
            Assert.Equal("System.String", delegateInvoke.Parameters(1).Type.ToDisplayString(SymbolDisplayFormat.TestFormat))
            Assert.True(delegateInvoke.Parameters(1).IsByRef())
            Assert.True(delegateInvoke.IsRuntimeImplemented())

            Dim delegateBeginInvoke = CType((From delegateMethod In delegateMembers Where delegateMethod.Name = "BeginInvoke").Single(), SourceDelegateMethodSymbol)
            Assert.True(delegateBeginInvoke.IsImplicitlyDeclared())
            Assert.False(delegateBeginInvoke.IsSub())
            Assert.Equal(4, delegateBeginInvoke.Parameters.Length())
            Assert.Equal("System.IAsyncResult", delegateBeginInvoke.ReturnType.ToDisplayString(SymbolDisplayFormat.TestFormat))
            Assert.Equal("param1", delegateBeginInvoke.Parameters(0).Name)
            Assert.Equal("System.Int32", delegateBeginInvoke.Parameters(0).Type.ToDisplayString(SymbolDisplayFormat.TestFormat))
            Assert.False(delegateInvoke.Parameters(0).IsByRef())
            Assert.Equal("param2", delegateBeginInvoke.Parameters(1).Name)
            Assert.Equal("System.String", delegateBeginInvoke.Parameters(1).Type.ToDisplayString(SymbolDisplayFormat.TestFormat))
            Assert.True(delegateInvoke.Parameters(1).IsByRef())
            Assert.Equal("System.AsyncCallback", delegateBeginInvoke.Parameters(2).Type.ToDisplayString(SymbolDisplayFormat.TestFormat))
            Assert.Equal("DelegateCallback", delegateBeginInvoke.Parameters(2).Name)
            Assert.False(delegateBeginInvoke.Parameters(2).IsByRef())
            Assert.Equal("System.Object", delegateBeginInvoke.Parameters(3).Type.ToDisplayString(SymbolDisplayFormat.TestFormat))
            Assert.Equal("DelegateAsyncState", delegateBeginInvoke.Parameters(3).Name)
            Assert.False(delegateBeginInvoke.Parameters(3).IsByRef())
            Assert.True(delegateBeginInvoke.IsRuntimeImplemented())

            Dim delegateEndInvoke = CType((From delegateMethod In delegateMembers Where delegateMethod.Name = "EndInvoke").Single(), SourceDelegateMethodSymbol)
            Assert.True(delegateEndInvoke.IsImplicitlyDeclared())
            Assert.True(delegateEndInvoke.IsSub())
            Assert.Equal("System.Void", delegateEndInvoke.ReturnType.ToDisplayString(SymbolDisplayFormat.TestFormat))
            Assert.Equal(2, delegateEndInvoke.Parameters.Length)
            Assert.Equal("System.String", delegateEndInvoke.Parameters(0).Type.ToDisplayString(SymbolDisplayFormat.TestFormat))
            Assert.Equal("param2", delegateEndInvoke.Parameters(0).Name)
            Assert.True(delegateEndInvoke.Parameters(0).IsByRef)
            Assert.Equal("System.IAsyncResult", delegateEndInvoke.Parameters(1).Type.ToDisplayString(SymbolDisplayFormat.TestFormat))
            Assert.Equal("DelegateAsyncResult", delegateEndInvoke.Parameters(1).Name)
            Assert.False(delegateEndInvoke.Parameters(1).IsByRef)
            Assert.True(delegateEndInvoke.IsRuntimeImplemented())

            ' --- test function delegate ----------------------------------------------------------------------------

            Dim funcDel As NamedTypeSymbol = CType(compilation.SourceModule.GlobalNamespace.GetTypeMembers("C1").Single().GetMembers("FuncDel").Single(), NamedTypeSymbol)
            Assert.Equal("System.MulticastDelegate", subDel.BaseType.ToDisplayString(SymbolDisplayFormat.TestFormat))

            delegateMembers = funcDel.GetMembers()
            delegateInvoke = CType((From delegateMethod In delegateMembers Where delegateMethod.Name = "Invoke").Single(), SourceDelegateMethodSymbol)
            Assert.True(delegateInvoke.IsImplicitlyDeclared())
            Assert.False(delegateInvoke.IsSub())
            Assert.Equal(2, delegateInvoke.Parameters.Length())
            Assert.Equal("System.Char", delegateInvoke.ReturnType.ToDisplayString(SymbolDisplayFormat.TestFormat))
            Assert.Equal("param1", delegateInvoke.Parameters(0).Name)
            Assert.False(delegateInvoke.Parameters(0).IsByRef())
            Assert.Equal("System.Int32", delegateInvoke.Parameters(0).Type.ToDisplayString(SymbolDisplayFormat.TestFormat))
            Assert.Equal("param2", delegateInvoke.Parameters(1).Name)
            Assert.Equal("System.String", delegateInvoke.Parameters(1).Type.ToDisplayString(SymbolDisplayFormat.TestFormat))
            Assert.False(delegateInvoke.Parameters(1).IsByRef())

            delegateEndInvoke = CType((From delegateMethod In delegateMembers Where delegateMethod.Name = "EndInvoke").Single(), SourceDelegateMethodSymbol)
            Assert.True(delegateEndInvoke.IsImplicitlyDeclared())
            Assert.False(delegateEndInvoke.IsSub())
            Assert.Equal("System.Char", delegateEndInvoke.ReturnType.ToDisplayString(SymbolDisplayFormat.TestFormat))
            Assert.Equal(1, delegateEndInvoke.Parameters.Length)
            Assert.Equal("System.IAsyncResult", delegateEndInvoke.Parameters(0).Type.ToDisplayString(SymbolDisplayFormat.TestFormat))
            Assert.Equal("DelegateAsyncResult", delegateEndInvoke.Parameters(0).Name)
            Assert.False(delegateEndInvoke.Parameters(0).IsByRef)

            ' --- test generic sub delegate -------------------------------------------------------------------------
            Dim genSubDel As NamedTypeSymbol = CType(compilation.SourceModule.GlobalNamespace.GetTypeMembers("C1").Single().GetMembers("SubGenDel").Single(), NamedTypeSymbol)
            Assert.Equal("System.MulticastDelegate", genSubDel.BaseType.ToDisplayString(SymbolDisplayFormat.TestFormat))

            delegateMembers = genSubDel.GetMembers()
            delegateInvoke = CType((From delegateMethod In delegateMembers Where delegateMethod.Name = "Invoke").Single(), SourceDelegateMethodSymbol)
            Assert.True(delegateInvoke.IsImplicitlyDeclared())
            Assert.True(delegateInvoke.IsSub())
            Assert.Equal(1, delegateInvoke.Parameters.Length())
            Assert.Equal("System.Void", delegateInvoke.ReturnType.ToDisplayString(SymbolDisplayFormat.TestFormat))
            Assert.Equal("param1", delegateInvoke.Parameters(0).Name)
            Assert.False(delegateInvoke.Parameters(0).IsByRef())
            Assert.Equal("T", delegateInvoke.Parameters(0).Type.ToDisplayString(SymbolDisplayFormat.TestFormat))
            Assert.True(delegateInvoke.IsRuntimeImplemented())

            ' --- test generic function delegate -------------------------------------------------------------------------
            Dim genFuncDel As NamedTypeSymbol = CType(compilation.SourceModule.GlobalNamespace.GetTypeMembers("C1").Single().GetMembers("FuncGenDel").Single(), NamedTypeSymbol)
            Assert.Equal("System.MulticastDelegate", genSubDel.BaseType.ToDisplayString(SymbolDisplayFormat.TestFormat))

            delegateMembers = genFuncDel.GetMembers()
            delegateInvoke = CType((From delegateMethod In delegateMembers Where delegateMethod.Name = "Invoke").Single(), SourceDelegateMethodSymbol)
            Assert.True(delegateInvoke.IsImplicitlyDeclared())
            Assert.False(delegateInvoke.IsSub())
            Assert.Equal("T", delegateInvoke.ReturnType.ToDisplayString(SymbolDisplayFormat.TestFormat))
            Assert.True(delegateInvoke.IsRuntimeImplemented())

        End Sub

        <Fact>
        Public Sub GenericDelegateSymbolTest()
            Dim source = <compilation name="C">
                             <file name="a.vb">
Imports System        

    Delegate Sub SubGenDel(Of T)(param1 as T)
    Delegate Function FuncGenDel(Of T As I1)(param1 as integer) as T

Class C1
    Sub Main()
    End Sub
End Class
                    </file>
                         </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

            Dim subGenDel As NamedTypeSymbol = CType(compilation.SourceModule.GlobalNamespace.GetTypeMembers("SubGenDel").Single(), NamedTypeSymbol)
            Dim param1Type = subGenDel.TypeParameters(0)
            Assert.Equal(param1Type.ContainingSymbol(), subGenDel)
            Assert.Equal(subGenDel.DelegateInvokeMethod.Parameters(0).Type, param1Type)
        End Sub

        <Fact>
        Public Sub DelegateSymbolLocationTest()
            Dim source = <compilation name="C">
                             <file name="a.vb">
Imports System        
    Delegate Sub SubDel(param1 as Integer, ByRef param2 as String)

Class C1
    Delegate Function FuncDel(param1 as Integer, param2 as String) as Char

    Sub Main()
    End Sub
End Class
                    </file>
                         </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

            Dim subDel As NamedTypeSymbol = CType(compilation.SourceModule.GlobalNamespace.GetTypeMembers("SubDel").Single(), NamedTypeSymbol)
            Assert.Equal(subDel.Locations(0), subDel.DelegateInvokeMethod.Locations(0))
            Assert.Equal(subDel.Locations(0), subDel.GetMembers(".ctor")(0).Locations(0))
            Assert.Equal(subDel.Locations(0), subDel.GetMembers("Invoke")(0).Locations(0))
            Assert.Equal(subDel.Locations(0), subDel.GetMembers("BeginInvoke")(0).Locations(0))
            Assert.Equal(subDel.Locations(0), subDel.GetMembers("EndInvoke")(0).Locations(0))
        End Sub

        <Fact>
        Public Sub MetadataDelegateField()
            Dim source = <compilation name="C">
                             <file name="a.vb">
Imports System        
Class C1
    Public Field As System.Func(Of Integer) 

    Sub Main()
    End Sub
End Class
                    </file>
                         </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

            Dim fieldSym = CType(compilation.SourceModule.GlobalNamespace.GetTypeMembers("C1").Single().GetMembers("Field").Single(), SourceFieldSymbol)
            Dim funcDel As NamedTypeSymbol = CType(fieldSym.Type, NamedTypeSymbol)
            Assert.Equal(TypeKind.Delegate, funcDel.TypeKind)
            Dim invoke = funcDel.DelegateInvokeMethod
            Assert.Equal(MethodKind.DelegateInvoke, invoke.MethodKind)
            Dim ctor = CType(funcDel.GetMembers(".ctor")(0), MethodSymbol)
            Assert.Equal(2, ctor.Parameters.Length)
            Assert.Equal(compilation.GetSpecialType(SpecialType.System_Object), ctor.Parameters(0).Type)
            Assert.Equal(compilation.GetSpecialType(SpecialType.System_IntPtr), ctor.Parameters(1).Type)
        End Sub

        <Fact>
        Public Sub DelegatesEverywhere()
            Dim source = <compilation name="C">
                             <file name="a.vb">
Imports System
Delegate Sub del1(param As Integer)
Delegate Function del2(param As Integer) As Integer

Class C1
    Delegate Sub del3(param As Integer)
    Delegate Function del4(param As Integer) As Integer

    Public Field As del3
    Private del9 As del3

    Function Func1(param As del3) As del3
        Dim myDel As del3 = param
        mydel = param
        Field = myDel
        myDel = Field
        Dim d1 As System.Delegate = Field
        Dim m1 As MulticastDelegate = del9
        Dim o As Object = param
        o = d1
        param = myDel
        Return param
    End Function
End Class

Interface I1
    Delegate Sub del5(param As Integer)
    Delegate Function del6(param As Integer) As Integer
End Interface

Structure S1
    Delegate Sub del7(param As Integer)
    Delegate Function del8(param As Integer) As Integer
End Structure
                    </file>
                         </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)
            CompilationUtils.AssertNoErrors(compilation)

        End Sub

        <Fact>
        Public Sub DuplicateParamNamesInDelegateDeclaration()
            ' yes strange, but allowed, according to 9.2.5 of the VB Language Spec
            Dim source = <compilation name="C">
                             <file name="a.vb">
Imports System
Delegate Sub del1(p As Integer, p as String, p as Object, p as String)
                    </file>
                         </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)
            CompilationUtils.AssertNoErrors(compilation)

            Dim del As NamedTypeSymbol = CType(compilation.SourceModule.GlobalNamespace.GetTypeMembers("del1").Single(), NamedTypeSymbol)
            Assert.Equal(del.DelegateInvokeMethod.Parameters.Length, 4)
            For Each param In del.DelegateInvokeMethod.Parameters
                Assert.Equal("p", param.Name)
            Next
        End Sub

        ' Test module method, shared method, instance method, constructor, property with valid args for both function and sub delegate
        <Fact>
        Public Sub ValidDelegateAddressOfTest()
            Dim source = <compilation name="C">
                             <file name="a.vb">
Imports System

Delegate Sub SubDel(p As String)
Delegate function FuncDel(p As String) as Integer

Module M1
    Public Sub ds1(p As String)
    End Sub
    Public Function df1(p As String) As Integer
    End Function

End Module

Class C1
    Public Sub ds2(p As String)
    End Sub
    Public Function df2(p As String) As Integer
    End Function

    Public Shared Sub ds3(p As String)
    End Sub
    Public Shared Function df3(p As String) As Integer
    End Function
End Class

Class c2
    Public Sub AssignDelegates()
        Dim ci As New C1()

        Dim ds As SubDel
        ds = AddressOf M1.ds1
        ds = AddressOf ci.ds2
        ds = AddressOf C1.ds3
    End Sub
End Class

Module Program
    Sub Main(args As String())
    End Sub
End Module
                    </file>
                         </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <WorkItem(540948, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540948")>
        <Fact>
        Public Sub AddressOfGenericMethod()
            Dim source = <compilation name="C">
                             <file name="a.vb">
Imports System

Module C
    Sub Main()
        Dim a As Action(Of Integer) = AddressOf Goo
    End Sub

    Sub Goo(Of T)(x As T)
    End Sub
End Module                    
                            </file>
                         </compilation>

            Dim comp1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.Off))
            CompilationUtils.AssertNoErrors(comp1)
            Dim comp2 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.On))
            CompilationUtils.AssertNoErrors(comp2)
        End Sub

        <WorkItem(541002, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541002")>
        <Fact>
        Public Sub TypeParameterCannotConflictWithDelegateMethod()
            Dim source = <compilation name="F">
                             <file name="F.vb">
Delegate Sub F1(Of Invoke)
Class C1
    Delegate Sub F2(Of Invoke)
End Class
                            </file>
                         </compilation>

            Dim comp1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.Off))
            CompilationUtils.AssertNoErrors(comp1)
            Dim comp2 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.On))
            CompilationUtils.AssertNoErrors(comp2)
        End Sub

        <WorkItem(541004, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541004")>
        <Fact>
        Public Sub DelegateParameterCanBeNamedInvoke()
            Dim source = <compilation name="D">
                             <file name="D.vb">
Delegate Function D1(Invoke As Boolean) As Boolean
Class C1
    Delegate Function D2(Invoke As Boolean) As Boolean
End Class
                            </file>
                         </compilation>

            Dim comp1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.Off))
            CompilationUtils.AssertNoErrors(comp1)
            Dim comp2 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.On))
            CompilationUtils.AssertNoErrors(comp2)
        End Sub
    End Class

End Namespace
