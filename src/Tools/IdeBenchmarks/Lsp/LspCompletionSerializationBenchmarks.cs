// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.LanguageServer.UnitTests.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using Roslyn.Test.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using static Roslyn.Test.Utilities.AbstractLanguageServerProtocolTests;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.PooledObjects;

namespace IdeBenchmarks.Lsp
{
    [MemoryDiagnoser]
    public class LspCompletionSerializationBenchmarks : AbstractLanguageServerProtocolTests
    {
        protected override TestComposition Composition => FeaturesLspComposition;
        private readonly UseExportProviderAttribute _useExportProviderAttribute = new();

        private LSP.CompletionList? _list;

        public LspCompletionSerializationBenchmarks() : base(null)
        {
        }

        [GlobalSetup]
        public void GlobalSetup()
        {
        }

        [IterationSetup]
        public void IterationSetup()
        {
            _useExportProviderAttribute.Before(null);
            LoadSolutionAsync().Wait();
        }

        [IterationCleanup]
        public void CleanupAsync()
        {
            _useExportProviderAttribute.Before(null);
        }

        private async Task LoadSolutionAsync()
        {
            var markup =
@"using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
class A
{
    void M()
    {
        {|caret:|}
    }
}";
            await using var testServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace: false, new LSP.VSInternalClientCapabilities
            {
                TextDocument = new LSP.TextDocumentClientCapabilities
                {
                    Completion = new LSP.CompletionSetting
                    {
                        CompletionListSetting = new LSP.CompletionListSetting
                        {
                            ItemDefaults = new string[] { "editRange", "commitCharacters", "data" },
                        }
                    }
                }
            }).ConfigureAwait(false);

            var globalOptions = testServer.TestWorkspace.GetService<IGlobalOptionService>();
            globalOptions.SetGlobalOption(LspOptionsStorage.MaxCompletionListSize, -1);

            var caret = testServer.GetLocations("caret").Single();
            var completionParams = new LSP.CompletionParams()
            {
                TextDocument = CreateTextDocumentIdentifier(caret.Uri),
                Position = caret.Range.Start,
                Context = new LSP.CompletionContext()
                {
                    TriggerKind = LSP.CompletionTriggerKind.Invoked,
                }
            };

            var document = testServer.GetCurrentSolution().Projects.First().Documents.First();
            var results = await testServer.ExecuteRequestAsync<LSP.CompletionParams, LSP.CompletionList>(LSP.Methods.TextDocumentCompletionName, completionParams, CancellationToken.None);

            var list = (await CompletionTests.RunGetCompletionsAsync(testServer, completionParams));
            if (list.Items.Length == 0)
                throw new System.Exception();

            using var _ = ArrayBuilder<LSP.CompletionItem>.GetInstance(out var builder);
            while (builder.Count < 10000)
            {
                foreach (var item in list.Items)
                {
                    builder.Add(item);
                    if (item.CommitCharacters is not null || item.Data is not null)
                        throw new InvalidDataException();

                    if (builder.Count == 10000)
                        break;
                }
            }

            list.Items = builder.ToArray();
            _list = list;
        }

        [Benchmark]
        public async Task Serialization()
        {
            var serializer = new JsonSerializer();
            serializer.Formatting = Formatting.None;
            serializer.NullValueHandling = NullValueHandling.Ignore;
            serializer.DefaultValueHandling = DefaultValueHandling.Ignore;

            using var stream = new MemoryStream();
            var sw = new StreamWriter(stream);
            var jsonWriter = new JsonTextWriter(sw);
            {
                serializer.Serialize(jsonWriter, _list);
                await jsonWriter.FlushAsync();
            }

            stream.Seek(0, SeekOrigin.Begin);

            using (var sr = new StreamReader(stream))
            using (var jsonReader = new JsonTextReader(sr))
            {
                var list = serializer.Deserialize<LSP.CompletionList>(jsonReader);
                if (list!.Items.Length != _list!.Items.Length)
                    throw new System.Exception();
            }
        }

        [Fact]
        public async Task Test()
        {
            await LoadSolutionAsync();
            await Serialization();
        }
    }
}
