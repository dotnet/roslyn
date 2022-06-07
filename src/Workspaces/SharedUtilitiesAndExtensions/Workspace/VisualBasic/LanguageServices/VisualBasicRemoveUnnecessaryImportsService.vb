' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Internal.Log
Imports Microsoft.CodeAnalysis.RemoveUnnecessaryImports
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryImports
    <ExportLanguageService(GetType(IRemoveUnnecessaryImportsService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend NotInheritable Class VisualBasicRemoveUnnecessaryImportsService
        Inherits AbstractRemoveUnnecessaryImportsService(Of ImportsClauseSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides ReadOnly Property UnnecessaryImportsProvider As IUnnecessaryImportsProvider
            Get
                Return VisualBasicUnnecessaryImportsProvider.Instance
            End Get
        End Property

        Public Overrides Async Function RemoveUnnecessaryImportsAsync(
                document As Document,
                predicate As Func(Of SyntaxNode, Boolean),
                formattingOptions As SyntaxFormattingOptions,
                cancellationToken As CancellationToken) As Task(Of Document)

            Contract.ThrowIfNull(formattingOptions)

            predicate = If(predicate, Functions(Of SyntaxNode).True)
            Using Logger.LogBlock(FunctionId.Refactoring_RemoveUnnecessaryImports_VisualBasic, cancellationToken)

                Dim unnecessaryImports = Await GetCommonUnnecessaryImportsOfAllContextAsync(
                    document, predicate, cancellationToken).ConfigureAwait(False)
                If unnecessaryImports.Any(Function(import) import.OverlapsHiddenPosition(cancellationToken)) Then
                    Return document
                End If

                Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)

                Dim oldRoot = DirectCast(root, CompilationUnitSyntax)
                Dim newRoot = New Rewriter(document, unnecessaryImports, cancellationToken).Visit(oldRoot)
                newRoot = newRoot.WithAdditionalAnnotations(Formatter.Annotation)

                cancellationToken.ThrowIfCancellationRequested()
                Return document.WithSyntaxRoot(newRoot)
            End Using
        End Function
    End Class
End Namespace
