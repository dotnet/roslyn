// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertTypeOfToNameOf
{
    public partial class ConvertTypeOfToNameOfTests
    {
        [Fact]
        [Trait(Traits.Feature, Traits.Features.ConvertTypeOfToNameOf)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task FixAllDocumentBasic()
        {
            var input = @"class Test
{
    static void Main()
    {
        var typeName1 = {|FixAllInDocument:typeof(Test).Name|};
        var typeName2 = typeof(Test).Name;
        var typeName3 = typeof(Test).Name;
    }
}
";

            var expected = @"class Test
{
    static void Main()
    {
        var typeName1 = nameof(Test);
        var typeName2 = nameof(Test);
        var typeName3 = nameof(Test);
    }
}
";

            await TestInRegularAndScriptAsync(input, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ConvertTypeOfToNameOf)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task FixAllDocumentVariedSingleLine()
        {
            var input = @"class Test
{
    static void Main()
    {
        var typeName1 = {|FixAllInDocument:typeof(Test).Name|}; var typeName2 = typeof(int).Name; var typeName3 = typeof(System.String).Name;
    }
}
";

            var expected = @"class Test
{
    static void Main()
    {
        var typeName1 = nameof(Test); var typeName2 = nameof(System.Int32); var typeName3 = nameof(System.String);
    }
}
";

            await TestInRegularAndScriptAsync(input, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ConvertTypeOfToNameOf)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task FixAllDocumentVariedWithUsing()
        {
            var input = @"using System;

class Test
{
    static void Main()
    {
        var typeName1 = typeof(Test).Name;
        var typeName2 = typeof(int).Name;
        var typeName3 = typeof(String).Name;
        var typeName4 = {|FixAllInDocument:typeof(System.Double).Name|};
    }
}
";

            var expected = @"using System;

class Test
{
    static void Main()
    {
        var typeName1 = nameof(Test);
        var typeName2 = nameof(Int32);
        var typeName3 = nameof(String);
        var typeName4 = nameof(Double);
    }
}
";

            await TestInRegularAndScriptAsync(input, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ConvertTypeOfToNameOf)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task FixAllProject()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
class Test1
{
    static void Main()
    {
        var typeName1 = {|FixAllInProject:typeof(Test1).Name|};
        var typeName2 = typeof(Test1).Name;
        var typeName3 = typeof(Test1).Name;
    }
}
        </Document>
        <Document>
using System;

class Test2
{
    static void Main()
    {
        var typeName1 = typeof(Test1).Name;
        var typeName2 = typeof(int).Name;
        var typeName3 = typeof(System.String).Name;
        var typeName4 = typeof(Double).Name;
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
class Test1
{
    static void Main()
    {
        var typeName1 = nameof(Test1);
        var typeName2 = nameof(Test1);
        var typeName3 = nameof(Test1);
    }
}
        </Document>
        <Document>
using System;

class Test2
{
    static void Main()
    {
        var typeName1 = nameof(Test1);
        var typeName2 = nameof(Int32);
        var typeName3 = nameof(String);
        var typeName4 = nameof(Double);
    }
}
        </Document>
    </Project>
</Workspace>";

            await TestInRegularAndScriptAsync(input, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ConvertTypeOfToNameOf)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task FixAllSolution()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
class Test1
{
    static void Main()
    {
        var typeName1 = {|FixAllInSolution:typeof(Test1).Name|};
        var typeName2 = typeof(Test1).Name;
        var typeName3 = typeof(Test1).Name;
    }
}
        </Document>
        <Document>
using System;

class Test2
{
    static void Main()
    {
        var typeName1 = typeof(Test1).Name;
        var typeName2 = typeof(int).Name;
        var typeName3 = typeof(System.String).Name;
        var typeName4 = typeof(Double).Name;
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Test3
{
    static void Main()
    {
        var typeName2 = typeof(int).Name; var typeName3 = typeof(System.String).Name;
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
class Test1
{
    static void Main()
    {
        var typeName1 = nameof(Test1);
        var typeName2 = nameof(Test1);
        var typeName3 = nameof(Test1);
    }
}
        </Document>
        <Document>
using System;

class Test2
{
    static void Main()
    {
        var typeName1 = nameof(Test1);
        var typeName2 = nameof(Int32);
        var typeName3 = nameof(String);
        var typeName4 = nameof(Double);
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Test3
{
    static void Main()
    {
        var typeName2 = nameof(System.Int32); var typeName3 = nameof(System.String);
    }
}
        </Document>
    </Project>
</Workspace>";

            await TestInRegularAndScriptAsync(input, expected);
        }
    }
}
