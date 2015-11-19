' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' A TypeSymbol is a base class for all the symbols that represent a type
    ''' in Visual Basic.
    ''' </summary>
    Friend MustInherit Class TypeSymbol
        Inherits NamespaceOrTypeSymbol
        Implements ITypeSymbol

        ' !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        ' Changes to the public interface of this class should remain synchronized with the C# version of Symbol.
        ' Do not make any changes to the public interface without making the corresponding change
        ' to the C# version.
        ' !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        ' TODO (tomat): Consider changing this to an empty name. This name shouldn't ever leak to the user in error messages.
        Friend Shared ReadOnly ImplicitTypeName As String = "<invalid-global-code>"

        Private Shared ReadOnly s_EmptyTypeSymbols() As TypeSymbol = {}

        Private _lazyAllInterfaces As ImmutableArray(Of NamedTypeSymbol)
        Private _lazyInterfacesAndTheirBaseInterfaces As ImmutableHashSet(Of NamedTypeSymbol)

        ' Map with the interface member implementations for this type.
        ' Key is implemented method, value is implementing method (from the perspective of this type)
        ' Don't allocate until someone needs it.
        Private _lazyImplementationForInterfaceMemberMap As ConcurrentDictionary(Of Symbol, Symbol)

        ' Map with all the explicitly implemented interface symbols declared on this type.
        ' key = interface method/property/event, value = explicitly implementing method/property/event declared on this type
        ' Access through ExplicitInterfaceImplementationMap property ONLY!
        Friend m_lazyExplicitInterfaceImplementationMap As Dictionary(Of Symbol, Symbol)


        Public Shared ReadOnly Property EmptyTypeSymbolsList As IList(Of TypeSymbol)
            Get
                Return s_EmptyTypeSymbols
            End Get
        End Property

        ''' <summary>
        ''' Get the original definition of this symbol. If this symbol is derived from another
        ''' symbol by (say) type substitution, this gets the original symbol, as it was defined
        ''' in source or metadata.
        ''' </summary>
        Public Shadows ReadOnly Property OriginalDefinition As TypeSymbol
            Get
                Return OriginalTypeSymbolDefinition
            End Get
        End Property

        Protected Overridable ReadOnly Property OriginalTypeSymbolDefinition As TypeSymbol
            Get
                ' Default implements returns Me.
                Return Me
            End Get
        End Property

        Protected NotOverridable Overrides ReadOnly Property OriginalSymbolDefinition As Symbol
            Get
                Return OriginalTypeSymbolDefinition
            End Get
        End Property

        ''' <summary>
        ''' Gets the BaseType of this type. If the base type could not be determined, then 
        ''' an instance of ErrorType is returned. If this kind of type does not have a base type
        ''' (for example, interfaces), Nothing is returned. Also the special class System.Object
        ''' always has a BaseType of Nothing.
        ''' </summary>
        Public ReadOnly Property BaseType As NamedTypeSymbol
            Get
                Return BaseTypeNoUseSiteDiagnostics
            End Get
        End Property

        Friend MustOverride ReadOnly Property BaseTypeNoUseSiteDiagnostics As NamedTypeSymbol

        Friend Function BaseTypeWithDefinitionUseSiteDiagnostics(<[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As NamedTypeSymbol
            Dim result = BaseTypeNoUseSiteDiagnostics

            If result IsNot Nothing Then
                result.OriginalDefinition.AddUseSiteDiagnostics(useSiteDiagnostics)
            End If

            Return result
        End Function

        Friend Function BaseTypeOriginalDefinition(<[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As NamedTypeSymbol
            Dim result = BaseTypeNoUseSiteDiagnostics

            If result IsNot Nothing Then
                result = result.OriginalDefinition
                result.AddUseSiteDiagnostics(useSiteDiagnostics)
            End If

            Return result
        End Function

        ''' <summary>
        ''' Gets the set of interfaces that this type directly implements. This set does not
        ''' include interfaces that are base interfaces of directly implemented interfaces.
        ''' </summary>
        Public ReadOnly Property Interfaces As ImmutableArray(Of NamedTypeSymbol)
            Get
                Return InterfacesNoUseSiteDiagnostics
            End Get
        End Property

        Friend MustOverride ReadOnly Property InterfacesNoUseSiteDiagnostics As ImmutableArray(Of NamedTypeSymbol)

        ''' <summary>
        ''' The list of all interfaces of which this type is a declared subtype, excluding this type
        ''' itself. This includes all declared base interfaces, all declared base interfaces of base
        ''' types, and all declared base interfaces of those results (recursively).  Each result
        ''' appears exactly once in the list. This list is topologically sorted by the inheritance
        ''' relationship: if interface type A extends interface type B, then A precedes B in the
        ''' list. This is not quite the same as "all interfaces of which this type is a proper
        ''' subtype" because it does not take into account variance: AllInterfaces for
        ''' IEnumerable(Of String) will not include IEnumerable(Of Object).
        ''' </summary>
        Public ReadOnly Property AllInterfaces As ImmutableArray(Of NamedTypeSymbol)
            Get
                Return AllInterfacesNoUseSiteDiagnostics
            End Get
        End Property

        Friend ReadOnly Property AllInterfacesNoUseSiteDiagnostics As ImmutableArray(Of NamedTypeSymbol)
            Get
                If (_lazyAllInterfaces.IsDefault) Then
                    ImmutableInterlocked.InterlockedInitialize(_lazyAllInterfaces, MakeAllInterfaces())
                End If

                Return _lazyAllInterfaces
            End Get
        End Property

        Friend Function AllInterfacesWithDefinitionUseSiteDiagnostics(<[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As ImmutableArray(Of NamedTypeSymbol)
            Dim result = AllInterfacesNoUseSiteDiagnostics

            ' Since bases affect content of AllInterfaces set, we need to make sure they all are good.
            Me.AddUseSiteDiagnosticsForBaseDefinitions(useSiteDiagnostics)

            For Each iface In result
                iface.OriginalDefinition.AddUseSiteDiagnostics(useSiteDiagnostics)
            Next

            Return result
        End Function

        ''' Produce all implemented interfaces in topologically sorted order. We use
        ''' TypeSymbol.Interfaces as the source of edge data, which has had cycles and infinitely
        ''' long dependency cycles removed. Consequently, it is possible (and we do) use the
        ''' simplest version of Tarjan's topological sorting algorithm.
        Protected Overridable Function MakeAllInterfaces() As ImmutableArray(Of NamedTypeSymbol)
            'TODO: Might want to use different implementation for substituted type (see C# code)

            Dim result = ArrayBuilder(Of NamedTypeSymbol).GetInstance()
            Dim visited = New HashSet(Of NamedTypeSymbol)()

            Dim baseType = Me

            While baseType IsNot Nothing
                Dim baseInterfaces As ImmutableArray(Of NamedTypeSymbol) = baseType.InterfacesNoUseSiteDiagnostics
                For n = baseInterfaces.Length - 1 To 0 Step -1
                    MakeAllInterfacesInternal(baseInterfaces(n), visited, result)
                Next

                baseType = baseType.BaseTypeNoUseSiteDiagnostics
            End While

            result.ReverseContents()
            Return result.ToImmutableAndFree()
        End Function

        Private Shared Sub MakeAllInterfacesInternal(i As NamedTypeSymbol, visited As HashSet(Of NamedTypeSymbol), result As ArrayBuilder(Of NamedTypeSymbol))
            If visited.Add(i) Then
                Dim baseInterfaces As ImmutableArray(Of NamedTypeSymbol) = i.InterfacesNoUseSiteDiagnostics
                For n = baseInterfaces.Length - 1 To 0 Step -1
                    MakeAllInterfacesInternal(baseInterfaces(n), visited, result)
                Next

                result.Add(i)
            End If
        End Sub

        ''' <summary>
        ''' Gets the set of interfaces that this type directly implements, plus the base interfaces
        ''' of all such types.
        ''' </summary>
        ''' <remarks>
        ''' CONSIDER: it probably isn't truly necessary to cache this.  If space gets tight, consider
        ''' alternative approaches (recompute every time, cache on the side, only store on some types,
        ''' etc).
        ''' </remarks>
        Friend ReadOnly Property InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics As ImmutableHashSet(Of NamedTypeSymbol)
            Get
                If _lazyInterfacesAndTheirBaseInterfaces Is Nothing Then
                    Interlocked.CompareExchange(_lazyInterfacesAndTheirBaseInterfaces, MakeInterfacesAndTheirBaseInterfaces(Me.InterfacesNoUseSiteDiagnostics), Nothing)
                End If

                Return _lazyInterfacesAndTheirBaseInterfaces
            End Get

        End Property

        ' Note: Unlike MakeAllInterfaces, this doesn't need to be virtual. It depends on
        ' AllInterfaces for its implementation, so it will pick up all changes to MakeAllInterfaces
        ' indirectly.
        Private Shared Function MakeInterfacesAndTheirBaseInterfaces(declaredInterfaces As ImmutableArray(Of NamedTypeSymbol)) As ImmutableHashSet(Of NamedTypeSymbol)
            Dim resultBuilder = New HashSet(Of NamedTypeSymbol)()
            For Each [interface] In declaredInterfaces
                If Not resultBuilder.Contains([interface]) Then
                    resultBuilder.Add([interface])
                    resultBuilder.UnionWith([interface].AllInterfacesNoUseSiteDiagnostics)
                End If
            Next

            Return If(resultBuilder.Count = 0, ImmutableHashSet.Create(Of NamedTypeSymbol)(), ImmutableHashSet.CreateRange(Of NamedTypeSymbol)(resultBuilder))
        End Function

        ''' <summary>
        ''' Returns true if this type is known to be a reference type. It is never the case
        ''' that <see cref="IsReferenceType"/> and <see cref="IsValueType"/> both return true. However, for an unconstrained
        ''' type parameter, <see cref="IsReferenceType"/> and <see cref="IsValueType"/> will both return false.
        ''' </summary>
        Public MustOverride ReadOnly Property IsReferenceType As Boolean Implements ITypeSymbol.IsReferenceType

        ''' <summary>
        ''' Returns true if this type is known to be a value type. It is never the case
        ''' that <see cref="IsReferenceType"/> and <see cref="IsValueType"/> both return true. However, for an unconstrained
        ''' type parameter, <see cref="IsReferenceType"/> and <see cref="IsValueType"/> will both return false.
        ''' </summary>
        Public MustOverride ReadOnly Property IsValueType As Boolean Implements ITypeSymbol.IsValueType

        ''' <summary>
        ''' Is this a symbol for an anonymous type (including delegate).
        ''' </summary>
        Public Overridable ReadOnly Property IsAnonymousType As Boolean Implements ITypeSymbol.IsAnonymousType
            Get
                Return False
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsShared As Boolean
            Get
                ' VB doesn't have a concept of shared types.
                Return False
            End Get
        End Property

        ' Only the compiler can create TypeSymbols.
        Friend Sub New()
        End Sub

        ''' <summary>
        ''' Gets the kind of this type.
        ''' </summary>
        Public MustOverride ReadOnly Property TypeKind As TypeKind

        ''' <summary>
        ''' Gets corresponding special TypeId of this type.
        ''' </summary>
        Public Overridable ReadOnly Property SpecialType As SpecialType Implements ITypeSymbol.SpecialType
            Get
                Return SpecialType.None
            End Get
        End Property

        ''' <summary>
        ''' Gets corresponding primitive type code for this type declaration.
        ''' </summary>
        Friend ReadOnly Property PrimitiveTypeCode As Microsoft.Cci.PrimitiveTypeCode
            Get
                Return SpecialTypes.GetTypeCode(SpecialType)
            End Get
        End Property

        ''' <summary>
        ''' Substitute the given type substitution within this type, returning a new type. If the
        ''' substitution had no effect, return Me. 
        ''' !!! Only code implementing construction of generic types is allowed to call this method !!!
        ''' !!! All other code should use Construct methods.                                        !!! 
        ''' </summary>
        Friend MustOverride Function InternalSubstituteTypeParameters(substitution As TypeSubstitution) As TypeWithModifiers

        <Obsolete("Use TypeWithModifiers.Is method.", True)>
        Friend Overloads Function Equals(other As TypeWithModifiers) As Boolean
            Return other.Is(Me)
        End Function

        ''' <summary>
        ''' Lookup an immediately nested type referenced from metadata, names should be
        ''' compared case-sensitively.
        ''' </summary>
        ''' <param name="emittedTypeName">
        ''' Type name.
        ''' </param>
        ''' <returns>
        ''' Symbol for the type, or MissingMetadataSymbol if the type isn't found.
        ''' </returns>
        ''' <remarks></remarks>
        Friend Overridable Function LookupMetadataType(ByRef emittedTypeName As MetadataTypeName) As NamedTypeSymbol
            Debug.Assert(Not emittedTypeName.IsNull)
            Debug.Assert(TypeOf Me Is NamedTypeSymbol)

            Dim namedType As NamedTypeSymbol = Nothing

            If Me.Kind <> SymbolKind.ErrorType Then
                Dim typeMembers As ImmutableArray(Of NamedTypeSymbol)

                If emittedTypeName.IsMangled Then
                    Debug.Assert(Not emittedTypeName.UnmangledTypeName.Equals(emittedTypeName.TypeName) AndAlso emittedTypeName.InferredArity > 0)

                    If emittedTypeName.ForcedArity = -1 OrElse emittedTypeName.ForcedArity = emittedTypeName.InferredArity Then
                        ' Let's handle mangling case first.
                        typeMembers = Me.GetTypeMembers(emittedTypeName.UnmangledTypeName)

                        For Each named In typeMembers
                            If emittedTypeName.InferredArity = named.Arity AndAlso named.MangleName AndAlso String.Equals(named.Name, emittedTypeName.UnmangledTypeName, StringComparison.Ordinal) Then
                                If namedType IsNot Nothing Then
                                    ' ambiguity
                                    namedType = Nothing
                                    Exit For
                                End If

                                namedType = named
                            End If
                        Next
                    End If
                Else
                    Debug.Assert(emittedTypeName.UnmangledTypeName Is emittedTypeName.TypeName AndAlso emittedTypeName.InferredArity = 0)
                End If

                ' Now try lookup without removing generic arity mangling.
                Dim forcedArity As Integer = emittedTypeName.ForcedArity

                If emittedTypeName.UseCLSCompliantNameArityEncoding Then
                    ' Only types with arity 0 are acceptable, we already examined types with mangled names.
                    If emittedTypeName.InferredArity > 0 Then
                        GoTo Done
                    ElseIf forcedArity = -1 Then
                        forcedArity = 0
                    ElseIf forcedArity <> 0 Then
                        GoTo Done
                    Else
                        Debug.Assert(forcedArity = emittedTypeName.InferredArity)
                    End If
                End If

                typeMembers = Me.GetTypeMembers(emittedTypeName.TypeName)

                For Each named In typeMembers
                    ' If the name of the type must include generic mangling, it cannot be our match.
                    If Not named.MangleName AndAlso (forcedArity = -1 OrElse forcedArity = named.Arity) AndAlso
                       String.Equals(named.Name, emittedTypeName.TypeName, StringComparison.Ordinal) Then

                        If namedType IsNot Nothing Then
                            ' ambiguity
                            namedType = Nothing
                            Exit For
                        End If

                        namedType = named
                    End If
                Next
            End If

Done:
            If namedType Is Nothing Then
                Return New MissingMetadataTypeSymbol.Nested(DirectCast(Me, NamedTypeSymbol), emittedTypeName)
            End If

            Return namedType
        End Function

        Friend Overridable Function GetDirectBaseTypeNoUseSiteDiagnostics(basesBeingResolved As ConsList(Of Symbol)) As NamedTypeSymbol
            Return BaseTypeNoUseSiteDiagnostics
        End Function

        Friend Overridable Function GetDirectBaseTypeWithDefinitionUseSiteDiagnostics(basesBeingResolved As ConsList(Of Symbol), <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As NamedTypeSymbol
            Dim result = GetDirectBaseTypeNoUseSiteDiagnostics(basesBeingResolved)

            If result IsNot Nothing Then
                result.OriginalDefinition.AddUseSiteDiagnostics(useSiteDiagnostics)
            End If

            Return result
        End Function

#Region "Use-Site Diagnostics"

        ''' <summary>
        ''' Return error code that has highest priority while calculating use site error for this symbol. 
        ''' </summary>
        Protected Overrides ReadOnly Property HighestPriorityUseSiteError As Integer
            Get
                Return ERRID.ERR_UnsupportedType1
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property HasUnsupportedMetadata As Boolean
            Get
                Dim info As DiagnosticInfo = GetUseSiteErrorInfo()
                Return info IsNot Nothing AndAlso info.Code = ERRID.ERR_UnsupportedType1
            End Get
        End Property

        Friend MustOverride Overloads Function GetUnificationUseSiteDiagnosticRecursive(owner As Symbol, ByRef checkedTypes As HashSet(Of TypeSymbol)) As DiagnosticInfo
#End Region

#Region "ITypeSymbol"

        Private Function ITypeSymbol_FindImplementationForInterfaceMember(interfaceMember As ISymbol) As ISymbol Implements ITypeSymbol.FindImplementationForInterfaceMember
            Return If(TypeOf interfaceMember Is Symbol,
                FindImplementationForInterfaceMember(DirectCast(interfaceMember, Symbol)),
                Nothing)
        End Function

        Private ReadOnly Property ITypeSymbol_AllInterfaces As ImmutableArray(Of INamedTypeSymbol) Implements ITypeSymbol.AllInterfaces
            Get
                Return StaticCast(Of INamedTypeSymbol).From(Me.AllInterfacesNoUseSiteDiagnostics)
            End Get
        End Property

        Private ReadOnly Property ITypeSymbol_BaseType As INamedTypeSymbol Implements ITypeSymbol.BaseType
            Get
                Return Me.BaseTypeNoUseSiteDiagnostics
            End Get
        End Property

        Private ReadOnly Property ITypeSymbol_Interfaces As ImmutableArray(Of INamedTypeSymbol) Implements ITypeSymbol.Interfaces
            Get
                Return StaticCast(Of INamedTypeSymbol).From(Me.InterfacesNoUseSiteDiagnostics)
            End Get
        End Property

        Private ReadOnly Property ITypeSymbol_OriginalDefinition As ITypeSymbol Implements ITypeSymbol.OriginalDefinition
            Get
                Return Me.OriginalDefinition
            End Get
        End Property

        Private ReadOnly Property ITypeSymbol_TypeKind As TypeKind Implements ITypeSymbol.TypeKind
            Get
                Return Me.TypeKind.ToCommon()
            End Get
        End Property

#End Region

#Region "Interface checks"
        ''' <summary>
        ''' Returns the corresponding symbol in this type or a base type that implements 
        ''' interfaceMember (either implicitly or explicitly), or null if no such symbol 
        ''' exists (which might be either because this type doesn't implement the container 
        ''' of interfaceMember, or this type doesn't supply a member that successfully 
        ''' implements interfaceMember).
        ''' </summary>
        ''' <param name="interfaceMember">
        ''' Must be a non-null interface property, method, or event.
        ''' </param>
        ''' <returns>The implementing member.</returns>
        Public Function FindImplementationForInterfaceMember(interfaceMember As Symbol) As Symbol
            ' This layer handles caching, ComputeImplementationForInterfaceMember does the work.
            If interfaceMember Is Nothing Then
                Throw New ArgumentNullException(NameOf(interfaceMember))
            End If

            If Not interfaceMember.ContainingType.IsInterfaceType() OrElse
                Not Me.ImplementsInterface(interfaceMember.ContainingType, Nothing) Then
                Return Nothing
            End If

            ' PERF: Avoid delegate allocation by splitting GetOrAdd into TryGetValue+TryAdd
            Dim map = ImplementationForInterfaceMemberMap
            Dim result As Symbol = Nothing
            If map.TryGetValue(interfaceMember, result) Then
                Return result
            End If

            result = ComputeImplementationForInterfaceMember(interfaceMember)
            map.TryAdd(interfaceMember, result)
            Return result
        End Function

        Private ReadOnly Property ImplementationForInterfaceMemberMap As ConcurrentDictionary(Of Symbol, Symbol)
            Get
                Dim map = _lazyImplementationForInterfaceMemberMap
                If map IsNot Nothing Then
                    Return map
                End If

                ' PERF: Avoid over-allocation. In many cases, there's only 1 entry and we don't expect concurrent updates.
                map = New ConcurrentDictionary(Of Symbol, Symbol)(concurrencyLevel:=1, capacity:=1)
                Return If(Interlocked.CompareExchange(_lazyImplementationForInterfaceMemberMap, map, Nothing), map)
            End Get
        End Property


        ''' <summary>
        ''' Compute the implementation for an interface member in this type, or Nothing if none.
        ''' </summary>
        Private Function ComputeImplementationForInterfaceMember(interfaceMember As Symbol) As Symbol
            Select Case interfaceMember.Kind
                Case SymbolKind.Method
                    Return ImplementsHelper.ComputeImplementationForInterfaceMember(Of MethodSymbol)(
                        DirectCast(interfaceMember, MethodSymbol),
                        Me,
                        MethodSignatureComparer.RuntimeMethodSignatureComparer)

                Case SymbolKind.Property
                    Return ImplementsHelper.ComputeImplementationForInterfaceMember(Of PropertySymbol)(
                        DirectCast(interfaceMember, PropertySymbol),
                        Me,
                        PropertySignatureComparer.RuntimePropertySignatureComparer)

                Case SymbolKind.Event
                    Return ImplementsHelper.ComputeImplementationForInterfaceMember(Of EventSymbol)(
                        DirectCast(interfaceMember, EventSymbol),
                        Me,
                        EventSignatureComparer.RuntimeEventSignatureComparer)

                Case Else
                    Return Nothing
            End Select
        End Function

        ' Given a symbol in an interface, return a symbol in THIS type that explicitly implements that method.
        ' Does NOT look into base types for implementations.
        Friend Function GetExplicitImplementationForInterfaceMember(Of T As Symbol)(interfaceMethod As T) As T
            Dim implementingMethod As Symbol = Nothing
            Me.ExplicitInterfaceImplementationMap.TryGetValue(interfaceMethod, implementingMethod)

            Return DirectCast(implementingMethod, T)
        End Function

        ' Get a dictionary with all the explicitly implemented interface symbols declared on this type.
        ' key = interface method/property/event, value = explicitly implementing method/property/event declared on this type
        '
        ' Note: This implementation is overridden by source symbols, because they diagnose errors also.
        Friend Overridable ReadOnly Property ExplicitInterfaceImplementationMap As Dictionary(Of Symbol, Symbol)
            Get
                If m_lazyExplicitInterfaceImplementationMap Is Nothing Then
                    Interlocked.CompareExchange(Me.m_lazyExplicitInterfaceImplementationMap, MakeExplicitInterfaceImplementationMap(), Nothing)
                End If

                Return m_lazyExplicitInterfaceImplementationMap
            End Get
        End Property

        ' An empty dictionary to use if there are no members in the explicit interface map.
        Friend Shared ReadOnly EmptyExplicitImplementationMap As Dictionary(Of Symbol, Symbol) = New Dictionary(Of Symbol, Symbol)

        ' Build the explicit interface map for this type. 
        ' This implementation is not used by source symbols, which additionally diagnose errors.
        Private Function MakeExplicitInterfaceImplementationMap() As Dictionary(Of Symbol, Symbol)
            If Me.IsClassType() OrElse Me.IsStructureType() Then
                Dim map = New Dictionary(Of Symbol, Symbol)()
                For Each implementingMember In Me.GetMembersUnordered()
                    For Each interfaceMember In GetExplicitInterfaceImplementations(implementingMember)
                        If Not map.ContainsKey(interfaceMember) Then
                            map.Add(interfaceMember, implementingMember) ' use first implementation found, even though duplicate is an error it could happen.
                        End If
                    Next
                Next

                If map.Count > 0 Then
                    Return map
                Else
                    Return EmptyExplicitImplementationMap  ' Better to use singleton and garbage collection the empty dictionary we just created.
                End If
            Else
                Return EmptyExplicitImplementationMap
            End If
        End Function


#End Region
    End Class
End Namespace
