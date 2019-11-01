' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' A method that results from the translation of a single lambda expression.
    ''' </summary>
    Friend NotInheritable Class SynthesizedLambdaMethod
        Inherits SynthesizedMethod
        Implements ISynthesizedMethodBodyImplementationSymbol

        Private ReadOnly _lambda As LambdaSymbol
        Private ReadOnly _parameters As ImmutableArray(Of ParameterSymbol)
        Private ReadOnly _locations As ImmutableArray(Of Location)
        Private ReadOnly _typeParameters As ImmutableArray(Of TypeParameterSymbol)
        Private ReadOnly _typeMap As TypeSubstitution
        Private ReadOnly _topLevelMethod As MethodSymbol

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return If(TypeOf ContainingType Is LambdaFrame, Accessibility.Friend, Accessibility.Private)
            End Get
        End Property

        Friend Overrides ReadOnly Property TypeMap As TypeSubstitution
            Get
                Return Me._typeMap
            End Get
        End Property

        ''' <summary>
        ''' Creates a symbol for a synthesized lambda method
        ''' </summary>
        ''' <param name="containingType">Type that contains lambda method 
        ''' - it is either Frame or enclosing class in a case if we do not lift anything.</param>
        ''' <param name="topLevelMethod">Method that contains lambda expression for which we do the rewrite.</param>
        ''' <param name="lambdaNode">Lambda expression which is represented by this method.</param>
        Friend Sub New(containingType As InstanceTypeSymbol,
                       closureKind As ClosureKind,
                       topLevelMethod As MethodSymbol,
                       topLevelMethodId As DebugId,
                       lambdaNode As BoundLambda,
                       lambdaId As DebugId,
                       diagnostics As DiagnosticBag)

            MyBase.New(lambdaNode.Syntax,
                       containingType,
                       MakeName(topLevelMethodId, closureKind, lambdaNode.LambdaSymbol.SynthesizedKind, lambdaId),
                       isShared:=False)

            Me._lambda = lambdaNode.LambdaSymbol
            Me._locations = ImmutableArray.Create(lambdaNode.Syntax.GetLocation())

            If Not topLevelMethod.IsGenericMethod Then
                Me._typeMap = Nothing
                Me._typeParameters = ImmutableArray(Of TypeParameterSymbol).Empty
            Else
                Dim containingTypeAsFrame = TryCast(containingType, LambdaFrame)
                If containingTypeAsFrame IsNot Nothing Then
                    Me._typeParameters = ImmutableArray(Of TypeParameterSymbol).Empty
                    Me._typeMap = containingTypeAsFrame.TypeMap
                Else
                    Me._typeParameters = SynthesizedClonedTypeParameterSymbol.MakeTypeParameters(topLevelMethod.TypeParameters, Me, LambdaFrame.CreateTypeParameter)
                    Me._typeMap = TypeSubstitution.Create(topLevelMethod, topLevelMethod.TypeParameters, Me.TypeArguments)
                End If
            End If

            Dim params = ArrayBuilder(Of ParameterSymbol).GetInstance
            For Each curParam In _lambda.Parameters
                params.Add(
                    WithNewContainerAndType(
                    Me,
                    curParam.Type.InternalSubstituteTypeParameters(TypeMap).Type,
                    curParam))
            Next

            Me._parameters = params.ToImmutableAndFree
            Me._topLevelMethod = topLevelMethod
        End Sub

        Private Shared Function MakeName(topLevelMethodId As DebugId,
                                         closureKind As ClosureKind,
                                         lambdaKind As SynthesizedLambdaKind,
                                         lambdaId As DebugId) As String
            ' Lambda method name must contain the declaring method ordinal to be unique unless the method is emitted into a closure class exclusive to the declaring method.
            ' Lambdas that only close over "Me" are emitted directly into the top-level method containing type.
            ' Lambdas that don't close over anything (static) are emitted into a shared closure singleton.
            Return GeneratedNames.MakeLambdaMethodName(
                If(closureKind = ClosureKind.General, -1, topLevelMethodId.Ordinal),
                topLevelMethodId.Generation,
                lambdaId.Ordinal,
                lambdaId.Generation,
                lambdaKind)
        End Function

        Public ReadOnly Property TopLevelMethod As MethodSymbol
            Get
                Return _topLevelMethod
            End Get
        End Property

        Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                Return _typeParameters
            End Get
        End Property

        Public Overrides ReadOnly Property TypeArguments As ImmutableArray(Of TypeSymbol)
            Get
                ' This is always a method definition, so the type arguments are the same as the type parameters.
                If Arity > 0 Then
                    Return StaticCast(Of TypeSymbol).From(Me.TypeParameters)
                Else
                    Return ImmutableArray(Of TypeSymbol).Empty
                End If
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return _locations
            End Get
        End Property

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                Return _parameters
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                Return _lambda.ReturnType.InternalSubstituteTypeParameters(TypeMap).Type
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsVararg As Boolean
            Get
                Debug.Assert(Not _lambda.IsVararg)
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property Arity As Integer
            Get
                Return _typeParameters.Length
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property IsAsync As Boolean
            Get
                Return Me._lambda.IsAsync
            End Get
        End Property

        Public Overrides ReadOnly Property IsIterator As Boolean
            Get
                Return Me._lambda.IsIterator
            End Get
        End Property

        Friend Overrides Function IsMetadataNewSlot(Optional ignoreInterfaceImplementationChanges As Boolean = False) As Boolean
            Return False
        End Function

        Friend Function AsMember(constructedFrame As NamedTypeSymbol) As MethodSymbol
            ' ContainingType is always a Frame here which is a type definition so we can use "Is"
            If constructedFrame Is ContainingType Then
                Return Me
            End If

            Dim substituted = DirectCast(constructedFrame, SubstitutedNamedType)
            Return DirectCast(substituted.GetMemberForDefinition(Me), MethodSymbol)
        End Function

        Friend Overrides Sub AddSynthesizedAttributes(compilationState As ModuleCompilationState, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
            MyBase.AddSynthesizedAttributes(compilationState, attributes)

            ' Lambda that doesn't contain user code may still call to a user code (e.g. delegate relaxation stubs). We want the stack frame to be hidden.
            ' Dev11 marks such lambda with DebuggerStepThrough attribute but that seems to be useless. Rather we hide the frame completely.
            If Not GenerateDebugInfoImpl Then
                AddSynthesizedAttribute(attributes, DeclaringCompilation.SynthesizeDebuggerHiddenAttribute())
            End If

            If Me.IsAsync OrElse Me.IsIterator Then
                AddSynthesizedAttribute(attributes, Me.DeclaringCompilation.SynthesizeStateMachineAttribute(Me, compilationState))

                If Me.IsAsync Then
                    ' Async kick-off method calls MoveNext, which contains user code. 
                    ' This means we need to emit DebuggerStepThroughAttribute in order
                    ' to have correct stepping behavior during debugging.
                    AddSynthesizedAttribute(attributes, Me.DeclaringCompilation.SynthesizeOptionalDebuggerStepThroughAttribute())
                End If
            End If
        End Sub

        Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
            Get
                Return _lambda.GenerateDebugInfoImpl
            End Get
        End Property

        Friend Overrides Function CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer
            ' Syntax offset of a syntax node contained in a lambda body is calculated by the containing top-level method.
            ' The offset is thus relative to the top-level method body start.
            Return _topLevelMethod.CalculateLocalSyntaxOffset(localPosition, localTree)
        End Function

        ' The lambda method body needs to be updated when the containing top-level method body is updated.
        Public ReadOnly Property HasMethodBodyDependency As Boolean Implements ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency
            Get
                Return True
            End Get
        End Property

        Public ReadOnly Property Method As IMethodSymbolInternal Implements ISynthesizedMethodBodyImplementationSymbol.Method
            Get
                Return _topLevelMethod
            End Get
        End Property

    End Class
End Namespace
