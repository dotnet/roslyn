' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Partial Friend NotInheritable Class AnonymousTypeManager

        Partial Private Class AnonymousTypeConstructorSymbol
            Inherits SynthesizedConstructorBase

            Private m_parameters As ImmutableArray(Of ParameterSymbol)

            Public Sub New(container As AnonymousTypeTemplateSymbol)
                MyBase.New(VisualBasic.VBSyntaxTree.Dummy.GetRoot(), container, False, Nothing, Nothing)

                '  Create constructor parameters
                Dim fieldsCount As Integer = container.Properties.Length
                Dim paramsArr = New ParameterSymbol(fieldsCount - 1) {}
                For index = 0 To fieldsCount - 1
                    Dim [property] As PropertySymbol = container.Properties(index)
                    paramsArr(index) = New SynthesizedParameterSimpleSymbol(Me, [property].Type, index, [property].Name)
                Next
                Me.m_parameters = paramsArr.AsImmutableOrNull()
            End Sub

            Friend NotOverridable Overrides ReadOnly Property ParameterCount As Integer
                Get
                    Return Me.m_parameters.Length
                End Get
            End Property

            Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
                Get
                    Return Me.m_parameters
                End Get
            End Property

            Friend Overrides Sub AddSynthesizedAttributes(ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
                MyBase.AddSynthesizedAttributes(attributes)

                Dim compilation = DirectCast(Me.ContainingType, AnonymousTypeTemplateSymbol).Manager.Compilation
                AddSynthesizedAttribute(attributes, compilation.SynthesizeDebuggerHiddenAttribute())
            End Sub
        End Class
    End Class
End Namespace