// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.SyncNamespace
{
    public partial class SyncNamespaceTests : CSharpSyncNamespaceTestsBase
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
        public async Task Mismatch_NoAction1_ChangeNamespace()
        {
            var defaultNamespace = "A";
            var declaredNamespace = "Foo.Bar";

            // No change namespace action because the folder name is not valid identifier
            var documentPath = CreateDocumentFilePath(new[] { "3B", "C" }, "File1.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" DefaultNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{documentPath.folder}"" FilePath=""{documentPath.filePath}""> 
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
        public async Task Mismatch_NoAction2_ChangeNamespace()
        {
            var defaultNamespace = "A";
            var declaredNamespace = "Foo.Bar";

            // No change namespace action because the folder name is not valid identifier
            var documentPath = CreateDocumentFilePath(new[] { "B.3C", "D" }, "File1.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" DefaultNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{documentPath.folder}"" FilePath=""{documentPath.filePath}""> 
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
        public async Task Mismatch_SingleDocumentNoRef_ChangeNamespace()
        {
            var defaultNamespace = "A";
            var declaredNamespace = "Foo.Bar";

            var documentPath = CreateDocumentFilePath(new[] { "B", "C" }, "File1.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" DefaultNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{documentPath.folder}"" FilePath=""{documentPath.filePath}""> 
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
        public async Task Mismatch_SingleDocumentLocalRef_ChangeNamespace()
        {
            var defaultNamespace = "A";
            var declaredNamespace = "Foo.Bar";

            var documentPath = CreateDocumentFilePath(new[] { "B", "C" }, "File1.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" DefaultNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{documentPath.folder}"" FilePath=""{documentPath.filePath}""> 
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

        void Class1.M1(){}
    }
}";
            await TestChangeNamespaceAsync(code, expectedSourceOriginal);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
        public async Task Mismatch_MultipleDocumentLocalRef_ChangeNamespace()
        {
            var defaultNamespace = "A";
            var declaredNamespace = "Foo.Bar.Baz";

            var documentPath1 = CreateDocumentFilePath(new[] { "B", "C" }, "File1.cs");
            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" DefaultNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{documentPath1.folder}"" FilePath=""{documentPath1.filePath}""> 
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
@"
using Foo;
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
        public async Task Mismatch_MultipleDocumentsLocalQualifiedRef_ChangeNamespace()
        {
            var defaultNamespace = "A";
            var declaredNamespace = "Foo.Bar.Baz";

            var documentPath1 = CreateDocumentFilePath(new[] { "B", "C" }, "File1.cs");
            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" DefaultNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{documentPath1.folder}"" FilePath=""{documentPath1.filePath}""> 
namespace [||]{declaredNamespace}
{{
    class Class1 
    {{ 
        private Foo.Class2 c2;
        private Bar.Class3 c3;
        private Baz.Class4 c4;
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
@"
using Foo;
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
        public async Task Mismatch_WithReferencesInOtherDocument1_ChangeNamespace()
        {
            var defaultNamespace = "A";
            var declaredNamespace = "Foo.Bar.Baz";

            var documentPath1 = CreateDocumentFilePath(new[] { "B", "C" }, "File1.cs");
            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" DefaultNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{documentPath1.folder}"" FilePath=""{documentPath1.filePath}""> 
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
        public async Task Mismatch_WithReferencesInOtherDocument2_ChangeNamespace()
        {
            var defaultNamespace = "A";
            var declaredNamespace = "Foo.Bar.Baz";

            var documentPath1 = CreateDocumentFilePath(new[] { "B", "C" }, "File1.cs");
            var documentPath2 = CreateDocumentFilePath(Array.Empty<string>(), "File2.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" DefaultNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{documentPath1.folder}"" FilePath=""{documentPath1.filePath}""> 
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


        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
        public async Task Mismatch_SingleDocumentNoRef_ToGlobal_ChangeNamespace()
        {
            var defaultNamespace = "";
            var declaredNamespace = "Foo.Bar";

            var documentPath = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" DefaultNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{documentPath.folder}"" FilePath=""{documentPath.filePath}""> 
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
    }
}
