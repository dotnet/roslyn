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
    }
}
