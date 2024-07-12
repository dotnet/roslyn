' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents either a namespace or a type.
    ''' </summary>
    Friend MustInherit Class NamespaceOrTypeSymbol
        Inherits Symbol
        Implements INamespaceOrTypeSymbol, INamespaceOrTypeSymbolInternal

        ' !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        ' Changes to the public interface of this class should remain synchronized with the C# version.
        ' Do not make any changes to the public interface without making the corresponding change
        ' to the C# version.
        ' !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        ''' <summary>
        ''' Returns true if this symbol is a namespace. If its not a namespace, it must be a type.
        ''' </summary>
        Public ReadOnly Property IsNamespace As Boolean Implements INamespaceOrTypeSymbol.IsNamespace
            Get
                Return Kind = SymbolKind.Namespace
            End Get
        End Property

        ''' <summary>
        ''' Returns true if this symbols is a type. Equivalent to Not IsNamespace.
        ''' </summary>
        Public ReadOnly Property IsType As Boolean Implements INamespaceOrTypeSymbol.IsType
            Get
                Return Not IsNamespace
            End Get
        End Property

        ''' <summary>
        ''' Get all the members of this symbol.
        ''' </summary>
        ''' <returns>An ImmutableArray containing all the members of this symbol. If this symbol has no members,
        ''' returns an empty ImmutableArray. Never returns Nothing.</returns>
        Public MustOverride Function GetMembers() As ImmutableArray(Of Symbol)

        ''' <summary>
        ''' Get all the members of this symbol. The members may not be in a particular order, and the order
        ''' may not be stable from call-to-call.
        ''' </summary>
        ''' <returns>An ImmutableArray containing all the members of this symbol. If this symbol has no members,
        ''' returns an empty ImmutableArray. Never returns null.</returns>
        Friend Overridable Function GetMembersUnordered() As ImmutableArray(Of Symbol)

            '' Default implementation Is to use ordered version. When performance indicates, we specialize to have
            '' separate implementation.

            Return GetMembers().ConditionallyDeOrder()
        End Function

        ''' <summary>
        ''' Get all the members of this symbol that have a particular name.
        ''' </summary>
        ''' <returns>An ImmutableArray containing all the members of this symbol with the given name. If there are
        ''' no members with this name, returns an empty ImmutableArray. The result is deterministic (i.e. the same
        ''' from call to call and from compilation to compilation). Members of the same kind appear in the result
        ''' in the same order in which they appeared at their origin (metadata or source).
        ''' Never returns Nothing.</returns>
        Public MustOverride Function GetMembers(name As String) As ImmutableArray(Of Symbol)

        ''' <summary>
        ''' Get all the type members of this symbol. The types may not be in a particular order, and the order
        ''' may not be stable from call-to-call.
        ''' </summary>
        ''' <returns>An ImmutableArray containing all the type members of this symbol. If this symbol has no type members,
        ''' returns an empty ImmutableArray. Never returns null.</returns>
        Friend Overridable Function GetTypeMembersUnordered() As ImmutableArray(Of NamedTypeSymbol)

            '' Default implementation Is to use ordered version. When performance indicates, we specialize to have
            '' separate implementation.

            Return GetTypeMembers().ConditionallyDeOrder()
        End Function

        ''' <summary>
        ''' Get all the members of this symbol that are types.
        ''' </summary>
        ''' <returns>An ImmutableArray containing all the types that are members of this symbol. If this symbol has no type members,
        ''' returns an empty ImmutableArray. Never returns Nothing.</returns>
        Public MustOverride Function GetTypeMembers() As ImmutableArray(Of NamedTypeSymbol)

        ''' <summary>
        ''' Get all the members of this symbol that are types that have a particular name, and any arity.
        ''' </summary>
        ''' <returns>An ImmutableArray containing all the types that are members of this symbol with the given name. 
        ''' If this symbol has no type members with this name,
        ''' returns an empty ImmutableArray. Never returns Nothing.</returns>
        Public MustOverride Function GetTypeMembers(name As String) As ImmutableArray(Of NamedTypeSymbol)

        ''' <summary>
        ''' Get all the members of this symbol that are types that have a particular name and arity.
        ''' </summary>
        ''' <returns>An ImmutableArray containing all the types that are members of this symbol with the given name and arity.
        ''' If this symbol has no type members with this name and arity,
        ''' returns an empty ImmutableArray. Never returns Nothing.</returns>
        Public Overridable Function GetTypeMembers(name As String, arity As Integer) As ImmutableArray(Of NamedTypeSymbol)
            ' default implementation does a post-filter. We can override this if its a performance burden, but 
            ' experience is that it won't be.
            Return GetTypeMembers(name).WhereAsArray(predicate:=Function(type, arity_) type.Arity = arity_, arg:=arity)
        End Function

        ' Only the compiler can create new instances.
        Friend Sub New()
        End Sub

        ''' <summary>
        ''' Returns true if this symbol was declared as requiring an override; i.e., declared
        ''' with the "MustOverride" modifier. Never returns true for types. 
        ''' </summary>
        ''' <returns>
        ''' Always returns False.
        ''' </returns>
        Public NotOverridable Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Returns true if this symbol was declared to override a base class members and was
        ''' also restricted from further overriding; i.e., declared with the "NotOverridable"
        ''' modifier. Never returns true for types.
        ''' </summary>
        ''' <returns>
        ''' Always returns False.
        ''' </returns>
        Public NotOverridable Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Returns true if this member is overridable, has an implementation,
        ''' and does not override a base class member; i.e., declared with the "Overridable"
        ''' modifier. Does not return true for members declared as MustOverride or Overrides.
        ''' </summary>
        ''' <returns>
        ''' Always returns False.
        ''' </returns>
        Public NotOverridable Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Returns true if this symbol was declared to override a base class members; i.e., declared
        ''' with the "Overrides" modifier. Still returns true if the members was declared
        ''' to override something, but (erroneously) no member to override exists.
        ''' </summary>
        ''' <returns>
        ''' Always returns False.
        ''' </returns>
        Public NotOverridable Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' This is a helper method shared between NamedTypeSymbol and NamespaceSymbol.
        ''' 
        ''' Its purpose is to add names of probable extension methods found in membersByName parameter
        ''' to nameSet parameter. Method's viability check is delegated to overridable method
        ''' AddExtensionMethodLookupSymbolsInfoViabilityCheck, which is overridden by RetargetingNamedtypeSymbol
        ''' and RetargetingNamespaceSymbol in order to perform the check on corresponding RetargetingMethodSymbol.
        ''' 
        ''' Returns true if there were extension methods among the members, 
        ''' regardless whether their names were added into the set. 
        ''' </summary>
        Friend Function AddExtensionMethodLookupSymbolsInfo(
            nameSet As LookupSymbolsInfo,
            options As LookupOptions,
            originalBinder As Binder,
            membersByName As IEnumerable(Of KeyValuePair(Of String, ImmutableArray(Of Symbol)))
        ) As Boolean
            Dim haveSeenExtensionMethod As Boolean = False

            For Each pair As KeyValuePair(Of String, ImmutableArray(Of Symbol)) In membersByName

                ' TODO: Should we check whether nameSet already contains pair.Key and
                '       go to the next pair? If we do that the, haveSeenExtensionMethod == false,
                '       won't actually mean that there are no extension methods in membersByName.

                For Each member As Symbol In pair.Value
                    If member.Kind = SymbolKind.Method Then
                        Dim method = DirectCast(member, MethodSymbol)

                        If method.MayBeReducibleExtensionMethod Then
                            haveSeenExtensionMethod = True

                            If AddExtensionMethodLookupSymbolsInfoViabilityCheck(method, options, nameSet, originalBinder) Then
                                nameSet.AddSymbol(member, member.Name, member.GetArity())

                                ' Move to the next name.
                                Exit For
                            End If
                        End If
                    End If
                Next
            Next

            Return haveSeenExtensionMethod
        End Function

        ''' <summary>
        ''' Perform extension method viability check within AppendExtensionMethodNames method above.
        ''' This method is overridden by RetargetingNamedtypeSymbol and RetargetingNamespaceSymbol in order to 
        ''' perform the check on corresponding RetargetingMethodSymbol.
        ''' 
        ''' Returns true if the method is viable. 
        ''' </summary>
        Friend Overridable Function AddExtensionMethodLookupSymbolsInfoViabilityCheck(
            method As MethodSymbol,
            options As LookupOptions,
            nameSet As LookupSymbolsInfo,
            originalBinder As Binder
        ) As Boolean
            Return originalBinder.CanAddLookupSymbolInfo(method, options, nameSet, accessThroughType:=method.ContainingType)
        End Function

        ''' <summary> 
        ''' Finds types or namespaces described by a qualified name. 
        ''' </summary> 
        ''' <param name="qualifiedName">Sequence of simple plain names.</param>
        '''  <returns> A set of namespace or type symbols with given qualified name (might comprise of types with multiple generic arities),  
        ''' or an empty set if the member can't be found (the qualified name is ambiguous or the symbol doesn't exist). 
        ''' </returns> 
        ''' <remarks> 
        ''' "C.D" matches C.D, C(Of T).D, C(Of S,T).D(Of U), etc. 
        ''' </remarks>
        Friend Function GetNamespaceOrTypeByQualifiedName(qualifiedName As IEnumerable(Of String)) As IEnumerable(Of NamespaceOrTypeSymbol)
            Dim namespaceOrType As NamespaceOrTypeSymbol = Me
            Dim symbols As IEnumerable(Of NamespaceOrTypeSymbol) = Nothing
            For Each namePart In qualifiedName
                If symbols IsNot Nothing Then
                    namespaceOrType = symbols.OfMinimalArity()
                    If namespaceOrType Is Nothing Then
                        Return SpecializedCollections.EmptyEnumerable(Of NamespaceOrTypeSymbol)()
                    End If
                End If

                symbols = namespaceOrType.GetMembers(namePart).OfType(Of NamespaceOrTypeSymbol)()
            Next

            Return symbols
        End Function

#Region "INamespaceOrTypeSymbol"

        Private Function INamespaceOrTypeSymbol_GetMembers() As ImmutableArray(Of ISymbol) Implements INamespaceOrTypeSymbol.GetMembers
            Return StaticCast(Of ISymbol).From(Me.GetMembers())
        End Function

        Private Function INamespaceOrTypeSymbol_GetMembers(name As String) As ImmutableArray(Of ISymbol) Implements INamespaceOrTypeSymbol.GetMembers
            Return StaticCast(Of ISymbol).From(Me.GetMembers(name))
        End Function

        Private Function INamespaceOrTypeSymbol_GetTypeMembers() As ImmutableArray(Of INamedTypeSymbol) Implements INamespaceOrTypeSymbol.GetTypeMembers
            Return StaticCast(Of INamedTypeSymbol).From(Me.GetTypeMembers())
        End Function

        Private Function INamespaceOrTypeSymbol_GetTypeMembers(name As String) As ImmutableArray(Of INamedTypeSymbol) Implements INamespaceOrTypeSymbol.GetTypeMembers
            Return StaticCast(Of INamedTypeSymbol).From(Me.GetTypeMembers(name))
        End Function

        Public Function INamespaceOrTypeSymbol_GetTypeMembers(name As String, arity As Integer) As ImmutableArray(Of INamedTypeSymbol) Implements INamespaceOrTypeSymbol.GetTypeMembers
            Return StaticCast(Of INamedTypeSymbol).From(Me.GetTypeMembers(name, arity))
        End Function

#End Region
    End Class
End Namespace
