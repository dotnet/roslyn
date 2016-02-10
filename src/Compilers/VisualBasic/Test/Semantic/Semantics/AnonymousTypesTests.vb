' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.ExtensionMethods

    Public Class AnonymousTypesTests
        Inherits BasicTestBase

        <Fact>
        Public Sub AnonymousTypeFieldsReferences()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="AnonymousTypeFieldsReferences">
    <file name="a.vb">
Module ModuleA
    Sub Test1()
        Dim v1 As Object = New With {.a=1, .b=.a, .c=.b+.a}
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact>
        Public Sub AnonymousTypeErrorInFieldReference()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="AnonymousTypeErrorInFieldReference">
    <file name="a.vb">
Module ModuleA
    Sub Test1()
        Dim v1 As Object = New With {.a=sss, .b=.a}
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30451: 'sss' is not declared. It may be inaccessible due to its protection level.
        Dim v1 As Object = New With {.a=sss, .b=.a}
                                        ~~~
</errors>)
        End Sub

        <Fact>
        Public Sub AnonymousTypeFieldOfRestrictedType()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="AnonymousTypeFieldOfRestrictedType">
    <file name="a.vb">
Module ModuleA
    Sub Test1(tr As System.TypedReference)
        Dim v1 As Object = New With {.a=tr}
        Dim v2 As Object = New With {.a={{tr}}}
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC31396: 'TypedReference' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim v1 As Object = New With {.a=tr}
                                        ~~
BC31396: 'TypedReference' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim v2 As Object = New With {.a={{tr}}}
                                        ~~~~~~
BC31396: 'TypedReference' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim v2 As Object = New With {.a={{tr}}}
                                        ~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub AnonymousTypeReferenceToOuterTypeField()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="AnonymousTypeReferenceToOuterTypeField">
    <file name="a.vb">
Module ModuleA
    Sub Test1()
        Dim c = New With {.a = 1, .b = New With {.c = .a}}
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC36557: 'a' is not a member of '&lt;anonymous type&gt;'; it does not exist in the current context.
        Dim c = New With {.a = 1, .b = New With {.c = .a}}
                                                      ~~
</errors>)
        End Sub

        <Fact>
        Public Sub AnonymousTypeFieldReferenceOutOfOrder01()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="AnonymousTypeFieldReferenceOutOfOrder01">
    <file name="a.vb">
Module ModuleA
    Sub Test1(x As Integer)
        Dim v1 As Object = New With {.b = .c, .c = .b}
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC36559: Anonymous type member property 'c' cannot be used to infer the type of another member property because the type of 'c' is not yet established.
        Dim v1 As Object = New With {.b = .c, .c = .b}
                                          ~~
</errors>)
        End Sub

        <Fact>
        Public Sub AnonymousTypeFieldReferenceOutOfOrder02()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="AnonymousTypeFieldReferenceOutOfOrder02">
    <file name="a.vb">
Module ModuleA
    Sub Test1(x As Integer)
        Dim v1 As Object = New With {.b = .c, .c = 1}
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC36559: Anonymous type member property 'c' cannot be used to infer the type of another member property because the type of 'c' is not yet established.
        Dim v1 As Object = New With {.b = .c, .c = 1}
                                          ~~
</errors>)
        End Sub

        <Fact>
        Public Sub AnonymousTypeFieldInitializedWithInstanceMethod()
            ' WARNING: NO ERROR IN DEV10
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="AnonymousTypeFieldInitializedWithInstanceMethod">
    <file name="a.vb">
Module ModuleA
    Sub Test1(x As Integer)
        Dim b = New With {.a = .ToString()}
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC36557: 'ToString' is not a member of '&lt;anonymous type&gt;'; it does not exist in the current context.
        Dim b = New With {.a = .ToString()}
                               ~~~~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub AnonymousTypeFieldInitializedWithSharedMethod()
            ' WARNING: NO ERROR IN DEV10
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="AnonymousTypeFieldInitializedWithSharedMethod">
    <file name="a.vb">
Module ModuleA
    Sub Test1(x As Integer)
        Dim b = New With {.a = .ReferenceEquals(Nothing, Nothing)}
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC36557: 'ReferenceEquals' is not a member of '&lt;anonymous type&gt;'; it does not exist in the current context.
        Dim b = New With {.a = .ReferenceEquals(Nothing, Nothing)}
                               ~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub AnonymousTypeFieldInitializedWithExtensionMethod()
            ' WARNING: NO ERROR IN DEV10
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation name="AnonymousTypeFieldInitializedWithExtensionMethod">
    <file name="a.vb">
Imports System.Runtime.CompilerServices
Module ModuleA
    Sub Main()
        Dim a = New With {.a = .EM()}
    End Sub
    &lt;Extension()&gt;
    Public Function EM(o As Object) As String
        Return "!"
    End Function
End Module
    </file>
</compilation>, {SystemCoreRef})

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC36557: 'EM' is not a member of '&lt;anonymous type&gt;'; it does not exist in the current context.
        Dim a = New With {.a = .EM()}
                               ~~~
</errors>)
        End Sub

        <Fact>
        Public Sub AnonymousTypeFieldInitializedWithConstructorCall()
            ' WARNING: Dev10 reports BC30282
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation name="AnonymousTypeFieldInitializedWithConstructorCall">
    <file name="a.vb">
Module ModuleA
    Sub Main()
        Dim a = New With {.a = .New()}
    End Sub
End Module
    </file>
</compilation>, {SystemCoreRef})

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC36557: 'New' is not a member of '&lt;anonymous type&gt;'; it does not exist in the current context.
        Dim a = New With {.a = .New()}
                               ~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub AnonymousTypeFieldOfVoidType()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="AnonymousTypeFieldOfVoidType">
    <file name="a.vb">
Module ModuleA
    Sub Main()
        Dim a = New With {.a = SubName()}
    End Sub
    Public Sub SubName()
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30491: Expression does not produce a value.
        Dim a = New With {.a = SubName()}
                               ~~~~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub AnonymousTypeFieldNameWithGeneric()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="AnonymousTypeFieldNameWithGeneric">
    <file name="a.vb">
Module ModuleA
    Sub Main()
        Dim a = New With {.a = 1, .b = .a(Of Integer)}
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC32045: 'Public Property a As T0' has no type parameters and so cannot have type arguments.
        Dim a = New With {.a = 1, .b = .a(Of Integer)}
                                         ~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub AnonymousTypeFieldWithSyntaxError()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="AnonymousTypeFieldWithSyntaxError">
    <file name="a.vb">
Module ModuleA
    Sub Main()
        Dim b = New With {.a = .}
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30203: Identifier expected.
        Dim b = New With {.a = .}
                                ~
</errors>)
        End Sub

        <Fact>
        Public Sub AnonymousTypeFieldWithNothingLiteral()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="AnonymousTypeFieldWithNothingLiteral">
    <file name="a.vb">
Module ModuleA
    Sub Main()
        Dim b = New With {.a = Nothing}
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <WorkItem(542246, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542246")>
        <Fact>
        Public Sub AnonymousTypeFieldNameInferenceFromGeneric01()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="AnonymousTypeFieldNameInferenceFromGeneric01">
    <file name="a.vb">
Friend Module AM
    Sub Main()
        Dim at = New With {New A().F(Of Integer)}
    End Sub

    Class A
        Public Function F(Of T)() As T
            Return Nothing
        End Function
    End Class
End Module
    </file>
</compilation>)

            'BC36556: Anonymous type member name can be inferred only from a simple or qualified name with no arguments.
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_AnonymousTypeFieldNameInference, "New A().F(Of Integer)"))
        End Sub

        <Fact>
        Public Sub AnonymousTypeFieldNameInferenceFromXml01()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation name="AnonymousTypeFieldNameInferenceFromXml01">
    <file name="a.vb"><![CDATA[
Module ModuleA
    Sub Main()
        Dim b = New With {<some-name></some-name>}
    End Sub
End Module
    ]]></file>
</compilation>, additionalRefs:=XmlReferences)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors><![CDATA[
BC36556: Anonymous type member name can be inferred only from a simple or qualified name with no arguments.
        Dim b = New With {<some-name></some-name>}
                          ~~~~~~~~~~~~~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact>
        Public Sub AnonymousTypeFieldNameInferenceFromXml02()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation name="AnonymousTypeFieldNameInferenceFromXml02">
    <file name="a.vb"><![CDATA[
Module ModuleA
    Sub Main()
        Dim b = New With {<some-name></some-name>.@aa}
    End Sub
End Module
    ]]></file>
</compilation>, additionalRefs:=XmlReferences)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact>
        Public Sub AnonymousTypeFieldNameInferenceFromXml03()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation name="AnonymousTypeFieldNameInferenceFromXml03">
    <file name="a.vb"><![CDATA[
Module ModuleA
    Sub Main()
        Dim b = New With {<some-name name="a"></some-name>.@<a-a>}
    End Sub
End Module
    ]]></file>
</compilation>, additionalRefs:=XmlReferences)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors><![CDATA[
BC36613: Anonymous type member name cannot be inferred from an XML identifier that is not a valid Visual Basic identifier.
        Dim b = New With {<some-name name="a"></some-name>.@<a-a>}
                          ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></errors>)
        End Sub

        <WorkItem(544370, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544370")>
        <Fact>
        Public Sub AnonymousTypeFieldNameInferenceFromXml04()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation name="AnonymousTypeFieldNameInferenceFromXml04">
    <file name="a.vb"><![CDATA[
Module ModuleA
    Sub Main()
        Dim err = New With {<a/>.<_>}
        Dim ok = New With {<a/>.<__>}
    End Sub
End Module
    ]]></file>
</compilation>, additionalRefs:=XmlReferences)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors><![CDATA[
BC36613: Anonymous type member name cannot be inferred from an XML identifier that is not a valid Visual Basic identifier.
        Dim err = New With {<a/>.<_>}
                            ~~~~~~~~
]]></errors>)
        End Sub

        <Fact>
        Public Sub AnonymousTypeFieldNameInferenceFromExpression01()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="AnonymousTypeFieldNameInferenceFromExpression01">
    <file name="a.vb">
Module ModuleA
    Sub Main()
        Dim a As Integer = 0
        Dim b = New With { a*2 }
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC36556: Anonymous type member name can be inferred only from a simple or qualified name with no arguments.
        Dim b = New With { a*2 }
                           ~~~
</errors>)
        End Sub

        <Fact>
        Public Sub AnonymousTypeFieldNameInferenceFromExpression02()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="AnonymousTypeFieldNameInferenceFromExpression02">
    <file name="a.vb">
Module ModuleA
    Sub Main()
        Dim a As Integer = 0
        Dim b = New With { .a = 1, a }
    End Sub
End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC36547: Anonymous type member or property 'a' is already declared.
        Dim b = New With { .a = 1, a }
                                   ~
</errors>)
        End Sub

        <Fact>
        Public Sub AnonymousTypeFieldNameInferenceFromExpression03()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="AnonymousTypeFieldNameInferenceFromExpression03">
    <file name="a.vb">
Module ModuleA
    Structure S
        Public Property FLD As Integer
    End Structure
    Sub Main()
        Dim a As S = new S()
        Dim b = New With { a.FLD, a.FLD() }
    End Sub
End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC36547: Anonymous type member or property 'FLD' is already declared.
        Dim b = New With { a.FLD, a.FLD() }
                                  ~~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub AnonymousTypeFieldNameInferenceFromExpression04()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="AnonymousTypeFieldNameInferenceFromExpression04">
    <file name="a.vb">
Imports System.Collections.Generic
Module ModuleA
    Sub Main()
        Dim a As New Dictionary(Of String, Integer)
        Dim b = New With {.x = 1, a!x}
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC36547: Anonymous type member or property 'x' is already declared.
        Dim b = New With {.x = 1, a!x}
                                  ~~~
</errors>)
        End Sub

        <Fact>
        Public Sub AnonymousTypeFieldInitializedWithAddressOf()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="AnonymousTypeFieldInitializedWithAddressOf">
    <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        Console.WriteLine(New With {Key .a = AddressOf S})
    End Sub
    Sub S()
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30491: Expression does not produce a value.
        Console.WriteLine(New With {Key .a = AddressOf S})
                                             ~~~~~~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub AnonymousTypeFieldInitializedWithDelegate01()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="AnonymousTypeFieldInitializedWithDelegate01">
    <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        Console.WriteLine(New With {
                          Key .x = "--value--",
                          Key .a = DirectCast(Function() As String
                                                  Return .x.ToString()
                                              End Function, Func(Of String)).Invoke()})
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC36549: Anonymous type property 'x' cannot be used in the definition of a lambda expression within the same initialization list.
                                                  Return .x.ToString()
                                                         ~~
</errors>)
        End Sub

        <Fact>
        Public Sub AnonymousTypeFieldInitializedWithDelegate02()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="AnonymousTypeFieldInitializedWithDelegate02">
    <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        Console.WriteLine(New With {
                          Key .a = DirectCast(Function() As String
                                                  Return .a.ToString()
                                              End Function, Func(Of String)).Invoke()})
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC36559: Anonymous type member property 'a' cannot be used to infer the type of another member property because the type of 'a' is not yet established.
                                                  Return .a.ToString()
                                                         ~~
</errors>)
        End Sub

        <Fact>
        Public Sub AnonymousTypeFieldInitializedWithDelegate03()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="AnonymousTypeFieldInitializedWithDelegate03">
    <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        Console.WriteLine(New With {
                          Key .a = DirectCast(Function() As String
                                                  Return .x.ToString()
                                              End Function, Func(Of String)).Invoke()})
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC36557: 'x' is not a member of '&lt;anonymous type&gt;'; it does not exist in the current context.
                                                  Return .x.ToString()
                                                         ~~
</errors>)
        End Sub

        <Fact>
        Public Sub AnonymousTypeFieldInitializedWithDelegate04()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="AnonymousTypeFieldInitializedWithDelegate04">
    <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        Console.WriteLine(New With {
                          Key .a = DirectCast(Function() As String
                                                  Return DirectCast(Function() As String
                                                                        Return .x.ToString()
                                                                    End Function, Func(Of String)).Invoke()
                                              End Function, Func(Of String)).Invoke()})
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC36557: 'x' is not a member of '&lt;anonymous type&gt;'; it does not exist in the current context.
                                                                        Return .x.ToString()
                                                                               ~~
</errors>)
        End Sub

        <WorkItem(542940, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542940")>
        <Fact>
        Public Sub LambdaReturningAnonymousType()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="AnonymousTypeFieldsReferences">
    <file name="a.vb">
Module Program
    Sub Main(args As String())
        Dim x1 As Object = Function() New With {.Default = "Test"}
        System.Console.WriteLine(x1)
    End Sub
End Module
    </file>
</compilation>, TestOptions.ReleaseExe)

            CompilationUtils.AssertNoErrors(compilation)

            CompileAndVerify(compilation, expectedOutput:=<![CDATA[
VB$AnonymousDelegate_0`1[VB$AnonymousType_0`1[System.String]]
]]>)
        End Sub

        <WorkItem(543286, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543286")>
        <Fact>
        Public Sub AnonymousTypeInALambdaInGenericMethod1()
            Dim compilationDef =
<compilation name="AnonymousTypeInALambdaInGenericMethod1">
    <file name="a.vb">
Imports System

Module S1
    Public Function Foo(Of T)() As System.Func(Of Object)
        Dim x2 As T = Nothing
        return Function()
                     Return new With {x2}
               End Function
    End Function

    Sub Main()
        Console.WriteLine(Foo(Of Integer)()())
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)
            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)
            Dim verifier = CompileAndVerify(compilation, expectedOutput:="{ x2 = 0 }")
        End Sub

        <WorkItem(543286, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543286")>
        <Fact>
        Public Sub AnonymousTypeInALambdaInGenericMethod2()
            Dim compilationDef =
<compilation name="AnonymousTypeInALambdaInGenericMethod2">
    <file name="a.vb">
Imports System

Module S1
    Public Function Foo(Of T)() As System.Func(Of Object)
        Dim x2 As T = Nothing
        Dim x3 = Function()
                     Dim result = new With {x2}
                     Return result
               End Function

        return x3
    End Function

    Sub Main()
        Console.WriteLine(Foo(Of Integer)()())
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)
            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)
            Dim verifier = CompileAndVerify(compilation, expectedOutput:="{ x2 = 0 }")
        End Sub

        <Fact()>
        Public Sub AnonymousTypeInALambdaInGenericMethod3()
            Dim compilationDef =
<compilation name="AnonymousTypeInALambdaInGenericMethod3">
    <file name="a.vb">
Imports System

Module S1
    Public Function Foo(Of T)() As System.Func(Of Object)
        Dim x2 As T = Nothing
        Dim x3 = Function()
                     Dim result = new With {x2}
                     Dim tmp = result.x2 ' Property getter should be also rewritten
                     Return result
               End Function

        return x3
    End Function

    Sub Main()
        Console.WriteLine(Foo(Of Integer)()())
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)
            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)
            Dim verifier = CompileAndVerify(compilation, expectedOutput:="{ x2 = 0 }")
        End Sub

        <WorkItem(529688, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529688")>
        <Fact()>
        Public Sub AssociatedAnonymousDelegate_Valid()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System

Module S1
    Public Sub Foo()
        Dim sss = Sub(x) Console.WriteLine() 'BIND2:"x" 
        sss(x:=1)'BIND1:"sss(x:=1)" 
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim node2 As ParameterSyntax = CompilationUtils.FindBindingText(Of ParameterSyntax)(compilation, "a.vb", 2)
            Dim symbol2 = model.GetDeclaredSymbol(node2)
            Assert.Equal(SymbolKind.Parameter, symbol2.Kind)
            Assert.Equal("x As Object", symbol2.ToDisplayString())

            Dim lambda2 = DirectCast(symbol2.ContainingSymbol, MethodSymbol)
            Assert.Equal(MethodKind.LambdaMethod, lambda2.MethodKind)
            Assert.Equal("Private Shared Sub (x As Object)", symbol2.ContainingSymbol.ToDisplayString())

            Dim associatedDelegate = lambda2.AssociatedAnonymousDelegate
            Assert.NotNull(associatedDelegate)
            Assert.True(associatedDelegate.IsDelegateType)
            Assert.True(associatedDelegate.IsAnonymousType)
            Assert.Equal("Sub <generated method>(x As Object)", associatedDelegate.ToDisplayString)
            Assert.Same(associatedDelegate, DirectCast(lambda2, IMethodSymbol).AssociatedAnonymousDelegate)

            Dim node As InvocationExpressionSyntax = CompilationUtils.FindBindingText(Of InvocationExpressionSyntax)(compilation, "a.vb", 1)
            Dim info = model.GetSymbolInfo(node)
            Assert.Equal("Public Overridable Sub Invoke(x As Object)", info.Symbol.ToDisplayString())
            Assert.Equal("Sub <generated method>(x As Object)", info.Symbol.ContainingSymbol.ToDisplayString())

            Assert.Same(associatedDelegate, info.Symbol.ContainingSymbol)
        End Sub

        <WorkItem(529688, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529688")>
        <Fact()>
        Public Sub AssociatedAnonymousDelegate_Action_SameSignature()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System

Module S1
    Public Sub Foo()
        Dim sss As Action(Of Object) = Sub(x) Console.WriteLine() 'BIND2:"x" 
        sss(obj:=1)'BIND1:"sss(obj:=1)" 
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim node2 As ParameterSyntax = CompilationUtils.FindBindingText(Of ParameterSyntax)(compilation, "a.vb", 2)
            Dim symbol2 = model.GetDeclaredSymbol(node2)
            Assert.Equal(SymbolKind.Parameter, symbol2.Kind)
            Assert.Equal("x As Object", symbol2.ToDisplayString())

            Dim lambda2 = DirectCast(symbol2.ContainingSymbol, MethodSymbol)
            Assert.Equal(MethodKind.LambdaMethod, lambda2.MethodKind)
            Assert.Equal("Private Shared Sub (x As Object)", symbol2.ContainingSymbol.ToDisplayString())

            Dim associatedDelegate = lambda2.AssociatedAnonymousDelegate
            'Assert.Null(associatedDelegate)
            Assert.True(associatedDelegate.IsDelegateType)
            Assert.True(associatedDelegate.IsAnonymousType)
            Assert.Equal("Sub <generated method>(x As Object)", associatedDelegate.ToDisplayString)
            Assert.Same(associatedDelegate, DirectCast(lambda2, IMethodSymbol).AssociatedAnonymousDelegate)

            Dim node As InvocationExpressionSyntax = CompilationUtils.FindBindingText(Of InvocationExpressionSyntax)(compilation, "a.vb", 1)
            Dim info = model.GetSymbolInfo(node)
            Assert.Equal("Public Overridable Overloads Sub Invoke(obj As Object)", info.Symbol.ToDisplayString())
            Assert.Equal("System.Action(Of Object)", info.Symbol.ContainingSymbol.ToDisplayString())

            Assert.NotSame(associatedDelegate, info.Symbol.ContainingSymbol)
        End Sub

        <WorkItem(529688, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529688")>
        <Fact()>
        Public Sub AssociatedAnonymousDelegate_Action_DifferentSignature()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System

Module S1
    Public Sub Foo()
        Dim sss As Action = Sub(x) Console.WriteLine() 'BIND2:"x" 
        sss()'BIND1:"sss()" 
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim node2 As ParameterSyntax = CompilationUtils.FindBindingText(Of ParameterSyntax)(compilation, "a.vb", 2)
            Dim symbol2 = model.GetDeclaredSymbol(node2)
            Assert.Equal(SymbolKind.Parameter, symbol2.Kind)
            Assert.Equal("x As Object", symbol2.ToDisplayString())

            Dim lambda2 = DirectCast(symbol2.ContainingSymbol, MethodSymbol)
            Assert.Equal(MethodKind.LambdaMethod, lambda2.MethodKind)
            Assert.Equal("Private Shared Sub (x As Object)", symbol2.ContainingSymbol.ToDisplayString())

            Dim associatedDelegate = lambda2.AssociatedAnonymousDelegate
            Assert.Null(associatedDelegate)
            Assert.Same(associatedDelegate, DirectCast(lambda2, IMethodSymbol).AssociatedAnonymousDelegate)

            Dim node As InvocationExpressionSyntax = CompilationUtils.FindBindingText(Of InvocationExpressionSyntax)(compilation, "a.vb", 1)
            Dim info = model.GetSymbolInfo(node)
            Assert.Equal("Public Overridable Overloads Sub Invoke()", info.Symbol.ToDisplayString())
            Assert.Equal("System.Action", info.Symbol.ContainingSymbol.ToDisplayString())
        End Sub
    End Class

End Namespace


