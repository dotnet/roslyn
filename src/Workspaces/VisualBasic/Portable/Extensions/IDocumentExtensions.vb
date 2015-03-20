' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Friend Module IDocumentExtensions

        <Extension()>
        Public Async Function GetVisualBasicSyntaxTreeAsync(document As Document, Optional cancellationToken As CancellationToken = Nothing) As Task(Of SyntaxTree)
            Dim tree = Await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(False)
            Return CType(tree, SyntaxTree)
        End Function

        <Extension()>
        Public Async Function GetVisualBasicSemanticModelAsync(document As Document, Optional cancellationToken As CancellationToken = Nothing) As Task(Of SemanticModel)
            Dim model = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Return CType(model, SemanticModel)
        End Function

        <Extension()>
        Public Async Function GetVisualBasicSemanticModelForNodeAsync(document As Document, node As SyntaxNode, Optional cancellationToken As CancellationToken = Nothing) As Task(Of SemanticModel)
            Dim model = Await document.GetSemanticModelForNodeAsync(node, cancellationToken).ConfigureAwait(False)
            Return CType(model, SemanticModel)
        End Function

        <Extension()>
        Public Async Function GetVisualBasicSemanticModelForSpanAsync(document As Document, span As TextSpan, Optional cancellationToken As CancellationToken = Nothing) As Task(Of SemanticModel)
            Dim model = Await document.GetSemanticModelForSpanAsync(span, cancellationToken).ConfigureAwait(False)
            Return CType(model, SemanticModel)
        End Function

        <Extension()>
        Public Async Function GetVisualBasicCompilationAsync(document As Document, Optional cancellationToken As CancellationToken = Nothing) As Task(Of VisualBasicCompilation)
            Dim compilation = Await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(False)
            Return CType(compilation, VisualBasicCompilation)
        End Function

        <Extension()>
        Public Async Function GetVisualBasicSyntaxRootAsync(document As Document, Optional cancellationToken As CancellationToken = Nothing) As Task(Of CompilationUnitSyntax)
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Return CType(root, CompilationUnitSyntax)
        End Function
    End Module
End Namespace
