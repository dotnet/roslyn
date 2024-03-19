' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter

        Public Overrides Function VisitFieldInitializer(node As BoundFieldInitializer) As BoundNode
            Return VisitFieldOrPropertyInitializer(node, ImmutableArray(Of Symbol).CastUp(node.InitializedFields))
        End Function

        Public Overrides Function VisitPropertyInitializer(node As BoundPropertyInitializer) As BoundNode
            Return VisitFieldOrPropertyInitializer(node, ImmutableArray(Of Symbol).CastUp(node.InitializedProperties))
        End Function

        ''' <summary>
        ''' Field initializers need to be rewritten multiple times in case of an AsNew declaration with multiple field names because the 
        ''' initializer may contain references to the current field like in the following example:
        ''' Class C1
        '''     Public x, y As New RefType() With {.Field1 = .Field2}
        ''' End Class 
        ''' 
        ''' in this example .Field2 references the temp that is created for x and y.
        ''' 
        ''' We moved the final rewriting for field initializers to the local 
        ''' rewriters because here we already have the infrastructure to replace placeholders. 
        ''' </summary>
        Private Function VisitFieldOrPropertyInitializer(node As BoundFieldOrPropertyInitializer, initializedSymbols As ImmutableArray(Of Symbol)) As BoundNode
            Dim syntax = node.Syntax

            Debug.Assert(
                syntax.IsKind(SyntaxKind.AsNewClause) OrElse                ' Dim a As New C(); Dim a,b As New C(); Property P As New C()
                syntax.IsKind(SyntaxKind.ModifiedIdentifier) OrElse         ' Dim a(1) As Integer
                syntax.IsKind(SyntaxKind.EqualsValue))                      ' Dim a = 1; Property P As Integer = 1

            Dim rewrittenStatements = ArrayBuilder(Of BoundStatement).GetInstance(initializedSymbols.Length)

            ' it's enough to create one me reference if the symbols are not shared that gets reused for all following rewritings.
            Dim meReferenceOpt As BoundExpression = Nothing
            If Not initializedSymbols.First.IsShared Then
                ' create me reference if needed
                Debug.Assert(_currentMethodOrLambda IsNot Nothing)
                meReferenceOpt = New BoundMeReference(syntax, _currentMethodOrLambda.ContainingType)
                meReferenceOpt.SetWasCompilerGenerated()
            End If

            Dim instrument As Boolean = Me.Instrument(node)

            For symbolIndex = 0 To initializedSymbols.Length - 1
                Dim symbol = initializedSymbols(symbolIndex)
                Dim accessExpression As BoundExpression

                Dim initialValueToRewrite As BoundExpression

                If symbolIndex = 0 Then
                    initialValueToRewrite = node.InitialValue
                Else
                    ' For all variables except the first one we rebind the initializer
                    ' and throw away diagnostics, those are supposed to be reported in the first
                    ' binding and don't need to be duplicated
                    '
                    ' Note that we have to rebind the initializers because current implementation of lambda 
                    ' rewriter does not handle correctly blocks and locals which are reused in bound tree. 
                    ' Actually it seems to heavily rely on an assumption that the bound tree is a tree, not 
                    ' a DAG, with just minor deviations. Thus, the natural way of just rewriting bound 
                    ' initializer stored in node.Initializer simply does not work because rewriting *may* 
                    ' keep many blocks and locals unchanged and reuse them in all rewritten initializers, 
                    ' in which case if we have a lambda inside the initializer lambda rewriter simply throws.

                    If symbol.Kind = SymbolKind.Field Then
                        ' Rebind and discard diagnostics
                        initialValueToRewrite = node.BinderOpt.BindFieldInitializerExpression(syntax, DirectCast(symbol, FieldSymbol), BindingDiagnosticBag.Discarded)
                    Else
                        ' Rebind and discard diagnostics
                        initialValueToRewrite = node.BinderOpt.BindPropertyInitializerExpression(syntax, DirectCast(symbol, PropertySymbol), BindingDiagnosticBag.Discarded)
                    End If
                End If

                Dim objectInitializer As BoundObjectInitializerExpression = Nothing
                Dim createTemporary = True

                If initialValueToRewrite.Kind = BoundKind.ObjectCreationExpression OrElse initialValueToRewrite.Kind = BoundKind.NewT Then
                    Dim objectCreationExpression = DirectCast(initialValueToRewrite, BoundObjectCreationExpressionBase)
                    If objectCreationExpression.InitializerOpt IsNot Nothing AndAlso
                    objectCreationExpression.InitializerOpt.Kind = BoundKind.ObjectInitializerExpression Then
                        objectInitializer = DirectCast(objectCreationExpression.InitializerOpt, BoundObjectInitializerExpression)
                        createTemporary = objectInitializer.CreateTemporaryLocalForInitialization
                    End If
                End If

                ' if there are more than one symbol we need to create a field or property access for each of them
                If initializedSymbols.Length > 1 Then
                    If symbol.Kind = SymbolKind.Field Then
                        Dim fieldSymbol = DirectCast(symbol, FieldSymbol)
                        accessExpression = New BoundFieldAccess(syntax, meReferenceOpt, fieldSymbol, True, fieldSymbol.Type)
                    Else
                        ' We can get here when multiple WithEvents fields are initialized with As New ...
                        Dim propertySymbol = DirectCast(symbol, PropertySymbol)
                        accessExpression = New BoundPropertyAccess(syntax,
                                                                   propertySymbol,
                                                                   propertyGroupOpt:=Nothing,
                                                                   accessKind:=PropertyAccessKind.Set,
                                                                   isWriteable:=propertySymbol.HasSet,
                                                                   receiverOpt:=meReferenceOpt,
                                                                   arguments:=ImmutableArray(Of BoundExpression).Empty)
                    End If
                Else
                    Debug.Assert(node.MemberAccessExpressionOpt IsNot Nothing)

                    ' otherwise use the node stored in the bound initializer node
                    accessExpression = node.MemberAccessExpressionOpt
                End If

                Dim rewrittenStatement As BoundStatement

                If Not createTemporary Then
                    Debug.Assert(objectInitializer.PlaceholderOpt IsNot Nothing)

                    ' we need to replace the placeholder with it, so add it to the replacement map
                    AddPlaceholderReplacement(objectInitializer.PlaceholderOpt, accessExpression)

                    rewrittenStatement = VisitExpressionNode(initialValueToRewrite).ToStatement

                    RemovePlaceholderReplacement(objectInitializer.PlaceholderOpt)
                Else
                    ' in all other cases we want the initial value be assigned to the member (field or property)
                    rewrittenStatement = VisitExpression(New BoundAssignmentOperator(syntax,
                                                                                     accessExpression,
                                                                                     initialValueToRewrite,
                                                                                     suppressObjectClone:=False)).ToStatement
                End If

                If instrument Then
                    rewrittenStatement = _instrumenterOpt.InstrumentFieldOrPropertyInitializer(node, rewrittenStatement, symbolIndex, createTemporary)
                End If

                rewrittenStatements.Add(rewrittenStatement)
            Next

            Return New BoundStatementList(node.Syntax, rewrittenStatements.ToImmutableAndFree())
        End Function
    End Class
End Namespace
