' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator
    Friend NotInheritable Class EEConstructorSymbol
        Inherits SynthesizedSimpleConstructorSymbol

        Public Sub New(container As EENamedTypeSymbol)
            MyBase.New(container)
        End Sub

        Friend Overrides Function GetBoundMethodBody(compilationState As TypeCompilationState, diagnostics As BindingDiagnosticBag, <Out> ByRef Optional methodBodyBinder As Binder = Nothing) As BoundBlock
            Return New BoundBlock(
                Me.Syntax,
                Nothing,
                ImmutableArray(Of LocalSymbol).Empty,
                ImmutableArray.Create(Of BoundStatement)(
                    MethodCompiler.BindDefaultConstructorInitializer(Me, diagnostics),
                    New BoundReturnStatement(Me.Syntax, Nothing, Nothing, Nothing)))

        End Function
    End Class
End Namespace
