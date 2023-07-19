' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Concurrent
Imports System.Collections.ObjectModel
Imports System.Reflection
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' A <see cref="NonMissingAssemblySymbol"/> is a special kind of <see cref="AssemblySymbol"/> that represents
    ''' an assembly that is not missing, i.e. the "real" thing.
    ''' </summary>
    Friend MustInherit Class NonMissingAssemblySymbol
        Inherits AssemblySymbol

        ''' <summary>
        ''' This is a cache similar to the one used by MetaImport::GetTypeByName
        ''' in native compiler. The difference is that native compiler pre-populates 
        ''' the cache when it loads types. Here we are populating the cache only
        ''' with things we looked for, so that next time we are looking for the same 
        ''' thing, the lookup is fast. This cache also takes care of TypeForwarders. 
        ''' Gives about 8% win on subsequent lookups in some scenarios.     
        ''' </summary>
        ''' <remarks></remarks>
        Private ReadOnly _emittedNameToTypeMap As New ConcurrentDictionary(Of MetadataTypeName.Key, NamedTypeSymbol)()

        ''' <summary>
        ''' The global namespace symbol. Lazily populated on first access.
        ''' </summary>
        Private _lazyGlobalNamespace As NamespaceSymbol

        ''' <summary>
        ''' Does this symbol represent a missing assembly.
        ''' </summary>
        Friend NotOverridable Overrides ReadOnly Property IsMissing As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Gets the merged root namespace that contains all namespaces and types defined in the modules
        ''' of this assembly. If there is just one module in this assembly, this property just returns the 
        ''' GlobalNamespace of that module.
        ''' </summary>
        Public NotOverridable Overrides ReadOnly Property GlobalNamespace As NamespaceSymbol
            Get
                If _lazyGlobalNamespace Is Nothing Then
                    Interlocked.CompareExchange(_lazyGlobalNamespace, MergedNamespaceSymbol.CreateGlobalNamespace(Me), Nothing)
                End If

                Return _lazyGlobalNamespace
            End Get
        End Property

        ''' <summary>
        ''' Lookup a top level type referenced from metadata, names should be
        ''' compared case-sensitively.
        ''' </summary>
        ''' <param name="emittedName">
        ''' Full type name, possibly with generic name mangling.
        ''' </param>
        Friend NotOverridable Overrides Function LookupDeclaredTopLevelMetadataType(ByRef emittedName As MetadataTypeName) As NamedTypeSymbol

            Dim result As NamedTypeSymbol = Nothing

            ' This is a cache similar to the one used by MetaImport::GetTypeByName
            ' in native compiler. The difference is that native compiler pre-populates 
            ' the cache when it loads types. Here we are populating the cache only
            ' with things we looked for, so that next time we are looking for the same 
            ' thing, the lookup is fast. This cache also takes care of TypeForwarders. 
            ' Gives about 8% win on subsequent lookups in some scenarios.     
            '    
            ' CONSIDER !!!
            '
            ' However, it is questionable how often subsequent lookup by name  is going to happen.
            ' Currently it doesn't happen for TypeDef tokens at all, for TypeRef tokens, the lookup by name 
            ' is done once and the result is cached. So, multiple lookups by name for the same type 
            ' are going to happen only in these cases:
            ' 1) Resolving GetType() in attribute application, type is encoded by name.
            ' 2) TypeRef token isn't reused within the same module, i.e. multiple TypeRefs point to the same type.
            ' 3) Different Module refers to the same type, lookup once per Module (with exception of #2).
            ' 4) Multitargeting - retargeting the type to a different version of assembly
            result = LookupTopLevelMetadataTypeInCache(emittedName)

            If result IsNot Nothing Then
                ' We only cache result equivalent to digging through type forwarders, which
                ' might produce a forwarder specific ErrorTypeSymbol. We don't want to 
                ' return that error symbol, unless digThroughForwardedTypes Is true.
                If Not result.IsErrorType() AndAlso result.ContainingAssembly Is Me Then
                    Return result
                End If

                ' According to the cache, the type wasn't found, or isn't declared in this assembly (forwarded).
                Return Nothing
            End If

            result = LookupDeclaredTopLevelMetadataTypeInModules(emittedName)
            Debug.Assert(result Is Nothing OrElse (result.ContainingAssembly Is Me AndAlso Not result.IsErrorType()))

            If result Is Nothing Then
                Return Nothing
            End If

            ' Add result of the lookup into the cache
            Return CacheTopLevelMetadataType(emittedName, result)
        End Function

        Private Function LookupDeclaredTopLevelMetadataTypeInModules(ByRef emittedName As MetadataTypeName) As NamedTypeSymbol
            ' Now we will look for the type in each module of the assembly and pick the 
            ' first type we find, this is what native VB compiler does.

            For Each [module] In Me.Modules
                Dim result As NamedTypeSymbol = [module].LookupTopLevelMetadataType(emittedName)

                If result IsNot Nothing Then
                    Return result
                End If
            Next

            Return Nothing
        End Function

        ''' <summary>
        ''' Lookup a top level type referenced from metadata, names should be
        ''' compared case-sensitively.  Detect cycles during lookup.
        ''' </summary>
        ''' <param name="emittedName">
        ''' Full type name, possibly with generic name mangling.
        ''' </param>
        ''' <param name="visitedAssemblies">
        ''' List of assemblies lookup has already visited (since type forwarding can introduce cycles).
        ''' </param>
        Friend NotOverridable Overrides Function LookupDeclaredOrForwardedTopLevelMetadataType(ByRef emittedName As MetadataTypeName, visitedAssemblies As ConsList(Of AssemblySymbol)) As NamedTypeSymbol

            Dim result As NamedTypeSymbol = Nothing

            result = LookupTopLevelMetadataTypeInCache(emittedName)

            If result IsNot Nothing Then
                Return result
            End If

            result = LookupDeclaredTopLevelMetadataTypeInModules(emittedName)
            Debug.Assert(result Is Nothing OrElse (result.ContainingAssembly Is Me AndAlso Not result.IsErrorType()))

            If result Is Nothing Then
                ' We didn't find the type
                result = TryLookupForwardedMetadataTypeWithCycleDetection(emittedName, visitedAssemblies, ignoreCase:=False)
            End If

            ' Add result of the lookup into the cache
            Return CacheTopLevelMetadataType(emittedName, If(result, New MissingMetadataTypeSymbol.TopLevel(Me.Modules(0), emittedName)))
        End Function

        Friend MustOverride Overrides Function TryLookupForwardedMetadataTypeWithCycleDetection(ByRef emittedName As MetadataTypeName, visitedAssemblies As ConsList(Of AssemblySymbol), ignoreCase As Boolean) As NamedTypeSymbol

        ''' <summary>
        ''' For test purposes only.
        ''' </summary>
        Friend Function CachedTypeByEmittedName(emittedname As String) As NamedTypeSymbol
            Dim mdName = MetadataTypeName.FromFullName(emittedname)
            Return _emittedNameToTypeMap(mdName.ToKey())
        End Function

        ''' <summary>
        ''' For test purposes only.
        ''' </summary>
        Friend ReadOnly Property EmittedNameToTypeMapCount As Integer
            Get
                Return _emittedNameToTypeMap.Count
            End Get
        End Property

        Private Function LookupTopLevelMetadataTypeInCache(
            ByRef emittedName As MetadataTypeName
        ) As NamedTypeSymbol
            Dim result As NamedTypeSymbol = Nothing

            If Me._emittedNameToTypeMap.TryGetValue(emittedName.ToKey(), result) Then
                Return result
            End If

            Return Nothing
        End Function

        Private Function CacheTopLevelMetadataType(
            ByRef emittedName As MetadataTypeName,
            result As NamedTypeSymbol
        ) As NamedTypeSymbol
            Dim result1 As NamedTypeSymbol = Nothing
            result1 = Me._emittedNameToTypeMap.GetOrAdd(emittedName.ToKey(), result)
            Debug.Assert(result1.Equals(result)) ' object identity may differ in error cases
            Return result1
        End Function

    End Class

End Namespace

