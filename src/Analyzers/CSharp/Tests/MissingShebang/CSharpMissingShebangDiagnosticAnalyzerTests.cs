// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.MissingShebang;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MissingShebang;

using VerifyCS = CSharpCodeFixVerifier<CSharpMissingShebangDiagnosticAnalyzer, EmptyCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
public sealed class CSharpMissingShebangDiagnosticAnalyzerTests
{
    private static string CreateEditorConfig(string entryPointFilePath)
        => $"""
            is_global = true
            build_property.EntryPointFilePath = {entryPointFilePath}
            """;

    [Fact]
    public Task EntryPointFileWithoutShebang_Warning()
        => new VerifyCS.Test
        {
            TestState =
            {
                Sources =
                {
                    ("/0/Test0.cs", """
                        {|IDE0400:using|} System;
                        Console.WriteLine("Hello");
                        """),
                },
            },
            EditorConfig = CreateEditorConfig("/0/Test0.cs"),
        }.RunAsync();

    [Fact]
    public Task EntryPointFileWithShebang_NoDiagnostic()
        => new VerifyCS.Test
        {
            TestState =
            {
                Sources =
                {
                    ("/0/Test0.cs", """
                        #!/usr/bin/env dotnet run
                        using System;
                        Console.WriteLine("Hello");
                        """),
                },
            },
            EditorConfig = CreateEditorConfig("/0/Test0.cs"),
        }.RunAsync();

    [Fact]
    public Task NoEntryPointFilePathProperty_NoDiagnostic()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                Console.WriteLine("Hello");
                """,
        }.RunAsync();

    [Fact]
    public Task NonEntryPointFileWithoutShebang_NoDiagnostic()
        => new VerifyCS.Test
        {
            TestState =
            {
                Sources =
                {
                    ("/0/Test0.cs", """
                        using System;
                        Console.WriteLine("Hello");
                        """),
                },
            },
            EditorConfig = CreateEditorConfig("/0/Other.cs"),
        }.RunAsync();

    [Fact]
    public Task EmptyEntryPointFilePath_NoDiagnostic()
        => new VerifyCS.Test
        {
            TestState =
            {
                Sources =
                {
                    ("/0/Test0.cs", """
                        using System;
                        Console.WriteLine("Hello");
                        """),
                },
            },
            EditorConfig = CreateEditorConfig(""),
        }.RunAsync();
}
