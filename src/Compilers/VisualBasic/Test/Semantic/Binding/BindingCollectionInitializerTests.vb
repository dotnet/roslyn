' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class BindingCollectionInitializerTests
        Inherits BasicTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub CollectionInitializerList()
            Dim source =
<compilation name="CollectionInitializerList">
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections.Generic

Class C1
    Public Shared Sub Main()
        Dim c As New List(Of String) From {"Hello World!"}    'BIND:"New List(Of String) From {"Hello World!"}"    
        Console.WriteLine(c(0))
    End Sub
End Class        
    </file>
</compilation>

            CompileAndVerify(source, "Hello World!")

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub System.Collections.Generic.List(Of System.String)..ctor()) (OperationKind.ObjectCreation, Type: System.Collections.Generic.List(Of System.String)) (Syntax: 'New List(Of ... lo World!"}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Collections.Generic.List(Of System.String)) (Syntax: 'From {"Hello World!"}')
      Initializers(1):
          IInvocationOperation ( Sub System.Collections.Generic.List(Of System.String).Add(item As System.String)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '"Hello World!"')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List(Of System.String), IsImplicit) (Syntax: 'New List(Of ... lo World!"}')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '"Hello World!"')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Hello World!") (Syntax: '"Hello World!"')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source.Value, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub CollectionInitializerListEachElementAsCollectionInitializer()
            Dim source =
<compilation name="CollectionInitializerListEachElementAsCollectionInitializer">
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections.Generic

Class C1
    Public Shared Sub Main()
        Dim c As New List(Of String) From {{"Hello"}, {" "}, {"World!"}}'BIND:"New List(Of String) From {{"Hello"}, {" "}, {"World!"}}"

        For each element in c
            Console.Write(element)
        next element
    End Sub
End Class        
    </file>
</compilation>

            CompileAndVerify(source, "Hello World!")

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub System.Collections.Generic.List(Of System.String)..ctor()) (OperationKind.ObjectCreation, Type: System.Collections.Generic.List(Of System.String)) (Syntax: 'New List(Of ... {"World!"}}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Collections.Generic.List(Of System.String)) (Syntax: 'From {{"Hel ... {"World!"}}')
      Initializers(3):
          IInvocationOperation ( Sub System.Collections.Generic.List(Of System.String).Add(item As System.String)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{"Hello"}')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List(Of System.String), IsImplicit) (Syntax: 'New List(Of ... {"World!"}}')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '"Hello"')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Hello") (Syntax: '"Hello"')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IInvocationOperation ( Sub System.Collections.Generic.List(Of System.String).Add(item As System.String)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{" "}')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List(Of System.String), IsImplicit) (Syntax: 'New List(Of ... {"World!"}}')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '" "')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: " ") (Syntax: '" "')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IInvocationOperation ( Sub System.Collections.Generic.List(Of System.String).Add(item As System.String)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{"World!"}')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List(Of System.String), IsImplicit) (Syntax: 'New List(Of ... {"World!"}}')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '"World!"')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "World!") (Syntax: '"World!"')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source.Value, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub CollectionInitializerDictionary()
            Dim source =
<compilation name="CollectionInitializerDictionary">
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections.Generic

Class C1
    Public Shared Sub Main()
        Dim c As New Dictionary(Of String, Integer) From {{"Hello", 23}, {"World", 42}}'BIND:"New Dictionary(Of String, Integer) From {{"Hello", 23}, {"World", 42}}"

        For Each keyValue In c
            Console.WriteLine(keyValue.Key + " " + keyValue.Value.ToString)
        Next

    End Sub
End Class        
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:=<![CDATA[
Hello 23
World 42
]]>)

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub System.Collections.Generic.Dictionary(Of System.String, System.Int32)..ctor()) (OperationKind.ObjectCreation, Type: System.Collections.Generic.Dictionary(Of System.String, System.Int32)) (Syntax: 'New Diction ... orld", 42}}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Collections.Generic.Dictionary(Of System.String, System.Int32)) (Syntax: 'From {{"Hel ... orld", 42}}')
      Initializers(2):
          IInvocationOperation ( Sub System.Collections.Generic.Dictionary(Of System.String, System.Int32).Add(key As System.String, value As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{"Hello", 23}')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.Dictionary(Of System.String, System.Int32), IsImplicit) (Syntax: 'New Diction ... orld", 42}}')
            Arguments(2):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: key) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '"Hello"')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Hello") (Syntax: '"Hello"')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '23')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 23) (Syntax: '23')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IInvocationOperation ( Sub System.Collections.Generic.Dictionary(Of System.String, System.Int32).Add(key As System.String, value As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{"World", 42}')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.Dictionary(Of System.String, System.Int32), IsImplicit) (Syntax: 'New Diction ... orld", 42}}')
            Arguments(2):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: key) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '"World"')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "World") (Syntax: '"World"')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '42')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42) (Syntax: '42')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source.Value, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub CollectionInitializerCustomCollection()
            Dim source =
<compilation name="CollectionInitializerCustomCollection">
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections.Generic

Class Custom
    Private list As New List(Of String)()

    Public Function GetEnumerator() As CustomEnumerator
        Return New CustomEnumerator(list)
    End Function

    Public Sub add(p As String)
        list.Add(p)
    End Sub

    Public Class CustomEnumerator
        Private list As list(Of String)
        Private index As Integer = -1

        Public Sub New(list As List(Of String))
            Me.list = list
        End Sub

        Public Function MoveNext() As Boolean
            If Me.index &lt; Me.list.Count - 1 Then
                index = index + 1
                Return True
            End If

            Return False
        End function

        Public ReadOnly Property Current As String
            Get
                Return Me.list(index)
            End Get
        End Property
    End Class
End Class

    Class C1
        Public Shared Sub Main()
            Dim c as Custom = New Custom() From {"Hello", " ", "World"}'BIND:"New Custom() From {"Hello", " ", "World"}"
            Output(c)
        End Sub

        Public Shared Sub Output(c as custom)
            For Each value In c
                Console.Write(value)
            Next
        End Sub
    End Class
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:=<![CDATA[
Hello World
]]>).VerifyIL("C1.Main", <![CDATA[
{
  // Code size       44 (0x2c)
  .maxstack  3
  IL_0000:  newobj     "Sub Custom..ctor()"
  IL_0005:  dup
  IL_0006:  ldstr      "Hello"
  IL_000b:  callvirt   "Sub Custom.add(String)"
  IL_0010:  dup
  IL_0011:  ldstr      " "
  IL_0016:  callvirt   "Sub Custom.add(String)"
  IL_001b:  dup
  IL_001c:  ldstr      "World"
  IL_0021:  callvirt   "Sub Custom.add(String)"
  IL_0026:  call       "Sub C1.Output(Custom)"
  IL_002b:  ret
}
]]>.Value)

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub Custom..ctor()) (OperationKind.ObjectCreation, Type: Custom) (Syntax: 'New Custom( ... ", "World"}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: Custom) (Syntax: 'From {"Hell ... ", "World"}')
      Initializers(3):
          IInvocationOperation ( Sub Custom.add(p As System.String)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '"Hello"')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Custom, IsImplicit) (Syntax: 'New Custom( ... ", "World"}')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: p) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '"Hello"')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Hello") (Syntax: '"Hello"')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IInvocationOperation ( Sub Custom.add(p As System.String)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '" "')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Custom, IsImplicit) (Syntax: 'New Custom( ... ", "World"}')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: p) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '" "')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: " ") (Syntax: '" "')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IInvocationOperation ( Sub Custom.add(p As System.String)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '"World"')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Custom, IsImplicit) (Syntax: 'New Custom( ... ", "World"}')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: p) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '"World"')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "World") (Syntax: '"World"')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source.Value, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub CollectionInitializerEmptyInitializers()
            Dim source = <![CDATA[
Option Strict On

Imports System.Collections.Generic

Class C2
End Class

Class C1
    Public Shared Sub Main()
        ' ok
        Dim a As New List(Of Integer) From {}

        ' not ok
        Dim b As New List(Of Integer) From {{}}'BIND:"New List(Of Integer) From {{}}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub System.Collections.Generic.List(Of System.Int32)..ctor()) (OperationKind.ObjectCreation, Type: System.Collections.Generic.List(Of System.Int32), IsInvalid) (Syntax: 'New List(Of ... ) From {{}}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Collections.Generic.List(Of System.Int32), IsInvalid) (Syntax: 'From {{}}')
      Initializers(1):
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '{}')
            Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36721: An aggregate collection initializer entry must contain at least one element.
        Dim b As New List(Of Integer) From {{}}'BIND:"New List(Of Integer) From {{}}"
                                            ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub CollectionInitializerNotACollection()
            Dim source = <![CDATA[
Option Strict On

Imports System
Imports System.Collections.Generic

Class C1
    Public Shared Sub Main()
        Dim c As New C1() From {"Hello World!"}'BIND:"New C1() From {"Hello World!"}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub C1..ctor()) (OperationKind.ObjectCreation, Type: C1, IsInvalid) (Syntax: 'New C1() Fr ... lo World!"}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C1, IsInvalid) (Syntax: 'From {"Hello World!"}')
      Initializers(1):
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '"Hello World!"')
            Children(1):
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Hello World!", IsInvalid) (Syntax: '"Hello World!"')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36718: Cannot initialize the type 'C1' with a collection initializer because it is not a collection type.
        Dim c As New C1() From {"Hello World!"}'BIND:"New C1() From {"Hello World!"}"
                          ~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub CollectionInitializerCannotCombineBothInitializers()
            Dim source = <![CDATA[
Option Strict On

Imports System
Imports System.Collections

Class C2
    Implements ICollection

    Public Sub CopyTo(array As Array, index As Integer) Implements ICollection.CopyTo
    End Sub

    Public ReadOnly Property Count As Integer Implements ICollection.Count
        Get
            Return 0
        End Get
    End Property

    Public ReadOnly Property IsSynchronized As Boolean Implements ICollection.IsSynchronized
        Get
            Return False
        End Get
    End Property

    Public ReadOnly Property SyncRoot As Object Implements ICollection.SyncRoot
        Get
            Return Nothing
        End Get
    End Property

    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return Nothing
    End Function

    Public a As String

    Public Sub Add(p As String)
    End Sub
End Class

Class C1
    Public a As String

    Public Shared Sub Main()'BIND:"Public Shared Sub Main()"
        Dim a As New C2() With {.a = "goo"} From {"Hello World!"}
        Dim b As New C2() From {"Hello World!"} With {.a = "goo"}
        Dim c As C2 = New C2() From {"Hello World!"} With {.a = "goo"}
        Dim d As C2 = New C2() With {.a = "goo"} From {"Hello World!"} 
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (6 statements, 4 locals) (OperationKind.Block, Type: null, IsInvalid) (Syntax: 'Public Shar ... End Sub')
  Locals: Local_1: a As C2
    Local_2: b As C2
    Local_3: c As C2
    Local_4: d As C2
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim a As Ne ... .a = "goo"}')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'a As New C2 ... .a = "goo"}')
      Declarators:
          IVariableDeclaratorOperation (Symbol: a As C2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
            Initializer: 
              null
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: 'As New C2() ... .a = "goo"}')
          IObjectCreationOperation (Constructor: Sub C2..ctor()) (OperationKind.ObjectCreation, Type: C2) (Syntax: 'New C2() Wi ... .a = "goo"}')
            Arguments(0)
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C2) (Syntax: 'With {.a = "goo"}')
                Initializers(1):
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: '.a = "goo"')
                      Left: 
                        IFieldReferenceOperation: C2.a As System.String (OperationKind.FieldReference, Type: System.String) (Syntax: 'a')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C2, IsImplicit) (Syntax: 'New C2() Wi ... .a = "goo"}')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "goo") (Syntax: '"goo"')
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim b As Ne ... lo World!"}')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'b As New C2 ... lo World!"}')
      Declarators:
          IVariableDeclaratorOperation (Symbol: b As C2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'b')
            Initializer: 
              null
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: 'As New C2() ... lo World!"}')
          IObjectCreationOperation (Constructor: Sub C2..ctor()) (OperationKind.ObjectCreation, Type: C2) (Syntax: 'New C2() Fr ... lo World!"}')
            Arguments(0)
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C2) (Syntax: 'From {"Hello World!"}')
                Initializers(1):
                    IInvocationOperation ( Sub C2.Add(p As System.String)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '"Hello World!"')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C2, IsImplicit) (Syntax: 'New C2() Fr ... lo World!"}')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: p) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '"Hello World!"')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Hello World!") (Syntax: '"Hello World!"')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim c As C2 ... lo World!"}')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'c As C2 = N ... lo World!"}')
      Declarators:
          IVariableDeclaratorOperation (Symbol: c As C2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c')
            Initializer: 
              null
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= New C2()  ... lo World!"}')
          IObjectCreationOperation (Constructor: Sub C2..ctor()) (OperationKind.ObjectCreation, Type: C2, IsInvalid) (Syntax: 'New C2() Fr ... lo World!"}')
            Arguments(0)
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C2, IsInvalid) (Syntax: 'From {"Hello World!"}')
                Initializers(1):
                    IInvocationOperation ( Sub C2.Add(p As System.String)) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '"Hello World!"')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C2, IsInvalid, IsImplicit) (Syntax: 'New C2() Fr ... lo World!"}')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: p) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: '"Hello World!"')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Hello World!", IsInvalid) (Syntax: '"Hello World!"')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim d As C2 ... .a = "goo"}')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'd As C2 = N ... .a = "goo"}')
      Declarators:
          IVariableDeclaratorOperation (Symbol: d As C2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'd')
            Initializer: 
              null
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= New C2()  ... .a = "goo"}')
          IObjectCreationOperation (Constructor: Sub C2..ctor()) (OperationKind.ObjectCreation, Type: C2, IsInvalid) (Syntax: 'New C2() Wi ... .a = "goo"}')
            Arguments(0)
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C2, IsInvalid) (Syntax: 'With {.a = "goo"}')
                Initializers(1):
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsInvalid) (Syntax: '.a = "goo"')
                      Left: 
                        IFieldReferenceOperation: C2.a As System.String (OperationKind.FieldReference, Type: System.String, IsInvalid) (Syntax: 'a')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C2, IsInvalid, IsImplicit) (Syntax: 'New C2() Wi ... .a = "goo"}')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "goo", IsInvalid) (Syntax: '"goo"')
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
    ReturnedValue: 
      null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36720: An Object Initializer and a Collection Initializer cannot be combined in the same initialization.
        Dim a As New C2() With {.a = "goo"} From {"Hello World!"}
                                            ~~~~
BC36720: An Object Initializer and a Collection Initializer cannot be combined in the same initialization.
        Dim b As New C2() From {"Hello World!"} With {.a = "goo"}
                                                ~~~~
BC36720: An Object Initializer and a Collection Initializer cannot be combined in the same initialization.
        Dim c As C2 = New C2() From {"Hello World!"} With {.a = "goo"}
                               ~~~~~~~~~~~~~~~~~~~~~
BC36720: An Object Initializer and a Collection Initializer cannot be combined in the same initialization.
        Dim d As C2 = New C2() With {.a = "goo"} From {"Hello World!"} 
                               ~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub CollectionInitializerNoAddMethod()
            Dim source = <![CDATA[
Option Strict On

Imports System
Imports System.Collections

Class C2
    Implements ICollection

    Public Sub CopyTo(array As Array, index As Integer) Implements ICollection.CopyTo
    End Sub

    Public ReadOnly Property Count As Integer Implements ICollection.Count
        Get
            Return 0
        End Get
    End Property

    Public ReadOnly Property IsSynchronized As Boolean Implements ICollection.IsSynchronized
        Get
            Return False
        End Get
    End Property

    Public ReadOnly Property SyncRoot As Object Implements ICollection.SyncRoot
        Get
            Return Nothing
        End Get
    End Property

    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return Nothing
    End Function
End Class

Class C3
    Inherits C2

    Protected Sub Add()
    End Sub
End Class

Class C4
    Inherits C2

    Public Property Add() As String
End Class

Class C5
    Inherits C2

    Public Add As String
End Class

Class C1
    Public a As String

    Public Shared Sub Main()
        Dim a As New C2() From {"Hello World!"}'BIND:"New C2() From {"Hello World!"}"
        Dim b As New C3() From {"Hello World!"}
        Dim c As New C4() From {"Hello World!"}
        Dim d As New C5() From {"Hello World!"}
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub C2..ctor()) (OperationKind.ObjectCreation, Type: C2, IsInvalid) (Syntax: 'New C2() Fr ... lo World!"}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C2, IsInvalid) (Syntax: 'From {"Hello World!"}')
      Initializers(1):
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '"Hello World!"')
            Children(1):
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Hello World!", IsInvalid) (Syntax: '"Hello World!"')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36719: Cannot initialize the type 'C2' with a collection initializer because it does not have an accessible 'Add' method.
        Dim a As New C2() From {"Hello World!"}'BIND:"New C2() From {"Hello World!"}"
                          ~~~~~~~~~~~~~~~~~~~~~
BC36719: Cannot initialize the type 'C3' with a collection initializer because it does not have an accessible 'Add' method.
        Dim b As New C3() From {"Hello World!"}
                          ~~~~~~~~~~~~~~~~~~~~~
BC36719: Cannot initialize the type 'C4' with a collection initializer because it does not have an accessible 'Add' method.
        Dim c As New C4() From {"Hello World!"}
                          ~~~~~~~~~~~~~~~~~~~~~
BC36719: Cannot initialize the type 'C5' with a collection initializer because it does not have an accessible 'Add' method.
        Dim d As New C5() From {"Hello World!"}
                          ~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub CollectionInitializerAddMethodIsFunction()
            Dim source =
    <compilation name="CollectionInitializerAddMethodIsFunction">
        <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections

Public Class C1
    Implements ICollection

    Public Sub CopyTo(array As Array, index As Integer) Implements ICollection.CopyTo
    End Sub

    Public ReadOnly Property Count As Integer Implements ICollection.Count
        Get
            Return 0
        End Get
    End Property

    Public ReadOnly Property IsSynchronized As Boolean Implements ICollection.IsSynchronized
        Get
            Return False
        End Get
    End Property

    Public ReadOnly Property SyncRoot As Object Implements ICollection.SyncRoot
        Get
            Return Nothing
        End Get
    End Property

    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return Nothing
    End Function

    Public Function Add(p As Integer) As String
        Console.WriteLine("What's the point of returning something here?")
        return "Boo!"
    End Function
End Class

Class C2
    Public Shared Sub Main()
        Dim x As New C1() From {1}'BIND:"New C1() From {1}"
    End Sub
End Class
    </file>
    </compilation>

            CompileAndVerify(source, expectedOutput:=<![CDATA[
What's the point of returning something here?
 ]]>)

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'New C1() From {1}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C1) (Syntax: 'From {1}')
      Initializers(1):
          IInvocationOperation ( Function C1.Add(p As System.Int32) As System.String) (OperationKind.Invocation, Type: System.String, IsImplicit) (Syntax: '1')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C1, IsImplicit) (Syntax: 'New C1() From {1}')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: p) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source.Value, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub CollectionInitializerOverloadResolutionErrors()
            Dim source = <![CDATA[
Option Strict On

Imports System
Imports System.Collections

Class C2
    Implements ICollection

    Public Sub CopyTo(array As Array, index As Integer) Implements ICollection.CopyTo
    End Sub

    Public ReadOnly Property Count As Integer Implements ICollection.Count
        Get
            Return 0
        End Get
    End Property

    Public ReadOnly Property IsSynchronized As Boolean Implements ICollection.IsSynchronized
        Get
            Return False
        End Get
    End Property

    Public ReadOnly Property SyncRoot As Object Implements ICollection.SyncRoot
        Get
            Return Nothing
        End Get
    End Property

    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return Nothing
    End Function

    Public Sub Add()
    End Sub

    Protected Sub Add(p As String)
    End Sub
End Class

Class C3
    Inherits C2

    ' first argument matches
    Public Overloads Sub Add(p As String, q As Integer)
    End Sub
End Class

Class C4
    Inherits C2

    ' first argument does not match -> multiple candidates
    Public Overloads Sub Add(p As Integer, q As String)
    End Sub
End Class

Class C5
    Inherits C2

    ' first argument does not match -> multiple candidates
    Public Overloads Sub Add(p As Byte)
    End Sub
End Class

Class C1
    Public a As String

    Public Shared Sub Main()
        Dim a As New C2() From {"Hello World!", "Errors will be shown for each initializer element"}'BIND:"From {"Hello World!", "Errors will be shown for each initializer element"}"
        Dim b As New C3() From {"Hello World!"}
        Dim c As New C4() From {"Hello World!"}
        Dim d As New C5() From {300%}
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C2, IsInvalid) (Syntax: 'From {"Hell ... r element"}')
  Initializers(2):
      IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '"Hello World!"')
        Children(2):
            IOperation:  (OperationKind.None, Type: null, IsInvalid, IsImplicit) (Syntax: '"Hello World!"')
              Children(1):
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C2, IsInvalid, IsImplicit) (Syntax: 'New C2() Fr ... r element"}')
            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Hello World!", IsInvalid) (Syntax: '"Hello World!"')
      IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '"Errors wil ... er element"')
        Children(2):
            IOperation:  (OperationKind.None, Type: null, IsInvalid, IsImplicit) (Syntax: '"Errors wil ... er element"')
              Children(1):
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C2, IsInvalid, IsImplicit) (Syntax: 'New C2() Fr ... r element"}')
            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Errors will be shown for each initializer element", IsInvalid) (Syntax: '"Errors wil ... er element"')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30057: Too many arguments to 'Public Sub Add()'.
        Dim a As New C2() From {"Hello World!", "Errors will be shown for each initializer element"}'BIND:"From {"Hello World!", "Errors will be shown for each initializer element"}"
                                ~~~~~~~~~~~~~~
BC30057: Too many arguments to 'Public Sub Add()'.
        Dim a As New C2() From {"Hello World!", "Errors will be shown for each initializer element"}'BIND:"From {"Hello World!", "Errors will be shown for each initializer element"}"
                                                ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30516: Overload resolution failed because no accessible 'Add' accepts this number of arguments.
        Dim b As New C3() From {"Hello World!"}
                                ~~~~~~~~~~~~~~
BC30516: Overload resolution failed because no accessible 'Add' accepts this number of arguments.
        Dim c As New C4() From {"Hello World!"}
                                ~~~~~~~~~~~~~~
BC30439: Constant expression not representable in type 'Byte'.
        Dim d As New C5() From {300%}
                                ~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCollectionInitializerSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub CollectionInitializerWarningsWillBeKept()
            Dim source = <![CDATA[
Option Strict On

Imports System
Imports System.Collections

Class C2
    Implements ICollection

    Public Sub CopyTo(array As Array, index As Integer) Implements ICollection.CopyTo
    End Sub

    Public ReadOnly Property Count As Integer Implements ICollection.Count
        Get
            Return 0
        End Get
    End Property

    Public ReadOnly Property IsSynchronized As Boolean Implements ICollection.IsSynchronized
        Get
            Return False
        End Get
    End Property

    Public ReadOnly Property SyncRoot As Object Implements ICollection.SyncRoot
        Get
            Return Nothing
        End Get
    End Property

    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return Nothing
    End Function

    Public Shared Sub Add(p As String)
    End Sub
End Class

Class C1
    Public a As String

    Public Shared Sub Main()
        Dim a As New C2() From {"Hello World!", "Errors will be shown for each initializer element"}'BIND:"New C2() From {"Hello World!", "Errors will be shown for each initializer element"}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub C2..ctor()) (OperationKind.ObjectCreation, Type: C2) (Syntax: 'New C2() Fr ... r element"}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C2) (Syntax: 'From {"Hell ... r element"}')
      Initializers(2):
          IInvocationOperation (Sub C2.Add(p As System.String)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '"Hello World!"')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: p) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '"Hello World!"')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Hello World!") (Syntax: '"Hello World!"')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IInvocationOperation (Sub C2.Add(p As System.String)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '"Errors wil ... er element"')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: p) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '"Errors wil ... er element"')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Errors will be shown for each initializer element") (Syntax: '"Errors wil ... er element"')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        Dim a As New C2() From {"Hello World!", "Errors will be shown for each initializer element"}'BIND:"New C2() From {"Hello World!", "Errors will be shown for each initializer element"}"
                                ~~~~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        Dim a As New C2() From {"Hello World!", "Errors will be shown for each initializer element"}'BIND:"New C2() From {"Hello World!", "Errors will be shown for each initializer element"}"
                                                ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact()>
        Public Sub CollectionInitializerExtensionMethodsAreSupported()
            Dim source =
    <compilation name="CollectionInitializerExtensionMethodsAreSupported">
        <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices

Class C2
    Implements ICollection

    Public Sub CopyTo(array As Array, index As Integer) Implements ICollection.CopyTo
    End Sub

    Public ReadOnly Property Count As Integer Implements ICollection.Count
        Get
            Return 0
        End Get
    End Property

    Public ReadOnly Property IsSynchronized As Boolean Implements ICollection.IsSynchronized
        Get
            Return False
        End Get
    End Property

    Public ReadOnly Property SyncRoot As Object Implements ICollection.SyncRoot
        Get
            Return Nothing
        End Get
    End Property

    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return Nothing
    End Function
End Class

Class C1
    public a as string

    Public Shared Sub Main()
        ' extensions for custom type
        Dim a As New C2() From {"Hello World!", "Errors will be shown for each initializer element"}

        ' extensions for predefined type
        Dim x0 As LinkedList(Of Integer) = New LinkedList(Of Integer) From {1, 2, 3}
    End Sub
End Class        

Module C2Extensions
    &lt;Extension()&gt;
    Public Sub Add(this as C2, p as string)
    End Sub

    &lt;Extension()&gt;
    Public Sub ADD(ByRef x As LinkedList(Of Integer), ByVal y As Integer)
        x.AddLast(y)
    End Sub
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
                                                </expected>)
        End Sub

        <Fact()>
        Public Sub CollectionInitializerExtensionMethodsAreSupportedForValueTypes()
            Dim source =
    <compilation name="CollectionInitializerExtensionMethodsAreSupportedForValueTypes">
        <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices

Structure C2
    Implements ICollection

    Public Sub CopyTo(array As Array, index As Integer) Implements ICollection.CopyTo
    End Sub

    Public ReadOnly Property Count As Integer Implements ICollection.Count
        Get
            Return 0
        End Get
    End Property

    Public ReadOnly Property IsSynchronized As Boolean Implements ICollection.IsSynchronized
        Get
            Return False
        End Get
    End Property

    Public ReadOnly Property SyncRoot As Object Implements ICollection.SyncRoot
        Get
            Return Nothing
        End Get
    End Property

    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return Nothing
    End Function
End Structure

Class C1
    public a as string

    Public Shared Sub Main()
        Dim a As New C2() From {"Hello World!", "Errors will be shown for each initializer element"}
    End Sub
End Class        

Module C2Extensions
    &lt;Extension()&gt;
    Public Sub Add(this as C2, p as string)
    End Sub
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
                                                </expected>)
        End Sub

        <Fact()>
        Public Sub CollectionInitializerTypeConstraintsAreSupported()
            Dim source =
    <compilation name="CollectionInitializerTypeConstraintsAreSupported">
        <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections
Imports System.Collections.Generic

Public Interface IAdd(Of T)
    Sub Add(p As T)
End Interface

Public Class C2
    Public Sub Add()
    End Sub
End Class

Class C3
    Implements IAdd(Of String), ICollection

    private mylist as new list(of String)()

    Public Sub New()
    End Sub

    Public Sub Add1(p As String) Implements IAdd(Of String).Add
        mylist.add(p)
    End Sub

    Public Sub CopyTo(array As Array, index As Integer) Implements ICollection.CopyTo
    End Sub

    Public ReadOnly Property Count As Integer Implements ICollection.Count
        Get
            Return 0
        End Get
    End Property

    Public ReadOnly Property IsSynchronized As Boolean Implements ICollection.IsSynchronized
        Get
            Return False
        End Get
    End Property

    Public ReadOnly Property SyncRoot As Object Implements ICollection.SyncRoot
        Get
            Return False
        End Get
    End Property

    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return mylist.getenumerator
    End Function
End Class

Class C1
    Public Shared Sub DoStuff(Of T As {IAdd(Of String), ICollection, New})()
        Dim a As New T() From {"Hello", " ", "World!"}

        for each str as string in a
            Console.Write(str)
        next str
    End Sub

    Public Shared Sub Main()
        DoStuff(Of C3)()
    End Sub
End Class 
    </file>
    </compilation>

            CompileAndVerify(source, "Hello World!")
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub CollectionInitializerTypeConstraintsAndAmbiguity()
            Dim source = <![CDATA[
Option Strict On

Imports System
Imports System.Collections
Imports System.Collections.Generic

Public Interface IAdd(Of T)
    Sub Add(p As String)
End Interface

Class C1
    Public Shared Sub DoStuff(Of T As {IAdd(Of String), IAdd(Of Integer), ICollection, New})()
        Dim a As New T() From {"Hello", " ", "World!"}'BIND:"New T() From {"Hello", " ", "World!"}"

        For Each str As String In a
            Console.Write(str)
        Next str
    End Sub

    Public Shared Sub Main()
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITypeParameterObjectCreationOperation (OperationKind.TypeParameterObjectCreation, Type: T, IsInvalid) (Syntax: 'New T() Fro ... , "World!"}')
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: T, IsInvalid) (Syntax: 'From {"Hell ... , "World!"}')
      Initializers(3):
          IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '"Hello"')
            Children(2):
                IOperation:  (OperationKind.None, Type: null, IsInvalid, IsImplicit) (Syntax: '"Hello"')
                  Children(1):
                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: T, IsInvalid, IsImplicit) (Syntax: 'New T() Fro ... , "World!"}')
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Hello", IsInvalid) (Syntax: '"Hello"')
          IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '" "')
            Children(2):
                IOperation:  (OperationKind.None, Type: null, IsInvalid, IsImplicit) (Syntax: '" "')
                  Children(1):
                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: T, IsInvalid, IsImplicit) (Syntax: 'New T() Fro ... , "World!"}')
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: " ", IsInvalid) (Syntax: '" "')
          IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '"World!"')
            Children(2):
                IOperation:  (OperationKind.None, Type: null, IsInvalid, IsImplicit) (Syntax: '"World!"')
                  Children(1):
                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: T, IsInvalid, IsImplicit) (Syntax: 'New T() Fro ... , "World!"}')
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "World!", IsInvalid) (Syntax: '"World!"')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30521: Overload resolution failed because no accessible 'Add' is most specific for these arguments:
    'Sub IAdd(Of String).Add(p As String)': Not most specific.
    'Sub IAdd(Of Integer).Add(p As String)': Not most specific.
        Dim a As New T() From {"Hello", " ", "World!"}'BIND:"New T() From {"Hello", " ", "World!"}"
                               ~~~~~~~
BC30521: Overload resolution failed because no accessible 'Add' is most specific for these arguments:
    'Sub IAdd(Of String).Add(p As String)': Not most specific.
    'Sub IAdd(Of Integer).Add(p As String)': Not most specific.
        Dim a As New T() From {"Hello", " ", "World!"}'BIND:"New T() From {"Hello", " ", "World!"}"
                                        ~~~
BC30521: Overload resolution failed because no accessible 'Add' is most specific for these arguments:
    'Sub IAdd(Of String).Add(p As String)': Not most specific.
    'Sub IAdd(Of Integer).Add(p As String)': Not most specific.
        Dim a As New T() From {"Hello", " ", "World!"}'BIND:"New T() From {"Hello", " ", "World!"}"
                                             ~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <WorkItem(529265, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529265")>
        <Fact()>
        Public Sub CollectionInitializerCollectionInitializerArityCheck()
            Dim source = <![CDATA[
Option Strict On

Imports System
Imports System.Collections.Generic

Class C1
    Public Shared Sub Main()
        Dim x As New Dictionary(Of String, Integer) From {{1}}'BIND:"New Dictionary(Of String, Integer) From {{1}}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub System.Collections.Generic.Dictionary(Of System.String, System.Int32)..ctor()) (OperationKind.ObjectCreation, Type: System.Collections.Generic.Dictionary(Of System.String, System.Int32), IsInvalid) (Syntax: 'New Diction ...  From {{1}}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Collections.Generic.Dictionary(Of System.String, System.Int32), IsInvalid) (Syntax: 'From {{1}}')
      Initializers(1):
          IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '{1}')
            Children(2):
                IOperation:  (OperationKind.None, Type: null, IsInvalid, IsImplicit) (Syntax: '{1}')
                  Children(1):
                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.Dictionary(Of System.String, System.Int32), IsInvalid, IsImplicit) (Syntax: 'New Diction ...  From {{1}}')
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30455: Argument not specified for parameter 'value' of 'Public Overloads Sub Add(key As String, value As Integer)'.
        Dim x As New Dictionary(Of String, Integer) From {{1}}'BIND:"New Dictionary(Of String, Integer) From {{1}}"
                                                          ~~~
BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'String'.
        Dim x As New Dictionary(Of String, Integer) From {{1}}'BIND:"New Dictionary(Of String, Integer) From {{1}}"
                                                           ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact()>
        Public Sub CollectionInitializerReferencingItself()
            Dim source =
<compilation name="CollectionInitializerReferencingItselfRefType">
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections.Generic
Imports System.Collections

Interface IMissingStuff
    Sub Add(p As String)
    Function Item() As String
End Interface

Structure Custom
    Implements IMissingStuff, IEnumerable(Of String)

    Public Shared list As New List(Of String)()

    Public Sub Add(p As String) Implements IMissingStuff.Add
        list.Add(p)
    End Sub

    Public Function Item() As String Implements IMissingStuff.Item
        Return Nothing
    End Function

    Public Structure CustomEnumerator
        Implements IEnumerator(Of String)

        Private list As List(Of String)
        Private Shared index As Integer = -1

        Public Sub New(list As List(Of String))
            Me.list = list
        End Sub

        Public Function MoveNext() As Boolean
            If index &lt; Me.list.Count - 1 Then
                index = index + 1
            Return True
            End If

            Return False
        End function

        Public ReadOnly Property Current As String
            Get
                Return Me.list(index)
            End Get
        End Property

        Public ReadOnly Property Current1 As String Implements IEnumerator(Of String).Current
            Get
                Return Current
            End Get
        End Property

        Public ReadOnly Property Current2 As Object Implements IEnumerator.Current
            Get
                Return Current
            End Get
        End Property

        Public Function MoveNext1() As Boolean Implements IEnumerator.MoveNext
            Return MoveNext()
        End Function

        Public Sub Reset() Implements IEnumerator.Reset

        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose

        End Sub
    end structure

    Public Function GetEnumerator1() As IEnumerator(Of String) Implements IEnumerable(Of String).GetEnumerator
        Return New CustomEnumerator(list)
    End Function

    Public Function GetEnumerator2() As IEnumerator Implements IEnumerable.GetEnumerator
        Return New CustomEnumerator(list)
    End Function
End Structure

Structure CustomNonEmpty
    Implements IMissingStuff, IEnumerable(Of String)

    Public MakeItNonEmpty as String

    Public Shared list As New List(Of String)()

    Public Sub Add(p As String) Implements IMissingStuff.Add
        list.Add(p)
    End Sub

    Public Function Item() As String Implements IMissingStuff.Item
        Return Nothing
    End Function

    Public Structure CustomEnumerator
        Implements IEnumerator(Of String)

        Private list As List(Of String)
        Private Shared index As Integer = -1

        Public Sub New(list As List(Of String))
            Me.list = list
        End Sub

        Public Function MoveNext() As Boolean
            If index &lt; Me.list.Count - 1 Then
                index = index + 1
            Return True
            End If

            Return False
        End function

        Public ReadOnly Property Current As String
            Get
                Return Me.list(index)
            End Get
        End Property

        Public ReadOnly Property Current1 As String Implements IEnumerator(Of String).Current
            Get
                Return Current
            End Get
        End Property

        Public ReadOnly Property Current2 As Object Implements IEnumerator.Current
            Get
                Return Current
            End Get
        End Property

        Public Function MoveNext1() As Boolean Implements IEnumerator.MoveNext
            Return MoveNext()
        End Function

        Public Sub Reset() Implements IEnumerator.Reset

        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose

        End Sub
    end structure

    Public Function GetEnumerator1() As IEnumerator(Of String) Implements IEnumerable(Of String).GetEnumerator
        Return New CustomEnumerator(list)
    End Function

    Public Function GetEnumerator2() As IEnumerator Implements IEnumerable.GetEnumerator
        Return New CustomEnumerator(list)
    End Function
End Structure

Class CBase(Of T)
    Public Overridable Sub TypeParameterValueTypeAsClassConstraint(Of U As {T, IEnumerable, IMissingStuff})()
    End Sub
End Class

Class CDerived
    Inherits CBase(Of Custom)

    Public Overrides Sub TypeParameterValueTypeAsClassConstraint(Of U As {Custom, IEnumerable, IMissingStuff})()
        Dim m As New U From {"Hello World!", m.Item(0)}                                             ' temp used, m is uninitialized, show warning
        Dim n As U = New U() From {"Hello World!", n.Item(0)}                                       ' temp used, h is uninitialized, show warning
        Dim o, p As New U() From {o.Item(0), p.Item(0)}                                             ' temps used, show warnings (although o is initialized when initializing p)
    End Sub
End Class

        Class C1
            Public Sub TypeParameterNotDefined(Of T As {IEnumerable, IMissingStuff, New})()
                ' no warnings from type parameters as well
                Dim e As New T From {"Hello World!", e.Item(0)}                                     ' Receiver type unknown, no warning
                Dim f As T = New T() From {"Hello World!", f.Item(0)}                               ' Receiver type unknown, no warning
            End Sub

            Public Sub TypeParameterAsStructure(Of T As {Structure, IEnumerable, IMissingStuff})()
                ' no warnings from type parameters as well
                Dim g As New T From {"Hello World!", g.Item(0)}                                     ' temp used, g is uninitialized, show warning
                Dim h As T = New T() From {"Hello World!", h.Item(0)}                               ' temp used, h is uninitialized, show warning
                Dim i, j As New T() From {i.Item(0), j.Item(0)}                                     ' temps used, show warnings (although i is initialized when initializing j)
            End Sub

            Public Sub TypeParameterAsRefType(Of T As {List(Of String), new})()
                Dim k As New T From {"Hello World!", k.Item(0)}                                     ' temp used, k is uninitialized, show warning
                Dim l As T = New T() From {"Hello World!", l.Item(0)}                               ' temp used, l is uninitialized, show warning
            End Sub

            Public Shared Sub Main()
                Dim a As New Custom From {"Hello World!", a.Item(0)}                                ' empty, non trackable structure, no warning
                Dim b As Custom = New Custom() From {"Hello World!", b.Item(0)}                     ' empty, non trackable structure, no warning

                Dim q As New CustomNonEmpty From {"Hello World!", q.Item(0)}                        ' temp used, q is uninitialized, show warning
                Dim r As CustomNonEmpty = New CustomNonEmpty() From {"Hello World!", r.Item(0)}     ' temp used, r is uninitialized, show warning

                ' reference types are not ok, they are still Nothing
                Dim c As New List(Of String) From {"Hello World!", c.Item(0)}                       ' show warning
                Dim d As List(Of String) = New List(Of String)() From {"Hello World!", d.Item(0)}   ' show warning

                ' was already assigned, no warning again.
                c = New List(Of String)() From {"Hello World!", c.Item(0)}                          ' no warning
            End Sub
        End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)
            AssertTheseDiagnostics(compilation, <expected>
BC42109: Variable 'm' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        Dim m As New U From {"Hello World!", m.Item(0)}                                             ' temp used, m is uninitialized, show warning
                                             ~
BC42109: Variable 'n' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        Dim n As U = New U() From {"Hello World!", n.Item(0)}                                       ' temp used, h is uninitialized, show warning
                                                   ~
BC42109: Variable 'o' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        Dim o, p As New U() From {o.Item(0), p.Item(0)}                                             ' temps used, show warnings (although o is initialized when initializing p)
                                  ~
BC42109: Variable 'p' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        Dim o, p As New U() From {o.Item(0), p.Item(0)}                                             ' temps used, show warnings (although o is initialized when initializing p)
                                             ~
BC42109: Variable 'g' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
                Dim g As New T From {"Hello World!", g.Item(0)}                                     ' temp used, g is uninitialized, show warning
                                                     ~
BC42109: Variable 'h' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
                Dim h As T = New T() From {"Hello World!", h.Item(0)}                               ' temp used, h is uninitialized, show warning
                                                           ~
BC42109: Variable 'i' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
                Dim i, j As New T() From {i.Item(0), j.Item(0)}                                     ' temps used, show warnings (although i is initialized when initializing j)
                                          ~
BC42109: Variable 'j' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
                Dim i, j As New T() From {i.Item(0), j.Item(0)}                                     ' temps used, show warnings (although i is initialized when initializing j)
                                                     ~
BC42104: Variable 'k' is used before it has been assigned a value. A null reference exception could result at runtime.
                Dim k As New T From {"Hello World!", k.Item(0)}                                     ' temp used, k is uninitialized, show warning
                                                     ~
BC42104: Variable 'l' is used before it has been assigned a value. A null reference exception could result at runtime.
                Dim l As T = New T() From {"Hello World!", l.Item(0)}                               ' temp used, l is uninitialized, show warning
                                                           ~
BC42109: Variable 'q' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
                Dim q As New CustomNonEmpty From {"Hello World!", q.Item(0)}                        ' temp used, q is uninitialized, show warning
                                                                  ~
BC42109: Variable 'r' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
                Dim r As CustomNonEmpty = New CustomNonEmpty() From {"Hello World!", r.Item(0)}     ' temp used, r is uninitialized, show warning
                                                                                     ~
BC42104: Variable 'c' is used before it has been assigned a value. A null reference exception could result at runtime.
                Dim c As New List(Of String) From {"Hello World!", c.Item(0)}                       ' show warning
                                                                   ~
BC42104: Variable 'd' is used before it has been assigned a value. A null reference exception could result at runtime.
                Dim d As List(Of String) = New List(Of String)() From {"Hello World!", d.Item(0)}   ' show warning
                                                                                       ~
                                           </expected>)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub CollectionInitializerReferencingItself_2()
            Dim source = <![CDATA[
Imports System
Imports System.Collections.Generic

Module Program
    Sub Main(args As String())
        Dim x, y As New List(Of String)() From {"1", x.Item(0)}'BIND:"New List(Of String)() From {"1", x.Item(0)}"
        Dim z As New List(Of String)() From {"1", z.Item(0)}
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub System.Collections.Generic.List(Of System.String)..ctor()) (OperationKind.ObjectCreation, Type: System.Collections.Generic.List(Of System.String)) (Syntax: 'New List(Of ...  x.Item(0)}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Collections.Generic.List(Of System.String)) (Syntax: 'From {"1", x.Item(0)}')
      Initializers(2):
          IInvocationOperation ( Sub System.Collections.Generic.List(Of System.String).Add(item As System.String)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '"1"')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List(Of System.String), IsImplicit) (Syntax: 'New List(Of ...  x.Item(0)}')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '"1"')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "1") (Syntax: '"1"')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IInvocationOperation ( Sub System.Collections.Generic.List(Of System.String).Add(item As System.String)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'x.Item(0)')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List(Of System.String), IsImplicit) (Syntax: 'New List(Of ...  x.Item(0)}')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'x.Item(0)')
                  IPropertyReferenceOperation: Property System.Collections.Generic.List(Of System.String).Item(index As System.Int32) As System.String (OperationKind.PropertyReference, Type: System.String) (Syntax: 'x.Item(0)')
                    Instance Receiver: 
                      ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Collections.Generic.List(Of System.String)) (Syntax: 'x')
                    Arguments(1):
                        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: index) (OperationKind.Argument, Type: null) (Syntax: '0')
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42104: Variable 'x' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim x, y As New List(Of String)() From {"1", x.Item(0)}'BIND:"New List(Of String)() From {"1", x.Item(0)}"
                                                     ~
BC42104: Variable 'z' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim z As New List(Of String)() From {"1", z.Item(0)}
                                                  ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub CollectionInitializerCustomCollectionOptionalParameter()
            Dim source =
<compilation name="CollectionInitializerCustomCollection">
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections.Generic

Class Custom
    Private list As New List(Of String)()

    Public Function GetEnumerator() As CustomEnumerator
        Return New CustomEnumerator(list)
    End Function

    Public Sub add(p As String, optional p2 as String = " ")
        list.Add(p)
        list.Add(p2)
    End Sub

    Public Class CustomEnumerator
        Private list As list(Of String)
        Private index As Integer = -1

        Public Sub New(list As List(Of String))
            Me.list = list
        End Sub

        Public Function MoveNext() As Boolean
            If Me.index &lt; Me.list.Count - 1 Then
                index = index + 1
                Return True
            End If

            Return False
        End function

        Public ReadOnly Property Current As String
            Get
                Return Me.list(index)
            End Get
        End Property
    End Class
End Class

    Class C1
        Public Shared Sub Main()
            Dim c as Custom = New Custom() From {"Hello", {"World", "!"}}'BIND:"New Custom() From {"Hello", {"World", "!"}}"
            Output(c)
        End Sub

        Public Shared Sub Output(c as custom)
            For Each value In c
                Console.Write(value)
            Next
        End Sub
    End Class
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:=<![CDATA[
Hello World!
]]>)

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub Custom..ctor()) (OperationKind.ObjectCreation, Type: Custom) (Syntax: 'New Custom( ... rld", "!"}}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: Custom) (Syntax: 'From {"Hell ... rld", "!"}}')
      Initializers(2):
          IInvocationOperation ( Sub Custom.add(p As System.String, [p2 As System.String = " "])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '"Hello"')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Custom, IsImplicit) (Syntax: 'New Custom( ... rld", "!"}}')
            Arguments(2):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: p) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '"Hello"')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Hello") (Syntax: '"Hello"')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: p2) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '"Hello"')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: " ", IsImplicit) (Syntax: '"Hello"')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IInvocationOperation ( Sub Custom.add(p As System.String, [p2 As System.String = " "])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{"World", "!"}')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Custom, IsImplicit) (Syntax: 'New Custom( ... rld", "!"}}')
            Arguments(2):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: p) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '"World"')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "World") (Syntax: '"World"')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: p2) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '"!"')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "!") (Syntax: '"!"')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source.Value, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact()>
        Public Sub CollectionInitializerCustomCollectionParamArray()
            Dim source =
<compilation name="CollectionInitializerCustomCollection">
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections.Generic

Class Custom
    Private list As New List(Of String)()

    Public Function GetEnumerator() As CustomEnumerator
        Return New CustomEnumerator(list)
    End Function

    Public Sub add(paramarray p() As String)
        list.AddRange(p)
    End Sub

    Public Class CustomEnumerator
        Private list As list(Of String)
        Private index As Integer = -1

        Public Sub New(list As List(Of String))
            Me.list = list
        End Sub

        Public Function MoveNext() As Boolean
            If Me.index &lt; Me.list.Count - 1 Then
                index = index + 1
                Return True
            End If

            Return False
        End function

        Public ReadOnly Property Current As String
            Get
                Return Me.list(index)
            End Get
        End Property
    End Class
End Class

    Class C1
        Public Shared Sub Main()
            Dim c as Custom = New Custom() From {"Hello", {" ", "World"}, ({"!", "!", "!"})} 
            Output(c)
        End Sub

        Public Shared Sub Output(c as custom)
            For Each value In c
                Console.Write(value)
            Next
        End Sub
    End Class
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:=<![CDATA[
Hello World!!!
]]>)
        End Sub

        <Fact(), WorkItem(529787, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529787")>
        Public Sub GetCollectionInitializerSymbolInfo_01()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
 
class X 
    Inherits List(Of Integer)

    Sub Add(x As Integer)
    End Sub

    Sub Add(x As String)
    End Sub
 
    Shared Sub Main()
        Dim z = new X() From { String.Empty, 'BIND1:"String.Empty"
                                12}          'BIND2:"12"
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticInfo As SemanticInfoSummary = Nothing

            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim symbolInfo As SymbolInfo

            If True Then
                Dim node1 As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", 1)
                symbolInfo = semanticModel.GetCollectionInitializerSymbolInfo(node1)

                Assert.NotNull(symbolInfo.Symbol)
                Assert.Equal("Sub X.Add(x As System.String)", symbolInfo.Symbol.ToTestDisplayString())
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length)
            End If

            If True Then
                Dim node2 As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", 2)
                symbolInfo = semanticModel.GetCollectionInitializerSymbolInfo(node2)

                Assert.NotNull(symbolInfo.Symbol)
                Assert.Equal("Sub X.Add(x As System.Int32)", symbolInfo.Symbol.ToTestDisplayString())
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length)
            End If
        End Sub

        <Fact(), WorkItem(529787, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529787")>
        Public Sub GetCollectionInitializerSymbolInfo_02()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
 
class X 
    Inherits List(Of Integer)

    Sub Add(x As X)
    End Sub

    Sub Add(x As List(Of Byte))
    End Sub
 
    Shared Sub Main()
        Dim z = new X() From { String.Empty } 'BIND1:"String.Empty"
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticInfo As SemanticInfoSummary = Nothing

            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim symbolInfo As SymbolInfo

            Dim node1 As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", 1)
            symbolInfo = semanticModel.GetCollectionInitializerSymbolInfo(node1)

            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason)
            Assert.Equal(2, symbolInfo.CandidateSymbols.Length)
            AssertEx.Equal({"Sub X.Add(x As System.Collections.Generic.List(Of System.Byte))",
                          "Sub X.Add(x As X)"},
                         (symbolInfo.CandidateSymbols.Select(Function(s) s.ToTestDisplayString()).Order()).ToArray())
        End Sub

        <Fact(), WorkItem(529787, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529787")>
        Public Sub GetCollectionInitializerSymbolInfo_03()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
 
Class Base
    Implements IEnumerable(Of Integer)
End Class

class X 
    Inherits Base

    Protected Sub Add(x As String)
    End Sub
End Class

class Y
    Shared Sub Main()
        Dim z = new X() From { String.Empty } 'BIND1:"String.Empty"
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticInfo As SemanticInfoSummary = Nothing

            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim symbolInfo As SymbolInfo

            Dim node1 As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", 1)
            symbolInfo = semanticModel.GetCollectionInitializerSymbolInfo(node1)

            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)
        End Sub

        <Fact(), WorkItem(529787, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529787")>
        Public Sub GetCollectionInitializerSymbolInfo_04()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
 
class X 
    Inherits List(Of Integer)

    Sub Add(x As String, y As Integer)
    End Sub
 
    Shared Sub Main()
        Dim z = new X() From { {String.Empty, 12} } 'BIND1:"{String.Empty, 12}"
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticInfo As SemanticInfoSummary = Nothing

            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim symbolInfo As SymbolInfo

            Dim node1 As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", 1)
            symbolInfo = semanticModel.GetCollectionInitializerSymbolInfo(node1)

            Assert.NotNull(symbolInfo.Symbol)
            Assert.Equal("Sub X.Add(x As System.String, y As System.Int32)", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)
        End Sub

        <Fact(), WorkItem(529787, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529787")>
        Public Sub GetCollectionInitializerSymbolInfo_05()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
 
class X 
    Inherits List(Of Integer)

    Sub Add(x As String, y As Integer)
    End Sub
 
    Shared Sub Main()
        Dim z = new X() From { {String.Empty, 'BIND1:"String.Empty"
                                12} }         'BIND2:"12"
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticInfo As SemanticInfoSummary = Nothing

            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim symbolInfo As SymbolInfo

            For i As Integer = 1 To 2
                Dim node1 As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", i)
                symbolInfo = semanticModel.GetCollectionInitializerSymbolInfo(node1)

                Assert.Null(symbolInfo.Symbol)
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length)
            Next
        End Sub

        <Fact()>
        <WorkItem(12983, "https://github.com/dotnet/roslyn/issues/12983")>
        Public Sub GetCollectionInitializerSymbolInfo_06()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections.Generic

Class C1
    Public Shared Sub Main()
        Dim list1 = new List(Of String)
        Dim list2 = new List(Of String)()
        
        Dim list3 = new List(Of String) With { .Count = 3 }
        Dim list4 = new List(Of String)() With { .Count = 3 }
        
        Dim list5 = new List(Of String)  From { 1, 2, 3 }
        Dim list6 = new List(Of String)() From { 1, 2, 3 }
    End Sub
End Class        
    </file>
</compilation>)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim nodes = tree.GetRoot().DescendantNodes().OfType(Of GenericNameSyntax)().ToArray()
            Assert.Equal(6, nodes.Length)

            For Each name In nodes
                Assert.Equal("List(Of String)", name.ToString())
                Assert.Equal("System.Collections.Generic.List(Of System.String)", semanticModel.GetSymbolInfo(name).Symbol.ToTestDisplayString())
                Assert.Null(semanticModel.GetTypeInfo(name).Type)
            Next
        End Sub

        <Fact()>
        <WorkItem(27034, "https://github.com/dotnet/roslyn/issues/27034")>
        Public Sub LateBoundCollectionInitializer()
            Dim source =
    <compilation>
        <file name="a.vb">
Imports System
Imports System.Collections
Imports System.Collections.Generic

Module Mod1
    Sub Main(args() As String)
        Dim c As New C()
        c.M(1)
    End Sub

    Class C
        Implements IEnumerable(Of Integer)

        Public Sub M(a As Object)
            Dim c = New C From {a}
        End Sub

        Public Function GetEnumerator() As IEnumerator(Of Integer) Implements IEnumerable(Of Integer).GetEnumerator
            Throw New NotImplementedException()
        End Function

        Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
            Throw New NotImplementedException()
        End Function

        Public Sub Add(i As Integer)
            Console.WriteLine("Called Integer Add")
        End Sub

        Public Sub Add(l As Long)
            Console.WriteLine("Called Long Add")
        End Sub
    End Class
End Module
        </file>
    </compilation>

            Dim verifier = CompileAndVerify(source, expectedOutput:="Called Integer Add")
            verifier.VerifyIL("Mod1.C.M", <![CDATA[
{
  // Code size       62 (0x3e)
  .maxstack  10
  .locals init (Mod1.C V_0,
                Object() V_1,
                Boolean() V_2)
  IL_0000:  newobj     "Sub Mod1.C..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldnull
  IL_0008:  ldstr      "Add"
  IL_000d:  ldc.i4.1
  IL_000e:  newarr     "Object"
  IL_0013:  dup
  IL_0014:  ldc.i4.0
  IL_0015:  ldarg.1
  IL_0016:  stelem.ref
  IL_0017:  dup
  IL_0018:  stloc.1
  IL_0019:  ldnull
  IL_001a:  ldnull
  IL_001b:  ldc.i4.1
  IL_001c:  newarr     "Boolean"
  IL_0021:  dup
  IL_0022:  ldc.i4.0
  IL_0023:  ldc.i4.1
  IL_0024:  stelem.i1
  IL_0025:  dup
  IL_0026:  stloc.2
  IL_0027:  ldc.i4.1
  IL_0028:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateCall(Object, System.Type, String, Object(), String(), System.Type(), Boolean(), Boolean) As Object"
  IL_002d:  pop
  IL_002e:  ldloc.2
  IL_002f:  ldc.i4.0
  IL_0030:  ldelem.u1
  IL_0031:  brfalse.s  IL_003d
  IL_0033:  ldloc.1
  IL_0034:  ldc.i4.0
  IL_0035:  ldelem.ref
  IL_0036:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_003b:  starg.s    V_1
  IL_003d:  ret
}
]]>)
        End Sub

    End Class
End Namespace
