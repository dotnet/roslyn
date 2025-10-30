' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class BindingMemberInitializerTests
        Inherits BasicTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub SimpleObjectInitialization()
            Dim source = <![CDATA[
Option Strict On

Imports System

Class C2
    Public Field As String
End Class

Class C1
    Public Shared Sub Main()
        Dim c As C2 = New C2() With {.Field = "Hello World!"}'BIND:"New C2() With {.Field = "Hello World!"}"
        Console.WriteLine(c.Field)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub C2..ctor()) (OperationKind.ObjectCreation, Type: C2) (Syntax: 'New C2() Wi ... lo World!"}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C2) (Syntax: 'With {.Fiel ... lo World!"}')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: '.Field = "Hello World!"')
            Left: 
              IFieldReferenceOperation: C2.Field As System.String (OperationKind.FieldReference, Type: System.String) (Syntax: 'Field')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C2, IsImplicit) (Syntax: 'New C2() Wi ... lo World!"}')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Hello World!") (Syntax: '"Hello World!"')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ObjectInitializationWithFieldOnRight()
            Dim source = <![CDATA[
Option Strict On

Imports System

Class C2
    Public Field As String
    Public HelloWorld As String = "Hello World!"
End Class

Class C1
    Public Shared Sub Main()
        Dim c As C2 = New C2() With {.Field = .HelloWorld}'BIND:"New C2() With {.Field = .HelloWorld}"
        Console.WriteLine(c.Field)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub C2..ctor()) (OperationKind.ObjectCreation, Type: C2) (Syntax: 'New C2() Wi ... HelloWorld}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C2) (Syntax: 'With {.Fiel ... HelloWorld}')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: '.Field = .HelloWorld')
            Left: 
              IFieldReferenceOperation: C2.Field As System.String (OperationKind.FieldReference, Type: System.String) (Syntax: 'Field')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C2, IsImplicit) (Syntax: 'New C2() Wi ... HelloWorld}')
            Right: 
              IFieldReferenceOperation: C2.HelloWorld As System.String (OperationKind.FieldReference, Type: System.String) (Syntax: '.HelloWorld')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C2, IsImplicit) (Syntax: 'New C2() Wi ... HelloWorld}')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ObjectInitializerEmptyInitializers()
            Dim source = <![CDATA[
Option Strict On

Imports System

Class C2
End Class

Class C1
    Public Shared Sub Main()
        Dim c As C2 = New C2() With {}'BIND:"New C2() With {}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub C2..ctor()) (OperationKind.ObjectCreation, Type: C2, IsInvalid) (Syntax: 'New C2() With {}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C2, IsInvalid) (Syntax: 'With {}')
      Initializers(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30996: Initializer expected.
        Dim c As C2 = New C2() With {}'BIND:"New C2() With {}"
                                    ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ObjectInitializerMissingIdentifierInInitializer()
            Dim source = <![CDATA[
Option Strict On

Imports System

Public Class C2
    Public Field As Integer
End Class

Class C1
    Public Shared Sub Main()
        Dim c As C2 = New C2() With {. = Unknown(), . = Unknown()}'BIND:"New C2() With {. = Unknown(), . = Unknown()}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub C2..ctor()) (OperationKind.ObjectCreation, Type: C2, IsInvalid) (Syntax: 'New C2() Wi ...  Unknown()}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C2, IsInvalid) (Syntax: 'With {. = U ...  Unknown()}')
      Initializers(2):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: '. = Unknown()')
            Left: 
              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '. = Unknown()')
                Children(0)
            Right: 
              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'Unknown()')
                Children(1):
                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'Unknown')
                      Children(0)
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: '. = Unknown()')
            Left: 
              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '. = Unknown()')
                Children(0)
            Right: 
              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'Unknown()')
                Children(1):
                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'Unknown')
                      Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30203: Identifier expected.
        Dim c As C2 = New C2() With {. = Unknown(), . = Unknown()}'BIND:"New C2() With {. = Unknown(), . = Unknown()}"
                                       ~
BC30451: 'Unknown' is not declared. It may be inaccessible due to its protection level.
        Dim c As C2 = New C2() With {. = Unknown(), . = Unknown()}'BIND:"New C2() With {. = Unknown(), . = Unknown()}"
                                         ~~~~~~~
BC30203: Identifier expected.
        Dim c As C2 = New C2() With {. = Unknown(), . = Unknown()}'BIND:"New C2() With {. = Unknown(), . = Unknown()}"
                                                      ~
BC30451: 'Unknown' is not declared. It may be inaccessible due to its protection level.
        Dim c As C2 = New C2() With {. = Unknown(), . = Unknown()}'BIND:"New C2() With {. = Unknown(), . = Unknown()}"
                                                        ~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ObjectInitializerOnlyDotIdentifierInInitializer()
            Dim source = <![CDATA[
Option Strict On

Imports System

Public Class C2
    Public Field As Integer
End Class

Class C1
    Public Shared Sub Main()
        Dim c As C2 = New C2() With {.Field}'BIND:"New C2() With {.Field}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub C2..ctor()) (OperationKind.ObjectCreation, Type: C2, IsInvalid) (Syntax: 'New C2() With {.Field}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C2, IsInvalid) (Syntax: 'With {.Field}')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: '.Field')
            Left: 
              IFieldReferenceOperation: C2.Field As System.Int32 (OperationKind.FieldReference, Type: System.Int32, IsInvalid) (Syntax: 'Field')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C2, IsInvalid, IsImplicit) (Syntax: 'New C2() With {.Field}')
            Right: 
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30201: Expression expected.
        Dim c As C2 = New C2() With {.Field}'BIND:"New C2() With {.Field}"
                                           ~
BC30984: '=' expected (object initializer).
        Dim c As C2 = New C2() With {.Field}'BIND:"New C2() With {.Field}"
                                           ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ObjectInitializerMissingExpressionInInitializer()
            Dim source = <![CDATA[
Option Strict On

Imports System

Public Class C2
    Public Field As Integer
End Class

Class C1
    Public Shared Sub Main()
        Dim c As C2 = New C2() With {.Field =}'BIND:"New C2() With {.Field =}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub C2..ctor()) (OperationKind.ObjectCreation, Type: C2, IsInvalid) (Syntax: 'New C2() With {.Field =}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C2, IsInvalid) (Syntax: 'With {.Field =}')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: '.Field =')
            Left: 
              IFieldReferenceOperation: C2.Field As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'Field')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C2, IsInvalid, IsImplicit) (Syntax: 'New C2() With {.Field =}')
            Right: 
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30201: Expression expected.
        Dim c As C2 = New C2() With {.Field =}'BIND:"New C2() With {.Field =}"
                                             ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <WorkItem(529213, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529213")>
        <Fact()>
        Public Sub ObjectInitializerKeyKeywordInInitializer()
            Dim source = <![CDATA[
Option Strict On

Imports System

Class C2
    Public Field As Integer
End Class

Class C1
    Public Shared Sub Main()
        Dim c As C2 = New C2() With {Key .Field = 23}'BIND:"New C2() With {Key .Field = 23}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub C2..ctor()) (OperationKind.ObjectCreation, Type: C2, IsInvalid) (Syntax: 'New C2() Wi ... Field = 23}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C2, IsInvalid) (Syntax: 'With {Key .Field = 23}')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: 'Key .Field = 23')
            Left: 
              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'Key .Field = 23')
                Children(0)
            Right: 
              IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperationKind.Binary, Type: ?, IsInvalid) (Syntax: 'Key .Field = 23')
                Left: 
                  IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'Key .Field')
                    Children(1):
                        IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'Key .Field')
                          Children(1):
                              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'Key')
                                Children(0)
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 23) (Syntax: '23')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30985: Name of field or property being initialized in an object initializer must start with '.'.
        Dim c As C2 = New C2() With {Key .Field = 23}'BIND:"New C2() With {Key .Field = 23}"
                                     ~
BC30451: 'Key' is not declared. It may be inaccessible due to its protection level.
        Dim c As C2 = New C2() With {Key .Field = 23}'BIND:"New C2() With {Key .Field = 23}"
                                     ~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <WorkItem(544357, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544357")>
        <Fact()>
        Public Sub ObjectInitializerMultipleInitializations()
            Dim source = <![CDATA[
Option Strict On

Imports System

Class C2
    Public Field As String
End Class

Class C1
    Public Shared Sub Main()
        Dim c As C2 = New C2() With {.Field = "a", .Field = "b"}'BIND:"New C2() With {.Field = "a", .Field = "b"}"
        Dim d As C2 = New C2() With {.Field = "a", .Field = "b"}
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub C2..ctor()) (OperationKind.ObjectCreation, Type: C2, IsInvalid) (Syntax: 'New C2() Wi ... ield = "b"}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C2, IsInvalid) (Syntax: 'With {.Fiel ... ield = "b"}')
      Initializers(2):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: '.Field = "a"')
            Left: 
              IFieldReferenceOperation: C2.Field As System.String (OperationKind.FieldReference, Type: System.String) (Syntax: 'Field')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C2, IsInvalid, IsImplicit) (Syntax: 'New C2() Wi ... ield = "b"}')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "a") (Syntax: '"a"')
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsInvalid) (Syntax: '.Field = "b"')
            Left: 
              IFieldReferenceOperation: C2.Field As System.String (OperationKind.FieldReference, Type: System.String, IsInvalid) (Syntax: 'Field')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C2, IsInvalid, IsImplicit) (Syntax: 'New C2() Wi ... ield = "b"}')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "b") (Syntax: '"b"')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30989: Multiple initializations of 'Field'.  Fields and properties can be initialized only once in an object initializer expression.
        Dim c As C2 = New C2() With {.Field = "a", .Field = "b"}'BIND:"New C2() With {.Field = "a", .Field = "b"}"
                                                    ~~~~~
BC30989: Multiple initializations of 'Field'.  Fields and properties can be initialized only once in an object initializer expression.
        Dim d As C2 = New C2() With {.Field = "a", .Field = "b"}
                                                    ~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ObjectInitializerInitializingObject()
            Dim source = <![CDATA[
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        Dim c As Object = New Object() With {.Field = "a"}'BIND:"New Object() With {.Field = "a"}"
        Dim d As New Object() With {.Field = "b"}
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub System.Object..ctor()) (OperationKind.ObjectCreation, Type: System.Object, IsInvalid) (Syntax: 'New Object( ... ield = "a"}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Object, IsInvalid) (Syntax: 'With {.Field = "a"}')
      Initializers(1):
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "a", IsInvalid) (Syntax: '"a"')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30994: Object initializer syntax cannot be used to initialize an instance of 'System.Object'.
        Dim c As Object = New Object() With {.Field = "a"}'BIND:"New Object() With {.Field = "a"}"
                                       ~~~~~~~~~~~~~~~~~~~
BC30994: Object initializer syntax cannot be used to initialize an instance of 'System.Object'.
        Dim d As New Object() With {.Field = "b"}
                              ~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
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

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ObjectInitializerInitializeSharedFieldOnNewInstance()
            Dim source = <![CDATA[
Option Strict On

Imports System

Class C1
    Public Shared Field1 As String

    Public Shared Sub Main()
        Dim c1 As New C1() With {.Field1 = "Hello World!"}'BIND:"New C1() With {.Field1 = "Hello World!"}"
    End Sub

End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub C1..ctor()) (OperationKind.ObjectCreation, Type: C1, IsInvalid) (Syntax: 'New C1() Wi ... lo World!"}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C1, IsInvalid) (Syntax: 'With {.Fiel ... lo World!"}')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsInvalid) (Syntax: '.Field1 = "Hello World!"')
            Left: 
              IFieldReferenceOperation: C1.Field1 As System.String (Static) (OperationKind.FieldReference, Type: System.String, IsInvalid) (Syntax: 'Field1')
                Instance Receiver: 
                  null
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Hello World!") (Syntax: '"Hello World!"')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30991: Member 'Field1' cannot be initialized in an object initializer expression because it is shared.
        Dim c1 As New C1() With {.Field1 = "Hello World!"}'BIND:"New C1() With {.Field1 = "Hello World!"}"
                                  ~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ObjectInitializerInitializeSharedPropertyOnNewInstance()
            Dim source = <![CDATA[
Option Strict On

Imports System

Class C1
    Public Shared Property Property1 As String

    Public Shared Sub Main()
        Dim c1 As New C1() With {.Property1 = "Hello World!"}'BIND:"New C1() With {.Property1 = "Hello World!"}"
    End Sub

End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub C1..ctor()) (OperationKind.ObjectCreation, Type: C1, IsInvalid) (Syntax: 'New C1() Wi ... lo World!"}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C1, IsInvalid) (Syntax: 'With {.Prop ... lo World!"}')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void, IsInvalid) (Syntax: '.Property1  ... llo World!"')
            Left: 
              IPropertyReferenceOperation: Property C1.Property1 As System.String (Static) (OperationKind.PropertyReference, Type: System.String, IsInvalid) (Syntax: 'Property1')
                Instance Receiver: 
                  null
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Hello World!") (Syntax: '"Hello World!"')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30991: Member 'Property1' cannot be initialized in an object initializer expression because it is shared.
        Dim c1 As New C1() With {.Property1 = "Hello World!"}'BIND:"New C1() With {.Property1 = "Hello World!"}"
                                  ~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ObjectInitializerInitializeNonExistentField()
            Dim source = <![CDATA[
Option Strict On

Imports System

Class C1

    Public Shared Sub Main()
        Dim c1 As New C1() With {.Field1 = Bar(.Field1)}'BIND:"New C1() With {.Field1 = Bar(.Field1)}"
    End Sub

End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub C1..ctor()) (OperationKind.ObjectCreation, Type: C1, IsInvalid) (Syntax: 'New C1() Wi ... r(.Field1)}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C1, IsInvalid) (Syntax: 'With {.Fiel ... r(.Field1)}')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: '.Field1 = Bar(.Field1)')
            Left: 
              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '.Field1 = Bar(.Field1)')
                Children(1):
                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'Field1')
                      Children(1):
                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'New C1() Wi ... r(.Field1)}')
            Right: 
              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'Bar(.Field1)')
                Children(2):
                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'Bar')
                      Children(0)
                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '.Field1')
                      Children(1):
                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'New C1() Wi ... r(.Field1)}')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30456: 'Field1' is not a member of 'C1'.
        Dim c1 As New C1() With {.Field1 = Bar(.Field1)}'BIND:"New C1() With {.Field1 = Bar(.Field1)}"
                                  ~~~~~~
BC30451: 'Bar' is not declared. It may be inaccessible due to its protection level.
        Dim c1 As New C1() With {.Field1 = Bar(.Field1)}'BIND:"New C1() With {.Field1 = Bar(.Field1)}"
                                           ~~~
BC30456: 'Field1' is not a member of 'C1'.
        Dim c1 As New C1() With {.Field1 = Bar(.Field1)}'BIND:"New C1() With {.Field1 = Bar(.Field1)}"
                                               ~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ObjectInitializerInitializeInaccessibleField()
            Dim source = <![CDATA[
Option Strict On

Imports System

Class C2
    Protected Field As Integer
End Class

Class C1

    Public Shared Sub Main()
        Dim c2 As New C2() With {.Field = 23}'BIND:"New C2() With {.Field = 23}"
    End Sub

End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub C2..ctor()) (OperationKind.ObjectCreation, Type: C2, IsInvalid) (Syntax: 'New C2() Wi ... Field = 23}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C2, IsInvalid) (Syntax: 'With {.Field = 23}')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: '.Field = 23')
            Left: 
              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '.Field = 23')
                Children(1):
                    IInvalidOperation (OperationKind.Invalid, Type: System.Int32, IsInvalid) (Syntax: 'Field')
                      Children(1):
                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C2, IsInvalid, IsImplicit) (Syntax: 'New C2() Wi ... Field = 23}')
            Right: 
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: ?, IsImplicit) (Syntax: '23')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 23) (Syntax: '23')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30389: 'C2.Field' is not accessible in this context because it is 'Protected'.
        Dim c2 As New C2() With {.Field = 23}'BIND:"New C2() With {.Field = 23}"
                                  ~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ObjectInitializerInitializeNonWriteableMember()
            Dim source = <![CDATA[
Option Strict On

Imports System

Class C1
    Public Sub Goo()
    End Sub

    Public Shared Sub Main()
        Dim c1 As New C1() With {.Goo = "Hello World!"}'BIND:"New C1() With {.Goo = "Hello World!"}"
    End Sub

End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub C1..ctor()) (OperationKind.ObjectCreation, Type: C1, IsInvalid) (Syntax: 'New C1() Wi ... lo World!"}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C1, IsInvalid) (Syntax: 'With {.Goo  ... lo World!"}')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: '.Goo = "Hello World!"')
            Left: 
              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '.Goo = "Hello World!"')
                Children(1):
                    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'Goo')
                      Children(1):
                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'New C1() Wi ... lo World!"}')
            Right: 
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: ?, IsImplicit) (Syntax: '"Hello World!"')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Hello World!") (Syntax: '"Hello World!"')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30990: Member 'Goo' cannot be initialized in an object initializer expression because it is not a field or property.
        Dim c1 As New C1() With {.Goo = "Hello World!"}'BIND:"New C1() With {.Goo = "Hello World!"}"
                                  ~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ObjectInitializerInitializeReadOnlyProperty()
            Dim source = <![CDATA[
Option Strict On

Imports System

Class C1
    Public ReadOnly Property X As String
        Get
            Return "goo"
        End Get
    End Property

    Public Shared Sub Main()
        Dim c1 As New C1() With {.X = "Hello World!"}'BIND:"New C1() With {.X = "Hello World!"}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub C1..ctor()) (OperationKind.ObjectCreation, Type: C1, IsInvalid) (Syntax: 'New C1() Wi ... lo World!"}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C1, IsInvalid) (Syntax: 'With {.X =  ... lo World!"}')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void, IsInvalid) (Syntax: '.X = "Hello World!"')
            Left: 
              IPropertyReferenceOperation: ReadOnly Property C1.X As System.String (OperationKind.PropertyReference, Type: System.String, IsInvalid) (Syntax: 'X')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'New C1() Wi ... lo World!"}')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Hello World!", IsInvalid) (Syntax: '"Hello World!"')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30526: Property 'X' is 'ReadOnly'.
        Dim c1 As New C1() With {.X = "Hello World!"}'BIND:"New C1() With {.X = "Hello World!"}"
                                 ~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ObjectInitializerInitializeReadOnlyField()
            Dim source = <![CDATA[
Option Strict On

Imports System

Class C1
    Public ReadOnly X As String

    Public Shared Sub Main()
        Dim c1 As New C1() With {.X = "Hello World!"}'BIND:"New C1() With {.X = "Hello World!"}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub C1..ctor()) (OperationKind.ObjectCreation, Type: C1, IsInvalid) (Syntax: 'New C1() Wi ... lo World!"}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C1, IsInvalid) (Syntax: 'With {.X =  ... lo World!"}')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: '.X = "Hello World!"')
            Left: 
              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'X')
                Children(1):
                    IFieldReferenceOperation: C1.X As System.String (OperationKind.FieldReference, Type: System.String, IsInvalid) (Syntax: 'X')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'New C1() Wi ... lo World!"}')
            Right: 
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: ?, IsImplicit) (Syntax: '"Hello World!"')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Hello World!") (Syntax: '"Hello World!"')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30064: 'ReadOnly' variable cannot be the target of an assignment.
        Dim c1 As New C1() With {.X = "Hello World!"}'BIND:"New C1() With {.X = "Hello World!"}"
                                  ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ObjectInitializerPropertyWithInaccessibleSet()
            Dim source = <![CDATA[
Class C1
    Public Property X As String
        Get
            Return "goo"
        End Get
        Private Set
        End Set

    End Property
End Class
Module Module1

    Sub Main()
        Dim x As New C1() With {.X = "goo"}'BIND:"New C1() With {.X = "goo"}"
    End Sub

End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub C1..ctor()) (OperationKind.ObjectCreation, Type: C1, IsInvalid) (Syntax: 'New C1() Wi ... .X = "goo"}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C1, IsInvalid) (Syntax: 'With {.X = "goo"}')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void, IsInvalid) (Syntax: '.X = "goo"')
            Left: 
              IPropertyReferenceOperation: Property C1.X As System.String (OperationKind.PropertyReference, Type: System.String, IsInvalid) (Syntax: 'X')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'New C1() Wi ... .X = "goo"}')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "goo", IsInvalid) (Syntax: '"goo"')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC31102: 'Set' accessor of property 'X' is not accessible.
        Dim x As New C1() With {.X = "goo"}'BIND:"New C1() With {.X = "goo"}"
                                ~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ObjectInitializerTypeIsErrorType()
            Dim source = <![CDATA[
Class C3
    Private Sub New()
    End Sub
End Class

Module Module1

    Sub Main()'BIND:"Sub Main()"
        Dim x As New C3() With {.X = "goo"}
        x = New C3() With {.X = Unknown()}
    End Sub

End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (4 statements, 1 locals) (OperationKind.Block, Type: null, IsInvalid) (Syntax: 'Sub Main()' ... End Sub')
  Locals: Local_1: x As C3
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim x As Ne ... .X = "goo"}')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'x As New C3 ... .X = "goo"}')
      Declarators:
          IVariableDeclaratorOperation (Symbol: x As C3) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x')
            Initializer: 
              null
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: 'As New C3() ... .X = "goo"}')
          IObjectCreationOperation (Constructor: Sub C3..ctor()) (OperationKind.ObjectCreation, Type: C3, IsInvalid) (Syntax: 'New C3() Wi ... .X = "goo"}')
            Arguments(0)
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C3, IsInvalid) (Syntax: 'With {.X = "goo"}')
                Initializers(1):
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: '.X = "goo"')
                      Left: 
                        IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '.X = "goo"')
                          Children(1):
                              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'X')
                                Children(1):
                                    IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C3, IsInvalid, IsImplicit) (Syntax: 'New C3() Wi ... .X = "goo"}')
                      Right: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: ?, IsImplicit) (Syntax: '"goo"')
                          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          Operand: 
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "goo") (Syntax: '"goo"')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'x = New C3( ...  Unknown()}')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C3, IsInvalid, IsImplicit) (Syntax: 'x = New C3( ...  Unknown()}')
        Left: 
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: C3) (Syntax: 'x')
        Right: 
          IObjectCreationOperation (Constructor: Sub C3..ctor()) (OperationKind.ObjectCreation, Type: C3, IsInvalid) (Syntax: 'New C3() Wi ...  Unknown()}')
            Arguments(0)
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C3, IsInvalid) (Syntax: 'With {.X = Unknown()}')
                Initializers(1):
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: '.X = Unknown()')
                      Left: 
                        IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '.X = Unknown()')
                          Children(1):
                              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'X')
                                Children(1):
                                    IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C3, IsInvalid, IsImplicit) (Syntax: 'New C3() Wi ...  Unknown()}')
                      Right: 
                        IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'Unknown()')
                          Children(1):
                              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'Unknown')
                                Children(0)
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
    ReturnedValue: 
      null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30517: Overload resolution failed because no 'New' is accessible.
        Dim x As New C3() With {.X = "goo"}
                     ~~
BC30456: 'X' is not a member of 'C3'.
        Dim x As New C3() With {.X = "goo"}
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
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ObjectInitializerNewTWith()
            Dim source = <![CDATA[
Imports System

Interface IGoo
    Property Bar As Integer
End Interface

Class C2
    Implements IGoo

    Public Property Bar As Integer Implements IGoo.Bar
End Class

Class C1
    Public Shared Sub main()
        DoStuff(Of C2)()
    End Sub

    Public Shared Sub DoStuff(Of T As {IGoo, New})()
        Dim x As New T() With {.Bar = 23}'BIND:"New T() With {.Bar = 23}"
        x = New T() With {.Bar = 23}

        Console.WriteLine(x.Bar)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITypeParameterObjectCreationOperation (OperationKind.TypeParameterObjectCreation, Type: T) (Syntax: 'New T() With {.Bar = 23}')
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: T) (Syntax: 'With {.Bar = 23}')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.Bar = 23')
            Left: 
              IPropertyReferenceOperation: Property IGoo.Bar As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'Bar')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: T, IsImplicit) (Syntax: 'New T() With {.Bar = 23}')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 23) (Syntax: '23')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ObjectInitializerTypeParametersInInitializers()
            Dim source = <![CDATA[
Class C1
    Public Field As Integer = 42
End Class

Class C1(Of T)
    Public Field As T
End Class

Class C2
    Public Shared Sub Main()
        Dim x As New C1(Of Integer) With {.Field = 23}

        Goo(Of C1)()
    End Sub

    Public Shared Sub Goo(Of T As New)()
        Dim x As New C1(Of T) With {.Field = New T}'BIND:"New C1(Of T) With {.Field = New T}"
    End Sub

End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub C1(Of T)..ctor()) (OperationKind.ObjectCreation, Type: C1(Of T)) (Syntax: 'New C1(Of T ... ld = New T}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C1(Of T)) (Syntax: 'With {.Field = New T}')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: T) (Syntax: '.Field = New T')
            Left: 
              IFieldReferenceOperation: C1(Of T).Field As T (OperationKind.FieldReference, Type: T) (Syntax: 'Field')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C1(Of T), IsImplicit) (Syntax: 'New C1(Of T ... ld = New T}')
            Right: 
              ITypeParameterObjectCreationOperation (OperationKind.TypeParameterObjectCreation, Type: T) (Syntax: 'New T')
                Initializer: 
                  null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
            Dim y As New C1() With {.Field = .Field}'BIND:"New C1() With {.Field = .Field}"
            Console.WriteLine(y.Field) ' should be 42
        End With
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source, <![CDATA[
42
]]>)

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'New C1() Wi ... d = .Field}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C1) (Syntax: 'With {.Field = .Field}')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '.Field = .Field')
            Left: 
              IFieldReferenceOperation: C1.Field As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'Field')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C1, IsImplicit) (Syntax: 'New C1() Wi ... d = .Field}')
            Right: 
              IFieldReferenceOperation: C1.Field As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: '.Field')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C1, IsImplicit) (Syntax: 'New C1() Wi ... d = .Field}')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source.Value, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ObjectInitializerNestedInWithStatement_2()
            Dim source = <![CDATA[
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
            Dim y As New C1() With {.Field = .Field2}'BIND:"New C1() With {.Field = .Field2}"
            Console.WriteLine(y.Field) ' should be 42
        End With

    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub C1..ctor()) (OperationKind.ObjectCreation, Type: C1, IsInvalid) (Syntax: 'New C1() Wi ...  = .Field2}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C1, IsInvalid) (Syntax: 'With {.Field = .Field2}')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: '.Field = .Field2')
            Left: 
              IFieldReferenceOperation: C1.Field As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'Field')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'New C1() Wi ...  = .Field2}')
            Right: 
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '.Field2')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '.Field2')
                    Children(1):
                        IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'New C1() Wi ...  = .Field2}')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30456: 'Field2' is not a member of 'C1'.
            Dim y As New C1() With {.Field = .Field2}'BIND:"New C1() With {.Field = .Field2}"
                                             ~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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

        Dim x As New C1() With {.Field = 23, .FieldC2 = New C2() With {.Field = 42}}'BIND:"New C1() With {.Field = 23, .FieldC2 = New C2() With {.Field = 42}}"

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

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'New C1() Wi ... ield = 42}}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C1) (Syntax: 'With {.Fiel ... ield = 42}}')
      Initializers(2):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '.Field = 23')
            Left: 
              IFieldReferenceOperation: C1.Field As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'Field')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C1, IsImplicit) (Syntax: 'New C1() Wi ... ield = 42}}')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 23) (Syntax: '23')
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C2) (Syntax: '.FieldC2 =  ... Field = 42}')
            Left: 
              IFieldReferenceOperation: C1.FieldC2 As C2 (OperationKind.FieldReference, Type: C2) (Syntax: 'FieldC2')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C1, IsImplicit) (Syntax: 'New C1() Wi ... ield = 42}}')
            Right: 
              IObjectCreationOperation (Constructor: Sub C2..ctor()) (OperationKind.ObjectCreation, Type: C2) (Syntax: 'New C2() Wi ... Field = 42}')
                Arguments(0)
                Initializer: 
                  IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C2) (Syntax: 'With {.Field = 42}')
                    Initializers(1):
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '.Field = 42')
                          Left: 
                            IFieldReferenceOperation: C2.Field As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'Field')
                              Instance Receiver: 
                                IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C2, IsImplicit) (Syntax: 'New C2() Wi ... Field = 42}')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42) (Syntax: '42')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source.Value, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ObjectInitializerNestedInitializers_2()
            Dim source = <![CDATA[
Imports System

Class C1
    Public Field1 As Integer = 1
    Public Field2 As Integer = 1
    Public FieldC2 As C2
End Class

Class C2
    Public Field1 As Integer = 2
End Class

Class C3
    Public Shared Sub Main()

        Dim x As New C1() With {.Field1 = 23, .FieldC2 = New C2() With {.Field1 = .Field2}}'BIND:"New C1() With {.Field1 = 23, .FieldC2 = New C2() With {.Field1 = .Field2}}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub C1..ctor()) (OperationKind.ObjectCreation, Type: C1, IsInvalid) (Syntax: 'New C1() Wi ... = .Field2}}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C1, IsInvalid) (Syntax: 'With {.Fiel ... = .Field2}}')
      Initializers(2):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '.Field1 = 23')
            Left: 
              IFieldReferenceOperation: C1.Field1 As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'Field1')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'New C1() Wi ... = .Field2}}')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 23) (Syntax: '23')
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C2, IsInvalid) (Syntax: '.FieldC2 =  ...  = .Field2}')
            Left: 
              IFieldReferenceOperation: C1.FieldC2 As C2 (OperationKind.FieldReference, Type: C2) (Syntax: 'FieldC2')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'New C1() Wi ... = .Field2}}')
            Right: 
              IObjectCreationOperation (Constructor: Sub C2..ctor()) (OperationKind.ObjectCreation, Type: C2, IsInvalid) (Syntax: 'New C2() Wi ...  = .Field2}')
                Arguments(0)
                Initializer: 
                  IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C2, IsInvalid) (Syntax: 'With {.Field1 = .Field2}')
                    Initializers(1):
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: '.Field1 = .Field2')
                          Left: 
                            IFieldReferenceOperation: C2.Field1 As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'Field1')
                              Instance Receiver: 
                                IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C2, IsInvalid, IsImplicit) (Syntax: 'New C2() Wi ...  = .Field2}')
                          Right: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '.Field2')
                              Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Operand: 
                                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '.Field2')
                                  Children(1):
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C2, IsInvalid, IsImplicit) (Syntax: 'New C2() Wi ...  = .Field2}')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30456: 'Field2' is not a member of 'C2'.
        Dim x As New C1() With {.Field1 = 23, .FieldC2 = New C2() With {.Field1 = .Field2}}'BIND:"New C1() With {.Field1 = 23, .FieldC2 = New C2() With {.Field1 = .Field2}}"
                                                                                  ~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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

        Dim x As New C1 With {.Field2 = Function() As Integer'BIND:"New C1 With {.Field2 = Function() As Integer"
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

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'New C1 With ... d Function}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C1) (Syntax: 'With {.Fiel ... d Function}')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Func(Of System.Int32)) (Syntax: '.Field2 = F ... nd Function')
            Left: 
              IFieldReferenceOperation: C1.Field2 As System.Func(Of System.Int32) (OperationKind.FieldReference, Type: System.Func(Of System.Int32)) (Syntax: 'Field2')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C1, IsImplicit) (Syntax: 'New C1 With ... d Function}')
            Right: 
              IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32), IsImplicit) (Syntax: 'Function()  ... nd Function')
                Target: 
                  IAnonymousFunctionOperation (Symbol: Function () As System.Int32) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Function()  ... nd Function')
                    IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Function()  ... nd Function')
                      Locals: Local_1: <anonymous local> As System.Int32
                      IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'Return .Field')
                        ReturnedValue: 
                          IFieldReferenceOperation: C1.Field As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: '.Field')
                            Instance Receiver: 
                              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C1, IsImplicit) (Syntax: 'New C1 With ... d Function}')
                      ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Function')
                        Statement: 
                          null
                      IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Function')
                        ReturnedValue: 
                          ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'End Function')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source.Value, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
    Public C1Inst As C1 = New C1() With {.Field = PrivateField}'BIND:"New C1() With {.Field = PrivateField}"

    Public Shared Sub Main()
        Console.WriteLine((new C2()).C1Inst.Field)
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source, <![CDATA[
23
]]>)
            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'New C1() Wi ... ivateField}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C1) (Syntax: 'With {.Fiel ... ivateField}')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '.Field = PrivateField')
            Left: 
              IFieldReferenceOperation: C1.Field As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'Field')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C1, IsImplicit) (Syntax: 'New C1() Wi ... ivateField}')
            Right: 
              IFieldReferenceOperation: C2.PrivateField As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'PrivateField')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C2, IsImplicit) (Syntax: 'PrivateField')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source.Value, expectedOperationTree, expectedDiagnostics)
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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC42104: Variable 'y' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim x As New C1 With {.RefTypeField = y.CreateC2}
                                              ~
                                               </expected>)
            ' Yeah! We did not have this in Dev10 :)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ObjectInitializerInitializePropertyWithOptionalParameters()
            Dim source = <![CDATA[
Option Strict On

Imports System

Class C1
    Public WriteOnly Property X(Optional p As Integer = 23) As String
        Set(value As String)
        End Set
    End Property

    Public Shared Sub Main()
        Dim c1 As New C1() With {.X = "Hello World!"}'BIND:"New C1() With {.X = "Hello World!"}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'New C1() Wi ... lo World!"}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C1) (Syntax: 'With {.X =  ... lo World!"}')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.X = "Hello World!"')
            Left: 
              IPropertyReferenceOperation: WriteOnly Property C1.X([p As System.Int32 = 23]) As System.String (OperationKind.PropertyReference, Type: System.String) (Syntax: 'X')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C1, IsImplicit) (Syntax: 'New C1() Wi ... lo World!"}')
                Arguments(1):
                    IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: p) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'X')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 23, IsImplicit) (Syntax: 'X')
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Hello World!") (Syntax: '"Hello World!"')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ObjectInitializerMemberAccessOnInitExpressionAllowsAllFields()
            Dim source = <![CDATA[
Option Strict On

Imports System

Class C1
    Public WriteOnly Property X(Optional p As Integer = 23) As String
        Set(value As String)
        End Set
    End Property

    Public Function InstanceFunction(p As String) As String
        Return Nothing
    End Function

    Public ReadOnly Property ROProp As String
        Get
            Return Nothing
        End Get
    End Property

    Public Shared Sub Main()
        Dim c1 As New C1() With {.X = .InstanceFunction(.ROProp)}'BIND:"New C1() With {.X = .InstanceFunction(.ROProp)}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'New C1() Wi ... n(.ROProp)}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C1) (Syntax: 'With {.X =  ... n(.ROProp)}')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.X = .Insta ... on(.ROProp)')
            Left: 
              IPropertyReferenceOperation: WriteOnly Property C1.X([p As System.Int32 = 23]) As System.String (OperationKind.PropertyReference, Type: System.String) (Syntax: 'X')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C1, IsImplicit) (Syntax: 'New C1() Wi ... n(.ROProp)}')
                Arguments(1):
                    IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: p) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'X')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 23, IsImplicit) (Syntax: 'X')
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Right: 
              IInvocationOperation ( Function C1.InstanceFunction(p As System.String) As System.String) (OperationKind.Invocation, Type: System.String) (Syntax: '.InstanceFu ... on(.ROProp)')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C1, IsImplicit) (Syntax: 'New C1() Wi ... n(.ROProp)}')
                Arguments(1):
                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: p) (OperationKind.Argument, Type: null) (Syntax: '.ROProp')
                      IPropertyReferenceOperation: ReadOnly Property C1.ROProp As System.String (OperationKind.PropertyReference, Type: System.String) (Syntax: '.ROProp')
                        Instance Receiver: 
                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C1, IsImplicit) (Syntax: 'New C1() Wi ... n(.ROProp)}')
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ObjectInitializerUsingInitializedTargetInInitializerValueType()
            Dim source = <![CDATA[
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
        Dim goo As New s1()
        goo.x = 23

        Dim s1 As New s1 With {.x = s1.x}'BIND:"New s1 With {.x = s1.x}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub s1..ctor()) (OperationKind.ObjectCreation, Type: s1) (Syntax: 'New s1 With {.x = s1.x}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: s1) (Syntax: 'With {.x = s1.x}')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.x = s1.x')
            Left: 
              IPropertyReferenceOperation: Property s1.x As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'x')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: s1, IsImplicit) (Syntax: 'As New s1 W ... {.x = s1.x}')
            Right: 
              IPropertyReferenceOperation: Property s1.x As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 's1.x')
                Instance Receiver: 
                  ILocalReferenceOperation: s1 (OperationKind.LocalReference, Type: s1) (Syntax: 's1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ObjectInitializerWithLifting_1()
            Dim source = <![CDATA[
Option Strict On

Imports System

Structure C2
    Public Field As Func(Of Object)
    Public Field2 As Func(Of Object)
End Structure

Class C1
    Public Shared Sub Main()
        Dim x As New C2 With {.Field = Function()'BIND:"New C2 With {.Field = Function()"
                                           Return .Field ' only the first read is unassigned
                                       End Function,
                              .Field2 = Function()
                                            Return .Field ' reading is fine now.
                                        End Function}

        If x.Field.Invoke() Is Nothing Then
            Console.WriteLine("Nothing returned, ok")
        End If
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub C2..ctor()) (OperationKind.ObjectCreation, Type: C2) (Syntax: 'New C2 With ... d Function}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C2) (Syntax: 'With {.Fiel ... d Function}')
      Initializers(2):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Func(Of System.Object)) (Syntax: '.Field = Fu ... nd Function')
            Left: 
              IFieldReferenceOperation: C2.Field As System.Func(Of System.Object) (OperationKind.FieldReference, Type: System.Func(Of System.Object)) (Syntax: 'Field')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C2, IsImplicit) (Syntax: 'As New C2 W ... d Function}')
            Right: 
              IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Object), IsImplicit) (Syntax: 'Function()' ... nd Function')
                Target: 
                  IAnonymousFunctionOperation (Symbol: Function () As System.Object) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Function()' ... nd Function')
                    IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Function()' ... nd Function')
                      Locals: Local_1: <anonymous local> As System.Object
                      IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'Return .Field')
                        ReturnedValue: 
                          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '.Field')
                            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            Operand: 
                              IFieldReferenceOperation: C2.Field As System.Func(Of System.Object) (OperationKind.FieldReference, Type: System.Func(Of System.Object)) (Syntax: '.Field')
                                Instance Receiver: 
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C2, IsImplicit) (Syntax: 'As New C2 W ... d Function}')
                      ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Function')
                        Statement: 
                          null
                      IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Function')
                        ReturnedValue: 
                          ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Object, IsImplicit) (Syntax: 'End Function')
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Func(Of System.Object)) (Syntax: '.Field2 = F ... nd Function')
            Left: 
              IFieldReferenceOperation: C2.Field2 As System.Func(Of System.Object) (OperationKind.FieldReference, Type: System.Func(Of System.Object)) (Syntax: 'Field2')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C2, IsImplicit) (Syntax: 'As New C2 W ... d Function}')
            Right: 
              IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Object), IsImplicit) (Syntax: 'Function() ... nd Function')
                Target: 
                  IAnonymousFunctionOperation (Symbol: Function () As System.Object) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Function() ... nd Function')
                    IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Function() ... nd Function')
                      Locals: Local_1: <anonymous local> As System.Object
                      IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'Return .Field')
                        ReturnedValue: 
                          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '.Field')
                            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            Operand: 
                              IFieldReferenceOperation: C2.Field As System.Func(Of System.Object) (OperationKind.FieldReference, Type: System.Func(Of System.Object)) (Syntax: '.Field')
                                Instance Receiver: 
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C2, IsImplicit) (Syntax: 'As New C2 W ... d Function}')
                      ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Function')
                        Statement: 
                          null
                      IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Function')
                        ReturnedValue: 
                          ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Object, IsImplicit) (Syntax: 'End Function')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ObjectInitializerDictionaryLookupOperatorSupported()
            Dim source = <![CDATA[
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
        Dim a As String = "goo"
        Dim c As New cust With {.x = !a}'BIND:"New cust With {.x = !a}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub cust..ctor()) (OperationKind.ObjectCreation, Type: cust) (Syntax: 'New cust With {.x = !a}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: cust) (Syntax: 'With {.x = !a}')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int64) (Syntax: '.x = !a')
            Left: 
              IFieldReferenceOperation: cust.x As System.Int64 (OperationKind.FieldReference, Type: System.Int64) (Syntax: 'x')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: cust, IsImplicit) (Syntax: 'New cust With {.x = !a}')
            Right: 
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: '!a')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  IPropertyReferenceOperation: ReadOnly Property cust.scen5(arg As System.String) As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: '!a')
                    Instance Receiver: 
                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: cust, IsImplicit) (Syntax: 'New cust With {.x = !a}')
                    Arguments(1):
                        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: arg) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'a')
                          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "a") (Syntax: 'a')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ObjectInitializerInField()
            Dim source = <![CDATA[
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
    Dim o As Object = New scen2 With {.Scen2 = 5}'BIND:"New scen2 With {.Scen2 = 5}"
End Class

Class C2
    Public Shared Sub Main()
        Dim x As New scen2_2
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub scen2..ctor()) (OperationKind.ObjectCreation, Type: scen2) (Syntax: 'New scen2 W ... .Scen2 = 5}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: scen2) (Syntax: 'With {.Scen2 = 5}')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.Scen2 = 5')
            Left: 
              IPropertyReferenceOperation: Property scen2.Scen2 As System.Int16 (OperationKind.PropertyReference, Type: System.Int16) (Syntax: 'Scen2')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: scen2, IsImplicit) (Syntax: 'New scen2 W ... .Scen2 = 5}')
            Right: 
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int16, Constant: 5, IsImplicit) (Syntax: '5')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <ConditionalFact(GetType(WindowsOrLinuxOnly)), WorkItem(34880, "https://github.com/dotnet/roslyn/issues/34880"), WorkItem(788522, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/788522")>
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
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)
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
    Public Property Goo As String
End Structure

Structure EmptyStructure
    Public Property Goo1 As Integer
    Public Property Goo2 As String
End Structure

Class RefClass
    Public RefField1 As String
    Public ValField1 As Integer
    Public Property Goo As String
    Public Property Goo1 As Integer
    Public Property Goo2 As String
End Class

Interface IMissingStuff2
    Property Goo1 As Integer
    Property Goo2 As String
End Interface

Class ObjectInitializerClass
    Public Sub TypeParameterNotDefined(Of T As {IMissingStuff2, New})()
        Dim var20 As New T() With {.Goo1 = .Goo2.Length}                        ' Receiver type unknown, no warning
        Dim var21 As New T() With {.Goo1 = var21.Goo2.Length}                   ' Receiver type unknown, no warning
        Dim var22 As T = New T() With {.Goo1 = .Goo2.Length}                    ' Receiver type unknown, no warning
        Dim var23 As T = New T() With {.Goo1 = var23.Goo2.Length}               ' Receiver type unknown, no warning
    End Sub

    Public Sub TypeParameterAsStructure(Of T As {Structure, IMissingStuff2})()
        Dim var24 As New T() With {.Goo1 = .Goo2.Length}                        ' no local referenced, no warning
        Dim var25 As New T() With {.Goo1 = var25.Goo2.Length}                   ' inplace initialized, no warning
        Dim var26 As T = New T() With {.Goo1 = .Goo2.Length}                    ' no local referenced, no warning
        Dim var27 As T = New T() With {.Goo1 = var27.Goo2.Length}               ' temporary used, warning

        Dim var28, var29 As New T() With {.Goo1 = var28.Goo2.Length}            ' no local referenced, no warning
        Dim var30, var31 As New T() With {.Goo1 = var31.Goo2.Length}            ' Receiver type unknown, no warning
    End Sub

    Public Sub DoStuff_3(Of T As {RefClass, New})()
        Dim var32 As New T() With {.Goo1 = .Goo2.Length}                        ' no local referenced, no warning
        Dim var33 As New T() With {.Goo1 = var33.Goo2.Length}                   ' not yet initialized, warning
        Dim var34 As T = New T() With {.Goo1 = .Goo2.Length}                    ' no local referenced, no warning
        Dim var35 As T = New T() With {.Goo1 = var35.Goo2.Length}               ' not yet initialized, warning
        Dim var36, var37 As New T() With {.Goo1 = var36.Goo2.Length}            ' not yet initialized, warning
        Dim var38, var39 As New T() With {.Goo1 = var39.Goo2.Length}            ' not yet initialized, warning
    End Sub

    Public Shared Sub Main()
        Dim var01 As New NonEmptyStructure() With {.ValField1 = var01.RefField1.Length, .RefField1 = var01.Goo} ' no warnings
        Dim var02, var03 As New NonEmptyStructure() With {.ValField1 = var03.RefField1.Length, .RefField1 = var03.Goo} ' warnings
        Dim var04, var05 As New NonEmptyStructure() With {.RefField1 = var04.Goo} ' no warnings

        Dim var06 As NonEmptyStructure = New NonEmptyStructure() With {.ValField1 = var06.RefField1.Length, .RefField1 = var06.Goo} ' warnings


        Dim var07 As New EmptyStructure() With {.Goo1 = var07.Goo2.Length} ' no warnings
        Dim var08, var09 As New EmptyStructure() With {.Goo1 = var09.Goo2.Length} ' warnings
        Dim var10, var11 As New EmptyStructure() With {.Goo1 = var10.Goo2.Length} ' no warnings

        Dim var12 As EmptyStructure = New EmptyStructure() With {.Goo1 = var12.Goo2.Length} ' warnings


        Dim var13 As New RefClass() With {.ValField1 = var13.RefField1.Length, .RefField1 = var13.Goo} ' no warnings
        Dim var14, var15 As New RefClass() With {.ValField1 = var15.RefField1.Length, .RefField1 = var15.Goo} ' warnings
        Dim var16, var17 As New RefClass() With {.ValField1 = var16.RefField1.Length, .RefField1 = var16.Goo} ' no warnings

        Dim var18 As RefClass = New RefClass() With {.ValField1 = var18.RefField1.Length, .RefField1 = var18.Goo} ' warnings


        Dim var19 = New RefClass() With {.ValField1 = var18.RefField1.Length, .RefField1 = var18.Goo} ' warnings
    End Sub
End Class

Class CObjInitBase(Of T)
    Public Overridable Sub TypeParameterValueTypeAsClassConstraint(Of U As {T, IMissingStuff2})()
    End Sub
End Class

Class CObjInitDerived
    Inherits CObjInitBase(Of NonEmptyStructure)

    Public Overrides Sub TypeParameterValueTypeAsClassConstraint(Of U As {NonEmptyStructure, IMissingStuff2})()
        Dim uinst1 As New U() With {.Goo1 = uinst1.Goo2.Length}
        Dim uinst2 As U = New U() With {.Goo1 = uinst2.Goo2.Length}
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC42109: Variable 'var27' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        Dim var27 As T = New T() With {.Goo1 = var27.Goo2.Length}               ' temporary used, warning
                                               ~~~~~
BC42109: Variable 'var31' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        Dim var30, var31 As New T() With {.Goo1 = var31.Goo2.Length}            ' Receiver type unknown, no warning
                                                  ~~~~~
BC42104: Variable 'var33' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim var33 As New T() With {.Goo1 = var33.Goo2.Length}                   ' not yet initialized, warning
                                           ~~~~~
BC42104: Variable 'var35' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim var35 As T = New T() With {.Goo1 = var35.Goo2.Length}               ' not yet initialized, warning
                                               ~~~~~
BC42104: Variable 'var36' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim var36, var37 As New T() With {.Goo1 = var36.Goo2.Length}            ' not yet initialized, warning
                                                  ~~~~~
BC42104: Variable 'var39' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim var38, var39 As New T() With {.Goo1 = var39.Goo2.Length}            ' not yet initialized, warning
                                                  ~~~~~
BC42104: Variable 'RefField1' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim var02, var03 As New NonEmptyStructure() With {.ValField1 = var03.RefField1.Length, .RefField1 = var03.Goo} ' warnings
                                                                       ~~~~~~~~~~~~~~~
BC42109: Variable 'var03' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        Dim var02, var03 As New NonEmptyStructure() With {.ValField1 = var03.RefField1.Length, .RefField1 = var03.Goo} ' warnings
                                                                                                            ~~~~~
BC42104: Variable 'RefField1' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim var06 As NonEmptyStructure = New NonEmptyStructure() With {.ValField1 = var06.RefField1.Length, .RefField1 = var06.Goo} ' warnings
                                                                                    ~~~~~~~~~~~~~~~
BC42109: Variable 'var06' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        Dim var06 As NonEmptyStructure = New NonEmptyStructure() With {.ValField1 = var06.RefField1.Length, .RefField1 = var06.Goo} ' warnings
                                                                                                                         ~~~~~
BC42109: Variable 'var09' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        Dim var08, var09 As New EmptyStructure() With {.Goo1 = var09.Goo2.Length} ' warnings
                                                               ~~~~~
BC42109: Variable 'var12' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        Dim var12 As EmptyStructure = New EmptyStructure() With {.Goo1 = var12.Goo2.Length} ' warnings
                                                                         ~~~~~
BC42104: Variable 'var13' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim var13 As New RefClass() With {.ValField1 = var13.RefField1.Length, .RefField1 = var13.Goo} ' no warnings
                                                       ~~~~~
BC42104: Variable 'var15' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim var14, var15 As New RefClass() With {.ValField1 = var15.RefField1.Length, .RefField1 = var15.Goo} ' warnings
                                                              ~~~~~
BC42104: Variable 'var16' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim var16, var17 As New RefClass() With {.ValField1 = var16.RefField1.Length, .RefField1 = var16.Goo} ' no warnings
                                                              ~~~~~
BC42104: Variable 'var18' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim var18 As RefClass = New RefClass() With {.ValField1 = var18.RefField1.Length, .RefField1 = var18.Goo} ' warnings
                                                                  ~~~~~
BC42109: Variable 'uinst2' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        Dim uinst2 As U = New U() With {.Goo1 = uinst2.Goo2.Length}
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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)
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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, references:=XmlReferences)
            CompileAndVerify(compilation)
        End Sub

        <Fact, CompilerTrait(CompilerFeature.IOperation), WorkItem("https://github.com/dotnet/roslyn/issues/72916")>
        Public Sub RefReturningProperty()
            Dim cSharpSource = <![CDATA[
public class C
{
    int _f = 0;
    public ref int P
    {
        get => ref _f;
    }
}]]>.Value
            Dim cSharpCompilation = CreateCSharpCompilation(cSharpSource).VerifyDiagnostics()
            Dim cSharpRef = cSharpCompilation.EmitToPortableExecutableReference()

            Dim source = <![CDATA[
Imports System

Class Program
    Public Shared Sub Main()
        Dim c As C = New C() With { .P = 123 }'BIND:"New C() With { .P = 123 }"
        Console.WriteLine(c.P)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'New C() Wit ...  .P = 123 }')
  Arguments(0)
  Initializer:
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C) (Syntax: 'With { .P = 123 }')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.P = 123')
            Left:
              IPropertyReferenceOperation: ReadOnly ByRef Property C.P As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'P')
                Instance Receiver:
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'New C() Wit ...  .P = 123 }')
            Right:
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 123) (Syntax: '123')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics, references:={cSharpRef})

            CompileAndVerify(CreateCompilation(source, {cSharpRef}, TestOptions.ReleaseExe), expectedOutput:=<![CDATA[123]]>).VerifyDiagnostics()
        End Sub

    End Class
End Namespace

