' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class Binder

        ''' <summary>
        ''' Used to store the bound field and property initializers and the associated list of
        ''' bound assignment statements because they are reused for multiple constructors
        ''' </summary>
        Friend NotInheritable Class ProcessedFieldOrPropertyInitializers
            Friend ReadOnly BoundInitializers As ImmutableArray(Of BoundInitializer)

            ''' <summary> 
            ''' Indicate the fact that binding of initializers produced a tree with errors. 
            ''' This property does not indicate whether or not a diagnostic was produced during the 
            ''' binding of the initializers. 
            ''' </summary>
            Friend ReadOnly HasAnyErrors As Boolean

            Private _loweredInitializers As ImmutableArray(Of BoundStatement)
            Friend Property InitializerStatements As ImmutableArray(Of BoundStatement)
                Get
                    Return _loweredInitializers
                End Get
                Set(value As ImmutableArray(Of BoundStatement))
                    Debug.Assert(Not value.IsDefault)
                    Debug.Assert(_loweredInitializers.IsDefault)
                    _loweredInitializers = value
                End Set
            End Property

            Friend Shared ReadOnly Empty As ProcessedFieldOrPropertyInitializers = New ProcessedFieldOrPropertyInitializers()

            Private Sub New()
                Me.BoundInitializers = ImmutableArray(Of BoundInitializer).Empty
                Me.HasAnyErrors = False
                Me._loweredInitializers = ImmutableArray(Of BoundStatement).Empty
            End Sub

            Friend Sub New(boundInitializers As ImmutableArray(Of BoundInitializer))
                Debug.Assert(Not boundInitializers.IsDefault)
                Me.BoundInitializers = boundInitializers
                Me.HasAnyErrors = boundInitializers.Any(Function(i) i.HasErrors)
            End Sub

            Private _analyzed As Boolean = False
            Friend Sub EnsureInitializersAnalyzed(method As MethodSymbol, diagnostics As DiagnosticBag)
                Debug.Assert(method IsNot Nothing)

                If Not _analyzed Then
                    If Not Me.BoundInitializers.IsEmpty Then
                        ' Create a dummy block
                        Dim block As New BoundBlock(Me.BoundInitializers(0).Syntax,
                                                    Nothing,
                                                    ImmutableArray(Of LocalSymbol).Empty,
                                                    StaticCast(Of BoundStatement).From(Me.BoundInitializers))

                        Analyzer.AnalyzeMethodBody(method, block, diagnostics)
                        DiagnosticsPass.IssueDiagnostics(block, diagnostics, method)
                    End If

                    _analyzed = True
                End If
            End Sub
        End Class

        ''' <summary>
        ''' Binds all field initializers of a <see cref="SourceNamedTypeSymbol"/>.
        ''' </summary>
        ''' <param name="symbol">The named type symbol where the field initializers are declared.</param>
        ''' <param name="scriptInitializerOpt">Script initializer or Nothing if not binding top-level statements.</param>
        ''' <param name="initializers">The initializers itself. For each partial type declaration there is an array of 
        ''' field initializers</param>
        ''' <param name="diagnostics">The diagnostics.</param>
        Friend Shared Function BindFieldAndPropertyInitializers(
            symbol As SourceMemberContainerTypeSymbol,
            initializers As ImmutableArray(Of ImmutableArray(Of FieldOrPropertyInitializer)),
            scriptInitializerOpt As SynthesizedInteractiveInitializerMethod,
            diagnostics As DiagnosticBag
        ) As ImmutableArray(Of BoundInitializer)
            Debug.Assert((scriptInitializerOpt IsNot Nothing) = symbol.IsScriptClass)

            If initializers.IsDefaultOrEmpty Then
                Return ImmutableArray(Of BoundInitializer).Empty
            End If

            Dim moduleSymbol = DirectCast(symbol.ContainingModule, SourceModuleSymbol)
            Dim compilation = moduleSymbol.ContainingSourceAssembly.DeclaringCompilation

            Dim boundInitializers = ArrayBuilder(Of BoundInitializer).GetInstance()
            For i = 0 To initializers.Length - 1
                Dim siblingInitializers = initializers(i)

                ' All sibling initializers share the same parent node and tree
                ' so we can reuse the parent binder across siblings.
                Dim parentBinder As Binder = Nothing

                For j = 0 To siblingInitializers.Length - 1
                    Dim initializer = siblingInitializers(j)

                    If Not initializer.FieldsOrProperties.IsDefault AndAlso initializer.FieldsOrProperties.First.ContainingType.IsEnumType Then
                        Continue For
                    End If

                    Dim syntaxRef = initializer.Syntax
                    Dim syntaxTree = syntaxRef.SyntaxTree
                    Dim initializerNode = DirectCast(syntaxRef.GetSyntax(), VisualBasicSyntaxNode)

                    If parentBinder Is Nothing Then
                        ' use binder for type, not ctor - no access to ctor parameters
                        parentBinder = BinderBuilder.CreateBinderForType(moduleSymbol, syntaxTree, symbol)

                        If scriptInitializerOpt IsNot Nothing Then
                            parentBinder = New TopLevelCodeBinder(scriptInitializerOpt, parentBinder)
                        End If
                    Else
                        Debug.Assert(parentBinder.SyntaxTree Is syntaxTree, "sibling initializer array contains initializers from two different syntax trees.")
                    End If

                    If initializer.FieldsOrProperties.IsDefault Then
                        ' use the binder of the Script class for global statements
                        Dim isLast = (i = initializers.Length - 1 AndAlso j = siblingInitializers.Length - 1)
                        boundInitializers.Add(parentBinder.BindGlobalStatement(scriptInitializerOpt, DirectCast(initializerNode, StatementSyntax), diagnostics, isLast))
                        Continue For
                    End If

                    Dim firstFieldOrProperty = initializer.FieldsOrProperties.First
                    Dim initializerBinder = BinderBuilder.CreateBinderForInitializer(parentBinder, firstFieldOrProperty)
                    If initializerNode.Kind = SyntaxKind.ModifiedIdentifier Then
                        ' Array field with no explicit initializer.
                        Debug.Assert(initializer.FieldsOrProperties.Length = 1)
                        Debug.Assert(firstFieldOrProperty.Kind = SymbolKind.Field)

                        Dim fieldSymbol = DirectCast(firstFieldOrProperty, SourceFieldSymbol)
                        Debug.Assert(fieldSymbol.HasDeclaredType)
                        Debug.Assert(fieldSymbol.Type.IsArrayType())

                        initializerBinder.BindArrayFieldImplicitInitializer(fieldSymbol, boundInitializers, diagnostics)
                    ElseIf firstFieldOrProperty.Kind = SymbolKind.Field Then
                        Dim fieldSymbol = DirectCast(firstFieldOrProperty, SourceFieldSymbol)
                        If fieldSymbol.IsConst Then
                            ' Bind constant to ensure diagnostics are captured,
                            ' only generate a field initializer for decimals and dates, because they can't be compile time
                            ' constant in CLR/IL.
                            initializerBinder.BindConstFieldInitializer(fieldSymbol,
                                                                        initializerNode,
                                                                        boundInitializers)

                            If fieldSymbol.Type.SpecialType = SpecialType.System_DateTime Then
                                '  report proper diagnostics for System_Runtime_CompilerServices_DateTimeConstantAttribute__ctor if needed
                                initializerBinder.ReportUseSiteErrorForSynthesizedAttribute(WellKnownMember.System_Runtime_CompilerServices_DateTimeConstantAttribute__ctor,
                                                                                            initializerNode,
                                                                                            diagnostics)
                            ElseIf fieldSymbol.Type.SpecialType = SpecialType.System_Decimal Then
                                '  report proper diagnostics for System_Runtime_CompilerServices_DecimalConstantAttribute__ctor if needed
                                initializerBinder.ReportUseSiteErrorForSynthesizedAttribute(WellKnownMember.System_Runtime_CompilerServices_DecimalConstantAttribute__ctor,
                                                                                            initializerNode,
                                                                                            diagnostics)
                            End If

                        Else
                            If fieldSymbol.Type.IsObjectType() AndAlso
                               initializerBinder.OptionStrict <> VisualBasic.OptionStrict.On AndAlso
                               fieldSymbol.Syntax IsNot Nothing AndAlso
                               fieldSymbol.Syntax.Kind = SyntaxKind.ModifiedIdentifier Then
                                Dim identifier = DirectCast(fieldSymbol.Syntax, ModifiedIdentifierSyntax)

                                If identifier.Nullable.Node IsNot Nothing AndAlso identifier.Parent IsNot Nothing AndAlso
                                   identifier.Parent.Kind = SyntaxKind.VariableDeclarator AndAlso
                                   DirectCast(identifier.Parent, VariableDeclaratorSyntax).AsClause Is Nothing Then
                                    ReportDiagnostic(diagnostics, identifier, ERRID.ERR_NullableTypeInferenceNotSupported)
                                End If
                            End If

                            initializerBinder.BindFieldInitializer(initializer.FieldsOrProperties,
                                                                   initializerNode,
                                                                   boundInitializers,
                                                                   diagnostics)
                        End If
                    Else
                        initializerBinder.BindPropertyInitializer(initializer.FieldsOrProperties,
                                                                  initializerNode,
                                                                  boundInitializers,
                                                                  diagnostics)

                    End If
                Next
            Next

            Return boundInitializers.ToImmutableAndFree()
        End Function

        Private Function BindGlobalStatement(
            scriptInitializerOpt As SynthesizedInteractiveInitializerMethod,
            statementNode As StatementSyntax,
            diagnostics As DiagnosticBag,
            isLast As Boolean) As BoundInitializer

            Dim boundStatement As BoundStatement = Me.BindStatement(statementNode, diagnostics)

            If Me.Compilation.IsSubmission AndAlso isLast AndAlso boundStatement.Kind = BoundKind.ExpressionStatement AndAlso Not boundStatement.HasErrors Then
                ' insert an implicit conversion to the submission return type (if needed):
                Dim expression = (DirectCast(boundStatement, BoundExpressionStatement)).Expression
                If expression.Type Is Nothing OrElse expression.Type.SpecialType <> SpecialType.System_Void Then
                    Dim submissionReturnType = scriptInitializerOpt.ResultType
                    expression = ApplyImplicitConversion(expression.Syntax, submissionReturnType, expression, diagnostics)
                    boundStatement = New BoundExpressionStatement(boundStatement.Syntax, expression, expression.HasErrors)
                End If
            End If

            Return New BoundGlobalStatementInitializer(statementNode, boundStatement)
        End Function

        ''' <summary>
        ''' Bind an initializer for an implicitly allocated array field (for example: Private F(2) As Object).
        ''' </summary>
        Public Sub BindArrayFieldImplicitInitializer(
            fieldSymbol As SourceFieldSymbol,
            boundInitializers As ArrayBuilder(Of BoundInitializer),
            diagnostics As DiagnosticBag)

            Debug.Assert(fieldSymbol.Syntax.Kind = SyntaxKind.ModifiedIdentifier)
            Debug.Assert(fieldSymbol.Type.Kind = SymbolKind.ArrayType)

            Dim syntax = DirectCast(fieldSymbol.Syntax, ModifiedIdentifierSyntax)

            Debug.Assert(syntax.ArrayBounds IsNot Nothing)
            Dim arraySize = BindArrayBounds(syntax.ArrayBounds, diagnostics)
            Dim arrayCreation = New BoundArrayCreation(syntax, arraySize, Nothing, fieldSymbol.Type)
            arrayCreation.SetWasCompilerGenerated()

            Dim boundReceiver = If(fieldSymbol.IsShared, Nothing, CreateMeReference(syntax, isSynthetic:=True))
            Dim boundFieldAccessExpression = New BoundFieldAccess(syntax, boundReceiver, fieldSymbol, True, fieldSymbol.Type)
            boundFieldAccessExpression.SetWasCompilerGenerated()

            Dim initializer = New BoundFieldOrPropertyInitializer(syntax,
                                                                  ImmutableArray.Create(Of Symbol)(fieldSymbol),
                                                                  boundFieldAccessExpression,
                                                                  arrayCreation)

            initializer.SetWasCompilerGenerated()
            boundInitializers.Add(initializer)
        End Sub

        ''' <summary>
        ''' Binds the field initializer. A bound field initializer contains the bound field access and bound init value.
        ''' </summary>
        ''' <param name="fieldSymbols">The field symbol.</param>
        ''' <param name="equalsValueOrAsNewSyntax">The syntax node for the optional initialization.</param>
        ''' <param name="boundInitializers">The array of bound initializers to add the newly bound ones to.</param>
        ''' <param name="diagnostics">The diagnostics.</param>
        Friend Sub BindFieldInitializer(
            fieldSymbols As ImmutableArray(Of Symbol),
            equalsValueOrAsNewSyntax As VisualBasicSyntaxNode,
            boundInitializers As ArrayBuilder(Of BoundInitializer),
            diagnostics As DiagnosticBag,
            Optional bindingForSemanticModel As Boolean = False
        )
            Debug.Assert(Not fieldSymbols.IsEmpty)
            Dim firstFieldSymbol = DirectCast(fieldSymbols.First, SourceFieldSymbol)
            Debug.Assert(bindingForSemanticModel OrElse Not firstFieldSymbol.IsConst)

            Dim boundReceiver = If(firstFieldSymbol.IsShared,
                                   Nothing,
                                   CreateMeReference(firstFieldSymbol.Syntax, isSynthetic:=True))

            ' Always generate a field access for the first symbol. In cases were we have multiple variables declared we will not
            ' use it (we don't store it in the bound initializer node), but we still need it for e.g. the data flow analysis
            ' (stored in the bound lvalue placeholder).
            Dim fieldAccess As BoundExpression = New BoundFieldAccess(firstFieldSymbol.Syntax,
                                                                      boundReceiver,
                                                                      firstFieldSymbol,
                                                                      True,
                                                                      firstFieldSymbol.Type)
            fieldAccess.SetWasCompilerGenerated()

            Dim asNewVariablePlaceholder As BoundWithLValueExpressionPlaceholder = Nothing
            If equalsValueOrAsNewSyntax.Kind = SyntaxKind.AsNewClause Then
                ' CONSIDER: using a bound field access directly instead of a placeholder for AsNew declarations
                '           with just one variable

                asNewVariablePlaceholder = New BoundWithLValueExpressionPlaceholder(equalsValueOrAsNewSyntax,
                                                                                    firstFieldSymbol.Type)
                asNewVariablePlaceholder.SetWasCompilerGenerated()
            End If

            Dim boundInitExpression As BoundExpression = BindFieldOrPropertyInitializerExpression(equalsValueOrAsNewSyntax,
                                                                                                  firstFieldSymbol.Type,
                                                                                                  asNewVariablePlaceholder,
                                                                                                  diagnostics)

            Dim hasErrors = False

            ' In speculative semantic model scenarios equalsValueOrAsNewSyntax might have no parent.
            If equalsValueOrAsNewSyntax.Parent IsNot Nothing Then
                Debug.Assert(Me.IsSemanticModelBinder OrElse
                             fieldSymbols.Length = DirectCast(equalsValueOrAsNewSyntax.Parent, VariableDeclaratorSyntax).Names.Count)

                If equalsValueOrAsNewSyntax.Kind() = SyntaxKind.AsNewClause Then
                    For Each name In DirectCast(equalsValueOrAsNewSyntax.Parent, VariableDeclaratorSyntax).Names
                        If Not (name.ArrayRankSpecifiers.IsEmpty AndAlso name.ArrayBounds Is Nothing) Then
                            ' Arrays cannot be declared with AsNew syntax
                            ReportDiagnostic(diagnostics, name, ERRID.ERR_AsNewArray)
                            hasErrors = True
                        End If
                    Next
                End If
            End If

            boundInitializers.Add(New BoundFieldOrPropertyInitializer(
                equalsValueOrAsNewSyntax,
                fieldSymbols,
                If(fieldSymbols.Length = 1, fieldAccess, Nothing),
                boundInitExpression,
                hasErrors))
        End Sub

        Friend Sub BindPropertyInitializer(
            propertySymbols As ImmutableArray(Of Symbol),
            initValueOrAsNewNode As VisualBasicSyntaxNode,
            boundInitializers As ArrayBuilder(Of BoundInitializer),
            diagnostics As DiagnosticBag
        )
            Dim propertySymbol = DirectCast(propertySymbols.First, PropertySymbol)
            Dim syntaxNode As VisualBasicSyntaxNode = initValueOrAsNewNode

            Dim boundReceiver = If(propertySymbol.IsShared, Nothing, CreateMeReference(syntaxNode, isSynthetic:=True))

            ' If the property has parameters, BC36759 should have already
            ' been reported for the auto-implemented property.
            Dim hasError = propertySymbol.ParameterCount > 0
            Dim boundPropertyOrFieldAccess As BoundExpression

            If propertySymbol.IsReadOnly AndAlso propertySymbol.AssociatedField IsNot Nothing Then
                ' For ReadOnly auto-implemented properties we have to write directly to the backing field.
                Debug.Assert(propertySymbol.Type = propertySymbol.AssociatedField.Type)
                Debug.Assert(propertySymbols.Length = 1)
                boundPropertyOrFieldAccess = New BoundFieldAccess(syntaxNode,
                                                                  boundReceiver,
                                                                  propertySymbol.AssociatedField,
                                                                  isLValue:=True,
                                                                  type:=propertySymbol.Type,
                                                                  hasErrors:=hasError)

            Else
                boundPropertyOrFieldAccess = New BoundPropertyAccess(syntaxNode,
                                                                     propertySymbol,
                                                                     propertyGroupOpt:=Nothing,
                                                                     accessKind:=PropertyAccessKind.Set,
                                                                     isWriteable:=propertySymbol.HasSet,
                                                                     receiverOpt:=boundReceiver,
                                                                     arguments:=ImmutableArray(Of BoundExpression).Empty,
                                                                     hasErrors:=hasError)
            End If

            boundPropertyOrFieldAccess = BindAssignmentTarget(syntaxNode, boundPropertyOrFieldAccess, diagnostics)
            Dim isError As Boolean
            boundPropertyOrFieldAccess = AdjustAssignmentTarget(syntaxNode, boundPropertyOrFieldAccess, diagnostics, isError)
            boundPropertyOrFieldAccess.SetWasCompilerGenerated()

            Dim boundInitExpression = BindFieldOrPropertyInitializerExpression(initValueOrAsNewNode,
                                                                               propertySymbol.Type,
                                                                               Nothing,
                                                                               diagnostics)

            boundInitializers.Add(New BoundFieldOrPropertyInitializer(initValueOrAsNewNode,
                                                                      propertySymbols,
                                                                      If(propertySymbols.Length = 1, boundPropertyOrFieldAccess, Nothing),
                                                                      boundInitExpression))

        End Sub

        Private Function BindFieldOrPropertyInitializerExpression(
            equalsValueOrAsNewSyntax As VisualBasicSyntaxNode,
            targetType As TypeSymbol,
            asNewVariablePlaceholderOpt As BoundWithLValueExpressionPlaceholder,
            diagnostics As DiagnosticBag
        ) As BoundExpression
            Dim boundInitExpression As BoundExpression = Nothing

            Dim fieldInitializerSyntax As VisualBasicSyntaxNode

            If equalsValueOrAsNewSyntax.Kind = SyntaxKind.AsNewClause Then
                Dim asNew = DirectCast(equalsValueOrAsNewSyntax, AsNewClauseSyntax)
                Select Case asNew.NewExpression.Kind
                    Case SyntaxKind.ObjectCreationExpression
                        Dim objectCreationExpressionSyntax = DirectCast(asNew.NewExpression, ObjectCreationExpressionSyntax)
                        boundInitExpression = BindObjectCreationExpression(asNew.NewExpression.Type,
                                                                           objectCreationExpressionSyntax.ArgumentList,
                                                                           targetType,
                                                                           objectCreationExpressionSyntax,
                                                                           diagnostics,
                                                                           asNewVariablePlaceholderOpt)
                    Case SyntaxKind.AnonymousObjectCreationExpression
                        boundInitExpression = BindAnonymousObjectCreationExpression(
                                                    DirectCast(asNew.NewExpression, AnonymousObjectCreationExpressionSyntax), diagnostics)
                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(asNew.NewExpression.Kind)
                End Select

                fieldInitializerSyntax = asNew
            Else
                Dim valueSyntax = DirectCast(equalsValueOrAsNewSyntax, EqualsValueSyntax)
                fieldInitializerSyntax = valueSyntax.Value
                boundInitExpression = BindValue(DirectCast(fieldInitializerSyntax, ExpressionSyntax), diagnostics)
            End If

            If targetType IsNot Nothing Then
                boundInitExpression = ApplyImplicitConversion(boundInitExpression.Syntax,
                                                              targetType,
                                                              boundInitExpression,
                                                              diagnostics)
            Else
                ' Try to reclassify boundInitValue if we still can.
                boundInitExpression = MakeRValueAndIgnoreDiagnostics(boundInitExpression)
            End If

            Return boundInitExpression
        End Function

        ''' <summary>
        ''' Checks for errors in the constant initialization of a field, and only returns a BoundFieldOrPropertyInitializer for
        ''' decimals and dates because they aren't compile time constant in CLR. Other data type end up directly in metadata and 
        ''' do not cause a BoundFieldOrPropertyInitializer node.
        ''' </summary>
        ''' <param name="fieldSymbol">The field symbol.</param>
        ''' <param name="equalsValueOrAsNewSyntax">The syntax node for the optional initialization.</param>
        ''' <param name="boundInitializers">The array of bound initializers to add the newly bound ones to.</param>
        Private Sub BindConstFieldInitializer(
            fieldSymbol As SourceFieldSymbol,
            equalsValueOrAsNewSyntax As VisualBasicSyntaxNode,
            boundInitializers As ArrayBuilder(Of BoundInitializer))
            Debug.Assert(fieldSymbol.IsConst)

            If Not fieldSymbol.IsConstButNotMetadataConstant Then
                Return
            End If

            ' Const fields of type Date or Decimal will get initialized in the synthesized shared constructor
            ' because their value is not regarded as compile time constant by the CLR.
            ' This will produce sequence points in the shared constructor which is exactly what Dev10 does.
            Dim constantValue = fieldSymbol.GetConstantValue(SymbolsInProgress(Of FieldSymbol).Empty)

            If constantValue IsNot Nothing Then
                Dim meSymbol As ParameterSymbol = Nothing
                Dim boundFieldAccessExpr = New BoundFieldAccess(equalsValueOrAsNewSyntax,
                                                                Nothing,
                                                                fieldSymbol,
                                                                True,
                                                                fieldSymbol.Type)
                boundFieldAccessExpr.SetWasCompilerGenerated()

                Dim boundInitValue = New BoundLiteral(equalsValueOrAsNewSyntax,
                                                      constantValue,
                                                      fieldSymbol.Type)

                boundInitializers.Add(New BoundFieldOrPropertyInitializer(equalsValueOrAsNewSyntax,
                                                                          ImmutableArray.Create(Of Symbol)(fieldSymbol),
                                                                          boundFieldAccessExpr,
                                                                          boundInitValue))
            End If
        End Sub

        ''' <summary>
        ''' Binds constant initialization value of the field.
        ''' </summary>
        ''' <param name="fieldSymbol">The symbol.</param>
        ''' <param name="equalsValueOrAsNewSyntax">The initialization syntax.</param>
        ''' <param name="diagnostics">The diagnostics.</param><returns></returns>
        Friend Function BindFieldAndEnumConstantInitializer(
            fieldSymbol As FieldSymbol,
            equalsValueOrAsNewSyntax As VisualBasicSyntaxNode,
            isEnum As Boolean,
            diagnostics As DiagnosticBag,
            <Out> ByRef constValue As ConstantValue
        ) As BoundExpression
            constValue = Nothing
            Dim boundInitValue As BoundExpression = Nothing
            Dim initValueDiagnostics = DiagnosticBag.GetInstance

            If equalsValueOrAsNewSyntax.Kind = SyntaxKind.EqualsValue Then
                Dim equalsValueSyntax As EqualsValueSyntax = DirectCast(equalsValueOrAsNewSyntax, EqualsValueSyntax)
                boundInitValue = BindValue(equalsValueSyntax.Value, initValueDiagnostics)
            Else
                ' illegal case, const fields cannot be initialized with AsNew
                ' all diagnostics here will be ignored because we are just binding for the purpose of storing
                ' the bound node in a BoundBadNode. The required diagnostics have already been reported in 
                ' SourceFieldSymbol.Type
                Dim asNewSyntax = DirectCast(equalsValueOrAsNewSyntax, AsNewClauseSyntax)
                Dim ignoredDiagnostics = DiagnosticBag.GetInstance
                Dim fieldType = If(fieldSymbol.HasDeclaredType, fieldSymbol.Type, GetSpecialType(SpecialType.System_Object, asNewSyntax, ignoredDiagnostics)) ' prevent recursion if field type is inferred.
                Select Case asNewSyntax.NewExpression.Kind
                    Case SyntaxKind.ObjectCreationExpression
                        Dim objectCreationExpressionSyntax = DirectCast(asNewSyntax.NewExpression,
                                                                        ObjectCreationExpressionSyntax)
                        boundInitValue = BindObjectCreationExpression(asNewSyntax.Type,
                                                                      objectCreationExpressionSyntax.ArgumentList,
                                                                      fieldType,
                                                                      objectCreationExpressionSyntax,
                                                                      ignoredDiagnostics,
                                                                      Nothing)
                    Case SyntaxKind.AnonymousObjectCreationExpression
                        boundInitValue = BindAnonymousObjectCreationExpression(
                                                DirectCast(asNewSyntax.NewExpression, AnonymousObjectCreationExpressionSyntax), ignoredDiagnostics)
                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(asNewSyntax.NewExpression.Kind)
                End Select

                boundInitValue = New BoundBadExpression(boundInitValue.Syntax,
                                                        LookupResultKind.Empty,
                                                        ImmutableArray(Of Symbol).Empty,
                                                        ImmutableArray.Create(Of BoundNode)(boundInitValue),
                                                        fieldType,
                                                        hasErrors:=True)
                ignoredDiagnostics.Free()
            End If

            If Not boundInitValue.HasErrors Then
                Dim targetType As TypeSymbol

                If fieldSymbol.HasDeclaredType Then
                    targetType = fieldSymbol.Type

                    If isEnum Then
                        targetType = targetType.GetEnumUnderlyingTypeOrSelf
                    End If

                    boundInitValue = ApplyImplicitConversion(boundInitValue.Syntax, targetType, boundInitValue, initValueDiagnostics)
                Else
                    targetType = If(boundInitValue.Type, ErrorTypeSymbol.UnknownResultType)
                End If

                Dim boundInitValueHasErrorsOrConstTypeIsWrong As Boolean =
                    initValueDiagnostics.HasAnyErrors OrElse fieldSymbol.HasDeclaredType AndAlso Not targetType.IsValidTypeForConstField()

                ' NOTE: we'll only report ERR_RequiredConstConversion2 ("Conversion from '...' to '.' cannot occur in 
                ' NOTE: a constant expression") and ERR_RequiredConstExpr ("Constant expression is required") in case
                ' NOTE: the type (if declared) is a valid type for const fields. This is different from Dev10 that sometimes
                ' NOTE: reported issues and sometimes not
                ' NOTE: e.g. reports in "const foo as DelegateType = AddressOf methodName" (ERR_ConstAsNonConstant + ERR_RequiredConstExpr)
                ' NOTE: only type diagnostics for "const s as StructureType = nothing"

                If boundInitValueHasErrorsOrConstTypeIsWrong Then
                    Dim discard = DiagnosticBag.GetInstance
                    constValue = Me.GetExpressionConstantValueIfAny(boundInitValue, discard, ConstantContext.Default)
                    discard.Free()
                Else
                    constValue = Me.GetExpressionConstantValueIfAny(boundInitValue, initValueDiagnostics, ConstantContext.Default)
                End If

                ' e.g. the init value of "Public foo as Byte = 2.2" is still considered as constant and therefore a CByte(2)
                ' is being assigned as the constant value of this field/enum. However in case of Option Strict On there has 
                ' been a diagnostics in the call to ApplyImplicitConversion.
                If constValue Is Nothing Then
                    ' set hasErrors to indicate later check not to add more diagnostics.
                    boundInitValue = BadExpression(boundInitValue.Syntax, boundInitValue, targetType)
                End If
            End If

            diagnostics.AddRange(initValueDiagnostics)
            initValueDiagnostics.Free()

            Return boundInitValue
        End Function

        ''' <summary>
        ''' Binds a constant local's value. 
        ''' </summary>
        ''' <param name="symbol">The local symbol.</param>
        ''' <param name="type">The local symbol's type. It is passed in because this method is called while the type is being resolved and before it is set.</param>
        Friend Function BindLocalConstantInitializer(symbol As LocalSymbol,
                                                type As TypeSymbol,
                                                name As ModifiedIdentifierSyntax,
                                                equalsValueOpt As EqualsValueSyntax,
                                                diagnostics As DiagnosticBag,
                                                <Out> ByRef constValue As ConstantValue) As BoundExpression
            constValue = Nothing

            Dim valueExpression As BoundExpression = Nothing

            If equalsValueOpt IsNot Nothing Then
                Dim valueSyntax = equalsValueOpt.Value

                If IsBindingImplicitlyTypedLocal(symbol) Then
                    ReportDiagnostic(diagnostics, name, ERRID.ERR_CircularEvaluation1, symbol)
                    Return BadExpression(valueSyntax, ErrorTypeSymbol.UnknownResultType)
                End If

                Dim binder = New LocalInProgressBinder(Me, symbol)

                valueExpression = binder.BindValue(valueSyntax, diagnostics)

                ' When inferring the type, the type is nothing.  Only apply conversion if there is a type. If there isn't a type then the
                ' constant gets the expression type and there is no need to apply a conversion.
                If type IsNot Nothing Then
                    valueExpression = binder.ApplyImplicitConversion(valueSyntax, type, valueExpression, diagnostics)
                End If

                If Not valueExpression.HasErrors Then

                    ' ExpressionIsConstant is called to report diagnostics at the correct location in the expression.
                    ' Only call ExpressionIsConstant if the expression is good to avoid reporting a bad expression is not
                    ' a constant. 
                    If (valueExpression.Type Is Nothing OrElse Not valueExpression.Type.IsErrorType) Then
                        constValue = binder.GetExpressionConstantValueIfAny(valueExpression, diagnostics, ConstantContext.Default)
                        If constValue IsNot Nothing Then
                            Return valueExpression
                        End If
                    End If

                    ' The result is not a constant and is not marked with hasErrors so return a BadExpression.
                    Return BadExpression(valueSyntax, valueExpression, ErrorTypeSymbol.UnknownResultType)

                End If

            Else
                valueExpression = BadExpression(name, ErrorTypeSymbol.UnknownResultType)
                ReportDiagnostic(diagnostics, name, ERRID.ERR_ConstantWithNoValue)
            End If

            Return valueExpression
        End Function

        ''' <summary>
        ''' Binds a parameter's default value syntax
        ''' </summary>
        Friend Function BindParameterDefaultValue(
            targetType As TypeSymbol,
            equalsValueSyntax As EqualsValueSyntax,
            diagnostics As DiagnosticBag,
            <Out> ByRef constValue As ConstantValue
        ) As BoundExpression
            constValue = Nothing

            Dim boundInitValue As BoundExpression = BindValue(equalsValueSyntax.Value, diagnostics)

            If Not boundInitValue.HasErrors Then

                ' Check if the constant can be converted to the parameter type.
                If Not targetType.IsErrorType Then
                    boundInitValue = ApplyImplicitConversion(boundInitValue.Syntax, targetType, boundInitValue, diagnostics)
                End If

                ' Report errors if value is not a constant.
                constValue = GetExpressionConstantValueIfAny(boundInitValue, diagnostics, ConstantContext.ParameterDefaultValue)
                If constValue Is Nothing Then
                    boundInitValue = BadExpression(boundInitValue.Syntax, boundInitValue, targetType)
                End If
            End If

            Return boundInitValue
        End Function

    End Class
End Namespace
