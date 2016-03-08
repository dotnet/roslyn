' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Linq.Enumerable
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class ForLoopTests
        Inherits BasicTestBase

        <Fact()>
        Public Sub SimpleForeachWithVariableDeclaration()
            Dim source =
<compilation name="SimpleForeachWithVariableDeclaration">
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        For Each element as Integer In arr
            Console.WriteLine(element)
        Next
    End Sub
End Class        
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
23
42
]]>)
        End Sub

        <Fact()>
        Public Sub SimpleForeachWithNextVariable()
            Dim source =
<compilation name="SimpleForeachWithNextVariable">
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        For Each element as Integer In arr
            Console.WriteLine(element)
        Next element

        For Each x As Char In "Hello"
        Next (x)
    End Sub
End Class        
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
23
42
]]>)
        End Sub

        <Fact()>
        Public Sub NestedForeachAllNexts()
            Dim source =
<compilation name="NestedForeachAllNexts">
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        For Each element as Integer In arr
            For Each element2 as Integer In arr
                Console.WriteLine(element)
            Next
        Next 
    End Sub
End Class        
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
23
23
42
42
]]>)
        End Sub

        <Fact()>
        Public Sub NestedForeachNextWithVariables()
            Dim source =
<compilation name="NestedForeachNextWithVariables">
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        For Each element as Integer In arr
            For Each element2 as Integer In arr
                Console.WriteLine(element)
        Next element2, element
    End Sub
End Class        
    </file>
</compilation>

            CompileAndVerify(source,
                expectedOutput:=<![CDATA[
23
23
42
42
]]>)
        End Sub

        <Fact()>
        Public Sub SimpleForeachReuseLocalAsControlVariable()
            For Each infer In {"On", "Off"}
                Dim source =
<compilation name="SimpleForeachReuseLocalAsControlVariable">
    <file name="a.vb">
Option Strict On
Option Infer <%= infer %>

Imports System

    Class C1
        Public Shared Sub Main()
            Dim arr As Integer() = New Integer(1) {}
            arr(0) = 23
            arr(1) = 42

            Dim element_local As Integer

            For Each element_local In arr
                Console.WriteLine(element_local)
            Next element_local
        End Sub
    End Class
    </file>
</compilation>

                CompileAndVerify(source,
                expectedOutput:=<![CDATA[
23
42
]]>)
            Next
        End Sub

        <Fact()>
        Public Sub SimpleForeachReuseFromModuleAsControlVariableInaccessible()
            Dim expectedErrors As New Dictionary(Of String, XElement) From {
{"On", <expected></expected>},
{"Off", <expected>BC30389: 'M1.i' is not accessible in this context because it is 'Private'.
            For Each i In arr
                     ~</expected>}}

            For Each infer In {"On", "Off"}
                Dim source =
<compilation>
    <file name="a.vb">
Option Strict On
Option Infer <%= infer %>

Imports System

    Module M1
        Private i as Integer = 23
    End Module

    Class C1
        Public Shared Sub Main()
            Dim arr As Integer() = New Integer(1) {}
            arr(0) = 23
            arr(1) = 42

            For Each i In arr
                Console.WriteLine(23)
            Next
        End Sub
    End Class
    </file>
</compilation>

                Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
                AssertTheseDiagnostics(compilation, expectedErrors(infer))
            Next
        End Sub

        <Fact()>
        Public Sub SimpleForeachReuseStaticLocalAsControlVariable()
            For Each infer In {"On", "Off"}
                Dim source =
    <compilation name="SimpleForeachReuseStaticLocalAsControlVariable">
        <file name="a.vb">
Option Strict On
Option Infer <%= infer %>

Imports System

Class C1
    Public Shared Sub Main()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        static element_static as Integer

        For Each element_static In arr
            Console.WriteLine(element_static)
        Next element_static
    End Sub
End Class        
    </file>
    </compilation>

                CompileAndVerify(source,
                expectedOutput:=<![CDATA[
23
42
]]>)
            Next
        End Sub

        <Fact()>
        Public Sub SimpleForeachReuseFieldAsControlVariableInferOff()
            Dim source =
<compilation name="SimpleForeachReuseFieldAsControlVariableInferOff">
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared element as Integer

    Public Shared Sub Main()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        For Each element In arr
            Console.WriteLine(element)
        Next element
    End Sub
End Class        
    </file>
</compilation>

            CompileAndVerify(source,
                expectedOutput:=<![CDATA[
23
42
]]>)
        End Sub

        <Fact()>
        Public Sub SimpleForeachReuseArrayElementAsControlVariable()
            Dim source =
<compilation name="SimpleForeachReuseArrayElementAsControlVariable">
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared element as Integer

    Public Shared Sub Main()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        Dim x2(2) As Integer

        For Each x2(2) In arr
            Console.WriteLine(x2(2))
        Next
    End Sub
End Class        
    </file>
</compilation>

            CompileAndVerify(source,
                expectedOutput:=<![CDATA[
23
42
]]>)
        End Sub

        <Fact()>
        Public Sub SimpleForeachReuseFieldAsControlVariableInferOnUnqualified()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On
Option Infer On

Imports System

Class C1
    Public element as Integer

    Public Shared Sub Main()
        Dim c As New C1()
        c.DoStuff()
    End Sub

    Public Sub DoStuff()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        For Each element In arr
            Console.WriteLine(element)
        Next element
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <Fact()>
        Public Sub SimpleForeachReuseFieldAsControlVariableInferOnQualified()
            Dim source =
<compilation name="SimpleForeachReuseFieldAsControlVariableInferOnQualified">
    <file name="a.vb">
Option Strict On
Option Infer On

Imports System

Class C1
    Public element as Integer

    Public Shared Sub Main()
        Dim c As New C1()
        c.DoStuff()
    End Sub

    Public Sub DoStuff()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        For Each Me.element In arr
            Console.WriteLine(element)
        Next element
    End Sub
End Class       
    </file>
</compilation>

            CompileAndVerify(source,
                expectedOutput:=<![CDATA[
23
42
]]>)
        End Sub

        <Fact()>
        Public Sub SimpleForeachReuseFieldAsControlVariableInferOnQualified2()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On
Option Infer On

Imports System

Class C1
    Public Shared element as Integer

    Public Shared Sub Main()
        Dim c As New C1()
        c.DoStuff()
    End Sub

    Public Sub DoStuff()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        For Each Me.element In arr
            Console.WriteLine(element)
        Next element
    End Sub
End Class       
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        For Each Me.element In arr
                 ~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub SimpleForeachReuseFieldAsControlVariableInLambda()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On
Option Infer On

Imports System

Class C1
    Private element_lambda_field as Integer

    Public Shared Sub Main()
      Dim c1 As New C1()
      c1.DoStuff()
    End Sub

    Public Sub DoStuff()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        Dim myDelegate as Action = Sub()
                                      Dim element_lambda_local as Integer
                                      For Each element_lambda_local In arr
                                        Console.WriteLine(element_lambda_local)
                                      Next element_lambda_local

                                      For Each element_lambda_field In arr
                                        Console.WriteLine(element_lambda_field)
                                      Next element_lambda_field
                                    End Sub

        myDelegate.Invoke()
    End Sub
End Class       
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <Fact()>
        Public Sub SimpleForeachReuseParameterAsControlVariable()
            For Each infer In {"On", "Off"}
                Dim source =
<compilation name="SimpleForeachReuseParameterAsControlVariable">
    <file name="a.vb">
Option Strict On
Option Infer <%= infer %>

Imports System

    Class C1
        Public Shared Sub Main()
            DoStuff(20111104)
        End Sub

        Public Shared Sub DoStuff(byref element_parameter as Integer)
            Dim arr As Integer() = New Integer(1) {23, 42}

            For Each element_parameter In arr
                Console.WriteLine(element_parameter)
            Next element_parameter
        End Sub
    End Class
    </file>
</compilation>

                CompileAndVerify(source,
                expectedOutput:=<![CDATA[
23
42
]]>)
            Next
        End Sub

        <Fact()>
        Public Sub SimpleForeachReusePropertyAsControlVariable()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared Property element as Integer

    Public Shared Sub Main()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        For Each element In arr
            Console.WriteLine(element)
        Next element
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC30039: Loop control variable cannot be a property or a late-bound indexed array.
        For Each element In arr
                 ~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub SimpleForeachReuseTypeAsControlVariable1()
            Dim expectedErrors As New Dictionary(Of String, XElement) From {
{"On", <expected></expected>},
{"Off", <expected>BC30109: 'C1' is a class type and cannot be used as an expression.
        For Each C1 In arr
                 ~~</expected>}}

            For Each infer In {"On", "Off"}
                Dim source =
    <compilation>
        <file name="a.vb">
Option Strict On
Option Infer <%= infer %>

Imports System

Class C1
    Public Shared Sub Main()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        For Each C1 In arr
            Console.WriteLine("foo")
        Next
    End Sub
End Class        
    </file>
    </compilation>

                Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
                AssertTheseDiagnostics(compilation, expectedErrors(infer))
            Next
        End Sub

        <Fact()>
        Public Sub SimpleForeachReuseTypeAsControlVariable2()
            For Each infer In {"On", "Off"}
                Dim source =
    <compilation>
        <file name="a.vb">
Option Strict On
Option Infer <%= infer %>

Imports System

Class C1
    Public Shared Sub C1()
    End Sub

    Public Shared Sub Main()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        For Each C1 In arr
            Console.WriteLine("foo")
        Next C1
    End Sub
End Class        
    </file>
    </compilation>

                Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
                AssertTheseDiagnostics(compilation,
    <expected>
BC30068: Expression is a value and therefore cannot be the target of an assignment.
        For Each C1 In arr
                 ~~
    </expected>)
            Next
        End Sub

        <Fact()>
        Public Sub SimpleForeachSingleExistingMethodSymbols()
            For Each infer In {"On", "Off"}
                Dim source =
    <compilation>
        <file name="a.vb">
Option Strict On
Option Infer <%= infer %>

Imports System

Class C1
    Public Shared Sub Element()
    End Sub

    Public Shared Sub Main()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        For Each element In arr
            Console.WriteLine("foo")
        Next element
    End Sub
End Class        
    </file>
    </compilation>

                Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
                AssertTheseDiagnostics(compilation,
    <expected>
BC30068: Expression is a value and therefore cannot be the target of an assignment.
        For Each element In arr
                 ~~~~~~~
    </expected>)
            Next
        End Sub

        <Fact()>
        Public Sub SimpleForeachMultipleExistingSymbols()
            For Each infer In {"On", "Off"}
                Dim source =
    <compilation>
        <file name="a.vb">
Option Strict On
Option Infer <%= infer %>

Imports System

Class C1
    Public Shared Sub Element()
    End Sub
    Public Shared Sub Element(x as Integer)
    End Sub

    Public Shared Sub Main()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        For Each element In arr
            Console.WriteLine("foo")
        Next element
    End Sub
End Class        
    </file>
    </compilation>

                Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
                AssertTheseDiagnostics(compilation,
    <expected>
BC30068: Expression is a value and therefore cannot be the target of an assignment.
        For Each element In arr
                 ~~~~~~~
    </expected>)
            Next
        End Sub

        <Fact()>
        Public Sub SimpleForeachHideLocalWithControlVariable()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        Dim element as Integer
        element = 23

        For Each element as Integer In arr
            Console.WriteLine(element)
        Next element
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC30616: Variable 'element' hides a variable in an enclosing block.
        For Each element as Integer In arr
                 ~~~~~~~    
</expected>)
        End Sub

        <Fact()>
        Public Sub SimpleForeachHideFieldWithControlVariable()
            Dim source =
<compilation name="SimpleForeachHideFieldWithControlVariable">
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared element as Integer

    Public Shared Sub Main()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        For Each element as Integer In arr
            Console.WriteLine(element)
        Next element
    End Sub
End Class        
    </file>
</compilation>

            CompileAndVerify(source,
                expectedOutput:=<![CDATA[
23
42
]]>)
        End Sub

        <Fact()>
        Public Sub SimpleForeachNextVariableMismatch1()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        Dim element2 as Integer
        element2 = 23

        For Each element as Integer In arr
            Console.WriteLine(element)
        Next element2
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC30070: Next control variable does not match For loop control variable 'element'.
        Next element2
             ~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub SimpleForeachNextVariableMismatch2()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        For Each element as Integer In arr
            Console.WriteLine(element)
        Next element2
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC30451: 'element2' is not declared. It may be inaccessible due to its protection level.
        Next element2
             ~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub SimpleForeachNextVariableMismatch3()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        For Each a as Integer In arr
            For Each b as Integer In arr
                For Each c as Integer In arr
                    For Each d as Integer In arr
                        For Each e as Integer In arr
                            Console.WriteLine(e)
                    Next d, e
            Next b, b
        Next 
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC30070: Next control variable does not match For loop control variable 'e'.
                    Next d, e
                         ~
BC30451: 'e' is not declared. It may be inaccessible due to its protection level.
                    Next d, e
                            ~
BC30070: Next control variable does not match For loop control variable 'c'.
            Next b, b
                 ~
</expected>)
        End Sub

        <Fact()>
        Public Sub SimpleForeachNextVariableMismatch4()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        Dim element2 as Integer
        element2 = 23

        For Each element as Integer In arr
            Console.WriteLine(element)
        Next element, element2, element3
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC32037: 'Next' statement names more variables than there are matching 'For' statements.
        Next element, element2, element3
                      ~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub SimpleForeachNextVariableMismatch5()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        For Each element as Integer In arr
            For Each element2 as Integer In arr
                Console.WriteLine(element)
            Next element2
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC30084: 'For' must end with a matching 'Next'.
        For Each element as Integer In arr
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub SimpleForeachNextVariableMismatch6()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        For Each element as Integer In arr
            For Each element2 as Integer In arr
                Console.WriteLine(element)
            Next element2, element
        next undefined
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC30092: 'Next' must be preceded by a matching 'For'.
        next undefined
        ~~~~~~~~~~~~~~
BC32037: 'Next' statement names more variables than there are matching 'For' statements.
        next undefined
             ~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub SimpleForeachNextVariableMismatch7()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On
Option Infer Off

Public Class MyClass1
    Public Shared Sub Main()        
        For n = 0 To 2  
          For m = 1 To 2
            Next n 
          Next m
    End Sub
 End Class  
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC30451: 'n' is not declared. It may be inaccessible due to its protection level.
        For n = 0 To 2  
            ~
BC30451: 'm' is not declared. It may be inaccessible due to its protection level.
          For m = 1 To 2
              ~
BC30451: 'n' is not declared. It may be inaccessible due to its protection level.
            Next n 
                 ~
BC30451: 'm' is not declared. It may be inaccessible due to its protection level.
          Next m
               ~
</expected>)
        End Sub

        <Fact()>
        Public Sub InvalidLoopNesting()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        For Each a as Integer In arr    
            if 23 &lt; 42 then        
                For Each b As Integer In arr
                Next b, a
            end if
        End Sub
    End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC30084: 'For' must end with a matching 'Next'.
        For Each a as Integer In arr    
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC32037: 'Next' statement names more variables than there are matching 'For' statements.
                Next b, a
                        ~
</expected>)
        End Sub

        <Fact()>
        Public Sub NonLValueControlVariableExpression()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        For Each Main() In arr
                Console.WriteLine("?")
        Next 
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC30068: Expression is a value and therefore cannot be the target of an assignment.
        For Each Main() In arr
                 ~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub NoNullableModifierInControlVariableIdentifier()

            Dim source =
    <compilation>
        <file name="a.vb">
Imports System

Class C1
    Public Shared Sub Main()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        For Each element? In arr
            Console.WriteLine(element)
        Next 
    End Sub
End Class        
    </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, options:=TestOptions.ReleaseDll.WithOptionInfer(True).WithOptionStrict(OptionStrict.Off))

            AssertTheseDiagnostics(compilation,
<expected>
BC33112: Nullable modifier cannot be used with a variable whose implicit type is 'Object'.
        For Each element? In arr
                 ~~~~~~~~
BC36629: Nullable type inference is not supported in this context.
        For Each element? In arr
                 ~~~~~~~~
</expected>)

            compilation = compilation.WithOptions(TestOptions.ReleaseDll.WithOptionInfer(False).WithOptionStrict(OptionStrict.Off))

            AssertTheseDiagnostics(compilation,
<expected>
BC36629: Nullable type inference is not supported in this context.
        For Each element? In arr
                 ~~~~~~~~
</expected>)

            compilation = compilation.WithOptions(TestOptions.ReleaseDll.WithOptionInfer(False).WithOptionStrict(OptionStrict.On))

            AssertTheseDiagnostics(compilation,
<expected>
BC30209: Option Strict On requires all variable declarations to have an 'As' clause.
        For Each element? In arr
                 ~~~~~~~
BC36629: Nullable type inference is not supported in this context.
        For Each element? In arr
                 ~~~~~~~~
</expected>)

            compilation = compilation.WithOptions(TestOptions.ReleaseDll.WithOptionInfer(True).WithOptionStrict(OptionStrict.On))

            AssertTheseDiagnostics(compilation,
<expected>
BC30209: Option Strict On requires all variable declarations to have an 'As' clause.
        For Each element? In arr
                 ~~~~~~~
BC33112: Nullable modifier cannot be used with a variable whose implicit type is 'Object'.
        For Each element? In arr
                 ~~~~~~~~
BC36629: Nullable type inference is not supported in this context.
        For Each element? In arr
                 ~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub NoNullableModifierInControlVariableIdentifier2()
            Dim expectedErrors As New Dictionary(Of String, XElement) From {
{"On", <expected>BC30616: Variable 'element' hides a variable in an enclosing block.
        For Each element? In arr
                 ~~~~~~~~
BC33112: Nullable modifier cannot be used with a variable whose implicit type is 'Object'.
        For Each element? In arr
                 ~~~~~~~~
BC36629: Nullable type inference is not supported in this context.
        For Each element? In arr
                 ~~~~~~~~</expected>},
{"Off", <expected>BC30616: Variable 'element' hides a variable in an enclosing block.
        For Each element? In arr
                 ~~~~~~~~
BC36629: Nullable type inference is not supported in this context.
        For Each element? In arr
                 ~~~~~~~~</expected>}}

            For Each infer In {"On", "Off"}
                Dim source =
    <compilation>
        <file name="a.vb">
Option Strict Off
Option Infer <%= infer %>

Imports System

Class C1
    Public Shared Sub Main()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        Dim element as Integer = 23

        For Each element? In arr
            Console.WriteLine(element)
        Next 
    End Sub
End Class        
    </file>
    </compilation>

                Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
                AssertTheseDiagnostics(compilation, expectedErrors(infer))
            Next
        End Sub

        <Fact()>
        Public Sub NoArraySizesInControlVariableDeclaration()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        Dim threeDimJaggedArray()()() As Integer = New Integer(2)()() {}

        For Each twoDimJaggedArray(1)() as Integer In threeDimJaggedArray
            Console.WriteLine("foo")
        Next twoDimJaggedArray
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC32039: Array declared as for loop control variable cannot be declared with an initial size.
        For Each twoDimJaggedArray(1)() as Integer In threeDimJaggedArray
                 ~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub InnerForReusesControlVariableOfOuterFor1()
            Dim source =
<compilation>
    <file name="a.vb">
Option Infer Off
Option Strict On

Imports System

Class C1
    Public Shared element as Integer

    Public Shared Sub Main()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        For Each element In arr ' outer              
            For Each element In arr ' inner
                Console.WriteLine(element)
            Next
        Next
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC30069: For loop control variable 'element' already in use by an enclosing For loop.
            For Each element In arr ' inner
                     ~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub InnerForReusesControlVariableOfOuterFor2()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        For Each element as Integer In arr ' outer              
            For Each element In arr ' inner
                Console.WriteLine(element)
            Next
        Next
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC30069: For loop control variable 'element' already in use by an enclosing For loop.
            For Each element In arr ' inner
                     ~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub InnerForReusesControlVariableOfOuterFor3()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Structure Struct1
    Public X as Integer
End Structure

Class C1
    Public Shared Sub Main()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        Dim s1 as Struct1 
        For Each s1.X In arr ' outer              
            Dim s2 as Struct1
            ' unexpectedly an error, but this is Dev10 behavior
            For Each s2.X In arr ' inner
                Console.WriteLine(s2.X)
            Next
        Next
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC30069: For loop control variable 'X' already in use by an enclosing For loop.
            For Each s2.X In arr ' inner
                     ~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub InnerForReusesControlVariableOfOuterFor4()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared element as Integer

    Public Shared Sub Main()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        For Each arr(0) In arr
            For Each arr(1) In arr
                Console.WriteLine(arr(0))
            Next
        next
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC30069: For loop control variable 'arr' already in use by an enclosing For loop.
            For Each arr(1) In arr
                     ~~~~~~
</expected>)
        End Sub

        ''' Bug 8590
        <Fact()>
        Public Sub MultipleNamesForControlVariable()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        For Each element, element2 as Integer In arr
            Console.WriteLine(element)
        Next
    End Sub
End Class        
    </file>
</compilation>

            CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_ExpectedIn, ""),
                Diagnostic(ERRID.ERR_Syntax, ","))
        End Sub

        ''' Bug 8590
        <Fact()>
        Public Sub InitializerInControlVariable()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        For Each element as Integer = 23 In arr
            Console.WriteLine(element)
        Next
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_ExpectedIn, ""),
                Diagnostic(ERRID.ERR_Syntax, "="))
        End Sub

        <Fact()>
        Public Sub LocalInferenceForControlVariable()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On
Option Infer Off

Imports System

Class C1
    Public Shared Sub Main()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        For Each element In arr
            Console.WriteLine(element)
        Next
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC30451: 'element' is not declared. It may be inaccessible due to its protection level.
        For Each element In arr
                 ~~~~~~~
BC30451: 'element' is not declared. It may be inaccessible due to its protection level.
            Console.WriteLine(element)
                              ~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub ControlVariableWithTypeCharacter()
            Dim expectedErrors As New Dictionary(Of String, XElement) From {
{"On", <expected></expected>},
{"Off", <expected>BC30451: 'element' is not declared. It may be inaccessible due to its protection level.
        For Each element% In arr
                 ~~~~~~~~</expected>}}

            For Each infer In {"On", "Off"}
                Dim source =
    <compilation>
        <file name="a.vb">
Option Strict On
Option Infer <%= infer %>

Imports System

Class C1
    Public Shared Sub Main()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        For Each element% In arr
            Console.WriteLine("hello")
        Next
    End Sub
End Class        
    </file>
    </compilation>

                Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
                AssertTheseDiagnostics(compilation, expectedErrors(infer))
            Next
        End Sub

        <Fact()>
        Public Sub SimpleForeachWithString()
            Dim source =
<compilation name="SimpleForeachWithString">
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        For Each element as Char In "Hello!"
            Console.WriteLine(element)
        Next
    End Sub
End Class        
    </file>
</compilation>

            CompileAndVerify(source,
                expectedOutput:=<![CDATA[
H
e
l
l
o
!
]]>)

        End Sub

        <Fact()>
        Public Sub SimpleForeachWithArrayListCollection()
            Dim source =
<compilation name="SimpleForeachWithArrayListCollection">
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections

Class C1
    Public Shared Sub Main()
        Dim myList As ArrayList = New ArrayList()
        myList.Add(23)
        myList.Add(42)

        For Each element as Integer In myList
            Console.WriteLine(element)
        Next
    End Sub
End Class        
    </file>
</compilation>

            CompileAndVerify(source,
                expectedOutput:=<![CDATA[
23
42
]]>)

        End Sub

        <Fact()>
        Public Sub SimpleForeachWithCustomCollection()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections

Class Custom
    Public Function GetEnumerator() As CustomEnumerator
        Return Nothing
    End Function

    Public Class CustomEnumerator
        Public Function MoveNext() As Boolean
            Return False
        End Function

        Public ReadOnly Property Current As Custom
            Get
                Return Nothing
            End Get
        End Property
    End Class
End Class

Class C1
    Public Shared Sub Main()
        Dim myCustomCollection As Custom = nothing

        For Each element as Custom In myCustomCollection
            Console.WriteLine("foo")
        Next
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <Fact()>
        Public Sub SimpleForeachWithCustomCollectionOptionalParametersOk()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections

Class Custom
    Public Function GetEnumerator(Optional foo as integer = 1) As CustomEnumerator
        Return Nothing
    End Function

    Public Class CustomEnumerator
        Public Function MoveNext(Optional foo as boolean = false) As Boolean
            Return False
        End Function

        Public ReadOnly Property Current As Custom
            Get
                Return Nothing
            End Get
        End Property
    End Class
End Class

Class C1
    Public Shared Sub Main()
        Dim myCustomCollection As Custom = nothing

        For Each element as Custom In myCustomCollection
            Console.WriteLine("foo")
        Next
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
</expected>)
        End Sub

        <Fact()>
        Public Sub SimpleForeachWithCustomCollectionFirstLookupNotMethod()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections

Class Custom
    Public GetEnumerator As Object = nothing

    Public Class CustomEnumerator
        Public Function MoveNext() As Boolean
            Return False
        End Function

        Public ReadOnly Property Current As Custom
            Get
                Return Nothing
            End Get
        End Property
    End Class
End Class

Class C1
    Public Shared Sub Main()
        Dim myCustomCollection As Custom = nothing

        For Each element as Custom In myCustomCollection
            Console.WriteLine("foo")
        Next
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC32023: Expression is of type 'Custom', which is not a collection type.
        For Each element as Custom In myCustomCollection
                                      ~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub SimpleForeachWithCustomCollectionProtectedInaccessible()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections

Public Class Custom
    Friend Function GetEnumerator() As CustomEnumerator
        Return Nothing
    End Function

    Public Class CustomEnumerator
        Protected Function MoveNext() As Boolean
            Return False
        End Function

        Protected ReadOnly Property Current As Custom
            Get
                Return Nothing
            End Get
        End Property
    End Class
End Class

Class C1
    Public Shared Sub Main()
        Dim myCustomCollection As Custom = nothing

        For Each element as Custom In myCustomCollection
            Console.WriteLine("foo")
        Next
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC32023: Expression is of type 'Custom', which is not a collection type.
        For Each element as Custom In myCustomCollection
                                      ~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub SimpleForeachWithCustomCollectionProtectedAccessible()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections

Public Class Custom
    Friend Function GetEnumerator() As CustomEnumerator
        Return Nothing
    End Function
End Class

Public Class CustomEnumerator
    Protected Function MoveNext() As Boolean
        Return False
    End Function

    Protected ReadOnly Property Current As Custom
        Get
            Return Nothing
        End Get
    End Property

    Public Shared Sub Main()
        Dim myCustomCollection As Custom = nothing

        For Each element as Custom In myCustomCollection
            Console.WriteLine("foo")
        Next
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <Fact()>
        Public Sub SimpleForeachWithCustomCollectionExtensionMethods()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections
Imports System.Runtime.CompilerServices

Class Custom
    Public Class CustomEnumerator
        Public Function MoveNext() As Boolean
            Return False
        End Function

        Public ReadOnly Property Current As Custom
            Get
                Return Nothing
            End Get
        End Property
    End Class
End Class

Module M1
    &lt;Extension()&gt;
    Public Function GetEnumerator(ByVal aCustom as Custom) As Custom.CustomEnumerator
        Return Nothing
    End Function
End Module

Class C1
    Public Shared Sub Main()
        Dim myCustomCollection As Custom = nothing

        For Each element as Custom In myCustomCollection
            Console.WriteLine("foo")
        Next
    End Sub
End Class        

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace

    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <Fact()>
        Public Sub SimpleForeachWithCustomCollectionInstanceAndExtensionMethods()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections
Imports System.Runtime.CompilerServices

Class Custom
    Public Function GetEnumerator(ByVal foo as Integer) As Double
        Return Nothing
    End Function

    Public Class CustomEnumerator
        Public Function MoveNext() As Boolean
            Return False
        End Function

        Public ReadOnly Property Current As Custom
            Get
                Return Nothing
            End Get
        End Property
    End Class
End Class

Module M1
    &lt;Extension()&gt;
    Public Function GetEnumerator(ByVal aCustom as Custom) As Custom.CustomEnumerator
        Return Nothing
    End Function
End Module

Class C1
    Public Shared Sub Main()
        Dim myCustomCollection As Custom = nothing

        For Each element as Custom In myCustomCollection
            Console.WriteLine("foo")
        Next
    End Sub
End Class        

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace

    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <Fact()>
        Public Sub SimpleForeachWithIEnumerable()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections

Public Interface IBetterEnumerable
    Inherits IEnumerable
End Interface

Public Class SomethingEnumerable
    Implements IEnumerable

    Public Function GetEnumerator2() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
        Return Nothing
    End Function
End Class

Class C1
    Public Shared Sub Main()
        Dim myCollection1 As IEnumerable = nothing
        For Each element as IEnumerable In myCollection1
            Console.WriteLine("foo")
        Next

        Dim myCollection2 As IBetterEnumerable = nothing
        For Each element as IBetterEnumerable In myCollection2
            Console.WriteLine("foo")
        Next

        Dim myCollection3 As SomethingEnumerable = nothing
        For Each element as SomethingEnumerable In myCollection3
            Console.WriteLine("foo")
        Next
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertNoErrors(compilation)
        End Sub

        <Fact()>
        Public Sub SimpleForeachWithGenericIEnumerable()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections.Generic

Public Interface IBetterEnumerable(Of T)
    Inherits IEnumerable(Of T)
End Interface

Public Class SomethingEnumerable(Of T)
    Implements IEnumerable(Of T)

    Public Function GetEnumerator() As System.Collections.Generic.IEnumerator(Of T) Implements System.Collections.Generic.IEnumerable(Of T).GetEnumerator
        Return Nothing
    End Function

    Public Function GetEnumerator1() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
        Return Nothing
    End Function
End Class

Class C1
    Public Shared Sub Main()
        Dim myCollection1 As IEnumerable(Of String) = nothing
        For Each element as String In myCollection1
            Console.WriteLine("foo")
        Next

        Dim myCollection2 As IBetterEnumerable(Of String) = nothing
        For Each element as String In myCollection2
            Console.WriteLine("foo")
        Next

        Dim myCollection3 As SomethingEnumerable(Of String) = nothing
        For Each element as String In myCollection3
            Console.WriteLine("foo")
        Next
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertNoErrors(compilation)
        End Sub

        <Fact()>
        Public Sub MultipleGenericIEnumerableImplementations()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System.Collections.Generic
Imports System.Collections

Module M1
    Sub Main()
        'Scenario 1
        Dim o3 As New C3(Of Integer)
        For Each i As Short In o3
        Next

        'Scenario 2
        Dim o4 As New C4(Of Integer)
        For Each i As Short In o4
        Next
    End Sub
End Module

Class C3(Of T)
    Implements IEnumerable(Of Integer), IEnumerable(Of Double)

    Private Function GetEnumerator() As System.Collections.Generic.IEnumerator(Of Double) Implements System.Collections.Generic.IEnumerable(Of Double).GetEnumerator
      return nothing
    End Function

    Private Function GetEnumerator1() As System.Collections.Generic.IEnumerator(Of Integer) Implements System.Collections.Generic.IEnumerable(Of Integer).GetEnumerator
      return nothing
    End Function

    Private Function GetEnumerator2() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
      return nothing
    End Function

End Class

Class B4
    Implements IEnumerable(Of Integer)

    Private Function GetEnumerator() As System.Collections.Generic.IEnumerator(Of Integer) Implements System.Collections.Generic.IEnumerable(Of Integer).GetEnumerator
      return nothing
    End Function

    Private Function GetEnumerator1() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
      return nothing
    End Function        
End Class

Class C4(Of T)
    Inherits B4
    Implements IEnumerable(Of Double)

    Private Function GetEnumerator() As System.Collections.Generic.IEnumerator(Of Double) Implements System.Collections.Generic.IEnumerable(Of Double).GetEnumerator
      return nothing
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC32096: 'For Each' on type 'C3(Of Integer)' is ambiguous because the type implements multiple instantiations of 'System.Collections.Generic.IEnumerable(Of T)'.
        For Each i As Short In o3
                               ~~
BC32096: 'For Each' on type 'C4(Of Integer)' is ambiguous because the type implements multiple instantiations of 'System.Collections.Generic.IEnumerable(Of T)'.
        For Each i As Short In o4
                               ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub MultipleGenericIEnumerableImplementationsOnT()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On
Imports System.Collections
Imports System.Collections.Generic
Structure S
End Structure
Interface I
    Inherits IEnumerable(Of S)
End Interface
Class A
    Implements IEnumerable(Of Integer)
    Private Function GetEnumerator() As IEnumerator(Of Integer) Implements IEnumerable(Of Integer).GetEnumerator
        Return Nothing
    End Function
    Private Function GetEnumerator1() As IEnumerator Implements IEnumerable.GetEnumerator
        Return Nothing
    End Function
End Class
Class B
    Inherits A
    Implements I
    Private Function GetEnumerator() As IEnumerator(Of S) Implements IEnumerable(Of S).GetEnumerator
        Return Nothing
    End Function
End Class
Class C
    Shared Sub M(Of T1 As A, T2 As B, T3 As {T1, I})(_a As A, _b As B, _1 As T1, _2 As T2, _3 As T3)
        For Each o As Integer In _a
        Next
        For Each o In _b
        Next
        For Each o As Integer In _1
        Next
        For Each o In _2
        Next
        For Each o As S In _3
        Next
    End Sub
End Class
    </file>
</compilation>
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC32096: 'For Each' on type 'B' is ambiguous because the type implements multiple instantiations of 'System.Collections.Generic.IEnumerable(Of T)'.
        For Each o In _b
                      ~~
BC32096: 'For Each' on type 'T2' is ambiguous because the type implements multiple instantiations of 'System.Collections.Generic.IEnumerable(Of T)'.
        For Each o In _2
                      ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub AmbiguousCurrentImplementationsOnT()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On
Imports System.Collections
Imports System.Collections.Generic


Class C1(Of T)
    Public Function GetEnumerator() As T
        Return Nothing
    End Function
End Class

Class C
    Shared Sub M(Of T1 As {IEnumerator(Of Integer), IEnumerator(Of String)}, T2 As C1(Of T1))(p As T2)
        For Each o In p
        Next
    End Sub
End Class      
    </file>
</compilation>
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC30521: Overload resolution failed because no accessible 'Current' is most specific for these arguments:
    'Property System.Collections.Generic.IEnumerator(Of Integer).Current As Integer': Not most specific.
    'Property System.Collections.Generic.IEnumerator(Of String).Current As String': Not most specific.
        For Each o In p
                      ~
</expected>)
        End Sub

        <Fact()>
        Public Sub NoPrecedingWarningsWithAmbiguousCurrentImplementationsOnT()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On
Imports System.Collections
Imports System.Collections.Generic


Class C1(Of T)
    Shared Public Function GetEnumerator() As T     ' without a succeeding error, an instance static mismatch would have been reported as warning.
        Return Nothing
    End Function
End Class

Class C
    Shared Sub M(Of T1 As {IEnumerator(Of Integer), IEnumerator(Of String)}, T2 As C1(Of T1))(p As T2)
        For Each o In p
        Next
    End Sub
End Class      
    </file>
</compilation>
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC30521: Overload resolution failed because no accessible 'Current' is most specific for these arguments:
    'Property System.Collections.Generic.IEnumerator(Of Integer).Current As Integer': Not most specific.
    'Property System.Collections.Generic.IEnumerator(Of String).Current As String': Not most specific.
        For Each o In p
                      ~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        For Each o In p
                      ~
</expected>)
        End Sub

        <Fact()>
        Public Sub AmbiguousMoveNextImplementationsOnT()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On
Imports System.Collections
Imports System.Collections.Generic


Class C1(Of T)
    Public Function GetEnumerator() As T
        Return Nothing
    End Function
End Class

Interface IWithMoveNext
    Function MoveNext() As Boolean
End Interface

Class C
    Shared Sub M(Of T1 As {IEnumerator(Of Integer), IWithMoveNext}, T2 As C1(Of T1))(p As T2)
        For Each o In p
        Next
    End Sub
End Class  
    </file>
</compilation>
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC32023: Expression is of type 'T2', which is not a collection type.
        For Each o In p
                      ~
</expected>)
        End Sub

        <Fact()>
        Public Sub MoveNextExtensionMethodImplementationsDifferentGenericityOnT()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On
Imports System.Collections
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices

Class C1
    Public Function GetEnumerator() As MyEnumerator
        Return Nothing
    End Function
End Class

Class MyEnumerator

    Public ReadOnly Property Current As Object
        Get
            Return New Integer()
        End Get
    End Property

End Class

Module Extensions1
    &lt;Extension()>
    Function MoveNext(Of S As MyEnumerator)(o As S) As Boolean
        Return False
    End Function
End Module

Module Extensions2
    &lt;Extension()>
    Function MoveNext(o As MyEnumerator) As Boolean
        Return False
    End Function
End Module

Class C
    Shared Sub M(Of T1 As C1)(p As T1)
        For Each o In p
        Next
    End Sub
End Class

Module Program
    Sub Main()
    End Sub
End Module    
</file>
</compilation>
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, additionalRefs:={SystemCoreRef})
            compilation.AssertNoDiagnostics()
            ' NOTE: this did not succeed in Dev10, but it does in Roslyn because we do a full overload resolution and can decide whether this 
            ' is ambiguous or not.
        End Sub

        <Fact()>
        Public Sub BindingWarningsFromMatchCollectionDesignPattern_1()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On
Imports System.Collections
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices


Class C1
    Public Function GetEnumerator() As MyEnumerator
        Return Nothing
    End Function
End Class

Class MyEnumerator

    Public ReadOnly Property Current As Object
        Get
            Return New Integer()
        End Get
    End Property

    Shared Function MoveNext() As Boolean
        Return False
    End Function

End Class

Class C
    Shared Sub M(Of T1 As C1)(p As T1)
        For Each o In p
        Next
    End Sub
End Class

Module Program
    Sub Main()
    End Sub
End Module
</file>
</compilation>
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, additionalRefs:={SystemCoreRef})
            AssertTheseDiagnostics(compilation,
                              <expected>
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        For Each o In p
                      ~                                  
                              </expected>)
        End Sub

        <Fact()>
        Public Sub BindingWarningsFromMatchCollectionDesignPatternAccumulate()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On
Imports System.Collections
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices


Class C1
    Public Function GetEnumerator() As MyEnumerator
        Return Nothing
    End Function
End Class

Class MyEnumerator

    Shared Public ReadOnly Property Current As Object
        Get
            Return New Integer()
        End Get
    End Property

    Shared Function MoveNext() As Boolean
        Return False
    End Function

End Class

Class C
    Shared Sub M(Of T1 As C1)(p As T1)
        For Each o In p
        Next
    End Sub
End Class

Module Program
    Sub Main()
    End Sub
End Module
</file>
</compilation>
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, additionalRefs:={SystemCoreRef})
            AssertTheseDiagnostics(compilation,
                              <expected>
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        For Each o In p
                      ~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        For Each o In p
                      ~
                              </expected>)
        End Sub

        <Fact()>
        Public Sub CollectionDoesNotMatchDesignPattern()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        Dim local as Integer
        For Each element as Integer In local
            Console.WriteLine(element)
        Next

        For Each element2 as Integer In Main()
            Console.WriteLine(element2)
        Next
    End Sub

    Public Shared Sub UnconstrainedTypeParameter(Of T)()
        Dim myCollection as T = nothing
        For Each element as Object in myCollection
            Console.WriteLine(element)
        Next
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC32023: Expression is of type 'Integer', which is not a collection type.
        For Each element as Integer In local
                                       ~~~~~
BC30491: Expression does not produce a value.
        For Each element2 as Integer In Main()
                                        ~~~~~~
BC32023: Expression is of type 'T', which is not a collection type.
        For Each element as Object in myCollection
                                      ~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub SimpleForeachWithCustomCollectionGetEnumeratorAsStructure()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections

Class Custom
    Public Function GetEnumerator() As CustomEnumerator
        Return Nothing
    End Function

    Public Structure CustomEnumerator
        Public Function MoveNext() As Boolean
            Return False
        End Function

        Public ReadOnly Property Current As Custom
            Get
                Return Nothing
            End Get
        End Property
    End Structure
End Class

Class C1
    Public Shared Sub Main()
        Dim myCustomCollection As Custom = nothing

        For Each element as Custom In myCustomCollection
            Console.WriteLine("foo")
        Next
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <Fact()>
        Public Sub SimpleForeachWithCustomCollectionGetEnumeratorAsTypeParameter()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections

Public Class Custom(Of T)
    Public Function GetEnumerator() As T
        Return Nothing
    End Function
End Class

Public Structure CustomEnumerator
    Public Function MoveNext() As Boolean
        Return False
    End Function

    Public ReadOnly Property Current As Integer
        Get
            Return Nothing
        End Get
    End Property
End Structure

Class C1
    Public Shared Sub Main()
        Dim myCustomCollection As Custom(Of CustomEnumerator) = Nothing

        For Each element As Integer In myCustomCollection
            Console.WriteLine("foo")
        Next
    End Sub
End Class   
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <Fact()>
        Public Sub SimpleForeachWithCollectionTypeIsTypeParameterWithIEnumerableConstraint()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections

Class C1
    Public Shared Sub Main()

    End Sub

    Public Sub DoStuff(Of T as IEnumerable)()
        Dim myCustomCollection As T = Nothing

        For Each element As Integer In myCustomCollection
            Console.WriteLine("foo")
        Next
    End Sub
End Class   
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertNoErrors(compilation)
        End Sub

        <Fact()>
        Public Sub ForEachWithMultidimArray()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        DoStuff(nothing)
    End Sub

    Public Shared Sub DoStuff(arr as Integer(,) )

        For Each element as Integer In arr
            Console.WriteLine(element)
        Next element
    End Sub
End Class        
    </file>
</compilation>

            CompileAndVerify(source, options:=TestOptions.ReleaseExe).VerifyIL("C1.DoStuff", <![CDATA[
{
  // Code size       34 (0x22)
  .maxstack  1
  .locals init (System.Collections.IEnumerator V_0)
  IL_0000:  ldarg.0
  IL_0001:  callvirt   "Function System.Array.GetEnumerator() As System.Collections.IEnumerator"
  IL_0006:  stloc.0
  IL_0007:  br.s       IL_0019
  IL_0009:  ldloc.0
  IL_000a:  callvirt   "Function System.Collections.IEnumerator.get_Current() As Object"
  IL_000f:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToInteger(Object) As Integer"
  IL_0014:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0019:  ldloc.0
  IL_001a:  callvirt   "Function System.Collections.IEnumerator.MoveNext() As Boolean"
  IL_001f:  brtrue.s   IL_0009
  IL_0021:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ForEachIEnumerableWorkingInErrorType()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Class Enumerable
    Inherits SomeUnknownTypeInAddition
    Implements System.Collections.IEnumerable
    ' Explicit implementation won't match pattern.
    Private Function System_Collections_IEnumerable_GetEnumerator() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
        Dim list As New System.Collections.Generic.List(Of Integer)()
        list.Add(3)
        list.Add(2)
        list.Add(1)
        Return list.GetEnumerator()
    End Function
End Class

Class C1
    Public Shared Sub Main()

        For Each element as Integer In New Enumerable
            Console.WriteLine(element)
        Next element
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC30002: Type 'SomeUnknownTypeInAddition' is not defined.
    Inherits SomeUnknownTypeInAddition
             ~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub ControlVariableInvalidConversionFromCharToInteger_String()
            For Each optionStrict In {"On", "Off"}
                Dim source =
    <compilation>
        <file name="a.vb">
Option Strict <%= optionStrict %>

Imports System

Class C1
    Public Shared Sub Main()
        For Each element as Integer In "Hello World."
            Console.WriteLine(element)
        Next
    End Sub
End Class        
    </file>
    </compilation>

                Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
                AssertTheseDiagnostics(compilation,
<expected>
BC32006: 'Char' values cannot be converted to 'Integer'. Use 'Microsoft.VisualBasic.AscW' to interpret a character as a Unicode value or 'Microsoft.VisualBasic.Val' to interpret it as a digit.
        For Each element as Integer In "Hello World."
                                       ~~~~~~~~~~~~~~
</expected>)
            Next
        End Sub

        <Fact()>
        Public Sub ControlVariableInvalidConversionFromCharToInteger_Array()
            For Each optionStrict In {"On", "Off"}
                Dim source =
    <compilation>
        <file name="a.vb">
Option Strict <%= optionStrict %>

Imports System

Class C1
    Public Shared Sub Main()
        Dim arr(2) as Char
        arr(0) = "a"c
        arr(1) = "b"c
        arr(2) = "c"c
        For Each element as Integer In arr
            Console.WriteLine(element)
        Next
    End Sub
End Class        
    </file>
    </compilation>

                Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
                AssertTheseDiagnostics(compilation,
<expected>
BC32006: 'Char' values cannot be converted to 'Integer'. Use 'Microsoft.VisualBasic.AscW' to interpret a character as a Unicode value or 'Microsoft.VisualBasic.Val' to interpret it as a digit.
        For Each element as Integer In arr
                                       ~~~
</expected>)
            Next
        End Sub

        <Fact()>
        Public Sub ControlVariableInvalidConversionFromCharToInteger_IEnumerable()
            For Each optionStrict In {"On", "Off"}
                Dim source =
    <compilation>
        <file name="a.vb">
Option Strict <%= optionStrict %>

Imports System
Imports System.Collections.Generic

Class C1
    Public Shared Sub Main()
        Dim arr(2) as Char
        arr(0) = "a"c
        arr(1) = "b"c
        arr(2) = "c"c
        Dim iface as IEnumerable(Of Char) = arr

        For Each element as Integer In iface
            Console.WriteLine(element)
        Next
    End Sub
End Class        
    </file>
    </compilation>

                Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
                AssertTheseDiagnostics(compilation,
<expected>
BC32006: 'Char' values cannot be converted to 'Integer'. Use 'Microsoft.VisualBasic.AscW' to interpret a character as a Unicode value or 'Microsoft.VisualBasic.Val' to interpret it as a digit.
        For Each element as Integer In iface
                                       ~~~~~
</expected>)
            Next
        End Sub

        <Fact()>
        Public Sub BreakFromForeach()
            Dim TEMP = CompileAndVerify(
<compilation name="BreakFromForeach">
    <file name="a.vb">
Class C
    Public Shared Sub Main()
        For Each x As Char In "Hello!"
            If x = "o"c Then
                Exit For
            Else
                System.Console.WriteLine(x)
            End If
        Next
    End Sub
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
H
e
l
l
]]>)
        End Sub

        ' Continuing for nested Loops
        <Fact()>
        Public Sub ContinueInForeach()
            Dim TEMP = CompileAndVerify(
<compilation name="ContinueInForeach">
    <file name="a.vb">
Class C
    Public Shared Sub Main()
        For Each x As Char In "Hello!"
            If x = "a"c Then
                continue For
            end if

            System.Console.WriteLine(x)
        Next
    End Sub
End Class
    </file>
</compilation>, options:=TestOptions.ReleaseExe, expectedOutput:=<![CDATA[
H
e
l
l
o
!
]]>).VerifyIL("C.Main", <![CDATA[
{
  // Code size       43 (0x2b)
  .maxstack  2
  .locals init (String V_0,
                Integer V_1,
                Char V_2) //x
  IL_0000:  ldstr      "Hello!"
  IL_0005:  stloc.0
  IL_0006:  ldc.i4.0
  IL_0007:  stloc.1
  IL_0008:  br.s       IL_0021
  IL_000a:  ldloc.0
  IL_000b:  ldloc.1
  IL_000c:  callvirt   "Function String.get_Chars(Integer) As Char"
  IL_0011:  stloc.2
  IL_0012:  ldloc.2
  IL_0013:  ldc.i4.s   97
  IL_0015:  beq.s      IL_001d
  IL_0017:  ldloc.2
  IL_0018:  call       "Sub System.Console.WriteLine(Char)"
  IL_001d:  ldloc.1
  IL_001e:  ldc.i4.1
  IL_001f:  add.ovf
  IL_0020:  stloc.1
  IL_0021:  ldloc.1
  IL_0022:  ldloc.0
  IL_0023:  callvirt   "Function String.get_Length() As Integer"
  IL_0028:  blt.s      IL_000a
  IL_002a:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ControlVariableWithNarrowingConversionIsExplicit()
            For Each optionStrict In {"On", "Off"}
                Dim source =
    <compilation>
        <file name="a.vb">
Option Strict <%= optionStrict %>

Imports System.Collections

Class C
    Public Shared Sub Main()
        For Each x as Integer In New Enumerable()
            System.Console.WriteLine(x)
        Next
    End Sub
End Class

Class Enumerable
    Implements IEnumerable
    ' Explicit implementation won't match pattern.
    Private Function System_Collections_IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Dim list As New Generic.List(Of Integer)()
        list.Add(3)
        list.Add(2)
        list.Add(1)
        Return list.GetEnumerator()
    End Function
End Class
    </file>
    </compilation>

                CompileAndVerify(source, expectedOutput:=<![CDATA[
3
2
1
]]>)
            Next
        End Sub

        ''' Bug 8821
        <Fact()>
        Public Sub ControlVariableVerificationOfBadNodes()
            Dim expectedErrors As New Dictionary(Of String, XElement) From {
            {"On", <expected></expected>},
            {"Off", <expected>
BC30451: 'c1' is not declared. It may be inaccessible due to its protection level.
        For Each c1 In New List(Of Integer)
                 ~~
BC30451: 'c2' is not declared. It may be inaccessible due to its protection level.
            For Each c2 In New List(Of Integer)
                     ~~
            </expected>}}

            For Each infer In {"On", "Off"}
                Dim source =
        <compilation>
            <file name="a.vb">
Option Infer <%= infer %>

Imports System.Collections.Generic

Module M
    Public Sub Main()
        For Each c1 In New List(Of Integer)
            For Each c2 In New List(Of Integer)
            Next
        Next
    End Sub
End Module
    </file>
        </compilation>

                Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
                AssertTheseDiagnostics(compilation, expectedErrors(infer))
            Next
        End Sub

        <Fact()>
        Public Sub UninitializedReferences_1()
            Dim source =
    <compilation>
        <file name="a.vb">
Option Infer On

Class C1
  public function foo() as Integer()
    return new Integer() {1,2,3}
  end function
End Class

Module M
    Public Sub Main()

        Dim unassignedRef1, unassignedRef2, unassignedRef3, unassignedRef4 as C1

        For Each unassignedRef1.foo()(0) In unassignedRef2.foo()

            if unassignedRef3 is nothing then
            End if
        Next unassignedRef1.foo()(unassignedRef4.foo()(0))

        For each unassignedRef8 as C1 in New C1() {unassignedRef8}
            System.Console.WriteLine(unassignedRef8)
        Next 
    End Sub
End Module
    </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC42024: Unused local variable: 'unassignedRef4'.
        Dim unassignedRef1, unassignedRef2, unassignedRef3, unassignedRef4 as C1
                                                            ~~~~~~~~~~~~~~
BC42104: Variable 'unassignedRef1' is used before it has been assigned a value. A null reference exception could result at runtime.
        For Each unassignedRef1.foo()(0) In unassignedRef2.foo()
                 ~~~~~~~~~~~~~~
BC42104: Variable 'unassignedRef2' is used before it has been assigned a value. A null reference exception could result at runtime.
        For Each unassignedRef1.foo()(0) In unassignedRef2.foo()
                                            ~~~~~~~~~~~~~~
BC42104: Variable 'unassignedRef3' is used before it has been assigned a value. A null reference exception could result at runtime.
            if unassignedRef3 is nothing then
               ~~~~~~~~~~~~~~
BC42104: Variable 'unassignedRef8' is used before it has been assigned a value. A null reference exception could result at runtime.
        For each unassignedRef8 as C1 in New C1() {unassignedRef8}
                                                   ~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub UninitializedReferences_2()
            Dim source =
    <compilation>
        <file name="a.vb">
Option Infer On

Class C1
  public shared function foo() as Integer()
    return new Integer() {1,2,3}
  end function
End Class

Module M
    Public Sub Main()

        Dim unassignedRef1, unassignedRef2, unassignedRef3, unassignedRef4 as C1

        For Each unassignedRef1.foo()(0) In unassignedRef2.foo()

            if unassignedRef3 is nothing then
            End if
        Next unassignedRef1.foo()(unassignedRef4.foo()(0))

        For each unassignedRef5 as C1 in New C1() {unassignedRef5}
        Next 

        For each unassignedRef5 as C1 in New C1() {unassignedRef5}
        Next 

        For i As Object = 0 To i        
        next

        For i As Object = 0 To i        
        next
    End Sub
End Module
    </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC42024: Unused local variable: 'unassignedRef4'.
        Dim unassignedRef1, unassignedRef2, unassignedRef3, unassignedRef4 as C1
                                                            ~~~~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        For Each unassignedRef1.foo()(0) In unassignedRef2.foo()
                 ~~~~~~~~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        For Each unassignedRef1.foo()(0) In unassignedRef2.foo()
                                            ~~~~~~~~~~~~~~~~~~
BC42104: Variable 'unassignedRef3' is used before it has been assigned a value. A null reference exception could result at runtime.
            if unassignedRef3 is nothing then
               ~~~~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        Next unassignedRef1.foo()(unassignedRef4.foo()(0))
             ~~~~~~~~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        Next unassignedRef1.foo()(unassignedRef4.foo()(0))
                                  ~~~~~~~~~~~~~~~~~~
BC42104: Variable 'unassignedRef5' is used before it has been assigned a value. A null reference exception could result at runtime.
        For each unassignedRef5 as C1 in New C1() {unassignedRef5}
                                                   ~~~~~~~~~~~~~~
BC42104: Variable 'unassignedRef5' is used before it has been assigned a value. A null reference exception could result at runtime.
        For each unassignedRef5 as C1 in New C1() {unassignedRef5}
                                                   ~~~~~~~~~~~~~~
BC42104: Variable 'i' is used before it has been assigned a value. A null reference exception could result at runtime.
        For i As Object = 0 To i        
                               ~
BC42104: Variable 'i' is used before it has been assigned a value. A null reference exception could result at runtime.
        For i As Object = 0 To i        
                               ~
</expected>)
        End Sub

        <Fact()>
        Public Sub UninitializedReferences_3()
            Dim source =
    <compilation>
        <file name="a.vb">
Option Infer On

Imports System

Module M
    Public Sub Main()

        Dim x As Action
        For Each x In New Action() {Sub() Console.WriteLine("hello")}
            x.Invoke()
        Next
        x.Invoke()
    End Sub
End Module
    </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC42104: Variable 'x' is used before it has been assigned a value. A null reference exception could result at runtime.
        x.Invoke()
        ~
</expected>)
        End Sub

        <Fact()>
        Public Sub UsedLocals()
            Dim source =
    <compilation>
        <file name="a.vb">
Option Infer On

Class C1
  public function foo() as Integer()
    return new Integer() {1,2,3}
  end function
End Class

Module M
    Public Sub Main()

        Dim used1, used2 as integer

        For Each used1 In new C1().foo()
            used2 = 23            
        Next used1
    End Sub
End Module
    </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <Fact()>
        Public Sub LocalDeclaredInVariableLiftedCorrectly()
            Dim source =
    <compilation>
        <file name="a.vb">
Option Infer On
Imports System
Module M
    Public Sub Main()

        Dim del as Action = nothing
        For Each a In New Integer() {1, 2}
            For Each b In New Integer() {3, 4}
                Dim x as Integer = x + a + b
                if a = 1 andalso b = 3 then
                    del = Sub() call Console.WriteLine(x) 
                end if
            Next b, a

        del.Invoke()
    End Sub
End Module
    </file>
    </compilation>

            CompileAndVerify(source, expectedOutput:=<![CDATA[
4
]]>)
        End Sub

        <Fact()>
        Public Sub ControlVariableIsArrayAndNextVariableHasNoIndex()
            Dim source =
    <compilation>
        <file name="a.vb">
        Imports System

        Module Program
            Sub Main(args As String())

                For Each x() As Integer In New Integer()() {}
                Next x()  ' not ok 1

                For Each x() As Integer In New Integer()() {}
                Next x    ' ok 1

                For Each x() As Integer In New Integer()() {}
                Next x(1) ' ok 2

                For Each x As Integer() In New Integer()() {}
                Next x()  ' not ok 2

                For Each x As Integer() In New Integer()() {}
                Next x    ' ok 4

                For Each x As Integer() In New Integer()() {}
                Next x(1) ' ok 5
            End Sub
        End Module 
    </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC30105: Number of indices is less than the number of dimensions of the indexed array.
                Next x()  ' not ok 1
                      ~~
BC30105: Number of indices is less than the number of dimensions of the indexed array.
                Next x()  ' not ok 2
                      ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub CollectionNeedsLowering()
            Dim source =
    <compilation>
        <file name="a.vb">
        Imports System
        Imports system.Linq

        Module Program
            Sub Main(args As String())
                for each v as Object in From S in new integer() {1, 2, 3} Select S
                    Console.WriteLine(v)
                next
            End Sub
        End Module 
    </file>
    </compilation>

            CompileAndVerify(source, expectedOutput:=<![CDATA[
1
2
3
]]>, additionalRefs:={SystemCoreRef})
        End Sub

        <Fact()>
        Public Sub TraversingNothingStrictOn()
            Dim source =
<compilation name="TraversingNothingStrictOn">
    <file name="a.vb">
Option Strict On
Option Infer On

Class C
    Shared Sub Main()
        For Each item In Nothing
        Next
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC32023: Expression is of type 'Object', which is not a collection type.
        For Each item In Nothing
                         ~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub TraversingNothingStrictOff()
            Dim source =
<compilation name="TraversingNothingStrictOff">
    <file name="a.vb">
Option Strict Off
Option Infer On

Class C
    Shared Sub Main()
        For Each item In Nothing
        Next
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertNoErrors(compilation)
        End Sub

        ''' Bug 9266
        <Fact()>
        Public Sub Bug9266()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections
Imports System.Runtime.CompilerServices

Module M
    Sub Main
        For Each x As Integer In 1
        Next
    End Sub

    &lt;Extension()&gt;
    Function GetEnumerator(x As Integer) As E
        Return New E
    End Function
End Module

Class E
    Function MoveNext() As Boolean
        Return False
    End Function

    ReadOnly Property Current(ParamArray x() As Integer) As Integer
        Get
            Return 0
        End Get
    End Property
End Class

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace

    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        ''' Bug 9268
        <Fact()>
        Public Sub Bug9268()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections
Imports System.Runtime.CompilerServices

Module M
    Sub Main
        For Each x As Integer In 1
        Next
    End Sub

    &lt;Extension&gt;
    Function GetEnumerator(x As Integer) As E(Of Boolean)
        Return New E(Of Boolean)
    End Function
End Module

Class E(Of T)
    Function MoveNext As T
        Return Nothing
    End Function

    Property Current As Integer
End Class

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace

    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
 <expected>
BC32023: Expression is of type 'Integer', which is not a collection type.
        For Each x As Integer In 1
                                 ~
</expected>)
        End Sub

        ''' Bug 9267
        <Fact()>
        Public Sub Bug9267()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections
Imports System.Runtime.CompilerServices

Module M
    Sub Main
        For Each x As Integer In 1
        Next
    End Sub
    &lt;Extension&gt;
    Function GetEnumerator(x As Integer) As E
        Return New E
    End Function
End Module

Class E
    Function MoveNext As Boolean
        Return True
    End Function
    Property Current As Integer
        Protected Get
            Return 0
        End Get
        Set(ByVal value As Integer)
        End Set
    End Property
End Class

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace

    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC31103: 'Get' accessor of property 'Current' is not accessible.
        For Each x As Integer In 1
                                 ~
</expected>)
        End Sub

        ''' Bug 9241
        <Fact()>
        Public Sub Bug9241()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System.Runtime.CompilerServices
Imports System.Collections.Generic

Module M
    Sub Main
        For Each x As Integer In 1
        Next
    End Sub

    &lt;Extension&gt;
    Function GetEnumerator(x As Integer, ParamArray y As Integer()) As List(Of Integer).Enumerator
        Return Nothing
    End Function

    &lt;Extension&gt;
    Function GetEnumerator(x As Integer) As Integer
        Return Nothing
    End Function
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC32023: Expression is of type 'Integer', which is not a collection type.
        For Each x As Integer In 1
                                 ~
</expected>)
        End Sub

        ''' Bug 9238
        <Fact()>
        Public Sub Bug9238()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System.Runtime.CompilerServices
Imports System.Collections.Generic

Module M
    Sub Main
        For Each x As Integer In 1
        Next
    End Sub
End Module

Module X
    &lt;Extension&gt;
    Function GetEnumerator(x As Integer) As List(Of Integer).Enumerator
        return nothing
    End Function
End Module

Module Y
    &lt;Extension&gt;
    Function GetEnumerator(x As Integer) As List(Of Integer).Enumerator
        return nothing    
    End Function
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC32023: Expression is of type 'Integer', which is not a collection type.
        For Each x As Integer In 1
                                 ~
</expected>)
        End Sub

        ''' Bug 9238
        <Fact()>
        Public Sub DontShowInvocationBindingErrorsIfNoDesignPatternMatch()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections
Imports System.Runtime.CompilerServices

Public Interface IBetterEnumerable
    Inherits IEnumerable
End Interface

Public Class SomethingEnumerable
    Implements IEnumerable

    Public Function GetEnumerator2() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
        Return Nothing
    End Function
End Class


Module X
    &lt;Extension()&gt;
    Function GetEnumerator(x As SomethingEnumerable) As IEnumerator
        Return nothing
    End Function
End Module

Module Y
    &lt;Extension()&gt;
    Function GetEnumerator(x As SomethingEnumerable) As IEnumerator
        Return nothing
    End Function
End Module

Class C1
    Public Shared Sub Main()
        Dim myCollection1 As IEnumerable = nothing
        For Each element as IEnumerable In myCollection1
            Console.WriteLine("foo")
        Next

        Dim myCollection2 As IBetterEnumerable = nothing
        For Each element as IBetterEnumerable In myCollection2
            Console.WriteLine("foo")
        Next

        Dim myCollection3 As SomethingEnumerable = nothing
        For Each element as SomethingEnumerable In myCollection3
            Console.WriteLine("foo")
        Next
    End Sub
End Class 

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <Fact()>
        Public Sub InvalidProperties()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections

Class Custom
    Public Function GetEnumerator() As CustomEnumerator
        Return Nothing
    End Function

    Public Class CustomEnumerator
        Public Function MoveNext() As Boolean
            Return False
        End Function

        Public ReadOnly Property Current As Custom
            Get
                Return Nothing
            End Get
        End Property

        Public ReadOnly Property Current As Integer
            Get
                Return Nothing
            End Get
        End Property

    End Class
End Class

Class C1
    Public Shared Sub Main()
        Dim myCustomCollection As Custom = nothing

        For Each element as Custom In myCustomCollection
            Console.WriteLine("foo")
        Next
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC30301: 'Public ReadOnly Property Current As Custom' and 'Public ReadOnly Property Current As Integer' cannot overload each other because they differ only by return types.
        Public ReadOnly Property Current As Custom
                                 ~~~~~~~
BC30521: Overload resolution failed because no accessible 'Current' is most specific for these arguments:
    'Public ReadOnly Property Current As Custom': Not most specific.
    'Public ReadOnly Property Current As Integer': Not most specific.
        For Each element as Custom In myCustomCollection
                                      ~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub CurrentDiffersInOptionalAndParamArrayOnly()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections

Class Custom
    Public Function GetEnumerator() As CustomEnumerator
        Return Nothing
    End Function

    Public Class CustomEnumerator
        Public Function MoveNext() As Boolean
            Return False
        End Function

        Public ReadOnly Property Current(optional f as integer = 23) As Custom
            Get
                Return Nothing
            End Get
        End Property

        Public ReadOnly Property Current(paramarray p() as Integer) As Custom
            Get
                Return Nothing
            End Get
        End Property

    End Class
End Class

Class C1
    Public Shared Sub Main()
        Dim myCustomCollection As Custom = nothing

        For Each element as Custom In myCustomCollection
            Console.WriteLine("foo")
        Next
    End Sub
End Class    
    </file>
</compilation>, options:=TestOptions.ReleaseExe).VerifyIL("C1.Main", <![CDATA[
{
  // Code size       37 (0x25)
  .maxstack  2
  .locals init (Custom.CustomEnumerator V_0)
  IL_0000:  ldnull
  IL_0001:  callvirt   "Function Custom.GetEnumerator() As Custom.CustomEnumerator"
  IL_0006:  stloc.0
  IL_0007:  br.s       IL_001c
  IL_0009:  ldloc.0
  IL_000a:  ldc.i4.s   23
  IL_000c:  callvirt   "Function Custom.CustomEnumerator.get_Current(Integer) As Custom"
  IL_0011:  pop
  IL_0012:  ldstr      "foo"
  IL_0017:  call       "Sub System.Console.WriteLine(String)"
  IL_001c:  ldloc.0
  IL_001d:  callvirt   "Function Custom.CustomEnumerator.MoveNext() As Boolean"
  IL_0022:  brtrue.s   IL_0009
  IL_0024:  ret
}
]]>)

            ' NOTE: Dev10 always picked the version with the optional parameter, not the param array.
        End Sub

        ''' Bug 9250
        <Fact()>
        Public Sub Bug9250_InferOn()
            Dim source =
<compilation>
    <file name="a.vb">
Option Infer On

Module Program
    Sub Main()
    End Sub

    Sub Foo(Of T, S, R)
        For Each t In ""
        Next

        For t = 1 to 2
        Next

        For Each r as char In ""
        Next

        Dim s = DirectCast(nothing, S)

    End Sub
End Module

    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC32089: 'r' is already declared as a type parameter of this method.
        For Each r as char In ""
                 ~
BC32089: 's' is already declared as a type parameter of this method.
        Dim s = DirectCast(nothing, S)
            ~
</expected>)
        End Sub

        <Fact()>
        Public Sub Bug9250_ExplicitOff()
            Dim source =
<compilation>
    <file name="a.vb">
Option Explicit Off
Option Infer On

Module Program
    Sub Main()
    End Sub

    Sub Foo(Of T, S, R)
        For Each t In ""
        Next

        For t = 1 to 2
        Next

        For Each r as char In ""
        Next

        s = DirectCast(nothing, S)

    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC32089: 'r' is already declared as a type parameter of this method.
        For Each r as char In ""
                 ~
BC30108: 'S' is a type and cannot be used as an expression.
        s = DirectCast(nothing, S)
        ~
</expected>)
        End Sub

        <WorkItem(529048, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529048")>
        <Fact()>
        Public Sub ForeachOutOfMethod()
            Dim source =
<compilation>
    <file name="a.vb">
For Each i In ""
Next
    </file>
</compilation>

            CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source).VerifyDiagnostics(Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "For Each i In """""),
            Diagnostic(ERRID.ERR_NextNoMatchingFor, "Next"))
        End Sub

        <WorkItem(529048, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529048")>
        <Fact()>
        Public Sub ForeachOutOfMethod_1()
            Dim source =
<compilation>
    <file name="a.vb">
Class c1        
    For Each i In ""
    Next
End Class
    </file>
</compilation>

            CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source).VerifyDiagnostics(Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "For Each i In """""),
            Diagnostic(ERRID.ERR_NextNoMatchingFor, "Next"))
        End Sub

        <WorkItem(543709, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543709")>
        <Fact()>
        Public Sub Bug11622()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System : Imports System.Collections : Imports System.Collections.Generic
 
Class MyCollection1(Of T)
    Implements IEnumerable, System.Collections.Generic.IEnumerable(Of T)
    Dim list As New List(Of T)
 
    Public Function GetEnumerator2() As System.Collections.Generic.IEnumerator(Of T) Implements System.Collections.Generic.IEnumerable(Of T).GetEnumerator
        Console.WriteLine("MyCollection1 generic")
        Return list.GetEnumerator
    End Function
 
    Public Function GetEnumerator3() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
        Console.WriteLine("MyCollection1 non generic")
        Return list.GetEnumerator
    End Function
End Class

Class MyCollection2
    Implements IEnumerable, System.Collections.Generic.IEnumerable(Of Integer)
    Dim list As New List(Of Integer)
 
    Public Function GetEnumerator2() As System.Collections.Generic.IEnumerator(Of Integer) Implements System.Collections.Generic.IEnumerable(Of Integer).GetEnumerator
        Console.WriteLine("MyCollection2 generic")
        Return list.GetEnumerator
    End Function
 
    Public Function GetEnumerator3() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
        Console.WriteLine("MyCollection2 non generic")
        Return list.GetEnumerator
    End Function
End Class

 
Public Module Program
    Sub Main()
        For Each x In New MyCollection1(Of Integer)()
        Next

        For Each x In New MyCollection2()
        Next
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:=<![CDATA[
MyCollection1 generic
MyCollection2 generic
]]>)
        End Sub

        <WorkItem(529049, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529049")>
        <Fact>
        Public Sub ForLoopsOutOfMethod()
            Dim source =
<compilation>
    <file name="a.vb">
For i As Integer = 1 To 100
Next
    </file>
</compilation>

            CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source).VerifyDiagnostics(Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "For i As Integer = 1 To 100"),
    Diagnostic(ERRID.ERR_NextNoMatchingFor, "Next"))
        End Sub

        <WorkItem(529049, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529049")>
        <Fact>
        Public Sub ForLoopsOutOfMethod_1()
            Dim source =
<compilation>
    <file name="a.vb">
Class c1        
    For i As Integer = 1 To 100
    Next
End Class
    </file>
</compilation>

            CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source).VerifyDiagnostics(Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "For i As Integer = 1 To 100"),
    Diagnostic(ERRID.ERR_NextNoMatchingFor, "Next"))
        End Sub

        <WorkItem(543842, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543842")>
        <Fact()>
        Public Sub ArraysAndUseSiteErrors()
            Dim source =
<compilation name="ArraysAndUseSiteErrors">
    <file name="a.vb">
        Public Class Program
            Public Shared Sub Main()
                Dim xs as Integer()

                for each x in xs
                next
            End Sub
        End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithReferences(source, ImmutableArray.Create(Of MetadataReference)().AsEnumerable)

            compilation.AssertTheseDiagnostics(
            <expected>
BC30002: Type 'System.Void' is not defined.
Public Class Program
~~~~~~~~~~~~~~~~~~~~~
BC31091: Import of type 'Object' from assembly or module 'ArraysAndUseSiteErrors.dll' failed.
Public Class Program
             ~~~~~~~
BC30002: Type 'System.Void' is not defined.
            Public Shared Sub Main()
            ~~~~~~~~~~~~~~~~~~~~~~~~~
BC30002: Type 'System.Int32' is not defined.
                Dim xs as Integer()
                          ~~~~~~~
BC30002: Type 'System.Object' is not defined.
                for each x in xs
                         ~
BC31091: Import of type 'Array' from assembly or module 'ArraysAndUseSiteErrors.dll' failed.
                for each x in xs
                              ~~
BC42104: Variable 'xs' is used before it has been assigned a value. A null reference exception could result at runtime.
                for each x in xs
                              ~~
            </expected>
            )
        End Sub

        <WorkItem(543842, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543842")>
        <Fact()>
        Public Sub StringsAndUseSiteErrors()
            Dim source =
<compilation name="StringsAndUseSiteErrors">
    <file name="a.vb">
        Public Class Program
            Public Shared Sub Main()
                Dim xs as String

                for each x in xs
                next
            End Sub
        End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithReferences(source, ImmutableArray.Create(Of MetadataReference)().AsEnumerable)

            compilation.AssertTheseDiagnostics(
            <expected>
BC30002: Type 'System.Void' is not defined.
Public Class Program
~~~~~~~~~~~~~~~~~~~~~
BC31091: Import of type 'Object' from assembly or module 'StringsAndUseSiteErrors.dll' failed.
Public Class Program
             ~~~~~~~
BC30002: Type 'System.Void' is not defined.
            Public Shared Sub Main()
            ~~~~~~~~~~~~~~~~~~~~~~~~~
BC30002: Type 'System.String' is not defined.
                Dim xs as String
                          ~~~~~~
BC30002: Type 'System.Object' is not defined.
                for each x in xs
                         ~
BC32023: Expression is of type 'String', which is not a collection type.
                for each x in xs
                              ~~
BC42104: Variable 'xs' is used before it has been assigned a value. A null reference exception could result at runtime.
                for each x in xs
                              ~~                
            </expected>
            )
        End Sub

        <WorkItem(545519, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545519")>
        <Fact()>
        Public Sub NewForEachScopeDev12()
            Dim source =
<compilation>
    <file name="a.vb">
imports system
imports system.collections.generic

Module m1
    Sub Main()
        Dim actions = New List(Of Action)()
        Dim values = New List(Of Integer) From {1, 2, 3}

        For Each i As Integer In values
            actions.Add(Sub() Console.WriteLine(i))
        Next

        For Each a In actions
            a()
        Next
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:=<![CDATA[
1
2
3
]]>)
        End Sub

        <WorkItem(545519, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545519")>
        <Fact()>
        Public Sub OriginalDev11Test()
            Dim source =
<compilation>
    <file name="a.vb">
Imports system.collections.generic
Imports system.threading.Tasks
Imports System

module module1

    sub main()
        LambdaTest()
        HeaderTest()
        HeaderWithParameterTest()
        SpecTest()
        ForTest()
    end sub

    sub LambdaTest()
        console.writeline("====LambdaTest====")
        Dim x(10) as action
    
        ' Test Array
        console.writeline("Array Test")
        For Each i In {1, 2, 3}
            x(i) = Sub() console.writeline(i.toString)
            'console.writeline(i.toString)
        Next
        for i = 1 to 3 
            x(i).invoke()
        next

       console.writeline("String Test")
        dim j = 1

        for each i in "123"
            x(j) = sub() console.writeline(i.toString)
            j += 1
        next

        for i = 1 to 3 
            x(i).invoke()
        next

        console.writeline("Collection Test")
        for each i in foo()
            x(i) = sub() console.writeline(i.toString)
        next
        for i = 1 to 3 
            x(i).invoke()
        next

        console.writeline("NonLocal Test")
        dim jj as integer
        for each jj in foo()
            x(jj) = sub() console.writeline(jj.toString)
        next
        for i = 1 to 3 
            x(i).invoke()
        next
        
    end sub
  
    function foo() as IEnumerable(of Integer)
        return new list(of integer) from {1, 2, 3}
    end function

    function foo2(kk as integer ) as IEnumerable(of Integer)
        return new list(of integer) from {1, 2, 3}
    end function

    sub HeaderTest()
        console.writeline("====HeaderTest====")
        Dim x(10) as action
    
        ' Test Array
        console.writeline("Header Test: Array Test")
        For Each i As Integer In (function() {i + 1, i + 2, i + 3})()
            x(i) = Sub() console.writeline(i.toString)
        Next
        for i = 1 to 3 
            x(i).invoke()
        next

       console.writeline("Header Test: String Test")
        dim j = 1
        for each i in (function() "123")()
            x(j) = sub() console.writeline(i.toString)
            j += 1
        next
        for i = 1 to 3 
            x(i).invoke()
        next

        console.writeline("Header Test: Collection Test")
        for each i in (function() foo())()
            x(i) = sub() console.writeline(i.toString)
        next
        for i = 1 to 3 
            x(i).invoke()
        next

        console.writeline("Header Test: NonLocal Test")
        dim jj as integer
        for each jj in (function() foo())()
            x(jj) = sub() console.writeline(jj.toString)
        next
        for i = 1 to 3 
            x(i).invoke()
        next
        
    end sub

    sub HeaderWithParameterTest()
        console.writeline("====HeaderWithParameterTest====")
        Dim x(10) as action
    
        ' Test Array
        console.writeline("Header Test: Array Test")
        For Each i As Integer In (function(a) {a + 1, a + 2, a + 3})(i)
            x(i) = Sub() console.writeline(i.toString)
        Next
        for i = 1 to 3 
            x(i).invoke()
        next

       console.writeline("Header Test: String Test")
        dim j = 1
        for each i as Char in (function(a) "123")(i)
            x(j) = sub() console.writeline(i.toString)
            j += 1
        next
        for i = 1 to 3 
            x(i).invoke()
        next

        console.writeline("Header Test: Collection Test")
        for each i as integer in (function(a) foo())(i)
            x(i) = sub() console.writeline(i.toString)
        next
        for i = 1 to 3 
            x(i).invoke()
        next

        console.writeline("Header Test: NonLocal Test")
        dim jj as integer
        for each jj in (function(a) foo())(jj)
            x(jj) = sub() console.writeline(jj.toString)
        next
        for i = 1 to 3 
            x(i).invoke()
        next

        console.writeline("Header Test: NonLocal Test2")
        dim kk as integer
        for each kk in (function(a) foo2(kk))(kk)
            x(kk) = sub() console.writeline(kk.toString)
        next

        for i = 1 to 3 
            x(i).invoke()
        next
        
    end sub

    Sub SpecTest()
        console.writeline("====SpecTest====")

        Dim lambdas As New List(Of Action)
        
        'Expected 1,2,3
        For y = 1 To 3
            Dim x As Integer
            lambdas.Add(Sub() Console.Write(x.ToString + ","))
            x += 1
        Next

        For Each lambda In lambdas
            lambda()
        Next
        Console.Writeline()
        Console.Writeline()

        ' Expected 1,2,3,  1,2,3,  1,2,15
        lambdas.clear
        Dim reset As action = Nothing
        For y = 1 To 3
            For Each x In {1, 2, 3}
                lambdas.Add(Sub() Console.Write(x.ToString + ","))
                reset = Sub() x = 15
            Next
            lambdas.Add(Sub() Console.WriteLine())
        Next

        reset()
        For Each lambda In lambdas
            lambda()
        Next
        Console.Writeline()

        'Expected 0,1,2,  0,1,2,  0,1,2
        lambdas.clear
        For y = 1 To 3
            For Each x As Integer In {x, 1, 2}
                Console.Write(x.TOString + ",")
            Next
            Console.WriteLine()
        Next
        For Each lambda In lambdas
            lambda()
        Next
        Console.Writeline()

        'Expected 0,1,2, 0,1,2, 0,1,2
        lambdas.clear
        For y = 1 To 3
            For Each x As Integer In {x, 1, 2}
                lambdas.add(Sub() Console.Write(x.ToString + ","))
            Next
            lambdas.add(sub() Console.WriteLine())
        Next
        For Each lambda In lambdas
            lambda()
        Next
        Console.Writeline()

        'Expected 0,1,2, 0,1,2, 0,1,2
        lambdas.clear    
        For y = 1 To 3
            For Each x As Integer In (function(a)
                                         x = x + 1 
                                        return {a, x, 2}
                                      end function)(x)
                lambdas.add( Sub() Console.Write(x.ToString + "," ) )
            Next
            lambdas.add(sub() Console.WriteLine())
        Next
        For Each lambda In lambdas
           lambda()
        Next
    end sub
    
    Sub ForTest()
        console.writeline("====ForTest====")
        Dim x(10) as action
        For i = 1 to 3
            x(i) = Sub() console.writeline(i.toString)
        Next
        for i = 1 to 3 
            x(i).invoke()
        next
    end sub 

End Module
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:=<![CDATA[
====LambdaTest====
Array Test
1
2
3
String Test
1
2
3
Collection Test
1
2
3
NonLocal Test
3
3
3
====HeaderTest====
Header Test: Array Test
1
2
3
Header Test: String Test
1
2
3
Header Test: Collection Test
1
2
3
Header Test: NonLocal Test
3
3
3
====HeaderWithParameterTest====
Header Test: Array Test
1
2
3
Header Test: String Test
1
2
3
Header Test: Collection Test
1
2
3
Header Test: NonLocal Test
3
3
3
Header Test: NonLocal Test2
3
3
3
====SpecTest====
1,2,3,

1,2,3,
1,2,3,
1,2,15,

0,1,2,
0,1,2,
0,1,2,

0,1,2,
0,1,2,
0,1,2,

0,1,2,
0,1,2,
0,1,2,
====ForTest====
4
4
4
]]>)
        End Sub

        <Fact()>
        Public Sub ForEachLateBinding()
            Dim source =
    <compilation>
        <file name="a.vb">
        Option Strict Off
imports system
Class C
    Shared Sub Main()
        Dim o As Object = {1, 2, 3}
        For Each x In o
            console.writeline(x)
        Next
    End Sub
End Class
    </file>
    </compilation>

            CompileAndVerify(source, expectedOutput:=<![CDATA[
1
2
3
]]>)
        End Sub

        <Fact()>
        Public Sub ForEachLateWithPatternFails()
            Dim source =
    <compilation>
        <file name="a.vb">
Option Strict Off

Imports System
Imports System.Collections.Generic

Class Custom
    dim internal = new List(of integer)() from {1,2,3}

    Public Function GetEnumerator() As IEnumerator(of Integer)
        Return internal.getEnumerator()
    End Function
End Class

Class C1
    Public Shared Sub Main()
        For Each element In new Custom()
            Console.WriteLine(element)
        Next
        
try
        For Each element In CType(new Custom(), Object)
            Console.WriteLine(element)
        Next
catch e as InvalidCastException
    Console.WriteLine("Too bad the pattern does not work with late binding ...")
end try
    End Sub
End Class        

    </file>
    </compilation>

            CompileAndVerify(source, expectedOutput:=<![CDATA[
1
2
3
Too bad the pattern does not work with late binding ...
]]>)
        End Sub

        <WorkItem(847507, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/847507")>
        <Fact>
        Public Sub InferIterationVariableTypeWithErrors()
            Dim source =
    <compilation>
        <file name="a.vb">
Option Infer On

Class Program
    Shared Sub Main()
        For Each x In New String() {}
            x.ToString()
        Next
    End Sub
End Class

Namespace System
    Public Class Object
    End Class

    Public Class String : Inherits Object
    End Class
End Namespace
        </file>
    </compilation>

            Dim comp = CreateCompilationWithoutReferences(source) ' Lots of errors, since corlib is missing.
            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)

            Dim foreachSyntax = tree.GetRoot().DescendantNodes().OfType(Of ForEachStatementSyntax)().Single()
            Dim variableSyntax = foreachSyntax.ControlVariable
            Dim invocationSyntax = tree.GetRoot().DescendantNodes().OfType(Of InvocationExpressionSyntax)().Single()

            ' Get local symbol without binding foreach loop.
            Dim localSymbol = CType(model.LookupSymbols(invocationSyntax.Position, name:="x").Single(), LocalSymbol)

            ' Code Path 1: LocalSymbol.Type
            Dim localSymbolType = localSymbol.Type
            Assert.NotEqual(TypeKind.Error, localSymbolType.TypeKind)

            ' Code Path 2: SemanticModel
            Dim info = model.GetTypeInfo(variableSyntax)
            Assert.Equal(localSymbolType, info.Type)
        End Sub

    End Class
End Namespace
