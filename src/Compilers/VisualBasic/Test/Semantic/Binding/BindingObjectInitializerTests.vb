' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    Public Class BindingMemberInitializerTests
        Inherits BasicTestBase

        <Fact()>
        Public Sub SimpleObjectInitialization()
            Dim source =
<compilation name="SimpleObjectInitialization">
    <file name="a.vb">
Option Strict On

Imports System

Class C2
    Public Field as String
End Class

Class C1
    Public Shared Sub Main()
        Dim c as C2 = New C2() With {.Field = "Hello World!"}        
        Console.WriteLine(c.Field)
    End Sub
End Class        
    </file>
</compilation>

            CompileAndVerify(source, <![CDATA[
Hello World!
]]>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializationWithFieldOnRight()
            Dim source =
<compilation name="ObjectInitializationWithFieldOnRight">
    <file name="a.vb">
Option Strict On

Imports System

Class C2
    Public Field as String
    Public HelloWorld as String = "Hello World!"
End Class

Class C1
    Public Shared Sub Main()
        Dim c as C2 = New C2() With {.Field = .HelloWorld}        
        Console.WriteLine(c.Field)
    End Sub
End Class        
    </file>
</compilation>

            CompileAndVerify(source, <![CDATA[
Hello World!
]]>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerEmptyInitializers()
            Dim source =
<compilation name="ObjectInitializerEmptyInitializers">
    <file name="a.vb">
Option Strict On

Imports System

Class C2
End Class

Class C1
    Public Shared Sub Main()
        Dim c as C2 = New C2() With {}
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC30996: Initializer expected.
        Dim c as C2 = New C2() With {}
                                    ~                                               
                                           </expected>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerMissingIdentifierInInitializer()
            Dim source =
<compilation name="ObjectInitializerMissingIdentifierInInitializer">
    <file name="a.vb">
Option Strict On

Imports System

Public Class C2
    Public Field as Integer
End Class

Class C1
    Public Shared Sub Main()
        Dim c as C2 = New C2() With {. = Unknown(), . = Unknown()}
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC30203: Identifier expected.
        Dim c as C2 = New C2() With {. = Unknown(), . = Unknown()}
                                       ~
BC30451: 'Unknown' is not declared. It may be inaccessible due to its protection level.
        Dim c as C2 = New C2() With {. = Unknown(), . = Unknown()}
                                         ~~~~~~~
BC30203: Identifier expected.
        Dim c as C2 = New C2() With {. = Unknown(), . = Unknown()}
                                                      ~
BC30451: 'Unknown' is not declared. It may be inaccessible due to its protection level.
        Dim c as C2 = New C2() With {. = Unknown(), . = Unknown()}
                                                        ~~~~~~~                                            
                                           </expected>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerOnlyDotIdentifierInInitializer()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System

Public Class C2
    Public Field as Integer
End Class

Class C1
    Public Shared Sub Main()
        Dim c as C2 = New C2() With {.Field}
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC30201: Expression expected.
        Dim c as C2 = New C2() With {.Field}
                                           ~
BC30984: '=' expected (object initializer).
        Dim c as C2 = New C2() With {.Field}
                                           ~                                         
                                           </expected>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerMissingExpressionInInitializer()
            Dim source =
<compilation name="ObjectInitializerMissingExpressionInInitializer">
    <file name="a.vb">
Option Strict On

Imports System

Public Class C2
    Public Field as Integer
End Class

Class C1
    Public Shared Sub Main()
        Dim c as C2 = New C2() With {.Field=}
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC30201: Expression expected.
        Dim c as C2 = New C2() With {.Field=}
                                            ~
                                           </expected>)
        End Sub

        <WorkItem(529213, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529213")>
        <Fact()>
        Public Sub ObjectInitializerKeyKeywordInInitializer()
            Dim source =
<compilation name="ObjectInitializerKeyKeywordInInitializer">
    <file name="a.vb">
Option Strict On

Imports System

Class C2
    Public Field as Integer
End Class

Class C1
    Public Shared Sub Main()
        Dim c as C2 = New C2() With {Key .Field=23}
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC30985: Name of field or property being initialized in an object initializer must start with '.'.
        Dim c as C2 = New C2() With {Key .Field=23}
                                     ~
BC30451: 'Key' is not declared. It may be inaccessible due to its protection level.
        Dim c as C2 = New C2() With {Key .Field=23}
                                     ~~~
                                           </expected>)
        End Sub

        <WorkItem(544357, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544357")>
        <Fact()>
        Public Sub ObjectInitializerMultipleInitializations()
            Dim source =
<compilation name="ObjectInitializerMultipleInitializations">
    <file name="a.vb">
Option Strict On

Imports System

Class C2
    Public Field as String
End Class

Class C1
    Public Shared Sub Main()
        Dim c as C2 = New C2() With {.Field = "a", .Field="b"}
        Dim d as C2 = New C2() With {.field = "a", .FIELD="b"}
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC30989: Multiple initializations of 'Field'.  Fields and properties can be initialized only once in an object initializer expression.
        Dim c as C2 = New C2() With {.Field = "a", .Field="b"}
                                                    ~~~~~
BC30989: Multiple initializations of 'FIELD'.  Fields and properties can be initialized only once in an object initializer expression.
        Dim d as C2 = New C2() With {.field = "a", .FIELD="b"}
                                                    ~~~~~
                                           </expected>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerInitializingObject()
            Dim source =
<compilation name="ObjectInitializerInitializingObject">
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        Dim c as Object = new Object() With {.Field = "a"}
        Dim d as new Object() With {.Field = "b"}
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC30994: Object initializer syntax cannot be used to initialize an instance of 'System.Object'.
        Dim c as Object = new Object() With {.Field = "a"}
                                       ~~~~~~~~~~~~~~~~~~~
BC30994: Object initializer syntax cannot be used to initialize an instance of 'System.Object'.
        Dim d as new Object() With {.Field = "b"}
                              ~~~~~~~~~~~~~~~~~~~
                                           </expected>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerInitializingSameClass()
            Dim source =
<compilation name="ObjectInitializerInitializingSameClass">
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Field1 as String = "Hello World!"
    Public Field2 as String

    Public Shared Sub Main()
        Dim c as new C1()
        c.Field1 = "No greeting today"
        c.DoStuff()
    End Sub

    Public Sub DoStuff()
        Dim c1 as New C1() With {.Field2 = .Field1}
        Console.WriteLine(c1.Field2)
    End Sub
End Class        
    </file>
</compilation>

            CompileAndVerify(source, <![CDATA[
Hello World!
]]>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerInitializeSharedFieldOnNewInstance()
            Dim source =
<compilation name="ObjectInitializerInitializeSharedFieldOnNewInstance">
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Shared Field1 as String
    
    Public Shared Sub Main()
        Dim c1 as New C1() With {.Field1 = "Hello World!"}
    End Sub

End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC30991: Member 'Field1' cannot be initialized in an object initializer expression because it is shared.
        Dim c1 as New C1() With {.Field1 = "Hello World!"}
                                  ~~~~~~
                                               </expected>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerInitializeNonExistentField()
            Dim source =
<compilation name="ObjectInitializerInitializeNonExistentField">
    <file name="a.vb">
Option Strict On

Imports System

Class C1

    Public Shared Sub Main()
        Dim c1 as New C1() With {.Field1 = Bar(.Field1)}
    End Sub

End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC30456: 'Field1' is not a member of 'C1'.
        Dim c1 as New C1() With {.Field1 = Bar(.Field1)}
                                  ~~~~~~
BC30451: 'Bar' is not declared. It may be inaccessible due to its protection level.
        Dim c1 as New C1() With {.Field1 = Bar(.Field1)}
                                           ~~~
BC30456: 'Field1' is not a member of 'C1'.
        Dim c1 as New C1() With {.Field1 = Bar(.Field1)}
                                               ~~~~~~~
                                               </expected>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerInitializeInaccessibleField()
            Dim source =
<compilation name="ObjectInitializerInitializeInaccessibleField">
    <file name="a.vb">
Option Strict On

Imports System

Class C2
    Protected Field as Integer
End Class

Class C1

    Public Shared Sub Main()
        Dim c2 as New C2() With {.Field = 23}
    End Sub

End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC30389: 'C2.Field' is not accessible in this context because it is 'Protected'.
        Dim c2 as New C2() With {.Field = 23}
                                  ~~~~~
                                               </expected>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerInitializeNonWriteableMember()
            Dim source =
<compilation name="ObjectInitializerInitializeNonWriteableMember">
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public Sub Foo()
    End Sub    

    Public Shared Sub Main()
        Dim c1 as New C1() With {.Foo = "Hello World!"}
    End Sub

End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC30990: Member 'Foo' cannot be initialized in an object initializer expression because it is not a field or property.
        Dim c1 as New C1() With {.Foo = "Hello World!"}
                                  ~~~                                                   
                                               </expected>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerInitializeReadOnlyProperty()
            Dim source =
<compilation name="ObjectInitializerInitializeReadOnlyProperty">
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public ReadOnly Property X As String
        Get
            Return "foo"
        End Get
    End Property

    Public Shared Sub Main()
        Dim c1 as New C1() With {.X = "Hello World!"}
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC30526: Property 'X' is 'ReadOnly'.
        Dim c1 as New C1() With {.X = "Hello World!"}
                                 ~~~~~~~~~~~~~~~~~~~
                                               </expected>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerInitializeReadOnlyField()
            Dim source =
<compilation name="ObjectInitializerInitializeReadOnlyField">
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public ReadOnly X As String

    Public Shared Sub Main()
        Dim c1 as New C1() With {.X = "Hello World!"}
    End Sub
End Class        
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC30064: 'ReadOnly' variable cannot be the target of an assignment.
        Dim c1 as New C1() With {.X = "Hello World!"}
                                  ~                                                  
                                               </expected>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerPropertyWithInaccessibleSet()
            Dim source =
<compilation name="ObjectInitializerPropertyWithInaccessibleSet">
    <file name="a.vb">
Class C1
    Public Property X As String
        Get
            Return "foo"
        End Get
        private set
        End set
        
    End Property
End Class
Module Module1

    Sub Main()
        Dim x As New C1() With {.X = "foo"}
    End Sub

End Module
       
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC31102: 'Set' accessor of property 'X' is not accessible.
        Dim x As New C1() With {.X = "foo"}
                                ~~~~~~~~~~
                                               </expected>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerTypeIsErrorType()
            Dim source =
<compilation name="ObjectInitializerTypeIsErrorType">
    <file name="a.vb">

Class C3
    Private Sub New()
    End Sub
End Class

Module Module1

    Sub Main()
        Dim x As New C3() With {.X = "foo"}
        x = New C3() With {.X = Unknown()}
    End Sub

End Module
       
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC30517: Overload resolution failed because no 'New' is accessible.
        Dim x As New C3() With {.X = "foo"}
                     ~~
BC30456: 'X' is not a member of 'C3'.
        Dim x As New C3() With {.X = "foo"}
                                 ~
BC30517: Overload resolution failed because no 'New' is accessible.
        x = New C3() With {.X = Unknown()}
                ~~
BC30456: 'X' is not a member of 'C3'.
        x = New C3() With {.X = Unknown()}
                            ~
BC30451: 'Unknown' is not declared. It may be inaccessible due to its protection level.
        x = New C3() With {.X = Unknown()}
                                ~~~~~~~
                                               </expected>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerNewTWith()
            Dim source =
<compilation name="ObjectInitializerNewTWith">
    <file name="a.vb">
Imports System

Interface IFoo
    Property Bar As Integer
End Interface

Class C2
    Implements IFoo

    Public Property Bar As Integer Implements IFoo.Bar
End Class

Class C1
    Public Shared Sub main()
        DoStuff(OF C2)()
    End Sub

    Public shared Sub DoStuff(Of T As {IFoo, New})()
        Dim x As New T() With {.Bar = 23}
        x = New T() With {.Bar = 23}

        Console.WriteLine(x.Bar)
    End Sub
End Class    
    </file>
</compilation>

            CompileAndVerify(source, <![CDATA[
23
]]>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerTypeParametersInInitializers()
            Dim source =
<compilation name="ObjectInitializerTypeParametersInInitializers">
    <file name="a.vb">

Class C1
    Public Field As Integer = 42
End Class

Class C1(Of T)
    Public Field As T
End Class

Class C2
    Public Shared Sub Main()
        Dim x As New C1(Of Integer) With {.Field = 23}

        Foo(Of C1)
    End Sub

    Public Shared Sub Foo(Of T As New)()
        Dim x As New C1(Of T) With {.Field = New T}
    End Sub

End Class  
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerNestedInWithStatement_1()
            Dim source =
<compilation name="ObjectInitializerNestedInWithStatement_1">
    <file name="a.vb">
Imports System

Class C1
    Public Field As Integer = 42
End Class

Class C3
    Public Field2 As Integer = 23
End Class

Class C2
    Public Shared Sub Main()
        Dim x As New C1()
        x.Field = 23

        ' test that initializer shadows fields
        With x
            Dim y As New C1() With {.Field = .Field}
            Console.WriteLine(y.Field) ' should be 42
        End With
    End Sub
End Class   
    </file>
</compilation>

            CompileAndVerify(source, <![CDATA[
42
]]>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerNestedInWithStatement_2()
            Dim source =
<compilation name="ObjectInitializerNestedInWithStatement_2">
    <file name="a.vb">
Imports System

Class C1
    Public Field As Integer = 42
End Class

Class C3
    Public Field2 As Integer = 23
End Class

Class C2
    Public Shared Sub Main()

        ' nesting of with is not supported
        With New C3()
            Dim y As New C1() With {.Field = .Field2}
            Console.WriteLine(y.Field) ' should be 42
        End With

    End Sub
End Class   
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC30456: 'Field2' is not a member of 'C1'.
            Dim y As New C1() With {.Field = .Field2}
                                             ~~~~~~~
                                               </expected>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerNestedInitializers_1()
            Dim source =
<compilation name="ObjectInitializerNestedInitializers_1">
    <file name="a.vb">
Imports System

Class C1
    Public Field As Integer = 1
    Public FieldC2 as C2
End Class

Class C2
    Public Field As Integer = 2
End Class

Class C3
    Public Shared Sub Main()

        Dim x as new C1() with {.Field=23, .FieldC2=new C2() with {.Field=42}}

        Console.WriteLine(x.Field)
        Console.WriteLine(x.FieldC2.Field)
    End Sub
End Class   
    </file>
</compilation>

            CompileAndVerify(source, <![CDATA[
23
42
]]>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerNestedInitializers_2()
            Dim source =
<compilation name="ObjectInitializerNestedInitializers_2">
    <file name="a.vb">
Imports System

Class C1
    Public Field1 As Integer = 1
    Public Field2 As Integer = 1
    Public FieldC2 as C2
End Class

Class C2
    Public Field1 As Integer = 2
End Class

Class C3
    Public Shared Sub Main()

        Dim x as new C1() with {.Field1=23, .FieldC2=new C2() with {.Field1=.Field2}}
    End Sub
End Class   
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC30456: 'Field2' is not a member of 'C2'.
        Dim x as new C1() with {.Field1=23, .FieldC2=new C2() with {.Field1=.Field2}}
                                                                            ~~~~~~~                                                   
                                               </expected>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerCaptureFieldForLambda()
            Dim source =
<compilation name="ObjectInitializerCaptureFieldForLambda">
    <file name="a.vb">
Imports System

Class C1
    Public Field As Integer = 42
    Public Field2 As Func(Of Integer)
End Class

Class C1(Of T)
    Public Field As T
End Class

Class C2
    Public Shared Sub Main()
        Dim y as new C1()
        y.Field = 23

        Dim x As New C1 With {.Field2 = Function() As Integer
                                            Return .Field
                                        End Function}
        x.Field = 42
        Console.WriteLine(x.Field2.Invoke())
    End Sub
End Class 
    </file>
</compilation>

            CompileAndVerify(source, <![CDATA[
42
]]>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerUsedInFieldInitializers()
            Dim source =
<compilation name="ObjectInitializerUsedInFieldInitializers">
    <file name="a.vb">
Imports System

Class C1
    Public Field As Integer = 42
End Class

Class C2
    Private PrivateField As Integer = 23
    Public C1Inst As C1 = New C1() With {.Field = PrivateField}

    Public Shared Sub Main()
        Console.WriteLine((new C2()).C1Inst.Field)
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source, <![CDATA[
23
]]>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerFlowAnalysisVisitsInitializers()
            Dim source =
<compilation name="ObjectInitializerFlowAnalysisVisitsInitializers">
    <file name="a.vb">
Class C1
    Public RefTypeField As C2
End Class

Class C2
    Public Function CreateC2() As C2
        Return Nothing
    End Function

    Public Shared Sub Main()
        Dim y As C2
        Dim x As New C1 With {.RefTypeField = y.CreateC2}
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC42104: Variable 'y' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim x As New C1 With {.RefTypeField = y.CreateC2}
                                              ~                                                   
                                               </expected>)
            ' Yeah! We did not have this in Dev10 :)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerInitializePropertyWithOptionalParameters()
            Dim source =
<compilation name="ObjectInitializerInitializePropertyWithOptionalParameters">
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public WriteOnly Property X(Optional p As Integer = 23) As String
        Set(value As String)
        End Set
    End Property

    Public Shared Sub Main()
        Dim c1 As New C1() With {.X = "Hello World!"}
    End Sub
End Class       
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerMemberAccessOnInitExpressionAllowsAllFields()
            Dim source =
<compilation name="ObjectInitializerMemberAccessOnInitExpressionAllowsAllFields">
    <file name="a.vb">
Option Strict On

Imports System

Class C1
    Public WriteOnly Property X(Optional p As Integer = 23) As String
        Set(value As String)
        End Set
    End Property

    Public Function InstanceFunction(p as string) as String
        return nothing
    End Function

    Public ReadOnly Property ROProp as String
        Get
            return nothing
        End Get
    End Property

    Public Shared Sub Main()
        Dim c1 As New C1() With {.X = .InstanceFunction(.ROProp)}
    End Sub
End Class       
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerUsingInitializedTargetInInitializerValueType()
            Dim source =
<compilation name="ObjectInitializerUsingInitializedTargetInInitializerValueType">
    <file name="a.vb">
Option Strict On

Imports System

    Structure s1
        Public y As Integer
        Private _x As Integer
        Public Property x() As Integer
            Get
                y = 5
                Return _x
            End Get
            Set(ByVal value As Integer)
                _x = value
            End Set
        End Property
    End Structure

Class C1

    Public Shared Sub Main()
        dim foo as new s1()
        foo.x = 23

        Dim s1 As New s1 With {.x = s1.x}
    End Sub
End Class       
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerWithLifting_1()
            Dim source =
<compilation name="ObjectInitializerWithLifting_1">
    <file name="a.vb">
Option Strict On

Imports System

Structure C2
    Public Field As Func(Of Object)
    Public Field2 As Func(Of Object)
End Structure

Class C1
    Public Shared Sub Main()
        Dim x As New C2 With {.Field = Function()
                                           Return .Field ' only the first read is unassigned
                                       End Function,
                              .Field2 = Function()
                                           Return .Field ' reading is fine now.
                                       End Function}

        if x.Field.Invoke() is nothing then
            console.Writeline("Nothing returned, ok")
        end if
    End Sub
End Class       
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerDictionaryLookupOperatorSupported()
            Dim source =
<compilation name="ObjectInitializerDictionaryLookupOperatorSupported">
    <file name="a.vb">
Option Strict On

Imports System

    Class cust
        Public x As Long

        Default Public ReadOnly Property scen5(ByVal arg As String) As Integer
            Get
                Return 23
            End Get
        End Property
    End Class
  
Class C1
    Public Shared Sub Main()
       dim a as string = "foo"
       Dim c As New cust With {.x = !a}
    End Sub
End Class       
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerInField()
            Dim source =
<compilation name="ObjectInitializerInField">
    <file name="a.vb">
Option Strict On

Imports System
        Class scen2
            Private _scen2 As Short
            Protected Friend Property Scen2() As Short
                Get
                    Return _scen2
                End Get
                Set(ByVal value As Short)
                    _scen2 = value
                End Set
            End Property
        End Class

        Class scen2_2
            Dim o As Object = New scen2 With {.scen2 = 5}
        End Class
  
Class C2
    Public Shared Sub Main()
       dim x as new scen2_2
    End Sub
End Class       
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <Fact(), WorkItem(788522, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/788522")>
        Public Sub ObjectInitializerNoStackOverflowFor150LevelsOfNesting()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Class Cust
    Public x As Cust
    Public y As Integer
End Class

Class C2
    Public Shared Sub Main()
       Dim c As Cust = New Cust With {.y = 1, 
                        .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1,
                        .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1,
                        .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1,
                        .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1,
                        .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1,
                        .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1,
                        .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1,
                        .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1,
                        .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1,
                        .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1,
                        .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1,
                        .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1,
                        .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1,
                        .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1, .x = New Cust With {.y = 1,
                        .x = nothing}
}}}}}}}}}}
}}}}}}}}}}
}}}}}}}}}}
}}}}}}}}}}
}}}}}}}}}}
}}}}}}}}}}
}}}}}}}}}}
}}}}}}}}}}
}}}}}}}}}}
}}}}}}}}}}
}}}}}}}}}}
}}}}}}}}}}
}}}}}}}}}}
}}}}}}}}}}
    End Sub
End Class       
    </file>
</compilation>

            ' NOTE: Dev10 handled 416 levels of nesting, both algorithms are recursive, so there's not much to
            ' do for us here :(
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <Fact()>
        Public Sub ObjectInitializerReferencingItself()
            Dim source =
<compilation name="ObjectInitializerReferencingItself">
    <file name="a.vb">
Structure NonEmptyStructure
    Public RefField1 As String
    Public ValField1 As Integer
    Public Property Foo As String
End Structure

Structure EmptyStructure
    Public Property Foo1 As Integer
    Public Property Foo2 As String
End Structure

Class RefClass
    Public RefField1 As String
    Public ValField1 As Integer
    Public Property Foo As String
    Public Property Foo1 As Integer
    Public Property Foo2 As String
End Class

Interface IMissingStuff2
    Property Foo1 As Integer
    Property Foo2 As String
End Interface

Class ObjectInitializerClass
    Public Sub TypeParameterNotDefined(Of T As {IMissingStuff2, New})()
        Dim var20 As New T() With {.Foo1 = .Foo2.Length}                        ' Receiver type unknown, no warning
        Dim var21 As New T() With {.Foo1 = var21.Foo2.Length}                   ' Receiver type unknown, no warning
        Dim var22 As T = New T() With {.Foo1 = .Foo2.Length}                    ' Receiver type unknown, no warning
        Dim var23 As T = New T() With {.Foo1 = var23.Foo2.Length}               ' Receiver type unknown, no warning
    End Sub

    Public Sub TypeParameterAsStructure(Of T As {Structure, IMissingStuff2})()
        Dim var24 As New T() With {.Foo1 = .Foo2.Length}                        ' no local referenced, no warning
        Dim var25 As New T() With {.Foo1 = var25.Foo2.Length}                   ' inplace initialized, no warning
        Dim var26 As T = New T() With {.Foo1 = .Foo2.Length}                    ' no local referenced, no warning
        Dim var27 As T = New T() With {.Foo1 = var27.Foo2.Length}               ' temporary used, warning

        Dim var28, var29 As New T() With {.Foo1 = var28.Foo2.Length}            ' no local referenced, no warning
        Dim var30, var31 As New T() With {.Foo1 = var31.Foo2.Length}            ' Receiver type unknown, no warning
    End Sub

    Public Sub DoStuff_3(Of T As {RefClass, New})()
        Dim var32 As New T() With {.Foo1 = .Foo2.Length}                        ' no local referenced, no warning
        Dim var33 As New T() With {.Foo1 = var33.Foo2.Length}                   ' not yet initialized, warning
        Dim var34 As T = New T() With {.Foo1 = .Foo2.Length}                    ' no local referenced, no warning
        Dim var35 As T = New T() With {.Foo1 = var35.Foo2.Length}               ' not yet initialized, warning
        Dim var36, var37 As New T() With {.Foo1 = var36.Foo2.Length}            ' not yet initialized, warning
        Dim var38, var39 As New T() With {.Foo1 = var39.Foo2.Length}            ' not yet initialized, warning
    End Sub

    Public Shared Sub Main()
        Dim var01 As New NonEmptyStructure() With {.ValField1 = var01.RefField1.Length, .RefField1 = var01.Foo} ' no warnings
        Dim var02, var03 As New NonEmptyStructure() With {.ValField1 = var03.RefField1.Length, .RefField1 = var03.Foo} ' warnings
        Dim var04, var05 As New NonEmptyStructure() With {.RefField1 = var04.Foo} ' no warnings

        Dim var06 As NonEmptyStructure = New NonEmptyStructure() With {.ValField1 = var06.RefField1.Length, .RefField1 = var06.Foo} ' warnings


        Dim var07 As New EmptyStructure() With {.Foo1 = var07.Foo2.Length} ' no warnings
        Dim var08, var09 As New EmptyStructure() With {.Foo1 = var09.Foo2.Length} ' warnings
        Dim var10, var11 As New EmptyStructure() With {.Foo1 = var10.Foo2.Length} ' no warnings

        Dim var12 As EmptyStructure = New EmptyStructure() With {.Foo1 = var12.Foo2.Length} ' warnings


        Dim var13 As New RefClass() With {.ValField1 = var13.RefField1.Length, .RefField1 = var13.Foo} ' no warnings
        Dim var14, var15 As New RefClass() With {.ValField1 = var15.RefField1.Length, .RefField1 = var15.Foo} ' warnings
        Dim var16, var17 As New RefClass() With {.ValField1 = var16.RefField1.Length, .RefField1 = var16.Foo} ' no warnings

        Dim var18 As RefClass = New RefClass() With {.ValField1 = var18.RefField1.Length, .RefField1 = var18.Foo} ' warnings


        Dim var19 = New RefClass() With {.ValField1 = var18.RefField1.Length, .RefField1 = var18.Foo} ' warnings
    End Sub
End Class

Class CObjInitBase(Of T)
    Public Overridable Sub TypeParameterValueTypeAsClassConstraint(Of U As {T, IMissingStuff2})()
    End Sub
End Class

Class CObjInitDerived
    Inherits CObjInitBase(Of NonEmptyStructure)

    Public Overrides Sub TypeParameterValueTypeAsClassConstraint(Of U As {NonEmptyStructure, IMissingStuff2})()
        Dim uinst1 As New U() With {.Foo1 = uinst1.Foo2.Length}
        Dim uinst2 As U = New U() With {.Foo1 = uinst2.Foo2.Length}
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC42109: Variable 'var27' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        Dim var27 As T = New T() With {.Foo1 = var27.Foo2.Length}               ' temporary used, warning
                                               ~~~~~
BC42109: Variable 'var31' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        Dim var30, var31 As New T() With {.Foo1 = var31.Foo2.Length}            ' Receiver type unknown, no warning
                                                  ~~~~~
BC42104: Variable 'var33' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim var33 As New T() With {.Foo1 = var33.Foo2.Length}                   ' not yet initialized, warning
                                           ~~~~~
BC42104: Variable 'var35' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim var35 As T = New T() With {.Foo1 = var35.Foo2.Length}               ' not yet initialized, warning
                                               ~~~~~
BC42104: Variable 'var36' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim var36, var37 As New T() With {.Foo1 = var36.Foo2.Length}            ' not yet initialized, warning
                                                  ~~~~~
BC42104: Variable 'var39' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim var38, var39 As New T() With {.Foo1 = var39.Foo2.Length}            ' not yet initialized, warning
                                                  ~~~~~
BC42104: Variable 'RefField1' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim var02, var03 As New NonEmptyStructure() With {.ValField1 = var03.RefField1.Length, .RefField1 = var03.Foo} ' warnings
                                                                       ~~~~~~~~~~~~~~~
BC42109: Variable 'var03' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        Dim var02, var03 As New NonEmptyStructure() With {.ValField1 = var03.RefField1.Length, .RefField1 = var03.Foo} ' warnings
                                                                                                            ~~~~~
BC42104: Variable 'RefField1' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim var06 As NonEmptyStructure = New NonEmptyStructure() With {.ValField1 = var06.RefField1.Length, .RefField1 = var06.Foo} ' warnings
                                                                                    ~~~~~~~~~~~~~~~
BC42109: Variable 'var06' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        Dim var06 As NonEmptyStructure = New NonEmptyStructure() With {.ValField1 = var06.RefField1.Length, .RefField1 = var06.Foo} ' warnings
                                                                                                                         ~~~~~
BC42109: Variable 'var09' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        Dim var08, var09 As New EmptyStructure() With {.Foo1 = var09.Foo2.Length} ' warnings
                                                               ~~~~~
BC42109: Variable 'var12' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        Dim var12 As EmptyStructure = New EmptyStructure() With {.Foo1 = var12.Foo2.Length} ' warnings
                                                                         ~~~~~
BC42104: Variable 'var13' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim var13 As New RefClass() With {.ValField1 = var13.RefField1.Length, .RefField1 = var13.Foo} ' no warnings
                                                       ~~~~~
BC42104: Variable 'var15' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim var14, var15 As New RefClass() With {.ValField1 = var15.RefField1.Length, .RefField1 = var15.Foo} ' warnings
                                                              ~~~~~
BC42104: Variable 'var16' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim var16, var17 As New RefClass() With {.ValField1 = var16.RefField1.Length, .RefField1 = var16.Foo} ' no warnings
                                                              ~~~~~
BC42104: Variable 'var18' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim var18 As RefClass = New RefClass() With {.ValField1 = var18.RefField1.Length, .RefField1 = var18.Foo} ' warnings
                                                                  ~~~~~
BC42109: Variable 'uinst2' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        Dim uinst2 As U = New U() With {.Foo1 = uinst2.Foo2.Length}
                                                ~~~~~~
                                           </expected>)
        End Sub

        <Fact(), WorkItem(567976, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/567976")>
        Public Sub Bug567976()
            Dim source =
<compilation>
    <file name="a.vb">
 Module Program
    'Sub Main()
        Dim b13() As New Integer() {1,2,3}
    End Sub
End Module
   </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC30053: Arrays cannot be declared with 'New'.
        Dim b13() As New Integer() {1,2,3}
            ~~~~~
BC30205: End of statement expected.
        Dim b13() As New Integer() {1,2,3}
                                   ~
BC30429: 'End Sub' must be preceded by a matching 'Sub'.
    End Sub
    ~~~~~~~
                                                </expected>)
        End Sub

        <Fact(), WorkItem(599393, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/599393")>
        Public Sub Bug599393()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Xml.Linq
Imports System.Collections.Generic
Imports System.Linq
Module TestLiterals
End Module
        Class Cust
            Inherits XElement
 Sub New
  mybase.new("")
 End Sub
            Public Cust As Integer
            Private nme As String
            Public Shadows Property Name() As String
                Get
                    Return nme
                End Get
                Set(ByVal value As String)
                    nme = value
                End Set
            End Property
            Public e As XElement = <e><e/></e>
        End Class
Public Module Module1
        Sub Main()
            Try
                '<><%= ======================================================================================= %=></>
             Dim cust = New Cust With {.Cust = 1, .Name = .Value, .e = .<e>(0)}
                    Dim cust2 = New Cust With {.Cust = 1, .Name = .Name, .e = ...<e>(0)}         
Console.writeline("Scenario 8")
Console.writeline(<e><e/></>.ToString)
Console.writeline( cust.e.ToString)
Console.writeline( "Scenario 8.1")
Console.writeline(<e><e/></>.ToString)
Console.writeline( cust2.e.ToString)

            Catch
            Finally
            End Try
        End Sub
    End Module
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, additionalRefs:=XmlReferences)
            CompileAndVerify(compilation)
        End Sub

    End Class
End Namespace

