// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Test.Utilities;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.CSharp.Analyzers.MatchFolderAndNamespace.CSharpMatchFolderAndNamespaceDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.CSharp.CodeFixes.MatchFolderAndNamespace.CSharpChangeNamespaceToMatchFolderCodeFixProvider>;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.UnitTests.MatchFolderAndNamespace;

public class CSharpMatchFolderAndNamespaceTests
{
    private static readonly string Directory = "/0/";

    // DefaultNamespace gets exposed as RootNamespace in the build properties
    private const string DefaultNamespace = "Test.Root.Namespace";

    private static readonly string EditorConfig = @$"
is_global=true
build_property.ProjectDir = {Directory}
build_property.RootNamespace = {DefaultNamespace}
";

    private static string CreateFolderPath(params string[] folders)
        => Path.Combine(Directory, Path.Combine(folders));

    private static Task RunTestAsync(string fileName, string fileContents, string? directory = null, string? editorConfig = null, string? fixedCode = null, string? defaultNamespace = null)
    {
        var filePath = Path.Combine(directory ?? Directory, fileName);
        fixedCode ??= fileContents;

        return RunTestAsync(
            new[] { (filePath, fileContents) },
            new[] { (filePath, fixedCode) },
            editorConfig,
            defaultNamespace);
    }

    private static Task RunTestAsync(IEnumerable<(string, string)> originalSources, IEnumerable<(string, string)>? fixedSources = null, string? editorconfig = null, string? defaultNamespace = null)
    {
        // When a namespace isn't provided we will fallback on our default
        defaultNamespace ??= DefaultNamespace;

        var testState = new VerifyCS.Test
        {
            EditorConfig = editorconfig ?? EditorConfig,
            CodeFixTestBehaviors = CodeAnalysis.Testing.CodeFixTestBehaviors.SkipFixAllInDocumentCheck,
            LanguageVersion = LanguageVersion.CSharp10,
        };

        foreach (var (fileName, content) in originalSources)
            testState.TestState.Sources.Add((fileName, content));

        fixedSources ??= [];
        foreach (var (fileName, content) in fixedSources)
            testState.FixedState.Sources.Add((fileName, content));

        // If empty string was provided as the namespace, then we will not set a default
        if (defaultNamespace.Length > 0)
        {
            testState.SolutionTransforms.Add((solution, projectId) =>
            {
                var project = solution.GetRequiredProject(projectId);
                return project.WithDefaultNamespace(defaultNamespace).Solution;
            });
        }

        return testState.RunAsync();
    }

    [Fact]
    public Task InvalidFolderName1_NoDiagnostic()
    {
        // No change namespace action because the folder name is not valid identifier
        var folder = CreateFolderPath(["3B", "C"]);
        var code =
            """
            namespace A.B
            {
                class Class1
                {
                }
            }
            """;

        return RunTestAsync(
            "File1.cs",
            code,
            directory: folder);
    }

    [Fact]
    public Task InvalidFolderName1_NoDiagnostic_FileScopedNamespace()
    {
        // No change namespace action because the folder name is not valid identifier
        var folder = CreateFolderPath(["3B", "C"]);
        var code =
            """
            namespace A.B;

            class Class1
            {
            }
            """;

        return RunTestAsync(
            "File1.cs",
            code,
            directory: folder);
    }

    [Fact]
    public Task InvalidFolderName2_NoDiagnostic()
    {
        // No change namespace action because the folder name is not valid identifier
        var folder = CreateFolderPath(["B.3C", "D"]);
        var code =
            """
            namespace A.B
            {
                class Class1
                {
                }
            }
            """;

        return RunTestAsync(
            "File1.cs",
            code,
            directory: folder);
    }

    [Fact]
    public Task InvalidFolderName3_NoDiagnostic()
    {
        // No change namespace action because the folder name is not valid identifier
        var folder = CreateFolderPath([".folder", "..subfolder", "name"]);
        var code =
            """
            namespace A.B
            {
                class Class1
                {
                }
            }
            """;

        return RunTestAsync(
            "File1.cs",
            code,
            directory: folder);
    }

    [Fact]
    public Task CaseInsensitiveMatch_NoDiagnostic()
    {
        var folder = CreateFolderPath(["A", "B"]);
        var code =
@$"
namespace {DefaultNamespace}.a.b
{{
    class Class1
    {{
    }}
}}";

        return RunTestAsync(
            "File1.cs",
            code,
            directory: folder);
    }

    [Fact]
    public async Task CodeStyleOptionIsFalse()
    {
        var folder = CreateFolderPath("B", "C");
        var code =
            """
            namespace A.B
            {
                class Class1
                {
                }
            }
            """;

        await RunTestAsync(
            fileName: "Class1.cs",
            fileContents: code,
            directory: folder,
            editorConfig: EditorConfig + """
            dotnet_style_namespace_match_folder = false
            """
);
    }

    [Fact]
    public async Task SingleDocumentNoReference()
    {
        var folder = CreateFolderPath("B", "C");
        var code =
            """
            namespace [|A.B|]
            {
                class Class1
                {
                }
            }
            """;

        var fixedCode =
@$"namespace {DefaultNamespace}.B.C
{{
    class Class1
    {{
    }}
}}";
        await RunTestAsync(
            fileName: "Class1.cs",
            fileContents: code,
            directory: folder,
            fixedCode: fixedCode);
    }

    [Fact]
    public async Task SingleDocumentNoReference_FileScopedNamespace()
    {
        var folder = CreateFolderPath("B", "C");
        var code =
            """
            namespace [|A.B|];

            class Class1
            {
            }
            """;

        var fixedCode =
@$"namespace {DefaultNamespace}.B.C;

class Class1
{{
}}";
        await RunTestAsync(
            fileName: "Class1.cs",
            fileContents: code,
            directory: folder,
            fixedCode: fixedCode);
    }

    [Fact]
    public async Task SingleDocumentNoReference_NoDefaultNamespace()
    {
        var editorConfig = @$"
is_global=true
build_property.ProjectDir = {Directory}
";

        var folder = CreateFolderPath("B", "C");
        var code =
            """
            namespace [|A.B|]
            {
                class Class1
                {
                }
            }
            """;

        var fixedCode =
@$"namespace B.C
{{
    class Class1
    {{
    }}
}}";
        await RunTestAsync(
            fileName: "Class1.cs",
            fileContents: code,
            directory: folder,
            fixedCode: fixedCode,
            editorConfig: editorConfig,
            // passing empty string means that a default namespace isn't set on the test Project
            defaultNamespace: string.Empty);
    }

    [Fact]
    public async Task SingleDocumentNoReference_NoDefaultNamespace_FileScopedNamespace()
    {
        var editorConfig = @$"
is_global=true
build_property.ProjectDir = {Directory}
";

        var folder = CreateFolderPath("B", "C");
        var code =
            """
            namespace [|A.B|];

            class Class1
            {
            }
            """;

        var fixedCode =
            """
            namespace B.C;

            class Class1
            {
            }
            """;
        await RunTestAsync(
            fileName: "Class1.cs",
            fileContents: code,
            directory: folder,
            fixedCode: fixedCode,
            editorConfig: editorConfig,
            // passing empty string means that a default namespace isn't set on the test Project
            defaultNamespace: string.Empty);
    }

    [Fact]
    public async Task NamespaceWithSpaces_NoDiagnostic()
    {
        var folder = CreateFolderPath("A", "B");
        var code =
@$"namespace {DefaultNamespace}.A    .     B
{{
    class Class1
    {{
    }}
}}";

        await RunTestAsync(
            fileName: "Class1.cs",
            fileContents: code,
            directory: folder);
    }

    [Fact]
    public async Task NestedNamespaces_NoDiagnostic()
    {
        // The code fix doesn't currently support nested namespaces for sync, so
        // diagnostic does not report.

        var folder = CreateFolderPath("B", "C");
        var code =
            """
            namespace A.B
            {
                namespace C.D
                {
                    class CDClass
                    {
                    }
                }

                class ABClass
                {
                }
            }
            """;

        await RunTestAsync(
            fileName: "Class1.cs",
            fileContents: code,
            directory: folder);
    }

    [Fact]
    public async Task PartialTypeWithMultipleDeclarations_NoDiagnostic()
    {
        // The code fix doesn't currently support nested namespaces for sync, so
        // diagnostic does not report.

        var folder = CreateFolderPath("B", "C");
        var code1 =
            """
            namespace A.B
            {
                partial class ABClass
                {
                    void M1() {}
                }
            }
            """;

        var code2 =
            """
            namespace A.B
            {
                partial class ABClass
                {
                    void M2() {}
                }
            }
            """;

        var sources = new[]
        {
            (Path.Combine(folder, "ABClass1.cs"), code1),
            (Path.Combine(folder, "ABClass2.cs"), code2),
        };

        await RunTestAsync(sources);
    }

    [Fact]
    public async Task FileNotInProjectFolder_NoDiagnostic()
    {
        // Default directory is Test\Directory for the project,
        // putting the file outside the directory should have no
        // diagnostic shown.

        var folder = Path.Combine("B", "C");
        var code =
$@"namespace A.B
{{
    class ABClass
    {{
    }}
}}";

        await RunTestAsync(
            fileName: "Class1.cs",
            fileContents: code,
            directory: folder);
    }

    [Fact]
    public async Task SingleDocumentLocalReference()
    {
        var @namespace = "Bar.Baz";

        var folder = CreateFolderPath("A", "B", "C");
        var code =
$@"
namespace [|{@namespace}|]
{{
    delegate void D1();

    interface Class1
    {{
        void M1();
    }}

    class Class2 : {@namespace}.Class1
    {{
        {@namespace}.D1 d;

        void {@namespace}.Class1.M1(){{}}
    }}
}}";

        var expected =
@$"namespace {DefaultNamespace}.A.B.C
{{
    delegate void D1();

    interface Class1
    {{
        void M1();
    }}

    class Class2 : Class1
    {{
        D1 d;

        void Class1.M1() {{ }}
    }}
}}";

        await RunTestAsync(
            "Class1.cs",
            code,
            folder,
            fixedCode: expected);
    }

    [Fact]
    public async Task ChangeUsingsInMultipleContainers()
    {
        var declaredNamespace = "Bar.Baz";

        var folder = CreateFolderPath("A", "B", "C");
        var code1 =
$@"namespace [|{declaredNamespace}|]
{{
    class Class1
    {{
    }}
}}";

        var code2 =
$@"namespace NS1
{{
    using {declaredNamespace};

    class Class2
    {{
        Class1 c2;
    }}

    namespace NS2
    {{
        using {declaredNamespace};

        class Class2
        {{
            Class1 c1;
        }}
    }}
}}";

        var fixed1 =
@$"namespace {DefaultNamespace}.A.B.C
{{
    class Class1
    {{
    }}
}}";

        var fixed2 =
@$"namespace NS1
{{
    using {DefaultNamespace}.A.B.C;

    class Class2
    {{
        Class1 c2;
    }}

    namespace NS2
    {{
        class Class2
        {{
            Class1 c1;
        }}
    }}
}}";

        var originalSources = new[]
        {
            (Path.Combine(folder, "Class1.cs"), code1),
            ("Class2.cs", code2)
        };

        var fixedSources = new[]
        {
            (Path.Combine(folder, "Class1.cs"), fixed1),
            ("Class2.cs", fixed2)
        };

        await RunTestAsync(originalSources, fixedSources);
    }

    [Fact]
    public async Task DocumentAtRoot_NoDiagnostic()
    {
        var folder = CreateFolderPath();

        var code = $@"
namespace {DefaultNamespace}
{{
    class C {{ }}
}}";

        await RunTestAsync(
            "File1.cs",
            code,
            folder);
    }

    [Fact]
    public async Task DocumentAtRoot_ChangeNamespace()
    {
        var folder = CreateFolderPath();

        var code =
$@"namespace [|{DefaultNamespace}.Test|]
{{
    class C {{ }}
}}";

        var fixedCode =
$@"namespace {DefaultNamespace}
{{
    class C {{ }}
}}";

        await RunTestAsync(
            "File1.cs",
            code,
            folder,
            fixedCode: fixedCode);
    }

    [Fact]
    public async Task ChangeNamespace_WithAliasReferencesInOtherDocument()
    {
        var declaredNamespace = $"Bar.Baz";

        var folder = CreateFolderPath("A", "B", "C");
        var code1 =
$@"namespace [|{declaredNamespace}|]
{{
    class Class1
    {{
    }}
}}";

        var code2 = $@"
using System;
using {declaredNamespace};
using Class1Alias = {declaredNamespace}.Class1;

namespace Foo
{{
    class RefClass
    {{
        private Class1Alias c1;
    }}
}}";

        var fixed1 =
@$"namespace {DefaultNamespace}.A.B.C
{{
    class Class1
    {{
    }}
}}";

        var fixed2 =
@$"
using System;
using Class1Alias = {DefaultNamespace}.A.B.C.Class1;

namespace Foo
{{
    class RefClass
    {{
        private Class1Alias c1;
    }}
}}";

        var originalSources = new[]
        {
            (Path.Combine(folder, "Class1.cs"), code1),
            ("Class2.cs", code2)
        };

        var fixedSources = new[]
        {
            (Path.Combine(folder, "Class1.cs"), fixed1),
            ("Class2.cs", fixed2)
        };

        await RunTestAsync(originalSources, fixedSources);
    }

    [Fact]
    public async Task FixAll()
    {
        var declaredNamespace = "Bar.Baz";

        var folder1 = CreateFolderPath("A", "B", "C");
        var fixedNamespace1 = $"{DefaultNamespace}.A.B.C";

        var folder2 = CreateFolderPath("Second", "Folder", "Path");
        var fixedNamespace2 = $"{DefaultNamespace}.Second.Folder.Path";

        var folder3 = CreateFolderPath("Third", "Folder", "Path");
        var fixedNamespace3 = $"{DefaultNamespace}.Third.Folder.Path";

        var code1 =
$@"namespace [|{declaredNamespace}|]
{{
    class Class1
    {{
        Class2 C2 {{ get; }}
        Class3 C3 {{ get; }}
    }}
}}";

        var fixed1 =
$@"using {fixedNamespace2};
using {fixedNamespace3};

namespace {fixedNamespace1}
{{
    class Class1
    {{
        Class2 C2 {{ get; }}
        Class3 C3 {{ get; }}
    }}
}}";

        var code2 =
$@"namespace [|{declaredNamespace}|]
{{
    class Class2
    {{
        Class1 C1 {{ get; }}
        Class3 C3 {{ get; }}
    }}
}}";

        var fixed2 =
$@"using {fixedNamespace1};
using {fixedNamespace3};

namespace {fixedNamespace2}
{{
    class Class2
    {{
        Class1 C1 {{ get; }}
        Class3 C3 {{ get; }}
    }}
}}";

        var code3 =
$@"namespace [|{declaredNamespace}|]
{{
    class Class3
    {{
        Class1 C1 {{ get; }}
        Class2 C2 {{ get; }}
    }}
}}";

        var fixed3 =
$@"using {fixedNamespace1};
using {fixedNamespace2};

namespace {fixedNamespace3}
{{
    class Class3
    {{
        Class1 C1 {{ get; }}
        Class2 C2 {{ get; }}
    }}
}}";

        var sources = new[]
        {
            (Path.Combine(folder1, "Class1.cs"), code1),
            (Path.Combine(folder2, "Class2.cs"), code2),
            (Path.Combine(folder3, "Class3.cs"), code3),
        };

        var fixedSources = new[]
        {
            (Path.Combine(folder1, "Class1.cs"), fixed1),
            (Path.Combine(folder2, "Class2.cs"), fixed2),
            (Path.Combine(folder3, "Class3.cs"), fixed3),
        };

        await RunTestAsync(sources, fixedSources);
    }

    [Fact]
    public async Task FixAll_MultipleProjects()
    {
        var declaredNamespace = "Bar.Baz";

        var folder1 = CreateFolderPath("A", "B", "C");
        var fixedNamespace1 = $"{DefaultNamespace}.A.B.C";

        var folder2 = CreateFolderPath("Second", "Folder", "Path");
        var fixedNamespace2 = $"{DefaultNamespace}.Second.Folder.Path";

        var folder3 = CreateFolderPath("Third", "Folder", "Path");
        var fixedNamespace3 = $"{DefaultNamespace}.Third.Folder.Path";

        var code1 =
$@"namespace [|{declaredNamespace}|]
{{
    public class Class1
    {{
        Class2 C2 {{ get; }}
        Class3 C3 {{ get; }}
    }}
}}";

        var fixed1 =
$@"using {fixedNamespace2};
using {fixedNamespace3};

namespace {fixedNamespace1}
{{
    public class Class1
    {{
        Class2 C2 {{ get; }}
        Class3 C3 {{ get; }}
    }}
}}";

        var code2 =
$@"namespace [|{declaredNamespace}|]
{{
    class Class2
    {{
        Class1 C1 {{ get; }}
        Class3 C3 {{ get; }}
    }}
}}";

        var fixed2 =
$@"using {fixedNamespace1};
using {fixedNamespace3};

namespace {fixedNamespace2}
{{
    class Class2
    {{
        Class1 C1 {{ get; }}
        Class3 C3 {{ get; }}
    }}
}}";

        var code3 =
$@"namespace [|{declaredNamespace}|]
{{
    class Class3
    {{
        Class1 C1 {{ get; }}
        Class2 C2 {{ get; }}
    }}
}}";

        var fixed3 =
$@"using {fixedNamespace1};
using {fixedNamespace2};

namespace {fixedNamespace3}
{{
    class Class3
    {{
        Class1 C1 {{ get; }}
        Class2 C2 {{ get; }}
    }}
}}";

        var project2Directory = "/Project2/";
        var project2folder = Path.Combine(project2Directory, "A", "B", "C");
        var project2EditorConfig = @$"
is_global=true
build_property.ProjectDir = {project2Directory}
build_property.RootNamespace = {DefaultNamespace}
";

        var project2Source =
@$"using {declaredNamespace};

namespace [|Project2.Test|]
{{
    class P
    {{
        Class1 _c1;
    }}
}}";

        var project2FixedSource =
$@"namespace {fixedNamespace1}
{{
    class P
    {{
        Class1 _c1;
    }}
}}";

        var testState = new VerifyCS.Test
        {
            EditorConfig = EditorConfig,
            CodeFixTestBehaviors = CodeAnalysis.Testing.CodeFixTestBehaviors.SkipFixAllInDocumentCheck | CodeAnalysis.Testing.CodeFixTestBehaviors.SkipFixAllInProjectCheck,
            LanguageVersion = LanguageVersion.CSharp10,
            TestState =
            {
                Sources =
                {
                    (Path.Combine(folder1, "Class1.cs"), code1),
                    (Path.Combine(folder2, "Class2.cs"), code2),
                    (Path.Combine(folder3, "Class3.cs"), code3),
                },
                AdditionalProjects =
                {
                    ["Project2"] =
                    {
                        AdditionalProjectReferences = { "TestProject" },
                        Sources = { (Path.Combine(project2folder, "P.cs"), project2Source) },
                        AnalyzerConfigFiles = { (Path.Combine(project2Directory, ".editorconfig"), project2EditorConfig) },
                    },
                },
            },
            FixedState =
            {
                Sources =
                {
                    (Path.Combine(folder1, "Class1.cs"), fixed1),
                    (Path.Combine(folder2, "Class2.cs"), fixed2),
                    (Path.Combine(folder3, "Class3.cs"), fixed3),
                },
                AdditionalProjects =
                {
                    ["Project2"] =
                    {
                        AdditionalProjectReferences = { "TestProject" },
                        Sources = { (Path.Combine(project2folder, "P.cs"), project2FixedSource) },
                        AnalyzerConfigFiles = { (Path.Combine(project2Directory, ".editorconfig"), project2EditorConfig) },
                    }
                }
            }
        };

        testState.SolutionTransforms.Add((solution, projectId) =>
        {
            foreach (var id in solution.ProjectIds)
            {
                var project = solution.GetRequiredProject(id);
                solution = project.WithDefaultNamespace(DefaultNamespace).Solution;
            }
            return solution;
        });

        await testState.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58372")]
    public async Task InvalidProjectName_ChangeNamespace()
    {
        var defaultNamespace = "Invalid-Namespace";
        var editorConfig = @$"
is_global=true
build_property.ProjectDir = {Directory}
build_property.RootNamespace = {defaultNamespace}
";

        var folder = CreateFolderPath(["B", "C"]);
        var code =
            """
            namespace [|A.B|]
            {
                class Class1
                {
                }
            }
            """;

        // The project name is invalid so the default namespace is not prepended
        var fixedCode =
@$"namespace B.C
{{
    class Class1
    {{
    }}
}}";

        await RunTestAsync(
            "Class1.cs",
            fileContents: code,
            fixedCode: fixedCode,
            directory: folder,
            editorConfig: editorConfig,
            defaultNamespace: defaultNamespace);
    }

    [Fact]
    public async Task InvalidProjectName_DocumentAtRoot_ChangeNamespace()
    {
        var defaultNamespace = "Invalid-Namespace";
        var editorConfig = @$"
is_global=true
build_property.ProjectDir = {Directory}
build_property.RootNamespace = {defaultNamespace}
";

        var folder = CreateFolderPath();

        var code =
$@"namespace Test.Code
{{
    class C {{ }}
}}";

        await RunTestAsync(
            "Class1.cs",
            fileContents: code,
            directory: folder,
            editorConfig: editorConfig,
            defaultNamespace: defaultNamespace);
    }

    [Fact]
    public async Task InvalidRootNamespace_DocumentAtRoot_ChangeNamespace()
    {
        var editorConfig = @$"
is_global=true
build_property.ProjectDir = {Directory}
build_property.RootNamespace = Test.Code # not an editorconfig comment even though it looks like one
";

        var folder = CreateFolderPath();

        var code =
$@"namespace Test.Code
{{
    class C {{ }}
}}";

        await RunTestAsync(
            "Class1.cs",
            fileContents: code,
            directory: folder,
            editorConfig: editorConfig,
            defaultNamespace: "Invalid-Namespace");
    }
}
