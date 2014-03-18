' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions

    Friend Module INamespaceOrTypeSymbolExtensions
        <Extension>
        Public Function GenerateTypeSyntax(symbol As INamespaceOrTypeSymbol, Optional addGlobal As Boolean = True) As TypeSyntax
            Return symbol.Accept(New TypeSyntaxGeneratorVisitor(addGlobal)).WithAdditionalAnnotations(Simplifier.Annotation)
        End Function
    End Module

End Namespace