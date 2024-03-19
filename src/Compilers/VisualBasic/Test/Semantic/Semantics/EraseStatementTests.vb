' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class EraseStatementTests
        Inherits BasicTestBase

        <Fact>
        <CompilerTrait(CompilerFeature.IOperation)>
        Public Sub Simple()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x(1) As Integer
        Dim y As System.Array = New Integer(1) {}
        Dim z As Object = New Integer(1) {}

        Erase x
        Erase y, z

        System.Console.WriteLine(x Is Nothing)
        System.Console.WriteLine(y Is Nothing)
        System.Console.WriteLine(z Is Nothing)
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef,
                                expectedOutput:=
            <![CDATA[
True
True
True
]]>)
            Dim compilation = verifier.Compilation

            Dim tree = compilation.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of EraseStatementSyntax)().Last()

            Assert.Equal("Erase y, z", node.ToString())

            compilation.VerifyOperationTree(node, expectedOperationTree:=
            <![CDATA[
IOperation:  (OperationKind.None, Type: null) (Syntax: 'Erase y, z')
  Children(2):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Array, IsImplicit) (Syntax: 'y')
        Left: 
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Array) (Syntax: 'y')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Array, Constant: null, IsImplicit) (Syntax: 'y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsImplicit) (Syntax: 'y')
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'z')
        Left: 
          ILocalReferenceOperation: z (OperationKind.LocalReference, Type: System.Object) (Syntax: 'z')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'z')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsImplicit) (Syntax: 'z')
]]>.Value)
        End Sub

        <Fact>
        Public Sub Flow()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x() As Integer

        x(0).ToString()
    End Sub

    Sub Test()
        Dim y() As Integer

        Erase y
        y(0).ToString()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertTheseDiagnostics(compilation,
<expected>
BC42104: Variable 'x' is used before it has been assigned a value. A null reference exception could result at runtime.
        x(0).ToString()
        ~
</expected>)
        End Sub

        <Fact>
        Public Sub Errors1()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x As String = ""
        Erase x

        Dim y As String = ""
        Dim z As Integer = 1

        Erase y, z
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertTheseDiagnostics(compilation,
<expected>
BC30049: 'Erase' statement requires an array.
        Erase x
              ~
BC30049: 'Erase' statement requires an array.
        Erase y, z
              ~
BC30049: 'Erase' statement requires an array.
        Erase y, z
                 ~
</expected>)
        End Sub

        <Fact>
        Public Sub Errors2()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Erase x1
        Erase x2
        Erase y
        Erase z
    End Sub

    ReadOnly Property x1 As String
        Get
            Return Nothing
        End Get
    End Property

    ReadOnly Property x2 As Integer()
        Get
            Return Nothing
        End Get
    End Property

    Property y As Integer()

    WriteOnly Property z As Integer()
        Set(value As Integer())
        End Set
    End Property
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertTheseDiagnostics(compilation,
<expected>
BC30049: 'Erase' statement requires an array.
        Erase x1
              ~~
BC30526: Property 'x2' is 'ReadOnly'.
        Erase x2
              ~~
</expected>)
        End Sub

        <Fact>
        Public Sub Errors3()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Erase x
    End Sub

    Function x() As Integer()
        Return Nothing
    End Function
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertTheseDiagnostics(compilation,
<expected>
BC30068: Expression is a value and therefore cannot be the target of an assignment.
        Erase x
              ~
</expected>)
        End Sub

    End Class
End Namespace
