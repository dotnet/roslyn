using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.CodingConventions;
using Microsoft.VisualStudio.Composition;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.EditorConfigStorageLocation
{
    [UseExportProvider]
    public class LegacyEditorConfigDocumentOptionsProviderTests
    {
        [Fact]
        public void OrderingOfEditorConfigMaintained()
        {
            using var tempRoot = new TempRoot();
            var tempDirectory = tempRoot.CreateDirectory();

            // Write out an .editorconfig. We'll write out 100 random GUIDs
            var expectedKeysInOrder = new List<string>();

            using (var writer = new StreamWriter(tempDirectory.CreateFile(".editorconfig").Path))
            {
                writer.WriteLine("root = true");
                writer.WriteLine("[*.cs]");

                for (var i = 0; i < 100; i++)
                {
                    var key = Guid.NewGuid().ToString();
                    expectedKeysInOrder.Add(key);
                    writer.WriteLine($"{key} = value");
                }
            }

            // Create a workspace with a file in that path
            var codingConventionsCatalog = ExportProviderCache.GetOrCreateAssemblyCatalog(typeof(ICodingConventionsManager).Assembly).WithPart(typeof(MockFileWatcher));
            var exportProvider = ExportProviderCache.GetOrCreateExportProviderFactory(TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithParts(codingConventionsCatalog)).CreateExportProvider();

            using var workspace = TestWorkspace.CreateWorkspace(
                new XElement("Workspace",
                    new XElement("Project", new XAttribute("Language", "C#"),
                        new XElement("Document", new XAttribute("FilePath", tempDirectory.CreateFile("Test.cs").Path)))), exportProvider: exportProvider);

            var document = workspace.CurrentSolution.Projects.Single().Documents.Single();

            var providerFactory = workspace.ExportProvider.GetExportedValues<IDocumentOptionsProviderFactory>().OfType<LegacyEditorConfigDocumentOptionsProviderFactory>().Single();
            var provider = providerFactory.TryCreate(workspace);

            var option = new Option<List<string>>(nameof(LegacyEditorConfigDocumentOptionsProviderTests), nameof(OrderingOfEditorConfigMaintained), null, new[] { new KeysReturningStorageLocation() });
            var optionKey = new OptionKey(option);

            // Fetch the underlying option order with a "option" that returns the keys
            provider.GetOptionsForDocumentAsync(document, CancellationToken.None).Result.TryGetDocumentOption(optionKey, workspace.Options, out var actualKeysInOrderObject);

            var actualKeysInOrder = Assert.IsAssignableFrom<IEnumerable<string>>(actualKeysInOrderObject);

            Assert.Equal(expectedKeysInOrder, actualKeysInOrder);
        }

        [PartNotDiscoverable]
        [Export(typeof(IFileWatcher))]
        [Shared]
        private class MockFileWatcher : IFileWatcher
        {
#pragma warning disable CS0067 // the event is unused

            public event ConventionsFileChangedAsyncEventHandler ConventionFileChanged;
            public event ContextFileMovedAsyncEventHandler ContextFileMoved;

#pragma warning restore CS0067

            public void Dispose()
            {
            }

            public void StartWatching(string fileName, string directoryPath)
            {
            }

            public void StopWatching(string fileName, string directoryPath)
            {
            }
        }

        /// <summary>
        /// An option storage location that returns as the value all the keys in the order they came from the underlying storage.
        /// </summary>
        private class KeysReturningStorageLocation : OptionStorageLocation, IEditorConfigStorageLocation
        {
            public bool TryGetOption(object underlyingOption, IReadOnlyDictionary<string, string> rawOptions, Type type, out object value)
            {
                value = rawOptions.Keys;

                return true;
            }
        }
    }
}
