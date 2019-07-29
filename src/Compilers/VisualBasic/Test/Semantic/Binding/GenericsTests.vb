' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class GenericsTests
        Inherits BasicTestBase

        <WorkItem(543690, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543690")>
        <WorkItem(543690, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543690")>
        <Fact()>
        Public Sub WrongNumberOfGenericArgumentsTest()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="WrongNumberOfGenericArguments">
        <file name="a.vb">
Namespace GenArity200
    Public Class vbCls5 (Of T)

	    Structure vbStrA (Of X, Y)
	       Dim i As Integer
	       Public Readonly Property rp () As String
	         Get
	           Return "vbCls5 (Of T) vbStrA (Of X, Y)"
	         End Get
	       End Property
	    End Structure

    End Class

    Class Problem
       Sub GenArity200()
	        Dim Str14 As New vbCls5 (Of UInteger ()()).vbStrA (Of Integer)
       End Sub
    End Class
End Namespace
    </file>
    </compilation>)

            AssertTheseDiagnostics(compilation,
    <expected>
BC32042: Too few type arguments to 'vbCls5(Of UInteger()()).vbStrA(Of X, Y)'.
	        Dim Str14 As New vbCls5 (Of UInteger ()()).vbStrA (Of Integer)
                          ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

        End Sub

        <WorkItem(543706, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543706")>
        <Fact()>
        Public Sub TestNestedGenericTypeInference()
            Dim vbCompilation = CreateVisualBasicCompilation("TestNestedGenericTypeInference",
            <![CDATA[Imports System
        Public Module Program
            Sub goo(Of U, T)(ByVal x As cls1(Of U).cls2(Of T))
                Console.WriteLine(GetType(U).ToString())
                Console.WriteLine(GetType(T).ToString())
            End Sub
            Class cls1(Of X)
                Class cls2(Of Y)
                End Class
            End Class

            Sub Main()
                Dim x = New cls1(Of Integer).cls2(Of Long)
                goo(x)
            End Sub
        End Module]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication))

            CompileAndVerify(vbCompilation, expectedOutput:=<![CDATA[System.Int32
System.Int64]]>).VerifyDiagnostics()
        End Sub

        <WorkItem(543783, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543783")>
        <Fact()>
        Public Sub ImportNestedGenericTypeWithErrors()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
Imports GenImpClassOpenErrors.GenClassA(Of String).GenClassB.GenClassC(Of String)

Namespace GenImpClassOpenErrors
    Module Module1
        Public Class GenClassA(Of T)
            Public Class GenClassB(Of U)
                Public Class GenClassC(Of V)
                End Class
            End Class
        End Class
    End Module
End Namespace
    ]]></file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC32042: Too few type arguments to 'Module1.GenClassA(Of String).GenClassB(Of U)'.
Imports GenImpClassOpenErrors.GenClassA(Of String).GenClassB.GenClassC(Of String)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <WorkItem(543850, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543850")>
        <Fact()>
        Public Sub ConflictingNakedConstraint()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Module Program
    Class c2
    End Class
    Class c3
    End Class
    Class C6(Of T As {c2, c3})
        'COMPILEERROR: BC32110, "c3", BC32110, "c3"
        Interface I1(Of S As {U, c3}, U As {c3, T})
        End Interface
    End Class
End Module
    </file>
    </compilation>)
            compilation.AssertTheseDiagnostics(
    <expected>
BC32047: Type parameter 'T' can only have one constraint that is a class.
    Class C6(Of T As {c2, c3})
                          ~~
BC32111: Indirect constraint 'Class c2' obtained from the type parameter constraint 'U' conflicts with the constraint 'Class c3'.
        Interface I1(Of S As {U, c3}, U As {c3, T})
                              ~
BC32110: Constraint 'Class c3' conflicts with the indirect constraint 'Class c2' obtained from the type parameter constraint 'T'.
        Interface I1(Of S As {U, c3}, U As {c3, T})
                                            ~~
</expected>)
        End Sub

        <WorkItem(11887, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub WideningNullableConversion()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Imports System

Module Program
    Sub Gen1E(Of T, V As Structure)(ByVal c As Func(Of V?, T))
        Dim k = New V
        c(k)
    End Sub
End Module
    </file>
    </compilation>
            )

            compilation.VerifyDiagnostics()
        End Sub

        <WorkItem(543900, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543900")>
        <Fact()>
        Public Sub NarrowingConversionNoReturn()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Imports System

Namespace Program
    Class C6
        Public Shared Narrowing Operator CType(ByVal arg As C6) As Exception
            Return Nothing
        End Operator
    End Class
End Namespace
    </file>
    </compilation>
            )

            compilation.VerifyDiagnostics()
        End Sub

        <WorkItem(543900, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543900")>
        <Fact()>
        Public Sub NarrowingConversionNoReturn2()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Imports System

Namespace Program
    Class c2
        Public Shared Narrowing Operator CType(ByVal arg As c2) As Integer
            Return Nothing
        End Operator
    End Class
End Namespace
    </file>
    </compilation>
            )

            compilation.AssertNoDiagnostics()
        End Sub

        <WorkItem(543902, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543902")>
        <Fact()>
        Public Sub ConversionOperatorShouldBePublic()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Namespace Program
    Class CScen5
        Shared Narrowing Operator CType(ByVal src As CScen5b) As CScen5
            str = "scen5"
            Return New CScen5
        End Operator
        Public Shared str = ""
    End Class
End Namespace
    </file>
    </compilation>
            )

            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_UndefinedType1, "CScen5b").WithArguments("CScen5b"))
        End Sub

        <Fact(), WorkItem(529249, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529249")>
        Public Sub ArrayOfRuntimeArgumentHandle()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Imports System

Module Program
    Sub goo(ByRef x As RuntimeArgumentHandle())
        ReDim x(100)
    End Sub
End Module
    </file>
    </compilation>
            )

            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_RestrictedType1, "RuntimeArgumentHandle()").WithArguments("System.RuntimeArgumentHandle"))
        End Sub

        <WorkItem(543909, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543909")>
        <Fact()>
        Public Sub StructureContainsItself()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Imports System

Namespace Program
    Structure s2
        Dim list As Collections.Generic.List(Of s2)
        'COMPILEERROR: BC30294, "Collections.Generic.List(Of s2).Enumerator"
        Dim enumerator As Collections.Generic.List(Of s2).Enumerator
    End Structure
End Namespace        
    </file>
    </compilation>
            )

            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_RecordCycle2, "enumerator").WithArguments("s2",
                                                                                                          Environment.NewLine &
            "    's2' contains 'List(Of s2).Enumerator' (variable 'enumerator')." & Environment.NewLine &
            "    'List(Of s2).Enumerator' contains 's2' (variable 'current')."))
        End Sub

        <WorkItem(543921, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543921")>
        <Fact()>
        Public Sub GenericConstraintInheritanceWithEvent()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Namespace GenClass7105

    Interface I1
        Event e()
        Property p1() As String
        Sub rEvent()
    End Interface

    Class CI1
        Implements I1
        Private s1 As String

        Public Event e() Implements I1.e
        Public Sub rEvent() Implements I1.rEvent
            RaiseEvent e()
        End Sub

        Public Property p1() As String Implements I1.p1
            Get
                Return s1
            End Get
            Set(ByVal value As String)
                s1 = value
            End Set
        End Property

        Sub eHandler() Handles Me.e
            Me.s1 = Me.s1 &amp; "eHandler"
        End Sub
    End Class

    Class CI2
        Inherits CI1

    End Class

    Class Cls1a(Of T1 As {I1, Class}, T2 As T1)
        Public WithEvents x1 As T1
        'This error is actually correct now as the Class Constraint Change which was made. Class can be a Class (OR INTERFACE) on a structure.
        'This testcase has changed to reflect the new behavior and this constraint change will also be caught.

        'COMPILEERROR: BC30413, "T2"
        Public WithEvents x2 As T2

        Public Function Test(ByVal i As Integer) As String
            x2.p1 = "x2_"
            Call x2.rEvent()
            Return x2.p1
        End Function
    End Class

End Namespace
    </file>
    </compilation>
            )

            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_WithEventsAsStruct, "x2"))
        End Sub

        <WorkItem(529287, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529287")>
        <Fact()>
        Public Sub ProtectedMemberGenericClass()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Public Class c1(Of T)
    'COMPILEERROR: BC30508, "c1(Of Integer).c2"
    Protected x As c1(Of Integer).c2
    Protected Class c2
    End Class
End Class
    </file>
    </compilation>
            )

            compilation.VerifyDiagnostics()
        End Sub

        <WorkItem(544122, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544122")>
        <Fact()>
        Public Sub BoxAlreadyBoxed()
            Dim compilation = CompileAndVerify(
    <compilation>
        <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Scen4(Of T As U, U As Class)(ByVal p As T)
        Dim x As T
        x = TryCast(p, U)
        If x Is Nothing Then
            Console.WriteLine("fail")
        End If
    End Sub
    Sub Main(args As String())
        
    End Sub
End Module
    </file>
    </compilation>
            )
        End Sub

        <WorkItem(531075, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531075")>
        <Fact()>
        Public Sub Bug17530()
            Dim vbCompilation = CreateVisualBasicCompilation("Bug17530",
            <![CDATA[
        Imports Tuple = System.Tuple
        Public Module Program
            Sub Main()
                Dim x As Object = Nothing
                Dim y = TryCast(x, Tuple(Of Integer, Integer, String))
            End Sub
        End Module
        ]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, globalImports:=GlobalImport.Parse({"System"})))

            CompileAndVerify(vbCompilation).VerifyDiagnostics(
                Diagnostic(ERRID.HDN_UnusedImportStatement, "Imports Tuple = System.Tuple"))
        End Sub

    End Class

End Namespace
