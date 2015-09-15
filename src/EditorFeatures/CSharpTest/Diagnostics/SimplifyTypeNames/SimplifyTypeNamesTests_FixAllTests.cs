// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.CodeFixes.SimplifyTypeNames;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.SimplifyTypeNames;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.SimplifyTypeNames
{
    public partial class SimplifyTypeNamesTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        #region "Fix all occurrences tests"

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public void TestFixAllInDocument()
        {
            var fixAllActionId = SimplifyTypeNamesCodeFixProvider.GetCodeActionId(IDEDiagnosticIds.SimplifyNamesDiagnosticId, "System.Int32");

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
    static int F(int x, System.Int16 y)
    {
        int i1 = 0;
        System.Int16 s1 = 0;
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

            Test(input, expected, compareTokens: false, fixAllActionEquivalenceKey: fixAllActionId);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public void TestFixAllInProject()
        {
            var fixAllActionId = SimplifyTypeNamesCodeFixProvider.GetCodeActionId(IDEDiagnosticIds.SimplifyNamesDiagnosticId, "System.Int32");

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
    static int F(int x, System.Int16 y)
    {
        int i1 = 0;
        System.Int16 s1 = 0;
        int i2 = 0;
        return i1 + s1 + i2;
    }
}
        </Document>
        <Document>
using System;

class Program2
{
    static int F(int x, System.Int16 y)
    {
        int i1 = 0;
        System.Int16 s1 = 0;
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

            Test(input, expected, compareTokens: false, fixAllActionEquivalenceKey: fixAllActionId);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public void TestFixAllInSolution()
        {
            var fixAllActionId = SimplifyTypeNamesCodeFixProvider.GetCodeActionId(IDEDiagnosticIds.SimplifyNamesDiagnosticId, "System.Int32");

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
    static int F(int x, System.Int16 y)
    {
        int i1 = 0;
        System.Int16 s1 = 0;
        int i2 = 0;
        return i1 + s1 + i2;
    }
}
        </Document>
        <Document>
using System;

class Program2
{
    static int F(int x, System.Int16 y)
    {
        int i1 = 0;
        System.Int16 s1 = 0;
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
    static int F(int x, System.Int16 y)
    {
        int i1 = 0;
        System.Int16 s1 = 0;
        int i2 = 0;
        return i1 + s1 + i2;
    }
}
        </Document>
    </Project>
</Workspace>";

            Test(input, expected, compareTokens: false, fixAllActionEquivalenceKey: fixAllActionId);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public void TestFixAllInSolution_RemoveThis()
        {
            var fixAllActionId = SimplifyTypeNamesCodeFixProvider.GetCodeActionId(IDEDiagnosticIds.SimplifyThisOrMeDiagnosticId, null);

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
        System.Int32 i1 = {|FixAllInSolution:this.x|};
        System.Int16 s1 = this.y;
        System.Int32 i2 = this.z;
        System.Console.Write(i1 + s1 + i2);
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
        System.Int32 i1 = x;
        System.Int16 s1 = y;
        System.Int32 i2 = z;
        System.Console.Write(i1 + s1 + i2);
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
        System.Int32 i1 = x2;
        System.Int16 s1 = y2;
        System.Int32 i2 = z2;
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
        System.Int32 i1 = x;
        System.Int16 s1 = y;
        System.Int32 i2 = z;
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
        System.Int32 i1 = x2;
        System.Int16 s1 = y2;
        System.Int32 i2 = z2;
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
        System.Int32 i1 = x;
        System.Int16 s1 = y;
        System.Int32 i2 = z;
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
        System.Int32 i1 = x2;
        System.Int16 s1 = y2;
        System.Int32 i2 = z2;
        System.Console.Write(i1 + s1 + i2);
        System.Console.WriteLine(i1 + s1 + i2);
        System.Exception ex = null;
        return i1 + s1 + i2;
    }
}
        </Document>
    </Project>
</Workspace>";

            Test(input, expected, compareTokens: false, fixAllActionEquivalenceKey: fixAllActionId);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public void TestFixAllInSolution_SimplifyMemberAccess()
        {
            var fixAllActionId = SimplifyTypeNamesCodeFixProvider.GetCodeActionId(IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId, "System.Console");

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

            Test(input, expected, compareTokens: false, fixAllActionEquivalenceKey: fixAllActionId);
        }

        #endregion
    }
}
