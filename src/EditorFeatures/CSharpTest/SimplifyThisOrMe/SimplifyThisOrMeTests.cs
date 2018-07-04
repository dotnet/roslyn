// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.SimplifyTypeNames;
using Microsoft.CodeAnalysis.CSharp.SimplifyThisOrMe;
using Microsoft.CodeAnalysis.CSharp.SimplifyTypeNames;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SimplifyThisOrMe
{
    public partial class SimplifyThisOrMeTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpSimplifyThisOrMeDiagnosticAnalyzer(), new CSharpSimplifyThisOrMeCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyThisOrMe)]
        public async Task TestSimplifyDiagnosticId()
        {
            await TestInRegularAndScriptAsync(
@"
using System;

class C
{
    private int x = 0;
    public void z()
    {
        var a = [|this.x|];
    }
}",
@"
using System;

class C
{
    private int x = 0;
    public void z()
    {
        var a = x;
    }
}");
        }

        [WorkItem(6682, "https://github.com/dotnet/roslyn/issues/6682")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyThisOrMe)]
        public async Task TestThisWithNoType()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    dynamic x = 7;

    static void Main(string[] args)
    {
        [|this|].x = default(dynamic);
    }
}",
@"class Program
{
    dynamic x = 7;

    static void Main(string[] args)
    {
        x = default(dynamic);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyThisOrMe)]
        public async Task TestAppropriateDiagnosticOnMissingQualifier()
        {
            await TestDiagnosticInfoAsync(
@"class C
{
    int SomeProperty { get; set; }

    void M()
    {
        [|this|].SomeProperty = 1;
    }
}",
                options: Option(CodeStyleOptions.QualifyPropertyAccess, false, NotificationOption.Warning),
                diagnosticId: IDEDiagnosticIds.RemoveQualificationDiagnosticId,
                diagnosticSeverity: DiagnosticSeverity.Warning);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyThisOrMe)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInSolution_RemoveThis()
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

            await TestInRegularAndScriptAsync(input, expected);
        }
    }
}
