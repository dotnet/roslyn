' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' This class represents a type symbol for compiler generated implementation methods,
    ''' the method being implemented is passed as a parameter and is used to build
    ''' implementation method's parameters, return value type, etc...
    ''' </summary>
    Friend MustInherit Class SynthesizedStateMachineMethod
        Inherits SynthesizedMethod
        Implements ISynthesizedMethodBodyImplementationSymbol

        Private ReadOnly _interfaceMethod As MethodSymbol
        Private ReadOnly _parameters As ImmutableArray(Of ParameterSymbol)
        Private ReadOnly _locations As ImmutableArray(Of Location)
        Private ReadOnly _accessibility As Accessibility
        Private ReadOnly _generateDebugInfo As Boolean
        Private ReadOnly _hasMethodBodyDependency As Boolean
        Private ReadOnly _associatedProperty As PropertySymbol

        Protected Sub New(stateMachineType As StateMachineTypeSymbol,
                          name As String,
                          interfaceMethod As MethodSymbol,
                          syntax As VisualBasicSyntaxNode,
                          declaredAccessibility As Accessibility,
                          generateDebugInfo As Boolean,
                          hasMethodBodyDependency As Boolean,
                          Optional associatedProperty As PropertySymbol = Nothing)

            MyBase.New(syntax, stateMachineType, name, isShared:=False)

            Me._locations = ImmutableArray.Create(syntax.GetLocation())
            Me._accessibility = declaredAccessibility
            Me._generateDebugInfo = generateDebugInfo
            Me._hasMethodBodyDependency = hasMethodBodyDependency

            Debug.Assert(Not interfaceMethod.IsGenericMethod)
            Me._interfaceMethod = interfaceMethod

            Dim params(Me._interfaceMethod.ParameterCount - 1) As ParameterSymbol
            For i = 0 To params.Length - 1
                Dim curParam = Me._interfaceMethod.Parameters(i)
                Debug.Assert(Not curParam.IsOptional)
                Debug.Assert(Not curParam.HasExplicitDefaultValue)
                params(i) = SynthesizedMethod.WithNewContainerAndType(Me, curParam.Type, curParam)
            Next
            Me._parameters = params.AsImmutableOrNull()

            Me._associatedProperty = associatedProperty
        End Sub

        Public ReadOnly Property StateMachineType As StateMachineTypeSymbol
            Get
                Return DirectCast(ContainingSymbol, StateMachineTypeSymbol)
            End Get
        End Property

        Friend Overrides ReadOnly Property TypeMap As TypeSubstitution
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property TypeArguments As ImmutableArray(Of TypeSymbol)
            Get
                Return ImmutableArray(Of TypeSymbol).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return Me._locations
            End Get
        End Property

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                Return Me._parameters
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                Return Me._interfaceMethod.ReturnType
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsSub As Boolean
            Get
                Return Me._interfaceMethod.IsSub
            End Get
        End Property

        Public Overrides ReadOnly Property IsVararg As Boolean
            Get
                Return Me._interfaceMethod.IsVararg
            End Get

        End Property

        Public Overrides ReadOnly Property Arity As Integer
            Get
                Return 0
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Me._accessibility
            End Get
        End Property

        Friend Overrides ReadOnly Property ParameterCount As Integer
            Get
                Return Me._parameters.Length
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of MethodSymbol)
            Get
                Return ImmutableArray.Create(Me._interfaceMethod)
            End Get
        End Property

        Public Overrides ReadOnly Property AssociatedSymbol As Symbol
            Get
                Return Me._associatedProperty
            End Get
        End Property

        Friend Overrides Function IsMetadataNewSlot(Optional ignoreInterfaceImplementationChanges As Boolean = False) As Boolean
            Return True
        End Function

        Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
            Get
                Return Me._generateDebugInfo
            End Get
        End Property

        Friend Overrides Function CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer
            Return Me.StateMachineType.KickoffMethod.CalculateLocalSyntaxOffset(localPosition, localTree)
        End Function

        Public ReadOnly Property HasMethodBodyDependency As Boolean Implements ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency
            Get
                Return _hasMethodBodyDependency
            End Get
        End Property

        Public ReadOnly Property Method As IMethodSymbol Implements ISynthesizedMethodBodyImplementationSymbol.Method
            Get
                Return StateMachineType.KickoffMethod
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Represents a state machine MoveNext method.
    ''' Handles special behavior around inheriting some attributes from the original async/iterator method.
    ''' </summary>
    Friend NotInheritable Class SynthesizedStateMachineMoveNextMethod
        Inherits SynthesizedStateMachineMethod

        Private _attributes As ImmutableArray(Of VisualBasicAttributeData)

        Friend Sub New(stateMachineType As StateMachineTypeSymbol,
                       interfaceMethod As MethodSymbol,
                       syntax As VisualBasicSyntaxNode,
                       declaredAccessibility As Accessibility)
            MyBase.New(stateMachineType, WellKnownMemberNames.MoveNextMethodName, interfaceMethod, syntax, declaredAccessibility, generateDebugInfo:=True, hasMethodBodyDependency:=True)
        End Sub

        Friend Overrides Sub AddSynthesizedAttributes(compilationState As ModuleCompilationState, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
            MyBase.AddSynthesizedAttributes(compilationState, attributes)

            Debug.Assert(WellKnownMembers.IsSynthesizedAttributeOptional(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor))
            Dim compilation = Me.DeclaringCompilation
            AddSynthesizedAttribute(attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor))
        End Sub

        Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            If _attributes.IsDefault Then
                Debug.Assert(MyBase.GetAttributes().Length = 0)

                Dim builder As ArrayBuilder(Of VisualBasicAttributeData) = Nothing

                ' Inherit some attributes from the kickoff method
                Dim kickoffMethod = StateMachineType.KickoffMethod
                For Each attribute In kickoffMethod.GetAttributes()
                    If attribute.IsTargetAttribute(kickoffMethod, AttributeDescription.DebuggerHiddenAttribute) OrElse
                       attribute.IsTargetAttribute(kickoffMethod, AttributeDescription.DebuggerNonUserCodeAttribute) OrElse
                       attribute.IsTargetAttribute(kickoffMethod, AttributeDescription.DebuggerStepperBoundaryAttribute) OrElse
                       attribute.IsTargetAttribute(kickoffMethod, AttributeDescription.DebuggerStepThroughAttribute) Then
                        If builder Is Nothing Then
                            builder = ArrayBuilder(Of VisualBasicAttributeData).GetInstance(4) ' only 4 different attributes are inherited at the moment
                        End If

                        builder.Add(attribute)
                    End If
                Next

                ImmutableInterlocked.InterlockedCompareExchange(_attributes,
                                                                If(builder Is Nothing, ImmutableArray(Of VisualBasicAttributeData).Empty, builder.ToImmutableAndFree()),
                                                                Nothing)
            End If

            Return _attributes
        End Function
    End Class

    ''' <summary>
    ''' Represents a state machine method other than a MoveNext method.
    ''' All such methods are considered non-user code. 
    ''' </summary>
    Friend NotInheritable Class SynthesizedStateMachineDebuggerNonUserCodeMethod
        Inherits SynthesizedStateMachineMethod

        Friend Sub New(stateMachineType As StateMachineTypeSymbol,
                       name As String,
                       interfaceMethod As MethodSymbol,
                       syntax As VisualBasicSyntaxNode,
                       declaredAccessibility As Accessibility,
                       hasMethodBodyDependency As Boolean,
                       Optional associatedProperty As PropertySymbol = Nothing)
            MyBase.New(stateMachineType, name, interfaceMethod, syntax, declaredAccessibility, generateDebugInfo:=False,
                       hasMethodBodyDependency:=hasMethodBodyDependency, associatedProperty:=associatedProperty)
        End Sub

        Friend Overrides Sub AddSynthesizedAttributes(compilationState As ModuleCompilationState, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
            MyBase.AddSynthesizedAttributes(compilationState, attributes)

            Debug.Assert(WellKnownMembers.IsSynthesizedAttributeOptional(WellKnownMember.System_Diagnostics_DebuggerNonUserCodeAttribute__ctor))
            Dim compilation = Me.DeclaringCompilation
            AddSynthesizedAttribute(attributes, compilation.SynthesizeDebuggerNonUserCodeAttribute())
        End Sub
    End Class

End Namespace
