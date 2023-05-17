' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind
Imports Microsoft.CodeAnalysis.Collections

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Represents a type other than an array, a type parameter.
    ''' </summary>
    Friend MustInherit Class NamedTypeSymbol
        Inherits TypeSymbol
        Implements INamedTypeSymbol, INamedTypeSymbolInternal

        Protected Shared ReadOnly s_requiredMembersErrorSentinel As ImmutableSegmentedDictionary(Of String, Symbol) = ImmutableSegmentedDictionary(Of String, Symbol).Empty.Add("<error sentinel>", Nothing)

        ''' <summary>
        ''' <see langword="Nothing" /> if uninitialized. <see cref="s_requiredMembersErrorSentinel"/> if there are errors.
        ''' <see cref="ImmutableSegmentedDictionary(Of String, Symbol).Empty"/> if there are no required members. Otherwise,
        ''' the required members.
        ''' </summary>
        Private _lazyRequiredMembers As ImmutableSegmentedDictionary(Of String, Symbol) = Nothing

        ' !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        ' Changes to the public interface of this class should remain synchronized with the C# version.
        ' Do not make any changes to the public interface without making the corresponding change
        ' to the C# version.
        ' !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        ''' <summary>
        ''' Returns the arity of this type, or the number of type parameters it takes.
        ''' A non-generic type has zero arity.
        ''' </summary>
        Public MustOverride ReadOnly Property Arity As Integer

        ''' <summary>
        ''' Returns the type parameters that this type has. If this is a non-generic type,
        ''' returns an empty ImmutableArray.  
        ''' </summary>
        Public MustOverride ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)

        ''' <summary>
        ''' Returns custom modifiers for the type argument that has been substituted for the type parameter. 
        ''' The modifiers correspond to the type argument at the same ordinal within the <see cref="TypeArgumentsNoUseSiteDiagnostics"/>
        ''' array.
        ''' </summary>
        Public MustOverride Function GetTypeArgumentCustomModifiers(ordinal As Integer) As ImmutableArray(Of CustomModifier)

        Friend Function GetEmptyTypeArgumentCustomModifiers(ordinal As Integer) As ImmutableArray(Of CustomModifier)
            If ordinal < 0 OrElse ordinal >= Me.Arity Then
                Throw New IndexOutOfRangeException()
            End If

            Return ImmutableArray(Of CustomModifier).Empty
        End Function

        Friend MustOverride ReadOnly Property HasTypeArgumentsCustomModifiers As Boolean

        ''' <summary>
        ''' Returns the type arguments that have been substituted for the type parameters. 
        ''' If nothing has been substituted for a given type parameter,
        ''' then the type parameter itself is consider the type argument.
        ''' </summary>
        Friend MustOverride ReadOnly Property TypeArgumentsNoUseSiteDiagnostics As ImmutableArray(Of TypeSymbol)

        Friend Function TypeArgumentsWithDefinitionUseSiteDiagnostics(<[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)) As ImmutableArray(Of TypeSymbol)
            Dim result = TypeArgumentsNoUseSiteDiagnostics

            For Each typeArgument In result
                typeArgument.OriginalDefinition.AddUseSiteInfo(useSiteInfo)
            Next

            Return result
        End Function

        Friend Function TypeArgumentWithDefinitionUseSiteDiagnostics(index As Integer, <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)) As TypeSymbol
            Dim result = TypeArgumentsNoUseSiteDiagnostics(index)
            result.OriginalDefinition.AddUseSiteInfo(useSiteInfo)
            Return result
        End Function

        ''' <summary>
        ''' Returns the type symbol that this type was constructed from. This type symbol
        ''' has the same containing type, but has type arguments that are the same
        ''' as the type parameters (although its containing type might not).
        ''' </summary>
        Public MustOverride ReadOnly Property ConstructedFrom As NamedTypeSymbol

        ''' <summary>
        ''' For enum types, gets the underlying type. Returns null on all other
        ''' kinds of types.
        ''' </summary>
        Public Overridable ReadOnly Property EnumUnderlyingType As NamedTypeSymbol
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingType As NamedTypeSymbol
            Get
                Return TryCast(Me.ContainingSymbol, NamedTypeSymbol)
            End Get
        End Property

        ''' <summary>
        ''' For implicitly declared delegate types returns the EventSymbol that caused this
        ''' delegate type to be generated.
        ''' For all other types returns null.
        ''' Note, the set of possible associated symbols might be expanded in the future to 
        ''' reflect changes in the languages.
        ''' </summary>
        Public Overridable ReadOnly Property AssociatedSymbol As Symbol
            Get
                Return Nothing
            End Get
        End Property

        ''' <summary>
        ''' Returns True for one of the types from a set of Structure types if
        ''' that set represents a cycle. This property is intended for flow
        ''' analysis only since it is only implemented for source types,
        ''' and only returns True for one of the types within a cycle, not all.
        ''' </summary>
        Friend Overridable ReadOnly Property KnownCircularStruct As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Is this a NoPia local type explicitly declared in source, i.e.
        ''' top level type with a TypeIdentifier attribute on it?
        ''' </summary>
        Friend Overridable ReadOnly Property IsExplicitDefinitionOfNoPiaLocalType As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Returns true and a string from the first GuidAttribute on the type, 
        ''' the string might be null or an invalid guid representation. False, 
        ''' if there is no GuidAttribute with string argument.
        ''' </summary>
        Friend Overridable Function GetGuidString(ByRef guidString As String) As Boolean
            Return GetGuidStringDefaultImplementation(guidString)
        End Function

        ' Named types have the arity suffix added to the metadata name.
        Public Overrides ReadOnly Property MetadataName As String
            Get
                ' CLR generally allows names with dots, however some APIs like IMetaDataImport
                ' can only return full type names combined with namespaces. 
                ' see: http://msdn.microsoft.com/en-us/library/ms230143.aspx (IMetaDataImport::GetTypeDefProps)
                ' When working with such APIs, names with dots become ambiguous since metadata 
                ' consumer cannot figure where namespace ends and actual type name starts.
                ' Therefore it is a good practice to avoid type names with dots.
                Debug.Assert(Me.IsErrorType OrElse Not (TypeOf Me Is SourceNamedTypeSymbol) OrElse Not Name.Contains("."), "type name contains dots: " + Name)

                Return If(MangleName, MetadataHelpers.ComposeAritySuffixedMetadataName(Name, Arity, associatedFileIdentifier:=Nothing), Name)
            End Get
        End Property

        ''' <summary>
        ''' True if the type itself Is excluded from code coverage instrumentation.
        ''' True for source types marked with <see cref="AttributeDescription.ExcludeFromCodeCoverageAttribute"/>.
        ''' </summary>
        Friend Overridable ReadOnly Property IsDirectlyExcludedFromCodeCoverage As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Should the name returned by Name property be mangled with [`arity] suffix in order to get metadata name.
        ''' Must return False for a type with Arity == 0.
        ''' </summary>
        ''' <remarks>
        ''' Default implementation to force consideration of appropriate implementation for each new subclass
        ''' </remarks>
        Friend MustOverride ReadOnly Property MangleName As Boolean

        ''' <summary>
        '''  True if this symbol has a special name (metadata flag SpecialName is set).
        ''' </summary>
        Friend MustOverride ReadOnly Property HasSpecialName As Boolean

        ''' <summary>
        '''  True if this type is considered serializable (metadata flag Serializable is set).
        ''' </summary>
        Public MustOverride ReadOnly Property IsSerializable As Boolean Implements INamedTypeSymbol.IsSerializable

        ''' <summary>
        ''' Type layout information (ClassLayout metadata and layout kind flags).
        ''' </summary>
        Friend MustOverride ReadOnly Property Layout As TypeLayout

        ''' <summary>
        ''' The default charset used for type marshalling. 
        ''' Can be changed via <see cref="DefaultCharSetAttribute"/> applied on the containing module.
        ''' </summary>
        Protected ReadOnly Property DefaultMarshallingCharSet As CharSet
            Get
                Return If(EffectiveDefaultMarshallingCharSet, CharSet.Ansi)
            End Get
        End Property

        ''' <summary>
        ''' Marshalling charset of string data fields within the type (string formatting flags in metadata).
        ''' </summary>
        Friend MustOverride ReadOnly Property MarshallingCharSet As CharSet

        ''' <summary>
        ''' For delegate types, gets the delegate's invoke method.  Returns null on
        ''' all other kinds of types.  Note that it is possible to have an ill-formed 
        ''' delegate type imported from metadata which does not have an Invoke method.
        ''' Such a type will be classified as a delegate but its DelegateInvokeMethod
        ''' would be null.
        ''' </summary>
        Public Overridable ReadOnly Property DelegateInvokeMethod As MethodSymbol
            Get
                If TypeKind <> TypeKind.Delegate Then
                    Return Nothing
                End If

                Dim methods As ImmutableArray(Of Symbol) = GetMembers(WellKnownMemberNames.DelegateInvokeName)
                If methods.Length <> 1 Then
                    Return Nothing
                End If

                Dim method = TryCast(methods(0), MethodSymbol)

                'EDMAURER we used to also check 'method.IsOverridable' because section 13.6
                'of the CLI spec dictates that it be virtual, but real world
                'working metadata has been found that contains an Invoke method that is
                'marked as virtual but not newslot (both of those must be combined to
                'meet the definition of virtual). Rather than weaken the check
                'I've removed it, as the Dev10 C# compiler makes no check, and we don't
                'stand to gain anything by having it.
                'Return If(method IsNot Nothing AndAlso method.IsOverridable, method, Nothing)
                Return method
            End Get
        End Property

        ''' <summary>
        ''' Returns true if this type was declared as requiring a derived class;
        ''' i.e., declared with the "MustInherit" modifier. Always true for interfaces.
        ''' </summary>
        Public MustOverride ReadOnly Property IsMustInherit As Boolean

        ''' <summary>
        ''' Returns true if this type does not allow derived types; i.e., declared
        ''' with the NotInheritable modifier, or else declared as a Module, Structure,
        ''' Enum, or Delegate.
        ''' </summary>
        Public MustOverride ReadOnly Property IsNotInheritable As Boolean

        ''' <summary>
        ''' If this property returns false, it is certain that there are no extension
        ''' methods inside this type. If this property returns true, it is highly likely
        ''' (but not certain) that this type contains extension methods. This property allows
        ''' the search for extension methods to be narrowed much more quickly.
        ''' 
        ''' !!! Note that this property can mutate during lifetime of the symbol !!!
        ''' !!! from True to False, as we learn more about the type.             !!! 
        ''' </summary>
        Public MustOverride ReadOnly Property MightContainExtensionMethods As Boolean Implements INamedTypeSymbol.MightContainExtensionMethods

        ''' <summary>
        ''' Returns True if the type is marked by 'Microsoft.CodeAnalysis.Embedded' attribute. 
        ''' </summary>
        Friend MustOverride ReadOnly Property HasCodeAnalysisEmbeddedAttribute As Boolean

        ''' <summary>
        ''' Returns True if the type is marked by 'Microsoft.VisualBasic.Embedded' attribute. 
        ''' </summary>
        Friend MustOverride ReadOnly Property HasVisualBasicEmbeddedAttribute As Boolean

        ''' <summary>
        ''' A Named type is an extensible interface if both the following are true:
        ''' (a) It is an interface type and
        ''' (b) It is either marked with 'TypeLibTypeAttribute( flags w/o TypeLibTypeFlags.FNonExtensible )' attribute OR
        '''     is marked with 'InterfaceTypeAttribute( flags with ComInterfaceType.InterfaceIsIDispatch )' attribute OR
        '''     inherits from an extensible interface type.
        ''' Member resolution for Extensible interfaces is late bound, i.e. members are resolved at run time by looking up the identifier on the actual run-time type of the expression. 
        ''' </summary>
        Friend MustOverride ReadOnly Property IsExtensibleInterfaceNoUseSiteDiagnostics As Boolean

        ''' <summary>
        ''' This method is an entry point for the Binder to collect extension methods with the given name
        ''' declared within this named type. Overridden by RetargetingNamedTypeSymbol.
        ''' </summary>
        Friend Overridable Sub AppendProbableExtensionMethods(name As String, methods As ArrayBuilder(Of MethodSymbol))
            If Me.MightContainExtensionMethods Then
                For Each member As Symbol In Me.GetMembers(name)
                    If member.Kind = SymbolKind.Method Then
                        Dim method = DirectCast(member, MethodSymbol)

                        If method.MayBeReducibleExtensionMethod Then
                            methods.Add(method)
                        End If
                    End If
                Next
            End If
        End Sub

        ''' <summary>
        ''' This method is called for a type within a namespace when we are building a map of extension methods 
        ''' for the whole (compilation merged or module level) namespace.
        ''' 
        ''' The 'appendThrough' parameter allows RetargetingNamespaceSymbol to delegate majority of the work 
        ''' to the underlying named type symbols, but still add RetargetingMethodSymbols to the map.
        ''' </summary>
        Friend Overridable Sub BuildExtensionMethodsMap(
            map As Dictionary(Of String, ArrayBuilder(Of MethodSymbol)),
            appendThrough As NamespaceSymbol
        )
            If Me.MightContainExtensionMethods Then
                Debug.Assert(False, "Possibly using inefficient implementation of AppendProbableExtensionMethods(map As Dictionary(Of String, ArrayBuilder(Of MethodSymbol)))")
                appendThrough.BuildExtensionMethodsMap(map,
                                         From name As String In Me.MemberNames
                                         Select New KeyValuePair(Of String, ImmutableArray(Of Symbol))(name, Me.GetMembers(name)))
            End If
        End Sub

        Friend Overridable Sub GetExtensionMethods(
            methods As ArrayBuilder(Of MethodSymbol),
            appendThrough As NamespaceSymbol,
            Name As String
        )
            If Me.MightContainExtensionMethods Then

                Dim candidates = Me.GetSimpleNonTypeMembers(Name)

                For Each member In candidates
                    appendThrough.AddMemberIfExtension(methods, member)
                Next
            End If
        End Sub

        ''' <summary>
        ''' This is an entry point for the Binder. Its purpose is to add names of viable extension methods declared 
        ''' in this type to nameSet parameter.
        ''' </summary>
        Friend Overridable Overloads Sub AddExtensionMethodLookupSymbolsInfo(nameSet As LookupSymbolsInfo,
                                                                              options As LookupOptions,
                                                                              originalBinder As Binder)
            AddExtensionMethodLookupSymbolsInfo(nameSet, options, originalBinder, appendThrough:=Me)
        End Sub

        ''' <summary>
        ''' Add names of viable extension methods declared in this type to nameSet parameter.
        ''' 
        ''' The 'appendThrough' parameter allows RetargetingNamedTypeSymbol to delegate majority of the work 
        ''' to the underlying named type symbol, but still perform viability check on RetargetingMethodSymbol.
        ''' </summary>
        Friend Overridable Overloads Sub AddExtensionMethodLookupSymbolsInfo(nameSet As LookupSymbolsInfo,
                                                                              options As LookupOptions,
                                                                              originalBinder As Binder,
                                                                              appendThrough As NamedTypeSymbol)
            If Me.MightContainExtensionMethods Then
                Debug.Assert(False, "Possibly using inefficient implementation of AppendExtensionMethodNames(nameSet As HashSet(Of String), options As LookupOptions, originalBinder As Binder, appendThrough As NamespaceOrTypeSymbol)")
                appendThrough.AddExtensionMethodLookupSymbolsInfo(nameSet, options, originalBinder,
                                                             From name As String In Me.MemberNames
                                                             Select New KeyValuePair(Of String, ImmutableArray(Of Symbol))(name, Me.GetMembers(name)))
            End If
        End Sub

        ''' <summary>
        ''' Get the instance constructors for this type.
        ''' </summary>
        Public ReadOnly Property InstanceConstructors As ImmutableArray(Of MethodSymbol)
            Get
                Return GetConstructors(Of MethodSymbol)(includeInstance:=True, includeShared:=False)
            End Get
        End Property

        ''' <summary>
        ''' Get the shared constructors for this type.
        ''' </summary>
        Public ReadOnly Property SharedConstructors As ImmutableArray(Of MethodSymbol)
            Get
                Return GetConstructors(Of MethodSymbol)(includeInstance:=False, includeShared:=True)
            End Get
        End Property

        ''' <summary>
        ''' Get the instance and shared constructors for this type.
        ''' </summary>
        Public ReadOnly Property Constructors As ImmutableArray(Of MethodSymbol)
            Get
                Return GetConstructors(Of MethodSymbol)(includeInstance:=True, includeShared:=True)
            End Get
        End Property

        Private Function GetConstructors(Of TMethodSymbol As {IMethodSymbol, Class})(includeInstance As Boolean, includeShared As Boolean) As ImmutableArray(Of TMethodSymbol)
            Debug.Assert(includeInstance OrElse includeShared)

            Dim instanceCandidates As ImmutableArray(Of Symbol) = If(includeInstance, GetMembers(WellKnownMemberNames.InstanceConstructorName), ImmutableArray(Of Symbol).Empty)
            Dim sharedCandidates As ImmutableArray(Of Symbol) = If(includeShared, GetMembers(WellKnownMemberNames.StaticConstructorName), ImmutableArray(Of Symbol).Empty)

            If instanceCandidates.IsEmpty AndAlso sharedCandidates.IsEmpty Then
                Return ImmutableArray(Of TMethodSymbol).Empty
            End If

            Dim constructors As ArrayBuilder(Of TMethodSymbol) = ArrayBuilder(Of TMethodSymbol).GetInstance()
            For Each candidate In instanceCandidates
                If candidate.Kind = SymbolKind.Method Then
                    Dim method As TMethodSymbol = TryCast(candidate, TMethodSymbol)
                    Debug.Assert(method IsNot Nothing)
                    Debug.Assert(method.MethodKind = MethodKind.Constructor)
                    constructors.Add(method)
                End If
            Next
            For Each candidate In sharedCandidates
                If candidate.Kind = SymbolKind.Method Then
                    Dim method As TMethodSymbol = TryCast(candidate, TMethodSymbol)
                    Debug.Assert(method IsNot Nothing)
                    Debug.Assert(method.MethodKind = MethodKind.StaticConstructor)
                    constructors.Add(method)
                End If
            Next
            Return constructors.ToImmutableAndFree()
        End Function

        ''' <summary>
        ''' Returns true if this type is known to be a reference type. It is never the case
        ''' that IsReferenceType and IsValueType both return true. However, for an unconstrained
        ''' type parameter, IsReferenceType and IsValueType will both return false.
        ''' </summary>
        Public Overrides ReadOnly Property IsReferenceType As Boolean
            Get
                ' TODO: Is this correct for VB Module?
                Return TypeKind <> TypeKind.Enum AndAlso TypeKind <> TypeKind.Structure AndAlso
                    TypeKind <> TypeKind.Error
            End Get
        End Property

        ''' <summary>
        ''' Returns true if this type is known to be a value type. It is never the case
        ''' that IsReferenceType and IsValueType both return true. However, for an unconstrained
        ''' type parameter, IsReferenceType and IsValueType will both return false.
        ''' </summary>
        Public Overrides ReadOnly Property IsValueType As Boolean
            Get
                ' TODO: Is this correct for VB Module?
                Return TypeKind = TypeKind.Enum OrElse TypeKind = TypeKind.Structure
            End Get
        End Property

        ''' <summary>
        ''' Returns True if this types has Arity >= 1 and Construct can be called. This is primarily useful
        ''' when deal with error cases.
        ''' </summary>
        Friend MustOverride ReadOnly Property CanConstruct As Boolean

        ''' <summary>
        ''' Returns a constructed type given its type arguments.
        ''' </summary>
        Public Function Construct(ParamArray typeArguments() As TypeSymbol) As NamedTypeSymbol
            Return Construct(typeArguments.AsImmutableOrNull())
        End Function

        ''' <summary>
        ''' Returns a constructed type given its type arguments.
        ''' </summary>
        Public Function Construct(typeArguments As IEnumerable(Of TypeSymbol)) As NamedTypeSymbol
            Return Construct(typeArguments.AsImmutableOrNull())
        End Function

        ''' <summary>
        ''' Construct a new type from this type, substituting the given type arguments for the 
        ''' type parameters. This method should only be called if this named type does not have
        ''' any substitutions applied for its own type arguments with exception of alpha-rename
        ''' substitution (although it's container might have substitutions applied).
        ''' </summary>
        ''' <param name="typeArguments">A set of type arguments to be applied. Must have the same length
        ''' as the number of type parameters that this type has.</param>
        Public MustOverride Function Construct(typeArguments As ImmutableArray(Of TypeSymbol)) As NamedTypeSymbol

        ''' <summary> Checks for validity of Construct(...) on this type with these type arguments. </summary>
        Protected Sub CheckCanConstructAndTypeArguments(typeArguments As ImmutableArray(Of TypeSymbol))
            'This helper is used by Public APIs to perform validation. This exception is part of the public
            'contract of Construct()
            If Not CanConstruct OrElse Me IsNot ConstructedFrom Then
                Throw New InvalidOperationException()
            End If

            ' Check type arguments
            typeArguments.CheckTypeArguments(Me.Arity)
        End Sub

        ''' <summary>
        ''' Construct a new type from this type definition, substituting the given type arguments for the 
        ''' type parameters. This method should only be called on the OriginalDefinition. Unlike previous 
        ''' Construct method, this overload supports type parameter substitution on this type and any number
        ''' of its containing types. See comments for TypeSubstitution type for more information.
        ''' </summary>
        Friend Function Construct(substitution As TypeSubstitution) As NamedTypeSymbol
            Debug.Assert(Me.IsDefinition)
            Debug.Assert(Me.IsOrInGenericType())

            If substitution Is Nothing Then
                Return Me
            End If

            Debug.Assert(substitution.IsValidToApplyTo(Me))

            ' Validate the map for use of alpha-renamed type parameters.
            substitution.ThrowIfSubstitutingToAlphaRenamedTypeParameter()

            Return DirectCast(InternalSubstituteTypeParameters(substitution).AsTypeSymbolOnly(), NamedTypeSymbol)
        End Function

        ''' <summary>
        ''' Returns an unbound generic type of this generic named type.
        ''' </summary>
        Public Function ConstructUnboundGenericType() As NamedTypeSymbol
            Return Me.AsUnboundGenericType()
        End Function

        ''' <summary>
        ''' Returns Default property name for the type.
        ''' If there is no default property name, then Nothing is returned.
        ''' </summary>
        Friend MustOverride ReadOnly Property DefaultPropertyName As String

        ''' <summary>
        ''' If this is a generic type instantiation or a nested type of a generic type instantiation,
        ''' return TypeSubstitution for this construction. Nothing otherwise.
        ''' Returned TypeSubstitution should target OriginalDefinition of the symbol.
        ''' </summary>
        Friend MustOverride ReadOnly Property TypeSubstitution As TypeSubstitution

        ' These properties of TypeRef, NamespaceOrType, or Symbol must be overridden.

        ''' <summary>
        ''' Gets the name of this symbol.
        ''' </summary>
        Public MustOverride Overrides ReadOnly Property Name As String

        ''' <summary>
        ''' Collection of names of members declared within this type.
        ''' </summary>
        Public MustOverride ReadOnly Property MemberNames As IEnumerable(Of String)

        ''' <summary>
        ''' Returns true if the type is a Script class. 
        ''' It might be an interactive submission class or a Script class in a csx file.
        ''' </summary>
        Public Overridable ReadOnly Property IsScriptClass As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Returns true if the type is a submission class. 
        ''' </summary>
        Public ReadOnly Property IsSubmissionClass As Boolean
            Get
                Return TypeKind = TypeKind.Submission
            End Get
        End Property

        Friend Function GetScriptConstructor() As SynthesizedConstructorBase
            Debug.Assert(IsScriptClass)
            Return DirectCast(InstanceConstructors.Single(), SynthesizedConstructorBase)
        End Function

        Friend Function GetScriptInitializer() As SynthesizedInteractiveInitializerMethod
            Debug.Assert(IsScriptClass)
            Return DirectCast(GetMembers(SynthesizedInteractiveInitializerMethod.InitializerName).Single(), SynthesizedInteractiveInitializerMethod)
        End Function

        Friend Function GetScriptEntryPoint() As SynthesizedEntryPointSymbol
            Debug.Assert(IsScriptClass)
            Dim name = If(TypeKind = TypeKind.Submission, SynthesizedEntryPointSymbol.FactoryName, SynthesizedEntryPointSymbol.MainName)
            Return DirectCast(GetMembers(name).Single(), SynthesizedEntryPointSymbol)
        End Function

        ''' <summary>
        ''' Returns true if the type is the implicit class that holds onto invalid global members (like methods or
        ''' statements in a non script file).
        ''' </summary>
        Public Overridable ReadOnly Property IsImplicitClass As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Get all the members of this symbol.
        ''' </summary>
        ''' <returns>An ImmutableArray containing all the members of this symbol. If this symbol has no members,
        ''' returns an empty ImmutableArray. Never returns Nothing.</returns>
        Public MustOverride Overrides Function GetMembers() As ImmutableArray(Of Symbol)

        ''' <summary>
        ''' Get all the members of this symbol that have a particular name.
        ''' </summary>
        ''' <returns>An ImmutableArray containing all the members of this symbol with the given name. If there are
        ''' no members with this name, returns an empty ImmutableArray. Never returns Nothing.</returns>
        Public MustOverride Overrides Function GetMembers(name As String) As ImmutableArray(Of Symbol)

        ''' <summary>
        ''' Get all the members of this symbol that are types.
        ''' </summary>
        ''' <returns>An ImmutableArray containing all the types that are members of this symbol. If this symbol has no type members,
        ''' returns an empty ImmutableArray. Never returns Nothing.</returns>
        Public MustOverride Overrides Function GetTypeMembers() As ImmutableArray(Of NamedTypeSymbol)

        ''' <summary>
        ''' Get all the members of this symbol that are types that have a particular name, and any arity.
        ''' </summary>
        ''' <returns>An ImmutableArray containing all the types that are members of this symbol with the given name. 
        ''' If this symbol has no type members with this name,
        ''' returns an empty ImmutableArray. Never returns Nothing.</returns>
        Public MustOverride Overrides Function GetTypeMembers(name As String) As ImmutableArray(Of NamedTypeSymbol)

        ''' <summary>
        ''' Get all the members of this symbol that are types that have a particular name and arity.
        ''' </summary>
        ''' <returns>An ImmutableArray containing all the types that are members of this symbol with the given name and arity.
        ''' If this symbol has no type members with this name and arity,
        ''' returns an empty ImmutableArray. Never returns Nothing.</returns>
        Public MustOverride Overrides Function GetTypeMembers(name As String, arity As Integer) As ImmutableArray(Of NamedTypeSymbol)

        ''' <summary>
        ''' Get this accessibility that was declared on this symbol. For symbols that do
        ''' not have accessibility declared on them, returns NotApplicable.
        ''' </summary>
        Public MustOverride Overrides ReadOnly Property DeclaredAccessibility As Accessibility

        ''' <summary>
        ''' Supports visitor pattern. 
        ''' </summary>
        Friend Overrides Function Accept(Of TArgument, TResult)(visitor As VisualBasicSymbolVisitor(Of TArgument, TResult), arg As TArgument) As TResult
            Return visitor.VisitNamedType(Me, arg)
        End Function

        ' Only the compiler can created NamedTypeSymbols.
        Friend Sub New()
        End Sub

        ''' <summary>
        ''' Gets the kind of this symbol.
        ''' </summary>
        Public Overrides ReadOnly Property Kind As SymbolKind ' Cannot seal this method because of the ErrorSymbol.
            Get
                Return SymbolKind.NamedType
            End Get
        End Property

        ''' <summary>
        ''' Returns a flag indicating whether this symbol is ComImport.
        ''' </summary>
        ''' <remarks>
        ''' A type can me marked as a ComImport type in source by applying the <see cref="System.Runtime.InteropServices.ComImportAttribute"/>
        ''' </remarks>
        Friend MustOverride ReadOnly Property IsComImport As Boolean

        ''' <summary>
        ''' If CoClassAttribute was applied to the type returns the type symbol for the argument. 
        ''' Type symbol may be an error type if the type was not found. Otherwise returns Nothing
        ''' </summary>
        Friend MustOverride ReadOnly Property CoClassType As TypeSymbol

        ''' <summary>
        ''' Returns a sequence of preprocessor symbols specified in <see cref="ConditionalAttribute"/> applied on this symbol, or null if there are none.
        ''' </summary>
        Friend MustOverride Function GetAppliedConditionalSymbols() As ImmutableArray(Of String)

        ''' <summary>
        ''' Returns a flag indicating whether this symbol has at least one applied conditional attribute.
        ''' </summary>
        ''' <remarks>
        ''' Forces binding and decoding of attributes.
        ''' NOTE: Conditional symbols on base type must be inherited by derived type, but the native VB compiler doesn't do so. We maintain compatibility.
        ''' </remarks>
        Friend ReadOnly Property IsConditional As Boolean
            Get
                Return Me.GetAppliedConditionalSymbols().Any()
            End Get
        End Property

        Friend Overridable ReadOnly Property AreMembersImplicitlyDeclared As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary> 
        ''' Gets the associated <see cref="AttributeUsageInfo"/> for an attribute type.
        ''' </summary>
        Friend MustOverride Function GetAttributeUsageInfo() As AttributeUsageInfo

        ''' <summary>
        ''' Declaration security information associated with this type, or null if there is none.
        ''' </summary>
        Friend MustOverride Function GetSecurityInformation() As IEnumerable(Of Microsoft.Cci.SecurityAttribute)

        ''' <summary>
        ''' True if the type has declarative security information (HasSecurity flags).
        ''' </summary>
        Friend MustOverride ReadOnly Property HasDeclarativeSecurity As Boolean

        'This represents the declared base type and base interfaces, once bound. 
        Private _lazyDeclaredBase As NamedTypeSymbol = ErrorTypeSymbol.UnknownResultType
        Private _lazyDeclaredInterfaces As ImmutableArray(Of NamedTypeSymbol) = Nothing

        ''' <summary>
        ''' NamedTypeSymbol calls derived implementations of this method when declared base type
        ''' is needed for the first time.
        ''' 
        ''' basesBeingResolved are passed if there are any types already have their bases resolved
        ''' so that the derived implementation could avoid infinite recursion
        ''' </summary>
        Friend MustOverride Function MakeDeclaredBase(basesBeingResolved As BasesBeingResolved, diagnostics As BindingDiagnosticBag) As NamedTypeSymbol

        ''' <summary>
        ''' NamedTypeSymbol calls derived implementations of this method when declared interfaces
        ''' are needed for the first time.
        ''' 
        ''' basesBeingResolved are passed if there are any types already have their bases resolved
        ''' so that the derived implementation could avoid infinite recursion
        ''' </summary>
        Friend MustOverride Function MakeDeclaredInterfaces(basesBeingResolved As BasesBeingResolved, diagnostics As BindingDiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)

        ''' <summary>
        ''' Base type as "declared".
        ''' Declared base type may contain circularities.
        ''' 
        ''' If DeclaredBase must be accessed while other DeclaredBases are being resolved, 
        ''' the bases that are being resolved must be specified here to prevent potential infinite recursion.
        ''' </summary>
        Friend Overridable Function GetDeclaredBase(basesBeingResolved As BasesBeingResolved) As NamedTypeSymbol
            If _lazyDeclaredBase Is ErrorTypeSymbol.UnknownResultType Then
                Dim diagnostics = BindingDiagnosticBag.GetInstance()
                AtomicStoreReferenceAndDiagnostics(_lazyDeclaredBase, MakeDeclaredBase(basesBeingResolved, diagnostics), diagnostics, ErrorTypeSymbol.UnknownResultType)
                diagnostics.Free()
            End If

            Return _lazyDeclaredBase
        End Function

        Friend Overridable Function GetSimpleNonTypeMembers(name As String) As ImmutableArray(Of Symbol)
            Return GetMembers(name)
        End Function

        Private Sub AtomicStoreReferenceAndDiagnostics(Of T As Class)(ByRef variable As T,
                                                                     value As T,
                                                                     diagBag As BindingDiagnosticBag,
                                                                     Optional comparand As T = Nothing)
            Debug.Assert(value IsNot comparand)

            If diagBag Is Nothing OrElse diagBag.IsEmpty Then
                Interlocked.CompareExchange(variable, value, comparand)
            Else
                Dim sourceModule = TryCast(Me.ContainingModule, SourceModuleSymbol)
                If sourceModule IsNot Nothing Then
                    sourceModule.AtomicStoreReferenceAndDiagnostics(variable, value, diagBag, comparand)
                End If
            End If
        End Sub

        Friend Sub AtomicStoreArrayAndDiagnostics(Of T)(ByRef variable As ImmutableArray(Of T),
                                                             value As ImmutableArray(Of T),
                                                             diagBag As BindingDiagnosticBag)
            Debug.Assert(Not value.IsDefault)

            If diagBag Is Nothing OrElse diagBag.IsEmpty Then
                ImmutableInterlocked.InterlockedCompareExchange(variable, value, Nothing)
            Else
                Dim sourceModule = TryCast(Me.ContainingModule, SourceModuleSymbol)
                If sourceModule IsNot Nothing Then
                    sourceModule.AtomicStoreArrayAndDiagnostics(variable, value, diagBag)
                End If
            End If
        End Sub

        ''' <summary>
        ''' Interfaces as "declared".
        ''' Declared interfaces may contain circularities.
        ''' 
        ''' If DeclaredInterfaces must be accessed while other DeclaredInterfaces are being resolved, 
        ''' the bases that are being resolved must be specified here to prevent potential infinite recursion.
        ''' </summary>
        Friend Overridable Function GetDeclaredInterfacesNoUseSiteDiagnostics(basesBeingResolved As BasesBeingResolved) As ImmutableArray(Of NamedTypeSymbol)
            If _lazyDeclaredInterfaces.IsDefault Then
                Dim diagnostics = BindingDiagnosticBag.GetInstance()
                AtomicStoreArrayAndDiagnostics(_lazyDeclaredInterfaces, MakeDeclaredInterfaces(basesBeingResolved, diagnostics), diagnostics)
                diagnostics.Free()
            End If

            Return _lazyDeclaredInterfaces
        End Function

        Friend Function GetDeclaredInterfacesWithDefinitionUseSiteDiagnostics(basesBeingResolved As BasesBeingResolved, <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)) As ImmutableArray(Of NamedTypeSymbol)
            Dim result = GetDeclaredInterfacesNoUseSiteDiagnostics(basesBeingResolved)

            For Each iface In result
                iface.OriginalDefinition.AddUseSiteInfo(useSiteInfo)
            Next

            Return result
        End Function

        Friend Function GetDirectBaseInterfacesNoUseSiteDiagnostics(basesBeingResolved As BasesBeingResolved) As ImmutableArray(Of NamedTypeSymbol)
            If Me.TypeKind = TypeKind.Interface Then
                If basesBeingResolved.InheritsBeingResolvedOpt Is Nothing Then
                    Return Me.InterfacesNoUseSiteDiagnostics
                Else
                    Return GetDeclaredBaseInterfacesSafe(basesBeingResolved)
                End If
            Else
                Return ImmutableArray(Of NamedTypeSymbol).Empty
            End If
        End Function

        Friend Overridable Function GetDeclaredBaseInterfacesSafe(basesBeingResolved As BasesBeingResolved) As ImmutableArray(Of NamedTypeSymbol)
            Debug.Assert(Me.IsInterface)
            Debug.Assert(basesBeingResolved.InheritsBeingResolvedOpt.Any)
            If basesBeingResolved.InheritsBeingResolvedOpt.Contains(Me) Then
                Return Nothing
            End If

            Return GetDeclaredInterfacesNoUseSiteDiagnostics(basesBeingResolved.PrependInheritsBeingResolved(Me))
        End Function

        ''' <summary>
        ''' NamedTypeSymbol calls derived implementations of this method when acyclic base type
        ''' is needed for the first time.
        ''' This method typically calls GetDeclaredBase, filters for 
        ''' illegal cycles and other conditions before returning result as acyclic.
        ''' </summary>
        Friend MustOverride Function MakeAcyclicBaseType(diagnostics As BindingDiagnosticBag) As NamedTypeSymbol

        ''' <summary>
        ''' NamedTypeSymbol calls derived implementations of this method when acyclic base interfaces
        ''' are needed for the first time.
        ''' This method typically calls GetDeclaredInterfaces, filters for 
        ''' illegal cycles and other conditions before returning result as acyclic.
        ''' </summary>
        Friend MustOverride Function MakeAcyclicInterfaces(diagnostics As BindingDiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)

        Private _lazyBaseType As NamedTypeSymbol = ErrorTypeSymbol.UnknownResultType
        Private _lazyInterfaces As ImmutableArray(Of NamedTypeSymbol)

        ''' <summary>
        ''' Base type. 
        ''' Could be Nothing for Interfaces or Object.
        ''' </summary>
        Friend NotOverridable Overrides ReadOnly Property BaseTypeNoUseSiteDiagnostics As NamedTypeSymbol
            Get
                If Me._lazyBaseType Is ErrorTypeSymbol.UnknownResultType Then
                    ' force resolution of bases in containing type
                    ' to make base resolution errors more deterministic
                    If ContainingType IsNot Nothing Then
                        Dim tmp = ContainingType.BaseTypeNoUseSiteDiagnostics
                    End If

                    Dim diagnostics = BindingDiagnosticBag.GetInstance
                    Dim acyclicBase = Me.MakeAcyclicBaseType(diagnostics)

                    AtomicStoreReferenceAndDiagnostics(Me._lazyBaseType, acyclicBase, diagnostics, ErrorTypeSymbol.UnknownResultType)
                    diagnostics.Free()
                End If

                Return Me._lazyBaseType
            End Get
        End Property

        ''' <summary>
        ''' Interfaces that are implemented or inherited (if current type is interface itself).
        ''' </summary>
        Friend NotOverridable Overrides ReadOnly Property InterfacesNoUseSiteDiagnostics As ImmutableArray(Of NamedTypeSymbol)
            Get
                If Me._lazyInterfaces.IsDefault Then
                    Dim diagnostics = BindingDiagnosticBag.GetInstance
                    Dim acyclicInterfaces As ImmutableArray(Of NamedTypeSymbol) = Me.MakeAcyclicInterfaces(diagnostics)

                    AtomicStoreArrayAndDiagnostics(Me._lazyInterfaces, acyclicInterfaces, diagnostics)
                    diagnostics.Free()
                End If

                Return Me._lazyInterfaces
            End Get
        End Property

        ''' <summary>
        ''' Returns declared base type or actual base type if already known
        ''' This is only used by cycle detection code so that it can observe when cycles are broken 
        ''' while not forcing actual Base to be realized.
        ''' </summary>
        Friend Function GetBestKnownBaseType() As NamedTypeSymbol

            'NOTE: we can be at race with another thread here.
            ' the worst thing that can happen though, is that error on same cycle may be reported twice
            ' if two threads analyze the same cycle at the same time but start from different ends.
            '
            ' For now we decided that this is something we can live with.

            Dim base = Me._lazyBaseType
            If base IsNot ErrorTypeSymbol.UnknownResultType Then
                Return base
            End If

            Return GetDeclaredBase(Nothing)
        End Function

        ''' <summary>
        ''' Returns declared interfaces or actual Interfaces if already known
        ''' This is only used by cycle detection code so that it can observe when cycles are broken 
        ''' while not forcing actual Interfaces to be realized.
        ''' </summary>
        Friend Function GetBestKnownInterfacesNoUseSiteDiagnostics() As ImmutableArray(Of NamedTypeSymbol)
            Dim interfaces = Me._lazyInterfaces
            If Not interfaces.IsDefault Then
                Return interfaces
            End If

            Return GetDeclaredInterfacesNoUseSiteDiagnostics(Nothing)
        End Function

        ''' <summary>
        ''' True if and only if this type or some containing type has type parameters.
        ''' </summary>
        Public ReadOnly Property IsGenericType As Boolean Implements INamedTypeSymbol.IsGenericType
            Get
                Dim p As NamedTypeSymbol = Me
                Do While p IsNot Nothing
                    If (p.Arity <> 0) Then
                        Return True
                    End If
                    p = p.ContainingType
                Loop
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Get the original definition of this symbol. If this symbol is derived from another
        ''' symbol by (say) type substitution, this gets the original symbol, as it was defined
        ''' in source or metadata.
        ''' </summary>
        Public Overridable Shadows ReadOnly Property OriginalDefinition As NamedTypeSymbol
            Get
                ' Default implements returns Me.
                Return Me
            End Get
        End Property

        Protected NotOverridable Overrides ReadOnly Property OriginalTypeSymbolDefinition As TypeSymbol
            Get
                Return Me.OriginalDefinition
            End Get
        End Property

        ''' <summary>
        ''' Should return full emitted namespace name for a top level type if the name 
        ''' might be different in case from containing namespace symbol full name, Nothing otherwise.
        ''' </summary>
        Friend Overridable Function GetEmittedNamespaceName() As String
            Return Nothing
        End Function

        ''' <summary>
        ''' Does this type implement all the members of the given interface. Does not include members
        ''' of interfaces that iface inherits, only direct members.
        ''' </summary>
        Friend Function ImplementsAllMembersOfInterface(iface As NamedTypeSymbol) As Boolean
            Dim implementationMap = ExplicitInterfaceImplementationMap

            For Each ifaceMember In iface.GetMembersUnordered()
                If ifaceMember.RequiresImplementation() AndAlso Not implementationMap.ContainsKey(ifaceMember) Then
                    Return False
                End If
            Next

            Return True
        End Function

        Friend Overrides Function GetUseSiteInfo() As UseSiteInfo(Of AssemblySymbol)
            If Me.IsDefinition Then
                Return New UseSiteInfo(Of AssemblySymbol)(PrimaryDependency)
            End If

            ' Doing check for constructed types here in order to share implementation across
            ' constructed non-error and error type symbols.

            ' Check definition.
            Dim definitionUseSiteInfo As UseSiteInfo(Of AssemblySymbol) = DeriveUseSiteInfoFromType(Me.OriginalDefinition)

            If definitionUseSiteInfo.DiagnosticInfo IsNot Nothing AndAlso IsHighestPriorityUseSiteError(definitionUseSiteInfo.DiagnosticInfo.Code) Then
                Return definitionUseSiteInfo
            End If

            ' Check type arguments.
            Dim argsUseSiteInfo As UseSiteInfo(Of AssemblySymbol) = DeriveUseSiteInfoFromTypeArguments()

            MergeUseSiteInfo(definitionUseSiteInfo, argsUseSiteInfo)
            Return definitionUseSiteInfo
        End Function

        Private Function DeriveUseSiteInfoFromTypeArguments() As UseSiteInfo(Of AssemblySymbol)
            Dim argsUseSiteInfo As UseSiteInfo(Of AssemblySymbol) = Nothing
            Dim currentType As NamedTypeSymbol = Me

            Do
                For Each arg As TypeSymbol In currentType.TypeArgumentsNoUseSiteDiagnostics
                    If MergeUseSiteInfo(argsUseSiteInfo, DeriveUseSiteInfoFromType(arg)) Then
                        Return argsUseSiteInfo
                    End If
                Next

                If currentType.HasTypeArgumentsCustomModifiers Then
                    For i As Integer = 0 To Me.Arity - 1
                        If MergeUseSiteInfo(argsUseSiteInfo, DeriveUseSiteInfoFromCustomModifiers(Me.GetTypeArgumentCustomModifiers(i))) Then
                            Return argsUseSiteInfo
                        End If
                    Next
                End If

                currentType = currentType.ContainingType
            Loop While currentType IsNot Nothing AndAlso Not currentType.IsDefinition

            Return argsUseSiteInfo
        End Function

        ''' <summary>
        ''' True if this is a reference to an <em>unbound</em> generic type.  These occur only
        ''' within a <c>GetType</c> expression.  A generic type is considered <em>unbound</em>
        ''' if all of the type argument lists in its fully qualified name are empty.
        ''' Note that the type arguments of an unbound generic type will be returned as error
        ''' types because they do not really have type arguments.  An unbound generic type
        ''' yields null for its BaseType and an empty result for its Interfaces.
        ''' </summary>
        Public Overridable ReadOnly Property IsUnboundGenericType As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Force all declaration errors to be generated.
        ''' </summary>
        Friend MustOverride Overrides Sub GenerateDeclarationErrors(cancellationToken As CancellationToken)

        ''' <summary>
        ''' Return compiler generated nested types that are created at Declare phase, but not exposed through GetMembers and the like APIs.
        ''' Should return Nothing if there are no such types.
        ''' </summary>
        Friend Overridable Function GetSynthesizedNestedTypes() As IEnumerable(Of Microsoft.Cci.INestedTypeDefinition)
            Return Nothing
        End Function

        ''' <summary>
        ''' True if the type is a Windows runtime type.
        ''' </summary>
        ''' <remarks>
        ''' A type can me marked as a Windows runtime type in source by applying the WindowsRuntimeImportAttribute.
        ''' WindowsRuntimeImportAttribute is a pseudo custom attribute defined as an internal class in System.Runtime.InteropServices.WindowsRuntime namespace.
        ''' This is needed to mark Windows runtime types which are redefined in mscorlib.dll and System.Runtime.WindowsRuntime.dll.
        ''' These two assemblies are special as they implement the CLR's support for WinRT.
        ''' </remarks>
        Friend MustOverride ReadOnly Property IsWindowsRuntimeImport As Boolean

        '''  <summary>
        ''' True if the type should have its WinRT interfaces projected onto .NET types and
        ''' have missing .NET interface members added to the type.
        ''' </summary>
        Friend MustOverride ReadOnly Property ShouldAddWinRTMembers As Boolean

        ''' <summary>
        ''' Requires less computation than <see cref="TypeSymbol.TypeKind"/>== <see cref="TypeKind.Interface"/>.
        ''' </summary>
        ''' <remarks>
        ''' Metadata types need to compute their base types in order to know their TypeKinds, And that can lead
        ''' to cycles if base types are already being computed.
        ''' </remarks>
        ''' <returns>True if this Is an interface type.</returns>
        Friend MustOverride ReadOnly Property IsInterface As Boolean

        ''' <summary>
        ''' Get synthesized WithEvents overrides that aren't returned by <see cref="GetMembers"/>
        ''' </summary>
        Friend MustOverride Function GetSynthesizedWithEventsOverrides() As IEnumerable(Of PropertySymbol)

        ''' <summary>
        ''' Gets all of the required members from this type and all base types. This will be a set of the most derived overrides. If <see cref="HasRequiredMembersError"/> is true,
        ''' this will be <see cref="ImmutableSegmentedDictionary(Of String, Symbol).Empty"/>.
        ''' </summary>
        Friend ReadOnly Property AllRequiredMembers As ImmutableSegmentedDictionary(Of String, Symbol)
            Get
                EnsureRequiredMembersCalculated()
                Debug.Assert(Not _lazyRequiredMembers.IsDefault)
                Return If(_lazyRequiredMembers = s_requiredMembersErrorSentinel, ImmutableSegmentedDictionary(Of String, Symbol).Empty, _lazyRequiredMembers)
            End Get
        End Property

        ''' <summary>
        ''' True if this or any base type has a required members error, and constructors should be blocked unless attributed with SetsRequiredMembersAttribute. When this is
        ''' true, <see cref="AllRequiredMembers"/> will be <see cref="ImmutableSegmentedDictionary(Of String, Symbol).Empty"/>
        ''' </summary>
        Friend ReadOnly Property HasRequiredMembersError As Boolean
            Get
                EnsureRequiredMembersCalculated()
                Debug.Assert(Not _lazyRequiredMembers.IsDefault)
                Return _lazyRequiredMembers = s_requiredMembersErrorSentinel
            End Get
        End Property

        Friend MustOverride ReadOnly Property HasAnyDeclaredRequiredMembers As Boolean

        Private Sub EnsureRequiredMembersCalculated()
            Dim lazyRequiredMembers As ImmutableSegmentedDictionary(Of String, Symbol) = _lazyRequiredMembers

            If Not lazyRequiredMembers.IsDefault Then
                Return
            End If

            Dim requiredMembersBuilder As ImmutableSegmentedDictionary(Of String, Symbol).Builder = Nothing
            Dim success = TryCalculateRequiredMembers(requiredMembersBuilder)

            Dim requiredMembers = If(success,
                If(requiredMembersBuilder?.ToImmutable(), If(BaseTypeNoUseSiteDiagnostics?.AllRequiredMembers, ImmutableSegmentedDictionary(Of String, Symbol).Empty)),
                s_requiredMembersErrorSentinel)

            RoslynImmutableInterlocked.InterlockedInitialize(_lazyRequiredMembers, requiredMembers)
        End Sub

        Private Function TryCalculateRequiredMembers(<Out> ByRef requiredMembersBuilder As ImmutableSegmentedDictionary(Of String, Symbol).Builder) As Boolean
            If BaseTypeNoUseSiteDiagnostics?.HasRequiredMembersError = True Then
                Return False
            End If

            Dim baseAllRequiredMembers = If(BaseTypeNoUseSiteDiagnostics?.AllRequiredMembers, ImmutableSegmentedDictionary(Of String, Symbol).Empty)
            Dim typeHasDeclaredRequiredMembers = HasAnyDeclaredRequiredMembers

            If (Not typeHasDeclaredRequiredMembers) AndAlso baseAllRequiredMembers.IsEmpty Then
                Return True
            End If

            For Each member In GetMembersUnordered()
                ' Indexed properties cannot be required.
                Dim [property] = TryCast(member, PropertySymbol)
                If [property] IsNot Nothing AndAlso [property].ParameterCount > 0 Then
                    If [property].IsRequired Then
                        Return False
                    Else
                        Continue For
                    End If
                End If

                Dim existingMember As Symbol = Nothing
                ' Need to make sure that members from a base type weren't hidden by members from the current type. That is an error scenario
                If baseAllRequiredMembers.TryGetValue(member.Name, existingMember) Then
                    ' This is only permitted if the member is an override of a required member from a base type, and is required itself
                    Dim overriddenMember = TryCast(member, PropertySymbol)?.OverriddenProperty

                    If (Not member.IsRequired()) OrElse
                       overriddenMember Is Nothing OrElse
                       (Not overriddenMember.Equals(existingMember, TypeCompareKind.IgnoreTupleNames)) Then
                        Return False
                    End If
                End If

                If Not member.IsRequired() Then
                    Continue For
                End If

                If Not typeHasDeclaredRequiredMembers Then
                    ' Bad metadata. Type claimed it didn't declare any required members, but we found one.
                    Return False
                End If

                If requiredMembersBuilder Is Nothing Then
                    requiredMembersBuilder = baseAllRequiredMembers.ToBuilder()
                End If

                requiredMembersBuilder(member.Name) = member
            Next

            Return True
        End Function

#Region "INamedTypeSymbol"

        Private ReadOnly Property INamedTypeSymbol_Arity As Integer Implements INamedTypeSymbol.Arity
            Get
                Return Me.Arity
            End Get
        End Property

        Private ReadOnly Property INamedTypeSymbol_ConstructedFrom As INamedTypeSymbol Implements INamedTypeSymbol.ConstructedFrom
            Get
                Return Me.ConstructedFrom
            End Get
        End Property

        Private ReadOnly Property INamedTypeSymbol_DelegateInvokeMethod As IMethodSymbol Implements INamedTypeSymbol.DelegateInvokeMethod
            Get
                Return Me.DelegateInvokeMethod
            End Get
        End Property

        Private ReadOnly Property INamedTypeSymbol_EnumUnderlyingType As INamedTypeSymbol Implements INamedTypeSymbol.EnumUnderlyingType
            Get
                Return Me.EnumUnderlyingType
            End Get
        End Property

        Private ReadOnly Property INamedTypeSymbolInternal_EnumUnderlyingType As INamedTypeSymbolInternal Implements INamedTypeSymbolInternal.EnumUnderlyingType
            Get
                Return Me.EnumUnderlyingType
            End Get
        End Property

        Private ReadOnly Property INamedTypeSymbol_MemberNames As IEnumerable(Of String) Implements INamedTypeSymbol.MemberNames
            Get
                Return Me.MemberNames
            End Get
        End Property

        Private ReadOnly Property INamedTypeSymbol_IsUnboundGenericType As Boolean Implements INamedTypeSymbol.IsUnboundGenericType
            Get
                Return Me.IsUnboundGenericType
            End Get
        End Property

        Private ReadOnly Property INamedTypeSymbol_OriginalDefinition As INamedTypeSymbol Implements INamedTypeSymbol.OriginalDefinition
            Get
                Return Me.OriginalDefinition
            End Get
        End Property

        Private Function INamedTypeSymbol_GetTypeArgumentCustomModifiers(ordinal As Integer) As ImmutableArray(Of CustomModifier) Implements INamedTypeSymbol.GetTypeArgumentCustomModifiers
            Return GetTypeArgumentCustomModifiers(ordinal)
        End Function

        Private ReadOnly Property INamedTypeSymbol_TypeArguments As ImmutableArray(Of ITypeSymbol) Implements INamedTypeSymbol.TypeArguments
            Get
                Return StaticCast(Of ITypeSymbol).From(Me.TypeArgumentsNoUseSiteDiagnostics)
            End Get
        End Property

        Private ReadOnly Property TypeArgumentNullableAnnotations As ImmutableArray(Of NullableAnnotation) Implements INamedTypeSymbol.TypeArgumentNullableAnnotations
            Get
                Return Me.TypeArgumentsNoUseSiteDiagnostics.SelectAsArray(Function(t) NullableAnnotation.None)
            End Get
        End Property

        Private ReadOnly Property INamedTypeSymbol_TypeParameters As ImmutableArray(Of ITypeParameterSymbol) Implements INamedTypeSymbol.TypeParameters
            Get
                Return StaticCast(Of ITypeParameterSymbol).From(Me.TypeParameters)
            End Get
        End Property

        Private ReadOnly Property INamedTypeSymbol_IsScriptClass As Boolean Implements INamedTypeSymbol.IsScriptClass
            Get
                Return Me.IsScriptClass
            End Get
        End Property

        Private ReadOnly Property INamedTypeSymbol_IsImplicitClass As Boolean Implements INamedTypeSymbol.IsImplicitClass
            Get
                Return Me.IsImplicitClass
            End Get
        End Property

        Private Function INamedTypeSymbol_Construct(ParamArray typeArguments() As ITypeSymbol) As INamedTypeSymbol Implements INamedTypeSymbol.Construct
            Return Construct(ConstructTypeArguments(typeArguments))
        End Function

        Private Function INamedTypeSymbol_Construct(typeArguments As ImmutableArray(Of ITypeSymbol), typeArgumentNullableAnnotations As ImmutableArray(Of CodeAnalysis.NullableAnnotation)) As INamedTypeSymbol Implements INamedTypeSymbol.Construct
            Return Construct(ConstructTypeArguments(typeArguments, typeArgumentNullableAnnotations))
        End Function

        Private Function INamedTypeSymbol_ConstructUnboundGenericType() As INamedTypeSymbol Implements INamedTypeSymbol.ConstructUnboundGenericType
            Return ConstructUnboundGenericType()
        End Function

        Private ReadOnly Property INamedTypeSymbol_InstanceConstructors As ImmutableArray(Of IMethodSymbol) Implements INamedTypeSymbol.InstanceConstructors
            Get
                Return GetConstructors(Of IMethodSymbol)(includeInstance:=True, includeShared:=False)
            End Get
        End Property

        Private ReadOnly Property INamedTypeSymbol_StaticConstructors As ImmutableArray(Of IMethodSymbol) Implements INamedTypeSymbol.StaticConstructors
            Get
                Return GetConstructors(Of IMethodSymbol)(includeInstance:=False, includeShared:=True)
            End Get
        End Property

        Private ReadOnly Property INamedTypeSymbol_Constructors As ImmutableArray(Of IMethodSymbol) Implements INamedTypeSymbol.Constructors
            Get
                Return GetConstructors(Of IMethodSymbol)(includeInstance:=True, includeShared:=True)
            End Get
        End Property

        Private ReadOnly Property INamedTypeSymbol_AssociatedSymbol As ISymbol Implements INamedTypeSymbol.AssociatedSymbol
            Get
                Return Me.AssociatedSymbol
            End Get
        End Property

        Private ReadOnly Property INamedTypeSymbol_IsComImport As Boolean Implements INamedTypeSymbol.IsComImport
            Get
                Return IsComImport
            End Get
        End Property

        Private ReadOnly Property INamedTypeSymbol_IsFileLocal As Boolean Implements INamedTypeSymbol.IsFileLocal
            Get
                Return False
            End Get
        End Property

        Private ReadOnly Property INamedTypeSymbol_NativeIntegerUnderlyingType As INamedTypeSymbol Implements INamedTypeSymbol.NativeIntegerUnderlyingType
            Get
                Return Nothing
            End Get
        End Property

#End Region

#Region "ISymbol"

        Protected Overrides ReadOnly Property ISymbol_IsAbstract As Boolean
            Get
                Return Me.IsMustInherit
            End Get
        End Property

        Protected Overrides ReadOnly Property ISymbol_IsSealed As Boolean
            Get
                Return Me.IsNotInheritable
            End Get
        End Property

        Private ReadOnly Property INamedTypeSymbol_TupleElements As ImmutableArray(Of IFieldSymbol) Implements INamedTypeSymbol.TupleElements
            Get
                Return StaticCast(Of IFieldSymbol).From(TupleElements)
            End Get
        End Property

        Private ReadOnly Property INamedTypeSymbol_TupleUnderlyingType As INamedTypeSymbol Implements INamedTypeSymbol.TupleUnderlyingType
            Get
                Return TupleUnderlyingType
            End Get
        End Property

        Public Overrides Sub Accept(visitor As SymbolVisitor)
            visitor.VisitNamedType(Me)
        End Sub

        Public Overrides Function Accept(Of TResult)(visitor As SymbolVisitor(Of TResult)) As TResult
            Return visitor.VisitNamedType(Me)
        End Function

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As SymbolVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNamedType(Me, argument)
        End Function

        Public Overrides Sub Accept(visitor As VisualBasicSymbolVisitor)
            visitor.VisitNamedType(Me)
        End Sub

        Public Overrides Function Accept(Of TResult)(visitor As VisualBasicSymbolVisitor(Of TResult)) As TResult
            Return visitor.VisitNamedType(Me)
        End Function

#End Region

        ''' <summary>
        ''' Verify if the given type can be used to back a tuple type 
        ''' and return cardinality of that tuple type in <paramref name="tupleCardinality"/>. 
        ''' </summary>
        ''' <param name="tupleCardinality">If method returns true, contains cardinality of the compatible tuple type.</param>
        ''' <returns></returns>
        Public NotOverridable Overrides Function IsTupleCompatible(<Out> ByRef tupleCardinality As Integer) As Boolean
            If IsTupleType Then
                tupleCardinality = 0
                Return False
            End If

            ' Should this be optimized for perf (caching for VT<0> to VT<7>, etc.)?
            If Not IsUnboundGenericType AndAlso
                If(ContainingSymbol?.Kind = SymbolKind.Namespace, False) AndAlso
                If(ContainingNamespace.ContainingNamespace?.IsGlobalNamespace, False) AndAlso
                Name = TupleTypeSymbol.TupleTypeName AndAlso
                ContainingNamespace.Name = MetadataHelpers.SystemString Then

                Dim arity = Me.Arity

                If arity > 0 AndAlso arity < TupleTypeSymbol.RestPosition Then
                    tupleCardinality = arity
                    Return True
                ElseIf arity = TupleTypeSymbol.RestPosition AndAlso Not IsDefinition Then
                    ' Skip through "Rest" extensions
                    Dim typeToCheck As TypeSymbol = Me
                    Dim levelsOfNesting As Integer = 0

                    Do
                        levelsOfNesting += 1
                        typeToCheck = DirectCast(typeToCheck, NamedTypeSymbol).TypeArgumentsNoUseSiteDiagnostics(TupleTypeSymbol.RestPosition - 1)
                    Loop While TypeSymbol.Equals(typeToCheck.OriginalDefinition, Me.OriginalDefinition, TypeCompareKind.ConsiderEverything) AndAlso Not typeToCheck.IsDefinition

                    If typeToCheck.IsTupleType Then
                        Dim underlying = typeToCheck.TupleUnderlyingType
                        If underlying.Arity = TupleTypeSymbol.RestPosition AndAlso Not TypeSymbol.Equals(underlying.OriginalDefinition, Me.OriginalDefinition, TypeCompareKind.ConsiderEverything) Then
                            tupleCardinality = 0
                            Return False
                        End If

                        tupleCardinality = (TupleTypeSymbol.RestPosition - 1) * levelsOfNesting + typeToCheck.TupleElementTypes.Length
                        Return True
                    End If

                    arity = If(TryCast(typeToCheck, NamedTypeSymbol)?.Arity, 0)

                    If arity > 0 AndAlso
                        arity < TupleTypeSymbol.RestPosition AndAlso
                        typeToCheck.IsTupleCompatible(tupleCardinality) Then
                        Debug.Assert(tupleCardinality < TupleTypeSymbol.RestPosition)
                        tupleCardinality += (TupleTypeSymbol.RestPosition - 1) * levelsOfNesting
                        Return True
                    End If
                End If
            End If

            tupleCardinality = 0
            Return False
        End Function

    End Class
End Namespace
