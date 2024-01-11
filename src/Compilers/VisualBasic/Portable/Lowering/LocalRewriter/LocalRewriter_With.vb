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

        Public Overrides Function VisitWithStatement(node As BoundWithStatement) As BoundNode
            If node.HasErrors Then
                Return node
            End If

            Dim saveState As UnstructuredExceptionHandlingContext = LeaveUnstructuredExceptionHandlingContext(node)

            ' With block attributes
            Dim rewrittenExpression As BoundExpression = VisitExpressionNode(node.OriginalExpression)
            Dim type As TypeSymbol = rewrittenExpression.Type
            Dim statementSyntax = DirectCast(node.Syntax, WithBlockSyntax).WithStatement

            Dim doNotUseByRefLocal = Me._currentMethodOrLambda.IsIterator OrElse
                                    Me._currentMethodOrLambda.IsAsync OrElse
                                    node.Binder.ExpressionIsAccessedFromNestedLambda

            ' What the placeholder should be replaced with
            Dim result As WithExpressionRewriter.Result =
                (New WithExpressionRewriter(statementSyntax)).AnalyzeWithExpression(Me._currentMethodOrLambda,
                                                             rewrittenExpression,
                                                             doNotUseByRefLocal,
                                                             isDraftRewrite:=False,
                                                             Nothing)

            RestoreUnstructuredExceptionHandlingContext(node, saveState)

            Return RewriteWithBlockStatements(node,
                                              ShouldGenerateUnstructuredExceptionHandlingResumeCode(node),
                                              result.Locals,
                                              result.Initializers,
                                              node.ExpressionPlaceholder,
                                              result.Expression)
        End Function

        Private Function RewriteWithBlockStatements(node As BoundWithStatement,
                                                    generateUnstructuredExceptionHandlingResumeCode As Boolean,
                                                    locals As ImmutableArray(Of LocalSymbol),
                                                    initializers As ImmutableArray(Of BoundExpression),
                                                    placeholder As BoundValuePlaceholderBase,
                                                    replaceWith As BoundExpression) As BoundBlock
            Debug.Assert(Not locals.IsDefault)
            Debug.Assert(Not initializers.IsDefault)
            Debug.Assert(placeholder IsNot Nothing)
            Debug.Assert(replaceWith IsNot Nothing)

            Dim block As BoundBlock = node.Body
            Dim syntax As SyntaxNode = node.Syntax

            ' We need to create a new Block with locals, initialization 
            ' statements, bound block and optional clean-up statements
            Dim initStatements = ArrayBuilder(Of BoundStatement).GetInstance
            Dim instrument As Boolean = Me.Instrument(node) AndAlso syntax.Kind = SyntaxKind.WithBlock

            If instrument Then
                Dim prologue = _instrumenterOpt.CreateWithStatementPrologue(node)
                If prologue IsNot Nothing Then
                    initStatements.Add(prologue)
                End If
            End If

            If generateUnstructuredExceptionHandlingResumeCode Then
                RegisterUnstructuredExceptionHandlingResumeTarget(syntax, canThrow:=True, statements:=initStatements)
            End If

            ' Initializers first
            For Each initializer In initializers
                initStatements.Add(New BoundExpressionStatement(syntax, initializer).MakeCompilerGenerated())
            Next

            ' place placeholder into the map
            AddPlaceholderReplacement(placeholder, replaceWith)

            ' Then the bound block 
            initStatements.Add(DirectCast(Visit(block), BoundStatement))
            RemovePlaceholderReplacement(placeholder)

            If instrument Then
                Dim epilogue = _instrumenterOpt.CreateWithStatementEpilogue(node)
                If epilogue IsNot Nothing Then
                    initStatements.Add(epilogue)
                End If
            End If

            If generateUnstructuredExceptionHandlingResumeCode Then
                initStatements.Add(RegisterUnstructuredExceptionHandlingNonThrowingResumeTarget(syntax))
            End If

            ' Cleanup code for locals which need it
            If Not node.Binder.ExpressionIsAccessedFromNestedLambda Then
                For Each _local In locals
                    Dim localType As TypeSymbol = _local.Type

                    ' Only for locals of reference type or type parameter type or non-primitive structs 
                    If Not _local.IsByRef AndAlso LocalOrFieldNeedsToBeCleanedUp(localType) Then
                        initStatements.Add(
                            New BoundExpressionStatement(
                                syntax,
                                VisitExpression(
                                    New BoundAssignmentOperator(
                                        syntax,
                                        New BoundLocal(syntax, _local, isLValue:=True, type:=localType).MakeCompilerGenerated(),
                                        New BoundConversion(
                                            syntax,
                                            New BoundLiteral(syntax, ConstantValue.Nothing, Nothing).MakeCompilerGenerated(),
                                            ConversionKind.WideningNothingLiteral,
                                            checked:=False,
                                            explicitCastInCode:=False,
                                            type:=localType).MakeCompilerGenerated(),
                                        suppressObjectClone:=True,
                                        type:=localType
                                    ).MakeCompilerGenerated()
                                )
                            ).MakeCompilerGenerated()
                        )
                    End If
                Next
            End If

            ' Create a new block
            Dim newBlock As New BoundBlock(syntax, Nothing, locals, initStatements.ToImmutableAndFree())

            Return newBlock
        End Function

        ''' <summary>
        ''' Cache of value types which were already calculated by LocalOrFieldNeedsToBeCleanedUp 
        ''' in this lowering, serves as an optimization 
        ''' </summary>
        Private ReadOnly _valueTypesCleanUpCache As New Dictionary(Of TypeSymbol, Boolean)

        Private Function LocalOrFieldNeedsToBeCleanedUp(currentType As TypeSymbol) As Boolean
            Debug.Assert(currentType IsNot Nothing)

            ' Locals of reference type always need to be cleaned after With statement
            If currentType.IsReferenceType OrElse currentType.IsTypeParameter Then
                Return True
            End If

            Debug.Assert(currentType.IsValueType)

            ' Locals of intrinsic and enum types does not need to be cleaned 
            If currentType.IsIntrinsicOrEnumType Then
                Return False
            End If

            ' Was it calculated before
            Dim _value As Boolean
            If _valueTypesCleanUpCache.TryGetValue(currentType, _value) Then
                Return _value
            End If

            ' NOTE: We assume here that the structure DOES NOT need to be cleaned up; 
            '       in recursive structures this will prevent recursion and return False
            _valueTypesCleanUpCache(currentType) = False

            ' Other structures need to be analyzed field-by-field: if there is any field 
            ' which needs to be cleaned up, the whole structure needs to be cleaned up.
            For Each member In currentType.GetMembers()
                If member.Kind = SymbolKind.Field Then
                    Dim fieldType = DirectCast(member, FieldSymbol).Type
                    Debug.Assert(fieldType IsNot Nothing)

                    ' If this is not the current type (optimization) and also it was not analyzed before
                    If fieldType IsNot currentType Then

                        ' If the field need to be cleaned up, stop iterations
                        If LocalOrFieldNeedsToBeCleanedUp(fieldType) Then
                            _valueTypesCleanUpCache(currentType) = True
                            Return True
                        End If
                    End If

                End If
            Next

            ' Value type does not need to be cleaned-up if there are no fields requiring clean-up
            Debug.Assert(Not _valueTypesCleanUpCache(currentType))
            Return False
        End Function

        Public Overrides Function VisitWithLValueExpressionPlaceholder(node As BoundWithLValueExpressionPlaceholder) As BoundNode
            Return PlaceholderReplacement(node)
        End Function

        Public Overrides Function VisitWithRValueExpressionPlaceholder(node As BoundWithRValueExpressionPlaceholder) As BoundNode
            Return PlaceholderReplacement(node)
        End Function

    End Class
End Namespace
