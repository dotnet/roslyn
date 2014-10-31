' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' A method that results from the translation of a single lambda expression.
    ''' </summary>
    Friend NotInheritable Class SynthesizedLambdaMethod
        Inherits SynthesizedMethod

        Private ReadOnly m_lambda As LambdaSymbol
        Private ReadOnly m_parameters As ImmutableArray(Of ParameterSymbol)
        Private ReadOnly m_locations As ImmutableArray(Of Location)
        Private ReadOnly m_typeParameters As ImmutableArray(Of TypeParameterSymbol)
        Private ReadOnly m_typeMap As TypeSubstitution

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return If(TypeOf ContainingType Is LambdaFrame, Accessibility.Friend, Accessibility.Private)
            End Get
        End Property

        Friend Overrides ReadOnly Property TypeMap As TypeSubstitution
            Get
                Return Me.m_typeMap
            End Get
        End Property

        ''' <summary>
        ''' Creates a symbol for a synthesized lambda method
        ''' </summary>
        ''' <param name="containingType">Type that contains lambda method 
        ''' - it is either Frame or enclosing class in a case if we do not lift anything.</param>
        ''' <param name="enclosingMethod">Method that contains lambda expression for which we do the rewrite.</param>
        ''' <param name="lambdaNode">Lambda expression which is represented by this method.</param>
        Friend Sub New(containingType As InstanceTypeSymbol,
                       enclosingMethod As MethodSymbol,
                       lambdaNode As BoundLambda,
                       tempNumber As Integer,
                       diagnostics As DiagnosticBag)

            MyBase.New(lambdaNode.Syntax, containingType, GeneratedNames.MakeLambdaMethodName(tempNumber), isShared:=False)
            Me.m_lambda = lambdaNode.LambdaSymbol
            Me.m_locations = ImmutableArray.Create(Of Location)(lambdaNode.Syntax.GetLocation())

            If Not enclosingMethod.IsGenericMethod Then
                Me.m_typeMap = Nothing
                Me.m_typeParameters = ImmutableArray(Of TypeParameterSymbol).Empty
            Else
                Dim containingTypeAsFrame = TryCast(containingType, LambdaFrame)
                If containingTypeAsFrame IsNot Nothing Then
                    Me.m_typeParameters = ImmutableArray(Of TypeParameterSymbol).Empty
                    Me.m_typeMap = containingTypeAsFrame.TypeMap
                Else
                    Me.m_typeParameters = SynthesizedClonedTypeParameterSymbol.MakeTypeParameters(enclosingMethod.TypeParameters, Me, LambdaFrame.CreateTypeParameter)
                    Me.m_typeMap = TypeSubstitution.Create(enclosingMethod, enclosingMethod.TypeParameters, Me.TypeArguments)
                End If
            End If

            Dim params = ArrayBuilder(Of ParameterSymbol).GetInstance
            For Each curParam In m_lambda.Parameters
                params.Add(
                    WithNewContainerAndType(
                    Me,
                    curParam.Type.InternalSubstituteTypeParameters(TypeMap),
                    curParam))
            Next

            Me.m_parameters = params.ToImmutableAndFree
        End Sub

        Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                Return m_typeParameters
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
                Return m_locations
            End Get
        End Property

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                Return m_parameters
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                Return m_lambda.ReturnType.InternalSubstituteTypeParameters(TypeMap)
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsVararg As Boolean
            Get
                Debug.Assert(Not m_lambda.IsVararg)
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property Arity As Integer
            Get
                Return m_typeParameters.Length
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property IsAsync As Boolean
            Get
                Return Me.m_lambda.IsAsync
            End Get
        End Property

        Public Overrides ReadOnly Property IsIterator As Boolean
            Get
                Return Me.m_lambda.IsIterator
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
            End If
        End Sub

        Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
            Get
                Return m_lambda.GenerateDebugInfoImpl
            End Get
        End Property

        Friend Overrides Function CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer
            Dim syntax = TryCast(Me.Syntax, LambdaExpressionSyntax)

            ' Assign -1 offset to all variables that are associated with the header.
            ' We can't assign >=0 since user-defined variables defined in the first statement of the body have 0
            ' and user-defined variables need to have a unique syntax offset.
            If syntax Is Nothing OrElse localPosition = syntax.Begin.SpanStart Then
                Return -1
            End If

            ' All other locals are declared within the body of the lambda:
            Debug.Assert(syntax.Span.Contains(localPosition))
            Dim multiLine = TryCast(syntax, MultiLineLambdaExpressionSyntax)
            Dim bodyStart = If(multiLine IsNot Nothing, multiLine.Statements.Span.Start, DirectCast(Me.Syntax, SingleLineLambdaExpressionSyntax).Body.SpanStart)

            Debug.Assert(localPosition >= bodyStart)
            Return localPosition - bodyStart
        End Function
    End Class
End Namespace