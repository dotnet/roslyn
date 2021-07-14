// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.CSharp.Analyzers.MatchFolderAndNamespace.CSharpMatchFolderAndNamespaceDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.CSharp.CodeFixes.MatchFolderAndNamespace.CSharpChangeNamespaceToMatchFolderCodeFixProvider>;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.UnitTests.MatchFolderAndNamespace
{
    public class CSharpMatchFolderAndNamespaceTests
    {
        private static readonly string Directory = Path.Combine("Test", "Directory");

        // DefaultNamespace gets exposed as RootNamespace in the build properties
        private const string DefaultNamespace = "Test.Root.Namespace";

        private static readonly string EditorConfig = @$"
is_global=true
build_property.ProjectDir = {Directory}
build_property.RootNamespace = {DefaultNamespace}
";

        private static string CreateFolderPath(params string[] folders)
            => Path.Combine(Directory, Path.Combine(folders));

        private static Task RunTestAsync(string fileName, string fileContents, string? directory = null, string? editorConfig = null, string? fixedCode = null)
        {
            var filePath = Path.Combine(directory ?? Directory, fileName);
            fixedCode ??= fileContents;

            return RunTestAsync(
                new[] { (filePath, fileContents) },
                new[] { (filePath, fixedCode) },
                editorConfig);
        }

        private static Task RunTestAsync(IEnumerable<(string, string)> originalSources, IEnumerable<(string, string)>? fixedSources = null, string? editorconfig = null)
        {
            var testState = new VerifyCS.Test
            {
                EditorConfig = editorconfig ?? EditorConfig,
                CodeFixTestBehaviors = CodeAnalysis.Testing.CodeFixTestBehaviors.SkipFixAllInDocumentCheck
            };

            foreach (var (fileName, content) in originalSources)
            {
                testState.TestState.Sources.Add((fileName, content));
            }

            fixedSources ??= Array.Empty<(string, string)>();
            foreach (var (fileName, content) in fixedSources)
            {
                testState.FixedState.Sources.Add((fileName, content));
            }

            return testState.RunAsync();
        }

        [Fact]
        public Task InvalidFolderName1_NoDiagnostic()
        {
            // No change namespace action because the folder name is not valid identifier
            var folder = CreateFolderPath(new[] { "3B", "C" });
            var code =
@"
namespace A.B
{    
    class Class1
    {
    }
}";

            return RunTestAsync(
                "File1.cs",
                code,
                directory: folder);
        }

        [Fact]
        public Task InvalidFolderName2_NoDiagnostic()
        {
            // No change namespace action because the folder name is not valid identifier
            var folder = CreateFolderPath(new[] { "B.3C", "D" });
            var code =
@"
namespace A.B
{    
    class Class1
    {
    }
}";

            return RunTestAsync(
                "File1.cs",
                code,
                directory: folder);
        }

        [Fact]
        public Task InvalidFolderName3_NoDiagnostic()
        {
            // No change namespace action because the folder name is not valid identifier
            var folder = CreateFolderPath(new[] { ".folder", "..subfolder", "name" });
            var code =
@"
namespace A.B
{    
    class Class1
    {
    }
}";

            return RunTestAsync(
                "File1.cs",
                code,
                directory: folder);
        }

        [Fact]
        public Task CaseInsensitiveMatch_NoDiagnostic()
        {
            var folder = CreateFolderPath(new[] { "A", "B" });
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
        public async Task SingleDocumentNoReference()
        {
            var folder = CreateFolderPath("B", "C");
            var code =
@"namespace [|A.B|]
{
    class Class1
    {
    }
}";

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
        public async Task SingleDocumentNoReference_NoDefaultNamespace()
        {
            var editorConfig = @$"
is_global=true
build_property.ProjectDir = {Directory}
";

            var folder = CreateFolderPath("B", "C");
            var code =
@"namespace [|A.B|]
{
    class Class1
    {
    }
}";

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
                editorConfig: editorConfig);
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
@"namespace A.B
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
}";

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
@"namespace A.B
{
    partial class ABClass
    {
        void M1() {}
    }
}";

            var code2 =
@"namespace A.B
{
    partial class ABClass
    {
        void M2() {}
    }
}";

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
    }
}
