' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Collections.ObjectModel
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend MustInherit Class UnboundGenericType
        Inherits NamedTypeSymbol

        Friend Shared ReadOnly UnboundTypeArgument As New ErrorTypeSymbol()

        ''' <summary>
        ''' Given a possibly constructed/specialized generic type, create a symbol
        ''' to represent an unbound generic type for its definition.
        ''' </summary>
        Friend Shared Function Create(type As NamedTypeSymbol) As NamedTypeSymbol
            If type.IsUnboundGenericType Then
                Return type
            End If

            Dim specialized = TryCast(type, UnboundGenericType.ConstructedFromSymbol)

            If specialized IsNot Nothing Then
                Return specialized.Constructed
            End If

            If type.IsGenericType Then
                Return New UnboundGenericType.ConstructedSymbol(type.OriginalDefinition)
            End If

            'EDMAURER This exception is part of the public contract of NamedTypeSymbol.ConstructUnboundGenericType
            Throw New InvalidOperationException()
        End Function

        Private Sub New()
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return OriginalDefinition.Name
            End Get
        End Property

        Friend Overrides ReadOnly Property MangleName As Boolean
            Get
                Return OriginalDefinition.MangleName
            End Get
        End Property

        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            Return OriginalDefinition.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken)
        End Function

        Friend NotOverridable Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return OriginalDefinition.HasSpecialName
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsSerializable As Boolean
            Get
                Return OriginalDefinition.IsSerializable
            End Get
        End Property

        Friend Overrides ReadOnly Property Layout As TypeLayout
            Get
                Return OriginalDefinition.Layout
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingCharSet As CharSet
            Get
                Return OriginalDefinition.MarshallingCharSet
            End Get
        End Property

        Public MustOverride Overrides ReadOnly Property IsUnboundGenericType As Boolean

        Public Overrides ReadOnly Property IsAnonymousType As Boolean
            Get
                Return OriginalDefinition.IsAnonymousType
            End Get
        End Property

        Public MustOverride Overrides ReadOnly Property OriginalDefinition As NamedTypeSymbol

        Public MustOverride Overrides ReadOnly Property ContainingSymbol As Symbol

        Public Overrides ReadOnly Property Arity As Integer
            Get
                Return OriginalDefinition.Arity
            End Get
        End Property

        Public MustOverride Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)

        Public MustOverride Overrides ReadOnly Property ConstructedFrom As NamedTypeSymbol

        Friend MustOverride Overrides ReadOnly Property TypeArgumentsNoUseSiteDiagnostics As ImmutableArray(Of TypeSymbol)

        Public NotOverridable Overrides Function GetTypeArgumentCustomModifiers(ordinal As Integer) As ImmutableArray(Of CustomModifier)
            Return GetEmptyTypeArgumentCustomModifiers(ordinal)
        End Function

        Friend NotOverridable Overrides ReadOnly Property HasTypeArgumentsCustomModifiers As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property EnumUnderlyingType As NamedTypeSymbol
            Get
                Return OriginalDefinition.EnumUnderlyingType
            End Get
        End Property

        Public Overrides ReadOnly Property MightContainExtensionMethods As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property HasCodeAnalysisEmbeddedAttribute As Boolean
            Get
                Return OriginalDefinition.HasCodeAnalysisEmbeddedAttribute
            End Get
        End Property

        Friend Overrides ReadOnly Property HasVisualBasicEmbeddedAttribute As Boolean
            Get
                Return OriginalDefinition.HasVisualBasicEmbeddedAttribute
            End Get
        End Property

        Friend Overrides ReadOnly Property IsExtensibleInterfaceNoUseSiteDiagnostics As Boolean
            Get
                Return OriginalDefinition.IsExtensibleInterfaceNoUseSiteDiagnostics
            End Get
        End Property

        Friend Overrides ReadOnly Property IsWindowsRuntimeImport As Boolean
            Get
                Return OriginalDefinition.IsWindowsRuntimeImport
            End Get
        End Property

        Friend Overrides ReadOnly Property ShouldAddWinRTMembers As Boolean
            Get
                Return OriginalDefinition.ShouldAddWinRTMembers
            End Get
        End Property

        Friend Overrides ReadOnly Property IsComImport As Boolean
            Get
                Return OriginalDefinition.IsComImport
            End Get
        End Property

        Friend Overrides ReadOnly Property CoClassType As TypeSymbol
            Get
                Return OriginalDefinition.CoClassType
            End Get
        End Property

        Friend Overrides Function GetAppliedConditionalSymbols() As ImmutableArray(Of String)
            Return OriginalDefinition.GetAppliedConditionalSymbols()
        End Function

        Friend Overrides Function GetAttributeUsageInfo() As AttributeUsageInfo
            Return OriginalDefinition.GetAttributeUsageInfo()
        End Function

        Friend Overrides ReadOnly Property HasDeclarativeSecurity As Boolean
            Get
                Return OriginalDefinition.HasDeclarativeSecurity
            End Get
        End Property

        Friend Overrides Function GetSecurityInformation() As IEnumerable(Of Microsoft.Cci.SecurityAttribute)
            Return OriginalDefinition.GetSecurityInformation()
        End Function

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return OriginalDefinition.DeclaredAccessibility
            End Get
        End Property

        Public Overrides ReadOnly Property TypeKind As TypeKind
            Get
                Return OriginalDefinition.TypeKind
            End Get
        End Property

        Friend Overrides ReadOnly Property IsInterface As Boolean
            Get
                Return OriginalDefinition.IsInterface
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return OriginalDefinition.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return OriginalDefinition.DeclaringSyntaxReferences
            End Get
        End Property

        Public Overrides ReadOnly Property IsMustInherit As Boolean
            Get
                Return OriginalDefinition.IsMustInherit
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotInheritable As Boolean
            Get
                Return OriginalDefinition.IsNotInheritable
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return OriginalDefinition.ObsoleteAttributeData
            End Get
        End Property

        Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return OriginalDefinition.GetAttributes()
        End Function

        Friend Overrides Function LookupMetadataType(ByRef emittedTypeName As MetadataTypeName) As NamedTypeSymbol
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend MustOverride Overrides Function InternalSubstituteTypeParameters(additionalSubstitution As TypeSubstitution) As TypeWithModifiers

        Friend Overrides Function MakeDeclaredBase(basesBeingResolved As BasesBeingResolved, diagnostics As DiagnosticBag) As NamedTypeSymbol
            Return Nothing
        End Function

        Friend Overrides Function MakeAcyclicBaseType(diagnostics As DiagnosticBag) As NamedTypeSymbol
            Return Nothing
        End Function

        Friend Overrides Function GetDirectBaseTypeNoUseSiteDiagnostics(basesBeingResolved As BasesBeingResolved) As NamedTypeSymbol
            Return Nothing
        End Function

        Friend Overrides Function GetDeclaredBase(basesBeingResolved As BasesBeingResolved) As NamedTypeSymbol
            Return Nothing
        End Function

        Friend Overrides Function MakeDeclaredInterfaces(basesBeingResolved As BasesBeingResolved, diagnostics As DiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Friend Overrides Function MakeAcyclicInterfaces(diagnostics As DiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Friend Overrides Function GetDeclaredInterfacesNoUseSiteDiagnostics(basesBeingResolved As BasesBeingResolved) As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Friend Overrides Function GetUseSiteErrorInfo() As DiagnosticInfo
            Return OriginalDefinition.GetUseSiteErrorInfo()
        End Function

        Public MustOverride Overrides Function Equals(obj As Object) As Boolean

        Public Overrides Function GetHashCode() As Integer
            Return OriginalDefinition.GetHashCode()
        End Function

        Friend Overrides ReadOnly Property CanConstruct As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides Function Construct(typeArguments As ImmutableArray(Of TypeSymbol)) As NamedTypeSymbol
            Throw New InvalidOperationException()
        End Function

        Friend Overrides ReadOnly Property DefaultPropertyName As String
            Get
                ' Properties are not members of UnboundGenericType.
                Return Nothing
            End Get
        End Property

        Friend MustOverride Overrides ReadOnly Property TypeSubstitution As TypeSubstitution

        ''' <summary>
        ''' Force all declaration errors to be generated.
        ''' </summary>
        Friend NotOverridable Overrides Sub GenerateDeclarationErrors(cancellationToken As CancellationToken)
            Throw ExceptionUtilities.Unreachable
        End Sub

        Friend NotOverridable Overrides Function GetUnificationUseSiteDiagnosticRecursive(owner As Symbol, ByRef checkedTypes As HashSet(Of TypeSymbol)) As DiagnosticInfo
            Return Nothing
        End Function

        Friend NotOverridable Overrides Function GetFieldsToEmit() As IEnumerable(Of FieldSymbol)
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend NotOverridable Overrides Function GetSynthesizedWithEventsOverrides() As IEnumerable(Of PropertySymbol)
            Return SpecializedCollections.EmptyEnumerable(Of PropertySymbol)()
        End Function

        Private NotInheritable Class ConstructedSymbol
            Inherits UnboundGenericType

            Private ReadOnly _originalDefinition As NamedTypeSymbol
            Private _lazyContainingSymbol As Symbol
            Private _lazyConstructedFrom As NamedTypeSymbol
            Private _lazyTypeArguments As ImmutableArray(Of TypeSymbol)
            Private _lazyTypeSubstitution As TypeSubstitution

            Public Sub New(originalDefinition As NamedTypeSymbol)
                MyBase.New()

                Debug.Assert(originalDefinition.IsDefinition)
                Debug.Assert(originalDefinition.IsGenericType)

                If originalDefinition.Arity = 0 Then
                    _lazyTypeArguments = ImmutableArray(Of TypeSymbol).Empty
                End If

                _originalDefinition = originalDefinition
            End Sub

            Public Overrides ReadOnly Property IsUnboundGenericType As Boolean
                Get
                    Return True
                End Get
            End Property

            Public Overrides ReadOnly Property OriginalDefinition As NamedTypeSymbol
                Get
                    Return _originalDefinition
                End Get
            End Property

            Public Overrides ReadOnly Property ContainingSymbol As Symbol
                Get
                    If _lazyContainingSymbol Is Nothing Then
                        Dim result As Symbol

                        Dim originalDefinitionContainingType As NamedTypeSymbol = OriginalDefinition.ContainingType

                        If originalDefinitionContainingType IsNot Nothing Then
                            If originalDefinitionContainingType.IsGenericType Then
                                result = UnboundGenericType.Create(originalDefinitionContainingType)
                            Else
                                result = originalDefinitionContainingType
                            End If
                        Else
                            result = OriginalDefinition.ContainingSymbol
                        End If

                        Interlocked.CompareExchange(_lazyContainingSymbol, result, Nothing)
                    End If

                    Return _lazyContainingSymbol
                End Get
            End Property

            Public Overrides ReadOnly Property ConstructedFrom As NamedTypeSymbol
                Get
                    If _lazyConstructedFrom Is Nothing Then
                        Dim result As NamedTypeSymbol
                        Dim originalDefinitionContainingType As NamedTypeSymbol = OriginalDefinition.ContainingType

                        If originalDefinitionContainingType Is Nothing OrElse Not originalDefinitionContainingType.IsGenericType Then
                            result = OriginalDefinition

                        ElseIf OriginalDefinition.Arity = 0 Then
                            result = Me
                        Else
                            result = New UnboundGenericType.ConstructedFromSymbol(Me)
                        End If

                        Interlocked.CompareExchange(_lazyConstructedFrom, result, Nothing)
                    End If

                    Return _lazyConstructedFrom
                End Get
            End Property

            Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
                Get
                    If OriginalDefinition.Arity = 0 Then
                        Return ImmutableArray(Of TypeParameterSymbol).Empty
                    End If

                    Return Me.ConstructedFrom.TypeParameters
                End Get
            End Property

            Friend Overrides ReadOnly Property TypeArgumentsNoUseSiteDiagnostics As ImmutableArray(Of TypeSymbol)
                Get
                    If _lazyTypeArguments.IsDefault Then
                        Debug.Assert(OriginalDefinition.Arity > 0)
                        Dim arguments(OriginalDefinition.Arity - 1) As TypeSymbol

                        For i As Integer = 0 To arguments.Length - 1
                            arguments(i) = UnboundTypeArgument
                        Next

                        ImmutableInterlocked.InterlockedInitialize(_lazyTypeArguments, arguments.AsImmutableOrNull())
                    End If

                    Return _lazyTypeArguments
                End Get
            End Property

            Friend Overrides ReadOnly Property TypeSubstitution As TypeSubstitution
                Get
                    If _lazyTypeSubstitution Is Nothing Then
                        Dim result As TypeSubstitution

                        Dim container As Symbol = ContainingSymbol
                        Dim containerAsConstructed = TryCast(container, UnboundGenericType.ConstructedSymbol)

                        Debug.Assert(Not Me.HasTypeArgumentsCustomModifiers)

                        If containerAsConstructed IsNot Nothing Then
                            If OriginalDefinition.Arity = 0 Then
                                result = VisualBasic.Symbols.TypeSubstitution.Concat(OriginalDefinition,
                                                                             containerAsConstructed.TypeSubstitution,
                                                                             Nothing)
                            Else
                                result = VisualBasic.Symbols.TypeSubstitution.Create(containerAsConstructed.TypeSubstitution,
                                                                             OriginalDefinition,
                                                                             Me.TypeArgumentsNoUseSiteDiagnostics)
                            End If
                        Else
                            Debug.Assert(Not (TypeOf container Is NamedTypeSymbol AndAlso
                                         DirectCast(container, NamedTypeSymbol).IsGenericType))
                            result = VisualBasic.Symbols.TypeSubstitution.Create(OriginalDefinition, OriginalDefinition.TypeParameters, Me.TypeArgumentsNoUseSiteDiagnostics)
                        End If

                        Interlocked.CompareExchange(_lazyTypeSubstitution, result, Nothing)
                    End If

                    Return _lazyTypeSubstitution
                End Get
            End Property

            Public Overrides ReadOnly Property MemberNames As IEnumerable(Of String)
                Get
                    Return New List(Of String)(From t In OriginalDefinition.GetTypeMembersUnordered() Select t.Name Distinct)
                End Get
            End Property

            Public Overrides Function GetMembers() As ImmutableArray(Of Symbol)
                ' Unbound types contain only types.
                Dim builder As ArrayBuilder(Of NamedTypeSymbol) = ArrayBuilder(Of NamedTypeSymbol).GetInstance()
                For Each member In OriginalDefinition.GetMembers() 'Using GetMembers() to enforce declaration order.
                    If member.Kind = SymbolKind.NamedType Then
                        builder.AddRange(DirectCast(member, NamedTypeSymbol))
                    End If
                Next

                Return StaticCast(Of Symbol).From(GetTypeMembers(builder.ToImmutableAndFree()))
            End Function

            Public Overrides Function GetMembers(name As String) As ImmutableArray(Of Symbol)
                ' Unbound types contain only types.
                Return StaticCast(Of Symbol).From(GetTypeMembers(name))
            End Function

            Friend Overrides Function GetTypeMembersUnordered() As ImmutableArray(Of NamedTypeSymbol)
                Return GetTypeMembers(OriginalDefinition.GetTypeMembersUnordered())
            End Function

            Public Overrides Function GetTypeMembers() As ImmutableArray(Of NamedTypeSymbol)
                Return GetTypeMembers(OriginalDefinition.GetTypeMembers())
            End Function

            Private Overloads Shared Function GetTypeMembers(originalTypeMembers As ImmutableArray(Of NamedTypeSymbol)) As ImmutableArray(Of NamedTypeSymbol)
                If originalTypeMembers.IsEmpty Then
                    Return originalTypeMembers
                End If

                Dim members(originalTypeMembers.Length - 1) As NamedTypeSymbol

                For i As Integer = 0 To members.Length - 1
                    members(i) = (New UnboundGenericType.ConstructedSymbol(originalTypeMembers(i))).ConstructedFrom
                Next

                Return members.AsImmutableOrNull()
            End Function

            Public Overrides Function GetTypeMembers(name As String) As ImmutableArray(Of NamedTypeSymbol)
                Return GetTypeMembers(OriginalDefinition.GetTypeMembers(name))
            End Function

            Public Overrides Function GetTypeMembers(name As String, arity As Integer) As ImmutableArray(Of NamedTypeSymbol)
                Return OriginalDefinition.GetTypeMembers(name, arity).SelectAsArray(Function(t) (New UnboundGenericType.ConstructedSymbol(t)).ConstructedFrom)
            End Function

            Friend Overrides Function InternalSubstituteTypeParameters(additionalSubstitution As TypeSubstitution) As TypeWithModifiers
                ' Has no effect.
                Return New TypeWithModifiers(Me)
            End Function

            Public Overrides Function Equals(obj As Object) As Boolean
                If Me Is obj Then
                    Return True
                End If

                Dim other = TryCast(obj, ConstructedSymbol)

                Return other IsNot Nothing AndAlso other.OriginalDefinition.Equals(OriginalDefinition)
            End Function

        End Class

        Private NotInheritable Class ConstructedFromSymbol
            Inherits UnboundGenericType

            Public ReadOnly Constructed As ConstructedSymbol
            Private ReadOnly _typeParameters As ImmutableArray(Of TypeParameterSymbol)
            Private ReadOnly _typeSubstitution As TypeSubstitution

            Public Sub New(constructed As ConstructedSymbol)
                MyBase.New()

                Dim originalDefinition As NamedTypeSymbol = constructed.OriginalDefinition
                Debug.Assert(originalDefinition.Arity > 0)
                Me.Constructed = constructed

                Dim typeParametersDefinitions As ImmutableArray(Of TypeParameterSymbol) = originalDefinition.TypeParameters
                Dim alphaRenamedTypeParameters = New SubstitutedTypeParameterSymbol(typeParametersDefinitions.Length - 1) {}

                For i As Integer = 0 To typeParametersDefinitions.Length - 1 Step 1
                    alphaRenamedTypeParameters(i) = New SubstitutedTypeParameterSymbol(typeParametersDefinitions(i))
                Next

                Dim newTypeParameters = alphaRenamedTypeParameters.AsImmutableOrNull()
                Dim container = DirectCast(constructed.ContainingSymbol, ConstructedSymbol)

                _typeParameters = StaticCast(Of TypeParameterSymbol).From(newTypeParameters)

                ' Add a substitution to map from type parameter definitions to corresponding
                ' alpha-renamed type parameters.
                Dim substitution = VisualBasic.Symbols.TypeSubstitution.CreateForAlphaRename(container.TypeSubstitution,
                                                                         StaticCast(Of TypeParameterSymbol).From(newTypeParameters))

                Debug.Assert(substitution.TargetGenericDefinition Is originalDefinition)

                ' Set container for type parameters
                For Each param In alphaRenamedTypeParameters
                    param.SetContainingSymbol(Me)
                Next

                _typeSubstitution = substitution
            End Sub

            Public Overrides ReadOnly Property IsUnboundGenericType As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property OriginalDefinition As NamedTypeSymbol
                Get
                    Return Constructed.OriginalDefinition
                End Get
            End Property

            Public Overrides ReadOnly Property ConstructedFrom As NamedTypeSymbol
                Get
                    Return Me
                End Get
            End Property

            Public Overrides ReadOnly Property ContainingSymbol As Symbol
                Get
                    Return Constructed.ContainingSymbol
                End Get
            End Property

            Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
                Get
                    Return _typeParameters
                End Get
            End Property

            Friend Overrides ReadOnly Property TypeArgumentsNoUseSiteDiagnostics As ImmutableArray(Of TypeSymbol)
                Get
                    Return StaticCast(Of TypeSymbol).From(_typeParameters)
                End Get
            End Property

            Friend Overrides ReadOnly Property TypeSubstitution As TypeSubstitution
                Get
                    Return _typeSubstitution
                End Get
            End Property

            Public Overrides ReadOnly Property MemberNames As IEnumerable(Of String)
                Get
                    Return SpecializedCollections.EmptyCollection(Of String)()
                End Get
            End Property

            Public Overrides Function GetMembers() As ImmutableArray(Of Symbol)
                Return ImmutableArray(Of Symbol).Empty
            End Function

            Public Overrides Function GetMembers(name As String) As ImmutableArray(Of Symbol)
                Return ImmutableArray(Of Symbol).Empty
            End Function

            Public Overrides Function GetTypeMembers() As ImmutableArray(Of NamedTypeSymbol)
                Return ImmutableArray(Of NamedTypeSymbol).Empty
            End Function

            Public Overrides Function GetTypeMembers(name As String) As ImmutableArray(Of NamedTypeSymbol)
                Return ImmutableArray(Of NamedTypeSymbol).Empty
            End Function

            Public Overrides Function GetTypeMembers(name As String, arity As Integer) As ImmutableArray(Of NamedTypeSymbol)
                Return ImmutableArray(Of NamedTypeSymbol).Empty
            End Function

            Friend Overrides Function InternalSubstituteTypeParameters(additionalSubstitution As TypeSubstitution) As TypeWithModifiers
                Throw ExceptionUtilities.Unreachable
            End Function

            Public Overrides Function Equals(obj As Object) As Boolean
                If Me Is obj Then
                    Return True
                End If

                Dim other = TryCast(obj, ConstructedFromSymbol)

                Return other IsNot Nothing AndAlso other.Constructed.OriginalDefinition.Equals(Constructed.OriginalDefinition)
            End Function
        End Class
    End Class

End Namespace
