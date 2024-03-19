' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
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

        Friend NotOverridable Overrides Sub AddSynthesizedAttributes(moduleBuilder As PEModuleBuilder, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
            MyBase.AddSynthesizedAttributes(moduleBuilder, attributes)

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

            If Me.Type.ContainsTupleNames() Then
                AddSynthesizedAttribute(attributes, DeclaringCompilation.SynthesizeTupleNamesAttribute(Type))
            End If
        End Sub

        Friend MustOverride Function WithTypeAndCustomModifiers(type As TypeSymbol, customModifiers As ImmutableArray(Of CustomModifier), refCustomModifiers As ImmutableArray(Of CustomModifier)) As ParameterSymbol

    End Class
End Namespace
