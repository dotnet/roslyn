// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.UnitTests.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace IdeBenchmarks.Lsp
{
    [MemoryDiagnoser]
    public class LspCompletionBenchmarks : AbstractLanguageServerProtocolTests
    {
        private readonly UseExportProviderAttribute _useExportProviderAttribute = new UseExportProviderAttribute();

        private TestLspServer? _testServer;
        private IGlobalOptionService? _globalOptionService;
        private LSP.CompletionParams? _completionParams;

        [GlobalSetup]
        public void GlobalSetup()
        {
        }

        [IterationSetup]
        public void IterationSetup() => LoadSolutionAsync().Wait();

        private async Task LoadSolutionAsync()
        {
            _useExportProviderAttribute.Before(null);

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
        T{|caret:|}
    }
}";
            _testServer = await CreateTestLspServerAsync(markup, new LSP.VSInternalClientCapabilities
            {
                TextDocument = new LSP.TextDocumentClientCapabilities
                {
                    Completion = new LSP.CompletionSetting
                    {
                        CompletionListSetting = new LSP.CompletionListSetting
                        {
                            ItemDefaults = new string[] { "editRange" },
                        }
                    }
                }
            }).ConfigureAwait(false);

            _completionParams = CreateCompletionParams(
                _testServer.GetLocations("caret").Single(),
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "T",
                triggerKind: LSP.CompletionTriggerKind.Invoked);

            _globalOptionService = _testServer.TestWorkspace.GetService<IGlobalOptionService>();
        }

        [Benchmark]
        public void GetCompletionsWithTextEdits()
        {
            var results = CompletionTests.RunGetCompletionsAsync(_testServer!, _completionParams!).Result;
            Assert.Equal(1000, results.Items.Length);
            Assert.True(results.IsIncomplete);
            Assert.NotNull(results.ItemDefaults?.EditRange);
        }

        [IterationCleanup]
        public void Cleanup()
        {
            _testServer?.Dispose();
            _useExportProviderAttribute.After(null);
        }
    }
}
