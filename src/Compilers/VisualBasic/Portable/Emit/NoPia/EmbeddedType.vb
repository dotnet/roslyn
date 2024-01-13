' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer

#If Not DEBUG Then
Imports NamedTypeSymbolAdapter = Microsoft.CodeAnalysis.VisualBasic.Symbols.NamedTypeSymbol
Imports FieldSymbolAdapter = Microsoft.CodeAnalysis.VisualBasic.Symbols.FieldSymbol
Imports MethodSymbolAdapter = Microsoft.CodeAnalysis.VisualBasic.Symbols.MethodSymbol
Imports EventSymbolAdapter = Microsoft.CodeAnalysis.VisualBasic.Symbols.EventSymbol
Imports PropertySymbolAdapter = Microsoft.CodeAnalysis.VisualBasic.Symbols.PropertySymbol
#End If

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit.NoPia

    Friend NotInheritable Class EmbeddedType
        Inherits EmbeddedTypesManager.CommonEmbeddedType

        Private _embeddedAllMembersOfImplementedInterface As Boolean

        Public Sub New(typeManager As EmbeddedTypesManager, underlyingNamedType As NamedTypeSymbolAdapter)
            MyBase.New(typeManager, underlyingNamedType)

            Debug.Assert(underlyingNamedType.AdaptedNamedTypeSymbol.IsDefinition)
            Debug.Assert(underlyingNamedType.AdaptedNamedTypeSymbol.IsTopLevelType())
            Debug.Assert(Not underlyingNamedType.AdaptedNamedTypeSymbol.IsGenericType)
        End Sub

        Public Sub EmbedAllMembersOfImplementedInterface(syntaxNodeOpt As SyntaxNode, diagnostics As DiagnosticBag)
            Debug.Assert(UnderlyingNamedType.AdaptedNamedTypeSymbol.IsInterfaceType())

            If _embeddedAllMembersOfImplementedInterface Then
                Return
            End If

            _embeddedAllMembersOfImplementedInterface = True

            ' Embed all members
            For Each m In UnderlyingNamedType.AdaptedNamedTypeSymbol.GetMethodsToEmit()
                If m IsNot Nothing Then
                    TypeManager.EmbedMethod(Me, m.GetCciAdapter(), syntaxNodeOpt, diagnostics)
                End If
            Next

            ' We also should embed properties and events, but we don't need to do this explicitly here
            ' because accessors embed them automatically.

            ' Do the same for implemented interfaces.
            For Each [interface] In UnderlyingNamedType.AdaptedNamedTypeSymbol.GetInterfacesToEmit()
                TypeManager.ModuleBeingBuilt.Translate([interface], syntaxNodeOpt, diagnostics, fromImplements:=True)
            Next
        End Sub

        Protected Overrides Function GetAssemblyRefIndex() As Integer
            Dim refs = TypeManager.ModuleBeingBuilt.SourceModule.GetReferencedAssemblySymbols()
            Return refs.IndexOf(UnderlyingNamedType.AdaptedNamedTypeSymbol.ContainingAssembly, ReferenceEqualityComparer.Instance)
        End Function

        Protected Overrides ReadOnly Property IsPublic As Boolean
            Get
                Return UnderlyingNamedType.AdaptedNamedTypeSymbol.DeclaredAccessibility = Accessibility.Public
            End Get
        End Property

        Protected Overrides Function GetBaseClass(moduleBuilder As PEModuleBuilder, syntaxNodeOpt As SyntaxNode, diagnostics As DiagnosticBag) As Cci.ITypeReference
            Dim baseType = UnderlyingNamedType.AdaptedNamedTypeSymbol.BaseTypeNoUseSiteDiagnostics
            Return If(baseType IsNot Nothing, moduleBuilder.Translate(baseType, syntaxNodeOpt, diagnostics), Nothing)
        End Function

        Protected Overrides Function GetFieldsToEmit() As IEnumerable(Of FieldSymbolAdapter)
#If DEBUG Then
            Return UnderlyingNamedType.AdaptedNamedTypeSymbol.GetFieldsToEmit().Select(Function(s) s.GetCciAdapter())
#Else
            Return UnderlyingNamedType.AdaptedNamedTypeSymbol.GetFieldsToEmit()
#End If
        End Function

        Protected Overrides Function GetMethodsToEmit() As IEnumerable(Of MethodSymbolAdapter)
#If DEBUG Then
            Return UnderlyingNamedType.AdaptedNamedTypeSymbol.GetMethodsToEmit().Select(Function(s) s?.GetCciAdapter())
#Else
            Return UnderlyingNamedType.AdaptedNamedTypeSymbol.GetMethodsToEmit()
#End If
        End Function

        Protected Overrides Function GetEventsToEmit() As IEnumerable(Of EventSymbolAdapter)
#If DEBUG Then
            Return UnderlyingNamedType.AdaptedNamedTypeSymbol.GetEventsToEmit().Select(Function(s) s.GetCciAdapter())
#Else
            Return UnderlyingNamedType.AdaptedNamedTypeSymbol.GetEventsToEmit()
#End If
        End Function

        Protected Overrides Function GetPropertiesToEmit() As IEnumerable(Of PropertySymbolAdapter)
#If DEBUG Then
            Return UnderlyingNamedType.AdaptedNamedTypeSymbol.GetPropertiesToEmit().Select(Function(s) s.GetCciAdapter())
#Else
            Return UnderlyingNamedType.AdaptedNamedTypeSymbol.GetPropertiesToEmit()
#End If
        End Function

        Protected Overrides Iterator Function GetInterfaces(context As EmitContext) As IEnumerable(Of Cci.TypeReferenceWithAttributes)
            Debug.Assert(TypeManager.ModuleBeingBuilt Is context.Module)

            Dim moduleBeingBuilt = DirectCast(context.Module, PEModuleBuilder)

            For Each [interface] In UnderlyingNamedType.AdaptedNamedTypeSymbol.GetInterfacesToEmit()
                Dim typeRef = moduleBeingBuilt.Translate([interface],
                                                            DirectCast(context.SyntaxNode, VisualBasicSyntaxNode),
                                                            context.Diagnostics)

                Yield [interface].GetTypeRefWithAttributes(UnderlyingNamedType.AdaptedNamedTypeSymbol.DeclaringCompilation, typeRef)
            Next
        End Function

        Protected Overrides ReadOnly Property IsAbstract As Boolean
            Get
                Return UnderlyingNamedType.AdaptedNamedTypeSymbol.IsMetadataAbstract
            End Get
        End Property

        Protected Overrides ReadOnly Property IsBeforeFieldInit As Boolean
            Get
                Select Case UnderlyingNamedType.AdaptedNamedTypeSymbol.TypeKind
                    Case TypeKind.Enum, TypeKind.Delegate, TypeKind.Interface
                        Return False
                End Select

                ' We shouldn't embed static constructor.
                Return True
            End Get
        End Property

        Protected Overrides ReadOnly Property IsComImport As Boolean
            Get
                Return UnderlyingNamedType.AdaptedNamedTypeSymbol.IsComImport
            End Get
        End Property

        Protected Overrides ReadOnly Property IsInterface As Boolean
            Get
                Return UnderlyingNamedType.AdaptedNamedTypeSymbol.IsInterfaceType()
            End Get
        End Property

        Protected Overrides ReadOnly Property IsDelegate As Boolean
            Get
                Return UnderlyingNamedType.AdaptedNamedTypeSymbol.IsDelegateType()
            End Get
        End Property

        Protected Overrides ReadOnly Property IsSerializable As Boolean
            Get
                Return UnderlyingNamedType.AdaptedNamedTypeSymbol.IsSerializable
            End Get
        End Property

        Protected Overrides ReadOnly Property IsSpecialName As Boolean
            Get
                Return UnderlyingNamedType.AdaptedNamedTypeSymbol.HasSpecialName
            End Get
        End Property

        Protected Overrides ReadOnly Property IsWindowsRuntimeImport As Boolean
            Get
                Return UnderlyingNamedType.AdaptedNamedTypeSymbol.IsWindowsRuntimeImport
            End Get
        End Property

        Protected Overrides ReadOnly Property IsSealed As Boolean
            Get
                Return UnderlyingNamedType.AdaptedNamedTypeSymbol.IsMetadataSealed
            End Get
        End Property

        Protected Overrides Function GetTypeLayoutIfStruct() As TypeLayout?
            Return If(UnderlyingNamedType.AdaptedNamedTypeSymbol.IsStructureType(), UnderlyingNamedType.AdaptedNamedTypeSymbol.Layout, Nothing)
        End Function

        Protected Overrides ReadOnly Property StringFormat As System.Runtime.InteropServices.CharSet
            Get
                Return UnderlyingNamedType.AdaptedNamedTypeSymbol.MarshallingCharSet
            End Get
        End Property

        Protected Overrides Function GetCustomAttributesToEmit(moduleBuilder As PEModuleBuilder) As IEnumerable(Of VisualBasicAttributeData)
            Return UnderlyingNamedType.AdaptedNamedTypeSymbol.GetCustomAttributesToEmit(moduleBuilder)
        End Function

        Protected Overrides Function CreateTypeIdentifierAttribute(hasGuid As Boolean, syntaxNodeOpt As SyntaxNode, diagnostics As DiagnosticBag) As VisualBasicAttributeData
            Dim member = If(hasGuid,
                WellKnownMember.System_Runtime_InteropServices_TypeIdentifierAttribute__ctor,
                WellKnownMember.System_Runtime_InteropServices_TypeIdentifierAttribute__ctorStringString)
            Dim ctor = TypeManager.GetWellKnownMethod(member, syntaxNodeOpt, diagnostics)
            If ctor Is Nothing Then
                Return Nothing
            End If

            If hasGuid Then
                ' This is an interface with a GuidAttribute, so we will generate the no-parameter TypeIdentifier.
                Return New SynthesizedAttributeData(TypeManager.ModuleBeingBuilt.Compilation, ctor, ImmutableArray(Of TypedConstant).Empty, ImmutableArray(Of KeyValuePair(Of String, TypedConstant)).Empty)

            Else
                ' This is an interface with no GuidAttribute, or some other type, so we will generate the
                ' TypeIdentifier with name and scope parameters.

                ' Look for a GUID attribute attached to type's containing assembly. If we find one, we'll use it;
                ' otherwise, we expect that we will have reported an error (ERRID_PIAHasNoAssemblyGuid1) about this assembly, since
                ' you can't /link against an assembly which lacks a GuidAttribute.

                Dim stringType = TypeManager.GetSystemStringType(syntaxNodeOpt, diagnostics)

                If stringType IsNot Nothing Then
                    Dim guidString = TypeManager.GetAssemblyGuidString(UnderlyingNamedType.AdaptedNamedTypeSymbol.ContainingAssembly)
                    Return New SynthesizedAttributeData(TypeManager.ModuleBeingBuilt.Compilation, ctor,
                        ImmutableArray.Create(New TypedConstant(stringType, TypedConstantKind.Primitive, guidString),
                            New TypedConstant(stringType, TypedConstantKind.Primitive, UnderlyingNamedType.AdaptedNamedTypeSymbol.ToDisplayString(SymbolDisplayFormat.QualifiedNameOnlyFormat))),
                        ImmutableArray(Of KeyValuePair(Of String, TypedConstant)).Empty)
                End If
            End If

            Return Nothing
        End Function

        Protected Overrides Sub ReportMissingAttribute(description As AttributeDescription, syntaxNodeOpt As SyntaxNode, diagnostics As DiagnosticBag)
            EmbeddedTypesManager.ReportDiagnostic(diagnostics, ERRID.ERR_NoPIAAttributeMissing2, syntaxNodeOpt, UnderlyingNamedType.AdaptedNamedTypeSymbol, description.FullName)
        End Sub

        Protected Overrides Sub EmbedDefaultMembers(defaultMember As String, syntaxNodeOpt As SyntaxNode, diagnostics As DiagnosticBag)
            For Each s In UnderlyingNamedType.AdaptedNamedTypeSymbol.GetMembers(defaultMember)
                Select Case s.Kind
                    Case SymbolKind.Field
                        TypeManager.EmbedField(Me, DirectCast(s, FieldSymbol).GetCciAdapter(), syntaxNodeOpt, diagnostics)
                    Case SymbolKind.Method
                        TypeManager.EmbedMethod(Me, DirectCast(s, MethodSymbol).GetCciAdapter(), syntaxNodeOpt, diagnostics)
                    Case SymbolKind.Property
                        TypeManager.EmbedProperty(Me, DirectCast(s, PropertySymbol).GetCciAdapter(), syntaxNodeOpt, diagnostics)
                    Case SymbolKind.Event
                        TypeManager.EmbedEvent(Me, DirectCast(s, EventSymbol).GetCciAdapter(), syntaxNodeOpt, diagnostics, isUsedForComAwareEventBinding:=False)
                End Select
            Next
        End Sub

    End Class

End Namespace
