// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.InvertConditional
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsInvertConditional)]
    public partial class InvertConditionalTests : AbstractCSharpCodeActionTest
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task InvertConditional_FixAllInDocument()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool x, int a, int b)
    {
        var c = {|FixAllInDocument:|}x ? a : b;
    }

    void M2(bool x, int a, int b)
    {
        var c = x ? a : b;
    }
}",
@"class C
{
    void M(bool x, int a, int b)
    {
        var c = !x ? b : a;
    }

    void M2(bool x, int a, int b)
    {
        var c = !x ? b : a;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task InvertConditional_FixAllInProject()
        {
            await TestInRegularAndScriptAsync(
@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
class Program1
{
    void M1(bool x, int a, int b)
    {
        var c = {|FixAllInProject:|}x ? a : b;
    }
}
        </Document>
        <Document>
class Program2
{
    void M2(bool x, int a, int b)
    {
        var c = x ? a : b;
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Program3
{
    void M3(bool x, int a, int b)
    {
        var c = x ? a : b;
    }
}
        </Document>
    </Project>
</Workspace>",
@"<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
class Program1
{
    void M1(bool x, int a, int b)
    {
        var c = !x ? b : a;
    }
}
        </Document>
        <Document>
class Program2
{
    void M2(bool x, int a, int b)
    {
        var c = !x ? b : a;
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Program3
{
    void M3(bool x, int a, int b)
    {
        var c = x ? a : b;
    }
}
        </Document>
    </Project>
</Workspace>");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task InvertConditional_FixAllInSolution()
        {
            await TestInRegularAndScriptAsync(
@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
class Program1
{
    void M1(bool x, int a, int b)
    {
        var c = {|FixAllInSolution:|}x ? a : b;
    }
}
        </Document>
        <Document>
class Program2
{
    void M2(bool x, int a, int b)
    {
        var c = x ? a : b;
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Program3
{
    void M3(bool x, int a, int b)
    {
        var c = x ? a : b;
    }
}
        </Document>
    </Project>
</Workspace>",
@"<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
class Program1
{
    void M1(bool x, int a, int b)
    {
        var c = !x ? b : a;
    }
}
        </Document>
        <Document>
class Program2
{
    void M2(bool x, int a, int b)
    {
        var c = !x ? b : a;
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Program3
{
    void M3(bool x, int a, int b)
    {
        var c = !x ? b : a;
    }
}
        </Document>
    </Project>
</Workspace>");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task InvertConditional_FixAllInContainingMember()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool x, int a, int b)
    {
        var c = {|FixAllInContainingMember:|}x ? a : b;
        var c2 = x ? a : b;
    }

    void M2(bool x, int a, int b)
    {
        var c = x ? a : b;
        var c2 = x ? a : b;
    }
}

class C2
{
    void M(bool x, int a, int b)
    {
        var c = x ? a : b;
        var c2 = x ? a : b;
    }
}",
@"class C
{
    void M(bool x, int a, int b)
    {
        var c = !x ? b : a;
        var c2 = !x ? b : a;
    }

    void M2(bool x, int a, int b)
    {
        var c = x ? a : b;
        var c2 = x ? a : b;
    }
}

class C2
{
    void M(bool x, int a, int b)
    {
        var c = x ? a : b;
        var c2 = x ? a : b;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task InvertConditional_FixAllInContainingType()
        {
            await TestInRegularAndScriptAsync(
@"partial class C
{
    void M(bool x, int a, int b)
    {
        var c = {|FixAllInContainingType:|}x ? a : b;
        var c2 = x ? a : b;
    }

    void M2(bool x, int a, int b)
    {
        var c = x ? a : b;
        var c2 = x ? a : b;
    }
}

class C2
{
    void M(bool x, int a, int b)
    {
        var c = x ? a : b;
        var c2 = x ? a : b;
    }
}

partial class C
{
    void M3(bool x, int a, int b)
    {
        var c = x ? a : b;
        var c2 = x ? a : b;
    }
}",
@"partial class C
{
    void M(bool x, int a, int b)
    {
        var c = !x ? b : a;
        var c2 = !x ? b : a;
    }

    void M2(bool x, int a, int b)
    {
        var c = !x ? b : a;
        var c2 = !x ? b : a;
    }
}

class C2
{
    void M(bool x, int a, int b)
    {
        var c = x ? a : b;
        var c2 = x ? a : b;
    }
}

partial class C
{
    void M3(bool x, int a, int b)
    {
        var c = !x ? b : a;
        var c2 = !x ? b : a;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task InvertConditional_FixAllInContainingType_AcrossFiles()
        {
            await TestInRegularAndScriptAsync(
@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
partial class Program1
{
    void M1(bool x, int a, int b)
    {
        var c = {|FixAllInContainingType:|}x ? a : b;
    }

    void M2(bool x, int a, int b)
    {
        var c = x ? a : b;
    }
}
        </Document>
        <Document>
partial class Program1
{
    void M3(bool x, int a, int b)
    {
        var c = x ? a : b;
    }
}

class Program2
{
    void M2(bool x, int a, int b)
    {
        var c = x ? a : b;
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Program3
{
    void M3(bool x, int a, int b)
    {
        var c = x ? a : b;
    }
}
        </Document>
    </Project>
</Workspace>",
@"<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
partial class Program1
{
    void M1(bool x, int a, int b)
    {
        var c = !x ? b : a;
    }

    void M2(bool x, int a, int b)
    {
        var c = !x ? b : a;
    }
}
        </Document>
        <Document>
partial class Program1
{
    void M3(bool x, int a, int b)
    {
        var c = !x ? b : a;
    }
}

class Program2
{
    void M2(bool x, int a, int b)
    {
        var c = x ? a : b;
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Program3
{
    void M3(bool x, int a, int b)
    {
        var c = x ? a : b;
    }
}
        </Document>
    </Project>
</Workspace>");
        }
    }
}
