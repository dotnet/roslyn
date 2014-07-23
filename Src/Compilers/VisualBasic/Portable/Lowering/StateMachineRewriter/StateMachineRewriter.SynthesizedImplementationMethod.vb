' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Linq
Imports System.Text
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.RuntimeMembers
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend MustInherit Class StateMachineRewriter(Of TStateMachineState As AbstractStateMachineTypeSymbol, TProxy)

        <Flags()>
        Friend Enum DebugAttributes As Byte
            None = 0
            CompilerGeneratedAttribute = 1
            DebuggerHiddenAttribute = 2
            DebuggerNonUserCodeAttribute = 4
        End Enum

        ''' <summary>
        ''' This class represents a type symbol for compiler generated implementation methods,
        ''' the method being implemented is passed as a parameter and is used to build
        ''' implementation method's parameters, return value type, etc...
        ''' </summary>
        Friend NotInheritable Class SynthesizedImplementationMethod
            Inherits MethodToClassRewriter(Of TProxy).SynthesizedMethod
            Implements ISynthesizedMethodBodyImplementationSymbol

            Private ReadOnly _interfaceMethod As MethodSymbol
            Private ReadOnly _parameters As ImmutableArray(Of ParameterSymbol)
            Private ReadOnly _locations As ImmutableArray(Of Location)
            Private ReadOnly _debugAttributes As DebugAttributes
            Private ReadOnly _accessibility As Accessibility
            Private ReadOnly _enableDebugInfo As Boolean
            Private ReadOnly _hasMethodBodyDependency As Boolean
            Private ReadOnly _associatedProperty As PropertySymbol
            Private ReadOnly _asyncKickoffMethod As MethodSymbol

            Friend Sub New(containingType As NamedTypeSymbol,
                           name As String,
                           interfaceMethod As MethodSymbol,
                           syntax As VisualBasicSyntaxNode,
                           attributes As DebugAttributes,
                           declaredAccessibility As Accessibility,
                           enableDebugInfo As Boolean,
                           hasMethodBodyDependency As Boolean,
                           Optional associatedProperty As PropertySymbol = Nothing,
                           Optional asyncKickoffMethod As MethodSymbol = Nothing)

                MyBase.New(syntax, containingType, name, isShared:=False)

                Me._locations = ImmutableArray.Create(Of Location)(syntax.GetLocation())
                Me._debugAttributes = attributes
                Me._accessibility = declaredAccessibility
                Me._enableDebugInfo = enableDebugInfo
                Me._hasMethodBodyDependency = hasMethodBodyDependency

                Debug.Assert(Not interfaceMethod.IsGenericMethod)
                Me._interfaceMethod = interfaceMethod

                Dim params(Me._interfaceMethod.ParameterCount - 1) As ParameterSymbol
                For i = 0 To params.Count - 1
                    Dim curParam = Me._interfaceMethod.Parameters(i)
                    Debug.Assert(Not curParam.IsOptional)
                    Debug.Assert(Not curParam.HasExplicitDefaultValue)
                    params(i) = MethodToClassRewriter(Of TProxy).SynthesizedMethod.WithNewContainerAndType(Me, curParam.Type, curParam)
                Next
                Me._parameters = params.AsImmutableOrNull()

                Me._associatedProperty = associatedProperty
                Me._asyncKickoffMethod = asyncKickoffMethod
            End Sub

            Friend Overrides ReadOnly Property TypeMap As TypeSubstitution
                Get
                    Return Nothing
                End Get
            End Property

            Friend Overrides Sub AddSynthesizedAttributes(ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
                MyBase.AddSynthesizedAttributes(attributes)

                Dim compilation = Me.DeclaringCompilation

                If (Me._debugAttributes And DebugAttributes.CompilerGeneratedAttribute) <> 0 Then
                    Debug.Assert(
                        WellKnownMembers.IsSynthesizedAttributeOptional(
                            WellKnownMember.System_Diagnostics_DebuggerNonUserCodeAttribute__ctor))

                    AddSynthesizedAttribute(attributes, compilation.SynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor))
                End If

                If (Me._debugAttributes And DebugAttributes.DebuggerHiddenAttribute) <> 0 Then
                    Debug.Assert(
                        WellKnownMembers.IsSynthesizedAttributeOptional(
                            WellKnownMember.System_Diagnostics_DebuggerNonUserCodeAttribute__ctor))

                    AddSynthesizedAttribute(attributes, compilation.SynthesizeDebuggerHiddenAttribute())
                End If

                If (Me._debugAttributes And DebugAttributes.DebuggerNonUserCodeAttribute) <> 0 Then
                    Debug.Assert(
                        WellKnownMembers.IsSynthesizedAttributeOptional(
                            WellKnownMember.System_Diagnostics_DebuggerNonUserCodeAttribute__ctor))

                    AddSynthesizedAttribute(attributes, compilation.SynthesizeDebuggerNonUserCodeAttribute())
                End If
            End Sub

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

            Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
                Get
                    Return Me._enableDebugInfo
                End Get
            End Property

            Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of MethodSymbol)
                Get
                    Return ImmutableArray.Create(Of MethodSymbol)(Me._interfaceMethod)
                End Get
            End Property

            Public Overrides ReadOnly Property AssociatedSymbol As Symbol
                Get
                    Return Me._associatedProperty
                End Get
            End Property

            Friend Overrides ReadOnly Property AsyncKickoffMethod As MethodSymbol
                Get
                    Return Me._asyncKickoffMethod
                End Get
            End Property

            Friend Overrides Function IsMetadataNewSlot(Optional ignoreInterfaceImplementationChanges As Boolean = False) As Boolean
                Return True
            End Function

            Public ReadOnly Property HasMethodBodyDependency As Boolean Implements ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency
                Get
                    Return _hasMethodBodyDependency
                End Get
            End Property

            Public ReadOnly Property Method As IMethodSymbol Implements ISynthesizedMethodBodyImplementationSymbol.Method
                Get
                    Dim symbol As ISynthesizedMethodBodyImplementationSymbol = CType(ContainingSymbol, ISynthesizedMethodBodyImplementationSymbol)
                    Return symbol.Method
                End Get
            End Property
        End Class
    End Class
End Namespace
