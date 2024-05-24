' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Symbols
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
        Friend ReadOnly TopLevelMethod As MethodSymbol
        Private ReadOnly _sharedConstructor As MethodSymbol
        Private ReadOnly _singletonCache As FieldSymbol

        'NOTE: this does not include captured parent frame references 
        Friend ReadOnly CapturedLocals As New ArrayBuilder(Of LambdaCapturedVariable)
        Private ReadOnly _constructor As SynthesizedLambdaConstructor
        Friend ReadOnly TypeMap As TypeSubstitution
        Private ReadOnly _scopeSyntaxOpt As SyntaxNode

        ' debug info:
        Public ReadOnly RudeEdit As RuntimeRudeEdit?
        Public ReadOnly ClosureId As DebugId

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
        Friend Sub New(topLevelMethod As MethodSymbol,
                       scopeSyntaxOpt As SyntaxNode,
                       methodId As DebugId,
                       closureId As DebugId,
                       rudeEdit As RuntimeRudeEdit?,
                       copyConstructor As Boolean,
                       isStatic As Boolean,
                       isDelegateRelaxationFrame As Boolean)

            MyBase.New(topLevelMethod, MakeName(scopeSyntaxOpt, methodId, closureId, isStatic, isDelegateRelaxationFrame), topLevelMethod.ContainingType, ImmutableArray(Of NamedTypeSymbol).Empty)

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

            If Not isDelegateRelaxationFrame Then
                AssertIsClosureScopeSyntax(_scopeSyntaxOpt)
            End If

            Me._typeParameters = SynthesizedClonedTypeParameterSymbol.MakeTypeParameters(topLevelMethod.TypeParameters, Me, CreateTypeParameter)
            Me.TypeMap = TypeSubstitution.Create(topLevelMethod, topLevelMethod.TypeParameters, Me.TypeArgumentsNoUseSiteDiagnostics)
            Me.TopLevelMethod = topLevelMethod
            Me.RudeEdit = rudeEdit
            Me.ClosureId = closureId
        End Sub

        Private Shared Function MakeName(scopeSyntaxOpt As SyntaxNode,
                                         methodId As DebugId,
                                         closureId As DebugId,
                                         isStatic As Boolean,
                                         isDelegateRelaxation As Boolean) As String

            If isStatic Then
                ' Display class is shared among static non-generic lambdas across generations, method ordinal is -1 in that case.
                ' A new display class of a static generic lambda is created for each method and each generation.
                Return GeneratedNames.MakeStaticLambdaDisplayClassName(methodId.Ordinal, methodId.Generation)
            End If

            Debug.Assert(methodId.Ordinal >= 0)
            Return GeneratedNames.MakeLambdaDisplayClassName(methodId.Ordinal, methodId.Generation, closureId.Ordinal, closureId.Generation, isDelegateRelaxation)
        End Function

        <Conditional("DEBUG")>
        Private Shared Sub AssertIsClosureScopeSyntax(syntaxOpt As SyntaxNode)
            ' static lambdas technically have the class scope so the scope syntax is nothing
            If syntaxOpt Is Nothing Then
                Return
            End If

            If LambdaUtilities.IsClosureScope(syntaxOpt) Then
                Return
            End If

            Select Case syntaxOpt.Kind()
                Case SyntaxKind.ObjectMemberInitializer
                    ' TODO: Closure capturing a synthesized "with" variable
                    Return
            End Select

            ExceptionUtilities.UnexpectedValue(syntaxOpt.Kind())
        End Sub

        Public ReadOnly Property ScopeSyntax As SyntaxNode
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

        Public Overrides ReadOnly Property IsSerializable As Boolean
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

        Friend Overrides Function MakeAcyclicBaseType(diagnostics As BindingDiagnosticBag) As NamedTypeSymbol
            Dim type = ContainingAssembly.GetSpecialType(SpecialType.System_Object)
            ' WARN: We assume that if System_Object was not found we would never reach 
            '       this point because the error should have been/processed generated earlier
            Debug.Assert(type.GetUseSiteInfo().DiagnosticInfo Is Nothing)
            Return type
        End Function

        Friend Overrides Function MakeAcyclicInterfaces(diagnostics As BindingDiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Friend Overrides Function MakeDeclaredBase(basesBeingResolved As BasesBeingResolved, diagnostics As BindingDiagnosticBag) As NamedTypeSymbol
            Dim type = ContainingAssembly.GetSpecialType(SpecialType.System_Object)
            ' WARN: We assume that if System_Object was not found we would never reach 
            '       this point because the error should have been/processed generated earlier
            Debug.Assert(type.GetUseSiteInfo().DiagnosticInfo Is Nothing)
            Return type
        End Function

        Friend Overrides Function MakeDeclaredInterfaces(basesBeingResolved As BasesBeingResolved, diagnostics As BindingDiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
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
                ' This method contains user code from the lambda.
                Return True
            End Get
        End Property

        Public ReadOnly Property Method As IMethodSymbolInternal Implements ISynthesizedMethodBodyImplementationSymbol.Method
            Get
                Return TopLevelMethod
            End Get
        End Property
    End Class

End Namespace
