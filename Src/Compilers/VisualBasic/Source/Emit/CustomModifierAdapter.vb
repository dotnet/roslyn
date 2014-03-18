' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Partial Class VisualBasicCustomModifier
        Implements Microsoft.Cci.ICustomModifier

        Private ReadOnly Property CciIsOptional As Boolean Implements Microsoft.Cci.ICustomModifier.IsOptional
            Get
                Return Me.IsOptional
            End Get
        End Property

        Private Function CciGetModifier(context As Microsoft.CodeAnalysis.Emit.Context) As ITypeReference Implements Microsoft.Cci.ICustomModifier.GetModifier
            Debug.Assert(Me.Modifier Is Me.Modifier.OriginalDefinition)
            Return DirectCast(context.Module, PEModuleBuilder).Translate(Me.Modifier, DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), context.Diagnostics)
        End Function
    End Class
End Namespace
