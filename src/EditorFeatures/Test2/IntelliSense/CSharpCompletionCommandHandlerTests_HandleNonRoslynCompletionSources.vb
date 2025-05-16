' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Text.[Shared].Extensions
Imports Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
Imports Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.Completion)>
    Public Class CSharpCompletionCommandHandlerTests_HandleNonRoslynCompletionSources

        <WpfFact>
        Public Async Function SingleItemFromNonRoslynSourceOnly() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
public class ItemFromRoslynSource {}
public class C
{
    void M()
    {        
        ItemFrom$$
    }
}
                              </Document>,
                              excludedTypes:={GetType(CompletionSourceProvider)}.ToList(),
                              extraExportedTypes:={GetType(MockCompletionSourceProvider)}.ToList())

                state.SendInvokeCompletionList()
                Dim session = Await state.GetCompletionSession()
                Dim items = session.GetComputedItems(CancellationToken.None).Items

                Assert.True(items.Any(Function(i) i.DisplayText = "ItemFromMockSource"))

            End Using
        End Function

        <WpfFact>
        Public Async Function HandleMultipleItemsFromBothSources() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
public class ItemFromRoslynSource {}
public class C
{
    void M()
    {        
        ItemFrom$$
    }
}
                              </Document>,
                              extraExportedTypes:={GetType(MockCompletionSourceProvider)}.ToList())

                state.SendInvokeCompletionList()
                Dim session = Await state.GetCompletionSession()
                Dim items = session.GetComputedItems(CancellationToken.None).Items

                Assert.True({"ItemFromRoslynSource", "ItemFromMockSource"}.All(Function(t) items.Any(Function(i) i.DisplayText = t)))

            End Using
        End Function

        <ComponentModel.Composition.Export(GetType(IAsyncCompletionSourceProvider))>
        <VisualStudio.Utilities.Name(NameOf(MockCompletionSourceProvider))>
        <VisualStudio.Utilities.ContentType(ContentTypeNames.RoslynContentType)>
        Private Class MockCompletionSourceProvider
            Implements IAsyncCompletionSourceProvider

            <ComponentModel.Composition.ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public Function GetOrCreate(textView As ITextView) As IAsyncCompletionSource Implements IAsyncCompletionSourceProvider.GetOrCreate
                Return New MockCompletionSource()
            End Function
        End Class

        Private Class MockCompletionSource
            Implements IAsyncCompletionSource

            Public Function GetCompletionContextAsync(session As IAsyncCompletionSession, trigger As CompletionTrigger, triggerLocation As SnapshotPoint, applicableToSpan As SnapshotSpan, token As CancellationToken) As Task(Of CompletionContext) Implements IAsyncCompletionSource.GetCompletionContextAsync
                Dim item = New CompletionItem("ItemFromMockSource", Me)
                Dim itemsList = session.CreateCompletionList({item})
                Return Task.FromResult(New CompletionContext(itemsList, suggestionItemOptions:=Nothing, InitialSelectionHint.RegularSelection, ImmutableArray(Of CompletionFilterWithState).Empty, isIncomplete:=False, Nothing))
            End Function

            Public Function GetDescriptionAsync(session As IAsyncCompletionSession, item As CompletionItem, token As CancellationToken) As Task(Of Object) Implements IAsyncCompletionSource.GetDescriptionAsync
                Throw New NotImplementedException()
            End Function

            Public Function InitializeCompletion(trigger As CompletionTrigger, triggerLocation As SnapshotPoint, token As CancellationToken) As CompletionStartData Implements IAsyncCompletionSource.InitializeCompletion
                Dim document = triggerLocation.Snapshot.GetOpenDocumentInCurrentContextWithChanges()
                Dim sourceText = document.GetTextSynchronously(CancellationToken.None)
                Dim span = New SnapshotSpan(triggerLocation.Snapshot, CodeAnalysis.Completion.CommonCompletionUtilities.GetWordSpan(sourceText, triggerLocation.Position,
                                                                                                                                    Function(c) Char.IsLetter(c), Function(c) Char.IsLetterOrDigit(c)).ToSpan())
                Return New CompletionStartData(CompletionParticipation.ProvidesItems, span)
            End Function
        End Class
    End Class
End Namespace
