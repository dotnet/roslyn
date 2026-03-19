' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    ' Unit tests for local type inference
    Public Class VariableTypeInference
        Inherits BasicTestBase

#Region "InferenceErrors"
        <Fact>
        Public Sub TestSelfInferenceCycleError()
            Dim options = TestOptions.ReleaseDll.WithRootNamespace("Goo.Bar")

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="inferOn.vb">
            
            Option Infer On
            Imports System

            Module Program
                Sub Main(args As String())
                    dim i = i
                End Sub
            End Module
    </file>
    </compilation>, options)

            Dim expectedErrors =
    <errors>
BC30980: Type of 'i' cannot be inferred from an expression containing 'i'.
                    dim i = i
                            ~
BC42104: Variable 'i' is used before it has been assigned a value. A null reference exception could result at runtime.
                    dim i = i
                            ~        
    </errors>

            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub TestMultiVariableInferenceCycleError()
            Dim options = TestOptions.ReleaseDll.WithRootNamespace("Goo.Bar")

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="inferOn.vb">
            
            Option Infer On
            Imports System

            Module Program
                Sub Main(args As String())
                    dim i = j
                    dim j = i
                End Sub
            End Module
    </file>
    </compilation>, options)

            Dim expectedErrors =
    <errors>
BC32000: Local variable 'j' cannot be referred to before it is declared.
                    dim i = j
                            ~        
    </errors>

            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub TestArrayInferenceRankError()
            Dim options = TestOptions.ReleaseDll.WithRootNamespace("Goo.Bar")

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="inferOn.vb">
            
            Option Infer On
            Imports System

            Module Program
                Sub Main(args As String())
                    dim i(,) = new integer() {}
                End Sub
            End Module
    </file>
    </compilation>, options)

            Dim expectedErrors =
    <errors>
BC36909: Cannot infer a data type for 'i' because the array dimensions do not match.
                    dim i(,) = new integer() {}
                        ~~~~
BC30414: Value of type 'Integer()' cannot be converted to 'Object(*,*)' because the array types have different numbers of dimensions.
                    dim i(,) = new integer() {}
                               ~~~~~~~~~~~~~~~~
    </errors>

            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub TestArrayInferenceNonNullableElementError()
            Dim options = TestOptions.ReleaseDll.WithRootNamespace("Goo.Bar")

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="inferOn.vb">
            
            Option Infer On
            Imports System

            Module Program
                Sub Main(args As String())
                    dim i?() = new integer() {}
                End Sub
            End Module
    </file>
    </compilation>, options)

            Dim expectedErrors =
    <errors>
BC36628: A nullable type cannot be inferred for variable 'i'.
                    dim i?() = new integer() {}
                        ~
BC30333: Value of type 'Integer()' cannot be converted to 'Object()' because 'Integer' is not a reference type.
                    dim i?() = new integer() {}
                               ~~~~~~~~~~~~~~~~
    </errors>

            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub TestNullableIdentifierWithArrayExpression()
            Dim options = TestOptions.ReleaseDll.WithRootNamespace("Goo.Bar")

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="inferOn.vb">
            
            Option Infer On
            Imports System

            Module Program
                Sub Main(args As String())
                     Dim x? = New Integer() {}
                End Sub
            End Module
    </file>
    </compilation>, options)

            Dim expectedErrors =
    <errors>
BC36628: A nullable type cannot be inferred for variable 'x'.
                     Dim x? = New Integer() {}
                         ~        
    </errors>

            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub TestArrayIdentifierWithScalarExpression()
            Dim options = TestOptions.ReleaseDll.WithRootNamespace("Goo.Bar")

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="inferOn.vb">
            
            Option Infer On
            Imports System

            Module Program
                Sub Main(args As String())
                     Dim x() = 1
                End Sub
            End Module
    </file>
    </compilation>, options)

            Dim expectedErrors =
    <errors>
BC36536: Variable cannot be initialized with non-array type 'Integer'.
                     Dim x() = 1
                         ~
BC30311: Value of type 'Integer' cannot be converted to 'Object()'.
                     Dim x() = 1
                               ~
    </errors>

            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub TestNullableIdentifierWithScalarReferenceType()
            Dim options = TestOptions.ReleaseDll.WithRootNamespace("Goo.Bar")

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="inferOn.vb">
            
            Option Infer On
            Imports System

            Module Program
                Sub Main(args As String())
                     Dim x? = "hello"
                End Sub
            End Module
    </file>
    </compilation>, options)

            Dim expectedErrors =
    <errors>
BC36628: A nullable type cannot be inferred for variable 'x'.
                     Dim x? = "hello"
                         ~        
    </errors>

            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors)
        End Sub

#End Region

        <Fact>
        Public Sub TestInferOffPrimitiveTypes()
            Dim options = TestOptions.ReleaseDll.WithRootNamespace("Goo.Bar")

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="inferOff.vb">
        Option Infer Off

        Module m2
            Sub Main()
'Test:a
                dim a = 1
'Test:b
                dim b = "a"
'Test:c
                dim c = 1.0
            End Sub
        End Module
    </file>
    </compilation>, options)

            Dim expectedErrors =
    <errors>
    </errors>

            Dim tree = CompilationUtils.GetTree(compilation, "inferOff.vb")
            Dim model = compilation.GetSemanticModel(tree)

            CheckVariableType(tree, model, "Test:a", "System.Object")
            CheckVariableType(tree, model, "Test:b", "System.Object")
            CheckVariableType(tree, model, "Test:c", "System.Object")

            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub TestInferOnPrimitiveTypes()
            Dim options = TestOptions.ReleaseDll.WithRootNamespace("Goo.Bar")

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="inferOn.vb">
        Option Infer On

        Module m1
            Sub Main()
'Test:a
                dim a = 1
'Test:b
                dim b = "a"
'Test:c
                dim c = 1.0
            End Sub
        End Module
    </file>
    </compilation>, options)

            Dim expectedErrors =
    <errors>
    </errors>

            Dim tree = CompilationUtils.GetTree(compilation, "inferOn.vb")
            Dim model = compilation.GetSemanticModel(tree)
            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors)

            CheckVariableType(tree, model, "Test:a", "System.Int32")
            CheckVariableType(tree, model, "Test:b", "System.String")
            CheckVariableType(tree, model, "Test:c", "System.Double")

        End Sub

        <Fact>
        Public Sub TestDontInferStaticLocal()
            Dim options = TestOptions.ReleaseDll.WithRootNamespace("Goo.Bar")

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="inferOn.vb">
        Option Infer On

        Module m1
            Sub Main()
'Test:a
                static a = 1 ' a is object not integer because static locals do not infer a type.
            End Sub
        End Module
    </file>
    </compilation>, options)

            Dim expectedErrors =
    <errors>
    </errors>

            Dim tree = CompilationUtils.GetTree(compilation, "inferOn.vb")
            Dim model = compilation.GetSemanticModel(tree)
            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors)

            CheckVariableType(tree, model, "Test:a", "System.Object")
        End Sub

        <Fact>
        Public Sub TestInferNullableArrayOfInteger()
            Dim options = TestOptions.ReleaseDll.WithRootNamespace("Goo.Bar")

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="inferOn.vb">
        Option Infer On

        Module m1
            Sub Main()
'Test:t
                Dim t?() = New Integer?() {}
            End Sub
        End Module
    </file>
    </compilation>, options)

            Dim expectedErrors =
    <errors>
    </errors>

            Dim tree = CompilationUtils.GetTree(compilation, "inferOn.vb")
            Dim model = compilation.GetSemanticModel(tree)
            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors)

            CheckVariableType(tree, model, "Test:t", "System.Nullable(Of System.Int32)()")

        End Sub

        <Fact>
        Public Sub TestArrayInference()
            Dim options = TestOptions.ReleaseDll.WithRootNamespace("Goo.Bar")

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="inferOn.vb">
        Option Infer On

        Module m1
            Sub Main()
'Test:t
                Dim t() = New Integer() {}
'Test:u
                Dim u() = new Integer()() {}
'Test:v
                Dim v() = new Integer()()() {}
            End Sub
        End Module
    </file>
    </compilation>, options)

            Dim expectedErrors =
    <errors>
    </errors>

            Dim tree = CompilationUtils.GetTree(compilation, "inferOn.vb")
            Dim model = compilation.GetSemanticModel(tree)
            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors)

            CheckVariableType(tree, model, "Test:t", "System.Int32()")
            CheckVariableType(tree, model, "Test:u", "System.Int32()()")
            CheckVariableType(tree, model, "Test:v", "System.Int32()()()")
        End Sub

        <WorkItem(542371, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542371")>
        <Fact>
        Public Sub TestOptionInferWithOptionStrict()
            Dim options = TestOptions.ReleaseDll.WithRootNamespace("Goo.Bar")

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="inferOn.vb">
        Module m1
            Sub m1()
                Dim t = New Integer()
                Dim u = 1
                Dim x$ = "test"
                Dim v() = new Integer()()() {}
            End Sub
        End Module
    </file>
</compilation>, TestOptions.ReleaseDll.WithOptionInfer(True).WithOptionStrict(OptionStrict.On))
            compilation.VerifyDiagnostics()

            compilation = compilation.WithOptions(TestOptions.ReleaseDll.WithOptionInfer(False).WithOptionStrict(OptionStrict.On))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30209: Option Strict On requires all variable declarations to have an 'As' clause.
                Dim t = New Integer()
                    ~
BC30209: Option Strict On requires all variable declarations to have an 'As' clause.
                Dim u = 1
                    ~
BC30209: Option Strict On requires all variable declarations to have an 'As' clause.
                Dim v() = new Integer()()() {}
                    ~ 
</errors>)

            compilation = compilation.WithOptions(TestOptions.ReleaseDll.WithOptionInfer(False).WithOptionStrict(OptionStrict.Off))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
</errors>)

        End Sub

        <Fact>
        Public Sub TestErrorsForLocalsWithoutAsClause()
            Dim options = TestOptions.ReleaseDll.WithRootNamespace("Goo.Bar")

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="TestInferredTypes.vb">
        Module m1
            Sub m1()
'Test:a
                const a = 1
'Test:b
                dim b = 1
                b = a
            End Sub
        End Module
    </file>
</compilation>, TestOptions.ReleaseDll.WithOptionInfer(True).WithOptionStrict(OptionStrict.On))
            compilation.VerifyDiagnostics()
            Dim tree = CompilationUtils.GetTree(compilation, "TestInferredTypes.vb")
            Dim model = compilation.GetSemanticModel(tree)

            CheckVariableType(tree, model, "Test:a", "System.Int32")
            CheckVariableType(tree, model, "Test:b", "System.Int32")

            compilation = compilation.WithOptions(TestOptions.ReleaseDll.WithOptionInfer(True).WithOptionStrict(OptionStrict.Off))
            compilation.VerifyDiagnostics()
            model = compilation.GetSemanticModel(tree)

            CheckVariableType(tree, model, "Test:a", "System.Int32")
            CheckVariableType(tree, model, "Test:b", "System.Int32")

            compilation = compilation.WithOptions(TestOptions.ReleaseDll.WithOptionInfer(False).WithOptionStrict(OptionStrict.On))
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_StrictDisallowImplicitObject, "a"),
                                          Diagnostic(ERRID.ERR_StrictDisallowImplicitObject, "b"))
            model = compilation.GetSemanticModel(tree)
            CheckVariableType(tree, model, "Test:a", "System.Int32")
            CheckVariableType(tree, model, "Test:b", "System.Object")

            compilation = compilation.WithOptions(TestOptions.ReleaseDll.WithOptionInfer(False).WithOptionStrict(OptionStrict.Off))
            compilation.VerifyDiagnostics()
            model = compilation.GetSemanticModel(tree)
            CheckVariableType(tree, model, "Test:a", "System.Int32")
            CheckVariableType(tree, model, "Test:b", "System.Object")

            compilation = compilation.WithOptions(TestOptions.ReleaseDll.WithOptionInfer(True).WithOptionStrict(OptionStrict.Custom))
            compilation.VerifyDiagnostics()
            model = compilation.GetSemanticModel(tree)

            CheckVariableType(tree, model, "Test:a", "System.Int32")
            CheckVariableType(tree, model, "Test:b", "System.Int32")

            compilation = compilation.WithOptions(TestOptions.ReleaseDll.WithOptionInfer(False).WithOptionStrict(OptionStrict.Custom))
            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.WRN_ObjectAssumedVar1, "a").WithArguments("Variable declaration without an 'As' clause; type of Object assumed."),
                Diagnostic(ERRID.WRN_ObjectAssumedVar1, "b").WithArguments("Variable declaration without an 'As' clause; type of Object assumed."))
            model = compilation.GetSemanticModel(tree)
            CheckVariableType(tree, model, "Test:a", "System.Int32")
            CheckVariableType(tree, model, "Test:b", "System.Object")

        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestErrorsForLocalsWithoutAsClauseStaticLocals()
            'Static Locals do not type infer
            Dim options = TestOptions.ReleaseDll.WithRootNamespace("Goo.Bar")

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="TestInferredTypes.vb">
        Module m1
            Sub m1()
'Test:a
                const a = 1
'Test:b
                Static b = 1
                b = a
            End Sub
        End Module
    </file>
</compilation>, TestOptions.ReleaseDll.WithOptionInfer(True).WithOptionStrict(OptionStrict.On))
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_StrictDisallowImplicitObject, "b"))

            Dim tree = CompilationUtils.GetTree(compilation, "TestInferredTypes.vb")
            Dim model = compilation.GetSemanticModel(tree)

            CheckVariableType(tree, model, "Test:a", "System.Int32")
            CheckVariableType(tree, model, "Test:b", "System.Object")

            compilation = compilation.WithOptions(TestOptions.ReleaseDll.WithOptionInfer(True).WithOptionStrict(OptionStrict.Off))
            compilation.VerifyDiagnostics()
            model = compilation.GetSemanticModel(tree)

            CheckVariableType(tree, model, "Test:a", "System.Int32")
            CheckVariableType(tree, model, "Test:b", "System.Object")

            compilation = compilation.WithOptions(TestOptions.ReleaseDll.WithOptionInfer(False).WithOptionStrict(OptionStrict.On))
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_StrictDisallowImplicitObject, "a"),
                                          Diagnostic(ERRID.ERR_StrictDisallowImplicitObject, "b"))
            model = compilation.GetSemanticModel(tree)
            CheckVariableType(tree, model, "Test:a", "System.Int32")
            CheckVariableType(tree, model, "Test:b", "System.Object")

            compilation = compilation.WithOptions(TestOptions.ReleaseDll.WithOptionInfer(False).WithOptionStrict(OptionStrict.Off))
            compilation.VerifyDiagnostics()
            model = compilation.GetSemanticModel(tree)
            CheckVariableType(tree, model, "Test:a", "System.Int32")
            CheckVariableType(tree, model, "Test:b", "System.Object")

            compilation = compilation.WithOptions(TestOptions.ReleaseDll.WithOptionInfer(True).WithOptionStrict(OptionStrict.Custom))
            compilation.VerifyDiagnostics(Diagnostic(ERRID.WRN_ObjectAssumedVar1, "b").WithArguments("Static variable declared without an 'As' clause; type of Object assumed."))

            model = compilation.GetSemanticModel(tree)

            CheckVariableType(tree, model, "Test:a", "System.Int32")
            CheckVariableType(tree, model, "Test:b", "System.Object")

            compilation = compilation.WithOptions(TestOptions.ReleaseDll.WithOptionInfer(False).WithOptionStrict(OptionStrict.Custom))
            compilation.VerifyDiagnostics(
                   Diagnostic(ERRID.WRN_ObjectAssumedVar1, "a").WithArguments("Variable declaration without an 'As' clause; type of Object assumed."),
                   Diagnostic(ERRID.WRN_ObjectAssumedVar1, "b").WithArguments("Static variable declared without an 'As' clause; type of Object assumed."))
            model = compilation.GetSemanticModel(tree)
            CheckVariableType(tree, model, "Test:a", "System.Int32")
            CheckVariableType(tree, model, "Test:b", "System.Object")

        End Sub

        <WorkItem(542402, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542402")>
        <Fact>
        Public Sub TestCircularDeclarationReference()
            Dim options = TestOptions.ReleaseDll.WithRootNamespace("Goo.Bar")

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="inferOn.vb">
        Option Infer On
        Option Explicit Off
        Option Strict Off
        Class TestClass
            Sub New(ByVal x As TestClass)
            End Sub
            Shared Function GetSomething(ByVal x As TestClass) As TestClass
                Return Nothing
            End Function
        End Class
 
        Friend Module TypeInfCircularTest
            Sub TypeInfCircular()
                Dim x = y.GetSomething(x), y = New TestClass(x)
            End Sub
        End Module
    </file>
</compilation>)
            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_UseOfLocalBeforeDeclaration1, "y").WithArguments("y"),
                Diagnostic(ERRID.ERR_CircularInference1, "x").WithArguments("x"),
                Diagnostic(ERRID.WRN_DefAsgUseNullRef, "x").WithArguments("x"))

        End Sub

        <WorkItem(545427, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545427")>
        <Fact()>
        Public Sub TestNothingConversionLocalConst1()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="TestSByteLocalConst">
    <file name="a.vb">
Class C
End Class
Module Module1
    Sub Main()
        Const bar1 = DirectCast(Nothing, Integer())
        Const bar2 = TryCast(Nothing, String())
        Const bar3 = CType(Nothing, C())
    End Sub
End Module


    </file>
</compilation>)
            compilation.VerifyDiagnostics(
                 Diagnostic(ERRID.WRN_UnusedLocalConst, "bar1").WithArguments("bar1"),
                 Diagnostic(ERRID.WRN_UnusedLocalConst, "bar2").WithArguments("bar2"),
                 Diagnostic(ERRID.WRN_UnusedLocalConst, "bar3").WithArguments("bar3")
                )
        End Sub

        <WorkItem(545427, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545427")>
        <Fact()>
        Public Sub TestNothingConversionLocalConst2()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="TestSByteLocalConst">
    <file name="a.vb">
Class C
End Class
Module Module1
    Sub Main()
        Const bar1 = DirectCast(Nothing, Integer)
        Const bar2 = DirectCast(Nothing, Object)
        Const bar3 = DirectCast(Nothing, String)
        Const bar4 = DirectCast(Nothing, Decimal)
        Const bar5 = DirectCast(Nothing, Date)
        const bar6 as string
    End Sub
End Module


    </file>
</compilation>)
            compilation.VerifyDiagnostics(
                    Diagnostic(ERRID.ERR_ConstantWithNoValue, "bar6"),
                    Diagnostic(ERRID.WRN_UnusedLocalConst, "bar1").WithArguments("bar1"),
                    Diagnostic(ERRID.WRN_UnusedLocalConst, "bar2").WithArguments("bar2"),
                    Diagnostic(ERRID.WRN_UnusedLocalConst, "bar3").WithArguments("bar3"),
                    Diagnostic(ERRID.WRN_UnusedLocalConst, "bar4").WithArguments("bar4"),
                    Diagnostic(ERRID.WRN_UnusedLocalConst, "bar5").WithArguments("bar5"))
        End Sub

        <WorkItem(545763, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545763")>
        <Fact()>
        Public Sub TestInferNullableType()

            Dim source =
<compilation name="TestSByteLocalConst">
    <file name="a.vb">
Imports System

Structure S1
    Dim x As Integer
End Structure

Module Module1

    Public Sub Main()
        Dim y? = New S1?(New S1)
        Console.WriteLine(y.GetType())
    End Sub

End Module
    </file>
</compilation>

            CompileAndVerify(source, options:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication), expectedOutput:=<![CDATA[
S1
            ]]>)

        End Sub

        Private Sub CheckVariableType(tree As SyntaxTree, model As SemanticModel, textToFind As String, typeName As String)
            Dim node = CompilationUtils.FindTokenFromText(tree, textToFind).Parent
            Dim varName = textToFind.Substring(textToFind.IndexOf(":"c) + 1)
            Dim vardecl = node.DescendantNodes().OfType(Of ModifiedIdentifierSyntax)().First()
            Dim varSymbol = model.GetDeclaredSymbol(vardecl)
            Assert.NotNull(varSymbol)
            Assert.Equal(varName, varSymbol.Name)
            Assert.Equal(SymbolKind.Local, varSymbol.Kind)
            Assert.Equal(typeName, DirectCast(varSymbol, LocalSymbol).Type.ToTestDisplayString())
        End Sub

    End Class

End Namespace
