' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Concurrent
Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Represents source or metadata assembly.
    ''' </summary>
    ''' <remarks></remarks>
    Friend MustInherit Class MetadataOrSourceAssemblySymbol
        Inherits NonMissingAssemblySymbol

        ''' <summary>
        ''' An array of cached Cor types defined in this assembly.
        ''' Lazily filled by GetSpecialType method.
        ''' </summary>
        ''' <remarks></remarks>
        Private m_LazySpecialTypes() As NamedTypeSymbol

        ''' <summary>
        ''' How many Cor types have we cached so far.
        ''' </summary>
        Private m_CachedSpecialTypes As Integer

        ''' <summary>
        ''' Lookup declaration for predefined CorLib type in this Assembly. Only should be
        ''' called if it is know that this is the Cor Library (mscorlib).
        ''' </summary>
        ''' <param name="type"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Overrides Function GetDeclaredSpecialType(type As SpecialType) As NamedTypeSymbol
#If DEBUG Then
            For Each [module] In Me.Modules
                Debug.Assert([module].GetReferencedAssemblies().Length = 0)
            Next
#End If

            If m_LazySpecialTypes Is Nothing OrElse m_LazySpecialTypes(type) Is Nothing Then

                Dim emittedName As MetadataTypeName = MetadataTypeName.FromFullName(SpecialTypes.GetMetadataName(type), useCLSCompliantNameArityEncoding:=True)

                Dim [module] As ModuleSymbol = Me.Modules(0)
                Dim result As NamedTypeSymbol = [module].LookupTopLevelMetadataType(emittedName)
                If result.TypeKind <> TypeKind.Error AndAlso result.DeclaredAccessibility <> Accessibility.Public Then
                    result = New MissingMetadataTypeSymbol.TopLevel([module], emittedName, type)
                End If
                RegisterDeclaredSpecialType(result)
            End If

            Return m_LazySpecialTypes(type)

        End Function

        ''' <summary>
        ''' Register declaration of predefined CorLib type in this Assembly.
        ''' </summary>
        ''' <param name="corType"></param>
        Friend Overrides Sub RegisterDeclaredSpecialType(corType As NamedTypeSymbol)
            Dim typeId As SpecialType = corType.SpecialType
            Debug.Assert(typeId <> SpecialType.None)
            Debug.Assert(corType.ContainingAssembly Is Me)
            Debug.Assert(corType.ContainingModule.Ordinal = 0)
            Debug.Assert(Me.CorLibrary Is Me)

            If (m_LazySpecialTypes Is Nothing) Then
                Interlocked.CompareExchange(m_LazySpecialTypes,
                    New NamedTypeSymbol(SpecialType.Count) {}, Nothing)
            End If

            If (Interlocked.CompareExchange(m_LazySpecialTypes(typeId), corType, Nothing) IsNot Nothing) Then
                Debug.Assert(corType Is m_LazySpecialTypes(typeId) OrElse
                                        (corType.Kind = SymbolKind.ErrorType AndAlso
                                        m_LazySpecialTypes(typeId).Kind = SymbolKind.ErrorType))
            Else
                Interlocked.Increment(m_CachedSpecialTypes)
                Debug.Assert(m_CachedSpecialTypes > 0 AndAlso m_CachedSpecialTypes <= SpecialType.Count)
            End If
        End Sub

        ''' <summary>
        ''' Continue looking for declaration of predefined CorLib type in this Assembly
        ''' while symbols for new type declarations are constructed.
        ''' </summary>
        Friend Overrides ReadOnly Property KeepLookingForDeclaredSpecialTypes As Boolean
            Get
                Return Me.CorLibrary Is Me AndAlso m_CachedSpecialTypes < SpecialType.Count
            End Get
        End Property

        Private m_lazyTypeNames As ICollection(Of String)
        Private m_lazyNamespaceNames As ICollection(Of String)

        Public Overrides ReadOnly Property TypeNames As ICollection(Of String)
            Get
                If m_lazyTypeNames Is Nothing Then
                    Interlocked.CompareExchange(m_lazyTypeNames, UnionCollection(Of String).Create(Me.Modules, Function(m) m.TypeNames), Nothing)
                End If

                Return m_lazyTypeNames
            End Get
        End Property

        Public Overrides ReadOnly Property NamespaceNames As ICollection(Of String)
            Get
                If m_lazyNamespaceNames Is Nothing Then
                    Interlocked.CompareExchange(m_lazyNamespaceNames, UnionCollection(Of String).Create(Me.Modules, Function(m) m.NamespaceNames), Nothing)
                End If
                Return m_lazyNamespaceNames
            End Get
        End Property

        ''' <summary>
        ''' Determine whether this assembly has been granted access to <paramref name="potentialGiverOfAccess"></paramref>.
        ''' Assumes that the public key has been determined. The result will be cached.
        ''' </summary>
        ''' <param name="potentialGiverOfAccess"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Function MakeFinalIVTDetermination(potentialGiverOfAccess As AssemblySymbol) As IVTConclusion
            Dim result As IVTConclusion = IVTConclusion.NoRelationshipClaimed
            If AssembliesToWhichInternalAccessHasBeenDetermined.TryGetValue(potentialGiverOfAccess, result) Then
                Return result
            End If

            result = IVTConclusion.NoRelationshipClaimed

            'EDMAURER returns an empty list if there was no IVT attribute at all for the given name
            'A name w/o a key is represented by a list with an entry that is empty
            Dim publicKeys As IEnumerable(Of ImmutableArray(Of Byte)) = potentialGiverOfAccess.GetInternalsVisibleToPublicKeys(Me.Name)

            'EDMAURER look for one that works, if none work, then return the failure for the last one examined.
            For Each key In publicKeys
                If result <> IVTConclusion.Match Then
                    result = PerformIVTCheck(key, potentialGiverOfAccess.Identity)
                End If
            Next

            AssembliesToWhichInternalAccessHasBeenDetermined.TryAdd(potentialGiverOfAccess, result)
            Return result
        End Function

        'EDMAURER This is a cache mapping from assemblies which we have analyzed whether or not they grant
        'internals access to us to the conclusion reached.
        Private m_AssembliesToWhichInternalAccessHasBeenAnalyzed As ConcurrentDictionary(Of AssemblySymbol, IVTConclusion)

        Private ReadOnly Property AssembliesToWhichInternalAccessHasBeenDetermined As ConcurrentDictionary(Of AssemblySymbol, IVTConclusion)
            Get
                If m_AssembliesToWhichInternalAccessHasBeenAnalyzed Is Nothing Then
                    Interlocked.CompareExchange(m_AssembliesToWhichInternalAccessHasBeenAnalyzed, New ConcurrentDictionary(Of AssemblySymbol, IVTConclusion), Nothing)
                End If
                Return m_AssembliesToWhichInternalAccessHasBeenAnalyzed
            End Get
        End Property

    End Class
End Namespace

