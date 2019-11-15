' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit.NoPia

    Friend NotInheritable Class EmbeddedType
        Inherits EmbeddedTypesManager.CommonEmbeddedType

        Private _embeddedAllMembersOfImplementedInterface As Boolean

        Public Sub New(typeManager As EmbeddedTypesManager, underlyingNamedType As NamedTypeSymbol)
            MyBase.New(typeManager, underlyingNamedType)

            Debug.Assert(underlyingNamedType.IsDefinition)
            Debug.Assert(underlyingNamedType.IsTopLevelType())
            Debug.Assert(Not underlyingNamedType.IsGenericType)
        End Sub

        Public Sub EmbedAllMembersOfImplementedInterface(syntaxNodeOpt As SyntaxNode, diagnostics As DiagnosticBag)
            Debug.Assert(UnderlyingNamedType.IsInterfaceType())

            If _embeddedAllMembersOfImplementedInterface Then
                Return
            End If

            _embeddedAllMembersOfImplementedInterface = True

            ' Embed all members
            For Each m In UnderlyingNamedType.GetMethodsToEmit()
                If m IsNot Nothing Then
                    TypeManager.EmbedMethod(Me, m, syntaxNodeOpt, diagnostics)
                End If
            Next

            ' We also should embed properties and events, but we don't need to do this explicitly here
            ' because accessors embed them automatically.

            ' Do the same for implemented interfaces.
            For Each [interface] In UnderlyingNamedType.GetInterfacesToEmit()
                TypeManager.ModuleBeingBuilt.Translate([interface], syntaxNodeOpt, diagnostics, fromImplements:=True)
            Next
        End Sub

        Protected Overrides Function GetAssemblyRefIndex() As Integer
            Dim refs = TypeManager.ModuleBeingBuilt.SourceModule.GetReferencedAssemblySymbols()
            Return refs.IndexOf(UnderlyingNamedType.ContainingAssembly, ReferenceEqualityComparer.Instance)
        End Function

        Protected Overrides ReadOnly Property IsPublic As Boolean
            Get
                Return UnderlyingNamedType.DeclaredAccessibility = Accessibility.Public
            End Get
        End Property

        Protected Overrides Function GetBaseClass(moduleBuilder As PEModuleBuilder, syntaxNodeOpt As SyntaxNode, diagnostics As DiagnosticBag) As Cci.ITypeReference
            Dim baseType = UnderlyingNamedType.BaseTypeNoUseSiteDiagnostics
            Return If(baseType IsNot Nothing, moduleBuilder.Translate(baseType, syntaxNodeOpt, diagnostics), Nothing)
        End Function

        Protected Overrides Function GetFieldsToEmit() As IEnumerable(Of FieldSymbol)
            Return UnderlyingNamedType.GetFieldsToEmit()
        End Function

        Protected Overrides Function GetMethodsToEmit() As IEnumerable(Of MethodSymbol)
            Return UnderlyingNamedType.GetMethodsToEmit()
        End Function

        Protected Overrides Function GetEventsToEmit() As IEnumerable(Of EventSymbol)
            Return UnderlyingNamedType.GetEventsToEmit()
        End Function

        Protected Overrides Function GetPropertiesToEmit() As IEnumerable(Of PropertySymbol)
            Return UnderlyingNamedType.GetPropertiesToEmit()
        End Function

        Protected Overrides Iterator Function GetInterfaces(context As EmitContext) As IEnumerable(Of Cci.TypeReferenceWithAttributes)
            Debug.Assert(TypeManager.ModuleBeingBuilt Is context.Module)

            Dim moduleBeingBuilt = DirectCast(context.Module, PEModuleBuilder)

            For Each [interface] In UnderlyingNamedType.GetInterfacesToEmit()
                Dim typeRef = moduleBeingBuilt.Translate([interface],
                                                            DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode),
                                                            context.Diagnostics)

                Yield [interface].GetTypeRefWithAttributes(UnderlyingNamedType.DeclaringCompilation, typeRef)
            Next
        End Function

        Protected Overrides ReadOnly Property IsAbstract As Boolean
            Get
                Return UnderlyingNamedType.IsMetadataAbstract
            End Get
        End Property

        Protected Overrides ReadOnly Property IsBeforeFieldInit As Boolean
            Get
                Select Case UnderlyingNamedType.TypeKind
                    Case TypeKind.Enum, TypeKind.Delegate, TypeKind.Interface
                        Return False
                End Select

                ' We shouldn't embed static constructor.
                Return True
            End Get
        End Property

        Protected Overrides ReadOnly Property IsComImport As Boolean
            Get
                Return UnderlyingNamedType.IsComImport
            End Get
        End Property

        Protected Overrides ReadOnly Property IsInterface As Boolean
            Get
                Return UnderlyingNamedType.IsInterfaceType()
            End Get
        End Property

        Protected Overrides ReadOnly Property IsDelegate As Boolean
            Get
                Return UnderlyingNamedType.IsDelegateType()
            End Get
        End Property

        Protected Overrides ReadOnly Property IsSerializable As Boolean
            Get
                Return UnderlyingNamedType.IsSerializable
            End Get
        End Property

        Protected Overrides ReadOnly Property IsSpecialName As Boolean
            Get
                Return UnderlyingNamedType.HasSpecialName
            End Get
        End Property

        Protected Overrides ReadOnly Property IsWindowsRuntimeImport As Boolean
            Get
                Return UnderlyingNamedType.IsWindowsRuntimeImport
            End Get
        End Property

        Protected Overrides ReadOnly Property IsSealed As Boolean
            Get
                Return UnderlyingNamedType.IsMetadataSealed
            End Get
        End Property

        Protected Overrides Function GetTypeLayoutIfStruct() As TypeLayout?
            Return If(UnderlyingNamedType.IsStructureType(), UnderlyingNamedType.Layout, Nothing)
        End Function

        Protected Overrides ReadOnly Property StringFormat As System.Runtime.InteropServices.CharSet
            Get
                Return UnderlyingNamedType.MarshallingCharSet
            End Get
        End Property

        Protected Overrides Function GetCustomAttributesToEmit(moduleBuilder As PEModuleBuilder) As IEnumerable(Of VisualBasicAttributeData)
            Return UnderlyingNamedType.GetCustomAttributesToEmit(moduleBuilder.CompilationState)
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
                Return New SynthesizedAttributeData(ctor, ImmutableArray(Of TypedConstant).Empty, ImmutableArray(Of KeyValuePair(Of String, TypedConstant)).Empty)

            Else
                ' This is an interface with no GuidAttribute, or some other type, so we will generate the
                ' TypeIdentifier with name and scope parameters.

                ' Look for a GUID attribute attached to type's containing assembly. If we find one, we'll use it; 
                ' otherwise, we expect that we will have reported an error (ERRID_PIAHasNoAssemblyGuid1) about this assembly, since
                ' you can't /link against an assembly which lacks a GuidAttribute.

                Dim stringType = TypeManager.GetSystemStringType(syntaxNodeOpt, diagnostics)

                If stringType IsNot Nothing Then
                    Dim guidString = TypeManager.GetAssemblyGuidString(UnderlyingNamedType.ContainingAssembly)
                    Return New SynthesizedAttributeData(ctor,
                        ImmutableArray.Create(New TypedConstant(stringType, TypedConstantKind.Primitive, guidString),
                            New TypedConstant(stringType, TypedConstantKind.Primitive, UnderlyingNamedType.ToDisplayString(SymbolDisplayFormat.QualifiedNameOnlyFormat))),
                        ImmutableArray(Of KeyValuePair(Of String, TypedConstant)).Empty)
                End If
            End If

            Return Nothing
        End Function

        Protected Overrides Sub ReportMissingAttribute(description As AttributeDescription, syntaxNodeOpt As SyntaxNode, diagnostics As DiagnosticBag)
            EmbeddedTypesManager.ReportDiagnostic(diagnostics, ERRID.ERR_NoPIAAttributeMissing2, syntaxNodeOpt, UnderlyingNamedType, description.FullName)
        End Sub

        Protected Overrides Sub EmbedDefaultMembers(defaultMember As String, syntaxNodeOpt As SyntaxNode, diagnostics As DiagnosticBag)
            For Each s In UnderlyingNamedType.GetMembers(defaultMember)
                Select Case s.Kind
                    Case SymbolKind.Field
                        TypeManager.EmbedField(Me, DirectCast(s, FieldSymbol), syntaxNodeOpt, diagnostics)
                    Case SymbolKind.Method
                        TypeManager.EmbedMethod(Me, DirectCast(s, MethodSymbol), syntaxNodeOpt, diagnostics)
                    Case SymbolKind.Property
                        TypeManager.EmbedProperty(Me, DirectCast(s, PropertySymbol), syntaxNodeOpt, diagnostics)
                    Case SymbolKind.Event
                        TypeManager.EmbedEvent(Me, DirectCast(s, EventSymbol), syntaxNodeOpt, diagnostics, isUsedForComAwareEventBinding:=False)
                End Select
            Next
        End Sub

    End Class

End Namespace
