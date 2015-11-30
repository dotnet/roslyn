' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Partial Friend MustInherit Class StructuredTriviaSyntax
        Friend Sub New(reader As ObjectReader)
            MyBase.New(reader)
            SetFlags(NodeFlags.ContainsStructuredTrivia)
        End Sub

        Friend Sub New(ByVal kind As SyntaxKind)
            MyBase.New(kind)
            SetFlags(NodeFlags.ContainsStructuredTrivia)
        End Sub

        Friend Sub New(ByVal kind As SyntaxKind, context As ISyntaxFactoryContext)
            MyBase.New(kind)
            SetFlags(NodeFlags.ContainsStructuredTrivia)
            SetFactoryContext(context)
        End Sub

        Friend Sub New(ByVal kind As SyntaxKind, ByVal errors As DiagnosticInfo(), ByVal annotations As SyntaxAnnotation())
            MyBase.New(kind, errors, annotations)
            SetFlags(NodeFlags.ContainsStructuredTrivia)
        End Sub

    End Class
End Namespace

