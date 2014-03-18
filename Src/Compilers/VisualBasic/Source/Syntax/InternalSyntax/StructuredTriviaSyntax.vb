' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.CompilerServices
Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Partial Friend MustInherit Class StructuredTriviaSyntax
        Friend Sub New(reader As ObjectReader)
            MyBase.New(reader)
            Me.SetFlags(NodeFlags.ContainsStructuredTrivia)
        End Sub

        Friend Sub New(ByVal kind As SyntaxKind)
            MyBase.New(kind)
            Me.SetFlags(NodeFlags.ContainsStructuredTrivia)
        End Sub

        Friend Sub New(ByVal kind As SyntaxKind, context As ISyntaxFactoryContext)
            MyBase.New(kind)
            Me.SetFlags(NodeFlags.ContainsStructuredTrivia)
            Me.SetFactoryContext(context)
        End Sub

        Friend Sub New(ByVal kind As SyntaxKind, ByVal errors As DiagnosticInfo(), ByVal annotations As SyntaxAnnotation())
            MyBase.New(kind, errors, annotations)
            Me.SetFlags(NodeFlags.ContainsStructuredTrivia)
        End Sub

    End Class
End Namespace

