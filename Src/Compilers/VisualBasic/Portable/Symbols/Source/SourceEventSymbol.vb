' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend Class SourceEventSymbol
        Inherits EventSymbol
        Implements IAttributeTargetSymbol

        Private ReadOnly m_containingType As SourceMemberContainerTypeSymbol
        Private ReadOnly m_name As String

        Private ReadOnly m_syntaxRef As SyntaxReference
        Private ReadOnly m_location As Location

        Private ReadOnly m_memberFlags As SourceMemberFlags

        Private ReadOnly m_addMethod As MethodSymbol
        Private ReadOnly m_removeMethod As MethodSymbol
        Private ReadOnly m_raiseMethod As MethodSymbol

        Private ReadOnly m_backingField As FieldSymbol

        ' Misc flags defining the state of this symbol (StateFlags)
        Private m_lazyState As Integer

        <Flags>
        Private Enum StateFlags As Integer
            IsTypeInferred = &H1                                ' Bit value valid once m_lazyType is assigned.
            IsDelegateFromImplements = &H2                      ' Bit value valid once m_lazyType is assigned.
            ReportedExplicitImplementationDiagnostics = &H4
        End Enum

        Private m_lazyType As TypeSymbol
        Private m_lazyImplementedEvents As ImmutableArray(Of EventSymbol)
        Private m_lazyDelegateParameters As ImmutableArray(Of ParameterSymbol)
        Private m_lazyDocComment As String

        ' Attributes on event. Set once after construction. IsNull means not set. 
        Private m_lazyCustomAttributesBag As CustomAttributesBag(Of VisualBasicAttributeData)

        ''' <summary>
        ''' Indicates whether event created a new delegate type.
        ''' In such case the Type must be added to the members of the containing type
        ''' </summary>
        Friend ReadOnly Property IsTypeInferred As Boolean
            Get
                Dim unused = Type ' Ensure lazy state is computed.
                Return (m_lazyState And StateFlags.IsTypeInferred) <> 0
            End Get
        End Property

        Friend Sub New(containingType As SourceMemberContainerTypeSymbol,
                       binder As Binder,
                       syntax As EventStatementSyntax,
                       blockSyntaxOpt As EventBlockSyntax,
                       diagnostics As DiagnosticBag)

            Debug.Assert(containingType IsNot Nothing)
            Debug.Assert(syntax IsNot Nothing)

            m_containingType = containingType

            ' Decode the flags.
            ' This will validate modifiers agains container (i.e. Protected is invalid in a structure...)
            Dim modifiers = DecodeModifiers(syntax.Modifiers,
                                            containingType,
                                            binder,
                                            diagnostics)

            m_memberFlags = modifiers.AllFlags

            Dim identifier = syntax.Identifier
            m_name = identifier.ValueText

            ' Events cannot have type characters
            If identifier.GetTypeCharacter() <> TypeCharacter.None Then
                binder.ReportDiagnostic(diagnostics, identifier, ERRID.ERR_TypecharNotallowed)
            End If

            Dim location = identifier.GetLocation()
            m_location = location
            m_syntaxRef = binder.GetSyntaxReference(syntax)

            binder = New LocationSpecificBinder(BindingLocation.EventSignature, Me, binder)

            If blockSyntaxOpt IsNot Nothing Then
                For Each accessorSyntax In blockSyntaxOpt.Accessors
                    Dim accessor As CustomEventAccessorSymbol = BindEventAccessor(accessorSyntax, binder)
                    Select Case (accessor.MethodKind)
                        Case MethodKind.EventAdd
                            If m_addMethod Is Nothing Then
                                m_addMethod = accessor
                            Else
                                diagnostics.Add(ERRID.ERR_DuplicateAddHandlerDef, accessor.Locations(0))
                            End If

                        Case MethodKind.EventRemove
                            If m_removeMethod Is Nothing Then
                                m_removeMethod = accessor
                            Else
                                diagnostics.Add(ERRID.ERR_DuplicateRemoveHandlerDef, accessor.Locations(0))
                            End If

                        Case MethodKind.EventRaise
                            If m_raiseMethod Is Nothing Then
                                m_raiseMethod = accessor
                            Else
                                diagnostics.Add(ERRID.ERR_DuplicateRaiseEventDef, accessor.Locations(0))
                            End If

                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(accessor.MethodKind)
                    End Select
                Next

                If m_addMethod Is Nothing Then
                    diagnostics.Add(ERRID.ERR_MissingAddHandlerDef1, location, Me)
                End If

                If m_removeMethod Is Nothing Then
                    diagnostics.Add(ERRID.ERR_MissingRemoveHandlerDef1, location, Me)
                End If

                If m_raiseMethod Is Nothing Then
                    diagnostics.Add(ERRID.ERR_MissingRaiseEventDef1, location, Me)
                End If
            Else
                ' Synthesize accessors
                m_addMethod = New SynthesizedAddAccessorSymbol(containingType, Me)
                m_removeMethod = New SynthesizedRemoveAccessorSymbol(containingType, Me)

                ' if this is a concrete class, add a backing field too
                If Not containingType.IsInterfaceType Then
                    m_backingField = New SynthesizedEventBackingFieldSymbol(Me, Me.Name & EVENT_VARIABLE_SUFFIX, Me.IsShared)
                End If
            End If
        End Sub

        Private Function ComputeType(diagnostics As DiagnosticBag, <Out()> ByRef isTypeInferred As Boolean, <Out()> ByRef isDelegateFromImplements As Boolean) As TypeSymbol
            Dim binder = CreateBinderForTypeDeclaration()
            Dim syntax = DirectCast(m_syntaxRef.GetSyntax(), EventStatementSyntax)

            isTypeInferred = False
            isDelegateFromImplements = False

            Dim type As TypeSymbol

            ' TODO: why AsClause is not a SimpleAsClause in events? There can't be "As New"

            ' WinRT events require either an as-clause or an implements-clause.
            Dim requiresDelegateType As Boolean = syntax.ImplementsClause Is Nothing AndAlso Me.IsWindowsRuntimeEvent

            ' if there is an As clause we use its type as event's type
            If syntax.AsClause IsNot Nothing Then
                type = binder.DecodeIdentifierType(syntax.Identifier, syntax.AsClause, Nothing, diagnostics)
                If Not syntax.AsClause.AsKeyword.IsMissing Then
                    If Not type.IsDelegateType Then
                        binder.ReportDiagnostic(diagnostics, syntax.AsClause.Type, ERRID.ERR_EventTypeNotDelegate)
                    Else
                        Dim invoke = DirectCast(type, NamedTypeSymbol).DelegateInvokeMethod
                        If invoke Is Nothing Then
                            binder.ReportDiagnostic(diagnostics, syntax.AsClause.Type, ERRID.ERR_UnsupportedType1, type.Name)
                        Else
                            If Not invoke.IsSub Then
                                binder.ReportDiagnostic(diagnostics, syntax.AsClause.Type, ERRID.ERR_EventDelegatesCantBeFunctions)
                            End If
                        End If
                    End If
                ElseIf requiresDelegateType Then
                    ' This will always be a cascading diagnostic but, arguably, it does provide additional context
                    ' and dev11 reports it.
                    binder.ReportDiagnostic(diagnostics, syntax.Identifier, ERRID.ERR_WinRTEventWithoutDelegate)
                End If

            Else
                If requiresDelegateType Then
                    binder.ReportDiagnostic(diagnostics, syntax.Identifier, ERRID.ERR_WinRTEventWithoutDelegate)
                End If

                Dim implementedEvents = ExplicitInterfaceImplementations

                ' when we have a match with interface event we should take the type from
                ' the implemented event.

                If Not implementedEvents.IsEmpty Then
                    ' use the type of the first implemented event
                    Dim implementedEventType = implementedEvents(0).Type

                    ' all other implemented events must be of the same type as the first
                    For i As Integer = 1 To implementedEvents.Length - 1
                        Dim implemented = implementedEvents(i)
                        If Not implemented.Type = implementedEventType Then
                            Dim errLocation = GetImplementingLocation(implemented)
                            binder.ReportDiagnostic(diagnostics,
                                                    errLocation,
                                                    ERRID.ERR_MultipleEventImplMismatch3,
                                                    Me,
                                                    implemented,
                                                    implemented.ContainingType)
                        End If
                    Next

                    type = implementedEventType
                    isDelegateFromImplements = True

                Else
                    ' get event's type from the containing type
                    Dim types = m_containingType.GetTypeMembers(Me.Name & EVENT_DELEGATE_SUFFIX)
                    Debug.Assert(Not types.IsDefault)

                    If Not types.IsEmpty Then
                        type = types(0)
                    Else
                        ' if we still do not know the type, get a temporary one (it is not a member of the containing class)
                        type = New SynthesizedEventDelegateSymbol(Me.m_syntaxRef, m_containingType)
                    End If

                    isTypeInferred = True
                End If
            End If

            If Not type.IsErrorType() Then
                AccessCheck.VerifyAccessExposureForMemberType(Me, syntax.Identifier, type, diagnostics, isDelegateFromImplements)
            End If

            Return type
        End Function

        Private Function ComputeImplementedEvents(diagnostics As DiagnosticBag) As ImmutableArray(Of EventSymbol)
            Dim syntax = DirectCast(m_syntaxRef.GetSyntax(), EventStatementSyntax)
            Dim implementsClause = syntax.ImplementsClause

            If implementsClause IsNot Nothing Then
                Dim binder = CreateBinderForTypeDeclaration()

                If m_containingType.IsInterfaceType Then
                    Dim implementsKeyword = implementsClause.ImplementsKeyword
                    ' // Interface events can't claim to implement anything
                    binder.ReportDiagnostic(diagnostics, implementsKeyword, ERRID.ERR_InterfaceEventCantUse1, implementsKeyword.ValueText)

                ElseIf IsShared AndAlso Not m_containingType.IsModuleType Then
                    ' // Implementing with shared events is illegal.
                    binder.ReportDiagnostic(diagnostics, syntax.Modifiers.First(SyntaxKind.SharedKeyword), ERRID.ERR_SharedOnProcThatImpl)

                Else
                    ' if event is inferred, only signature needs to match
                    ' otherwise event types must match exactly
                    Return ProcessImplementsClause(Of EventSymbol)(implementsClause,
                                                                   Me,
                                                                   m_containingType,
                                                                   binder,
                                                                   diagnostics)
                End If
            End If

            Return ImmutableArray(Of EventSymbol).Empty
        End Function

        ''' <summary>
        ''' Unless the type is inferred, check that all
        ''' implemented events have the same type.
        ''' </summary>
        Private Sub CheckExplicitImplementationTypes()
            If (m_lazyState And (StateFlags.IsTypeInferred Or StateFlags.IsDelegateFromImplements Or StateFlags.ReportedExplicitImplementationDiagnostics)) <> 0 Then
                Return
            End If

            Dim diagnostics As DiagnosticBag = Nothing
            Dim type = Me.Type
            For Each implemented In ExplicitInterfaceImplementations
                If Not implemented.Type = type Then
                    If diagnostics Is Nothing Then
                        diagnostics = DiagnosticBag.GetInstance()
                    End If

                    Dim errLocation = GetImplementingLocation(implemented)
                    diagnostics.Add(ERRID.ERR_EventImplMismatch5, errLocation, {Me, implemented, implemented.ContainingType, type, implemented.Type})
                End If
            Next

            If diagnostics IsNot Nothing Then
                ContainingSourceModule.AtomicSetFlagAndStoreDiagnostics(m_lazyState, StateFlags.ReportedExplicitImplementationDiagnostics, 0, diagnostics, CompilationStage.Declare)
                diagnostics.Free()
            End If
        End Sub

        Friend Overrides ReadOnly Property DelegateParameters As ImmutableArray(Of ParameterSymbol)
            Get
                If m_lazyDelegateParameters.IsDefault Then
                    Dim syntax = DirectCast(m_syntaxRef.GetSyntax(), EventStatementSyntax)
                    If syntax.AsClause IsNot Nothing Then
                        ' We can access use the base implementation which relies
                        ' on the Type property since the type in "Event E As D" is explicit.
                        m_lazyDelegateParameters = MyBase.DelegateParameters
                    Else
                        ' Avoid using the base implementation since that relies
                        ' on Type which is inferred, potentially from interface
                        ' implementations which relies on DelegateParameters.
                        Dim binder = CreateBinderForTypeDeclaration()
                        Dim diagnostics = DiagnosticBag.GetInstance()

                        ContainingSourceModule.AtomicStoreArrayAndDiagnostics(
                            m_lazyDelegateParameters,
                            binder.DecodeParameterListOfDelegateDeclaration(Me, syntax.ParameterList, diagnostics),
                            diagnostics,
                            CompilationStage.Declare)

                        diagnostics.Free()
                    End If
                End If

                Return m_lazyDelegateParameters
            End Get
        End Property

        Private Function BindEventAccessor(blockSyntax As AccessorBlockSyntax,
                                           binder As Binder) As CustomEventAccessorSymbol

            Dim syntax = blockSyntax.Begin
            Debug.Assert(syntax.Modifiers.IsEmpty, "event accessors cannot have modifiers")

            ' Include modifiers from the containing event.
            Dim flags = Me.m_memberFlags

            If Me.IsImplementing Then
                flags = flags Or SourceMemberFlags.Overrides Or SourceMemberFlags.NotOverridable
            End If

            ' All event accessors are subs.
            Select Case blockSyntax.Kind
                Case SyntaxKind.AddHandlerAccessorBlock
                    flags = flags Or SourceMemberFlags.MethodKindEventAdd Or SourceMemberFlags.MethodIsSub

                Case SyntaxKind.RemoveHandlerAccessorBlock
                    flags = flags Or SourceMemberFlags.MethodKindEventRemove Or SourceMemberFlags.MethodIsSub

                Case SyntaxKind.RaiseEventAccessorBlock
                    flags = flags Or SourceMemberFlags.MethodKindEventRaise Or SourceMemberFlags.MethodIsSub

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(blockSyntax.Kind)
            End Select

            Dim location = syntax.GetLocation()
            ' Event symbols aren't affected if the output kind is winmd, mark false
            Dim method As New CustomEventAccessorSymbol(
                Me.m_containingType,
                Me,
                binder.GetAccessorName(Me.Name, flags.ToMethodKind(), isWinMd:=False),
                flags,
                binder.GetSyntaxReference(syntax),
                location)

            ' TODO: Handle custom modifiers, including modifiers.

            Return method
        End Function

        Private Function IsImplementing() As Boolean
            Return Not ExplicitInterfaceImplementations.IsEmpty
        End Function

        ''' <summary>
        ''' Helper method for accessors to get the overridden accessor methods. Should only be called by the
        ''' accessor method symbols.
        ''' </summary>
        Friend Function GetAccessorImplementations(kind As MethodKind) As ImmutableArray(Of MethodSymbol)
            Dim implementedEvents = ExplicitInterfaceImplementations
            Debug.Assert(Not implementedEvents.IsDefault)

            If implementedEvents.IsEmpty Then
                Return ImmutableArray(Of MethodSymbol).Empty
            Else
                Dim builder As ArrayBuilder(Of MethodSymbol) = ArrayBuilder(Of MethodSymbol).GetInstance()

                For Each implementedEvent In implementedEvents

                    Dim accessor As MethodSymbol

                    Select Case kind
                        Case MethodKind.EventAdd
                            accessor = implementedEvent.AddMethod
                        Case MethodKind.EventRemove
                            accessor = implementedEvent.RemoveMethod
                        Case MethodKind.EventRaise
                            accessor = implementedEvent.RaiseMethod
                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(kind)
                    End Select

                    If accessor IsNot Nothing Then
                        builder.Add(accessor)
                    End If
                Next

                Return builder.ToImmutableAndFree()
            End If
        End Function

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return m_containingType
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingType As NamedTypeSymbol
            Get
                Return m_containingType
            End Get
        End Property

        Public ReadOnly Property ContainingSourceModule As SourceModuleSymbol
            Get
                Return m_containingType.ContainingSourceModule
            End Get
        End Property

        Friend Overrides Function GetLexicalSortKey() As LexicalSortKey
            ' WARNING: this should not allocate memory!
            Return New LexicalSortKey(m_location, Me.DeclaringCompilation)
        End Function

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray.Create(m_location)
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return GetDeclaringSyntaxReferenceHelper(m_syntaxRef)
            End Get
        End Property

        Friend NotOverridable Overrides Function IsDefinedInSourceTree(tree As SyntaxTree, definedWithinSpan As TextSpan?, Optional cancellationToken As CancellationToken = Nothing) As Boolean
            Dim eventBlock = Me.m_syntaxRef.GetSyntax(cancellationToken).Parent
            Return IsDefinedInSourceTree(eventBlock, tree, definedWithinSpan, cancellationToken)
        End Function

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                If m_lazyType Is Nothing Then
                    Dim diagnostics = DiagnosticBag.GetInstance()
                    Dim isTypeInferred = False
                    Dim isDelegateFromImplements = False
                    Dim eventType = ComputeType(diagnostics, isTypeInferred, isDelegateFromImplements)

                    Dim newState = If(isTypeInferred, StateFlags.IsTypeInferred, 0) Or
                                   If(isDelegateFromImplements, StateFlags.IsDelegateFromImplements, 0)

                    ThreadSafeFlagOperations.Set(m_lazyState, newState)

                    ContainingSourceModule.AtomicStoreReferenceAndDiagnostics(m_lazyType, eventType, diagnostics, CompilationStage.Declare)
                    diagnostics.Free()
                End If

                Return m_lazyType
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return m_name
            End Get
        End Property

        Public Overrides ReadOnly Property AddMethod As MethodSymbol
            Get
                Return m_addMethod
            End Get
        End Property

        Public Overrides ReadOnly Property RemoveMethod As MethodSymbol
            Get
                Return m_removeMethod
            End Get
        End Property

        Public Overrides ReadOnly Property RaiseMethod As MethodSymbol
            Get
                Return m_raiseMethod
            End Get
        End Property

        Friend Overrides ReadOnly Property AssociatedField As FieldSymbol
            Get
                Return m_backingField
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of EventSymbol)
            Get
                If m_lazyImplementedEvents.IsDefault Then
                    Dim diagnostics = DiagnosticBag.GetInstance()
                    If ContainingSourceModule.AtomicStoreArrayAndDiagnostics(m_lazyImplementedEvents,
                                                                          ComputeImplementedEvents(diagnostics),
                                                                          diagnostics,
                                                                          CompilationStage.Declare) Then
                        DeclaringCompilation.SymbolDeclaredEvent(Me)
                    End If
                    diagnostics.Free()
                End If

                Return m_lazyImplementedEvents
            End Get
        End Property

        Friend ReadOnly Property SyntaxReference As SyntaxReference
            Get
                Return Me.m_syntaxRef
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return (m_memberFlags And SourceMemberFlags.Shared) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                ' event can be MustOverride if it is defined in an interface
                Return (m_memberFlags And SourceMemberFlags.MustOverride) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Debug.Assert((m_memberFlags And SourceMemberFlags.Overridable) = 0)
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Debug.Assert((m_memberFlags And SourceMemberFlags.Overrides) = 0)
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Debug.Assert((m_memberFlags And SourceMemberFlags.NotOverridable) = 0)
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return CType((m_memberFlags And SourceMemberFlags.AccessibilityMask), Accessibility)
            End Get
        End Property

        Friend Overrides ReadOnly Property ShadowsExplicitly As Boolean
            Get
                Return (m_memberFlags And SourceMemberFlags.Shadows) <> 0
            End Get
        End Property

        Friend ReadOnly Property AttributeDeclarationSyntaxList As SyntaxList(Of AttributeListSyntax)
            Get
                Return DirectCast(m_syntaxRef.GetSyntax, EventStatementSyntax).AttributeLists
            End Get
        End Property

        ReadOnly Property DefaultAttributeLocation As AttributeLocation Implements IAttributeTargetSymbol.DefaultAttributeLocation
            Get
                Return AttributeLocation.Event
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                ' If there are no attributes then this symbol is not Obsolete.
                If (Not Me.m_containingType.AnyMemberHasAttributes) Then
                    Return Nothing
                End If

                Dim lazyCustomAttributesBag = Me.m_lazyCustomAttributesBag
                If (lazyCustomAttributesBag IsNot Nothing AndAlso lazyCustomAttributesBag.IsEarlyDecodedWellKnownAttributeDataComputed) Then
                    Dim data = DirectCast(m_lazyCustomAttributesBag.EarlyDecodedWellKnownAttributeData, CommonEventEarlyWellKnownAttributeData)
                    Return If(data IsNot Nothing, data.ObsoleteAttributeData, Nothing)
                End If

                Return ObsoleteAttributeData.Uninitialized
            End Get
        End Property

        ''' <summary>
        ''' Gets the attributes applied on this symbol.
        ''' Returns an empty array if there are no attributes.
        ''' </summary>
        ''' <remarks>
        ''' NOTE: This method should always be kept as a NotOverridable method.
        ''' If you want to override attribute binding logic for a sub-class, then override <see cref="GetAttributesBag"/> method.
        ''' </remarks>
        Public NotOverridable Overloads Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return Me.GetAttributesBag().Attributes
        End Function

        Private Function GetAttributesBag() As CustomAttributesBag(Of VisualBasicAttributeData)
            If m_lazyCustomAttributesBag Is Nothing OrElse Not m_lazyCustomAttributesBag.IsSealed Then
                LoadAndValidateAttributes(OneOrMany.Create(Me.AttributeDeclarationSyntaxList), m_lazyCustomAttributesBag)
            End If
            Return m_lazyCustomAttributesBag
        End Function

        Friend Function GetDecodedWellKnownAttributeData() As EventWellKnownAttributeData
            Dim attributesBag As CustomAttributesBag(Of VisualBasicAttributeData) = Me.m_lazyCustomAttributesBag
            If attributesBag Is Nothing OrElse Not attributesBag.IsDecodedWellKnownAttributeDataComputed Then
                attributesBag = Me.GetAttributesBag()
            End If

            Return DirectCast(attributesBag.DecodedWellKnownAttributeData, EventWellKnownAttributeData)
        End Function

        Friend Overrides Function EarlyDecodeWellKnownAttribute(ByRef arguments As EarlyDecodeWellKnownAttributeArguments(Of EarlyWellKnownAttributeBinder, NamedTypeSymbol, AttributeSyntax, AttributeLocation)) As VisualBasicAttributeData
            Debug.Assert(arguments.AttributeType IsNot Nothing)
            Debug.Assert(Not arguments.AttributeType.IsErrorType())

            Dim boundAttribute As VisualBasicAttributeData = Nothing
            Dim obsoleteData As ObsoleteAttributeData = Nothing

            If EarlyDecodeDeprecatedOrObsoleteAttribute(arguments, boundAttribute, obsoleteData) Then
                If obsoleteData IsNot Nothing Then
                    arguments.GetOrCreateData(Of CommonEventEarlyWellKnownAttributeData)().ObsoleteAttributeData = obsoleteData
                End If

                Return boundAttribute
            End If

            Return MyBase.EarlyDecodeWellKnownAttribute(arguments)
        End Function

        Friend Overrides Sub DecodeWellKnownAttribute(ByRef arguments As DecodeWellKnownAttributeArguments(Of AttributeSyntax, VisualBasicAttributeData, AttributeLocation))
            Debug.Assert(arguments.AttributeSyntaxOpt IsNot Nothing)

            Dim attrData = arguments.Attribute
            If attrData.IsTargetAttribute(Me, AttributeDescription.NonSerializedAttribute) Then
                ' Although NonSerialized attribute is only applicable on fields we relax that restriction and allow application on events as well
                ' to allow making the backing field non-serializable.

                If Me.ContainingType.IsSerializable Then
                    arguments.GetOrCreateData(Of EventWellKnownAttributeData).HasNonSerializedAttribute = True
                Else
                    arguments.Diagnostics.Add(ERRID.ERR_InvalidNonSerializedUsage, arguments.AttributeSyntaxOpt.GetLocation())
                End If

            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.SpecialNameAttribute) Then
                arguments.GetOrCreateData(Of EventWellKnownAttributeData).HasSpecialNameAttribute = True
            End If

            MyBase.DecodeWellKnownAttribute(arguments)
        End Sub

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Dim data = GetDecodedWellKnownAttributeData()
                Return data IsNot Nothing AndAlso data.HasSpecialNameAttribute
            End Get
        End Property

        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing,
                                                             Optional expandIncludes As Boolean = False,
                                                             Optional cancellationToken As CancellationToken = Nothing) As String
            If m_lazyDocComment Is Nothing Then
                ' NOTE: replace Nothing with empty comment
                Interlocked.CompareExchange(
                    m_lazyDocComment, GetDocumentationCommentForSymbol(Me, preferredCulture, expandIncludes, cancellationToken), Nothing)
            End If

            Return m_lazyDocComment
        End Function

        Friend Shared Function DecodeModifiers(modifiers As SyntaxTokenList,
                                               container As SourceMemberContainerTypeSymbol,
                                               binder As Binder,
                                               diagBag As DiagnosticBag) As MemberModifiers
            ' Decode the flags.
            Dim eventModifiers = binder.DecodeModifiers(modifiers,
                SourceMemberFlags.AllAccessibilityModifiers Or
                SourceMemberFlags.Shadows Or
                SourceMemberFlags.Shared,
                ERRID.ERR_BadEventFlags1,
                Accessibility.Public,
                diagBag)

            eventModifiers = binder.ValidateEventModifiers(modifiers, eventModifiers, container, diagBag)

            Return eventModifiers
        End Function

        ' Get the location of the implements name for an explicit implemented event, for later error reporting.
        Friend Function GetImplementingLocation(implementedEvent As EventSymbol) As Location

            Dim eventSyntax = DirectCast(m_syntaxRef.GetSyntax(), EventStatementSyntax)
            Dim syntaxTree = m_syntaxRef.SyntaxTree

            If eventSyntax.ImplementsClause IsNot Nothing Then
                Dim binder = CreateBinderForTypeDeclaration()
                Dim implementingSyntax = FindImplementingSyntax(Of EventSymbol)(eventSyntax.ImplementsClause,
                                                                                 Me,
                                                                                 implementedEvent,
                                                                                 m_containingType,
                                                                                 binder)
                Return implementingSyntax.GetLocation()
            End If

            Return If(Locations.FirstOrDefault(), NoLocation.Singleton)
        End Function

        Private Function CreateBinderForTypeDeclaration() As Binder
            Dim binder = BinderBuilder.CreateBinderForType(ContainingSourceModule, m_syntaxRef.SyntaxTree, m_containingType)
            Return New LocationSpecificBinder(BindingLocation.EventSignature, Me, binder)
        End Function

        Friend Overrides Sub GenerateDeclarationErrors(cancellationToken As CancellationToken)
            MyBase.GenerateDeclarationErrors(cancellationToken)

            Dim unusedType = Me.Type
            Dim unusedImplementations = Me.ExplicitInterfaceImplementations
            Me.CheckExplicitImplementationTypes()
        End Sub

        Public Overrides ReadOnly Property IsWindowsRuntimeEvent As Boolean
            Get
                ' The first implemented event wins - if the others disagree, we'll produce diagnostics.
                ' If no interface events are implemented, then the result is based on the output kind of the compilation.
                Dim implementedEvents As ImmutableArray(Of EventSymbol) = ExplicitInterfaceImplementations
                Return If(implementedEvents.Any,
                            implementedEvents(0).IsWindowsRuntimeEvent,
                            Me.IsCompilationOutputWinMdObj())
            End Get
        End Property

    End Class
End Namespace
