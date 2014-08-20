' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Concurrent
Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit
    Friend NotInheritable Class VisualBasicSymbolMatcher
        Inherits SymbolMatcher

        Private Shared ReadOnly NameComparer As StringComparer = IdentifierComparison.Comparer

        Private ReadOnly defs As MatchDefs
        Private ReadOnly symbols As MatchSymbols

        Public Sub New(anonymousTypeMap As IReadOnlyDictionary(Of AnonymousTypeKey, AnonymousTypeValue),
                      sourceAssembly As SourceAssemblySymbol,
                      sourceContext As EmitContext,
                      otherAssembly As SourceAssemblySymbol,
                      otherContext As EmitContext)

            Me.defs = New MatchDefsToSource(sourceContext, otherContext)
            Me.symbols = New MatchSymbols(anonymousTypeMap, sourceAssembly, otherAssembly)
        End Sub

        Public Sub New(anonymousTypeMap As IReadOnlyDictionary(Of AnonymousTypeKey, AnonymousTypeValue),
                      sourceAssembly As SourceAssemblySymbol,
                      sourceContext As EmitContext,
                      otherAssembly As PEAssemblySymbol)

            Me.defs = New MatchDefsToMetadata(sourceContext, otherAssembly)
            Me.symbols = New MatchSymbols(anonymousTypeMap, sourceAssembly, otherAssembly)
        End Sub

        Friend Function MapDefinition(def As Cci.IDefinition) As Cci.IDefinition
            Dim symbol As Symbol = TryCast(def, Symbol)
            If symbol IsNot Nothing Then
                Return DirectCast(Me.symbols.Visit(symbol), Cci.IDefinition)
            End If
            Return Me.defs.VisitDef(def)
        End Function

        Public Overrides Function MapReference(reference As Cci.ITypeReference) As Cci.ITypeReference
            Dim symbol As Symbol = TryCast(reference, Symbol)
            If symbol IsNot Nothing Then
                Return DirectCast(Me.symbols.Visit(symbol), Cci.ITypeReference)
            End If
            Return Nothing
        End Function

        Friend Function TryGetAnonymousTypeName(template As NamedTypeSymbol, <Out()> ByRef name As String, <Out()> ByRef index As Integer) As Boolean
            Return Me.symbols.TryGetAnonymousTypeName(template, name, index)
        End Function

        Private MustInherit Class MatchDefs
            Private ReadOnly sourceContext As EmitContext
            Private ReadOnly matches As ConcurrentDictionary(Of Cci.IDefinition, Cci.IDefinition)
            Private lazyTopLevelTypes As IReadOnlyDictionary(Of String, Cci.INamespaceTypeDefinition)

            Public Sub New(sourceContext As EmitContext)
                Me.sourceContext = sourceContext
                Me.matches = New ConcurrentDictionary(Of Cci.IDefinition, Cci.IDefinition)()
            End Sub

            Public Function VisitDef(def As Cci.IDefinition) As Cci.IDefinition
                Return Me.matches.GetOrAdd(def, AddressOf Me.VisitDefInternal)
            End Function

            Private Function VisitDefInternal(def As Cci.IDefinition) As Cci.IDefinition
                Dim type = TryCast(def, Cci.ITypeDefinition)
                If type IsNot Nothing Then
                    Dim namespaceType As Cci.INamespaceTypeDefinition = type.AsNamespaceTypeDefinition(Me.sourceContext)
                    If namespaceType IsNot Nothing Then
                        Return Me.VisitNamespaceType(namespaceType)
                    End If

                    Dim nestedType As Cci.INestedTypeDefinition = type.AsNestedTypeDefinition(Me.sourceContext)
                    Debug.Assert(nestedType IsNot Nothing)

                    Dim otherContainer = DirectCast(Me.VisitDef(nestedType.ContainingTypeDefinition), Cci.ITypeDefinition)
                    If otherContainer Is Nothing Then
                        Return Nothing
                    End If

                    Return Me.VisitTypeMembers(otherContainer, nestedType, AddressOf GetNestedTypes, Function(a, b) NameComparer.Equals(a.Name, b.Name))
                End If

                Dim member = TryCast(def, Cci.ITypeDefinitionMember)
                If member IsNot Nothing Then
                    Dim otherContainer = DirectCast(Me.VisitDef(member.ContainingTypeDefinition), Cci.ITypeDefinition)
                    If otherContainer Is Nothing Then
                        Return Nothing
                    End If

                    Dim field = TryCast(def, Cci.IFieldDefinition)
                    If field IsNot Nothing Then
                        Return Me.VisitTypeMembers(otherContainer, field, AddressOf GetFields, Function(a, b) NameComparer.Equals(a.Name, b.Name))
                    End If
                End If

                ' We are only expecting types and fields currently.
                Throw ExceptionUtilities.UnexpectedValue(def)
            End Function

            Protected MustOverride Function GetTopLevelTypes() As IEnumerable(Of Cci.INamespaceTypeDefinition)
            Protected MustOverride Function GetNestedTypes(def As Cci.ITypeDefinition) As IEnumerable(Of Cci.INestedTypeDefinition)
            Protected MustOverride Function GetFields(def As Cci.ITypeDefinition) As IEnumerable(Of Cci.IFieldDefinition)

            Private Function VisitNamespaceType(def As Cci.INamespaceTypeDefinition) As Cci.INamespaceTypeDefinition
                ' All generated top-level types are assumed to be in the global namespace.
                ' However, this may be an embedded NoPIA type within a namespace.
                ' Since we do not support edits that include references to NoPIA types
                ' (see #855640), it's reasonable to simply drop such cases.
                If Not String.IsNullOrEmpty(def.NamespaceName) Then
                    Return Nothing
                End If

                Dim otherDef As Cci.INamespaceTypeDefinition = Nothing
                Me.GetTopLevelTypesByName().TryGetValue(def.Name, otherDef)
                Return otherDef
            End Function

            Private Function GetTopLevelTypesByName() As IReadOnlyDictionary(Of String, Cci.INamespaceTypeDefinition)
                If Me.lazyTopLevelTypes Is Nothing Then
                    Dim typesByName As Dictionary(Of String, Cci.INamespaceTypeDefinition) = New Dictionary(Of String, Cci.INamespaceTypeDefinition)(NameComparer)
                    For Each type As Cci.INamespaceTypeDefinition In Me.GetTopLevelTypes()
                        ' All generated top-level types are assumed to be in the global namespace.
                        If String.IsNullOrEmpty(type.NamespaceName) Then
                            typesByName.Add(type.Name, type)
                        End If
                    Next
                    Interlocked.CompareExchange(Me.lazyTopLevelTypes, typesByName, Nothing)
                End If
                Return Me.lazyTopLevelTypes
            End Function

            Private Function VisitTypeMembers(Of T As {Class, Cci.ITypeDefinitionMember})(
                otherContainer As Cci.ITypeDefinition,
                member As T,
                getMembers As Func(Of Cci.ITypeDefinition, IEnumerable(Of T)),
                predicate As Func(Of T, T, Boolean)) As T

                ' We could cache the members by name (see Matcher.VisitNamedTypeMembers)
                ' but the assumption is this class is only used for types with few members
                ' so caching is not necessary and linear search is acceptable.
                Return getMembers(otherContainer).FirstOrDefault(Function(otherMember As T) predicate(member, otherMember))
            End Function
        End Class

        Private NotInheritable Class MatchDefsToMetadata
            Inherits MatchDefs

            Private ReadOnly otherAssembly As PEAssemblySymbol

            Public Sub New(sourceContext As EmitContext, otherAssembly As PEAssemblySymbol)
                MyBase.New(sourceContext)
                Me.otherAssembly = otherAssembly
            End Sub

            Protected Overrides Function GetTopLevelTypes() As IEnumerable(Of Cci.INamespaceTypeDefinition)
                Dim builder As ArrayBuilder(Of Cci.INamespaceTypeDefinition) = ArrayBuilder(Of Cci.INamespaceTypeDefinition).GetInstance()
                GetTopLevelTypes(builder, Me.otherAssembly.GlobalNamespace)
                Return builder.ToArrayAndFree()
            End Function

            Protected Overrides Function GetNestedTypes(def As Cci.ITypeDefinition) As IEnumerable(Of Cci.INestedTypeDefinition)
                Return (DirectCast(def, PENamedTypeSymbol)).GetTypeMembers().Cast(Of Cci.INestedTypeDefinition)()
            End Function

            Protected Overrides Function GetFields(def As Cci.ITypeDefinition) As IEnumerable(Of Cci.IFieldDefinition)
                Return (DirectCast(def, PENamedTypeSymbol)).GetFieldsToEmit().Cast(Of Cci.IFieldDefinition)()
            End Function

            Private Overloads Shared Sub GetTopLevelTypes(builder As ArrayBuilder(Of Cci.INamespaceTypeDefinition), [namespace] As NamespaceSymbol)
                For Each member In [namespace].GetMembers()
                    If member.Kind = SymbolKind.Namespace Then
                        GetTopLevelTypes(builder, DirectCast(member, NamespaceSymbol))
                    Else
                        builder.Add(DirectCast(member, Cci.INamespaceTypeDefinition))
                    End If
                Next
            End Sub
        End Class

        Private NotInheritable Class MatchDefsToSource
            Inherits MatchDefs

            Private ReadOnly otherContext As EmitContext

            Public Sub New(sourceContext As EmitContext, otherContext As EmitContext)
                MyBase.New(sourceContext)
                Me.otherContext = otherContext
            End Sub

            Protected Overrides Function GetTopLevelTypes() As IEnumerable(Of Cci.INamespaceTypeDefinition)
                Return Me.otherContext.Module.GetTopLevelTypes(Me.otherContext)
            End Function

            Protected Overrides Function GetNestedTypes(def As Cci.ITypeDefinition) As IEnumerable(Of Cci.INestedTypeDefinition)
                Return def.GetNestedTypes(Me.otherContext)
            End Function

            Protected Overrides Function GetFields(def As Cci.ITypeDefinition) As IEnumerable(Of Cci.IFieldDefinition)
                Return def.GetFields(Me.otherContext)
            End Function
        End Class

        Private NotInheritable Class MatchSymbols
            Inherits VisualBasicSymbolVisitor(Of Symbol)

            Private ReadOnly anonymousTypeMap As IReadOnlyDictionary(Of AnonymousTypeKey, AnonymousTypeValue)
            Private ReadOnly sourceAssembly As SourceAssemblySymbol
            Private ReadOnly otherAssembly As AssemblySymbol
            Private ReadOnly comparer As SymbolComparer
            Private ReadOnly matches As ConcurrentDictionary(Of Symbol, Symbol)

            ' A cache of members per type, populated when the first member for a given
            ' type Is needed. Within each type, members are indexed by name. The reason
            ' for caching, And indexing by name, Is to avoid searching sequentially
            ' through all members of a given kind each time a member Is matched.
            Private ReadOnly typeMembers As ConcurrentDictionary(Of NamedTypeSymbol, IReadOnlyDictionary(Of String, ImmutableArray(Of Symbol)))

            Public Sub New(
                          anonymousTypeMap As IReadOnlyDictionary(Of AnonymousTypeKey, AnonymousTypeValue),
                          sourceAssembly As SourceAssemblySymbol,
                          otherAssembly As AssemblySymbol)
                Me.anonymousTypeMap = anonymousTypeMap
                Me.sourceAssembly = sourceAssembly
                Me.otherAssembly = otherAssembly
                Me.comparer = New SymbolComparer(Me)
                Me.matches = New ConcurrentDictionary(Of Symbol, Symbol)()
                Me.typeMembers = New ConcurrentDictionary(Of NamedTypeSymbol, IReadOnlyDictionary(Of String, ImmutableArray(Of Symbol)))()
            End Sub

            Friend Function TryGetAnonymousTypeName(type As NamedTypeSymbol, <Out()> ByRef name As String, <Out()> ByRef index As Integer) As Boolean
                Dim otherType As AnonymousTypeValue = Nothing
                If TryFindAnonymousType(type, otherType) Then
                    name = otherType.Name
                    index = otherType.UniqueIndex
                    Return True
                End If
                name = Nothing
                index = -1
                Return False
            End Function

            Public Overrides Function DefaultVisit(symbol As Symbol) As Symbol
                ' Symbol should have been handled elsewhere.
                Throw New NotImplementedException()
            End Function

            Public Overrides Function Visit(symbol As Symbol) As Symbol
                Debug.Assert(symbol.ContainingAssembly IsNot Me.otherAssembly)

                If symbol.ContainingAssembly IsNot Me.sourceAssembly Then
                    ' The symbol Is Not from the source assembly. Unless the symbol
                    ' Is a constructed symbol, no matching Is necessary.
                    Dim kind As SymbolKind = symbol.Kind
                    If kind <> SymbolKind.ArrayType Then
                        If kind <> SymbolKind.NamedType Then
                            Debug.Assert(symbol.IsDefinition)
                            Return symbol
                        Else
                            If symbol.IsDefinition Then
                                Return symbol
                            End If
                        End If
                    End If
                End If

                ' Add an entry for the match, even if there Is no match, to avoid
                ' matching the same symbol unsuccessfully multiple times.
                Return Me.matches.GetOrAdd(symbol, AddressOf MyBase.Visit)
            End Function

            Public Overrides Function VisitArrayType(symbol As ArrayTypeSymbol) As Symbol
                Dim otherElementType As TypeSymbol = DirectCast(Me.Visit(symbol.ElementType), TypeSymbol)
                Debug.Assert(otherElementType IsNot Nothing)
                Dim otherModifiers = VisitCustomModifiers(symbol.CustomModifiers)
                Return New ArrayTypeSymbol(otherElementType, otherModifiers, symbol.Rank, Me.otherAssembly)
            End Function

            Public Overrides Function VisitEvent(symbol As EventSymbol) As Symbol
                Return Me.VisitNamedTypeMember(symbol, AddressOf Me.AreEventsEqual)
            End Function

            Public Overrides Function VisitField(symbol As FieldSymbol) As Symbol
                Return Me.VisitNamedTypeMember(symbol, AddressOf Me.AreFieldsEqual)
            End Function

            Public Overrides Function VisitMethod(symbol As MethodSymbol) As Symbol
                ' Not expecting constructed method.
                Debug.Assert(symbol.IsDefinition)
                Return Me.VisitNamedTypeMember(symbol, AddressOf Me.AreMethodsEqual)
            End Function

            Public Overrides Function VisitModule([module] As ModuleSymbol) As Symbol
                Debug.Assert([module].ContainingSymbol Is Me.sourceAssembly)

                Return Me.otherAssembly.Modules([module].Ordinal)
            End Function

            Public Overrides Function VisitNamespace([namespace] As NamespaceSymbol) As Symbol
                Dim otherContainer As Symbol = Me.Visit([namespace].ContainingSymbol)
                Dim kind As SymbolKind = otherContainer.Kind
                If kind = SymbolKind.NetModule Then
                    Return (DirectCast(otherContainer, ModuleSymbol)).GlobalNamespace
                End If
                If kind <> SymbolKind.Namespace Then
                    Throw ExceptionUtilities.UnexpectedValue(kind)
                End If

                Return VisitNamespaceMembers(DirectCast(otherContainer, NamespaceSymbol), [namespace], Function(s As NamespaceSymbol, o As NamespaceSymbol) True)
            End Function

            Public Overrides Function VisitNamedType(type As NamedTypeSymbol) As Symbol
                Dim originalDef As NamedTypeSymbol = type.OriginalDefinition
                If originalDef IsNot type Then
                    Dim typeArguments = type.GetAllTypeArguments
                    Dim otherDef As NamedTypeSymbol = DirectCast(Me.Visit(originalDef), NamedTypeSymbol)

                    ' For anonymous delegates the rewriter generates a _ClosureCache$_N field
                    ' of the constructed delegate type. For those cases, the matched result will
                    ' be Nothing if the anonymous delegate is new to this compilation.
                    If otherDef Is Nothing Then
                        Return Nothing
                    End If

                    Dim otherTypeParameters As ImmutableArray(Of TypeParameterSymbol) = otherDef.GetAllTypeParameters()
                    Dim otherTypeArguments As ImmutableArray(Of TypeSymbol) = typeArguments.SelectAsArray(Function(t, v) DirectCast(v.Visit(t), TypeSymbol), Me)
                    Debug.Assert(otherTypeArguments.All(Function(t) t IsNot Nothing))

                    Dim typeMap = TypeSubstitution.Create(otherDef, otherTypeParameters, otherTypeArguments, False)
                    Return otherDef.Construct(typeMap)
                End If

                Debug.Assert(type.IsDefinition)

                Dim otherContainer As Symbol = Me.Visit(type.ContainingSymbol)
                ' Containing type will be missing from other assembly
                ' if the type was added in the (newer) source assembly.
                If otherContainer Is Nothing Then
                    Return Nothing
                End If

                Dim kind As SymbolKind = otherContainer.Kind
                Select Case kind
                    Case SymbolKind.Namespace
                        If AnonymousTypeManager.IsAnonymousTypeTemplate(type) Then
                            Debug.Assert(otherContainer Is otherAssembly.GlobalNamespace)
                            Dim value As AnonymousTypeValue = Nothing
                            TryFindAnonymousType(type, value)
                            Return DirectCast(value.Type, NamedTypeSymbol)
                        ElseIf type.IsAnonymousType Then
                            Return Me.Visit(AnonymousTypeManager.TranslateAnonymousTypeSymbol(type))
                        Else
                            Return VisitNamespaceMembers(DirectCast(otherContainer, NamespaceSymbol), type, AddressOf Me.AreNamedTypesEqual)
                        End If

                    Case SymbolKind.NamedType
                        Return Me.VisitNamedTypeMembers(DirectCast(otherContainer, NamedTypeSymbol), type, AddressOf Me.AreNamedTypesEqual)

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(kind)
                End Select
            End Function

            Public Overrides Function VisitParameter(parameter As ParameterSymbol) As Symbol
                Throw New InvalidOperationException()
            End Function

            Public Overrides Function VisitProperty(symbol As PropertySymbol) As Symbol
                Return Me.VisitNamedTypeMember(symbol, AddressOf Me.ArePropertiesEqual)
            End Function

            Public Overrides Function VisitTypeParameter(symbol As TypeParameterSymbol) As Symbol
                Dim otherContainer As Symbol = Me.Visit(symbol.ContainingSymbol)
                Debug.Assert(otherContainer IsNot Nothing)

                Dim otherTypeParameters As ImmutableArray(Of TypeParameterSymbol)

                Select Case otherContainer.Kind
                    Case SymbolKind.NamedType,
                         SymbolKind.ErrorType
                        otherTypeParameters = (DirectCast(otherContainer, NamedTypeSymbol)).TypeParameters

                    Case SymbolKind.Method
                        otherTypeParameters = (DirectCast(otherContainer, MethodSymbol)).TypeParameters

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(otherContainer.Kind)
                End Select

                Return otherTypeParameters(symbol.Ordinal)
            End Function

            Private Function VisitCustomModifiers(modifiers As ImmutableArray(Of CustomModifier)) As ImmutableArray(Of CustomModifier)
                Return modifiers.SelectAsArray(AddressOf VisitCustomModifier)
            End Function

            Private Function VisitCustomModifier(modifier As CustomModifier) As CustomModifier
                Dim type = DirectCast(Me.Visit(DirectCast(modifier.Modifier, Symbol)), NamedTypeSymbol)
                Debug.Assert(type IsNot Nothing)
                Return If(modifier.IsOptional,
                    VisualBasicCustomModifier.CreateOptional(type),
                    VisualBasicCustomModifier.CreateRequired(type))
            End Function

            Friend Function TryFindAnonymousType(type As NamedTypeSymbol, <Out()> ByRef otherType As AnonymousTypeValue) As Boolean
                Debug.Assert(type.ContainingSymbol Is sourceAssembly.GlobalNamespace)
                Debug.Assert(AnonymousTypeManager.IsAnonymousTypeTemplate(type))

                Dim key = AnonymousTypeManager.GetAnonymousTypeKey(type)
                Return anonymousTypeMap.TryGetValue(key, otherType)
            End Function

            Private Shared Function VisitNamespaceMembers(Of T As Symbol)(otherNamespace As NamespaceSymbol, member As T, predicate As Func(Of T, T, Boolean)) As T
                For Each otherMember In otherNamespace.GetMembers(member.Name)
                    If member.Kind = otherMember.Kind Then
                        Dim other As T = DirectCast(otherMember, T)
                        If predicate(member, other) Then
                            Return other
                        End If
                    End If
                Next
                Return Nothing
            End Function

            Private Function VisitNamedTypeMember(Of T As Symbol)(member As T, predicate As Func(Of T, T, Boolean)) As Symbol
                Dim otherType As NamedTypeSymbol = DirectCast(Me.Visit(member.ContainingType), NamedTypeSymbol)
                If otherType Is Nothing Then
                    Return Nothing
                End If
                Return Me.VisitNamedTypeMembers(otherType, member, predicate)
            End Function

            Private Function VisitNamedTypeMembers(Of T As Symbol)(otherType As NamedTypeSymbol, member As T, predicate As Func(Of T, T, Boolean)) As T
                Dim otherMembers As ImmutableArray(Of Symbol) = Nothing
                If Me.typeMembers.GetOrAdd(otherType, AddressOf GetTypeMembers).TryGetValue(member.Name, otherMembers) Then
                    For Each otherMember In otherMembers
                        If member.Kind = otherMember.Kind Then
                            Dim other As T = DirectCast(otherMember, T)
                            If predicate(member, other) Then
                                Return other
                            End If
                        End If
                    Next
                End If
                Return Nothing
            End Function

            Private Function AreArrayTypesEqual(type As ArrayTypeSymbol, other As ArrayTypeSymbol) As Boolean
                Debug.Assert(type.CustomModifiers.IsEmpty)
                Debug.Assert(other.CustomModifiers.IsEmpty)
                Return type.Rank = other.Rank AndAlso Me.AreTypesEqual(type.ElementType, other.ElementType)
            End Function

            Private Function AreEventsEqual([event] As EventSymbol, other As EventSymbol) As Boolean
                Debug.Assert(NameComparer.Equals([event].Name, other.Name))
                Return Me.comparer.Equals([event].Type, other.Type)
            End Function

            Private Function AreFieldsEqual(field As FieldSymbol, other As FieldSymbol) As Boolean
                Debug.Assert(NameComparer.Equals(field.Name, other.Name))
                Return Me.comparer.Equals(field.Type, other.Type)
            End Function

            Private Function AreMethodsEqual(method As MethodSymbol, other As MethodSymbol) As Boolean
                Debug.Assert(NameComparer.Equals(method.Name, other.Name))

                Debug.Assert(method.IsDefinition)
                Debug.Assert(other.IsDefinition)

                method = SubstituteTypeParameters(method)
                other = SubstituteTypeParameters(other)

                Return Me.comparer.Equals(method.ReturnType, other.ReturnType) AndAlso
                    method.Parameters.SequenceEqual(other.Parameters, AddressOf Me.AreParametersEqual) AndAlso
                    method.TypeArguments.SequenceEqual(other.TypeArguments, AddressOf Me.AreTypesEqual)
            End Function

            Private Shared Function SubstituteTypeParameters(method As MethodSymbol) As MethodSymbol
                Debug.Assert(method.IsDefinition)

                Dim i As Integer = method.TypeParameters.Length
                If i = 0 Then
                    Return method
                End If
                Return method.Construct(ImmutableArrayExtensions.Cast(Of TypeParameterSymbol, TypeSymbol)(IndexedTypeParameterSymbol.Take(i)))
            End Function

            Private Function AreNamedTypesEqual(type As NamedTypeSymbol, other As NamedTypeSymbol) As Boolean
                Debug.Assert(NameComparer.Equals(type.Name, other.Name))
                Return type.TypeArgumentsNoUseSiteDiagnostics.SequenceEqual(other.TypeArgumentsNoUseSiteDiagnostics, AddressOf Me.AreTypesEqual)
            End Function

            Private Function AreParametersEqual(parameter As ParameterSymbol, other As ParameterSymbol) As Boolean
                Debug.Assert(parameter.Ordinal = other.Ordinal)
                Return NameComparer.Equals(parameter.Name, other.Name) AndAlso parameter.IsByRef = other.IsByRef AndAlso Me.comparer.Equals(parameter.Type, other.Type)
            End Function

            Private Function ArePropertiesEqual([property] As PropertySymbol, other As PropertySymbol) As Boolean
                Debug.Assert(NameComparer.Equals([property].Name, other.Name))
                Return Me.comparer.Equals([property].Type, other.Type) AndAlso
                    [property].Parameters.SequenceEqual(other.Parameters, AddressOf Me.AreParametersEqual)
            End Function

            Private Function AreTypeParametersEqual(type As TypeParameterSymbol, other As TypeParameterSymbol) As Boolean
                Debug.Assert(type.Ordinal = other.Ordinal)
                Debug.Assert(NameComparer.Equals(type.Name, other.Name))
                ' Comparing constraints is unnecessary: two methods cannot differ by
                ' constraints alone and changing the signature of a method is a rude
                ' edit. Furthermore, comparing constraint types might lead to a cycle.
                Debug.Assert(type.HasConstructorConstraint = other.HasConstructorConstraint)
                Debug.Assert(type.HasValueTypeConstraint = other.HasValueTypeConstraint)
                Debug.Assert(type.HasReferenceTypeConstraint = other.HasReferenceTypeConstraint)
                Debug.Assert(type.ConstraintTypesNoUseSiteDiagnostics.Length = other.ConstraintTypesNoUseSiteDiagnostics.Length)
                Return True
            End Function

            Private Function AreTypesEqual(type As TypeSymbol, other As TypeSymbol) As Boolean
                If type.Kind <> other.Kind Then
                    Return False
                End If
                Select Case type.Kind
                    Case SymbolKind.ArrayType
                        Return AreArrayTypesEqual(DirectCast(type, ArrayTypeSymbol), DirectCast(other, ArrayTypeSymbol))

                    Case SymbolKind.NamedType,
                         SymbolKind.ErrorType
                        Return AreNamedTypesEqual(DirectCast(type, NamedTypeSymbol), DirectCast(other, NamedTypeSymbol))

                    Case SymbolKind.TypeParameter
                        Return AreTypeParametersEqual(DirectCast(type, TypeParameterSymbol), DirectCast(other, TypeParameterSymbol))

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(type.Kind)
                End Select
            End Function

            Private Shared Function GetTypeMembers(type As NamedTypeSymbol) As IReadOnlyDictionary(Of String, ImmutableArray(Of Symbol))
                Dim members As ArrayBuilder(Of Symbol) = ArrayBuilder(Of Symbol).GetInstance()
                members.AddRange(type.GetEventsToEmit())
                members.AddRange(type.GetFieldsToEmit())
                members.AddRange(type.GetMethodsToEmit())
                members.AddRange(type.GetTypeMembers())
                members.AddRange(type.GetPropertiesToEmit())

                Dim result As IReadOnlyDictionary(Of String, ImmutableArray(Of Symbol)) = members.ToDictionary(Of String)(Function(s As Symbol) s.Name, NameComparer)
                members.Free()
                Return result
            End Function


            Private Class SymbolComparer
                Implements IEqualityComparer(Of Symbol)

                Private matcher As MatchSymbols

                Public Sub New(matcher As MatchSymbols)
                    Me.matcher = matcher
                End Sub

                Public Overloads Function Equals(x As Symbol, y As Symbol) As Boolean Implements IEqualityComparer(Of Symbol).Equals
                    Return Me.matcher.Visit(x) = y
                End Function

                Public Overloads Function GetHashCode(obj As Symbol) As Integer Implements IEqualityComparer(Of Symbol).GetHashCode
                    Return obj.GetHashCode()
                End Function
            End Class

        End Class
    End Class
End Namespace