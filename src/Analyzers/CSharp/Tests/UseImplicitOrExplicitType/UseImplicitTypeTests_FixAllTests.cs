// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.UseImplicitType
{
    public partial class UseImplicitTypeTests
    {
        #region "Fix all occurrences tests"

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInDocumentScope_PreferImplicitTypeEverywhere()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

class Program
{
    static int F(int x, int y)
    {
        {|FixAllInDocument:int|} i1 = 0;
        Program p = new Program();
        Tuple&lt;bool, int&gt; tuple = Tuple.Create(true, 1);

        return i1;
    }
}
        </Document>
        <Document>
using System;

class Program2
{
    static int F(int x, int y)
    {
        int i1 = 0;
        Program2 p = new Program2();
        Tuple&lt;bool, int&gt; tuple = Tuple.Create(true, 1);

        return i1;
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
using System;

class Program2
{
    static int F(int x, int y)
    {
        int i1 = 0;
        Program2 p = new Program2();
        Tuple&lt;bool, int&gt; tuple = Tuple.Create(true, 1);

        return i1;
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

class Program
{
    static int F(int x, int y)
    {
        var i1 = 0;
        var p = new Program();
        var tuple = Tuple.Create(true, 1);

        return i1;
    }
}
        </Document>
        <Document>
using System;

class Program2
{
    static int F(int x, int y)
    {
        int i1 = 0;
        Program2 p = new Program2();
        Tuple&lt;bool, int&gt; tuple = Tuple.Create(true, 1);

        return i1;
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
using System;

class Program2
{
    static int F(int x, int y)
    {
        int i1 = 0;
        Program2 p = new Program2();
        Tuple&lt;bool, int&gt; tuple = Tuple.Create(true, 1);

        return i1;
    }
}
        </Document>
    </Project>
</Workspace>";

            await TestInRegularAndScriptAsync(input, expected, options: ImplicitTypeEverywhere());
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInProject_PreferImplicitTypeEverywhere()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

class Program
{
    static int F(int x, int y)
    {
        {|FixAllInProject:int|} i1 = 0;
        Program p = new Program();
        Tuple&lt;bool, int&gt; tuple = Tuple.Create(true, 1);

        return i1;
    }
}
        </Document>
        <Document>
using System;

class Program2
{
    static int F(int x, int y)
    {
        int i2 = 0;
        Program2 p2 = new Program2();
        Tuple&lt;bool, int&gt; tuple2 = Tuple.Create(true, 1);

        return i2;
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
using System;

class Program2
{
    static int F(int x, int y)
    {
        int i3 = 0;
        Program2 p3 = new Program2();
        Tuple&lt;bool, int&gt; tuple3 = Tuple.Create(true, 1);

        return i3;
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

class Program
{
    static int F(int x, int y)
    {
        var i1 = 0;
        var p = new Program();
        var tuple = Tuple.Create(true, 1);

        return i1;
    }
}
        </Document>
        <Document>
using System;

class Program2
{
    static int F(int x, int y)
    {
        var i2 = 0;
        var p2 = new Program2();
        var tuple2 = Tuple.Create(true, 1);

        return i2;
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
using System;

class Program2
{
    static int F(int x, int y)
    {
        int i3 = 0;
        Program2 p3 = new Program2();
        Tuple&lt;bool, int&gt; tuple3 = Tuple.Create(true, 1);

        return i3;
    }
}
        </Document>
    </Project>
</Workspace>";

            await TestInRegularAndScriptAsync(input, expected, options: ImplicitTypeEverywhere());
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInSolution_PreferImplicitTypeEverywhere()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

class Program
{
    static int F(int x, int y)
    {
        {|FixAllInSolution:int|} i1 = 0;
        Program p = new Program();
        Tuple&lt;bool, int&gt; tuple = Tuple.Create(true, 1);

        return i1;
    }
}
        </Document>
        <Document>
using System;

class Program2
{
    static int F(int x, int y)
    {
        int i2 = 0;
        Program2 p2 = new Program2();
        Tuple&lt;bool, int&gt; tuple2 = Tuple.Create(true, 1);

        return i2;
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
using System;

class Program2
{
    static int F(int x, int y)
    {
        int i3 = 0;
        Program2 p3 = new Program2();
        Tuple&lt;bool, int&gt; tuple3 = Tuple.Create(true, 1);

        return i3;
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

class Program
{
    static int F(int x, int y)
    {
        var i1 = 0;
        var p = new Program();
        var tuple = Tuple.Create(true, 1);

        return i1;
    }
}
        </Document>
        <Document>
using System;

class Program2
{
    static int F(int x, int y)
    {
        var i2 = 0;
        var p2 = new Program2();
        var tuple2 = Tuple.Create(true, 1);

        return i2;
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
using System;

class Program2
{
    static int F(int x, int y)
    {
        var i3 = 0;
        var p3 = new Program2();
        var tuple3 = Tuple.Create(true, 1);

        return i3;
    }
}
        </Document>
    </Project>
</Workspace>";

            await TestInRegularAndScriptAsync(input, expected, options: ImplicitTypeEverywhere());
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitType)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInDocumentScope_PreferBuiltInTypes()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

class Program
{
    static int F(int x, int y)
    {
        {|FixAllInDocument:Program|} p = new Program();
        int i1 = 0;
        Tuple&lt;bool, int&gt; tuple = Tuple.Create(true, 1);

        return i1;
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

class Program
{
    static int F(int x, int y)
    {
        var p = new Program();
        int i1 = 0;
        var tuple = Tuple.Create(true, 1);

        return i1;
    }
}
        </Document>
    </Project>
</Workspace>";

            await TestInRegularAndScriptAsync(input, expected, options: ImplicitTypeButKeepIntrinsics());
        }

        #endregion
    }
}
