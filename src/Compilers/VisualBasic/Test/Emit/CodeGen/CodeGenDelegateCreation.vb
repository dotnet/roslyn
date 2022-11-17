' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Reflection
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class CodeGenDelegateCreation
        Inherits BasicTestBase

        <Fact>
        Public Sub DelegateMethods()
            For Each optionValue In {"On", "Off"}
                Dim source =
<compilation>
    <file name="a.vb">
Option strict <%= optionValue %>

Imports System        
    ' delegate as type
    Delegate Function FuncDel(param1 as Integer, param2 as String) as Char

Class C2
    public intMember as Integer
End Class

Class C1
    ' delegate as nested type
    Delegate Sub SubDel(param1 as Integer, ByRef param2 as String)

    Delegate Sub SubGenDel(Of T)(param1 as T)
    Delegate Function FuncGenDel(Of T)(param1 as integer) as T

    Shared Sub Main()
    End Sub
End Class
    </file>
</compilation>

                Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

                CompileAndVerify(c1, symbolValidator:=
                    Sub([module])
                        ' Note: taking dev10 metadata as golden results, not the ECMA spec.
                        Dim reader = (DirectCast([module], PEModuleSymbol)).Module.GetMetadataReader()

                        Dim expectedMethodMethodFlags = MethodAttributes.Public Or MethodAttributes.NewSlot Or MethodAttributes.Virtual Or MethodAttributes.CheckAccessOnOverride
                        Dim expectedConstructorMethodFlags = MethodAttributes.Public Or MethodAttributes.SpecialName Or MethodAttributes.RTSpecialName

                        ' --- test SubDel methods -----------------------------------------------------------------------------------------------

                        Dim subDel = [module].GlobalNamespace.GetTypeMembers("C1").Single().GetTypeMembers("SubDel").Single()

                        ' test .ctor
                        '.method public specialname rtspecialname instance void .ctor(object Instance, native int Method) runtime managed {}
                        Dim ctor = subDel.GetMembers(".ctor").OfType(Of MethodSymbol)().Single()
                        Assert.True(ctor.IsSub())
                        Assert.Equal("System.Void", ctor.ReturnType.ToDisplayString(SymbolDisplayFormat.TestFormat))
                        Assert.Equal(2, ctor.Parameters.Length)
                        Assert.Equal("System.Object", ctor.Parameters(0).Type.ToDisplayString(SymbolDisplayFormat.TestFormat))
                        Assert.Equal("TargetObject", ctor.Parameters(0).Name)
                        Assert.Equal("System.IntPtr", ctor.Parameters(1).Type.ToDisplayString(SymbolDisplayFormat.TestFormat))
                        Assert.Equal("TargetMethod", ctor.Parameters(1).Name)
                        Dim methodDef = CType(ctor, PEMethodSymbol).Handle
                        Dim methodFlags = reader.GetMethodDefinition(methodDef).Attributes
                        Assert.Equal(expectedConstructorMethodFlags, methodFlags)
                        Dim methodImplFlags = reader.GetMethodDefinition(methodDef).ImplAttributes
                        Dim expectedMethodImplFlags = MethodImplAttributes.Runtime
                        Assert.Equal(expectedMethodImplFlags, methodImplFlags)
                        'Assert.True(CType(ctor, PEMethodSymbol).IsImplicitlyDeclared) ' does not work for PEMethodSymbols
                        Assert.True(CType(ctor, PEMethodSymbol).IsRuntimeImplemented)

                        ' test Invoke
                        ' .method public newslot strict virtual instance void Invoke(int32 param1, string& param2) runtime managed {}
                        Dim invoke = subDel.GetMembers("Invoke").OfType(Of MethodSymbol)().Single()
                        Assert.True(invoke.IsSub())
                        Assert.Equal("System.Void", invoke.ReturnType.ToDisplayString(SymbolDisplayFormat.TestFormat))
                        Assert.Equal(2, invoke.Parameters.Length)
                        Assert.Equal("System.Int32", invoke.Parameters(0).Type.ToDisplayString(SymbolDisplayFormat.TestFormat))
                        Assert.Equal("param1", invoke.Parameters(0).Name)
                        Assert.False(invoke.Parameters(0).IsByRef)
                        Assert.Equal("System.String", invoke.Parameters(1).Type.ToDisplayString(SymbolDisplayFormat.TestFormat))
                        Assert.Equal("param2", invoke.Parameters(1).Name)
                        Assert.True(invoke.Parameters(1).IsByRef)
                        methodDef = CType(invoke, PEMethodSymbol).Handle
                        methodFlags = reader.GetMethodDefinition(methodDef).Attributes
                        Assert.Equal(expectedMethodMethodFlags, methodFlags)
                        methodImplFlags = reader.GetMethodDefinition(methodDef).ImplAttributes
                        expectedMethodImplFlags = MethodImplAttributes.Runtime
                        Assert.Equal(expectedMethodImplFlags, methodImplFlags)
                        'Assert.True(CType(invoke, PEMethodSymbol).IsImplicitlyDeclared) ' does not work for PEMethodSymbols
                        Assert.True(CType(invoke, PEMethodSymbol).IsRuntimeImplemented)

                        ' test BeginInvoke
                        ' .method public newslot strict virtual instance class [mscorlib]System.IAsyncResult 
                        '    BeginInvoke(int32 param1,
                        '      string& param2,
                        '      class [mscorlib]System.AsyncCallback DelegateCallback,
                        '      object DelegateAsyncState) runtime managed {}
                        Dim beginInvoke = subDel.GetMembers("BeginInvoke").OfType(Of MethodSymbol)().Single()
                        Assert.False(beginInvoke.IsSub())
                        Assert.Equal("System.IAsyncResult", beginInvoke.ReturnType.ToDisplayString(SymbolDisplayFormat.TestFormat))
                        Assert.Equal(4, beginInvoke.Parameters.Length)
                        Assert.Equal("System.Int32", beginInvoke.Parameters(0).Type.ToDisplayString(SymbolDisplayFormat.TestFormat))
                        Assert.Equal("param1", beginInvoke.Parameters(0).Name)
                        Assert.False(beginInvoke.Parameters(0).IsByRef)
                        Assert.Equal("System.String", beginInvoke.Parameters(1).Type.ToDisplayString(SymbolDisplayFormat.TestFormat))
                        Assert.Equal("param2", beginInvoke.Parameters(1).Name)
                        Assert.True(beginInvoke.Parameters(1).IsByRef)
                        Assert.Equal("System.AsyncCallback", beginInvoke.Parameters(2).Type.ToDisplayString(SymbolDisplayFormat.TestFormat))
                        Assert.Equal("DelegateCallback", beginInvoke.Parameters(2).Name)
                        Assert.False(beginInvoke.Parameters(2).IsByRef)
                        Assert.Equal("System.Object", beginInvoke.Parameters(3).Type.ToDisplayString(SymbolDisplayFormat.TestFormat))
                        Assert.Equal("DelegateAsyncState", beginInvoke.Parameters(3).Name)
                        Assert.False(beginInvoke.Parameters(3).IsByRef)
                        methodDef = CType(beginInvoke, PEMethodSymbol).Handle
                        methodFlags = reader.GetMethodDefinition(methodDef).Attributes
                        Assert.Equal(expectedMethodMethodFlags, methodFlags)
                        methodImplFlags = reader.GetMethodDefinition(methodDef).ImplAttributes
                        expectedMethodImplFlags = MethodImplAttributes.Runtime
                        Assert.Equal(expectedMethodImplFlags, methodImplFlags)
                        'Assert.True(CType(beginInvoke, PEMethodSymbol).IsImplicitlyDeclared) ' does not work for PEMethodSymbols
                        Assert.True(CType(beginInvoke, PEMethodSymbol).IsRuntimeImplemented)

                        ' test EndInvoke
                        ' .method public newslot strict virtual instance 
                        '    void EndInvoke(string& param2, class [mscorlib]System.IAsyncResult DelegateAsyncResult) runtime managed {}
                        Dim endInvoke = subDel.GetMembers("EndInvoke").OfType(Of MethodSymbol)().Single()
                        Assert.True(endInvoke.IsSub())
                        Assert.Equal("System.Void", endInvoke.ReturnType.ToDisplayString(SymbolDisplayFormat.TestFormat))
                        Assert.Equal(2, endInvoke.Parameters.Length)
                        Assert.Equal("System.String", endInvoke.Parameters(0).Type.ToDisplayString(SymbolDisplayFormat.TestFormat))
                        Assert.Equal("param2", endInvoke.Parameters(0).Name)
                        Assert.True(endInvoke.Parameters(0).IsByRef)
                        Assert.Equal("System.IAsyncResult", endInvoke.Parameters(1).Type.ToDisplayString(SymbolDisplayFormat.TestFormat))
                        Assert.Equal("DelegateAsyncResult", endInvoke.Parameters(1).Name)
                        Assert.False(endInvoke.Parameters(1).IsByRef)
                        methodDef = CType(endInvoke, PEMethodSymbol).Handle
                        methodFlags = reader.GetMethodDefinition(methodDef).Attributes
                        Assert.Equal(expectedMethodMethodFlags, methodFlags)
                        methodImplFlags = reader.GetMethodDefinition(methodDef).ImplAttributes
                        expectedMethodImplFlags = MethodImplAttributes.Runtime
                        Assert.Equal(expectedMethodImplFlags, methodImplFlags)
                        'Assert.True(CType(endInvoke, PEMethodSymbol).IsImplicitlyDeclared) ' does not work for PEMethodSymbols
                        Assert.True(CType(endInvoke, PEMethodSymbol).IsRuntimeImplemented)

                        ' --- test FuncDel methods ----------------------------------------------------------------------------------------------

                        Dim funcDel = [module].GlobalNamespace.GetTypeMembers("FuncDel").Single()

                        ' test Invoke
                        ' .method public newslot strict virtual instance char Invoke(int32 param1, string param2) runtime managed
                        invoke = funcDel.GetMembers("Invoke").OfType(Of MethodSymbol)().Single()
                        Assert.False(invoke.IsSub())
                        Assert.Equal("System.Char", invoke.ReturnType.ToDisplayString(SymbolDisplayFormat.TestFormat))
                        Assert.Equal(2, invoke.Parameters.Length)
                        Assert.Equal("System.Int32", invoke.Parameters(0).Type.ToDisplayString(SymbolDisplayFormat.TestFormat))
                        Assert.Equal("param1", invoke.Parameters(0).Name)
                        Assert.False(invoke.Parameters(0).IsByRef)
                        Assert.Equal("System.String", invoke.Parameters(1).Type.ToDisplayString(SymbolDisplayFormat.TestFormat))
                        Assert.Equal("param2", invoke.Parameters(1).Name)
                        Assert.False(invoke.Parameters(1).IsByRef)
                        methodDef = CType(invoke, PEMethodSymbol).Handle
                        methodFlags = reader.GetMethodDefinition(methodDef).Attributes
                        Assert.Equal(expectedMethodMethodFlags, methodFlags)
                        methodImplFlags = reader.GetMethodDefinition(methodDef).ImplAttributes
                        expectedMethodImplFlags = MethodImplAttributes.Runtime
                        Assert.Equal(expectedMethodImplFlags, methodImplFlags)
                        'Assert.True(CType(invoke, PEMethodSymbol).IsImplicitlyDeclared) ' does not work for PEMethodSymbols

                        ' test EndInvoke
                        ' .method public newslot strict virtual instance 
                        ' charEndInvoke(class [mscorlib]System.IAsyncResult DelegateAsyncResult) runtime managed {}
                        endInvoke = funcDel.GetMembers("EndInvoke").OfType(Of MethodSymbol)().Single()
                        Assert.False(endInvoke.IsSub())
                        Assert.Equal("System.Char", endInvoke.ReturnType.ToDisplayString(SymbolDisplayFormat.TestFormat))
                        Assert.Equal(1, endInvoke.Parameters.Length)
                        Assert.Equal("System.IAsyncResult", endInvoke.Parameters(0).Type.ToDisplayString(SymbolDisplayFormat.TestFormat))
                        Assert.Equal("DelegateAsyncResult", endInvoke.Parameters(0).Name)
                        Assert.False(endInvoke.Parameters(0).IsByRef)
                        methodDef = CType(endInvoke, PEMethodSymbol).Handle
                        methodFlags = reader.GetMethodDefinition(methodDef).Attributes
                        Assert.Equal(expectedMethodMethodFlags, methodFlags)
                        methodImplFlags = reader.GetMethodDefinition(methodDef).ImplAttributes
                        expectedMethodImplFlags = MethodImplAttributes.Runtime
                        Assert.Equal(expectedMethodImplFlags, methodImplFlags)
                        'Assert.True(CType(endInvoke, PEMethodSymbol).IsImplicitlyDeclared) ' does not work for PEMethodSymbols

                        ' --- test FuncGenDel methods -------------------------------------------------------------------------------------------

                        Dim subGenDel = [module].GlobalNamespace.GetTypeMembers("C1").Single().GetTypeMembers("SubGenDel").Single()

                        ' test Invoke
                        ' .method public newslot strict virtual instance char Invoke(int32 param1, string param2) runtime managed
                        invoke = subGenDel.GetMembers("Invoke").OfType(Of MethodSymbol)().Single()
                        Assert.True(invoke.IsSub())
                        Assert.Equal(1, invoke.Parameters.Length)
                        Assert.Equal("T", invoke.Parameters(0).Type.ToDisplayString(SymbolDisplayFormat.TestFormat))
                        Assert.Equal("param1", invoke.Parameters(0).Name)
                        Assert.False(invoke.Parameters(0).IsByRef)
                        Assert.True(CType(invoke, PEMethodSymbol).IsRuntimeImplemented)
                    End Sub)
            Next
        End Sub

        ' Test module method, shared method, instance method, constructor, property with valid args for both function and sub delegate
        <Fact>
        Public Sub ValidDelegateAddressOfTest()
            For Each optionValue In {"On", "Off"}
                Dim source = <compilation>
                                 <file name="a.vb">
Option strict <%= optionValue %>
Imports System

Delegate Sub SubDel(p As String)
Delegate function FuncDel(p As String) as Integer

Module M1
    Public Sub ds1(p As String)
    End Sub
    Public Function df1(p As String) As Integer
        return 23
    End Function

End Module

Class C1
    Public Sub ds2(p As String)
    End Sub
    Public Function df2(p As String) As Integer
        return 23
    End Function

    Public Shared Sub ds3(p As String)
    End Sub
    Public Shared Function df3(p As String) As Integer
        return 23
    End Function
End Class

Class C2
    Public Sub AssignDelegates()
        Dim ci As New C1()

        Dim ds As SubDel
        ds = AddressOf M1.ds1
        Console.WriteLine(ds)
        ds = AddressOf ci.ds2
        Console.WriteLine(ds)
        ds = AddressOf C1.ds3
        Console.WriteLine(ds)
    End Sub
End Class

Module Program
    Sub Main(args As String())
        Dim c as new C2()
        c.AssignDelegates()
    End Sub
End Module
                    </file>
                             </compilation>

                CompileAndVerify(source,
                                 expectedOutput:=<![CDATA[
SubDel
SubDel
SubDel
    ]]>).
                    VerifyIL("C2.AssignDelegates",
                <![CDATA[
    {
  // Code size       56 (0x38)
  .maxstack  3
  IL_0000:  newobj     "Sub C1..ctor()"
  IL_0005:  ldnull
  IL_0006:  ldftn      "Sub M1.ds1(String)"
  IL_000c:  newobj     "Sub SubDel..ctor(Object, System.IntPtr)"
  IL_0011:  call       "Sub System.Console.WriteLine(Object)"
  IL_0016:  ldftn      "Sub C1.ds2(String)"
  IL_001c:  newobj     "Sub SubDel..ctor(Object, System.IntPtr)"
  IL_0021:  call       "Sub System.Console.WriteLine(Object)"
  IL_0026:  ldnull
  IL_0027:  ldftn      "Sub C1.ds3(String)"
  IL_002d:  newobj     "Sub SubDel..ctor(Object, System.IntPtr)"
  IL_0032:  call       "Sub System.Console.WriteLine(Object)"
  IL_0037:  ret
    }
    ]]>)
            Next
        End Sub

        ''' Bug 5987 "Target parameter of a delegate instantiation is not boxed in case of a structure"
        <Fact>
        Public Sub DelegateInvocationStructureTests()
            For Each optionValue In {"On", "Off"}
                Dim source =
    <compilation>
        <file name="a.vb">
Option strict <%= optionValue %>
Imports System

Delegate Sub SubDel(p As String)
Delegate function FuncDel(p As String) as Integer

Structure S1
    Public Sub ds4(p As String)
        Console.WriteLine("S1.ds4 " + p)
    End Sub
    Public Function df4(p As String) As Integer
        return 4
    End Function

    Public Shared Sub ds5(p As String)
        Console.WriteLine("S1.ds5 " + p)
    End Sub
    Public Shared Function df5(p As String) As Integer
        return 5
    End Function
End Structure

Module Program
    Sub Main(args As String())
        Dim ds As SubDel
        Dim s as new S1()
        ds = AddressOf s.ds4
        ds.Invoke("(passed arg)")
        ds = AddressOf S1.ds5
        ds.Invoke("(passed arg)")

        Moo("Hi")
        Moo(42)
    End Sub

    Sub Moo(of T)(x as T)
        Dim f as Func(of String) = AddressOf x.ToString
        Console.WriteLine(f.Invoke())
    End Sub
End Module
    </file>
    </compilation>

                Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

                CompileAndVerify(source,
                                 expectedOutput:=<![CDATA[
S1.ds4 (passed arg)
S1.ds5 (passed arg)
Hi
42
    ]]>)
            Next
        End Sub

        <Fact>
        Public Sub DelegateInvocationTests()
            For Each optionValue In {"On", "Off"}
                Dim source =
    <compilation>
        <file name="a.vb">
Option strict <%= optionValue %>
Imports System

Delegate Sub SubDel(Of T)(p As T)
Delegate function FuncDel(p As Integer) as Integer

Module M1
    Public Sub ds1(p As String)
        Console.WriteLine("M1.ds1 " + p)
    End Sub
    Public Function df1(p As Integer) As Integer
        return 1 + p
    End Function
End Module

Class C1
    Public Sub ds2(p As String)
        Console.WriteLine("C1.ds2 " + p)
    End Sub
    Public Function df2(p As Integer) As Integer
        return 2 + p
    End Function

    Public Shared Sub ds3(p As String)
        Console.WriteLine("C1.ds3 " + p)
    End Sub
    Public Shared Function df3(p As Integer) As Integer
        return 3 + p
    End Function
End Class

Module Program
    Sub Main(args As String())

        Dim ds As SubDel(of string)
        ds = AddressOf M1.ds1
        ds.Invoke("(passed arg)")
        ds("(passed arg)")
        Dim ci As New C1()
        ds = AddressOf ci.ds2
        ds.Invoke("(passed arg)")
        ds("(passed arg)")
        ds = AddressOf C1.ds3
        ds.Invoke("(passed arg)")
        ds("(passed arg)")

        Dim df As FuncDel
        Dim target as Integer
        df = AddressOf M1.df1
        target = df.Invoke(41)
        Console.WriteLine(target)        
        target = df(41)
        Console.WriteLine(target)
        df = AddressOf ci.df2
        target = df.Invoke(41)
        Console.WriteLine(target)
        target = df(41)
        Console.WriteLine(target)
        df = AddressOf C1.df3
        target = df.Invoke(41)
        Console.WriteLine(target)
        target = df(41)
        Console.WriteLine(target)
    End Sub
End Module
    </file>
    </compilation>

                Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

                CompileAndVerify(source,
                                 expectedOutput:=<![CDATA[
M1.ds1 (passed arg)
M1.ds1 (passed arg)
C1.ds2 (passed arg)
C1.ds2 (passed arg)
C1.ds3 (passed arg)
C1.ds3 (passed arg)
42
42
43
43
44
44
    ]]>)
            Next
        End Sub

        <Fact>
        Public Sub DelegateInvocationDelegatesFromMetadataTests()
            For Each optionValue In {"On", "Off"}
                Dim source =
    <compilation>
        <file name="a.vb">
Option strict <%= optionValue %>
Imports System

Module M1
    Public Sub ds1(p$)
        Console.WriteLine("M1.ds1 " + p)
    End Sub
    Public Function df1(p%) As Integer
        Return 1 + p
    End Function
End Module

Class C1
    Public Sub ds2(p As String)
        Console.WriteLine("C1.ds2 " + p)
    End Sub
    Public Function df2%(p As Integer)
        Return 2 + p
    End Function

    Public Shared Sub ds3(p As String)
        Console.WriteLine("C1.ds3 " + p)
    End Sub
    Public Shared Function df3(p As Integer) As Integer
        Return 3 + p
    End Function
End Class

Module Program
    Sub Main(args As String())

        Dim ds As Action(Of String)
        ds = AddressOf M1.ds1
        ds.Invoke("(passed arg)")
        ds("(passed arg)")
        Dim ci As New C1()
        ds = AddressOf ci.ds2
        ds.Invoke("(passed arg)")
        ds("(passed arg)")
        ds = AddressOf C1.ds3
        ds.Invoke("(passed arg)")
        ds("(passed arg)")

        Dim df As Func(Of Integer, Integer)
        Dim target As Integer
        df = AddressOf M1.df1
        target = df.Invoke(41)
        Console.WriteLine(target)
        target = df(41)
        Console.WriteLine(target)
        df = AddressOf ci.df2
        target = df.Invoke(41)
        Console.WriteLine(target)
        target = df(41)
        Console.WriteLine(target)
        df = AddressOf C1.df3
        target = df.Invoke(41)
        Console.WriteLine(target)
        target = df(41)
        Console.WriteLine(target)
    End Sub
End Module
    </file>
    </compilation>

                Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

                CompileAndVerify(source,
                                 expectedOutput:=<![CDATA[
M1.ds1 (passed arg)
M1.ds1 (passed arg)
C1.ds2 (passed arg)
C1.ds2 (passed arg)
C1.ds3 (passed arg)
C1.ds3 (passed arg)
42
42
43
43
44
44
    ]]>)
            Next
        End Sub

        <Fact>
        Public Sub Error_ERR_AddressOfNotCreatableDelegate1()
            Dim source = <compilation>
                             <file name="a.vb">
Imports System

Delegate Sub SubDel(p As String)

Class C2
    Public Shared Sub goo(p as string)
    end sub

    Public Sub AssignDelegates()
        Dim v1 As System.Delegate = AddressOf goo
        Dim v2 As System.MulticastDelegate = AddressOf C2.goo
    End Sub
End Class
                    </file>
                         </compilation>

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

            CompilationUtils.AssertTheseDiagnostics(c1,
<expected>
BC30939: 'AddressOf' expression cannot be converted to '[Delegate]' because type '[Delegate]' is declared 'MustInherit' and cannot be created.
        Dim v1 As System.Delegate = AddressOf goo
                                    ~~~~~~~~~~~~~
BC30939: 'AddressOf' expression cannot be converted to 'MulticastDelegate' because type 'MulticastDelegate' is declared 'MustInherit' and cannot be created.
        Dim v2 As System.MulticastDelegate = AddressOf C2.goo
                                             ~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub NewDelegateWithAddressOf()
            For Each optionValue In {"On", "Off"}
                Dim source =
    <compilation>
        <file name="a.vb">
    Option strict <%= optionValue %>    
    IMPORTS SYStEM
    Delegate Sub D()

    Class C1
        public field as string = ""

        public sub new(param as string)
            field = param
        end sub

        public sub goo()
            console.writeline("Hello " &amp; Me.field)
        end sub

        public shared sub goo2()
            console.writeline("... and again.")
        end sub

    end class

    Module Program
        Sub Main(args As String())
            Dim x As D
            x = New D(AddressOf Method)
            x

            Dim c as new C1("again.")
            Dim y as new D(addressof c.goo)
            y

            Dim z as new D(addressof C1.goo2)
            z
        End Sub
        Public Sub Method()
            console.writeline("Hello.")
        End Sub
    End Module
    </file>
    </compilation>

                Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

                CompileAndVerify(source,
                                 expectedOutput:=<![CDATA[
Hello.
Hello again.
... and again.
    ]]>)
            Next
        End Sub

        <Fact>
        Public Sub WideningArgumentsDelegateSubRelaxation()
            For Each optionValue In {"On", "Off"}
                Dim source =
    <compilation>
        <file name="a.vb">
    Option strict <%= optionValue %>    
    Imports System
    
    Delegate Sub WideningNumericDelegate(p as Byte)
    Delegate Sub WideningStringDelegate(p as Char)
    Delegate Sub WideningNullableDelegate(p as Byte?)
    Delegate Sub WideningReferenceDelegate(p as Derived)
    Delegate Sub WideningArrayDelegate(p() as Derived)
    Delegate Sub WideningValueDelegate(p as S1)

    Structure S1
    End Structure

    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class C
        Public Sub WideningNumericSub(p as Integer)
            console.writeline("Hello from instance WideningNumericDelegate " &amp; p.ToString() )
        End Sub

        Public Sub WideningStringSub(p as String)
            console.writeline("Hello from instance WideningStringDelegate " &amp; p.ToString() )        
        End Sub

        Public Sub WideningNullableSub(p as Integer?)
            console.writeline("Hello from instance WideningNullableDelegate " &amp; p.ToString() )
        End Sub

        Public Sub WideningReferenceSub(p as Base)
            console.writeline("Hello from instance WideningReferenceDelegate " &amp; p.ToString() )
        End Sub

        Public Sub WideningArraySub(p() as Base)
            console.writeline("Hello from instance WideningArrayDelegate " &amp; p.ToString() )
        End Sub

        Public Sub WideningValueSub(p as Object)
            console.writeline("Hello from instance WideningValueDelegate " &amp; p.ToString() )
        End Sub

    End Class

    Module Program
        Sub Main(args As String())
            'Dim n? As Integer' = 23
            Dim arr(1) as Derived
            arr(0) = new Derived()
            arr(1) = new Derived()
            Dim ci as new C()
            Dim d1 as new WideningNumericDelegate(AddressOf ci.WideningNumericSub)
            d1(23)
            Dim d2 as new WideningStringDelegate(AddressOf ci.WideningStringSub)
            d2("c"c)
            'Dim d3 as new WideningNullableDelegate(AddressOf ci.WideningNullableSub)
            'd3(n)
            Dim d4 as new WideningReferenceDelegate(AddressOf ci.WideningReferenceSub)
            d4(new Derived())
            Dim d5 as new WideningArrayDelegate(AddressOf ci.WideningArraySub)
            d5( arr )
            Dim d6 as new WideningValueDelegate(AddressOf ci.WideningValueSub)
            d6(new S1())
        End Sub
    End Module
    </file>
    </compilation>

                Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

                CompileAndVerify(source,
                                 expectedOutput:=<![CDATA[
Hello from instance WideningNumericDelegate 23
Hello from instance WideningStringDelegate c
Hello from instance WideningReferenceDelegate Derived
Hello from instance WideningArrayDelegate Derived[]
Hello from instance WideningValueDelegate S1
    ]]>)
            Next
        End Sub

        ''' Bug 7191: "Lambda rewriter does not work for widening conversions of byref params with option strict off"
        <Fact>
        Public Sub WideningArgumentsDelegateSubRelaxationByRefStrictOff()
            For Each optionValue In {"Off"}
                Dim source =
    <compilation>
        <file name="a.vb">
    Option strict <%= optionValue %>    
Imports System

    Delegate Sub WideningNumericDelegate(byref p as Byte)
    Delegate Sub WideningStringDelegate(byref p as Char)
    Delegate Sub WideningNullableDelegate(byref p as Byte?)
    Delegate Sub WideningReferenceDelegate(byref p as Derived)
    Delegate Sub WideningArrayDelegate(byref p() as Derived)
    Delegate Sub WideningValueDelegate(byref p as S1)

    Structure S1
        public field as integer
        Public sub New(p as integer)
            field = p
        end sub
    End Structure

    Class Base
        public field as integer
    End Class

    Class Derived
        Inherits Base

        Public sub New(p as integer)
            field = p
        end sub
    End Class

    Class C
        Public Sub WideningNumericSub(byref p as Integer)
            console.writeline("Hello from instance WideningNumericDelegate " &amp; p.ToString() )
            p = 42
        End Sub

        Public Sub WideningStringSub(byref p as String)
            console.writeline("Hello from instance WideningStringDelegate " &amp; p.ToString() )        
            p = "touched"
        End Sub

        'Public Sub WideningNullableSub(byref p as Integer?)
        '    console.writeline("Hello from instance WideningNullableDelegate " &amp; p.ToString() )
        '    p = 42
        'End Sub

        Public Sub WideningReferenceSub(byref p as Base)
            console.writeline("Hello from instance WideningReferenceDelegate " &amp; p.ToString() )
            p = new Derived(42)
        End Sub

        Public Sub WideningArraySub(byref p() as Base)
            console.writeline("Hello from instance WideningArrayDelegate " &amp; p.ToString() )
            Dim arr(1) as Derived
            arr(0) = new Derived(23)
            arr(1) = new Derived(42)
            p = arr
        End Sub

        Public Sub WideningValueSub(byref p as Object)
            console.writeline("Hello from instance WideningValueDelegate " &amp; p.ToString() )
            p = new S1(42)
        End Sub

    End Class

    Module Program
        Sub Main(args As String())
            'Dim n? As Integer' = 23
            Dim arr(1) as Derived
            arr(0) = new Derived(1)
            arr(1) = new Derived(2)
            Dim ci as new C()
            Dim d1 as new WideningNumericDelegate(AddressOf ci.WideningNumericSub)
            dim pbyte as byte = 23
            d1(pbyte)
            console.writeline(pbyte)
            Dim d2 as new WideningStringDelegate(AddressOf ci.WideningStringSub)
            dim pchar as char = "c"c
            d2(pchar)
            console.writeline(pchar)
            'Dim d3 as new WideningNullableDelegate(AddressOf ci.WideningNullableSub)
            'd3(n)
            'console.writeline(n.Value)
            Dim d4 as new WideningReferenceDelegate(AddressOf ci.WideningReferenceSub)
            dim pderived as Derived = new Derived(23)
            d4(pderived)
            console.writeline(pderived.field)
            Dim d5 as new WideningArrayDelegate(AddressOf ci.WideningArraySub)
            d5( arr )
            console.writeline(arr(0).field &amp; " " &amp; arr(1).field)
            Dim d6 as new WideningValueDelegate(AddressOf ci.WideningValueSub)
            dim ps1 as S1 = new S1(23)
            d6(ps1)
            console.writeline(ps1.field)
        End Sub
    End Module
    </file>
    </compilation>

                Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

                CompileAndVerify(source,
                 expectedOutput:=<![CDATA[
Hello from instance WideningNumericDelegate 23
42
Hello from instance WideningStringDelegate c
t
Hello from instance WideningReferenceDelegate Derived
42
Hello from instance WideningArrayDelegate Derived[]
23 42
Hello from instance WideningValueDelegate S1
42
                                    ]]>)
            Next
        End Sub

        <Fact()>
        Public Sub WideningArgumentsDelegateSubRelaxation_nullable()
            For Each optionValue In {"On", "Off"}
                Dim source =
    <compilation>
        <file name="a.vb">
    Option strict <%= optionValue %>    
    Imports System
    Delegate Sub WideningNullableDelegate(p as Byte?)

    Class C
        Public Sub WideningNullableSub(p as Integer?)
            console.writeline("Hello from instance WideningNullableDelegate " &amp; p.Value.ToString() )
        End Sub
    End Class

    Module Program
        Sub Main(args As String())
            Dim n? As Byte = 23
            Dim ci as new C()
            Dim d3 as new WideningNullableDelegate(AddressOf ci.WideningNullableSub)
            d3(n)
        End Sub
    End Module
    </file>
    </compilation>

                Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

                CompileAndVerify(source,
                                 expectedOutput:=<![CDATA[
Hello from instance WideningNullableDelegate 23
    ]]>)
            Next
        End Sub

        <Fact>
        Public Sub IdentityArgumentsDelegateSubRelaxationByRef()
            For Each optionValue In {"On", "Off"}
                Dim source =
    <compilation>
        <file name="a.vb">
    Option strict <%= optionValue %>    
Imports System

    Delegate Sub IdentityNumericDelegate(byref p as Byte)
    Delegate Sub IdentityStringDelegate(byref p as Char)
    Delegate Sub IdentityNullableDelegate(byref p as Byte?)
    Delegate Sub IdentityReferenceDelegate(byref p as Derived)
    Delegate Sub IdentityArrayDelegate(byref p() as Derived)
    Delegate Sub IdentityValueDelegate(byref p as S1)

    Structure S1
        public field as integer
        Public sub New(p as integer)
            field = p
        end sub
    End Structure

    Class Base
        public field as integer
    End Class

    Class Derived
        Inherits Base

        Public sub New(p as integer)
            field = p
        end sub
    End Class

    Class C
        Public Sub IdentityNumericSub(byref p as Byte)
            console.writeline("Hello from instance IdentityNumericDelegate " &amp; p.ToString() )
            p = 42
        End Sub

        Public Sub IdentityStringSub(byref p as Char)
            console.writeline("Hello from instance IdentityStringDelegate " &amp; p.ToString() )        
            p = "t"c
        End Sub

        'Public Sub IdentityNullableSub(byref p as Byte?)
        '    console.writeline("Hello from instance IdentityNullableDelegate " &amp; p.ToString() )
        '    p = 42
        'End Sub

        Public Sub IdentityReferenceSub(byref p as Derived)
            console.writeline("Hello from instance IdentityReferenceDelegate " &amp; p.ToString() )
            p = new Derived(42)
        End Sub

        Public Sub IdentityArraySub(byref p() as Derived)
            console.writeline("Hello from instance IdentityArrayDelegate " &amp; p.ToString() )
            Dim arr(1) as Derived
            arr(0) = new Derived(23)
            arr(1) = new Derived(42)
            p = arr
        End Sub

        Public Sub IdentityValueSub(byref p as S1)
            console.writeline("Hello from instance IdentityValueDelegate " &amp; p.ToString() )
            p = new S1(42)
        End Sub

    End Class

    Module Program
        Sub Main(args As String())
            'Dim n? As Integer' = 23
            Dim arr(1) as Derived
            arr(0) = new Derived(1)
            arr(1) = new Derived(2)
            Dim ci as new C()
            Dim d1 as new IdentityNumericDelegate(AddressOf ci.IdentityNumericSub)
            dim pbyte as byte = 23
            d1(pbyte)
            console.writeline(pbyte)
            Dim d2 as new IdentityStringDelegate(AddressOf ci.IdentityStringSub)
            dim pchar as char = "c"c
            d2(pchar)
            console.writeline(pchar)
            'Dim d3 as new IdentityNullableDelegate(AddressOf ci.IdentityNullableSub)
            'd3(n)
            'console.writeline(n.Value)
            Dim d4 as new IdentityReferenceDelegate(AddressOf ci.IdentityReferenceSub)
            dim pderived as Derived = new Derived(23)
            d4(pderived)
            console.writeline(pderived.field)
            Dim d5 as new IdentityArrayDelegate(AddressOf ci.IdentityArraySub)
            d5( arr )
            console.writeline(arr(0).field &amp; " " &amp; arr(1).field)
            Dim d6 as new IdentityValueDelegate(AddressOf ci.IdentityValueSub)
            dim ps1 as S1 = new S1(23)
            d6(ps1)
            console.writeline(ps1.field)
        End Sub
    End Module
    </file>
    </compilation>

                Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

                CompileAndVerify(source,
                                 expectedOutput:=<![CDATA[
Hello from instance IdentityNumericDelegate 23
42
Hello from instance IdentityStringDelegate c
t
Hello from instance IdentityReferenceDelegate Derived
42
Hello from instance IdentityArrayDelegate Derived[]
23 42
Hello from instance IdentityValueDelegate S1
42
                    ]]>)
            Next
        End Sub

        <Fact>
        Public Sub WideningArgumentsDelegateFunctionRelaxation()
            For Each optionValue In {"On", "Off"}
                Dim source =
    <compilation>
        <file name="a.vb">
    Option strict <%= optionValue %>    
    Imports System
    
    Delegate Function WideningNumericDelegate(p as byte, p as Byte) as integer
    Delegate Function WideningStringDelegate(p as Char) as String
    Delegate Function WideningNullableDelegate(p as Byte?) as integer
    Delegate Function WideningReferenceDelegate(p as Derived) as integer
    Delegate Function WideningArrayDelegate(p() as Derived) as integer
    Delegate Function WideningValueDelegate(p as S1) as integer

    Structure S1
    End Structure

    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class C
        Public Function WideningNumericFunction(i as integer, p as Integer) as integer
            console.writeline("Hello from instance WideningNumericDelegate ")
            return 23
        End Function

        Public Function WideningStringFunction$(p as String)
            console.writeline("Hello from instance WideningStringDelegate ")        
            return "24"
        End Function

        Public Function WideningNullableFunction(p as Integer?) as integer
            console.writeline("Hello from instance WideningNullableDelegate ")
            return 25
        End Function

        Public Function WideningReferenceFunction(p as Base) as integer
            console.writeline("Hello from instance WideningReferenceDelegate ")
            return 26
        End Function

        Public Function WideningArrayFunction(p() as Base) as integer
            console.writeline("Hello from instance WideningArrayDelegate ")
            return 27
        End Function

        Public Function WideningValueFunction(p as Object) as integer
            console.writeline("Hello from instance WideningValueDelegate ")
            return 28
        End Function

    End Class

    Module Program
        Sub Main(args As String())
            'Dim n? As Integer' = 23
            Dim arr(1) as Derived
            arr(0) = new Derived()
            arr(1) = new Derived()
            Dim ci as new C()
            Dim d1 as new WideningNumericDelegate(AddressOf ci.WideningNumericFunction)
            console.writeline( d1(1, 23) )
            Dim d2 as new WideningStringDelegate(AddressOf ci.WideningStringFunction)
            console.writeline( d2("c"c) )
            'Dim d3 as new WideningNullableDelegate(AddressOf ci.WideningNullableFunction)
            'console.writeline( d3(n) )
            Dim d4 as new WideningReferenceDelegate(AddressOf ci.WideningReferenceFunction)
            console.writeline( d4(new Derived()) )
            Dim d5 as new WideningArrayDelegate(AddressOf ci.WideningArrayFunction)
            console.writeline( d5(arr) )
            Dim d6 as new WideningValueDelegate(AddressOf ci.WideningValueFunction)
            console.writeline( d6(new S1()) )
        End Sub
    End Module
    </file>
    </compilation>

                Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

                CompileAndVerify(source,
                                 expectedOutput:=<![CDATA[
Hello from instance WideningNumericDelegate 
23
Hello from instance WideningStringDelegate 
24
Hello from instance WideningReferenceDelegate 
26
Hello from instance WideningArrayDelegate 
27
Hello from instance WideningValueDelegate 
28
    ]]>)
            Next
        End Sub

        <Fact()>
        Public Sub WideningArgumentsDelegateFunctionRelaxation_nullable()
            For Each optionValue In {"On", "Off"}
                Dim source =
    <compilation>
        <file name="a.vb">
    Option strict <%= optionValue %>    
    Imports System
    
    Delegate Function WideningNullableDelegate(p as Byte?) as integer

    Class C
        Public Function WideningNullableFunction(p as Integer?) as integer
            console.writeline("Hello from instance WideningNullableDelegate ")
            return 25
        End Function
    End Class

    Module Program
        Sub Main(args As String())
            Dim n? As Byte = 23
            Dim ci as new C()
            Dim d3 as new WideningNullableDelegate(AddressOf ci.WideningNullableFunction)
            console.writeline( d3(n) )
        End Sub
    End Module
    </file>
    </compilation>

                Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

                CompileAndVerify(source,
                                 expectedOutput:=<![CDATA[
Hello from instance WideningNullableDelegate 
25
    ]]>)
            Next
        End Sub

        <Fact>
        Public Sub WideningReturnValueDelegateFunctionRelaxation()
            For Each optionValue In {"On", "Off"}
                Dim source =
    <compilation>
        <file name="a.vb">
    Option strict <%= optionValue %>    
    Imports System
    
    Delegate Function WideningReturnNumericDelegate() as Integer
    Delegate Function WideningReturnStringDelegate() as String
    'Delegate Function WideningReturnNullableDelegate() as Integer?
    Delegate Function WideningReturnReferenceDelegate() as Base
    Delegate Function WideningReturnArrayDelegate() as Base()
    Delegate Function WideningReturnValueDelegate() as Object

    Structure S1
    End Structure

    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

        Class C
        Public Function WideningReturnNumericFunction() as Byte
            console.writeline("Hello from instance WideningReturnNumericFunction ")
            return 23
        End Function

        Public Function WideningReturnStringFunction() as Char
            console.writeline("Hello from instance WideningReturnStringFunction ")        
            return "A"c
        End Function

        'Public Function WideningReturnNullableFunction() as Byte?
        '    console.writeline("Hello from instance WideningReturnNullableFunction ")
        '    return 25
        'End Function

        Public Function WideningReturnReferenceFunction() as Derived
            console.writeline("Hello from instance WideningReturnReferenceFunction ")
            return new Derived()
        End Function

        Public Function WideningReturnArrayFunction() as Derived()
            console.writeline("Hello from instance WideningReturnArrayFunction ")
            Dim arr(1) as Derived
            arr(0) = new Derived()
            arr(1) = new Derived()
            return arr
        End Function

        Public Function WideningReturnValueFunction() as S1
            console.writeline("Hello from instance WideningReturnValueFunction ")
            return new S1()
        End Function

    End Class

    Module Program
        Sub Main(args As String())
            'Dim n? As Integer' = 23
            Dim ci as new C()
            Dim d1 as new WideningReturnNumericDelegate(AddressOf ci.WideningReturnNumericFunction)
            console.writeline( d1() )
            Dim d2 as new WideningReturnStringDelegate(AddressOf ci.WideningReturnStringFunction)
            console.writeline( d2() )
            'Dim d3 as new WideningReturnNullableDelegate(AddressOf ci.WideningReturnNullableFunction)
            'console.writeline( d3(n) )
            Dim d4 as new WideningReturnReferenceDelegate(AddressOf ci.WideningReturnReferenceFunction)
            console.writeline( d4() )
            Dim d5 as new WideningReturnArrayDelegate(AddressOf ci.WideningReturnArrayFunction)
            console.writeline( d5() )
            Dim d6 as new WideningReturnValueDelegate(AddressOf ci.WideningReturnValueFunction)
            console.writeline( d6() )
        End Sub
    End Module
    </file>
    </compilation>

                Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

                CompileAndVerify(source,
                                 expectedOutput:=<![CDATA[
Hello from instance WideningReturnNumericFunction 
23
Hello from instance WideningReturnStringFunction 
A
Hello from instance WideningReturnReferenceFunction 
Derived
Hello from instance WideningReturnArrayFunction 
Derived[]
Hello from instance WideningReturnValueFunction 
S1
    ]]>)
            Next
        End Sub

        <Fact()>
        Public Sub WideningReturnValueDelegateFunctionRelaxation_nullable()
            For Each optionValue In {"On", "Off"}
                Dim source =
    <compilation>
        <file name="a.vb">
    Option strict <%= optionValue %>    
    Imports System

Delegate Function WideningReturnNullableDelegate() As Integer?

Class C
    Public Function WideningReturnNullableFunction() As Byte?
        console.writeline("Hello from instance WideningReturnNullableFunction ")
        Return 25
    End Function
End Class

Module Program
    Sub Main(args As String())
        Dim ci As New C()
        Dim d3 As New WideningReturnNullableDelegate(AddressOf ci.WideningReturnNullableFunction)
        Console.WriteLine((d3()).Value())
    End Sub
End Module
    </file>
    </compilation>

                Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

                CompileAndVerify(source,
                                 expectedOutput:=<![CDATA[
Hello from instance WideningReturnNullableFunction 
25
    ]]>)
            Next
        End Sub

        <Fact>
        Public Sub NarrowingArgumentsDelegateSubRelaxation()
            Dim source =
<compilation>
    <file name="a.vb">
    Option Strict Off
    Imports System
    Delegate Sub NarrowingNumericDelegate(pdel as Integer)
    Delegate Sub NarrowingStringDelegate(pdel as String)
    Delegate Sub NarrowingNullableDelegate(pdel as Integer?)
    Delegate Sub NarrowingReferenceDelegate(pdel as Base)
    Delegate Sub NarrowingArrayDelegate(pdel() as Base)
    Delegate Sub NarrowingValueDelegate(pdel as Object)

    Structure S1
    End Structure

    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class C
        Public Sub NarrowingNumericSub(psub as Byte)
            console.writeline("Hello from instance NarrowingNumericDelegate " &amp; psub.ToString() )
        End Sub

        Public Sub NarrowingStringSub(psub as Char)
            console.writeline("Hello from instance NarrowingStringDelegate " &amp; psub.ToString() )        
        End Sub

        Public Sub NarrowingNullableSub(psub as Byte?)
            console.writeline("Hello from instance NarrowingNullableDelegate " &amp; psub.ToString() )
        End Sub

        Public Sub NarrowingReferenceSub(psub as Derived)
            console.writeline("Hello from instance NarrowingReferenceDelegate " &amp; psub.ToString() )
        End Sub

        Public Sub NarrowingArraySub(psub() as Derived)
            console.writeline("Hello from instance NarrowingArrayDelegate " &amp; psub.ToString() )
        End Sub

        Public Sub NarrowingValueSub(psub as S1)
            console.writeline("Hello from instance NarrowingValueDelegate")
        End Sub

    End Class

    Module Program
        Sub Main(args As String())
            Dim n? As Integer' = 23
            Dim arr(1) as Derived
            arr(0) = new Derived()
            arr(1) = new Derived()
            Dim ci as new C()
            Dim d1 as new NarrowingNumericDelegate(AddressOf ci.NarrowingNumericSub)
            d1(23)
            Dim d2 as new NarrowingStringDelegate(AddressOf ci.NarrowingStringSub)
            d2("c")
            'Dim d3 as new NarrowingNullableDelegate(AddressOf ci.NarrowingNullableSub)
            'd3(n)
            Dim d4 as new NarrowingReferenceDelegate(AddressOf ci.NarrowingReferenceSub)
            d4(new Derived())
            Dim d5 as new NarrowingArrayDelegate(AddressOf ci.NarrowingArraySub)
            d5(arr)
            Dim d6 as new NarrowingValueDelegate(AddressOf ci.NarrowingValueSub)
            d6(new S1())
        End Sub
    End Module
    </file>
</compilation>

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

            CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
Hello from instance NarrowingNumericDelegate 23
Hello from instance NarrowingStringDelegate c
Hello from instance NarrowingReferenceDelegate Derived
Hello from instance NarrowingArrayDelegate Derived[]
Hello from instance NarrowingValueDelegate
]]>)
        End Sub

        <Fact()>
        Public Sub NarrowingArgumentsDelegateSubRelaxation_nullable()
            Dim source =
<compilation>
    <file name="a.vb">
    Option Strict Off
    Imports System
    Delegate Sub NarrowingNullableDelegate(p as Integer?)

    Class C
        Public Sub NarrowingNullableSub(p as Byte?)
            console.writeline("Hello from instance NarrowingNullableDelegate " &amp; p.Value.ToString() )
        End Sub
    End Class

    Module Program
        Sub Main(args As String())
            Dim n? As Integer = 23
            Dim ci as new C()
            Dim d3 as new NarrowingNullableDelegate(AddressOf ci.NarrowingNullableSub)
            d3(n)
        End Sub
    End Module
    </file>
</compilation>

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

            CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
Hello from instance NarrowingNullableDelegate 23
]]>)
        End Sub

        <Fact>
        Public Sub OmittingArgumentsDelegateSubRelaxation_strictoff()
            Dim source =
<compilation>
    <file name="a.vb">
    Option Strict Off
    Imports System

    Delegate Sub OmittingArgumentsDelegate(p as Integer)

    Class C
        Public Sub OmittingArgumentsSub()
            console.writeline("Hello from instance OmittingArgumentsDelegate.")
        End Sub
    End Class

    Module Program

        Sub Main(args As String())
            Dim ci as new C()
            Dim d1 as new OmittingArgumentsDelegate(AddressOf ci.OmittingArgumentsSub)
            d1(23)
        End Sub
    End Module
    </file>
</compilation>

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

            CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
Hello from instance OmittingArgumentsDelegate.
]]>)
        End Sub

        <Fact()>
        Public Sub OmittingArgumentsDelegateSubRelaxation_lambda()
            Dim source =
<compilation>
    <file name="a.vb">
    Option Strict On
    Imports System

    Delegate Sub OmittingArgumentsDelegate(p as Integer)

    Module Program

        Sub Main(args As String())
            Dim d1 as new OmittingArgumentsDelegate(Sub() console.writeline("Hello from instance OmittingArgumentsDelegate."))
            d1(23)

            d1 = Sub() console.writeline("Hello from instance OmittingArgumentsDelegate.")
            d1.Invoke(23)

        End Sub
    End Module
    </file>
</compilation>

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

            CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
Hello from instance OmittingArgumentsDelegate.
Hello from instance OmittingArgumentsDelegate.
]]>)
        End Sub

        <Fact>
        Public Sub OmittingReturnValueDelegateFunctionRelaxation()
            For Each optionValue In {"On", "Off"}
                Dim source =
    <compilation>
        <file name="a.vb">
    Option strict <%= optionValue %>
    Imports System

    Delegate Sub OmittingReturnValueDelegate(p as Integer)

    Class C
        Public Function OmittingReturnValueSub(p as Integer) as Integer
            console.writeline("Hello from instance OmittingReturnValueSub.")
            return 23
        End Function
    End Class

    Module Program

        Sub Main(args As String())
            Dim ci as new C()
            Dim d1 as new OmittingReturnValueDelegate(AddressOf ci.OmittingReturnValueSub)
            d1(23)
        End Sub
    End Module
    </file>
    </compilation>

                Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

                CompileAndVerify(source,
                                 expectedOutput:=<![CDATA[
Hello from instance OmittingReturnValueSub.
    ]]>)
            Next
        End Sub

        <Fact()>
        Public Sub OmittingOptionalParameter()
            For Each optionValue In {"On", "Off"}
                Dim source =
    <compilation>
        <file name="a.vb">
    Option strict <%= optionValue %>
    Imports System

    Delegate Sub OmittingOptionalParameter1Delegate(p as Integer, p as Integer)
    Delegate Sub OmittingOptionalParameter2Delegate(p as Integer)
    Delegate Sub OmittingOptionalParameter3Delegate()

    Class C
        Public Sub OmittingOptionalParameter1Sub(optional a as integer = 23, optional b as integer = 42)
            console.writeline("Hello from instance OmittingOptionalParameter1Sub.")
        End Sub
    End Class

    Module Program

        Sub Main(args As String())
            Dim ci as new C()
            Dim d1 as new OmittingOptionalParameter1Delegate(AddressOf ci.OmittingOptionalParameter1Sub)
            d1(23, 23)

            Dim d2 as new OmittingOptionalParameter2Delegate(AddressOf ci.OmittingOptionalParameter1Sub)
            d2(23)

            Dim d3 as new OmittingOptionalParameter3Delegate(AddressOf ci.OmittingOptionalParameter1Sub)
            d3()
        End Sub
    End Module
    </file>
    </compilation>

                Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

                CompileAndVerify(source,
                                 expectedOutput:=<![CDATA[
Hello from instance OmittingOptionalParameter1Sub.
Hello from instance OmittingOptionalParameter1Sub.
Hello from instance OmittingOptionalParameter1Sub.
    ]]>)
            Next
        End Sub

        <Fact>
        Public Sub ParamArrayDelegateRelaxation1()
            For Each optionValue In {"On", "Off"}
                Dim source =
    <compilation>
        <file name="a.vb">
    Option strict <%= optionValue %>
    Imports System

    Delegate Sub ParamArrayInSubPerfectMatch1Delegate(pdel as Integer, pdel as Integer)
    Delegate Sub ParamArrayInSubPerfectMatch2Delegate(p as Base, p as Base)
    Delegate Sub ParamArrayInSubPerfectMatch3Delegate()
    Delegate Sub ParamArrayInSubWideningMatch1Delegate(p as Integer, p as Byte)

    Class Base
    End Class

    Class Derived
        Inherits Base
    end Class

    Class C
        Public Sub ParamArrayInSubPerfectMatch1Sub(paramarray p() as Integer)
            console.writeline("Hello from instance ParamArrayInSubPerfectMatch1Sub.")
        End Sub

        Public Sub ParamArrayInSubPerfectMatch2Sub(paramarray p() as Base)
            console.writeline("Hello from instance ParamArrayInSubPerfectMatch2Sub.")
        End Sub
    End Class

    Module Program

        Sub Main(args As String())
            Dim ci as new C()
            Dim d1 as new ParamArrayInSubPerfectMatch1Delegate(AddressOf ci.ParamArrayInSubPerfectMatch1Sub)
            d1(23, 23)

            Dim d2 as new ParamArrayInSubPerfectMatch2Delegate(AddressOf ci.ParamArrayInSubPerfectMatch2Sub)
            d2(new Derived(), new Derived())

            Dim d3 as new ParamArrayInSubPerfectMatch3Delegate(AddressOf ci.ParamArrayInSubPerfectMatch2Sub)
            d3()

            Dim d4 as new ParamArrayInSubPerfectMatch3Delegate(AddressOf ci.ParamArrayInSubPerfectMatch1Sub)
            d4()

            Dim d5 as new ParamArrayInSubWideningMatch1Delegate(AddressOf ci.ParamArrayInSubPerfectMatch1Sub)
            d5(23,42)

        End Sub
    End Module
    </file>
    </compilation>

                Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

                CompileAndVerify(source,
                                 expectedOutput:=<![CDATA[
Hello from instance ParamArrayInSubPerfectMatch1Sub.
Hello from instance ParamArrayInSubPerfectMatch2Sub.
Hello from instance ParamArrayInSubPerfectMatch2Sub.
Hello from instance ParamArrayInSubPerfectMatch1Sub.
Hello from instance ParamArrayInSubPerfectMatch1Sub.
    ]]>)
            Next
        End Sub

        <Fact>
        Public Sub ParamArrayDelegateRelaxation2()
            For Each optionValue In {"On", "Off"}
                Dim source =
    <compilation>
        <file name="a.vb">
    Option strict <%= optionValue %>
    Imports System

    Delegate Sub ParamArrayDelegateRelaxation1Delegate(p() as Integer)

    Class C
        Public Sub ParamArrayDelegateRelaxation1Sub(paramarray b() as integer)
            console.writeline("Hello from instance ParamArrayDelegateRelaxation1Sub.")
            console.writeline(b(0) &amp; " " &amp; b(1)) 
        End Sub
    End Class

    Module Program

        Sub Main(args As String())
            Dim ci as new C()
            Dim arr(1) as integer
            arr(0) = 23
            arr(1) = 42
            Dim d2 as new ParamArrayDelegateRelaxation1Delegate(AddressOf ci.ParamArrayDelegateRelaxation1Sub)
            d2(arr)
        End Sub
    End Module
    </file>
    </compilation>

                Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

                CompileAndVerify(source,
                                 expectedOutput:=<![CDATA[
Hello from instance ParamArrayDelegateRelaxation1Sub.
23 42
    ]]>)
            Next
        End Sub

        <Fact>
        Public Sub ParamArrayDelegateRelaxation4()
            For Each optionValue In {"On", "Off"}
                Dim source =
    <compilation>
        <file name="a.vb">
    Option strict <%= optionValue %>
    Imports System

    Delegate Sub ExpandedParamArrayDelegate(b as byte, b as byte, b as byte)

    Class C
        Public Sub ParamArraySub(b as byte, paramarray p() as Byte)
            console.writeline("Hello from instance ParamArraySub.")
            console.writeline(p)
            console.writeline(p(0))
        End Sub
    End Class

    Module Program
        Sub Main(args As String())
            Dim ci as new C()

            Dim d2 as new ExpandedParamArrayDelegate(AddressOf ci.ParamArraySub)
            d2(1,2,3)
        End Sub
    End Module
</file>
    </compilation>

                CompileAndVerify(source,
                               expectedOutput:=<![CDATA[
Hello from instance ParamArraySub.
System.Byte[]
2
           ]]>)

            Next
        End Sub

        <Fact>
        Public Sub ParamArrayDelegateRelaxation5()
            For Each optionValue In {"On", "Off"}
                Dim source =
    <compilation>
        <file name="a.vb">
    Option strict <%= optionValue %>
    Imports System

    Class Base
    End Class
    Class Derived
        Inherits Base
    End Class

    Delegate Sub ExpandedParamArrayDelegate1(b as byte, b as Integer, b as Byte)
    Delegate Sub ExpandedParamArrayDelegate2(b as byte, b as Base, b as Derived)

    Class C
        Public Sub ParamArraySub1(b as byte, paramarray p() as Integer)
            console.writeline("Hello from instance ParamArraySub.")
            console.writeline(p)
            console.writeline(p(0))
            console.writeline(p(1))
        End Sub
        Public Sub ParamArraySub2(b as byte, paramarray p() as Base)
            console.writeline("Hello from instance ParamArraySub.")
            console.writeline(p)
            console.writeline(p(0))
            console.writeline(p(1))
        End Sub

    End Class

    Module Program
        Sub Main(args As String())
            Dim ci as new C()

            Dim d1 as new ExpandedParamArrayDelegate1(AddressOf ci.ParamArraySub1)
            d1(1,2,3)

            Dim d2 as new ExpandedParamArrayDelegate2(AddressOf ci.ParamArraySub2)
            d2(1,new Derived(), new Derived())
            d2(1,new Base(), new Derived())
        End Sub
    End Module
</file>
    </compilation>

                CompileAndVerify(source,
                               expectedOutput:=<![CDATA[
Hello from instance ParamArraySub.
System.Int32[]
2
3
Hello from instance ParamArraySub.
Base[]
Derived
Derived
Hello from instance ParamArraySub.
Base[]
Base
Derived
           ]]>)

            Next
        End Sub

        <Fact>
        Public Sub ByRefParamArraysFromMetadata()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Module M

  Public Sub Main()
    Dim d1 as DelegateByRefParamArray.DelegateSubWithParamArrayOfReferenceTypes = AddressOf SubWithParamArray

    Dim bases(1) as DelegateByRefParamArray_Base
    bases(0) = new DelegateByRefParamArray_Base(1)
    bases(1) = new DelegateByRefParamArray_Base(2)

    ' does not work.
    ' d1(new DelegateByRefParamArray_Base(1), new DelegateByRefParamArray_Base(2))
    d1(bases)
    Console.WriteLine("SubWithParamArray returned: " &amp; bases(0).field &amp; " " &amp; bases(1).field)    

    Dim d2 as DelegateByRefParamArray.DelegateSubWithByRefParamArrayOfReferenceTypes = AddressOf DelegateByRefParamArray.SubWithByRefParamArrayOfReferenceTypes_Identify_2
    ' byref is ignored when calling through the delegate.
    d2(bases)
    Console.WriteLine("SubWithByRefParamArrayOfReferenceTypes_Identify_2 returned: " &amp; bases(0).field &amp; " " &amp; bases(1).field)    

    ' byref is also ignored when calling the method directly.
    DelegateByRefParamArray.SubWithByRefParamArrayOfReferenceTypes_Identify_2(bases)
    Console.WriteLine("SubWithByRefParamArrayOfReferenceTypes_Identify_2 returned: " &amp; bases(0).field &amp; " " &amp; bases(1).field)    

  End Sub

  Public Sub SubWithParamArray(ParamArray p() as DelegateByRefParamArray_Base)
    Console.WriteLine("Called SubWithParamArray: " &amp; p(0).field &amp; " " &amp; p(1).field)    
  End Sub

End Module
    </file>
</compilation>
            CompileAndVerify(source,
                references:={TestReferences.SymbolsTests.DelegateImplementation.DelegateByRefParamArray},
                expectedOutput:=<![CDATA[
Called SubWithParamArray: 1 2
SubWithParamArray returned: 1 2
Called SubWithByRefParamArrayOfReferenceTypes_Identify_2.
SubWithByRefParamArrayOfReferenceTypes_Identify_2 returned: 1 2
Called SubWithByRefParamArrayOfReferenceTypes_Identify_2.
SubWithByRefParamArrayOfReferenceTypes_Identify_2 returned: 1 2
    ]]>)
        End Sub

        <Fact>
        Public Sub ByRefParamArraysFromMetadataNarrowingBack()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict Off
Imports System

Module M
    Delegate Sub ByRefArrayOfDerived(ByRef x as DelegateByRefParamArray_Derived())
    Delegate Sub ByRefArrayOfBase(ByRef x as DelegateByRefParamArray_Base())

  Public Sub Main()
    ' Testing:
    ' Delegate Sub (ByRef x as Derived())
    ' Sub TargetMethod(ByRef ParamArray x As Base())
    ' dev 10 does not create a stub and uses byval here.
    Dim d3 as ByRefArrayOfDerived = AddressOf DelegateByRefParamArray.SubWithByRefParamArrayOfReferenceTypes_Identify_1
    Dim derived(1) as DelegateByRefParamArray_Derived
    derived(0) = new DelegateByRefParamArray_Derived(1)
    derived(1) = new DelegateByRefParamArray_Derived(2)
    d3(derived)
    Console.WriteLine("SubWithByRefParamArrayOfReferenceTypes_Identify_1 returned: " &amp; derived(0).field &amp; " " &amp; derived(1).field)    

    ' same as above, this time, delegate's last parameter is paramarray as well.
    ' 
    dim d4 as DelegateByRefParamArray.DelegateSubWithRefParamArrayOfReferenceTypesDerived = addressof DelegateByRefParamArray.ByRefParamArraySubOfBase
    Dim arr2(1) as DelegateByRefParamArray_Derived
    arr2(0) = new DelegateByRefParamArray_Derived(1)
    arr2(1) = new DelegateByRefParamArray_Derived(2)    
    d4(arr2)
    Console.WriteLine("ByRefParamArraySubOfBase returned: " &amp; arr2(0).field &amp; " " &amp; arr2(1).field)    

    ' testing Delegate Sub(ByRef x as Base()) with
    ' Sub goo (ByRef ParamArray x as Base())
    dim d5 as ByRefArrayOfBase = addressof DelegateByRefParamArray.ByRefParamArraySubOfBase
    Dim arr3(1) as DelegateByRefParamArray_Base
    arr3(0) = new DelegateByRefParamArray_Base(1)
    arr3(1) = new DelegateByRefParamArray_Base(2)    
    d5(arr3)
    Console.WriteLine("ByRefParamArraySubOfBase returned: " &amp; arr3(0).field &amp; " " &amp; arr3(1).field)    

  End Sub

End Module      
    </file>
</compilation>

            CompileAndVerify(source,
                references:={TestReferences.SymbolsTests.DelegateImplementation.DelegateByRefParamArray},
                expectedOutput:=<![CDATA[
Called SubWithByRefParamArrayOfReferenceTypes_Identify_1.
SubWithByRefParamArrayOfReferenceTypes_Identify_1 returned: 1 2
ByRefParamArraySubOfBase returned: 1 2
ByRefParamArraySubOfBase returned: 23 42
    ]]>)
        End Sub

        <Fact>
        Public Sub DelegatesWithParamArraysFromMetadataExpandParameters()
            For Each optionValue In {"On", "Off"}

                Dim source =
    <compilation>
        <file name="a.vb">
Option strict <%= optionValue %>
Imports System

Module M

  Public Sub Main()
    Dim d1 as DelegateByRefParamArray.DelegateSubWithParamArrayOfReferenceTypes = AddressOf SubWithParamArray
    d1(new DelegateByRefParamArray_Base(1), new DelegateByRefParamArray_Base(2))

    Dim d2 as DelegateByRefParamArray.DelegateSubWithByRefParamArrayOfReferenceTypes = AddressOf DelegateByRefParamArray.SubWithByRefParamArrayOfReferenceTypes_Identify_2
    d2(new DelegateByRefParamArray_Base(1), new DelegateByRefParamArray_Base(2))
  End Sub

  Public Sub SubWithParamArray(ParamArray p() as DelegateByRefParamArray_Base)
    Console.WriteLine("Called SubWithParamArray: " &amp; p(0).field &amp; " " &amp; p(1).field)    
  End Sub

End Module
    </file>
    </compilation>
                CompileAndVerify(source,
                    references:={TestReferences.SymbolsTests.DelegateImplementation.DelegateByRefParamArray},
                    expectedOutput:=<![CDATA[
Called SubWithParamArray: 1 2
Called SubWithByRefParamArrayOfReferenceTypes_Identify_2.
    ]]>)
            Next
        End Sub

        <Fact>
        Public Sub IgnoringTypeCharactersInAddressOf()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Module M1
    Sub MySub()
        console.writeline("Ignored type characters ...")
    End Sub

    Function MyFunction() As String
        console.writeline("Really ignored type characters ...")
        Return ""
    End Function

    Sub Main()
        Dim d1 As Action = AddressOf MySub%
        Dim d2 As Func(Of String) = AddressOf MyFunction%
        d1()
        d2()
    End Sub
End Module    
</file>
</compilation>
            CompileAndVerify(source,
                         expectedOutput:=<![CDATA[
Ignored type characters ...
Really ignored type characters ...
    ]]>)
        End Sub

        <Fact>
        Public Sub ConversionsOfUnexpandedParamArrays()
            Dim source =
<compilation>
    <file name="a.vb">
        imports system 

        Class Base
        End Class

        Class Derived
            Inherits Base
        End Class

Module Module1

            Sub M1(paramarray x() As Base)
                console.writeline("Hello from Delegate.")
            End Sub

            Sub M2(x As Action(Of Base()))
                console.writeline("Sub M2(x As Action(Of Base())) called.")
                Dim arr(0) as Base
                arr(0) = new Derived()
                x(arr)
            End Sub

            Sub M2(x As Action(Of Derived()))
                console.writeline("Sub M2(x As Action(Of Derived())) called.")
                Dim arr(0) as Derived
                arr(0) = new Derived()
                x(arr)
            End Sub

            Sub Main()
                Dim x1 As Action(Of Base) = AddressOf M1
                x1(new Derived())
                Dim x2 As Action(Of Derived) = AddressOf M1
                x2(new Derived())
                M2(AddressOf M1)
            End Sub


        End Module
</file>
</compilation>
            CompileAndVerify(source,
                         expectedOutput:=<![CDATA[
Hello from Delegate.
Hello from Delegate.
Sub M2(x As Action(Of Base())) called.
Hello from Delegate.
    ]]>)
        End Sub

        <Fact>
        Public Sub NarrowingWarningForOptionStrictCustomReturnValues()
            Dim source =
<compilation>
    <file name="a.vb">
Imports system 

Class Base
End Class

Class Derived
    Inherits Base
End Class

Module Module1
    Delegate Function MyDelegate() as Derived

    Public Function goo() as Base
        return new Derived()
    End Function

    Sub Main()
        Dim x1 As new MyDelegate(AddressOf goo)
        Console.WriteLine(x1())
    End Sub

End Module
</file>
</compilation>

            CompileAndVerify(source, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
expectedOutput:=<![CDATA[
Derived
    ]]>)

            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom)).
                AssertTheseDiagnostics(<expected>
BC42016: Implicit conversion from 'Base' to 'Derived'.
        Dim x1 As new MyDelegate(AddressOf goo)
                                 ~~~~~~~~~~~~~
                                       </expected>)
        End Sub

        <Fact>
        Public Sub NewDelegateWithLambdaExpression()
            For Each OptionStrict In {"On", "Off"}
                Dim source =
    <compilation>
        <file name="a.vb">
    option strict <%= OptionStrict %>
    IMPORTS SYStEM
    Delegate Sub D1()
    Delegate Function D2() as String

    Module Program
        Sub Main(args As String())
            Dim x As New D1(Sub() Console.WriteLine("Hello from lambda."))
            x
            Dim y As New D2(Function() "Hello from lambda 2.")
            console.writeline(y.Invoke())

            Dim z as Func(Of String) = Function() "Hello from lambda 3."
            console.writeline(z.Invoke())
        End Sub
    End Module
    </file>
    </compilation>

                Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

                CompileAndVerify(source,
                                expectedOutput:=<![CDATA[
Hello from lambda.
Hello from lambda 2.
Hello from lambda 3.
    ]]>)
            Next
        End Sub

        ''' bug 7319
        <Fact>
        Public Sub AddressOfAndGenerics1()
            Dim comp1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Imports System
Module M1
    Sub Bar(Of T)(ByVal x As T) 
        Console.WriteLine(x)
    End Sub

    Delegate Sub Goo(p as Byte)

    Sub Main()
        Dim x As new Goo(AddressOf Bar(Of String))
        x.Invoke(23)
    End Sub
End Module
        </file>
    </compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(comp1, expectedOutput:="23")
        End Sub

        <Fact>
        Public Sub ConversionsOptBackExceedTargetParameter()
            Dim source =
    <compilation>
        <file name="a.vb">
        Imports System
Module Program
    Delegate Sub DelByRefExpanded(ByRef a as DelegateByRefParamArray_Derived, ByRef b as DelegateByRefParamArray_Derived, ByRef b as DelegateByRefParamArray_Derived)

    Sub Main()
        ' this does crash in Dev10. The bug was resolved as won't fix.
        Dim x As DelByRefExpanded = addressof DelegateByRefParamArray.ByRefParamAndParamArraySubOfBase
        dim derived0 as new DelegateByRefParamArray_Derived(1)
        dim derived1 as new DelegateByRefParamArray_Derived(2)
        dim derived2 as new DelegateByRefParamArray_Derived(3)
        x(derived0, derived1, derived2)

        Console.WriteLine("ByRefParamArraySubOfBase returned: " &amp; derived0.field &amp; " " &amp; derived1.field &amp; " " &amp; derived2.field)  
    End Sub
End Module
        </file>
    </compilation>

            CompileAndVerify(source,
                references:={TestReferences.SymbolsTests.DelegateImplementation.DelegateByRefParamArray},
                expectedOutput:=<![CDATA[
ByRefParamArraySubOfBase returned: 23 2 3
    ]]>)

        End Sub

        <Fact>
        Public Sub CaptureReceiverUsedByStub()
            Dim source =
    <compilation>
        <file name="a.vb">
Option Strict Off
Imports System

Module Module1

    Sub Main()
        Test1()
        Test2()
        Test3()
        Test4()

        Dim x As New C1()
        x.Test5()
        x.Test6()

        Dim y As New S1()
        y.Test7()
        y.Test8()

        Test11()
        Test12()
        Test13()
        Test14()

        Test15()
        Test16()

        Test17()
    End Sub

    Sub Test1()
        System.Console.WriteLine("-- Test1 --")
        Dim d As Func(Of Integer) = AddressOf SideEffect1().F1
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
    End Sub

    Sub Test2()
        System.Console.WriteLine("-- Test2 --")
        Dim d As Func(Of Long) = AddressOf SideEffect1().F1
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
    End Sub

    Sub Test3()
        System.Console.WriteLine("-- Test3 --")
        Dim d As Func(Of Integer) = AddressOf SideEffect2().F1
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
    End Sub

    Sub Test4()
        System.Console.WriteLine("-- Test4 --")
        Dim d As Func(Of Long) = AddressOf SideEffect2().F1
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
    End Sub

    Sub Test11()
        System.Console.WriteLine("-- Test11 --")
        Dim d As Func(Of Integer) = AddressOf SideEffect1().F2
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
    End Sub

    Sub Test12()
        System.Console.WriteLine("-- Test12 --")
        Dim d As Func(Of Long) = AddressOf SideEffect1().F2
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
    End Sub

    Sub Test13()
        System.Console.WriteLine("-- Test13 --")
        Dim d As Func(Of Integer) = AddressOf SideEffect2().F2
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
    End Sub

    Sub Test14()
        System.Console.WriteLine("-- Test14 --")
        Dim d As Func(Of Long) = AddressOf SideEffect2().F2
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
    End Sub

    Sub Test15()
        System.Console.WriteLine("-- Test15 --")
        Dim s As Object = New S1()
        Dim d As Func(Of Integer) = AddressOf s.GetHashCode
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
    End Sub

    Sub Test16()
        System.Console.WriteLine("-- Test16 --")
        Dim s As Object = New S1()
        Dim d As Func(Of Long) = AddressOf s.GetHashCode
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
    End Sub

    Public ReadOnly Property P1 As C1
        Get
            System.Console.WriteLine("P1")
            Return New C1()
        End Get
    End Property

    Sub Test17()
        System.Console.WriteLine("-- Test17 --")
        Dim d As Func(Of Integer) = AddressOf P1.F1
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
    End Sub

    Function SideEffect1() As C1
        System.Console.WriteLine("SideEffect1")
        Return New C1()
    End Function

    Function SideEffect2() As S1
        System.Console.WriteLine("SideEffect2")
        Return New S1()
    End Function

End Module


Class C1
    Public f As Integer

    Function F1() As Integer
        f = f + 1
        Return f
    End Function

    Shared Function F2() As Integer
        Return 100
    End Function

    Sub Test5()
        System.Console.WriteLine("-- Test5 --")
        Dim d As Func(Of Integer) = AddressOf Me.F1
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
        System.Console.WriteLine(f)
    End Sub

    Sub Test6()
        System.Console.WriteLine("-- Test6 --")
        Dim d As Func(Of Long) = AddressOf Me.F1
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
        System.Console.WriteLine(f)
    End Sub
End Class

Structure S1
    Public f As Integer

    Function F1() As Integer
        f = f - 1
        Return f
    End Function

    Shared Function F2() As Integer
        Return -100
    End Function

    Sub Test7()
        System.Console.WriteLine("-- Test7 --")
        Dim d As Func(Of Integer) = AddressOf Me.F1
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
        System.Console.WriteLine(f)
    End Sub

    Sub Test8()
        System.Console.WriteLine("-- Test8 --")
        Dim d As Func(Of Long) = AddressOf Me.F1
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
        System.Console.WriteLine(f)
    End Sub

    Public Overrides Function GetHashCode() As Integer
        f = f - 10
        Return f
    End Function
End Structure
        </file>
    </compilation>

            CompileAndVerify(source,
                         expectedOutput:=<![CDATA[
-- Test1 --
SideEffect1
1
2
-- Test2 --
SideEffect1
1
2
-- Test3 --
SideEffect2
-1
-2
-- Test4 --
SideEffect2
-1
-2
-- Test5 --
1
2
2
-- Test6 --
3
4
4
-- Test7 --
-1
-2
0
-- Test8 --
-1
-2
0
-- Test11 --
100
100
-- Test12 --
100
100
-- Test13 --
-100
-100
-- Test14 --
-100
-100
-- Test15 --
-10
-20
-- Test16 --
-10
-20
-- Test17 --
P1
1
2
]]>)

        End Sub

        <WorkItem(9029, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub DelegateRelaxationConversions_Simple()

            Dim source =
<compilation>
    <file name="a.vb">

Imports System
Module Test
    Sub goo(x As Func(Of Exception, Exception), y As Func(Of Exception, ArgumentException), z As Func(Of Exception, ArgumentException), a As Func(Of Exception, Exception), b As Func(Of ArgumentException, Exception), c As Func(Of ArgumentException, Exception))
        Console.WriteLine("goo")
    End Sub
    Sub Main()
        Dim f1 As Func(Of Exception, ArgumentException) = Function(a As Exception) New ArgumentException()
        Dim f2 As Func(Of ArgumentException, Exception) = Function(a As ArgumentException) New ArgumentException()
        Dim f As Func(Of Exception, Exception) = Function(a As Exception) New ArgumentException
        f = f2
        f = f1
        f1 = f2
        f2 = f1
        goo(f1, f1, f1, f1, f2, f2)
    End Sub
End Module
    </file>
</compilation>
            Dim c = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, options:=TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(c,
<expected>
BC42016: Implicit conversion from 'Func(Of ArgumentException, Exception)' to 'Func(Of Exception, Exception)'; this conversion may fail because 'Exception' is not derived from 'ArgumentException', as required for the 'In' generic parameter 'T' in 'Delegate Function Func(Of In T, Out TResult)(arg As T) As TResult'.
        f = f2
            ~~
BC42016: Implicit conversion from 'Func(Of ArgumentException, Exception)' to 'Func(Of Exception, ArgumentException)'; this conversion may fail because 'Exception' is not derived from 'ArgumentException', as required for the 'Out' generic parameter 'TResult' in 'Delegate Function Func(Of In T, Out TResult)(arg As T) As TResult'.
        f1 = f2
             ~~
</expected>)

            c = c.WithOptions(c.Options.WithOptionStrict(OptionStrict.On))
            CompilationUtils.AssertTheseDiagnostics(c,
<expected>
BC36755: 'Func(Of ArgumentException, Exception)' cannot be converted to 'Func(Of Exception, Exception)' because 'Exception' is not derived from 'ArgumentException', as required for the 'In' generic parameter 'T' in 'Delegate Function Func(Of In T, Out TResult)(arg As T) As TResult'.
        f = f2
            ~~
BC36754: 'Func(Of ArgumentException, Exception)' cannot be converted to 'Func(Of Exception, ArgumentException)' because 'Exception' is not derived from 'ArgumentException', as required for the 'Out' generic parameter 'TResult' in 'Delegate Function Func(Of In T, Out TResult)(arg As T) As TResult'.
        f1 = f2
             ~~
</expected>)
        End Sub

        <WorkItem(542068, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542068")>
        <Fact>
        Public Sub DelegateBindingForGenericMethods01()
            For Each OptionStrict In {"On", "Off"}
                Dim source =
    <compilation>
        <file name="a.vb">
Option Strict <%= OptionStrict %>
Imports System
Imports System.Collections.Generic

' VB has different behavior between whether the below class is generic 
' or non-generic. The below code produces no errors. However, if I get
' rid of the "(Of T, U)" bit in the below line, the code suddenly
' starts reporting error BC30794 (No 'goo' is most specific).
Public Class Runner(Of T, U)
    Delegate Function Del1(Of TT, UU)(
        x As TT,
        y As List(Of TT),
        z As Dictionary(Of List(Of TT), UU)) As UU

    Delegate Sub Del2(Of TT, UU, VV)(
        x As Func(Of TT, List(Of TT), UU, Dictionary(Of List(Of TT), UU)),
        y As Del1(Of UU, VV),
        z As Action(Of VV, List(Of VV), Dictionary(Of List(Of VV), TT)))

    'Most specific overload in Dev10 VB
    'In Dev10 C# this overload is less specific - but in Dev10 VB this is (strangely) more specific
    Sub goo(Of TT, UU, VV)(
        xx As TT,
        yy As UU,
        zz As VV)
        Console.Write("pass")
    End Sub

    'Can bind to this overload - but above overload is more specific in Dev10 VB
    'In Dev10 C# this overload is more specific - but in Dev10 VB this is (strangely) less specific
    Sub goo(Of TT, UU, VV)(
        x As Func(Of TT, List(Of TT), UU, Dictionary(Of List(Of TT), UU)),
        y As Del1(Of UU, VV),
        z As Action(Of VV, List(Of VV), Dictionary(Of List(Of VV), TT)))
        Console.Write("fail")
    End Sub

    'Unrelated overload
    Sub goo(Of TT, UU, VV)(
        x As Func(Of TT, UU, VV),
        y As Func(Of UU, VV, TT),
        z As Func(Of VV, TT, UU))
        Console.Write("fail2")
    End Sub

    Public Sub Run(Of AA, BB, CC)()
        Dim d As Del2(Of AA, BB, CC) = AddressOf goo
        Dim d2 As Del2(Of Long, Long, Long) = AddressOf goo
        d(Nothing, Nothing, Nothing)
        d2(Nothing, Nothing, Nothing)
    End Sub
End Class

Module Runner
    Sub Main()
        Dim t As New Runner(Of Long, Long)
        t.Run(Of Long, Long, Long)()
    End Sub
End Module
    </file>
    </compilation>

                Dim c = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
                CompilationUtils.AssertNoErrors(c)
                CompileAndVerify(c, expectedOutput:="passpass")
            Next
        End Sub

        <Fact>
        Public Sub DelegateBindingForGenericMethods02()
            For Each OptionStrict In {"Off", "On"}
                Dim source =
    <compilation>
        <file name="a.vb">
Option Strict <%= OptionStrict %>
Imports System
Imports System.Collections.Generic

Class Runner(Of T, U)
    Delegate Function Del1(Of TT, UU)(
        x As TT,
        y As List(Of TT),
        z As Dictionary(Of List(Of TT), UU)) As UU

    Delegate Sub Del2(Of TT, UU, VV)(
        x As Func(Of TT, List(Of TT), UU, Dictionary(Of List(Of TT), UU)),
        y As Del1(Of UU, VV),
        z As Action(Of VV, List(Of VV), Dictionary(Of List(Of VV), TT)))

    'Should bind to this overload
    Sub goo(Of TT, UU, VV)(
        x As Func(Of TT, List(Of TT), UU, Dictionary(Of List(Of TT), UU)),
        y As Del1(Of UU, VV),
        z As Action(Of VV, List(Of VV), Dictionary(Of List(Of VV), TT)))
        Console.Write("pass")
    End Sub

    'Unrelated overload
    Sub goo(Of TT, UU, VV)(
        x As Func(Of TT, UU, VV),
        y As Func(Of UU, VV, TT),
        z As Func(Of VV, TT, UU))
        Console.Write("fail")
    End Sub

    Public Sub Run(Of AA, BB, CC)()
        Dim d As Del2(Of AA, BB, CC) = AddressOf goo
        Dim d2 As Del2(Of Long, Long, Long) = AddressOf goo
        d(Nothing, Nothing, Nothing)
        d2(Nothing, Nothing, Nothing)
    End Sub
End Class

Module Test
    Sub Main()
        Dim t As New Runner(Of Long, Long)
        t.Run(Of Long, Long, Long)()
    End Sub
End Module
    </file>
    </compilation>

                Dim c = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
                CompilationUtils.AssertNoErrors(c)
                CompileAndVerify(c, expectedOutput:="passpass")
            Next
        End Sub

        <Fact>
        Public Sub DelegateBindingForGenericMethods03()
            For Each OptionStrict In {"Off", "On"}
                Dim source =
    <compilation>
        <file name="a.vb">
Option Strict <%= OptionStrict %>
Imports System
Imports System.Collections.Generic

Class Runner(Of T, U)
    Delegate Function Del1(Of TT, UU)(
        x As TT,
        y As List(Of TT),
        z As Dictionary(Of List(Of TT), UU)) As UU

    Delegate Sub Del2(Of TT, UU, VV)(
        x As Func(Of TT, List(Of TT), UU, Dictionary(Of List(Of TT), UU)),
        y As Del1(Of UU, VV),
        z As Action(Of VV, List(Of VV), Dictionary(Of List(Of VV), TT)))

    'Should bind to this overload
    Sub goo(Of TT, UU, VV)(
        xx As TT,
        yy As UU,
        zz As VV)
        Console.Write("pass")
    End Sub

    'Unrelated overload
    Sub goo(Of TT, UU, VV)(
        x As Func(Of TT, UU, VV),
        y As Func(Of UU, VV, TT),
        z As Func(Of VV, TT, UU))
        Console.Write("fail")
    End Sub

    Public Sub Run(Of AA, BB, CC)()
        Dim d As Del2(Of AA, BB, CC) = AddressOf goo
        Dim d2 As Del2(Of Long, Long, Long) = AddressOf goo
        d(Nothing, Nothing, Nothing)
        d2(Nothing, Nothing, Nothing)
    End Sub
End Class

Module Test
    Sub Main()
        Dim t As New Runner(Of Long, Long)
        t.Run(Of Long, Long, Long)()
    End Sub
End Module
    </file>
    </compilation>

                CompileAndVerify(source, expectedOutput:="passpass").VerifyDiagnostics()
            Next
        End Sub

        <Fact()>
        Public Sub ZeroArgumentRelaxationVsOtherNarrowing_1()
            Dim source =
    <compilation>
        <file name="a.vb">
Option Strict Off
Imports System

Module Test
    Sub Test111(x As Integer)
        System.Console.WriteLine("Test111(x As Integer)")
    End Sub

    Sub Test111(x As Byte)
        System.Console.WriteLine("Test111(x As Byte)")
    End Sub

    Sub Test111()
        System.Console.WriteLine("Test111()")
    End Sub

    Sub Main()
        Dim ttt1 As Action(Of Long)
        ttt1 = AddressOf Test111
        ttt1(2)
    End Sub
End Module
    </file>
    </compilation>

            CompileAndVerify(source, expectedOutput:="Test111()").VerifyDiagnostics()
        End Sub

        <WorkItem(544065, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544065")>
        <Fact()>
        Public Sub Bug12211()
            Dim source =
    <compilation>
        <file name="a.vb">
Option Strict Off

    Class C1
      Public Shared Narrowing Operator CType(ByVal arg As Long) As c1
            Return New c1
        End Operator
    End Class

    Class C3
        Public Shared Sub goo(Optional ByVal y As C1 = Nothing)
            res1 = "Correct Method called"

            if y is nothing then
                res1 = res1 &amp; " and no parameter was passed."
            end if  

        End Sub

        Public shared sub goo(arg as integer)
        End Sub

    End Class

    Delegate Sub goo6(ByVal arg As Long)

    Friend Module DelOverl0020mod
        Public res1 As String


        Sub Main()
            Dim d6 As goo6 = AddressOf c3.goo
            d6(5L)
            System.Console.WriteLine(res1)
        End Sub
    End Module

    </file>
    </compilation>

#If False Then
            CompileAndVerify(source, expectedOutput:="Correct Method called and no parameter was passed.").VerifyDiagnostics().VerifyIL("DelOverl0020mod.Main",
<![CDATA[
{
  // Code size       32 (0x20)
  .maxstack  2
  .locals init (goo6 V_0) //d6
  IL_0000:  ldnull
  IL_0001:  ldftn      "Sub DelOverl0020mod._Lambda$__1(Long)"
  IL_0007:  newobj     "Sub goo6..ctor(Object, System.IntPtr)"
  IL_000c:  stloc.0
  IL_000d:  ldloc.0
  IL_000e:  ldc.i4.5
  IL_000f:  conv.i8
  IL_0010:  callvirt   "Sub goo6.Invoke(Long)"
  IL_0015:  ldsfld     "DelOverl0020mod.res1 As String"
  IL_001a:  call       "Sub System.Console.WriteLine(String)"
  IL_001f:  ret
}
]]>).VerifyIL("DelOverl0020mod._Lambda$__1",
<![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  call       "Sub C3.goo(C1)"
  IL_0006:  ret
}
]]>)
#Else
            ' According to the spec, zero argument relaxation can be used only when target method has NO parameters.
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected>
BC30950: No accessible method 'goo' has a signature compatible with delegate 'Delegate Sub goo6(arg As Long)':
    'Public Shared Sub goo([y As C1 = Nothing])': Argument matching parameter 'y' narrows from 'Long' to 'C1'.
    'Public Shared Sub goo(arg As Integer)': Argument matching parameter 'arg' narrows from 'Long' to 'Integer'.
            Dim d6 As goo6 = AddressOf c3.goo
                                       ~~~~~~
</expected>)
#End If

        End Sub

        <Fact(), WorkItem(545253, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545253")>
        Public Sub Bug13571()
            Dim source =
    <compilation>
        <file name="a.vb">
Imports System            

Class C1
    Event e As Action(Of Exception)
    Sub Goo(ParamArray x() As Integer) Handles MyClass.e
    End Sub

    Sub Test()
        Dim e1 As Action(Of Exception) = AddressOf Goo
    End Sub
End Class

Class C2
    Event e As Action(Of Exception)
    Sub Goo(Optional x As Integer = 2) Handles MyClass.e
    End Sub

    Sub Test()
        Dim e1 As Action(Of Exception) = AddressOf Goo
    End Sub
End Class
    </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseDll)

            AssertTheseDiagnostics(compilation,
<expected>
BC31029: Method 'Goo' cannot handle event 'e' because they do not have a compatible signature.
    Sub Goo(ParamArray x() As Integer) Handles MyClass.e
                                                       ~
BC31143: Method 'Public Sub Goo(ParamArray x As Integer())' does not have a signature compatible with delegate 'Delegate Sub Action(Of Exception)(obj As Exception)'.
        Dim e1 As Action(Of Exception) = AddressOf Goo
                                                   ~~~
BC31029: Method 'Goo' cannot handle event 'e' because they do not have a compatible signature.
    Sub Goo(Optional x As Integer = 2) Handles MyClass.e
                                                       ~
BC31143: Method 'Public Sub Goo([x As Integer = 2])' does not have a signature compatible with delegate 'Delegate Sub Action(Of Exception)(obj As Exception)'.
        Dim e1 As Action(Of Exception) = AddressOf Goo
                                                   ~~~
</expected>)
        End Sub

        <Fact>
        Public Sub DelegateMethodDelegates()
            Dim source =
<compilation>
    <file>
Imports System

Class C
    Dim invoke As Action
    Dim beginInvoke As Func(Of AsyncCallback, Object, IAsyncResult)
    Dim endInvoke As Action(Of IAsyncResult)
    Dim dynamicInvoke As Func(Of Object(), Object)
    Dim clone As Func(Of Object)
    Dim eql As Func(Of Object, Boolean)

    Sub M()
        Dim a = New Action(AddressOf M)
        invoke = New Action(AddressOf a.Invoke)
        beginInvoke = New Func(Of AsyncCallback, Object, IAsyncResult)(AddressOf a.BeginInvoke)
        endInvoke = New Action(Of IAsyncResult)(AddressOf a.EndInvoke)
        dynamicInvoke = New Func(Of Object(), Object)(AddressOf a.DynamicInvoke)
        clone = New Func(Of Object)(AddressOf a.Clone)
        eql = New Func(Of Object, Boolean)(AddressOf a.Equals)
    End Sub
End Class
    </file>
</compilation>

            ' Dev11 emits ldvirtftn, we emit ldftn
            CompileAndVerify(source).VerifyIL("C.M", <![CDATA[
{
  // Code size      124 (0x7c)
  .maxstack  3
  .locals init (System.Action V_0) //a
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Sub C.M()"
  IL_0007:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_000c:  stloc.0
  IL_000d:  ldarg.0
  IL_000e:  ldloc.0
  IL_000f:  ldftn      "Sub System.Action.Invoke()"
  IL_0015:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_001a:  stfld      "C.invoke As System.Action"
  IL_001f:  ldarg.0
  IL_0020:  ldloc.0
  IL_0021:  ldftn      "Function System.Action.BeginInvoke(System.AsyncCallback, Object) As System.IAsyncResult"
  IL_0027:  newobj     "Sub System.Func(Of System.AsyncCallback, Object, System.IAsyncResult)..ctor(Object, System.IntPtr)"
  IL_002c:  stfld      "C.beginInvoke As System.Func(Of System.AsyncCallback, Object, System.IAsyncResult)"
  IL_0031:  ldarg.0
  IL_0032:  ldloc.0
  IL_0033:  ldftn      "Sub System.Action.EndInvoke(System.IAsyncResult)"
  IL_0039:  newobj     "Sub System.Action(Of System.IAsyncResult)..ctor(Object, System.IntPtr)"
  IL_003e:  stfld      "C.endInvoke As System.Action(Of System.IAsyncResult)"
  IL_0043:  ldarg.0
  IL_0044:  ldloc.0
  IL_0045:  ldftn      "Function System.Delegate.DynamicInvoke(ParamArray Object()) As Object"
  IL_004b:  newobj     "Sub System.Func(Of Object(), Object)..ctor(Object, System.IntPtr)"
  IL_0050:  stfld      "C.dynamicInvoke As System.Func(Of Object(), Object)"
  IL_0055:  ldarg.0
  IL_0056:  ldloc.0
  IL_0057:  dup
  IL_0058:  ldvirtftn  "Function System.Delegate.Clone() As Object"
  IL_005e:  newobj     "Sub System.Func(Of Object)..ctor(Object, System.IntPtr)"
  IL_0063:  stfld      "C.clone As System.Func(Of Object)"
  IL_0068:  ldarg.0
  IL_0069:  ldloc.0
  IL_006a:  dup
  IL_006b:  ldvirtftn  "Function System.MulticastDelegate.Equals(Object) As Boolean"
  IL_0071:  newobj     "Sub System.Func(Of Object, Boolean)..ctor(Object, System.IntPtr)"
  IL_0076:  stfld      "C.eql As System.Func(Of Object, Boolean)"
  IL_007b:  ret
}
]]>)
        End Sub

        <Fact(), WorkItem(629369, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/629369")>
        Public Sub DelegateConversionOfNothing()
            Dim source =
    <compilation>
        <file name="a.vb">
Imports System
Module Module1
 
    Sub Main()
        Dim f As Func(Of Integer, Integer)
        Dim f2 = Function(x As Integer) x
        f2 = Nothing
        f = f2
        Console.WriteLine(If(f Is Nothing, "pass", "fail"))
    End Sub
 
End Module
    </file>
    </compilation>
            CompileAndVerify(source, expectedOutput:="pass").VerifyDiagnostics()
        End Sub

        <Fact(), WorkItem(629369, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/629369")>
        Public Sub DelegateConversionOfNothing_02()
            Dim source =
    <compilation>
        <file name="a.vb">
Imports System
Module Module1
 
    Sub Main()
        Dim f As Func(Of Integer, Integer)
        Dim f2 = Function(x As Integer) CShort(x)
        f2 = Nothing
        f = f2
        Console.WriteLine(If(f Is Nothing, "pass", "fail"))
    End Sub
 
End Module
    </file>
    </compilation>
            CompileAndVerify(source, expectedOutput:="pass").VerifyDiagnostics()
        End Sub

    End Class
End Namespace
