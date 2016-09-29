' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
Imports System.Text
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols.Metadata
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Partial Public Class TypeBindingTests
        Inherits BasicTestBase

        <Fact>
        Public Sub BC30371()  ' ModuleAsType1
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Compilation">
    <file name="a.vb">
    Module m1
        Class Cls1
        End Class

        Dim x0 As m1.Cls1     ' no errors
        Dim x1 As m1     ' error here

        Sub Foo(x As m1, y as m1.Cls1)   ' one error here
            Dim v0 As m1.Cls1     ' no errors
            Dim v1 As m1     ' error here           
        End Sub

        sub Bar()
            dim x as new m1
            dim y = new m1
            dim z = new m1(12)
        End Sub
    End Module 
    </file>
</compilation>)

            Dim expectedErrors = <errors>
BC30371: Module 'm1' cannot be used as a type.
        Dim x1 As m1     ' error here
                  ~~
BC30371: Module 'm1' cannot be used as a type.
        Sub Foo(x As m1, y as m1.Cls1)   ' one error here
                     ~~

                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)

            expectedErrors = <errors>
BC30371: Module 'm1' cannot be used as a type.
        Dim x1 As m1     ' error here
                  ~~
BC30371: Module 'm1' cannot be used as a type.
        Sub Foo(x As m1, y as m1.Cls1)   ' one error here
                     ~~
BC42024: Unused local variable: 'v0'.
            Dim v0 As m1.Cls1     ' no errors
                ~~
BC42024: Unused local variable: 'v1'.
            Dim v1 As m1     ' error here           
                ~~
BC30371: Module 'm1' cannot be used as a type.
            Dim v1 As m1     ' error here           
                      ~~
BC30371: Module 'm1' cannot be used as a type.
            dim x as new m1
                         ~~
BC30371: Module 'm1' cannot be used as a type.
            dim y = new m1
                        ~~
BC30371: Module 'm1' cannot be used as a type.
            dim z = new m1(12)
                        ~~
                                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub BC31422()  ' BadUseOfVoid
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Compilation">
    <file name="a.vb">
Imports System        
Imports System.Void ' no error here
Imports Void1 = System.Void ' no error here
Module m1

    Class Cls1
    End Class

    Dim x1 As System.Void     ' error here

    Sub Foo(x As Void)   ' error here
        Dim v1 As Void     ' error here           
        Dim v2 As Void1     ' error here           
    End Sub
End Module
    </file>
</compilation>)

            Dim expectedErrors = <errors>
BC31422: 'System.Void' can only be used in a GetType expression.
    Dim x1 As System.Void     ' error here
              ~~~~~~~~~~~
BC31422: 'System.Void' can only be used in a GetType expression.
    Sub Foo(x As Void)   ' error here
                 ~~~~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)

            expectedErrors = <errors>
BC31422: 'System.Void' can only be used in a GetType expression.
    Dim x1 As System.Void     ' error here
              ~~~~~~~~~~~
BC31422: 'System.Void' can only be used in a GetType expression.
    Sub Foo(x As Void)   ' error here
                 ~~~~
BC42024: Unused local variable: 'v1'.
        Dim v1 As Void     ' error here           
            ~~
BC31422: 'System.Void' can only be used in a GetType expression.
        Dim v1 As Void     ' error here           
                  ~~~~
BC42024: Unused local variable: 'v2'.
        Dim v2 As Void1     ' error here           
            ~~
BC31422: 'System.Void' can only be used in a GetType expression.
        Dim v2 As Void1     ' error here           
                  ~~~~~
                             </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors)


        End Sub

        <WorkItem(538814, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538814")>
        <Fact>
        Public Sub DuplicateInterfaceInheritance()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
               <compilation name="C">
                   <file name="a.vb">
Interface IA(OF T)
End Interface
 
Interface IB
  Inherits IA(Of String), IA(Of String)
End Interface
                    </file>
               </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30584: 'IA(Of String)' cannot be inherited more than once.
  Inherits IA(Of String), IA(Of String)
                          ~~~~~~~~~~~~~
</expected>)
        End Sub

        <WorkItem(543788, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543788")>
        <Fact()>
        Public Sub BC30294_GenericStructureContainingInstanceOfItself()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                <compilation>
                    <file name="a.vb">
Module GenStrErr100mod
    Structure gStr1(Of U)
	' COMPILEERROR: BC30294, "gStr1(Of genStructure1(Of U))"
        Dim v4 As gStr1(Of genStructure1(Of U))
    End Structure

    public Structure genStructure1 (Of ST)
    End Structure
    
    Sub GenStrErr100()
		Dim s As gStr1 (Of Long)
    End Sub
End Module
                    </file>
                </compilation>)
            compilation.AssertTheseDiagnostics(<expected>
BC30294: Structure 'gStr1' cannot contain an instance of itself: 
    'GenStrErr100mod.gStr1(Of U)' contains 'GenStrErr100mod.gStr1(Of GenStrErr100mod.genStructure1(Of U))' (variable 'v4').
        Dim v4 As gStr1(Of genStructure1(Of U))
            ~~
BC42024: Unused local variable: 's'.
		Dim s As gStr1 (Of Long)
      ~
</expected>)
        End Sub

        <WorkItem(543909, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543909")>
        <Fact()>
        Public Sub BC30294_GenericStructureContainingInstanceOfItself_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
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
                </compilation>)
            compilation.AssertTheseDiagnostics(
<expected>
BC30294: Structure 's2' cannot contain an instance of itself: 
    's2' contains 'List(Of s2).Enumerator' (variable 'enumerator').
    'List(Of s2).Enumerator' contains 's2' (variable 'current').
        Dim enumerator As Collections.Generic.List(Of s2).Enumerator
            ~~~~~~~~~~
</expected>)
        End Sub

        <Fact, WorkItem(607394, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/607394")>
        Public Sub Bug607394()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb">
Class C
    Dim Const x As Object = 1
    Const Dim y As Object = 1

    Private Const z As Object = 1
    Private Dim x1 As Object 
    Dim Private y1 As Object

    Friend Dim x2 As Object
    Dim Friend y2 As Object

    Sub Main()
        Dim Const x3 As Object = 1
        Const Dim y3 As Object = 1
    End Sub

End Class    </file>
</compilation>, TestOptions.ReleaseDll)

            Dim expectedErrors = <errors>
BC30233: 'Dim' is not valid on a constant declaration.
    Dim Const x As Object = 1
    ~~~
BC30233: 'Dim' is not valid on a constant declaration.
    Const Dim y As Object = 1
          ~~~
                                 </errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)

            expectedErrors = <errors>
BC30233: 'Dim' is not valid on a constant declaration.
    Dim Const x As Object = 1
    ~~~
BC30233: 'Dim' is not valid on a constant declaration.
    Const Dim y As Object = 1
          ~~~
BC30246: 'Dim' is not valid on a local constant declaration.
        Dim Const x3 As Object = 1
        ~~~
BC42099: Unused local constant: 'x3'.
        Dim Const x3 As Object = 1
                  ~~
BC30246: 'Dim' is not valid on a local constant declaration.
        Const Dim y3 As Object = 1
              ~~~
BC42099: Unused local constant: 'y3'.
        Const Dim y3 As Object = 1
                  ~~
                             </errors>

            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors)
        End Sub

    End Class

End Namespace
