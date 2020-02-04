' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Partial Friend Module INamespaceOrTypeSymbolExtensions
        <Extension>
        Public Function GenerateTypeSyntax(symbol As INamespaceOrTypeSymbol, Optional addGlobal As Boolean = True) As TypeSyntax
            Return symbol.Accept(TypeSyntaxGeneratorVisitor.Create(addGlobal)).WithAdditionalAnnotations(Simplifier.Annotation)
        End Function
    End Module
End Namespace
