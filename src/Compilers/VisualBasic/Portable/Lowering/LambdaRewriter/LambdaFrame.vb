' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' A class that represents the set of variables in a scope that have been
    ''' captured by lambdas within that scope.
    ''' </summary>
    Friend NotInheritable Class LambdaFrame
        Inherits SynthesizedContainer
        Implements ISynthesizedMethodBodyImplementationSymbol

        Private ReadOnly _typeParameters As ImmutableArray(Of TypeParameterSymbol)
        Private ReadOnly _topLevelMethod As MethodSymbol
        Private ReadOnly _sharedConstructor As MethodSymbol
        Private ReadOnly _singletonCache As FieldSymbol
        Friend ReadOnly ClosureOrdinal As Integer

        'NOTE: this does not include captured parent frame references 
        Friend ReadOnly CapturedLocals As New ArrayBuilder(Of LambdaCapturedVariable)
        Private ReadOnly _constructor As SynthesizedLambdaConstructor
        Friend ReadOnly TypeMap As TypeSubstitution

        Private ReadOnly _scopeSyntaxOpt As VisualBasicSyntaxNode

        Private Shared ReadOnly s_typeSubstitutionFactory As Func(Of Symbol, TypeSubstitution) =
            Function(container)
                Dim f = TryCast(container, LambdaFrame)
                Return If(f IsNot Nothing, f.TypeMap, DirectCast(container, SynthesizedMethod).TypeMap)
            End Function

        Friend Shared ReadOnly CreateTypeParameter As Func(Of TypeParameterSymbol, Symbol, TypeParameterSymbol) =
            Function(typeParameter, container) New SynthesizedClonedTypeParameterSymbol(typeParameter,
                                                                                        container,
                                                                                        GeneratedNames.MakeDisplayClassGenericParameterName(typeParameter.Ordinal),
                                                                                        s_typeSubstitutionFactory)
        Friend Sub New(slotAllocatorOpt As VariableSlotAllocator,
                       topLevelMethod As MethodSymbol,
                       methodId As MethodDebugId,
                       scopeSyntaxOpt As VisualBasicSyntaxNode,
                       closureOrdinal As Integer,
                       copyConstructor As Boolean,
                       isStatic As Boolean,
                       isDelegateRelaxationFrame As Boolean)

            MyBase.New(topLevelMethod, MakeName(slotAllocatorOpt, scopeSyntaxOpt, methodId, closureOrdinal, isStatic, isDelegateRelaxationFrame), topLevelMethod.ContainingType, ImmutableArray(Of NamedTypeSymbol).Empty)

            If copyConstructor Then
                Me._constructor = New SynthesizedLambdaCopyConstructor(scopeSyntaxOpt, Me)
            Else
                Me._constructor = New SynthesizedLambdaConstructor(scopeSyntaxOpt, Me)
            End If

            ' static lambdas technically have the class scope so the scope syntax is Nothing 
            If isStatic Then
                Me._sharedConstructor = New SynthesizedConstructorSymbol(Nothing, Me, isShared:=True, isDebuggable:=False, binder:=Nothing, diagnostics:=Nothing)
                Dim cacheVariableName = GeneratedNames.MakeCachedFrameInstanceName()
                Me._singletonCache = New SynthesizedLambdaCacheFieldSymbol(Me, Me, Me, cacheVariableName, topLevelMethod, Accessibility.Public, isReadOnly:=True, isShared:=True)
                _scopeSyntaxOpt = Nothing
            Else
                _scopeSyntaxOpt = scopeSyntaxOpt
            End If

            AssertIsLambdaScopeSyntax(_scopeSyntaxOpt)

            Me._typeParameters = SynthesizedClonedTypeParameterSymbol.MakeTypeParameters(topLevelMethod.TypeParameters, Me, CreateTypeParameter)
            Me.TypeMap = TypeSubstitution.Create(topLevelMethod, topLevelMethod.TypeParameters, Me.TypeArgumentsNoUseSiteDiagnostics)
            Me._topLevelMethod = topLevelMethod
        End Sub

        Private Shared Function MakeName(slotAllocatorOpt As VariableSlotAllocator,
                                         scopeSyntaxOpt As SyntaxNode,
                                         methodId As MethodDebugId,
                                         closureOrdinal As Integer,
                                         isStatic As Boolean,
                                         isDelegateRelaxation As Boolean) As String

            If isStatic Then
                ' Display class is shared among static non-generic lambdas across generations, method ordinal is -1 in that case.
                ' A new display class of a static generic lambda is created for each method and each generation.
                Return GeneratedNames.MakeStaticLambdaDisplayClassName(methodId.Ordinal, methodId.Generation)
            End If

            Dim previousClosureOrdinal As Integer
            If slotAllocatorOpt IsNot Nothing AndAlso slotAllocatorOpt.TryGetPreviousClosure(scopeSyntaxOpt, previousClosureOrdinal) Then
                methodId = slotAllocatorOpt.PreviousMethodId
                closureOrdinal = previousClosureOrdinal
            End If

            ' If we haven't found existing closure in the previous generation, use the current generation method ordinal.
            ' That is, don't try to reuse previous generation method ordinal as that might create name conflict.
            ' E.g.
            '     Gen0                    Gen1
            '                             F() { new closure } // ordinal 0
            '     G() { } // ordinal 0    G() { new closure } // ordinal 1
            '
            ' In the example above G is updated and F is added. 
            ' G's ordinal in Gen0 is 0. If we used that ordinal for updated G's new closure it would conflict with F's ordinal.
            Debug.Assert(methodId.Ordinal >= 0)
            Return GeneratedNames.MakeLambdaDisplayClassName(methodId.Ordinal, methodId.Generation, closureOrdinal, isDelegateRelaxation)
        End Function

        <Conditional("DEBUG")>
        Private Shared Sub AssertIsLambdaScopeSyntax(syntaxOpt As VisualBasicSyntaxNode)
            'TODO: Add checks for possible syntax
        End Sub

        Public ReadOnly Property ScopeSyntax As VisualBasicSyntaxNode
            Get
                Return _constructor.Syntax
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
            Dim members = StaticCast(Of Symbol).From(CapturedLocals.AsImmutable())
            If _sharedConstructor IsNot Nothing Then
                members = members.AddRange(ImmutableArray.Create(Of Symbol)(_constructor, _sharedConstructor, _singletonCache))
            Else
                members = members.Add(_constructor)
            End If

            Return members
        End Function

        Protected Friend Overrides ReadOnly Property Constructor As MethodSymbol
            Get
                Return _constructor
            End Get
        End Property

        Protected Friend ReadOnly Property SharedConstructor As MethodSymbol
            Get
                Return _sharedConstructor
            End Get
        End Property

        Friend ReadOnly Property SingletonCache As FieldSymbol
            Get
                Return _singletonCache
            End Get
        End Property

        Friend Overrides ReadOnly Property IsSerializable As Boolean
            Get
                Return _singletonCache IsNot Nothing
            End Get
        End Property


        Friend Overrides Function GetFieldsToEmit() As IEnumerable(Of FieldSymbol)
            If _singletonCache Is Nothing Then
                Return CapturedLocals
            Else
                Return DirectCast(CapturedLocals, IEnumerable(Of FieldSymbol)).Concat(Me._singletonCache)
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
                Return Me._typeParameters.Length
            End Get
        End Property

        Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                Return Me._typeParameters
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
                Return _topLevelMethod
            End Get
        End Property
    End Class

End Namespace
