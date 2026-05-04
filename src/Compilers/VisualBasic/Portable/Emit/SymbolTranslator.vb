' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Concurrent
Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    Partial Friend Class PEModuleBuilder

        ' TODO: Need to estimate amount of elements for this map and pass that value to the constructor.
        Protected ReadOnly m_AssemblyOrModuleSymbolToModuleRefMap As New ConcurrentDictionary(Of Symbol, Microsoft.Cci.IModuleReference)()
        Private ReadOnly _genericInstanceMap As New ConcurrentDictionary(Of Symbol, Object)()
        Private ReadOnly _translatedImportsMap As New ConcurrentDictionary(Of SourceFile, ImmutableArray(Of Cci.UsedNamespaceOrType))(ReferenceEqualityComparer.Instance)
        Private ReadOnly _reportedErrorTypesMap As New ConcurrentSet(Of TypeSymbol)()

        Private ReadOnly _embeddedTypesManagerOpt As NoPia.EmbeddedTypesManager
        Public Overrides ReadOnly Property EmbeddedTypesManagerOpt As NoPia.EmbeddedTypesManager
            Get
                Return _embeddedTypesManagerOpt
            End Get
        End Property

        ''' <summary> Stores collection of all embedded symbols referenced from IL </summary>
        Private _addedEmbeddedSymbols As ConcurrentSet(Of Symbol) = Nothing

        ''' <summary> Adds a symbol to the collection of referenced embedded symbols </summary>
        Private Sub ProcessReferencedSymbol(symbol As Symbol)
            Dim kind = symbol.EmbeddedSymbolKind
            If kind = EmbeddedSymbolKind.None Then
                Return
            End If

            Debug.Assert(symbol.ContainingModule Is Me.SourceModule)

            If _addedEmbeddedSymbols Is Nothing Then
                Interlocked.CompareExchange(_addedEmbeddedSymbols, New ConcurrentSet(Of Symbol)(ReferenceEqualityComparer.Instance), Nothing)
            End If

            Dim manager = SourceModule.ContainingSourceAssembly.DeclaringCompilation.EmbeddedSymbolManager
            manager.MarkSymbolAsReferenced(symbol.OriginalDefinition, _addedEmbeddedSymbols)
        End Sub

        Friend NotOverridable Overrides Function Translate(assembly As AssemblySymbol, diagnostics As DiagnosticBag) As Microsoft.Cci.IAssemblyReference
            If SourceModule.ContainingAssembly Is assembly Then
                Return DirectCast(Me, Microsoft.Cci.IAssemblyReference)
            End If

            Dim reference As Microsoft.Cci.IModuleReference = Nothing

            If m_AssemblyOrModuleSymbolToModuleRefMap.TryGetValue(assembly, reference) Then
                Return DirectCast(reference, Microsoft.Cci.IAssemblyReference)
            End If

            Dim asmRef = New AssemblyReference(assembly)

            Dim cachedAsmRef = DirectCast(m_AssemblyOrModuleSymbolToModuleRefMap.GetOrAdd(assembly, asmRef), AssemblyReference)

            If cachedAsmRef Is asmRef Then
                ValidateReferencedAssembly(assembly, cachedAsmRef, diagnostics)
            End If

            ' TryAdd here because whatever is associated with assembly should be associated with modules(0)
            m_AssemblyOrModuleSymbolToModuleRefMap.TryAdd(assembly.Modules(0), cachedAsmRef)

            Return cachedAsmRef
        End Function

        Friend Overloads Function Translate([module] As ModuleSymbol, diagnostics As DiagnosticBag) As Microsoft.Cci.IModuleReference
            If SourceModule Is [module] Then
                Return Me
            End If

            Dim moduleRef As Microsoft.Cci.IModuleReference = Nothing

            If m_AssemblyOrModuleSymbolToModuleRefMap.TryGetValue([module], moduleRef) Then
                Return moduleRef
            End If

            moduleRef = TranslateModule([module], diagnostics)
            moduleRef = m_AssemblyOrModuleSymbolToModuleRefMap.GetOrAdd([module], moduleRef)
            Return moduleRef
        End Function

        Protected Overridable Function TranslateModule([module] As ModuleSymbol, diagnostics As DiagnosticBag) As Microsoft.Cci.IModuleReference
            Dim container As AssemblySymbol = [module].ContainingAssembly

            If container IsNot Nothing AndAlso container.Modules(0) Is [module] Then
                Dim moduleRef As Microsoft.Cci.IModuleReference = New AssemblyReference(container)
                Dim cachedModuleRef As Microsoft.Cci.IModuleReference = m_AssemblyOrModuleSymbolToModuleRefMap.GetOrAdd(container, moduleRef)

                If cachedModuleRef Is moduleRef Then
                    ValidateReferencedAssembly(container, DirectCast(moduleRef, AssemblyReference), diagnostics)
                Else
                    moduleRef = cachedModuleRef
                End If

                Return moduleRef
            Else
                Return New ModuleReference(Me, [module])
            End If
        End Function

        Friend Overloads Function Translate(
            namedTypeSymbol As NamedTypeSymbol,
            syntaxNodeOpt As SyntaxNode,
            diagnostics As DiagnosticBag,
            Optional fromImplements As Boolean = False,
            Optional needDeclaration As Boolean = False
        ) As Microsoft.Cci.INamedTypeReference
            Debug.Assert(namedTypeSymbol Is namedTypeSymbol.OriginalDefinition OrElse
                                            Not namedTypeSymbol.Equals(namedTypeSymbol.OriginalDefinition))
            Debug.Assert(diagnostics IsNot Nothing)

            ' Anonymous type being translated
            If namedTypeSymbol.IsAnonymousType Then
                namedTypeSymbol = AnonymousTypeManager.TranslateAnonymousTypeSymbol(namedTypeSymbol)
            ElseIf namedTypeSymbol.IsTupleType Then
                Debug.Assert(Not needDeclaration)
                namedTypeSymbol = namedTypeSymbol.TupleUnderlyingType

                CheckTupleUnderlyingType(namedTypeSymbol, syntaxNodeOpt, diagnostics)
            End If

            ' Substitute error types with a special singleton object.
            ' Unreported bad types can come through NoPia embedding, for example.
            If namedTypeSymbol.OriginalDefinition.Kind = SymbolKind.ErrorType Then
                Dim errorType = DirectCast(namedTypeSymbol.OriginalDefinition, ErrorTypeSymbol)
                Dim diagInfo = If(errorType.GetUseSiteInfo().DiagnosticInfo, errorType.ErrorInfo)

                If diagInfo Is Nothing AndAlso namedTypeSymbol.Kind = SymbolKind.ErrorType Then
                    errorType = DirectCast(namedTypeSymbol, ErrorTypeSymbol)
                    diagInfo = If(errorType.GetUseSiteInfo().DiagnosticInfo, errorType.ErrorInfo)
                End If

                ' Try to decrease noise by not complaining about the same type over and over again.
                If _reportedErrorTypesMap.Add(errorType) Then
                    diagnostics.Add(New VBDiagnostic(
                                    If(diagInfo, ErrorFactory.ErrorInfo(ERRID.ERR_UnsupportedType1, String.Empty)),
                                    If(syntaxNodeOpt Is Nothing, NoLocation.Singleton, syntaxNodeOpt.GetLocation())))
                End If

                Return Microsoft.CodeAnalysis.Emit.ErrorType.Singleton
            End If

            Me.ProcessReferencedSymbol(namedTypeSymbol)

            If namedTypeSymbol IsNot namedTypeSymbol.OriginalDefinition Then
                ' generic instantiation for sure
                Debug.Assert(Not needDeclaration)

                If namedTypeSymbol.IsUnboundGenericType Then
                    namedTypeSymbol = namedTypeSymbol.OriginalDefinition
                Else
                    Return DirectCast(GetCciAdapter(namedTypeSymbol), Microsoft.Cci.INamedTypeReference)
                End If

            ElseIf Not needDeclaration Then

                Dim reference As Object = Nothing
                Dim typeRef As Microsoft.Cci.INamedTypeReference
                Dim container As NamedTypeSymbol = namedTypeSymbol.ContainingType

                If namedTypeSymbol.Arity > 0 Then

                    If _genericInstanceMap.TryGetValue(namedTypeSymbol, reference) Then
                        Return DirectCast(reference, Microsoft.Cci.INamedTypeReference)
                    End If

                    If container IsNot Nothing Then
                        If IsOrInGenericType(container) Then
                            ' Container is a generic instance too.
                            typeRef = New SpecializedGenericNestedTypeInstanceReference(namedTypeSymbol)
                        Else
                            typeRef = New GenericNestedTypeInstanceReference(namedTypeSymbol)
                        End If
                    Else
                        typeRef = New GenericNamespaceTypeInstanceReference(namedTypeSymbol)
                    End If

                    typeRef = DirectCast(_genericInstanceMap.GetOrAdd(namedTypeSymbol, typeRef), Microsoft.Cci.INamedTypeReference)
                    Return typeRef
                Else
                    If IsOrInGenericType(container) Then
                        Debug.Assert(container IsNot Nothing)

                        If _genericInstanceMap.TryGetValue(namedTypeSymbol, reference) Then
                            Return DirectCast(reference, Microsoft.Cci.INamedTypeReference)
                        End If

                        typeRef = New SpecializedNestedTypeReference(namedTypeSymbol)
                        typeRef = DirectCast(_genericInstanceMap.GetOrAdd(namedTypeSymbol, typeRef), Microsoft.Cci.INamedTypeReference)
                        Return typeRef
                    End If
                End If
            End If

            Return If(_embeddedTypesManagerOpt?.EmbedTypeIfNeedTo(namedTypeSymbol, fromImplements, syntaxNodeOpt, diagnostics), namedTypeSymbol.GetCciAdapter())
        End Function

        Private Function GetCciAdapter(symbol As Symbol) As Object
            Return _genericInstanceMap.GetOrAdd(symbol, Function(s) s.GetCciAdapter())
        End Function

        Private Sub CheckTupleUnderlyingType(namedTypeSymbol As NamedTypeSymbol, syntaxNodeOpt As SyntaxNode, diagnostics As DiagnosticBag)
            ' check that underlying type of a ValueTuple is indeed a value type (or error)
            ' this should never happen, in theory,
            ' but if it does happen we should make it a failure.
            ' NOTE: declaredBase could be null for interfaces
            Dim declaredBase = namedTypeSymbol.BaseTypeNoUseSiteDiagnostics
            If declaredBase IsNot Nothing AndAlso declaredBase.SpecialType = SpecialType.System_ValueType Then
                Return
            End If

            ' Try to decrease noise by not complaining about the same type over and over again.
            If Not _reportedErrorTypesMap.Add(namedTypeSymbol) Then
                Return
            End If

            Dim location = If(syntaxNodeOpt Is Nothing, NoLocation.Singleton, syntaxNodeOpt.GetLocation())
            If declaredBase IsNot Nothing Then
                Dim diagnosticInfo = declaredBase.GetUseSiteInfo().DiagnosticInfo
                If diagnosticInfo IsNot Nothing Then
                    diagnostics.Add(diagnosticInfo, location)
                    Return
                End If
            End If

            diagnostics.Add(
                New VBDiagnostic(
                    ErrorFactory.ErrorInfo(ERRID.ERR_PredefinedValueTupleTypeMustBeStruct, namedTypeSymbol.MetadataName),
                    location))
        End Sub

        Friend Overloads Function Translate([param] As TypeParameterSymbol) As Microsoft.Cci.IGenericParameterReference
            Debug.Assert(param Is param.OriginalDefinition)
            Return [param].GetCciAdapter()
        End Function

        Friend NotOverridable Overrides Function Translate(
            typeSymbol As TypeSymbol,
            syntaxNodeOpt As SyntaxNode,
            diagnostics As DiagnosticBag
        ) As Microsoft.Cci.ITypeReference
            Debug.Assert(diagnostics IsNot Nothing)

            Select Case typeSymbol.Kind
                Case SymbolKind.ArrayType
                    Return Translate(DirectCast(typeSymbol, ArrayTypeSymbol))
                Case SymbolKind.NamedType, SymbolKind.ErrorType
                    Return Translate(DirectCast(typeSymbol, NamedTypeSymbol), syntaxNodeOpt, diagnostics)
                Case SymbolKind.TypeParameter
                    Return Translate(DirectCast(typeSymbol, TypeParameterSymbol))
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(typeSymbol.Kind)
            End Select
        End Function

        Friend Overloads Function Translate(
            fieldSymbol As FieldSymbol,
            syntaxNodeOpt As SyntaxNode,
            diagnostics As DiagnosticBag,
            Optional needDeclaration As Boolean = False
        ) As Microsoft.Cci.IFieldReference
            Debug.Assert(fieldSymbol Is fieldSymbol.OriginalDefinition OrElse
                         Not fieldSymbol.Equals(fieldSymbol.OriginalDefinition))
            Debug.Assert(Not fieldSymbol.IsTupleField, "tuple fields should be rewritten to underlying by now")

            Me.ProcessReferencedSymbol(fieldSymbol)

            If fieldSymbol IsNot fieldSymbol.OriginalDefinition Then
                Debug.Assert(Not needDeclaration)
                Return DirectCast(GetCciAdapter(fieldSymbol), Microsoft.Cci.IFieldReference)

            ElseIf Not needDeclaration AndAlso IsOrInGenericType(fieldSymbol.ContainingType) Then

                Dim reference As Object = Nothing
                Dim fieldRef As Microsoft.Cci.IFieldReference

                If _genericInstanceMap.TryGetValue(fieldSymbol, reference) Then
                    Return DirectCast(reference, Microsoft.Cci.IFieldReference)
                End If

                fieldRef = New SpecializedFieldReference(fieldSymbol)
                fieldRef = DirectCast(_genericInstanceMap.GetOrAdd(fieldSymbol, fieldRef), Microsoft.Cci.IFieldReference)
                Return fieldRef
            End If

            If _embeddedTypesManagerOpt IsNot Nothing Then
                Return _embeddedTypesManagerOpt.EmbedFieldIfNeedTo(fieldSymbol.GetCciAdapter(), syntaxNodeOpt, diagnostics)
            End If

            Return fieldSymbol.GetCciAdapter()
        End Function

        Friend Overloads Overrides Function Translate(symbol As MethodSymbol, diagnostics As DiagnosticBag, needDeclaration As Boolean) As IMethodReference
            Return Translate(symbol, Nothing, diagnostics, needDeclaration)
        End Function

        Friend Overloads Function Translate(
            methodSymbol As MethodSymbol,
            syntaxNodeOpt As SyntaxNode,
            diagnostics As DiagnosticBag,
            Optional needDeclaration As Boolean = False
        ) As Microsoft.Cci.IMethodReference
            Debug.Assert(Not methodSymbol.IsDefaultValueTypeConstructor())
            Debug.Assert(methodSymbol Is methodSymbol.OriginalDefinition OrElse
                                            Not methodSymbol.Equals(methodSymbol.OriginalDefinition))

            Dim container As NamedTypeSymbol = methodSymbol.ContainingType

            ' Method of anonymous type being translated
            If container.IsAnonymousType Then
                methodSymbol = AnonymousTypeManager.TranslateAnonymousTypeMethodSymbol(methodSymbol)

            ElseIf methodSymbol.IsTupleMethod Then
                Debug.Assert(Not needDeclaration)
                Debug.Assert(container.IsTupleType)
                container = container.TupleUnderlyingType
                methodSymbol = methodSymbol.TupleUnderlyingMethod
            End If

            Debug.Assert(Not container.IsTupleType)

            Me.ProcessReferencedSymbol(methodSymbol)

            If methodSymbol.OriginalDefinition IsNot methodSymbol Then

                Debug.Assert(Not needDeclaration)
                Return DirectCast(GetCciAdapter(methodSymbol), Microsoft.Cci.IMethodReference)

            ElseIf Not needDeclaration Then
                Dim methodIsGeneric As Boolean = methodSymbol.IsGenericMethod
                Dim typeIsGeneric As Boolean = IsOrInGenericType(container)

                If methodIsGeneric OrElse typeIsGeneric Then
                    Dim reference As Object = Nothing
                    Dim methodRef As Microsoft.Cci.IMethodReference

                    If _genericInstanceMap.TryGetValue(methodSymbol, reference) Then
                        Return DirectCast(reference, Microsoft.Cci.IMethodReference)
                    End If

                    If methodIsGeneric Then
                        If typeIsGeneric Then
                            ' Specialized and generic instance at the same time.
                            methodRef = New SpecializedGenericMethodInstanceReference(methodSymbol)
                        Else
                            methodRef = New GenericMethodInstanceReference(methodSymbol)
                        End If
                    Else
                        Debug.Assert(typeIsGeneric)
                        methodRef = New SpecializedMethodReference(methodSymbol)
                    End If

                    methodRef = DirectCast(_genericInstanceMap.GetOrAdd(methodSymbol, methodRef), Microsoft.Cci.IMethodReference)
                    Return methodRef
                End If
            End If

            If _embeddedTypesManagerOpt IsNot Nothing Then
                Return _embeddedTypesManagerOpt.EmbedMethodIfNeedTo(methodSymbol.GetCciAdapter(), syntaxNodeOpt, diagnostics)
            End If

            Return methodSymbol.GetCciAdapter()
        End Function

        Friend Overloads Function TranslateOverriddenMethodReference(methodSymbol As MethodSymbol, syntaxNodeOpt As VisualBasicSyntaxNode, diagnostics As DiagnosticBag) As Microsoft.Cci.IMethodReference
            Dim methodRef As Microsoft.Cci.IMethodReference
            Dim container As NamedTypeSymbol = methodSymbol.ContainingType

            If container.IsOrInGenericType() Then
                If methodSymbol.IsDefinition Then
                    Dim reference As Object = Nothing

                    If _genericInstanceMap.TryGetValue(methodSymbol, reference) Then
                        methodRef = DirectCast(reference, Microsoft.Cci.IMethodReference)
                    Else
                        methodRef = New SpecializedMethodReference(methodSymbol)
                        methodRef = DirectCast(_genericInstanceMap.GetOrAdd(methodSymbol, methodRef), Microsoft.Cci.IMethodReference)
                    End If
                Else
                    methodRef = New SpecializedMethodReference(methodSymbol)
                End If
            Else
                Debug.Assert(methodSymbol.IsDefinition)

                If _embeddedTypesManagerOpt IsNot Nothing Then
                    methodRef = _embeddedTypesManagerOpt.EmbedMethodIfNeedTo(methodSymbol.GetCciAdapter(), syntaxNodeOpt, diagnostics)
                Else
                    methodRef = methodSymbol.GetCciAdapter()
                End If
            End If

            Return methodRef
        End Function

        Friend Overloads Function Translate(params As ImmutableArray(Of ParameterSymbol)) As ImmutableArray(Of Microsoft.Cci.IParameterTypeInformation)
            Debug.Assert(params.All(Function(p) p.IsDefinitionOrDistinct()))

            Dim mustBeTranslated = params.Any AndAlso MustBeWrapped(params.First())
            Debug.Assert(params.All(Function(p) mustBeTranslated = MustBeWrapped(p)), "either all or no parameters need translating")

            If (Not mustBeTranslated) Then
#If DEBUG Then
                Return params.SelectAsArray(Of Cci.IParameterTypeInformation)(Function(p) p.GetCciAdapter())
#Else
                Return StaticCast(Of Microsoft.Cci.IParameterTypeInformation).From(params)
#End If
            End If

            Return TranslateAll(params)
        End Function

        Private Shared Function MustBeWrapped(param As ParameterSymbol) As Boolean
            ' we represent parameters of generic methods as definitions
            ' CCI wants them represented as IParameterTypeInformation
            ' so we need to create a wrapper of parameters iff
            ' 1) parameters are definitions And
            ' 2) container Is generic
            ' NOTE: all parameters must always agree On whether they need wrapping
            If (param.IsDefinition) Then
                Dim container = param.ContainingSymbol
                If (ContainerIsGeneric(container)) Then
                    Return True
                End If
            End If

            Return False
        End Function

        Private Function TranslateAll(params As ImmutableArray(Of ParameterSymbol)) As ImmutableArray(Of Microsoft.Cci.IParameterTypeInformation)
            Dim builder = ArrayBuilder(Of Microsoft.Cci.IParameterTypeInformation).GetInstance()
            For Each param In params
                builder.Add(CreateParameterTypeInformationWrapper(param))
            Next
            Return builder.ToImmutableAndFree
        End Function

        Private Function CreateParameterTypeInformationWrapper(param As ParameterSymbol) As Cci.IParameterTypeInformation
            Dim reference As Object = Nothing
            Dim paramRef As Microsoft.Cci.IParameterTypeInformation

            If (Me._genericInstanceMap.TryGetValue(param, reference)) Then
                Return DirectCast(reference, Microsoft.Cci.IParameterTypeInformation)
            End If

            paramRef = New ParameterTypeInformation(param)
            paramRef = DirectCast(_genericInstanceMap.GetOrAdd(param, paramRef), Microsoft.Cci.IParameterTypeInformation)

            Return paramRef
        End Function

        Private Shared Function ContainerIsGeneric(container As Symbol) As Boolean
            Return container.Kind = SymbolKind.Method AndAlso (DirectCast(container, MethodSymbol)).IsGenericMethod OrElse
                container.ContainingType.IsGenericType

        End Function

        Friend Overloads Function Translate(symbol As ArrayTypeSymbol) As Microsoft.Cci.IArrayTypeReference
            Return DirectCast(GetCciAdapter(symbol), Microsoft.Cci.IArrayTypeReference)
        End Function

        Friend Function TryGetTranslatedImports(file As SourceFile, <Runtime.InteropServices.Out> ByRef [imports] As ImmutableArray(Of Cci.UsedNamespaceOrType)) As Boolean
            Return _translatedImportsMap.TryGetValue(file, [imports])
        End Function

        Friend Function GetOrAddTranslatedImports(file As SourceFile, [imports] As ImmutableArray(Of Cci.UsedNamespaceOrType)) As ImmutableArray(Of Cci.UsedNamespaceOrType)
            Return _translatedImportsMap.GetOrAdd(file, [imports])
        End Function
    End Class
End Namespace
