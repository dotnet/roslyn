' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class BindingDelegateCreationTests
        Inherits BasicTestBase

        <Fact>
        Public Sub InvalidDelegateAddressOfTest()
            Dim source = <compilation name="InvalidDelegateAddressOfTest">
                             <file name="a.vb">
Imports System

Delegate Sub SubDel(p As String)
Delegate Sub SubDel2(p As UnknownErrorType)
Delegate function FuncDel(p As String) as Integer
Delegate function FuncDel2(p As String) as UnknownErrorType

Class C2

    Public Shared Sub foo(p as string)
    end sub

    Public Shared Sub foo2(p as string, p2 as integer)
    end sub

    Public Sub AssignDelegates()
        Dim v5 as subdel = addressof 
        Dim v6 as subdel = addressof nothing
        Dim v7 as subdel = addressof C2
        Dim v8 as subdel = addressof C2.foo2
        Dim v9 as FuncDel = addressof C2.foo
    End Sub
End Class
                    </file>
                         </compilation>

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)

            CompilationUtils.AssertTheseDiagnostics(c1,
<expected>
BC30002: Type 'UnknownErrorType' is not defined.
Delegate Sub SubDel2(p As UnknownErrorType)
                          ~~~~~~~~~~~~~~~~
BC30002: Type 'UnknownErrorType' is not defined.
Delegate function FuncDel2(p As String) as UnknownErrorType
                                           ~~~~~~~~~~~~~~~~
BC30201: Expression expected.
        Dim v5 as subdel = addressof 
                                     ~
BC30577: 'AddressOf' operand must be the name of a method (without parentheses).
        Dim v6 as subdel = addressof nothing
                                     ~~~~~~~
BC30577: 'AddressOf' operand must be the name of a method (without parentheses).
        Dim v7 as subdel = addressof C2
                                     ~~
BC31143: Method 'Public Shared Sub foo2(p As String, p2 As Integer)' does not have a signature compatible with delegate 'Delegate Sub SubDel(p As String)'.
        Dim v8 as subdel = addressof C2.foo2
                                     ~~~~~~~
BC31143: Method 'Public Shared Sub foo(p As String)' does not have a signature compatible with delegate 'Delegate Function FuncDel(p As String) As Integer'.
        Dim v9 as FuncDel = addressof C2.foo
                                      ~~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub DelegateAddressOfMethods()
            For Each optionValue In {"On", "Off"}
                Dim source =
    <compilation name="DelegateAddressOfMethods">
        <file name="a.vb">
option strict <%= optionValue %> 
Imports System        
                ' delegate as type
    Delegate Function FuncDel(param1 as Integer, param2 as String) as Char

Class C2
            Public intMember As Integer

            Public Sub delimpl(param1 As Integer, ByRef param2 As String)
            End Sub
        End Class

        Class C1
            ' delegate as nested type
            Delegate Sub SubDel(param1 As Integer, ByRef param2 As String)

            Delegate Sub SubGenDel(Of T)(param1 As T)
            Delegate Function FuncGenDel(Of T)(param1 As Integer) As T

            Shared Sub delimpl(param1 As Integer, ByRef param2 As String)
            End Sub

            Public Shared Sub sub1()
                Dim d As SubDel = AddressOf delimpl
                Console.WriteLine(d)
                Dim c2i As New C2()
                d = AddressOf c2i.delimpl
                Console.WriteLine(d)
            End Sub
        End Class

Module M1
            Sub Main(args As String())
                C1.sub1()
            End Sub
        End Module

    </file>
    </compilation>

                CompileAndVerify(source,
                                 expectedOutput:=<![CDATA[
C1+SubDel
C1+SubDel
    ]]>)
            Next
        End Sub

        <Fact>
        Public Sub Error_ERR_AddressOfNotDelegate1()
            Dim source = <compilation name="Error_ERR_AddressOfNotDelegate1">
                             <file name="a.vb">
Imports System

Delegate Sub SubDel(p As String)
Delegate function FuncDel(p As String) as Integer

Class C2

    Public Shared Sub foo(p as string)
    end sub

    Public Shared Sub foo2(p as string, p2 as integer)
    end sub

    Public Sub AssignDelegates()
        Dim v1 As C2 = AddressOf C2.foo
        Dim v2 As Object = AddressOf C2.foo
    End Sub
End Class
                    </file>
                         </compilation>

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)

            CompilationUtils.AssertTheseDiagnostics(c1,
<expected>
BC30581: 'AddressOf' expression cannot be converted to 'C2' because 'C2' is not a delegate type.
        Dim v1 As C2 = AddressOf C2.foo
                       ~~~~~~~~~~~~~~~~
BC30581: 'AddressOf' expression cannot be converted to 'Object' because 'Object' is not a delegate type.
        Dim v2 As Object = AddressOf C2.foo
                           ~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Error_ERR_DelegateNoInvoke1()
            Dim source =
<compilation name="Error_ERR_DelegateNoInvoke1">
    <file name="a.vb">
Imports System

Class C1
    Public Shared Sub ds3(p As String)
        Console.WriteLine("C1.ds3 " + p)
    End Sub
    Public Shared Function df3(p As Integer) As Integer
        return 3 + p
    End Function
End Class

Module Program
    Sub Main(args As String())

        Dim metaSubDel as DelegateWithoutInvoke.DelegateSubWithoutInvoke = addressof C1.ds3
        metaSubDel("foo")

        Dim metaFuncDel as DelegateWithoutInvoke.DelegateFunctionWithoutInvoke = addressof C1.df3
        Console.WriteLine(metaFuncDel("foo"))

    End Sub
End Module
    </file>
</compilation>
            Dim ref = MetadataReference.CreateFromImage(TestResources.General.DelegatesWithoutInvoke.AsImmutableOrNull())
            Dim c1 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, {ref}, TestOptions.ReleaseExe)
            CompilationUtils.AssertTheseDiagnostics(c1,
<errors>
BC30657: 'DelegateWithoutInvoke.DelegateSubWithoutInvoke' has a return type that is not supported or parameter types that are not supported.
        Dim metaSubDel as DelegateWithoutInvoke.DelegateSubWithoutInvoke = addressof C1.ds3
                                                                           ~~~~~~~~~~~~~~~~
BC30220: Delegate class 'DelegateWithoutInvoke.DelegateSubWithoutInvoke' has no Invoke method, so an expression of this type cannot be the target of a method call.
        metaSubDel("foo")
        ~~~~~~~~~~
BC30657: 'DelegateWithoutInvoke.DelegateFunctionWithoutInvoke' has a return type that is not supported or parameter types that are not supported.
        Dim metaFuncDel as DelegateWithoutInvoke.DelegateFunctionWithoutInvoke = addressof C1.df3
                                                                                 ~~~~~~~~~~~~~~~~
BC30220: Delegate class 'DelegateWithoutInvoke.DelegateFunctionWithoutInvoke' has no Invoke method, so an expression of this type cannot be the target of a method call.
        Console.WriteLine(metaFuncDel("foo"))
                          ~~~~~~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub Error_ERR_DelegateBindingIncompatible2()
            Dim source = <compilation name="Error_ERR_DelegateBindingIncompatible2">
                             <file name="a.vb">
option strict on

Imports System

Delegate sub SubDel(p As Integer)

Class C1
End Class

Class C2
    ' no match because of too many args / one candidate
    Public Shared sub foo1(p as integer, p2 as string)
    end sub

    ' no match because of too few args / one candidate
    Public Shared sub foo2()
    end sub

    ' no match because of no conversion
    Public Shared sub foo3(p as C1)
    end sub

    ' no match because of no conversion
    Public Shared sub foo4(byref p as C1)
    end sub

    Public Sub AssignDelegates()
        Dim v1 As SubDel = AddressOf C2.foo1
        Dim v2 As SubDel = AddressOf C2.foo2
        Dim v3 As SubDel = AddressOf C2.foo3
        Dim v4 As SubDel = AddressOf C2.foo4
    end sub
End Class
                             </file>
                         </compilation>

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)

            CompilationUtils.AssertTheseDiagnostics(c1,
<expected>
BC31143: Method 'Public Shared Sub foo1(p As Integer, p2 As String)' does not have a signature compatible with delegate 'Delegate Sub SubDel(p As Integer)'.
        Dim v1 As SubDel = AddressOf C2.foo1
                                     ~~~~~~~
BC36663: Option Strict On does not allow narrowing in implicit type conversions between method 'Public Shared Sub foo2()' and delegate 'Delegate Sub SubDel(p As Integer)'.
        Dim v2 As SubDel = AddressOf C2.foo2
                                     ~~~~~~~
BC31143: Method 'Public Shared Sub foo3(p As C1)' does not have a signature compatible with delegate 'Delegate Sub SubDel(p As Integer)'.
        Dim v3 As SubDel = AddressOf C2.foo3
                                     ~~~~~~~
BC31143: Method 'Public Shared Sub foo4(ByRef p As C1)' does not have a signature compatible with delegate 'Delegate Sub SubDel(p As Integer)'.
        Dim v4 As SubDel = AddressOf C2.foo4
                                     ~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Error_ERR_NoArgumentCountOverloadCandidates1()
            Dim source = <compilation name="Error_ERR_NoArgumentCountOverloadCandidates1">
                             <file name="a.vb">
option strict on

Imports System

Delegate sub SubDel(p As Integer)
Delegate sub SubDel2(p As Integer, p as Integer, p as Integer)

Class C2
    ' no match because of too many args / multiple candidates
    Public Shared sub foo1(p as integer, p2 as string)
    end sub

    ' no match because of too many args / multiple candidates
    Public Shared sub foo1(p as integer, p2 as integer)
    end sub

    ' no match because of too few many args / multiple candidates
    Public Shared sub foo2(p as integer, p2 as integer)
    end sub

    ' no match because of too few args / multiple candidates
    Public Shared sub foo2(p as integer)
    end sub

    ' no match because of too many args / multiple candidates
    Public Shared sub foo3(p as integer, byref p2 as string)
    end sub

    ' no match because of too many args / multiple candidates
    Public Shared sub foo3(p as integer, byref p2 as integer)
    end sub

    ' no match because of too few many args / multiple candidates
    Public Shared sub foo4(p as integer, byref p2 as integer)
    end sub

    ' no match because of too few args / multiple candidates
    Public Shared sub foo4(byref p as integer)
    end sub


    Public Sub AssignDelegates()
        Dim v1 As SubDel = AddressOf C2.foo1
        Dim v2 As SubDel2 = AddressOf C2.foo2
        Dim v3 As SubDel = AddressOf C2.foo3
        Dim v4 As SubDel2 = AddressOf C2.foo4
    end sub
End Class
                             </file>
                         </compilation>

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)

            CompilationUtils.AssertTheseDiagnostics(c1,
<expected>
BC30516: Overload resolution failed because no accessible 'foo1' accepts this number of arguments.
        Dim v1 As SubDel = AddressOf C2.foo1
                                     ~~~~~~~
BC30516: Overload resolution failed because no accessible 'foo2' accepts this number of arguments.
        Dim v2 As SubDel2 = AddressOf C2.foo2
                                      ~~~~~~~
BC30516: Overload resolution failed because no accessible 'foo3' accepts this number of arguments.
        Dim v3 As SubDel = AddressOf C2.foo3
                                     ~~~~~~~
BC30516: Overload resolution failed because no accessible 'foo4' accepts this number of arguments.
        Dim v4 As SubDel2 = AddressOf C2.foo4
                                      ~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Error_ERR_AmbiguousDelegateBinding2()
            Dim source = <compilation name="Error_ERR_AmbiguousDelegateBinding2">
                             <file name="a.vb">
option strict on

Imports System

Delegate Sub SubDel(p As Byte)

Class C2(of T, S)
    Public Shared Sub foo(p as T)
    end sub

    Public Shared Sub foo(p as S)
    end sub

    Public Sub AssignDelegates()
        Dim v1 As SubDel = AddressOf C2(of integer, integer).foo
    End Sub
End Class
                    </file>
                         </compilation>

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)

            CompilationUtils.AssertTheseDiagnostics(c1,
<expected>
BC30794: No accessible 'foo' is most specific: 
    Public Shared Sub foo(p As Integer)
    Public Shared Sub foo(p As Integer)
        Dim v1 As SubDel = AddressOf C2(of integer, integer).foo
                                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Error_ERR_AmbiguousDelegateBinding2_with_ByRef_StrictOff()
            Dim source = <compilation name="Error_ERR_AmbiguousDelegateBinding2_with_ByRef_StrictOff">
                             <file name="a.vb">
option strict off

Imports System

Delegate Sub SubDel(p As Byte)

Class C2(of T, S)
    Public Shared Sub foo(byref p as T)
    end sub

    Public Shared Sub foo(byref p as S)
    end sub

    Public Sub AssignDelegates()
        Dim v1 As SubDel = AddressOf C2(of integer, integer).foo
    End Sub
End Class
                    </file>
                         </compilation>

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)

            CompilationUtils.AssertTheseDiagnostics(c1,
<expected>
BC30794: No accessible 'foo' is most specific: 
    Public Shared Sub foo(ByRef p As Integer)
    Public Shared Sub foo(ByRef p As Integer)
        Dim v1 As SubDel = AddressOf C2(of integer, integer).foo
                                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Error_ERR_AmbiguousDelegateBinding2_with_ByRef_StrictOn()
            Dim source = <compilation name="Error_ERR_AmbiguousDelegateBinding2_with_ByRef_StrictOn">
                             <file name="a.vb">
option strict on

Imports System

Delegate Sub SubDel(p As Byte)

Class C2(of T, S)
    Public Shared Sub foo(byref p as T)
    end sub

    Public Shared Sub foo(byref p as S)
    end sub

    Public Sub AssignDelegates()
        Dim v1 As SubDel = AddressOf C2(of integer, integer).foo
    End Sub
End Class
                    </file>
                         </compilation>

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)

            CompilationUtils.AssertTheseDiagnostics(c1,
<expected>
BC30794: No accessible 'foo' is most specific: 
    Public Shared Sub foo(ByRef p As Integer)
    Public Shared Sub foo(ByRef p As Integer)
        Dim v1 As SubDel = AddressOf C2(of integer, integer).foo
                                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Error_ERR_DelegateBindingMismatchStrictOff2()
            Dim source = <compilation name="Error_ERR_DelegateBindingMismatchStrictOff2">
                             <file name="a.vb">
option strict on

Imports System

Delegate Sub SubDel(p As Integer)
Delegate Sub SubDel2(p As Integer, p As Integer)
Delegate Function FuncDel() as Byte

Class C2
    Public Shared Sub foo(p as string)
    end sub

    Public Shared Sub foo2(p as string)
    end sub
    Public Shared Sub foo2(p as Byte)
    end sub

    Public Shared Sub foo3(p as string, p2 as string)
    end sub
    Public Shared Sub foo3(p as Byte, p2 as byte)
    end sub
    Public Shared Sub foo3(p as integer, p2 as byte)
    end sub

    Public Shared Sub foo4(p as string)
    end sub

    Public Shared Sub foo5(p as string)
    end sub
    Public Shared Sub foo5(p as Byte)
    end sub

    Public Shared Sub foo6(p as string, p2 as string)
    end sub
    Public Shared Sub foo6(p as Byte, p2 as byte)
    end sub
    Public Shared Sub foo6(p as integer, p2 as byte)
    end sub

    Public Shared Function foo7() as Integer
        return 23
    end function

    Public shared Sub Main()
        Dim v1 As SubDel = AddressOf C2.foo
        Dim v2 As SubDel = AddressOf C2.foo2
        Dim v3 As SubDel2 = AddressOf C2.foo3

        Dim v4 As SubDel = AddressOf C2.foo4
        Dim v5 As SubDel = AddressOf C2.foo5
        Dim v6 As SubDel2 = AddressOf C2.foo6

        Dim v7 as FuncDel = AddressOf C2.foo7
    End Sub
End Class
                    </file>
                         </compilation>

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)

            CompilationUtils.AssertTheseDiagnostics(c1,
<expected>
BC36663: Option Strict On does not allow narrowing in implicit type conversions between method 'Public Shared Sub foo(p As String)' and delegate 'Delegate Sub SubDel(p As Integer)'.
        Dim v1 As SubDel = AddressOf C2.foo
                                     ~~~~~~
BC30950: No accessible method 'foo2' has a signature compatible with delegate 'Delegate Sub SubDel(p As Integer)':
    'Public Shared Sub foo2(p As String)': Argument matching parameter 'p' narrows from 'Integer' to 'String'.
    'Public Shared Sub foo2(p As Byte)': Argument matching parameter 'p' narrows from 'Integer' to 'Byte'.
        Dim v2 As SubDel = AddressOf C2.foo2
                                     ~~~~~~~
BC30950: No accessible method 'foo3' has a signature compatible with delegate 'Delegate Sub SubDel2(p As Integer, p As Integer)':
    'Public Shared Sub foo3(p As String, p2 As String)': Method does not have a signature compatible with the delegate.
    'Public Shared Sub foo3(p As Byte, p2 As Byte)': Method does not have a signature compatible with the delegate.
    'Public Shared Sub foo3(p As Integer, p2 As Byte)': Argument matching parameter 'p2' narrows from 'Integer' to 'Byte'.
        Dim v3 As SubDel2 = AddressOf C2.foo3
                                      ~~~~~~~
BC36663: Option Strict On does not allow narrowing in implicit type conversions between method 'Public Shared Sub foo4(p As String)' and delegate 'Delegate Sub SubDel(p As Integer)'.
        Dim v4 As SubDel = AddressOf C2.foo4
                                     ~~~~~~~
BC30950: No accessible method 'foo5' has a signature compatible with delegate 'Delegate Sub SubDel(p As Integer)':
    'Public Shared Sub foo5(p As String)': Argument matching parameter 'p' narrows from 'Integer' to 'String'.
    'Public Shared Sub foo5(p As Byte)': Argument matching parameter 'p' narrows from 'Integer' to 'Byte'.
        Dim v5 As SubDel = AddressOf C2.foo5
                                     ~~~~~~~
BC30950: No accessible method 'foo6' has a signature compatible with delegate 'Delegate Sub SubDel2(p As Integer, p As Integer)':
    'Public Shared Sub foo6(p As String, p2 As String)': Method does not have a signature compatible with the delegate.
    'Public Shared Sub foo6(p As Byte, p2 As Byte)': Method does not have a signature compatible with the delegate.
    'Public Shared Sub foo6(p As Integer, p2 As Byte)': Argument matching parameter 'p2' narrows from 'Integer' to 'Byte'.
        Dim v6 As SubDel2 = AddressOf C2.foo6
                                      ~~~~~~~
BC36663: Option Strict On does not allow narrowing in implicit type conversions between method 'Public Shared Function foo7() As Integer' and delegate 'Delegate Function FuncDel() As Byte'.
        Dim v7 as FuncDel = AddressOf C2.foo7
                                      ~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub WideningArgumentsDelegateSubRelaxationByRefStrictOn()
            For Each optionValue In {"On"}
                Dim source =
    <compilation name="WideningArgumentsDelegateSubRelaxationByRefStrictOn">
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

                Dim c1 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)

                CompilationUtils.AssertTheseDiagnostics(c1,
<expected>
BC36663: Option Strict On does not allow narrowing in implicit type conversions between method 'Public Sub WideningNumericSub(ByRef p As Integer)' and delegate 'Delegate Sub WideningNumericDelegate(ByRef p As Byte)'.
            Dim d1 as new WideningNumericDelegate(AddressOf ci.WideningNumericSub)
                                                            ~~~~~~~~~~~~~~~~~~~~~
BC36663: Option Strict On does not allow narrowing in implicit type conversions between method 'Public Sub WideningStringSub(ByRef p As String)' and delegate 'Delegate Sub WideningStringDelegate(ByRef p As Char)'.
            Dim d2 as new WideningStringDelegate(AddressOf ci.WideningStringSub)
                                                           ~~~~~~~~~~~~~~~~~~~~
BC36663: Option Strict On does not allow narrowing in implicit type conversions between method 'Public Sub WideningReferenceSub(ByRef p As Base)' and delegate 'Delegate Sub WideningReferenceDelegate(ByRef p As Derived)'.
            Dim d4 as new WideningReferenceDelegate(AddressOf ci.WideningReferenceSub)
                                                              ~~~~~~~~~~~~~~~~~~~~~~~
BC36663: Option Strict On does not allow narrowing in implicit type conversions between method 'Public Sub WideningArraySub(ByRef p As Base())' and delegate 'Delegate Sub WideningArrayDelegate(ByRef p As Derived())'.
            Dim d5 as new WideningArrayDelegate(AddressOf ci.WideningArraySub)
                                                          ~~~~~~~~~~~~~~~~~~~
BC36663: Option Strict On does not allow narrowing in implicit type conversions between method 'Public Sub WideningValueSub(ByRef p As Object)' and delegate 'Delegate Sub WideningValueDelegate(ByRef p As S1)'.
            Dim d6 as new WideningValueDelegate(AddressOf ci.WideningValueSub)
                                                          ~~~~~~~~~~~~~~~~~~~
</expected>)
            Next
        End Sub

        <Fact()>
        Public Sub WideningArgumentsDelegateSubRelaxationByRef_nullable()
            For Each optionValue In {"On", "Off"}
                Dim source =
    <compilation name="WideningArgumentsDelegateSubRelaxationByRef_nullable">
        <file name="a.vb">
    Option strict <%= optionValue %>    
Imports System

    Delegate Sub WideningNullableDelegate(byref p as Byte?)

    Class C
        Public Sub WideningNullableSub(byref p as Integer?)
            console.writeline("Hello from instance WideningNullableDelegate " &amp; p.ToString() )
            p = 42
        End Sub
    End Class

    Module Program
        Sub Main(args As String())
            Dim n? As Byte = 23
            Dim ci As New C()
            Dim d3 as new WideningNullableDelegate(AddressOf ci.WideningNullableSub)
            d3(n)
            console.writeline(n.Value)
        End Sub
    End Module
    </file>
    </compilation>

                Dim c1 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)

                If optionValue = "On" Then

                    CompilationUtils.AssertTheseDiagnostics(c1,
    <expected>
BC36663: Option Strict On does not allow narrowing in implicit type conversions between method 'Public Sub WideningNullableSub(ByRef p As Integer?)' and delegate 'Delegate Sub WideningNullableDelegate(ByRef p As Byte?)'.
            Dim d3 as new WideningNullableDelegate(AddressOf ci.WideningNullableSub)
                                                             ~~~~~~~~~~~~~~~~~~~~~~
</expected>)
                Else
                    CompilationUtils.AssertNoErrors(c1)
                End If
            Next
        End Sub

        <Fact>
        Public Sub NoZeroArgumentRelaxationIfAmbiguousMatchesExist()
            For Each optionValue In {"Off"}
                Dim source =
    <compilation>
        <file name="a.vb">
Option strict <%= optionValue %>    
Imports System

Module M1
    Sub Test2(x As Integer, y As Long)
    End Sub

    Sub Test2(x As Long, y As Integer)
    End Sub

    Sub Test2()
    End Sub

    Public Sub Main()
        Dim z As Action(Of Integer, Integer) = AddressOf Test2
    End Sub
End Module
    </file>
    </compilation>

                Dim c1 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)

                CompilationUtils.AssertTheseDiagnostics(c1,
<expected>
BC30794: No accessible 'Test2' is most specific: 
    Public Sub Test2(x As Integer, y As Long)
    Public Sub Test2(x As Long, y As Integer)
        Dim z As Action(Of Integer, Integer) = AddressOf Test2
                                                         ~~~~~
</expected>)

            Next
        End Sub

        <Fact>
        Public Sub ParamArrayDelegateRelaxation3()
            For Each optionValue In {"Off", "On"}
                Dim source =
    <compilation name="ParamArrayDelegateRelaxation3">
        <file name="a.vb">
    Option strict <%= optionValue %>
    Imports System

    Delegate Sub ParamArrayNarrowingReferenceDelegate(b as integer, p() as Base)

    Class Base
    End Class

    Class Derived
        Inherits Base
    end Class

    Class C
        Public Sub ParamArrayNarrowingReferenceSub(b as byte, paramarray p() as Derived)
            console.writeline("Hello from instance ParamArrayNarrowingReferenceSub.")
            console.writeline(p)
            console.writeline(p(0))
        End Sub
    End Class

    Module Program

        Sub Main(args As String())
            Dim ci as new C()

            Dim arr(1) as Base
            arr(0) = new Derived()
            arr(1) = new Derived()
            Dim d1 as new ParamArrayNarrowingReferenceDelegate(AddressOf ci.ParamArrayNarrowingReferenceSub)
            d1(23, arr)
        End Sub
    End Module
</file>
    </compilation>

                Dim c1 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
                AssertTheseDiagnostics(c1,
<expected>
BC31143: Method 'Public Sub ParamArrayNarrowingReferenceSub(b As Byte, ParamArray p As Derived())' does not have a signature compatible with delegate 'Delegate Sub ParamArrayNarrowingReferenceDelegate(b As Integer, p As Base())'.
            Dim d1 as new ParamArrayNarrowingReferenceDelegate(AddressOf ci.ParamArrayNarrowingReferenceSub)
                                                                         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
            Next
        End Sub

        <Fact>
        Public Sub DelegatesWithParamArraysFromMetadataAreNotOptional()
            For Each optionValue In {"On", "Off"}

                Dim source =
    <compilation name="DelegatesWithParamArraysFromMetadataAreNotOptional">
        <file name="a.vb">
Option strict <%= optionValue %>
Imports System

Module M

  Public Sub Main()
    Dim d1 as DelegateByRefParamArray.DelegateSubWithParamAndParamArrayOfReferenceTypes = AddressOf SubWithNoParams
    d1(23)
  End Sub

  Public Sub SubWithNoParams(foo as integer)
    Console.WriteLine("Called SubWithNoParams.")
  End Sub

End Module
    </file>
    </compilation>

                Dim ref = MetadataReference.CreateFromImage(TestResources.General.DelegateByRefParamArray.AsImmutableOrNull())

                Dim c1 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, {ref}, TestOptions.ReleaseExe)
                AssertTheseDiagnostics(c1,
<expected>
BC31143: Method 'Public Sub SubWithNoParams(foo As Integer)' does not have a signature compatible with delegate 'Delegate Sub DelegateByRefParamArray.DelegateSubWithParamAndParamArrayOfReferenceTypes(A_0 As Integer, ParamArray A_1 As DelegateByRefParamArray_Base())'.
    Dim d1 as DelegateByRefParamArray.DelegateSubWithParamAndParamArrayOfReferenceTypes = AddressOf SubWithNoParams
                                                                                                    ~~~~~~~~~~~~~~~
</expected>)
                ' note the generated parameter names A_0 and A_1! Metadata did not contain these names.
            Next
        End Sub

        <Fact>
        Public Sub NoZeroArgumentRelaxationIfOptionOnAndNarrowingConversion()
            For Each optionValue In {"On"}
                Dim source =
    <compilation name="NoZeroArgumentRelaxationIfOptionOnAndNarrowingConversion">
        <file name="a.vb">
    Option strict <%= optionValue %>
    Imports System

    Delegate function MyDelegateFunction(p as integer) as byte

    Module Program

        function MyFunction1(p as Byte) as Integer
            return 42
        End Function

        function MyFunction1() as Integer
            return 42
        End Function

        function MyFunction2(p as Integer) as Integer
            return 42
        End Function

        function MyFunction2() as byte
            return 42
        End function


        Sub Main(args As String())

            Dim d1 as MyDelegateFunction = addressof MyFunction1
            Dim d2 as MyDelegateFunction = addressof MyFunction2

        End Sub
    End Module
</file>
    </compilation>

                Dim c1 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
                AssertTheseDiagnostics(c1,
<expected>
BC36663: Option Strict On does not allow narrowing in implicit type conversions between method 'Public Function MyFunction1(p As Byte) As Integer' and delegate 'Delegate Function MyDelegateFunction(p As Integer) As Byte'.
            Dim d1 as MyDelegateFunction = addressof MyFunction1
                                                     ~~~~~~~~~~~
BC36663: Option Strict On does not allow narrowing in implicit type conversions between method 'Public Function MyFunction2(p As Integer) As Integer' and delegate 'Delegate Function MyDelegateFunction(p As Integer) As Byte'.
            Dim d2 as MyDelegateFunction = addressof MyFunction2
                                                     ~~~~~~~~~~~
</expected>)
            Next
        End Sub

        <Fact>
        Public Sub NoConversionBecauseOfByRefByValMismatch()
            For Each optionValue In {"On", "Off"}
                Dim source =
    <compilation name="NoZeroArgumentRelaxationIfOptionOnAndNarrowingConversion">
        <file name="a.vb">
    Option strict <%= optionValue %>
    Imports System

    Delegate sub MyDelegate1(byref p as integer)
    Delegate sub MyDelegate2(byval p as integer)

    Module Program

        sub foo1(byval p as integer)
        end sub

        sub foo2(byref p as integer)
        end sub

        Sub Main(args As String())

            ' don't work
            Dim d1 as MyDelegate1 = addressof foo1
            Dim d2 as MyDelegate2 = addressof foo2

            ' work
            'Dim d3 as MyDelegate1 = addressof foo2
            'Dim d4 as MyDelegate2 = addressof foo1

        End Sub
    End Module
</file>
    </compilation>

                Dim c1 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
                AssertTheseDiagnostics(c1,
<expected>
BC31143: Method 'Public Sub foo1(p As Integer)' does not have a signature compatible with delegate 'Delegate Sub MyDelegate1(ByRef p As Integer)'.
            Dim d1 as MyDelegate1 = addressof foo1
                                              ~~~~
BC31143: Method 'Public Sub foo2(ByRef p As Integer)' does not have a signature compatible with delegate 'Delegate Sub MyDelegate2(p As Integer)'.
            Dim d2 as MyDelegate2 = addressof foo2
                                              ~~~~
</expected>)
            Next
        End Sub

        <Fact>
        Public Sub NewDelegateWithLambdaExpressionNoMatches()
            For Each OptionStrict In {"On", "Off"}
                Dim source =
    <compilation name="NewDelegateWithAddressOf">
        <file name="a.vb">
    option strict <%= OptionStrict %>
    IMPORTS SYStEM
    Delegate Sub D1(p as byte)
    Delegate Function D2(p as byte) as String

    Module Program
        Sub Main(args As String())
            Dim x As New D1(Sub(p as byte, b as boolean) Console.WriteLine("Hello from lambda."))
            Dim y As New D2(Function(byref p as byte) "Hello from lambda 2.")
            Dim z as Func(Of byte) = Sub(a as byte) Console.WriteLine("Hello from lambda.")
        End Sub
    End Module
    </file>
    </compilation>

                Dim c1 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)

                CompilationUtils.AssertTheseDiagnostics(c1,
<expected>
BC36670: Nested sub does not have a signature that is compatible with delegate 'Delegate Sub D1(p As Byte)'.
            Dim x As New D1(Sub(p as byte, b as boolean) Console.WriteLine("Hello from lambda."))
                            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36532: Nested function does not have the same signature as delegate 'Delegate Function D2(p As Byte) As String'.
            Dim y As New D2(Function(byref p as byte) "Hello from lambda 2.")
                            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36670: Nested sub does not have a signature that is compatible with delegate 'Func(Of Byte)'.
            Dim z as Func(Of byte) = Sub(a as byte) Console.WriteLine("Hello from lambda.")
                                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
            Next
        End Sub

        <WorkItem(9029, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub DelegateRelaxationConversions_TypeArgumentInferenceAndOverloadResolution()

            Dim source =
<compilation name="NewDelegateWithAddressOf">
    <file name="a.vb">
Option Strict On
Imports System
Imports System.Collections.Generic
Module Test
    Sub foo(Of TT, UU, VV)(x As Func(Of TT, UU, VV), y As Func(Of UU, VV, TT), z As Func(Of VV, TT, UU))
    End Sub
    Sub foo(Of TT, UU, VV)(xx As TT, yy As UU, zz As VV)
    End Sub
    Sub foo(Of TT, UU, VV)(x As Func(Of TT, List(Of TT), UU, Dictionary(Of List(Of TT), UU)), y As Func(Of UU, VV), z As Action(Of VV, List(Of VV), Dictionary(Of List(Of VV), TT)))
    End Sub
    Sub foo(Of TT, UU, VV)(x As Func(Of TT, UU), y As Func(Of TT, VV), z As Func(Of UU, VV), a As Func(Of UU, TT), b As Func(Of VV, TT), c As Func(Of VV, UU))
        Console.WriteLine(GetType(TT))
        Console.WriteLine(GetType(UU))
        Console.WriteLine(GetType(VV))
        Console.WriteLine("foo")
    End Sub
    Sub Main()
        Dim f1 As Func(Of Exception, ArgumentException) = Function(a As Exception) New ArgumentException()
        Dim f2 As Func(Of ArgumentException, Exception) = Function(a As ArgumentException) New ArgumentException()
        foo(f1, f1, f1, f1, f2, f2)
    End Sub
End Module
    </file>
</compilation>

            Dim c = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, options:=TestOptions.ReleaseExe)
            CompilationUtils.AssertNoErrors(c)
            CompileAndVerify(c,
            <![CDATA[
System.Exception
System.Exception
System.ArgumentException
foo
]]>)
        End Sub

        <WorkItem(542068, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542068")>
        <Fact>
        Public Sub DelegateBindingForGenericMethods01b()
            For Each OptionStrict In {"On", "Off"}
                Dim source =
    <compilation name="NewDelegateWithAddressOf">
        <file name="a.vb">
Option Strict <%= OptionStrict %>
Imports System
Imports System.Collections.Generic

Public Class Runner
    Delegate Function Del1(Of TT, UU)(
        x As TT,
        y As List(Of TT),
        z As Dictionary(Of List(Of TT), UU)) As UU

    Delegate Sub Del2(Of TT, UU, VV)(
        x As Func(Of TT, List(Of TT), UU, Dictionary(Of List(Of TT), UU)),
        y As Del1(Of UU, VV),
        z As Action(Of VV, List(Of VV), Dictionary(Of List(Of VV), TT)))

    Sub foo(Of TT, UU, VV)(
        xx As TT,
        yy As UU,
        zz As VV)
        Console.Write("pass")
    End Sub

    Sub foo(Of TT, UU, VV)(
        x As Func(Of TT, List(Of TT), UU, Dictionary(Of List(Of TT), UU)),
        y As Del1(Of UU, VV),
        z As Action(Of VV, List(Of VV), Dictionary(Of List(Of VV), TT)))
        Console.Write("fail")
    End Sub

    Sub foo(Of TT, UU, VV)(
        x As Func(Of TT, UU, VV),
        y As Func(Of UU, VV, TT),
        z As Func(Of VV, TT, UU))
        Console.Write("fail2")
    End Sub

    Public Sub Run(Of AA, BB, CC)()
        Dim d As Del2(Of AA, BB, CC) = AddressOf foo
        Dim d2 As Del2(Of Long, Long, Long) = AddressOf foo
        d(Nothing, Nothing, Nothing)
        d2(Nothing, Nothing, Nothing)
    End Sub
End Class

Module Test
    Sub Main()
        Dim t As New Runner
        t.Run(Of Long, Long, Long)()
    End Sub
End Module
    </file>
    </compilation>

                Dim c = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.ReleaseExe)
                CompilationUtils.AssertTheseDiagnostics(c, <errors></errors>)
                'NOTE: No error in Dev11
            Next
        End Sub

        <WorkItem(543083, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543083")>
        <Fact()>
        Public Sub AddressOfOfCurrentMethod()
            Dim source =
    <compilation name="NewDelegateWithAddressOf">
        <file name="a.vb">
Option Strict On
Imports System

Module Test
    Function Fooo(p As Double) As Integer
        Dim f As Func(Of Double, Integer)        
        f = AddressOf Fooo
        Dim g As new Func(Of Double, Integer)(AddressOf Fooo)
        Return 0    
    End Function    

    Sub Main()
        dim x as Action = addressof Main
        dim y as new Action(addressof Main)
    End Sub
End Module
    </file>
    </compilation>

            CompileAndVerify(source).VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub ZeroArgumentRelaxationVsOtherNarrowing_2()
            Dim source =
    <compilation name="NewDelegateWithAddressOf">
        <file name="a.vb">
Option Strict On
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

            Dim c = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.ReleaseExe)
            CompilationUtils.AssertTheseDiagnostics(c,
<errors>
BC36663: Option Strict On does not allow narrowing in implicit type conversions between method 'Public Sub Test111()' and delegate 'Delegate Sub Action(Of Long)(obj As Long)'.
        ttt1 = AddressOf Test111
                         ~~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub ExpressionTreeConversionErrors()
            For Each OptionStrict In {"On", "Off"}
                Dim source =
    <compilation name="NewDelegateWithLambdaExpressionNoMatchesExpressionTree">
        <file name="a.vb">
    option strict <%= OptionStrict %>
    Imports System
    Imports System.Linq.Expressions

    Delegate Sub D1(p as byte)
    Delegate Function D2(p as byte) as String

    Module Program
        Sub Main(args As String())
            Dim x As Expression(Of D1) = Sub(p as byte, b as boolean) Console.WriteLine("Hello from lambda.")
            Dim y As Expression(Of D2) = Function(byref p as byte) "Hello from lambda 2."
            Dim z as Expression(Of Func(Of byte)) = Sub(a as byte) Console.WriteLine("Hello from lambda.")
            Dim w as Expression(Of Byte) = Sub(a as byte) Console.WriteLine("Hello from lambda.")
        End Sub
    End Module
    </file>
    </compilation>

                Dim c1 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, additionalRefs:={SystemCoreRef})

                CompilationUtils.AssertTheseDiagnostics(c1,
<expected>
BC36670: Nested sub does not have a signature that is compatible with delegate 'Delegate Sub D1(p As Byte)'.
            Dim x As Expression(Of D1) = Sub(p as byte, b as boolean) Console.WriteLine("Hello from lambda.")
                                         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36532: Nested function does not have the same signature as delegate 'Delegate Function D2(p As Byte) As String'.
            Dim y As Expression(Of D2) = Function(byref p as byte) "Hello from lambda 2."
                                         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36538: References to 'ByRef' parameters cannot be converted to an expression tree.
            Dim y As Expression(Of D2) = Function(byref p as byte) "Hello from lambda 2."
                                         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36670: Nested sub does not have a signature that is compatible with delegate 'Func(Of Byte)'.
            Dim z as Expression(Of Func(Of byte)) = Sub(a as byte) Console.WriteLine("Hello from lambda.")
                                                    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36625: Lambda expression cannot be converted to 'Expression(Of Byte)' because 'Expression(Of Byte)' is not a delegate type.
            Dim w as Expression(Of Byte) = Sub(a as byte) Console.WriteLine("Hello from lambda.")
                                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
            Next
        End Sub

        <WorkItem(546014, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546014")>
        <Fact>
        Public Sub Bug14947()
            Dim source =
    <compilation>
        <file name="a.vb">
Option Explicit Off

imports system

Public Module Test
    Friend Delegate Function scenario1(ByVal x() As Object) As Object
    Sub Main()
        i1 = New e1()
        d1 = New scenario1(AddressOf i1.Scenario1)
        d1.Invoke( {nothing,nothing})
    End Sub
End Module

Class e1
    Public Function Scenario1(ByVal x() As Object) As Object
        Console.Writeline("all working here")
        return nothing
    End Function
End Class

    </file>
    </compilation>

            CompileAndVerify(source, "all working here")
        End Sub

        <Fact, WorkItem(17302)>
        Public Sub InvalidDelegateRelaxationForSharednessMismatch()
            Dim compilationDef = <compilation>
                                     <file name="a.vb"><![CDATA[
Option Strict On

Imports System

Module M
    Sub Main()
        Foo(AddressOf Object.Equals)
    End Sub

    Sub Foo(x As Func(Of Object, Boolean))
        Console.WriteLine(1)
    End Sub

    Sub Foo(x As Func(Of Object, Object, Boolean))
        Console.WriteLine(2)
    End Sub
End Module
    ]]></file>
                                 </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication))

            CompileAndVerify(compilation, expectedOutput:="2")
        End Sub

        <Fact, WorkItem(17302)>
        Public Sub InvalidDelegateRelaxationForSharednessMismatch_2()
            Dim compilationDef = <compilation>
                                     <file name="a.vb"><![CDATA[
Option Strict On

Imports System

Module M
    Sub Main()
        Foo(AddressOf C1.Boo)
    End Sub

    Sub Foo(x As Func(Of Object, Boolean))
        Console.WriteLine(1)
    End Sub

    Sub Foo(x As Func(Of Object, Object, Boolean))
        Console.WriteLine(2)
    End Sub

    Class C1
        Public Function Boo(a as object, b as object) as boolean
            return false        
        End Function
    End Class

End Module
    ]]></file>
                                 </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication))

            AssertTheseDiagnostics(compilation, <expected>
BC30518: Overload resolution failed because no accessible 'Foo' can be called with these arguments:
    'Public Sub Foo(x As Func(Of Object, Boolean))': Method 'Public Function Boo(a As Object, b As Object) As Boolean' does not have a signature compatible with delegate 'Delegate Function Func(Of Object, Boolean)(arg As Object) As Boolean'.
    'Public Sub Foo(x As Func(Of Object, Object, Boolean))': Reference to a non-shared member requires an object reference.
        Foo(AddressOf C1.Boo)
        ~~~
                                           </expected>)
        End Sub

        <Fact, WorkItem(17302)>
        Public Sub InvalidDelegateRelaxationForMyClassMismatch()
            Dim compilationDef = <compilation>
                                     <file name="a.vb"><![CDATA[
Option Strict On

Imports System

Module M
    Sub Main()
        Console.WriteLine(New C2().FLD)
    End Sub

    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Public MustInherit Class C1
        Public MustOverride Function F1(o As Derived) As Boolean

        Public Function F1(o As Base, p As Base) As Boolean
            Return False
        End Function

        Function Foo(x As Func(Of Derived, Derived, Boolean)) As Integer
            Return 1
        End Function

        Function Foo(x As Func(Of Derived, Boolean)) As Integer
            Return 2
        End Function

        Public FLD As Integer = Foo(AddressOf MyClass.F1)
    End Class

    Public Class C2
        Inherits C1

        Public Overloads Overrides Function F1(o As Derived) As Boolean
            Return False
        End Function
    End Class
End Module
    ]]></file>
                                 </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication))

            CompileAndVerify(compilation, expectedOutput:="1")
        End Sub

    End Class
End Namespace

