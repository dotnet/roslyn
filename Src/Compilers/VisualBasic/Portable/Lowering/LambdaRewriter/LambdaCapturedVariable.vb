' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' A field of a frame class that represents a variable that has been captured in a lambda.
    ''' </summary>
    Friend NotInheritable Class LambdaCapturedVariable
        Inherits FieldSymbol

        Private ReadOnly m_frame As LambdaFrame
        Private ReadOnly m_name As String
        Private ReadOnly m_type As TypeSymbol
        Private ReadOnly m_constantValue As ConstantValue
        Private ReadOnly m_isMe As Boolean

        Friend Sub New(frame As LambdaFrame, captured As Symbol)
            Me.m_frame = frame

            ' captured symbol is either a local or parameter.
            Dim local = TryCast(captured, LocalSymbol)

            If local IsNot Nothing Then
                ' it is a local variable
                Dim localTypeAsFrame = TryCast(local.Type.OriginalDefinition, LambdaFrame)
                If localTypeAsFrame IsNot Nothing Then
                    ' if we're capturing a generic frame pointer, construct it with the new frame's type parameters
                    Me.m_type = LambdaRewriter.ConstructFrameType(localTypeAsFrame, frame.TypeArgumentsNoUseSiteDiagnostics)
                Else
                    Me.m_type = local.Type.InternalSubstituteTypeParameters(frame.TypeMap)
                End If

                If local.IsCompilerGenerated Then
                    Me.m_name = StringConstants.LiftedNonLocalPrefix & captured.Name
                Else
                    Me.m_name = StringConstants.LiftedLocalPrefix & captured.Name
                End If

                If local.IsConst Then
                    Me.m_constantValue = local.GetConstantValue(Nothing)
                End If
            Else
                ' it must be a parameter
                Dim parameter = DirectCast(captured, ParameterSymbol)
                Me.m_type = parameter.Type.InternalSubstituteTypeParameters(frame.TypeMap)

                If parameter.IsMe Then
                    Me.m_name = StringConstants.LiftedMeName
                    Me.m_isMe = True
                Else
                    Me.m_name = StringConstants.LiftedLocalPrefix & captured.Name
                End If
            End If
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return m_name
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property HasRuntimeSpecialName As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property IsNotSerialized As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingInformation As MarshalPseudoCustomAttributeData
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides ReadOnly Property TypeLayoutOffset As Integer?
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return IsConst
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Accessibility.Public
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray(Of Location).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return m_frame
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingType As NamedTypeSymbol
            Get
                Return m_frame
            End Get
        End Property

        Public Overrides ReadOnly Property IsConst As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsReadOnly As Boolean
            Get
                Return m_constantValue IsNot Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property AssociatedSymbol As Symbol
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return ImmutableArray(Of CustomModifier).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return m_type
            End Get
        End Property

        Friend Overrides Function GetConstantValue(inProgress As SymbolsInProgress(Of FieldSymbol)) As ConstantValue
            Return m_constantValue
        End Function

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCapturedFrame As Boolean
            Get
                Return Me.m_isMe
            End Get
        End Property
    End Class

End Namespace