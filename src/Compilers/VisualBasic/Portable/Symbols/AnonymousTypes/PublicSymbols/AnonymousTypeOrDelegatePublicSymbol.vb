' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Partial Friend NotInheritable Class AnonymousTypeManager

        Friend MustInherit Class AnonymousTypeOrDelegatePublicSymbol
            Inherits InstanceTypeSymbol

            Public ReadOnly Manager As AnonymousTypeManager
            Public ReadOnly TypeDescriptor As AnonymousTypeDescriptor

            Protected Sub New(manager As AnonymousTypeManager, typeDescr As AnonymousTypeDescriptor)
                typeDescr.AssertGood()
                Debug.Assert((TypeKind = TypeKind.Class AndAlso TypeOf Me Is AnonymousTypePublicSymbol) OrElse
                             (TypeKind = TypeKind.Delegate AndAlso TypeOf Me Is AnonymousDelegatePublicSymbol))

                Me.Manager = manager
                Me.TypeDescriptor = typeDescr
            End Sub

            Public NotOverridable Overrides ReadOnly Property Name As String
                Get
                    Return String.Empty
                End Get
            End Property

            Friend NotOverridable Overrides ReadOnly Property MangleName As Boolean
                Get
                    Return False
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

            Public MustOverride Overrides ReadOnly Property TypeKind As TYPEKIND

            Public Overrides ReadOnly Property Arity As Integer
                Get
                    Return 0
                End Get
            End Property

            Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
                Get
                    Return ImmutableArray(Of TypeParameterSymbol).Empty
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

            Friend Overrides ReadOnly Property DefaultPropertyName As String
                Get
                    Return Nothing
                End Get
            End Property

            Friend Overrides Function MakeDeclaredBase(basesBeingResolved As BasesBeingResolved, diagnostics As BindingDiagnosticBag) As NamedTypeSymbol
                Return MakeAcyclicBaseType(diagnostics)
            End Function

            Friend Overrides Function MakeDeclaredInterfaces(basesBeingResolved As BasesBeingResolved, diagnostics As BindingDiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
                Return MakeAcyclicInterfaces(diagnostics)
            End Function

            Public Overrides Function GetMembers(name As String) As ImmutableArray(Of Symbol)
                ' TODO - Perf
                Return ImmutableArray.CreateRange(Of Symbol)(From member In GetMembers() Where CaseInsensitiveComparison.Equals(member.Name, name))
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
                    ' this type is always global
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
                    Return ImmutableArray.Create(Of Location)(Me.TypeDescriptor.Location)
                End Get
            End Property

            Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
                Get
                    Return ImmutableArray(Of SyntaxReference).Empty
                End Get
            End Property

            Public Overrides ReadOnly Property IsAnonymousType As Boolean
                Get
                    Return True
                End Get
            End Property

            Public NotOverridable Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
                Get
                    Return Me.TypeDescriptor.IsImplicitlyDeclared
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

            Friend NotOverridable Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
                Get
                    Return Nothing
                End Get
            End Property

            Friend Overrides Function GetSecurityInformation() As IEnumerable(Of Microsoft.Cci.SecurityAttribute)
                Throw ExceptionUtilities.Unreachable
            End Function

            ''' <summary>
            ''' Force all declaration errors to be generated.
            ''' </summary>
            Friend Overrides Sub GenerateDeclarationErrors(cancellationToken As CancellationToken)
                Throw ExceptionUtilities.Unreachable
            End Sub

            Friend MustOverride Overrides Function InternalSubstituteTypeParameters(substitution As TypeSubstitution) As TypeWithModifiers

            Friend NotOverridable Overrides Function GetFieldsToEmit() As IEnumerable(Of FieldSymbol)
                Throw ExceptionUtilities.Unreachable
            End Function

            ''' <summary> 
            ''' Map an anonymous type or delegate public symbol to an implementation symbol to be 
            ''' used in emit.
            ''' 
            ''' NOTE: All anonymous types/delegated (except for delegate with signature 'Sub()') with the 
            ''' same set of fields/parameters (field names and IsKey flags are taken into account) are 
            ''' generated based on the same generic type template.
            ''' </summary>
            Public MustOverride Function MapToImplementationSymbol() As NamedTypeSymbol

            ''' <summary> 
            ''' Map an anonymous type or delegate's method symbol to an implementation method symbol to be used in emit
            ''' </summary>
            Public Function MapMethodToImplementationSymbol(method As MethodSymbol) As MethodSymbol
                Debug.Assert(method.ContainingType Is Me)
                Return FindMethodInTypeProvided(method, MapToImplementationSymbol())
            End Function

            ''' <summary> 
            ''' Map an anonymous type or delegate's method symbol to a substituted method symbol.
            ''' </summary>
            Public Function FindSubstitutedMethodSymbol(method As MethodSymbol) As MethodSymbol
                Debug.Assert(method.ContainingType.IsAnonymousType)
                Return FindMethodInTypeProvided(method, Me)
            End Function

            Private Shared Function FindMethodInTypeProvided(method As MethodSymbol, type As NamedTypeSymbol) As MethodSymbol
                If type.IsDefinition Then
                    ' Find a method by name 

                    ' Get method's index in owning type members
                    Dim index As Integer = 0
                    For Each member In method.ContainingType.GetMembers()
                        If member Is method Then
                            Exit For
                        End If
                        index += 1
                    Next
                    Debug.Assert(index < method.ContainingType.GetMembers().Length)

                    ' Get a method in the type provided by it's index
                    ' WARNING: this functionality assumes that 'type' has the 
                    '          same method indexes as the original type
                    Dim mappedMethod As MethodSymbol = DirectCast(type.GetMembers()(index), MethodSymbol)
                    Debug.Assert(IdentifierComparison.Equals(method.Name, mappedMethod.Name))
                    Debug.Assert(method.OverriddenMethod Is mappedMethod.OverriddenMethod)
                    Return mappedMethod

                Else
                    Dim otherTypeDef As NamedTypeSymbol = type.OriginalDefinition
                    Dim methodInDefinition = FindMethodInTypeProvided(method, otherTypeDef)
                    Return DirectCast(DirectCast(type, SubstitutedNamedType).GetMemberForDefinition(methodInDefinition), MethodSymbol)
                End If

                Throw ExceptionUtilities.Unreachable
            End Function

            Friend NotOverridable Overrides Function GetSynthesizedWithEventsOverrides() As IEnumerable(Of PropertySymbol)
                Return SpecializedCollections.EmptyEnumerable(Of PropertySymbol)()
            End Function

            Public NotOverridable Overrides Function Equals(other As TypeSymbol, comparison As TypeCompareKind) As Boolean
                Return Equals(TryCast(other, AnonymousTypeOrDelegatePublicSymbol), comparison)
            End Function

            Public Overloads Function Equals(other As AnonymousTypeOrDelegatePublicSymbol, comparison As TypeCompareKind) As Boolean
                If Me Is other Then
                    Return True
                End If

                Return other IsNot Nothing AndAlso Me.TypeKind = other.TypeKind AndAlso Me.TypeDescriptor.Equals(other.TypeDescriptor, comparison)
            End Function

            Public NotOverridable Overrides Function GetHashCode() As Integer
                Return Hash.Combine(Me.TypeDescriptor.GetHashCode(), TypeKind)
            End Function

            Friend NotOverridable Overrides ReadOnly Property HasAnyDeclaredRequiredMembers As Boolean
                Get
                    Return False
                End Get
            End Property
        End Class
    End Class
End Namespace
