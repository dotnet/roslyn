' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend MustInherit Class MethodSymbol
        Inherits Symbol
        Implements IMethodSymbolInternal

        ''' <summary>
        ''' Gets what kind of method this is. There are several different kinds of things in the
        ''' VB language that are represented as methods. This property allow distinguishing those things
        ''' without having to decode the name of the method.
        ''' </summary>
        Public MustOverride ReadOnly Property MethodKind As MethodKind

        ''' <summary>
        ''' True, if the method kind was determined by examining a syntax node (i.e. for source methods -
        ''' including substituted and retargeted ones); false, otherwise.
        ''' </summary>
        Friend MustOverride ReadOnly Property IsMethodKindBasedOnSyntax As Boolean

        Friend Overridable Function IsParameterlessConstructor() As Boolean
            Return Me.ParameterCount = 0 AndAlso Me.MethodKind = MethodKind.Constructor
        End Function

        ''' <summary>
        ''' Returns whether this method is using VARARG calling convention.
        ''' </summary>
        Public MustOverride ReadOnly Property IsVararg As Boolean Implements IMethodSymbol.IsVararg

        ''' <summary>
        ''' Returns whether this built-in operator checks for integer overflow.
        ''' </summary>
        Public Overridable ReadOnly Property IsCheckedBuiltin As Boolean Implements IMethodSymbol.IsCheckedBuiltin
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Returns whether this method is generic; i.e., does it have any type parameters?
        ''' </summary>
        Public Overridable ReadOnly Property IsGenericMethod As Boolean
            Get
                Return Arity <> 0
            End Get
        End Property

        ''' <summary>
        ''' Returns the arity of this method, or the number of type parameters it takes.
        ''' A non-generic method has zero arity.
        ''' </summary>
        Public MustOverride ReadOnly Property Arity As Integer

        ''' <summary>
        ''' Get the type parameters on this method. If the method has not generic,
        ''' returns an empty list.
        ''' </summary>
        Public MustOverride ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)

        ''' <summary>
        ''' Returns the type arguments that have been substituted for the type parameters. 
        ''' If nothing has been substituted for a give type parameters,
        ''' then the type parameter itself is consider the type argument.
        ''' </summary>
        Public MustOverride ReadOnly Property TypeArguments As ImmutableArray(Of TypeSymbol)

        ''' <summary>
        ''' Get the original definition of this symbol. If this symbol is derived from another
        ''' symbol by (say) type substitution, this gets the original symbol, as it was defined
        ''' in source or metadata.
        ''' </summary>
        Public Overridable Shadows ReadOnly Property OriginalDefinition As MethodSymbol
            Get
                ' Default implements returns Me.
                Return Me
            End Get
        End Property

        Protected NotOverridable Overrides ReadOnly Property OriginalSymbolDefinition As Symbol
            Get
                Return Me.OriginalDefinition
            End Get
        End Property

        ''' <summary>
        ''' Returns the method symbol that this method was constructed from. This method symbol
        ''' has the same containing type (if any), but has type arguments that are the same
        ''' as the type parameters (although its containing type might not).
        ''' </summary>
        Public Overridable ReadOnly Property ConstructedFrom As MethodSymbol
            Get
                Return Me
            End Get
        End Property

        ''' <summary>
        ''' Returns true if this method has no return type; i.e., is a Sub instead of a Function.
        ''' </summary>
        Public MustOverride ReadOnly Property IsSub As Boolean

        ''' <summary>
        ''' Source: Returns whether this method is async; i.e., does it have the Async modifier?
        ''' Metadata: Returns False; methods from metadata cannot be async.
        ''' </summary>
        Public MustOverride ReadOnly Property IsAsync As Boolean

        ''' <summary>
        ''' Source: Returns whether this method is an iterator; i.e., does it have the Iterator modifier?
        ''' Metadata: Returns False; methods from metadata cannot be an iterator.
        ''' </summary>
        Public MustOverride ReadOnly Property IsIterator As Boolean

        ''' <summary>
        ''' Source: Returns False; methods from source cannot return by reference.
        ''' Metadata: Returns whether or not this method returns by reference.
        ''' </summary>
        Public MustOverride ReadOnly Property ReturnsByRef As Boolean

        ''' <summary>
        ''' Gets the return type of the method. If the method is a Sub, returns
        ''' the same type symbol as is returned by Compilation.VoidType.
        ''' </summary>
        Public MustOverride ReadOnly Property ReturnType As TypeSymbol

        ''' <summary>
        ''' Returns the list of custom modifiers, if any, associated with the returned value. 
        ''' </summary>
        Public MustOverride ReadOnly Property ReturnTypeCustomModifiers As ImmutableArray(Of CustomModifier)

        ''' <summary>
        ''' Returns the list of attributes, if any, associated with the return type.
        ''' </summary>
        Public Overridable Function GetReturnTypeAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return ImmutableArray(Of VisualBasicAttributeData).Empty
        End Function

        ''' <summary>
        ''' Optimization: in many cases, the parameter count (fast) is sufficient and we
        ''' don't need the actual parameter symbols (slow).
        ''' </summary>
        ''' <remarks>
        ''' The default implementation is always correct, but may be unnecessarily slow.
        ''' </remarks>
        Friend Overridable ReadOnly Property ParameterCount As Integer
            Get
                Return Me.Parameters.Length
            End Get
        End Property

        ''' <summary>
        ''' Gets the parameters of this method. If this method has no parameters, returns
        ''' an empty list.
        ''' </summary>
        Public MustOverride ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)

        ''' <summary>
        ''' Should return syntax node that originated the method. 
        ''' </summary>
        Friend MustOverride ReadOnly Property Syntax As VisualBasicSyntaxNode

        ''' <summary>
        ''' Returns true if calls to this method are omitted in the given syntax tree at the given syntax node location.
        ''' Calls are omitted when the called method is a partial method with no implementation part, or when the
        ''' called method is a conditional method whose condition is not true at the given syntax node location in the source file
        ''' corresponding to the given syntax tree.
        ''' </summary>
        Friend Overridable Function CallsAreOmitted(atNode As SyntaxNodeOrToken, syntaxTree As SyntaxTree) As Boolean
            Return Me.IsPartialWithoutImplementation OrElse
                (syntaxTree IsNot Nothing AndAlso Me.CallsAreConditionallyOmitted(atNode, syntaxTree))
        End Function

        ''' <summary>
        ''' Calls are conditionally omitted if all the following requirements are true:
        '''  (a) Me.IsSub == True.
        '''  (b) Containing type is not an interface type.
        '''  (c) Me.IsConditional == True, i.e. it has at least one applied conditional attribute.
        '''  (d) This method is not the Property Set method.
        '''  (e) None of conditional symbols corresponding to these conditional attributes are true at the given syntax node location.
        ''' </summary>
        ''' <remarks>
        ''' Forces binding and decoding of attributes.
        ''' </remarks>
        Private Function CallsAreConditionallyOmitted(atNode As SyntaxNodeOrToken, syntaxTree As SyntaxTree) As Boolean
            ' UNDONE: Ignore conditional attributes if within EE.
            ' For the EE, we always want to eval functions with the Conditional attribute applied to them (there are no CC symbols to check)

            Dim containingType As NamedTypeSymbol = Me.ContainingType
            If Me.IsConditional AndAlso Me.IsSub AndAlso
                Me.MethodKind <> MethodKind.PropertySet AndAlso
                (containingType Is Nothing OrElse Not containingType.IsInterfaceType) Then

                Dim conditionalSymbols As IEnumerable(Of String) = Me.GetAppliedConditionalSymbols()
                Debug.Assert(conditionalSymbols IsNot Nothing)
                Debug.Assert(conditionalSymbols.Any())

                If syntaxTree.IsAnyPreprocessorSymbolDefined(conditionalSymbols, atNode) Then
                    Return False
                End If

                ' NOTE: Conditional symbols on the overridden method must be inherited by the overriding method, but the native VB compiler doesn't do so. We will maintain compatibility.
                Return True
            Else
                Return False
            End If
        End Function

        ''' <summary>
        ''' Returns a sequence of preprocessor symbols specified in <see cref="ConditionalAttribute"/> applied on this symbol, or null if there are none.
        ''' </summary>
        Friend MustOverride Function GetAppliedConditionalSymbols() As ImmutableArray(Of String)

        ''' <summary>
        ''' Returns a flag indicating whether this symbol has at least one applied conditional attribute.
        ''' </summary>
        ''' <remarks>
        ''' Forces binding and decoding of attributes.
        ''' NOTE: Conditional symbols on the overridden method must be inherited by the overriding method, but the native VB compiler doesn't do so. We maintain compatibility.
        ''' </remarks>
        Friend ReadOnly Property IsConditional As Boolean
            Get
                Return Me.GetAppliedConditionalSymbols.Any()
            End Get
        End Property

        ''' <summary>
        ''' True if this symbol has a special name (metadata flag SpecialName is set).
        ''' </summary>
        ''' <remarks>
        ''' This is set for methods with special semantics such as constructors or accessors
        ''' as well as in special synthetic methods such as lambdas.
        ''' Also set for methods marked with System.Runtime.CompilerServices.SpecialNameAttribute.
        ''' </remarks>
        Friend MustOverride ReadOnly Property HasSpecialName As Boolean

        ''' <summary>
        ''' If this method has MethodKind of MethodKind.PropertyGet or MethodKind.PropertySet,
        ''' returns the property that this method is the getter or setter for.
        ''' If this method has MethodKind of MethodKind.EventAdd or MethodKind.EventRemove,
        ''' returns the event that this method is the adder or remover for.
        ''' Note, the set of possible associated symbols might be expanded in the future to 
        ''' reflect changes in the languages.
        ''' </summary>
        Public MustOverride ReadOnly Property AssociatedSymbol As Symbol

        ''' <summary>
        ''' If this method is a Lambda method (MethodKind = MethodKind.LambdaMethod) and 
        ''' there is an anonymous delegate associated with it, returns this delegate.
        ''' 
        ''' Returns Nothing if the symbol is not a lambda or if it does not have an
        ''' anonymous delegate associated with it.
        ''' </summary>
        Public Overridable ReadOnly Property AssociatedAnonymousDelegate As NamedTypeSymbol
            Get
                Return Nothing
            End Get
        End Property

        ''' <summary>
        ''' If this method overrides another method (because it both had the Overrides modifier
        ''' and there correctly was a method to override), returns the overridden method.
        ''' </summary>
        Public Overridable ReadOnly Property OverriddenMethod As MethodSymbol
            Get
                If Me.IsAccessor AndAlso Me.AssociatedSymbol.Kind = SymbolKind.Property Then
                    ' Property accessors use the overridden property to determine overriding.
                    Return DirectCast(Me.AssociatedSymbol, PropertySymbol).GetAccessorOverride(getter:=(MethodKind = MethodKind.PropertyGet))
                Else
                    If Me.IsOverrides AndAlso Me.ConstructedFrom Is Me Then
                        If IsDefinition Then
                            Return OverriddenMembers.OverriddenMember
                        End If

                        Return OverriddenMembersResult(Of MethodSymbol).GetOverriddenMember(Me, Me.OriginalDefinition.OverriddenMethod)
                    End If
                End If

                Return Nothing
            End Get
        End Property

        ' Get the set of overridden and hidden members for this method.
        Friend Overridable ReadOnly Property OverriddenMembers As OverriddenMembersResult(Of MethodSymbol)
            Get
                ' To save space, the default implementation does not cache its result.  We expect there to
                ' be a very large number of MethodSymbols and we expect that a large percentage of them will
                ' obviously not override anything (e.g. static methods, constructors, destructors, etc).
                Return OverrideHidingHelper(Of MethodSymbol).MakeOverriddenMembers(Me)
            End Get
        End Property

        ' Get the set of handled events for this method.
        Public Overridable ReadOnly Property HandledEvents As ImmutableArray(Of HandledEvent)
            Get
                Return ImmutableArray(Of HandledEvent).Empty
            End Get
        End Property


        ''' <summary>
        ''' Returns interface methods explicitly implemented by this method.
        ''' </summary>
        Public MustOverride ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of MethodSymbol)

#Disable Warning RS0010
        ''' <summary>
        ''' Returns true if this method is not implemented in IL of the assembly it is defined in.
        ''' </summary>
        ''' <remarks>
        ''' External methods are 
        ''' 1) Declare Subs and Declare Functions, 
        ''' 2) methods marked by <see cref="System.Runtime.InteropServices.DllImportAttribute"/>, 
        ''' 3) methods marked by <see cref="System.Runtime.CompilerServices.MethodImplAttribute"/> 
        '''    with <see cref="T:System.Runtime.CompilerServices.MethodImplOptions.InternalCall"/> or 
        '''    <see cref="T:System.Runtime.CompilerServices.MethodCodeType.Runtime"/> flags.
        ''' 4) Synthesized constructors of ComImport types
        ''' </remarks>
#Enable Warning RS0010
        Public MustOverride ReadOnly Property IsExternalMethod As Boolean

        ''' <summary>
        ''' Returns platform invocation information for this method if it is a PlatformInvoke method, otherwise returns Nothing.
        ''' </summary>
        Public MustOverride Function GetDllImportData() As DllImportData Implements IMethodSymbol.GetDllImportData

        ''' <summary>
        ''' Marshalling information for return value (FieldMarshal in metadata). 
        ''' </summary>
        Friend MustOverride ReadOnly Property ReturnTypeMarshallingInformation As MarshalPseudoCustomAttributeData

        ''' <summary>
        ''' Misc implementation metadata flags (ImplFlags in metadata).
        ''' </summary>
        Friend MustOverride ReadOnly Property ImplementationAttributes As System.Reflection.MethodImplAttributes

        ''' <summary>
        ''' Declaration security information associated with this method, or null if there is none.
        ''' </summary>
        Friend MustOverride Function GetSecurityInformation() As IEnumerable(Of Microsoft.Cci.SecurityAttribute)

        ''' <summary>
        ''' True if the method has declarative security information (HasSecurity flags).
        ''' </summary>
        Friend MustOverride ReadOnly Property HasDeclarativeSecurity As Boolean

        ''' <summary>
        ''' Returns true if this method is an extension method from the VB language perspective; 
        ''' i.e., declared with an Extension attribute and meets other language requirements.
        ''' </summary>
        Public MustOverride ReadOnly Property IsExtensionMethod As Boolean

        ''' <summary>
        ''' Returns true if this method might be a reducible extension method. This method may return true
        ''' even if the method is not an extension method, but if it returns false, it must be the
        ''' case that this is not an extension method.
        ''' 
        ''' Allows checking extension methods from source in a quicker manner than fully binding attributes.
        ''' </summary>
        Friend Overridable ReadOnly Property MayBeReducibleExtensionMethod As Boolean
            Get
                Return IsExtensionMethod AndAlso MethodKind <> MethodKind.ReducedExtension
            End Get
        End Property

        ''' <summary>
        ''' Returns true if this method hides a base method by name and signature.
        ''' The equivalent of the "hidebysig" flag in metadata. 
        ''' </summary>
        ''' <remarks>
        ''' This property should not be confused with general method overloading in Visual Basic, and is not directly related. 
        ''' This property will only return true if this method hides a base method by name and signature (Overloads keyword).
        ''' </remarks>
        Public MustOverride ReadOnly Property IsOverloads As Boolean

        ''' <summary>
        ''' True if the implementation of this method is supplied by the runtime.
        ''' </summary>
        ''' <remarks>
        ''' <see cref="IsRuntimeImplemented"/> implies <see cref="IsExternalMethod"/>.
        ''' </remarks>
        Friend ReadOnly Property IsRuntimeImplemented As Boolean
            Get
                Return (Me.ImplementationAttributes And Reflection.MethodImplAttributes.Runtime) <> 0
            End Get
        End Property

        Friend Overrides ReadOnly Property ImplicitlyDefinedBy(Optional membersInProgress As Dictionary(Of String, ArrayBuilder(Of Symbol)) = Nothing) As Symbol
            Get
                Return Me.AssociatedSymbol
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property Kind As SymbolKind
            Get
                Return SymbolKind.Method
            End Get
        End Property

        Friend ReadOnly Property IsScriptConstructor As Boolean
            Get
                Return Me.MethodKind = MethodKind.Constructor AndAlso Me.ContainingType.IsScriptClass
            End Get
        End Property

        Friend Overridable ReadOnly Property IsScriptInitializer As Boolean
            Get
                Return False
            End Get
        End Property

        Friend ReadOnly Property IsSubmissionConstructor As Boolean
            Get
                Return IsScriptConstructor AndAlso ContainingAssembly.IsInteractive
            End Get
        End Property

        ''' <summary> 
        ''' Determines whether this method is a candidate for a default 
        ''' assembly entry point. Any method called "Main" is.
        ''' </summary> 
        ''' <returns>True if the method can be used as an entry point.</returns>
        Friend ReadOnly Property IsEntryPointCandidate As Boolean
            Get
                Return String.Equals(Name, WellKnownMemberNames.EntryPointMethodName, StringComparison.OrdinalIgnoreCase)
            End Get
        End Property

        Friend ReadOnly Property IsViableMainMethod As Boolean
            Get
                Return IsShared AndAlso
                       IsAccessibleEntryPoint() AndAlso
                       HasEntryPointSignature()
            End Get
        End Property

        ''' <summary>
        ''' Entry point is considered accessible if it is not private and none of the containing types is private (they all might be Family or Friend).
        ''' </summary>
        Private Function IsAccessibleEntryPoint() As Boolean
            If Me.DeclaredAccessibility = Accessibility.Private Then
                Return False
            End If

            Dim type = Me.ContainingType
            While type IsNot Nothing
                If type.DeclaredAccessibility = Accessibility.Private Then
                    Return False
                End If

                type = type.ContainingType
            End While

            Return True
        End Function

        ''' <summary> 
        ''' Checks if the method has an entry point compatible signature, i.e. 
        ''' - the return type is either void or int 
        ''' - has either no parameter or a single parameter of type string[] 
        ''' </summary>
        Friend Function HasEntryPointSignature() As Boolean
            Dim returnType As TypeSymbol = Me.ReturnType
            If returnType.SpecialType <> SpecialType.System_Int32 AndAlso returnType.SpecialType <> SpecialType.System_Void Then
                Return False
            End If

            If Parameters.Length = 0 Then
                Return True
            End If

            If Parameters.Length > 1 Then
                Return False
            End If

            If Parameters(0).IsByRef Then
                Return False
            End If

            Dim firstType = Parameters(0).Type
            If firstType.TypeKind <> TypeKind.Array Then
                Return False
            End If

            Dim array = DirectCast(firstType, ArrayTypeSymbol)
            Return array.IsSZArray AndAlso array.ElementType.SpecialType = SpecialType.System_String
        End Function

        Friend Overrides Function Accept(Of TArgument, TResult)(visitor As VisualBasicSymbolVisitor(Of TArgument, TResult), arg As TArgument) As TResult
            Return visitor.VisitMethod(Me, arg)
        End Function

        Friend Sub New()
        End Sub

        ' Returns True if this method has Arity >= 1 and Construct can be called. This is primarily useful
        ' when deal with error cases.
        Friend Overridable ReadOnly Property CanConstruct As Boolean
            Get
                Return Me.IsDefinition AndAlso Me.Arity > 0
            End Get
        End Property

        ''' <summary> Checks for validity of Construct(...) on this method with these type arguments. </summary>
        Protected Sub CheckCanConstructAndTypeArguments(typeArguments As ImmutableArray(Of TypeSymbol))
            'EDMAURER this exception is part of the public contract for Construct(...)
            If Not CanConstruct OrElse Me IsNot ConstructedFrom Then
                Throw New InvalidOperationException()
            End If

            ' Check type arguments
            typeArguments.CheckTypeArguments(Me.Arity)
        End Sub

        ' Apply type substitution to a generic method to create an method symbol with the given type parameters supplied.
        Public Overridable Function Construct(typeArguments As ImmutableArray(Of TypeSymbol)) As MethodSymbol
            CheckCanConstructAndTypeArguments(typeArguments)

            Debug.Assert(Me.IsDefinition)
            Dim substitution = TypeSubstitution.Create(Me, Me.TypeParameters, typeArguments, allowAlphaRenamedTypeParametersAsArguments:=True)

            If substitution Is Nothing Then
                ' identity substitution
                Return Me
            Else
                Debug.Assert(substitution.TargetGenericDefinition Is Me)
                Return New SubstitutedMethodSymbol.ConstructedNotSpecializedGenericMethod(substitution, typeArguments)
            End If
        End Function

        Public Function Construct(ParamArray typeArguments() As TypeSymbol) As MethodSymbol
            Return Construct(ImmutableArray.Create(typeArguments))
        End Function

        Friend MustOverride ReadOnly Property CallingConvention As Microsoft.Cci.CallingConvention

        ''' <summary>
        ''' Call <see cref="TryGetMeParameter"/> and throw if it returns false.
        ''' </summary>
        ''' <returns></returns>
        Friend ReadOnly Property MeParameter As ParameterSymbol
            Get
                Dim parameter As ParameterSymbol = Nothing
                If Not Me.TryGetMeParameter(parameter) Then
                    Throw ExceptionUtilities.Unreachable
                End If
                Return parameter
            End Get
        End Property

        ''' <returns>
        ''' True if this <see cref="MethodSymbol"/> type supports retrieving the Me parameter
        ''' and false otherwise.  Note that a return value of true does not guarantee a non-Nothing
        ''' <paramref name="meParameter"/> (e.g. fails for shared methods).
        ''' </returns>
        Friend Overridable Function TryGetMeParameter(<Out> ByRef meParameter As ParameterSymbol) As Boolean
            meParameter = Nothing
            Return False
        End Function

        Friend Overrides Function GetUseSiteErrorInfo() As DiagnosticInfo
            If Me.IsDefinition Then
                Return MyBase.GetUseSiteErrorInfo()
            End If

            ' There is no reason to specially check type arguments because
            ' constructed members are never imported.
            Return Me.OriginalDefinition.GetUseSiteErrorInfo()
        End Function

        Friend Function CalculateUseSiteErrorInfo() As DiagnosticInfo

            Debug.Assert(IsDefinition)

            ' Check returns by ref.
            If Me.ReturnsByRef Then
                Return ErrorFactory.ErrorInfo(ERRID.ERR_UnsupportedMethod1, CustomSymbolDisplayFormatter.ShortErrorName(Me))
            End If

            ' Check return type.
            Dim errorInfo As DiagnosticInfo = DeriveUseSiteErrorInfoFromType(Me.ReturnType)

            If errorInfo IsNot Nothing AndAlso errorInfo.Code = ERRID.ERR_UnsupportedMethod1 Then
                Return errorInfo
            End If

            ' Check return type custom modifiers.
            Dim paramsErrorInfo = DeriveUseSiteErrorInfoFromCustomModifiers(Me.ReturnTypeCustomModifiers)

            If paramsErrorInfo IsNot Nothing Then
                If paramsErrorInfo.Code = ERRID.ERR_UnsupportedMethod1 Then
                    Return paramsErrorInfo
                End If

                If errorInfo Is Nothing Then
                    errorInfo = paramsErrorInfo
                End If
            End If

            ' Check parameters.
            Dim result = MergeUseSiteErrorInfo(errorInfo, DeriveUseSiteErrorInfoFromParameters(Me.Parameters))

            ' If the member is in an assembly with unified references, 
            ' we check if its definition depends on a type from a unified reference.
            If result Is Nothing AndAlso Me.ContainingModule.HasUnifiedReferences Then
                Dim unificationCheckedTypes As HashSet(Of TypeSymbol) = Nothing
                result = If(Me.ReturnType.GetUnificationUseSiteDiagnosticRecursive(Me, unificationCheckedTypes),
                         If(GetUnificationUseSiteDiagnosticRecursive(Me.ReturnTypeCustomModifiers, Me, unificationCheckedTypes),
                         If(GetUnificationUseSiteDiagnosticRecursive(Me.Parameters, Me, unificationCheckedTypes),
                            GetUnificationUseSiteDiagnosticRecursive(Me.TypeParameters, Me, unificationCheckedTypes))))
            End If

            Return result
        End Function

        ''' <summary>
        ''' Return error code that has highest priority while calculating use site error for this symbol. 
        ''' </summary>
        Protected Overrides ReadOnly Property HighestPriorityUseSiteError As Integer
            Get
                Return ERRID.ERR_UnsupportedMethod1
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property HasUnsupportedMetadata As Boolean
            Get
                Dim info As DiagnosticInfo = GetUseSiteErrorInfo()
                Return info IsNot Nothing AndAlso info.Code = ERRID.ERR_UnsupportedMethod1
            End Get
        End Property

        ''' <summary>
        ''' If this method is a reduced extension method, gets the extension method definition that
        ''' this method was reduced from. Otherwise, returns Nothing.
        ''' </summary>
        Public Overridable ReadOnly Property ReducedFrom As MethodSymbol
            Get
                Return Nothing
            End Get
        End Property

        ''' <summary>
        ''' Is this a reduced extension method?
        ''' </summary>
        Friend ReadOnly Property IsReducedExtensionMethod As Boolean
            Get
                Return Me.ReducedFrom IsNot Nothing
            End Get
        End Property

        ''' <summary>
        ''' If this method is a reduced extension method, gets the extension method (possibly constructed) that
        ''' should be used at call site during ILGen. Otherwise, returns Nothing.
        ''' </summary>
        Friend Overridable ReadOnly Property CallsiteReducedFromMethod As MethodSymbol
            Get
                Return Nothing
            End Get
        End Property

        ''' <summary>
        ''' If this method can be applied to an object, returns the type of object it is applied to.
        ''' </summary>
        Public Overridable ReadOnly Property ReceiverType As TypeSymbol
            Get
                Return Me.ContainingType
            End Get
        End Property

        ''' <summary>
        ''' If this method is a reduced extension method, returns a type inferred during reduction process for the type parameter. 
        ''' </summary>
        ''' <param name="reducedFromTypeParameter">Type parameter of the corresponding <see cref="ReducedFrom"/> method.</param>
        ''' <returns>Inferred type or Nothing if nothing was inferred.</returns>
        ''' <exception cref="System.InvalidOperationException">If this is not a reduced extension method.</exception>
        ''' <exception cref="System.ArgumentNullException">If <paramref name="reducedFromTypeParameter"/> is Nothing.</exception>
        ''' <exception cref="System.ArgumentException">If <paramref name="reducedFromTypeParameter"/> doesn't belong to the corresponding <see cref="ReducedFrom"/> method.</exception>
        Public Overridable Function GetTypeInferredDuringReduction(reducedFromTypeParameter As TypeParameterSymbol) As TypeSymbol
            Throw New InvalidOperationException()
        End Function

        ''' <summary>
        ''' Fixed type parameters for a reduced extension method or empty.
        ''' </summary>
        Friend Overridable ReadOnly Property FixedTypeParameters As ImmutableArray(Of KeyValuePair(Of TypeParameterSymbol, TypeSymbol))
            Get
                Return ImmutableArray(Of KeyValuePair(Of TypeParameterSymbol, TypeSymbol)).Empty
            End Get
        End Property

        ''' <summary>
        ''' If this is an extension method that can be applied to a instance of the given type,
        ''' returns the reduced method symbol thus formed. Otherwise, returns Nothing.
        ''' 
        ''' Name lookup should use this method in order to capture proximity, which affects 
        ''' overload resolution. 
        ''' </summary>
        Friend Function ReduceExtensionMethod(instanceType As TypeSymbol, proximity As Integer) As MethodSymbol
            Return ReducedExtensionMethodSymbol.Create(instanceType, Me, proximity)
        End Function

        ''' <summary>
        ''' If this is an extension method that can be applied to a instance of the given type,
        ''' returns the reduced method symbol thus formed. Otherwise, returns Nothing.
        ''' </summary>
        Public Function ReduceExtensionMethod(instanceType As TypeSymbol) As MethodSymbol
            Return ReduceExtensionMethod(instanceType, proximity:=0)
        End Function

        ''' <summary>
        ''' Proximity level of a reduced extension method.
        ''' </summary>
        Friend Overridable ReadOnly Property Proximity As Integer
            Get
                Return 0
            End Get
        End Property

        Friend Overrides ReadOnly Property EmbeddedSymbolKind As EmbeddedSymbolKind
            Get
                Return If(Me.ContainingSymbol Is Nothing, EmbeddedSymbolKind.None, Me.ContainingSymbol.EmbeddedSymbolKind)
            End Get
        End Property

        ''' <summary> 
        ''' Returns bound block representing method's body. This method is called 
        ''' by 'method compiler' when it is ready to emit IL code for the method.
        ''' 
        ''' The bound method body is typically a high-level tree - it may contain 
        ''' lambdas, foreach etc... which will be processed in CompileMethod(...)
        ''' </summary>
        ''' <param name="methodBodyBinder">Optionally returns a binder, OUT parameter!</param>
        ''' <remarks>
        ''' The method MAY return a binder used for binding so it can be reused later in method compiler
        ''' </remarks>
        Friend Overridable Function GetBoundMethodBody(diagnostics As DiagnosticBag, <Out()> Optional ByRef methodBodyBinder As Binder = Nothing) As BoundBlock
            Throw ExceptionUtilities.Unreachable
        End Function

        ''' <remarks>
        ''' True iff the method contains user code.
        ''' </remarks>
        Friend MustOverride ReadOnly Property GenerateDebugInfoImpl As Boolean

        Friend ReadOnly Property GenerateDebugInfo As Boolean
            Get
                ' Dev11 generates debug info for embedded symbols. 
                ' There is no reason to do so since the source code is not available to the user.
                Return GenerateDebugInfoImpl AndAlso Not IsEmbedded
            End Get
        End Property

        ''' <summary>
        ''' Calculates a syntax offset for a local (user-defined or long-lived synthesized) declared at <paramref name="localPosition"/>.
        ''' Must be implemented by all methods that may contain user code.
        ''' </summary>
        ''' <remarks>
        ''' Syntax offset is a unique identifier for the local within the emitted method body.
        ''' It's based on position of the local declarator. In single-part method bodies it's simply the distance
        ''' from the start of the method body syntax span. If a method body has multiple parts (such as a constructor 
        ''' comprising of code for member initializers and constructor initializer calls) the offset is calculated
        ''' as if all source these parts were concatenated together and prepended to the constructor body.
        ''' The resulting syntax offset is then negative for locals defined outside of the constructor body.
        ''' </remarks>
        Friend MustOverride Function CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer

        ''' <summary>
        ''' Specifies whether existing, "unused" locals (corresponding to proxies) are preserved during lambda rewriting.
        ''' </summary>
        ''' <remarks>
        ''' This value will be checked by the <see cref="LambdaRewriter"/> and is needed so that existing locals aren't
        ''' omitted in the EE (method symbols in the EE will override this property to return True).
        ''' </remarks>
        Friend Overridable ReadOnly Property PreserveOriginalLocals As Boolean
            Get
                Return False
            End Get
        End Property

#Region "IMethodSymbol"

        Private ReadOnly Property IMethodSymbol_Arity As Integer Implements IMethodSymbol.Arity
            Get
                Return Me.Arity
            End Get
        End Property

        Private ReadOnly Property IMethodSymbol_ConstructedFrom As IMethodSymbol Implements IMethodSymbol.ConstructedFrom
            Get
                Return Me.ConstructedFrom
            End Get
        End Property

        Private ReadOnly Property IMethodSymbol_ExplicitInterfaceImplementations As ImmutableArray(Of IMethodSymbol) Implements IMethodSymbol.ExplicitInterfaceImplementations
            Get
                Return ImmutableArrayExtensions.Cast(Of MethodSymbol, IMethodSymbol)(Me.ExplicitInterfaceImplementations)
            End Get
        End Property

        Private ReadOnly Property IMethodSymbol_IsExtensionMethod As Boolean Implements IMethodSymbol.IsExtensionMethod
            Get
                Return Me.IsExtensionMethod
            End Get
        End Property

        Private ReadOnly Property IMethodSymbol_MethodKind As MethodKind Implements IMethodSymbol.MethodKind
            Get
                Return Me.MethodKind
            End Get
        End Property

        Private ReadOnly Property IMethodSymbol_OriginalDefinition As IMethodSymbol Implements IMethodSymbol.OriginalDefinition
            Get
                Return Me.OriginalDefinition
            End Get
        End Property

        Private ReadOnly Property IMethodSymbol_OverriddenMethod As IMethodSymbol Implements IMethodSymbol.OverriddenMethod
            Get
                Return Me.OverriddenMethod
            End Get
        End Property

        Private ReadOnly Property IMethodSymbol_ReceiverType As ITypeSymbol Implements IMethodSymbol.ReceiverType
            Get
                Return Me.ReceiverType
            End Get
        End Property

        Private Function IMethodSymbol_GetTypeInferredDuringReduction(reducedFromTypeParameter As ITypeParameterSymbol) As ITypeSymbol Implements IMethodSymbol.GetTypeInferredDuringReduction
            Return Me.GetTypeInferredDuringReduction(reducedFromTypeParameter.EnsureVbSymbolOrNothing(Of TypeParameterSymbol)(NameOf(reducedFromTypeParameter)))
        End Function

        Private ReadOnly Property IMethodSymbol_ReducedFrom As IMethodSymbol Implements IMethodSymbol.ReducedFrom
            Get
                Return Me.ReducedFrom
            End Get
        End Property

        Private Function IMethodSymbol_ReduceExtensionMethod(receiverType As ITypeSymbol) As IMethodSymbol Implements IMethodSymbol.ReduceExtensionMethod
            If receiverType Is Nothing Then
                Throw New ArgumentNullException(NameOf(receiverType))
            End If

            Return Me.ReduceExtensionMethod(receiverType.EnsureVbSymbolOrNothing(Of TypeSymbol)(NameOf(receiverType)))
        End Function

        Private ReadOnly Property IMethodSymbol_Parameters As ImmutableArray(Of IParameterSymbol) Implements IMethodSymbol.Parameters
            Get
                Return ImmutableArray(Of IParameterSymbol).CastUp(Me.Parameters)
            End Get
        End Property

        ''' <summary>
        ''' Returns true if this symbol is defined outside of the compilation.
        ''' For instance if the method is <c>Declare Sub</c>.
        ''' </summary>
        Private ReadOnly Property ISymbol_IsExtern As Boolean Implements ISymbol.IsExtern
            Get
                Return IsExternalMethod
            End Get
        End Property

        ''' <summary>
        ''' If this is a partial method declaration without a body, and the method also
        ''' has a part that implements it with a body, returns that implementing
        ''' definition.  Otherwise null.
        ''' </summary>
        Public Overridable ReadOnly Property PartialImplementationPart As MethodSymbol
            Get
                Return Nothing
            End Get
        End Property

        ''' <summary>
        ''' If this is a partial method with a body, returns the corresponding
        ''' definition part (without a body).  Otherwise null.
        ''' </summary>
        Public Overridable ReadOnly Property PartialDefinitionPart As MethodSymbol
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IMethodSymbol_PartialDefinitionPart As IMethodSymbol Implements IMethodSymbol.PartialDefinitionPart
            Get
                Return PartialDefinitionPart
            End Get
        End Property

        Private ReadOnly Property IMethodSymbol_PartialImplementationPart As IMethodSymbol Implements IMethodSymbol.PartialImplementationPart
            Get
                Return PartialImplementationPart
            End Get
        End Property

        Private ReadOnly Property IMethodSymbol_ReturnsVoid As Boolean Implements IMethodSymbol.ReturnsVoid
            Get
                Return Me.IsSub
            End Get
        End Property

        Private ReadOnly Property IMethodSymbol_ReturnsByRef As Boolean Implements IMethodSymbol.ReturnsByRef
            Get
                Return Me.ReturnsByRef
            End Get
        End Property

        Private ReadOnly Property IMethodSymbol_ReturnType As ITypeSymbol Implements IMethodSymbol.ReturnType
            Get
                Return Me.ReturnType
            End Get
        End Property

        Private ReadOnly Property IMethodSymbol_TypeArguments As ImmutableArray(Of ITypeSymbol) Implements IMethodSymbol.TypeArguments
            Get
                Return StaticCast(Of ITypeSymbol).From(Me.TypeArguments)
            End Get
        End Property

        Private ReadOnly Property IMethodSymbol_TypeParameters As ImmutableArray(Of ITypeParameterSymbol) Implements IMethodSymbol.TypeParameters
            Get
                Return StaticCast(Of ITypeParameterSymbol).From(Me.TypeParameters)
            End Get
        End Property

        Private ReadOnly Property IMethodSymbol_AssociatedSymbol As ISymbol Implements IMethodSymbol.AssociatedSymbol
            Get
                Return Me.AssociatedSymbol
            End Get
        End Property

        Private ReadOnly Property IMethodSymbol_IsGenericMethod As Boolean Implements IMethodSymbol.IsGenericMethod
            Get
                Return Me.IsGenericMethod
            End Get
        End Property

        Private ReadOnly Property IMethodSymbol_IsAsync As Boolean Implements IMethodSymbol.IsAsync
            Get
                Return Me.IsAsync
            End Get
        End Property

        Private ReadOnly Property IMethodSymbol_HidesBaseMethodsByName As Boolean Implements IMethodSymbol.HidesBaseMethodsByName
            Get
                Return True
            End Get
        End Property

        Private ReadOnly Property IMethodSymbol_ReturnTypeCustomModifiers As ImmutableArray(Of CustomModifier) Implements IMethodSymbol.ReturnTypeCustomModifiers
            Get
                Return Me.ReturnTypeCustomModifiers
            End Get
        End Property

        Private Function IMethodSymbol_GetReturnTypeAttributes() As ImmutableArray(Of AttributeData) Implements IMethodSymbol.GetReturnTypeAttributes
            Return ImmutableArrayExtensions.Cast(Of VisualBasicAttributeData, AttributeData)(Me.GetReturnTypeAttributes)
        End Function

        Private Function IMethodSymbol_Construct(ParamArray arguments() As ITypeSymbol) As IMethodSymbol Implements IMethodSymbol.Construct
            For Each arg In arguments
                arg.EnsureVbSymbolOrNothing(Of TypeSymbol)("typeArguments")
            Next

            Return Construct(arguments.Cast(Of TypeSymbol).ToArray())
        End Function

        Private ReadOnly Property IMethodSymbol_AssociatedAnonymousDelegate As INamedTypeSymbol Implements IMethodSymbol.AssociatedAnonymousDelegate
            Get
                Return Me.AssociatedAnonymousDelegate
            End Get
        End Property
#End Region

#Region "IMethodSymbolInternal"
        Private Function IMethodSymbolInternal_CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer Implements IMethodSymbolInternal.CalculateLocalSyntaxOffset
            Return CalculateLocalSyntaxOffset(localPosition, localTree)
        End Function
#End Region

#Region "ISymbol"

        Public Overrides Sub Accept(ByVal visitor As SymbolVisitor)
            visitor.VisitMethod(Me)
        End Sub

        Public Overrides Function Accept(Of TResult)(ByVal visitor As SymbolVisitor(Of TResult)) As TResult
            Return visitor.VisitMethod(Me)
        End Function

        Public Overrides Sub Accept(visitor As VisualBasicSymbolVisitor)
            visitor.VisitMethod(Me)
        End Sub

        Public Overrides Function Accept(Of TResult)(visitor As VisualBasicSymbolVisitor(Of TResult)) As TResult
            Return visitor.VisitMethod(Me)
        End Function

#End Region

    End Class
End Namespace
