' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' This class represents a synthesized delegate type derived from an Event declaration
    ''' </summary>
    ''' <remarks>
    ''' <example>
    ''' Class C
    '''   Event Name(a As Integer, b As Integer)
    ''' End Class
    ''' 
    ''' defines an event and a delegate type:
    ''' 
    ''' Event Name As NamedEventHandler
    ''' Delegate Sub NameEventHandler(a As Integer, b As Integer)
    ''' 
    ''' </example>
    ''' </remarks>
    Friend NotInheritable Class SynthesizedEventDelegateSymbol
        Inherits InstanceTypeSymbol

        Private ReadOnly _eventName As String
        Private ReadOnly _name As String
        Private ReadOnly _containingType As NamedTypeSymbol
        Private ReadOnly _syntaxRef As SyntaxReference

        Private _lazyMembers As ImmutableArray(Of Symbol)
        Private _lazyEventSymbol As EventSymbol

        Private _reportedAllDeclarationErrors As Integer = 0 ' An integer to be able to do Interlocked operations.

        Friend Sub New(syntaxRef As SyntaxReference, containingSymbol As NamedTypeSymbol)
            Me._containingType = containingSymbol
            Me._syntaxRef = syntaxRef

            Dim eventName = Me.EventSyntax.Identifier.ValueText
            Me._eventName = eventName
            Me._name = _eventName & StringConstants.EventDelegateSuffix
        End Sub

        Public Overloads Overrides Function GetMembers() As ImmutableArray(Of Symbol)
            If Not _lazyMembers.IsDefault Then
                Return _lazyMembers
            End If

            Dim sourceModule = DirectCast(Me.ContainingModule, SourceModuleSymbol)
            Dim binder As Binder = BinderBuilder.CreateBinderForType(sourceModule, _syntaxRef.SyntaxTree, Me.ContainingType)

            Dim diagBag = BindingDiagnosticBag.GetInstance()

            Dim syntax = Me.EventSyntax
            Dim paramListOpt = syntax.ParameterList

            Dim ctor As MethodSymbol = Nothing
            Dim beginInvoke As MethodSymbol = Nothing
            Dim endInvoke As MethodSymbol = Nothing
            Dim invoke As MethodSymbol = Nothing

            SourceDelegateMethodSymbol.MakeDelegateMembers(Me, Me.EventSyntax, syntax.ParameterList, binder, ctor, beginInvoke, endInvoke, invoke, diagBag)

            ' We shouldn't need to check if this is a winmd compilation because 
            ' winmd output requires that all events be declared Event ... As ...,
            ' but we can't add Nothing to the array, even if a diagnostic will be produced later
            ' Invoke must always be the last member
            Dim members As ImmutableArray(Of Symbol)
            If beginInvoke Is Nothing OrElse endInvoke Is Nothing Then
                members = ImmutableArray.Create(Of Symbol)(ctor, invoke)
            Else
                members = ImmutableArray.Create(Of Symbol)(ctor, beginInvoke, endInvoke, invoke)
            End If

            sourceModule.AtomicStoreArrayAndDiagnostics(_lazyMembers, members, diagBag)
            diagBag.Free()

            Return _lazyMembers
        End Function

        Public Overloads Overrides Function GetMembers(name As String) As ImmutableArray(Of Symbol)
            Return (From m In GetMembers() Where IdentifierComparison.Equals(m.Name, name)).AsImmutable
        End Function

        Friend Overrides Function GetFieldsToEmit() As IEnumerable(Of FieldSymbol)
            Return SpecializedCollections.EmptyEnumerable(Of FieldSymbol)()
        End Function

        Private ReadOnly Property EventSyntax As EventStatementSyntax
            Get
                Return DirectCast(Me._syntaxRef.GetSyntax, EventStatementSyntax)
            End Get
        End Property

        Public Overrides ReadOnly Property AssociatedSymbol As Symbol
            Get
                If _lazyEventSymbol Is Nothing Then
                    Dim events = _containingType.GetMembers(_eventName)
                    For Each e In events
                        Dim asEvent = TryCast(e, SourceEventSymbol)
                        If asEvent IsNot Nothing Then
                            Dim evSyntax = asEvent.SyntaxReference.GetSyntax
                            If evSyntax IsNot Nothing AndAlso evSyntax Is EventSyntax Then
                                _lazyEventSymbol = asEvent
                            End If
                        End If
                    Next
                End If

                Debug.Assert(_lazyEventSymbol IsNot Nothing, "We should have found our event here")
                Return _lazyEventSymbol
            End Get
        End Property

        ''' <summary>
        ''' This property may be called while containing type is still being constructed.
        ''' Therefore it can take membersInProgress context to ensure that returned symbol
        ''' is relevant to the current type construction.
        ''' (there could be several optimistically concurrent sessions)
        ''' </summary>
        Friend Overrides ReadOnly Property ImplicitlyDefinedBy(Optional membersInProgress As Dictionary(Of String, ArrayBuilder(Of Symbol)) = Nothing) As Symbol
            Get
                If membersInProgress Is Nothing Then
                    Return AssociatedSymbol
                End If

                Dim candidates = membersInProgress(_eventName)
                Dim eventInCurrentContext As SourceEventSymbol = Nothing

                Debug.Assert(candidates IsNot Nothing, "where is my event?")
                If candidates IsNot Nothing Then
                    For Each e In candidates
                        Dim asEvent = TryCast(e, SourceEventSymbol)
                        If asEvent IsNot Nothing Then
                            Dim evSyntax = asEvent.SyntaxReference.GetSyntax
                            If evSyntax IsNot Nothing AndAlso evSyntax Is EventSyntax Then
                                eventInCurrentContext = asEvent
                            End If
                        End If
                    Next
                End If

                Return eventInCurrentContext
            End Get
        End Property

        Public Overrides ReadOnly Property Arity As Integer
            Get
                Return 0
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _containingType
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingType As NamedTypeSymbol
            Get
                Return _containingType
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return AssociatedSymbol.DeclaredAccessibility
            End Get
        End Property

        Friend Overrides ReadOnly Property DefaultPropertyName As String
            Get
                Return Nothing
            End Get
        End Property

        Public Overloads Overrides Function GetTypeMembers() As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Public Overloads Overrides Function GetTypeMembers(name As String) As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Public Overloads Overrides Function GetTypeMembers(name As String, arity As Integer) As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Public Overrides ReadOnly Property IsMustInherit As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotInheritable As Boolean
            Get
                Return True
            End Get
        End Property

        Friend Overrides ReadOnly Property ShadowsExplicitly As Boolean
            Get
                Return AssociatedSymbol.ShadowsExplicitly
            End Get
        End Property

        Friend Overrides Function GetLexicalSortKey() As LexicalSortKey
            ' WARNING: this should not allocate memory!
            Return New LexicalSortKey(_syntaxRef, Me.DeclaringCompilation)
        End Function

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray.Create(_syntaxRef.GetLocation())
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property

        Friend Overrides Function MakeAcyclicBaseType(diagnostics As BindingDiagnosticBag) As NamedTypeSymbol
            Return MakeDeclaredBase(Nothing, diagnostics)
        End Function

        Friend Overrides Function MakeAcyclicInterfaces(diagnostics As BindingDiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Friend Overrides Function MakeDeclaredBase(basesBeingResolved As BasesBeingResolved, diagnostics As BindingDiagnosticBag) As NamedTypeSymbol
            Return _containingType.ContainingAssembly.GetSpecialType(Microsoft.CodeAnalysis.SpecialType.System_MulticastDelegate)
        End Function

        Friend Overrides Function MakeDeclaredInterfaces(basesBeingResolved As BasesBeingResolved, diagnostics As BindingDiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Friend Overrides ReadOnly Property MangleName As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property MemberNames As System.Collections.Generic.IEnumerable(Of String)
            Get
                Return New HashSet(Of String)(From member In GetMembers() Select member.Name)
            End Get
        End Property

        Public Overrides ReadOnly Property MightContainExtensionMethods As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property HasCodeAnalysisEmbeddedAttribute As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property HasVisualBasicEmbeddedAttribute As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property IsExtensibleInterfaceNoUseSiteDiagnostics As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property IsWindowsRuntimeImport As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property ShouldAddWinRTMembers As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property IsComImport As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property CoClassType As TypeSymbol
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides Function GetAppliedConditionalSymbols() As ImmutableArray(Of String)
            Return ImmutableArray(Of String).Empty
        End Function

        Friend Overrides Function GetAttributeUsageInfo() As AttributeUsageInfo
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend Overrides ReadOnly Property HasDeclarativeSecurity As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides Function GetSecurityInformation() As IEnumerable(Of Microsoft.Cci.SecurityAttribute)
            Throw ExceptionUtilities.Unreachable
        End Function

        Public Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsSerializable As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property Layout As TypeLayout
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingCharSet As CharSet
            Get
                Return DefaultMarshallingCharSet
            End Get
        End Property

        Public Overrides ReadOnly Property TypeKind As TypeKind
            Get
                Return TypeKind.Delegate
            End Get
        End Property

        Friend Overrides ReadOnly Property IsInterface As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                Return ImmutableArray(Of TypeParameterSymbol).Empty
            End Get
        End Property

        ''' <summary>
        ''' Force all declaration errors to be generated.
        ''' </summary>
        Friend Overrides Sub GenerateDeclarationErrors(cancellationToken As CancellationToken)
            If _reportedAllDeclarationErrors <> 0 Then
                Return
            End If

            GetMembers()

            cancellationToken.ThrowIfCancellationRequested()

            Dim diagnostics = BindingDiagnosticBag.GetInstance()

            ' Force parameters and return value of Invoke method to be bound and errors reported.
            ' Parameters on other delegate methods are derived from Invoke so we don't need to call those.
            Me.DelegateInvokeMethod.GenerateDeclarationErrors(cancellationToken)

            Dim container = _containingType
            Dim outermostVariantInterface As NamedTypeSymbol = Nothing

            Do
                If Not container.IsInterfaceType() Then
                    Debug.Assert(Not container.IsDelegateType())
                    ' Non-interface, non-delegate containers are illegal within variant interfaces.
                    ' An error on the container will be sufficient if we haven't run into an interface already.
                    Exit Do
                End If

                If container.TypeParameters.HaveVariance() Then
                    ' We are inside of a variant interface
                    outermostVariantInterface = container
                End If

                container = container.ContainingType
            Loop While container IsNot Nothing

            If outermostVariantInterface IsNot Nothing Then
                ' "Event definitions with parameters are not allowed in an interface such as '|1' that has
                ' 'In' or 'Out' type parameters. Consider declaring the event by using a delegate type which
                ' is not defined within '|1'. For example, 'Event |2 As Action(Of ...)'."
                diagnostics.Add(New VBDiagnostic(ErrorFactory.ErrorInfo(ERRID.ERR_VariancePreventsSynthesizedEvents2,
                                                                      CustomSymbolDisplayFormatter.QualifiedName(outermostVariantInterface),
                                                                      AssociatedSymbol.Name),
                                                Locations(0)))
            End If

            DirectCast(ContainingModule, SourceModuleSymbol).AtomicStoreIntegerAndDiagnostics(_reportedAllDeclarationErrors, 1, 0, diagnostics)

            diagnostics.Free()
        End Sub

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                ' these are always implicitly declared.
                Return True
            End Get
        End Property

        Friend Overrides ReadOnly Property EmbeddedSymbolKind As EmbeddedSymbolKind
            Get
                Return Me.ContainingType.EmbeddedSymbolKind
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides Function GetSynthesizedWithEventsOverrides() As IEnumerable(Of PropertySymbol)
            Return SpecializedCollections.EmptyEnumerable(Of PropertySymbol)()
        End Function

        Friend Overrides ReadOnly Property HasAnyDeclaredRequiredMembers As Boolean
            Get
                Return False
            End Get
        End Property
    End Class
End Namespace

