' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Emit

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend Partial Class VisualBasicCustomModifier
        Implements Cci.ICustomModifier

        Private ReadOnly Property CciIsOptional As Boolean Implements Cci.ICustomModifier.IsOptional
            Get
                Return Me.IsOptional
            End Get
        End Property

        Private Function CciGetModifier(context As EmitContext) As Cci.ITypeReference Implements Cci.ICustomModifier.GetModifier
            Return DirectCast(context.Module, PEModuleBuilder).Translate(Me.Modifier, DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), context.Diagnostics)
        End Function
    End Class
End Namespace
