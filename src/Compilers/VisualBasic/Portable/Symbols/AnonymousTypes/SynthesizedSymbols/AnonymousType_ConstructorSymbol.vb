' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Partial Friend NotInheritable Class AnonymousTypeManager

        Partial Private NotInheritable Class AnonymousTypeConstructorSymbol
            Inherits SynthesizedConstructorBase

            Private _parameters As ImmutableArray(Of ParameterSymbol)

            Public Sub New(container As AnonymousTypeTemplateSymbol)
                MyBase.New(VisualBasicSyntaxTree.DummyReference, container, False, Nothing, Nothing)

                '  Create constructor parameters
                Dim fieldsCount As Integer = container.Properties.Length
                Dim paramsArr = New ParameterSymbol(fieldsCount - 1) {}
                For index = 0 To fieldsCount - 1
                    Dim [property] As PropertySymbol = container.Properties(index)
                    paramsArr(index) = New SynthesizedParameterSimpleSymbol(Me, [property].Type, index, [property].Name)
                Next
                Me._parameters = paramsArr.AsImmutableOrNull()
            End Sub

            Friend Overrides ReadOnly Property ParameterCount As Integer
                Get
                    Return Me._parameters.Length
                End Get
            End Property

            Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
                Get
                    Return Me._parameters
                End Get
            End Property

            Friend Overrides Sub AddSynthesizedAttributes(compilationState As ModuleCompilationState, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
                MyBase.AddSynthesizedAttributes(compilationState, attributes)

                Dim compilation = DirectCast(Me.ContainingType, AnonymousTypeTemplateSymbol).Manager.Compilation
                AddSynthesizedAttribute(attributes, compilation.SynthesizeDebuggerHiddenAttribute())
            End Sub

            Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
                Get
                    Return False
                End Get
            End Property

            Friend Overrides Function CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer
                Throw ExceptionUtilities.Unreachable
            End Function
        End Class
    End Class
End Namespace
