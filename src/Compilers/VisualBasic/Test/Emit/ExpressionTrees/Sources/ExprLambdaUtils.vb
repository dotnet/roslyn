' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

    Module ext
        '                       1         2         3
        '              12345678901234567890123456789012
        Const spc32 = "                                "
        Const spc16 = "                "
        Const spc14 = "              "
        Const spc12 = "            "
        Const spc10 = "          "
        Const spc08 = "        "
        Const spc06 = "      "
        Const spc04 = "    "
        Const spc02 = "  "
        <System.Runtime.CompilerServices.Extension>
        Friend Function Indent(ByRef sb As StringBuilder, level As Integer) As StringBuilder
simplecases:
            Select Case level
                Case 0 : Return sb
                Case 2 : sb = sb.Append(spc02)
                Case 4 : sb = sb.Append(spc04)
                Case 6 : sb = sb.Append(spc06)
                Case 8 : sb = sb.Append(spc08)
                Case 10 : sb = sb.Append(spc10)
                Case 12 : sb = sb.Append(spc12)
                Case 14 : sb = sb.Append(spc14)
                Case Else
                    While level >= 16
                        sb.Append(spc16) : level -= 16
                    End While
                    GoTo simplecases
            End Select
            Return sb
        End Function
    End Module

    Friend Class ExpressionPrinter
        Inherits System.Linq.Expressions.ExpressionVisitor

        Private ReadOnly _s As New StringBuilder(256)

        Private ReadOnly _IndentStep As Integer = 2
        Private _indent As Integer = 0

        Public Shared Function GetCultureInvariantString(val As Object) As String
            If val Is Nothing Then Return Nothing


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
            Dim indent = _indent
            If node Is Nothing Then
                _s.Indent(_indent).AppendLine("<NULL>")
                Return Nothing
            End If

            _s.Indent(indent).Append(node.NodeType.ToString()).AppendLine("(")
            _indent = indent + _IndentStep
            MyBase.Visit(node)
            _s.Indent(indent + _IndentStep).Append("type: ").AppendLine(node.Type.ToString())
            _s.Indent(indent).AppendLine(")")
            Return Nothing
        End Function

        Protected Overrides Function VisitMemberBinding(node As MemberBinding) As MemberBinding
            If node IsNot Nothing Then Return MyBase.VisitMemberBinding(node)
            _s.Indent(_indent).AppendLine("<NULL>")
            Return Nothing
        End Function

        Protected Overrides Function VisitMemberMemberBinding(node As MemberMemberBinding) As MemberMemberBinding
            Dim indent = _indent
            _s.Indent(indent).AppendLine("MemberMemberBinding(")
            _s.Indent(indent + _IndentStep).Append("member: ").AppendLine(node.Member.ToString())
            For Each b In node.Bindings
                _indent = indent + _IndentStep
                VisitMemberBinding(b)
            Next

            _s.Indent(indent).AppendLine(")")
            Return Nothing
        End Function

        Protected Overrides Function VisitMemberListBinding(node As MemberListBinding) As MemberListBinding
            Dim indent = _indent
            _s.Indent(indent).AppendLine("MemberListBinding(")
            _s.Indent(indent + _IndentStep).Append("member: ").AppendLine(node.Member.ToString())
            For Each i In node.Initializers
                _indent = indent + _IndentStep
                VisitElementInit(i)
            Next
            _s.Indent(indent).AppendLine(")")
            Return Nothing
        End Function

        Protected Overrides Function VisitMemberAssignment(node As MemberAssignment) As MemberAssignment
            Dim indent = _indent
            _s.Indent(indent).AppendLine("MemberAssignment(")
            Dim nl = indent + _IndentStep
            _s.Indent(nl).Append("member: ").AppendLine(node.Member.ToString())
            _s.Indent(nl).AppendLine("expression: {")
            _indent = nl + _IndentStep
            Visit(node.Expression)
            _s.Indent(nl).AppendLine("}")
            _s.Indent(indent).AppendLine(")")
            Return Nothing
        End Function

        Protected Overrides Function VisitMemberInit(node As MemberInitExpression) As Expression
            Dim indent = _indent
            _s.Indent(indent).AppendLine("NewExpression(")
            _indent = indent + _IndentStep
            Visit(node.NewExpression)
            _s.Indent(indent).AppendLine(")")
            _s.Indent(indent).AppendLine("bindings:")
            For Each b In node.Bindings
                _indent = indent + _IndentStep
                VisitMemberBinding(b)
            Next

            Return Nothing
        End Function

        Protected Overrides Function VisitBinary(node As BinaryExpression) As Expression
            Dim indent = _indent

            _indent = indent
            Visit(node.Left)

            _indent = indent
            Visit(node.Right)

            If node.Conversion IsNot Nothing Then
                _s.Indent(indent).AppendLine("conversion:")
                _indent = indent + _IndentStep
                Visit(node.Conversion)
            End If

            If node.IsLifted Then _s.Indent(indent).AppendLine("Lifted")

            If node.IsLiftedToNull Then _s.Indent(indent).
                                             AppendLine("LiftedToNull")

            If node.Method IsNot Nothing Then _s.Indent(indent).
                                                   Append("method: ").
                                                   Append(node.Method.ToString()).
                                                   Append(" in ").
                                                   AppendLine(node.Method.DeclaringType.ToString())

            Return Nothing
        End Function

        Protected Overrides Function VisitConditional(node As ConditionalExpression) As Expression
            Dim indent = _indent
            Visit(node.Test)
            _indent = indent
            Visit(node.IfTrue)
            _indent = indent
            Visit(node.IfFalse)
            Return Nothing
        End Function

        Protected Overrides Function VisitConstant(node As ConstantExpression) As Expression
            _s.Indent(_indent).AppendLine(If(node.Value Is Nothing, "null", GetCultureInvariantString(node.Value)))
            Return Nothing
        End Function

        Protected Overrides Function VisitDefault(node As DefaultExpression) As Expression
            Return Nothing
        End Function

        Protected Overrides Function VisitIndex(node As IndexExpression) As Expression
            Dim indent = _indent
            Visit(node.[Object])

            _indent = indent
            _s.Indent(indent).AppendLine("indices(")
            Dim n As Integer = node.Arguments.Count - 1

            For i = 0 To n
                _indent = indent + _IndentStep
                Visit(node.Arguments(i))
            Next

            _s.Indent(indent).AppendLine(")")

            If node.Indexer IsNot Nothing Then _s.Indent(indent).Append("indexer: ").AppendLine(node.Indexer.ToString())

            Return Nothing
        End Function

        Protected Overrides Function VisitInvocation(node As InvocationExpression) As Expression
            Dim indent = _indent
            Visit(node.Expression)
            _s.Indent(indent).AppendLine("(")
            Dim n As Integer = node.Arguments.Count - 1

            For i = 0 To n
                _indent = indent + _IndentStep
                Visit(node.Arguments(i))
            Next

            _s.Indent(indent).AppendLine(")")
            Return Nothing
        End Function

        Protected Overrides Function VisitLambda(Of T)(node As Expression(Of T)) As Expression
            Dim indent = _indent
            Dim n As Integer = node.Parameters.Count - 1

            For i = 0 To n
                _indent = indent
                Visit(node.Parameters(i))
            Next

            If node.Name IsNot Nothing Then _s.Indent(indent).AppendLine(node.Name)

            _s.Indent(indent).AppendLine("body {")
            _indent = indent + _IndentStep
            Visit(node.Body)

            _s.Indent(indent).AppendLine("}")

            If node.ReturnType IsNot Nothing Then _s.Indent(indent).Append("return type: ").AppendLine(node.ReturnType.ToString())

            If node.TailCall Then _s.Indent(indent).AppendLine("tail call")

            Return Nothing
        End Function

        Protected Overrides Function VisitParameter(node As ParameterExpression) As Expression
            _s.Indent(_indent).Append(node.Name)

            If node.IsByRef Then _s.Append(" ByRef")

            _s.AppendLine()

            Return Nothing
        End Function

        Protected Overrides Function VisitListInit(node As ListInitExpression) As Expression
            Dim indent = _indent

            Visit(node.NewExpression)

            _s.Indent(indent).AppendLine("{")

            Dim n As Integer = node.Initializers.Count - 1
            For i = 0 To n
                _indent = indent + _IndentStep
                Visit(node.Initializers(i))
            Next
            _s.Indent(indent).AppendLine("}")
            Return Nothing
        End Function

        Protected Overrides Function VisitElementInit(node As ElementInit) As ElementInit
            Visit(node)
            Return Nothing
        End Function

        Private Overloads Sub Visit(node As ElementInit)
            Dim indent = _indent
            _s.Indent(indent).AppendLine("ElementInit(")
            _s.Indent(indent + _IndentStep).AppendLine(node.AddMethod.ToString)
            Dim n As Integer = node.Arguments.Count
            For i = 0 To n - 1
                _indent = indent + _IndentStep
                Visit(node.Arguments(i))
            Next
            _s.Indent(indent).AppendLine(")")
        End Sub

        Protected Overrides Function VisitUnary(node As UnaryExpression) As Expression
            Dim indent = _indent
            Visit(node.Operand)

            If node.IsLifted Then _s.Indent(indent).AppendLine("Lifted")

            If node.IsLiftedToNull Then _s.Indent(indent).AppendLine("LiftedToNull")

            If node.Method IsNot Nothing Then _s.Indent(indent).Append("method: ").Append(node.Method.ToString()).Append(" in ").AppendLine(node.Method.DeclaringType.ToString())
            Return Nothing
        End Function

        Protected Overrides Function VisitMember(node As MemberExpression) As Expression
            Dim indent = _indent
            Visit(node.Expression)
            _s.Indent(indent).Append("-> ").AppendLine(node.Member.Name)
            Return Nothing
        End Function

        Protected Overrides Function VisitMethodCall(node As MethodCallExpression) As Expression
            Dim indent = _indent
            Visit(node.[Object])
            _s.Indent(indent).Append("method: ").Append(node.Method.ToString()).Append(" in ").Append(node.Method.DeclaringType.ToString()).AppendLine(" (")

            Dim n As Integer = node.Arguments.Count
            For i = 0 To n - 1
                _indent = indent + _IndentStep
                Visit(node.Arguments(i))
            Next

            _s.Indent(indent).AppendLine(")")
            Return Nothing
        End Function

        Protected Overrides Function VisitNew(node As NewExpression) As Expression
            Dim indent = _indent
            _s.Indent(indent).AppendLine(If((node.Constructor IsNot Nothing), node.Constructor.ToString(), "<.ctor>") + "(")
            Dim n As Integer = node.Arguments.Count
            For i = 0 To n - 1
                _indent = indent + _IndentStep
                Visit(node.Arguments(i))
            Next
            _s.Indent(indent).AppendLine(")")
            If node.Members Is Nothing Then Return Nothing
            n = node.Members.Count
            If n = 0 Then Return Nothing
            _s.Indent(indent).AppendLine("members: {")

            For i = 0 To n - 1
                Dim info = node.Members(i)
                _s.Indent(indent + _IndentStep).AppendLine(info.ToString())
            Next
            _s.Indent(indent).AppendLine("}")

            Return Nothing
        End Function

        Protected Overrides Function VisitNewArray(node As NewArrayExpression) As Expression
            Dim indent = _indent
            Dim n As Integer = node.Expressions.Count
            For i = 0 To n - 1
                _indent = indent
                Visit(node.Expressions(i))
            Next
            Return Nothing
        End Function

        Protected Overrides Function VisitTypeBinary(node As TypeBinaryExpression) As Expression
            Dim indent = _indent
            Visit(node.Expression)
            _s.Indent(indent).Append("Type Operand: ").AppendLine(node.TypeOperand.ToString())
            Return Nothing
        End Function
    End Class

End Namespace
