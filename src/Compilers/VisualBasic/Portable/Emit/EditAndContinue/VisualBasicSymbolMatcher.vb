' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Concurrent
Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit
    Friend NotInheritable Class VisualBasicSymbolMatcher
        Inherits SymbolMatcher

        Private Shared ReadOnly s_nameComparer As StringComparer = IdentifierComparison.Comparer

        Private ReadOnly _visitor As Visitor

        Public Sub New(sourceAssembly As SourceAssemblySymbol,
                       otherAssembly As SourceAssemblySymbol,
                       otherSynthesizedTypes As SynthesizedTypeMaps,
                       otherSynthesizedMembers As IReadOnlyDictionary(Of ISymbolInternal, ImmutableArray(Of ISymbolInternal)),
                       otherDeletedMembers As IReadOnlyDictionary(Of ISymbolInternal, ImmutableArray(Of ISymbolInternal)))

            _visitor = New Visitor(sourceAssembly,
                                   otherAssembly,
                                   otherSynthesizedTypes,
                                   otherSynthesizedMembers,
                                   otherDeletedMembers,
                                   New DeepTranslator(otherAssembly.GetSpecialType(SpecialType.System_Object)))
        End Sub

        Public Sub New(sourceAssembly As SourceAssemblySymbol,
                       otherAssembly As PEAssemblySymbol,
                       otherSynthesizedTypes As SynthesizedTypeMaps)

            _visitor = New Visitor(sourceAssembly,
                                   otherAssembly,
                                   otherSynthesizedTypes,
                                   otherSynthesizedMembers:=SpecializedCollections.EmptyReadOnlyDictionary(Of ISymbolInternal, ImmutableArray(Of ISymbolInternal)),
                                   otherDeletedMembers:=SpecializedCollections.EmptyReadOnlyDictionary(Of ISymbolInternal, ImmutableArray(Of ISymbolInternal)),
                                   deepTranslatorOpt:=Nothing)
        End Sub

        Public Overrides Function MapDefinition(definition As Cci.IDefinition) As Cci.IDefinition
            Dim symbol As Symbol = TryCast(definition.GetInternalSymbol(), Symbol)
            If symbol IsNot Nothing Then
                Return DirectCast(_visitor.Visit(symbol)?.GetCciAdapter(), Cci.IDefinition)
            End If

            ' For simplicity, PID helpers and no-PIA embedded definitions are not reused across generations, so we don't map them here.
            ' Instead, new ones are regenerated as needed.
            Debug.Assert(TypeOf definition Is PrivateImplementationDetails OrElse
                         TypeOf definition Is Cci.IEmbeddedDefinition OrElse
                         TypeOf definition Is MappedField OrElse
                         TypeOf definition Is ExplicitSizeStruct)

            Return Nothing
        End Function

        Public Overrides Function MapNamespace([namespace] As Cci.INamespace) As Cci.INamespace
            Debug.Assert(TypeOf [namespace].GetInternalSymbol() Is NamespaceSymbol)
            Return DirectCast(_visitor.Visit(DirectCast([namespace]?.GetInternalSymbol(), NamespaceSymbol))?.GetCciAdapter(), Cci.INamespace)
        End Function

        Public Overrides Function MapReference(reference As Cci.ITypeReference) As Cci.ITypeReference
            Dim symbol As Symbol = TryCast(reference.GetInternalSymbol(), Symbol)
            If symbol IsNot Nothing Then
                Return DirectCast(_visitor.Visit(symbol)?.GetCciAdapter(), Cci.ITypeReference)
            End If
            Return Nothing
        End Function

        Friend Function TryGetAnonymousTypeName(template As AnonymousTypeManager.AnonymousTypeOrDelegateTemplateSymbol, <Out> ByRef name As String, <Out> ByRef index As Integer) As Boolean
            Return _visitor.TryGetAnonymousTypeName(template, name, index)
        End Function

        Protected Overrides Function TryGetMatchingDelegateWithIndexedName(delegateTemplate As INamedTypeSymbolInternal, values As ImmutableArray(Of AnonymousTypeValue), ByRef match As AnonymousTypeValue) As Boolean
            ' VB does not have delegates with indexed names
            Throw ExceptionUtilities.Unreachable()
        End Function

        Private NotInheritable Class Visitor
            Inherits VisualBasicSymbolVisitor(Of Symbol)

            Private ReadOnly _synthesizedTypes As SynthesizedTypeMaps
            Private ReadOnly _comparer As SymbolComparer
            Private ReadOnly _matches As ConcurrentDictionary(Of Symbol, Symbol)

            Private ReadOnly _sourceAssembly As SourceAssemblySymbol
            Private ReadOnly _otherAssembly As AssemblySymbol
            Private ReadOnly _otherSynthesizedMembers As IReadOnlyDictionary(Of ISymbolInternal, ImmutableArray(Of ISymbolInternal))
            Private ReadOnly _otherDeletedMembersOpt As IReadOnlyDictionary(Of ISymbolInternal, ImmutableArray(Of ISymbolInternal))

            ' A cache of members per type, populated when the first member for a given
            ' type Is needed. Within each type, members are indexed by name. The reason
            ' for caching, And indexing by name, Is to avoid searching sequentially
            ' through all members of a given kind each time a member Is matched.
            Private ReadOnly _otherMembers As ConcurrentDictionary(Of ISymbolInternal, IReadOnlyDictionary(Of String, ImmutableArray(Of ISymbolInternal)))

            Public Sub New(sourceAssembly As SourceAssemblySymbol,
                           otherAssembly As AssemblySymbol,
                           synthesizedTypes As SynthesizedTypeMaps,
                           otherSynthesizedMembers As IReadOnlyDictionary(Of ISymbolInternal, ImmutableArray(Of ISymbolInternal)),
                           otherDeletedMembers As IReadOnlyDictionary(Of ISymbolInternal, ImmutableArray(Of ISymbolInternal)),
                           deepTranslatorOpt As DeepTranslator)

                _synthesizedTypes = synthesizedTypes
                _sourceAssembly = sourceAssembly
                _otherAssembly = otherAssembly
                _otherSynthesizedMembers = otherSynthesizedMembers
                _otherDeletedMembersOpt = otherDeletedMembers
                _comparer = New SymbolComparer(Me, deepTranslatorOpt)
                _matches = New ConcurrentDictionary(Of Symbol, Symbol)(ReferenceEqualityComparer.Instance)
                _otherMembers = New ConcurrentDictionary(Of ISymbolInternal, IReadOnlyDictionary(Of String, ImmutableArray(Of ISymbolInternal)))(ReferenceEqualityComparer.Instance)
            End Sub

            Friend Function TryGetAnonymousTypeName(type As AnonymousTypeManager.AnonymousTypeOrDelegateTemplateSymbol, <Out> ByRef name As String, <Out> ByRef index As Integer) As Boolean
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
                Throw ExceptionUtilities.Unreachable
            End Function

            Public Overrides Function Visit(symbol As Symbol) As Symbol
                Debug.Assert(symbol.ContainingAssembly IsNot Me._otherAssembly)

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
                Dim otherAssembly = DirectCast(Visit([module].ContainingAssembly), AssemblySymbol)
                If otherAssembly Is Nothing Then
                    Return Nothing
                End If

                ' manifest module:
                If [module].Ordinal = 0 Then
                    Return otherAssembly.Modules(0)
                End If

                ' match non-manifest module by name:
                For i = 1 To otherAssembly.Modules.Length - 1
                    Dim otherModule = otherAssembly.Modules(i)

                    ' use case sensitive comparison -- modules whose names differ in casing are considered distinct
                    If StringComparer.Ordinal.Equals(otherModule.Name, [module].Name) Then
                        Return otherModule
                    End If
                Next

                Return Nothing
            End Function

            Public Overrides Function VisitAssembly(assembly As AssemblySymbol) As Symbol
                If assembly.IsLinked Then
                    Return assembly
                End If

                ' When we map synthesized symbols from previous generations to the latest compilation
                ' we might encounter a symbol that is defined in arbitrary preceding generation,
                ' not just the immediately preceding generation. If the source assembly uses time-based
                ' versioning assemblies of preceding generations might differ in their version number.
                If IdentityEqualIgnoringVersionWildcard(assembly, _sourceAssembly) Then
                    Return _otherAssembly
                End If

                ' find a referenced assembly with the exactly same source identity:
                For Each otherReferencedAssembly In _otherAssembly.Modules(0).ReferencedAssemblySymbols
                    If IdentityEqualIgnoringVersionWildcard(assembly, otherReferencedAssembly) Then
                        Return otherReferencedAssembly
                    End If
                Next

                Return Nothing
            End Function

            Private Shared Function IdentityEqualIgnoringVersionWildcard(left As AssemblySymbol, right As AssemblySymbol) As Boolean
                Dim leftIdentity = left.Identity
                Dim rightIdentity = right.Identity
                Return AssemblyIdentityComparer.SimpleNameComparer.Equals(leftIdentity.Name, rightIdentity.Name) AndAlso
                       If(left.AssemblyVersionPattern, leftIdentity.Version).Equals(If(right.AssemblyVersionPattern, rightIdentity.Version)) AndAlso
                       AssemblyIdentity.EqualIgnoringNameAndVersion(leftIdentity, rightIdentity)
            End Function

            Public Overrides Function VisitNamespace([namespace] As NamespaceSymbol) As Symbol
                Dim otherContainer As Symbol = Visit([namespace].ContainingSymbol)

                ' Containing namespace will be missing from other assembly
                ' if its was added in the (newer) source assembly.
                If otherContainer Is Nothing Then
                    Return Nothing
                End If

                Select Case otherContainer.Kind
                    Case SymbolKind.NetModule
                        Return DirectCast(otherContainer, ModuleSymbol).GlobalNamespace

                    Case SymbolKind.Namespace
                        Return FindMatchingMember(otherContainer, [namespace], AddressOf AreNamespacesEqual)

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(otherContainer.Kind)

                End Select
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
                ElseIf type.IsTupleType Then
                    Dim otherDef = DirectCast(Me.Visit(type.TupleUnderlyingType), NamedTypeSymbol)
                    If otherDef Is Nothing OrElse Not otherDef.IsTupleOrCompatibleWithTupleOfCardinality(type.TupleElementTypes.Length) Then
                        Return Nothing
                    End If

                    Return otherDef
                End If

                Debug.Assert(type.IsDefinition)

                Dim otherContainer As Symbol = Me.Visit(type.ContainingSymbol)
                ' Containing type will be missing from other assembly
                ' if the type was added in the (newer) source assembly.
                If otherContainer Is Nothing Then
                    Return Nothing
                End If

                Select Case otherContainer.Kind
                    Case SymbolKind.Namespace
                        Dim template = TryCast(type, AnonymousTypeManager.AnonymousTypeOrDelegateTemplateSymbol)
                        If template IsNot Nothing Then
                            Debug.Assert(otherContainer Is _otherAssembly.GlobalNamespace)
                            Dim value As AnonymousTypeValue = Nothing
                            TryFindAnonymousType(template, value)
                            Return DirectCast(value.Type?.GetInternalSymbol(), NamedTypeSymbol)
                        End If

                        If type.IsAnonymousType Then
                            Return Visit(AnonymousTypeManager.TranslateAnonymousTypeSymbol(type))
                        End If

                        Return FindMatchingMember(otherContainer, type, AddressOf AreNamedTypesEqual)

                    Case SymbolKind.NamedType
                        Return FindMatchingMember(otherContainer, type, AddressOf AreNamedTypesEqual)

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(otherContainer.Kind)
                End Select
            End Function

            Public Overrides Function VisitParameter(parameter As ParameterSymbol) As Symbol
                Throw ExceptionUtilities.Unreachable
            End Function

            Public Overrides Function VisitProperty(symbol As PropertySymbol) As Symbol
                Return Me.VisitNamedTypeMember(symbol, AddressOf Me.ArePropertiesEqual)
            End Function

            Public Overrides Function VisitTypeParameter(symbol As TypeParameterSymbol) As Symbol
                Dim indexed = TryCast(symbol, IndexedTypeParameterSymbol)
                If indexed IsNot Nothing Then
                    Return indexed
                End If

                Dim otherContainer As Symbol = Me.Visit(symbol.ContainingSymbol)
                Debug.Assert(otherContainer IsNot Nothing)

                Dim otherTypeParameters As ImmutableArray(Of TypeParameterSymbol)

                Select Case otherContainer.Kind
                    Case SymbolKind.NamedType,
                         SymbolKind.ErrorType
                        otherTypeParameters = DirectCast(otherContainer, NamedTypeSymbol).TypeParameters

                    Case SymbolKind.Method
                        otherTypeParameters = DirectCast(otherContainer, MethodSymbol).TypeParameters

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

            Friend Function TryFindAnonymousType(type As AnonymousTypeManager.AnonymousTypeOrDelegateTemplateSymbol, <Out> ByRef otherType As AnonymousTypeValue) As Boolean
                Return _synthesizedTypes.AnonymousTypes.TryGetValue(type.GetAnonymousTypeKey(), otherType)
            End Function

            Private Function VisitNamedTypeMember(Of T As Symbol)(member As T, predicate As Func(Of T, T, Boolean)) As Symbol
                If member.ContainingType Is Nothing Then
                    ' ContainingType is null for synthesized PrivateImplementationDetails helpers.
                    ' For simplicity, these helpers are not reused across generations.
                    ' Instead new ones are regenerated as needed.
                    Debug.Assert(TypeOf member Is ISynthesizedGlobalMethodSymbol)

                    Return Nothing
                End If

                Dim otherType As NamedTypeSymbol = DirectCast(Visit(member.ContainingType), NamedTypeSymbol)
                If otherType Is Nothing Then
                    Return Nothing
                End If
                Return FindMatchingMember(otherType, member, predicate)
            End Function

            Private Function FindMatchingMember(Of T As Symbol)(otherTypeOrNamespace As ISymbolInternal, sourceMember As T, predicate As Func(Of T, T, Boolean)) As T
                Dim otherMembersByName = _otherMembers.GetOrAdd(otherTypeOrNamespace, AddressOf GetAllEmittedMembers)

                Dim otherMembers As ImmutableArray(Of ISymbolInternal) = Nothing
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
                ' Events can't be overloaded on type.
                ' ECMA: Within the rows owned by a given row in the TypeDef table, there shall be no duplicates based upon Name [ERROR]
                Return True
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

                Return _comparer.Equals(method.ReturnType, other.ReturnType) AndAlso
                    method.Parameters.SequenceEqual(other.Parameters, AddressOf Me.AreParametersEqual) AndAlso
                    method.TypeParameters.SequenceEqual(other.TypeParameters, AddressOf Me.AreTypesEqual)
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

                ' Tuple types should be unwrapped to their underlying type before getting here (see MatchSymbols.VisitNamedType)
                Debug.Assert(Not type.IsTupleType)
                Debug.Assert(Not other.IsTupleType)

                Return type.TypeArgumentsNoUseSiteDiagnostics.SequenceEqual(other.TypeArgumentsNoUseSiteDiagnostics, AddressOf Me.AreTypesEqual)
            End Function

            Private Function AreNamespacesEqual([namespace] As NamespaceSymbol, other As NamespaceSymbol) As Boolean
                Debug.Assert(s_nameComparer.Equals([namespace].Name, other.Name))
                Return True
            End Function

            Private Function AreParametersEqual(parameter As ParameterSymbol, other As ParameterSymbol) As Boolean
                Debug.Assert(parameter.Ordinal = other.Ordinal)

                ' allow a different ref-kind as long as the runtime type is the same:
                Return parameter.IsByRef = other.IsByRef AndAlso Me._comparer.Equals(parameter.Type, other.Type)
            End Function

            Private Function ArePropertiesEqual([property] As PropertySymbol, other As PropertySymbol) As Boolean
                Debug.Assert(s_nameComparer.Equals([property].Name, other.Name))

                ' Properties may be overloaded on their signature.
                ' ECMA: Within the rows owned by a given row in the TypeDef table, there shall be no duplicates based upon Name+Type [ERROR]
                Return Me._comparer.Equals([property].Type, other.Type) AndAlso
                    [property].Parameters.SequenceEqual(other.Parameters, AddressOf Me.AreParametersEqual)
            End Function

            Private Shared Function AreTypeParametersEqual(type As TypeParameterSymbol, other As TypeParameterSymbol) As Boolean
                Debug.Assert(type.Ordinal = other.Ordinal)
                Debug.Assert(s_nameComparer.Equals(type.Name, other.Name))
                ' Comparing constraints is unnecessary: two methods cannot differ by
                ' constraints alone and changing the signature of a method is a rude
                ' edit. Furthermore, comparing constraint types might lead to a cycle.
                Debug.Assert(type.HasConstructorConstraint = other.HasConstructorConstraint)
                Debug.Assert(type.HasValueTypeConstraint = other.HasValueTypeConstraint)
                Debug.Assert(type.AllowsRefLikeType = other.AllowsRefLikeType)
                Debug.Assert(type.HasReferenceTypeConstraint = other.HasReferenceTypeConstraint)
                Debug.Assert(type.ConstraintTypesNoUseSiteDiagnostics.Length = other.ConstraintTypesNoUseSiteDiagnostics.Length)
                Debug.Assert(type.HasUnmanagedTypeConstraint = other.HasUnmanagedTypeConstraint)
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

            Private Function GetAllEmittedMembers(symbol As ISymbolInternal) As IReadOnlyDictionary(Of String, ImmutableArray(Of ISymbolInternal))
                Dim members = ArrayBuilder(Of ISymbolInternal).GetInstance()

                If symbol.Kind = SymbolKind.NamedType Then
                    Dim type = CType(symbol, NamedTypeSymbol)
                    members.AddRange(type.GetEventsToEmit())
                    members.AddRange(type.GetFieldsToEmit())
                    members.AddRange(type.GetMethodsToEmit())
                    members.AddRange(type.GetTypeMembers())
                    members.AddRange(type.GetPropertiesToEmit())
                Else
                    members.AddRange(CType(symbol, NamespaceSymbol).GetMembers())
                End If

                Dim synthesizedMembers As ImmutableArray(Of ISymbolInternal) = Nothing
                If _otherSynthesizedMembers.TryGetValue(symbol, synthesizedMembers) Then
                    members.AddRange(synthesizedMembers)
                End If

                Dim deletedMembers As ImmutableArray(Of ISymbolInternal) = Nothing
                If _otherDeletedMembersOpt.TryGetValue(symbol, deletedMembers) Then
                    members.AddRange(deletedMembers)
                End If

                Dim result = members.ToDictionary(Function(s) s.Name, s_nameComparer)
                members.Free()
                Return result
            End Function

            Private Class SymbolComparer
                Private ReadOnly _matcher As Visitor
                Private ReadOnly _deepTranslatorOpt As DeepTranslator

                Public Sub New(matcher As Visitor, deepTranslatorOpt As DeepTranslator)
                    Debug.Assert(matcher IsNot Nothing)
                    _matcher = matcher
                    _deepTranslatorOpt = deepTranslatorOpt
                End Sub

                Public Overloads Function Equals(source As TypeSymbol, other As TypeSymbol) As Boolean
                    If ReferenceEquals(source, other) Then
                        Return True
                    End If

                    Dim visitedSource = DirectCast(_matcher.Visit(source), TypeSymbol)
                    Dim visitedOther = If(_deepTranslatorOpt IsNot Nothing, DirectCast(_deepTranslatorOpt.Visit(other), TypeSymbol), other)

                    ' If both visitedSource and visitedOther are Nothing, return false meaning that the method was not able to verify the equality.
                    Return visitedSource IsNot Nothing AndAlso visitedOther IsNot Nothing AndAlso visitedSource.IsSameType(visitedOther, TypeCompareKind.IgnoreTupleNames)
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
                Throw ExceptionUtilities.Unreachable
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
                If type.IsTupleType Then
                    type = type.TupleUnderlyingType
                    Debug.Assert(Not type.IsTupleType)
                End If

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
