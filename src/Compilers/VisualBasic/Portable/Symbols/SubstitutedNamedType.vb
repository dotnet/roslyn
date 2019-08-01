' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' A SubstitutedNamedType represents a named type that has had some sort
    ''' of substitution applied to it. I.e., its not a pure instance type, but at least
    ''' one type parameter in this type or a containing type has a substitution made for
    ''' it. 
    ''' </summary>
    Friend MustInherit Class SubstitutedNamedType
        Inherits NamedTypeSymbol

        ''' <summary>
        ''' Type substitution for this symbol, it targets OriginalDefinition of the symbol.
        ''' </summary>
        Private ReadOnly _substitution As TypeSubstitution

        Private Sub New(substitution As TypeSubstitution)
            Debug.Assert(substitution IsNot Nothing AndAlso substitution.TargetGenericDefinition.IsDefinition)
            _substitution = substitution
        End Sub

        Public NotOverridable Overrides ReadOnly Property Name As String
            Get
                Return OriginalDefinition.Name
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property MangleName As Boolean
            Get
                Return OriginalDefinition.MangleName
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property MetadataName As String
            Get
                Return OriginalDefinition.MetadataName
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property DefaultPropertyName As String
            Get
                Return OriginalDefinition.DefaultPropertyName
            End Get
        End Property

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

        Friend NotOverridable Overrides ReadOnly Property TypeSubstitution As TypeSubstitution
            Get
                Return _substitution
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property OriginalDefinition As NamedTypeSymbol
            Get
                Return DirectCast(_substitution.TargetGenericDefinition, NamedTypeSymbol)
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property ContainingAssembly As AssemblySymbol
            Get
                Return OriginalDefinition.ContainingAssembly
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property Arity As Integer
            Get
                Return OriginalDefinition.Arity
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return OriginalDefinition.DeclaredAccessibility
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsMustInherit As Boolean
            Get
                Return OriginalDefinition.IsMustInherit
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsNotInheritable As Boolean
            Get
                Return OriginalDefinition.IsNotInheritable
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return OriginalDefinition.IsImplicitlyDeclared
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property EmbeddedSymbolKind As EmbeddedSymbolKind
            Get
                Return OriginalDefinition.EmbeddedSymbolKind
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property MightContainExtensionMethods As Boolean
            Get
                Return OriginalDefinition.MightContainExtensionMethods
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property HasCodeAnalysisEmbeddedAttribute As Boolean
            Get
                Return OriginalDefinition.HasCodeAnalysisEmbeddedAttribute
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property HasVisualBasicEmbeddedAttribute As Boolean
            Get
                Return OriginalDefinition.HasVisualBasicEmbeddedAttribute
            End Get
        End Property

        Friend Overrides ReadOnly Property IsExtensibleInterfaceNoUseSiteDiagnostics As Boolean
            Get
                Return OriginalDefinition.IsExtensibleInterfaceNoUseSiteDiagnostics
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property IsWindowsRuntimeImport As Boolean
            Get
                Return OriginalDefinition.IsWindowsRuntimeImport
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property ShouldAddWinRTMembers As Boolean
            Get
                Return OriginalDefinition.ShouldAddWinRTMembers
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property IsComImport As Boolean
            Get
                Return OriginalDefinition.IsComImport
            End Get
        End Property

        Friend Overrides ReadOnly Property CoClassType As TypeSymbol
            Get
                Return OriginalDefinition.CoClassType
            End Get
        End Property

        Friend NotOverridable Overrides Function GetAppliedConditionalSymbols() As ImmutableArray(Of String)
            Return OriginalDefinition.GetAppliedConditionalSymbols()
        End Function

        Friend NotOverridable Overrides Function GetAttributeUsageInfo() As AttributeUsageInfo
            Return OriginalDefinition.GetAttributeUsageInfo()
        End Function

        Friend NotOverridable Overrides ReadOnly Property HasDeclarativeSecurity As Boolean
            Get
                Return OriginalDefinition.HasDeclarativeSecurity
            End Get
        End Property

        Friend NotOverridable Overrides Function GetSecurityInformation() As IEnumerable(Of Microsoft.Cci.SecurityAttribute)
            Return OriginalDefinition.GetSecurityInformation()
        End Function

        Public NotOverridable Overrides ReadOnly Property TypeKind As TypeKind
            Get
                Return OriginalDefinition.TypeKind
            End Get
        End Property

        Friend Overrides ReadOnly Property IsInterface As Boolean
            Get
                Return OriginalDefinition.IsInterface
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return OriginalDefinition.Locations
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return OriginalDefinition.DeclaringSyntaxReferences
            End Get
        End Property

        Public NotOverridable Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return OriginalDefinition.GetAttributes()
        End Function

        Public NotOverridable Overrides ReadOnly Property EnumUnderlyingType As NamedTypeSymbol
            Get
                Return OriginalDefinition.EnumUnderlyingType
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return OriginalDefinition.ObsoleteAttributeData
            End Get
        End Property

        Friend NotOverridable Overrides Function MakeDeclaredBase(basesBeingResolved As ConsList(Of Symbol), diagnostics As DiagnosticBag) As NamedTypeSymbol
            Return DirectCast(OriginalDefinition.GetDeclaredBase(basesBeingResolved).InternalSubstituteTypeParameters(_substitution).AsTypeSymbolOnly(), NamedTypeSymbol)
        End Function

        Friend NotOverridable Overrides Function MakeDeclaredInterfaces(basesBeingResolved As ConsList(Of Symbol), diagnostics As DiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
            Dim instanceInterfaces = OriginalDefinition.GetDeclaredInterfacesNoUseSiteDiagnostics(basesBeingResolved)

            If instanceInterfaces.Length = 0 Then
                Return ImmutableArray(Of NamedTypeSymbol).Empty

            Else
                Dim substitutedInterfaces = New NamedTypeSymbol(instanceInterfaces.Length - 1) {}

                For i As Integer = 0 To instanceInterfaces.Length - 1 Step 1
                    substitutedInterfaces(i) = DirectCast(instanceInterfaces(i).InternalSubstituteTypeParameters(_substitution).AsTypeSymbolOnly(), NamedTypeSymbol)
                Next

                Return substitutedInterfaces.AsImmutableOrNull
            End If

        End Function

        Friend NotOverridable Overrides Function MakeAcyclicBaseType(diagnostics As DiagnosticBag) As NamedTypeSymbol
            Dim fullBase = OriginalDefinition.BaseTypeNoUseSiteDiagnostics

            If fullBase IsNot Nothing Then
                Return DirectCast(fullBase.InternalSubstituteTypeParameters(_substitution).AsTypeSymbolOnly(), NamedTypeSymbol)
            End If

            Return Nothing
        End Function

        Friend NotOverridable Overrides Function MakeAcyclicInterfaces(diagnostics As DiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
            Dim instanceInterfaces = OriginalDefinition.InterfacesNoUseSiteDiagnostics

            If instanceInterfaces.Length = 0 Then
                Return ImmutableArray(Of NamedTypeSymbol).Empty

            Else
                Dim substitutedInterfaces = New NamedTypeSymbol(instanceInterfaces.Length - 1) {}

                For i As Integer = 0 To instanceInterfaces.Length - 1 Step 1
                    substitutedInterfaces(i) = DirectCast(instanceInterfaces(i).InternalSubstituteTypeParameters(_substitution).AsTypeSymbolOnly(), NamedTypeSymbol)
                Next

                Return substitutedInterfaces.AsImmutableOrNull()
            End If
        End Function

        Private Overloads Function SubstituteTypeParametersForMemberType(memberType As NamedTypeSymbol) As NamedTypeSymbol
            Debug.Assert(memberType.IsDefinition AndAlso memberType.ContainingSymbol Is Me.OriginalDefinition)

            If memberType.Arity = 0 Then
                Return SpecializedNonGenericType.Create(Me, memberType, _substitution)
            End If

            Return SpecializedGenericType.Create(Me, memberType)
        End Function

        Protected Overridable Function SubstituteTypeParametersForMemberMethod(memberMethod As MethodSymbol) As SubstitutedMethodSymbol
            If memberMethod.Arity > 0 Then
                Return SubstitutedMethodSymbol.SpecializedGenericMethod.Create(Me, memberMethod)
            End If

            Return New SubstitutedMethodSymbol.SpecializedNonGenericMethod(Me, memberMethod)
        End Function

        Protected Overridable Function SubstituteTypeParametersForMemberField(memberField As FieldSymbol) As SubstitutedFieldSymbol
            Return New SubstitutedFieldSymbol(Me, memberField)
        End Function

        Private Function SubstituteTypeParametersForMemberProperty(memberProperty As PropertySymbol) As SubstitutedPropertySymbol
            Dim getMethod = If(memberProperty.GetMethod Is Nothing, Nothing, SubstituteTypeParametersForMemberMethod(memberProperty.GetMethod))
            Dim setMethod = If(memberProperty.SetMethod Is Nothing, Nothing, SubstituteTypeParametersForMemberMethod(memberProperty.SetMethod))
            Dim associatedField = If(memberProperty.AssociatedField Is Nothing, Nothing, SubstituteTypeParametersForMemberField(memberProperty.AssociatedField))

            Return New SubstitutedPropertySymbol(Me, memberProperty, getMethod, setMethod, associatedField)
        End Function

        Private Function SubstituteTypeParametersForMemberEvent(memberEvent As EventSymbol) As SubstitutedEventSymbol
            Dim addMethod = If(memberEvent.AddMethod Is Nothing, Nothing, SubstituteTypeParametersForMemberMethod(memberEvent.AddMethod))
            Dim removeMethod = If(memberEvent.RemoveMethod Is Nothing, Nothing, SubstituteTypeParametersForMemberMethod(memberEvent.RemoveMethod))
            Dim raiseMethod = If(memberEvent.RaiseMethod Is Nothing, Nothing, SubstituteTypeParametersForMemberMethod(memberEvent.RaiseMethod))
            Dim associatedField = If(memberEvent.AssociatedField Is Nothing, Nothing, SubstituteTypeParametersForMemberField(memberEvent.AssociatedField))

            Return CreateSubstitutedEventSymbol(memberEvent, addMethod, removeMethod, raiseMethod, associatedField)
        End Function

        Public Overrides ReadOnly Property MemberNames As IEnumerable(Of String)
            Get
                Return OriginalDefinition.MemberNames
            End Get
        End Property

        Public Overrides Function GetMembers() As ImmutableArray(Of Symbol)
            Dim members = OriginalDefinition.GetMembers()

            Return GetMembers_Worker(members)
        End Function

        Friend Overrides Function GetMembersUnordered() As ImmutableArray(Of Symbol)
            Dim members = OriginalDefinition.GetMembersUnordered()

            Return GetMembers_Worker(members)
        End Function

        Private Function GetMembers_Worker(members As ImmutableArray(Of Symbol)) As ImmutableArray(Of Symbol)
            Dim result = ArrayBuilder(Of Symbol).GetInstance()

            ' Substitute methods first to ensure accessor methods are
            ' available when constructing properties and events.
            Dim methodSubstitutions = members.OfType(Of MethodSymbol)().ToDictionary(Function(m) m, Function(m) SubstituteTypeParametersForMemberMethod(m))

            ' Substitute remaining members.
            For Each member In members
                Select Case member.Kind
                    Case SymbolKind.NamedType
                        result.Add(SubstituteTypeParametersForMemberType(DirectCast(member, NamedTypeSymbol)))

                    Case SymbolKind.Method
                        result.Add(methodSubstitutions(DirectCast(member, MethodSymbol)))

                    Case SymbolKind.Property
                        Dim memberProperty = DirectCast(member, PropertySymbol)
                        Dim getMethod = GetMethodSubstitute(methodSubstitutions, memberProperty.GetMethod)
                        Dim setMethod = GetMethodSubstitute(methodSubstitutions, memberProperty.SetMethod)
                        Dim associatedField = If(memberProperty.AssociatedField Is Nothing, Nothing, SubstituteTypeParametersForMemberField(memberProperty.AssociatedField))
                        result.Add(New SubstitutedPropertySymbol(Me, memberProperty, getMethod, setMethod, associatedField))

                    Case SymbolKind.Event
                        Dim memberEvent = DirectCast(member, EventSymbol)
                        Dim addMethod = GetMethodSubstitute(methodSubstitutions, memberEvent.AddMethod)
                        Dim removeMethod = GetMethodSubstitute(methodSubstitutions, memberEvent.RemoveMethod)
                        Dim raiseMethod = GetMethodSubstitute(methodSubstitutions, memberEvent.RaiseMethod)
                        Dim associatedField = If(memberEvent.AssociatedField Is Nothing, Nothing, SubstituteTypeParametersForMemberField(memberEvent.AssociatedField))

                        result.Add(CreateSubstitutedEventSymbol(memberEvent, addMethod, removeMethod, raiseMethod, associatedField))

                    Case SymbolKind.Field
                        result.Add(SubstituteTypeParametersForMemberField(DirectCast(member, FieldSymbol)))

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(member.Kind)
                End Select
            Next

            Return result.ToImmutableAndFree()
        End Function

        Protected Overridable Function CreateSubstitutedEventSymbol(memberEvent As EventSymbol,
                                                               addMethod As SubstitutedMethodSymbol,
                                                               removeMethod As SubstitutedMethodSymbol,
                                                               raiseMethod As SubstitutedMethodSymbol,
                                                               associatedField As SubstitutedFieldSymbol) As SubstitutedEventSymbol

            Return New SubstitutedEventSymbol(Me, memberEvent, addMethod, removeMethod, raiseMethod, associatedField)
        End Function

        Private Shared Function GetMethodSubstitute(methodSubstitutions As Dictionary(Of MethodSymbol, SubstitutedMethodSymbol), method As MethodSymbol) As SubstitutedMethodSymbol
            Return If((method Is Nothing), Nothing, methodSubstitutions(method))
        End Function

        Public Overrides Function GetMembers(name As String) As ImmutableArray(Of Symbol)
            ' TODO - Perf
            Return OriginalDefinition.GetMembers(name).SelectAsArray(Function(member, self) self.SubstituteTypeParametersInMember(member), Me)
        End Function

        Friend Overrides Function GetTypeMembersUnordered() As ImmutableArray(Of NamedTypeSymbol)
            Return OriginalDefinition.GetTypeMembersUnordered().SelectAsArray(Function(nestedType, self) self.SubstituteTypeParametersForMemberType(nestedType), Me)
        End Function

        Public Overrides Function GetTypeMembers() As ImmutableArray(Of NamedTypeSymbol)
            Return OriginalDefinition.GetTypeMembers().SelectAsArray(Function(nestedType, self) self.SubstituteTypeParametersForMemberType(nestedType), Me)
        End Function

        Public Overrides Function GetTypeMembers(name As String) As ImmutableArray(Of NamedTypeSymbol)
            ' TODO - Perf
            Return OriginalDefinition.GetTypeMembers(name).SelectAsArray(Function(nestedType, self) self.SubstituteTypeParametersForMemberType(nestedType), Me)
        End Function

        Public Overrides Function GetTypeMembers(name As String, arity As Integer) As ImmutableArray(Of NamedTypeSymbol)
            Return OriginalDefinition.GetTypeMembers(name, arity).SelectAsArray(Function(nestedType, self) self.SubstituteTypeParametersForMemberType(nestedType), Me)
        End Function

        Friend NotOverridable Overrides Function GetFieldsToEmit() As IEnumerable(Of FieldSymbol)
            Throw ExceptionUtilities.Unreachable
        End Function

        ' Given a member from the original type of this type, substitute into it and get the corresponding member in this type.
        Friend Function GetMemberForDefinition(member As Symbol) As Symbol
            Debug.Assert(member.IsDefinition)
            Debug.Assert(TypeSymbol.Equals(member.ContainingType, Me.OriginalDefinition, TypeCompareKind.ConsiderEverything))

            Return SubstituteTypeParametersInMember(member)
        End Function

        ' Given a member from the full instance type, substitute into it and get the new member in this type.
        Private Function SubstituteTypeParametersInMember(member As Symbol) As Symbol
            Select Case member.Kind
                Case SymbolKind.NamedType
                    Return SubstituteTypeParametersForMemberType(DirectCast(member, NamedTypeSymbol))

                Case SymbolKind.Method
                    Dim memberMethod = DirectCast(member, MethodSymbol)

                    ' If the method is a property or event accessor, substitute the property or event
                    ' and return the specific accessor so that AssociatedPropertyOrEvent is set correctly
                    ' on the returned accessor. Note: since this function is used to substitute members
                    ' on demand, if this function is called for a property and it's accessor methods
                    ' individually, we'll end up creating 3 properties, 3 get accessors, and 3 set accessors.
                    Select Case memberMethod.MethodKind
                        Case MethodKind.PropertyGet, MethodKind.PropertySet
                            Debug.Assert(memberMethod.AssociatedSymbol IsNot Nothing)
                            Dim propertySymbol = SubstituteTypeParametersForMemberProperty(DirectCast(memberMethod.AssociatedSymbol, PropertySymbol))
                            Return If(memberMethod.MethodKind = MethodKind.PropertyGet, propertySymbol.GetMethod, propertySymbol.SetMethod)

                        Case MethodKind.EventAdd
                            Debug.Assert(memberMethod.AssociatedSymbol IsNot Nothing)
                            Dim eventSymbol = SubstituteTypeParametersForMemberEvent(DirectCast(memberMethod.AssociatedSymbol, EventSymbol))
                            Return eventSymbol.AddMethod

                        Case MethodKind.EventRemove
                            Debug.Assert(memberMethod.AssociatedSymbol IsNot Nothing)
                            Dim eventSymbol = SubstituteTypeParametersForMemberEvent(DirectCast(memberMethod.AssociatedSymbol, EventSymbol))
                            Return eventSymbol.RemoveMethod

                        Case MethodKind.EventRaise
                            Debug.Assert(memberMethod.AssociatedSymbol IsNot Nothing)
                            Dim eventSymbol = SubstituteTypeParametersForMemberEvent(DirectCast(memberMethod.AssociatedSymbol, EventSymbol))
                            Return eventSymbol.RaiseMethod

                        Case Else
                            Return SubstituteTypeParametersForMemberMethod(memberMethod)

                    End Select

                Case SymbolKind.Property
                    Return SubstituteTypeParametersForMemberProperty(DirectCast(member, PropertySymbol))

                Case SymbolKind.Event
                    Return SubstituteTypeParametersForMemberEvent(DirectCast(member, EventSymbol))

                Case SymbolKind.Field
                    Return SubstituteTypeParametersForMemberField(DirectCast(member, FieldSymbol))

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(member.Kind)
            End Select
        End Function

        Public Overrides Function GetHashCode() As Integer
            Dim _hash As Integer = OriginalDefinition.GetHashCode()
            If Me._substitution.WasConstructedForModifiers() Then
                Return _hash
            End If

            _hash = Hash.Combine(ContainingType, _hash)

            ' There is a circularity problem here with alpha-renamed type parameters.
            ' Calculating GetHashCode for them calls back into container's GetHashCode.
            ' Do not ask for hash code of type arguments here, derived classes 
            ' override this function and do that when appropriate. 
            Return _hash
        End Function

        Public MustOverride Overrides Function Equals(obj As Object) As Boolean

        ''' <summary>
        ''' Compare SubstitutedNamedTypes with no regard to type arguments.
        ''' </summary>
        Private Function EqualsWithNoRegardToTypeArguments(Of T As SubstitutedNamedType)(other As T) As Boolean

            If other Is Nothing Then
                Return False
            End If

            If Not OriginalDefinition.Equals(other.OriginalDefinition) Then
                Return False
            End If

            Dim containingType = Me.ContainingType

            If containingType IsNot Nothing AndAlso
                Not containingType.Equals(other.ContainingType) Then
                Return False
            End If

            ' There is a circularity problem here with alpha-renamed type parameters.
            ' Equals for them calls back into container's Equals.
            ' Do not compare type arguments here, derived classes 
            ' override Equals and do that when appropriate. 
            Return True
        End Function

        Friend Overrides Function GetDirectBaseTypeNoUseSiteDiagnostics(basesBeingResolved As ConsList(Of Symbol)) As NamedTypeSymbol
            Dim fullBase = OriginalDefinition.GetDirectBaseTypeNoUseSiteDiagnostics(basesBeingResolved)

            If fullBase IsNot Nothing Then
                Return DirectCast(fullBase.InternalSubstituteTypeParameters(_substitution).AsTypeSymbolOnly(), NamedTypeSymbol)
            End If

            Return Nothing
        End Function

        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            Return OriginalDefinition.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken)
        End Function

        Friend NotOverridable Overrides Iterator Function GetSynthesizedWithEventsOverrides() As IEnumerable(Of PropertySymbol)
            For Each definition In OriginalDefinition.GetSynthesizedWithEventsOverrides()
                Yield SubstituteTypeParametersForMemberProperty(definition)
            Next
        End Function

        ''' <summary>
        ''' Base class for symbols representing non-generic or open generic types contained within constructed generic type.
        ''' For example: A(Of Integer).B, A(Of Integer).B.C or A(Of Integer).B.C(Of ).
        ''' </summary>
        Friend MustInherit Class SpecializedType
            Inherits SubstitutedNamedType

            ''' <summary>
            '''  Symbol for the containing type, either specialized or constructed.
            ''' </summary>
            Protected ReadOnly _container As NamedTypeSymbol

            Protected Sub New(container As NamedTypeSymbol, substitution As TypeSubstitution)
                MyBase.New(substitution)

                Debug.Assert(container IsNot Nothing)
                Debug.Assert(TypeOf container Is SubstitutedNamedType)
                Debug.Assert(substitution.Parent Is container.TypeSubstitution)

                _container = container
            End Sub

            Public Overrides ReadOnly Property ConstructedFrom As NamedTypeSymbol
                Get
                    Return Me
                End Get
            End Property

            Public NotOverridable Overrides ReadOnly Property ContainingSymbol As Symbol
                Get
                    Return _container
                End Get
            End Property

            Public Shadows ReadOnly Property ContainingType As NamedTypeSymbol
                Get
                    Return _container
                End Get
            End Property

            Friend NotOverridable Overrides Function GetUnificationUseSiteDiagnosticRecursive(owner As Symbol, ByRef checkedTypes As HashSet(Of TypeSymbol)) As DiagnosticInfo
                Return Nothing
            End Function
        End Class

        ''' <summary>
        ''' Symbol representing open generic type directly or indirectly contained within constructed
        ''' generic type.
        ''' For example: A(Of Integer).B(Of ) or A(Of Integer).B.C(Of , )
        ''' </summary>
        Friend Class SpecializedGenericType
            Inherits SpecializedType

            ''' <summary>
            ''' Alpha-renamed type parameters, i.e. type parameters with constraints substituted according
            ''' to containing type's TypeSubstitution.
            ''' For example:
            '''     Class A (Of T)
            '''         Class B(Of S As T)
            '''             Dim x As A(Of Integer).B(Of S) 'error BC32044: Type argument 'S' does not inherit from or implement the constraint type 'Integer'.
            '''         End Class
            '''     End Class
            '''  
            ''' Given a type A(Of IComparable).B(Of ), alpha-renamed type parameter S will have type constraint IComparable.
            ''' </summary>
            Private ReadOnly _typeParameters As ImmutableArray(Of TypeParameterSymbol)

            Public Shared Function Create(
                container As NamedTypeSymbol,
                fullInstanceType As NamedTypeSymbol
            ) As SpecializedGenericType
                Debug.Assert(fullInstanceType.IsDefinition)
                Debug.Assert(fullInstanceType.Arity > 0)

                ' Create alpha-renamed type parameters.
                ' Note that these type parameters don't have their containing symbol set yet.
                ' It will be done later, in the constructor of this type.

                Dim typeParametersDefinitions As ImmutableArray(Of TypeParameterSymbol) = fullInstanceType.TypeParameters
                Dim alphaRenamedTypeParameters = New SubstitutedTypeParameterSymbol(typeParametersDefinitions.Length - 1) {}

                For i As Integer = 0 To typeParametersDefinitions.Length - 1 Step 1
                    alphaRenamedTypeParameters(i) = New SubstitutedTypeParameterSymbol(typeParametersDefinitions(i))
                Next

                Dim newTypeParameters = alphaRenamedTypeParameters.AsImmutableOrNull()

                ' Add a substitution to map from type parameter definitions to corresponding
                ' alpha-renamed type parameters.
                Debug.Assert(container.TypeSubstitution IsNot Nothing AndAlso
                             container.TypeSubstitution.TargetGenericDefinition Is fullInstanceType.ContainingSymbol)
                Dim substitution = VisualBasic.Symbols.TypeSubstitution.CreateForAlphaRename(container.TypeSubstitution,
                                                                         StaticCast(Of TypeParameterSymbol).From(newTypeParameters))
                Debug.Assert(substitution.TargetGenericDefinition Is fullInstanceType)

                ' Now create the symbol.
                Return New SpecializedGenericType(container, substitution, newTypeParameters)
            End Function

            Private Sub New(
                container As NamedTypeSymbol,
                substitution As TypeSubstitution,
                typeParameters As ImmutableArray(Of SubstitutedTypeParameterSymbol)
            )
                MyBase.New(container, substitution)
                Debug.Assert(Not typeParameters.IsDefault AndAlso typeParameters.Length = DirectCast(substitution.TargetGenericDefinition, NamedTypeSymbol).Arity)

                _typeParameters = StaticCast(Of TypeParameterSymbol).From(typeParameters)

                ' Set container for type parameters
                For Each param In typeParameters
                    param.SetContainingSymbol(Me)
                Next
            End Sub

            Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
                Get
                    Return _typeParameters
                End Get
            End Property

            Friend Overrides ReadOnly Property TypeArgumentsNoUseSiteDiagnostics As ImmutableArray(Of TypeSymbol)
                Get
                    Return StaticCast(Of TypeSymbol).From(TypeParameters)
                End Get
            End Property

            Public NotOverridable Overrides Function GetTypeArgumentCustomModifiers(ordinal As Integer) As ImmutableArray(Of CustomModifier)
                Return GetEmptyTypeArgumentCustomModifiers(ordinal)
            End Function

            Friend NotOverridable Overrides ReadOnly Property HasTypeArgumentsCustomModifiers As Boolean
                Get
                    Return False
                End Get
            End Property

            Friend Overrides ReadOnly Property CanConstruct As Boolean
                Get
                    ' Cannot construct this type if any container of this type is another SpecializedGenericType.
                    Dim containerToCheck As NamedTypeSymbol = _container

                    Do
                        Debug.Assert(Not containerToCheck.IsDefinition)

                        If containerToCheck.Arity > 0 Then
                            If containerToCheck.ConstructedFrom Is containerToCheck Then
                                ' Run into a SpecializedGenericType
                                Debug.Assert(TypeOf containerToCheck Is SpecializedGenericType)
                                Return False
                            Else
                                ' Run into a Constructed type
                                Debug.Assert(TypeOf containerToCheck Is ConstructedType)
                                Return True
                            End If
                        End If

                        containerToCheck = containerToCheck.ContainingType
                    Loop While containerToCheck IsNot Nothing AndAlso Not containerToCheck.IsDefinition

                    Return True
                End Get
            End Property

            Public Overrides Function Construct(typeArguments As ImmutableArray(Of TypeSymbol)) As NamedTypeSymbol
                CheckCanConstructAndTypeArguments(typeArguments)

                typeArguments = typeArguments.TransformToCanonicalFormFor(Me)

                If typeArguments.IsDefault Then
                    ' identity substitution
                    Return Me
                End If

                Debug.Assert(_substitution.Parent IsNot Nothing)

                Dim substitution = TypeSubstitution.Create(_substitution.Parent, Me.OriginalDefinition, typeArguments,
                                                           allowAlphaRenamedTypeParametersAsArguments:=True)
                Return New ConstructedSpecializedGenericType(Me, substitution)
            End Function

            ''' <summary>
            ''' Substitute the given type substitution within this type, returning a new type. If the
            ''' substitution had no effect, return Me. 
            ''' !!! Only code implementing construction of generic types is allowed to call this method !!!
            ''' !!! All other code should use Construct methods.                                        !!! 
            ''' </summary>
            Friend Overrides Function InternalSubstituteTypeParameters(additionalSubstitution As TypeSubstitution) As TypeWithModifiers

                ' I do not believe it is ever valid to do this operation on an open generic type.
                ' However, just in case later we discover that it is valid, I'll leave commented out 
                ' implementation below.
                Throw ExceptionUtilities.Unreachable

                ' TODO: Remove this code once we are confident that it is really unreachable.
                'If additionalSubstitution Is Nothing Then
                '    Return Me
                'End If

                'Dim newContainer = _container.InternalSubstituteTypeParameters(additionalSubstitution)

                'Dim additionalSubstitutionForMe = additionalSubstitution.GetSubstitutionForGenericDefinition(_fullInstanceType)

                'Dim constructFrom As SpecializedGenericType = Me

                'If newContainer IsNot _container Then
                '    If newContainer.IsDefinition Then
                '        Debug.Assert(newContainer Is _fullInstanceType.ContainingSymbol)

                '        If additionalSubstitutionForMe Is Nothing OrElse additionalSubstitutionForMe.Pairs.Count = 0 Then
                '            Return _fullInstanceType
                '        End If

                '        ' My type parameters are substituted
                '        Dim substitution As TypeSubstitution = additionalSubstitutionForMe.AdjustParent(Nothing)

                '        Return New ConstructedInstanceType(_fullInstanceType, substitution)
                '    End If

                '    ' The constructed from is changed.
                '    constructFrom = Create(DirectCast(newContainer, NamedTypeSymbol), _fullInstanceType)
                'End If

                'If additionalSubstitutionForMe Is Nothing Then
                '    ' Substitution for my type parameters hasn't changed.
                '    ' Was identity and stays identity.
                '    Return constructFrom
                'Else
                '    ' My type parameters are substituted
                '    Dim substitution As TypeSubstitution = additionalSubstitutionForMe.AdjustParent(newContainer.TypeSubstitution)
                '    Return New ConstructedSpecializedGenericType(constructFrom, substitution)
                'End If
            End Function

            Public Overrides Function Equals(obj As Object) As Boolean
                If Me Is obj Then
                    Return True
                End If

                Return EqualsWithNoRegardToTypeArguments(TryCast(obj, SpecializedGenericType))
            End Function

        End Class

        ''' <summary>
        ''' Symbol representing non-generic type directly or indirectly contained within constructed
        ''' generic type.
        ''' For example: A(Of Integer).B or A(Of Integer).B.C
        ''' </summary>
        Friend Class SpecializedNonGenericType
            Inherits SpecializedType

            Public Shared Function Create(
                container As NamedTypeSymbol,
                fullInstanceType As NamedTypeSymbol,
                substitution As TypeSubstitution
            ) As SpecializedType
                Debug.Assert(fullInstanceType.IsDefinition)
                Debug.Assert(fullInstanceType.Arity = 0)

                ' Parent's substitution might not match similar part of passed in substitution, 
                ' due to an alpha-rename in parent, etc. We need to use that part, because parent's
                ' alpha-rename should be taken into consideration within this type.
                Dim parentsTypeSubstitution = container.TypeSubstitution

                Debug.Assert(parentsTypeSubstitution IsNot Nothing)
                Debug.Assert(parentsTypeSubstitution.TargetGenericDefinition Is fullInstanceType.ContainingSymbol)

                If substitution.TargetGenericDefinition IsNot fullInstanceType Then
                    ' We can ignore passed in substitution completely.
                    substitution = VisualBasic.Symbols.TypeSubstitution.Concat(fullInstanceType, parentsTypeSubstitution, Nothing)
                    Debug.Assert(substitution.TargetGenericDefinition Is fullInstanceType)
                Else
                    Debug.Assert(substitution.Pairs.Length = 0)

                    If substitution.Parent IsNot parentsTypeSubstitution Then
                        substitution = VisualBasic.Symbols.TypeSubstitution.Concat(fullInstanceType, parentsTypeSubstitution, Nothing)
                    End If
                End If

                Return New SpecializedNonGenericType(DirectCast(container, NamedTypeSymbol), substitution)
            End Function

            Private Sub New(container As NamedTypeSymbol, substitution As TypeSubstitution)
                MyBase.New(container, substitution)
                Debug.Assert(substitution.Pairs.Length = 0)
            End Sub

            Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
                Get
                    Return ImmutableArray(Of TypeParameterSymbol).Empty
                End Get
            End Property

            Friend Overrides ReadOnly Property TypeArgumentsNoUseSiteDiagnostics As ImmutableArray(Of TypeSymbol)
                Get
                    Return ImmutableArray(Of TypeSymbol).Empty
                End Get
            End Property

            Public NotOverridable Overrides Function GetTypeArgumentCustomModifiers(ordinal As Integer) As ImmutableArray(Of CustomModifier)
                Return GetEmptyTypeArgumentCustomModifiers(ordinal)
            End Function

            Friend NotOverridable Overrides ReadOnly Property HasTypeArgumentsCustomModifiers As Boolean
                Get
                    Return False
                End Get
            End Property

            Friend Overrides ReadOnly Property CanConstruct As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides Function Construct(typeArguments As ImmutableArray(Of TypeSymbol)) As NamedTypeSymbol
                Throw New InvalidOperationException()
            End Function

            ''' <summary>
            ''' Substitute the given type substitution within this type, returning a new type. If the
            ''' substitution had no effect, return Me. 
            ''' !!! Only code implementing construction of generic types is allowed to call this method !!!
            ''' !!! All other code should use Construct methods.                                        !!! 
            ''' </summary>
            Friend Overrides Function InternalSubstituteTypeParameters(additionalSubstitution As TypeSubstitution) As TypeWithModifiers
                Return New TypeWithModifiers(InternalSubstituteTypeParametersInSpecializedNonGenericType(additionalSubstitution))
            End Function

            Private Overloads Function InternalSubstituteTypeParametersInSpecializedNonGenericType(additionalSubstitution As TypeSubstitution) As NamedTypeSymbol
                If additionalSubstitution Is Nothing Then
                    Return Me
                End If

                Dim newContainer = DirectCast(_container.InternalSubstituteTypeParameters(additionalSubstitution).AsTypeSymbolOnly(), NamedTypeSymbol)

                If newContainer IsNot _container Then
                    ' The container is affected.

                    Dim definition = Me.OriginalDefinition

                    If newContainer.IsDefinition Then
                        ' New substitution cancelled out original substitution.
                        Debug.Assert(newContainer.TypeSubstitution Is Nothing AndAlso definition.ContainingSymbol Is newContainer)
                        Return definition
                    End If

                    Return Create(DirectCast(newContainer, NamedTypeSymbol), definition, newContainer.TypeSubstitution)
                End If

                ' No effect.
                Return Me
            End Function

            Public Overrides Function Equals(obj As Object) As Boolean
                If Me Is obj Then
                    Return True
                End If

                Return EqualsWithNoRegardToTypeArguments(TryCast(obj, SpecializedNonGenericType))
            End Function

        End Class

        ''' <summary>
        ''' Base class for symbols representing constructed generic types.
        ''' For example: A(Of Integer), A.B(Of Integer), A(Of Integer).B.C(Of Integer).
        ''' </summary>
        Friend MustInherit Class ConstructedType
            Inherits SubstitutedNamedType

            Private ReadOnly _typeArguments As ImmutableArray(Of TypeSymbol)
            Private ReadOnly _hasTypeArgumentsCustomModifiers As Boolean

            Protected Sub New(substitution As TypeSubstitution)
                MyBase.New(substitution)
                _typeArguments = substitution.GetTypeArgumentsFor(OriginalDefinition, _hasTypeArgumentsCustomModifiers)
            End Sub

            Public NotOverridable Overrides ReadOnly Property ContainingSymbol As Symbol
                Get
                    Return ConstructedFrom.ContainingSymbol
                End Get
            End Property

            Public Overrides ReadOnly Property IsAnonymousType As Boolean
                Get
                    Return ConstructedFrom.IsAnonymousType
                End Get
            End Property

            Public NotOverridable Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
                Get
                    Return ConstructedFrom.TypeParameters
                End Get
            End Property

            Friend NotOverridable Overrides ReadOnly Property TypeArgumentsNoUseSiteDiagnostics As ImmutableArray(Of TypeSymbol)
                Get
                    Return _typeArguments
                End Get
            End Property

            Public NotOverridable Overrides Function GetTypeArgumentCustomModifiers(ordinal As Integer) As ImmutableArray(Of CustomModifier)
                If _hasTypeArgumentsCustomModifiers Then
                    Return _substitution.GetTypeArgumentsCustomModifiersFor(OriginalDefinition.TypeParameters(ordinal))
                End If

                Return GetEmptyTypeArgumentCustomModifiers(ordinal)
            End Function

            Friend NotOverridable Overrides ReadOnly Property HasTypeArgumentsCustomModifiers As Boolean
                Get
                    Return _hasTypeArgumentsCustomModifiers
                End Get
            End Property

            Friend Overrides ReadOnly Property CanConstruct As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides Function Construct(typeArguments As ImmutableArray(Of TypeSymbol)) As NamedTypeSymbol
                Throw New InvalidOperationException()
            End Function

            Public Overrides Function GetHashCode() As Integer
                If Me._substitution.WasConstructedForModifiers() Then
                    Return OriginalDefinition.GetHashCode()
                End If

                Dim _hash As Integer = MyBase.GetHashCode()

                For Each typeArgument In TypeArgumentsNoUseSiteDiagnostics
                    _hash = Hash.Combine(typeArgument, _hash)
                Next

                Return _hash
            End Function

            Public Overrides Function Equals(obj As Object) As Boolean
                If Me Is obj Then
                    Return True
                End If

                Dim other = TryCast(obj, ConstructedType)

                If Not EqualsWithNoRegardToTypeArguments(other) Then
                    Return False
                End If

                If _hasTypeArgumentsCustomModifiers <> other._hasTypeArgumentsCustomModifiers Then
                    Return False
                End If

                Dim arguments = TypeArgumentsNoUseSiteDiagnostics
                Dim otherArguments = other.TypeArgumentsNoUseSiteDiagnostics
                Dim count As Integer = arguments.Length

                For i As Integer = 0 To count - 1 Step 1
                    If Not arguments(i).Equals(otherArguments(i)) Then
                        Return False
                    End If
                Next

                If _hasTypeArgumentsCustomModifiers Then
                    For i As Integer = 0 To count - 1 Step 1
                        If Not GetTypeArgumentCustomModifiers(i).SequenceEqual(other.GetTypeArgumentCustomModifiers(i)) Then
                            Return False
                        End If
                    Next
                End If

                Return True
            End Function

            Friend NotOverridable Overrides Function GetUnificationUseSiteDiagnosticRecursive(owner As Symbol, ByRef checkedTypes As HashSet(Of TypeSymbol)) As DiagnosticInfo
                Dim result As DiagnosticInfo = If(ConstructedFrom.GetUnificationUseSiteDiagnosticRecursive(owner, checkedTypes),
                                                  GetUnificationUseSiteDiagnosticRecursive(_typeArguments, owner, checkedTypes))

                If result Is Nothing AndAlso _hasTypeArgumentsCustomModifiers Then
                    For i As Integer = 0 To Me.Arity - 1
                        result = GetUnificationUseSiteDiagnosticRecursive(Me.GetTypeArgumentCustomModifiers(i), owner, checkedTypes)

                        If result IsNot Nothing Then
                            Exit For
                        End If
                    Next
                End If

                Return result
            End Function

        End Class

        ''' <summary>
        ''' Symbols representing constructed generic type that isn't contained within another constructed generic type.
        ''' For example: A(Of Integer), A.B(Of Integer), but not A(Of Integer).B.C(Of Integer).
        ''' </summary>
        Friend Class ConstructedInstanceType
            Inherits ConstructedType

            Public Sub New(substitution As TypeSubstitution)
                MyBase.New(substitution)
                Debug.Assert(substitution.Parent Is Nothing)
            End Sub

            Public Overrides ReadOnly Property ConstructedFrom As NamedTypeSymbol
                Get
                    Return Me.OriginalDefinition
                End Get
            End Property

            ''' <summary>
            ''' Substitute the given type substitution within this type, returning a new type. If the
            ''' substitution had no effect, return Me. 
            ''' !!! Only code implementing construction of generic types is allowed to call this method !!!
            ''' !!! All other code should use Construct methods.                                        !!! 
            ''' </summary>
            Friend Overrides Function InternalSubstituteTypeParameters(additionalSubstitution As TypeSubstitution) As TypeWithModifiers
                Return New TypeWithModifiers(InternalSubstituteTypeParametersInConstructedInstanceType(additionalSubstitution))
            End Function

            Private Overloads Function InternalSubstituteTypeParametersInConstructedInstanceType(additionalSubstitution As TypeSubstitution) As NamedTypeSymbol
                If additionalSubstitution Is Nothing Then
                    Return Me
                End If

                Dim definition As NamedTypeSymbol = Me.OriginalDefinition
                Dim containedType As NamedTypeSymbol = definition.ContainingType
                Dim newContainedType As NamedTypeSymbol

                If containedType IsNot Nothing Then
                    newContainedType = DirectCast(containedType.InternalSubstituteTypeParameters(additionalSubstitution).AsTypeSymbolOnly, NamedTypeSymbol)
                Else
                    newContainedType = Nothing
                End If

                Dim substitution As TypeSubstitution

                If newContainedType IsNot containedType Then
                    ' Old container was a definition then new container must be constructed or specialized
                    Debug.Assert(containedType.IsDefinition AndAlso Not newContainedType.IsDefinition)
                    Dim constructFrom As SpecializedGenericType = SpecializedGenericType.Create(newContainedType, definition)

                    Debug.Assert(newContainedType.TypeSubstitution IsNot Nothing)
                    substitution = VisualBasic.Symbols.TypeSubstitution.AdjustForConstruct(newContainedType.TypeSubstitution, _substitution, additionalSubstitution)

                    Debug.Assert(substitution IsNot Nothing)
                    Return New ConstructedSpecializedGenericType(constructFrom, substitution)
                End If

                Debug.Assert(newContainedType Is Nothing OrElse newContainedType.TypeSubstitution Is Nothing)
                substitution = VisualBasic.Symbols.TypeSubstitution.AdjustForConstruct(Nothing, _substitution, additionalSubstitution)

                If substitution Is Nothing Then
                    ' Old substitution is cancelled out.
                    Return Me.OriginalDefinition
                End If

                If substitution IsNot _substitution Then
                    Debug.Assert(substitution.TargetGenericDefinition Is _substitution.TargetGenericDefinition)
                    Return New ConstructedInstanceType(substitution)
                End If

                ' No effect.
                Return Me
            End Function

        End Class

        ''' <summary>
        ''' Symbols representing constructed generic type that is contained within another constructed generic type.
        ''' For example: A(Of Integer).B(Of Integer), A(Of Integer).B.C(Of Integer).
        ''' </summary>
        Friend Class ConstructedSpecializedGenericType
            Inherits ConstructedType

            ''' <summary>
            ''' Symbol for the ConstructedFrom type.
            '''      A(Of Integer).B(Of ) for A(Of Integer).B(Of Integer),
            '''      A(Of Integer).B.C(Of ) for A(Of Integer).B.C(Of Integer)
            ''' 
            ''' All types in its containership hierarchy must be either constructed or non-generic, or original definitions.
            ''' </summary>
            Private ReadOnly _constructedFrom As SpecializedGenericType

            Public Sub New(constructedFrom As SpecializedGenericType, substitution As TypeSubstitution)
                MyBase.New(substitution)
                Debug.Assert(constructedFrom IsNot Nothing)
                Debug.Assert(substitution.TargetGenericDefinition Is constructedFrom.OriginalDefinition)
                Debug.Assert(substitution.Parent Is constructedFrom.TypeSubstitution.Parent)
                Debug.Assert(constructedFrom.CanConstruct)

                _constructedFrom = constructedFrom
            End Sub

            Public Overrides ReadOnly Property ConstructedFrom As NamedTypeSymbol
                Get
                    Return _constructedFrom
                End Get
            End Property

            ''' <summary>
            ''' Substitute the given type substitution within this type, returning a new type. If the
            ''' substitution had no effect, return Me. 
            ''' !!! Only code implementing construction of generic types is allowed to call this method !!!
            ''' !!! All other code should use Construct methods.                                        !!! 
            ''' </summary>
            Friend Overrides Function InternalSubstituteTypeParameters(additionalSubstitution As TypeSubstitution) As TypeWithModifiers
                Return New TypeWithModifiers(InternalSubstituteTypeParametersInConstructedSpecializedGenericType(additionalSubstitution))
            End Function

            Private Overloads Function InternalSubstituteTypeParametersInConstructedSpecializedGenericType(additionalSubstitution As TypeSubstitution) As NamedTypeSymbol
                If additionalSubstitution Is Nothing Then
                    Return Me
                End If

                Dim fullInstanceType As NamedTypeSymbol = _constructedFrom.OriginalDefinition

                Dim container As NamedTypeSymbol = _constructedFrom.ContainingType
                Debug.Assert(Not container.IsDefinition)

                Dim newContainer = DirectCast(container.InternalSubstituteTypeParameters(additionalSubstitution).AsTypeSymbolOnly, NamedTypeSymbol)
                Dim newSubstitution As TypeSubstitution = VisualBasic.Symbols.TypeSubstitution.AdjustForConstruct(newContainer.TypeSubstitution, _substitution, additionalSubstitution)

                If newSubstitution Is Nothing Then
                    ' Substitutions cancelled each other out.
                    Debug.Assert(newContainer.IsDefinition AndAlso newContainer.TypeSubstitution Is Nothing)
                    Return fullInstanceType
                End If

                If newContainer.IsDefinition Then
                    ' Only container's substitution got cancelled out.
                    Debug.Assert(newSubstitution.Parent Is Nothing AndAlso fullInstanceType.ContainingSymbol Is newContainer AndAlso
                                 newSubstitution.TargetGenericDefinition Is fullInstanceType)
                    Return New ConstructedInstanceType(newSubstitution)
                End If

                Dim constructFrom As SpecializedGenericType = _constructedFrom

                If newContainer IsNot container Then
                    ' The constructed from is changed.
                    constructFrom = SpecializedGenericType.Create(DirectCast(newContainer, NamedTypeSymbol), fullInstanceType)
                End If

                If constructFrom IsNot _constructedFrom OrElse newSubstitution IsNot _substitution Then
                    Return New ConstructedSpecializedGenericType(constructFrom, newSubstitution)
                End If

                ' No effect
                Return Me
            End Function

        End Class

        ''' <summary>
        ''' Force all declaration errors to be generated.
        ''' </summary>
        Friend Overrides Sub GenerateDeclarationErrors(cancellationToken As CancellationToken)
            Throw ExceptionUtilities.Unreachable
        End Sub

    End Class

End Namespace
