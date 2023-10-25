' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Class to represent a synthesized attribute
    ''' </summary>
    Friend NotInheritable Class SynthesizedAttributeData
        Inherits VisualBasicAttributeData

        Private ReadOnly _compilation As VisualBasicCompilation
        Private ReadOnly _attributeConstructor As MethodSymbol
        Private ReadOnly _constructorArguments As ImmutableArray(Of TypedConstant)
        Private ReadOnly _namedArguments As ImmutableArray(Of KeyValuePair(Of String, TypedConstant))

        Friend Sub New(compilation As VisualBasicCompilation,
                       wellKnownMember As MethodSymbol,
                       arguments As ImmutableArray(Of TypedConstant),
                       namedArgs As ImmutableArray(Of KeyValuePair(Of String, TypedConstant)))

            Debug.Assert(wellKnownMember IsNot Nothing AndAlso Not arguments.IsDefault)

            Me._compilation = compilation
            Me._attributeConstructor = wellKnownMember
            Me._constructorArguments = arguments.NullToEmpty()
            Me._namedArguments = namedArgs.NullToEmpty()
        End Sub

        ''' <summary>
        ''' Synthesizes attribute data for given constructor symbol.
        ''' If the constructor has UseSiteErrors and the attribute is optional returns Nothing.
        ''' </summary>
        Friend Shared Function Create(
            compilation As VisualBasicCompilation,
            constructorSymbol As MethodSymbol,
            constructor As WellKnownMember,
            Optional arguments As ImmutableArray(Of TypedConstant) = Nothing,
            Optional namedArguments As ImmutableArray(Of KeyValuePair(Of String, TypedConstant)) = Nothing) As SynthesizedAttributeData

            ' If we reach here, unlikely a VBCore scenario.  Will result in ERR_MissingRuntimeHelper diagnostic            
            If Binder.GetUseSiteInfoForWellKnownTypeMember(constructorSymbol, constructor, False).DiagnosticInfo IsNot Nothing Then
                If WellKnownMembers.IsSynthesizedAttributeOptional(constructor) Then
                    Return Nothing
                Else
                    'UseSiteErrors for member have not been checked before emitting
                    Throw ExceptionUtilities.Unreachable
                End If
            Else
                If arguments.IsDefault Then
                    arguments = ImmutableArray(Of TypedConstant).Empty
                End If

                If namedArguments.IsDefault Then
                    namedArguments = ImmutableArray(Of KeyValuePair(Of String, TypedConstant)).Empty
                End If

                Return New SynthesizedAttributeData(compilation, constructorSymbol, arguments, namedArguments)
            End If
        End Function

        Public Overrides ReadOnly Property AttributeClass As NamedTypeSymbol
            Get
                Return _attributeConstructor.ContainingType
            End Get
        End Property

        Public Overrides ReadOnly Property AttributeConstructor As MethodSymbol
            Get
                Return _attributeConstructor
            End Get
        End Property

        Public Overrides ReadOnly Property ApplicationSyntaxReference As SyntaxReference
            Get
                Return Nothing
            End Get
        End Property

        Protected Overrides ReadOnly Property CommonConstructorArguments As ImmutableArray(Of TypedConstant)
            Get
                Return _constructorArguments
            End Get
        End Property

        Protected Overrides ReadOnly Property CommonNamedArguments As ImmutableArray(Of KeyValuePair(Of String, TypedConstant))
            Get
                Return _namedArguments
            End Get
        End Property

        Friend Overrides ReadOnly Property IsConditionallyOmitted As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property HasErrors As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property ErrorInfo As DiagnosticInfo
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides Function IsTargetAttribute(namespaceName As String, typeName As String, Optional ignoreCase As Boolean = False) As Boolean
            Return SourceAttributeData.IsTargetAttribute(AttributeClass, namespaceName, typeName, ignoreCase)
        End Function

        Friend Overrides Function GetTargetAttributeSignatureIndex(description As AttributeDescription) As Integer
            Return SourceAttributeData.GetTargetAttributeSignatureIndex(_compilation, AttributeClass, AttributeConstructor, description)
        End Function
    End Class
End Namespace
