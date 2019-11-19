' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Partial Friend NotInheritable Class AnonymousTypeManager

        Friend NotInheritable Class NameAndIndex
            Public Sub New(name As String, index As Integer)
                Me.Name = name
                Me.Index = index
            End Sub

            Public ReadOnly Name As String
            Public ReadOnly Index As Integer
        End Class

        Friend MustInherit Class AnonymousTypeOrDelegateTemplateSymbol
            Inherits InstanceTypeSymbol

            Public ReadOnly Manager As AnonymousTypeManager

            ''' <summary>
            ''' The name used to emit definition of the type. Will be set when the type's 
            ''' metadata is ready to be emitted, Name property will throw exception if this field 
            ''' is queried before that moment because the name is not defined yet.
            ''' </summary>
            Private _nameAndIndex As NameAndIndex

            Private ReadOnly _typeParameters As ImmutableArray(Of TypeParameterSymbol)
            Private _adjustedPropertyNames As LocationAndNames

            ''' <summary>
            ''' The key of the anonymous type descriptor used for this type template
            ''' </summary>
            Friend ReadOnly TypeDescriptorKey As String

            Protected Sub New(
                manager As AnonymousTypeManager,
                typeDescr As AnonymousTypeDescriptor
            )
                Debug.Assert(TypeKind = TypeKind.Class OrElse TypeKind = TypeKind.Delegate)
                Me.Manager = manager
                Me.TypeDescriptorKey = typeDescr.Key
                _adjustedPropertyNames = New LocationAndNames(typeDescr)

                Dim arity As Integer = typeDescr.Fields.Length

                If TypeKind = TypeKind.Delegate AndAlso typeDescr.Fields.IsSubDescription() Then
                    ' It is a Sub, don't need type parameter for the return type of the Invoke.
                    arity -= 1
                End If

                ' Create type parameters
                If arity = 0 Then
                    Debug.Assert(TypeKind = TypeKind.Delegate)
                    Debug.Assert(typeDescr.Parameters.Length = 1)
                    Debug.Assert(typeDescr.Parameters.IsSubDescription())
                    _typeParameters = ImmutableArray(Of TypeParameterSymbol).Empty
                Else
                    Dim typeParameters = New TypeParameterSymbol(arity - 1) {}

                    For ordinal = 0 To arity - 1
                        typeParameters(ordinal) = New AnonymousTypeOrDelegateTypeParameterSymbol(Me, ordinal)
                    Next

                    _typeParameters = typeParameters.AsImmutable()
                End If
            End Sub

            Friend MustOverride Function GetAnonymousTypeKey() As AnonymousTypeKey

            Public Overrides ReadOnly Property Name As String
                Get
                    Return _nameAndIndex.Name
                End Get
            End Property

            Friend Overrides ReadOnly Property MangleName As Boolean
                Get
                    Return _typeParameters.Length > 0
                End Get
            End Property

            Friend NotOverridable Overrides ReadOnly Property HasSpecialName As Boolean
                Get
                    Return False
                End Get
            End Property

            Public NotOverridable Overrides ReadOnly Property IsSerializable As Boolean
                Get
                    Return False
                End Get
            End Property

            Friend Overrides ReadOnly Property Layout As TypeLayout
                Get
                    Return Nothing
                End Get
            End Property

            Friend Overrides ReadOnly Property MarshallingCharSet As CharSet
                Get
                    Return DefaultMarshallingCharSet
                End Get
            End Property

            Public MustOverride Overrides ReadOnly Property TypeKind As TypeKind

            Public Overrides ReadOnly Property Arity As Integer
                Get
                    Return _typeParameters.Length
                End Get
            End Property

            Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
                Get
                    Return _typeParameters
                End Get
            End Property

            Public Overrides ReadOnly Property IsMustInherit As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property IsNotInheritable As Boolean
                Get
                    Return True
                End Get
            End Property

            Public Overrides ReadOnly Property MightContainExtensionMethods As Boolean
                Get
                    Return False
                End Get
            End Property

            Friend Overrides ReadOnly Property HasCodeAnalysisEmbeddedAttribute As Boolean
                Get
                    Return False
                End Get
            End Property

            Friend Overrides ReadOnly Property HasVisualBasicEmbeddedAttribute As Boolean
                Get
                    Return False
                End Get
            End Property

            Friend Overrides ReadOnly Property IsExtensibleInterfaceNoUseSiteDiagnostics As Boolean
                Get
                    Return False
                End Get
            End Property

            Friend Overrides ReadOnly Property IsWindowsRuntimeImport As Boolean
                Get
                    Return False
                End Get
            End Property

            Friend Overrides ReadOnly Property ShouldAddWinRTMembers As Boolean
                Get
                    Return False
                End Get
            End Property

            Friend Overrides ReadOnly Property IsComImport As Boolean
                Get
                    Return False
                End Get
            End Property

            Friend Overrides ReadOnly Property CoClassType As TypeSymbol
                Get
                    Return Nothing
                End Get
            End Property

            Friend Overrides Function GetAppliedConditionalSymbols() As ImmutableArray(Of String)
                Return ImmutableArray(Of String).Empty
            End Function

            Friend Overrides Function GetAttributeUsageInfo() As AttributeUsageInfo
                Throw ExceptionUtilities.Unreachable
            End Function

            Friend Overrides ReadOnly Property HasDeclarativeSecurity As Boolean
                Get
                    Return False
                End Get
            End Property

            Friend Overrides Function GetSecurityInformation() As IEnumerable(Of SecurityAttribute)
                Throw ExceptionUtilities.Unreachable
            End Function

            Friend Overrides ReadOnly Property DefaultPropertyName As String
                Get
                    Return Nothing
                End Get
            End Property

            Friend Overrides Function MakeDeclaredBase(basesBeingResolved As BasesBeingResolved, diagnostics As DiagnosticBag) As NamedTypeSymbol
                Return MakeAcyclicBaseType(diagnostics)
            End Function

            Friend Overrides Function MakeDeclaredInterfaces(basesBeingResolved As BasesBeingResolved, diagnostics As DiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
                Return MakeAcyclicInterfaces(diagnostics)
            End Function

            Public Overrides Function GetMembers(name As String) As ImmutableArray(Of Symbol)
                ' TODO - Perf
                Return ImmutableArray.CreateRange(From member In GetMembers() Where CaseInsensitiveComparison.Equals(member.Name, name))
            End Function

            Public Overrides ReadOnly Property MemberNames As IEnumerable(Of String)
                Get
                    ' TODO - Perf
                    Return New HashSet(Of String)(From member In GetMembers() Select member.Name)
                End Get
            End Property

            Public Overrides ReadOnly Property ContainingSymbol As Symbol
                Get
                    Return Me.Manager.ContainingModule.GlobalNamespace
                End Get
            End Property

            Public Overrides ReadOnly Property ContainingType As NamedTypeSymbol
                Get
                    ' always global
                    Return Nothing
                End Get
            End Property

            Public Overrides Function GetTypeMembers() As ImmutableArray(Of NamedTypeSymbol)
                Return ImmutableArray(Of NamedTypeSymbol).Empty
            End Function

            Public Overrides Function GetTypeMembers(name As String) As ImmutableArray(Of NamedTypeSymbol)
                Return ImmutableArray(Of NamedTypeSymbol).Empty
            End Function

            Public Overrides Function GetTypeMembers(name As String, arity As Integer) As ImmutableArray(Of NamedTypeSymbol)
                Return ImmutableArray(Of NamedTypeSymbol).Empty
            End Function

            Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
                Get
                    Return Accessibility.Friend
                End Get
            End Property

            Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
                Get
                    Return ImmutableArray(Of Location).Empty
                End Get
            End Property

            Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
                Get
                    Return ImmutableArray(Of SyntaxReference).Empty
                End Get
            End Property

            Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
                Get
                    Return True
                End Get
            End Property

            Friend NotOverridable Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
                Get
                    Return Nothing
                End Get
            End Property

            Friend Property NameAndIndex As NameAndIndex
                Get
                    Return _nameAndIndex
                End Get
                Set(value As NameAndIndex)
                    Dim oldValue = Interlocked.CompareExchange(Me._nameAndIndex, value, Nothing)
                    Debug.Assert(oldValue Is Nothing OrElse
                                 (oldValue.Name = value.Name AndAlso oldValue.Index = value.Index))
                End Set
            End Property

            Friend MustOverride ReadOnly Property GeneratedNamePrefix As String

            ''' <summary> Describes the type descriptor location and property/parameter names associated with this location </summary>
            Private NotInheritable Class LocationAndNames

                Public ReadOnly Location As Location
                Public ReadOnly Names As ImmutableArray(Of String)

                Public Sub New(typeDescr As AnonymousTypeDescriptor)
                    Me.Location = typeDescr.Location
                    Me.Names = typeDescr.Fields.SelectAsArray(Function(d) d.Name)
                End Sub

            End Class

            Public ReadOnly Property SmallestLocation As Location
                Get
                    Return Me._adjustedPropertyNames.Location
                End Get
            End Property

            ''' <summary>
            ''' In emit phase every time a created anonymous type is referenced we try to adjust name of 
            ''' template's fields as well as store the lowest location of the template. The last one will 
            ''' be used for ordering templates and assigning emitted type names.
            ''' </summary>
            Friend Sub AdjustMetadataNames(typeDescr As AnonymousTypeDescriptor)

                ' adjust template location only for type descriptors from source 
                Dim newLocation As Location = typeDescr.Location
                Debug.Assert(newLocation.IsInSource)

                Do
                    ' Loop until we managed to set location and names OR we detected that we don't need 
                    ' to set it ('location' in type descriptor is bigger that the one in m_adjustedPropertyNames)
                    Dim currentAdjustedNames As LocationAndNames = Me._adjustedPropertyNames
                    If currentAdjustedNames IsNot Nothing AndAlso
                            Me.Manager.Compilation.CompareSourceLocations(currentAdjustedNames.Location, newLocation) < 0 Then

                        ' The template's adjusted property names do not need to be changed
                        Exit Sub
                    End If

                    Dim newAdjustedNames As New LocationAndNames(typeDescr)

                    If Interlocked.CompareExchange(Me._adjustedPropertyNames, newAdjustedNames, currentAdjustedNames) Is currentAdjustedNames Then
                        ' Changed successfully, proceed to updating the fields
                        Exit Do
                    End If
                Loop
            End Sub

            Friend Function GetAdjustedName(index As Integer) As String
                Dim names = Me._adjustedPropertyNames
                Debug.Assert(names IsNot Nothing)
                Debug.Assert(names.Names.Length > index)
                Return names.Names(index)
            End Function

            ''' <summary>
            ''' Force all declaration errors to be generated.
            ''' </summary>
            Friend Overrides Sub GenerateDeclarationErrors(cancellationToken As CancellationToken)
                Throw ExceptionUtilities.Unreachable
            End Sub

            Friend NotOverridable Overrides Function GetSynthesizedWithEventsOverrides() As IEnumerable(Of PropertySymbol)
                Return SpecializedCollections.EmptyEnumerable(Of PropertySymbol)()
            End Function
        End Class

    End Class
End Namespace
