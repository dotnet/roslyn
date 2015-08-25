// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.EngineV1;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    public class DiagnosticStateTests
    {
        [Fact, Trait(Roslyn.Test.Utilities.Traits.Feature, Roslyn.Test.Utilities.Traits.Features.Diagnostics)]
        public void SerializationTest_Document()
        {
            using (var workspace = new TestWorkspace(TestExportProvider.ExportProviderWithCSharpAndVisualBasic, workspaceKind: "DiagnosticTest"))
            {
                var utcTime = DateTime.UtcNow;
                var version1 = VersionStamp.Create(utcTime);
                var version2 = VersionStamp.Create(utcTime.AddDays(1));

                var document = workspace.CurrentSolution.AddProject("TestProject", "TestProject", LanguageNames.CSharp).AddDocument("TestDocument", "");
                var diagnostics = new[]
                {
                    new DiagnosticData(
                        "test1", "Test", "test1 message", "test1 message format",
                        DiagnosticSeverity.Info, DiagnosticSeverity.Info, false, 1,
                        ImmutableArray<string>.Empty, ImmutableDictionary<string, string>.Empty,
                        workspace, document.Project.Id, new DiagnosticDataLocation(document.Id,
                            new TextSpan(10, 20), "originalFile1", 30, 30, 40, 40, "mappedFile1", 10, 10, 20, 20)),
                    new DiagnosticData(
                        "test2", "Test", "test2 message", "test2 message format",
                        DiagnosticSeverity.Warning, DiagnosticSeverity.Warning, true, 0,
                        ImmutableArray.Create<string>("Test2"), ImmutableDictionary<string, string>.Empty.Add("propertyKey", "propertyValue"),
                        workspace, document.Project.Id, new DiagnosticDataLocation(document.Id,
                            new TextSpan(30, 40), "originalFile2", 70, 70, 80, 80, "mappedFile2", 50, 50, 60, 60), title: "test2 title", description: "test2 description", helpLink: "http://test2link"),
                    new DiagnosticData(
                        "test3", "Test", "test3 message", "test3 message format",
                        DiagnosticSeverity.Error, DiagnosticSeverity.Warning, true, 2,
                        ImmutableArray.Create<string>("Test3", "Test3_2"), ImmutableDictionary<string, string>.Empty.Add("p1Key", "p1Value").Add("p2Key", "p2Value"),
                        workspace, document.Project.Id, new DiagnosticDataLocation(document.Id,
                            new TextSpan(50, 60), "originalFile3", 110, 110, 120, 120, "mappedFile3", 90, 90, 100, 100), title: "test3 title", description: "test3 description", helpLink: "http://test3link"),
                };

                var original = new DiagnosticIncrementalAnalyzer.AnalysisData(version1, version2, diagnostics.ToImmutableArray());
                var state = new DiagnosticIncrementalAnalyzer.DiagnosticState("Test", VersionStamp.Default, LanguageNames.CSharp);
                state.PersistAsync(document, original, CancellationToken.None).Wait();

                var recovered = state.TryGetExistingDataAsync(document, CancellationToken.None).Result;

                Assert.Equal(original.TextVersion, recovered.TextVersion);
                Assert.Equal(original.DataVersion, recovered.DataVersion);

                AssertDiagnostics(original.Items, recovered.Items);
            }
        }

        [Fact, Trait(Roslyn.Test.Utilities.Traits.Feature, Roslyn.Test.Utilities.Traits.Features.Diagnostics)]
        public void SerializationTest_Project()
        {
            using (var workspace = new TestWorkspace(TestExportProvider.ExportProviderWithCSharpAndVisualBasic, workspaceKind: "DiagnosticTest"))
            {
                var utcTime = DateTime.UtcNow;
                var version1 = VersionStamp.Create(utcTime);
                var version2 = VersionStamp.Create(utcTime.AddDays(1));

                var document = workspace.CurrentSolution.AddProject("TestProject", "TestProject", LanguageNames.CSharp).AddDocument("TestDocument", "");
                var diagnostics = new[]
                {
                    new DiagnosticData(
                        "test1", "Test", "test1 message", "test1 message format",
                        DiagnosticSeverity.Info, DiagnosticSeverity.Info, false, 1,
                        ImmutableArray<string>.Empty, ImmutableDictionary<string, string>.Empty,
                        workspace, document.Project.Id, description: "test1 description", helpLink: "http://test1link"),
                    new DiagnosticData(
                        "test2", "Test", "test2 message", "test2 message format",
                        DiagnosticSeverity.Warning, DiagnosticSeverity.Warning, true, 0,
                        ImmutableArray.Create<string>("Test2"), ImmutableDictionary<string, string>.Empty.Add("p1Key", "p2Value"),
                        workspace, document.Project.Id),
                    new DiagnosticData(
                        "test3", "Test", "test3 message", "test3 message format",
                        DiagnosticSeverity.Error, DiagnosticSeverity.Warning, true, 2,
                        ImmutableArray.Create<string>("Test3", "Test3_2"), ImmutableDictionary<string, string>.Empty.Add("p2Key", "p2Value").Add("p1Key", "p1Value"),
                        workspace, document.Project.Id, description: "test3 description", helpLink: "http://test3link"),
                };

                var original = new DiagnosticIncrementalAnalyzer.AnalysisData(version1, version2, diagnostics.ToImmutableArray());
                var state = new DiagnosticIncrementalAnalyzer.DiagnosticState("Test", VersionStamp.Default, LanguageNames.CSharp);
                state.PersistAsync(document.Project, original, CancellationToken.None).Wait();

                var recovered = state.TryGetExistingDataAsync(document.Project, CancellationToken.None).Result;

                Assert.Equal(original.TextVersion, recovered.TextVersion);
                Assert.Equal(original.DataVersion, recovered.DataVersion);

                AssertDiagnostics(original.Items, recovered.Items);
            }
        }

        private void AssertDiagnostics(ImmutableArray<DiagnosticData> items1, ImmutableArray<DiagnosticData> items2)
        {
            Assert.Equal(items1.Length, items1.Length);

            for (var i = 0; i < items1.Length; i++)
            {
                Assert.Equal(items1[i].Id, items2[i].Id);
                Assert.Equal(items1[i].Category, items2[i].Category);
                Assert.Equal(items1[i].Message, items2[i].Message);
                Assert.Equal(items1[i].ENUMessageForBingSearch, items2[i].ENUMessageForBingSearch);
                Assert.Equal(items1[i].Severity, items2[i].Severity);
                Assert.Equal(items1[i].IsEnabledByDefault, items2[i].IsEnabledByDefault);
                Assert.Equal(items1[i].WarningLevel, items2[i].WarningLevel);
                Assert.Equal(items1[i].DefaultSeverity, items2[i].DefaultSeverity);

                Assert.Equal(items1[i].CustomTags.Count, items2[i].CustomTags.Count);
                for (var j = 0; j < items1[i].CustomTags.Count; j++)
                {
                    Assert.Equal(items1[i].CustomTags[j], items2[i].CustomTags[j]);
                }

                Assert.Equal(items1[i].Properties.Count, items2[i].Properties.Count);
                Assert.True(items1[i].Properties.SetEquals(items2[i].Properties));

                Assert.Equal(items1[i].Workspace, items2[i].Workspace);
                Assert.Equal(items1[i].ProjectId, items2[i].ProjectId);
                Assert.Equal(items1[i].DocumentId, items2[i].DocumentId);

                Assert.Equal(items1[i].HasTextSpan, items2[i].HasTextSpan);
                if (items1[i].HasTextSpan)
                {
                    Assert.Equal(items1[i].TextSpan, items2[i].TextSpan);
                }

                Assert.Equal(items1[i].DataLocation?.MappedFilePath, items2[i].DataLocation?.MappedFilePath);
                Assert.Equal(items1[i].DataLocation?.MappedStartLine, items2[i].DataLocation?.MappedStartLine);
                Assert.Equal(items1[i].DataLocation?.MappedStartColumn, items2[i].DataLocation?.MappedStartColumn);
                Assert.Equal(items1[i].DataLocation?.MappedEndLine, items2[i].DataLocation?.MappedEndLine);
                Assert.Equal(items1[i].DataLocation?.MappedEndColumn, items2[i].DataLocation?.MappedEndColumn);

                Assert.Equal(items1[i].DataLocation?.OriginalFilePath, items2[i].DataLocation?.OriginalFilePath);
                Assert.Equal(items1[i].DataLocation?.OriginalStartLine, items2[i].DataLocation?.OriginalStartLine);
                Assert.Equal(items1[i].DataLocation?.OriginalStartColumn, items2[i].DataLocation?.OriginalStartColumn);
                Assert.Equal(items1[i].DataLocation?.OriginalEndLine, items2[i].DataLocation?.OriginalEndLine);
                Assert.Equal(items1[i].DataLocation?.OriginalEndColumn, items2[i].DataLocation?.OriginalEndColumn);

                Assert.Equal(items1[i].Description, items2[i].Description);
                Assert.Equal(items1[i].HelpLink, items2[i].HelpLink);
            }
        }

        [ExportWorkspaceServiceFactory(typeof(IPersistentStorageService), "DiagnosticTest"), Shared]
        public class PersistentStorageServiceFactory : IWorkspaceServiceFactory
        {
            public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            {
                return new Service();
            }

            public class Service : IPersistentStorageService
            {
                private readonly Storage _instance = new Storage();

                IPersistentStorage IPersistentStorageService.GetStorage(Solution solution)
                {
                    return _instance;
                }

                internal class Storage : IPersistentStorage
                {
                    private readonly Dictionary<object, Stream> _map = new Dictionary<object, Stream>();

                    public Task<Stream> ReadStreamAsync(string name, CancellationToken cancellationToken = default(CancellationToken))
                    {
                        var stream = _map[name];
                        stream.Position = 0;

                        return Task.FromResult(stream);
                    }

                    public Task<Stream> ReadStreamAsync(Project project, string name, CancellationToken cancellationToken = default(CancellationToken))
                    {
                        var stream = _map[Tuple.Create(project, name)];
                        stream.Position = 0;

                        return Task.FromResult(stream);
                    }

                    public Task<Stream> ReadStreamAsync(Document document, string name, CancellationToken cancellationToken = default(CancellationToken))
                    {
                        var stream = _map[Tuple.Create(document, name)];
                        stream.Position = 0;

                        return Task.FromResult(stream);
                    }

                    public Task<bool> WriteStreamAsync(string name, Stream stream, CancellationToken cancellationToken = default(CancellationToken))
                    {
                        _map[name] = new MemoryStream();
                        stream.CopyTo(_map[name]);

                        return SpecializedTasks.True;
                    }

                    public Task<bool> WriteStreamAsync(Project project, string name, Stream stream, CancellationToken cancellationToken = default(CancellationToken))
                    {
                        _map[Tuple.Create(project, name)] = new MemoryStream();
                        stream.CopyTo(_map[Tuple.Create(project, name)]);

                        return SpecializedTasks.True;
                    }

                    public Task<bool> WriteStreamAsync(Document document, string name, Stream stream, CancellationToken cancellationToken = default(CancellationToken))
                    {
                        _map[Tuple.Create(document, name)] = new MemoryStream();
                        stream.CopyTo(_map[Tuple.Create(document, name)]);

                        return SpecializedTasks.True;
                    }

                    protected virtual void Dispose(bool disposing)
                    {
                    }

                    public void Dispose()
                    {
                        Dispose(true);
                    }
                }
            }
        }
    }
}
