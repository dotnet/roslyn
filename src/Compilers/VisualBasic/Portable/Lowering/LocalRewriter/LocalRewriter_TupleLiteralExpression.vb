' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter
        Public Overrides Function VisitTupleLiteral(node As BoundTupleLiteral) As BoundNode
            Return VisitTupleExpression(node)
        End Function

        Public Overrides Function VisitConvertedTupleLiteral(node As BoundConvertedTupleLiteral) As BoundNode
            Return VisitTupleExpression(node)
        End Function

        Private Function VisitTupleExpression(node As BoundTupleExpression) As BoundNode
            Dim rewrittenArguments As ImmutableArray(Of BoundExpression) = VisitList(node.Arguments)
            Return RewriteTupleCreationExpression(node, rewrittenArguments)
        End Function

        ''' <summary>
        ''' Converts the expression for creating a tuple instance into an expression creating a ValueTuple (if short) or nested ValueTuples (if longer).
        '''
        ''' For instance, for a long tuple we'll generate:
        ''' creationExpression(ctor=largestCtor, args=firstArgs+(nested creationExpression for remainder, with smaller ctor and next few args))
        ''' </summary>
        Private Function RewriteTupleCreationExpression(node As BoundTupleExpression, rewrittenArguments As ImmutableArray(Of BoundExpression)) As BoundExpression
            Return MakeTupleCreationExpression(node.Syntax, DirectCast(node.Type, NamedTypeSymbol), rewrittenArguments)
        End Function

        Private Function MakeTupleCreationExpression(syntax As SyntaxNode, type As NamedTypeSymbol, rewrittenArguments As ImmutableArray(Of BoundExpression)) As BoundExpression
            Dim underlyingTupleType As NamedTypeSymbol = If(type.TupleUnderlyingType, type)
            Debug.Assert(underlyingTupleType.IsTupleCompatible())

            Dim underlyingTupleTypeChain As ArrayBuilder(Of NamedTypeSymbol) = ArrayBuilder(Of NamedTypeSymbol).GetInstance()
            TupleTypeSymbol.GetUnderlyingTypeChain(underlyingTupleType, underlyingTupleTypeChain)

            Try
                ' make a creation expression for the smallest type
                Dim smallestType As NamedTypeSymbol = underlyingTupleTypeChain.Pop()
                Dim smallestCtorArguments As ImmutableArray(Of BoundExpression) = ImmutableArray.Create(rewrittenArguments,
                                                                                              underlyingTupleTypeChain.Count * (TupleTypeSymbol.RestPosition - 1),
                                                                                              smallestType.Arity)

                Dim smallestCtor As MethodSymbol = DirectCast(TupleTypeSymbol.GetWellKnownMemberInType(smallestType.OriginalDefinition,
                                                                                            TupleTypeSymbol.GetTupleCtor(smallestType.Arity),
                                                                                            _diagnostics,
                                                                                            syntax), MethodSymbol)
                If smallestCtor Is Nothing Then
                    Return New BoundBadExpression(
                                    syntax,
                                    LookupResultKind.Empty,
                                    ImmutableArray(Of Symbol).Empty,
                                    rewrittenArguments,
                                    type,
                                    hasErrors:=True)
                End If

                Dim smallestConstructor As MethodSymbol = smallestCtor.AsMember(smallestType)
                Dim currentCreation As BoundObjectCreationExpression = New BoundObjectCreationExpression(syntax, smallestConstructor, smallestCtorArguments, initializerOpt:=Nothing, type:=smallestType)

                Binder.CheckRequiredMembersInObjectInitializer(smallestConstructor, smallestType, initializers:=ImmutableArray(Of BoundExpression).Empty, syntax, _diagnostics)

                If underlyingTupleTypeChain.Count > 0 Then
                    Dim tuple8Type As NamedTypeSymbol = underlyingTupleTypeChain.Peek()
                    Dim tuple8Ctor As MethodSymbol = DirectCast(TupleTypeSymbol.GetWellKnownMemberInType(tuple8Type.OriginalDefinition,
                                                                                            TupleTypeSymbol.GetTupleCtor(TupleTypeSymbol.RestPosition),
                                                                                            _diagnostics,
                                                                                            syntax), MethodSymbol)

                    If tuple8Ctor Is Nothing Then
                        Return New BoundBadExpression(
                                    syntax,
                                    LookupResultKind.Empty,
                                    ImmutableArray(Of Symbol).Empty,
                                    rewrittenArguments,
                                    type,
                                    hasErrors:=True)
                    End If

                    Binder.CheckRequiredMembersInObjectInitializer(tuple8Ctor, tuple8Ctor.ContainingType, initializers:=ImmutableArray(Of BoundExpression).Empty, syntax, _diagnostics)

                    ' make successively larger creation expressions containing the previous one
                    Do
                        Dim ctorArguments As ImmutableArray(Of BoundExpression) = ImmutableArray.Create(rewrittenArguments,
                                                                                              (underlyingTupleTypeChain.Count - 1) * (TupleTypeSymbol.RestPosition - 1),
                                                                                              TupleTypeSymbol.RestPosition - 1).Add(currentCreation)

                        Dim constructor As MethodSymbol = tuple8Ctor.AsMember(underlyingTupleTypeChain.Pop())
                        currentCreation = New BoundObjectCreationExpression(syntax, constructor, ctorArguments, initializerOpt:=Nothing, type:=constructor.ContainingType)

                    Loop While (underlyingTupleTypeChain.Count > 0)

                End If

                currentCreation = currentCreation.Update(
                    currentCreation.ConstructorOpt,
                    methodGroupOpt:=Nothing,
                    arguments:=currentCreation.Arguments,
                    defaultArguments:=currentCreation.DefaultArguments,
                    initializerOpt:=currentCreation.InitializerOpt,
                    type:=type)

                Return currentCreation
            Finally
                underlyingTupleTypeChain.Free()
            End Try

        End Function
    End Class
End Namespace
