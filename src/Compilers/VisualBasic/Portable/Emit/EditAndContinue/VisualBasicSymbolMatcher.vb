' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        Private Shared ReadOnly s_nameComparer As StringComparer = IdentifierComparison.Comparer

        Private ReadOnly _defs As MatchDefs
        Private ReadOnly _symbols As MatchSymbols

        Public Sub New(anonymousTypeMap As IReadOnlyDictionary(Of AnonymousTypeKey, AnonymousTypeValue),
                      sourceAssembly As SourceAssemblySymbol,
                      sourceContext As EmitContext,
                      otherAssembly As SourceAssemblySymbol,
                      otherContext As EmitContext,
                      otherSynthesizedMembersOpt As ImmutableDictionary(Of Cci.ITypeDefinition, ImmutableArray(Of Cci.ITypeDefinitionMember)))

            Me._defs = New MatchDefsToSource(sourceContext, otherContext)
            Me._symbols = New MatchSymbols(anonymousTypeMap, sourceAssembly, otherAssembly, otherSynthesizedMembersOpt, New DeepTranslator(otherAssembly.GetSpecialType(SpecialType.System_Object)))
        End Sub

        Public Sub New(anonymousTypeMap As IReadOnlyDictionary(Of AnonymousTypeKey, AnonymousTypeValue),
                      sourceAssembly As SourceAssemblySymbol,
                      sourceContext As EmitContext,
                      otherAssembly As PEAssemblySymbol)

            Me._defs = New MatchDefsToMetadata(sourceContext, otherAssembly)
            Me._symbols = New MatchSymbols(anonymousTypeMap, sourceAssembly, otherAssembly, otherSynthesizedMembersOpt:=Nothing, deepTranslatorOpt:=Nothing)
        End Sub

        Public Overrides Function MapDefinition(def As Cci.IDefinition) As Cci.IDefinition
            Dim symbol As symbol = TryCast(def, symbol)
            If symbol IsNot Nothing Then
                Return DirectCast(Me._symbols.Visit(symbol), Cci.IDefinition)
            End If
            Return Me._defs.VisitDef(def)
        End Function

        Public Overrides Function MapReference(reference As Cci.ITypeReference) As Cci.ITypeReference
            Dim symbol As symbol = TryCast(reference, symbol)
            If symbol IsNot Nothing Then
                Return DirectCast(Me._symbols.Visit(symbol), Cci.ITypeReference)
            End If
            Return Nothing
        End Function

        Friend Function TryGetAnonymousTypeName(template As NamedTypeSymbol, <Out()> ByRef name As String, <Out()> ByRef index As Integer) As Boolean
            Return Me._symbols.TryGetAnonymousTypeName(template, name, index)
        End Function

        Private MustInherit Class MatchDefs
            Private ReadOnly _sourceContext As EmitContext
            Private ReadOnly _matches As ConcurrentDictionary(Of Cci.IDefinition, Cci.IDefinition)
            Private _lazyTopLevelTypes As IReadOnlyDictionary(Of String, Cci.INamespaceTypeDefinition)

            Public Sub New(sourceContext As EmitContext)
                Me._sourceContext = sourceContext
                Me._matches = New ConcurrentDictionary(Of Cci.IDefinition, Cci.IDefinition)(ReferenceEqualityComparer.Instance)
            End Sub

            Public Function VisitDef(def As Cci.IDefinition) As Cci.IDefinition
                Return Me._matches.GetOrAdd(def, AddressOf Me.VisitDefInternal)
            End Function

            Private Function VisitDefInternal(def As Cci.IDefinition) As Cci.IDefinition
                Dim type = TryCast(def, Cci.ITypeDefinition)
                If type IsNot Nothing Then
                    Dim namespaceType As Cci.INamespaceTypeDefinition = type.AsNamespaceTypeDefinition(Me._sourceContext)
                    If namespaceType IsNot Nothing Then
                        Return Me.VisitNamespaceType(namespaceType)
                    End If

                    Dim nestedType As Cci.INestedTypeDefinition = type.AsNestedTypeDefinition(Me._sourceContext)
                    Debug.Assert(nestedType IsNot Nothing)

                    Dim otherContainer = DirectCast(Me.VisitDef(nestedType.ContainingTypeDefinition), Cci.ITypeDefinition)
                    If otherContainer Is Nothing Then
                        Return Nothing
                    End If

                    Return Me.VisitTypeMembers(otherContainer, nestedType, AddressOf GetNestedTypes, Function(a, b) s_nameComparer.Equals(a.Name, b.Name))
                End If

                Dim member = TryCast(def, Cci.ITypeDefinitionMember)
                If member IsNot Nothing Then
                    Dim otherContainer = DirectCast(Me.VisitDef(member.ContainingTypeDefinition), Cci.ITypeDefinition)
                    If otherContainer Is Nothing Then
                        Return Nothing
                    End If

                    Dim field = TryCast(def, Cci.IFieldDefinition)
                    If field IsNot Nothing Then
                        Return Me.VisitTypeMembers(otherContainer, field, AddressOf GetFields, Function(a, b) s_nameComparer.Equals(a.Name, b.Name))
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
                If Me._lazyTopLevelTypes Is Nothing Then
                    Dim typesByName As Dictionary(Of String, Cci.INamespaceTypeDefinition) = New Dictionary(Of String, Cci.INamespaceTypeDefinition)(s_nameComparer)
                    For Each type As Cci.INamespaceTypeDefinition In Me.GetTopLevelTypes()
                        ' All generated top-level types are assumed to be in the global namespace.
                        If String.IsNullOrEmpty(type.NamespaceName) Then
                            typesByName.Add(type.Name, type)
                        End If
                    Next
                    Interlocked.CompareExchange(Me._lazyTopLevelTypes, typesByName, Nothing)
                End If
                Return Me._lazyTopLevelTypes
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

            Private ReadOnly _otherAssembly As PEAssemblySymbol

            Public Sub New(sourceContext As EmitContext, otherAssembly As PEAssemblySymbol)
                MyBase.New(sourceContext)
                Me._otherAssembly = otherAssembly
            End Sub

            Protected Overrides Function GetTopLevelTypes() As IEnumerable(Of Cci.INamespaceTypeDefinition)
                Dim builder As ArrayBuilder(Of Cci.INamespaceTypeDefinition) = ArrayBuilder(Of Cci.INamespaceTypeDefinition).GetInstance()
                GetTopLevelTypes(builder, Me._otherAssembly.GlobalNamespace)
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

            Private ReadOnly _otherContext As EmitContext

            Public Sub New(sourceContext As EmitContext, otherContext As EmitContext)
                MyBase.New(sourceContext)
                Me._otherContext = otherContext
            End Sub

            Protected Overrides Function GetTopLevelTypes() As IEnumerable(Of Cci.INamespaceTypeDefinition)
                Return Me._otherContext.Module.GetTopLevelTypes(Me._otherContext)
            End Function

            Protected Overrides Function GetNestedTypes(def As Cci.ITypeDefinition) As IEnumerable(Of Cci.INestedTypeDefinition)
                Return def.GetNestedTypes(Me._otherContext)
            End Function

            Protected Overrides Function GetFields(def As Cci.ITypeDefinition) As IEnumerable(Of Cci.IFieldDefinition)
                Return def.GetFields(Me._otherContext)
            End Function
        End Class

        Private NotInheritable Class MatchSymbols
            Inherits VisualBasicSymbolVisitor(Of Symbol)

            Private ReadOnly _anonymousTypeMap As IReadOnlyDictionary(Of AnonymousTypeKey, AnonymousTypeValue)
            Private ReadOnly _comparer As SymbolComparer
            Private ReadOnly _matches As ConcurrentDictionary(Of Symbol, Symbol)

            Private ReadOnly _sourceAssembly As SourceAssemblySymbol
            Private ReadOnly _otherAssembly As AssemblySymbol
            Private ReadOnly _otherSynthesizedMembersOpt As ImmutableDictionary(Of Cci.ITypeDefinition, ImmutableArray(Of Cci.ITypeDefinitionMember))

            ' A cache of members per type, populated when the first member for a given
            ' type Is needed. Within each type, members are indexed by name. The reason
            ' for caching, And indexing by name, Is to avoid searching sequentially
            ' through all members of a given kind each time a member Is matched.
            Private ReadOnly _typeMembers As ConcurrentDictionary(Of NamedTypeSymbol, IReadOnlyDictionary(Of String, ImmutableArray(Of Cci.ITypeDefinitionMember)))

            Public Sub New(anonymousTypeMap As IReadOnlyDictionary(Of AnonymousTypeKey, AnonymousTypeValue),
                           sourceAssembly As SourceAssemblySymbol,
                           otherAssembly As AssemblySymbol,
                           otherSynthesizedMembersOpt As ImmutableDictionary(Of Cci.ITypeDefinition, ImmutableArray(Of Cci.ITypeDefinitionMember)),
                           deepTranslatorOpt As DeepTranslator)

                Me._anonymousTypeMap = anonymousTypeMap
                Me._sourceAssembly = sourceAssembly
                Me._otherAssembly = otherAssembly
                Me._otherSynthesizedMembersOpt = otherSynthesizedMembersOpt
                Me._comparer = New SymbolComparer(Me, deepTranslatorOpt)
                Me._matches = New ConcurrentDictionary(Of Symbol, Symbol)(ReferenceEqualityComparer.Instance)
                Me._typeMembers = New ConcurrentDictionary(Of NamedTypeSymbol, IReadOnlyDictionary(Of String, ImmutableArray(Of Cci.ITypeDefinitionMember)))()
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
                Debug.Assert(symbol.ContainingAssembly IsNot Me._otherAssembly)

                ' If the symbol is not defined in any of the previous source assemblies and not a constructed symbol
                ' no matching is necessary, just return the symbol.
                If TypeOf symbol.ContainingAssembly IsNot SourceAssemblySymbol Then
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
                Return Me._matches.GetOrAdd(symbol, AddressOf MyBase.Visit)
            End Function

            Public Overrides Function VisitArrayType(symbol As ArrayTypeSymbol) As Symbol
                Dim otherElementType As TypeSymbol = DirectCast(Me.Visit(symbol.ElementType), TypeSymbol)
                If otherElementType Is Nothing Then
                    ' For a newly added type, there is no match in the previous generation, so it could be Nothing.
                    Return Nothing
                End If
                Dim otherModifiers = VisitCustomModifiers(symbol.CustomModifiers)

                If symbol.IsSZArray Then
                    Return ArrayTypeSymbol.CreateSZArray(otherElementType, otherModifiers, Me._otherAssembly)
                End If

                Return ArrayTypeSymbol.CreateMDArray(otherElementType, otherModifiers, symbol.Rank, symbol.Sizes, symbol.LowerBounds, Me._otherAssembly)
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
                ' Only map symbols from source assembly and its previous generations to the other assembly. 
                ' All other symbols should map to themselves.
                If [module].ContainingAssembly.Identity.Equals(_sourceAssembly.Identity) Then
                    Return _otherAssembly.Modules([module].Ordinal)
                Else
                    Return [module]
                End If
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

                Return FindMatchingNamespaceMember(DirectCast(otherContainer, NamespaceSymbol), [namespace], Function(s As NamespaceSymbol, o As NamespaceSymbol) True)
            End Function

            Public Overrides Function VisitNamedType(type As NamedTypeSymbol) As Symbol
                Dim originalDef As NamedTypeSymbol = type.OriginalDefinition
                If originalDef IsNot type Then
                    Dim otherDef As NamedTypeSymbol = DirectCast(Me.Visit(originalDef), NamedTypeSymbol)

                    ' For anonymous delegates the rewriter generates a _ClosureCache$_N field
                    ' of the constructed delegate type. For those cases, the matched result will
                    ' be Nothing if the anonymous delegate is new to this compilation.
                    If otherDef Is Nothing Then
                        Return Nothing
                    End If

                    Dim otherTypeParameters As ImmutableArray(Of TypeParameterSymbol) = otherDef.GetAllTypeParameters()
                    Dim translationFailed As Boolean = False
                    Dim otherTypeArguments = type.GetAllTypeArgumentsWithModifiers().SelectAsArray(Function(t, v)
                                                                                                       Dim newType = DirectCast(v.Visit(t.Type), TypeSymbol)
                                                                                                       If newType Is Nothing Then
                                                                                                           ' For a newly added type, there is no match in the previous generation, so it could be Nothing.
                                                                                                           translationFailed = True
                                                                                                           newType = t.Type
                                                                                                       End If

                                                                                                       Return New TypeWithModifiers(newType, v.VisitCustomModifiers(t.CustomModifiers))
                                                                                                   End Function, Me)
                    If translationFailed Then
                        ' There is no match in the previous generation.
                        Return Nothing
                    End If

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
                            Debug.Assert(otherContainer Is _otherAssembly.GlobalNamespace)
                            Dim value As AnonymousTypeValue = Nothing
                            TryFindAnonymousType(type, value)
                            Return DirectCast(value.Type, NamedTypeSymbol)
                        ElseIf type.IsAnonymousType Then
                            Return Me.Visit(AnonymousTypeManager.TranslateAnonymousTypeSymbol(type))
                        Else
                            Return FindMatchingNamespaceMember(DirectCast(otherContainer, NamespaceSymbol), type, AddressOf Me.AreNamedTypesEqual)
                        End If

                    Case SymbolKind.NamedType
                        Return Me.FindMatchingNamedTypeMember(DirectCast(otherContainer, NamedTypeSymbol), type, AddressOf Me.AreNamedTypesEqual)

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
                Dim otherContainer As symbol = Me.Visit(symbol.ContainingSymbol)
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
                Debug.Assert(type.ContainingSymbol Is _sourceAssembly.GlobalNamespace)
                Debug.Assert(AnonymousTypeManager.IsAnonymousTypeTemplate(type))

                Dim key = AnonymousTypeManager.GetAnonymousTypeKey(type)
                Return _anonymousTypeMap.TryGetValue(key, otherType)
            End Function

            Private Shared Function FindMatchingNamespaceMember(Of T As Symbol)(otherNamespace As NamespaceSymbol, sourceMember As T, predicate As Func(Of T, T, Boolean)) As T
                For Each otherMember In otherNamespace.GetMembers(sourceMember.Name)
                    If sourceMember.Kind = otherMember.Kind Then
                        Dim other As T = DirectCast(otherMember, T)
                        If predicate(sourceMember, other) Then
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
                Return Me.FindMatchingNamedTypeMember(otherType, member, predicate)
            End Function

            Private Function FindMatchingNamedTypeMember(Of T As Symbol)(otherType As NamedTypeSymbol, sourceMember As T, predicate As Func(Of T, T, Boolean)) As T
                Dim otherMembersByName = Me._typeMembers.GetOrAdd(otherType, AddressOf GetOtherTypeMembers)

                Dim otherMembers As ImmutableArray(Of Cci.ITypeDefinitionMember) = Nothing
                If otherMembersByName.TryGetValue(sourceMember.Name, otherMembers) Then
                    For Each otherMember In otherMembers
                        Dim other = TryCast(otherMember, T)
                        If other IsNot Nothing AndAlso predicate(sourceMember, other) Then
                            Return other
                        End If
                    Next
                End If

                Return Nothing
            End Function

            Private Function AreArrayTypesEqual(type As ArrayTypeSymbol, other As ArrayTypeSymbol) As Boolean
                Debug.Assert(type.CustomModifiers.IsEmpty)
                Debug.Assert(other.CustomModifiers.IsEmpty)
                Return type.HasSameShapeAs(other) AndAlso Me.AreTypesEqual(type.ElementType, other.ElementType)
            End Function

            Private Function AreEventsEqual([event] As EventSymbol, other As EventSymbol) As Boolean
                Debug.Assert(s_nameComparer.Equals([event].Name, other.Name))
                Return Me._comparer.Equals([event].Type, other.Type)
            End Function

            Private Function AreFieldsEqual(field As FieldSymbol, other As FieldSymbol) As Boolean
                Debug.Assert(s_nameComparer.Equals(field.Name, other.Name))
                Return Me._comparer.Equals(field.Type, other.Type)
            End Function

            Private Function AreMethodsEqual(method As MethodSymbol, other As MethodSymbol) As Boolean
                Debug.Assert(s_nameComparer.Equals(method.Name, other.Name))

                Debug.Assert(method.IsDefinition)
                Debug.Assert(other.IsDefinition)

                method = SubstituteTypeParameters(method)
                other = SubstituteTypeParameters(other)

                Return Me._comparer.Equals(method.ReturnType, other.ReturnType) AndAlso
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
                Debug.Assert(s_nameComparer.Equals(type.Name, other.Name))
                Debug.Assert(Not type.HasTypeArgumentsCustomModifiers)
                Debug.Assert(Not other.HasTypeArgumentsCustomModifiers)
                Return type.TypeArgumentsNoUseSiteDiagnostics.SequenceEqual(other.TypeArgumentsNoUseSiteDiagnostics, AddressOf Me.AreTypesEqual)
            End Function

            Private Function AreParametersEqual(parameter As ParameterSymbol, other As ParameterSymbol) As Boolean
                Debug.Assert(parameter.Ordinal = other.Ordinal)
                Return s_nameComparer.Equals(parameter.Name, other.Name) AndAlso parameter.IsByRef = other.IsByRef AndAlso Me._comparer.Equals(parameter.Type, other.Type)
            End Function

            Private Function ArePropertiesEqual([property] As PropertySymbol, other As PropertySymbol) As Boolean
                Debug.Assert(s_nameComparer.Equals([property].Name, other.Name))
                Return Me._comparer.Equals([property].Type, other.Type) AndAlso
                    [property].Parameters.SequenceEqual(other.Parameters, AddressOf Me.AreParametersEqual)
            End Function

            Private Function AreTypeParametersEqual(type As TypeParameterSymbol, other As TypeParameterSymbol) As Boolean
                Debug.Assert(type.Ordinal = other.Ordinal)
                Debug.Assert(s_nameComparer.Equals(type.Name, other.Name))
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

            Private Function GetOtherTypeMembers(otherType As NamedTypeSymbol) As IReadOnlyDictionary(Of String, ImmutableArray(Of Cci.ITypeDefinitionMember))
                Dim members = ArrayBuilder(Of Cci.ITypeDefinitionMember).GetInstance()

                members.AddRange(otherType.GetEventsToEmit())
                members.AddRange(otherType.GetFieldsToEmit())
                members.AddRange(otherType.GetMethodsToEmit())
                members.AddRange(otherType.GetTypeMembers())
                members.AddRange(otherType.GetPropertiesToEmit())

                Dim synthesizedMembers As ImmutableArray(Of Cci.ITypeDefinitionMember) = Nothing
                If _otherSynthesizedMembersOpt IsNot Nothing AndAlso _otherSynthesizedMembersOpt.TryGetValue(otherType, synthesizedMembers) Then
                    members.AddRange(synthesizedMembers)
                End If

                Dim result = members.ToDictionary(Function(s) DirectCast(s, Symbol).Name, s_nameComparer)
                members.Free()
                Return result
            End Function

            Private Class SymbolComparer
                Private ReadOnly _matcher As MatchSymbols
                Private ReadOnly _deepTranslatorOpt As DeepTranslator

                Public Sub New(matcher As MatchSymbols, deepTranslatorOpt As DeepTranslator)
                    Debug.Assert(matcher IsNot Nothing)
                    _matcher = matcher
                    _deepTranslatorOpt = deepTranslatorOpt
                End Sub

                Public Overloads Function Equals(source As TypeSymbol, other As TypeSymbol) As Boolean
                    Dim visitedSource = _matcher.Visit(source)
                    Dim visitedOther = If(_deepTranslatorOpt IsNot Nothing, _deepTranslatorOpt.Visit(other), other)

                    Return visitedSource = visitedOther
                End Function
            End Class
        End Class

        Friend NotInheritable Class DeepTranslator
            Inherits VisualBasicSymbolVisitor(Of Symbol)

            Private ReadOnly _matches As ConcurrentDictionary(Of Symbol, Symbol)
            Private ReadOnly _systemObject As NamedTypeSymbol

            Public Sub New(systemObject As NamedTypeSymbol)
                _matches = New ConcurrentDictionary(Of Symbol, Symbol)(ReferenceEqualityComparer.Instance)
                _systemObject = systemObject
            End Sub

            Public Overrides Function DefaultVisit(symbol As Symbol) As Symbol
                ' Symbol should have been handled elsewhere.
                Throw New NotImplementedException()
            End Function

            Public Overrides Function Visit(symbol As Symbol) As Symbol
                Return _matches.GetOrAdd(symbol, AddressOf MyBase.Visit)
            End Function

            Public Overrides Function VisitArrayType(symbol As ArrayTypeSymbol) As Symbol
                Dim translatedElementType As TypeSymbol = DirectCast(Me.Visit(symbol.ElementType), TypeSymbol)
                Dim translatedModifiers = VisitCustomModifiers(symbol.CustomModifiers)

                If symbol.IsSZArray Then
                    Return ArrayTypeSymbol.CreateSZArray(translatedElementType, translatedModifiers, symbol.BaseTypeNoUseSiteDiagnostics.ContainingAssembly)
                End If

                Return ArrayTypeSymbol.CreateMDArray(translatedElementType, translatedModifiers, symbol.Rank, symbol.Sizes, symbol.LowerBounds, symbol.BaseTypeNoUseSiteDiagnostics.ContainingAssembly)
            End Function

            Public Overrides Function VisitNamedType(type As NamedTypeSymbol) As Symbol
                Dim originalDef As NamedTypeSymbol = type.OriginalDefinition
                If originalDef IsNot type Then
                    Dim translatedTypeArguments = type.GetAllTypeArgumentsWithModifiers().SelectAsArray(Function(t, v) New TypeWithModifiers(DirectCast(v.Visit(t.Type), TypeSymbol),
                                                                                                                                             v.VisitCustomModifiers(t.CustomModifiers)), Me)

                    Dim translatedOriginalDef = DirectCast(Me.Visit(originalDef), NamedTypeSymbol)
                    Dim typeMap = TypeSubstitution.Create(translatedOriginalDef, translatedOriginalDef.GetAllTypeParameters(), translatedTypeArguments, False)
                    Return translatedOriginalDef.Construct(typeMap)
                End If

                Debug.Assert(type.IsDefinition)

                If type.IsAnonymousType Then
                    Return Me.Visit(AnonymousTypeManager.TranslateAnonymousTypeSymbol(type))
                End If

                Return type
            End Function

            Public Overrides Function VisitTypeParameter(symbol As TypeParameterSymbol) As Symbol
                Return symbol
            End Function

            Private Function VisitCustomModifiers(modifiers As ImmutableArray(Of CustomModifier)) As ImmutableArray(Of CustomModifier)
                Return modifiers.SelectAsArray(AddressOf VisitCustomModifier)
            End Function

            Private Function VisitCustomModifier(modifier As CustomModifier) As CustomModifier
                Dim translatedType = DirectCast(Me.Visit(DirectCast(modifier.Modifier, Symbol)), NamedTypeSymbol)
                Debug.Assert(translatedType IsNot Nothing)
                Return If(modifier.IsOptional,
                    VisualBasicCustomModifier.CreateOptional(translatedType),
                    VisualBasicCustomModifier.CreateRequired(translatedType))
            End Function
        End Class
    End Class
End Namespace
