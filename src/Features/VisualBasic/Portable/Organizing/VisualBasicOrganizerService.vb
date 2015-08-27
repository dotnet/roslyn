' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Organizing
Imports Microsoft.CodeAnalysis.Organizing.Organizers

Namespace Microsoft.CodeAnalysis.VisualBasic.Organizing
    <ExportLanguageService(GetType(IOrganizingService), LanguageNames.VisualBasic), [Shared]>
    Friend Partial Class VisualBasicOrganizingService
        Inherits AbstractOrganizingService

        <ImportingConstructor>
        Public Sub New(<ImportMany()> organizers As IEnumerable(Of Lazy(Of ISyntaxOrganizer, LanguageMetadata)))
            MyBase.New(organizers.Where(Function(o) o.Metadata.Language = LanguageNames.VisualBasic).Select(Function(o) o.Value))
        End Sub

        Protected Overrides Async Function ProcessAsync(document As Document, organizers As IEnumerable(Of ISyntaxOrganizer), cancellationToken As CancellationToken) As Task(Of Document)
            Dim root = DirectCast(Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False), SyntaxNode)
            Dim rewriter = New Rewriter(Me, organizers, Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False), cancellationToken)
            Return document.WithSyntaxRoot(rewriter.Visit(root))
        End Function
    End Class
End Namespace
