' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Emit

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Partial Friend NotInheritable Class AnonymousTypeManager

        Partial Private NotInheritable Class AnonymousTypeConstructorSymbol
            Inherits SynthesizedConstructorBase

            Private ReadOnly _parameters As ImmutableArray(Of ParameterSymbol)

            Public Sub New(container As AnonymousTypeTemplateSymbol)
                MyBase.New(VisualBasicSyntaxTree.DummyReference, container, False, Nothing, Nothing)

                '  Create constructor parameters
                Dim fieldsCount As Integer = container.Properties.Length
                Dim paramsArr = New ParameterSymbol(fieldsCount - 1) {}
                For index = 0 To fieldsCount - 1
                    Dim [property] As PropertySymbol = container.Properties(index)
                    paramsArr(index) = New AnonymousTypeOrDelegateParameterSymbol(Me, [property].Type, index, isByRef:=False, [property].Name, correspondingInvokeParameterOrProperty:=index)
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

            Friend Overrides Sub AddSynthesizedAttributes(moduleBuilder As PEModuleBuilder, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
                MyBase.AddSynthesizedAttributes(moduleBuilder, attributes)

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
