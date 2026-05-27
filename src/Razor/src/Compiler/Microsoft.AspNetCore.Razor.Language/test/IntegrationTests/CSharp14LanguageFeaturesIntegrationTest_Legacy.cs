// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

// Language features not covered by tests:
// - First-class Span Types: still primarily a compiler/runtime interaction surface and not clearly isolated by a Razor-specific snippet here.
// - Simple lambda parameters with modifiers: syntax is still evolving and not yet represented with a stable Razor-specific case here.
public sealed class CSharp14LanguageFeaturesIntegrationTest_Legacy : IntegrationTestBase
{
    private const string DefaultLegacyFileName = "TestView.cshtml";
    private CSharpParseOptions? _csharpParseOptions;

    private const string LegacyTemplateBaseSource =
        """
        public abstract class LegacyTemplateBase
        {
            public virtual System.Threading.Tasks.Task ExecuteAsync()
                => System.Threading.Tasks.Task.CompletedTask;

            protected void WriteLiteral(string value)
            {
            }

            protected void Write(object value)
            {
            }
        }
        """;

    public CSharp14LanguageFeaturesIntegrationTest_Legacy()
        : base(layer: TestProject.Layer.Compiler)
    {
        AddCSharpSyntaxTree(LegacyTemplateBaseSource, filePath: "LegacyTemplateBase.cs");
    }

    protected override CSharpParseOptions CSharpParseOptions => _csharpParseOptions ?? base.CSharpParseOptions;

    public override string GetTestFileName([CallerMemberName] string? testName = null)
    {
        var fileName = $"TestFiles/IntegrationTests/{GetType().Name}/{testName}";
        var directory = Path.GetDirectoryName(fileName);
        if (directory is not null)
        {
            Directory.CreateDirectory(Path.Combine(TestProjectRoot, directory));
        }

        return fileName;
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-14.0/field-keyword.md")]
    public void FieldKeywordInProperties()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                Value = 1;
            }

            <p>@Value</p>

            @functions {
                public int Value
                {
                    get => field;
                    set => field = value;
                }
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-14.0/partial-events-and-constructors.md")]
    public void PartialEventsAndConstructors()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                var value = new Example();
                value.Updated += static () => { };
                _ = value.Value;
            }

            @functions {
                public partial class Example
                {
                    public partial event System.Action Updated;
                    public partial event System.Action Updated
                    {
                        add { }
                        remove { }
                    }

                    public int Value { get; }

                    public partial Example();
                    public partial Example()
                    {
                        Value = 1;
                    }
                }
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/76234")]
    public void StringLiteralsInDataSectionAsUtf8()
    {
        _csharpParseOptions = base.CSharpParseOptions.WithFeatures([new KeyValuePair<string, string>("experimental-data-section-string-literals", "0")]);
        CSharpSyntaxTrees.Clear();
        AddCSharpSyntaxTree(LegacyTemplateBaseSource, filePath: "LegacyTemplateBase.cs");

        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            <section>Razor markup literal emitted from generated code.</section>
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);

        var compiled = CompileToAssembly(generated);
        using var peReader = new PEReader(new MemoryStream(compiled.ImageBytes));
        AssertContainsDataSectionStringLiteralType(peReader.GetMetadataReader());
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-14.0/unbound-generic-types-in-nameof.md")]
    public void UnboundGenericTypesInNameof()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                _ = nameof(System.Collections.Generic.List<>);
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-14.0/extensions.md")]
    public void Extensions()
    {
        AddCSharpSyntaxTree("""
            public static class NumberExtensions
            {
                extension(int value)
                {
                    public int Double()
                        => value * 2;
                }
            }
            """);

        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                var value = 1;
                _ = value.Double();
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-14.0/null-conditional-assignment.md")]
    public void NullConditionalAssignment()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                int[] values = new int[1];
                values?[0] = 1;
                _ = values[0];
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-14.0/user-defined-compound-assignment.md")]
    public void UserDefinedCompoundAssignmentOperators()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                var counter = new Counter();
                counter += 1;
                _ = counter.Value;
            }

            @functions {
                public class Counter
                {
                    public int Value { get; private set; }
                
                    public void operator +=(int value)
                    {
                        Value += value;
                    }
                }
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/pull/83862")]
    public void IgnoredDirectivesDoNotBreakCodeGeneration()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @functions {
                #:package Newtonsoft.Json@13.0.3
                private static readonly int Value = 1;
            }

            <p>@Value</p>
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-14.0/optional-and-named-parameters-in-expression-trees.md")]
    public void OptionalAndNamedArgumentsInExpressionTrees()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @functions {
                public static int Sum(int first = 0, int second = 0)
                    => first + second;
            }
            
            @{
                System.Linq.Expressions.Expression<System.Func<int>> named = () => Sum(first: 1, second: 2);
                System.Linq.Expressions.Expression<System.Func<int>> optional = () => Sum();
                _ = named.Body;
                _ = optional.Body;
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    private static void AssertContainsDataSectionStringLiteralType(MetadataReader metadataReader)
    {
        var typeNames = metadataReader.TypeDefinitions
            .Select(handle => metadataReader.GetString(metadataReader.GetTypeDefinition(handle).Name))
            .ToArray();

        Assert.Contains("<PrivateImplementationDetails>", typeNames);
        Assert.Contains(typeNames, static name => name.StartsWith("<S>", StringComparison.Ordinal));
    }
}
