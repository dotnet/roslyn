' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Emit

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Partial Friend Class VisualBasicCustomModifier
        Implements Cci.ICustomModifier

        Private ReadOnly Property CciIsOptional As Boolean Implements Cci.ICustomModifier.IsOptional
            Get
                Return Me.IsOptional
            End Get
        End Property

        Private Function CciGetModifier(context As EmitContext) As Cci.ITypeReference Implements Cci.ICustomModifier.GetModifier
            Return DirectCast(context.Module, PEModuleBuilder).Translate(Me.ModifierSymbol, DirectCast(context.SyntaxNode, VisualBasicSyntaxNode), context.Diagnostics)
        End Function
    End Class
End Namespace
