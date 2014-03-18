' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents a namespace.
    ''' </summary>
    Friend MustInherit Class PEOrSourceOrMergedNamespaceSymbol
        Inherits NamespaceSymbol

        ''' <summary>
        ''' For a given namespace in context of a particular Compilation all binders use 
        ''' either a compilation merged namespace symbol, or a module level namespace symbol 
        ''' (PE, Source or Retargeting). In order to speed-up lookup of extension methods performed 
        ''' by a binder, we build and cache a map of all extension methods declared within the namespace 
        ''' grouped by name (case-insensitively). 
        ''' 
        ''' If binder uses compilation merged namespace symbol, the map is built across all underlying 
        ''' module level namespace symbols, separate maps for underlying namespace symbols are not built.
        ''' 
        ''' If binder uses Retargeting module level namespace symbol, we build the map for the underlying 
        ''' namespace symbol instead and push all requests through the underlying namespace.
        ''' 
        ''' The map actually stores ImmutableArray(Of MethodSymbol), but we are using ImmutableArray(Of Symbol)
        ''' in order to be able to pass the map to a more general API.
        ''' </summary>
        Private m_lazyExtensionMethodsMap As Dictionary(Of String, ImmutableArray(Of Symbol))

        ' counter of extension method queries against this namespace until we decide to build complete map
        Private m_extQueryCnt As Integer

        Private Shared ReadOnly EmptyDictionary As New Dictionary(Of String, ImmutableArray(Of Symbol))()

        Private m_LazyDeclaredAccessibilityOfMostAccessibleDescendantType As Byte = CByte(Accessibility.Private) ' Not calculated yet.

        Friend ReadOnly Property RawLazyDeclaredAccessibilityOfMostAccessibleDescendantType As Accessibility
            Get
                Return CType(m_LazyDeclaredAccessibilityOfMostAccessibleDescendantType, Accessibility)
            End Get
        End Property

        Friend MustOverride Overrides ReadOnly Property EmbeddedSymbolKind As EmbeddedSymbolKind

        ''' <summary>
        ''' Returns declared accessibility of most accessible type within this namespace or within a containing namespace recursively.
        ''' Valid return values:
        '''     Friend,
        '''     Public,
        '''     NotApplicable - if there are no types.
        ''' </summary>
        Friend NotOverridable Overrides ReadOnly Property DeclaredAccessibilityOfMostAccessibleDescendantType As Accessibility
            Get
                If m_LazyDeclaredAccessibilityOfMostAccessibleDescendantType = Accessibility.Private Then
                    m_LazyDeclaredAccessibilityOfMostAccessibleDescendantType = CByte(GetDeclaredAccessibilityOfMostAccessibleDescendantType())

                    ' Bubble up public accessibility
                    If m_LazyDeclaredAccessibilityOfMostAccessibleDescendantType = Accessibility.Public Then
                        Dim parent = TryCast(Me.ContainingSymbol, PEOrSourceOrMergedNamespaceSymbol)

                        While parent IsNot Nothing AndAlso
                              parent.m_LazyDeclaredAccessibilityOfMostAccessibleDescendantType = Accessibility.Private

                            parent.m_LazyDeclaredAccessibilityOfMostAccessibleDescendantType = CByte(Accessibility.Public)
                            parent = TryCast(parent.ContainingSymbol, PEOrSourceOrMergedNamespaceSymbol)
                        End While
                    End If
                End If

                Return CType(m_LazyDeclaredAccessibilityOfMostAccessibleDescendantType, Accessibility)
            End Get
        End Property


        ''' <summary>
        ''' This is an entry point for the Binder to collect extension methods with the given name 
        ''' declared within this (compilation merged or module level) namespace, so that methods 
        ''' from the same type are grouped together. 
        ''' 
        ''' A cached map of extension methods is used to optimize the lookup.
        ''' </summary>
        Friend Overrides Sub AppendProbableExtensionMethods(name As String, methods As ArrayBuilder(Of MethodSymbol))
            Dim match As ImmutableArray(Of Symbol) = Nothing

            If m_lazyExtensionMethodsMap Is Nothing Then
                ' Do not force collection of all extension methods as that might be expensive
                ' unless we see a lot of traffic here
                Dim cnt = Interlocked.Increment(Me.m_extQueryCnt)

                ' 40 is a rough threshold when we consider current namespace to be popular enough to build a complete
                ' cache of extension methods. Bigger number would favor dynamic scenarios like typing (vs. static compiling)
                ' by delaying collection of all extension methods which may be unnecessary if user continues typing.
                ' 
                ' when I tested some typing scenarios in VB compiler, it seems that when this 
                ' number in 30-50 range the time spent in GetExtensionMethods and EnsureExtensionMethodsAreCollected
                ' are on the same order. Overal perf is not very sensitive to the threshold. 
                '
                ' "=" because we want only one thread to do collecting.
                If cnt = 40 Then
                    EnsureExtensionMethodsAreCollected()
                Else
                    ' get just the extension methods of the given name. 
                    GetExtensionMethods(methods, name)
                    Return
                End If
            End If

            If m_lazyExtensionMethodsMap.TryGetValue(name, match) Then
                methods.AddRange(match.As(Of MethodSymbol))
            End If
        End Sub

        ''' <summary>
        ''' Add names of viable extension methods declared in this (compilation merged or module level) 
        ''' namespace to nameSet parameter.
        ''' 
        ''' The 'appendThrough' parameter allows RetargetingNamespaceSymbol to delegate majority of the work 
        ''' to the underlying namespace symbol, but still perform viability check on RetargetingMethodSymbol.
        ''' 
        ''' A cached map of extension methods is used to optimize the operation.
        ''' </summary>
        Friend Overrides Sub AddExtensionMethodLookupSymbolsInfo(nameSet As LookupSymbolsInfo,
                                                                  options As LookupOptions,
                                                                  originalBinder As Binder,
                                                                  appendThrough As NamespaceSymbol)
            EnsureExtensionMethodsAreCollected()
            appendThrough.AddExtensionMethodLookupSymbolsInfo(nameSet, options, originalBinder, m_lazyExtensionMethodsMap)
        End Sub

        ''' <summary>
        ''' Build and cache a map of probable extension methods for this namespace.
        ''' </summary>
        Private Sub EnsureExtensionMethodsAreCollected()
            If m_lazyExtensionMethodsMap Is Nothing Then
                Dim map As New Dictionary(Of String, ArrayBuilder(Of MethodSymbol))(CaseInsensitiveComparison.Comparer)
                BuildExtensionMethodsMap(map)

                If map.Count = 0 Then
                    m_lazyExtensionMethodsMap = EmptyDictionary
                Else
                    Dim extensionMethods As New Dictionary(Of String, ImmutableArray(Of Symbol))(map.Count, CaseInsensitiveComparison.Comparer)

                    For Each pair As KeyValuePair(Of String, ArrayBuilder(Of MethodSymbol)) In map
                        extensionMethods.Add(pair.Key, StaticCast(Of Symbol).From(pair.Value.ToImmutableAndFree()))
                    Next

                    m_lazyExtensionMethodsMap = extensionMethods
                End If
            End If
        End Sub

    End Class

End Namespace