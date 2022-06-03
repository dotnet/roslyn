' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Public Class SelectCaseTests
        Inherits BasicTestBase
        <Fact()>
        Public Sub SelectCaseExpression_NothingLiteral()
            Dim compilation = CreateCompilationWithMscorlib40(
        <compilation>
            <file name="a.vb"><![CDATA[
Public Module M
    Sub SelectCaseExpression()
        Select Case Nothing'BIND:"Nothing"
        End Select
    End Sub
End Module
    ]]></file>
        </compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of LiteralExpressionSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Equal("System.Object", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningNothingLiteral, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.None, semanticSummary.CandidateReason)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.True(semanticSummary.ConstantValue.HasValue)
            Assert.Null(semanticSummary.ConstantValue.Value)
        End Sub

        <Fact()>
        Public Sub SelectCaseExpression_Literal()
            Dim compilation = CreateCompilationWithMscorlib40(
        <compilation>
            <file name="a.vb"><![CDATA[
Public Module M
    Sub SelectCaseExpression()
        Select Case 1.1'BIND:"1.1"
        End Select
    End Sub
End Module
    ]]></file>
        </compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of LiteralExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Double", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Double", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.None, semanticSummary.CandidateReason)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.True(semanticSummary.ConstantValue.HasValue)
            Assert.Equal(1.1, semanticSummary.ConstantValue.Value)
        End Sub

        <Fact()>
        Public Sub SelectCaseExpression_Local_IdentifierNameSyntax()
            Dim compilation = CreateCompilationWithMscorlib40(
        <compilation>
            <file name="a.vb"><![CDATA[
Imports System
Module M1
    Sub SelectCaseExpression()
        Dim number As Integer = 0
        Select Case number'BIND:"number"
        End Select
    End Sub
Ehd Module
    ]]></file>
        </compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.Int32", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Int32", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("number As System.Int32", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Local, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub SelectCaseExpression_MethodCall_InvocationExpressionSyntax()
            Dim compilation = CreateCompilationWithMscorlib40(
        <compilation>
            <file name="a.vb"><![CDATA[
Imports System
Module M1
    Function Goo() As Integer
        Console.WriteLine("Goo")
        Return 0
    End Function

    Sub SelectCaseExpression()
        Select Case Goo()'BIND:"Goo()"
        End Select
    End Sub
End Module
    ]]></file>
        </compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Int32", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Int32", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("Function M1.Goo() As System.Int32", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub SelectCaseExpression_MethodCall_IdentifierNameSyntax()
            Dim compilation = CreateCompilationWithMscorlib40(
        <compilation>
            <file name="a.vb"><![CDATA[
Imports System
Module M1
    Function Goo() As Integer
        Console.WriteLine("Goo")
        Return 0
    End Function

    Sub SelectCaseExpression()
        Select Case Goo()'BIND:"Goo"
        End Select
    End Sub
End Module
    ]]></file>
        </compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("Function M1.Goo() As System.Int32", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(1, semanticSummary.MemberGroup.Length)
            Dim sortedMethodGroup = semanticSummary.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Function M1.Goo() As System.Int32", sortedMethodGroup(0).ToTestDisplayString())

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub SelectCaseExpression_Lambda()
            Dim compilation = CreateCompilationWithMscorlib40(
        <compilation>
            <file name="a.vb"><![CDATA[
Public Module M
    Sub SelectCaseExpression()
        Select Case (Function(arg) arg Is Nothing)'BIND:"Function(arg) arg Is Nothing"
        End Select
    End Sub
End Module
    ]]></file>
        </compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of SingleLineLambdaExpressionSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Equal("Function <generated method>(arg As System.Object) As System.Boolean", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Delegate, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Widening Or ConversionKind.Lambda, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("Function (arg As System.Object) As System.Boolean", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub SelectCaseExpression_ParenthesizedLambda()
            Dim compilation = CreateCompilationWithMscorlib40(
        <compilation>
            <file name="a.vb"><![CDATA[
Public Module M
    Sub SelectCaseExpression()
        Select Case (Function(arg) arg Is Nothing)'BIND:"(Function(arg) arg Is Nothing)"
        End Select
    End Sub
End Module
    ]]></file>
        </compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of ParenthesizedExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("Function <generated method>(arg As System.Object) As System.Boolean", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Delegate, semanticSummary.Type.TypeKind)
            Assert.Equal("Function <generated method>(arg As System.Object) As System.Boolean", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Delegate, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.None, semanticSummary.CandidateReason)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub SelectCaseExpression_Error_NotAValue_InvocationExpressionSyntax()
            Dim compilation = CreateCompilationWithMscorlib40(
        <compilation>
            <file name="a.vb"><![CDATA[
Imports System
Module M1
    Sub Goo()
    End Sub

    Sub SelectCaseExpression(number As Integer)
        Select Case Goo()'BIND:"Goo()"
        End Select
    End Sub
End Module
    ]]></file>
        </compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Void", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Void", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.NotAValue, semanticSummary.CandidateReason)
            Assert.Equal(1, semanticSummary.CandidateSymbols.Length)
            Dim sortedCandidates = semanticSummary.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub M1.Goo()", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(0).Kind)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub SelectCaseExpression_Error_NotAValue_IdentifierNameSyntax()
            Dim compilation = CreateCompilationWithMscorlib40(
        <compilation>
            <file name="a.vb"><![CDATA[
Imports System
Module M1
    Sub Goo()
    End Sub

    Sub SelectCaseExpression(number As Integer)
        Select Case Goo'BIND:"Goo"
        End Select
    End Sub
End Module
    ]]></file>
        </compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.Void", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Void", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.NotAValue, semanticSummary.CandidateReason)
            Assert.Equal(1, semanticSummary.CandidateSymbols.Length)
            Dim sortedCandidates = semanticSummary.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub M1.Goo()", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(0).Kind)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub SelectCaseExpression_Error_OverloadResolutionFailure()
            Dim compilation = CreateCompilationWithMscorlib40(
        <compilation>
            <file name="a.vb"><![CDATA[
Imports System
Module M1
    Sub Goo(i As Integer)
    End Sub

    Sub SelectCaseExpression(number As Integer)
        Select Case Goo'BIND:"Goo"
        End Select
    End Sub
End Module
    ]]></file>
        </compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Equal("System.Void", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticSummary.CandidateReason)
            Assert.Equal(1, semanticSummary.CandidateSymbols.Length)
            Dim sortedCandidates = semanticSummary.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub M1.Goo(i As System.Int32)", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(0).Kind)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(1, semanticSummary.MemberGroup.Length)
            Dim sortedMethodGroup = semanticSummary.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub M1.Goo(i As System.Int32)", sortedMethodGroup(0).ToTestDisplayString())

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub SelectCase_RelationalCaseClauseExpression_Literal()
            Dim compilation = CreateCompilationWithMscorlib40(
        <compilation>
            <file name="a.vb"><![CDATA[
Imports System
Module M1
    Sub RelationalCaseClauseExpression(number As Integer)
        Select Case number
            Case Is < 1'BIND:"1"
        End Select
    End Sub
End Module
    ]]></file>
        </compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of LiteralExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Int32", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Int32", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.None, semanticSummary.CandidateReason)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.True(semanticSummary.ConstantValue.HasValue)
            Assert.Equal(1, semanticSummary.ConstantValue.Value)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub SelectCase_RelationalCaseClauseExpression_IOperation()
            Dim source = <![CDATA[
Class C
    Function LessThan(i As Integer, j As Integer) As Boolean
        Select Case i
            Case Is < j'BIND:"Is < j"
                Return True
            Case Else
                Return False
        End Select
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IRelationalCaseClauseOperation (Relational operator kind: BinaryOperatorKind.LessThan) (CaseKind.Relational) (OperationKind.CaseClause, Type: null) (Syntax: 'Is < j')
  Value: 
    IParameterReferenceOperation: j (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'j')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of RelationalCaseClauseSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub SelectCase_RangeCaseClauseExpression_IOperation()
            Dim source = <![CDATA[
Class C
    Function InRange(i As Integer, min As Integer, max As Integer) As Boolean
        Select Case i
            Case min To max'BIND:"min To max"
                Return True
            Case Else
                Return False
        End Select
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IRangeCaseClauseOperation (CaseKind.Range) (OperationKind.CaseClause, Type: null) (Syntax: 'min To max')
  Min: 
    IParameterReferenceOperation: min (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'min')
  Max: 
    IParameterReferenceOperation: max (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'max')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of RangeCaseClauseSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact()>
        Public Sub SelectCase_RangeCaseClauseExpression_MethodCall()
            Dim compilation = CreateCompilationWithMscorlib40(
        <compilation>
            <file name="a.vb"><![CDATA[
Imports System
Module M1
    Function Goo() As Integer
        Return 0
    End Function

    Sub RangeCaseClauseExpression(number As Integer)
        Select Case number
            Case Goo() To 1'BIND:"Goo()"
        End Select
    End Sub
End Module
    ]]></file>
        </compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Int32", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Int32", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("Function M1.Goo() As System.Int32", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub SelectCase_SimpleCaseClauseExpression_DateTime()
            Dim compilation = CreateCompilationWithMscorlib40(
        <compilation>
            <file name="a.vb"><![CDATA[
Imports System
Module M1
    Function Goo() As Integer
        Return 0
    End Function

    Sub SimpleCaseClauseExpression(number As Integer)
        Select Case number
            Case #8/13/2002 12:14 PM#'BIND:"#8/13/2002 12:14 PM#"
        End Select
    End Sub
End Module
    ]]></file>
        </compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of LiteralExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.DateTime", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Int32", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.DelegateRelaxationLevelNone, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.None, semanticSummary.CandidateReason)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.True(semanticSummary.ConstantValue.HasValue)
            Assert.Equal(#8/13/2002 12:14:00 PM#, semanticSummary.ConstantValue.Value)
        End Sub

        <WorkItem(543098, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543098")>
        <Fact()>
        Public Sub SelectCase_BoundLocal()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class Program
    Sub Test()
        Dim i As Integer = 10
        Select Case i'BIND:"i"
            Case NewMethod()
                Console.Write(5)
        End Select
    End Sub

    Private Shared Function NewMethod() As Integer
        Return 5
    End Function
End Class
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.Int32", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Int32", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("i As System.Int32", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Local, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <WorkItem(543387, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543387")>
        <Fact()>
        Public Sub SelectCase_AnonymousLambda()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Module Program
    Sub Main()
        Select Case Nothing
            Case Function() 5
                System.Console.WriteLine("Failed")
            Case Else
                System.Console.WriteLine("Succeeded")
        End Select
    End Sub
End Module
    ]]></file>
</compilation>, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
Succeeded
]]>)

            AssertTheseDiagnostics(compilation,
<expected>
BC42036: Operands of type Object used in expressions for 'Select', 'Case' statements; runtime errors could occur.
        Select Case Nothing
                    ~~~~~~~
BC42016: Implicit conversion from 'Object' to 'Boolean'.
            Case Function() 5
                 ~~~~~~~~~~~~
</expected>)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub SelectCase_AnonymousLambda_OperationTree()
            Dim source = <![CDATA[
Module Program
    Sub Main()
        Select Case Nothing'BIND:"Select Case Nothing"
            Case Function() 5
                System.Console.WriteLine("Failed")
            Case Else
                System.Console.WriteLine("Succeeded")
        End Select
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ISwitchOperation (2 cases, Exit Label Id: 0) (OperationKind.Switch, Type: null) (Syntax: 'Select Case ... End Select')
  Switch expression: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'Nothing')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
  Sections:
      ISwitchCaseOperation (1 case clauses, 1 statements) (OperationKind.SwitchCase, Type: null) (Syntax: 'Case Functi ... e("Failed")')
          Clauses:
              ISingleValueCaseClauseOperation (CaseKind.SingleValue) (OperationKind.CaseClause, Type: null) (Syntax: 'Function() 5')
                Value: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'Function() 5')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IDelegateCreationOperation (OperationKind.DelegateCreation, Type: Function <generated method>() As System.Int32, IsImplicit) (Syntax: 'Function() 5')
                        Target: 
                          IAnonymousFunctionOperation (Symbol: Function () As System.Int32) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Function() 5')
                            IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Function() 5')
                              Locals: Local_1: <anonymous local> As System.Int32
                              IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '5')
                                ReturnedValue: 
                                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
                              ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Function() 5')
                                Statement: 
                                  null
                              IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Function() 5')
                                ReturnedValue: 
                                  ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'Function() 5')
          Body:
              IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Case Functi ... e("Failed")')
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... e("Failed")')
                  Expression: 
                    IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... e("Failed")')
                      Instance Receiver: 
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '"Failed"')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Failed") (Syntax: '"Failed"')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      ISwitchCaseOperation (1 case clauses, 1 statements) (OperationKind.SwitchCase, Type: null) (Syntax: 'Case Else ... Succeeded")')
          Clauses:
              IDefaultCaseClauseOperation (CaseKind.Default) (OperationKind.CaseClause, Type: null) (Syntax: 'Case Else')
          Body:
              IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Case Else ... Succeeded")')
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... Succeeded")')
                  Expression: 
                    IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... Succeeded")')
                      Instance Receiver: 
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '"Succeeded"')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Succeeded") (Syntax: '"Succeeded"')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SelectBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <WorkItem(948019, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/948019")>
        <Fact()>
        Public Sub Bug948019_01()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Public Sub M(day As DayOfWeek)
        Dim day2 = day
        Select Case day 'BIND:"day"
            Case DayOfWeek.A
            Case
        End Select
    End Sub
    Enum DayOfWeek
        A
        B
    End Enum
End Class
    ]]></file>
</compilation>)

            Dim node = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb")
            Dim semanticModel = compilation.GetSemanticModel(node.SyntaxTree)

            Dim typeInfo = semanticModel.GetTypeInfo(node)

            Assert.Equal("C.DayOfWeek", typeInfo.Type.ToTestDisplayString())
            Assert.Equal("C.DayOfWeek", typeInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Identity, semanticModel.GetConversion(node).Kind)

            Dim symbolInfo = semanticModel.GetSymbolInfo(node)

            Assert.Equal("day As C.DayOfWeek", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Parameter, symbolInfo.Symbol.Kind)
        End Sub

        <WorkItem(948019, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/948019")>
        <Fact()>
        Public Sub Bug948019_02()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Public Sub M(day As DayOfWeek)
        Dim day2 = day
        Select Case day 'BIND:"day"
            Case DayOfWeek.A
            Case 2
        End Select
    End Sub
    Enum DayOfWeek
        A
        B
    End Enum
End Class
    ]]></file>
</compilation>)

            Dim node = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb")
            Dim semanticModel = compilation.GetSemanticModel(node.SyntaxTree)

            Dim typeInfo = semanticModel.GetTypeInfo(node)

            Assert.Equal("C.DayOfWeek", typeInfo.Type.ToTestDisplayString())
            Assert.Equal("C.DayOfWeek", typeInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Identity, semanticModel.GetConversion(node).Kind)

            Dim symbolInfo = semanticModel.GetSymbolInfo(node)

            Assert.Equal("day As C.DayOfWeek", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Parameter, symbolInfo.Symbol.Kind)
        End Sub

    End Class
End Namespace
