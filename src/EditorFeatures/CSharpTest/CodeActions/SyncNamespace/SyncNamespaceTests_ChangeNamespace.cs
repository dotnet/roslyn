// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.SyncNamespace
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
    public partial class SyncNamespaceTests : CSharpSyncNamespaceTestsBase
    {
        [Fact]
        public async Task ChangeNamespace_InvalidFolderName1()
        {
            var defaultNamespace = "A";
            var declaredNamespace = "Foo.Bar";

            // No change namespace action because the folder name is not valid identifier
            var (folder, filePath) = CreateDocumentFilePath(["3B", "C"], "File1.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]{declaredNamespace}
{{    
    class Class1
    {{
    }}
}}  
        </Document>
    </Project>
</Workspace>";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal: null);
        }

        [Fact]
        public async Task ChangeNamespace_InvalidFolderName2()
        {
            var defaultNamespace = "A";
            var declaredNamespace = "Foo.Bar";

            // No change namespace action because the folder name is not valid identifier
            var (folder, filePath) = CreateDocumentFilePath(["B.3C", "D"], "File1.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]{declaredNamespace}
{{    
    class Class1
    {{
    }}
}}  
        </Document>
    </Project>
</Workspace>";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal: null);
        }

        [Fact]
        public async Task ChangeNamespace_SingleDocumentNoReference()
        {
            var defaultNamespace = "A";
            var declaredNamespace = "Foo.Bar";

            var (folder, filePath) = CreateDocumentFilePath(["B", "C"], "File1.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]{declaredNamespace}
{{
    class Class1
    {{
    }}
}}</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"namespace A.B.C
{
    class Class1
    {
    }
}";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal);
        }

        [Fact]
        public async Task ChangeNamespace_SingleDocumentNoReference_FileScopedNamespace()
        {
            var defaultNamespace = "A";
            var declaredNamespace = "Foo.Bar";

            var (folder, filePath) = CreateDocumentFilePath(["B", "C"], "File1.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]{declaredNamespace};

class Class1
{{
}}
</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"namespace A.B.C;

class Class1
{
}
";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal);
        }

        [Fact]
        public async Task ChangeNamespace_SingleDocumentLocalReference()
        {
            var defaultNamespace = "A";
            var declaredNamespace = "Foo.Bar";

            var (folder, filePath) = CreateDocumentFilePath(["B", "C"], "File1.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]{declaredNamespace}
{{
    delegate void D1;

    interface Class1
    {{
        void M1();
    }}

    class Class2 : {declaredNamespace}.Class1
    {{
        {declaredNamespace}.D1 d;  

        void {declaredNamespace}.Class1.M1(){{}}
    }}
}}</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"namespace A.B.C
{
    delegate void D1;

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
            await TestChangeNamespaceAsync(code, expectedSourceOriginal);
        }

        [Fact]
        public async Task ChangeNamespace_WithCrefReference()
        {
            var defaultNamespace = "A";
            var declaredNamespace = "Foo.Bar.Baz";

            var (folder, filePath) = CreateDocumentFilePath(["B", "C"], "File1.cs");
            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]{declaredNamespace}
{{
    /// &lt;summary&gt;
    /// See &lt;see cref=""Class1""/&gt;
    /// See &lt;see cref=""{declaredNamespace}.Class1""/&gt;
    /// See &lt;see cref=""global::{declaredNamespace}.Class1""/&gt;
    /// See &lt;see cref=""global::{declaredNamespace}.Class1.M1""/&gt;
    /// &lt;/summary&gt;
    public class Class1
    {{
        public void M1() {{ }}
    }}
}}</Document>
<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
namespace Foo
{{
    using {declaredNamespace};

    /// &lt;summary&gt;
    /// See &lt;see cref=""Class1""/&gt;
    /// See &lt;see cref=""{declaredNamespace}.Class1""/&gt;
    /// See &lt;see cref=""global::{declaredNamespace}.Class1""/&gt;
    /// See &lt;see cref=""global::{declaredNamespace}.Class1.M1""/&gt;
    /// &lt;/summary&gt;
    class RefClass
    {{
    }}
}}</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"namespace A.B.C
{
    /// <summary>
    /// See <see cref=""Class1""/>
    /// See <see cref=""Class1""/>
    /// See <see cref=""global::A.B.C.Class1""/>
    /// See <see cref=""global::A.B.C.Class1.M1""/>
    /// </summary>
    public class Class1
    {
        public void M1() { }
    }
}";
            var expectedSourceReference =
@"
namespace Foo
{
    using A.B.C;

    /// <summary>
    /// See <see cref=""Class1""/>
    /// See <see cref=""A.B.C.Class1""/>
    /// See <see cref=""global::A.B.C.Class1""/>
    /// See <see cref=""global::A.B.C.Class1.M1""/>
    /// </summary>
    class RefClass
    {
    }
}";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        }

        [Fact]
        public async Task ChangeNamespace_WithCrefReferencesInVB()
        {
            var defaultNamespace = "A.B.C";
            var declaredNamespace = "A.B.C.D";

            var (folder, filePath) = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]{declaredNamespace}
{{
    /// &lt;summary&gt;
    /// See &lt;see cref=""Class1""/&gt;
    /// See &lt;see cref=""{declaredNamespace}.Class1""/&gt;
    /// &lt;/summary&gt;
    public class Class1
    {{
    }}
}}</Document>
    </Project>    
<Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document> 
Imports {declaredNamespace}

''' &lt;summary&gt;
''' See &lt;see cref=""Class1""/&gt;
''' See &lt;see cref=""{declaredNamespace}.Class1""/&gt;
''' &lt;/summary&gt;
Public Class VBClass
    Public ReadOnly Property C1 As Class1
End Class</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"namespace A.B.C
{
    /// <summary>
    /// See <see cref=""Class1""/>
    /// See <see cref=""Class1""/>
    /// </summary>
    public class Class1
    {
    }
}";
            var expectedSourceReference =
@"
Imports A.B.C

''' <summary>
''' See <see cref=""Class1""/>
''' See <see cref=""Class1""/>
''' </summary>
Public Class VBClass
    Public ReadOnly Property C1 As Class1
End Class";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        }

        [Fact]
        public async Task ChangeNamespace_ReferencingTypesDeclaredInOtherDocument()
        {
            var defaultNamespace = "A";
            var declaredNamespace = "Foo.Bar.Baz";

            var (folder, filePath) = CreateDocumentFilePath(["B", "C"], "File1.cs");
            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]{declaredNamespace}
{{
    class Class1 
    {{ 
        private Class2 c2;
        private Class3 c3;
        private Class4 c4;
    }}
}}</Document>
<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
namespace Foo
{{
    class Class2 {{}}

    namespace Bar
    {{
        class Class3 {{}}
        
        namespace Baz
        {{
            class Class4 {{}}    
        }}
    }}
}}</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"using Foo;
using Foo.Bar;
using Foo.Bar.Baz;

namespace A.B.C
{
    class Class1
    {
        private Class2 c2;
        private Class3 c3;
        private Class4 c4;
    }
}";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal);
        }

        [Fact]
        public async Task ChangeNamespace_ReferencingQualifiedTypesDeclaredInOtherDocument()
        {
            var defaultNamespace = "A";
            var declaredNamespace = "Foo.Bar.Baz";

            var (folder, filePath) = CreateDocumentFilePath(["B", "C"], "File1.cs");
            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]{declaredNamespace}
{{
    class Class1 
    {{ 
        private Foo.Class2 c2;
        private Foo.Bar.Class3 c3;
        private Foo.Bar.Baz.Class4 c4;
    }}
}}</Document>
<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
namespace Foo
{{
    class Class2 {{}}

    namespace Bar
    {{
        class Class3 {{}}
        
        namespace Baz
        {{
            class Class4 {{}}    
        }}
    }}
}}</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"using Foo;
using Foo.Bar;
using Foo.Bar.Baz;

namespace A.B.C
{
    class Class1
    {
        private Class2 c2;
        private Class3 c3;
        private Class4 c4;
    }
}";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal);
        }

        [Fact]
        public async Task ChangeNamespace_WithReferencesInOtherDocument()
        {
            var defaultNamespace = "A";
            var declaredNamespace = "Foo.Bar.Baz";

            var (folder, filePath) = CreateDocumentFilePath(["B", "C"], "File1.cs");
            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]{declaredNamespace}
{{
    class Class1 
    {{ 
    }}
    
    class Class2 
    {{ 
    }}
}}</Document>
<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
using Foo.Bar.Baz;

namespace Foo
{{
    class RefClass
    {{
        private Class1 c1;

        void M1()
        {{
            Bar.Baz.Class2 c2 = null;
        }}
    }}
}}</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"namespace A.B.C
{
    class Class1
    {
    }

    class Class2
    {
    }
}";
            var expectedSourceReference =
@"
using A.B.C;

namespace Foo
{
    class RefClass
    {
        private Class1 c1;

        void M1()
        {
            Class2 c2 = null;
        }
    }
}";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        }

        [Fact]
        public async Task ChangeNamespace_WithQualifiedReferencesInOtherDocument()
        {
            var defaultNamespace = "A";
            var declaredNamespace = "Foo.Bar.Baz";

            var (folder, filePath) = CreateDocumentFilePath(["B", "C"], "File1.cs");
            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]{declaredNamespace}
{{
    interface Interface1 
    {{
        void M1(Interface1 c1);   
    }}
}}</Document>
<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
namespace Foo
{{
    using {declaredNamespace};

    class RefClass : Interface1
    {{
        void {declaredNamespace}.Interface1.M1(Interface1 c1){{}}
    }}
}}</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"namespace A.B.C
{
    interface Interface1
    {
        void M1(Interface1 c1);
    }
}";
            var expectedSourceReference =
@"
namespace Foo
{
    using A.B.C;

    class RefClass : Interface1
    {
        void Interface1.M1(Interface1 c1){}
    }
}";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        }

        [Fact]
        public async Task ChangeNamespace_ChangeUsingsInMultipleContainers()
        {
            var defaultNamespace = "A";
            var declaredNamespace = "Foo.Bar.Baz";

            var (folder, filePath) = CreateDocumentFilePath(["B", "C"], "File1.cs");
            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]{declaredNamespace}
{{
    class Class1
    {{
    }}
}}</Document>
<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
namespace NS1
{{
    using Foo.Bar.Baz;

    class Class2
    {{
        Class1 c2;
    }}

    namespace NS2
    {{
        using Foo.Bar.Baz;

        class Class2
        {{
            Class1 c1;
        }}
    }}
}}</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"namespace A.B.C
{
    class Class1
    {
    }
}";
            var expectedSourceReference =
@"
namespace NS1
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
            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        }

        [Fact]
        public async Task ChangeNamespace_WithAliasReferencesInOtherDocument()
        {
            var defaultNamespace = "A";
            var declaredNamespace = "Foo.Bar.Baz";

            var (folder, filePath) = CreateDocumentFilePath(["B", "C"], "File1.cs");
            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]{declaredNamespace}
{{
    class Class1 
    {{ 
    }}
    
    class Class2 
    {{ 
    }}
}}</Document>
<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}"">
using System;
using Class1Alias = Foo.Bar.Baz.Class1;

namespace Foo
{{
    class RefClass
    {{
        private Class1Alias c1;

        void M1()
        {{
            Bar.Baz.Class2 c2 = null;
        }}
    }}
}}</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"namespace A.B.C
{
    class Class1
    {
    }

    class Class2
    {
    }
}";
            var expectedSourceReference =
@"
using System;
using A.B.C;
using Class1Alias = A.B.C.Class1;

namespace Foo
{
    class RefClass
    {
        private Class1Alias c1;

        void M1()
        {
            Class2 c2 = null;
        }
    }
}";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        }

        [Fact]
        public async Task ChangeToGlobalNamespace_SingleDocumentNoRef()
        {
            var defaultNamespace = "";
            var declaredNamespace = "Foo.Bar";

            var (folder, filePath) = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}"">
using System;

// Comments before declaration.
namespace [||]{declaredNamespace}
{{  // Comments after opening brace
    class Class1
    {{
    }}
    // Comments before closing brace
}} // Comments after declaration.
</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"
using System;

// Comments before declaration.
// Comments after opening brace
class Class1
{
}
// Comments before closing brace
// Comments after declaration.
";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal);
        }

        [Fact]
        public async Task ChangeToGlobalNamespace_SingleDocumentLocalRef()
        {
            var defaultNamespace = "";
            var declaredNamespace = "Foo.Bar";

            var (folder, filePath) = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]{declaredNamespace}
{{
    delegate void D1;

    interface Class1
    {{
        void M1();
    }}

    class Class2 : {declaredNamespace}.Class1
    {{
        global::{declaredNamespace}.D1 d;  

        void {declaredNamespace}.Class1.M1() {{ }}
    }}
}}</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"delegate void D1;

interface Class1
{
    void M1();
}

class Class2 : Class1
{
    global::D1 d;

    void Class1.M1() { }
}
";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal);
        }

        [Fact]
        public async Task ChangeToGlobalNamespace_WithReferencesInOtherDocument()
        {
            var defaultNamespace = "";
            var declaredNamespace = "Foo.Bar.Baz";

            var (folder, filePath) = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]{declaredNamespace}
{{
    class Class1 
    {{ 
    }}
    
    class Class2 
    {{ 
    }}
}}</Document>
<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
using Foo.Bar.Baz;

namespace Foo
{{
    class RefClass
    {{
        private Class1 c1;

        void M1()
        {{
            Bar.Baz.Class2 c2 = null;
        }}
    }}
}}</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"class Class1
{
}

class Class2
{
}
";
            var expectedSourceReference =
@"namespace Foo
{
    class RefClass
    {
        private Class1 c1;

        void M1()
        {
            Class2 c2 = null;
        }
    }
}";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        }

        [Fact]
        public async Task ChangeToGlobalNamespace_WithQualifiedReferencesInOtherDocument()
        {
            var defaultNamespace = "";
            var declaredNamespace = "Foo.Bar.Baz";

            var (folder, filePath) = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]{declaredNamespace}
{{
    interface Interface1 
    {{
        void M1(Interface1 c1);   
    }}
}}</Document>
<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
namespace Foo
{{
    using {declaredNamespace};

    class RefClass : Interface1
    {{
        void {declaredNamespace}.Interface1.M1(Interface1 c1){{}}
    }}
}}</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"interface Interface1
{
    void M1(Interface1 c1);
}
";
            var expectedSourceReference =
@"
namespace Foo
{
    class RefClass : Interface1
    {
        void Interface1.M1(Interface1 c1){}
    }
}";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        }

        [Fact]
        public async Task ChangeToGlobalNamespace_WithReferenceAndConflictDeclarationInOtherDocument()
        {
            var defaultNamespace = "";
            var declaredNamespace = "Foo.Bar.Baz";

            var (folder, filePath) = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]{declaredNamespace}
{{
    class MyClass 
    {{
    }}
}}</Document>
<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
namespace Foo
{{
    using {declaredNamespace};

    class RefClass
    {{
        Foo.Bar.Baz.MyClass c;
    }}

    class MyClass
    {{
    }}
}}</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"class MyClass
{
}
";
            var expectedSourceReference =
@"
namespace Foo
{
    class RefClass
    {
        global::MyClass c;
    }

    class MyClass
    {
    }
}";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        }

        [Fact]
        public async Task ChangeToGlobalNamespace_ReferencingTypesDeclaredInOtherDocument()
        {
            var defaultNamespace = "";
            var declaredNamespace = "Foo.Bar.Baz";

            var (folder, filePath) = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]{declaredNamespace}
{{
    class Class1 
    {{ 
        private Class2 c2;
        private Class3 c3;
        private Class4 c4;
    }}
}}</Document>
<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
namespace Foo
{{
    class Class2 {{}}

    namespace Bar
    {{
        class Class3 {{}}
        
        namespace Baz
        {{
            class Class4 {{}}    
        }}
    }}
}}</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"using Foo;
using Foo.Bar;
using Foo.Bar.Baz;

class Class1
{
    private Class2 c2;
    private Class3 c3;
    private Class4 c4;
}
";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal);
        }

        [Fact]
        public async Task ChangeToGlobalNamespace_ChangeUsingsInMultipleContainers()
        {
            var defaultNamespace = "";
            var declaredNamespace = "Foo.Bar.Baz";

            var (folder, filePath) = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]{declaredNamespace}
{{
    class Class1
    {{
    }}
}}</Document>
<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
namespace NS1
{{
    using Foo.Bar.Baz;

    class Class2
    {{
        Class1 c2;
    }}

    namespace NS2
    {{
        using Foo.Bar.Baz;

        class Class2
        {{
            Class1 c1;
        }}
    }}
}}</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"class Class1
{
}
";
            var expectedSourceReference =
@"
namespace NS1
{
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
            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        }

        [Fact]
        public async Task ChangeToGlobalNamespace_WithAliasReferencesInOtherDocument()
        {
            var defaultNamespace = "";
            var declaredNamespace = "Foo.Bar.Baz";

            var (folder, filePath) = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]{declaredNamespace}
{{
    class Class1 
    {{ 
    }}
    
    class Class2 
    {{ 
    }}
}}</Document>
<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}"">
using System;
using Class1Alias = Foo.Bar.Baz.Class1;

namespace Foo
{{
    class RefClass
    {{
        private Class1Alias c1;

        void M1()
        {{
            Bar.Baz.Class2 c2 = null;
        }}
    }}
}}</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"class Class1
{
}

class Class2
{
}
";
            var expectedSourceReference =
@"
using System;
using Class1Alias = Class1;

namespace Foo
{
    class RefClass
    {
        private Class1Alias c1;

        void M1()
        {
            Class2 c2 = null;
        }
    }
}";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        }

        [Fact]
        public async Task ChangeFromGlobalNamespace_SingleDocumentNoRef()
        {
            var defaultNamespace = "A";
            var (folder, filePath) = CreateDocumentFilePath(["B", "C"], "File1.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}"">
using System;

class [||]Class1
{{
}}
</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"
using System;

namespace A.B.C
{
    class Class1
    {
    }
}";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal);
        }

        [Fact]
        public async Task ChangeFromGlobalNamespace_SingleDocumentLocalRef()
        {
            var defaultNamespace = "A";
            var (folder, filePath) = CreateDocumentFilePath(["B", "C"], "File1.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
delegate void [||]D1;

interface Class1
{{
    void M1();
}}

class Class2 : Class1
{{
    D1 d;  

    void Class1.M1() {{ }}
}}</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"namespace A.B.C
{
    delegate void D1;

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
            await TestChangeNamespaceAsync(code, expectedSourceOriginal);
        }

        [Fact]
        public async Task ChangeFromGlobalNamespace_WithReferencesInOtherDocument()
        {
            var defaultNamespace = "A";

            var (folder, filePath) = CreateDocumentFilePath(["B", "C"], "File1.cs");
            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
class [||]Class1 
{{ 
}}
    
class Class2 
{{ 
}}</Document>
<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
namespace Foo
{{
    class RefClass
    {{
        private Class1 c1;

        void M1()
        {{
            Class2 c2 = null;
        }}
    }}
}}</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"namespace A.B.C
{
    class Class1
    {
    }

    class Class2
    {
    }
}";
            var expectedSourceReference =
@"
using A.B.C;

namespace Foo
{
    class RefClass
    {
        private Class1 c1;

        void M1()
        {
            Class2 c2 = null;
        }
    }
}";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        }

        [Fact]
        public async Task ChangeFromGlobalNamespace_WithQualifiedReferencesInOtherDocument()
        {
            var defaultNamespace = "A";
            var (folder, filePath) = CreateDocumentFilePath(["B", "C"], "File1.cs");
            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
interface [||]Interface1 
{{
    void M1(Interface1 c1);   
}}</Document>
<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
namespace Foo
{{
    class RefClass : Interface1
    {{
        void Interface1.M1(Interface1 c1){{}}
    }}
}}</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"namespace A.B.C
{
    interface Interface1
    {
        void M1(Interface1 c1);
    }
}";
            var expectedSourceReference =
@"
using A.B.C;

namespace Foo
{
    class RefClass : Interface1
    {
        void Interface1.M1(Interface1 c1){}
    }
}";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        }

        [Fact]
        public async Task ChangeFromGlobalNamespace_ReferencingQualifiedTypesDeclaredInOtherDocument()
        {
            var defaultNamespace = "A";
            var (folder, filePath) = CreateDocumentFilePath(["B", "C"], "File1.cs");
            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
class [||]Class1 
{{ 
    private A.Class2 c2;
    private A.B.Class3 c3;
    private A.B.C.Class4 c4;
}}</Document>

<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
namespace A
{{
    class Class2 {{}}

    namespace B
    {{
        class Class3 {{}}
        
        namespace C
        {{
            class Class4 {{}}    
        }}
    }}
}}</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"namespace A.B.C
{
    class Class1
    {
        private Class2 c2;
        private Class3 c3;
        private Class4 c4;
    }
}";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal);
        }

        [Fact]
        public async Task ChangeFromGlobalNamespace_ChangeUsingsInMultipleContainers()
        {
            var defaultNamespace = "A";

            var (folder, filePath) = CreateDocumentFilePath(["B", "C"], "File1.cs");
            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
class [||]Class1
{{
}}</Document>
<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
namespace NS1
{{
    using System;

    class Class2
    {{
        Class1 c2;
    }}

    namespace NS2
    {{
        using System;

        class Class2
        {{
            Class1 c1;
        }}
    }}
}}</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"namespace A.B.C
{
    class Class1
    {
    }
}";
            var expectedSourceReference =
@"
namespace NS1
{
    using System;
    using A.B.C;

    class Class2
    {
        Class1 c2;
    }

    namespace NS2
    {
        using System;

        class Class2
        {
            Class1 c1;
        }
    }
}";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        }

        [Fact]
        public async Task ChangeFromGlobalNamespace_WithAliasReferencesInOtherDocument()
        {
            var defaultNamespace = "A";

            var (folder, filePath) = CreateDocumentFilePath(["B", "C"], "File1.cs");
            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
class [||]Class1 
{{ 
}}
    
class Class2 
{{ 
}}</Document>
<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}"">
using Class1Alias = Class1;

namespace Foo
{{
    using System;

    class RefClass
    {{
        private Class1Alias c1;

        void M1()
        {{
            Class2 c2 = null;
        }}
    }}
}}</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"namespace A.B.C
{
    class Class1
    {
    }

    class Class2
    {
    }
}";
            var expectedSourceReference =
@"
using A.B.C;
using Class1Alias = Class1;

namespace Foo
{
    using System;

    class RefClass
    {
        private Class1Alias c1;

        void M1()
        {
            Class2 c2 = null;
        }
    }
}";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        }

        [Fact]
        public async Task ChangeNamespace_WithReferencesInVBDocument()
        {
            var defaultNamespace = "A.B.C";
            var declaredNamespace = "A.B.C.D";

            var (folder, filePath) = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]{declaredNamespace}
{{
    public class Class1
    {{ 
    }}
}}</Document>
    </Project>    
<Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document> 
Imports {declaredNamespace}

Public Class VBClass
    Public ReadOnly Property C1 As Class1
End Class</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"namespace A.B.C
{
    public class Class1
    {
    }
}";
            var expectedSourceReference =
@"
Imports A.B.C

Public Class VBClass
    Public ReadOnly Property C1 As Class1
End Class";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        }

        [Fact]
        public async Task ChangeNamespace_WithQualifiedReferencesInVBDocument()
        {
            var defaultNamespace = "A.B.C";
            var declaredNamespace = "A.B.C.D";

            var (folder, filePath) = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]{declaredNamespace}
{{
    public class Class1
    {{ 
    }}
}}</Document>
    </Project>    
<Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
Public Class VBClass
    Public ReadOnly Property C1 As A.B.C.D.Class1
End Class</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"namespace A.B.C
{
    public class Class1
    {
    }
}";
            var expectedSourceReference =
@"Public Class VBClass
    Public ReadOnly Property C1 As A.B.C.Class1
End Class";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        }

        [Fact]
        public async Task ChangeFromGlobalNamespace_WithReferencesInVBDocument()
        {
            var defaultNamespace = "A";

            var (folder, filePath) = CreateDocumentFilePath(["B", "C"], "File1.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
public class [||]Class1
{{ 
}}
</Document>
    </Project>    
<Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document> 
Public Class VBClass
    Public ReadOnly Property C1 As Class1
End Class</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"namespace A.B.C
{
    public class Class1
    {
    }
}";
            var expectedSourceReference =
@"
Imports A.B.C

Public Class VBClass
    Public ReadOnly Property C1 As Class1
End Class";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        }

        [Fact]
        public async Task ChangeFromGlobalNamespace_WithCredReferences()
        {
            var defaultNamespace = "A";
            var (folder, filePath) = CreateDocumentFilePath(["B", "C"], "File1.cs");
            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
/// &lt;summary&gt;
/// See &lt;see cref=""Class1""/&gt;
/// &lt;/summary&gt;
class [||]Class1 
{{
}}</Document>
<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
namespace Foo
{{
    /// &lt;summary&gt;
    /// See &lt;see cref=""Class1""/&gt;
    /// &lt;/summary&gt;
    class Bar
    {{
    }}
}}</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"namespace A.B.C
{

    /// <summary>
    /// See <see cref=""Class1""/>
    /// </summary>
    class Class1
    {
    }
}";
            var expectedSourceReference =
@"
using A.B.C;

namespace Foo
{
    /// <summary>
    /// See <see cref=""Class1""/>
    /// </summary>
    class Bar
    {
    }
}";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        }

        [Fact]
        public async Task ChangeToGlobalNamespace_WithReferencesInVBDocument()
        {
            var defaultNamespace = "";
            var declaredNamespace = "Foo.Bar.Baz";

            var (folder, filePath) = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]{declaredNamespace}
{{
    public class Class1
    {{ 
    }}
}}</Document>
    </Project>    
<Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document> 
Imports {declaredNamespace}

Public Class VBClass
    Public ReadOnly Property C1 As Class1
End Class</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"public class Class1
{
}
";
            var expectedSourceReference =
@"Public Class VBClass
    Public ReadOnly Property C1 As Class1
End Class";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        }

        [Fact]
        public async Task ChangeToGlobalNamespace_WithReferenceAndConflictDeclarationInVBDocument()
        {
            var defaultNamespace = "";
            var declaredNamespace = "Foo.Bar.Baz";

            var (folder, filePath) = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]{declaredNamespace}
{{
    public class MyClass
    {{ 
    }}
}}</Document>
    </Project>    
<Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
Namespace Foo
    Public Class VBClass
        Public ReadOnly Property C1 As Foo.Bar.Baz.MyClass
    End Class

    Public Class MyClass
    End Class
End Namespace</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"public class MyClass
{
}
";
            var expectedSourceReference =
@"Namespace Foo
    Public Class VBClass
        Public ReadOnly Property C1 As Global.MyClass
    End Class

    Public Class MyClass
    End Class
End Namespace";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        }

        [Fact]
        public async Task ChangeToGlobalNamespace_WithCredReferences()
        {
            var defaultNamespace = "";
            var declaredNamespace = "Foo.Bar.Baz";

            var (folder, filePath) = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}"">
namespace [||]{declaredNamespace}
{{
    /// &lt;summary&gt;
    /// See &lt;see cref=""Class1""/&gt;
    /// See &lt;see cref=""{declaredNamespace}.Class1""/&gt;
    /// &lt;/summary&gt;
    public class Class1
    {{
    }}
}}</Document>
<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
namespace Foo
{{
    using {declaredNamespace};

    /// &lt;summary&gt;
    /// See &lt;see cref=""Class1""/&gt;
    /// See &lt;see cref=""{declaredNamespace}.Class1""/&gt;
    /// &lt;/summary&gt;
    class RefClass
    {{
    }}
}}</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"/// <summary>
/// See <see cref=""Class1""/>
/// See <see cref=""Class1""/>
/// </summary>
public class Class1
{
}
";
            var expectedSourceReference =
@"
namespace Foo
{
    /// <summary>
    /// See <see cref=""Class1""/>
    /// See <see cref=""Class1""/>
    /// </summary>
    class RefClass
    {
    }
}";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        }

        [WorkItem("https://github.com/dotnet/roslyn/issues/33890")]
        [Fact]
        public async Task ChangeNamespace_ExtensionMethodInReducedForm()
        {
            var defaultNamespace = "A";

            var (folder, filePath) = CreateDocumentFilePath(["B", "C"], "File1.cs");
            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]{defaultNamespace}
{{
    public static class Extensions
    {{ 
        public static bool Foo(this Class1 c1) => true;
    }}
}}</Document>
<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}"">
namespace {defaultNamespace}
{{
    using System;

    public class Class1
    {{
        public bool Bar(Class1 c1) => c1.Foo();
    }}
}}</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
$@"namespace A.B.C
{{
    public static class Extensions
    {{
        public static bool Foo(this Class1 c1) => true;
    }}
}}";
            var expectedSourceReference =
$@"
namespace {defaultNamespace}
{{
    using System;
    using A.B.C;

    public class Class1
    {{
        public bool Bar(Class1 c1) => c1.Foo();
    }}
}}";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        }

        [WorkItem("https://github.com/dotnet/roslyn/issues/33890")]
        [Fact]
        public async Task ChangeNamespace_ExternsionMethodInRegularForm()
        {
            var defaultNamespace = "A";

            var (folder, filePath) = CreateDocumentFilePath(["B", "C"], "File1.cs");
            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]A
{{
    public static class Extensions
    {{ 
        public static bool Foo(this Class1 c1) => true;
    }}
}}</Document>
<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}"">
using System;

namespace A
{{
    public class Class1
    {{
        public bool Bar(Class1 c1) => Extensions.Foo(c1);
    }}
}}</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
$@"namespace A.B.C
{{
    public static class Extensions
    {{
        public static bool Foo(this Class1 c1) => true;
    }}
}}";
            var expectedSourceReference =
$@"
using System;
using A.B.C;

namespace A
{{
    public class Class1
    {{
        public bool Bar(Class1 c1) => Extensions.Foo(c1);
    }}
}}";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        }

        [WorkItem("https://github.com/dotnet/roslyn/issues/33890")]
        [Fact]
        public async Task ChangeNamespace_ContainsBothTypeAndExternsionMethod()
        {
            var defaultNamespace = "A";

            var (folder, filePath) = CreateDocumentFilePath(["B", "C"], "File1.cs");
            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]A
{{
    public static class Extensions
    {{ 
        public static bool Foo(this Class1 c1) => true;
    }}

    public class Class2
    {{ }}
}}</Document>
<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}"">
using System;

namespace A
{{
    public class Class1
    {{
        public bool Bar(Class1 c1, Class2 c2) => c2 == null ? c1.Foo() : true;
    }}
}}</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"namespace A.B.C
{
    public static class Extensions
    {
        public static bool Foo(this Class1 c1) => true;
    }

    public class Class2
    { }
}";
            var expectedSourceReference =
@"
using System;
using A.B.C;

namespace A
{
    public class Class1
    {
        public bool Bar(Class1 c1, Class2 c2) => c2 == null ? c1.Foo() : true;
    }
}";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        }

        [WorkItem("https://github.com/dotnet/roslyn/issues/33890")]
        [Fact]
        public async Task ChangeNamespace_WithExtensionMethodReferencesInVBDocument()
        {
            var defaultNamespace = "A.B.C";
            var declaredNamespace = "A.B.C.D";

            var (folder, filePath) = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}"">
using System;

namespace [||]{declaredNamespace}
{{
    public static class Extensions
    {{
        public static bool Foo(this String s) => true;
    }}
}}</Document>
    </Project>    
<Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
Imports {declaredNamespace}

Public Class VBClass
    Public Function Foo(s As string) As Boolean
        Return s.Foo()
    End Function
End Class</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
$@"
using System;

namespace {defaultNamespace}
{{
    public static class Extensions
    {{
        public static bool Foo(this string s) => true;
    }}
}}";
            var expectedSourceReference =
$@"
Imports {defaultNamespace}

Public Class VBClass
    Public Function Foo(s As string) As Boolean
        Return s.Foo()
    End Function
End Class";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        }

        [WorkItem("https://github.com/dotnet/roslyn/issues/37891")]
        [Fact]
        public async Task ChangeNamespace_WithMemberAccessReferencesInOtherDocument()
        {
            var defaultNamespace = "A";
            var declaredNamespace = "Foo.Bar.Baz";

            var documentPath1 = CreateDocumentFilePath(["B", "C"], "File1.cs");
            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{documentPath1.folder}"" FilePath=""{documentPath1.filePath}""> 
namespace [||]{declaredNamespace}
{{
    enum Enum1 
    {{
        A,
        B,
        C
    }}
}}</Document>
<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
namespace Foo
{{
    class RefClass
    {{
        Enum1 M1()
        {{
            return {declaredNamespace}.Enum1.A;
        }}
    }}
}}</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"namespace A.B.C
{
    enum Enum1
    {
        A,
        B,
        C
    }
}";
            var expectedSourceReference =
@"
using A.B.C;

namespace Foo
{
    class RefClass
    {
        Enum1 M1()
        {
            return Enum1.A;
        }
    }
}";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        }

        [WorkItem("https://github.com/dotnet/roslyn/issues/37891")]
        [Fact]
        public async Task ChangeToGlobalNamespace_WithMemberAccessReferencesInOtherDocument()
        {
            var defaultNamespace = "";
            var declaredNamespace = "Foo.Bar.Baz";

            var documentPath1 = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{documentPath1.folder}"" FilePath=""{documentPath1.filePath}""> 
namespace [||]{declaredNamespace}
{{
    enum Enum1 
    {{
        A,
        B,
        C
    }}
}}</Document>
<Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
namespace Foo
{{
    class RefClass
    {{
        Enum1 M1()
        {{
            return {declaredNamespace}.Enum1.A;
        }}
    }}
}}</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"enum Enum1
{
    A,
    B,
    C
}
";
            var expectedSourceReference =
@"namespace Foo
{
    class RefClass
    {
        Enum1 M1()
        {
            return Enum1.A;
        }
    }
}";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        }

        [WorkItem("https://github.com/dotnet/roslyn/issues/37891")]
        [Fact]
        public async Task ChangeNamespace_WithMemberAccessReferencesInVBDocument()
        {
            var defaultNamespace = "A.B.C";
            var declaredNamespace = "A.B.C.D";

            var documentPath1 = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{documentPath1.folder}"" FilePath=""{documentPath1.filePath}""> 
namespace [||]{declaredNamespace}
{{
    public enum Enum1
    {{
        A,
        B,
        C
    }}
}}</Document>
    </Project>    
<Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
Public Class VBClass
    Sub M()
        Dim x = A.B.C.D.Enum1.A
    End Sub
End Class</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"namespace A.B.C
{
    public enum Enum1
    {
        A,
        B,
        C
    }
}";
            var expectedSourceReference =
@"Public Class VBClass
    Sub M()
        Dim x = A.B.C.Enum1.A
    End Sub
End Class";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        }

        [WorkItem("https://github.com/dotnet/roslyn/issues/37891")]
        [Fact]
        public async Task ChangeToGlobalNamespace_WithMemberAccessReferencesInVBDocument()
        {
            var defaultNamespace = "";
            var declaredNamespace = "A.B.C.D";

            var documentPath1 = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{documentPath1.folder}"" FilePath=""{documentPath1.filePath}""> 
namespace [||]{declaredNamespace}
{{
    public enum Enum1
    {{
        A,
        B,
        C
    }}
}}</Document>
    </Project>    
<Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
Public Class VBClass
    Sub M()
        Dim x = A.B.C.D.Enum1.A
    End Sub
End Class</Document>
    </Project>
</Workspace>";

            var expectedSourceOriginal =
@"public enum Enum1
{
    A,
    B,
    C
}
";
            var expectedSourceReference =
@"Public Class VBClass
    Sub M()
        Dim x = Enum1.A
    End Sub
End Class";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal, expectedSourceReference);
        }

        [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1889796")]
        public async Task ChangeNamespace_DoesNotThrowInDuplicateProgramDeclaration()
        {
            var defaultNamespace = "A";

            // No change namespace action because the folder name is not valid identifier
            var (topLevelProgramFolder, topLevelProgramFilePath) = CreateDocumentFilePath(["3B", "C"], "Program.cs");
            var (duplicateProgramFolder, _) = CreateDocumentFilePath([], "Program.cs");

            var code =
$$"""
<Workspace>
    <Project Language="C#" AssemblyName="Assembly1" FilePath="{{ProjectFilePath}}" RootNamespace="{{defaultNamespace}}" CommonReferences="true">
        <Document Folders="{{duplicateProgramFolder}}" FilePath="{{duplicateProgramFolder}}"> 
internal class [||]Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
    }
}
        </Document>
        <Document Folders="{{topLevelProgramFolder}}" FilePath="{{topLevelProgramFilePath}}"> 
Console.WriteLine("Hello Two");
        </Document>
    </Project>
</Workspace>
""";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal: null);
        }
    }
}
