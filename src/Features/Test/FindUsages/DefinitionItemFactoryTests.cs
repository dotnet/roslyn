// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.CodeAnalysis.UnitTests.FindUsages;

using PartDescription = (string tag, string text, TaggedTextStyle style, string? target, string? hint);

[UseExportProvider]
public sealed class DefinitionItemFactoryTests
{
    private static string Inspect(DocumentSpan span)
        => $"{span.Document.Name} {span.SourceSpan}";

    private static string Inspect(AssemblyLocation location)
        => $"{location.Name} {location.Version} '{location.FilePath}'";

    private static string InspectFlags<TEnum>(TEnum e) where TEnum : Enum
        => string.Join(" | ", e.ToString().Split(',').Select(s => $"{typeof(TEnum).Name}.{s.Trim()}"));

    private static string Inspect(string? str)
        => (str == null) ? "null" : $"""
        "{str.Replace(@"\", @"\\").Replace("\"", "\\\"")}"
        """;

    private static string InspectValueAsExpression(string? value, IReadOnlyDictionary<string, string> expressionMap)
        => value != null && expressionMap.TryGetValue(value, out var syntax) ? syntax : Inspect(value);

    private static void VerifyParts(PartDescription[] expected, IEnumerable<TaggedText> actual, string propertyName, IReadOnlyDictionary<string, string> expressionMap)
    {
        var expectedTaggedTexts = expected.Select(t => new TaggedText(t.tag, t.text, t.style, t.target, t.hint));

        AssertEx.Equal(
            expectedTaggedTexts,
            actual,
            itemInspector: text =>
                $"(" +
                $"tag: {Inspect(text.Tag)}, " +
                $"text: {Inspect(text.Text)}, " +
                $"{InspectFlags(text.Style)}, " +
                $"target: {InspectValueAsExpression(text.NavigationTarget, expressionMap)}, " +
                $"hint: {Inspect(text.NavigationHint)}" +
                $")",
            message: PropertyMessage(propertyName));
    }

    private static void VerifyProperties(IEnumerable<(string key, string value)> expected, IReadOnlyDictionary<string, string> actual, string? propertyName, IReadOnlyDictionary<string, string> expressionMap)
        => VerifyProperties(
            expected,
            actual.Select(item => (key: item.Key, value: item.Value)).OrderBy(item => item.key),
            propertyName,
            expressionMap);

    private static void VerifyProperties(IEnumerable<(string key, string value)> expected, IEnumerable<(string key, string value)> actual, string? propertyName, IReadOnlyDictionary<string, string> expressionMap)
        => AssertEx.SetEqual(
            expected,
            actual.OrderBy(item => item.key),
            itemSeparator: "," + Environment.NewLine,
            itemInspector: item => $"({Inspect(item.key)}, {InspectValueAsExpression(item.value, expressionMap)})",
            message: PropertyMessage(propertyName));

    private static void VerifyItems(IEnumerable<string> expected, IEnumerable<string> actual, string? propertyName = null)
        => AssertEx.Equal(expected, actual, message: PropertyMessage(propertyName), itemInspector: Inspect);

    private static void VerifyDefinitionItem(
        DefinitionItem item,
        Project project,
        (ISymbol symbol, string localName)[]? symbols = null,
        PartDescription[]? displayParts = null,
        PartDescription[]? nameDisplayParts = null,
        string[]? sourceSpans = null,
        string[]? metadataLocations = null,
        string[]? tags = null,
        (string key, string value)[]? properties = null,
        (string key, string value)[]? displayableProperties = null)
    {
        var failures = new List<Exception>();
        var expressionMap = (symbols ?? []).ToDictionary(s => SymbolKey.Create(s.symbol).ToString(), s => $"{nameof(SymbolKey)}.{nameof(SymbolKey.CreateString)}({s.localName})");
        expressionMap.Add(project.Id.Id.ToString(), $"project.{nameof(Project.Id)}.{nameof(Project.Id.Id)}.{nameof(Project.Id.Id.ToString)}()");

        verify(() => VerifyParts(displayParts ?? [], item.DisplayParts, nameof(item.DisplayParts), expressionMap));
        verify(() => VerifyParts(nameDisplayParts ?? [], item.NameDisplayParts, nameof(item.NameDisplayParts), expressionMap));
        verify(() => VerifyItems(sourceSpans ?? [], item.SourceSpans.Select(Inspect), nameof(item.SourceSpans)));
        verify(() => VerifyItems(metadataLocations ?? [], item.MetadataLocations.Select(Inspect), nameof(item.MetadataLocations)));
        verify(() => VerifyItems(tags ?? [], item.Tags, nameof(item.Tags)));
        verify(() => VerifyProperties(properties ?? [], item.Properties, nameof(item.Properties), expressionMap));
        verify(() => VerifyProperties(displayableProperties ?? [], item.DisplayableProperties, nameof(item.DisplayableProperties), expressionMap));

        if (!failures.IsEmpty())
        {
            throw new AggregateException(failures);
        }

        void verify(Action assert)
        {
            try
            {
                assert();
            }
            catch (Exception e) when (e is IAssertionException)
            {
                failures.Add(e);
            }
        }
    }

    private static string? PropertyMessage(string? propertyName)
        => propertyName == null ? null : $"{Environment.NewLine}{nameof(DefinitionItem)}.{propertyName} does not match expected value.";

    [Fact]
    public async Task ToClassifiedDefinitionItemAsync_Assembly_Source()
    {
        using var workspace = TestWorkspace.CreateCSharp("class C;");

        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();
        var compilation = await project.GetCompilationAsync();
        Contract.ThrowIfNull(compilation);
        var a = compilation.Assembly;
        var classificationOptions = TestOptionsProvider.Create(ClassificationOptions.Default);
        var searchOptions = FindReferencesSearchOptions.Default;

        var item = await DefinitionItemFactory.ToClassifiedDefinitionItemAsync(a, classificationOptions, solution, searchOptions, isPrimary: true, includeHiddenLocations: true, CancellationToken.None);

        VerifyDefinitionItem(item, project, symbols: [(a, nameof(a))],
            displayParts:
            [
                (tag: "Assembly", text: "Test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", TaggedTextStyle.None, target: SymbolKey.CreateString(a), hint: "Test")
            ],
            nameDisplayParts:
            [
                (tag: "Assembly", text: "Test", TaggedTextStyle.None, target: SymbolKey.CreateString(a), hint: "Test")
            ],
            sourceSpans: [],
            tags:
            [
                "Assembly"
            ],
            properties:
            [
                ("NonNavigable", ""),
                ("Primary", ""),
            ]);
    }

    [Fact]
    public async Task ToClassifiedDefinitionItemAsync_Assembly_Metadata()
    {
        using var workspace = TestWorkspace.CreateCSharp("");

        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();
        var compilation = await project.GetCompilationAsync();
        Contract.ThrowIfNull(compilation);
        var m = compilation.GetReferencedAssemblySymbols().Single(a => a.Name == "mscorlib");
        var classificationOptions = TestOptionsProvider.Create(ClassificationOptions.Default);
        var searchOptions = FindReferencesSearchOptions.Default;

        var item = await DefinitionItemFactory.ToClassifiedDefinitionItemAsync(m, classificationOptions, solution, searchOptions, isPrimary: true, includeHiddenLocations: true, CancellationToken.None);

        VerifyDefinitionItem(item, project, symbols: [(m, nameof(m))],
            displayParts:
            [
                (tag: "Assembly", text: "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", TaggedTextStyle.None, target: SymbolKey.CreateString(m), hint: "mscorlib")
            ],
            nameDisplayParts:
            [
                (tag: "Assembly", text: "mscorlib", TaggedTextStyle.None, target: SymbolKey.CreateString(m), hint: "mscorlib")
            ],
            sourceSpans: [],
            metadataLocations:
            [
                "mscorlib 4.0.0.0 'Z:\\FxReferenceAssembliesUri'"
            ],
            tags:
            [
                "Assembly"
            ],
            properties:
            [
                ("MetadataSymbolKey", SymbolKey.CreateString(m)),
                ("MetadataSymbolOriginatingProjectIdDebugName", "Test"),
                ("MetadataSymbolOriginatingProjectIdGuid", project.Id.Id.ToString()),
                ("Primary", "")
            ]);
    }

    [Fact]
    public async Task ToClassifiedDefinitionItemAsync_Module_Source()
    {
        using var workspace = TestWorkspace.CreateCSharp("class C;");

        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();
        var compilation = await project.GetCompilationAsync();
        Contract.ThrowIfNull(compilation);
        var m = compilation.Assembly.Modules.Single();
        var classificationOptions = TestOptionsProvider.Create(ClassificationOptions.Default);
        var searchOptions = FindReferencesSearchOptions.Default;

        var item = await DefinitionItemFactory.ToClassifiedDefinitionItemAsync(m, classificationOptions, solution, searchOptions, isPrimary: true, includeHiddenLocations: true, CancellationToken.None);

        VerifyDefinitionItem(item, project, symbols: [(m, nameof(m))],
            displayParts:
            [
                (tag: "Module", text: "Test.dll", TaggedTextStyle.None, target: SymbolKey.CreateString(m), hint: "Test.dll")
            ],
            nameDisplayParts:
            [
                (tag: "Module", text: "Test.dll", TaggedTextStyle.None, target: SymbolKey.CreateString(m), hint: "Test.dll")
            ],
            sourceSpans: [],
            tags:
            [
                "Assembly"
            ],
            properties:
            [
                ("NonNavigable", ""),
                ("Primary", ""),
            ]);
    }

    [Fact]
    public async Task ToClassifiedDefinitionItemAsync_Module_Metadata()
    {
        using var workspace = TestWorkspace.CreateCSharp("");

        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();
        var compilation = await project.GetCompilationAsync();
        Contract.ThrowIfNull(compilation);
        var m = compilation.GetReferencedAssemblySymbols().Single(a => a.Name == "mscorlib").Modules.Single();
        var classificationOptions = TestOptionsProvider.Create(ClassificationOptions.Default);
        var searchOptions = FindReferencesSearchOptions.Default;

        var item = await DefinitionItemFactory.ToClassifiedDefinitionItemAsync(m, classificationOptions, solution, searchOptions, isPrimary: true, includeHiddenLocations: true, CancellationToken.None);

        VerifyDefinitionItem(item, project, symbols: [(m, nameof(m))],
            displayParts:
            [
                (tag: "Module", text: "CommonLanguageRuntimeLibrary", TaggedTextStyle.None, target: SymbolKey.CreateString(m), hint: "CommonLanguageRuntimeLibrary")
            ],
            nameDisplayParts:
            [
                (tag: "Module", text: "CommonLanguageRuntimeLibrary", TaggedTextStyle.None, target: SymbolKey.CreateString(m), hint: "CommonLanguageRuntimeLibrary")
            ],
            sourceSpans: [],
            metadataLocations:
            [
                "mscorlib 4.0.0.0 'Z:\\FxReferenceAssembliesUri'"
            ],
            tags:
            [
                "Assembly"
            ],
            properties:
            [
                ("MetadataSymbolKey", SymbolKey.CreateString(m)),
                ("MetadataSymbolOriginatingProjectIdDebugName", "Test"),
                ("MetadataSymbolOriginatingProjectIdGuid", project.Id.Id.ToString()),
                ("Primary", "")
            ]);
    }

    [Fact]
    public async Task ToClassifiedDefinitionItemAsync_Namespace_Source()
    {
        using var workspace = TestWorkspace.CreateCSharp("namespace N;");

        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();
        var compilation = await project.GetCompilationAsync();
        Contract.ThrowIfNull(compilation);
        var symbol = compilation.GetMember("N");
        var classificationOptions = TestOptionsProvider.Create(ClassificationOptions.Default);
        var searchOptions = FindReferencesSearchOptions.Default;

        var item = await DefinitionItemFactory.ToClassifiedDefinitionItemAsync(symbol, classificationOptions, solution, searchOptions, isPrimary: true, includeHiddenLocations: true, CancellationToken.None);

        VerifyDefinitionItem(item, project,
            // navigation target is generally not provided for namespaces
            displayParts:
            [
                (tag: "Keyword", text: "namespace", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Space", text: " ", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Namespace", text: "N", TaggedTextStyle.None, target: null, hint: null)
            ],
            nameDisplayParts:
            [
                (tag: "Namespace", text: "N", TaggedTextStyle.None, target: null, hint: null)
            ],
            sourceSpans:
            [
                "test1.cs [10..11)"
            ],
            tags:
            [
                "Namespace"
            ],
            properties:
            [
                ("Primary", ""),
                ("RQNameKey1", "Ns(NsName(N))")
            ]);
    }

    [Fact]
    public async Task ToClassifiedDefinitionItemAsync_Namespace_Metadata()
    {
        using var workspace = TestWorkspace.CreateCSharp("");

        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();
        var compilation = await project.GetCompilationAsync();
        Contract.ThrowIfNull(compilation);
        var n = compilation.GetMember("System");
        var classificationOptions = TestOptionsProvider.Create(ClassificationOptions.Default);
        var searchOptions = FindReferencesSearchOptions.Default;

        var item = await DefinitionItemFactory.ToClassifiedDefinitionItemAsync(n, classificationOptions, solution, searchOptions, isPrimary: true, includeHiddenLocations: true, CancellationToken.None);

        VerifyDefinitionItem(item, project,
            // navigation target is generally not provided for namespaces
            displayParts:
            [
                (tag: "Keyword", text: "namespace", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Space", text: " ", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Namespace", text: "System", TaggedTextStyle.None, target: null, hint: null)
            ],
            nameDisplayParts:
            [
                (tag: "Namespace", text: "System", TaggedTextStyle.None, target: null, hint: null)
            ],
            sourceSpans: [],
            metadataLocations:
            [
                "mscorlib 4.0.0.0 'Z:\\FxReferenceAssembliesUri'",
                "System 4.0.0.0 ''",
                "System.Core 4.0.0.0 ''",
                "System.ValueTuple 4.0.3.0 'System.ValueTuple.dll'",
                "System.Runtime 4.0.20.0 ''"
            ],
            tags:
            [
                "Namespace"
            ],
            properties:
            [
                ("MetadataSymbolKey", SymbolKey.CreateString(n)),
                ("MetadataSymbolOriginatingProjectIdGuid", project.Id.Id.ToString()),
                ("MetadataSymbolOriginatingProjectIdDebugName", "Test"),
                ("Primary", ""),
                ("RQNameKey1", "Ns(NsName(System))"),
            ]);
    }

    [Fact]
    public async Task ToClassifiedDefinitionItemAsync_Namespace_MetadataAndSource()
    {
        using var workspace = TestWorkspace.CreateCSharp("""
            namespace System { class C {} }
            namespace System { class D {} }
            """);

        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();
        var compilation = await project.GetCompilationAsync();
        Contract.ThrowIfNull(compilation);
        var n = compilation.GetMember("System");
        var classificationOptions = TestOptionsProvider.Create(ClassificationOptions.Default);
        var searchOptions = FindReferencesSearchOptions.Default;

        var item = await DefinitionItemFactory.ToClassifiedDefinitionItemAsync(n, classificationOptions, solution, searchOptions, isPrimary: true, includeHiddenLocations: true, CancellationToken.None);

        VerifyDefinitionItem(item, project, [(n, nameof(n))],
            // navigation target is generally not provided for namespaces
            displayParts:
            [
                (tag: "Keyword", text: "namespace", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Space", text: " ", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Namespace", text: "System", TaggedTextStyle.None, target: null, hint: null)
            ],
            nameDisplayParts:
            [
                (tag: "Namespace", text: "System", TaggedTextStyle.None, target: null, hint: null)
            ],
            metadataLocations:
            [
                "mscorlib 4.0.0.0 'Z:\\FxReferenceAssembliesUri'",
                "System 4.0.0.0 ''",
                "System.Core 4.0.0.0 ''",
                "System.ValueTuple 4.0.3.0 'System.ValueTuple.dll'",
                "System.Runtime 4.0.20.0 ''"
            ],
            sourceSpans:
            [
                "test1.cs [10..16)",
                "test1.cs [43..49)"
            ],
            tags:
            [
                "Namespace"
            ],
            properties:
            [
                ("MetadataSymbolKey", SymbolKey.CreateString(n)),
                ("MetadataSymbolOriginatingProjectIdDebugName", "Test"),
                ("MetadataSymbolOriginatingProjectIdGuid", project.Id.Id.ToString()),
                ("Primary", ""),
                ("RQNameKey1", "Ns(NsName(System))"),
            ]);
    }

    [Fact]
    public async Task ToClassifiedDefinitionItemAsync_Namespace_Global_Source()
    {
        using var workspace = TestWorkspace.CreateCSharp("namespace N {}");

        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();
        var compilation = await project.GetCompilationAsync();
        Contract.ThrowIfNull(compilation);
        var symbol = compilation.Assembly.GlobalNamespace;
        var classificationOptions = TestOptionsProvider.Create(ClassificationOptions.Default);
        var searchOptions = FindReferencesSearchOptions.Default;

        var item = await DefinitionItemFactory.ToClassifiedDefinitionItemAsync(symbol, classificationOptions, solution, searchOptions, isPrimary: true, includeHiddenLocations: true, CancellationToken.None);

        VerifyDefinitionItem(item, project, [(symbol, nameof(symbol))],
            // navigation target is generally not provided for namespaces
            displayParts:
            [
                (tag: "Keyword", text: "namespace", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Space", text: " ", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Text", text: "<global namespace>", TaggedTextStyle.None, target: SymbolKey.CreateString(symbol), hint: "")
            ],
            nameDisplayParts:
            [
                (tag: "Text", text: "<global namespace>", TaggedTextStyle.None, target: SymbolKey.CreateString(symbol), hint: "")
            ],
            sourceSpans: [],
            tags:
            [
                "Namespace"
            ],
            properties:
            [
                ("NonNavigable", ""),
                ("Primary", ""),
                ("RQNameKey1", "Ns()")
            ]);
    }

    [Fact]
    public async Task ToClassifiedDefinitionItemAsync_Namespace_Global_SourceAndMetadata()
    {
        using var workspace = TestWorkspace.CreateCSharp("namespace N {}");

        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();
        var compilation = await project.GetCompilationAsync();
        Contract.ThrowIfNull(compilation);
        var symbol = compilation.GlobalNamespace;
        var classificationOptions = TestOptionsProvider.Create(ClassificationOptions.Default);
        var searchOptions = FindReferencesSearchOptions.Default;

        var item = await DefinitionItemFactory.ToClassifiedDefinitionItemAsync(symbol, classificationOptions, solution, searchOptions, isPrimary: true, includeHiddenLocations: true, CancellationToken.None);

        VerifyDefinitionItem(item, project,
            // navigation target is generally not provided for namespaces
            displayParts:
            [
                (tag: "Keyword", text: "namespace", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Space", text: " ", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Text", text: "<global namespace>", TaggedTextStyle.None, target: SymbolKey.CreateString(symbol), hint: "")
            ],
            nameDisplayParts:
            [
                (tag: "Text", text: "<global namespace>", TaggedTextStyle.None, target: SymbolKey.CreateString(symbol), hint: "")
            ],
            sourceSpans: [],
            tags:
            [
                "Namespace"
            ],
            properties:
            [
                ("NonNavigable", ""),
                ("Primary", ""),
                ("RQNameKey1", "Ns()")
            ]);
    }

    [Fact]
    public async Task ToClassifiedDefinitionItemAsync_Class()
    {
        using var workspace = TestWorkspace.CreateCSharp("class C;");

        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();
        var compilation = await project.GetCompilationAsync();
        Contract.ThrowIfNull(compilation);
        var c = compilation.GetMember("C");
        var classificationOptions = TestOptionsProvider.Create(ClassificationOptions.Default);
        var searchOptions = FindReferencesSearchOptions.Default;

        var item = await DefinitionItemFactory.ToClassifiedDefinitionItemAsync(c, classificationOptions, solution, searchOptions, isPrimary: true, includeHiddenLocations: true, CancellationToken.None);

        VerifyDefinitionItem(item, project, [(c, nameof(c))],
            displayParts:
            [
                (tag: "Keyword", text: "class", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Space", text: " ", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Class", text: "C", TaggedTextStyle.None, target: SymbolKey.CreateString(c), hint: "C")
            ],
            nameDisplayParts:
            [
                (tag: "Class", text: "C", TaggedTextStyle.None, target: SymbolKey.CreateString(c), hint: "C")
            ],
            sourceSpans:
            [
                "test1.cs [6..7)"
            ],
            tags:
            [
                "Class",
                "Internal"
            ],
            properties:
            [
                ("Primary", ""),
                ("RQNameKey1", "Agg(AggName(C,TypeVarCnt(0)))")
            ]);
    }

    [Fact]
    public async Task ToClassifiedDefinitionItemAsync_Class_Metadata()
    {
        using var workspace = TestWorkspace.CreateCSharp("");

        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();
        var compilation = await project.GetCompilationAsync();
        Contract.ThrowIfNull(compilation);
        var c = compilation.GetMember("System.Activator");
        var classificationOptions = TestOptionsProvider.Create(ClassificationOptions.Default);
        var searchOptions = FindReferencesSearchOptions.Default;

        var item = await DefinitionItemFactory.ToClassifiedDefinitionItemAsync(c, classificationOptions, solution, searchOptions, isPrimary: true, includeHiddenLocations: true, CancellationToken.None);

        VerifyDefinitionItem(item, project, [(c, nameof(c))],
            displayParts:
            [
                (tag: "Keyword", text: "class", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Space", text: " ", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Class", text: "Activator", TaggedTextStyle.None, target: SymbolKey.CreateString(c), hint: "Activator")
            ],
            nameDisplayParts:
            [
                (tag: "Class", text: "Activator", TaggedTextStyle.None, target: SymbolKey.CreateString(c), hint: "Activator")
            ],
            sourceSpans: [],
            metadataLocations:
            [
                "mscorlib 4.0.0.0 'Z:\\FxReferenceAssembliesUri'"
            ],
            tags:
            [
                "Class",
                "Public"
            ],
            properties:
            [
                ("MetadataSymbolKey", SymbolKey.CreateString(c)),
                ("MetadataSymbolOriginatingProjectIdDebugName", "Test"),
                ("MetadataSymbolOriginatingProjectIdGuid", project.Id.Id.ToString()),
                ("Primary", ""),
                ("RQNameKey1", "Agg(NsName(System),AggName(Activator,TypeVarCnt(0)))")
            ]);
    }

    [Fact]
    public async Task ToClassifiedDefinitionItemAsync_ClassCrossLanguage()
    {
        using var workspace = TestWorkspace.Create("""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="P1">
                    <ProjectReference>P2</ProjectReference>
                </Project>
                <Project Language="Visual Basic" CommonReferences="true" AssemblyName="P2">
                    <Document><![CDATA[
                        Class C
                        End Class
                    ]]></Document>
                </Project>
            </Workspace>
            """);

        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single(p => p.Name == "P1");
        var compilation = await project.GetCompilationAsync();
        Contract.ThrowIfNull(compilation);
        var c = compilation.GetMember("C");
        var classificationOptions = TestOptionsProvider.Create(ClassificationOptions.Default);
        var searchOptions = FindReferencesSearchOptions.Default;

        var item = await DefinitionItemFactory.ToClassifiedDefinitionItemAsync(c, classificationOptions, solution, searchOptions, isPrimary: true, includeHiddenLocations: true, CancellationToken.None);

        VerifyDefinitionItem(item, project, [(c, nameof(c))],
            displayParts:
            [
                (tag: "Keyword", text: "class", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Space", text: " ", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Class", text: "C", TaggedTextStyle.None, target: SymbolKey.CreateString(c), hint: "C")
            ],
            nameDisplayParts:
            [
                (tag: "Class", text: "C", TaggedTextStyle.None, target: SymbolKey.CreateString(c), hint: "C")
            ],
            sourceSpans: [],
            metadataLocations:
            [
                "P2 0.0.0.0 ''"
            ],
            tags:
            [
                "Class",
                "Internal"
            ],
            properties:
            [
                ("MetadataSymbolKey", SymbolKey.CreateString(c)),
                ("MetadataSymbolOriginatingProjectIdDebugName", "P1"),
                ("MetadataSymbolOriginatingProjectIdGuid", project.Id.Id.ToString()),
                ("Primary", ""),
                ("RQNameKey1", "Agg(AggName(C,TypeVarCnt(0)))")
            ]);
    }

    [Fact]
    public async Task ToClassifiedDefinitionItemAsync_Dynamic()
    {
        using var workspace = TestWorkspace.CreateCSharp("class C { dynamic F; }");

        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();
        var compilation = await project.GetCompilationAsync();
        Contract.ThrowIfNull(compilation);
        var c = compilation.GetMember<IFieldSymbol>("C.F").Type;
        var classificationOptions = TestOptionsProvider.Create(ClassificationOptions.Default);
        var searchOptions = FindReferencesSearchOptions.Default;

        var item = await DefinitionItemFactory.ToClassifiedDefinitionItemAsync(c, classificationOptions, solution, searchOptions, isPrimary: true, includeHiddenLocations: true, CancellationToken.None);

        VerifyDefinitionItem(item, project, [(c, nameof(c))],
            displayParts:
            [
                (tag: "Keyword", text: "dynamic", TaggedTextStyle.None, target: SymbolKey.CreateString(c), hint: "dynamic")
            ],
            nameDisplayParts:
            [
                (tag: "Keyword", text: "dynamic", TaggedTextStyle.None, target: SymbolKey.CreateString(c), hint: "dynamic")
            ],
            sourceSpans: [],
            tags:
            [
                "Class",
                "Public"
            ],
            properties:
            [
                ("NonNavigable", ""),
                ("Primary", "")
            ]);
    }

    [Fact]
    public async Task ToClassifiedDefinitionItemAsync_TupleSyntax()
    {
        using var workspace = TestWorkspace.CreateCSharp("class C { (int a, int b) F; }");

        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();
        var compilation = await project.GetCompilationAsync();
        Contract.ThrowIfNull(compilation);
        var tuple = (INamedTypeSymbol)compilation.GetMember<IFieldSymbol>("C.F").Type;
        var t1 = tuple.TypeParameters[0];
        var t2 = tuple.TypeParameters[1];
        var genericTuple = tuple.OriginalDefinition;
        var classificationOptions = TestOptionsProvider.Create(ClassificationOptions.Default);
        var searchOptions = FindReferencesSearchOptions.Default;

        var item = await DefinitionItemFactory.ToClassifiedDefinitionItemAsync(tuple, classificationOptions, solution, searchOptions, isPrimary: true, includeHiddenLocations: true, CancellationToken.None);

        VerifyDefinitionItem(item, project, [(tuple, nameof(tuple)), (genericTuple, nameof(genericTuple)), (t1, nameof(t1)), (t2, nameof(t2))],
            displayParts:
            [
                (tag: "Punctuation", text: "(", TaggedTextStyle.None, target: null, hint: null),
                (tag: "TypeParameter", text: "T1", TaggedTextStyle.None, target: SymbolKey.CreateString(t1), hint: "T1"),
                (tag: "Punctuation", text: ",", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Space", text: " ", TaggedTextStyle.None, target: null, hint: null),
                (tag: "TypeParameter", text: "T2", TaggedTextStyle.None, target: SymbolKey.CreateString(t2), hint: "T2"),
                (tag: "Punctuation", text: ")", TaggedTextStyle.None, target: null, hint: null)
            ],
            nameDisplayParts:
            [
                (tag: "Punctuation", text: "(", TaggedTextStyle.None, target: null, hint: null),
                (tag: "TypeParameter", text: "T1", TaggedTextStyle.None, target: SymbolKey.CreateString(t1), hint: "T1"),
                (tag: "Punctuation", text: ",", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Space", text: " ", TaggedTextStyle.None, target: null, hint: null),
                (tag: "TypeParameter", text: "T2", TaggedTextStyle.None, target: SymbolKey.CreateString(t2), hint: "T2"),
                (tag: "Punctuation", text: ")", TaggedTextStyle.None, target: null, hint: null)
            ],
            // the symbol has source location because it defines names for items in source:
            sourceSpans:
            [
                "test1.cs [10..24)"
            ],
            // the symbol has metadata locations because the generic type is in metadata:
            metadataLocations:
            [
                "System.ValueTuple 4.0.3.0 'System.ValueTuple.dll'"
            ],
            tags:
            [
                "Structure",
                "Public"
            ],
            properties:
            [
                ("MetadataSymbolOriginatingProjectIdDebugName", "Test"),
                ("RQNameKey1", "Agg(NsName(System),AggName(ValueTuple,TypeVarCnt(2)))"),
                ("Primary", ""),
                ("MetadataSymbolKey", SymbolKey.CreateString(genericTuple)),
                ("MetadataSymbolOriginatingProjectIdGuid", project.Id.Id.ToString())
            ]);
    }

    [Fact]
    public async Task ToClassifiedDefinitionItemAsync_ValueTuple()
    {
        using var workspace = TestWorkspace.CreateCSharp("class C { System.ValueTuple<int, int> F; }");

        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();
        var compilation = await project.GetCompilationAsync();
        Contract.ThrowIfNull(compilation);
        var tuple = (INamedTypeSymbol)compilation.GetMember<IFieldSymbol>("C.F").Type;
        var t1 = tuple.TypeParameters[0];
        var t2 = tuple.TypeParameters[1];
        var genericTuple = tuple.OriginalDefinition;
        var classificationOptions = TestOptionsProvider.Create(ClassificationOptions.Default);
        var searchOptions = FindReferencesSearchOptions.Default;

        var item = await DefinitionItemFactory.ToClassifiedDefinitionItemAsync(tuple, classificationOptions, solution, searchOptions, isPrimary: true, includeHiddenLocations: true, CancellationToken.None);

        VerifyDefinitionItem(item, project, [(tuple, nameof(tuple)), (genericTuple, nameof(genericTuple)), (t1, nameof(t1)), (t2, nameof(t2))],
            displayParts:
            [
                (tag: "Punctuation", text: "(", TaggedTextStyle.None, target: null, hint: null),
                (tag: "TypeParameter", text: "T1", TaggedTextStyle.None, target: SymbolKey.CreateString(t1), hint: "T1"),
                (tag: "Punctuation", text: ",", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Space", text: " ", TaggedTextStyle.None, target: null, hint: null),
                (tag: "TypeParameter", text: "T2", TaggedTextStyle.None, target: SymbolKey.CreateString(t2), hint: "T2"),
                (tag: "Punctuation", text: ")", TaggedTextStyle.None, target: null, hint: null)
            ],
            nameDisplayParts:
            [
                (tag: "Punctuation", text: "(", TaggedTextStyle.None, target: null, hint: null),
                (tag: "TypeParameter", text: "T1", TaggedTextStyle.None, target: SymbolKey.CreateString(t1), hint: "T1"),
                (tag: "Punctuation", text: ",", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Space", text: " ", TaggedTextStyle.None, target: null, hint: null),
                (tag: "TypeParameter", text: "T2", TaggedTextStyle.None, target: SymbolKey.CreateString(t2), hint: "T2"),
                (tag: "Punctuation", text: ")", TaggedTextStyle.None, target: null, hint: null)
            ],
            metadataLocations:
            [
                "System.ValueTuple 4.0.3.0 'System.ValueTuple.dll'"
            ],
            tags:
            [
                "Structure",
                "Public"
            ],
            properties:
            [
                ("MetadataSymbolOriginatingProjectIdDebugName", "Test"),
                ("RQNameKey1", "Agg(NsName(System),AggName(ValueTuple,TypeVarCnt(2)))"),
                ("Primary", ""),
                ("MetadataSymbolKey", SymbolKey.CreateString(genericTuple)),
                ("MetadataSymbolOriginatingProjectIdGuid", project.Id.Id.ToString())
            ]);
    }

    [Fact]
    public async Task ToClassifiedDefinitionItemAsync_GenericInstatiation_Source()
    {
        using var workspace = TestWorkspace.CreateCSharp("class C<T1, T2> { C<int, string> F; }");

        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();
        var compilation = await project.GetCompilationAsync();
        Contract.ThrowIfNull(compilation);
        var type = (INamedTypeSymbol)compilation.GetMember<IFieldSymbol>("C.F").Type;
        var t1 = type.TypeParameters[0];
        var t2 = type.TypeParameters[1];
        var genericType = type.OriginalDefinition;
        var classificationOptions = TestOptionsProvider.Create(ClassificationOptions.Default);
        var searchOptions = FindReferencesSearchOptions.Default;

        var item = await DefinitionItemFactory.ToClassifiedDefinitionItemAsync(type, classificationOptions, solution, searchOptions, isPrimary: true, includeHiddenLocations: true, CancellationToken.None);

        VerifyDefinitionItem(item, project, [(type, nameof(type)), (genericType, nameof(genericType)), (t1, nameof(t1)), (t2, nameof(t2))],
            displayParts:
            [
                (tag: "Keyword", text: "class", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Space", text: " ", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Class", text: "C", TaggedTextStyle.None, target: SymbolKey.CreateString(genericType), hint: "C<T1, T2>"),
                (tag: "Punctuation", text: "<", TaggedTextStyle.None, target: null, hint: null),
                (tag: "TypeParameter", text: "T1", TaggedTextStyle.None, target: SymbolKey.CreateString(t1), hint: "T1"),
                (tag: "Punctuation", text: ",", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Space", text: " ", TaggedTextStyle.None, target: null, hint: null),
                (tag: "TypeParameter", text: "T2", TaggedTextStyle.None, target: SymbolKey.CreateString(t2), hint: "T2"),
                (tag: "Punctuation", text: ">", TaggedTextStyle.None, target: null, hint: null)
            ],
            nameDisplayParts:
            [
                (tag: "Class", text: "C", TaggedTextStyle.None, target: SymbolKey.CreateString(genericType), hint: "C<T1, T2>")
            ],
            sourceSpans:
            [
                "test1.cs [6..7)"
            ],
            tags:
            [
                "Class",
                "Internal"
            ],
            properties:
            [
                ("Primary", ""),
                ("RQNameKey1", "Agg(AggName(C,TypeVarCnt(2)))")
            ]);
    }

    [Fact]
    public async Task ToClassifiedDefinitionItemAsync_GenericInstatiation_Metadata()
    {
        using var workspace = TestWorkspace.CreateCSharp("""
            using System.Collections.Generic;
            class C { Dictionary<int, string> F; }
            """);

        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();
        var compilation = await project.GetCompilationAsync();
        Contract.ThrowIfNull(compilation);
        var type = (INamedTypeSymbol)compilation.GetMember<IFieldSymbol>("C.F").Type;
        var t1 = type.TypeParameters[0];
        var t2 = type.TypeParameters[1];
        var genericType = type.OriginalDefinition;
        var classificationOptions = TestOptionsProvider.Create(ClassificationOptions.Default);
        var searchOptions = FindReferencesSearchOptions.Default;

        var item = await DefinitionItemFactory.ToClassifiedDefinitionItemAsync(type, classificationOptions, solution, searchOptions, isPrimary: true, includeHiddenLocations: true, CancellationToken.None);

        VerifyDefinitionItem(item, project, [(type, nameof(type)), (genericType, nameof(genericType)), (t1, nameof(t1)), (t2, nameof(t2))],
            displayParts:
            [
                (tag: "Keyword", text: "class", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Space", text: " ", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Class", text: "Dictionary", TaggedTextStyle.None, target: SymbolKey.CreateString(genericType), hint: "Dictionary<TKey, TValue>"),
                (tag: "Punctuation", text: "<", TaggedTextStyle.None, target: null, hint: null),
                (tag: "TypeParameter", text: "TKey", TaggedTextStyle.None, target: SymbolKey.CreateString(t1), hint: "TKey"),
                (tag: "Punctuation", text: ",", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Space", text: " ", TaggedTextStyle.None, target: null, hint: null),
                (tag: "TypeParameter", text: "TValue", TaggedTextStyle.None, target: SymbolKey.CreateString(t2), hint: "TValue"),
                (tag: "Punctuation", text: ">", TaggedTextStyle.None, target: null, hint: null)
            ],
            nameDisplayParts:
            [
                (tag: "Class", text: "Dictionary", TaggedTextStyle.None, target: SymbolKey.CreateString(genericType), hint: "Dictionary<TKey, TValue>")
            ],
            sourceSpans: [],
            metadataLocations:
            [
                "mscorlib 4.0.0.0 'Z:\\FxReferenceAssembliesUri'"
            ],
            tags:
            [
                "Class",
                "Public"
            ],
            properties:
            [
                ("MetadataSymbolKey", SymbolKey.CreateString(genericType)),
                ("MetadataSymbolOriginatingProjectIdDebugName", "Test"),
                ("MetadataSymbolOriginatingProjectIdGuid", project.Id.Id.ToString()),
                ("Primary", ""),
                ("RQNameKey1", "Agg(NsName(System),NsName(Collections),NsName(Generic),AggName(Dictionary,TypeVarCnt(2)))")
            ]);
    }

    [Fact]
    public async Task ToClassifiedDefinitionItemAsync_TypeTypeParameter()
    {
        using var workspace = TestWorkspace.CreateCSharp("class C<T>;");

        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();

        var document = project.Documents.Single();
        var tree = await document.GetSyntaxTreeAsync();
        Contract.ThrowIfNull(tree);

        var compilation = await project.GetCompilationAsync();
        Contract.ThrowIfNull(compilation);

        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
        var c = compilation.GetMember("C");
        var p = c.GetTypeParameters().Single();

        var classificationOptions = TestOptionsProvider.Create(ClassificationOptions.Default);
        var searchOptions = FindReferencesSearchOptions.Default;
        var item = await DefinitionItemFactory.ToClassifiedDefinitionItemAsync(p, classificationOptions, solution, searchOptions, isPrimary: true, includeHiddenLocations: true, CancellationToken.None);

        VerifyDefinitionItem(item, project, symbols: [(c, nameof(c)), (p, nameof(p))],
            displayParts:
            [
                (tag: "TypeParameter", text: "T", TaggedTextStyle.None, target: SymbolKey.CreateString(p), hint: "T")
            ],
            nameDisplayParts:
            [
                (tag: "TypeParameter", text: "T", TaggedTextStyle.None, target: SymbolKey.CreateString(p), hint: "T")
            ],
            sourceSpans:
            [
                "test1.cs [8..9)"
            ],
            tags:
            [
                "TypeParameter"
            ],
            properties:
            [
                ("Primary", "")
            ],
            displayableProperties:
            [
                ("ContainingTypeInfo", "C")
            ]);
    }

    [Fact]
    public async Task ToClassifiedDefinitionItemAsync_Method()
    {
        using var workspace = TestWorkspace.CreateCSharp("class C { void M(int x) { } }");

        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();
        var compilation = await project.GetCompilationAsync();
        Contract.ThrowIfNull(compilation);
        var m = compilation.GetMember("C.M");
        var c = m.ContainingType;
        var i = compilation.GetSpecialType(SpecialType.System_Int32);
        var classificationOptions = TestOptionsProvider.Create(ClassificationOptions.Default);
        var searchOptions = FindReferencesSearchOptions.Default;

        var item = await DefinitionItemFactory.ToClassifiedDefinitionItemAsync(m, classificationOptions, solution, searchOptions, isPrimary: true, includeHiddenLocations: true, CancellationToken.None);

        VerifyDefinitionItem(item, project, symbols: [(c, nameof(c)), (m, nameof(m)), (i, nameof(i))],
            displayParts:
            [
                (tag: "Keyword", text: "void", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Space", text: " ", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Class", text: "C", TaggedTextStyle.None, target: SymbolKey.CreateString(c), hint: "C"),
                (tag: "Punctuation", text: ".", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Method", text: "M", TaggedTextStyle.None, target: SymbolKey.CreateString(m), hint: "void C.M(int x)"),
                (tag: "Punctuation", text: "(", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Keyword", text: "int", TaggedTextStyle.None, target: SymbolKey.CreateString(i), hint: "int"),
                (tag: "Punctuation", text: ")", TaggedTextStyle.None, target: null, hint: null)
            ],
            nameDisplayParts:
            [
                (tag: "Class", text: "C", TaggedTextStyle.None, target: SymbolKey.CreateString(c), hint: "C"),
                (tag: "Punctuation", text: ".", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Method", text: "M", TaggedTextStyle.None, target: SymbolKey.CreateString(m), hint: "void C.M(int x)")
            ],
            sourceSpans:
            [
                "test1.cs [15..16)"
            ],
            tags:
            [
                "Method",
                "Private"
            ],
            properties:
            [
                ("Primary", ""),
                ("RQNameKey1", "Meth(Agg(AggName(C,TypeVarCnt(0))),MethName(M),TypeVarCnt(0),Params(Param(AggType(Agg(NsName(System),AggName(Int32,TypeVarCnt(0))),TypeParams()))))")
            ],
            displayableProperties:
            [
                ("ContainingTypeInfo", "C")
            ]);
    }

    [Fact]
    public async Task ToClassifiedDefinitionItemAsync_Field()
    {
        using var workspace = TestWorkspace.CreateCSharp("class C { int M; }");

        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();
        var compilation = await project.GetCompilationAsync();
        Contract.ThrowIfNull(compilation);
        var m = compilation.GetMember("C.M");
        var c = m.ContainingType;
        var i = compilation.GetSpecialType(SpecialType.System_Int32);
        var classificationOptions = TestOptionsProvider.Create(ClassificationOptions.Default);
        var searchOptions = FindReferencesSearchOptions.Default;

        var item = await DefinitionItemFactory.ToClassifiedDefinitionItemAsync(m, classificationOptions, solution, searchOptions, isPrimary: true, includeHiddenLocations: true, CancellationToken.None);

        VerifyDefinitionItem(item, project, symbols: [(c, nameof(c)), (m, nameof(m)), (i, nameof(i))],
            displayParts:
            [
                (tag: "Keyword", text: "int", TaggedTextStyle.None, target: SymbolKey.CreateString(i), hint: "int"),
                (tag: "Space", text: " ", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Class", text: "C", TaggedTextStyle.None, target: SymbolKey.CreateString(c), hint: "C"),
                (tag: "Punctuation", text: ".", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Field", text: "M", TaggedTextStyle.None, target: SymbolKey.CreateString(m), hint: "int C.M")
            ],
            nameDisplayParts:
            [
                (tag: "Class", text: "C", TaggedTextStyle.None, target: SymbolKey.CreateString(c), hint: "C"),
                (tag: "Punctuation", text: ".", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Field", text: "M", TaggedTextStyle.None, target: SymbolKey.CreateString(m), hint: "int C.M")
            ],
            sourceSpans:
            [
                "test1.cs [14..15)"
            ],
            tags:
            [
                "Field",
                "Private"
            ],
            properties:
            [
                ("Primary", ""),
                ("RQNameKey1", "Membvar(Agg(AggName(C,TypeVarCnt(0))),MembvarName(M))"),
            ],
            displayableProperties:
            [
                ("ContainingTypeInfo", "C")
            ]);
    }

    [Fact]
    public async Task ToClassifiedDefinitionItemAsync_Property()
    {
        using var workspace = TestWorkspace.CreateCSharp("class C { int P { get; set; } }");

        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();
        var compilation = await project.GetCompilationAsync();
        Contract.ThrowIfNull(compilation);
        var p = compilation.GetMember("C.P");
        var c = p.ContainingType;
        var i = compilation.GetSpecialType(SpecialType.System_Int32);
        var classificationOptions = TestOptionsProvider.Create(ClassificationOptions.Default);
        var searchOptions = FindReferencesSearchOptions.Default;

        var item = await DefinitionItemFactory.ToClassifiedDefinitionItemAsync(p, classificationOptions, solution, searchOptions, isPrimary: true, includeHiddenLocations: true, CancellationToken.None);

        VerifyDefinitionItem(item, project, symbols: [(c, nameof(c)), (p, nameof(p)), (i, nameof(i))],
            displayParts:
            [
                (tag: "Keyword", text: "int", TaggedTextStyle.None, target: SymbolKey.CreateString(i), hint: "int"),
                (tag: "Space", text: " ", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Class", text: "C", TaggedTextStyle.None, target: SymbolKey.CreateString(c), hint: "C"),
                (tag: "Punctuation", text: ".", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Property", text: "P", TaggedTextStyle.None, target: SymbolKey.CreateString(p), hint: "int C.P"),
                (tag: "Space", text: " ", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Punctuation", text: "{", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Space", text: " ", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Keyword", text: "get", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Punctuation", text: ";", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Space", text: " ", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Keyword", text: "set", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Punctuation", text: ";", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Space", text: " ", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Punctuation", text: "}", TaggedTextStyle.None, target: null, hint: null)
            ],
            nameDisplayParts:
            [
                (tag: "Class", text: "C", TaggedTextStyle.None, target: SymbolKey.CreateString(c), hint: "C"),
                (tag: "Punctuation", text: ".", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Property", text: "P", TaggedTextStyle.None, target: SymbolKey.CreateString(p), hint: "int C.P")
            ],
            sourceSpans:
            [
                "test1.cs [14..15)"
            ],
            tags:
            [
                "Property",
                "Private"
            ],
            properties:
            [
                ("Primary", ""),
                ("RQNameKey1", "Prop(Agg(AggName(C,TypeVarCnt(0))),PropName(P),TypeVarCnt(0),Params())"),
            ],
            displayableProperties:
            [
                ("ContainingTypeInfo", "C")
            ]);
    }

    [Fact]
    public async Task ToClassifiedDefinitionItemAsync_Property_Getter()
    {
        using var workspace = TestWorkspace.CreateCSharp("class C { int P { get; set; } }");

        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();
        var compilation = await project.GetCompilationAsync();
        Contract.ThrowIfNull(compilation);
        var p = compilation.GetMember<IPropertySymbol>("C.P");
        var g = p.GetMethod;
        Contract.ThrowIfNull(g);
        var c = p.ContainingType;
        var i = compilation.GetSpecialType(SpecialType.System_Int32);
        var classificationOptions = TestOptionsProvider.Create(ClassificationOptions.Default);
        var searchOptions = FindReferencesSearchOptions.Default;

        var item = await DefinitionItemFactory.ToClassifiedDefinitionItemAsync(g, classificationOptions, solution, searchOptions, isPrimary: true, includeHiddenLocations: true, CancellationToken.None);

        VerifyDefinitionItem(item, project, symbols: [(c, nameof(c)), (p, nameof(p)), (g, nameof(g)), (i, nameof(i))],
            displayParts:
            [
                (tag: "Keyword", text: "int", TaggedTextStyle.None, target: SymbolKey.CreateString(i), hint: "int"),
                (tag: "Space", text: " ", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Class", text: "C", TaggedTextStyle.None, target: SymbolKey.CreateString(c), hint: "C"),
                (tag: "Punctuation", text: ".", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Property", text: "P", TaggedTextStyle.None, target: SymbolKey.CreateString(p), hint: "int C.P"),
                (tag: "Punctuation", text: ".", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Keyword", text: "get", TaggedTextStyle.None, target: null, hint: null)
            ],
            nameDisplayParts:
            [
                (tag: "Class", text: "C", TaggedTextStyle.None, target: SymbolKey.CreateString(c), hint: "C"),
                (tag: "Punctuation", text: ".", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Property", text: "P", TaggedTextStyle.None, target: SymbolKey.CreateString(p), hint: "int C.P"),
                (tag: "Punctuation", text: ".", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Keyword", text: "get", TaggedTextStyle.None, target: null, hint: null)
            ],
            sourceSpans:
            [
                "test1.cs [18..21)"
            ],
            tags:
            [
                "Method",
                "Private"
            ],
            properties:
            [
                ("Primary", ""),
            ],
            displayableProperties:
            [
                ("ContainingTypeInfo", "C")
            ]);
    }

    [Fact]
    public async Task ToClassifiedDefinitionItemAsync_Property_Setter()
    {
        using var workspace = TestWorkspace.CreateCSharp("class C { int P { get; set; } }");

        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();
        var compilation = await project.GetCompilationAsync();
        Contract.ThrowIfNull(compilation);
        var p = compilation.GetMember<IPropertySymbol>("C.P");
        var g = p.SetMethod;
        Contract.ThrowIfNull(g);
        var c = p.ContainingType;
        var i = compilation.GetSpecialType(SpecialType.System_Int32);
        var classificationOptions = TestOptionsProvider.Create(ClassificationOptions.Default);
        var searchOptions = FindReferencesSearchOptions.Default;

        var item = await DefinitionItemFactory.ToClassifiedDefinitionItemAsync(g, classificationOptions, solution, searchOptions, isPrimary: true, includeHiddenLocations: true, CancellationToken.None);

        VerifyDefinitionItem(item, project, symbols: [(c, nameof(c)), (p, nameof(p)), (g, nameof(g)), (i, nameof(i))],
            displayParts:
            [
                (tag: "Keyword", text: "void", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Space", text: " ", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Class", text: "C", TaggedTextStyle.None, target: SymbolKey.CreateString(c), hint: "C"),
                (tag: "Punctuation", text: ".", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Property", text: "P", TaggedTextStyle.None, target: SymbolKey.CreateString(p), hint: "int C.P"),
                (tag: "Punctuation", text: ".", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Keyword", text: "set", TaggedTextStyle.None, target: null, hint: null)
            ],
            nameDisplayParts:
            [
                (tag: "Class", text: "C", TaggedTextStyle.None, target: SymbolKey.CreateString(c), hint: "C"),
                (tag: "Punctuation", text: ".", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Property", text: "P", TaggedTextStyle.None, target: SymbolKey.CreateString(p), hint: "int C.P"),
                (tag: "Punctuation", text: ".", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Keyword", text: "set", TaggedTextStyle.None, target: null, hint: null)
            ],
            sourceSpans:
            [
                "test1.cs [23..26)"
            ],
            tags:
            [
                "Method",
                "Private"
            ],
            properties:
            [
                ("Primary", ""),
            ],
            displayableProperties:
            [
                ("ContainingTypeInfo", "C")
            ]);
    }

    [Fact]
    public async Task ToClassifiedDefinitionItemAsync_Indexer()
    {
        using var workspace = TestWorkspace.CreateCSharp("abstract class C { abstract int this[int x] { get; set; } }");

        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();
        var compilation = await project.GetCompilationAsync();
        Contract.ThrowIfNull(compilation);
        var p = compilation.GetMember("C.this[]");
        var c = p.ContainingType;
        var i = compilation.GetSpecialType(SpecialType.System_Int32);
        var classificationOptions = TestOptionsProvider.Create(ClassificationOptions.Default);
        var searchOptions = FindReferencesSearchOptions.Default;

        var item = await DefinitionItemFactory.ToClassifiedDefinitionItemAsync(p, classificationOptions, solution, searchOptions, isPrimary: true, includeHiddenLocations: true, CancellationToken.None);

        VerifyDefinitionItem(item, project, symbols: [(c, nameof(c)), (p, nameof(p)), (i, nameof(i))],
            displayParts:
            [
                (tag: "Keyword", text: "abstract", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Space", text: " ", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Keyword", text: "int", TaggedTextStyle.None, target: SymbolKey.CreateString(i), hint: "int"),
                (tag: "Space", text: " ", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Class", text: "C", TaggedTextStyle.None, target: SymbolKey.CreateString(c), hint: "C"),
                (tag: "Punctuation", text: ".", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Keyword", text: "this", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Punctuation", text: "[", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Keyword", text: "int", TaggedTextStyle.None, target: SymbolKey.CreateString(i), hint: "int"),
                (tag: "Punctuation", text: "]", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Space", text: " ", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Punctuation", text: "{", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Space", text: " ", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Keyword", text: "get", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Punctuation", text: ";", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Space", text: " ", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Keyword", text: "set", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Punctuation", text: ";", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Space", text: " ", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Punctuation", text: "}", TaggedTextStyle.None, target: null, hint: null)
            ],
            nameDisplayParts:
            [
                (tag: "Class", text: "C", TaggedTextStyle.None, target: SymbolKey.CreateString(c), hint: "C"),
                (tag: "Punctuation", text: ".", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Keyword", text: "this", TaggedTextStyle.None, target: null, hint: null)
            ],
            sourceSpans:
            [
                "test1.cs [32..36)"
            ],
            tags:
            [
                "Property",
                "Private"
            ],
            properties:
            [
                ("Primary", ""),
                ("RQNameKey1", "Prop(Agg(AggName(C,TypeVarCnt(0))),PropName($Item$),TypeVarCnt(0),Params(Param(AggType(Agg(NsName(System),AggName(Int32,TypeVarCnt(0))),TypeParams()))))"),
            ],
            displayableProperties:
            [
                ("ContainingTypeInfo", "C")
            ]);
    }

    [Fact]
    public async Task ToClassifiedDefinitionItemAsync_Parameter()
    {
        using var workspace = TestWorkspace.CreateCSharp("""
            class C
            {
                void M(int p) { }
            }
            """);

        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();

        var document = project.Documents.Single();
        var tree = await document.GetSyntaxTreeAsync();
        Contract.ThrowIfNull(tree);

        var compilation = await project.GetCompilationAsync();
        Contract.ThrowIfNull(compilation);

        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
        var m = compilation.GetMember("C.M");
        var c = m.ContainingType;
        var p = m.GetParameters().Single();
        var i = compilation.GetSpecialType(SpecialType.System_Int32);

        var classificationOptions = TestOptionsProvider.Create(ClassificationOptions.Default);
        var searchOptions = FindReferencesSearchOptions.Default;
        var item = await DefinitionItemFactory.ToClassifiedDefinitionItemAsync(p, classificationOptions, solution, searchOptions, isPrimary: true, includeHiddenLocations: true, CancellationToken.None);

        VerifyDefinitionItem(item, project, symbols: [(c, nameof(c)), (p, nameof(p)), (i, nameof(i))],
            displayParts:
            [
                (tag: "Keyword", text: "int", TaggedTextStyle.None, target: SymbolKey.CreateString(i), hint: "int"),
                (tag: "Space", text: " ", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Parameter", text: "p", TaggedTextStyle.None, target: SymbolKey.CreateString(p), hint: "int p")
            ],
            nameDisplayParts:
            [
                (tag: "Parameter", text: "p", TaggedTextStyle.None, target: SymbolKey.CreateString(p), hint: "int p")
            ],
            sourceSpans:
            [
                "test1.cs [27..28)"
            ],
            tags:
            [
                "Parameter"
            ],
            properties:
            [
                ("Primary", "")
            ],
            displayableProperties:
            [
                ("ContainingTypeInfo", "C"),
                ("ContainingMemberInfo", "M")
            ]);
    }

    [Fact]
    public async Task ToClassifiedDefinitionItemAsync_MethodTypeParameter()
    {
        using var workspace = TestWorkspace.CreateCSharp("""
            class C
            {
                void M<T>() { }
            }
            """);

        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();

        var document = project.Documents.Single();
        var tree = await document.GetSyntaxTreeAsync();
        Contract.ThrowIfNull(tree);

        var compilation = await project.GetCompilationAsync();
        Contract.ThrowIfNull(compilation);

        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
        var m = compilation.GetMember("C.M");
        var c = m.ContainingType;
        var p = m.GetTypeParameters().Single();

        var classificationOptions = TestOptionsProvider.Create(ClassificationOptions.Default);
        var searchOptions = FindReferencesSearchOptions.Default;
        var item = await DefinitionItemFactory.ToClassifiedDefinitionItemAsync(p, classificationOptions, solution, searchOptions, isPrimary: true, includeHiddenLocations: true, CancellationToken.None);

        VerifyDefinitionItem(item, project, symbols: [(c, nameof(c)), (p, nameof(p))],
            displayParts:
            [
                (tag: "TypeParameter", text: "T", TaggedTextStyle.None, target: SymbolKey.CreateString(p), hint: "T")
            ],
            nameDisplayParts:
            [
                (tag: "TypeParameter", text: "T", TaggedTextStyle.None, target: SymbolKey.CreateString(p), hint: "T")
            ],
            sourceSpans:
            [
                "test1.cs [23..24)"
            ],
            tags:
            [
                "TypeParameter"
            ],
            properties:
            [
                ("Primary", "")
            ],
            displayableProperties:
            [
                ("ContainingTypeInfo", "C"),
                ("ContainingMemberInfo", "M")
            ]);
    }

    [Fact]
    public async Task ToClassifiedDefinitionItemAsync_LocalFunction()
    {
        using var workspace = TestWorkspace.CreateCSharp("class C { void M(int x) { void F() {} } }");

        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();
        var document = project.Documents.Single();
        var tree = await document.GetSyntaxTreeAsync();
        Contract.ThrowIfNull(tree);

        var compilation = await project.GetCompilationAsync();
        Contract.ThrowIfNull(compilation);
        var model = compilation.GetSemanticModel(tree);

        var f = model.GetDeclaredSymbol(tree.GetRoot().DescendantNodes().Single(n => n is LocalFunctionStatementSyntax));
        Contract.ThrowIfNull(f);

        var c = compilation.GetMember("C");

        var classificationOptions = TestOptionsProvider.Create(ClassificationOptions.Default);
        var searchOptions = FindReferencesSearchOptions.Default;
        var item = await DefinitionItemFactory.ToClassifiedDefinitionItemAsync(f, classificationOptions, solution, searchOptions, isPrimary: true, includeHiddenLocations: true, CancellationToken.None);

        VerifyDefinitionItem(item, project, symbols: [(c, nameof(c)), (f, nameof(f))],
            displayParts:
            [
                (tag: "Keyword", text: "void", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Space", text: " ", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Method", text: "F", TaggedTextStyle.None, target: SymbolKey.CreateString(f), hint: "void F()"),
                (tag: "Punctuation", text: "(", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Punctuation", text: ")", TaggedTextStyle.None, target: null, hint: null)
            ],
            nameDisplayParts:
            [
                (tag: "Method", text: "F", TaggedTextStyle.None, target: SymbolKey.CreateString(f), hint: "void F()")
            ],
            sourceSpans:
            [
                "test1.cs [31..32)"
            ],
            tags:
            [
                "Method",
                "Private"
            ],
            properties:
            [
                ("Primary", ""),
                ("RQNameKey1", "Meth(Agg(AggName(C,TypeVarCnt(0))),MethName(F),TypeVarCnt(0),Params())"), // TODO: doesn't look right
            ],
            displayableProperties:
            [
                ("ContainingMemberInfo", "M"),
                ("ContainingTypeInfo", "C")
            ]);
    }

    [Fact]
    public async Task ToClassifiedDefinitionItemAsync_LocalVariable()
    {
        using var workspace = TestWorkspace.CreateCSharp("""
            class C
            {
                void M() { int x; }
            }
            """);

        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();

        var document = project.Documents.Single();
        var tree = await document.GetSyntaxTreeAsync();
        Contract.ThrowIfNull(tree);

        var compilation = await project.GetCompilationAsync();
        Contract.ThrowIfNull(compilation);

        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
        var model = compilation.GetSemanticModel(tree);

        var x = model.GetDeclaredSymbol(tree.GetRoot().DescendantNodes().Single(n => n is VariableDeclaratorSyntax));
        Contract.ThrowIfNull(x);

        var c = compilation.GetMember("C");
        var i = compilation.GetSpecialType(SpecialType.System_Int32);

        var classificationOptions = TestOptionsProvider.Create(ClassificationOptions.Default);
        var searchOptions = FindReferencesSearchOptions.Default;
        var item = await DefinitionItemFactory.ToClassifiedDefinitionItemAsync(x, classificationOptions, solution, searchOptions, isPrimary: true, includeHiddenLocations: true, CancellationToken.None);

        VerifyDefinitionItem(item, project, symbols: [(c, nameof(c)), (x, nameof(x)), (i, nameof(i))],
            displayParts:
            [
                (tag: "Keyword", text: "int", TaggedTextStyle.None, target: SymbolKey.CreateString(i), hint: "int"),
                (tag: "Space", text: " ", TaggedTextStyle.None, target: null, hint: null),
                (tag: "Local", text: "x", TaggedTextStyle.None, target: SymbolKey.CreateString(x), hint: "int x")
            ],
            nameDisplayParts:
            [
                (tag: "Local", text: "x", TaggedTextStyle.None, target: SymbolKey.CreateString(x), hint: "int x")
            ],
            sourceSpans:
            [
                "test1.cs [31..32)"
            ],
            tags:
            [
                "Local"
            ],
            properties:
            [
                ("Primary", "")
            ],
            displayableProperties:
            [
                ("ContainingTypeInfo", "C"),
                ("ContainingMemberInfo", "M")
            ]);
    }

    [Fact]
    public async Task ToClassifiedDefinitionItemAsync_RangeVariable()
    {
        using var workspace = TestWorkspace.Create("""
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document><![CDATA[
                        using System.Linq;
                        using System.Collections.Generic;
                        class C
                        {
                            IEnumerable<int> M() => from x in new[] { 1, 2, 3 } select x;
                        }
                    ]]></Document>
                </Project>
            </Workspace>
            """);

        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();

        var document = project.Documents.Single();
        var tree = await document.GetSyntaxTreeAsync();
        Contract.ThrowIfNull(tree);

        var compilation = await project.GetCompilationAsync();
        Contract.ThrowIfNull(compilation);

        Assert.Empty(compilation.GetDiagnostics());
        var model = compilation.GetSemanticModel(tree);

        var r = model.GetDeclaredSymbol(tree.GetRoot().DescendantNodes().Single(n => n is FromClauseSyntax));
        Contract.ThrowIfNull(r);

        var c = compilation.GetMember("C");
        var i = compilation.GetSpecialType(SpecialType.System_Int32);

        var classificationOptions = TestOptionsProvider.Create(ClassificationOptions.Default);
        var searchOptions = FindReferencesSearchOptions.Default;
        var item = await DefinitionItemFactory.ToClassifiedDefinitionItemAsync(r, classificationOptions, solution, searchOptions, isPrimary: true, includeHiddenLocations: true, CancellationToken.None);

        VerifyDefinitionItem(item, project, symbols: [(c, nameof(c)), (r, nameof(r)), (i, nameof(i))],
            displayParts:
            [
                (tag: "Keyword", text: "int", TaggedTextStyle.None, target: SymbolKey.CreateString(i), hint: "int"),
                (tag: "Space", text: " ", TaggedTextStyle.None, target: null, hint: null),
                (tag: "RangeVariable", text: "x", TaggedTextStyle.None, target: SymbolKey.CreateString(r), hint: "? x")
            ],
            nameDisplayParts:
            [
                (tag: "RangeVariable", text: "x", TaggedTextStyle.None, target: SymbolKey.CreateString(r), hint: "? x")
            ],
            sourceSpans:
            [
                "Test1.cs [162..163)"
            ],
            tags:
            [
                "RangeVariable"
            ],
            properties:
            [
                ("Primary", "")
            ],
            displayableProperties:
            [
                ("ContainingMemberInfo", "M"),
                ("ContainingTypeInfo", "C")
            ]);
    }
}
