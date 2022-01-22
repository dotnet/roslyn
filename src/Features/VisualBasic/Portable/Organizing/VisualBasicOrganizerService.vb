' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Organizing
Imports Microsoft.CodeAnalysis.Organizing.Organizers

Namespace Microsoft.CodeAnalysis.VisualBasic.Organizing
    <ExportLanguageService(GetType(IOrganizingService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicOrganizingService
        Inherits AbstractOrganizingService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
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
