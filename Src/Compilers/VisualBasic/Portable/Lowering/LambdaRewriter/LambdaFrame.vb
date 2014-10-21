' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' A class that represents the set of variables in a scope that have been
    ''' captured by lambdas within that scope.
    ''' </summary>
    Friend NotInheritable Class LambdaFrame
        Inherits SynthesizedContainer
        Implements ISynthesizedMethodBodyImplementationSymbol

        Private ReadOnly m_typeParameters As ImmutableArray(Of TypeParameterSymbol)
        Private ReadOnly m_topLevelMethod As MethodSymbol
        Private ReadOnly m_sharedConstructor As MethodSymbol
        Private ReadOnly m_singletonCache As FieldSymbol

        'NOTE: this does not include captured parent frame references 
        Friend ReadOnly m_captured_locals As New ArrayBuilder(Of LambdaCapturedVariable)
        Friend ReadOnly m_constructor As SynthesizedLambdaConstructor
        Friend ReadOnly TypeMap As TypeSubstitution

        Private ReadOnly m_scopeSyntaxOpt As VisualBasicSyntaxNode

        Private Shared ReadOnly TypeSubstitutionFactory As Func(Of Symbol, TypeSubstitution) =
            Function(container)
                Dim f = TryCast(container, LambdaFrame)
                Return If(f IsNot Nothing, f.TypeMap, DirectCast(container, SynthesizedMethod).TypeMap)
            End Function

        Friend Shared ReadOnly CreateTypeParameter As Func(Of TypeParameterSymbol, Symbol, TypeParameterSymbol) =
            Function(typeParameter, container) New SynthesizedClonedTypeParameterSymbol(typeParameter,
                                                                                        container,
                                                                                        StringConstants.CLOSURE_GENERICPARAM_PREFIX & typeParameter.Ordinal,
                                                                                        TypeSubstitutionFactory)
        ''' <summary>
        ''' Creates a Frame definition
        ''' </summary>
        Friend Sub New(compilationState As TypeCompilationState,
                       topLevelMethod As MethodSymbol,
                       syntax As VisualBasicSyntaxNode,
                       copyConstructor As Boolean,
                       isShared As Boolean)

            MyBase.New(topLevelMethod, GeneratedNames.MakeLambdaDisplayClassName(compilationState.GenerateTempNumber()), topLevelMethod.ContainingType, ImmutableArray(Of NamedTypeSymbol).Empty)

            If copyConstructor Then
                Me.m_constructor = New SynthesizedLambdaCopyConstructor(syntax, Me)
            Else
                Me.m_constructor = New SynthesizedLambdaConstructor(syntax, Me)
            End If

            ' static lambdas technically have the class scope so the scope syntax is Nothing 
            If isShared Then
                Me.m_sharedConstructor = New SynthesizedConstructorSymbol(Nothing, Me, isShared:=True, isDebuggable:=False, binder:=Nothing, diagnostics:=Nothing)
                Dim cacheVariableName = GeneratedNames.MakeCachedFrameInstanceName()
                Me.m_singletonCache = New SynthesizedFieldSymbol(Me, Me, Me, cacheVariableName, Accessibility.Public, isReadOnly:=True, isShared:=True)
                m_scopeSyntaxOpt = Nothing
            Else
                m_scopeSyntaxOpt = syntax
            End If

            AssertIsLambdaScopeSyntax(m_scopeSyntaxOpt)

            Me.m_typeParameters = SynthesizedClonedTypeParameterSymbol.MakeTypeParameters(topLevelMethod.TypeParameters, Me, CreateTypeParameter)
            Me.TypeMap = TypeSubstitution.Create(topLevelMethod, topLevelMethod.TypeParameters, Me.TypeArgumentsNoUseSiteDiagnostics)
            Me.m_topLevelMethod = topLevelMethod
        End Sub

        <Conditional("DEBUG")>
        Private Shared Sub AssertIsLambdaScopeSyntax(syntax As VisualBasicSyntaxNode)

        End Sub

        Public ReadOnly Property ScopeSyntax As VisualBasicSyntaxNode
            Get
                Return m_constructor.Syntax
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                ' Dev11 uses "assembly" here. No need to be different.
                Return Accessibility.Friend
            End Get
        End Property

        Public Overloads Overrides Function GetMembers(name As String) As ImmutableArray(Of Symbol)
            Return ImmutableArray(Of Symbol).Empty
        End Function

        Public Overloads Overrides Function GetMembers() As ImmutableArray(Of Symbol)
            Dim members = StaticCast(Of Symbol).From(m_captured_locals.AsImmutable())
            If m_sharedConstructor IsNot Nothing Then
                members = members.AddRange(ImmutableArray.Create(Of Symbol)(m_constructor, m_sharedConstructor, m_singletonCache))
            Else
                members = members.Add(m_constructor)
            End If

            Return members
        End Function

        Protected Friend Overrides ReadOnly Property Constructor As MethodSymbol
            Get
                Return m_constructor
            End Get
        End Property

        Protected Friend ReadOnly Property SharedConstructor As MethodSymbol
            Get
                Return m_sharedConstructor
            End Get
        End Property

        Friend ReadOnly Property SingletonCache As FieldSymbol
            Get
                Return m_singletonCache
            End Get
        End Property

        Friend Overrides Function GetFieldsToEmit() As IEnumerable(Of FieldSymbol)
            If m_singletonCache Is Nothing Then
                Return m_captured_locals
            Else
                Return DirectCast(m_captured_locals, IEnumerable(Of FieldSymbol)).Concat(Me.m_singletonCache)
            End If
        End Function

        Friend Overrides Function MakeAcyclicBaseType(diagnostics As DiagnosticBag) As NamedTypeSymbol
            Dim type = ContainingAssembly.GetSpecialType(SpecialType.System_Object)
            ' WARN: We assume that if System_Object was not found we would never reach 
            '       this point because the error should have been/processed generated earlier
            Debug.Assert(type.GetUseSiteErrorInfo() Is Nothing)
            Return type
        End Function

        Friend Overrides Function MakeAcyclicInterfaces(diagnostics As DiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Friend Overrides Function MakeDeclaredBase(basesBeingResolved As ConsList(Of Symbol), diagnostics As DiagnosticBag) As NamedTypeSymbol
            Dim type = ContainingAssembly.GetSpecialType(SpecialType.System_Object)
            ' WARN: We assume that if System_Object was not found we would never reach 
            '       this point because the error should have been/processed generated earlier
            Debug.Assert(type.GetUseSiteErrorInfo() Is Nothing)
            Return type
        End Function

        Friend Overrides Function MakeDeclaredInterfaces(basesBeingResolved As ConsList(Of Symbol), diagnostics As DiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Public Overrides ReadOnly Property MemberNames As IEnumerable(Of String)
            Get
                Return SpecializedCollections.EmptyEnumerable(Of String)()
            End Get
        End Property

        Public Overrides ReadOnly Property TypeKind As TypeKind
            Get
                Return TypeKind.Class
            End Get
        End Property

        Public Overrides ReadOnly Property Arity As Integer
            Get
                Return Me.m_typeParameters.Length
            End Get
        End Property

        Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                Return Me.m_typeParameters
            End Get
        End Property

        Friend Overrides ReadOnly Property IsInterface As Boolean
            Get
                Return False
            End Get
        End Property

        Public ReadOnly Property HasMethodBodyDependency As Boolean Implements ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency
            Get
                ' This method contains user code from the lamda
                Return True
            End Get
        End Property

        Public ReadOnly Property Method As IMethodSymbol Implements ISynthesizedMethodBodyImplementationSymbol.Method
            Get
                Return m_topLevelMethod
            End Get
        End Property
    End Class

End Namespace