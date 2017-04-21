' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend MustInherit Class SynthesizedPropertyAccessorBase(Of T As PropertySymbol)
        Inherits SynthesizedAccessor(Of T)

        Protected Sub New(container As NamedTypeSymbol, [property] As T)
            MyBase.New(container, [property])
        End Sub

        Friend MustOverride ReadOnly Property BackingFieldSymbol As FieldSymbol

        Friend Overloads Overrides Function GetBoundMethodBody(compilationState As TypeCompilationState, diagnostics As DiagnosticBag, Optional ByRef methodBodyBinder As Binder = Nothing) As BoundBlock
            Return SynthesizedPropertyAccessorHelper.GetBoundMethodBody(Me, Me.BackingFieldSymbol, methodBodyBinder)
        End Function

    End Class
End Namespace
