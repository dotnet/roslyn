' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class Binder

#Region "Get All Attributes"

        ' Method to bind attributes types early for all attributes to enable early decoding of some well-known attributes used within the binder.
        ' Note: attributesToBind contains merged attributes from all the different syntax locations (e.g. for named types, partial methods, etc.).
        Friend Shared Function BindAttributeTypes(binders As ImmutableArray(Of Binder),
                                                  attributesToBind As ImmutableArray(Of AttributeSyntax),
                                                  ownerSymbol As Symbol,
                                                  diagnostics As DiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
            Debug.Assert(binders.Any())
            Debug.Assert(attributesToBind.Any())
            Debug.Assert(ownerSymbol IsNot Nothing)
            Debug.Assert(binders.Length = attributesToBind.Length)

            Dim totalAttributesCount As Integer = attributesToBind.Length
            Dim boundAttributeTypes = New NamedTypeSymbol(totalAttributesCount - 1) {}
            For i = 0 To totalAttributesCount - 1
                boundAttributeTypes(i) = BindAttributeType(binders(i), attributesToBind(i), ownerSymbol, diagnostics)
            Next

            Return boundAttributeTypes.AsImmutableOrNull()
        End Function

        ' Method to bind attributes types early for all attributes to enable early decoding of some well-known attributes used within the binder.
        Friend Shared Function BindAttributeType(binder As Binder,
                                                 attribute As AttributeSyntax,
                                                 ownerSymbol As Symbol,
                                                 diagnostics As DiagnosticBag) As NamedTypeSymbol
            binder = New LocationSpecificBinder(VisualBasic.BindingLocation.Attribute, ownerSymbol, binder)
            Return DirectCast(binder.BindTypeSyntax(attribute.Name, diagnostics), NamedTypeSymbol)
        End Function

        ''' <summary>
        ''' Gets but does not fully validate a symbol's attributes. Returns binding errors but not attribute usage and attribute specific errors.
        ''' </summary>
        Friend Shared Sub GetAttributes(binders As ImmutableArray(Of Binder),
                                        attributesToBind As ImmutableArray(Of AttributeSyntax),
                                        boundAttributeTypes As ImmutableArray(Of NamedTypeSymbol),
                                        attributeBuilder As VisualBasicAttributeData(),
                                        ownerSymbol As Symbol,
                                        diagnostics As DiagnosticBag)
            Debug.Assert(Not binders.IsEmpty)
            Debug.Assert(Not attributesToBind.IsEmpty)
            Debug.Assert(binders.Length = attributesToBind.Length)

            For index = 0 To attributesToBind.Length - 1
                If attributeBuilder(index) Is Nothing Then
                    Dim binder = binders(index)
                    binder = New LocationSpecificBinder(VisualBasic.BindingLocation.Attribute, ownerSymbol, binder)
                    attributeBuilder(index) = binder.GetAttribute(attributesToBind(index), boundAttributeTypes(index), diagnostics)
                End If
            Next
        End Sub
#End Region

#Region "Get Single Attribute"
        Friend Function GetAttribute(node As AttributeSyntax, boundAttributeType As NamedTypeSymbol, diagnostics As DiagnosticBag) As SourceAttributeData
            Dim boundAttribute As boundAttribute = BindAttribute(node, boundAttributeType, diagnostics)

            Dim visitor As New AttributeExpressionVisitor(Me, boundAttribute.HasErrors)
            Dim args As ImmutableArray(Of TypedConstant) = visitor.VisitPositionalArguments(boundAttribute.ConstructorArguments, diagnostics)
            Dim namedArgs As ImmutableArray(Of KeyValuePair(Of String, TypedConstant)) = visitor.VisitNamedArguments(boundAttribute.NamedArguments, diagnostics)
            Dim isConditionallyOmitted As Boolean = Not visitor.HasErrors AndAlso IsAttributeConditionallyOmitted(boundAttributeType, node, boundAttribute.SyntaxTree)

            Return New SourceAttributeData(node.GetReference(), DirectCast(boundAttribute.Type, NamedTypeSymbol), boundAttribute.Constructor, args, namedArgs, isConditionallyOmitted, hasErrors:=visitor.HasErrors)
        End Function

        Protected Function IsAttributeConditionallyOmitted(attributeType As NamedTypeSymbol, node As AttributeSyntax, syntaxTree As SyntaxTree) As Boolean
            If IsEarlyAttributeBinder Then
                Return False
            End If

            Debug.Assert(attributeType IsNot Nothing)
            Debug.Assert(Not attributeType.IsErrorType())

            ' Source attribute is conditionally omitted if the attribute type is conditional and none of the conditional symbols are true at the attribute source location.
            If attributeType.IsConditional Then
                Dim conditionalSymbols As IEnumerable(Of String) = attributeType.GetAppliedConditionalSymbols()
                Debug.Assert(conditionalSymbols IsNot Nothing)
                Debug.Assert(conditionalSymbols.Any())

                If syntaxTree.IsAnyPreprocessorSymbolDefined(conditionalSymbols, node) Then
                    Return False
                End If

                ' NOTE: Conditional symbols on base type must be inherited by derived type, but the native VB compiler doesn't do so. We will maintain compatibility.
                Return True
            Else
                Return False
            End If
        End Function
#End Region

#Region "Bind Single Attribute"

        Friend Function BindAttribute(node As AttributeSyntax, diagnostics As DiagnosticBag) As BoundAttribute
            Dim namedType As NamedTypeSymbol = DirectCast(BindTypeSyntax(node.Name, diagnostics), NamedTypeSymbol)

            Return BindAttribute(node, namedType, diagnostics)
        End Function

        Friend Sub LookupAttributeType(lookupResult As LookupResult,
                    container As NamespaceOrTypeSymbol,
                    name As String,
                    options As LookupOptions,
                    <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo))

            Debug.Assert(lookupResult.IsClear)
            Debug.Assert(options.IsValid())
            Debug.Assert(options.IsAttributeTypeLookup())

            ' Per 5.2.1 When the compiler resolves an attribute name, it appends "Attribute" to the name and tries the
            ' lookup. If that lookup fails, the compiler tries the lookup without the suffix. 

            options = options Or LookupOptions.IgnoreExtensionMethods

            Lookup(lookupResult, container, name & "Attribute", options, useSiteDiagnostics)

            ' If no result is found then do a second lookup without the attribute suffix. 
            ' The result is that namespace symbols or inaccessible symbols with the attribute 
            ' suffix will be returned from the first lookup.

            If lookupResult.IsClear OrElse lookupResult.IsWrongArity Then
                lookupResult.Clear()
                Lookup(lookupResult, container, name, options, useSiteDiagnostics)
            End If

            If Not lookupResult.IsGood Then
                ' Didn't find a viable symbol just return
                Return
            End If

            ' Found a good symbol, now check that it is appropriate to use as an attribute.
            CheckAttributeTypeViability(lookupResult)
        End Sub

        Private Sub Lookup(lookupResult As LookupResult,
             container As NamespaceOrTypeSymbol,
             name As String,
             options As LookupOptions,
             <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo))

            If container IsNot Nothing Then
                LookupMember(lookupResult, container, name, 0, options, useSiteDiagnostics)
            Else
                Lookup(lookupResult, name, 0, options, useSiteDiagnostics)
            End If
        End Sub

        Private Sub CheckAttributeTypeViability(lookupResult As LookupResult)
            Debug.Assert(lookupResult.HasSingleSymbol AndAlso lookupResult.IsGood)

            ' For error reporting, check the unwrapped symbol. However, return the unwrapped alias symbol if it is an alias.  
            ' BindTypeOrNamespace will do the final unwrap.
            Dim symbol = UnwrapAlias(lookupResult.SingleSymbol)
            Dim diagInfo As DiagnosticInfo = Nothing
            Dim errorId As ERRID
            Dim resultKind As LookupResultKind

            If symbol.Kind = SymbolKind.Namespace Then
                errorId = ERRID.ERR_UnrecognizedType

            ElseIf symbol.Kind = SymbolKind.TypeParameter Then
                errorId = ERRID.ERR_AttrCannotBeGenerics

            ElseIf symbol.Kind <> SymbolKind.NamedType Then
                errorId = ERRID.ERR_UnrecognizedType
                resultKind = LookupResultKind.NotATypeOrNamespace

            Else
                Dim namedType = DirectCast(symbol, NamedTypeSymbol)
                Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing

                ' type cannot be generic
                If namedType.IsGenericType Then
                    errorId = ERRID.ERR_AttrCannotBeGenerics

                    ' type must be a class
                ElseIf namedType.IsStructureType Then
                    errorId = ERRID.ERR_AttributeMustBeClassNotStruct1

                    ' type must inherit from System.Attribute
                ElseIf Not Compilation.GetWellKnownType(WellKnownType.System_Attribute).IsBaseTypeOf(namedType, useSiteDiagnostics) Then
                    errorId = ERRID.ERR_AttributeMustInheritSysAttr

                    If Not useSiteDiagnostics.IsNullOrEmpty() Then
                        diagInfo = useSiteDiagnostics.First()
                    End If

                    ' type can not be "mustinherit"
                ElseIf namedType.IsMustInherit Then
                    errorId = ERRID.ERR_AttributeCannotBeAbstract

                Else
                    ' Return the symbol from the lookup result. In the case of an alias, it will be the alias symbol not
                    ' the unwrapped symbol.  This is the convention for lookup methods.
                    Return
                End If

            End If

            If diagInfo Is Nothing Then
                diagInfo = New BadSymbolDiagnostic(symbol, errorId)
            End If

            lookupResult.Clear()
            lookupResult.SetFrom(SingleLookupResult.NotAnAttributeType(symbol, diagInfo))
            Return
        End Sub

        Friend Function BindAttribute(node As AttributeSyntax, type As NamedTypeSymbol, diagnostics As DiagnosticBag) As BoundAttribute

            ' If attribute name bound to an error type with a single named type
            ' candidate symbol, we want to bind the attribute constructor
            ' and arguments with that named type to generate better semantic info.

            ' CONSIDER:    Do we need separate code paths for IDE and 
            ' CONSIDER:    batch compilation scenarios? Above mentioned scenario
            ' CONSIDER:    is not useful for batch compilation.

            Dim attributeTypeForBinding As NamedTypeSymbol = type
            Dim resultKind = LookupResultKind.Good

            If type.IsErrorType() Then
                Dim errorType = DirectCast(type, ErrorTypeSymbol)
                resultKind = errorType.ResultKind
                If errorType.CandidateSymbols.Length = 1 AndAlso errorType.CandidateSymbols(0).Kind = SymbolKind.NamedType Then
                    attributeTypeForBinding = DirectCast(errorType.CandidateSymbols(0), NamedTypeSymbol)
                End If
            End If

            ' Get the bound arguments and the argument names.
            Dim argumentListOpt = node.ArgumentList
            Dim methodSym As MethodSymbol = Nothing

            Dim analyzedArguments = BindAttributeArguments(attributeTypeForBinding, argumentListOpt, diagnostics)
            Dim boundArguments As ImmutableArray(Of BoundExpression) = analyzedArguments.positionalArguments
            Dim boundNamedArguments As ImmutableArray(Of BoundExpression) = analyzedArguments.namedArguments

            If Not attributeTypeForBinding.IsErrorType() Then

                ' Filter out inaccessible constructors 
                Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
                Dim accessibleConstructors = GetAccessibleConstructors(attributeTypeForBinding, useSiteDiagnostics)

                If accessibleConstructors.Length = 0 Then
                    ' TODO: we may want to fix the behavior of the Lookup result to contain more than one e.g. inaccessible symbol. 
                    ' Then we could display which method was inaccessible here. Until then, we're giving a generic diagnostic
                    ' which is a little deviation from Dev10 which reports:
                    ' "'C.Protected Sub New()' is not accessible in this context because it is 'Protected'.
                    ' Having multiple bad symbols in a LookupResult was tried already by acasey, but getting this right is pretty
                    ' complicated and a performance hit (multiple diagnostics, ...).

                    diagnostics.Add(node, useSiteDiagnostics)

                    ' Avoid cascading diagnostics
                    If Not type.IsErrorType() Then
                        ReportDiagnostic(diagnostics, node, ErrorFactory.ErrorInfo(ERRID.ERR_NoViableOverloadCandidates1, "New"))
                    End If

                    If attributeTypeForBinding.InstanceConstructors.IsEmpty Then
                        resultKind = LookupResult.WorseResultKind(resultKind, LookupResultKind.Empty)
                    Else
                        resultKind = LookupResult.WorseResultKind(resultKind, LookupResultKind.Inaccessible)
                    End If
                Else
                    Dim constructorsGroup = New BoundMethodGroup(node.Name, Nothing, accessibleConstructors, LookupResultKind.Good, Nothing, QualificationKind.QualifiedViaTypeName)

                    Dim results As OverloadResolution.OverloadResolutionResult = OverloadResolution.MethodInvocationOverloadResolution(constructorsGroup, boundArguments, Nothing, Me, callerInfoOpt:=node.Name,
                                                                                                                                       useSiteDiagnostics:=useSiteDiagnostics)

                    If diagnostics.Add(node.Name, useSiteDiagnostics) Then
                        ' Suppress additional diagnostics
                        diagnostics = New DiagnosticBag()
                    End If

                    If Not results.BestResult.HasValue Then
                        resultKind = LookupResult.WorseResultKind(resultKind, LookupResultKind.OverloadResolutionFailure)

                        ' Avoid cascading diagnostics
                        If Not type.IsErrorType() Then
                            ' Create and report the diagnostic.
                            If results.Candidates.Length = 0 Then
                                results = OverloadResolution.MethodInvocationOverloadResolution(constructorsGroup, boundArguments, Nothing, Me, includeEliminatedCandidates:=True, callerInfoOpt:=node.Name,
                                                                                                useSiteDiagnostics:=useSiteDiagnostics)
                            End If

                            ' Report overload resolution but do not use the bound node result. We always want to return a
                            ' SourceAttributeData not a BadBoundExpression.
                            ' TODO - Split ReportOverloadResolutionFailureAndProduceBoundNode into two methods.  One that does error reporting and one that
                            ' builds the bound node.  We only need the error reporting here.

                            ReportOverloadResolutionFailureAndProduceBoundNode(node,
                                                                                constructorsGroup,
                                                                                boundArguments, Nothing, results, diagnostics, callerInfoOpt:=node.Name)
                        End If
                    Else
                        Dim methodResult = results.BestResult.Value
                        methodSym = DirectCast(methodResult.Candidate.UnderlyingSymbol, MethodSymbol)
                        Dim errorsReported As Boolean = False

                        ReportDiagnosticsIfObsoleteOrNotSupportedByRuntime(diagnostics, methodSym, node)

                        ' Check that all formal parameters have attribute-compatible types and are public
                        For Each param In methodSym.Parameters
                            If Not IsValidTypeForAttributeArgument(param.Type) Then
                                errorsReported = True
                                ReportDiagnostic(diagnostics, node.Name, ERRID.ERR_BadAttributeConstructor1, param.Type)
                            ElseIf param.IsByRef Then
                                errorsReported = True
                                ReportDiagnostic(diagnostics, node.Name, ERRID.ERR_BadAttributeConstructor2, param.Type)
                            End If

                            ' Check that the type is public. 
                            If DigThroughArrayType(param.Type).DeclaredAccessibility <> Accessibility.Public Then
                                errorsReported = True
                                ReportDiagnostic(diagnostics, node.Name, ERRID.ERR_BadAttributeNonPublicType1, param.Type)
                            Else
                                '  Check all containers.
                                Dim container = param.Type.ContainingType
                                While container IsNot Nothing
                                    If DigThroughArrayType(container).DeclaredAccessibility <> Accessibility.Public Then
                                        errorsReported = True
                                        ReportDiagnostic(diagnostics, node.Name, ERRID.ERR_BadAttributeNonPublicContType2, param.Type, container)
                                    End If
                                    container = container.ContainingType
                                End While
                            End If
                        Next

                        If Not errorsReported Then
                            ' There should not be any used temporaries or copy back expressions because arguments must
                            ' be constants and they cannot be passed byref. 
                            Dim argumentInfo As (Arguments As ImmutableArray(Of BoundExpression), DefaultArguments As BitVector) = PassArguments(node.Name, methodResult, boundArguments, diagnostics)
                            ' We don't do anything with the default parameter info currently, as we don't expose IOperations for
                            ' Attributes. If that changes, we can add this info to the BoundAttribute node.
                            boundArguments = argumentInfo.Arguments

                            Debug.Assert(Not boundArguments.Any(Function(a) a.Kind = BoundKind.ByRefArgumentWithCopyBack))

                            If methodSym.DeclaredAccessibility <> Accessibility.Public Then
                                ReportDiagnostic(diagnostics, node.Name, ERRID.ERR_BadAttributeNonPublicConstructor)
                            End If

                        End If

                    End If

                End If

            End If

            Return New BoundAttribute(node, methodSym, boundArguments, boundNamedArguments, resultKind, type, hasErrors:=resultKind <> LookupResultKind.Good)
        End Function


        ' Given a list of arguments, create arrays of the bound arguments and pairs of names and expression syntax. Attribute arguments are bound but
        ' named arguments are not yet bound. Assumption is that the parser enforces that named arguments come after arguments.
        Private Function BindAttributeArguments(
            type As NamedTypeSymbol,
            argumentListOpt As ArgumentListSyntax,
             diagnostics As DiagnosticBag
        ) As AnalyzedAttributeArguments

            Dim boundArguments As ImmutableArray(Of BoundExpression)
            Dim namedArguments As ImmutableArray(Of BoundExpression)

            If (argumentListOpt Is Nothing) Then
                boundArguments = s_noArguments
                namedArguments = s_noArguments
            Else

                Dim arguments As SeparatedSyntaxList(Of ArgumentSyntax) = argumentListOpt.Arguments
                Dim boundArgumentsBuilder As ArrayBuilder(Of BoundExpression) = ArrayBuilder(Of BoundExpression).GetInstance
                Dim namedArgumentsBuilder As ArrayBuilder(Of BoundExpression) = Nothing
                Dim argCount As Integer = 0
                Dim argumentSyntax As ArgumentSyntax
                Try
                    For Each argumentSyntax In arguments
                        Select Case argumentSyntax.Kind
                            Case SyntaxKind.SimpleArgument

                                Dim simpleArgument = DirectCast(argumentSyntax, SimpleArgumentSyntax)

                                If Not simpleArgument.IsNamed Then
                                    ' Validating the expression is done when the bound expression is converted to a TypedConstant
                                    Dim expression As BoundExpression = BindValue(simpleArgument.Expression, diagnostics)
                                    MarkEmbeddedTypeReferenceIfNeeded(expression)
                                    boundArgumentsBuilder.Add(expression)
                                Else
                                    If namedArgumentsBuilder Is Nothing Then
                                        namedArgumentsBuilder = ArrayBuilder(Of BoundExpression).GetInstance()
                                    End If

                                    namedArgumentsBuilder.Add(BindAttributeNamedArgument(type, simpleArgument, diagnostics))
                                End If

                            Case SyntaxKind.OmittedArgument
                                boundArgumentsBuilder.Add(New BoundOmittedArgument(argumentSyntax, Nothing))

                        End Select

                        argCount += 1
                    Next
                Finally
                    boundArguments = boundArgumentsBuilder.ToImmutableAndFree
                    namedArguments = If(namedArgumentsBuilder Is Nothing, s_noArguments, namedArgumentsBuilder.ToImmutableAndFree)
                End Try
            End If

            Return New AnalyzedAttributeArguments(boundArguments, namedArguments)
        End Function

        Private Function BindAttributeNamedArgument(container As TypeSymbol,
                                                    namedArg As SimpleArgumentSyntax,
                                                    diagnostics As DiagnosticBag) As BoundExpression
            Debug.Assert(namedArg.IsNamed)
            ' Bind the named argument
            Dim result As LookupResult = LookupResult.GetInstance()
            Dim identifierName As IdentifierNameSyntax = namedArg.NameColonEquals.Name

            Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
            LookupMember(result, container, identifierName.Identifier.ValueText, 0, LookupOptions.IgnoreExtensionMethods, useSiteDiagnostics)

            ' Validating the expression is done when the bound expression is converted to a TypedConstant
            Dim rValue As BoundExpression = Me.BindValue(namedArg.Expression, diagnostics)
            MarkEmbeddedTypeReferenceIfNeeded(rValue)
            Dim lValue As BoundExpression = Nothing

            If result.IsGood Then

                Dim sym As Symbol = GetBestAttributeFieldOrProperty(result)
                Dim fieldSym As FieldSymbol = Nothing
                Dim propertySym As PropertySymbol = Nothing
                Dim fieldOrPropType As TypeSymbol = Nothing
                Dim isReadOnly As Boolean = False
                Dim hasErrors As Boolean = False

                ReportDiagnosticsIfObsoleteOrNotSupportedByRuntime(diagnostics, sym, namedArg)

                Select Case sym.Kind
                    Case SymbolKind.Field
                        fieldSym = DirectCast(sym, FieldSymbol)
                        fieldOrPropType = fieldSym.Type
                        isReadOnly = fieldSym.IsReadOnly

                    Case SymbolKind.Property
                        propertySym = DirectCast(sym, PropertySymbol)
                        fieldOrPropType = propertySym.GetTypeFromSetMethod()

                        ' NOTE: to match Dev10/VB behavior we intentionally do NOT check propertySym.IsWritable,
                        '       but instead rely on presence of Set method in this particular property symbol
                        Dim setMethod = propertySym.SetMethod
                        isReadOnly = setMethod Is Nothing

                        If setMethod IsNot Nothing Then
                            If setMethod.ParameterCount <> 1 Then
                                ReportDiagnostic(diagnostics, identifierName, ERRID.ERR_NoNonIndexProperty1, sym.Name)
                                hasErrors = True
                            End If

                            If Not IsAccessible(setMethod, useSiteDiagnostics) Then
                                ReportDiagnostic(diagnostics, identifierName, ERRID.ERR_InaccessibleMember3,
                                                   propertySym.ContainingSymbol,
                                                   propertySym,
                                                   AccessCheck.GetAccessibilityForErrorMessage(setMethod, Me.Compilation.Assembly))
                                hasErrors = True
                            End If
                        End If

                    Case Else
                        ' Must be a field or a property symbol
                        ReportDiagnostic(diagnostics, identifierName, ERRID.ERR_AttrAssignmentNotFieldOrProp1, identifierName.Identifier.ValueText)
                        hasErrors = True
                End Select

                If sym.DeclaredAccessibility <> Accessibility.Public Then
                    ReportDiagnostic(diagnostics, identifierName, ERRID.ERR_BadAttributeNonPublicProperty1, sym.Name)
                    hasErrors = True
                End If

                If sym.IsShared Then
                    ' Shared attribute property cannot be the target
                    ReportDiagnostic(diagnostics, identifierName, ERRID.ERR_BadAttributeSharedProperty1, sym.Name)
                    hasErrors = True
                End If

                If isReadOnly Then
                    ReportDiagnostic(diagnostics, identifierName, ERRID.ERR_BadAttributeReadOnlyProperty1, sym.Name)
                    hasErrors = True
                End If

                If fieldOrPropType IsNot Nothing Then
                    If Not IsValidTypeForAttributeArgument(fieldOrPropType) Then
                        ReportDiagnostic(diagnostics, identifierName, ERRID.ERR_BadAttributePropertyType1, sym.Name)
                        hasErrors = True
                    End If

                    ' Convert the value to the field or property type
                    rValue = ApplyImplicitConversion(namedArg.Expression, fieldOrPropType, rValue, diagnostics)
                Else
                    rValue = MakeRValue(rValue, diagnostics)
                End If

                If propertySym IsNot Nothing Then
                    lValue = New BoundPropertyAccess(identifierName, propertySym, Nothing, PropertyAccessKind.Set, Not isReadOnly, Nothing, ImmutableArray(Of BoundExpression).Empty, defaultArguments:=BitVector.Null, hasErrors)
                    Debug.Assert(TypeSymbol.Equals(lValue.Type, fieldOrPropType, TypeCompareKind.ConsiderEverything))
                ElseIf fieldSym IsNot Nothing Then
                    lValue = New BoundFieldAccess(identifierName, Nothing, fieldSym, True, fieldOrPropType, hasErrors)
                Else
                    lValue = BadExpression(identifierName, ErrorTypeSymbol.UnknownResultType)
                End If

            Else
                ' Did not find anything with that name.
                If result.HasDiagnostic Then
                    ReportDiagnostic(diagnostics, identifierName, result.Diagnostic)
                Else
                    ReportDiagnostic(diagnostics, identifierName, ERRID.ERR_PropertyOrFieldNotDefined1, identifierName.Identifier.ValueText)
                End If

                lValue = BadExpression(identifierName, ErrorTypeSymbol.UnknownResultType)
                rValue = MakeRValue(rValue, diagnostics)
            End If

            diagnostics.Add(namedArg, useSiteDiagnostics)
            result.Free()

            Dim namedArgExpr = New BoundAssignmentOperator(namedArg, lValue, rValue, True)

            Return namedArgExpr
        End Function

        Private Sub MarkEmbeddedTypeReferenceIfNeeded(expression As BoundExpression)
            ' If we are embedding code and also there are no errors
            If (Me.Compilation.EmbeddedSymbolManager.Embedded <> 0) AndAlso Not expression.HasErrors Then

                ' And also is the expression comes from compilation syntax trees
                If expression.Syntax.SyntaxTree IsNot Nothing AndAlso
                    Me.Compilation.ContainsSyntaxTree(expression.Syntax.SyntaxTree) Then

                    ' Mark type if it is referenced in expression like 'GetType(Microsoft.VisualBasic.Strings)'
                    If expression.Kind = BoundKind.GetType Then
                        MarkEmbeddedTypeReferencedFromGetTypeExpression(DirectCast(expression, BoundGetType))

                    ElseIf expression.Kind = BoundKind.ArrayCreation Then
                        Dim arrayCreation = DirectCast(expression, BoundArrayCreation)
                        Dim arrayInitialization As BoundArrayInitialization = arrayCreation.InitializerOpt
                        If arrayInitialization IsNot Nothing Then
                            For Each initializer In arrayInitialization.Initializers
                                MarkEmbeddedTypeReferenceIfNeeded(initializer)
                            Next
                        End If
                    End If
                End If
            End If
        End Sub

        Private Sub MarkEmbeddedTypeReferencedFromGetTypeExpression(expression As BoundGetType)
            Dim sourceType As TypeSymbol = expression.SourceType.Type
            If sourceType.IsEmbedded Then

                ' We assume that none of embedded types references 
                ' other embedded types in attribute values
                Debug.Assert(Not expression.Syntax.SyntaxTree.IsEmbeddedSyntaxTree)

                ' Note that none of the embedded symbols from referenced 
                ' assemblies or compilations should be found/referenced
                Debug.Assert(sourceType.ContainingAssembly Is Me.Compilation.Assembly)

                Me.Compilation.EmbeddedSymbolManager.MarkSymbolAsReferenced(sourceType)
            End If
        End Sub

        ' Find the first field or property with a Set method in the result.
        Private Shared Function GetBestAttributeFieldOrProperty(result As LookupResult) As Symbol

            If result.HasSingleSymbol Then
                Return result.SingleSymbol
            End If

            Dim bestSym As Symbol = Nothing
            Dim symbols = result.Symbols

            For Each sym In symbols
                Select Case sym.Kind
                    Case SymbolKind.Field
                        Return sym

                    Case SymbolKind.Property
                        ' WARNING: This code seems to rely on an assumption that result.Symbols collection have 
                        '          symbols sorted by containing type (symbols from most-derived type first, 
                        '          then symbols from base types in order of inheritance). Thus, if we have the
                        '          following inheritance of attribute types:
                        '
                        '                   D Inherits B Inherits Attribute
                        '
                        '          where B defines a virtual property PROP and D overrides it, 'result.Symbols' 
                        '          will have both symbols {D.PROP, B.PROP} and we should always grab D.PROP
                        '
                        ' TODO: revise
                        bestSym = sym
                        Dim propSym = DirectCast(sym, PropertySymbol)
                        Dim setMethod = propSym.GetMostDerivedSetMethod()

                        ' NOTE: Dev10 seems to grab the first property and report error in case the 
                        '       property is ReadOnly (actually, does not have Set method)
                        '
                        ' TODO: check/revise
                        If setMethod IsNot Nothing AndAlso setMethod.ParameterCount = 1 Then
                            Return propSym
                        End If

                End Select
            Next

            If bestSym Is Nothing Then
                Return symbols(0)
            End If
            Return bestSym
        End Function

        ' Determines if the type is a valid type for a custom attribute argument The only valid types are 
        ' 1. primitive types except date and decimal, 
        ' 2. object, system.type, public enumerated types
        ' 3. one dimensional arrays of (1) and (2) above
        Private Function IsValidTypeForAttributeArgument(type As TypeSymbol) As Boolean
            Return type.IsValidTypeForAttributeArgument(Me.Compilation)
        End Function

#End Region

#Region "AttributeExpressionVisitor"

        ''' <summary>
        ''' Walk a custom attribute argument bound node and return a TypedConstant.  Verify that the expression is a constant expression.
        ''' </summary>
        ''' <remarks></remarks>
        Friend Structure AttributeExpressionVisitor

            Private ReadOnly _binder As Binder
            Private _hasErrors As Boolean

            Public Sub New(binder As Binder, hasErrors As Boolean)
                Me._binder = binder
                Me._hasErrors = hasErrors
            End Sub

            Public ReadOnly Property HasErrors As Boolean
                Get
                    Return Me._hasErrors
                End Get
            End Property

            Public Function VisitPositionalArguments(arguments As ImmutableArray(Of BoundExpression), diag As DiagnosticBag) As ImmutableArray(Of TypedConstant)
                Return VisitArguments(arguments, diag)
            End Function

            Private Function VisitArguments(arguments As ImmutableArray(Of BoundExpression), diag As DiagnosticBag) As ImmutableArray(Of TypedConstant)
                Dim builder As ArrayBuilder(Of TypedConstant) = Nothing
                For Each exp In arguments
                    If builder Is Nothing Then
                        builder = ArrayBuilder(Of TypedConstant).GetInstance()
                    End If

                    builder.Add(VisitExpression(exp, diag))
                Next
                If builder Is Nothing Then
                    Return ImmutableArray(Of TypedConstant).Empty
                End If

                Return builder.ToImmutableAndFree
            End Function

            Public Function VisitNamedArguments(arguments As ImmutableArray(Of BoundExpression), diag As DiagnosticBag) As ImmutableArray(Of KeyValuePair(Of String, TypedConstant))
                Dim builder As ArrayBuilder(Of KeyValuePair(Of String, TypedConstant)) = Nothing
                For Each namedArg In arguments

                    Dim kv = VisitNamedArgument(namedArg, diag)

                    If kv.HasValue Then
                        If builder Is Nothing Then
                            builder = ArrayBuilder(Of KeyValuePair(Of String, TypedConstant)).GetInstance()
                        End If

                        builder.Add(kv.Value)
                    End If
                Next

                If builder Is Nothing Then
                    Return ImmutableArray(Of KeyValuePair(Of String, TypedConstant)).Empty
                End If

                Return builder.ToImmutableAndFree
            End Function

            Private Function VisitNamedArgument(argument As BoundExpression, diag As DiagnosticBag) As Nullable(Of KeyValuePair(Of String, TypedConstant))
                Select Case argument.Kind
                    Case BoundKind.AssignmentOperator
                        Dim assignment = DirectCast(argument, BoundAssignmentOperator)

                        Select Case assignment.Left.Kind
                            Case BoundKind.FieldAccess
                                Dim left = DirectCast(assignment.Left, BoundFieldAccess)
                                Return New KeyValuePair(Of String, TypedConstant)(left.FieldSymbol.Name, VisitExpression(assignment.Right, diag))

                            Case BoundKind.PropertyAccess
                                Dim left = DirectCast(assignment.Left, BoundPropertyAccess)
                                Return New KeyValuePair(Of String, TypedConstant)(left.PropertySymbol.Name, VisitExpression(assignment.Right, diag))

                        End Select
                End Select

                Return Nothing
            End Function

            Public Function VisitExpression(node As BoundExpression, diagBag As DiagnosticBag) As TypedConstant
                Do
                    If node.IsConstant Then
                        If _binder.IsValidTypeForAttributeArgument(node.Type) Then
                            Return CreateTypedConstant(node.Type, node.ConstantValueOpt.Value)
                        Else
                            Return CreateErrorTypedConstant(node.Type)
                        End If
                    Else
                        Select Case node.Kind
                            Case BoundKind.GetType
                                Return VisitGetType(DirectCast(node, BoundGetType), diagBag)

                            Case BoundKind.ArrayCreation
                                Return VisitArrayCreation(DirectCast(node, BoundArrayCreation), diagBag)

                            Case BoundKind.DirectCast
                                Dim conv = DirectCast(node, BoundDirectCast)
                                If conv.HasErrors OrElse
                                   Not Conversions.IsWideningConversion(conv.ConversionKind) OrElse
                                   Not _binder.IsValidTypeForAttributeArgument(conv.Operand.Type) Then

                                    If Not conv.HasErrors Then
                                        ReportDiagnostic(diagBag, conv.Operand.Syntax, ERRID.ERR_RequiredAttributeConstConversion2, conv.Operand.Type, conv.Type)
                                    End If
                                    Return CreateErrorTypedConstant(node.Type)
                                Else
                                    node = conv.Operand
                                End If

                            Case BoundKind.TryCast
                                Dim conv = DirectCast(node, BoundTryCast)
                                If conv.HasErrors OrElse
                                   Not Conversions.IsWideningConversion(conv.ConversionKind) OrElse
                                   Not _binder.IsValidTypeForAttributeArgument(conv.Operand.Type) Then

                                    If Not conv.HasErrors Then
                                        ReportDiagnostic(diagBag, conv.Operand.Syntax, ERRID.ERR_RequiredAttributeConstConversion2, conv.Operand.Type, conv.Type)
                                    End If
                                    Return CreateErrorTypedConstant(node.Type)
                                Else
                                    node = conv.Operand
                                End If

                            Case BoundKind.Conversion
                                Dim conv = DirectCast(node, BoundConversion)
                                If conv.HasErrors OrElse
                                   Not Conversions.IsWideningConversion(conv.ConversionKind) OrElse
                                   Not _binder.IsValidTypeForAttributeArgument(conv.Operand.Type) Then

                                    If Not conv.HasErrors Then
                                        ReportDiagnostic(diagBag, conv.Operand.Syntax, ERRID.ERR_RequiredAttributeConstConversion2, conv.Operand.Type, conv.Type)
                                    End If
                                    Return CreateErrorTypedConstant(node.Type)
                                Else
                                    If node.Syntax.Kind = SyntaxKind.PredefinedCastExpression Then
                                        Dim cast = DirectCast(node.Syntax, PredefinedCastExpressionSyntax)

                                        If cast.Keyword.Kind = SyntaxKind.CObjKeyword Then
                                            InternalSyntax.Parser.CheckFeatureAvailability(diagBag,
                                                                                           cast.Keyword.GetLocation(),
                                                                                           DirectCast(cast.SyntaxTree, VisualBasicSyntaxTree).Options.LanguageVersion,
                                                                                           InternalSyntax.Feature.CObjInAttributeArguments)
                                        End If
                                    End If
                                    node = conv.Operand
                                End If

                            Case BoundKind.Parenthesized
                                node = DirectCast(node, BoundParenthesized).Expression

                            Case BoundKind.BadExpression
                                Return CreateErrorTypedConstant(node.Type)

                            Case Else
                                ReportDiagnostic(diagBag, node.Syntax, ERRID.ERR_RequiredConstExpr)
                                Return CreateErrorTypedConstant(node.Type)
                        End Select
                    End If
                Loop
            End Function

            Private Function VisitGetType(node As BoundGetType, diagBag As DiagnosticBag) As TypedConstant
                Dim sourceType = node.SourceType
                Dim getTypeArgument = sourceType.Type

                ' GetType argument is allowed to be:
                ' (a) an unbound type
                ' (b) a closed constructed type
                ' It can not be an open type. i.e. either all type arguments are missing or all type arguments do not contain any type parameter symbols.

                If getTypeArgument IsNot Nothing Then
                    Dim isValidArgument = getTypeArgument.IsUnboundGenericType OrElse Not getTypeArgument.IsOrRefersToTypeParameter

                    If Not isValidArgument Then
                        Dim diagInfo = New BadSymbolDiagnostic(getTypeArgument, ERRID.ERR_OpenTypeDisallowed)
                        ReportDiagnostic(diagBag, sourceType.Syntax, diagInfo)
                        Return CreateErrorTypedConstant(node.Type)
                    End If
                End If

                Return CreateTypedConstant(node.Type, getTypeArgument)
            End Function

            Private Function VisitArrayCreation(node As BoundArrayCreation, diag As DiagnosticBag) As TypedConstant
                Dim type = DirectCast(node.Type, ArrayTypeSymbol)

                Dim values As ImmutableArray(Of TypedConstant) = Nothing
                Dim initializerOpt = node.InitializerOpt

                If initializerOpt Is Nothing OrElse initializerOpt.Initializers.Length = 0 Then
                    If node.Bounds.Length = 1 Then
                        Dim lastIndex = node.Bounds(0)
                        If lastIndex.IsConstant AndAlso Not lastIndex.ConstantValueOpt.IsDefaultValue Then
                            ' Arrays used as attribute arguments require explicitly specifying the
                            ' values for all the elements. Note that we check this only for 1-D
                            ' arrays because only 1-D arrays are allowed as attribute arguments.
                            ' For all other array arguments, a more general error is given during
                            ' normal array initializer binding.

                            ReportDiagnostic(diag, initializerOpt.Syntax, ERRID.ERR_MissingValuesForArraysInApplAttrs)
                            _hasErrors = True
                        End If
                    End If
                End If

                If initializerOpt IsNot Nothing Then
                    values = VisitArguments(initializerOpt.Initializers, diag)
                End If
                Return CreateTypedConstant(type, values)
            End Function

            Private Shared Function CreateTypedConstant(type As ArrayTypeSymbol, array As ImmutableArray(Of TypedConstant)) As TypedConstant
                Return New TypedConstant(type, array)
            End Function

            Private Function CreateTypedConstant(type As TypeSymbol, value As Object) As TypedConstant
                Dim kind = TypedConstant.GetTypedConstantKind(type, _binder.Compilation)

                If kind = TypedConstantKind.Array Then
                    Debug.Assert(value Is Nothing)
                    Return New TypedConstant(type, Nothing)
                End If

                Return New TypedConstant(type, kind, value)
            End Function

            Private Function CreateErrorTypedConstant(type As TypeSymbol) As TypedConstant
                _hasErrors = True
                Return New TypedConstant(type, TypedConstantKind.Error, Nothing)
            End Function

        End Structure

#End Region

#Region "AnalyzedAttributeArguments"
        Private Structure AnalyzedAttributeArguments

            Public positionalArguments As ImmutableArray(Of BoundExpression)
            Public namedArguments As ImmutableArray(Of BoundExpression)

            Public Sub New(positionalArguments As ImmutableArray(Of BoundExpression), namedArguments As ImmutableArray(Of BoundExpression))
                Me.positionalArguments = positionalArguments
                Me.namedArguments = namedArguments
            End Sub

        End Structure
#End Region


    End Class

End Namespace
