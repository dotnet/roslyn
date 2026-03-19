' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class Binder
        Private Function BindObjectCreationExpression(
            node As ObjectCreationExpressionSyntax,
            diagnostics As BindingDiagnosticBag
        ) As BoundExpression

            DisallowNewOnTupleType(node.Type, diagnostics)
            Dim type As TypeSymbol = Me.BindTypeSyntax(node.Type, diagnostics)

            ' When the type is an error still try to bind the arguments for better data flow analysis and 
            ' to permit them to be analyzed via the binding API.
            If type.IsErrorType() Then

                Dim extendedErrorType = TryCast(type, ExtendedErrorTypeSymbol)

                If extendedErrorType IsNot Nothing AndAlso extendedErrorType.CandidateSymbols.Length = 1 AndAlso
                   extendedErrorType.CandidateSymbols(0).Kind = SymbolKind.NamedType Then
                    ' Continue binding with candidate symbol as the target type, but suppress any additional diagnostics
                    type = DirectCast(extendedErrorType.CandidateSymbols(0), TypeSymbol)
                    diagnostics = BindingDiagnosticBag.Discarded
                Else
                    Dim argumentDiagnostics = BindingDiagnosticBag.Discarded
                    Dim boundArguments As ImmutableArray(Of BoundExpression) = Nothing
                    Dim argumentNames As ImmutableArray(Of String) = Nothing
                    Dim argumentNamesLocations As ImmutableArray(Of Location) = Nothing

                    BindArgumentsAndNames(node.ArgumentList, boundArguments, argumentNames, argumentNamesLocations, argumentDiagnostics)

                    ' We also want to put into the bound bad expression node all bound arguments as 
                    ' r-values AND bound type which will be used for semantic info
                    Dim boundNodes = ArrayBuilder(Of BoundExpression).GetInstance()

                    ' Add all bound arguments as r-values
                    For Each arg In boundArguments
                        boundNodes.Add(MakeRValueAndIgnoreDiagnostics(arg))
                    Next

                    Dim boundInitializer As BoundExpression =
                        BindObjectCollectionOrMemberInitializer(node, type, Nothing, argumentDiagnostics)
                    If boundInitializer IsNot Nothing Then
                        boundNodes.Add(boundInitializer)
                    End If

                    argumentDiagnostics.Free()

                    Return BadExpression(node, boundNodes.ToImmutableAndFree(), type)
                End If
            End If

            Return BindObjectCreationExpression(node.Type, node.ArgumentList, type, node, diagnostics, Nothing)
        End Function

        Private Shared Sub DisallowNewOnTupleType(type As TypeSyntax, diagnostics As BindingDiagnosticBag)
            If type.Kind = SyntaxKind.TupleType Then
                diagnostics.Add(ERRID.ERR_NewWithTupleTypeSyntax, type.Location)
            End If
        End Sub

        Friend Function BindObjectCreationExpression(
            typeNode As TypeSyntax,
            argumentListOpt As ArgumentListSyntax,
            type0 As TypeSymbol,
            node As ObjectCreationExpressionSyntax,
            diagnostics As BindingDiagnosticBag,
            asNewVariablePlaceholderOpt As BoundWithLValueExpressionPlaceholder
        ) As BoundExpression

            Select Case type0.TypeKind
                Case TypeKind.Delegate
                    ' delegate creations need to handled differently.
                    Return BindDelegateCreationExpression(type0, argumentListOpt, node, diagnostics)

                Case TypeKind.Structure
                    ' SPECIAL CASE: process calls to a parameterless structure constructors separately
                    If argumentListOpt Is Nothing OrElse argumentListOpt.Arguments.Count = 0 Then
                        ' Skip overload resolution, go straight to the structure parameterless constructor if exists
                        Dim constructorSymbol As MethodSymbol = Nothing

                        ' NOTE: in case the structure has a constructor with all optional parameters 
                        '       we don't catch it here; this matches Dev10 behavior

                        Dim namedType = DirectCast(type0, NamedTypeSymbol)
                        Dim ctors = namedType.InstanceConstructors

                        If Not ctors.IsEmpty Then
                            For Each constructor In ctors
                                If constructor.ParameterCount = 0 Then
                                    '  the first parameterless constructor will do the job
                                    Dim useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics)
                                    If IsAccessible(constructor, useSiteInfo) Then
                                        ' if not accessible, just clear symbol information
                                        constructorSymbol = constructor
                                        ReportUseSite(diagnostics, node, constructorSymbol)
                                    End If

                                    diagnostics.Add(node, useSiteInfo)
                                    Exit For
                                End If
                            Next
                        End If

                        ' NOTE: we don't report an error if the constructor is inaccessible.
                        '       Emitter will just emit 'initobj' instead of constructor call instead

                        '  create a simplified object creation expression
                        Dim initializerOpt As BoundObjectInitializerExpressionBase = BindObjectCollectionOrMemberInitializer(node,
                                                                                type0,
                                                                                asNewVariablePlaceholderOpt,
                                                                                diagnostics)

                        CheckRequiredMembersInObjectInitializer(constructorSymbol, namedType, If(initializerOpt?.Initializers, ImmutableArray(Of BoundExpression).Empty), typeNode, diagnostics)

                        Return New BoundObjectCreationExpression(
                                        node,
                                        constructorSymbol,
                                        If(constructorSymbol Is Nothing,
                                           Nothing,
                                           New BoundMethodGroup(typeNode, Nothing,
                                                             ImmutableArray.Create(constructorSymbol), LookupResultKind.Good, Nothing,
                                                             QualificationKind.QualifiedViaTypeName)),
                                        arguments:=ImmutableArray(Of BoundExpression).Empty,
                                        defaultArguments:=BitVector.Null,
                                        initializerOpt,
                                        type0)
                    End If

            End Select

            ' Get the bound arguments and the argument names.
            Dim boundArguments As ImmutableArray(Of BoundExpression) = Nothing
            Dim argumentNames As ImmutableArray(Of String) = Nothing
            Dim argumentNamesLocations As ImmutableArray(Of Location) = Nothing

            BindArgumentsAndNames(argumentListOpt, boundArguments, argumentNames, argumentNamesLocations, diagnostics)

            Dim objectInitializerExpression = BindObjectCollectionOrMemberInitializer(node,
                                                                                      type0,
                                                                                      asNewVariablePlaceholderOpt,
                                                                                      diagnostics)
            Return BindObjectCreationExpression(typeNode,
                                                argumentListOpt,
                                                type0,
                                                node,
                                                boundArguments,
                                                argumentNames,
                                                objectInitializerExpression,
                                                diagnostics,
                                                callerInfoOpt:=typeNode)
        End Function

        Friend Function BindObjectCreationExpression(
            syntax As SyntaxNode,
            type As TypeSymbol,
            arguments As ImmutableArray(Of BoundExpression),
            diagnostics As BindingDiagnosticBag) As BoundExpression
            Return BindObjectCreationExpression(
                typeNode:=syntax,
                argumentListOpt:=Nothing,
                type0:=type,
                node:=syntax,
                boundArguments:=arguments,
                argumentNames:=Nothing,
                objectInitializerExpressionOpt:=Nothing,
                diagnostics:=diagnostics,
                callerInfoOpt:=Nothing)
        End Function

        Private Shared Function MergeBoundChildNodesWithObjectInitializerForBadNode(
            boundArguments As ImmutableArray(Of BoundExpression),
            objectInitializerExpression As BoundObjectInitializerExpressionBase
        ) As ImmutableArray(Of BoundExpression)
            Dim boundChildNodesForError = boundArguments

            If objectInitializerExpression IsNot Nothing Then
                boundChildNodesForError = boundChildNodesForError.Add(objectInitializerExpression)
            End If

            Return boundChildNodesForError
        End Function

        Private Function BindObjectCreationExpression(
            typeNode As SyntaxNode,
            argumentListOpt As ArgumentListSyntax,
            type0 As TypeSymbol,
            node As SyntaxNode,
            boundArguments As ImmutableArray(Of BoundExpression),
            argumentNames As ImmutableArray(Of String),
            objectInitializerExpressionOpt As BoundObjectInitializerExpressionBase,
            diagnostics As BindingDiagnosticBag,
            callerInfoOpt As VisualBasicSyntaxNode
        ) As BoundExpression

            Dim resultKind As LookupResultKind = LookupResultKind.Good
            Dim errorReported As Boolean = False        ' was an error already reported?
            Dim type As NamedTypeSymbol = Nothing

            Debug.Assert(objectInitializerExpressionOpt Is Nothing OrElse TypeSymbol.Equals(objectInitializerExpressionOpt.Type, type0, TypeCompareKind.ConsiderEverything))

            Select Case type0.TypeKind
                Case TypeKind.Class
                    If DirectCast(type0, NamedTypeSymbol).IsMustInherit Then
                        ReportDiagnostic(diagnostics, node, ERRID.ERR_NewOnAbstractClass)
                        resultKind = LookupResultKind.NotCreatable
                        errorReported = True
                    End If

                    type = DirectCast(type0, NamedTypeSymbol)

                Case TypeKind.Interface
                    Dim coClass As TypeSymbol = DirectCast(type0, NamedTypeSymbol).CoClassType
                    Dim diagInfo As DiagnosticInfo
                    If coClass IsNot Nothing Then

                        Select Case coClass.TypeKind
                            Case TypeKind.Error,
                                 TypeKind.Interface

                                ' Generate basic missing CoClass error
                                ' Note the same error of interfaces specified as CoClasses
                                diagInfo = ErrorFactory.ErrorInfo(ERRID.ERR_CoClassMissing2, coClass, type0)

                            Case TypeKind.Array
                                ' NOTE: In case of array type in CoClass Dev11 generates a call to 
                                '       Array's element type constructor which does not make any 
                                '       sense, we treat this case as a 'invalid-coclass' error
                                diagInfo = ErrorFactory.ErrorInfo(ERRID.ERR_InvalidCoClass1, coClass)

                            Case TypeKind.Class,
                                 TypeKind.Delegate,
                                 TypeKind.Enum,
                                 TypeKind.Module,
                                 TypeKind.Structure

                                ' NOTE: Dev11 does not 'see' implicit parameterless constructors in structs
                                ' NOTE: and enums while Roslyn does. This means that some edge case scenarios 
                                ' NOTE: of struct/enum CoClass types now work

                                Dim namedCoClass = DirectCast(coClass, NamedTypeSymbol)
                                If namedCoClass.IsUnboundGenericType Then
                                    ' Cannot use unbound generic type as a CoClass 
                                    diagInfo = ErrorFactory.ErrorInfo(ERRID.ERR_InvalidCoClass1, coClass)
                                    Exit Select
                                End If

                                ' Check accessibility
                                Dim useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics)
                                Dim isInAccessible As Boolean = (Me.CheckAccessibility(namedCoClass, useSiteInfo) <> AccessCheckResult.Accessible)
                                diagnostics.Add(node, useSiteInfo)

                                If isInAccessible Then
                                    ' CoClass is inaccessible
                                    diagInfo = New BadSymbolDiagnostic(coClass, ERRID.ERR_InAccessibleCoClass3, coClass, type0, coClass.DeclaredAccessibility.ToDiagnosticString())
                                    Exit Select
                                End If

                                ' NoPIA support
                                If type0.ContainingAssembly.IsLinked Then
                                    Return BindNoPiaObjectCreationExpression(node, type0, namedCoClass, boundArguments, objectInitializerExpressionOpt, diagnostics)
                                End If

                                If namedCoClass.IsMustInherit Then
                                    ' NOTE: Dev11 does allow abstract classes to serve as a CoClass 
                                    ' NOTE: types and generates not verifiable code as a result
                                    diagInfo = ErrorFactory.ErrorInfo(ERRID.ERR_InvalidCoClass1, coClass)
                                    Exit Select
                                End If

                                type = namedCoClass
                                diagInfo = Nothing

                            Case Else
                                Throw ExceptionUtilities.UnexpectedValue(coClass.TypeKind)
                        End Select

                    Else
                        diagInfo = ErrorFactory.ErrorInfo(ERRID.ERR_NewIfNullOnNonClass)
                    End If

                    If diagInfo IsNot Nothing Then
                        ReportDiagnostic(diagnostics, node, diagInfo)
                        resultKind = LookupResultKind.NotCreatable
                        errorReported = True
                        type = DirectCast(type0, NamedTypeSymbol)
                    End If

                Case TypeKind.Error
                    Return New BoundBadExpression(node, LookupResultKind.Empty,
                                                  ImmutableArray(Of Symbol).Empty,
                                                  MergeBoundChildNodesWithObjectInitializerForBadNode(boundArguments,
                                                                                                      objectInitializerExpressionOpt),
                                                  type0, hasErrors:=True)

                Case TypeKind.TypeParameter
                    Dim typeParameter = DirectCast(type0, TypeParameterSymbol)

                    If Not typeParameter.HasConstructorConstraint AndAlso Not typeParameter.IsValueType Then
                        ' "'New' cannot be used on a type parameter that does not have a 'New' constraint."
                        ReportDiagnostic(diagnostics, typeNode, ERRID.ERR_NewIfNullOnGenericParam)

                    ElseIf Not boundArguments.IsEmpty Then
                        ' "Arguments cannot be passed to a 'New' used on a type parameter."
                        Dim span = argumentListOpt.Arguments.Span
                        ReportDiagnostic(diagnostics, GetLocation(span), ERRID.ERR_NewArgsDisallowedForTypeParam)

                    Else
                        Return New BoundNewT(node,
                                             objectInitializerExpressionOpt,
                                             typeParameter)
                    End If

                    resultKind = LookupResultKind.NotCreatable
                    errorReported = True

                Case TypeKind.Enum, TypeKind.Structure
                    type = DirectCast(type0, NamedTypeSymbol)

                Case TypeKind.Module
                    ' the diagnostic that a module cannot be used as a type has already been reported
                    ' so there is nothing to do here.
                    type = DirectCast(type0, NamedTypeSymbol)
                    resultKind = LookupResultKind.NotCreatable
                    errorReported = True

                Case TypeKind.Array
                    ' the diagnostic that AsNew cannot be used to init arrays has already been reported
                    ' so there is nothing to do here.
                    resultKind = LookupResultKind.NotCreatable
                    errorReported = True

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(type0.TypeKind)

            End Select

            ' TODO: handle cases of types that are not newable (see type libraries and the 
            ' System.Runtime.InteropServices.TypeLibType attribute).
            ' example: 
            ' <System.Runtime.InteropServices.TypeLibType(System.Runtime.InteropServices.TypeLibTypeFlags.FHidden)>
            ' Class test
            ' End Class

            ' Filter out inaccessible constructors
            Dim resultExpression As BoundExpression
            Dim constructorsGroup As BoundMethodGroup = Nothing

            If type IsNot Nothing AndAlso Not type.IsInterface Then
                Dim useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics)
                Dim constructors As ImmutableArray(Of MethodSymbol) = GetAccessibleConstructors(type, useSiteInfo)

                diagnostics.Add(node, useSiteInfo)

                Dim groupResultKind As LookupResultKind = LookupResultKind.Good

                If constructors.Length = 0 Then
                    constructors = type.InstanceConstructors
                    groupResultKind = LookupResultKind.Inaccessible
                End If

                If constructors.Length > 0 Then
                    constructorsGroup = New BoundMethodGroup(typeNode, Nothing,
                                                             constructors, groupResultKind, Nothing,
                                                             QualificationKind.QualifiedViaTypeName).MakeCompilerGenerated()
                End If
            End If

            If constructorsGroup Is Nothing OrElse constructorsGroup.ResultKind = LookupResultKind.Inaccessible Then
                If Not errorReported Then
                    ReportDiagnostic(diagnostics, If(typeNode.IsKind(SyntaxKind.QualifiedName), DirectCast(typeNode, QualifiedNameSyntax).Right, typeNode), ErrorFactory.ErrorInfo(ERRID.ERR_NoViableOverloadCandidates1, "New"))
                End If

                ' Suppress any additional diagnostics
                diagnostics = BindingDiagnosticBag.Discarded
            End If

            If constructorsGroup Is Nothing Then
                resultExpression = New BoundBadExpression(node, LookupResult.WorseResultKind(resultKind, LookupResultKind.Empty),
                                                          ImmutableArray(Of Symbol).Empty,
                                                          MergeBoundChildNodesWithObjectInitializerForBadNode(boundArguments,
                                                                                                              objectInitializerExpressionOpt),
                                                          type0, hasErrors:=True)
            Else
                ' We rely on the asserted condition when we are merging/changing result kinds below. 
                Debug.Assert(constructorsGroup.ResultKind = LookupResultKind.Good OrElse constructorsGroup.ResultKind = LookupResultKind.Inaccessible)

                Dim useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics)
                Dim results As OverloadResolution.OverloadResolutionResult = OverloadResolution.MethodInvocationOverloadResolution(constructorsGroup,
                                                                                                                                   boundArguments,
                                                                                                                                   argumentNames,
                                                                                                                                   Me,
                                                                                                                                   callerInfoOpt,
                                                                                                                                   useSiteInfo)

                If diagnostics.Add(node, useSiteInfo) Then
                    If constructorsGroup.ResultKind <> LookupResultKind.Inaccessible Then
                        ' Suppress additional diagnostics
                        diagnostics = BindingDiagnosticBag.Discarded
                    End If
                End If

                If Not results.BestResult.HasValue Then

                    ' Create and report the diagnostic.
                    If results.Candidates.Length = 0 Then
                        results = OverloadResolution.MethodInvocationOverloadResolution(constructorsGroup, boundArguments, argumentNames, Me, includeEliminatedCandidates:=True, callerInfoOpt:=callerInfoOpt,
                                                                                        useSiteInfo:=CompoundUseSiteInfo(Of AssemblySymbol).Discarded)
                    End If

                    ' NOTE: current services implementation expects all diagnostics to be associated with type node, not with a node itself
                    resultExpression = ReportOverloadResolutionFailureAndProduceBoundNode(node,
                                                                                          constructorsGroup,
                                                                                          boundArguments, argumentNames,
                                                                                          results, diagnostics, callerInfoOpt,
                                                                                          overrideCommonReturnType:=type0)

                    ' update the bad expression to also contain the bound initializer.
                    Dim badExpression = DirectCast(resultExpression, BoundBadExpression)

                    ' Here we have possibly three different LookupResultKinds to store
                    ' 1) LookupResultKind.NotCreatable - if the type is not creatable
                    ' 2) LookupResultKind.OverloadResolutionFailure 
                    ' 3) LookupResultKind from constructorsGroup

                    ' Let's preserve two worst since we only have two locations to store them.
                    Dim children = ArrayBuilder(Of BoundExpression).GetInstance()

#If DEBUG Then
                    Dim foundGroup As Boolean = False
#End If

                    For Each child In badExpression.ChildBoundNodes
                        If child Is constructorsGroup Then
#If DEBUG Then
                            foundGroup = True
#End If
                            children.Add(constructorsGroup.Update(constructorsGroup.TypeArgumentsOpt, constructorsGroup.Methods, constructorsGroup.PendingExtensionMethodsOpt,
                                                                  LookupResult.WorseResultKind(constructorsGroup.ResultKind, badExpression.ResultKind), constructorsGroup.ReceiverOpt,
                                                                  constructorsGroup.QualificationKind))
                        Else
                            children.Add(child)
                        End If
                    Next

#If DEBUG Then
                    Debug.Assert(foundGroup)
#End If

                    If objectInitializerExpressionOpt IsNot Nothing Then
                        children.Add(objectInitializerExpressionOpt)
                    End If

                    resultExpression = badExpression.Update(LookupResult.WorseResultKind(resultKind, badExpression.ResultKind),
                                                            badExpression.Symbols,
                                                            children.ToImmutableAndFree(),
                                                            badExpression.Type)
                Else

                    Dim methodResult = results.BestResult.Value

                    Dim argumentInfo As (Arguments As ImmutableArray(Of BoundExpression), DefaultArguments As BitVector) = PassArguments(typeNode, methodResult, boundArguments, diagnostics)
                    boundArguments = argumentInfo.Arguments

                    ReportDiagnosticsIfObsoleteOrNotSupported(diagnostics, methodResult.Candidate.UnderlyingSymbol, node)

                    ' If a coclass was instantiated, convert the class to the interface type.
                    If type0.IsInterfaceType() Then
                        Debug.Assert(type.Equals(DirectCast(type0, NamedTypeSymbol).CoClassType))
                        ApplyImplicitConversion(node, type0, New BoundRValuePlaceholder(node, type), diagnostics)
                    Else
                        Debug.Assert(TypeSymbol.Equals(type, type0, TypeCompareKind.ConsiderEverything))
                    End If

                    ' If the type was not creatable, create a bad expression so that semantic model results can reflect that.
                    If resultKind <> LookupResultKind.Good Then
                        Dim children = ArrayBuilder(Of BoundExpression).GetInstance()

                        children.Add(constructorsGroup)
                        children.AddRange(boundArguments)

                        If objectInitializerExpressionOpt IsNot Nothing Then
                            children.Add(objectInitializerExpressionOpt)
                        End If

                        resultExpression = New BoundBadExpression(node, resultKind,
                                                                  ImmutableArray.Create(Of Symbol)(methodResult.Candidate.UnderlyingSymbol),
                                                                  children.ToImmutableAndFree(),
                                                                  type0, hasErrors:=True)
                    Else
                        Dim constructorSymbol As MethodSymbol = DirectCast(methodResult.Candidate.UnderlyingSymbol, MethodSymbol)

                        CheckRequiredMembersInObjectInitializer(constructorSymbol, constructorSymbol.ContainingType, If(objectInitializerExpressionOpt?.Initializers, ImmutableArray(Of BoundExpression).Empty), typeNode, diagnostics)

                        resultExpression = New BoundObjectCreationExpression(node,
                                                                             constructorSymbol,
                                                                             constructorsGroup,
                                                                             boundArguments,
                                                                             argumentInfo.DefaultArguments,
                                                                             objectInitializerExpressionOpt,
                                                                             type0)
                    End If
                End If
            End If

            Debug.Assert(resultExpression.Type.IsSameTypeIgnoringAll(type0))
            Debug.Assert(LookupResult.WorseResultKind(resultKind, resultExpression.ResultKind) = resultExpression.ResultKind)

            Return resultExpression
        End Function

        Friend Shared Sub CheckRequiredMembersInObjectInitializer(
            constructor As MethodSymbol,
            containingType As NamedTypeSymbol,
            initializers As ImmutableArray(Of BoundExpression),
            creationSyntax As SyntaxNode,
            diagnostics As BindingDiagnosticBag)

            ' The only time constructor will be null is if we're trying to invoke a parameterless struct ctor, and it's not accessible (such as being protected).
            Debug.Assert((constructor IsNot Nothing AndAlso ReferenceEquals(constructor.ContainingType, containingType)) OrElse containingType.IsStructureType())

            If constructor IsNot Nothing AndAlso constructor.HasSetsRequiredMembers Then
                Return
            End If

            If containingType.HasRequiredMembersError Then
                ' A use-site diagnostic will be reported on the use, so we don't need to do any more checking here.
                Return
            End If

            Dim requiredMembers = containingType.AllRequiredMembers

            If requiredMembers.Count = 0 Then
                Return
            End If

            Dim requiredMembersBuilder = requiredMembers.ToBuilder()

            If Not initializers.IsDefaultOrEmpty Then
                For Each initializer In initializers
                    Dim assignmentOperator = TryCast(initializer, BoundAssignmentOperator)
                    If assignmentOperator Is Nothing Then
                        Continue For
                    End If

                    Dim memberSymbol As Symbol = If(
                        DirectCast(TryCast(assignmentOperator.Left, BoundPropertyAccess)?.PropertySymbol, Symbol),
                        TryCast(assignmentOperator.Left, BoundFieldAccess)?.FieldSymbol)

                    If memberSymbol Is Nothing Then
                        Continue For
                    End If

                    Dim requiredMember As Symbol = Nothing
                    If Not requiredMembersBuilder.TryGetValue(memberSymbol.Name, requiredMember) Then
                        Continue For
                    End If

                    If Not memberSymbol.Equals(requiredMember, TypeCompareKind.AllIgnoreOptionsForVB) Then
                        Continue For
                    End If

                    requiredMembersBuilder.Remove(memberSymbol.Name)
                Next
            End If

            For Each kvp In requiredMembersBuilder
                diagnostics.Add(ERRID.ERR_RequiredMemberMustBeSet, creationSyntax.Location, kvp.Value)
            Next
        End Sub

        Private Function BindNoPiaObjectCreationExpression(
            node As SyntaxNode,
            [interface] As TypeSymbol,
            coClass As NamedTypeSymbol,
            boundArguments As ImmutableArray(Of BoundExpression),
            initializerOpt As BoundObjectInitializerExpressionBase,
            diagnostics As BindingDiagnosticBag
        ) As BoundExpression
            Dim hasErrors = False

            Dim guidString As String = Nothing
            If Not coClass.GetGuidString(guidString) Then
                ' Match Dev11 VB by reporting ERRID_NoPIAAttributeMissing2 if guid isn't there.
                ' C# doesn't complain and instead uses zero guid.
                ReportDiagnostic(diagnostics, node, ERRID.ERR_NoPIAAttributeMissing2, coClass, AttributeDescription.GuidAttribute.FullName)
                hasErrors = True
            End If

            Dim expr = New BoundNoPiaObjectCreationExpression(node, guidString, initializerOpt, [interface], hasErrors)

            If boundArguments.Any() Then
                ' Note: Dev11 silently drops any arguments and does not report an error.
                ReportDiagnostic(diagnostics, node, ERRID.ERR_NoArgumentCountOverloadCandidates1, "New")

                Dim children = boundArguments.Add(expr)
                Return BadExpression(node, children, expr.Type)
            End If

            Return expr
        End Function

        ''' <summary>
        ''' Binds the object collection or member initializer from a object creation.
        ''' E.g. "new CollType() From {...}" or "new AType() With {...}"
        ''' </summary>
        ''' <param name="initializedObjectType">The type of the created object expression.</param>
        ''' <param name="syntaxNode">The object creation expression syntax.</param>
        ''' <param name="diagnostics">The diagnostics.</param>
        Private Function BindObjectCollectionOrMemberInitializer(
            syntaxNode As ObjectCreationExpressionSyntax,
            initializedObjectType As TypeSymbol,
            asNewVariablePlaceholderOpt As BoundWithLValueExpressionPlaceholder,
            diagnostics As BindingDiagnosticBag
        ) As BoundObjectInitializerExpressionBase

            If syntaxNode.Initializer Is Nothing Then
                Return Nothing
            End If

            If syntaxNode.Initializer.Kind = SyntaxKind.ObjectMemberInitializer Then
                Return BindObjectInitializer(syntaxNode, initializedObjectType, asNewVariablePlaceholderOpt, diagnostics)

            ElseIf syntaxNode.Initializer.Kind = SyntaxKind.ObjectCollectionInitializer Then
                Return BindCollectionInitializer(syntaxNode, initializedObjectType, diagnostics)

            Else
                Throw ExceptionUtilities.UnexpectedValue(syntaxNode.Initializer.Kind)
            End If
        End Function

        ''' <summary>
        ''' Bind the ObjectInitializer.
        ''' During the binding we basically bind the member access for each initializer, as well as the value that will be assigned.
        ''' The main information stored in the bound node is a list of assignment operators (that may contain placeholders), as
        ''' well as the information whether expression creates a temporary or not.
        ''' </summary>
        Private Function BindObjectInitializer(
            objectCreationSyntax As ObjectCreationExpressionSyntax,
            initializedObjectType As TypeSymbol,
            asNewVariablePlaceholderOpt As BoundWithLValueExpressionPlaceholder,
            diagnostics As BindingDiagnosticBag
        ) As BoundObjectInitializerExpression

            Dim memberInitializerSyntax = DirectCast(objectCreationSyntax.Initializer, ObjectMemberInitializerSyntax)

            ' We need to use a declared variable directly if it is declared using AsNew and it's type is a value type or a type 
            ' parameter with a structure constraint.
            ' Otherwise the object initializer should initialize a temporary.
            ' According to Jonathan Aneja this is a bug and should always use a temporary, but unfortunately it's what Dev10 emits
            ' (we have legacy tests that check this specifically).
            Dim createTemporary = asNewVariablePlaceholderOpt Is Nothing OrElse Not initializedObjectType.IsValueType

            Dim variableOrTempPlaceholder As BoundWithLValueExpressionPlaceholder = Nothing
            If createTemporary Then
                variableOrTempPlaceholder = New BoundWithLValueExpressionPlaceholder(objectCreationSyntax, initializedObjectType)
                variableOrTempPlaceholder.SetWasCompilerGenerated()
            Else
                Debug.Assert(asNewVariablePlaceholderOpt IsNot Nothing)
                variableOrTempPlaceholder = asNewVariablePlaceholderOpt
            End If

            Dim objectInitializerBinder = New ObjectInitializerBinder(Me, variableOrTempPlaceholder)

            ' bind RHS for error cases.
            If initializedObjectType.SpecialType = SpecialType.System_Object OrElse initializedObjectType.IsErrorType Then
                ' VB Spec 11.10.1:
                ' "the member access will not be late bound if the type being constructed is Object"
                If initializedObjectType.SpecialType = SpecialType.System_Object Then
                    ' BC30994: Object initializer syntax cannot be used to initialize an instance of 'System.Object'.
                    ReportDiagnostic(diagnostics, memberInitializerSyntax, ErrorFactory.ErrorInfo(ERRID.ERR_AggrInitInvalidForObject))
                End If

                ' Bind RHS of the assignments for the sake of error reporting
                Dim initializerCount = memberInitializerSyntax.Initializers.Count
                Dim boundAssignmentValues(initializerCount - 1) As BoundExpression
                For fieldIndex = 0 To initializerCount - 1
                    Dim boundValue = objectInitializerBinder.BindValue(DirectCast(memberInitializerSyntax.Initializers(fieldIndex),
                                                                                  NamedFieldInitializerSyntax).Expression,
                                                                       diagnostics)
                    boundAssignmentValues(fieldIndex) = MakeRValueAndIgnoreDiagnostics(boundValue)
                Next

                Return New BoundObjectInitializerExpression(objectCreationSyntax.Initializer,
                                                            True,
                                                            variableOrTempPlaceholder,
                                                            boundAssignmentValues.AsImmutableOrNull,
                                                            initializedObjectType,
                                                            hasErrors:=True)
            End If

            Dim processedMembers As New HashSet(Of String)(CaseInsensitiveComparison.Comparer)
            Dim memberAssignments = ArrayBuilder(Of BoundExpression).GetInstance

            ' The temporary diagnostic bag is needed to collect diagnostics until it is known that the accessed symbol
            ' is a field or property, and if so, if it is shared or not.
            ' The temporary error messages should only be shown if binding the member access itself failed, or the bound
            ' member access is usable for the initialization (non shared, writable field or property).
            ' Otherwise more specific diagnostics will be shown .
            Dim memberBindingDiagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics:=True, withDependencies:=diagnostics.AccumulatesDependencies)

            For Each fieldInitializer In memberInitializerSyntax.Initializers
                ' NamedFieldInitializerSyntax is derived from FieldInitializerSyntax, which has this optional keyword as a member
                ' however it should not be set in case of a NamedFieldInitializerSyntax
                Debug.Assert(fieldInitializer.KeyKeyword.Kind <> SyntaxKind.KeyKeyword)

                Dim target As BoundExpression
                Dim namedFieldInitializer = DirectCast(fieldInitializer, NamedFieldInitializerSyntax)
                Dim fieldName As IdentifierNameSyntax = namedFieldInitializer.Name

                ' we're only binding the target if there were no previous diagnostics on the identifier.
                ' In that case binding the target would always fail.
                If Not fieldName.HasErrors Then

                    ' no need to use the memberInitializerBinder here, because there is no MemberAccessExpression 
                    target = BindMemberAccess(fieldName, variableOrTempPlaceholder, fieldName, False, memberBindingDiagnostics)
                    diagnostics.AddRange(memberBindingDiagnostics)

                    Dim identifierName As String = fieldName.Identifier.ValueText

                    If target.Kind = BoundKind.FieldAccess OrElse target.Kind = BoundKind.PropertyGroup Then
                        target = BindAssignmentTarget(fieldName,
                                                      target,
                                                      diagnostics)

                        Dim propertyAccess = TryCast(target, BoundPropertyAccess)

                        If propertyAccess IsNot Nothing Then
                            Debug.Assert(propertyAccess.AccessKind = PropertyAccessKind.Unknown)
                            ' See if we can reclassify access as writable given that this is an object initializer.
                            ' This is needed to accommodate init-only properties.
                            If propertyAccess.AccessKind <> PropertyAccessKind.Get AndAlso Not propertyAccess.IsWriteable AndAlso
                               propertyAccess.PropertySymbol.IsWritable(propertyAccess.ReceiverOpt, Me, isKnownTargetOfObjectMemberInitializer:=True) Then

                                propertyAccess = propertyAccess.Update(propertyAccess.PropertySymbol, propertyAccess.PropertyGroupOpt, propertyAccess.AccessKind, isWriteable:=True,
                                                                       propertyAccess.IsLValue, propertyAccess.ReceiverOpt, propertyAccess.Arguments, propertyAccess.DefaultArguments,
                                                                       propertyAccess.Type)
                                target = propertyAccess
                            End If
                        End If

                        If Not target.HasErrors Then
                            Dim isShared As Boolean
                            If target.Kind = BoundKind.FieldAccess Then
                                isShared = DirectCast(target, BoundFieldAccess).FieldSymbol.IsShared
                            Else
                                Dim [property] = propertyAccess.PropertySymbol
                                ' Treat extension properties as Shared in this context so we generate
                                ' an error (BC30991) that such properties cannot be used in an initializer.
                                ' Currently, there is only one extension property, InternalXmlHelper.Value:
                                ' e.g.: New List(Of XElement) With {.Value = Nothing}. Consider a specific
                                ' error for extension properties if this scenario is more common.
                                isShared = ([property].ReducedFrom IsNot Nothing) OrElse [property].IsShared
                            End If

                            If isShared Then
                                ' report that initializing shared members is not supported
                                ' BC30991: Member '{0}' cannot be initialized in an object initializer expression because it is shared.
                                ReportDiagnostic(diagnostics,
                                                 fieldName,
                                                 ErrorFactory.ErrorInfo(ERRID.ERR_SharedMemberAggrMemberInit1, identifierName))
                            Else
                                ' each field can only be initialized once.
                                ' only do this if the target did not show errors, to reduce noise
                                If Not processedMembers.Add(identifierName) Then
                                    ReportDiagnostic(diagnostics,
                                                     fieldName,
                                                     ErrorFactory.ErrorInfo(ERRID.ERR_DuplicateAggrMemberInit1, identifierName))
                                End If
                            End If
                        End If
                    Else
                        ' if the node already had diagnostics, report these, otherwise report that the accessed member
                        ' is not a field or property.
                        If Not memberBindingDiagnostics.HasAnyErrors Then
                            ' BC30990: Member '{0}' cannot be initialized in an object initializer expression because it is not a field or property.
                            ReportDiagnostic(diagnostics,
                                             fieldName,
                                             ErrorFactory.ErrorInfo(ERRID.ERR_NonFieldPropertyAggrMemberInit1, identifierName))
                        End If

                        target = BadExpression(namedFieldInitializer,
                                               target,
                                               ErrorTypeSymbol.UnknownResultType).MakeCompilerGenerated()
                    End If
                Else
                    target = BadExpression(namedFieldInitializer, ErrorTypeSymbol.UnknownResultType).MakeCompilerGenerated()
                End If

                ' in contrast to Dev10 Roslyn continues to bind the initialization value even if the receiver had errors.
                Dim value = objectInitializerBinder.BindValue(namedFieldInitializer.Expression, diagnostics)
                ' no need to apply an implicit conversion here, this will be done in BindAssignment

                Dim assignmentOperator As BoundExpression = BindAssignment(namedFieldInitializer, target, value, diagnostics)
                memberAssignments.Add(assignmentOperator)

                ' assert that the conversion really happened.
                Debug.Assert(TypeSymbol.Equals(DirectCast(memberAssignments.Last, BoundAssignmentOperator).Right.Type, DirectCast(memberAssignments.Last, BoundAssignmentOperator).Left.Type, TypeCompareKind.ConsiderEverything))

                memberBindingDiagnostics.Clear()
            Next

            memberBindingDiagnostics.Free()

            Return New BoundObjectInitializerExpression(objectCreationSyntax.Initializer,
                                                        createTemporary,
                                                        variableOrTempPlaceholder,
                                                        memberAssignments.ToImmutableAndFree,
                                                        initializedObjectType)
        End Function

        ''' <summary>
        ''' Binds a object collection initializer.
        ''' During the binding of this node we are binding calls to Add methods of the created object. Once the "collection" 
        ''' type passed the requirements (same as for each collection requirements + must have accessible Add method), all 
        ''' diagnostics are handled by the overload resolution.
        ''' The bound node contains a list of call expressions (that may contain placeholders).
        ''' </summary>
        Private Function BindCollectionInitializer(
            objectCreationSyntax As ObjectCreationExpressionSyntax,
            initializedObjectType As TypeSymbol,
            diagnostics As BindingDiagnosticBag
        ) As BoundCollectionInitializerExpression

            Dim collectionInitializerSyntax = DirectCast(objectCreationSyntax.Initializer, ObjectCollectionInitializerSyntax)
            Dim collectionType = initializedObjectType

            Dim unusedType As TypeSymbol = Nothing
            Dim unusedExpression As BoundExpression = Nothing
            Dim unusedLValuePlaceholder As BoundLValuePlaceholder = Nothing
            Dim unusedRValuePlaceholder As BoundRValuePlaceholder = Nothing
            Dim temporaryDiagnostics = BindingDiagnosticBag.GetInstance(diagnostics)
            Dim matchesDesignPattern As Boolean = False

            ' From Dev10:
            ' Verification of collection type: first we ensure that the collection type really is a collection, i.e.
            ' basically that once we've created the collection then people will be able to "for each" over it.
            ' lwischik: WHY? All we're doing is invoking the "Add" method on something. Why even care
            ' that people can enumerate it afterwards? Was this intended to support "new Collection from MyEnumerable"?
            ' But here we're checking the collection pNewExpression, rather than "From" expression pInput->Initializer...
            ' Answer: that's what the decision was when the spec was designed. The motive was that this "From"
            ' is really intended to be used for collections, and not just for arbitrary classes that happen
            ' to have an "Add" method.

            ' Spec 10.9 (this comment is in answer to bug Dev10#531849): a type is considered a collection type if
            '    (1) it satisfies MatchesForEachCollectionDesignPattern (i.e. has a method named GetEnumerator() which
            '        returns a type with MoveNext/Current); or
            '    (2) it implements System.Collections.Generic.IEnumerable(Of T); or
            '    (3) it implements System.Collections.IEnumerable.
            If MatchesForEachCollectionDesignPattern(collectionType,
                                                     New BoundRValuePlaceholder(collectionInitializerSyntax, initializedObjectType),
                                                     unusedType,
                                                     unusedExpression,
                                                     unusedLValuePlaceholder,
                                                     unusedExpression,
                                                     unusedExpression,
                                                     unusedRValuePlaceholder,
                                                     temporaryDiagnostics) Then
                diagnostics.AddRange(temporaryDiagnostics)
                matchesDesignPattern = True

            Else
                Dim ienumerableUseSiteDiagnostics = BindingDiagnosticBag.GetInstance(diagnostics)
                Dim ienumerable = GetSpecialType(SpecialType.System_Collections_IEnumerable,
                                                 objectCreationSyntax,
                                                 ienumerableUseSiteDiagnostics)

                Dim useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics)
                If IsOrInheritsFromOrImplementsInterface(collectionType, ienumerable, useSiteInfo) Then
                    diagnostics.AddRange(ienumerableUseSiteDiagnostics)
                    matchesDesignPattern = True

                Else
                    ReportDiagnostic(diagnostics,
                                     collectionInitializerSyntax,
                                     ErrorFactory.ErrorInfo(ERRID.ERR_NotACollection1, collectionType.Name))
                End If

                diagnostics.Add(collectionInitializerSyntax, useSiteInfo)

                ienumerableUseSiteDiagnostics.Free()
            End If

            Dim result = LookupResult.GetInstance
            temporaryDiagnostics.Clear()

            GetMemberIfMatchesRequirements(WellKnownMemberNames.CollectionInitializerAddMethodName,
                                           collectionType,
                                           Function(y As Symbol) As Boolean
                                               Return y.Kind = SymbolKind.Method
                                           End Function,
                                           result,
                                           collectionInitializerSyntax,
                                           temporaryDiagnostics)

            Dim initializers = collectionInitializerSyntax.Initializer.Initializers
            Dim initializerCount = initializers.Count
            Dim addInvocationExpressions As ImmutableArray(Of BoundExpression)
            Dim placeholder As BoundWithLValueExpressionPlaceholder = Nothing

            If result.IsGood Then
                diagnostics.AddRange(temporaryDiagnostics)
            ElseIf matchesDesignPattern Then
                ' the collection does not have a single accessible Add method.
                ' do not report this error if the collection did not match the design pattern
                ReportDiagnostic(diagnostics,
                                 collectionInitializerSyntax,
                                 ErrorFactory.ErrorInfo(ERRID.ERR_NoAddMethod1, collectionType))
            End If

            temporaryDiagnostics.Free()

            ' As long as there is a least one accessible Add method we will accept the collection and start 
            ' binding call expressions with the given arguments. All error reporting will now come from the 
            ' overload resolution.

            Dim addInvocations(initializerCount - 1) As BoundExpression

            ' iterate over the top level initializers.
            placeholder = New BoundWithLValueExpressionPlaceholder(objectCreationSyntax,
                                                                   collectionType)
            placeholder.SetWasCompilerGenerated()

            For initializerIndex = 0 To initializerCount - 1
                addInvocations(initializerIndex) = BindCollectionInitializerElement(initializers(initializerIndex),
                                                                                    placeholder,
                                                                                    result,
                                                                                    diagnostics)
            Next

            addInvocationExpressions = addInvocations.AsImmutableOrNull
            result.Free()

            Return New BoundCollectionInitializerExpression(objectCreationSyntax.Initializer,
                                                            placeholder,
                                                            addInvocationExpressions,
                                                            collectionType)
        End Function

        ''' <summary>
        ''' Binds a call expression for a given top level object collection initializer.
        ''' </summary>
        Private Function BindCollectionInitializerElement(
            topLevelInitializer As ExpressionSyntax,
            placeholder As BoundWithLValueExpressionPlaceholder,
            result As LookupResult,
            diagnostics As BindingDiagnosticBag
        ) As BoundExpression

            Dim arguments = ArrayBuilder(Of BoundExpression).GetInstance
            If topLevelInitializer.Kind = SyntaxKind.CollectionInitializer Then
                ' handle "... From {{1, 2, 3}, ..."
                Dim collectionInitializer = DirectCast(topLevelInitializer, CollectionInitializerSyntax)

                Dim initializers As SeparatedSyntaxList(Of ExpressionSyntax) = collectionInitializer.Initializers
                If initializers.IsEmpty Then
                    ReportDiagnostic(diagnostics, topLevelInitializer, ErrorFactory.ErrorInfo(ERRID.ERR_EmptyAggregateInitializer))
                Else
                    For Each expression In initializers
                        arguments.Add(BindValue(expression, diagnostics))
                    Next
                End If
            Else
                ' handle "... From {1, ..."
                arguments.Add(BindValue(topLevelInitializer, diagnostics))
            End If

            If result.IsGood() AndAlso Not arguments.IsEmpty Then
                Dim methodGroup As BoundMethodGroup = CreateBoundMethodGroup(topLevelInitializer,
                                                                             result,
                                                                             LookupOptions.AllMethodsOfAnyArity,
                                                                             diagnostics.AccumulatesDependencies,
                                                                             placeholder,
                                                                             Nothing,
                                                                             QualificationKind.QualifiedViaValue).MakeCompilerGenerated()

                Dim invocation = BindInvocationExpression(topLevelInitializer, topLevelInitializer,
                                                          TypeCharacter.None,
                                                          methodGroup,
                                                          arguments.ToImmutableAndFree,
                                                          Nothing,
                                                          diagnostics,
                                                          callerInfoOpt:=topLevelInitializer)
                invocation.SetWasCompilerGenerated()

                If invocation.Kind = BoundKind.LateInvocation Then
                    invocation = DirectCast(invocation, BoundLateInvocation).SetLateBoundAccessKind(LateBoundAccessKind.Call)
                End If

                Return invocation
            Else
                Return New BoundBadExpression(topLevelInitializer,
                                              LookupResultKind.Empty,
                                              ImmutableArray(Of Symbol).Empty,
                                              arguments.ToImmutableAndFree,
                                              ErrorTypeSymbol.UnknownResultType,
                                              hasErrors:=True).MakeCompilerGenerated()
            End If
        End Function

    End Class

    ''' <summary>
    ''' Special binder for binding ObjectInitializers. 
    ''' This binder stores a reference to the receiver of the initialization, because fields in an object initializer can be 
    ''' referenced with an omitted left expression in a member access expression (e.g. .Fieldname = .OtherFieldname).
    ''' </summary>
    Friend Class ObjectInitializerBinder
        Inherits Binder

        Private ReadOnly _receiver As BoundExpression

        Public Sub New(containingBinder As Binder, receiver As BoundExpression)
            MyBase.New(containingBinder)

            _receiver = receiver
        End Sub

        ''' <summary>
        ''' Use the receiver of the ObjectCreationExpression as the omitted left of a member access.
        ''' </summary>
        Protected Friend Overrides Function TryBindOmittedLeftForMemberAccess(
            node As MemberAccessExpressionSyntax,
            diagnostics As BindingDiagnosticBag,
            accessingBinder As Binder,
            ByRef wholeMemberAccessExpressionBound As Boolean
        ) As BoundExpression

            Return _receiver
        End Function

        Protected Friend Overrides Function TryBindOmittedLeftForXmlMemberAccess(node As XmlMemberAccessExpressionSyntax, diagnostics As BindingDiagnosticBag, accessingBinder As Binder) As BoundExpression
            Return _receiver
        End Function

        ''' <summary>
        ''' Use the receiver of the ObjectCreationExpression to as the omitted left of a dictionary access.
        ''' </summary>
        Protected Overrides Function TryBindOmittedLeftForDictionaryAccess(
                    node As MemberAccessExpressionSyntax,
                    accessingBinder As Binder,
                    diagnostics As BindingDiagnosticBag
                ) As BoundExpression

            Return _receiver
        End Function

        Protected Overrides Function TryBindOmittedLeftForConditionalAccess(node As ConditionalAccessExpressionSyntax, accessingBinder As Binder, diagnostics As BindingDiagnosticBag) As BoundExpression
            Return Nothing
        End Function
    End Class
End Namespace
