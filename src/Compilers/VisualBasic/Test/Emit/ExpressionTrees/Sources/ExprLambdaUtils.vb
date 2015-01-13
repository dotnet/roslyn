' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

    Class ExpressionPrinter
        Inherits System.Linq.Expressions.ExpressionVisitor

        Private s As StringBuilder = New StringBuilder()

        Private indent As String = ""
        Private indentStep As String = "  "

        Shared Function GetCultureInvariantString(val As Object) As String
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
            Return p.s.ToString()
        End Function

        Public Overrides Function Visit(node As Expression) As Expression
            Dim indent = Me.indent
            If node Is Nothing Then
                s.AppendLine(Me.indent + "<NULL>")
                Return Nothing
            End If

            s.AppendLine(indent + node.NodeType.ToString() + "(")
            Me.indent = indent + indentStep
            MyBase.Visit(node)
            s.AppendLine(indent + indentStep + "type: " + node.Type.ToString())
            s.AppendLine(indent + ")")
            Return Nothing
        End Function

        Protected Overrides Function VisitMemberBinding(node As MemberBinding) As MemberBinding
            If node Is Nothing Then
                s.AppendLine(Me.indent + "<NULL>")
                Return Nothing
            End If

            Return MyBase.VisitMemberBinding(node)
        End Function

        Protected Overrides Function VisitMemberMemberBinding(node As MemberMemberBinding) As MemberMemberBinding
            Dim indent = Me.indent
            s.AppendLine(indent + "MemberMemberBinding(")
            s.AppendLine(indent + indentStep + "member: " + node.Member.ToString())
            For Each b In node.Bindings
                Me.indent = indent + indentStep
                VisitMemberBinding(b)
            Next

            s.AppendLine(indent + ")")
            Return Nothing
        End Function

        Protected Overrides Function VisitMemberListBinding(node As MemberListBinding) As MemberListBinding
            Dim indent = Me.indent
            s.AppendLine(indent + "MemberListBinding(")
            s.AppendLine(indent + indentStep + "member: " + node.Member.ToString())
            For Each i In node.Initializers
                Me.indent = indent + indentStep
                VisitElementInit(i)
            Next

            s.AppendLine(indent + ")")
            Return Nothing
        End Function

        Protected Overrides Function VisitMemberAssignment(node As MemberAssignment) As MemberAssignment
            Dim indent = Me.indent
            s.AppendLine(indent + "MemberAssignment(")
            s.AppendLine(indent + indentStep + "member: " + node.Member.ToString())
            s.AppendLine(indent + indentStep + "expression: {")
            Me.indent = indent + indentStep + indentStep
            Visit(node.Expression)
            s.AppendLine(indent + indentStep + "}")
            s.AppendLine(indent + ")")
            Return Nothing
        End Function

        Protected Overrides Function VisitMemberInit(node As MemberInitExpression) As Expression
            Dim indent = Me.indent
            s.AppendLine(indent + "NewExpression(")
            Me.indent = indent + indentStep
            Visit(node.NewExpression)
            s.AppendLine(indent + ")")
            s.AppendLine(indent + "bindings:")
            For Each b In node.Bindings
                Me.indent = indent + indentStep
                VisitMemberBinding(b)
            Next
            Return Nothing
        End Function

        Protected Overrides Function VisitBinary(node As BinaryExpression) As Expression
            Dim indent = Me.indent

            Me.indent = indent
            Visit(node.Left)

            Me.indent = indent
            Visit(node.Right)

            If node.Conversion IsNot Nothing Then
                s.AppendLine(indent + "conversion:")
                Me.indent = indent + indentStep
                Visit(node.Conversion)
            End If

            If node.IsLifted Then
                s.AppendLine(indent + "Lifted")
            End If

            If node.IsLiftedToNull Then
                s.AppendLine(indent + "LiftedToNull")
            End If

            If node.Method IsNot Nothing Then
                s.AppendLine(indent + "method: " + node.Method.ToString() + " in " + node.Method.DeclaringType.ToString())
            End If

            Return Nothing
        End Function

        Protected Overrides Function VisitConditional(node As ConditionalExpression) As Expression
            Dim indent = Me.indent
            Visit(node.Test)
            Me.indent = indent
            Visit(node.IfTrue)
            Me.indent = indent
            Visit(node.IfFalse)
            Return Nothing
        End Function

        Protected Overrides Function VisitConstant(node As ConstantExpression) As Expression
            s.AppendLine(indent + If(node.Value Is Nothing, "null", GetCultureInvariantString(node.Value)))
            Return Nothing
        End Function

        Protected Overrides Function VisitDefault(node As DefaultExpression) As Expression
            Return Nothing
        End Function

        Protected Overrides Function VisitIndex(node As IndexExpression) As Expression
            Dim indent = Me.indent
            Visit(node.[Object])

            Me.indent = indent
            s.AppendLine(indent + "indices(")
            Dim n As Integer = node.Arguments.Count
            For i = 0 To n - 1
                Me.indent = indent + indentStep
                Visit(node.Arguments(i))
            Next

            s.AppendLine(indent + ")")

            If node.Indexer IsNot Nothing Then
                s.AppendLine(indent + "indexer: " + node.Indexer.ToString())
            End If

            Return Nothing
        End Function

        Protected Overrides Function VisitInvocation(node As InvocationExpression) As Expression
            Dim indent = Me.indent
            Visit(node.Expression)
            s.AppendLine(indent + "(")
            Dim n As Integer = node.Arguments.Count
            For i = 0 To n - 1
                Me.indent = indent + indentStep
                Visit(node.Arguments(i))
            Next

            s.AppendLine(indent + ")")
            Return Nothing
        End Function

        Protected Overrides Function VisitLambda(Of T)(node As Expression(Of T)) As Expression
            Dim indent = Me.indent
            Dim n As Integer = node.Parameters.Count
            For i = 0 To n - 1
                Me.indent = indent
                Visit(node.Parameters(i))
            Next

            If node.Name IsNot Nothing Then
                s.AppendLine(indent + node.Name)
            End If

            s.AppendLine(indent + "body {")
            Me.indent = indent + indentStep
            Visit(node.Body)
            s.AppendLine(indent + "}")

            If node.ReturnType IsNot Nothing Then
                s.AppendLine(indent + "return type: " + node.ReturnType.ToString())
            End If

            If node.TailCall Then
                s.AppendLine(indent + "tail call")
            End If

            Return Nothing
        End Function

        Protected Overrides Function VisitParameter(node As ParameterExpression) As Expression
            s.Append(Me.indent + node.Name)
            If node.IsByRef Then
                s.Append(" ByRef")
            End If
            s.AppendLine()
            Return Nothing
        End Function

        Protected Overrides Function VisitListInit(node As ListInitExpression) As Expression
            Dim indent = Me.indent
            Visit(node.NewExpression)

            s.AppendLine(indent + "{")
            Dim n As Integer = node.Initializers.Count
            For i = 0 To n - 1
                Me.indent = indent + indentStep
                Visit(node.Initializers(i))
            Next

            s.AppendLine(indent + "}")
            Return Nothing
        End Function

        Protected Overrides Function VisitElementInit(node As ElementInit) As ElementInit
            Visit(node)
            Return Nothing
        End Function

        Private Overloads Sub Visit(node As ElementInit)
            Dim indent = Me.indent
            s.AppendLine(indent + "ElementInit(")
            s.AppendLine(indent + indentStep + node.AddMethod.ToString)
            Dim n As Integer = node.Arguments.Count
            For i = 0 To n - 1
                Me.indent = indent + indentStep
                Visit(node.Arguments(i))
            Next

            s.AppendLine(indent + ")")
        End Sub

        Protected Overrides Function VisitUnary(node As UnaryExpression) As Expression
            Dim indent = Me.indent
            Visit(node.Operand)

            If node.IsLifted Then
                s.AppendLine(indent + "Lifted")
            End If

            If node.IsLiftedToNull Then
                s.AppendLine(indent + "LiftedToNull")
            End If

            If node.Method IsNot Nothing Then
                s.AppendLine(indent + "method: " + node.Method.ToString() + " in " + node.Method.DeclaringType.ToString())
            End If

            Return Nothing
        End Function

        Protected Overrides Function VisitMember(node As MemberExpression) As Expression
            Dim indent = Me.indent
            Visit(node.Expression)
            s.AppendLine(indent + "-> " + node.Member.Name)
            Return Nothing
        End Function

        Protected Overrides Function VisitMethodCall(node As MethodCallExpression) As Expression
            Dim indent = Me.indent
            Visit(node.[Object])

            s.AppendLine(indent + "method: " + node.Method.ToString() + " in " + node.Method.DeclaringType.ToString() + " (")

            Dim n As Integer = node.Arguments.Count
            For i = 0 To n - 1
                Me.indent = indent + indentStep
                Visit(node.Arguments(i))
            Next

            s.AppendLine(indent + ")")
            Return Nothing
        End Function

        Protected Overrides Function VisitNew(node As NewExpression) As Expression
            Dim indent = Me.indent

            s.AppendLine(indent + If((node.Constructor IsNot Nothing), node.Constructor.ToString(), "<.ctor>") + "(")
            Dim n As Integer = node.Arguments.Count
            For i = 0 To n - 1
                Me.indent = indent + indentStep
                Visit(node.Arguments(i))
            Next
            s.AppendLine(indent + ")")

            If node.Members IsNot Nothing Then
                n = node.Members.Count
                If n <> 0 Then
                    s.AppendLine(indent + "members: {")
                    For i = 0 To n - 1
                        Dim info = node.Members(i)
                        s.AppendLine(indent + indentStep + info.ToString())
                    Next

                    s.AppendLine(indent + "}")
                End If
            End If

            Return Nothing
        End Function

        Protected Overrides Function VisitNewArray(node As NewArrayExpression) As Expression
            Dim indent = Me.indent
            Dim n As Integer = node.Expressions.Count
            For i = 0 To n - 1
                Me.indent = indent
                Visit(node.Expressions(i))
            Next
            Return Nothing
        End Function

        Protected Overrides Function VisitTypeBinary(node As TypeBinaryExpression) As Expression
            Dim indent = Me.indent
            Visit(node.Expression)

            s.AppendLine(indent + "Type Operand: " + node.TypeOperand.ToString())
            Return Nothing
        End Function
    End Class

End Namespace
