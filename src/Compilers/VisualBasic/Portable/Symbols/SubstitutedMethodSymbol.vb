' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Represents a method that has undergone type substitution. This is use for a method
    ''' inside a generic type that has undergone type substitution. It also serves as a base class
    ''' for ConstructedMethodSymbol, which is used for a method after type substitution in the method type parameters.  
    ''' </summary>
    Friend MustInherit Class SubstitutedMethodSymbol
        Inherits MethodSymbol

        Private _propertyOrEventSymbolOpt As Symbol

        Protected Sub New()
        End Sub

        ' Create substituted version of all the parameters
        Protected Overridable Function SubstituteParameters() As ImmutableArray(Of ParameterSymbol)

            Dim unsubstituted = OriginalDefinition.Parameters
            Dim count = unsubstituted.Length

            If count = 0 Then
                Return ImmutableArray(Of ParameterSymbol).Empty
            Else
                Dim substituted As ParameterSymbol() = New ParameterSymbol(count - 1) {}

                For i = 0 To count - 1
                    substituted(i) = SubstitutedParameterSymbol.CreateMethodParameter(Me, unsubstituted(i))
                Next

                Return substituted.AsImmutableOrNull()
            End If
        End Function

        Public MustOverride Overrides ReadOnly Property OriginalDefinition As MethodSymbol

        Public NotOverridable Overrides ReadOnly Property Name As String
            Get
                Return OriginalDefinition.Name
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property MetadataName As String
            Get
                Return OriginalDefinition.MetadataName
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return OriginalDefinition.IsImplicitlyDeclared
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return OriginalDefinition.HasSpecialName
            End Get
        End Property

        Public NotOverridable Overrides Function GetDllImportData() As DllImportData
            Return OriginalDefinition.GetDllImportData()
        End Function

        Friend NotOverridable Overrides ReadOnly Property ReturnTypeMarshallingInformation As MarshalPseudoCustomAttributeData
            Get
                Return OriginalDefinition.ReturnTypeMarshallingInformation
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property ImplementationAttributes As Reflection.MethodImplAttributes
            Get
                Return OriginalDefinition.ImplementationAttributes
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property HasDeclarativeSecurity As Boolean
            Get
                Return OriginalDefinition.HasDeclarativeSecurity
            End Get
        End Property

        Friend Overrides Function GetSecurityInformation() As IEnumerable(Of Microsoft.Cci.SecurityAttribute)
            Return OriginalDefinition.GetSecurityInformation()
        End Function

        Friend Overrides Function GetAppliedConditionalSymbols() As ImmutableArray(Of String)
            Return OriginalDefinition.GetAppliedConditionalSymbols()
        End Function

        Public Overrides ReadOnly Property AssociatedSymbol As Symbol
            Get
                Return _propertyOrEventSymbolOpt
            End Get
        End Property

        Public Overrides ReadOnly Property ReducedFrom As MethodSymbol
            Get
                Return OriginalDefinition.ReducedFrom
            End Get
        End Property

        Public Overrides ReadOnly Property ReceiverType As TypeSymbol
            Get
                If OriginalDefinition.IsReducedExtensionMethod Then
                    Return OriginalDefinition.ReceiverType
                End If

                Return Me.ContainingType
            End Get
        End Property

        Public Overrides Function GetTypeInferredDuringReduction(reducedFromTypeParameter As TypeParameterSymbol) As TypeSymbol
            Return OriginalDefinition.GetTypeInferredDuringReduction(reducedFromTypeParameter)
        End Function

        Friend Overrides Function CallsAreOmitted(atNode As SyntaxNodeOrToken, syntaxTree As SyntaxTree) As Boolean
            Return OriginalDefinition.CallsAreOmitted(atNode, syntaxTree)
        End Function

        Friend Overrides ReadOnly Property FixedTypeParameters As ImmutableArray(Of KeyValuePair(Of TypeParameterSymbol, TypeSymbol))
            Get
                Return OriginalDefinition.FixedTypeParameters
            End Get
        End Property

        Friend Overrides ReadOnly Property Proximity As Integer
            Get
                Return OriginalDefinition.Proximity
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Me.OriginalDefinition.ObsoleteAttributeData
            End Get
        End Property

        Public MustOverride Overrides ReadOnly Property ConstructedFrom As MethodSymbol

        Public MustOverride Overrides ReadOnly Property ContainingSymbol As Symbol

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return OriginalDefinition.DeclaredAccessibility
            End Get
        End Property

        Public Overloads Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            ' Attributes do not undergo substitution
            Return OriginalDefinition.GetAttributes()
        End Function

        Public Overrides ReadOnly Property IsExtensionMethod As Boolean
            Get
                Return OriginalDefinition.IsExtensionMethod
            End Get
        End Property

        Friend Overrides ReadOnly Property MayBeReducibleExtensionMethod As Boolean
            Get
                Return OriginalDefinition.MayBeReducibleExtensionMethod
            End Get
        End Property

        Public Overrides ReadOnly Property IsExternalMethod As Boolean
            Get
                Return OriginalDefinition.IsExternalMethod
            End Get
        End Property

        Public Overrides ReadOnly Property IsGenericMethod As Boolean
            Get
                Return OriginalDefinition.IsGenericMethod
            End Get
        End Property

        ''' <summary>
        ''' If this is a generic method return TypeSubstitution for it. 
        ''' TypeSubstitution for containing type otherwise.
        ''' </summary>
        Public MustOverride ReadOnly Property TypeSubstitution As TypeSubstitution

        Public Overrides ReadOnly Property Arity As Integer
            Get
                Return OriginalDefinition.Arity
            End Get
        End Property

        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return OriginalDefinition.IsMustOverride
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return OriginalDefinition.IsNotOverridable
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverloads As Boolean
            Get
                Return OriginalDefinition.IsOverloads
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Return OriginalDefinition.IsOverridable
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return OriginalDefinition.IsOverrides
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return OriginalDefinition.IsShared
            End Get
        End Property

        Public Overrides ReadOnly Property IsSub As Boolean
            Get
                Return OriginalDefinition.IsSub
            End Get
        End Property

        Public Overrides ReadOnly Property IsAsync As Boolean
            Get
                Return OriginalDefinition.IsAsync
            End Get
        End Property

        Public Overrides ReadOnly Property IsIterator As Boolean
            Get
                Return OriginalDefinition.IsIterator
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsInitOnly As Boolean
            Get
                Return OriginalDefinition.IsInitOnly
            End Get
        End Property

        Public Overrides ReadOnly Property IsVararg As Boolean
            Get
                Return OriginalDefinition.IsVararg
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

        Public Overrides ReadOnly Property MethodKind As MethodKind
            Get
                Return OriginalDefinition.MethodKind
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property IsMethodKindBasedOnSyntax As Boolean
            Get
                Return OriginalDefinition.IsMethodKindBasedOnSyntax
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property ParameterCount As Integer
            Get
                Return OriginalDefinition.ParameterCount
            End Get
        End Property

        Public MustOverride Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)

        Public NotOverridable Overrides ReadOnly Property ReturnsByRef As Boolean
            Get
                Return OriginalDefinition.ReturnsByRef
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                Return OriginalDefinition.ReturnType.InternalSubstituteTypeParameters(Me.TypeSubstitution).Type
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnTypeCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return Me.TypeSubstitution.SubstituteCustomModifiers(OriginalDefinition.ReturnType, OriginalDefinition.ReturnTypeCustomModifiers)
            End Get
        End Property

        Public Overrides ReadOnly Property RefCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return Me.TypeSubstitution.SubstituteCustomModifiers(OriginalDefinition.RefCustomModifiers)
            End Get
        End Property

        Public MustOverride Overrides ReadOnly Property TypeArguments As ImmutableArray(Of TypeSymbol)

        Public MustOverride Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of MethodSymbol)
            Get
                Return ImplementsHelper.SubstituteExplicitInterfaceImplementations(OriginalDefinition.ExplicitInterfaceImplementations,
                                                                                   Me.TypeSubstitution)
            End Get
        End Property

        Friend Overrides ReadOnly Property CallingConvention As Microsoft.Cci.CallingConvention
            Get
                Return OriginalDefinition.CallingConvention
            End Get
        End Property

        Friend NotOverridable Overrides Function IsMetadataNewSlot(Optional ignoreInterfaceImplementationChanges As Boolean = False) As Boolean
            Return OriginalDefinition.IsMetadataNewSlot(ignoreInterfaceImplementationChanges)
        End Function

        Friend NotOverridable Overrides Function TryGetMeParameter(<Out> ByRef meParameter As ParameterSymbol) As Boolean
            ' Required in EE scenarios.  Specifically, the EE binds in the context of a 
            ' substituted method, whereas the core compiler always binds within the
            ' context of an original definition.  
            ' There should never be any reason to call this in normal compilation
            ' scenarios, but the behavior should be sensible if it does occur.
            Dim originalMeParameter As ParameterSymbol = Nothing
            If Not OriginalDefinition.TryGetMeParameter(originalMeParameter) Then
                meParameter = Nothing
                Return False
            End If
            meParameter = If(originalMeParameter IsNot Nothing,
                New MeParameterSymbol(Me),
                Nothing)
            Return True
        End Function

        Public Overrides Function GetHashCode() As Integer
            Dim _hash As Integer = OriginalDefinition.GetHashCode()
            _hash = Hash.Combine(ContainingType, _hash)

            ' There is a circularity problem here with alpha-renamed type parameters.
            ' Calculating GetHashCode for them calls back into container's GetHashCode.
            ' Do not ask for hash code of type arguments here, derived classes 
            ' override this function and do that when appropriate. 
            Return _hash
        End Function

        Public MustOverride Overrides Function Equals(obj As Object) As Boolean

        ''' <summary>
        ''' Compare with no regard to type arguments.
        ''' </summary>
        Private Function EqualsWithNoRegardToTypeArguments(Of T As SubstitutedMethodSymbol)(other As T) As Boolean
            If other Is Nothing Then
                Return False
            End If

            If Not OriginalDefinition.Equals(other.OriginalDefinition) Then
                Return False
            End If

            Dim containingType = Me.ContainingType

            If Not Me.ContainingType.Equals(other.ContainingType) Then
                Return False
            End If

            ' There is a circularity problem here with alpha-renamed type parameters.
            ' Equals for them calls back into container's Equals.
            ' Do not compare type arguments here, derived classes 
            ' override Equals and do that when appropriate. 
            Return True
        End Function

        Friend MustOverride Overrides ReadOnly Property CanConstruct As Boolean

        Public MustOverride Overrides Function Construct(typeArguments As ImmutableArray(Of TypeSymbol)) As MethodSymbol

        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            Return OriginalDefinition.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken)
        End Function

        Friend NotOverridable Overrides ReadOnly Property HasSetsRequiredMembers As Boolean
            Get
                Return OriginalDefinition.HasSetsRequiredMembers
            End Get
        End Property

        ''' <summary>
        ''' Base class for symbols representing non-generic or open generic methods contained within constructed generic type.
        ''' For example: A(Of Integer).B, A(Of Integer).B.C or A(Of Integer).B.C(Of ).
        ''' </summary>
        Public MustInherit Class SpecializedMethod
            Inherits SubstitutedMethodSymbol

            Protected ReadOnly _container As SubstitutedNamedType

            Protected Sub New(container As SubstitutedNamedType)
                _container = container
            End Sub

            Public MustOverride Overrides ReadOnly Property OriginalDefinition As MethodSymbol

            Public Overrides ReadOnly Property ConstructedFrom As MethodSymbol
                Get
                    Return Me
                End Get
            End Property

            Public Overrides ReadOnly Property ContainingSymbol As Symbol
                Get
                    Return _container
                End Get
            End Property
        End Class

        ''' <summary>
        ''' Associate the method with a particular property. Returns
        ''' false if the method is already associated with a property.
        ''' </summary>
        Friend Function SetAssociatedPropertyOrEvent(propertyOrEventSymbol As Symbol) As Boolean
            If _propertyOrEventSymbolOpt Is Nothing Then
                Debug.Assert(TypeSymbol.Equals(propertyOrEventSymbol.ContainingType, Me.ContainingType, TypeCompareKind.ConsiderEverything))

                ' No locking required since SetAssociatedProperty will only be called by the
                ' thread that created the method symbol (and will be called before the method
                ' symbol is added to the containing type members and available to other threads).
                _propertyOrEventSymbolOpt = propertyOrEventSymbol
                Return True
            End If

            Return False
        End Function

        Friend Overrides ReadOnly Property Syntax As SyntaxNode
            Get
                Return Nothing
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
            Get
                Return OriginalDefinition.GenerateDebugInfo
            End Get
        End Property

        Friend NotOverridable Overrides Function CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer
            Throw ExceptionUtilities.Unreachable
        End Function

        ''' <summary>
        ''' Symbol representing non-generic method directly or indirectly contained within constructed
        ''' generic type.
        ''' For example: A(Of Integer).B or A(Of Integer).B.C
        ''' </summary>
        Public Class SpecializedNonGenericMethod
            Inherits SpecializedMethod

            Private ReadOnly _originalDefinition As MethodSymbol
            Private ReadOnly _parameters As ImmutableArray(Of ParameterSymbol)

            Public Sub New(container As SubstitutedNamedType, originalDefinition As MethodSymbol)
                MyBase.New(container)
                Debug.Assert(originalDefinition.IsDefinition)
                _originalDefinition = originalDefinition
                _parameters = SubstituteParameters()
            End Sub

            Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
                Get
                    Return _parameters
                End Get
            End Property

            Public Overrides ReadOnly Property TypeSubstitution As TypeSubstitution
                Get
                    Return _container.TypeSubstitution
                End Get
            End Property

            Public Overrides ReadOnly Property OriginalDefinition As MethodSymbol
                Get
                    Return _originalDefinition
                End Get
            End Property

            Friend Overrides ReadOnly Property CanConstruct As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides Function Construct(typeArguments As ImmutableArray(Of TypeSymbol)) As MethodSymbol
                Throw New InvalidOperationException()
            End Function

            Public Overrides ReadOnly Property TypeArguments As ImmutableArray(Of TypeSymbol)
                Get
                    Return ImmutableArray(Of TypeSymbol).Empty
                End Get
            End Property

            Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
                Get
                    Return ImmutableArray(Of TypeParameterSymbol).Empty
                End Get
            End Property

            Public Overrides Function Equals(obj As Object) As Boolean
                Return obj Is Me OrElse EqualsWithNoRegardToTypeArguments(TryCast(obj, SpecializedNonGenericMethod))
            End Function

        End Class

        ''' <summary>
        ''' Symbol representing open generic method directly or indirectly contained within constructed
        ''' generic type.
        ''' For example: A(Of Integer).B(Of ) or A(Of Integer).B.C(Of , )
        ''' </summary>
        Public NotInheritable Class SpecializedGenericMethod
            Inherits SpecializedMethod

            Private ReadOnly _substitution As TypeSubstitution

            ''' <summary>
            ''' Alpha-renamed type parameters, i.e. type parameters with constraints substituted according
            ''' to containing type's TypeSubstitution.
            ''' For example:
            '''     Class A (Of T)
            '''         Sub B(Of S As T)()
            '''         End Sub
            '''     End Class
            '''  
            ''' Given a method A(Of IComparable).B(Of ), alpha-renamed type parameter S will have type constraint IComparable.
            ''' </summary>
            Private ReadOnly _typeParameters As ImmutableArray(Of TypeParameterSymbol)
            Private ReadOnly _parameters As ImmutableArray(Of ParameterSymbol)

            Public Shared Function Create(
                container As SubstitutedNamedType,
                originalDefinition As MethodSymbol
            ) As SpecializedGenericMethod
                Debug.Assert(originalDefinition.IsDefinition)
                Debug.Assert(originalDefinition.Arity > 0)

                ' Create alpha-renamed type parameters.
                ' Note that these type parameters don't have their containing symbol set yet.
                ' It will be done later, in the constructor of this type.

                Dim typeParametersDefinitions As ImmutableArray(Of TypeParameterSymbol) = originalDefinition.TypeParameters
                Dim alphaRenamedTypeParameters = New SubstitutedTypeParameterSymbol(typeParametersDefinitions.Length - 1) {}

                For i As Integer = 0 To typeParametersDefinitions.Length - 1 Step 1
                    alphaRenamedTypeParameters(i) = New SubstitutedTypeParameterSymbol(typeParametersDefinitions(i))
                Next

                Dim newTypeParameters = alphaRenamedTypeParameters.AsImmutableOrNull()

                ' Add a substitution to map from type parameter definitions to corresponding
                ' alpha-renamed type parameters.
                Debug.Assert(container.TypeSubstitution IsNot Nothing AndAlso
                             container.TypeSubstitution.TargetGenericDefinition Is originalDefinition.ContainingSymbol)
                Dim substitution = VisualBasic.Symbols.TypeSubstitution.CreateForAlphaRename(container.TypeSubstitution, newTypeParameters)
                Debug.Assert(substitution.TargetGenericDefinition Is originalDefinition)

                ' Now create the symbol.
                Return New SpecializedGenericMethod(container, substitution, newTypeParameters)
            End Function

            Private Sub New(
                container As SubstitutedNamedType,
                substitution As TypeSubstitution,
                typeParameters As ImmutableArray(Of SubstitutedTypeParameterSymbol)
            )
                MyBase.New(container)
                Debug.Assert(substitution.TargetGenericDefinition.IsDefinition)
                Debug.Assert(Not typeParameters.IsDefault AndAlso typeParameters.Length = DirectCast(substitution.TargetGenericDefinition, MethodSymbol).Arity)

                _substitution = substitution
                _typeParameters = StaticCast(Of TypeParameterSymbol).From(typeParameters)

                ' Set container for type parameters
                For Each param In typeParameters
                    param.SetContainingSymbol(Me)
                Next

                _parameters = SubstituteParameters()
            End Sub

            Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
                Get
                    Return _parameters
                End Get
            End Property

            Public Overrides ReadOnly Property TypeSubstitution As TypeSubstitution
                Get
                    Return _substitution
                End Get
            End Property

            Public Overrides ReadOnly Property OriginalDefinition As MethodSymbol
                Get
                    Return DirectCast(_substitution.TargetGenericDefinition, MethodSymbol)
                End Get
            End Property

            Friend Overrides ReadOnly Property CanConstruct As Boolean
                Get
                    ' Cannot construct this method if any container of this method is a SpecializedGenericType.
                    Dim containerToCheck As NamedTypeSymbol = _container

                    Do
                        Debug.Assert(Not containerToCheck.IsDefinition)

                        If containerToCheck.Arity > 0 Then
                            If containerToCheck.ConstructedFrom Is containerToCheck Then
                                ' Run into a SpecializedGenericType
                                Debug.Assert(TypeOf containerToCheck Is SubstitutedNamedType.SpecializedGenericType)
                                Return False
                            Else
                                ' Run into a Constructed type
                                Debug.Assert(TypeOf containerToCheck Is SubstitutedNamedType.ConstructedType)
                                Return True
                            End If
                        End If

                        containerToCheck = containerToCheck.ContainingType
                    Loop While containerToCheck IsNot Nothing AndAlso Not containerToCheck.IsDefinition

                    Return True
                End Get
            End Property

            Public Overrides Function Construct(typeArguments As ImmutableArray(Of TypeSymbol)) As MethodSymbol
                CheckCanConstructAndTypeArguments(typeArguments)

                typeArguments = typeArguments.TransformToCanonicalFormFor(Me)

                If typeArguments.IsDefault Then
                    ' identity substitution
                    Return Me
                End If

                Debug.Assert(_substitution.Parent IsNot Nothing)
                Dim substitution = TypeSubstitution.Create(_substitution.Parent, _substitution.TargetGenericDefinition, typeArguments,
                                                           allowAlphaRenamedTypeParametersAsArguments:=True)
                Return New ConstructedSpecializedGenericMethod(Me, substitution, typeArguments)
            End Function

            Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
                Get
                    Return _typeParameters
                End Get
            End Property

            Public Overrides ReadOnly Property TypeArguments As ImmutableArray(Of TypeSymbol)
                Get
                    Return StaticCast(Of TypeSymbol).From(_typeParameters)
                End Get
            End Property

            Public Overrides Function Equals(obj As Object) As Boolean
                Return obj Is Me OrElse EqualsWithNoRegardToTypeArguments(TryCast(obj, SpecializedGenericMethod))
            End Function

        End Class

        ''' <summary>
        ''' Base class for symbols representing constructed generic methods.
        ''' For example: A(Of Integer), A.B(Of Integer), A(Of Integer).B.C(Of Integer).
        ''' </summary>
        Public MustInherit Class ConstructedMethod
            Inherits SubstitutedMethodSymbol

            Protected ReadOnly _substitution As TypeSubstitution
            Protected ReadOnly _typeArguments As ImmutableArray(Of TypeSymbol)

            Protected Sub New(substitution As TypeSubstitution, typeArguments As ImmutableArray(Of TypeSymbol))
                Debug.Assert(Not typeArguments.IsEmpty)
                Debug.Assert(substitution.TargetGenericDefinition.IsDefinition)
                _substitution = substitution
                _typeArguments = typeArguments
            End Sub

            Public Overrides ReadOnly Property TypeSubstitution As TypeSubstitution
                Get
                    Return _substitution
                End Get
            End Property

            Public NotOverridable Overrides ReadOnly Property OriginalDefinition As MethodSymbol
                Get
                    Return DirectCast(_substitution.TargetGenericDefinition, MethodSymbol)
                End Get
            End Property

            Friend Overrides ReadOnly Property CanConstruct As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides Function Construct(typeArguments As ImmutableArray(Of TypeSymbol)) As MethodSymbol
                Throw New InvalidOperationException()
            End Function

            Public Overrides ReadOnly Property TypeArguments As ImmutableArray(Of TypeSymbol)
                Get
                    Return _typeArguments
                End Get
            End Property

            Public Overrides Function GetHashCode() As Integer
                Dim _hash As Integer = MyBase.GetHashCode()

                For Each typeArgument In TypeArguments
                    _hash = Hash.Combine(typeArgument, _hash)
                Next

                Return _hash
            End Function

            Public Overrides Function Equals(obj As Object) As Boolean
                If obj Is Me Then
                    Return True
                End If

                Dim other = TryCast(obj, ConstructedMethod)

                If Not EqualsWithNoRegardToTypeArguments(other) Then
                    Return False
                End If

                Dim arguments = TypeArguments
                Dim otherArguments = other.TypeArguments
                Dim count As Integer = arguments.Length

                For i As Integer = 0 To count - 1 Step 1
                    If Not arguments(i).Equals(otherArguments(i)) Then
                        Return False
                    End If
                Next

                Return True
            End Function
        End Class

        ''' <summary>
        ''' Symbols representing constructed generic method that is contained within constructed generic type.
        ''' For example: A(Of Integer).B(Of Integer), A(Of Integer).B.C(Of Integer).
        ''' </summary>
        Public NotInheritable Class ConstructedSpecializedGenericMethod
            Inherits ConstructedMethod

            ''' <summary>
            ''' Symbol for the ConstructedFrom method.
            '''      A(Of Integer).B(Of ) for A(Of Integer).B(Of Integer),
            '''      A(Of Integer).B.C(Of ) for A(Of Integer).B.C(Of Integer)
            ''' 
            ''' All types in its containership hierarchy must be either constructed or non-generic, or original definitions.
            ''' </summary>
            Private ReadOnly _constructedFrom As SpecializedGenericMethod
            Private ReadOnly _parameters As ImmutableArray(Of ParameterSymbol)

            Public Sub New(constructedFrom As SpecializedGenericMethod, substitution As TypeSubstitution, typeArguments As ImmutableArray(Of TypeSymbol))
                MyBase.New(substitution, typeArguments)
                Debug.Assert(substitution.TargetGenericDefinition Is constructedFrom.OriginalDefinition)
                Debug.Assert(typeArguments.Length = constructedFrom.Arity)
                Debug.Assert(constructedFrom IsNot Nothing)
                Debug.Assert(substitution.Parent Is constructedFrom.TypeSubstitution.Parent)
                Debug.Assert(constructedFrom.CanConstruct)

                _constructedFrom = constructedFrom
                _parameters = SubstituteParameters()
            End Sub

            Public Overrides ReadOnly Property ConstructedFrom As MethodSymbol
                Get
                    Return _constructedFrom
                End Get
            End Property

            Public Overrides ReadOnly Property ContainingSymbol As Symbol
                Get
                    Return _constructedFrom.ContainingSymbol
                End Get
            End Property

            Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
                Get
                    Return _parameters
                End Get
            End Property

            Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
                Get
                    Return _constructedFrom.TypeParameters
                End Get
            End Property
        End Class

        ''' <summary>
        ''' Symbols representing constructed generic method that isn't contained within constructed generic type.
        ''' For example: A.B(Of Integer), but not A(Of Integer).B.C(Of Integer).
        ''' </summary>
        Public NotInheritable Class ConstructedNotSpecializedGenericMethod
            Inherits ConstructedMethod

            Private ReadOnly _parameters As ImmutableArray(Of ParameterSymbol)

            Public Sub New(substitution As TypeSubstitution, typeArguments As ImmutableArray(Of TypeSymbol))
                MyBase.New(substitution, typeArguments)
                Debug.Assert(substitution.Parent Is Nothing)
                Debug.Assert(typeArguments.Length = DirectCast(substitution.TargetGenericDefinition, MethodSymbol).Arity)

                _parameters = SubstituteParameters()
            End Sub

            Public Overrides ReadOnly Property ConstructedFrom As MethodSymbol
                Get
                    Return Me.OriginalDefinition
                End Get
            End Property

            Public Overrides ReadOnly Property ContainingSymbol As Symbol
                Get
                    Return Me.OriginalDefinition.ContainingSymbol
                End Get
            End Property

            Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
                Get
                    Return _parameters
                End Get
            End Property

            Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
                Get
                    Return Me.OriginalDefinition.TypeParameters
                End Get
            End Property

            Friend Overrides ReadOnly Property CallsiteReducedFromMethod As MethodSymbol
                Get
                    Dim reducedDef As MethodSymbol = Me.ReducedFrom

                    If reducedDef Is Nothing Then
                        Return Nothing
                    End If

                    If Me.Arity = reducedDef.Arity Then
                        Return reducedDef.Construct(Me.TypeArguments)
                    End If

                    Dim resultTypeArguments(reducedDef.Arity - 1) As TypeSymbol

                    For Each pair As KeyValuePair(Of TypeParameterSymbol, TypeSymbol) In Me.FixedTypeParameters
                        resultTypeArguments(pair.Key.Ordinal) = pair.Value
                    Next

                    Dim typeParameters As ImmutableArray(Of TypeParameterSymbol) = Me.TypeParameters
                    Dim typeArguments As ImmutableArray(Of TypeSymbol) = Me.TypeArguments

                    For i As Integer = 0 To typeArguments.Length - 1
                        resultTypeArguments(typeParameters(i).ReducedFrom.Ordinal) = typeArguments(i)
                    Next

                    Return reducedDef.Construct(resultTypeArguments.AsImmutableOrNull())
                End Get
            End Property
        End Class

    End Class

End Namespace
