' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Globalization
Imports System.Linq.Expressions
Imports System.Text

Namespace Global

    Public Class ExprLambdaTest
        Public Shared Sub DCheck(Of T)(e As Expression(Of T), expected As String)
            Check(e.Dump(), expected)
        End Sub

        Public Shared Sub Check(Of T)(e As Expression(Of Func(Of T)), expected As String)
            Check(e.Dump(), expected)
        End Sub

        Public Shared Sub Check(Of T1, T2)(e As Expression(Of Func(Of T1, T2)), expected As String)
            Check(e.Dump(), expected)
        End Sub

        Public Shared Sub Check(Of T1, T2, T3)(e As Expression(Of Func(Of T1, T2, T3)), expected As String)
            Check(e.Dump(), expected)
        End Sub

        Public Shared Sub Check(Of T1, T2, T3, T4)(e As Expression(Of Func(Of T1, T2, T3, T4)), expected As String)
            Check(e.Dump(), expected)
        End Sub

        Private Shared Sub Check(actual As String, expected As String)
            If actual <> expected Then
                Console.WriteLine("FAIL")
                Console.WriteLine("expected: '" & expected & "'")
                Console.WriteLine("actual:   '" & actual & "'")
                Console.WriteLine()
            End If
        End Sub
    End Class

    Public Module ExpressionExtensions
        <System.Runtime.CompilerServices.Extension>
        Public Function Dump(Of T)(self As Expression(Of T)) As String
            self.Compile()
            Return ExpressionPrinter.Print(self)
        End Function
    End Module

    Friend Class ExpressionPrinter
        Inherits System.Linq.Expressions.ExpressionVisitor

        Private ReadOnly _s As StringBuilder = New StringBuilder()

        Private _indent As String = ""
        Private ReadOnly _indentStep As String = "  "

        Public Shared Function GetCultureInvariantString(val As Object) As String
            If val Is Nothing Then
                Return Nothing
            End If

            Dim vType = val.GetType()
            Dim valStr = val.ToString()
            If vType Is GetType(DateTime) Then
                valStr = DirectCast(val, DateTime).ToString("M/d/yyyy h:mm:ss tt", CultureInfo.InvariantCulture)
            ElseIf vType Is GetType(Single) Then
                valStr = DirectCast(val, Single).ToString(CultureInfo.InvariantCulture)
            ElseIf vType Is GetType(Double) Then
                valStr = DirectCast(val, Double).ToString(CultureInfo.InvariantCulture)
            ElseIf vType Is GetType(Decimal) Then
                valStr = DirectCast(val, Decimal).ToString(CultureInfo.InvariantCulture)
            End If

            Return valStr
        End Function

        Public Shared Function Print(e As Expression) As String
            Dim p = New ExpressionPrinter()
            p.Visit(e)
            Return p._s.ToString()
        End Function

        Public Overrides Function Visit(node As Expression) As Expression
            Dim indent = Me._indent
            If node Is Nothing Then
                _s.AppendLine(Me._indent + "<NULL>")
                Return Nothing
            End If

            _s.AppendLine(indent + node.NodeType.ToString() + "(")
            Me._indent = indent + _indentStep
            MyBase.Visit(node)
            _s.AppendLine(indent + _indentStep + "type: " + node.Type.ToString())
            _s.AppendLine(indent + ")")
            Return Nothing
        End Function

        Protected Overrides Function VisitMemberBinding(node As MemberBinding) As MemberBinding
            If node Is Nothing Then
                _s.AppendLine(Me._indent + "<NULL>")
                Return Nothing
            End If

            Return MyBase.VisitMemberBinding(node)
        End Function

        Protected Overrides Function VisitMemberMemberBinding(node As MemberMemberBinding) As MemberMemberBinding
            Dim indent = Me._indent
            _s.AppendLine(indent + "MemberMemberBinding(")
            _s.AppendLine(indent + _indentStep + "member: " + node.Member.ToString())
            For Each b In node.Bindings
                Me._indent = indent + _indentStep
                VisitMemberBinding(b)
            Next

            _s.AppendLine(indent + ")")
            Return Nothing
        End Function

        Protected Overrides Function VisitMemberListBinding(node As MemberListBinding) As MemberListBinding
            Dim indent = Me._indent
            _s.AppendLine(indent + "MemberListBinding(")
            _s.AppendLine(indent + _indentStep + "member: " + node.Member.ToString())
            For Each i In node.Initializers
                Me._indent = indent + _indentStep
                VisitElementInit(i)
            Next

            _s.AppendLine(indent + ")")
            Return Nothing
        End Function

        Protected Overrides Function VisitMemberAssignment(node As MemberAssignment) As MemberAssignment
            Dim indent = Me._indent
            _s.AppendLine(indent + "MemberAssignment(")
            _s.AppendLine(indent + _indentStep + "member: " + node.Member.ToString())
            _s.AppendLine(indent + _indentStep + "expression: {")
            Me._indent = indent + _indentStep + _indentStep
            Visit(node.Expression)
            _s.AppendLine(indent + _indentStep + "}")
            _s.AppendLine(indent + ")")
            Return Nothing
        End Function

        Protected Overrides Function VisitMemberInit(node As MemberInitExpression) As Expression
            Dim indent = Me._indent
            _s.AppendLine(indent + "NewExpression(")
            Me._indent = indent + _indentStep
            Visit(node.NewExpression)
            _s.AppendLine(indent + ")")
            _s.AppendLine(indent + "bindings:")
            For Each b In node.Bindings
                Me._indent = indent + _indentStep
                VisitMemberBinding(b)
            Next
            Return Nothing
        End Function

        Protected Overrides Function VisitBinary(node As BinaryExpression) As Expression
            Dim indent = Me._indent

            Me._indent = indent
            Visit(node.Left)

            Me._indent = indent
            Visit(node.Right)

            If node.Conversion IsNot Nothing Then
                _s.AppendLine(indent + "conversion:")
                Me._indent = indent + _indentStep
                Visit(node.Conversion)
            End If

            If node.IsLifted Then
                _s.AppendLine(indent + "Lifted")
            End If

            If node.IsLiftedToNull Then
                _s.AppendLine(indent + "LiftedToNull")
            End If

            If node.Method IsNot Nothing Then
                _s.AppendLine(indent + "method: " + node.Method.ToString() + " in " + node.Method.DeclaringType.ToString())
            End If

            Return Nothing
        End Function

        Protected Overrides Function VisitConditional(node As ConditionalExpression) As Expression
            Dim indent = Me._indent
            Visit(node.Test)
            Me._indent = indent
            Visit(node.IfTrue)
            Me._indent = indent
            Visit(node.IfFalse)
            Return Nothing
        End Function

        Protected Overrides Function VisitConstant(node As ConstantExpression) As Expression
            _s.AppendLine(_indent + If(node.Value Is Nothing, "null", GetCultureInvariantString(node.Value)))
            Return Nothing
        End Function

        Protected Overrides Function VisitDefault(node As DefaultExpression) As Expression
            Return Nothing
        End Function

        Protected Overrides Function VisitIndex(node As IndexExpression) As Expression
            Dim indent = Me._indent
            Visit(node.[Object])

            Me._indent = indent
            _s.AppendLine(indent + "indices(")
            Dim n As Integer = node.Arguments.Count
            For i = 0 To n - 1
                Me._indent = indent + _indentStep
                Visit(node.Arguments(i))
            Next

            _s.AppendLine(indent + ")")

            If node.Indexer IsNot Nothing Then
                _s.AppendLine(indent + "indexer: " + node.Indexer.ToString())
            End If

            Return Nothing
        End Function

        Protected Overrides Function VisitInvocation(node As InvocationExpression) As Expression
            Dim indent = Me._indent
            Visit(node.Expression)
            _s.AppendLine(indent + "(")
            Dim n As Integer = node.Arguments.Count
            For i = 0 To n - 1
                Me._indent = indent + _indentStep
                Visit(node.Arguments(i))
            Next

            _s.AppendLine(indent + ")")
            Return Nothing
        End Function

        Protected Overrides Function VisitLambda(Of T)(node As Expression(Of T)) As Expression
            Dim indent = Me._indent
            Dim n As Integer = node.Parameters.Count
            For i = 0 To n - 1
                Me._indent = indent
                Visit(node.Parameters(i))
            Next

            If node.Name IsNot Nothing Then
                _s.AppendLine(indent + node.Name)
            End If

            _s.AppendLine(indent + "body {")
            Me._indent = indent + _indentStep
            Visit(node.Body)
            _s.AppendLine(indent + "}")

            If node.ReturnType IsNot Nothing Then
                _s.AppendLine(indent + "return type: " + node.ReturnType.ToString())
            End If

            If node.TailCall Then
                _s.AppendLine(indent + "tail call")
            End If

            Return Nothing
        End Function

        Protected Overrides Function VisitParameter(node As ParameterExpression) As Expression
            _s.Append(Me._indent + node.Name)
            If node.IsByRef Then
                _s.Append(" ByRef")
            End If
            _s.AppendLine()
            Return Nothing
        End Function

        Protected Overrides Function VisitListInit(node As ListInitExpression) As Expression
            Dim indent = Me._indent
            Visit(node.NewExpression)

            _s.AppendLine(indent + "{")
            Dim n As Integer = node.Initializers.Count
            For i = 0 To n - 1
                Me._indent = indent + _indentStep
                Visit(node.Initializers(i))
            Next

            _s.AppendLine(indent + "}")
            Return Nothing
        End Function

        Protected Overrides Function VisitElementInit(node As ElementInit) As ElementInit
            Visit(node)
            Return Nothing
        End Function

        Private Overloads Sub Visit(node As ElementInit)
            Dim indent = Me._indent
            _s.AppendLine(indent + "ElementInit(")
            _s.AppendLine(indent + _indentStep + node.AddMethod.ToString)
            Dim n As Integer = node.Arguments.Count
            For i = 0 To n - 1
                Me._indent = indent + _indentStep
                Visit(node.Arguments(i))
            Next

            _s.AppendLine(indent + ")")
        End Sub

        Protected Overrides Function VisitUnary(node As UnaryExpression) As Expression
            Dim indent = Me._indent
            Visit(node.Operand)

            If node.IsLifted Then
                _s.AppendLine(indent + "Lifted")
            End If

            If node.IsLiftedToNull Then
                _s.AppendLine(indent + "LiftedToNull")
            End If

            If node.Method IsNot Nothing Then
                _s.AppendLine(indent + "method: " + node.Method.ToString() + " in " + node.Method.DeclaringType.ToString())
            End If

            Return Nothing
        End Function

        Protected Overrides Function VisitMember(node As MemberExpression) As Expression
            Dim indent = Me._indent
            Visit(node.Expression)
            _s.AppendLine(indent + "-> " + node.Member.Name)
            Return Nothing
        End Function

        Protected Overrides Function VisitMethodCall(node As MethodCallExpression) As Expression
            Dim indent = Me._indent
            Visit(node.[Object])

            _s.AppendLine(indent + "method: " + node.Method.ToString() + " in " + node.Method.DeclaringType.ToString() + " (")

            Dim n As Integer = node.Arguments.Count
            For i = 0 To n - 1
                Me._indent = indent + _indentStep
                Visit(node.Arguments(i))
            Next

            _s.AppendLine(indent + ")")
            Return Nothing
        End Function

        Protected Overrides Function VisitNew(node As NewExpression) As Expression
            Dim indent = Me._indent

            _s.AppendLine(indent + If((node.Constructor IsNot Nothing), node.Constructor.ToString(), "<.ctor>") + "(")
            Dim n As Integer = node.Arguments.Count
            For i = 0 To n - 1
                Me._indent = indent + _indentStep
                Visit(node.Arguments(i))
            Next
            _s.AppendLine(indent + ")")

            If node.Members IsNot Nothing Then
                n = node.Members.Count
                If n <> 0 Then
                    _s.AppendLine(indent + "members: {")
                    For i = 0 To n - 1
                        Dim info = node.Members(i)
                        _s.AppendLine(indent + _indentStep + info.ToString())
                    Next

                    _s.AppendLine(indent + "}")
                End If
            End If

            Return Nothing
        End Function

        Protected Overrides Function VisitNewArray(node As NewArrayExpression) As Expression
            Dim indent = Me._indent
            Dim n As Integer = node.Expressions.Count
            For i = 0 To n - 1
                Me._indent = indent
                Visit(node.Expressions(i))
            Next
            Return Nothing
        End Function

        Protected Overrides Function VisitTypeBinary(node As TypeBinaryExpression) As Expression
            Dim indent = Me._indent
            Visit(node.Expression)

            _s.AppendLine(indent + "Type Operand: " + node.TypeOperand.ToString())
            Return Nothing
        End Function
    End Class

End Namespace
