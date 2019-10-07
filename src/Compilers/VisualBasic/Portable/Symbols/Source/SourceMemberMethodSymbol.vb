' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents a method declared in source. 
    ''' </summary>
    Friend NotInheritable Class SourceMemberMethodSymbol
        Inherits SourceNonPropertyAccessorMethodSymbol

        Private ReadOnly _name As String

        ' Cache this value upon creation as it is needed for LookupSymbols and is expensive to 
        ' compute by creating the actual type parameters.
        Private ReadOnly _arity As Integer

        ' Flags indicates results of quick scan of the attributes
        Private ReadOnly _quickAttributes As QuickAttributes

        Private _lazyMetadataName As String

        ' The explicitly implemented interface methods, or Empty if none.
        Private _lazyImplementedMethods As ImmutableArray(Of MethodSymbol)

        ' Type parameters. Nothing if none.
        Private _lazyTypeParameters As ImmutableArray(Of TypeParameterSymbol)

        ' The overridden or hidden methods.
        Private _lazyHandles As ImmutableArray(Of HandledEvent)

        ''' <summary>
        ''' If this symbol represents a partial method definition or implementation part, its other part (if any).
        ''' This should be set, if at all, before this symbol appears among the members of its owner.  
        ''' The implementation part is not listed among the "members" of the enclosing type.
        ''' </summary>
        Private _otherPartOfPartial As SourceMemberMethodSymbol

        ''' <summary>
        ''' In case the method is an 'Async' method, stores the reference to a state machine type 
        ''' synthesized in AsyncRewriter. Note, that this field is mutable and is being assigned  
        ''' by calling AssignAsyncStateMachineType(...).
        ''' </summary>
        Private _asyncStateMachineType As NamedTypeSymbol = Nothing

        ' lazily evaluated state of the symbol (StateFlags)
        Private _lazyState As Integer

        <Flags>
        Private Enum StateFlags As Integer
            ''' <summary>
            ''' If this flag is set this method will be ignored 
            ''' in duplicated signature analysis, see ERR_DuplicateProcDef1 diagnostics.
            ''' </summary>
            SuppressDuplicateProcDefDiagnostics = &H1

            ''' <summary>
            ''' Set after all diagnostics have been reported for this symbol.
            ''' </summary>
            AllDiagnosticsReported = &H2
        End Enum

#If DEBUG Then
        Private _partialMethodInfoIsFrozen As Boolean = False
#End If

        Friend Sub New(containingType As SourceMemberContainerTypeSymbol,
                       name As String,
                       flags As SourceMemberFlags,
                       binder As Binder,
                       syntax As MethodBaseSyntax,
                       arity As Integer,
                       Optional handledEvents As ImmutableArray(Of HandledEvent) = Nothing)
            MyBase.New(containingType, flags, binder.GetSyntaxReference(syntax))

            ' initialized lazily if unset:
            _lazyHandles = handledEvents
            _name = name
            _arity = arity

            ' Check attributes quickly.
            _quickAttributes = binder.QuickAttributeChecker.CheckAttributes(syntax.AttributeLists)
            If Not containingType.AllowsExtensionMethods() Then
                ' Extension methods in source can only be inside modules.
                _quickAttributes = _quickAttributes And Not QuickAttributes.Extension
            End If
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Protected Overrides ReadOnly Property BoundAttributesSource As SourceMethodSymbol
            Get
                Return Me.SourcePartialDefinition
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataName As String
            Get
                If _lazyMetadataName Is Nothing Then
                    ' VB has special rules for changing the metadata name of method overloads/overrides.
                    If MethodKind = MethodKind.Ordinary Then
                        OverloadingHelper.SetMetadataNameForAllOverloads(_name, SymbolKind.Method, m_containingType)
                    Else
                        ' Constructors, conversion operators, etc. just use their regular name.
                        SetMetadataName(_name)
                    End If

                    Debug.Assert(_lazyMetadataName IsNot Nothing)
                End If

                Return _lazyMetadataName
            End Get
        End Property

        ' Set the metadata name for this symbol. Called from OverloadingHelper.SetMetadataNameForAllOverloads
        ' for each symbol of the same name in a type.
        Friend Overrides Sub SetMetadataName(metadataName As String)
            Dim old = Interlocked.CompareExchange(_lazyMetadataName, metadataName, Nothing)
            Debug.Assert(old Is Nothing OrElse old = metadataName) ';If there was a race, make sure it was consistent

            If Me.IsPartial Then
                Dim partialImpl = Me.OtherPartOfPartial
                If partialImpl IsNot Nothing Then
                    partialImpl.SetMetadataName(metadataName)
                End If
            End If
        End Sub

        Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
            Get
                Return MyBase.GenerateDebugInfoImpl AndAlso Not IsAsync
            End Get
        End Property

        Protected Overrides Function GetAttributeDeclarations() As OneOrMany(Of SyntaxList(Of AttributeListSyntax))
            If Me.SourcePartialImplementation IsNot Nothing Then
                Return OneOrMany.Create(ImmutableArray.Create(AttributeDeclarationSyntaxList, Me.SourcePartialImplementation.AttributeDeclarationSyntaxList))
            Else
                Return OneOrMany.Create(AttributeDeclarationSyntaxList)
            End If
        End Function

        Private Function GetQuickAttributes() As QuickAttributes
            Dim quickAttrs = _quickAttributes

            If Me.IsPartial Then
                Dim partialImpl = Me.OtherPartOfPartial
                If partialImpl IsNot Nothing Then
                    Return quickAttrs Or partialImpl._quickAttributes
                End If
            End If

            Return quickAttrs
        End Function

        Friend Overrides ReadOnly Property MayBeReducibleExtensionMethod As Boolean
            Get
                Return (GetQuickAttributes() And QuickAttributes.Extension) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property IsExtensionMethod As Boolean
            Get
                If MayBeReducibleExtensionMethod Then
                    Return MyBase.IsExtensionMethod
                Else
                    Return False
                End If
            End Get
        End Property

        Friend Overrides Sub AddSynthesizedAttributes(compilationState As ModuleCompilationState, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
            MyBase.AddSynthesizedAttributes(compilationState, attributes)

            If Me.IsAsync OrElse Me.IsIterator Then
                AddSynthesizedAttribute(attributes, Me.DeclaringCompilation.SynthesizeStateMachineAttribute(Me, compilationState))

                If Me.IsAsync Then
                    ' Async kick-off method calls MoveNext, which contains user code. 
                    ' This means we need to emit DebuggerStepThroughAttribute in order
                    ' to have correct stepping behavior during debugging.
                    AddSynthesizedAttribute(attributes, Me.DeclaringCompilation.SynthesizeOptionalDebuggerStepThroughAttribute())
                End If
            End If
        End Sub

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                If (GetQuickAttributes() And QuickAttributes.Obsolete) <> 0 Then
                    Return MyBase.ObsoleteAttributeData
                Else
                    Return Nothing
                End If
            End Get
        End Property

        Friend Overrides Sub GenerateDeclarationErrors(cancellationToken As CancellationToken)
            If (_lazyState And StateFlags.AllDiagnosticsReported) <> 0 Then
                Return
            End If

            MyBase.GenerateDeclarationErrors(cancellationToken)

            Dim diagnostics As DiagnosticBag = DiagnosticBag.GetInstance()

            ' Ensure explicit implementations are resolved.
            If Not Me.ExplicitInterfaceImplementations.IsEmpty Then
                ' Check any constraints against implemented methods.
                ValidateImplementedMethodConstraints(diagnostics)
            End If

            Dim methodImpl As SourceMemberMethodSymbol = If(Me.IsPartial, SourcePartialImplementation, Me)

            If methodImpl IsNot Nothing AndAlso
               (methodImpl.IsAsync OrElse methodImpl.IsIterator) AndAlso
               Not methodImpl.ContainingType.IsInterfaceType() Then

                Dim container As NamedTypeSymbol = methodImpl.ContainingType

                Do
                    Dim sourceType = TryCast(container, SourceNamedTypeSymbol)

                    If sourceType IsNot Nothing AndAlso sourceType.HasSecurityCriticalAttributes Then
                        Dim location As Location = methodImpl.NonMergedLocation

                        If location IsNot Nothing Then
                            Binder.ReportDiagnostic(diagnostics, location, ERRID.ERR_SecurityCriticalAsyncInClassOrStruct)
                        End If

                        Exit Do
                    End If

                    container = container.ContainingType
                Loop While container IsNot Nothing

                If methodImpl.IsAsync AndAlso (methodImpl.ImplementationAttributes And Reflection.MethodImplAttributes.Synchronized) <> 0 Then
                    Dim location As Location = methodImpl.NonMergedLocation

                    If location IsNot Nothing Then
                        Binder.ReportDiagnostic(diagnostics, location, ERRID.ERR_SynchronizedAsyncMethod)
                    End If
                End If
            End If

            If methodImpl IsNot Nothing AndAlso methodImpl IsNot Me Then
                ' Check for parameter default value mismatch.
                Dim result As SymbolComparisonResults = MethodSignatureComparer.DetailedCompare(Me, methodImpl,
                                                                                                SymbolComparisonResults.OptionalParameterValueMismatch Or
                                                                                                SymbolComparisonResults.ParamArrayMismatch)

                If result <> Nothing Then
                    Dim location As Location = methodImpl.NonMergedLocation

                    If location IsNot Nothing Then
                        If (result And SymbolComparisonResults.ParamArrayMismatch) <> 0 Then
                            Binder.ReportDiagnostic(diagnostics, location, ERRID.ERR_PartialMethodParamArrayMismatch2, methodImpl, Me)
                        ElseIf (result And SymbolComparisonResults.OptionalParameterValueMismatch) <> 0 Then
                            Binder.ReportDiagnostic(diagnostics, location, ERRID.ERR_PartialMethodDefaultParameterValueMismatch2, methodImpl, Me)
                        End If
                    End If
                End If
            End If

            ContainingSourceModule.AtomicSetFlagAndStoreDiagnostics(_lazyState, StateFlags.AllDiagnosticsReported, 0, diagnostics, CompilationStage.Declare)
            diagnostics.Free()
        End Sub

#Region "Type Parameters"

        Public Overrides ReadOnly Property Arity As Integer
            Get
                Return _arity
            End Get
        End Property

        Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                Dim params = _lazyTypeParameters
                If params.IsDefault Then

                    Dim diagBag = DiagnosticBag.GetInstance
                    Dim sourceModule = DirectCast(Me.ContainingModule, SourceModuleSymbol)
                    params = GetTypeParameters(sourceModule, diagBag)

                    sourceModule.AtomicStoreArrayAndDiagnostics(_lazyTypeParameters,
                                                                    params,
                                                                    diagBag,
                                                                    CompilationStage.Declare)

                    diagBag.Free()

                    params = _lazyTypeParameters
                End If

                Return params
            End Get
        End Property

        Private Function GetTypeParameters(sourceModule As SourceModuleSymbol,
                                     diagBag As DiagnosticBag) As ImmutableArray(Of TypeParameterSymbol)

            Dim paramList = GetTypeParameterListSyntax(Me.DeclarationSyntax)
            If paramList Is Nothing Then
                Return ImmutableArray(Of TypeParameterSymbol).Empty
            End If

            Dim binder As Binder = BinderBuilder.CreateBinderForType(sourceModule, Me.SyntaxTree, m_containingType)
            Dim typeParamsSyntax = paramList.Parameters
            Dim arity As Integer = typeParamsSyntax.Count
            Dim typeParameters(0 To arity - 1) As TypeParameterSymbol

            For i = 0 To arity - 1
                Dim typeParamSyntax = typeParamsSyntax(i)
                Dim ident = typeParamSyntax.Identifier
                Binder.DisallowTypeCharacter(ident, diagBag, ERRID.ERR_TypeCharOnGenericParam)
                typeParameters(i) = New SourceTypeParameterOnMethodSymbol(Me, i, ident.ValueText,
                                                                          binder.GetSyntaxReference(typeParamSyntax))

                ' method type parameters cannot have same name as containing Function (but can for a Sub)
                If Me.DeclarationSyntax.Kind = SyntaxKind.FunctionStatement AndAlso CaseInsensitiveComparison.Equals(Me.Name, ident.ValueText) Then
                    Binder.ReportDiagnostic(diagBag, typeParamSyntax, ERRID.ERR_TypeParamNameFunctionNameCollision)
                End If
            Next

            ' Add the type parameters to our binder for binding parameters and return values
            binder = New MethodTypeParametersBinder(binder, typeParameters.AsImmutableOrNull)

            Dim containingSourceType = TryCast(ContainingType, SourceNamedTypeSymbol)
            If containingSourceType IsNot Nothing Then
                containingSourceType.CheckForDuplicateTypeParameters(typeParameters.AsImmutableOrNull, diagBag)
            End If

            Return typeParameters.AsImmutableOrNull
        End Function

#End Region

#Region "Explicit Interface Implementations"

        Friend Function HasExplicitInterfaceImplementations() As Boolean
            Dim syntax = TryCast(Me.DeclarationSyntax, MethodStatementSyntax)
            Return syntax IsNot Nothing AndAlso syntax.ImplementsClause IsNot Nothing
        End Function

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of MethodSymbol)
            Get
                If _lazyImplementedMethods.IsDefault Then
                    Dim sourceModule = DirectCast(Me.ContainingModule, SourceModuleSymbol)
                    Dim diagnostics = DiagnosticBag.GetInstance()
                    Dim implementedMethods As ImmutableArray(Of MethodSymbol)

                    If Me.IsPartial Then
                        ' Partial method declaration itself cannot have Implements clause (an error should 
                        ' be reported) and just returns implemented methods from the implementation

                        ' Report diagnostic if needed
                        Debug.Assert(Me.SyntaxTree IsNot Nothing)
                        Dim syntax = TryCast(Me.DeclarationSyntax, MethodStatementSyntax)
                        If (syntax IsNot Nothing) AndAlso (syntax.ImplementsClause IsNot Nothing) Then

                            ' Don't allow implements on partial method declarations.
                            diagnostics.Add(ERRID.ERR_PartialDeclarationImplements1,
                                            syntax.Identifier.GetLocation(), syntax.Identifier.ToString())
                        End If

                        ' Store implemented interface methods from the partial method implementation.
                        Dim implementation As MethodSymbol = Me.PartialImplementationPart
                        implementedMethods = If(implementation Is Nothing,
                                                                       ImmutableArray(Of MethodSymbol).Empty,
                                                                       implementation.ExplicitInterfaceImplementations)
                    Else
                        implementedMethods = Me.GetExplicitInterfaceImplementations(sourceModule, diagnostics)
                    End If

                    sourceModule.AtomicStoreArrayAndDiagnostics(_lazyImplementedMethods, implementedMethods, diagnostics, CompilationStage.Declare)
                    diagnostics.Free()
                End If
                Return _lazyImplementedMethods
            End Get
        End Property

        Private Function GetExplicitInterfaceImplementations(sourceModule As SourceModuleSymbol, diagBag As DiagnosticBag) As ImmutableArray(Of MethodSymbol)
            Debug.Assert(Not Me.IsPartial)
            Dim syntax = TryCast(Me.DeclarationSyntax, MethodStatementSyntax)

            If (syntax IsNot Nothing) AndAlso
                (syntax.ImplementsClause IsNot Nothing) Then

                Dim binder As Binder = BinderBuilder.CreateBinderForType(sourceModule, Me.SyntaxTree, ContainingType)
                If Me.IsShared And Not ContainingType.IsModuleType Then
                    ' Implementing with shared methods is illegal.
                    ' Module case is caught inside ProcessImplementsClause and has different message.
                    Binder.ReportDiagnostic(diagBag,
                                            syntax.Modifiers.First(SyntaxKind.SharedKeyword),
                                            ERRID.ERR_SharedOnProcThatImpl,
                                            syntax.Identifier.ToString())

                    ' Don't clear the Shared flag because then you get errors from semantics about needing an object reference to the non-shared member, etc.
                Else
                    Return ProcessImplementsClause(Of MethodSymbol)(syntax.ImplementsClause, Me, DirectCast(ContainingType, SourceMemberContainerTypeSymbol), binder, diagBag)
                End If
            End If

            Return ImmutableArray(Of MethodSymbol).Empty
        End Function

        ''' <summary>
        ''' Validate method type parameter constraints against implemented methods.
        ''' </summary>
        Friend Sub ValidateImplementedMethodConstraints(diagnostics As DiagnosticBag)
            If Me.IsPartial AndAlso Me.OtherPartOfPartial IsNot Nothing Then
                Me.OtherPartOfPartial.ValidateImplementedMethodConstraints(diagnostics)
            Else
                Dim implementedMethods = ExplicitInterfaceImplementations
                If implementedMethods.IsEmpty Then
                    Return
                End If
                For Each implementedMethod In implementedMethods
                    ImplementsHelper.ValidateImplementedMethodConstraints(Me, implementedMethod, diagnostics)
                Next
            End If
        End Sub
#End Region

#Region "Partials"
        Friend Overrides ReadOnly Property HasEmptyBody As Boolean
            Get
                If Not MyBase.HasEmptyBody Then
                    Return False
                End If

                Dim impl = SourcePartialImplementation
                Return impl Is Nothing OrElse impl.HasEmptyBody
            End Get
        End Property

        Friend ReadOnly Property IsPartialDefinition As Boolean
            Get
                Return Me.IsPartial
            End Get
        End Property

        Friend ReadOnly Property IsPartialImplementation As Boolean
            Get
                Return Not IsPartialDefinition AndAlso Me.OtherPartOfPartial IsNot Nothing
            End Get
        End Property

        Public ReadOnly Property SourcePartialDefinition As SourceMemberMethodSymbol
            Get
                Return If(IsPartialDefinition, Nothing, Me.OtherPartOfPartial)
            End Get
        End Property

        Public ReadOnly Property SourcePartialImplementation As SourceMemberMethodSymbol
            Get
                Return If(IsPartialDefinition, Me.OtherPartOfPartial, Nothing)
            End Get
        End Property

        Public Overrides ReadOnly Property PartialDefinitionPart As MethodSymbol
            Get
                Return SourcePartialDefinition
            End Get
        End Property

        Public Overrides ReadOnly Property PartialImplementationPart As MethodSymbol
            Get
                Return SourcePartialImplementation
            End Get
        End Property

        Friend Property OtherPartOfPartial As SourceMemberMethodSymbol
            Get
#If DEBUG Then
                Me._partialMethodInfoIsFrozen = True
#End If
                Return Me._otherPartOfPartial
            End Get
            Private Set(value As SourceMemberMethodSymbol)
                Dim oldValue As SourceMemberMethodSymbol = Me._otherPartOfPartial
                Me._otherPartOfPartial = value

#If DEBUG Then
                ' As we want to make sure we always validate attributes on partial 
                ' method declaration, we need to make sure we don't validate
                ' attributes before PartialMethodCounterpart is assigned
                Debug.Assert(Me.m_lazyCustomAttributesBag Is Nothing)
                Debug.Assert(Me.m_lazyReturnTypeCustomAttributesBag Is Nothing)
                For Each param In Me.Parameters
                    Dim complexParam = TryCast(param, SourceComplexParameterSymbol)
                    If complexParam IsNot Nothing Then
                        complexParam.AssertAttributesNotValidatedYet()
                    End If
                Next

                ' If partial method info is frozen the new and the old values must be equal
                Debug.Assert(Not Me._partialMethodInfoIsFrozen OrElse oldValue Is value)
                Me._partialMethodInfoIsFrozen = True
#End If
            End Set
        End Property

        Friend Property SuppressDuplicateProcDefDiagnostics As Boolean
            Get
#If DEBUG Then
                Me._partialMethodInfoIsFrozen = True
#End If
                Return (_lazyState And StateFlags.SuppressDuplicateProcDefDiagnostics) <> 0
            End Get

            Set(value As Boolean)
                Dim stateChanged = ThreadSafeFlagOperations.Set(_lazyState, StateFlags.SuppressDuplicateProcDefDiagnostics)
#If DEBUG Then
                ' If partial method info is frozen the new and the old values must be equal
                Debug.Assert(Not Me._partialMethodInfoIsFrozen OrElse Not stateChanged)
                Me._partialMethodInfoIsFrozen = True
#End If
            End Set
        End Property

        ''' <summary>
        '''  This method is to be called to assign implementation to a partial method.
        '''  </summary>
        Friend Shared Sub InitializePartialMethodParts(definition As SourceMemberMethodSymbol, implementation As SourceMemberMethodSymbol)
            Debug.Assert(definition.IsPartial)
            Debug.Assert(implementation Is Nothing OrElse Not implementation.IsPartial)

            definition.OtherPartOfPartial = implementation
            If implementation IsNot Nothing Then
                implementation.OtherPartOfPartial = definition
            End If
        End Sub

        Friend Overrides Function GetBoundMethodBody(compilationState As TypeCompilationState, diagnostics As DiagnosticBag, Optional ByRef methodBodyBinder As Binder = Nothing) As BoundBlock
            If Me.IsPartial Then
                Throw ExceptionUtilities.Unreachable
            End If

            Return MyBase.GetBoundMethodBody(compilationState, diagnostics, methodBodyBinder)
        End Function

#End Region

#Region "Handles"

        Public Overrides ReadOnly Property HandledEvents As ImmutableArray(Of HandledEvent)
            Get
                If _lazyHandles.IsDefault Then
                    Dim sourceModule = DirectCast(Me.ContainingModule, SourceModuleSymbol)

                    Dim diagnostics = DiagnosticBag.GetInstance()
                    Dim boundHandledEvents = Me.GetHandles(sourceModule, diagnostics)

                    sourceModule.AtomicStoreArrayAndDiagnostics(Of HandledEvent)(_lazyHandles,
                                                                                  boundHandledEvents,
                                                                                  diagnostics,
                                                                                  CompilationStage.Declare)

                    diagnostics.Free()
                End If

                Return _lazyHandles
            End Get
        End Property

        Private Function GetHandles(sourceModule As SourceModuleSymbol, diagBag As DiagnosticBag) As ImmutableArray(Of HandledEvent)
            Dim syntax = TryCast(Me.DeclarationSyntax, MethodStatementSyntax)

            If (syntax Is Nothing) OrElse (syntax.HandlesClause Is Nothing) Then
                Return ImmutableArray(Of HandledEvent).Empty
            End If

            Dim typeBinder As Binder = BinderBuilder.CreateBinderForType(sourceModule, Me.SyntaxTree, m_containingType)
            typeBinder = New LocationSpecificBinder(BindingLocation.HandlesClause, Me, typeBinder)
            Dim handlesBuilder = ArrayBuilder(Of HandledEvent).GetInstance

            For Each singleHandleClause As HandlesClauseItemSyntax In syntax.HandlesClause.Events
                Dim handledEventResult = BindSingleHandlesClause(singleHandleClause, typeBinder, diagBag)

                If handledEventResult IsNot Nothing Then
                    handlesBuilder.Add(handledEventResult)
                End If
            Next

            Return handlesBuilder.ToImmutableAndFree
        End Function

        Friend Function BindSingleHandlesClause(singleHandleClause As HandlesClauseItemSyntax,
                                           typeBinder As Binder,
                                           diagBag As DiagnosticBag,
                                           Optional candidateEventSymbols As ArrayBuilder(Of Symbol) = Nothing,
                                           Optional candidateWithEventsSymbols As ArrayBuilder(Of Symbol) = Nothing,
                                           Optional candidateWithEventsPropertySymbols As ArrayBuilder(Of Symbol) = Nothing,
                                           Optional ByRef resultKind As LookupResultKind = Nothing) As HandledEvent

            Dim handlesKind As HandledEventKind
            Dim eventContainingType As TypeSymbol = Nothing
            Dim withEventsSourceProperty As PropertySymbol = Nothing

            ' This is the WithEvents property that looks as event container to the user. (it could be in a base class)
            Dim witheventsProperty As PropertySymbol = Nothing

            ' This is the WithEvents property that will actually used to hookup handlers. (it could be a proxy override)
            Dim witheventsPropertyInCurrentClass As PropertySymbol = Nothing

            If Me.ContainingType.IsModuleType AndAlso singleHandleClause.EventContainer.Kind <> SyntaxKind.WithEventsEventContainer Then
                Binder.ReportDiagnostic(diagBag, singleHandleClause, ERRID.ERR_HandlesSyntaxInModule)
                Return Nothing
            End If

            Dim eventContainerKind = singleHandleClause.EventContainer.Kind
            Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing

            If eventContainerKind = SyntaxKind.KeywordEventContainer Then
                Select Case DirectCast(singleHandleClause.EventContainer, KeywordEventContainerSyntax).Keyword.Kind
                    Case SyntaxKind.MeKeyword
                        handlesKind = HandledEventKind.Me
                        eventContainingType = Me.ContainingType

                    Case SyntaxKind.MyClassKeyword
                        handlesKind = HandledEventKind.MyClass
                        eventContainingType = Me.ContainingType

                    Case SyntaxKind.MyBaseKeyword
                        handlesKind = HandledEventKind.MyBase
                        eventContainingType = Me.ContainingType.BaseTypeNoUseSiteDiagnostics
                End Select

            ElseIf eventContainerKind = SyntaxKind.WithEventsEventContainer OrElse
                eventContainerKind = SyntaxKind.WithEventsPropertyEventContainer Then

                handlesKind = HandledEventKind.WithEvents
                Dim witheventsContainer = If(eventContainerKind = SyntaxKind.WithEventsPropertyEventContainer,
                                        DirectCast(singleHandleClause.EventContainer, WithEventsPropertyEventContainerSyntax).WithEventsContainer,
                                        DirectCast(singleHandleClause.EventContainer, WithEventsEventContainerSyntax))

                Dim witheventsName = witheventsContainer.Identifier.ValueText
                witheventsProperty = FindWithEventsProperty(m_containingType,
                                                            typeBinder,
                                                            witheventsName,
                                                            useSiteDiagnostics,
                                                            candidateWithEventsSymbols,
                                                            resultKind)

                diagBag.Add(singleHandleClause.EventContainer, useSiteDiagnostics)
                useSiteDiagnostics = Nothing

                If witheventsProperty Is Nothing Then
                    Binder.ReportDiagnostic(diagBag, singleHandleClause.EventContainer, ERRID.ERR_NoWithEventsVarOnHandlesList)
                    Return Nothing
                End If

                If witheventsProperty.IsShared AndAlso Not Me.IsShared Then
                    'Events of shared WithEvents variables cannot be handled by non-shared methods.
                    Binder.ReportDiagnostic(diagBag, singleHandleClause.EventContainer, ERRID.ERR_SharedEventNeedsSharedHandler)
                End If

                If eventContainerKind = SyntaxKind.WithEventsPropertyEventContainer Then
                    Dim propName = DirectCast(singleHandleClause.EventContainer, WithEventsPropertyEventContainerSyntax).Property.Identifier.ValueText
                    withEventsSourceProperty = FindProperty(witheventsProperty.Type,
                                                          typeBinder,
                                                          propName,
                                                          useSiteDiagnostics,
                                                          candidateWithEventsPropertySymbols,
                                                          resultKind)

                    If withEventsSourceProperty Is Nothing Then
                        Binder.ReportDiagnostic(diagBag, singleHandleClause.EventContainer, ERRID.ERR_HandlesSyntaxInClass)
                        Return Nothing
                    End If

                    eventContainingType = withEventsSourceProperty.Type
                Else
                    eventContainingType = witheventsProperty.Type
                End If

                ' if was found in one of bases, need to override it
                If Not TypeSymbol.Equals(witheventsProperty.ContainingType, Me.ContainingType, TypeCompareKind.ConsiderEverything) Then
                    witheventsPropertyInCurrentClass = DirectCast(Me.ContainingType, SourceNamedTypeSymbol).GetOrAddWithEventsOverride(witheventsProperty)
                Else
                    witheventsPropertyInCurrentClass = witheventsProperty
                End If

                typeBinder.ReportDiagnosticsIfObsoleteOrNotSupportedByRuntime(diagBag, witheventsPropertyInCurrentClass, singleHandleClause.EventContainer)
            Else
                Binder.ReportDiagnostic(diagBag, singleHandleClause.EventContainer, ERRID.ERR_HandlesSyntaxInClass)
                Return Nothing
            End If

            Dim eventName As String = singleHandleClause.EventMember.Identifier.ValueText
            Dim eventSymbol As EventSymbol = Nothing

            If eventContainingType IsNot Nothing Then
                Binder.ReportUseSiteError(diagBag, singleHandleClause.EventMember, eventContainingType)

                ' Bind event symbol
                eventSymbol = FindEvent(eventContainingType,
                                        typeBinder,
                                        eventName,
                                        handlesKind = HandledEventKind.MyBase,
                                        useSiteDiagnostics,
                                        candidateEventSymbols,
                                        resultKind)
            End If

            diagBag.Add(singleHandleClause.EventMember, useSiteDiagnostics)

            If eventSymbol Is Nothing Then
                'Event '{0}' cannot be found.
                Binder.ReportDiagnostic(diagBag, singleHandleClause.EventMember, ERRID.ERR_EventNotFound1, eventName)
                Return Nothing
            End If

            typeBinder.ReportDiagnosticsIfObsoleteOrNotSupportedByRuntime(diagBag, eventSymbol, singleHandleClause.EventMember)

            Binder.ReportUseSiteError(diagBag, singleHandleClause.EventMember, eventSymbol)

            If eventSymbol.AddMethod IsNot Nothing Then
                Binder.ReportUseSiteError(diagBag, singleHandleClause.EventMember, eventSymbol.AddMethod)
            End If

            If eventSymbol.RemoveMethod IsNot Nothing Then
                Binder.ReportUseSiteError(diagBag, singleHandleClause.EventMember, eventSymbol.RemoveMethod)
            End If

            ' For WinRT events, we require that certain well-known members be present (needed in synthesize code).
            If eventSymbol.IsWindowsRuntimeEvent Then
                typeBinder.GetWellKnownTypeMember(
                    WellKnownMember.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T__AddEventHandler,
                    singleHandleClause.EventMember,
                    diagBag)

                typeBinder.GetWellKnownTypeMember(
                    WellKnownMember.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T__RemoveEventHandler,
                    singleHandleClause.EventMember,
                    diagBag)
            End If

            Select Case ContainingType.TypeKind
                Case TypeKind.Interface, TypeKind.Structure, TypeKind.Enum, TypeKind.Delegate
                    ' Handles clause is invalid in this context. 
                    Return Nothing

                Case TypeKind.Class, TypeKind.Module
                    ' Valid context

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(ContainingType.TypeKind)
            End Select

            Dim receiverOpt As BoundExpression = Nothing

            ' synthesize delegate creation (may involve relaxation)

            Dim hookupMethod As MethodSymbol = Nothing

            If handlesKind = HandledEventKind.WithEvents Then
                hookupMethod = witheventsPropertyInCurrentClass.SetMethod
            Else
                If eventSymbol.IsShared AndAlso Me.IsShared Then
                    ' if both method and event are shared, host method is shared ctor
                    hookupMethod = Me.ContainingType.SharedConstructors(0) ' There will only be one in a correct program.
                Else
                    ' if either method, or event are not shared, host method is instance ctor
                    Dim instanceCtors = Me.ContainingType.InstanceConstructors
                    Debug.Assert(Not instanceCtors.IsEmpty, "bind non-type members should have ensured at least one ctor for us")

                    ' any instance ctor will do for our purposes here. 
                    ' We will only use "Me" and that does not need to be from a particular ctor.
                    hookupMethod = instanceCtors(0)
                End If
            End If

            Debug.Assert(hookupMethod IsNot Nothing, "bind non-type members should have ensured appropriate host method for handles injection")
            ' No use site errors, since method is from source (or synthesized)

            If Not hookupMethod.IsShared Then
                receiverOpt = New BoundMeReference(singleHandleClause, Me.ContainingType).MakeCompilerGenerated
            End If

            Dim handlingMethod As MethodSymbol = Me
            If Me.PartialDefinitionPart IsNot Nothing Then
                ' it is ok for a partial method to have Handles, but if there is a definition part,
                ' it is the definition method that will do the handling
                handlingMethod = Me.PartialDefinitionPart
            End If

            Dim qualificationKind As QualificationKind
            If hookupMethod.IsShared Then
                qualificationKind = QualificationKind.QualifiedViaTypeName
            Else
                qualificationKind = QualificationKind.QualifiedViaValue
            End If

            Dim syntheticMethodGroup = New BoundMethodGroup(
                                       singleHandleClause,
                                       Nothing,
                                       ImmutableArray.Create(Of MethodSymbol)(handlingMethod),
                                       LookupResultKind.Good,
                                       receiverOpt,
                                       qualificationKind:=qualificationKind)


            ' AddressOf currentMethod
            Dim syntheticAddressOf = New BoundAddressOfOperator(singleHandleClause, typeBinder, syntheticMethodGroup).MakeCompilerGenerated

            ' 9.2.6  Event handling
            ' ... A handler method M is considered a valid event handler for an event E 
            '   if the statement " AddHandler E, AddressOf M " would also be valid. 
            '   Unlike an AddHandler statement, however, explicit event handlers allow 
            '   handling an event with a method with no arguments regardless of
            '   whether strict semantics are being used or not.
            Dim resolutionResult = Binder.InterpretDelegateBinding(syntheticAddressOf, eventSymbol.Type, isForHandles:=True)

            If Not Conversions.ConversionExists(resolutionResult.DelegateConversions) Then
                ' TODO: Consider skip reporting this diagnostic if "ERR_SharedEventNeedsSharedHandler" was already reported above.

                'Method '{0}' cannot handle event '{1}' because they do not have a compatible signature.
                Binder.ReportDiagnostic(diagBag, singleHandleClause.EventMember, ERRID.ERR_EventHandlerSignatureIncompatible2, Me.Name, eventName)
                Return Nothing

            End If

            Dim delegateCreation = typeBinder.ReclassifyAddressOf(syntheticAddressOf, resolutionResult, eventSymbol.Type, diagBag, isForHandles:=True,
                                                                  warnIfResultOfAsyncMethodIsDroppedDueToRelaxation:=True)

            Dim handledEventResult = New HandledEvent(handlesKind,
                                                     eventSymbol,
                                                     witheventsProperty,
                                                     withEventsSourceProperty,
                                                     delegateCreation,
                                                     hookupMethod)

            Return handledEventResult
        End Function

        Friend Shared Function FindWithEventsProperty(containingType As TypeSymbol,
                                                      binder As Binder,
                                                      name As String,
                                                      <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo),
                                                      Optional candidateEventSymbols As ArrayBuilder(Of Symbol) = Nothing,
                                                      Optional ByRef resultKind As LookupResultKind = Nothing) As PropertySymbol

            Dim witheventsLookup = LookupResult.GetInstance

            ' WithEvents properties are always accessed via Me/MyBase
            Dim options = LookupOptions.IgnoreExtensionMethods Or LookupOptions.UseBaseReferenceAccessibility
            binder.LookupMember(witheventsLookup, containingType, name, 0, options, useSiteDiagnostics)

            If candidateEventSymbols IsNot Nothing Then
                candidateEventSymbols.AddRange(witheventsLookup.Symbols)
                resultKind = witheventsLookup.Kind
            End If

            Dim result As PropertySymbol = Nothing
            If witheventsLookup.IsGood Then
                If witheventsLookup.HasSingleSymbol Then
                    Dim prop = TryCast(witheventsLookup.SingleSymbol, PropertySymbol)

                    If prop IsNot Nothing AndAlso prop.IsWithEvents Then
                        result = prop
                    Else
                        resultKind = LookupResultKind.NotAWithEventsMember
                    End If

                Else
                    resultKind = LookupResultKind.Ambiguous
                End If
            End If

            witheventsLookup.Free()
            Return result
        End Function

        Friend Shared Function FindEvent(containingType As TypeSymbol,
                                         binder As Binder,
                                         name As String,
                                         isThroughMyBase As Boolean,
                                         <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo),
                                         Optional candidateEventSymbols As ArrayBuilder(Of Symbol) = Nothing,
                                         Optional ByRef resultKind As LookupResultKind = Nothing) As EventSymbol

            Dim options = LookupOptions.IgnoreExtensionMethods Or LookupOptions.EventsOnly
            If isThroughMyBase Then
                options = options Or LookupOptions.UseBaseReferenceAccessibility
            End If

            Dim eventLookup = LookupResult.GetInstance
            binder.LookupMember(eventLookup, containingType, name, 0, options, useSiteDiagnostics)

            If candidateEventSymbols IsNot Nothing Then
                candidateEventSymbols.AddRange(eventLookup.Symbols)
                resultKind = eventLookup.Kind
            End If

            Dim result As EventSymbol = Nothing
            If eventLookup.IsGood Then
                If eventLookup.HasSingleSymbol Then
                    result = TryCast(eventLookup.SingleSymbol, EventSymbol)
                    If result Is Nothing Then
                        resultKind = LookupResultKind.NotAnEvent
                    End If
                Else
                    ' finding more than one item is ambiguous
                    resultKind = LookupResultKind.Ambiguous
                End If
            End If

            eventLookup.Free()
            Return result
        End Function

        Private Shared Function FindProperty(containingType As TypeSymbol,
                                 binder As Binder,
                                 name As String,
                                 <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo),
                                 Optional candidatePropertySymbols As ArrayBuilder(Of Symbol) = Nothing,
                                 Optional ByRef resultKind As LookupResultKind = Nothing) As PropertySymbol

            ' // Note:
            ' // - we don't support finding the Property if it is overloaded
            ' // across classes.
            ' // - Also note that we don't support digging into bases. This was
            ' // how the previous impl. was.
            ' // Given that this is not a user feature, we don't bother doing
            ' // this extra work.
            Dim options = CType(LookupOptions.IgnoreExtensionMethods Or LookupOptions.NoBaseClassLookup, LookupOptions)

            Dim propertyLookup = LookupResult.GetInstance
            binder.LookupMember(propertyLookup, containingType, name, 0, options, useSiteDiagnostics)

            If candidatePropertySymbols IsNot Nothing Then
                candidatePropertySymbols.AddRange(propertyLookup.Symbols)
                resultKind = propertyLookup.Kind
            End If

            Dim result As PropertySymbol = Nothing
            If propertyLookup.IsGood Then

                Dim symbols = propertyLookup.Symbols

                For Each symbol In symbols
                    If symbol.Kind = SymbolKind.Property Then


                        'Function ValidEventSourceProperty( _
                        '    ByVal [Property] As BCSym.[Property] _
                        ') As Boolean
                        '        ' // We only care about properties returning classes or interfaces
                        '        ' // because only classes and interfaces can be specified in a
                        '        ' // handles clause.
                        '        ' //
                        '        Return _
                        '            [Property].ReturnsEventSource() AndAlso _
                        '            [Property].GetParameterCount() = 0 AndAlso _
                        '            [Property].GetProperty() IsNot Nothing AndAlso _
                        '            IsClassOrInterface([Property]._GetType())
                        '    End Function

                        Dim prop = DirectCast(symbol, PropertySymbol)
                        If prop.Parameters.Any Then
                            Continue For
                        End If

                        ' here we have parameterless property, it must be ours, As long as it is readable.
                        If prop.GetMethod IsNot Nothing AndAlso
                            prop.GetMethod.ReturnType.IsClassOrInterfaceType AndAlso
                            ReturnsEventSource(prop, binder.Compilation) Then

                            ' finding more than one item would seem ambiguous
                            ' not sure if this can happen though, 
                            ' regardless, native compiler does not consider it an error
                            ' it just uses the first found item.

                            result = prop
                            Exit For
                        End If
                    End If
                Next

                If result Is Nothing Then
                    resultKind = LookupResultKind.Empty
                End If
            End If

            propertyLookup.Free()
            Return result
        End Function

        Private Shared Function ReturnsEventSource(prop As PropertySymbol, compilation As VisualBasicCompilation) As Boolean
            Dim attrs = prop.GetAttributes()
            For Each attr In attrs
                If attr.AttributeClass Is compilation.GetWellKnownType(WellKnownType.System_ComponentModel_DesignerSerializationVisibilityAttribute) Then
                    Dim args = attr.CommonConstructorArguments
                    If args.Length = 1 Then
                        Dim arg = args(0)
                        Const DESIGNERSERIALIZATIONVISIBILITYTYPE_CONTENT As Integer = 2
                        If arg.Kind <> TypedConstantKind.Array AndAlso CInt(arg.Value) = DESIGNERSERIALIZATIONVISIBILITYTYPE_CONTENT Then
                            Return True
                        End If
                    End If
                End If
            Next
            Return False
        End Function

#End Region

    End Class
End Namespace
