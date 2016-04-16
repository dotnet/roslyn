' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Base class for all parameters that are emitted.
    ''' </summary>
    Friend MustInherit Class SourceParameterSymbolBase
        Inherits ParameterSymbol

        Private ReadOnly _containingSymbol As Symbol
        Private ReadOnly _ordinal As UShort

        Friend Sub New(containingSymbol As Symbol, ordinal As Integer)
            _containingSymbol = containingSymbol
            _ordinal = CUShort(ordinal)
        End Sub

        Public NotOverridable Overrides ReadOnly Property Ordinal As Integer
            Get
                Return _ordinal
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _containingSymbol
            End Get
        End Property

        Friend MustOverride ReadOnly Property HasParamArrayAttribute As Boolean

        Friend MustOverride ReadOnly Property HasDefaultValueAttribute As Boolean

        Friend NotOverridable Overrides Sub AddSynthesizedAttributes(compilationState as ModuleCompilationState, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
            MyBase.AddSynthesizedAttributes(compilationState, attributes)

            ' Create the ParamArrayAttribute
            If IsParamArray AndAlso Not HasParamArrayAttribute Then
                Dim compilation = Me.DeclaringCompilation
                AddSynthesizedAttribute(attributes, compilation.TrySynthesizeAttribute(
                    WellKnownMember.System_ParamArrayAttribute__ctor))
            End If

            ' Create the default attribute
            If HasExplicitDefaultValue AndAlso Not HasDefaultValueAttribute Then
                ' Synthesize DateTimeConstantAttribute or DecimalConstantAttribute when the default
                ' value is either DateTime or Decimal and there is not an explicit custom attribute.
                Dim compilation = Me.DeclaringCompilation
                Dim defaultValue = ExplicitDefaultConstantValue

                Select Case defaultValue.SpecialType
                    Case SpecialType.System_DateTime
                        AddSynthesizedAttribute(attributes, compilation.TrySynthesizeAttribute(
                            WellKnownMember.System_Runtime_CompilerServices_DateTimeConstantAttribute__ctor,
                            ImmutableArray.Create(New TypedConstant(compilation.GetSpecialType(SpecialType.System_Int64),
                                                                            TypedConstantKind.Primitive,
                                                                            defaultValue.DateTimeValue.Ticks))))

                    Case SpecialType.System_Decimal
                        AddSynthesizedAttribute(attributes, compilation.SynthesizeDecimalConstantAttribute(defaultValue.DecimalValue))
                End Select
            End If
        End Sub

        Friend Overrides ReadOnly Property CountOfCustomModifiersPrecedingByRef As UShort
            Get
                Return 0
            End Get
        End Property

        Friend MustOverride Function WithTypeAndCustomModifiers(type As TypeSymbol, customModifiers As ImmutableArray(Of CustomModifier), countOfCustomModifiersPrecedingByRef As UShort) As ParameterSymbol

    End Class
End Namespace
