// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.SyncNamespace
{
    public partial class SyncNamespaceTests : CSharpSyncNamespaceTestsBase
    {

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
        public async Task SyncNamespace_MissingNotOnDeclaration()
        {
            var code =
@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document Folders=""A\B""> 
namespace NS
{    
    [||]class Class1
    {
    }
}
        </Document>
    </Project>
</Workspace>";
            
            await TestMissingInRegularAndScriptAsync(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
        public async Task SyncNamespace_MissingMultipleNamespaceDeclarations()
        {
            var code =
@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document Folders=""A\B""> 
[||]namespace NS1
{    
    class Class1
    {
    }
}  

namespace NS2
{    
    class Class1
    {
    }
}
        </Document>
    </Project>
</Workspace>";

            await TestMissingInRegularAndScriptAsync(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
        public async Task SyncNamespace_ActionCounts_MoveOnly()
        {
            var code =
@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document Folders=""A\3B""> 
[||]namespace NS1.NS2.NS3
{    
    class Class1
    {
    }
}  
        </Document>
    </Project>
</Workspace>";

            RootNamespace = "NS1";

            // Fixes offered will be move file to matching folder.
            // No rename namespace action since the folder name is invalid identifier.
            await TestActionCountAsync(code, count: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
        public async Task SyncNamespace_MissingMatchingNamespace1()
        {
            var code =

@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document Folders=""A\B""> 
[||]namespace NS.A.B
{    
    class Class1
    {
    }
}  
        </Document>
    </Project>
</Workspace>";
            RootNamespace = "NS";
            await TestMissingInRegularAndScriptAsync(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
        public async Task SyncNamespace_MissingMatchingNamespace2()
        {
            var code =

@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document Folders=""A.B""> 
[||]namespace NS.A.B
{    
    class Class1
    {
    }
}  
        </Document>
    </Project>
</Workspace>";
            RootNamespace = "NS";
            await TestMissingInRegularAndScriptAsync(code);
        }
    }
}
