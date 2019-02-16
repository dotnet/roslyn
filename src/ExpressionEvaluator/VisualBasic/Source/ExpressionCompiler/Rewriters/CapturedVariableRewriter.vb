' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator
    Friend NotInheritable Class CapturedVariableRewriter
        Inherits BoundTreeRewriterWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator

        Friend Shared Function Rewrite(
            targetMethodMeParameter As ParameterSymbol,
            displayClassVariables As ImmutableDictionary(Of String, DisplayClassVariable),
            node As BoundNode,
            diagnostics As DiagnosticBag) As BoundNode

            Dim rewriter = New CapturedVariableRewriter(targetMethodMeParameter, displayClassVariables, diagnostics)
            Return rewriter.Visit(node)
        End Function

        Private ReadOnly _targetMethodMeParameter As ParameterSymbol
        Private ReadOnly _displayClassVariables As ImmutableDictionary(Of String, DisplayClassVariable)
        Private ReadOnly _diagnostics As DiagnosticBag

        Private Sub New(
            targetMethodMeParameter As ParameterSymbol,
            displayClassVariables As ImmutableDictionary(Of String, DisplayClassVariable),
            diagnostics As DiagnosticBag)

            _targetMethodMeParameter = targetMethodMeParameter
            _displayClassVariables = displayClassVariables
            _diagnostics = diagnostics
        End Sub

        Public Overrides Function Visit(node As BoundNode) As BoundNode
            ' Ignore nodes that will be rewritten to literals in the LocalRewriter.
            If TryCast(node, BoundExpression)?.ConstantValueOpt IsNot Nothing Then
                Return node
            End If

            Return MyBase.Visit(node)
        End Function

        Public Overrides Function VisitBlock(node As BoundBlock) As BoundNode
            Dim rewrittenLocals = node.Locals.WhereAsArray(AddressOf IncludeLocal)
            Dim rewrittenStatements = VisitList(node.Statements)
            Return node.Update(node.StatementListSyntax, rewrittenLocals, rewrittenStatements)
        End Function

        Private Function IncludeLocal(local As LocalSymbol) As Boolean
            Return Not local.IsStatic AndAlso
                (local.IsCompilerGenerated OrElse local.Name Is Nothing OrElse GetVariable(local.Name) Is Nothing)
        End Function

        Public Overrides Function VisitLocal(node As BoundLocal) As BoundNode
            Dim local = node.LocalSymbol
            If Not local.IsCompilerGenerated Then
                Dim syntax = node.Syntax
                Dim staticLocal = TryCast(local, EEStaticLocalSymbol)
                If staticLocal IsNot Nothing Then
                    Dim receiver = If(_targetMethodMeParameter Is Nothing,
                        Nothing,
                        GetRewrittenMeParameter(syntax, New BoundMeReference(syntax, _targetMethodMeParameter.Type)))
                    Dim result = staticLocal.ToBoundExpression(receiver, syntax, node.IsLValue)
                    Debug.Assert(TypeSymbol.Equals(node.Type, result.Type, TypeCompareKind.ConsiderEverything))
                    Return result
                End If
                Dim variable = GetVariable(local.Name)
                If variable IsNot Nothing Then
                    Dim result = variable.ToBoundExpression(syntax, node.IsLValue, node.SuppressVirtualCalls)
                    Debug.Assert(TypeSymbol.Equals(node.Type, result.Type, TypeCompareKind.ConsiderEverything))
                    Return result
                End If
            End If
            Return node
        End Function

        Public Overrides Function VisitParameter(node As BoundParameter) As BoundNode
            Return RewriteParameter(node.Syntax, node.ParameterSymbol, node)
        End Function

        Public Overrides Function VisitMeReference(node As BoundMeReference) As BoundNode
            Return Me.GetRewrittenMeParameter(node.Syntax, node)
        End Function

        Public Overrides Function VisitMyBaseReference(node As BoundMyBaseReference) As BoundNode
            ' Rewrite as a "Me" reference with a conversion
            ' to the base type and with no virtual calls.
            Debug.Assert(node.SuppressVirtualCalls)

            Dim syntax = node.Syntax

            Dim meParameter = Me.GetRewrittenMeParameter(syntax, node)
            Debug.Assert(meParameter.Type.TypeKind = TypeKind.Class) ' Illegal in structures and modules.

            Dim baseType = node.Type
            Debug.Assert(baseType.TypeKind = TypeKind.Class) ' Illegal in structures and modules.

            Dim result = New BoundDirectCast(
                syntax:=syntax,
                operand:=meParameter,
                conversionKind:=ConversionKind.WideningReference, ' From a class to its base class.
                suppressVirtualCalls:=node.SuppressVirtualCalls,
                constantValueOpt:=Nothing,
                relaxationLambdaOpt:=Nothing,
                type:=baseType)
            Debug.Assert(TypeSymbol.Equals(result.Type, node.Type, TypeCompareKind.ConsiderEverything))
            Return result
        End Function

        Public Overrides Function VisitMyClassReference(node As BoundMyClassReference) As BoundNode
            ' MyClass is just Me with virtual calls suppressed.
            Debug.Assert(node.SuppressVirtualCalls)
            Return Me.GetRewrittenMeParameter(node.Syntax, node)
        End Function

        Public Overrides Function VisitValueTypeMeReference(node As BoundValueTypeMeReference) As BoundNode
            ' ValueTypeMe is just Me with IsLValue true.
            Debug.Assert(node.IsLValue)
            Return Me.GetRewrittenMeParameter(node.Syntax, node)
        End Function

        Private Function GetRewrittenMeParameter(syntax As SyntaxNode, node As BoundExpression) As BoundExpression
            If _targetMethodMeParameter Is Nothing Then
                ReportMissingMe(node.Syntax)
                Return node
            End If

            Dim result = RewriteParameter(syntax, _targetMethodMeParameter, node)
            Debug.Assert(result IsNot Nothing)
            Return result
        End Function

        Private Function RewriteParameter(syntax As SyntaxNode, symbol As ParameterSymbol, node As BoundExpression) As BoundExpression
            Dim name As String = symbol.Name
            Dim variable = Me.GetVariable(name)
            If variable Is Nothing Then
                ' The state machine case is for async lambdas.  The state machine
                ' will have a hoisted "me" field if it needs access to the containing
                ' display class, but the display class may not have a "me" field.
                If symbol.Type.IsClosureOrStateMachineType() AndAlso
                    GeneratedNames.GetKind(name) <> GeneratedNameKind.TransparentIdentifier Then

                    ReportMissingMe(syntax)
                End If

                Return If(TryCast(node, BoundParameter), New BoundParameter(syntax, symbol, node.IsLValue, node.SuppressVirtualCalls, symbol.Type))
            End If

            Dim result = variable.ToBoundExpression(syntax, node.IsLValue, node.SuppressVirtualCalls)
            Debug.Assert(TypeSymbol.Equals(result.Type, node.Type, TypeCompareKind.ConsiderEverything) OrElse
                         TypeSymbol.Equals(result.Type.BaseTypeNoUseSiteDiagnostics, node.Type, TypeCompareKind.ConsiderEverything))
            Return result
        End Function

        Private Sub ReportMissingMe(syntax As SyntaxNode)
            _diagnostics.Add(New VBDiagnostic(ErrorFactory.ErrorInfo(ERRID.ERR_UseOfKeywordNotInInstanceMethod1, syntax.ToString()), syntax.GetLocation()))
        End Sub

        Private Function GetVariable(name As String) As DisplayClassVariable
            Dim variable As DisplayClassVariable = Nothing
            _displayClassVariables.TryGetValue(name, variable)
            Return variable
        End Function

    End Class
End Namespace
