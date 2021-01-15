// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.CSharp.Analyzers.NamespaceSync.CSharpNamespaceSyncDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.CSharp.CodeFixes.NamespaceSync.CSharpNamespaceSyncCodeFixProvider>;
using System.IO;
using System.Collections.Generic;
using System;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.UnitTests.NamespaceSync
{
    public class NamespaceSyncTests
    {
        private static readonly string Directory = Path.Combine("Test", "Directory");
        private static readonly string EditorConfig = @$"
is_global=true
build_property.ProjectDir = {Directory}
";

        private static string CreateFolderPath(params string[] folders)
            => Path.Combine(Directory, Path.Combine(folders));

        private static Task RunTestAsync(string fileName, string fileContents, string? directory = null, string? editorconfig = null, string? fixedCode = null)
        {
            var filePath = Path.Combine(directory ?? Directory, fileName);
            fixedCode ??= fileContents;

            return RunTestAsync(
                new[] { (filePath, fileContents) },
                new[] { (filePath, fixedCode) },
                editorconfig);
        }

        private static Task RunTestAsync(IEnumerable<(string, string)> originalSources, IEnumerable<(string, string)>? fixedSources = null, string? editorconfig = null)
        {
            var testState = new VerifyCS.Test
            {
                EditorConfig = editorconfig ?? EditorConfig,
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
        public Task CaseInsensitiveMatch_NoDiagnostic()
        {
            var folder = CreateFolderPath(new[] { "A", "B" });
            var code =
@"
namespace a.b
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
@"namespace B.C
{
    class Class1
    {
    }
}";
            await RunTestAsync(
                fileName: "Class1.cs",
                fileContents: code,
                directory: folder,
                fixedCode: fixedCode);
        }

        [Fact]
        public async Task NamespaceWithSpaces_NoDiagnostic()
        {
            var folder = CreateFolderPath("B", "C");
            var code =
@"namespace [|A    .     B|]
{
    class Class1
    {
    }
}";

            var fixedCode =
@"namespace B.C
{
    class Class1
    {
    }
}";
            await RunTestAsync(
                fileName: "Class1.cs",
                fileContents: code,
                directory: folder,
                fixedCode: fixedCode);
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
@"namespace A.B.C
{
    delegate void D1();

    interface Class1
    {
        void M1();
    }

    class Class2 : Class1
    {
        D1 d;

        void Class1.M1() { }
    }
}";

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
@"namespace A.B.C
{
    class Class1
    {
    }
}";

            var fixed2 =
@"namespace NS1
{
    using A.B.C;

    class Class2
    {
        Class1 c2;
    }

    namespace NS2
    {
        class Class2
        {
            Class1 c1;
        }
    }
}";

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
            var declaredNamespace = "Bar.Baz";

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
@"namespace A.B.C
{
    class Class1
    {
    }
}";

            var fixed2 =
@"
using System;
using Class1Alias = A.B.C.Class1;

namespace Foo
{
    class RefClass
    {
        private Class1Alias c1;
    }
}";

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

        //        [Fact]
        //        public async Task ChangeToGlobalNamespace_SingleDocumentNoRef()
        //        {
        //            var defaultNamespace = "";
        //            var declaredNamespace = "Foo.Bar";

        //            var (folder, filePath) = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
        //            var code =
        //$@"
        //<Workspace>
        //    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        //        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
        //using System;

        //// Comments before declaration.
        //namespace [||]{declaredNamespace}
        //{{  // Comments after opening brace
        //    class Class1
        //    {{
        //    }}
        //    // Comments before closing brace
        //}} // Comments after declaration.
        //</Document>
        //    </Project>
        //</Workspace>";

        //            var expectedSourceOriginal =
        //@"using System;

        //// Comments before declaration.
        //// Comments after opening brace
        //class Class1
        //{
        //}
        //// Comments before closing brace
        //// Comments after declaration.
        //";
        //            await TestChangeNamespaceAsync(code, expectedSourceOriginal);
        //        }

        //        [Fact]
        //        public async Task ChangeToGlobalNamespace_SingleDocumentLocalRef()
        //        {
        //            var defaultNamespace = "";
        //            var declaredNamespace = "Foo.Bar";

        //            var (folder, filePath) = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
        //            var code =
        //$@"
        //<Workspace>
        //    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        //        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
        //namespace [||]{declaredNamespace}
        //{{
        //    delegate void D1;

        //    interface Class1
        //    {{
        //        void M1();
        //    }}

        //    class Class2 : {declaredNamespace}.Class1
        //    {{
        //        global::{declaredNamespace}.D1 d;  

        //        void {declaredNamespace}.Class1.M1() {{ }}
        //    }}
        //}}</Document>
        //    </Project>
        //</Workspace>";

        //            var expectedSourceOriginal =
        //@"delegate void D1;

        //interface Class1
        //{
        //    void M1();
        //}

        //class Class2 : Class1
        //{
        //    global::D1 d;

        //    void Class1.M1() { }
        //}
        //";
        //            await TestChangeNamespaceAsync(code, expectedSourceOriginal);
        //        }

        //        [Fact]
        //        public async Task ChangeToGlobalNamespace_WithReferencesInOtherDocument()
        //        {
        //            var defaultNamespace = "";
        //            var declaredNamespace = "Foo.Bar.Baz";

        //            var (folder, filePath) = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
        //            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
        //            var code =
        //$@"
        //<Workspace>
        //    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        //        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
        //namespace [||]{declaredNamespace}
        //{{
        //    class Class1 
        //    {{ 
        //    }}

        //    class Class2 
        //    {{ 
        //    }}
        //}}</Document>
        //<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
        //using Foo.Bar.Baz;

        //namespace Foo
        //{{
        //    class RefClass
        //    {{
        //        private Class1 c1;

        //        void M1()
        //        {{
        //            Bar.Baz.Class2 c2 = null;
        //        }}
        //    }}
        //}}</Document>
        //    </Project>
        //</Workspace>";

        //            var expectedSourceOriginal =
        //@"class Class1
        //{
        //}

        //class Class2
        //{
        //}
        //";
        //            var expectedSourceReference =
        //@"namespace Foo
        //{
        //    class RefClass
        //    {
        //        private Class1 c1;

        //        void M1()
        //        {
        //            Class2 c2 = null;
        //        }
        //    }
        //}";
        //            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        //        }

        //        [Fact]
        //        public async Task ChangeToGlobalNamespace_WithQualifiedReferencesInOtherDocument()
        //        {
        //            var defaultNamespace = "";
        //            var declaredNamespace = "Foo.Bar.Baz";

        //            var (folder, filePath) = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
        //            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
        //            var code =
        //$@"
        //<Workspace>
        //    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        //        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
        //namespace [||]{declaredNamespace}
        //{{
        //    interface Interface1 
        //    {{
        //        void M1(Interface1 c1);   
        //    }}
        //}}</Document>
        //<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
        //namespace Foo
        //{{
        //    using {declaredNamespace};

        //    class RefClass : Interface1
        //    {{
        //        void {declaredNamespace}.Interface1.M1(Interface1 c1){{}}
        //    }}
        //}}</Document>
        //    </Project>
        //</Workspace>";

        //            var expectedSourceOriginal =
        //@"interface Interface1
        //{
        //    void M1(Interface1 c1);
        //}
        //";
        //            var expectedSourceReference =
        //@"
        //namespace Foo
        //{
        //    class RefClass : Interface1
        //    {
        //        void Interface1.M1(Interface1 c1){}
        //    }
        //}";
        //            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        //        }

        //        [Fact]
        //        public async Task ChangeToGlobalNamespace_WithReferenceAndConflictDeclarationInOtherDocument()
        //        {
        //            var defaultNamespace = "";
        //            var declaredNamespace = "Foo.Bar.Baz";

        //            var (folder, filePath) = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
        //            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
        //            var code =
        //$@"
        //<Workspace>
        //    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        //        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
        //namespace [||]{declaredNamespace}
        //{{
        //    class MyClass 
        //    {{
        //    }}
        //}}</Document>
        //<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
        //namespace Foo
        //{{
        //    using {declaredNamespace};

        //    class RefClass
        //    {{
        //        Foo.Bar.Baz.MyClass c;
        //    }}

        //    class MyClass
        //    {{
        //    }}
        //}}</Document>
        //    </Project>
        //</Workspace>";

        //            var expectedSourceOriginal =
        //@"class MyClass
        //{
        //}
        //";
        //            var expectedSourceReference =
        //@"
        //namespace Foo
        //{
        //    class RefClass
        //    {
        //        global::MyClass c;
        //    }

        //    class MyClass
        //    {
        //    }
        //}";
        //            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        //        }

        //        [Fact]
        //        public async Task ChangeToGlobalNamespace_ReferencingTypesDeclaredInOtherDocument()
        //        {
        //            var defaultNamespace = "";
        //            var declaredNamespace = "Foo.Bar.Baz";

        //            var (folder, filePath) = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
        //            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
        //            var code =
        //$@"
        //<Workspace>
        //    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        //        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
        //namespace [||]{declaredNamespace}
        //{{
        //    class Class1 
        //    {{ 
        //        private Class2 c2;
        //        private Class3 c3;
        //        private Class4 c4;
        //    }}
        //}}</Document>
        //<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
        //namespace Foo
        //{{
        //    class Class2 {{}}

        //    namespace Bar
        //    {{
        //        class Class3 {{}}

        //        namespace Baz
        //        {{
        //            class Class4 {{}}    
        //        }}
        //    }}
        //}}</Document>
        //    </Project>
        //</Workspace>";

        //            var expectedSourceOriginal =
        //@"using Foo;
        //using Foo.Bar;
        //using Foo.Bar.Baz;

        //class Class1
        //{
        //    private Class2 c2;
        //    private Class3 c3;
        //    private Class4 c4;
        //}
        //";
        //            await TestChangeNamespaceAsync(code, expectedSourceOriginal);
        //        }

        //        [Fact]
        //        public async Task ChangeToGlobalNamespace_ChangeUsingsInMultipleContainers()
        //        {
        //            var defaultNamespace = "";
        //            var declaredNamespace = "Foo.Bar.Baz";

        //            var (folder, filePath) = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
        //            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
        //            var code =
        //$@"
        //<Workspace>
        //    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        //        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
        //namespace [||]{declaredNamespace}
        //{{
        //    class Class1
        //    {{
        //    }}
        //}}</Document>
        //<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
        //namespace NS1
        //{{
        //    using Foo.Bar.Baz;

        //    class Class2
        //    {{
        //        Class1 c2;
        //    }}

        //    namespace NS2
        //    {{
        //        using Foo.Bar.Baz;

        //        class Class2
        //        {{
        //            Class1 c1;
        //        }}
        //    }}
        //}}</Document>
        //    </Project>
        //</Workspace>";

        //            var expectedSourceOriginal =
        //@"class Class1
        //{
        //}
        //";
        //            var expectedSourceReference =
        //@"
        //namespace NS1
        //{
        //    class Class2
        //    {
        //        Class1 c2;
        //    }

        //    namespace NS2
        //    {
        //        class Class2
        //        {
        //            Class1 c1;
        //        }
        //    }
        //}";
        //            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        //        }

        //        [Fact]
        //        public async Task ChangeToGlobalNamespace_WithAliasReferencesInOtherDocument()
        //        {
        //            var defaultNamespace = "";
        //            var declaredNamespace = "Foo.Bar.Baz";

        //            var (folder, filePath) = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
        //            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
        //            var code =
        //$@"
        //<Workspace>
        //    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        //        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
        //namespace [||]{declaredNamespace}
        //{{
        //    class Class1 
        //    {{ 
        //    }}

        //    class Class2 
        //    {{ 
        //    }}
        //}}</Document>
        //<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}"">
        //using System;
        //using Class1Alias = Foo.Bar.Baz.Class1;

        //namespace Foo
        //{{
        //    class RefClass
        //    {{
        //        private Class1Alias c1;

        //        void M1()
        //        {{
        //            Bar.Baz.Class2 c2 = null;
        //        }}
        //    }}
        //}}</Document>
        //    </Project>
        //</Workspace>";

        //            var expectedSourceOriginal =
        //@"class Class1
        //{
        //}

        //class Class2
        //{
        //}
        //";
        //            var expectedSourceReference =
        //@"using System;
        //using Class1Alias = Class1;

        //namespace Foo
        //{
        //    class RefClass
        //    {
        //        private Class1Alias c1;

        //        void M1()
        //        {
        //            Class2 c2 = null;
        //        }
        //    }
        //}";
        //            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        //        }

        //        [Fact]
        //        public async Task ChangeFromGlobalNamespace_SingleDocumentNoRef()
        //        {
        //            var defaultNamespace = "A";
        //            var (folder, filePath) = CreateDocumentFilePath(new[] { "B", "C" }, "File1.cs");
        //            var code =
        //$@"
        //<Workspace>
        //    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        //        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
        //using System;

        //class [||]Class1
        //{{
        //}}
        //</Document>
        //    </Project>
        //</Workspace>";

        //            var expectedSourceOriginal =
        //@"using System;

        //namespace A.B.C
        //{
        //    class Class1
        //    {
        //    }
        //}";
        //            await TestChangeNamespaceAsync(code, expectedSourceOriginal);
        //        }

        //        [Fact]
        //        public async Task ChangeFromGlobalNamespace_SingleDocumentLocalRef()
        //        {
        //            var defaultNamespace = "A";
        //            var (folder, filePath) = CreateDocumentFilePath(new[] { "B", "C" }, "File1.cs");
        //            var code =
        //$@"
        //<Workspace>
        //    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        //        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
        //delegate void [||]D1;

        //interface Class1
        //{{
        //    void M1();
        //}}

        //class Class2 : Class1
        //{{
        //    D1 d;  

        //    void Class1.M1() {{ }}
        //}}</Document>
        //    </Project>
        //</Workspace>";

        //            var expectedSourceOriginal =
        //@"namespace A.B.C
        //{
        //    delegate void D1;

        //    interface Class1
        //    {
        //        void M1();
        //    }

        //    class Class2 : Class1
        //    {
        //        D1 d;

        //        void Class1.M1() { }
        //    }
        //}";
        //            await TestChangeNamespaceAsync(code, expectedSourceOriginal);
        //        }

        //        [Fact]
        //        public async Task ChangeFromGlobalNamespace_WithReferencesInOtherDocument()
        //        {
        //            var defaultNamespace = "A";

        //            var (folder, filePath) = CreateDocumentFilePath(new[] { "B", "C" }, "File1.cs");
        //            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
        //            var code =
        //$@"
        //<Workspace>
        //    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        //        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
        //class [||]Class1 
        //{{ 
        //}}

        //class Class2 
        //{{ 
        //}}</Document>
        //<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
        //namespace Foo
        //{{
        //    class RefClass
        //    {{
        //        private Class1 c1;

        //        void M1()
        //        {{
        //            Class2 c2 = null;
        //        }}
        //    }}
        //}}</Document>
        //    </Project>
        //</Workspace>";

        //            var expectedSourceOriginal =
        //@"namespace A.B.C
        //{
        //    class Class1
        //    {
        //    }

        //    class Class2
        //    {
        //    }
        //}";
        //            var expectedSourceReference =
        //@"
        //using A.B.C;

        //namespace Foo
        //{
        //    class RefClass
        //    {
        //        private Class1 c1;

        //        void M1()
        //        {
        //            Class2 c2 = null;
        //        }
        //    }
        //}";
        //            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        //        }

        //        [Fact]
        //        public async Task ChangeFromGlobalNamespace_WithQualifiedReferencesInOtherDocument()
        //        {
        //            var defaultNamespace = "A";
        //            var (folder, filePath) = CreateDocumentFilePath(new[] { "B", "C" }, "File1.cs");
        //            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
        //            var code =
        //$@"
        //<Workspace>
        //    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        //        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
        //interface [||]Interface1 
        //{{
        //    void M1(Interface1 c1);   
        //}}</Document>
        //<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
        //namespace Foo
        //{{
        //    class RefClass : Interface1
        //    {{
        //        void Interface1.M1(Interface1 c1){{}}
        //    }}
        //}}</Document>
        //    </Project>
        //</Workspace>";

        //            var expectedSourceOriginal =
        //@"namespace A.B.C
        //{
        //    interface Interface1
        //    {
        //        void M1(Interface1 c1);
        //    }
        //}";
        //            var expectedSourceReference =
        //@"
        //using A.B.C;

        //namespace Foo
        //{
        //    class RefClass : Interface1
        //    {
        //        void Interface1.M1(Interface1 c1){}
        //    }
        //}";
        //            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        //        }

        //        [Fact]
        //        public async Task ChangeFromGlobalNamespace_ReferencingQualifiedTypesDeclaredInOtherDocument()
        //        {
        //            var defaultNamespace = "A";
        //            var (folder, filePath) = CreateDocumentFilePath(new[] { "B", "C" }, "File1.cs");
        //            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
        //            var code =
        //$@"
        //<Workspace>
        //    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        //        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
        //class [||]Class1 
        //{{ 
        //    private A.Class2 c2;
        //    private A.B.Class3 c3;
        //    private A.B.C.Class4 c4;
        //}}</Document>

        //<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
        //namespace A
        //{{
        //    class Class2 {{}}

        //    namespace B
        //    {{
        //        class Class3 {{}}

        //        namespace C
        //        {{
        //            class Class4 {{}}    
        //        }}
        //    }}
        //}}</Document>
        //    </Project>
        //</Workspace>";

        //            var expectedSourceOriginal =
        //@"namespace A.B.C
        //{
        //    class Class1
        //    {
        //        private Class2 c2;
        //        private Class3 c3;
        //        private Class4 c4;
        //    }
        //}";
        //            await TestChangeNamespaceAsync(code, expectedSourceOriginal);
        //        }

        //        [Fact]
        //        public async Task ChangeFromGlobalNamespace_ChangeUsingsInMultipleContainers()
        //        {
        //            var defaultNamespace = "A";

        //            var (folder, filePath) = CreateDocumentFilePath(new[] { "B", "C" }, "File1.cs");
        //            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
        //            var code =
        //$@"
        //<Workspace>
        //    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        //        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
        //class [||]Class1
        //{{
        //}}</Document>
        //<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
        //namespace NS1
        //{{
        //    using System;

        //    class Class2
        //    {{
        //        Class1 c2;
        //    }}

        //    namespace NS2
        //    {{
        //        using System;

        //        class Class2
        //        {{
        //            Class1 c1;
        //        }}
        //    }}
        //}}</Document>
        //    </Project>
        //</Workspace>";

        //            var expectedSourceOriginal =
        //@"namespace A.B.C
        //{
        //    class Class1
        //    {
        //    }
        //}";
        //            var expectedSourceReference =
        //@"
        //namespace NS1
        //{
        //    using System;
        //    using A.B.C;

        //    class Class2
        //    {
        //        Class1 c2;
        //    }

        //    namespace NS2
        //    {
        //        using System;

        //        class Class2
        //        {
        //            Class1 c1;
        //        }
        //    }
        //}";
        //            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        //        }

        //        [Fact]
        //        public async Task ChangeFromGlobalNamespace_WithAliasReferencesInOtherDocument()
        //        {
        //            var defaultNamespace = "A";

        //            var (folder, filePath) = CreateDocumentFilePath(new[] { "B", "C" }, "File1.cs");
        //            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
        //            var code =
        //$@"
        //<Workspace>
        //    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        //        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
        //class [||]Class1 
        //{{ 
        //}}

        //class Class2 
        //{{ 
        //}}</Document>
        //<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}"">
        //using Class1Alias = Class1;

        //namespace Foo
        //{{
        //    using System;

        //    class RefClass
        //    {{
        //        private Class1Alias c1;

        //        void M1()
        //        {{
        //            Class2 c2 = null;
        //        }}
        //    }}
        //}}</Document>
        //    </Project>
        //</Workspace>";

        //            var expectedSourceOriginal =
        //@"namespace A.B.C
        //{
        //    class Class1
        //    {
        //    }

        //    class Class2
        //    {
        //    }
        //}";
        //            var expectedSourceReference =
        //@"
        //using A.B.C;
        //using Class1Alias = Class1;

        //namespace Foo
        //{
        //    using System;

        //    class RefClass
        //    {
        //        private Class1Alias c1;

        //        void M1()
        //        {
        //            Class2 c2 = null;
        //        }
        //    }
        //}";
        //            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        //        }

        //        [Fact]
        //        public async Task ChangeNamespace_WithReferencesInVBDocument()
        //        {
        //            var defaultNamespace = "A.B.C";
        //            var declaredNamespace = "A.B.C.D";

        //            var (folder, filePath) = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
        //            var code =
        //$@"
        //<Workspace>
        //    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        //        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
        //namespace [||]{declaredNamespace}
        //{{
        //    public class Class1
        //    {{ 
        //    }}
        //}}</Document>
        //    </Project>    
        //<Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        //        <Document> 
        //Imports {declaredNamespace}

        //Public Class VBClass
        //    Public ReadOnly Property C1 As Class1
        //End Class</Document>
        //    </Project>
        //</Workspace>";

        //            var expectedSourceOriginal =
        //@"namespace A.B.C
        //{
        //    public class Class1
        //    {
        //    }
        //}";
        //            var expectedSourceReference =
        //@"
        //Imports A.B.C

        //Public Class VBClass
        //    Public ReadOnly Property C1 As Class1
        //End Class";
        //            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        //        }

        //        [Fact]
        //        public async Task ChangeNamespace_WithQualifiedReferencesInVBDocument()
        //        {
        //            var defaultNamespace = "A.B.C";
        //            var declaredNamespace = "A.B.C.D";

        //            var (folder, filePath) = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
        //            var code =
        //$@"
        //<Workspace>
        //    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        //        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
        //namespace [||]{declaredNamespace}
        //{{
        //    public class Class1
        //    {{ 
        //    }}
        //}}</Document>
        //    </Project>    
        //<Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        //        <Document>
        //Public Class VBClass
        //    Public ReadOnly Property C1 As A.B.C.D.Class1
        //End Class</Document>
        //    </Project>
        //</Workspace>";

        //            var expectedSourceOriginal =
        //@"namespace A.B.C
        //{
        //    public class Class1
        //    {
        //    }
        //}";
        //            var expectedSourceReference =
        //@"Public Class VBClass
        //    Public ReadOnly Property C1 As A.B.C.Class1
        //End Class";
        //            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        //        }

        //        [Fact]
        //        public async Task ChangeFromGlobalNamespace_WithReferencesInVBDocument()
        //        {
        //            var defaultNamespace = "A";

        //            var (folder, filePath) = CreateDocumentFilePath(new[] { "B", "C" }, "File1.cs");
        //            var code =
        //$@"
        //<Workspace>
        //    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        //        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
        //public class [||]Class1
        //{{ 
        //}}
        //</Document>
        //    </Project>    
        //<Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        //        <Document> 
        //Public Class VBClass
        //    Public ReadOnly Property C1 As Class1
        //End Class</Document>
        //    </Project>
        //</Workspace>";

        //            var expectedSourceOriginal =
        //@"namespace A.B.C
        //{
        //    public class Class1
        //    {
        //    }
        //}";
        //            var expectedSourceReference =
        //@"
        //Imports A.B.C

        //Public Class VBClass
        //    Public ReadOnly Property C1 As Class1
        //End Class";
        //            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        //        }

        //        [Fact]
        //        public async Task ChangeFromGlobalNamespace_WithCredReferences()
        //        {
        //            var defaultNamespace = "A";
        //            var (folder, filePath) = CreateDocumentFilePath(new[] { "B", "C" }, "File1.cs");
        //            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
        //            var code =
        //$@"
        //<Workspace>
        //    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        //        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
        ///// &lt;summary&gt;
        ///// See &lt;see cref=""Class1""/&gt;
        ///// &lt;/summary&gt;
        //class [||]Class1 
        //{{
        //}}</Document>
        //<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
        //namespace Foo
        //{{
        //    /// &lt;summary&gt;
        //    /// See &lt;see cref=""Class1""/&gt;
        //    /// &lt;/summary&gt;
        //    class Bar
        //    {{
        //    }}
        //}}</Document>
        //    </Project>
        //</Workspace>";

        //            var expectedSourceOriginal =
        //@"namespace A.B.C
        //{
        //    /// <summary>
        //    /// See <see cref=""Class1""/>
        //    /// </summary>
        //    class Class1
        //    {
        //    }
        //}";
        //            var expectedSourceReference =
        //@"
        //using A.B.C;

        //namespace Foo
        //{
        //    /// <summary>
        //    /// See <see cref=""Class1""/>
        //    /// </summary>
        //    class Bar
        //    {
        //    }
        //}";
        //            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        //        }

        //        [Fact]
        //        public async Task ChangeToGlobalNamespace_WithReferencesInVBDocument()
        //        {
        //            var defaultNamespace = "";
        //            var declaredNamespace = "Foo.Bar.Baz";

        //            var (folder, filePath) = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
        //            var code =
        //$@"
        //<Workspace>
        //    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        //        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
        //namespace [||]{declaredNamespace}
        //{{
        //    public class Class1
        //    {{ 
        //    }}
        //}}</Document>
        //    </Project>    
        //<Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        //        <Document> 
        //Imports {declaredNamespace}

        //Public Class VBClass
        //    Public ReadOnly Property C1 As Class1
        //End Class</Document>
        //    </Project>
        //</Workspace>";

        //            var expectedSourceOriginal =
        //@"public class Class1
        //{
        //}
        //";
        //            var expectedSourceReference =
        //@"Public Class VBClass
        //    Public ReadOnly Property C1 As Class1
        //End Class";
        //            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        //        }

        //        [Fact]
        //        public async Task ChangeToGlobalNamespace_WithReferenceAndConflictDeclarationInVBDocument()
        //        {
        //            var defaultNamespace = "";
        //            var declaredNamespace = "Foo.Bar.Baz";

        //            var (folder, filePath) = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
        //            var code =
        //$@"
        //<Workspace>
        //    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        //        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
        //namespace [||]{declaredNamespace}
        //{{
        //    public class MyClass
        //    {{ 
        //    }}
        //}}</Document>
        //    </Project>    
        //<Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        //        <Document>
        //Namespace Foo
        //    Public Class VBClass
        //        Public ReadOnly Property C1 As Foo.Bar.Baz.MyClass
        //    End Class

        //    Public Class MyClass
        //    End Class
        //End Namespace</Document>
        //    </Project>
        //</Workspace>";

        //            var expectedSourceOriginal =
        //@"public class MyClass
        //{
        //}
        //";
        //            var expectedSourceReference =
        //@"Namespace Foo
        //    Public Class VBClass
        //        Public ReadOnly Property C1 As Global.MyClass
        //    End Class

        //    Public Class MyClass
        //    End Class
        //End Namespace";
        //            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        //        }

        //        [Fact]
        //        public async Task ChangeToGlobalNamespace_WithCredReferences()
        //        {
        //            var defaultNamespace = "";
        //            var declaredNamespace = "Foo.Bar.Baz";

        //            var (folder, filePath) = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
        //            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
        //            var code =
        //$@"
        //<Workspace>
        //    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        //        <Document Folders=""{folder}"" FilePath=""{filePath}"">
        //namespace [||]{declaredNamespace}
        //{{
        //    /// &lt;summary&gt;
        //    /// See &lt;see cref=""Class1""/&gt;
        //    /// See &lt;see cref=""{declaredNamespace}.Class1""/&gt;
        //    /// &lt;/summary&gt;
        //    public class Class1
        //    {{
        //    }}
        //}}</Document>
        //<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
        //namespace Foo
        //{{
        //    using {declaredNamespace};

        //    /// &lt;summary&gt;
        //    /// See &lt;see cref=""Class1""/&gt;
        //    /// See &lt;see cref=""{declaredNamespace}.Class1""/&gt;
        //    /// &lt;/summary&gt;
        //    class RefClass
        //    {{
        //    }}
        //}}</Document>
        //    </Project>
        //</Workspace>";

        //            var expectedSourceOriginal =
        //@"/// <summary>
        ///// See <see cref=""Class1""/>
        ///// See <see cref=""Class1""/>
        ///// </summary>
        //public class Class1
        //{
        //}
        //";
        //            var expectedSourceReference =
        //@"
        //namespace Foo
        //{
        //    /// <summary>
        //    /// See <see cref=""Class1""/>
        //    /// See <see cref=""Class1""/>
        //    /// </summary>
        //    class RefClass
        //    {
        //    }
        //}";
        //            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        //        }

        //        [WorkItem(33890, "https://github.com/dotnet/roslyn/issues/33890")]
        //        [Fact]
        //        public async Task ChangeNamespace_ExtensionMethodInReducedForm()
        //        {
        //            var defaultNamespace = "A";

        //            var (folder, filePath) = CreateDocumentFilePath(new[] { "B", "C" }, "File1.cs");
        //            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
        //            var code =
        //$@"
        //<Workspace>
        //    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        //        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
        //namespace [||]{defaultNamespace}
        //{{
        //    public static class Extensions
        //    {{ 
        //        public static bool Foo(this Class1 c1) => true;
        //    }}
        //}}</Document>
        //<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}"">
        //namespace {defaultNamespace}
        //{{
        //    using System;

        //    public class Class1
        //    {{
        //        public bool Bar(Class1 c1) => c1.Foo();
        //    }}
        //}}</Document>
        //    </Project>
        //</Workspace>";

        //            var expectedSourceOriginal =
        //$@"namespace A.B.C
        //{{
        //    public static class Extensions
        //    {{
        //        public static bool Foo(this Class1 c1) => true;
        //    }}
        //}}";
        //            var expectedSourceReference =
        //$@"
        //namespace {defaultNamespace}
        //{{
        //    using System;
        //    using A.B.C;

        //    public class Class1
        //    {{
        //        public bool Bar(Class1 c1) => c1.Foo();
        //    }}
        //}}";
        //            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        //        }

        //        [WorkItem(33890, "https://github.com/dotnet/roslyn/issues/33890")]
        //        [Fact]
        //        public async Task ChangeNamespace_ExternsionMethodInRegularForm()
        //        {
        //            var defaultNamespace = "A";

        //            var (folder, filePath) = CreateDocumentFilePath(new[] { "B", "C" }, "File1.cs");
        //            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
        //            var code =
        //$@"
        //<Workspace>
        //    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        //        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
        //namespace [||]A
        //{{
        //    public static class Extensions
        //    {{ 
        //        public static bool Foo(this Class1 c1) => true;
        //    }}
        //}}</Document>
        //<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}"">
        //using System;

        //namespace A
        //{{
        //    public class Class1
        //    {{
        //        public bool Bar(Class1 c1) => Extensions.Foo(c1);
        //    }}
        //}}</Document>
        //    </Project>
        //</Workspace>";

        //            var expectedSourceOriginal =
        //$@"namespace A.B.C
        //{{
        //    public static class Extensions
        //    {{
        //        public static bool Foo(this Class1 c1) => true;
        //    }}
        //}}";
        //            var expectedSourceReference =
        //$@"
        //using System;
        //using A.B.C;

        //namespace A
        //{{
        //    public class Class1
        //    {{
        //        public bool Bar(Class1 c1) => Extensions.Foo(c1);
        //    }}
        //}}";
        //            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        //        }

        //        [WorkItem(33890, "https://github.com/dotnet/roslyn/issues/33890")]
        //        [Fact]
        //        public async Task ChangeNamespace_ContainsBothTypeAndExternsionMethod()
        //        {
        //            var defaultNamespace = "A";

        //            var (folder, filePath) = CreateDocumentFilePath(new[] { "B", "C" }, "File1.cs");
        //            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
        //            var code =
        //$@"
        //<Workspace>
        //    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        //        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
        //namespace [||]A
        //{{
        //    public static class Extensions
        //    {{ 
        //        public static bool Foo(this Class1 c1) => true;
        //    }}

        //    public class Class2
        //    {{ }}
        //}}</Document>
        //<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}"">
        //using System;

        //namespace A
        //{{
        //    public class Class1
        //    {{
        //        public bool Bar(Class1 c1, Class2 c2) => c2 == null ? c1.Foo() : true;
        //    }}
        //}}</Document>
        //    </Project>
        //</Workspace>";

        //            var expectedSourceOriginal =
        //@"namespace A.B.C
        //{
        //    public static class Extensions
        //    {
        //        public static bool Foo(this Class1 c1) => true;
        //    }

        //    public class Class2
        //    { }
        //}";
        //            var expectedSourceReference =
        //@"
        //using System;
        //using A.B.C;

        //namespace A
        //{
        //    public class Class1
        //    {
        //        public bool Bar(Class1 c1, Class2 c2) => c2 == null ? c1.Foo() : true;
        //    }
        //}";
        //            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        //        }

        //        [WorkItem(33890, "https://github.com/dotnet/roslyn/issues/33890")]
        //        [Fact]
        //        public async Task ChangeNamespace_WithExtensionMethodReferencesInVBDocument()
        //        {
        //            var defaultNamespace = "A.B.C";
        //            var declaredNamespace = "A.B.C.D";

        //            var (folder, filePath) = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
        //            var code =
        //$@"
        //<Workspace>
        //    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        //        <Document Folders=""{folder}"" FilePath=""{filePath}"">
        //using System;

        //namespace [||]{declaredNamespace}
        //{{
        //    public static class Extensions
        //    {{
        //        public static bool Foo(this String s) => true;
        //    }}
        //}}</Document>
        //    </Project>    
        //<Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        //        <Document>
        //Imports {declaredNamespace}

        //Public Class VBClass
        //    Public Function Foo(s As string) As Boolean
        //        Return s.Foo()
        //    End Function
        //End Class</Document>
        //    </Project>
        //</Workspace>";

        //            var expectedSourceOriginal =
        //$@"
        //using System;

        //namespace {defaultNamespace}
        //{{
        //    public static class Extensions
        //    {{
        //        public static bool Foo(this string s) => true;
        //    }}
        //}}";
        //            var expectedSourceReference =
        //$@"
        //Imports {defaultNamespace}

        //Public Class VBClass
        //    Public Function Foo(s As string) As Boolean
        //        Return s.Foo()
        //    End Function
        //End Class";
        //            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        //        }

        //        [WorkItem(37891, "https://github.com/dotnet/roslyn/issues/37891")]
        //        [Fact]
        //        public async Task ChangeNamespace_WithMemberAccessReferencesInOtherDocument()
        //        {
        //            var defaultNamespace = "A";
        //            var declaredNamespace = "Foo.Bar.Baz";

        //            var documentPath1 = CreateDocumentFilePath(new[] { "B", "C" }, "File1.cs");
        //            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
        //            var code =
        //$@"
        //<Workspace>
        //    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        //        <Document Folders=""{documentPath1.folder}"" FilePath=""{documentPath1.filePath}""> 
        //namespace [||]{declaredNamespace}
        //{{
        //    enum Enum1 
        //    {{
        //        A,
        //        B,
        //        C
        //    }}
        //}}</Document>
        //<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
        //namespace Foo
        //{{
        //    class RefClass
        //    {{
        //        Enum1 M1()
        //        {{
        //            return {declaredNamespace}.Enum1.A;
        //        }}
        //    }}
        //}}</Document>
        //    </Project>
        //</Workspace>";

        //            var expectedSourceOriginal =
        //@"namespace A.B.C
        //{
        //    enum Enum1
        //    {
        //        A,
        //        B,
        //        C
        //    }
        //}";
        //            var expectedSourceReference =
        //@"
        //using A.B.C;

        //namespace Foo
        //{
        //    class RefClass
        //    {
        //        Enum1 M1()
        //        {
        //            return Enum1.A;
        //        }
        //    }
        //}";
        //            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        //        }

        //        [WorkItem(37891, "https://github.com/dotnet/roslyn/issues/37891")]
        //        [Fact]
        //        public async Task ChangeToGlobalNamespace_WithMemberAccessReferencesInOtherDocument()
        //        {
        //            var defaultNamespace = "";
        //            var declaredNamespace = "Foo.Bar.Baz";

        //            var documentPath1 = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
        //            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
        //            var code =
        //$@"
        //<Workspace>
        //    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        //        <Document Folders=""{documentPath1.folder}"" FilePath=""{documentPath1.filePath}""> 
        //namespace [||]{declaredNamespace}
        //{{
        //    enum Enum1 
        //    {{
        //        A,
        //        B,
        //        C
        //    }}
        //}}</Document>
        //<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
        //namespace Foo
        //{{
        //    class RefClass
        //    {{
        //        Enum1 M1()
        //        {{
        //            return {declaredNamespace}.Enum1.A;
        //        }}
        //    }}
        //}}</Document>
        //    </Project>
        //</Workspace>";

        //            var expectedSourceOriginal =
        //@"enum Enum1
        //{
        //    A,
        //    B,
        //    C
        //}
        //";
        //            var expectedSourceReference =
        //@"namespace Foo
        //{
        //    class RefClass
        //    {
        //        Enum1 M1()
        //        {
        //            return Enum1.A;
        //        }
        //    }
        //}";
        //            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        //        }

        //        [WorkItem(37891, "https://github.com/dotnet/roslyn/issues/37891")]
        //        [Fact]
        //        public async Task ChangeNamespace_WithMemberAccessReferencesInVBDocument()
        //        {
        //            var defaultNamespace = "A.B.C";
        //            var declaredNamespace = "A.B.C.D";

        //            var documentPath1 = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
        //            var code =
        //$@"
        //<Workspace>
        //    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        //        <Document Folders=""{documentPath1.folder}"" FilePath=""{documentPath1.filePath}""> 
        //namespace [||]{declaredNamespace}
        //{{
        //    public enum Enum1
        //    {{
        //        A,
        //        B,
        //        C
        //    }}
        //}}</Document>
        //    </Project>    
        //<Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        //        <Document>
        //Public Class VBClass
        //    Sub M()
        //        Dim x = A.B.C.D.Enum1.A
        //    End Sub
        //End Class</Document>
        //    </Project>
        //</Workspace>";

        //            var expectedSourceOriginal =
        //@"namespace A.B.C
        //{
        //    public enum Enum1
        //    {
        //        A,
        //        B,
        //        C
        //    }
        //}";
        //            var expectedSourceReference =
        //@"Public Class VBClass
        //    Sub M()
        //        Dim x = A.B.C.Enum1.A
        //    End Sub
        //End Class";
        //            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        //        }

        //        [WorkItem(37891, "https://github.com/dotnet/roslyn/issues/37891")]
        //        [Fact]
        //        public async Task ChangeToGlobalNamespace_WithMemberAccessReferencesInVBDocument()
        //        {
        //            var defaultNamespace = "";
        //            var declaredNamespace = "A.B.C.D";

        //            var documentPath1 = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
        //            var code =
        //$@"
        //<Workspace>
        //    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        //        <Document Folders=""{documentPath1.folder}"" FilePath=""{documentPath1.filePath}""> 
        //namespace [||]{declaredNamespace}
        //{{
        //    public enum Enum1
        //    {{
        //        A,
        //        B,
        //        C
        //    }}
        //}}</Document>
        //    </Project>    
        //<Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        //        <Document>
        //Public Class VBClass
        //    Sub M()
        //        Dim x = A.B.C.D.Enum1.A
        //    End Sub
        //End Class</Document>
        //    </Project>
        //</Workspace>";

        //            var expectedSourceOriginal =
        //@"public enum Enum1
        //{
        //    A,
        //    B,
        //    C
        //}
        //";
        //            var expectedSourceReference =
        //@"Public Class VBClass
        //    Sub M()
        //        Dim x = Enum1.A
        //    End Sub
        //End Class";
        //            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        //        }
    }
}
