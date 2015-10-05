' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq
Imports System.Runtime.InteropServices
Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend NotInheritable Class DiagnosticsPass
        Inherits BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator

        Private ReadOnly _diagnostics As DiagnosticBag
        Private ReadOnly _compilation As VisualBasicCompilation
        Private _containingSymbol As MethodSymbol
        Private _withExpressionPlaceholderMap As Dictionary(Of BoundValuePlaceholderBase, BoundWithStatement)
        Private _expressionsBeingVisited As Stack(Of BoundExpression)

        Private _inExpressionLambda As Boolean

        Public Shared Sub IssueDiagnostics(node As BoundNode, diagnostics As DiagnosticBag, containingSymbol As MethodSymbol)
            Debug.Assert(node IsNot Nothing)
            Debug.Assert(containingSymbol IsNot Nothing)

            Try
                Dim diagnosticPass As New DiagnosticsPass(containingSymbol.DeclaringCompilation, diagnostics, containingSymbol)
                diagnosticPass.Visit(node)
            Catch ex As CancelledByStackGuardException
                ex.AddAnError(diagnostics)
            End Try
        End Sub

        Private Sub New(compilation As VisualBasicCompilation, diagnostics As DiagnosticBag, containingSymbol As MethodSymbol)
            Me._compilation = compilation
            Me._diagnostics = diagnostics
            Me._containingSymbol = containingSymbol

            Me._inExpressionLambda = False
        End Sub

        Private ReadOnly Property IsInExpressionLambda As Boolean
            Get
                Return Me._inExpressionLambda
            End Get
        End Property

        Public Overrides Function VisitQueryLambda(node As BoundQueryLambda) As BoundNode
            Dim save_containingSymbol = Me._containingSymbol
            Me._containingSymbol = node.LambdaSymbol
            Me.Visit(node.Expression)
            Me._containingSymbol = save_containingSymbol
            Return Nothing
        End Function

        Public Overrides Function VisitParameter(node As BoundParameter) As BoundNode
            Dim parameterSymbol As ParameterSymbol = node.ParameterSymbol

            If parameterSymbol.IsByRef Then
                ' cannot access ByRef parameters in Lambdas
                Dim parameterSymbolContainingSymbol As Symbol = parameterSymbol.ContainingSymbol

                If _containingSymbol IsNot parameterSymbolContainingSymbol Then
                    ' Need to go up the chain of containers and see if the last lambda we see
                    ' is a QueryLambda, before we reach parameter's container. 
                    If Binder.IsTopMostEnclosingLambdaAQueryLambda(_containingSymbol, parameterSymbolContainingSymbol) Then
                        Binder.ReportDiagnostic(Me._diagnostics, node.Syntax, ERRID.ERR_CannotLiftByRefParamQuery1, parameterSymbol.Name)
                    Else
                        Binder.ReportDiagnostic(Me._diagnostics, node.Syntax, ERRID.ERR_CannotLiftByRefParamLambda1, parameterSymbol.Name)
                    End If
                End If
            End If

            Return MyBase.VisitParameter(node)
        End Function

        Public Overrides Function VisitMeReference(node As BoundMeReference) As BoundNode
            Dim errorId As ERRID = GetMeAccessError()

            If errorId <> ERRID.ERR_None Then
                Binder.ReportDiagnostic(Me._diagnostics, node.Syntax, errorId)
            End If

            Return MyBase.VisitMeReference(node)
        End Function

        Public Overrides Function VisitMyClassReference(node As BoundMyClassReference) As BoundNode
            Dim errorId As ERRID = GetMeAccessError()

            If errorId <> ERRID.ERR_None Then
                Binder.ReportDiagnostic(Me._diagnostics, node.Syntax, errorId)
            End If

            Return MyBase.VisitMyClassReference(node)
        End Function

        Private Function GetMeAccessError() As ERRID
            Dim meParameter As ParameterSymbol = Me._containingSymbol.MeParameter
            If meParameter IsNot Nothing AndAlso meParameter.IsByRef Then
                If _containingSymbol.MethodKind = MethodKind.LambdaMethod Then
                    ' Need to go up the chain of containers and see if the last lambda we see
                    ' is a QueryLambda, before we reach a type member. 
                    If Binder.IsTopMostEnclosingLambdaAQueryLambda(_containingSymbol, Nothing) Then
                        Return ERRID.ERR_CannotLiftStructureMeQuery
                    Else
                        Return ERRID.ERR_CannotLiftStructureMeLambda
                    End If
                End If
            End If

            Return ERRID.ERR_None
        End Function

        Public Overrides Function VisitWithRValueExpressionPlaceholder(node As BoundWithRValueExpressionPlaceholder) As BoundNode
            CheckMeAccessInWithExpression(node)
            Return MyBase.VisitWithRValueExpressionPlaceholder(node)
        End Function

        Private Sub CheckMeAccessInWithExpression(node As BoundValuePlaceholderBase)
            Dim trackedWithStatement As BoundWithStatement = Nothing

            If _withExpressionPlaceholderMap IsNot Nothing AndAlso _withExpressionPlaceholderMap.TryGetValue(node, trackedWithStatement) Then
                Dim withBlockBinder As WithBlockBinder = trackedWithStatement.Binder

                If withBlockBinder.ContainingMember IsNot _containingSymbol Then
                    ' If With statement expression is accessed from a different symbol,
                    ' we need to check for value-typed Me reference being lifted
                    If Not withBlockBinder.IsInLambda Then
                        Debug.Assert(_containingSymbol.IsLambdaMethod) ' What else can it be?

                        Dim containingType = withBlockBinder.ContainingType
                        If containingType IsNot Nothing AndAlso containingType.IsValueType AndAlso withBlockBinder.Info.ExpressionHasByRefMeReference(RecursionDepth) Then
                            Dim errorId As ERRID = GetMeAccessError()
                            If errorId <> ERRID.ERR_None Then
                                ' The placeholder node will actually use syntax from With statement and it might have some wrappers and conversions on top of it with the same syntax.
                                ' Let's use syntax of parent node instead. 
                                Dim errorSyntax As SyntaxNode = node.Syntax

                                For Each expr In _expressionsBeingVisited
                                    If expr.Syntax IsNot errorSyntax Then
                                        errorSyntax = expr.Syntax
                                        Exit For
                                    End If
                                Next

                                Binder.ReportDiagnostic(Me._diagnostics, errorSyntax, errorId)
                            End If
                        End If
                    End If
                End If
            End If
        End Sub

        Public Overrides Function VisitWithStatement(node As BoundWithStatement) As BoundNode
            Me.Visit(node.OriginalExpression)

            Dim info As WithBlockBinder.WithBlockInfo = node.Binder.Info

            If info IsNot Nothing AndAlso info.ExpressionIsAccessedFromNestedLambda Then
                If _withExpressionPlaceholderMap Is Nothing Then
                    _withExpressionPlaceholderMap = New Dictionary(Of BoundValuePlaceholderBase, BoundWithStatement)()
                    _expressionsBeingVisited = New Stack(Of BoundExpression)()
                End If

                Debug.Assert(info.ExpressionPlaceholder IsNot Nothing)
                _withExpressionPlaceholderMap.Add(info.ExpressionPlaceholder, node)
            Else
                info = Nothing
            End If

            Me.Visit(node.Body)

            If info IsNot Nothing Then
                _withExpressionPlaceholderMap.Remove(info.ExpressionPlaceholder)
            End If

            Return Nothing
        End Function

        Public Overrides Function Visit(node As BoundNode) As BoundNode
            Dim pop As Boolean = False

            If _withExpressionPlaceholderMap IsNot Nothing AndAlso _withExpressionPlaceholderMap.Count > 0 Then
                Dim expr = TryCast(node, BoundExpression)

                If expr IsNot Nothing Then
                    _expressionsBeingVisited.Push(expr)
                    pop = True
                End If
            End If

            Dim result = MyBase.Visit(node)

            If pop Then
                Debug.Assert(_expressionsBeingVisited.Peek Is node)
                _expressionsBeingVisited.Pop()
            End If

            Return result
        End Function

    End Class

End Namespace
