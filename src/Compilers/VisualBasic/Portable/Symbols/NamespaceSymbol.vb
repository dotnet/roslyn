' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents a namespace.
    ''' </summary>
    Friend MustInherit Class NamespaceSymbol
        Inherits NamespaceOrTypeSymbol
        Implements INamespaceSymbol, INamespaceSymbolInternal

        ' !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        ' Changes to the public interface of this class should remain synchronized with the C# version.
        ' Do not make any changes to the public interface without making the corresponding change
        ' to the C# version.
        ' !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        ''' <summary>
        ''' Get all the members of this symbol that are namespaces.
        ''' </summary>
        ''' <returns>An ImmutableArray containing all the namespaces that are members of this symbol. If this symbol has no namespace members,
        ''' returns an empty ImmutableArray. Never returns Nothing.</returns>
        Public Overridable Function GetNamespaceMembers() As IEnumerable(Of NamespaceSymbol)
            Return Me.GetMembers().OfType(Of NamespaceSymbol)()
        End Function

        ''' <summary>
        ''' Get all the members of this symbol that are modules.
        ''' </summary>
        ''' <returns>An ImmutableArray containing all the types that are members of this namespace. If this namespace has no module members,
        ''' returns an empty ImmutableArray. Never returns Nothing.</returns>
        Public MustOverride Function GetModuleMembers() As ImmutableArray(Of NamedTypeSymbol)

        ''' <summary>
        ''' Get all the members of this symbol that are modules that have a particular name
        ''' </summary>
        ''' <returns>An ImmutableArray containing all the modules that are members of this namespace with the given name. 
        ''' If this symbol has no modules with this name,
        ''' returns an empty ImmutableArray. Never returns Nothing.</returns>
        Public Overridable Function GetModuleMembers(name As String) As ImmutableArray(Of NamedTypeSymbol)
            ' default implementation does a post-filter. We can override this if its a performance burden, but 
            ' experience is that it won't be.
            Return GetTypeMembers(name).WhereAsArray(Function(t) t.TypeKind = TypeKind.Module)
        End Function

        ''' <summary>
        ''' Returns whether this namespace is the unnamed, global namespace that is 
        ''' at the root of all namespaces.
        ''' </summary>
        Public Overridable ReadOnly Property IsGlobalNamespace As Boolean Implements INamespaceSymbol.IsGlobalNamespace, INamespaceSymbolInternal.IsGlobalNamespace
            Get
                Return ContainingNamespace Is Nothing
            End Get
        End Property

        Friend MustOverride ReadOnly Property Extent As NamespaceExtent

        ''' <summary>
        ''' The kind of namespace: Module, Assembly or Compilation.
        ''' Module namespaces contain only members from the containing module that share the same namespace name.
        ''' Assembly namespaces contain members for all modules in the containing assembly that share the same namespace name.
        ''' Compilation namespaces contain all members, from source or referenced metadata (assemblies and modules) that share the same namespace name.
        ''' </summary>
        Public ReadOnly Property NamespaceKind As NamespaceKind
            Get
                Return Me.Extent.Kind
            End Get
        End Property

        ''' <summary>
        ''' The containing compilation for compilation namespaces.
        ''' </summary>
        Public ReadOnly Property ContainingCompilation As VisualBasicCompilation
            Get
                Return If(Me.NamespaceKind = NamespaceKind.Compilation, Me.Extent.Compilation, Nothing)
            End Get
        End Property

        ''' <summary>
        ''' If a namespace has Assembly or Compilation extent, it may be composed of multiple
        ''' namespaces that are merged together. If so, ConstituentNamespaces returns
        ''' all the namespaces that were merged. If this namespace was not merged, returns
        ''' an array containing only this namespace.
        ''' </summary>
        Public Overridable ReadOnly Property ConstituentNamespaces As ImmutableArray(Of NamespaceSymbol)
            Get
                Return ImmutableArray.Create(Of NamespaceSymbol)(Me)
            End Get
        End Property

        ''' <summary>
        ''' Containing assembly.
        ''' </summary>
        Public MustOverride Overrides ReadOnly Property ContainingAssembly As AssemblySymbol

        Public NotOverridable Overrides ReadOnly Property ContainingType As NamedTypeSymbol
            Get
                Return Nothing
            End Get
        End Property

        ''' <summary>
        ''' Containing module.
        ''' </summary>
        Public Overrides ReadOnly Property ContainingModule As ModuleSymbol
            Get
                Dim extent = Me.Extent

                If extent.Kind = NamespaceKind.Module Then
                    Return extent.Module
                Else
                    Return Nothing
                End If
            End Get
        End Property

        ''' <summary>
        ''' Gets the kind of this symbol.
        ''' </summary>
        Public NotOverridable Overrides ReadOnly Property Kind As SymbolKind
            Get
                Return SymbolKind.Namespace
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return Me.IsGlobalNamespace
            End Get
        End Property

        ''' <summary>
        ''' Implements visitor pattern.
        ''' </summary>
        Friend Overrides Function Accept(Of TArgument, TResult)(visitor As VisualBasicSymbolVisitor(Of TArgument, TResult), arg As TArgument) As TResult
            Return visitor.VisitNamespace(Me, arg)
        End Function

        ' Only the compiler can create namespace symbols.
        Friend Sub New()
        End Sub

        ''' <summary>
        ''' Get this accessibility that was declared on this symbol. For symbols that do
        ''' not have accessibility declared on them, returns NotApplicable.
        ''' </summary>
        Public NotOverridable Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Accessibility.Public
            End Get
        End Property

        ''' <summary>
        ''' Returns data decoded from Obsolete attribute or null if there is no Obsolete attribute.
        ''' This property returns ObsoleteAttributeData.Uninitialized if attribute arguments haven't been decoded yet.
        ''' </summary>
        Friend NotOverridable Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Nothing
            End Get
        End Property

        ''' <summary>
        ''' Returns declared accessibility of most accessible type within this namespace or within a containing namespace recursively.
        ''' Valid return values:
        '''     Friend,
        '''     Public,
        '''     NotApplicable - if there are no types.
        ''' </summary>
        Friend MustOverride ReadOnly Property DeclaredAccessibilityOfMostAccessibleDescendantType As Accessibility

        ''' <summary>
        ''' Calculate declared accessibility of most accessible type within this namespace or within a containing namespace recursively.
        ''' Expected to be called at most once per namespace symbol, unless there is a race condition.
        ''' 
        ''' Valid return values:
        '''     Friend,
        '''     Public,
        '''     NotApplicable - if there are no types.
        ''' </summary>
        Protected Overridable Function GetDeclaredAccessibilityOfMostAccessibleDescendantType() As Accessibility

            Dim result As Accessibility = Accessibility.NotApplicable

            ' First, iterate through types within this namespace.
            For Each typeMember In Me.GetTypeMembersUnordered()
                Select Case typeMember.DeclaredAccessibility
                    Case Accessibility.Public
                        Return Accessibility.Public

                    Case Else
                        result = Accessibility.Friend

                End Select
            Next

            ' Now, iterate through child namespaces
            For Each member In Me.GetMembersUnordered()
                If member.Kind = SymbolKind.Namespace Then
                    Dim [namespace] = DirectCast(member, NamespaceSymbol)
                    Dim childResult As Accessibility = [namespace].DeclaredAccessibilityOfMostAccessibleDescendantType

                    If childResult > result Then
                        If childResult = Accessibility.Public Then
                            Return Accessibility.Public
                        End If

                        result = childResult
                    End If
                End If
            Next

            Return result
        End Function

        ''' <summary>
        ''' Returns true if namespace contains types accessible from the target assembly.
        ''' </summary>
        Friend Overridable Function ContainsTypesAccessibleFrom(fromAssembly As AssemblySymbol) As Boolean
            Dim accessibility = Me.DeclaredAccessibilityOfMostAccessibleDescendantType

            If accessibility = Accessibility.Public Then
                Return True
            End If

            If accessibility = Accessibility.Friend Then
                Dim containingAssembly = Me.ContainingAssembly

                Return (containingAssembly IsNot Nothing) AndAlso AccessCheck.HasFriendAccessTo(fromAssembly, containingAssembly)
            End If

            Return False
        End Function

        ''' <summary>
        ''' Returns true if this symbol is "shared"; i.e., declared with the "Shared"
        ''' modifier or implicitly always shared.
        ''' </summary>
        Public NotOverridable Overrides ReadOnly Property IsShared As Boolean
            Get
                Return True
            End Get
        End Property

        ''' <summary>
        ''' Lookup a nested namespace.
        ''' </summary>
        ''' <param name="names">
        ''' Sequence of names for nested child namespaces.
        ''' </param>
        ''' <returns>
        ''' Symbol for the most nested namespace, if found. Nothing 
        ''' if namespace or any part of it can not be found.
        ''' </returns>
        ''' <remarks></remarks>
        Friend Function LookupNestedNamespace(names As ImmutableArray(Of String)) As NamespaceSymbol
            Dim scope As NamespaceSymbol = Me

            For Each name As String In names

                Dim nextScope As NamespaceSymbol = Nothing

                For Each symbol As NamespaceOrTypeSymbol In scope.GetMembers(name)

                    Dim ns = TryCast(symbol, NamespaceSymbol)

                    If ns IsNot Nothing Then
                        If nextScope IsNot Nothing Then
                            Debug.Assert(nextScope Is Nothing,
                                                            "Why did we run into an unmerged namespace?")
                            nextScope = Nothing
                            Exit For
                        End If

                        nextScope = ns
                    End If
                Next

                scope = nextScope

                If scope Is Nothing Then
                    Exit For
                End If
            Next

            Return scope
        End Function

        Friend Function LookupNestedNamespace(names As String()) As NamespaceSymbol
            Return LookupNestedNamespace(names.AsImmutableOrNull())
        End Function

        ''' <summary>
        ''' Lookup an immediately nested type referenced from metadata, names should be
        ''' compared case-sensitively.
        ''' </summary>
        ''' <param name="fullEmittedName">
        ''' Full type name possibly with generic name mangling.
        ''' </param>
        ''' <returns>
        ''' Symbol for the type, or Nothing if the type isn't found.
        ''' </returns>
        ''' <remarks></remarks>
        Friend Overridable Function LookupMetadataType(ByRef fullEmittedName As MetadataTypeName) As NamedTypeSymbol
            Debug.Assert(Not fullEmittedName.IsNull)

            Dim namedType As NamedTypeSymbol = Nothing

            ' Because namespaces are merged case-insensitively,
            ' we need to make sure that we have a match for
            ' full emitted name of the type.
            Dim qualifiedName As String = Me.ToDisplayString(SymbolDisplayFormat.QualifiedNameOnlyFormat)

            Dim typeMembers As ImmutableArray(Of NamedTypeSymbol)

            If fullEmittedName.IsMangled Then
                Debug.Assert(Not fullEmittedName.UnmangledTypeName.Equals(fullEmittedName.TypeName) AndAlso fullEmittedName.InferredArity > 0)

                If fullEmittedName.ForcedArity = -1 OrElse fullEmittedName.ForcedArity = fullEmittedName.InferredArity Then

                    ' Let's handle mangling case first.
                    typeMembers = Me.GetTypeMembers(fullEmittedName.UnmangledTypeName)

                    For Each named In typeMembers
                        If fullEmittedName.InferredArity = named.Arity AndAlso named.MangleName AndAlso
                           String.Equals(named.Name, fullEmittedName.UnmangledTypeName, StringComparison.Ordinal) AndAlso
                           String.Equals(fullEmittedName.NamespaceName, If(named.GetEmittedNamespaceName(), qualifiedName), StringComparison.Ordinal) Then

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
                Debug.Assert(fullEmittedName.UnmangledTypeName Is fullEmittedName.TypeName AndAlso fullEmittedName.InferredArity = 0)
            End If

            ' Now try lookup without removing generic arity mangling.
            Dim forcedArity As Integer = fullEmittedName.ForcedArity

            If fullEmittedName.UseCLSCompliantNameArityEncoding Then
                ' Only types with arity 0 are acceptable, we already examined types with mangled names.
                If fullEmittedName.InferredArity > 0 Then
                    GoTo Done
                ElseIf forcedArity = -1 Then
                    forcedArity = 0
                ElseIf forcedArity <> 0 Then
                    GoTo Done
                Else
                    Debug.Assert(forcedArity = fullEmittedName.InferredArity)
                End If
            End If

            typeMembers = Me.GetTypeMembers(fullEmittedName.TypeName)

            For Each named In typeMembers
                ' If the name of the type must include generic mangling, it cannot be our match.
                If Not named.MangleName AndAlso (forcedArity = -1 OrElse forcedArity = named.Arity) AndAlso
                   String.Equals(named.Name, fullEmittedName.TypeName, StringComparison.Ordinal) AndAlso
                   String.Equals(fullEmittedName.NamespaceName, If(named.GetEmittedNamespaceName(), qualifiedName), StringComparison.Ordinal) Then

                    If namedType IsNot Nothing Then
                        ' ambiguity
                        namedType = Nothing
                        Exit For
                    End If

                    namedType = named
                End If
            Next

Done:
            Return namedType
        End Function

        Friend Overrides Function IsDefinedInSourceTree(tree As SyntaxTree, definedWithinSpan As TextSpan?, Optional cancellationToken As CancellationToken = Nothing) As Boolean
            If IsGlobalNamespace Then
                ' Every source file defines the global name space. This is a more efficient implementation.
                Return True
            Else
                Return MyBase.IsDefinedInSourceTree(tree, definedWithinSpan, cancellationToken)
            End If
        End Function

        Friend Function GetNestedNamespace(name As String) As NamespaceSymbol
            For Each sym In Me.GetMembers(name)
                If sym.Kind = SymbolKind.Namespace Then
                    Return DirectCast(sym, NamespaceSymbol)
                End If
            Next
            Return Nothing
        End Function

        Friend Function GetNestedNamespace(name As NameSyntax) As NamespaceSymbol
            Select Case name.Kind
                Case SyntaxKind.IdentifierName
                    Return Me.GetNestedNamespace(DirectCast(name, IdentifierNameSyntax).Identifier.ValueText)
                Case SyntaxKind.QualifiedName
                    Dim qn = DirectCast(name, QualifiedNameSyntax)
                    Dim leftNs = Me.GetNestedNamespace(qn.Left)
                    If leftNs IsNot Nothing Then
                        Return leftNs.GetNestedNamespace(qn.Right)
                    End If
            End Select

            Return Nothing
        End Function

        Friend Overridable Function IsDeclaredInSourceModule([module] As ModuleSymbol) As Boolean
            Return Me.ContainingModule Is [module]
        End Function

        ''' <summary>
        ''' This is an entry point for the Binder to collect extension methods with the given name 
        ''' declared within this (compilation merged or module level) namespace, so that methods 
        ''' from the same type are grouped together. 
        ''' </summary>
        Friend MustOverride Sub AppendProbableExtensionMethods(name As String, methods As ArrayBuilder(Of MethodSymbol))

        ''' <summary>
        ''' This is an entry point for the Binder. Its purpose is to add names of viable extension methods declared 
        ''' in this (compilation merged or module level) namespace to nameSet parameter.
        ''' </summary>
        Friend Overridable Overloads Sub AddExtensionMethodLookupSymbolsInfo(nameSet As LookupSymbolsInfo,
                                                                              options As LookupOptions,
                                                                              originalBinder As Binder)
            AddExtensionMethodLookupSymbolsInfo(nameSet, options, originalBinder, appendThrough:=Me)
        End Sub

        ''' <summary>
        ''' Add names of viable extension methods declared in this (compilation merged or module level) 
        ''' namespace to nameSet parameter.
        ''' 
        ''' The 'appendThrough' parameter allows RetargetingNamespaceSymbol to delegate majority of the work 
        ''' to the underlying namespace symbol, but still perform viability check on RetargetingMethodSymbol.
        ''' </summary>
        Friend MustOverride Overloads Sub AddExtensionMethodLookupSymbolsInfo(
            nameSet As LookupSymbolsInfo,
            options As LookupOptions,
            originalBinder As Binder,
            appendThrough As NamespaceSymbol)

        ''' <summary>
        ''' Populate the map with all probable extension methods declared within this namespace, so that methods from
        ''' the same type were grouped together within each bucket. 
        ''' </summary>
        Friend Overridable Sub BuildExtensionMethodsMap(map As Dictionary(Of String, ArrayBuilder(Of MethodSymbol)))
            For Each containedType As NamedTypeSymbol In Me.TypesToCheckForExtensionMethods
                containedType.BuildExtensionMethodsMap(map, appendThrough:=Me)
            Next
        End Sub

        ''' <summary>
        ''' Gets all extension methods in this namespace given a method's name. 
        ''' </summary>
        Friend Overridable Sub GetExtensionMethods(methods As ArrayBuilder(Of MethodSymbol), name As String)
            For Each containedType As NamedTypeSymbol In Me.TypesToCheckForExtensionMethods
                containedType.GetExtensionMethods(methods, appendThrough:=Me, Name:=name)
            Next
        End Sub

        ''' <summary>
        ''' Return the set of types that should be checked for presence of extension methods in order to build
        ''' a map of extension methods for the namespace. 
        ''' </summary>
        Friend MustOverride ReadOnly Property TypesToCheckForExtensionMethods As ImmutableArray(Of NamedTypeSymbol)

        ''' <summary>
        ''' Populate the map with all probable extension methods in membersByName parameter.
        ''' 
        ''' Returns True if an extension method was appended, False otherwise.
        ''' </summary>
        Friend Function BuildExtensionMethodsMap(
            map As Dictionary(Of String, ArrayBuilder(Of MethodSymbol)),
            membersByName As IEnumerable(Of KeyValuePair(Of String, ImmutableArray(Of Symbol)))
        ) As Boolean
            Dim result As Boolean = False

            For Each pair As KeyValuePair(Of String, ImmutableArray(Of Symbol)) In membersByName
                Dim bucket As ArrayBuilder(Of MethodSymbol) = Nothing

                For Each member As Symbol In pair.Value
                    If member.Kind = SymbolKind.Method Then
                        Dim method = DirectCast(member, MethodSymbol)

                        If method.MayBeReducibleExtensionMethod Then

                            If bucket Is Nothing AndAlso Not map.TryGetValue(method.Name, bucket) Then
                                bucket = ArrayBuilder(Of MethodSymbol).GetInstance()
                                map.Add(pair.Key, bucket)
                            End If

                            BuildExtensionMethodsMapBucket(bucket, method)
                            result = True
                        End If
                    End If
                Next
            Next

            Return result
        End Function

        Friend Sub AddMemberIfExtension(bucket As ArrayBuilder(Of MethodSymbol), member As Symbol)
            If member.Kind = SymbolKind.Method Then
                Dim method = DirectCast(member, MethodSymbol)

                If method.MayBeReducibleExtensionMethod Then
                    BuildExtensionMethodsMapBucket(bucket, method)
                End If
            End If
        End Sub

        ''' <summary>
        ''' This method is overridden by RetargetingNamespaceSymbol and allows it to delegate majority of the work 
        ''' to the underlying namespace symbol, but still retarget method symbols before they are added to the map
        ''' of extension methods.
        ''' </summary>
        Friend Overridable Sub BuildExtensionMethodsMapBucket(bucket As ArrayBuilder(Of MethodSymbol), method As MethodSymbol)
            bucket.Add(method)
        End Sub

#Region "INamespaceSymbol"

        Private Function INamespaceSymbol_GetMembers() As IEnumerable(Of INamespaceOrTypeSymbol) Implements INamespaceSymbol.GetMembers
            Return Me.GetMembers().OfType(Of INamespaceOrTypeSymbol)()
        End Function

        Private Function INamespaceSymbol_GetMembers(name As String) As IEnumerable(Of INamespaceOrTypeSymbol) Implements INamespaceSymbol.GetMembers
            Return Me.GetMembers(name).OfType(Of INamespaceOrTypeSymbol)()
        End Function

        Private Sub INamespaceSymbol_GetMembers(Of TArg)(name As String, callback As Action(Of INamespaceOrTypeSymbol, TArg), argument As TArg) Implements INamespaceSymbol.GetMembers
            For Each member In DirectCast(Me, INamespaceSymbol).GetMembers(name)
                callback(member, argument)
            Next
        End Sub

        Private Function INamespaceSymbol_GetNamespaceMembers() As IEnumerable(Of INamespaceSymbol) Implements INamespaceSymbol.GetNamespaceMembers
            Return Me.GetNamespaceMembers()
        End Function

        Public Overrides Sub Accept(visitor As SymbolVisitor)
            visitor.VisitNamespace(Me)
        End Sub

        Public Overrides Function Accept(Of TResult)(visitor As SymbolVisitor(Of TResult)) As TResult
            Return visitor.VisitNamespace(Me)
        End Function

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As SymbolVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNamespace(Me, argument)
        End Function

        Public Overrides Sub Accept(visitor As VisualBasicSymbolVisitor)
            visitor.VisitNamespace(Me)
        End Sub

        Public Overrides Function Accept(Of TResult)(visitor As VisualBasicSymbolVisitor(Of TResult)) As TResult
            Return visitor.VisitNamespace(Me)
        End Function

        Private ReadOnly Property INamespaceSymbol_ConstituentNamespaces As ImmutableArray(Of INamespaceSymbol) Implements INamespaceSymbol.ConstituentNamespaces
            Get
                Return StaticCast(Of INamespaceSymbol).From(Me.ConstituentNamespaces)
            End Get
        End Property

        Private ReadOnly Property INamespaceSymbol_NamespaceKind As NamespaceKind Implements INamespaceSymbol.NamespaceKind
            Get
                Return Me.NamespaceKind
            End Get
        End Property

        Private ReadOnly Property INamespaceSymbol_ContainingCompilation As Compilation Implements INamespaceSymbol.ContainingCompilation
            Get
                Return Me.ContainingCompilation
            End Get
        End Property

#End Region
    End Class
End Namespace
