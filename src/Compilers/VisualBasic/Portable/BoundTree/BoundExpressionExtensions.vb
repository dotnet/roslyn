' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq
Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Module BoundExpressionExtensions

        ' returns true when expression has no side-effects and produces
        ' default value (null, zero, false, default(T) ...)
        ' NOTE: This method is a very shallow check.
        '       It does not make any assumptions about what this node could become 
        '       after some folding/propagation/algebraic transformations.
        <Extension>
        Public Function IsDefaultValue(node As BoundExpression) As Boolean
            Dim constValue As ConstantValue = node.ConstantValueOpt
            If constValue IsNot Nothing AndAlso constValue.IsDefaultValue Then
                Return True
            End If

            'TODO: I have seen other places where we do similar digging.
            '      It seems a bit bug-prone. What if Nothing is wrapped in more than one conversion or a parenthesized or whatever...? 
            '      Perhaps it may be worth it to introduce BoundDefault and reduce the "default value" patterns to 
            '      a node with unambiguous meaning?

            ' there is no BoundDefault node in VB, 'default" is represented through several means
            ' so we need to match several patterns.
            Select Case node.Kind
                Case BoundKind.Conversion
                    constValue = DirectCast(node, BoundConversion).Operand.ConstantValueOpt
                    Return constValue IsNot Nothing AndAlso constValue.IsNothing

                Case BoundKind.DirectCast
                    ' DirectCast(Nothing, <ValueType>) is emitted as an unbox on a null reference.
                    ' It is not equivalent to a default value
                    If node.Type.IsTypeParameter() OrElse Not node.Type.IsValueType Then
                        constValue = DirectCast(node, BoundDirectCast).Operand.ConstantValueOpt
                        Return constValue IsNot Nothing AndAlso constValue.IsNothing
                    End If

                Case BoundKind.TryCast
                    constValue = DirectCast(node, BoundTryCast).Operand.ConstantValueOpt
                    Return constValue IsNot Nothing AndAlso constValue.IsNothing

                Case BoundKind.ObjectCreationExpression
                    Dim ctor = DirectCast(node, BoundObjectCreationExpression).ConstructorOpt
                    Return ctor Is Nothing OrElse ctor.IsDefaultValueTypeConstructor()

            End Select

            Return False
        End Function

        ' Is this a kind of bound node that can act as a value? This include variables, but does not
        ' include things like types or namespaces.
        <Extension()>
        Public Function IsValue(node As BoundExpression) As Boolean
            Select Case node.Kind
                Case BoundKind.Parenthesized
                    Return DirectCast(node, BoundParenthesized).Expression.IsValue

                Case BoundKind.BadExpression
                    Return node.Type IsNot Nothing

                Case BoundKind.TypeExpression,
                    BoundKind.NamespaceExpression,
                    BoundKind.MethodGroup,
                    BoundKind.PropertyGroup,
                    BoundKind.ArrayInitialization,
                    BoundKind.TypeArguments,
                    BoundKind.Label,
                    BoundKind.EventAccess
                    Return False

                Case Else
                    Return True
            End Select
        End Function

        <Extension()>
        Public Function IsMeReference(node As BoundExpression) As Boolean
            Return node.Kind = BoundKind.MeReference
        End Function

        <Extension()>
        Public Function IsMyBaseReference(node As BoundExpression) As Boolean
            Return node.Kind = BoundKind.MyBaseReference
        End Function

        <Extension()>
        Public Function IsMyClassReference(node As BoundExpression) As Boolean
            Return node.Kind = BoundKind.MyClassReference
        End Function

        ''' <summary> Returns True if the node specified is one of Me/MyClass/MyBase </summary>
        <Extension()>
        Public Function IsInstanceReference(node As BoundExpression) As Boolean
            Return node.IsMeReference OrElse node.IsMyBaseReference OrElse node.IsMyClassReference
        End Function

        ''' <summary>
        ''' Returns True if the expression is a property access expression,
        ''' either directly or wrapped in an XML member access expression.
        ''' </summary>
        <Extension()>
        Public Function IsPropertyOrXmlPropertyAccess(node As BoundExpression) As Boolean
            Select Case node.Kind
                Case BoundKind.XmlMemberAccess
                    Return DirectCast(node, BoundXmlMemberAccess).MemberAccess.IsPropertyOrXmlPropertyAccess()

                Case BoundKind.PropertyAccess
                    Return True

                Case Else
                    Return False

            End Select
        End Function

        <Extension()>
        Public Function IsPropertyReturnsByRef(node As BoundExpression) As Boolean
            Return node.Kind = BoundKind.PropertyAccess AndAlso
                DirectCast(node, BoundPropertyAccess).PropertySymbol.ReturnsByRef
        End Function

        <Extension()>
        Public Function IsLateBound(node As BoundExpression) As Boolean
            Select Case node.Kind
                Case BoundKind.LateMemberAccess,
                    BoundKind.LateInvocation
                    Return True

                Case Else
                    Return False
            End Select
        End Function

        <Extension()>
        Public Function GetTypeOfAssignmentTarget(node As BoundExpression) As TypeSymbol
            Debug.Assert(node.IsSupportingAssignment())

            If node.Kind = BoundKind.PropertyAccess Then
                Return DirectCast(node, BoundPropertyAccess).PropertySymbol.GetTypeFromSetMethod()
            End If

            Return node.Type
        End Function

        <Extension()>
        Public Function GetPropertyOrXmlProperty(node As BoundExpression) As PropertySymbol
            Select Case node.Kind
                Case BoundKind.XmlMemberAccess
                    Return DirectCast(node, BoundXmlMemberAccess).MemberAccess.GetPropertyOrXmlProperty()

                Case BoundKind.PropertyAccess
                    Return DirectCast(node, BoundPropertyAccess).PropertySymbol

                Case Else
                    Return Nothing

            End Select
        End Function

        ''' <summary>
        ''' Does this node represent a property with Set accessor and AccessKind not yet bound to Get?
        ''' </summary>
        <Extension()>
        Public Function IsPropertySupportingAssignment(node As BoundExpression) As Boolean
            Select Case node.Kind
                Case BoundKind.XmlMemberAccess
                    Return DirectCast(node, BoundXmlMemberAccess).MemberAccess.IsPropertySupportingAssignment()

                Case BoundKind.PropertyAccess
                    Dim propertyAccess = DirectCast(node, BoundPropertyAccess)

                    If propertyAccess.AccessKind = PropertyAccessKind.Get Then
                        Return False
                    End If

                    Return propertyAccess.IsWriteable

                Case Else
                    Return False

            End Select
        End Function

        ''' <summary>
        ''' Does this node represent a property or latebound access not yet determined to be Get?
        ''' </summary>
        <Extension()>
        Public Function IsSupportingAssignment(node As BoundExpression) As Boolean
            If node Is Nothing Then
                Return False
            End If

            If node.IsLValue Then
                Return True
            End If

            Select Case node.Kind
                Case BoundKind.LateMemberAccess
                    Dim member = DirectCast(node, BoundLateMemberAccess)
                    Return member.AccessKind <> LateBoundAccessKind.Get AndAlso member.AccessKind <> LateBoundAccessKind.Call

                Case BoundKind.LateInvocation
                    Dim invocation = DirectCast(node, BoundLateInvocation)

                    If invocation.AccessKind = LateBoundAccessKind.Unknown Then
                        Dim group = invocation.MethodOrPropertyGroupOpt

                        If group IsNot Nothing AndAlso group.Kind = BoundKind.MethodGroup Then
                            ' latebound invocation of a method group is considered not assignable
                            ' NOTE: interestingly, property group is considered assignable even if all properties are readonly.
                            Return False
                        End If
                    End If

                    Return invocation.AccessKind <> LateBoundAccessKind.Get AndAlso invocation.AccessKind <> LateBoundAccessKind.Call

                Case BoundKind.LateBoundArgumentSupportingAssignmentWithCapture
                    Return True

                Case Else
                    Return IsPropertySupportingAssignment(node)

            End Select
        End Function

        ''' <summary>
        ''' Get the access kind from property access expression.
        ''' </summary>
        <Extension()>
        Public Function GetAccessKind(node As BoundExpression) As PropertyAccessKind
            Select Case node.Kind
                Case BoundKind.XmlMemberAccess
                    Return DirectCast(node, BoundXmlMemberAccess).MemberAccess.GetAccessKind()

                Case BoundKind.PropertyAccess
                    Return DirectCast(node, BoundPropertyAccess).AccessKind

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(node.Kind)

            End Select
        End Function

        <Extension()>
        Public Function GetLateBoundAccessKind(node As BoundExpression) As LateBoundAccessKind
            Select Case node.Kind
                Case BoundKind.LateMemberAccess
                    Return DirectCast(node, BoundLateMemberAccess).AccessKind

                Case BoundKind.LateInvocation
                    Return DirectCast(node, BoundLateInvocation).AccessKind

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(node.Kind)

            End Select
        End Function

        ''' <summary>
        ''' Sets the access kind on the property access expression. To clear the access
        ''' kind, 'newAccessKind' should be Unknown. Otherwise, the current property
        ''' access kind should be Unknown or equal to 'newAccessKind'.
        ''' </summary>
        <Extension()>
        Public Function SetAccessKind(node As BoundExpression, newAccessKind As PropertyAccessKind) As BoundExpression
            Select Case node.Kind
                Case BoundKind.XmlMemberAccess
                    Dim memberAccess = DirectCast(node, BoundXmlMemberAccess)
                    Return memberAccess.SetAccessKind(newAccessKind)

                Case BoundKind.PropertyAccess
                    Dim propertyAccess = DirectCast(node, BoundPropertyAccess)
                    Debug.Assert(Not propertyAccess.PropertySymbol.ReturnsByRef OrElse (newAccessKind And PropertyAccessKind.Set) = 0)
                    Return propertyAccess.SetAccessKind(newAccessKind)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(node.Kind)

            End Select
        End Function

        <Extension()>
        Public Function SetLateBoundAccessKind(node As BoundExpression, newAccessKind As LateBoundAccessKind) As BoundExpression
            Select Case node.Kind
                Case BoundKind.LateMemberAccess
                    Return DirectCast(node, BoundLateMemberAccess).SetAccessKind(newAccessKind)

                Case BoundKind.LateInvocation
                    Return DirectCast(node, BoundLateInvocation).SetAccessKind(newAccessKind)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(node.Kind)
            End Select
        End Function

        <Extension()>
        Public Function SetAccessKind(node As BoundXmlMemberAccess, newAccessKind As PropertyAccessKind) As BoundXmlMemberAccess
            Dim expr = node.MemberAccess.SetAccessKind(newAccessKind)
            Return node.Update(expr)
        End Function

        <Extension()>
        Public Function SetGetSetAccessKindIfAppropriate(node As BoundExpression) As BoundExpression
            Select Case node.Kind
                Case BoundKind.XmlMemberAccess
                    Dim memberAccess = DirectCast(node, BoundXmlMemberAccess)
                    Return memberAccess.SetAccessKind(PropertyAccessKind.Get Or PropertyAccessKind.Set)

                Case BoundKind.PropertyAccess
                    Dim propertyAccess = DirectCast(node, BoundPropertyAccess)
                    Dim accessKind = If(propertyAccess.PropertySymbol.ReturnsByRef, PropertyAccessKind.Get, PropertyAccessKind.Get Or PropertyAccessKind.Set)
                    Return propertyAccess.SetAccessKind(accessKind)

                Case BoundKind.LateMemberAccess
                    Return DirectCast(node, BoundLateMemberAccess).SetAccessKind(LateBoundAccessKind.Get Or LateBoundAccessKind.Set)

                Case BoundKind.LateInvocation
                    Return DirectCast(node, BoundLateInvocation).SetAccessKind(LateBoundAccessKind.Get Or LateBoundAccessKind.Set)

                Case Else
                    Return node

            End Select
        End Function

        ''' <summary>
        ''' Return a BoundXmlMemberAccess node with
        ''' updated MemberAccess property.
        ''' </summary>
        <Extension()>
        Public Function Update(node As BoundXmlMemberAccess, memberAccess As BoundExpression) As BoundXmlMemberAccess
            Return node.Update(memberAccess, memberAccess.Type)
        End Function

        ''' <summary>
        ''' Return true if and only if an expression is a integral literal with a value of zero.
        ''' Non-literal constant value zero does not qualify.
        ''' </summary>
        <Extension()>
        Public Function IsIntegerZeroLiteral(node As BoundExpression) As Boolean
            ' Dev10 treats parenthesized 0 as a literal.
            While node.Kind = BoundKind.Parenthesized
                node = DirectCast(node, BoundParenthesized).Expression
            End While

            Return node.Kind = BoundKind.Literal AndAlso
                IsIntegerZeroLiteral(DirectCast(node, BoundLiteral))
        End Function

        ''' <summary>
        ''' Return true if and only if an expression is a integral literal with a value of zero.
        ''' Non-literal constant value zero does not qualify.
        ''' </summary>
        <Extension()>
        Public Function IsIntegerZeroLiteral(node As BoundLiteral) As Boolean
            Debug.Assert(node.Value.IsBad OrElse node.Type.IsValidForConstantValue(node.Value))

            If node.Value.Discriminator = ConstantValueTypeDiscriminator.Int32 AndAlso node.Type.SpecialType = SpecialType.System_Int32 Then
                Return node.Value.Int32Value = 0
            End If

            Return False
        End Function

        ''' <summary>
        ''' Checks if the expression is a default value (0 or Nothing)
        ''' </summary>
        <Extension()>
        Public Function IsDefaultValueConstant(expr As BoundExpression) As Boolean
            Dim c = expr.ConstantValueOpt
            Return c IsNot Nothing AndAlso c.IsDefaultValue
        End Function

        ''' <summary>
        ''' Checks if the expression is a constant and that constant is False
        ''' </summary>
        <Extension()>
        Public Function IsTrueConstant(expr As BoundExpression) As Boolean
            Return expr.ConstantValueOpt Is ConstantValue.True
        End Function

        ''' <summary>
        ''' Checks if the expression is a constant and that constant is True
        ''' </summary>
        <Extension()>
        Public Function IsFalseConstant(expr As BoundExpression) As Boolean
            Return expr.ConstantValueOpt Is ConstantValue.False
        End Function

        ''' <summary>
        ''' Checks if the expression is a negative integer constant value.
        ''' </summary>
        <Extension()>
        Public Function IsNegativeIntegerConstant(expression As BoundExpression) As Boolean
            Debug.Assert(expression IsNot Nothing)

            If expression.GetIntegerConstantValue() < 0 Then
                Return True
            End If
            Return False
        End Function

        ''' <summary>
        ''' Return the integer constant value (if any) from a BoundExpression
        ''' </summary>
        <Extension()>
        Public Function GetIntegerConstantValue(expression As BoundExpression) As Integer?
            Debug.Assert(expression IsNot Nothing)

            If Not expression.HasErrors AndAlso expression.IsConstant Then
                Dim type As SpecialType = expression.Type.SpecialType
                Select Case type
                    Case SpecialType.System_Int16
                        Return expression.ConstantValueOpt.Int16Value

                    Case SpecialType.System_Int32
                        Return expression.ConstantValueOpt.Int32Value

                    Case SpecialType.System_Int64
                        If expression.ConstantValueOpt.Int64Value <= Integer.MaxValue AndAlso expression.ConstantValueOpt.Int64Value >= Integer.MinValue Then
                            Return CInt(expression.ConstantValueOpt.Int64Value)
                        End If
                        Return Nothing
                End Select
            End If
            Return Nothing
        End Function

        ''' <summary>
        ''' Return true if and only if an expression is a semantical Nothing literal, 
        ''' which is defined as follows (the definition is consistent with 
        ''' definition used by Dev10 compiler):
        ''' - A Nothing literal according to the language grammar, or
        ''' - A parenthesized expression, for which IsNothingLiteral returns true, or
        ''' - An expression of type Object with constant value == Nothing.
        ''' </summary>
        <Extension()>
        Public Function IsNothingLiteral(node As BoundExpression) As Boolean
            Dim type = node.Type

            If type Is Nothing OrElse type.SpecialType = SpecialType.System_Object Then
                Dim constantValue = node.ConstantValueOpt
                If constantValue IsNot Nothing AndAlso constantValue.IsNothing Then
                    Debug.Assert(type IsNot Nothing OrElse node.Kind = BoundKind.Literal OrElse node.Kind = BoundKind.Parenthesized)

                    Return True
                End If
            End If

            Return False
        End Function

        ''' <summary>
        ''' Return true if target BoundLiteral represents Nothing literal as defined by the language grammar.
        ''' </summary>
        <Extension()>
        Public Function IsNothingLiteral(node As BoundLiteral) As Boolean
            If node.Value.IsNothing Then
                Debug.Assert(node.Type Is Nothing)
                Return node.Type Is Nothing
            End If

            Return False
        End Function

        ''' <summary>
        ''' Return true if and only if an expression represents optionally 
        ''' parenthesized Nothing literal as defined by the language grammar. 
        ''' I.e. implicit conversions are Ok, but explicit conversions aren't.
        ''' </summary>
        <Extension()>
        Public Function IsStrictNothingLiteral(node As BoundExpression) As Boolean

            If Not IsNothingLiteral(node) Then
                Return False
            End If

            Dim constantValue As ConstantValue

            ' Dev10 treats parenthesized NOTHING as a literal.
            Do
                Select Case node.Kind
                    Case BoundKind.Literal
                        Return IsNothingLiteral(DirectCast(node, BoundLiteral))

                    Case BoundKind.Parenthesized
                        Dim parenthesized = DirectCast(node, BoundParenthesized)
                        node = parenthesized.Expression

                    Case BoundKind.Conversion

                        Dim conversion = DirectCast(node, BoundConversion)
                        constantValue = conversion.ConstantValueOpt

                        If Not (Not conversion.ExplicitCastInCode AndAlso
                                constantValue IsNot Nothing AndAlso constantValue.IsNothing) Then
                            Return False
                        End If

                        node = conversion.Operand

                    Case Else
                        Return False
                End Select
            Loop

        End Function

        <Extension()>
        Public Function GetMostEnclosedParenthesizedExpression(expression As BoundExpression) As BoundExpression
            While expression.Kind = BoundKind.Parenthesized
                expression = DirectCast(expression, BoundParenthesized).Expression
            End While
            Return expression
        End Function

        <Extension()>
        Public Function HasExpressionSymbols(node As BoundExpression) As Boolean
            Select Case node.Kind
                Case BoundKind.Call,
                    BoundKind.Local,
                    BoundKind.RangeVariable,
                    BoundKind.FieldAccess,
                    BoundKind.PropertyAccess,
                    BoundKind.EventAccess,
                    BoundKind.MethodGroup,
                    BoundKind.PropertyGroup,
                    BoundKind.ObjectCreationExpression,
                    BoundKind.TypeExpression,
                    BoundKind.NamespaceExpression,
                    BoundKind.Conversion
                    Return True

                Case BoundKind.BadExpression
                    Return DirectCast(node, BoundBadExpression).Symbols.Length > 0

                Case Else
                    Return False
            End Select
        End Function

        <Extension()>
        Public Sub GetExpressionSymbols(methodGroup As BoundMethodGroup, symbols As ArrayBuilder(Of Symbol))
            Dim targetArity As Integer = 0

            If methodGroup.TypeArgumentsOpt IsNot Nothing Then
                targetArity = methodGroup.TypeArgumentsOpt.Arguments.Length
            End If

            For Each method In methodGroup.Methods
                ' This is a quick fix for the fact that binder lookup in VB does not perform arity check for 
                '       method symbols leaving it to overload resolution code. Here we filter wrong arity methods
                If targetArity = 0 Then
                    symbols.Add(method)
                ElseIf targetArity = method.Arity Then
                    symbols.Add(method.Construct(methodGroup.TypeArgumentsOpt.Arguments))
                End If
            Next

            ' Merge methodGroup.AdditionalExtensionMethods into the result.
            For Each method In methodGroup.AdditionalExtensionMethods(useSiteInfo:=CompoundUseSiteInfo(Of AssemblySymbol).Discarded)
                ' This is a quick fix for the fact that binder lookup in VB does not perform arity check for 
                '       method symbols leaving it to overload resolution code. Here we filter wrong arity methods
                If targetArity = 0 Then
                    symbols.Add(method)
                ElseIf targetArity = method.Arity Then
                    symbols.Add(method.Construct(methodGroup.TypeArgumentsOpt.Arguments))
                End If
            Next
        End Sub

        <Extension()>
        Public Sub GetExpressionSymbols(node As BoundExpression, symbols As ArrayBuilder(Of Symbol))
            Select Case node.Kind
                Case BoundKind.MethodGroup
                    DirectCast(node, BoundMethodGroup).GetExpressionSymbols(symbols)

                Case BoundKind.PropertyGroup
                    symbols.AddRange(DirectCast(node, BoundPropertyGroup).Properties)

                Case BoundKind.BadExpression
                    symbols.AddRange(DirectCast(node, BoundBadExpression).Symbols)

                Case BoundKind.QueryClause
                    DirectCast(node, BoundQueryClause).UnderlyingExpression.GetExpressionSymbols(symbols)

                Case BoundKind.AggregateClause
                    DirectCast(node, BoundAggregateClause).UnderlyingExpression.GetExpressionSymbols(symbols)

                Case BoundKind.Ordering
                    DirectCast(node, BoundOrdering).UnderlyingExpression.GetExpressionSymbols(symbols)

                Case BoundKind.QuerySource
                    DirectCast(node, BoundQuerySource).Expression.GetExpressionSymbols(symbols)

                Case BoundKind.ToQueryableCollectionConversion
                    DirectCast(node, BoundToQueryableCollectionConversion).ConversionCall.GetExpressionSymbols(symbols)

                Case BoundKind.QueryableSource
                    DirectCast(node, BoundQueryableSource).Source.GetExpressionSymbols(symbols)

                Case Else
                    Dim symbol = node.ExpressionSymbol
                    If symbol IsNot Nothing Then
                        If symbol.Kind = SymbolKind.Namespace AndAlso DirectCast(symbol, NamespaceSymbol).NamespaceKind = NamespaceKindNamespaceGroup Then
                            symbols.AddRange(DirectCast(symbol, NamespaceSymbol).ConstituentNamespaces)
                        Else
                            symbols.Add(symbol)
                        End If
                    End If
            End Select
        End Sub

        <Extension()>
        Public Function ToStatement(node As BoundExpression) As BoundExpressionStatement
            Return New BoundExpressionStatement(node.Syntax, node, node.HasErrors)
        End Function

        <Extension()> <Conditional("DEBUG")>
        Public Sub AssertRValue(node As BoundExpression)
            If Not node.HasErrors Then
                Debug.Assert(node.IsValue)
                Debug.Assert(Not node.IsLValue)
                Debug.Assert(Not node.IsPropertyOrXmlPropertyAccess() OrElse node.GetAccessKind() = PropertyAccessKind.Get)
                Debug.Assert(Not node.IsLateBound() OrElse node.GetLateBoundAccessKind() = LateBoundAccessKind.Get)
                Debug.Assert(If(node.Type Is Nothing,
                                node.IsNothingLiteral() OrElse
                                    node.GetMostEnclosedParenthesizedExpression().Kind = BoundKind.AddressOfOperator OrElse
                                    node.GetMostEnclosedParenthesizedExpression().Kind = BoundKind.Lambda OrElse
                                    node.Kind = BoundKind.QueryLambda,
                                Not node.Type.IsVoidType()))
            End If
        End Sub

        ''' <summary>
        ''' returns type arguments or Nothing if group does not have type arguments.
        ''' </summary>
        <Extension()>
        Friend Function TypeArguments(this As BoundMethodOrPropertyGroup) As BoundTypeArguments
            Dim asMethodGroup = TryCast(this, BoundMethodGroup)
            If asMethodGroup IsNot Nothing Then
                Return asMethodGroup.TypeArgumentsOpt
            End If

            Return Nothing
        End Function
    End Module
End Namespace

