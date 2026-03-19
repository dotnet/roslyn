' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class OperatorsTests
        Inherits BasicTestBase

        <Fact>
        Public Sub Operators1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation name="C">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Linq

Public Class A1
    Public Shared Operator +(x As A1) As A1 'BIND1:"A1"
        Return x
    End Operator

    Public Shared Operator -(x As A1) As A1 'BIND2:"A1"
        Return x
    End Operator

    Public Shared Operator Not(x As A1) As A1 'BIND3:"A1"
        Return x
    End Operator

    Public Shared Operator IsTrue(x As A1) As Boolean 'BIND4:"A1"
        Return True
    End Operator

    Public Shared Operator IsFalse(x As A1) As Boolean 'BIND5:"A1"
        Return False
    End Operator

    Public Shared Operator +(x As A1, y As A1) As A1 'BIND6:"A1"
        Return x
    End Operator

    Public Shared Operator -(x As A1, y As A1) As A1 'BIND7:"A1"
        Return x
    End Operator

    Public Shared Operator *(x As A1, y As A1) As A1 'BIND8:"A1"
        Return x
    End Operator

    Public Shared Operator /(x As A1, y As A1) As A1 'BIND9:"A1"
        Return x
    End Operator

    Public Shared Operator \(x As A1, y As A1) As A1 'BIND10:"A1"
        Return x
    End Operator

    Public Shared Operator Mod(x As A1, y As A1) As A1 'BIND11:"A1"
        Return x
    End Operator

    Public Shared Operator ^(x As A1, y As A1) As A1 'BIND12:"A1"
        Return x
    End Operator

    Public Shared Operator =(x As A1, y As A1) As A1 'BIND13:"A1"
        Return x
    End Operator

    Public Shared Operator <>(x As A1, y As A1) As A1 'BIND14:"A1"
        Return x
    End Operator

    Public Shared Operator <(x As A1, y As A1) As A1 'BIND15:"A1"
        Return x
    End Operator

    Public Shared Operator >(x As A1, y As A1) As A1 'BIND16:"A1"
        Return x
    End Operator

    Public Shared Operator <=(x As A1, y As A1) As A1 'BIND17:"A1"
        Return x
    End Operator

    Public Shared Operator >=(x As A1, y As A1) As A1 'BIND18:"A1"
        Return x
    End Operator

    Public Shared Operator Like(x As A1, y As A1) As A1 'BIND19:"A1"
        Return x
    End Operator

    Public Shared Operator &(x As A1, y As A1) As A1 'BIND20:"A1"
        Return x
    End Operator

    Public Shared Operator And(x As A1, y As A1) As A1 'BIND21:"A1"
        Return x
    End Operator

    Public Shared Operator Or(x As A1, y As A1) As A1 'BIND22:"A1"
        Return x
    End Operator

    Public Shared Operator Xor(x As A1, y As A1) As A1 'BIND23:"A1"
        Return x
    End Operator

    Public Shared Operator <<(x As A1, y As Integer) As A1 'BIND24:"A1"
        Return x
    End Operator

    Public Shared Operator >>(x As A1, y As Integer) As A1 'BIND25:"A1"
        Return x
    End Operator

    Public Shared Widening Operator CType(x As A1) As Integer 'BIND26:"A1"
        Return 0
    End Operator
 
    Public Shared Narrowing Operator CType(x As A1) As Byte 'BIND27:"A1"
        Return 0
    End Operator
End Class

Module Program
    Sub Main()
        Dim t As System.Type = GetType(A1)

        For Each m In t.GetMethods(Reflection.BindingFlags.DeclaredOnly Or Reflection.BindingFlags.Static Or Reflection.BindingFlags.Public).OrderBy(Function(m1) m1.Name)
            System.Console.WriteLine("{0} - {1}", m.Name, m.IsSpecialName)
        Next
    End Sub
End Module
    ]]></file>
</compilation>, options:=TestOptions.ReleaseExe, references:={SystemCoreRef})

            Dim model As VBSemanticModel = GetSemanticModel(compilation, "a.vb")
            Dim operatorSyntax As OperatorStatementSyntax
            Dim op As MethodSymbol

            Dim baseLine() As BaseLine = {
                New BaseLine(MethodKind.UserDefinedOperator, "op_UnaryPlus"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_UnaryNegation"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_OnesComplement"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_True"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_False"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_Addition"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_Subtraction"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_Multiply"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_Division"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_IntegerDivision"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_Modulus"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_Exponent"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_Equality"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_Inequality"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_LessThan"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_GreaterThan"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_LessThanOrEqual"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_GreaterThanOrEqual"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_Like"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_Concatenate"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_BitwiseAnd"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_BitwiseOr"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_ExclusiveOr"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_LeftShift"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_RightShift"),
                New BaseLine(MethodKind.Conversion, "op_Implicit"),
                New BaseLine(MethodKind.Conversion, "op_Explicit")
            }

            For i As Integer = 1 To 27
                operatorSyntax = GetEnclosingOperatorStatement(CompilationUtils.FindBindingText(Of VisualBasicSyntaxNode)(compilation, "a.vb", i))
                op = DirectCast(model.GetDeclaredSymbol(operatorSyntax), MethodSymbol)
                Assert.Equal(baseLine(i - 1).Kind, op.MethodKind)
                Assert.Equal(baseLine(i - 1).Name, op.Name)
                Assert.Equal(Accessibility.Public, op.DeclaredAccessibility)
                Assert.True(op.IsShared)
                Assert.False(op.IsOverloads)
                Assert.False(op.ShadowsExplicitly)

                Dim syntax As String = operatorSyntax.ToString()
                Assert.Equal(syntax, op.ToDisplayString())
                Assert.Equal("Function A1." & baseLine(i - 1).Name &
                             syntax.Substring(syntax.IndexOf("("c)).
                                Replace("Boolean", "System.Boolean").
                                Replace("Integer", "System.Int32").
                                Replace("Byte", "System.Byte"), op.ToTestDisplayString())
            Next

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
</expected>)

            CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
op_Addition - True
op_BitwiseAnd - True
op_BitwiseOr - True
op_Concatenate - True
op_Division - True
op_Equality - True
op_ExclusiveOr - True
op_Explicit - True
op_Exponent - True
op_False - True
op_GreaterThan - True
op_GreaterThanOrEqual - True
op_Implicit - True
op_Inequality - True
op_IntegerDivision - True
op_LeftShift - True
op_LessThan - True
op_LessThanOrEqual - True
op_Like - True
op_Modulus - True
op_Multiply - True
op_OnesComplement - True
op_RightShift - True
op_Subtraction - True
op_True - True
op_UnaryNegation - True
op_UnaryPlus - True
]]>)
        End Sub

        Private Structure BaseLine
            Public ReadOnly Kind As MethodKind
            Public ReadOnly Name As String

            Public Sub New(kind As MethodKind, name As String)
                Me.Kind = kind
                Me.Name = name
            End Sub
        End Structure

        Public Shared Function GetEnclosingOperatorStatement(node As VisualBasicSyntaxNode) As OperatorStatementSyntax
            Do
                If node.Kind = SyntaxKind.OperatorStatement Then
                    Return DirectCast(node, OperatorStatementSyntax)
                End If

                node = node.Parent
            Loop
        End Function

        <Fact>
        Public Sub Operators2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="C">
    <file name="a.vb"><![CDATA[
Public Class A1
    Private Shared Operator +(x As A1) As A1 'BIND1:"A1"
        Return x
    End Operator

    Protected Shared Operator -(x As A1) As A1 'BIND2:"A1"
        Return x
    End Operator

    Friend Shared Operator Not(x As A1) As A1 'BIND3:"A1"
        Return x
    End Operator

    Protected Friend Shared Operator IsTrue(x As A1) As Boolean 'BIND4:"A1"
        Return True
    End Operator

    Private Protected Shared Operator IsFalse(x As A1) As Boolean 'BIND5:"A1"
        Return False
    End Operator

    Public Operator +(x As A1, y As A1) As A1 'BIND6:"A1"
        Return x
    End Operator

    Public Shared Widening Operator -(x As A1, y As A1) As A1 'BIND7:"A1"
        Return x
    End Operator

    Public Shared Narrowing Operator *(x As A1, y As A1) As A1 'BIND8:"A1"
        Return x
    End Operator

    Public Shared Widening Narrowing Operator /(x As A1, y As A1) As A1 'BIND9:"A1"
        Return x
    End Operator

    Public Shared Narrowing Widening Operator \(x As A1, y As A1) As A1 'BIND10:"A1"
        Return x
    End Operator

    Public Overloads Shared Operator Mod(x As A1, y As A1) As A1 'BIND11:"A1"
        Return x
    End Operator

    Public Shadows Shared Operator ^(x As A1, y As A1) As A1 'BIND12:"A1"
        Return x
    End Operator

    Public Overloads Shadows Shared Operator =(x As A1, y As A1) As A1 'BIND13:"A1"
        Return x
    End Operator

    Public Shadows Overloads Shared Operator <>(x As A1, y As A1) As A1 'BIND14:"A1"
        Return x
    End Operator

    Public Shared Operator CType(x As A1) As Integer 'BIND15:"A1"
        Return 0
    End Operator
 
    Public Shared Widening Narrowing Operator CType(x As A1) As Byte 'BIND16:"A1"
        Return 0
    End Operator

    Public Shared Narrowing Widening Operator CType(x As Integer) As A1 'BIND17:"A1"
        Return Nothing
    End Operator

    Default Public Shared Operator <<(x As A1, y As Integer) As A1 'BIND18:"A1"
        Return x
    End Operator

    Public Shared ReadOnly Operator >>(x As A1, y As Integer) As A1 'BIND19:"A1"
        Return x
    End Operator
End Class

Module A2
    Public Shared Operator Xor(x As A2, y As A2) As A2 'BIND20:"A2"
        Return x
    End Operator
End Module

Class A3
    Shared Widening Operator CType(x As A3) As Byte
        Return 0
    End Operator
End Class
    ]]></file>
</compilation>, parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15))
            Dim model As VBSemanticModel = GetSemanticModel(compilation, "a.vb")
            Dim operatorSyntax As OperatorStatementSyntax
            Dim op As MethodSymbol

            Dim baseLine() As BaseLine = {
                New BaseLine(MethodKind.UserDefinedOperator, "op_UnaryPlus"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_UnaryNegation"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_OnesComplement"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_True"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_False"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_Addition"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_Subtraction"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_Multiply"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_Division"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_IntegerDivision"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_Modulus"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_Exponent"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_Equality"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_Inequality"),
                New BaseLine(MethodKind.Conversion, "op_Explicit"),
                New BaseLine(MethodKind.Conversion, "op_Implicit"),
                New BaseLine(MethodKind.Conversion, "op_Explicit"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_LeftShift"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_RightShift"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_ExclusiveOr")
            }

            For i As Integer = 1 To 20
                operatorSyntax = GetEnclosingOperatorStatement(CompilationUtils.FindBindingText(Of VisualBasicSyntaxNode)(compilation, "a.vb", i))
                op = DirectCast(model.GetDeclaredSymbol(operatorSyntax), MethodSymbol)
                Assert.Equal(baseLine(i - 1).Kind, op.MethodKind)
                Assert.Equal(baseLine(i - 1).Name, op.Name)
                Assert.Equal(Accessibility.Public, op.DeclaredAccessibility)
                Assert.True(op.IsShared)

                Assert.Equal(i = 11 OrElse i = 13, op.IsOverloads)
                Assert.Equal(i = 12 OrElse i = 14, op.ShadowsExplicitly)
            Next

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC33011: Operators must be declared 'Public'.
    Private Shared Operator +(x As A1) As A1 'BIND1:"A1"
    ~~~~~~~
BC33011: Operators must be declared 'Public'.
    Protected Shared Operator -(x As A1) As A1 'BIND2:"A1"
    ~~~~~~~~~
BC33011: Operators must be declared 'Public'.
    Friend Shared Operator Not(x As A1) As A1 'BIND3:"A1"
    ~~~~~~
BC33011: Operators must be declared 'Public'.
    Protected Friend Shared Operator IsTrue(x As A1) As Boolean 'BIND4:"A1"
    ~~~~~~~~~~~~~~~~
BC33011: Operators must be declared 'Public'.
    Private Protected Shared Operator IsFalse(x As A1) As Boolean 'BIND5:"A1"
    ~~~~~~~
BC36716: Visual Basic 15.0 does not support Private Protected.
    Private Protected Shared Operator IsFalse(x As A1) As Boolean 'BIND5:"A1"
            ~~~~~~~~~
BC33012: Operators must be declared 'Shared'.
    Public Operator +(x As A1, y As A1) As A1 'BIND6:"A1"
                    ~
BC33019: Only conversion operators can be declared 'Widening'.
    Public Shared Widening Operator -(x As A1, y As A1) As A1 'BIND7:"A1"
                  ~~~~~~~~
BC33019: Only conversion operators can be declared 'Narrowing'.
    Public Shared Narrowing Operator *(x As A1, y As A1) As A1 'BIND8:"A1"
                  ~~~~~~~~~
BC33019: Only conversion operators can be declared 'Widening'.
    Public Shared Widening Narrowing Operator /(x As A1, y As A1) As A1 'BIND9:"A1"
                  ~~~~~~~~
BC33001: 'Widening' and 'Narrowing' cannot be combined.
    Public Shared Widening Narrowing Operator /(x As A1, y As A1) As A1 'BIND9:"A1"
                           ~~~~~~~~~
BC33019: Only conversion operators can be declared 'Narrowing'.
    Public Shared Narrowing Widening Operator \(x As A1, y As A1) As A1 'BIND10:"A1"
                  ~~~~~~~~~
BC33001: 'Widening' and 'Narrowing' cannot be combined.
    Public Shared Narrowing Widening Operator \(x As A1, y As A1) As A1 'BIND10:"A1"
                            ~~~~~~~~
BC31408: 'Overloads' and 'Shadows' cannot be combined.
    Public Overloads Shadows Shared Operator =(x As A1, y As A1) As A1 'BIND13:"A1"
                     ~~~~~~~
BC31408: 'Overloads' and 'Shadows' cannot be combined.
    Public Shadows Overloads Shared Operator <>(x As A1, y As A1) As A1 'BIND14:"A1"
                   ~~~~~~~~~
BC33017: Conversion operators must be declared either 'Widening' or 'Narrowing'.
    Public Shared Operator CType(x As A1) As Integer 'BIND15:"A1"
                           ~~~~~
BC33001: 'Widening' and 'Narrowing' cannot be combined.
    Public Shared Widening Narrowing Operator CType(x As A1) As Byte 'BIND16:"A1"
                           ~~~~~~~~~
BC33001: 'Widening' and 'Narrowing' cannot be combined.
    Public Shared Narrowing Widening Operator CType(x As Integer) As A1 'BIND17:"A1"
                            ~~~~~~~~
BC33013: Operators cannot be declared 'Default'.
    Default Public Shared Operator <<(x As A1, y As Integer) As A1 'BIND18:"A1"
    ~~~~~~~
BC33013: Operators cannot be declared 'ReadOnly'.
    Public Shared ReadOnly Operator >>(x As A1, y As Integer) As A1 'BIND19:"A1"
                  ~~~~~~~~
BC33018: Operators cannot be declared in modules.
    Public Shared Operator Xor(x As A2, y As A2) As A2 'BIND20:"A2"
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30371: Module 'A2' cannot be used as a type.
    Public Shared Operator Xor(x As A2, y As A2) As A2 'BIND20:"A2"
                                    ~~
BC30371: Module 'A2' cannot be used as a type.
    Public Shared Operator Xor(x As A2, y As A2) As A2 'BIND20:"A2"
                                             ~~
BC30371: Module 'A2' cannot be used as a type.
    Public Shared Operator Xor(x As A2, y As A2) As A2 'BIND20:"A2"
                                                    ~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub Operators3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="a.vb"><![CDATA[
Public Class A1
    Public Shared Operator +() As A1 'BIND1:"A1"
        Return Nothing
    End Operator

    Public Shared Operator -() As A1 'BIND2:"A1"
        Return Nothing
    End Operator

    Public Shared Operator Not() As A1 'BIND3:"A1"
        Return Nothing
    End Operator

    Public Shared Operator IsTrue(x As A1, y As A1) As Boolean 'BIND4:"A1"
        Return True
    End Operator

    Public Shared Operator IsFalse(x As A1, y As A1) As Boolean 'BIND5:"A1"
        Return False
    End Operator

    Public Shared Operator +(x As A1, y As A1, z As A1) As A1 'BIND6:"A1"
        Return x
    End Operator

    Public Shared Operator -(x As A1, y As A1, z As A1) As A1 'BIND7:"A1"
        Return x
    End Operator

    Public Shared Operator *(x As A1, y As A1, z As A1) As A1 'BIND8:"A1"
        Return x
    End Operator

    Public Shared Operator /() As A1 'BIND9:"A1"
        Return Nothing
    End Operator

    Public Shared Widening Operator CType() As A1 'BIND10:"A1"
        Return Nothing
    End Operator

    Public Shared Narrowing Operator CType(x As A1, y As A1) As Byte 'BIND11:"A1"
        Return 0
    End Operator
End Class
    ]]></file>
</compilation>)
            Dim model As VBSemanticModel = GetSemanticModel(compilation, "a.vb")
            Dim operatorSyntax As OperatorStatementSyntax
            Dim op As MethodSymbol

            Dim baseLine() As BaseLine = {
                New BaseLine(MethodKind.UserDefinedOperator, "op_UnaryPlus"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_UnaryNegation"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_OnesComplement"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_True"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_False"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_Addition"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_Subtraction"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_Multiply"),
                New BaseLine(MethodKind.UserDefinedOperator, "op_Division"),
                New BaseLine(MethodKind.Conversion, "op_Implicit"),
                New BaseLine(MethodKind.Conversion, "op_Explicit")
            }

            For i As Integer = 1 To 11
                operatorSyntax = GetEnclosingOperatorStatement(CompilationUtils.FindBindingText(Of VisualBasicSyntaxNode)(compilation, "a.vb", i))
                op = DirectCast(model.GetDeclaredSymbol(operatorSyntax), MethodSymbol)
                Assert.Equal(baseLine(i - 1).Kind, op.MethodKind)
                Assert.Equal(baseLine(i - 1).Name, op.Name)
                Assert.Equal(Accessibility.Public, op.DeclaredAccessibility)
                Assert.True(op.IsShared)
                Assert.False(op.IsOverloads)
                Assert.False(op.ShadowsExplicitly)
            Next

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC33016: Operator '+' must have either one or two parameters.
    Public Shared Operator +() As A1 'BIND1:"A1"
                           ~
BC33016: Operator '-' must have either one or two parameters.
    Public Shared Operator -() As A1 'BIND2:"A1"
                           ~
BC33014: Operator 'Not' must have one parameter.
    Public Shared Operator Not() As A1 'BIND3:"A1"
                           ~~~
BC33014: Operator 'IsTrue' must have one parameter.
    Public Shared Operator IsTrue(x As A1, y As A1) As Boolean 'BIND4:"A1"
                           ~~~~~~
BC33014: Operator 'IsFalse' must have one parameter.
    Public Shared Operator IsFalse(x As A1, y As A1) As Boolean 'BIND5:"A1"
                           ~~~~~~~
BC33016: Operator '+' must have either one or two parameters.
    Public Shared Operator +(x As A1, y As A1, z As A1) As A1 'BIND6:"A1"
                           ~
BC33016: Operator '-' must have either one or two parameters.
    Public Shared Operator -(x As A1, y As A1, z As A1) As A1 'BIND7:"A1"
                           ~
BC33015: Operator '*' must have two parameters.
    Public Shared Operator *(x As A1, y As A1, z As A1) As A1 'BIND8:"A1"
                           ~
BC33015: Operator '/' must have two parameters.
    Public Shared Operator /() As A1 'BIND9:"A1"
                           ~
BC33014: Operator 'CType' must have one parameter.
    Public Shared Widening Operator CType() As A1 'BIND10:"A1"
                                    ~~~~~
BC33014: Operator 'CType' must have one parameter.
    Public Shared Narrowing Operator CType(x As A1, y As A1) As Byte 'BIND11:"A1"
                                     ~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Operators4()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="a.vb"><![CDATA[
Public Class A1
    Public Shared Operator IsTrue(x As A1) As Byte
        Return Nothing
    End Operator

    Public Shared Operator IsFalse(x As A1) As Byte
        Return Nothing
    End Operator

    Public Shared Operator <<(x As A1, y As Integer) As A1
        Return x
    End Operator

    Public Shared Operator <<(x As A1, y As Integer?) As A1
        Return x
    End Operator

    Public Shared Operator <<(x As A1, y As Short) As A1
        Return x
    End Operator

    Public Shared Operator >>(x As A1, y As Integer) As A1
        Return x
    End Operator

    Public Shared Operator >>(x As A1, y As Integer?) As A1
        Return x
    End Operator

    Public Shared Operator >>(x As A1, y As Short) As A1
        Return x
    End Operator

    Public Shared Operator +(x As Integer) As A1
        Return Nothing
    End Operator

    Public Shared Operator -(x As Integer) As A1
        Return Nothing
    End Operator

    Public Shared Operator Not(x As Integer) As A1
        Return Nothing
    End Operator

    Public Shared Operator *(x As Byte, y As Integer) As A1
        Return Nothing
    End Operator

    Public Shared Operator +(x As Byte, y As Integer) As A1
        Return Nothing
    End Operator

    Public Shared Operator -(x As Byte, y As Integer) As A1
        Return Nothing
    End Operator

    Public Shared Widening Operator CType(x As Byte) As Integer
        Return 0
    End Operator
End Class

Class A2(Of T)
    Public Shared Widening Operator CType(x As A2(Of T)) As Integer
        Return 0
    End Operator

    Public Shared Widening Operator CType(x As A2(Of Integer)) As Integer
        Return 0
    End Operator
End Class

Class A3
    Public Shared Operator IsTrue(x As A3) As Boolean?
        Return Nothing
    End Operator

    Public Shared Operator IsFalse(x As A3) As Boolean?
        Return Nothing
    End Operator
End Class
    ]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC33023: Operator 'IsTrue' must have a return type of Boolean.
    Public Shared Operator IsTrue(x As A1) As Byte
                           ~~~~~~
BC33023: Operator 'IsFalse' must have a return type of Boolean.
    Public Shared Operator IsFalse(x As A1) As Byte
                           ~~~~~~~
BC33041: Operator '<<' must have a second parameter of type 'Integer' or 'Integer?'.
    Public Shared Operator <<(x As A1, y As Short) As A1
                           ~~
BC33041: Operator '>>' must have a second parameter of type 'Integer' or 'Integer?'.
    Public Shared Operator >>(x As A1, y As Short) As A1
                           ~~
BC33020: Parameter of this unary operator must be of the containing type 'A1'.
    Public Shared Operator +(x As Integer) As A1
                           ~
BC33020: Parameter of this unary operator must be of the containing type 'A1'.
    Public Shared Operator -(x As Integer) As A1
                           ~
BC33020: Parameter of this unary operator must be of the containing type 'A1'.
    Public Shared Operator Not(x As Integer) As A1
                           ~~~
BC33021: At least one parameter of this binary operator must be of the containing type 'A1'.
    Public Shared Operator *(x As Byte, y As Integer) As A1
                           ~
BC33021: At least one parameter of this binary operator must be of the containing type 'A1'.
    Public Shared Operator +(x As Byte, y As Integer) As A1
                           ~
BC33021: At least one parameter of this binary operator must be of the containing type 'A1'.
    Public Shared Operator -(x As Byte, y As Integer) As A1
                           ~
BC33022: Either the parameter type or the return type of this conversion operator must be of the containing type 'A1'.
    Public Shared Widening Operator CType(x As Byte) As Integer
                                    ~~~~~
BC33022: Either the parameter type or the return type of this conversion operator must be of the containing type 'A2(Of T)'.
    Public Shared Widening Operator CType(x As A2(Of Integer)) As Integer
                                    ~~~~~
BC33023: Operator 'IsTrue' must have a return type of Boolean.
    Public Shared Operator IsTrue(x As A3) As Boolean?
                           ~~~~~~
BC33023: Operator 'IsFalse' must have a return type of Boolean.
    Public Shared Operator IsFalse(x As A3) As Boolean?
                           ~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub Operators5()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="a.vb"><![CDATA[
Public Class A1
    Public Shared Widening Operator CType(x As A1) As Object
        Return Nothing
    End Operator

    Public Shared Widening Operator CType(x As Object) As A1
        Return Nothing
    End Operator
End Class

Public Class A11
    Public Shared Widening Operator CType(x As A11) As System.IComparable
        Return Nothing
    End Operator

    Public Shared Widening Operator CType(x As System.IComparable) As A11
        Return Nothing
    End Operator
End Class

Public Class A12
    Public Shared Widening Operator CType(x As A12) As A12
        Return Nothing
    End Operator
End Class

Structure A2
    Public Shared Widening Operator CType(x As A2) As A2
        Return Nothing
    End Operator

    Public Shared Widening Operator CType(x As A2?) As A2
        Return Nothing
    End Operator
End Structure

Structure A22
    Public Shared Widening Operator CType(x As A22) As A22?
        Return Nothing
    End Operator

    Public Shared Widening Operator CType(x As A22?) As A22?
        Return Nothing
    End Operator
End Structure

Class A3
    Inherits A1

    Public Overloads Shared Widening Operator CType(x As A3) As A1
        Return Nothing
    End Operator

    Public Overloads Shared Widening Operator CType(x As A1) As A3
        Return Nothing
    End Operator
End Class

Class A33
    Public Overloads Shared Widening Operator CType(x As A33) As A4
        Return Nothing
    End Operator

    Public Overloads Shared Widening Operator CType(x As A4) As A33
        Return Nothing
    End Operator
End Class

Class A4
    Inherits A33
End Class


Class A5(Of T As A5(Of T, S), S As A3)
    Inherits A3

    Public Overloads Shared Widening Operator CType(x As A5(Of T, S)) As T
        Return Nothing
    End Operator

    Public Overloads Shared Widening Operator CType(x As T) As A5(Of T, S)
        Return Nothing
    End Operator
End Class

Class A55(Of T As A5(Of T, S), S As A3)
    Inherits A3

    Public Overloads Shared Widening Operator CType(x As A55(Of T, S)) As S
        Return Nothing
    End Operator

    Public Overloads Shared Widening Operator CType(x As S) As A55(Of T, S)
        Return Nothing
    End Operator
End Class
    ]]></file>
</compilation>)
            '<![CDATA[
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC33028: Conversion operators cannot convert to Object.
    Public Shared Widening Operator CType(x As A1) As Object
                                    ~~~~~
BC33032: Conversion operators cannot convert from Object.
    Public Shared Widening Operator CType(x As Object) As A1
                                    ~~~~~
BC33025: Conversion operators cannot convert to an interface type.
    Public Shared Widening Operator CType(x As A11) As System.IComparable
                                    ~~~~~
BC33029: Conversion operators cannot convert from an interface type.
    Public Shared Widening Operator CType(x As System.IComparable) As A11
                                    ~~~~~
BC33024: Conversion operators cannot convert from a type to the same type.
    Public Shared Widening Operator CType(x As A12) As A12
                                    ~~~~~
BC33024: Conversion operators cannot convert from a type to the same type.
    Public Shared Widening Operator CType(x As A2) As A2
                                    ~~~~~
BC33024: Conversion operators cannot convert from a type to the same type.
    Public Shared Widening Operator CType(x As A2?) As A2
                                    ~~~~~
BC33024: Conversion operators cannot convert from a type to the same type.
    Public Shared Widening Operator CType(x As A22) As A22?
                                    ~~~~~
BC33024: Conversion operators cannot convert from a type to the same type.
    Public Shared Widening Operator CType(x As A22?) As A22?
                                    ~~~~~
BC33026: Conversion operators cannot convert from a type to its base type.
    Public Overloads Shared Widening Operator CType(x As A3) As A1
                                              ~~~~~
BC33030: Conversion operators cannot convert from a base type.
    Public Overloads Shared Widening Operator CType(x As A1) As A3
                                              ~~~~~
BC33027: Conversion operators cannot convert from a type to its derived type.
    Public Overloads Shared Widening Operator CType(x As A33) As A4
                                              ~~~~~
BC33031: Conversion operators cannot convert from a derived type.
    Public Overloads Shared Widening Operator CType(x As A4) As A33
                                              ~~~~~
BC33027: Conversion operators cannot convert from a type to its derived type.
    Public Overloads Shared Widening Operator CType(x As A5(Of T, S)) As T
                                              ~~~~~
BC33031: Conversion operators cannot convert from a derived type.
    Public Overloads Shared Widening Operator CType(x As T) As A5(Of T, S)
                                              ~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Operators6()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="a.vb"><![CDATA[
Public Class A12
    Public Shared Operator +(Optional x As A12 = Nothing, y As A12) As A12 'BIND1:"A12"
        Return Nothing
    End Operator

    Public Shared Operator -(ParamArray x As A12(), y As A12) As A12 'BIND2:"A12"
        Return Nothing
    End Operator

    Public Shared Operator *(ByRef x As A12, y As A12) As A12 'BIND3:"A12"
        Return Nothing
    End Operator
End Class
    ]]></file>
</compilation>)
            '<![CDATA[
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC33010: 'operator' parameters cannot be declared 'Optional'.
    Public Shared Operator +(Optional x As A12 = Nothing, y As A12) As A12 'BIND1:"A12"
                             ~~~~~~~~
BC33009: 'operator' parameters cannot be declared 'ParamArray'.
    Public Shared Operator -(ParamArray x As A12(), y As A12) As A12 'BIND2:"A12"
                             ~~~~~~~~~~
BC30651: operator parameters cannot be declared 'ByRef'.
    Public Shared Operator *(ByRef x As A12, y As A12) As A12 'BIND3:"A12"
                             ~~~~~
</expected>)

            Dim model As VBSemanticModel = GetSemanticModel(compilation, "a.vb")
            Dim operatorSyntax As OperatorStatementSyntax
            Dim op As MethodSymbol

            For i As Integer = 1 To 3
                operatorSyntax = GetEnclosingOperatorStatement(CompilationUtils.FindBindingText(Of VisualBasicSyntaxNode)(compilation, "a.vb", i))
                op = DirectCast(model.GetDeclaredSymbol(operatorSyntax), MethodSymbol)
                Dim param = op.Parameters(0)
                Assert.False(param.IsByRef)
                Assert.False(param.IsParamArray)
                Assert.False(param.IsOptional)
                Assert.False(param.HasExplicitDefaultValue)
            Next
        End Sub

        <Fact()>
        Public Sub Operators7()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="a.vb"><![CDATA[
Public Class A1
    Public Shared Narrowing Operator CType(x As A1) As Integer
        Return Nothing
    End Operator

    Public Shared Narrowing Operator CType(x As A1) As Byte
        Return Nothing
    End Operator
End Class

Public Class A2
    Public Shared Widening Operator CType(x As A2) As Integer
        Return Nothing
    End Operator

    Public Shared Widening Operator CType(x As A2) As Byte
        Return Nothing
    End Operator
End Class

Public Class A3
    Public Shared Widening Operator CType(x As A3) As Integer
        Return Nothing
    End Operator

    Public Shared Function op_Implicit(x As A3) As Byte
        Return Nothing
    End Function
End Class

Public Class A4
    Public Shared Function op_Implicit(x As A4) As Byte
        Return Nothing
    End Function

    Public Shared Widening Operator CType(x As A4) As Integer
        Return Nothing
    End Operator
End Class

Public Class A5
    Public Shared Widening Operator CType(x As A5) As Integer
        Return Nothing
    End Operator

    Public Sub op_Implicit(x As A3)
    End Sub
End Class

Public Class A6
    Public Sub op_Implicit(x As A3)
    End Sub

    Public Shared Widening Operator CType(x As A6) As Integer
        Return Nothing
    End Operator
End Class

Public Class A7
    Class op_Implicit
    End Class

    Public Shared Widening Operator CType(x As A7) As Integer
        Return Nothing
    End Operator
End Class

Public Class A8
    Public Shared Widening Operator CType(x As A8) As Integer
        Return Nothing
    End Operator

    Class op_Implicit
    End Class
End Class

Public Class A9
    Public Shared Widening Operator CType(x As A9) As Integer
        Return Nothing
    End Operator

    Public Shared Widening Operator CType(x As A9) As Integer
        Return Nothing
    End Operator
End Class

Public Class A10
    Public Shared Widening Operator CType(x As A10) As Integer
        Return Nothing
    End Operator

    Public Shared Narrowing Operator CType(x As A10) As Integer
        Return Nothing
    End Operator
End Class

Public Class A11
    Public Shared Widening Operator CType(x As A11) As Integer
        Return Nothing
    End Operator

    Public Sub [CType](x As A11)
    End Sub
End Class

Public Class A12
    Public Shared Operator +(x As A12, y As A12) As A12
        Return Nothing
    End Operator
    Public Shared Operator +(x As A12, y As A12) As Integer
        Return Nothing
    End Operator
End Class

Public Class A13
    Public Shared Narrowing Operator CType(x As A13) As Integer
        Return Nothing
    End Operator

    Public Shared Widening Operator CType(x As A13) As Integer
        Return Nothing
    End Operator
End Class

Public Class A14
    Public Shared Operator \(x As A14, y As A14) As A14
        Return Nothing
    End Operator

    Sub Test1()
        Dim x = Me / Me
        Dim y = op_IntegerDivision(Me, Me)
    End Sub

    Sub Test2()
        op_Division()
    End Sub

    Sub op_Division()
    End Sub

    Public Shared Widening Operator CType(x As A14) As Integer
        Return Nothing
    End Operator

    Public Shared Narrowing Operator CType(x As A14) As Byte
        Return Nothing
    End Operator

    Sub Test()
        Dim x = op_Implicit(Me)
        Dim y = op_Explicit(Me)
    End Sub
End Class
    ]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC31060: operator 'CType' implicitly defines 'op_Implicit', which conflicts with a member of the same name in class 'A3'.
    Public Shared Widening Operator CType(x As A3) As Integer
                                    ~~~~~
BC31061: function 'op_Implicit' conflicts with a member implicitly declared for operator 'CType' in class 'A4'.
    Public Shared Function op_Implicit(x As A4) As Byte
                           ~~~~~~~~~~~
BC31060: operator 'CType' implicitly defines 'op_Implicit', which conflicts with a member of the same name in class 'A5'.
    Public Shared Widening Operator CType(x As A5) As Integer
                                    ~~~~~
BC31061: sub 'op_Implicit' conflicts with a member implicitly declared for operator 'CType' in class 'A6'.
    Public Sub op_Implicit(x As A3)
               ~~~~~~~~~~~
BC31061: class 'op_Implicit' conflicts with a member implicitly declared for operator 'CType' in class 'A7'.
    Class op_Implicit
          ~~~~~~~~~~~
BC31061: class 'op_Implicit' conflicts with a member implicitly declared for operator 'CType' in class 'A8'.
    Class op_Implicit
          ~~~~~~~~~~~
BC30269: 'Public Shared Widening Operator CType(x As A9) As Integer' has multiple definitions with identical signatures.
    Public Shared Widening Operator CType(x As A9) As Integer
                                    ~~~~~
BC30269: 'Public Shared Widening Operator CType(x As A10) As Integer' has multiple definitions with identical signatures.
    Public Shared Widening Operator CType(x As A10) As Integer
                                    ~~~~~
BC30301: 'Public Shared Operator +(x As A12, y As A12) As A12' and 'Public Shared Operator +(x As A12, y As A12) As Integer' cannot overload each other because they differ only by return types.
    Public Shared Operator +(x As A12, y As A12) As A12
                           ~
BC30269: 'Public Shared Narrowing Operator CType(x As A13) As Integer' has multiple definitions with identical signatures.
    Public Shared Narrowing Operator CType(x As A13) As Integer
                                     ~~~~~
BC30452: Operator '/' is not defined for types 'A14' and 'A14'.
        Dim x = Me / Me
                ~~~~~~~
</expected>)
        End Sub

        <Fact(), WorkItem(34872, "https://github.com/dotnet/roslyn/issues/34872")>
        Public Sub GenericOperatorVoidConversion()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="a.vb"><![CDATA[
Public Class C(Of T)
    Public Shared Widening Operator CType(t As T) As C(Of T)
        Return New C(Of T)
	End Operator

    Public Sub M()
    End Sub

    Public Function M2() As C(Of Object)
		Return M()
	End Function
End Class
    ]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30491: Expression does not produce a value.
		Return M()
         ~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub Operators8()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="a.vb"><![CDATA[
Public Class A1
    Public Shared Operator =(x As A1, y As A1) As Boolean
        Return Nothing
    End Operator

    Public Shared Operator <>(x As A1, y As A1) As Boolean?
        Return Nothing
    End Operator

    Public Shared Operator <(x As A1, y As A1) As Boolean
        Return Nothing
    End Operator

    Public Shared Operator >(x As A1, y As A1) As Boolean?
        Return Nothing
    End Operator

    Public Shared Operator <=(x As A1, y As A1) As Boolean
        Return Nothing
    End Operator

    Public Shared Operator >=(x As A1, y As A1) As Boolean?
        Return Nothing
    End Operator

    Public Shared Operator IsTrue(y As A1) As Boolean
        Return Nothing
    End Operator
End Class

Public Class A2
    Public Shared Operator IsFalse(y As A2) As Boolean
        Return Nothing
    End Operator
End Class

Public Class A3
    Public Shared Operator +(x As A3, y As A3) As Boolean
        op_Addition = True
        Return Nothing
    End Operator

    Public Shared Operator +(x As A3, op_Addition As System.Guid) As Boolean
        op_Addition = New System.Guid()
        Return Nothing
    End Operator
End Class
    ]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC33033: Matching '<>' operator is required for 'Public Shared Operator =(x As A1, y As A1) As Boolean'.
    Public Shared Operator =(x As A1, y As A1) As Boolean
                           ~
BC33033: Matching '=' operator is required for 'Public Shared Operator <>(x As A1, y As A1) As Boolean?'.
    Public Shared Operator <>(x As A1, y As A1) As Boolean?
                           ~~
BC33033: Matching '>' operator is required for 'Public Shared Operator <(x As A1, y As A1) As Boolean'.
    Public Shared Operator <(x As A1, y As A1) As Boolean
                           ~
BC33033: Matching '<' operator is required for 'Public Shared Operator >(x As A1, y As A1) As Boolean?'.
    Public Shared Operator >(x As A1, y As A1) As Boolean?
                           ~
BC33033: Matching '>=' operator is required for 'Public Shared Operator <=(x As A1, y As A1) As Boolean'.
    Public Shared Operator <=(x As A1, y As A1) As Boolean
                           ~~
BC33033: Matching '<=' operator is required for 'Public Shared Operator >=(x As A1, y As A1) As Boolean?'.
    Public Shared Operator >=(x As A1, y As A1) As Boolean?
                           ~~
BC33033: Matching 'IsFalse' operator is required for 'Public Shared Operator IsTrue(y As A1) As Boolean'.
    Public Shared Operator IsTrue(y As A1) As Boolean
                           ~~~~~~
BC33033: Matching 'IsTrue' operator is required for 'Public Shared Operator IsFalse(y As A2) As Boolean'.
    Public Shared Operator IsFalse(y As A2) As Boolean
                           ~~~~~~~
BC30516: Overload resolution failed because no accessible '+' accepts this number of arguments.
        op_Addition = True
        ~~~~~~~~~~~
]]></expected>)
        End Sub

        ''' <summary>
        ''' Operators AndAlso and OrElse require that operators
        ''' And, Or, IsTrue, and IsFalse are defined on the same type.
        ''' </summary>
        <Fact()>
        Public Sub UserDefinedShortCircuitingOperators_IsTrueAndIsFalseOnBaseType()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Class A(Of T)
    Public Shared Operator IsTrue(o As A(Of T)) As Boolean
        Return True
    End Operator
    Public Shared Operator IsFalse(o As A(Of T)) As Boolean
        Return False
    End Operator
End Class
Class B
    Inherits A(Of Object)
    Public Shared Operator And(x As B, y As B) As B
        Return x
    End Operator
End Class
Class C
    Inherits B
    Public Shared Operator Or(x As C, y As C) As C
        Return x
    End Operator
End Class
Module M
    Sub M(x As C, y As C)
        If x AndAlso y Then
        End If
        If x OrElse y Then
        End If
    End Sub
End Module
    ]]></file>
</compilation>)
            comp.AssertTheseDiagnostics(
<expected><![CDATA[
BC30452: Operator 'AndAlso' is not defined for types 'C' and 'C'.
        If x AndAlso y Then
           ~~~~~~~~~~~
BC30452: Operator 'OrElse' is not defined for types 'C' and 'C'.
        If x OrElse y Then
           ~~~~~~~~~~
]]></expected>)
        End Sub

        ''' <summary>
        ''' Operators AndAlso and OrElse require that operators
        ''' And, Or, IsTrue, and IsFalse are defined on the same type.
        ''' </summary>
        <Fact()>
        Public Sub UserDefinedShortCircuitingOperators_IsTrueAndIsFalseOnDerivedType()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Class A(Of T)
    Public Shared Operator Or(x As A(Of T), y As A(Of T)) As A(Of T)
        Return x
    End Operator
End Class
Class B
    Inherits A(Of Object)
    Public Shared Operator And(x As B, y As B) As B
        Return x
    End Operator
End Class
Class C
    Inherits B
    Public Shared Operator IsTrue(o As C) As Boolean
        Return True
    End Operator
    Public Shared Operator IsFalse(o As C) As Boolean
        Return False
    End Operator
End Class
Module M
    Sub M(x As C, y As C)
        If x AndAlso y Then
        End If
        If x OrElse y Then
        End If
    End Sub
End Module
    ]]></file>
</compilation>)
            comp.AssertTheseDiagnostics(
<expected><![CDATA[
BC33035: Type 'B' must define operator 'IsFalse' to be used in a 'AndAlso' expression.
        If x AndAlso y Then
           ~~~~~~~~~~~
BC33035: Type 'A(Of Object)' must define operator 'IsTrue' to be used in a 'OrElse' expression.
        If x OrElse y Then
           ~~~~~~~~~~
]]></expected>)
        End Sub

    End Class

End Namespace

