' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend NotInheritable Class AsyncRewriter
        Inherits StateMachineRewriter(Of AsyncStateMachineTypeSymbol, CapturedSymbolOrExpression)

        Friend NotInheritable Class AsyncStateMachineTypeSymbol
            Inherits AbstractStateMachineTypeSymbol

            Private ReadOnly _constructor As SynthesizedSimpleConstructorSymbol

            Protected Friend Sub New(topLevelMethod As MethodSymbol,
                                     typeIndex As Integer,
                                     valueTypeSymbol As NamedTypeSymbol,
                                     iAsyncStateMachineInterface As NamedTypeSymbol)

                MyBase.New(topLevelMethod,
                           GeneratedNames.MakeStateMachineTypeName(typeIndex, topLevelMethod.Name),
                           valueTypeSymbol,
                           ImmutableArray.Create(Of NamedTypeSymbol)(iAsyncStateMachineInterface))

                Me._constructor = New SynthesizedSimpleConstructorSymbol(Me)
                Me._constructor.SetParameters(ImmutableArray(Of ParameterSymbol).Empty)
            End Sub

            Public Overrides ReadOnly Property TypeKind As TypeKind
                Get
                    Return TypeKind.Structure
                End Get
            End Property

            Friend Overrides ReadOnly Property IsInterface As Boolean
                Get
                    Return False
                End Get
            End Property

            Protected Friend Overrides ReadOnly Property Constructor As MethodSymbol
                Get
                    Return Me._constructor
                End Get
            End Property
        End Class

    End Class

End Namespace


