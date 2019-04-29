// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.LanguageServer.CustomProtocol;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Text.Adornments;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests
{
    [UseExportProvider]
    public abstract class AbstractLanguageServerProtocolTests
    {
        protected virtual ExportProvider GetExportProvider()
        {
            var requestHelperTypes = DesktopTestHelpers.GetAllTypesImplementingGivenInterface(
                    typeof(IRequestHandler).Assembly, typeof(IRequestHandler));
            var exportProviderFactory = ExportProviderCache.GetOrCreateExportProviderFactory(
                TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic
                .WithPart(typeof(LanguageServerProtocol))
                .WithParts(requestHelperTypes));
            return exportProviderFactory.CreateExportProvider();
        }

        protected static void AssertSymbolInformationsEqual(LSP.SymbolInformation expected, LSP.SymbolInformation actual)
        {
            if (expected is null)
            {
                Assert.Null(actual);
                return;
            }

            Assert.Equal(expected.Kind, actual.Kind);
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.Location, actual.Location);
        }

        protected static void AssertMarkupContentsEqual(LSP.MarkupContent expected, LSP.MarkupContent actual)
        {
            if (expected is null)
            {
                Assert.Null(actual);
                return;
            }

            Assert.Equal(expected.Kind, actual.Kind);
            Assert.Equal(expected.Value, actual.Value);
        }

        protected static void AssertTextEditsEqual(LSP.TextEdit expected, LSP.TextEdit actual)
        {
            if (expected is null)
            {
                Assert.Null(actual);
                return;
            }

            Assert.Equal(expected.NewText, actual.NewText);
            Assert.Equal(expected.Range, actual.Range);
        }

        protected static void AssertCompletionItemsEqual(LSP.VSCompletionItem expected, LSP.VSCompletionItem actual, bool isResolved)
        {
            Assert.Equal(expected.FilterText, actual.FilterText);
            Assert.Equal(expected.InsertText, actual.InsertText);
            Assert.Equal(expected.Label, actual.Label);
            Assert.Equal(expected.InsertTextFormat, actual.InsertTextFormat);
            Assert.Equal(expected.Kind, actual.Kind);

            AssertTextEditsEqual(expected.TextEdit, actual.TextEdit);
            AssertIconsEqual(expected.Icon, actual.Icon);

            if (isResolved)
            {
                Assert.Equal(expected.Detail, actual.Detail);
                AssertMarkupContentsEqual(expected.Documentation, actual.Documentation);
                AssertCollectionsEqual(expected.Description.Runs, actual.Description.Runs, AssertClassifiedTextRunsEqual);
            }

            // local functions
            static void AssertClassifiedTextRunsEqual(ClassifiedTextRun expectedRun, ClassifiedTextRun actualRun)
            {
                Assert.Equal(expectedRun.Text, actualRun.Text);
                Assert.Equal(expectedRun.ClassificationTypeName, actualRun.ClassificationTypeName);
            }

            static void AssertIconsEqual(ImageElement expectedIcon, ImageElement actualIcon)
            {
                if (expectedIcon != null)
                {
                    Assert.Equal(expectedIcon.AutomationName, actualIcon.AutomationName);
                    Assert.Equal(expectedIcon.ImageId.Guid, actualIcon.ImageId.Guid);
                    Assert.Equal(expectedIcon.ImageId.Id, actualIcon.ImageId.Id);
                }
            }
        }

        /// <summary>
        /// Assert that two location lists are equivalent.
        /// Locations are not returned in a consistent order, so they must be sorted.
        /// </summary>
        protected static void AssertLocationsEqual(IEnumerable<LSP.Location> expectedLocations, IEnumerable<LSP.Location> actualLocations)
        {
            AssertCollectionsEqual(expectedLocations, actualLocations.Select(loc => (object)loc), Assert.Equal, CompareLocations);

            // local functions
            static int CompareLocations(LSP.Location l1, LSP.Location l2)
            {
                var compareDocument = l1.Uri.OriginalString.CompareTo(l2.Uri.OriginalString);
                var compareRange = CompareRange(l1.Range, l2.Range);
                return compareDocument != 0 ? compareDocument : compareRange;
            }
        }

        protected static int CompareRange(LSP.Range r1, LSP.Range r2)
        {
            var compareLine = r1.Start.Line.CompareTo(r2.Start.Line);
            var compareChar = r1.Start.Character.CompareTo(r2.Start.Character);
            return compareLine != 0 ? compareLine : compareChar;
        }

        /// <summary>
        /// Asserts that two collections are equal.
        /// Optionally uses a sorting function before comparing the lists.
        /// </summary>
        protected static void AssertCollectionsEqual<T>(IEnumerable<T> expected, IEnumerable<object> actual, Action<T, T> assertionFunction, Func<T, T, int> compareFunc = null)
        {
            Assert.Equal(expected.Count(), actual.Count());
            var actualWithType = actual.Select(actualObject => (T)actualObject);

            var expectedResult = compareFunc != null ? expected.OrderBy((T t1, T t2) => compareFunc(t1, t2)).ToList() : expected.ToList();
            var actualResult = compareFunc != null ? actualWithType.OrderBy((T t1, T t2) => compareFunc(t1, t2)).ToList() : actualWithType.ToList();
            for (var i = 0; i < actualResult.Count; i++)
            {
                assertionFunction(expectedResult[i], actualResult[i]);
            }
        }

        protected static string ApplyTextEdits(LSP.TextEdit[] edits, SourceText originalMarkup)
        {
            var text = originalMarkup;
            foreach (var edit in edits)
            {
                var lines = text.Lines;
                var startPosition = ProtocolConversions.PositionToLinePosition(edit.Range.Start);
                var endPosition = ProtocolConversions.PositionToLinePosition(edit.Range.End);
                var textSpan = lines.GetTextSpan(new LinePositionSpan(startPosition, endPosition));
                text = text.Replace(textSpan, edit.NewText);
            }

            return text.ToString();
        }

        protected static LSP.SymbolInformation CreateSymbolInformation(LSP.SymbolKind kind, string name, LSP.Location location)
            => new LSP.SymbolInformation()
            {
                Kind = kind,
                Name = name,
                Location = location
            };

        protected static LSP.TextDocumentIdentifier CreateTextDocumentIdentifier(Uri uri)
            => new LSP.TextDocumentIdentifier()
            {
                Uri = uri
            };

        protected static LSP.TextDocumentPositionParams CreateTextDocumentPositionParams(LSP.Location caret)
            => new LSP.TextDocumentPositionParams()
            {
                TextDocument = CreateTextDocumentIdentifier(caret.Uri),
                Position = caret.Range.Start
            };

        protected static LSP.MarkupContent CreateMarkupContent(LSP.MarkupKind kind, string value)
            => new LSP.MarkupContent()
            {
                Kind = kind,
                Value = value
            };

        protected static LSP.CompletionParams CreateCompletionParams(LSP.Location caret)
            => new LSP.CompletionParams()
            {
                TextDocument = CreateTextDocumentIdentifier(caret.Uri),
                Position = caret.Range.Start,
                Context = new LSP.CompletionContext()
                {
                    // TODO - completion should respect context.
                }
            };

        protected static LSP.VSCompletionItem CreateCompletionItem(string text, LSP.CompletionItemKind kind, string[] tags, LSP.CompletionParams requestParameters)
            => new LSP.VSCompletionItem()
            {
                FilterText = text,
                InsertText = text,
                Label = text,
                SortText = text,
                InsertTextFormat = LSP.InsertTextFormat.Plaintext,
                Kind = kind,
                Data = new CompletionResolveData()
                {
                    DisplayText = text,
                    CompletionParams = requestParameters
                },
                Icon = tags != null ? new ImageElement(tags.ToImmutableArray().GetFirstGlyph().GetImageId()) : null
            };

        // Private procted because RunCodeActionParams is internal.
        private protected static RunCodeActionParams CreateRunCodeActionParams(LSP.Location location, string title)
            => new RunCodeActionParams()
            {
                Range = location.Range,
                TextDocument = CreateTextDocumentIdentifier(location.Uri),
                Title = title
            };


        /// <summary>
        /// Creates a solution with a document.
        /// </summary>
        /// <returns>the solution and the annotated ranges in the document.</returns>
        protected (Solution solution, Dictionary<string, IList<LSP.Location>> locations) CreateTestSolution(string markup)
            => CreateTestSolution(new string[] { markup });

        /// <summary>
        /// Create a solution with multiple documents.
        /// </summary>
        /// <returns>
        /// the solution with the documents plus a list for each document of all annotated ranges in the document.
        /// </returns>
        protected (Solution solution, Dictionary<string, IList<LSP.Location>> locations) CreateTestSolution(string[] markups)
        {
            using var workspace = TestWorkspace.CreateCSharp(markups, exportProvider: GetExportProvider());
            var solution = workspace.CurrentSolution;
            var locations = new Dictionary<string, IList<LSP.Location>>();

            foreach (var document in workspace.Documents)
            {
                var text = document.TextBuffer.AsTextContainer().CurrentText;
                foreach (var kvp in document.AnnotatedSpans)
                {
                    locations.GetOrAdd(kvp.Key, CreateLocation)
                        .AddRange(kvp.Value.Select(s => ProtocolConversions.TextSpanToLocation(s, text, new Uri(GetDocumentFilePathFromName(document.Name)))));
                }

                // Pass in the text without markup.
                workspace.ChangeSolution(ChangeDocumentFilePathToValidURI(workspace.CurrentSolution, document, text));
            }

            return (workspace.CurrentSolution, locations);

            // local functions
            static List<LSP.Location> CreateLocation(string s) => new List<LSP.Location>();
        }

        // Private protected because LanguageServerProtocol is internal
        private protected static LanguageServerProtocol GetLanguageServer(Solution solution)
        {
            var workspace = (TestWorkspace)solution.Workspace;
            return workspace.ExportProvider.GetExportedValue<LanguageServerProtocol>();
        }

        private static string GetDocumentFilePathFromName(string documentName)
            => "C:\\" + documentName;

        /// <summary>
        /// Changes the document file path.
        /// Adds/Removes the document instead of updating file path due to
        /// https://github.com/dotnet/roslyn/issues/34837
        /// </summary>
        private static Solution ChangeDocumentFilePathToValidURI(Solution originalSolution, TestHostDocument originalDocument, SourceText text)
        {
            var documentName = originalDocument.Name;
            var documentPath = GetDocumentFilePathFromName(documentName);

            var solution = originalSolution.RemoveDocument(originalDocument.Id);

            var newDocumentId = DocumentId.CreateNewId(originalDocument.Project.Id);
            return solution.AddDocument(newDocumentId, documentName, text, filePath: documentPath);
        }
    }
}
