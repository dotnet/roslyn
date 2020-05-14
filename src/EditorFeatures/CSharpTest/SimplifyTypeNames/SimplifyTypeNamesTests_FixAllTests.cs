﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SimplifyTypeNames
{
    public partial class SimplifyTypeNamesTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        #region "Fix all occurrences tests"

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInDocument()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

class Program
{
    static System.Int32 F(System.Int32 x, System.Int16 y)
    {
        {|FixAllInDocument:System.Int32|} i1 = 0;
        System.Int16 s1 = 0;
        System.Int32 i2 = 0;
        return i1 + s1 + i2;
    }
}
        </Document>
        <Document>
using System;

class Program2
{
    static System.Int32 F(System.Int32 x, System.Int16 y)
    {
        System.Int32 i1 = 0;
        System.Int16 s1 = 0;
        System.Int32 i2 = 0;
        return i1 + s1 + i2;
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
using System;

class Program2
{
    static System.Int32 F(System.Int32 x, System.Int16 y)
    {
        System.Int32 i1 = 0;
        System.Int16 s1 = 0;
        System.Int32 i2 = 0;
        return i1 + s1 + i2;
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
    static int F(int x, short y)
    {
        int i1 = 0;
        short s1 = 0;
        int i2 = 0;
        return i1 + s1 + i2;
    }
}
        </Document>
        <Document>
using System;

class Program2
{
    static System.Int32 F(System.Int32 x, System.Int16 y)
    {
        System.Int32 i1 = 0;
        System.Int16 s1 = 0;
        System.Int32 i2 = 0;
        return i1 + s1 + i2;
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
using System;

class Program2
{
    static System.Int32 F(System.Int32 x, System.Int16 y)
    {
        System.Int32 i1 = 0;
        System.Int16 s1 = 0;
        System.Int32 i2 = 0;
        return i1 + s1 + i2;
    }
}
        </Document>
    </Project>
</Workspace>";

            await TestInRegularAndScriptAsync(input, expected, options: PreferIntrinsicTypeEverywhere);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInProject()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

class Program
{
    static System.Int32 F(System.Int32 x, System.Int16 y)
    {
        {|FixAllInProject:System.Int32|} i1 = 0;
        System.Int16 s1 = 0;
        System.Int32 i2 = 0;
        return i1 + s1 + i2;
    }
}
        </Document>
        <Document>
using System;

class Program2
{
    static System.Int32 F(System.Int32 x, System.Int16 y)
    {
        System.Int32 i1 = 0;
        System.Int16 s1 = 0;
        System.Int32 i2 = 0;
        return i1 + s1 + i2;
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
using System;

class Program2
{
    static System.Int32 F(System.Int32 x, System.Int16 y)
    {
        System.Int32 i1 = 0;
        System.Int16 s1 = 0;
        System.Int32 i2 = 0;
        return i1 + s1 + i2;
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
    static int F(int x, short y)
    {
        int i1 = 0;
        short s1 = 0;
        int i2 = 0;
        return i1 + s1 + i2;
    }
}
        </Document>
        <Document>
using System;

class Program2
{
    static int F(int x, short y)
    {
        int i1 = 0;
        short s1 = 0;
        int i2 = 0;
        return i1 + s1 + i2;
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
using System;

class Program2
{
    static System.Int32 F(System.Int32 x, System.Int16 y)
    {
        System.Int32 i1 = 0;
        System.Int16 s1 = 0;
        System.Int32 i2 = 0;
        return i1 + s1 + i2;
    }
}
        </Document>
    </Project>
</Workspace>";

            await TestInRegularAndScriptAsync(input, expected, options: PreferIntrinsicTypeEverywhere);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInSolution()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

class Program
{
    static System.Int32 F(System.Int32 x, System.Int16 y)
    {
        {|FixAllInSolution:System.Int32|} i1 = 0;
        System.Int16 s1 = 0;
        System.Int32 i2 = 0;
        return i1 + s1 + i2;
    }
}
        </Document>
        <Document>
using System;

class Program2
{
    static System.Int32 F(System.Int32 x, System.Int16 y)
    {
        System.Int32 i1 = 0;
        System.Int16 s1 = 0;
        System.Int32 i2 = 0;
        return i1 + s1 + i2;
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
using System;

class Program2
{
    static System.Int32 F(System.Int32 x, System.Int16 y)
    {
        System.Int32 i1 = 0;
        System.Int16 s1 = 0;
        System.Int32 i2 = 0;
        return i1 + s1 + i2;
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
    static int F(int x, short y)
    {
        int i1 = 0;
        short s1 = 0;
        int i2 = 0;
        return i1 + s1 + i2;
    }
}
        </Document>
        <Document>
using System;

class Program2
{
    static int F(int x, short y)
    {
        int i1 = 0;
        short s1 = 0;
        int i2 = 0;
        return i1 + s1 + i2;
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
using System;

class Program2
{
    static int F(int x, short y)
    {
        int i1 = 0;
        short s1 = 0;
        int i2 = 0;
        return i1 + s1 + i2;
    }
}
        </Document>
    </Project>
</Workspace>";

            await TestInRegularAndScriptAsync(input, expected, options: PreferIntrinsicTypeEverywhere);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInSolution_SimplifyMemberAccess()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;
class ProgramA
{
    private int x = 0;
    private int y = 0;
    private int z = 0;

    private System.Int32 F(System.Int32 p1, System.Int16 p2)
    {
        System.Int32 i1 = this.x;
        System.Int16 s1 = this.y;
        System.Int32 i2 = this.z;
        {|FixAllInSolution:System.Console.Write|}(i1 + s1 + i2);
        System.Console.WriteLine(i1 + s1 + i2);
        System.Exception ex = null;
        return i1 + s1 + i2;
    }
}

class ProgramB
{
    private int x2 = 0;
    private int y2 = 0;
    private int z2 = 0;

    private System.Int32 F(System.Int32 p1, System.Int16 p2)
    {
        System.Int32 i1 = this.x2;
        System.Int16 s1 = this.y2;
        System.Int32 i2 = this.z2;
        System.Console.Write(i1 + s1 + i2);
        System.Console.WriteLine(i1 + s1 + i2);
        System.Exception ex = null;
        return i1 + s1 + i2;
    }
}
        </Document>
        <Document>
using System;
class ProgramA2
{
    private int x = 0;
    private int y = 0;
    private int z = 0;

    private System.Int32 F(System.Int32 p1, System.Int16 p2)
    {
        System.Int32 i1 = this.x;
        System.Int16 s1 = this.y;
        System.Int32 i2 = this.z;
        System.Console.Write(i1 + s1 + i2);
        System.Console.WriteLine(i1 + s1 + i2);
        System.Exception ex = null;
        return i1 + s1 + i2;
    }
}

class ProgramB2
{
    private int x2 = 0;
    private int y2 = 0;
    private int z2 = 0;

    private System.Int32 F(System.Int32 p1, System.Int16 p2)
    {
        System.Int32 i1 = this.x2;
        System.Int16 s1 = this.y2;
        System.Int32 i2 = this.z2;
        System.Console.Write(i1 + s1 + i2);
        System.Console.WriteLine(i1 + s1 + i2);
        System.Exception ex = null;
        return i1 + s1 + i2;
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
using System;
class ProgramA3
{
    private int x = 0;
    private int y = 0;
    private int z = 0;

    private System.Int32 F(System.Int32 p1, System.Int16 p2)
    {
        System.Int32 i1 = this.x;
        System.Int16 s1 = this.y;
        System.Int32 i2 = this.z;
        System.Console.Write(i1 + s1 + i2);
        System.Console.WriteLine(i1 + s1 + i2);
        System.Exception ex = null;
        return i1 + s1 + i2;
    }
}

class ProgramB3
{
    private int x2 = 0;
    private int y2 = 0;
    private int z2 = 0;

    private System.Int32 F(System.Int32 p1, System.Int16 p2)
    {
        System.Int32 i1 = this.x2;
        System.Int16 s1 = this.y2;
        System.Int32 i2 = this.z2;
        System.Console.Write(i1 + s1 + i2);
        System.Console.WriteLine(i1 + s1 + i2);
        System.Exception ex = null;
        return i1 + s1 + i2;
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
class ProgramA
{
    private int x = 0;
    private int y = 0;
    private int z = 0;

    private System.Int32 F(System.Int32 p1, System.Int16 p2)
    {
        System.Int32 i1 = this.x;
        System.Int16 s1 = this.y;
        System.Int32 i2 = this.z;
        Console.Write(i1 + s1 + i2);
        Console.WriteLine(i1 + s1 + i2);
        System.Exception ex = null;
        return i1 + s1 + i2;
    }
}

class ProgramB
{
    private int x2 = 0;
    private int y2 = 0;
    private int z2 = 0;

    private System.Int32 F(System.Int32 p1, System.Int16 p2)
    {
        System.Int32 i1 = this.x2;
        System.Int16 s1 = this.y2;
        System.Int32 i2 = this.z2;
        Console.Write(i1 + s1 + i2);
        Console.WriteLine(i1 + s1 + i2);
        System.Exception ex = null;
        return i1 + s1 + i2;
    }
}
        </Document>
        <Document>
using System;
class ProgramA2
{
    private int x = 0;
    private int y = 0;
    private int z = 0;

    private System.Int32 F(System.Int32 p1, System.Int16 p2)
    {
        System.Int32 i1 = this.x;
        System.Int16 s1 = this.y;
        System.Int32 i2 = this.z;
        Console.Write(i1 + s1 + i2);
        Console.WriteLine(i1 + s1 + i2);
        System.Exception ex = null;
        return i1 + s1 + i2;
    }
}

class ProgramB2
{
    private int x2 = 0;
    private int y2 = 0;
    private int z2 = 0;

    private System.Int32 F(System.Int32 p1, System.Int16 p2)
    {
        System.Int32 i1 = this.x2;
        System.Int16 s1 = this.y2;
        System.Int32 i2 = this.z2;
        Console.Write(i1 + s1 + i2);
        Console.WriteLine(i1 + s1 + i2);
        System.Exception ex = null;
        return i1 + s1 + i2;
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
using System;
class ProgramA3
{
    private int x = 0;
    private int y = 0;
    private int z = 0;

    private System.Int32 F(System.Int32 p1, System.Int16 p2)
    {
        System.Int32 i1 = this.x;
        System.Int16 s1 = this.y;
        System.Int32 i2 = this.z;
        Console.Write(i1 + s1 + i2);
        Console.WriteLine(i1 + s1 + i2);
        System.Exception ex = null;
        return i1 + s1 + i2;
    }
}

class ProgramB3
{
    private int x2 = 0;
    private int y2 = 0;
    private int z2 = 0;

    private System.Int32 F(System.Int32 p1, System.Int16 p2)
    {
        System.Int32 i1 = this.x2;
        System.Int16 s1 = this.y2;
        System.Int32 i2 = this.z2;
        Console.Write(i1 + s1 + i2);
        Console.WriteLine(i1 + s1 + i2);
        System.Exception ex = null;
        return i1 + s1 + i2;
    }
}
        </Document>
    </Project>
</Workspace>";

            await TestInRegularAndScriptAsync(input, expected);
        }

        #endregion
    }
}
