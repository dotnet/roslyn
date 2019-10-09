' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Collections.ObjectModel
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' A MergedNamespaceSymbol represents a namespace that merges the contents of two or more other
    ''' namespaces. Any sub-namespaces with the same names are also merged if they have two or more
    ''' instances.
    ''' 
    ''' Merged namespaces are used to merged the symbols from multiple metadata modules and the source "module"
    ''' into a single symbol tree that represents all the available symbols. The compiler resolves names
    ''' against Me merged set of symbols.
    ''' 
    ''' Typically there will not be very many merged namespaces in a Compilation: only the root namespaces and
    ''' namespaces that are used in multiple referenced modules. (Microsoft, System, System.Xml,
    ''' System.Diagnostics, System.Threading, ...)
    ''' </summary>
    Friend MustInherit Class MergedNamespaceSymbol
        Inherits PEOrSourceOrMergedNamespaceSymbol

        Protected ReadOnly _namespacesToMerge As ImmutableArray(Of NamespaceSymbol)
        Protected ReadOnly _containingNamespace As MergedNamespaceSymbol

        ' The cachedLookup caches results of lookups on the constituent namespaces so
        ' that subsequent lookups for the same name are much faster than having to ask
        ' each of the constituent namespaces.
        Private ReadOnly _cachedLookup As CachingDictionary(Of String, Symbol)

        ' This caches results of GetModuleMembers()
        Private _lazyModuleMembers As ImmutableArray(Of NamedTypeSymbol)

        ' This caches results of GetMembers()
        Private _lazyMembers As ImmutableArray(Of Symbol)

        Private _lazyEmbeddedKind As Integer = EmbeddedSymbolKind.Unset

        ''' <summary>
        ''' Create a possibly merged namespace symbol representing global namespace on an assembly level.
        ''' </summary>
        Public Shared Function CreateGlobalNamespace(extent As AssemblySymbol) As NamespaceSymbol
            ' Get the root namespace from each module, and merge them all together. If there is only one, 
            ' then MergedNamespaceSymbol.Create will just return that one.
            Return MergedNamespaceSymbol.Create(extent, Nothing, ConstituentGlobalNamespaces(extent).AsImmutable())
        End Function

        Private Shared Iterator Function ConstituentGlobalNamespaces(extent As AssemblySymbol) As IEnumerable(Of NamespaceSymbol)
            For Each m In extent.Modules
                Yield m.GlobalNamespace
            Next
        End Function

        ''' <summary>
        ''' Create a possibly merged namespace symbol. If only a single namespace is passed it, it is just returned directly.
        ''' If two or more namespaces are passed in, then a new merged namespace is created with the given extent and container.
        ''' </summary>
        ''' <param name="extent">The namespace extent to use, IF a merged namespace is created.</param>
        ''' <param name="containingNamespace">The containing namespace to used, IF a merged namespace is created.</param>
        ''' <param name="namespacesToMerge">One or more namespaces to merged. If just one, then it is returned.
        ''' The merged namespace symbol may hold onto the array.</param>
        ''' <returns></returns>A namespace symbol representing the merged namespace.(of /returns)
        Private Shared Function Create(extent As AssemblySymbol, containingNamespace As AssemblyMergedNamespaceSymbol, namespacesToMerge As ImmutableArray(Of NamespaceSymbol)) As NamespaceSymbol
            ' Currently, if we are just merging 1 namespace, we just return the namespace itself. This is
            ' by far the most efficient, because it means that we don't create merged namespaces (which have a fair amount
            ' of memory overhead) unless there is actual merging going on. However, it means that
            ' the child namespace of a Compilation extent namespace may be a Module extent namespace, and the containing of that
            ' module extent namespace will be another module extent namespace. This is basically no different than type members of namespaces,
            ' so it shouldn't be TOO unexpected.

            Debug.Assert(namespacesToMerge.Length <> 0)

            If namespacesToMerge.Length = 1 Then
                Dim result = namespacesToMerge(0)
                Return result
            Else
                Return New AssemblyMergedNamespaceSymbol(extent, containingNamespace, namespacesToMerge)
            End If
        End Function

        Friend Shared Function CreateForTestPurposes(extent As AssemblySymbol, namespacesToMerge As IEnumerable(Of NamespaceSymbol)) As NamespaceSymbol
            Return Create(extent, Nothing, namespacesToMerge.AsImmutable())
        End Function

        ''' <summary>
        ''' Create a possibly merged namespace symbol representing global namespace on a compilation level.
        ''' </summary>
        Public Shared Function CreateGlobalNamespace(extent As VisualBasicCompilation) As NamespaceSymbol
            ' Get the root namespace from each module, and merge them all together.
            Return MergedNamespaceSymbol.Create(extent, Nothing, ConstituentGlobalNamespaces(extent))
        End Function

        Private Shared Iterator Function ConstituentGlobalNamespaces(extent As VisualBasicCompilation) As IEnumerable(Of NamespaceSymbol)
            For Each m In extent.Assembly.Modules
                Yield m.GlobalNamespace
            Next

            For Each reference In extent.SourceModule.GetReferencedAssemblySymbols()
                For Each m In reference.Modules
                    Yield m.GlobalNamespace
                Next
            Next
        End Function

        Private Shared Function Create(extent As VisualBasicCompilation, containingNamespace As CompilationMergedNamespaceSymbol, namespacesToMerge As IEnumerable(Of NamespaceSymbol)) As NamespaceSymbol
            Dim namespaceArray = ArrayBuilder(Of NamespaceSymbol).GetInstance()
            namespaceArray.AddRange(namespacesToMerge)

            ' Currently, if we are just merging 1 namespace, we just return the namespace itself. This is
            ' by far the most efficient, because it means that we don't create merged namespaces (which have a fair amount
            ' of memory overhead) unless there is actual merging going on. However, it means that
            ' the child namespace of a Compilation extent namespace may be a Module extent namespace, and the containing of that
            ' module extent namespace will be another module extent namespace. This is basically no different than type members of namespaces,
            ' so it shouldn't be TOO unexpected.

            Debug.Assert(namespaceArray.Count <> 0)

            If (namespaceArray.Count = 1) Then
                Dim result = namespaceArray(0)
                namespaceArray.Free()
                Return result
            Else
                Return New CompilationMergedNamespaceSymbol(extent, containingNamespace, namespaceArray.ToImmutableAndFree())
            End If
        End Function

        ''' <summary>
        ''' Create a possibly merged namespace symbol (namespace group). If only a single namespace is passed it, it is just returned directly.
        ''' If two or more namespaces are passed in, then a new merged namespace is created
        ''' </summary>
        ''' <param name="namespacesToMerge">One or more namespaces to merged. If just one, then it is returned.
        ''' The merged namespace symbol may hold onto the array.</param>
        ''' <returns></returns>A namespace symbol representing the merged namespace.(of /returns)
        Public Shared Function CreateNamespaceGroup(namespacesToMerge As IEnumerable(Of NamespaceSymbol)) As NamespaceSymbol
            Return Create(NamespaceGroupSymbol.GlobalNamespace, namespacesToMerge)
        End Function

        Public Overridable Function Shrink(namespacesToMerge As IEnumerable(Of NamespaceSymbol)) As NamespaceSymbol
            Throw ExceptionUtilities.Unreachable
        End Function

        ''' <summary>
        ''' Create a possibly merged namespace symbol (namespace group). If only a single namespace is passed it, it is just returned directly.
        ''' If two or more namespaces are passed in, then a new merged namespace is created with the given extent and container.
        ''' </summary>
        ''' <param name="containingNamespace">The containing namespace to used, IF a merged namespace is created.</param>
        ''' <param name="namespacesToMerge">One or more namespaces to merged. If just one, then it is returned.
        ''' The merged namespace symbol may hold onto the array.</param>
        ''' <returns></returns>A namespace symbol representing the merged namespace.(of /returns)
        Private Shared Function Create(containingNamespace As NamespaceGroupSymbol, namespacesToMerge As IEnumerable(Of NamespaceSymbol)) As NamespaceSymbol
            Dim namespaceArray = ArrayBuilder(Of NamespaceSymbol).GetInstance()
            namespaceArray.AddRange(namespacesToMerge)

            ' Currently, if we are just merging 1 namespace, we just return the namespace itself. This is
            ' by far the most efficient, because it means that we don't create merged namespaces (which have a fair amount
            ' of memory overhead) unless there is actual merging going on. However, it means that
            ' the child namespace of a Compilation extent namespace may be a Module extent namespace, and the containing of that
            ' module extent namespace will be another module extent namespace. This is basically no different than type members of namespaces,
            ' so it shouldn't be TOO unexpected.

            Debug.Assert(namespaceArray.Count <> 0)

            If (namespaceArray.Count = 1) Then
                Dim result = namespaceArray(0)
                namespaceArray.Free()
                Return result
            Else
                Return New NamespaceGroupSymbol(containingNamespace, namespaceArray.ToImmutableAndFree())
            End If
        End Function

        ' Constructor. Use static Create method to create instances.
        Private Sub New(containingNamespace As MergedNamespaceSymbol, namespacesToMerge As ImmutableArray(Of NamespaceSymbol))
            Debug.Assert(namespacesToMerge.Distinct().Length = namespacesToMerge.Length)
            Me._namespacesToMerge = namespacesToMerge
            Me._containingNamespace = containingNamespace

            Me._cachedLookup = New CachingDictionary(Of String, Symbol)(AddressOf SlowGetChildrenOfName, AddressOf SlowGetChildNames, IdentifierComparison.Comparer)
        End Sub

        Friend Function GetConstituentForCompilation(compilation As VisualBasicCompilation) As NamespaceSymbol
            For Each constituent In _namespacesToMerge
                If constituent.IsFromCompilation(compilation) Then
                    Return constituent
                End If
            Next
            Return Nothing
        End Function

        Public Overrides ReadOnly Property ConstituentNamespaces As ImmutableArray(Of NamespaceSymbol)
            Get
                Return _namespacesToMerge
            End Get
        End Property

        ''' <summary>
        ''' Method that is called from the CachingLookup to lookup the children of a given name. Looks
        ''' in all the constituent namespaces.
        ''' </summary>
        Private Function SlowGetChildrenOfName(name As String) As ImmutableArray(Of Symbol)
            Dim nsSymbols As ArrayBuilder(Of NamespaceSymbol) = Nothing
            Dim otherSymbols = ArrayBuilder(Of Symbol).GetInstance()

            ' Accumulate all the child namespaces and types.
            For Each nsSym As NamespaceSymbol In _namespacesToMerge
                For Each childSym As Symbol In nsSym.GetMembers(name)
                    If (childSym.Kind = SymbolKind.Namespace) Then
                        nsSymbols = If(nsSymbols, ArrayBuilder(Of NamespaceSymbol).GetInstance())
                        nsSymbols.Add(DirectCast(childSym, NamespaceSymbol))
                    Else
                        otherSymbols.Add(childSym)
                    End If
                Next
            Next

            If nsSymbols IsNot Nothing Then
                otherSymbols.Add(CreateChildMergedNamespaceSymbol(nsSymbols.ToImmutableAndFree()))
            End If

            Return otherSymbols.ToImmutableAndFree()
        End Function

        Protected MustOverride Function CreateChildMergedNamespaceSymbol(nsSymbols As ImmutableArray(Of NamespaceSymbol)) As NamespaceSymbol

        ''' <summary>
        ''' Method that is called from the CachingLookup to get all child names. Looks
        ''' in all constituent namespaces.
        ''' </summary>
        Private Function SlowGetChildNames(comparer As IEqualityComparer(Of String)) As HashSet(Of String)
            Dim childNames As New HashSet(Of String)(comparer)

            For Each nsSym As NamespaceSymbol In _namespacesToMerge
                For Each childSym As NamespaceOrTypeSymbol In nsSym.GetMembersUnordered()
                    childNames.Add(childSym.Name)
                Next
            Next

            Return childNames
        End Function

        Public Overrides ReadOnly Property Name As String
            Get
                Return _namespacesToMerge(0).Name
            End Get
        End Property

        Friend Overrides ReadOnly Property EmbeddedSymbolKind As EmbeddedSymbolKind
            Get
                If _lazyEmbeddedKind = EmbeddedSymbolKind.Unset Then
                    Dim value As Integer = EmbeddedSymbolKind.None
                    For Each ns In _namespacesToMerge
                        value = value Or ns.EmbeddedSymbolKind
                    Next
                    Interlocked.CompareExchange(_lazyEmbeddedKind, value, EmbeddedSymbolKind.Unset)
                End If

                Return CType(_lazyEmbeddedKind, EmbeddedSymbolKind)
            End Get
        End Property

        ' This is very performance critical for type lookup.
        ' It's important that this NOT enumerable and instantiate all the members. Instead, we just want to return
        ' all the modules in all constituent namespaces, and cache that.
        Public Overrides Function GetModuleMembers() As ImmutableArray(Of NamedTypeSymbol)
            If _lazyModuleMembers.IsDefault Then
                Dim moduleMembers = ArrayBuilder(Of NamedTypeSymbol).GetInstance()

                ' Accumulate all the child modules.
                For Each nsSym As NamespaceSymbol In _namespacesToMerge
                    moduleMembers.AddRange(nsSym.GetModuleMembers())
                Next

                ImmutableInterlocked.InterlockedCompareExchange(_lazyModuleMembers,
                                                    moduleMembers.ToImmutableAndFree,
                                                    Nothing)
            End If

            Return _lazyModuleMembers
        End Function

        Public Overrides Function GetMembers() As ImmutableArray(Of Symbol)
            If _lazyMembers.IsDefault Then
                Dim builder = ArrayBuilder(Of Symbol).GetInstance()
                _cachedLookup.AddValues(builder)
                _lazyMembers = builder.ToImmutableAndFree()
            End If

            Return _lazyMembers
        End Function

        Public Overrides Function GetMembers(name As String) As ImmutableArray(Of Symbol)
            Return _cachedLookup(name)
        End Function

        Friend Overrides Function GetTypeMembersUnordered() As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray.CreateRange(Of NamedTypeSymbol)(GetMembersUnordered().OfType(Of NamedTypeSymbol))
        End Function

        Public Overrides Function GetTypeMembers() As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray.CreateRange(Of NamedTypeSymbol)(GetMembers().OfType(Of NamedTypeSymbol))
        End Function

        Public Overrides Function GetTypeMembers(name As String) As ImmutableArray(Of NamedTypeSymbol)
            'TODO - Perf
            Return ImmutableArray.CreateRange(Of NamedTypeSymbol)(GetMembers(name).OfType(Of NamedTypeSymbol))

        End Function

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _containingNamespace
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingAssembly As AssemblySymbol
            Get
                If Me.Extent.Kind = NamespaceKind.Module Then
                    Return Me.Extent.Module.ContainingAssembly

                ElseIf Me.Extent.Kind = NamespaceKind.Assembly Then
                    Return Me.Extent.Assembly

                Else
                    Return Nothing
                End If
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray.CreateRange(Of Location)((From ns In _namespacesToMerge, loc In ns.Locations Select loc))
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray.CreateRange(Of SyntaxReference)(From ns In _namespacesToMerge, reference In ns.DeclaringSyntaxReferences Select reference)
            End Get
        End Property

        ''' <summary>
        ''' Calculate declared accessibility of most accessible type within this namespace or within a containing namespace recursively.
        ''' Expected to be called at most once per namespace symbol, unless there is a race condition.
        ''' 
        ''' Valid return values:
        '''     Friend,
        '''     Public,
        '''     NotApplicable - if there are no types.
        ''' </summary>
        Protected NotOverridable Overrides Function GetDeclaredAccessibilityOfMostAccessibleDescendantType() As Accessibility

            Dim result As Accessibility = Accessibility.NotApplicable

            For Each nsSym As NamespaceSymbol In _namespacesToMerge
                Dim current As Accessibility = nsSym.DeclaredAccessibilityOfMostAccessibleDescendantType

                If current > result Then
                    If current = Accessibility.Public Then
                        Return Accessibility.Public
                    End If

                    result = current
                End If
            Next

            Return result
        End Function

        Friend Overrides Function IsDeclaredInSourceModule([module] As ModuleSymbol) As Boolean
            For Each nsSym As NamespaceSymbol In _namespacesToMerge
                If nsSym.IsDeclaredInSourceModule([module]) Then
                    Return True
                End If
            Next

            Return False
        End Function

        ''' <summary>
        ''' For test purposes only.
        ''' </summary>
        Friend MustOverride ReadOnly Property RawContainsAccessibleTypes As ThreeState

        Private NotInheritable Class AssemblyMergedNamespaceSymbol
            Inherits MergedNamespaceSymbol

            Private ReadOnly _assembly As AssemblySymbol

            Public Sub New(assembly As AssemblySymbol, containingNamespace As AssemblyMergedNamespaceSymbol, namespacesToMerge As ImmutableArray(Of NamespaceSymbol))
                MyBase.New(containingNamespace, namespacesToMerge)
#If DEBUG Then
                ' We shouldn't merge merged namespaces.
                For Each ns In namespacesToMerge
                    Debug.Assert(ns.ConstituentNamespaces.Length = 1)
                Next
#End If

                Debug.Assert(namespacesToMerge.Length > 0)
                Debug.Assert(containingNamespace Is Nothing OrElse containingNamespace._assembly Is assembly)
                Me._assembly = assembly
            End Sub

            Friend Overrides ReadOnly Property Extent As NamespaceExtent
                Get
                    Return New NamespaceExtent(_assembly)
                End Get
            End Property

            Protected Overrides Function CreateChildMergedNamespaceSymbol(nsSymbols As ImmutableArray(Of NamespaceSymbol)) As NamespaceSymbol
                Return MergedNamespaceSymbol.Create(_assembly, Me, nsSymbols)
            End Function

            Friend Overrides ReadOnly Property RawContainsAccessibleTypes As ThreeState
                Get
                    Throw ExceptionUtilities.Unreachable
                End Get
            End Property

            Friend Overrides Sub BuildExtensionMethodsMap(map As Dictionary(Of String, ArrayBuilder(Of MethodSymbol)))
                ' Assembly merged namespace symbols aren't used by binders.
                Throw ExceptionUtilities.Unreachable
            End Sub

            Friend Overrides ReadOnly Property TypesToCheckForExtensionMethods As ImmutableArray(Of NamedTypeSymbol)
                Get
                    ' Assembly merged namespace symbols aren't used by binders.
                    Throw ExceptionUtilities.Unreachable
                End Get
            End Property
        End Class

        Private NotInheritable Class CompilationMergedNamespaceSymbol
            Inherits MergedNamespaceSymbol

            Private ReadOnly _compilation As VisualBasicCompilation
            Private _containsAccessibleTypes As ThreeState = ThreeState.Unknown
            Private _isDeclaredInSourceModule As ThreeState = ThreeState.Unknown

            Friend Overrides ReadOnly Property RawContainsAccessibleTypes As ThreeState
                Get
                    Return _containsAccessibleTypes
                End Get
            End Property

            Public Sub New(compilation As VisualBasicCompilation, containingNamespace As CompilationMergedNamespaceSymbol, namespacesToMerge As ImmutableArray(Of NamespaceSymbol))
                MyBase.New(containingNamespace, namespacesToMerge)
#If DEBUG Then
                ' We shouldn't merge merged namespaces.
                For Each ns In namespacesToMerge
                    Debug.Assert(ns.ConstituentNamespaces.Length = 1)
                Next
#End If
                Debug.Assert(namespacesToMerge.Length > 0)
                Debug.Assert(containingNamespace Is Nothing OrElse containingNamespace._compilation Is compilation)

                Me._compilation = compilation
            End Sub

            Friend Overrides ReadOnly Property Extent As NamespaceExtent
                Get
                    Return New NamespaceExtent(_compilation)
                End Get
            End Property

            Protected Overrides Function CreateChildMergedNamespaceSymbol(nsSymbols As ImmutableArray(Of NamespaceSymbol)) As NamespaceSymbol
                Return MergedNamespaceSymbol.Create(_compilation, Me, nsSymbols)
            End Function

            ''' <summary>
            ''' Returns true if namespace contains types accessible from the target assembly.
            ''' </summary>
            Friend Overrides Function ContainsTypesAccessibleFrom(fromAssembly As AssemblySymbol) As Boolean
                ' For a compilation merged namespace we only need to support scenario when 
                ' [fromAssembly] is compilation's assembly, because this namespace symbol will never be 
                ' "imported" namespace in context of other compilation. 
                ' This allows us to do efficient caching of the result.
                Debug.Assert(fromAssembly Is _compilation.Assembly)

                If _containsAccessibleTypes.HasValue Then
                    Return _containsAccessibleTypes = ThreeState.True
                End If

                If Me.RawLazyDeclaredAccessibilityOfMostAccessibleDescendantType = Accessibility.Public Then
                    Return True
                End If

                Dim result As Boolean = False

                For Each nsSym As NamespaceSymbol In _namespacesToMerge
                    If nsSym.ContainsTypesAccessibleFrom(fromAssembly) Then
                        result = True
                        Exit For
                    End If
                Next

                If result Then
                    _containsAccessibleTypes = ThreeState.True

                    ' Bubble up the value.
                    Dim parent = TryCast(Me.ContainingSymbol, CompilationMergedNamespaceSymbol)

                    While parent IsNot Nothing AndAlso
                          parent._containsAccessibleTypes = ThreeState.Unknown

                        parent._containsAccessibleTypes = ThreeState.True
                        parent = TryCast(parent.ContainingSymbol, CompilationMergedNamespaceSymbol)
                    End While
                Else
                    _containsAccessibleTypes = ThreeState.False
                End If

                Return result
            End Function

            Friend Overrides Function IsDeclaredInSourceModule([module] As ModuleSymbol) As Boolean
                ' For a compilation merged namespace we only need to support scenario when 
                ' [module] is compilation's source module, because this namespace symbol will never be 
                ' "imported" namespace in context of other compilation. 
                ' This allows us to do efficient caching of the result.
                Debug.Assert([module] Is _compilation.SourceModule)

                If _isDeclaredInSourceModule.HasValue Then
                    Return _isDeclaredInSourceModule = ThreeState.True
                End If

                Dim result = MyBase.IsDeclaredInSourceModule([module])

                If result Then
                    _isDeclaredInSourceModule = ThreeState.True

                    ' Bubble up the value.
                    Dim parent = TryCast(Me.ContainingSymbol, CompilationMergedNamespaceSymbol)

                    While parent IsNot Nothing AndAlso
                          parent._isDeclaredInSourceModule = ThreeState.Unknown

                        parent._isDeclaredInSourceModule = ThreeState.True
                        parent = TryCast(parent.ContainingSymbol, CompilationMergedNamespaceSymbol)
                    End While
                Else
                    _isDeclaredInSourceModule = ThreeState.False
                End If

                Return result
            End Function

            ''' <summary>
            ''' Populate the map with all extension methods declared within this namespace, so that methods from
            ''' the same type are grouped together within each bucket. 
            ''' </summary>
            Friend Overrides Sub BuildExtensionMethodsMap(map As Dictionary(Of String, ArrayBuilder(Of MethodSymbol)))
                For Each nsSym As NamespaceSymbol In _namespacesToMerge
                    ' CONSIDER: Is it worth using m_lazyExtensionMethodsMap for nsSym if we've built it already for a 
                    '           different compilation?
                    nsSym.BuildExtensionMethodsMap(map)
                Next
            End Sub

            Friend Overrides Sub GetExtensionMethods(methods As ArrayBuilder(Of MethodSymbol), name As String)
                For Each nsSym As NamespaceSymbol In _namespacesToMerge
                    nsSym.GetExtensionMethods(methods, name)
                Next
            End Sub

            Friend Overrides ReadOnly Property TypesToCheckForExtensionMethods As ImmutableArray(Of NamedTypeSymbol)
                Get
                    ' We should override all callers of this function and go through implementation
                    ' provided by each individual namespace symbol.
                    Throw ExceptionUtilities.Unreachable
                End Get
            End Property
        End Class

        Private NotInheritable Class NamespaceGroupSymbol
            Inherits MergedNamespaceSymbol

            Public Shared ReadOnly GlobalNamespace As New NamespaceGroupSymbol(Nothing, ImmutableArray(Of NamespaceSymbol).Empty)

            Public Sub New(containingNamespace As NamespaceGroupSymbol, namespacesToMerge As ImmutableArray(Of NamespaceSymbol))
                MyBase.New(containingNamespace, namespacesToMerge)
#If DEBUG Then
                Dim name As String = If(namespacesToMerge.Length > 0, namespacesToMerge(0).Name, Nothing)

                For Each ns In namespacesToMerge
                    Debug.Assert(ns.NamespaceKind = NamespaceKind.Module OrElse ns.NamespaceKind = NamespaceKind.Compilation)
                    Debug.Assert(Not ns.IsGlobalNamespace)
                    Debug.Assert(IdentifierComparison.Equals(name, ns.Name))
                Next
#End If
                Debug.Assert(containingNamespace Is Nothing OrElse namespacesToMerge.Length > 0)
            End Sub

            Public Overrides ReadOnly Property Name As String
                Get
                    Return If(_namespacesToMerge.Length > 0, _namespacesToMerge(0).Name, "")
                End Get
            End Property

#If DEBUG Then
            Shared Sub New()
                ' Below is a set of compile time asserts, they break build if violated.

                ' Assert(SymbolExtensions.NamespaceKindNamespaceGroup = 0) 
                ' This is needed because we use default NamespaceExtent constructor in Extent property.
                Const assert01_1 As Integer = SymbolExtensions.NamespaceKindNamespaceGroup + Int32.MaxValue
                Const assert01_2 As Integer = SymbolExtensions.NamespaceKindNamespaceGroup + Int32.MinValue

                Dim dummy = assert01_1 + assert01_2
            End Sub
#End If

            Friend Overrides ReadOnly Property Extent As NamespaceExtent
                Get
                    Return New NamespaceExtent()
                End Get
            End Property

            Friend Overrides ReadOnly Property RawContainsAccessibleTypes As ThreeState
                Get
                    Throw ExceptionUtilities.Unreachable
                End Get
            End Property

            Friend Overrides ReadOnly Property TypesToCheckForExtensionMethods As ImmutableArray(Of NamedTypeSymbol)
                Get
                    Throw ExceptionUtilities.Unreachable
                End Get
            End Property

            Friend Overrides Sub BuildExtensionMethodsMap(map As Dictionary(Of String, ArrayBuilder(Of MethodSymbol)))
                Throw ExceptionUtilities.Unreachable
            End Sub

            Protected Overrides Function CreateChildMergedNamespaceSymbol(nsSymbols As ImmutableArray(Of NamespaceSymbol)) As NamespaceSymbol
                Return MergedNamespaceSymbol.Create(Me, nsSymbols)
            End Function

            Public Overrides Function Shrink(namespacesToMerge As IEnumerable(Of NamespaceSymbol)) As NamespaceSymbol
                Dim namespaceArray = ArrayBuilder(Of NamespaceSymbol).GetInstance()
                namespaceArray.AddRange(namespacesToMerge)

                ' Currently, if we are just merging 1 namespace, we just return the namespace itself. This is
                ' by far the most efficient, because it means that we don't create merged namespaces (which have a fair amount
                ' of memory overhead) unless there is actual merging going on. 
                Debug.Assert(namespaceArray.Count <> 0)
                If namespaceArray.Count = 0 Then
                    namespaceArray.Free()
                    Return Me
                End If

                ' New set of parts should be a subset of what we already have

                If namespaceArray.Count = 1 Then
                    Dim result = namespaceArray(0)
                    namespaceArray.Free()

                    If _namespacesToMerge.Contains(result) Then
                        Return result
                    End If

                    Throw ExceptionUtilities.Unreachable
                End If

                Dim lookup = New SmallDictionary(Of NamespaceSymbol, Boolean)()

                For Each item In _namespacesToMerge
                    lookup(item) = False
                Next

                For Each item In namespaceArray
                    If Not lookup.TryGetValue(item, Nothing) Then
                        Debug.Assert(False)
                        namespaceArray.Free()
                        Return Me
                    End If
                Next

                Return Shrink(namespaceArray.ToImmutableAndFree())
            End Function

            Private Overloads Function Shrink(namespaceArray As ImmutableArray(Of NamespaceSymbol)) As NamespaceGroupSymbol
                Debug.Assert(namespaceArray.Length < _namespacesToMerge.Length)
                If namespaceArray.Length >= _namespacesToMerge.Length Then
                    Debug.Assert(False)
                    Return Me
                End If

                If _containingNamespace Is GlobalNamespace Then
                    Return New NamespaceGroupSymbol(GlobalNamespace, namespaceArray)
                End If

                Dim parentsArray = ArrayBuilder(Of NamespaceSymbol).GetInstance(namespaceArray.Length)
                For Each item In namespaceArray
                    parentsArray.Add(item.ContainingNamespace)
                Next

                Return New NamespaceGroupSymbol(DirectCast(_containingNamespace, NamespaceGroupSymbol).Shrink(parentsArray.ToImmutableAndFree()), namespaceArray)
            End Function

        End Class
    End Class
End Namespace
