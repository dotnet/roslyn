' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend MustInherit Class SynthesizedPropertyAccessorBase(Of T As PropertySymbol)
        Inherits SynthesizedAccessor(Of T)

        Protected Sub New(container As NamedTypeSymbol, [property] As T)
            MyBase.New(container, [property])
        End Sub

        Friend MustOverride ReadOnly Property BackingFieldSymbol As FieldSymbol

        Friend Overloads Overrides Function GetBoundMethodBody(compilationState As TypeCompilationState, diagnostics As BindingDiagnosticBag, Optional ByRef methodBodyBinder As Binder = Nothing) As BoundBlock
            Return SynthesizedPropertyAccessorHelper.GetBoundMethodBody(Me, Me.BackingFieldSymbol, methodBodyBinder)
        End Function

    End Class
End Namespace
