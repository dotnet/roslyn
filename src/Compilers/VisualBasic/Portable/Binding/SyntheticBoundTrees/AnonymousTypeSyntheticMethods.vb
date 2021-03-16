' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Partial Friend NotInheritable Class AnonymousTypeManager

        Partial Private Class AnonymousTypeConstructorSymbol
            Inherits SynthesizedConstructorBase

            Friend Overrides Function GetBoundMethodBody(compilationState As TypeCompilationState, diagnostics As BindingDiagnosticBag, Optional ByRef methodBodyBinder As Binder = Nothing) As BoundBlock
                methodBodyBinder = Nothing

                Dim syntax As SyntaxNode = Me.Syntax

                ' List of statements
                Dim statements = ArrayBuilder(Of BoundStatement).GetInstance()

                Dim anonymousType = DirectCast(Me.ContainingType, AnonymousTypeTemplateSymbol)
                Debug.Assert(anonymousType.Properties.Length = Me.ParameterCount)

                ' 'Me' reference
                Dim boundMeReference = New BoundMeReference(syntax, anonymousType).MakeCompilerGenerated()

                For index = 0 To Me.ParameterCount - 1

                    Dim [property] As AnonymousTypePropertySymbol = anonymousType.Properties(index)
                    Dim propertyType As TypeSymbol = [property].Type

                    '  Generate 'field' = 'parameter' statement
                    Dim fieldAccess = New BoundFieldAccess(syntax, boundMeReference, [property].AssociatedField, True, propertyType).MakeCompilerGenerated()
                    Dim parameter = New BoundParameter(syntax, Me._parameters(index), isLValue:=False, type:=propertyType).MakeCompilerGenerated()
                    Dim assignment = New BoundAssignmentOperator(syntax, fieldAccess, parameter, False, propertyType).MakeCompilerGenerated()
                    statements.Add(New BoundExpressionStatement(syntax, assignment).MakeCompilerGenerated())
                Next

                ' Final return statement
                statements.Add(New BoundReturnStatement(syntax, Nothing, Nothing, Nothing).MakeCompilerGenerated())

                ' Create a bound block 
                Return New BoundBlock(syntax, Nothing, ImmutableArray(Of LocalSymbol).Empty, statements.ToImmutableAndFree()).MakeCompilerGenerated()
            End Function
        End Class

        Partial Private NotInheritable Class AnonymousTypeEqualsMethodSymbol
            Inherits SynthesizedRegularMethodBase

            Friend Overrides Function GetBoundMethodBody(compilationState As TypeCompilationState, diagnostics As BindingDiagnosticBag, Optional ByRef methodBodyBinder As Binder = Nothing) As BoundBlock
                methodBodyBinder = Nothing

                Dim syntax As SyntaxNode = Me.Syntax

                ' 'Me' reference
                Dim boundMeReference As BoundMeReference = New BoundMeReference(syntax, AnonymousType).MakeCompilerGenerated()

                ' Argument 'obj' reference
                Dim boundObjReference As BoundParameter = New BoundParameter(syntax, Me._parameters(0), isLValue:=False,
                                                                             type:=AnonymousType.Manager.System_Object).MakeCompilerGenerated()

                ' TryCast(obj, <anonymous-type>)
                Dim boundTryCast As BoundExpression = New BoundTryCast(syntax, boundObjReference, ConversionKind.NarrowingReference,
                                                                       AnonymousType, Nothing).MakeCompilerGenerated()

                ' Call Me.Equals(TryCast(obj, <anonymous-type>))
                Dim boundCallToEquals As BoundExpression = New BoundCall(syntax, Me._iEquatableEqualsMethod, Nothing,
                                                                         boundMeReference, ImmutableArray.Create(Of BoundExpression)(boundTryCast),
                                                                         Nothing, AnonymousType.Manager.System_Boolean).MakeCompilerGenerated()

                ' Create a bound block 
                Return New BoundBlock(syntax, Nothing, ImmutableArray(Of LocalSymbol).Empty,
                                      ImmutableArray.Create(Of BoundStatement)(
                                          New BoundReturnStatement(syntax, boundCallToEquals, Nothing, Nothing).MakeCompilerGenerated()))
            End Function
        End Class

        Partial Private NotInheritable Class AnonymousTypeGetHashCodeMethodSymbol
            Inherits SynthesizedRegularMethodBase

            Friend Overrides Function GetBoundMethodBody(compilationState As TypeCompilationState, diagnostics As BindingDiagnosticBag, Optional ByRef methodBodyBinder As Binder = Nothing) As BoundBlock
                methodBodyBinder = Nothing

                Dim syntax As SyntaxNode = Me.Syntax

                Dim objectType As TypeSymbol = Me.AnonymousType.Manager.System_Object
                Dim getHashCodeMethod As MethodSymbol = Me.AnonymousType.Manager.System_Object__GetHashCode
                Dim integerType As TypeSymbol = Me.AnonymousType.Manager.System_Int32
                Dim booleanType As TypeSymbol = Me.AnonymousType.Manager.System_Boolean

                ' Generate Hash base

#If False Then
                ' NOTE: Following Dev10 behavior we ensure all anonymous types with the same field 
                '       names (case-insensitive) have the same hash base. Algorithm differs though...
                Dim builder = PooledStringBuilder.GetInstance()
                For Each [property] In Me.AnonymousType.Properties
                    If [property].IsReadOnly Then
                        builder.Builder.Append("|"c)
                        builder.Builder.Append([property].Name)
                    End If
                Next
                Dim hashBase As Integer = builder.ToStringAndFree().ToLower().GetHashCode()
#Else
                Dim properties = Me.AnonymousType.Properties
                Dim names(properties.Length - 1) As String
                For i = 0 To properties.Length - 1
                    names(i) = properties(i).Name
                Next
                Dim hashBase As Integer = CInt(CRC32.ComputeCRC32(names))
#End If

                ' 'Me' reference
                Dim boundMeReference = New BoundMeReference(syntax, AnonymousType).MakeCompilerGenerated()

                ' This variable accumulates the expression as we build it, final expression should be a bound 
                ' expression representing the following expression (only readonly=key fields are processed):
                '       '((...(( <hashBase> )*&HA5555529 + IF(field0 Is Nothing, 0, field0.GetHashCode())
                '                                                   )*&HA5555529 + IF(field1 Is Nothing, 0, field1.GetHashCode())
                '                                                       ...
                '                                                           )*&HA5555529 + IF(lastField Is Nothing, 0, lastField.GetHashCode()))'
                Dim expression As BoundExpression = Nothing

                ' <expression> ::= <hashBase>
                expression = New BoundLiteral(syntax, ConstantValue.Create(hashBase), integerType).MakeCompilerGenerated()

                Dim factorLiteral = New BoundLiteral(syntax, ConstantValue.Create(&HA5555529), integerType).MakeCompilerGenerated()
                Dim zeroLiteral = New BoundLiteral(syntax, ConstantValue.Create(0), integerType).MakeCompilerGenerated()
                Dim nothingLiteral = New BoundLiteral(syntax, ConstantValue.Nothing, objectType).MakeCompilerGenerated()

                For Each [property] In Me.AnonymousType.Properties
                    If [property].IsReadOnly Then

                        '  <expression> ::= <expression>*<factor>
                        expression = New BoundBinaryOperator(syntax, BinaryOperatorKind.Multiply,
                                                             expression, factorLiteral, False, integerType).MakeCompilerGenerated()

                        ' boundCondition ::= 'DirectCast(field, System.Object) is nothing'
                        Dim boundCondition = New BoundBinaryOperator(syntax, BinaryOperatorKind.Is,
                                                                     New BoundDirectCast(syntax,
                                                                                         New BoundFieldAccess(
                                                                                             syntax, boundMeReference,
                                                                                             [property].AssociatedField, False,
                                                                                             [property].Type).MakeCompilerGenerated(),
                                                                                         ConversionKind.WideningTypeParameter,
                                                                                         objectType, Nothing).MakeCompilerGenerated(),
                                                                     nothingLiteral, False, booleanType).MakeCompilerGenerated()

                        ' boundGetHashCode ::= 'field.GetHashCode()'
                        Dim boundGetHashCode = New BoundCall(syntax, getHashCodeMethod, Nothing,
                                                             New BoundFieldAccess(
                                                                 syntax, boundMeReference,
                                                                 [property].AssociatedField, False,
                                                                 [property].Type).MakeCompilerGenerated(),
                                                             ImmutableArray(Of BoundExpression).Empty,
                                                             Nothing, integerType).MakeCompilerGenerated()

                        ' boundTernaryConditional = IF(<boundCondition>, 0, <boundGetHashCode>)
                        Dim boundTernaryConditional = New BoundTernaryConditionalExpression(syntax,
                                                                                            boundCondition, zeroLiteral, boundGetHashCode,
                                                                                            Nothing, integerType).MakeCompilerGenerated()

                        '  <expression> ::= <expression> + <boundTernaryConditional>
                        expression = New BoundBinaryOperator(syntax, BinaryOperatorKind.Add, expression,
                                                             boundTernaryConditional, False, integerType).MakeCompilerGenerated()
                    End If
                Next

                ' Create a bound block 
                Return New BoundBlock(syntax, Nothing,
                                      ImmutableArray(Of LocalSymbol).Empty,
                                      ImmutableArray.Create(Of BoundStatement)(
                                          New BoundReturnStatement(syntax, expression, Nothing, Nothing).MakeCompilerGenerated()))
            End Function
        End Class

        Partial Private NotInheritable Class AnonymousType_IEquatable_EqualsMethodSymbol
            Inherits SynthesizedRegularMethodBase

            Friend Overrides Function GetBoundMethodBody(compilationState As TypeCompilationState, diagnostics As BindingDiagnosticBag, Optional ByRef methodBodyBinder As Binder = Nothing) As BoundBlock
                methodBodyBinder = Nothing

                Dim syntax As SyntaxNode = Me.Syntax

                Dim objectType As TypeSymbol = Me.AnonymousType.Manager.System_Object
                Dim booleanType As TypeSymbol = Me.AnonymousType.Manager.System_Boolean

                ' Locals
                Dim localMyFieldBoxed As LocalSymbol = New SynthesizedLocal(Me, objectType, SynthesizedLocalKind.LoweringTemp)
                Dim localOtherFieldBoxed As LocalSymbol = New SynthesizedLocal(Me, objectType, SynthesizedLocalKind.LoweringTemp)

                ' 'Me' reference
                Dim boundMeReference As BoundMeReference = New BoundMeReference(syntax, AnonymousType)

                ' Argument 'val' reference
                Dim boundValReference As BoundParameter = New BoundParameter(syntax, Me._parameters(0), isLValue:=False, type:=AnonymousType)

                ' 'Nothing' Literal to be reused 
                Dim nothingLiteral = New BoundLiteral(syntax, ConstantValue.Nothing, objectType).MakeCompilerGenerated()

                ' Build combined condition for all fields
                Dim combinedFieldCheck = BuildConditionsForFields(boundMeReference, boundValReference, nothingLiteral,
                                                                  localMyFieldBoxed, localOtherFieldBoxed, booleanType)

                ' Build 'val IsNot Nothing' condition
                Dim valIsNotNothing = BuildIsCheck(New BoundDirectCast(syntax, boundValReference,
                                                                       ConversionKind.WideningReference, objectType, Nothing).MakeCompilerGenerated(),
                                                   nothingLiteral, booleanType, reverse:=True)

                ' Final equality check: Me Is val OrElse (<valIsNotNothing> AndAlso <combinedFieldCheck>)
                Dim finalEqualityCheck = BuildAndAlso(valIsNotNothing, combinedFieldCheck, booleanType)

                Dim meIsValCheck = BuildIsCheck(boundMeReference, boundValReference, booleanType)
                finalEqualityCheck = BuildOrElse(meIsValCheck, finalEqualityCheck, booleanType)

                ' Create a bound block 
                Return New BoundBlock(syntax, Nothing,
                                      ImmutableArray.Create(localMyFieldBoxed, localOtherFieldBoxed),
                                      ImmutableArray.Create(Of BoundStatement)(
                                          New BoundReturnStatement(syntax, finalEqualityCheck, Nothing, Nothing).MakeCompilerGenerated()))
            End Function

            Private Function BuildConditionsForFields(boundMe As BoundMeReference, boundOther As BoundParameter, boundNothing As BoundExpression,
                                                      localMyFieldBoxed As LocalSymbol, localOtherFieldBoxed As LocalSymbol, booleanType As TypeSymbol) As BoundExpression

                ' <expression> will be build in a form of (only key fields are reported)
                '   (<condition-for-field-1> AndAlso <condition-for-field-2> OrElse ... AndAlso <condition-for-field-N> )
                Dim expression As BoundExpression = Nothing

                ' Process fields
                For Each [property] In AnonymousType.Properties
                    If [property].IsReadOnly Then
                        Dim condition As BoundExpression = BuildConditionForField([property], boundMe, boundOther, boundNothing, localMyFieldBoxed, localOtherFieldBoxed, booleanType)
                        expression = If(expression Is Nothing, condition, BuildAndAlso(expression, condition, booleanType))
                    End If
                Next

                Return expression
            End Function

            ''' <summary> 
            ''' Builds a condition in the following form: 
            ''' 
            ''' [preaction: localMyFieldBoxed = DirectCast(Me.field, System.Object)]
            ''' [preaction: localOtherFieldBoxed = DirectCast(Other.field, System.Object)]
            ''' IF(localMyFieldBoxed IsNot Nothing AndAlso localOtherFieldBoxed IsNot Nothing,
            '''    localMyFieldBoxed.Equals(localOtherFieldBoxed),
            '''    localMyFieldBoxed Is localOtherFieldBoxed
            ''' ) 
            ''' </summary>
            Private Function BuildConditionForField([property] As AnonymousTypePropertySymbol, boundMe As BoundMeReference, boundOther As BoundParameter,
                                                    boundNothing As BoundExpression, localMyFieldBoxed As LocalSymbol, localOtherFieldBoxed As LocalSymbol,
                                                    booleanType As TypeSymbol) As BoundExpression
                Dim field As FieldSymbol = [property].AssociatedField
                Dim syntax As SyntaxNode = Me.Syntax

                Dim boundLocalMyFieldBoxed = New BoundLocal(syntax, localMyFieldBoxed,
                                                            False, localMyFieldBoxed.Type).MakeCompilerGenerated()
                Dim boundLocalOtherFieldBoxed = New BoundLocal(syntax, localOtherFieldBoxed,
                                                               False, localOtherFieldBoxed.Type).MakeCompilerGenerated()

                Dim condition As BoundExpression = BuildAndAlso(BuildIsCheck(boundLocalMyFieldBoxed, boundNothing, booleanType, reverse:=True),
                                                                BuildIsCheck(boundLocalOtherFieldBoxed, boundNothing, booleanType, reverse:=True),
                                                                booleanType)

                ' TODO: Make sure we don't call GetObjectValue here
                Dim boundCallToEquals As BoundExpression = New BoundCall(syntax,
                                                                         Me.AnonymousType.Manager.System_Object__Equals, Nothing,
                                                                         boundLocalMyFieldBoxed,
                                                                         ImmutableArray.Create(Of BoundExpression)(boundLocalOtherFieldBoxed),
                                                                         Nothing, booleanType).MakeCompilerGenerated()

                Dim ternary As BoundExpression = New BoundTernaryConditionalExpression(syntax,
                                                                                       condition, boundCallToEquals,
                                                                                       BuildIsCheck(boundLocalMyFieldBoxed, boundLocalOtherFieldBoxed, booleanType),
                                                                                       Nothing, booleanType).MakeCompilerGenerated()

                Dim assignLocalMyField As BoundExpression = New BoundAssignmentOperator(syntax,
                                                                                        New BoundLocal(syntax, localMyFieldBoxed,
                                                                                                       True, localMyFieldBoxed.Type),
                                                                                        BuildBoxedFieldAccess(boundMe, field),
                                                                                        True, localMyFieldBoxed.Type).MakeCompilerGenerated()
                Dim assignLocalOtherField As BoundExpression = New BoundAssignmentOperator(syntax,
                                                                                           New BoundLocal(syntax, localOtherFieldBoxed,
                                                                                                          True, localOtherFieldBoxed.Type),
                                                                                           BuildBoxedFieldAccess(boundOther, field),
                                                                                           True, localOtherFieldBoxed.Type).MakeCompilerGenerated()

                Return New BoundSequence(syntax, ImmutableArray(Of LocalSymbol).Empty,
                                                         ImmutableArray.Create(Of BoundExpression)(assignLocalMyField, assignLocalOtherField),
                                                         ternary, ternary.Type).MakeCompilerGenerated()

            End Function

            Private Function BuildBoxedFieldAccess(receiver As BoundExpression, field As FieldSymbol) As BoundExpression
                Return New BoundDirectCast(Syntax,
                                           New BoundFieldAccess(Syntax, receiver, field, False, field.Type).MakeCompilerGenerated(),
                                           ConversionKind.WideningTypeParameter,
                                           Me.AnonymousType.Manager.System_Object, Nothing).MakeCompilerGenerated()
            End Function

            Private Function BuildIsCheck(left As BoundExpression, right As BoundExpression, booleanType As TypeSymbol, Optional reverse As Boolean = False) As BoundExpression
                Return New BoundBinaryOperator(Syntax,
                                               If(reverse, BinaryOperatorKind.IsNot, BinaryOperatorKind.Is),
                                               left, right, False, booleanType).MakeCompilerGenerated()
            End Function

            Private Function BuildAndAlso(left As BoundExpression, right As BoundExpression, booleanType As TypeSymbol) As BoundExpression
                Return New BoundBinaryOperator(Syntax, BinaryOperatorKind.AndAlso,
                                               left, right, False, booleanType).MakeCompilerGenerated()
            End Function

            Private Function BuildOrElse(left As BoundExpression, right As BoundExpression, booleanType As TypeSymbol) As BoundExpression
                Return New BoundBinaryOperator(Syntax, BinaryOperatorKind.OrElse,
                                               left, right, False, booleanType).MakeCompilerGenerated()
            End Function
        End Class

        Partial Private NotInheritable Class AnonymousTypeToStringMethodSymbol
            Inherits SynthesizedRegularMethodBase

            Friend Overrides Function GetBoundMethodBody(compilationState As TypeCompilationState, diagnostics As BindingDiagnosticBag, Optional ByRef methodBodyBinder As Binder = Nothing) As BoundBlock
                methodBodyBinder = Nothing

                Dim syntax As SyntaxNode = Me.Syntax

                Dim objectType As TypeSymbol = Me.AnonymousType.Manager.System_Object
                Dim stringType As TypeSymbol = Me.ReturnType

                Dim arrayOfObjectsType As TypeSymbol = Me.AnonymousType.Manager.Compilation.CreateArrayTypeSymbol(objectType)

                Dim numberOfFields As Integer = AnonymousType.Properties.Length

                ' Process all parameters, build string format pattern, build arguments
                Dim boundMeReference = New BoundMeReference(syntax, AnonymousType).MakeCompilerGenerated()
                Dim boundFieldAccessArray(numberOfFields - 1) As BoundExpression

                Dim formatStringBuilder = PooledStringBuilder.GetInstance()
                formatStringBuilder.Builder.Append("{{ ")
                For index = 0 To numberOfFields - 1
                    Dim [property] As AnonymousTypePropertySymbol = AnonymousType.Properties(index)

                    '  format pattern
                    formatStringBuilder.Builder.AppendFormat(If(index = 0, "{0} = {{{1}}}", ", {0} = {{{1}}}"), [property].MetadataName, index)

                    '  put the field accessor to boundFieldAccessArray
                    boundFieldAccessArray(index) = New BoundDirectCast(syntax,
                                                                       New BoundFieldAccess(syntax, boundMeReference, [property].AssociatedField,
                                                                                            False, [property].Type).MakeCompilerGenerated(),
                                                                       ConversionKind.WideningTypeParameter, objectType, Nothing).MakeCompilerGenerated()
                Next
                formatStringBuilder.Builder.Append(" }}")
                Dim formatString As String = formatStringBuilder.ToStringAndFree()

                '  array initializer:  { field0, field1, ... }
                Dim boundArrayInitializer As BoundArrayInitialization = New BoundArrayInitialization(syntax, boundFieldAccessArray.AsImmutableOrNull(),
                                                                                                     arrayOfObjectsType).MakeCompilerGenerated()
                ' New Object(numberOfFields - 1) { field0, field1, ... }
                Dim arrayInstantiation As BoundExpression = New BoundArrayCreation(syntax,
                                                                                   ImmutableArray.Create(Of BoundExpression)(
                                                                                       New BoundLiteral(syntax, ConstantValue.Create(numberOfFields),
                                                                                                        Me.AnonymousType.Manager.System_Int32).MakeCompilerGenerated()),
                                                                                   boundArrayInitializer, arrayOfObjectsType).MakeCompilerGenerated()

                ' String.Format(<formatPattern>, New Object(numberOfFields - 1) { field0, field1, ... })
                Dim formatMethod = Me.AnonymousType.Manager.System_String__Format_IFormatProvider
                Dim [call] As BoundExpression = New BoundCall(syntax, formatMethod, Nothing, Nothing,
                                                              ImmutableArray.Create(Of BoundExpression)(
                                                                        New BoundLiteral(syntax, ConstantValue.Nothing,
                                                                                         formatMethod.Parameters(0).Type).MakeCompilerGenerated(),
                                                                        New BoundLiteral(syntax, ConstantValue.Create(formatString),
                                                                                         stringType).MakeCompilerGenerated(),
                                                                        arrayInstantiation),
                                                              Nothing, stringType).MakeCompilerGenerated()

                ' Create a bound block 
                Return New BoundBlock(syntax, Nothing,
                                      ImmutableArray(Of LocalSymbol).Empty,
                                      ImmutableArray.Create(Of BoundStatement)(
                                          New BoundReturnStatement(syntax, [call], Nothing, Nothing).MakeCompilerGenerated()))
            End Function
        End Class

    End Class

End Namespace
