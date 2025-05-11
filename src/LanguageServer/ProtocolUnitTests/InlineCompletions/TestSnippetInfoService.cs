// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.InlineCompletions;
using Microsoft.CodeAnalysis.Snippets;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

[ExportLanguageService(typeof(ISnippetInfoService), LanguageNames.CSharp), Shared, PartNotDiscoverable]
internal sealed class TestSnippetInfoService : ISnippetInfoService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public TestSnippetInfoService()
    {
    }

    public IEnumerable<SnippetInfo> GetSnippetsIfAvailable()
    {
        var snippetsFile = Path.Combine(Directory.GetCurrentDirectory(), "InlineCompletions", "TestSnippets.snippet");
        if (!File.Exists(snippetsFile))
        {
            throw new InvalidOperationException($"Could not find test snippets file at {snippetsFile}");
        }

        var testSnippetsXml = XDocument.Load(snippetsFile);
        var snippets = XmlSnippetParser.CodeSnippet.ReadSnippets(testSnippetsXml);
        Contract.ThrowIfNull(snippets);

        var snippetInfos = snippets.Value.Select(s => new SnippetInfo(s.Shortcut, s.Title, s.Title, snippetsFile));
        return snippetInfos;
    }

    public bool ShouldFormatSnippet(SnippetInfo snippetInfo)
    {
        throw new NotImplementedException();
    }

    public bool SnippetShortcutExists_NonBlocking(string? shortcut)
    {
        throw new NotImplementedException();
    }
}
