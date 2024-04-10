// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.UseRecursivePatterns;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.UseRecursivePatterns
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsUseRecursivePatterns)]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public class UseRecursivePatternsRefactoringFixAllTests : AbstractCSharpCodeActionTest_NoEditor
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
            => new UseRecursivePatternsCodeRefactoringProvider();

        [Fact]
        public async Task UseRecursivePatterns_FixAllInDocument()
        {
            await TestInRegularAndScriptAsync(@"
namespace NS
{
    class C : B
    {
        void M1()
        {
            if (n == a.b.c.d {|FixAllInDocument:|}&& a.b.c.a == n)
            {
            }

            if (this.P1 < 1 && 2 >= this.P2)
            {
            }

            if (!B1 && B2)
            {
            }
        }
    }

    class D : C
    {
        void M2()
        {
            _ = this switch
            {
                { a: var x } when x is { b: n } => 0
            };

            switch (this)
            {
                case { a: var x } when x is { b: n }:
                    break;
            }
        }
    }

    class B
    {
        public const C n = null;
        public C a, b, c, d;
        public int P1, P2, P3;
        public bool B1, B2;
        public C CP1, CP2;
        public static C SCP1, SCP2;
        public static int SP1, SP2;
        public C m() { return null; }
    }
}", @"
namespace NS
{
    class C : B
    {
        void M1()
        {
            if (a.b.c is { d: n, a: n })
            {
            }

            if (this is { P1: < 1, P2: <= 2 })
            {
            }

            if (this is { B1: false, B2: true })
            {
            }
        }
    }

    class D : C
    {
        void M2()
        {
            _ = this switch
            {
                { a: { b: n } x } => 0
            };

            switch (this)
            {
                case { a: { b: n } x }:
                    break;
            }
        }
    }

    class B
    {
        public const C n = null;
        public C a, b, c, d;
        public int P1, P2, P3;
        public bool B1, B2;
        public C CP1, CP2;
        public static C SCP1, SCP2;
        public static int SP1, SP2;
        public C m() { return null; }
    }
}");
        }

        [Fact]
        public async Task UseRecursivePatterns_FixAllInProject()
        {
            await TestInRegularAndScriptAsync(@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
namespace NS
{
    class C : B
    {
        void M1()
        {
            _ = this switch
            {
                { a: var x } {|FixAllInProject:|}when x is { b: n } => 0
            };

            switch (this)
            {
                case { a: var x } when x is { b: n }:
                    break;
            }
        }
    }
}
        </Document>
        <Document>
namespace NS
{
    class D : C
    {
        void M2()
        {
            _ = this switch
            {
                { a: var x } when x is { b: n } => 0
            };

            switch (this)
            {
                case { a: var x } when x is { b: n }:
                    break;
            }
        }
    }
}
        </Document>
        <Document>
namespace NS
{
    class B
    {
        public const C n = null;
        public C a, b, c, d;
        public int P1, P2, P3;
        public bool B1, B2;
        public C CP1, CP2;
        public static C SCP1, SCP2;
        public static int SP1, SP2;
        public C m() { return null; }
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
namespace NS
{
    class E : C
    {
        void M3()
        {
            _ = this switch
            {
                { a: var x } when x is { b: n } => 0
            };

            switch (this)
            {
                case { a: var x } when x is { b: n }:
                    break;
            }
        }
    }
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
namespace NS
{
    class C : B
    {
        void M1()
        {
            _ = this switch
            {
                { a: { b: n } x } => 0
            };

            switch (this)
            {
                case { a: { b: n } x }:
                    break;
            }
        }
    }
}
        </Document>
        <Document>
namespace NS
{
    class D : C
    {
        void M2()
        {
            _ = this switch
            {
                { a: { b: n } x } => 0
            };

            switch (this)
            {
                case { a: { b: n } x }:
                    break;
            }
        }
    }
}
        </Document>
        <Document>
namespace NS
{
    class B
    {
        public const C n = null;
        public C a, b, c, d;
        public int P1, P2, P3;
        public bool B1, B2;
        public C CP1, CP2;
        public static C SCP1, SCP2;
        public static int SP1, SP2;
        public C m() { return null; }
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
namespace NS
{
    class E : C
    {
        void M3()
        {
            _ = this switch
            {
                { a: var x } when x is { b: n } => 0
            };

            switch (this)
            {
                case { a: var x } when x is { b: n }:
                    break;
            }
        }
    }
}
        </Document>
    </Project>
</Workspace>");
        }

        [Fact]
        public async Task UseRecursivePatterns_FixAllInSolution()
        {
            await TestInRegularAndScriptAsync(@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
namespace NS
{
    class C : B
    {
        void M1()
        {
            _ = this switch
            {
                { a: var x } {|FixAllInSolution:|}when x is { b: n } => 0
            };

            switch (this)
            {
                case { a: var x } when x is { b: n }:
                    break;
            }
        }
    }
}
        </Document>
        <Document>
namespace NS
{
    class D : C
    {
        void M2()
        {
            _ = this switch
            {
                { a: var x } when x is { b: n } => 0
            };

            switch (this)
            {
                case { a: var x } when x is { b: n }:
                    break;
            }
        }
    }
}
        </Document>
        <Document>
namespace NS
{
    class B
    {
        public const C n = null;
        public C a, b, c, d;
        public int P1, P2, P3;
        public bool B1, B2;
        public C CP1, CP2;
        public static C SCP1, SCP2;
        public static int SP1, SP2;
        public C m() { return null; }
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
namespace NS
{
    class E : C
    {
        void M3()
        {
            _ = this switch
            {
                { a: var x } when x is { b: n } => 0
            };

            switch (this)
            {
                case { a: var x } when x is { b: n }:
                    break;
            }
        }
    }
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
namespace NS
{
    class C : B
    {
        void M1()
        {
            _ = this switch
            {
                { a: { b: n } x } => 0
            };

            switch (this)
            {
                case { a: { b: n } x }:
                    break;
            }
        }
    }
}
        </Document>
        <Document>
namespace NS
{
    class D : C
    {
        void M2()
        {
            _ = this switch
            {
                { a: { b: n } x } => 0
            };

            switch (this)
            {
                case { a: { b: n } x }:
                    break;
            }
        }
    }
}
        </Document>
        <Document>
namespace NS
{
    class B
    {
        public const C n = null;
        public C a, b, c, d;
        public int P1, P2, P3;
        public bool B1, B2;
        public C CP1, CP2;
        public static C SCP1, SCP2;
        public static int SP1, SP2;
        public C m() { return null; }
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
namespace NS
{
    class E : C
    {
        void M3()
        {
            _ = this switch
            {
                { a: { b: n } x } => 0
            };

            switch (this)
            {
                case { a: { b: n } x }:
                    break;
            }
        }
    }
}
        </Document>
    </Project>
</Workspace>");
        }

        [Fact]
        public async Task UseRecursivePatterns_FixAllInContainingMember()
        {
            await TestInRegularAndScriptAsync(@"
namespace NS
{
    class C : B
    {
        void M1()
        {
            if (n == a.b.c.d {|FixAllInContainingMember:|}&& a.b.c.a == n)
            {
            }

            if (this.P1 < 1 && 2 >= this.P2)
            {
            }

            if (!B1 && B2)
            {
            }
        }
    }

    class D : C
    {
        void M2()
        {
            _ = this switch
            {
                { a: var x } when x is { b: n } => 0
            };

            switch (this)
            {
                case { a: var x } when x is { b: n }:
                    break;
            }
        }
    }

    class B
    {
        public const C n = null;
        public C a, b, c, d;
        public int P1, P2, P3;
        public bool B1, B2;
        public C CP1, CP2;
        public static C SCP1, SCP2;
        public static int SP1, SP2;
        public C m() { return null; }
    }
}", @"
namespace NS
{
    class C : B
    {
        void M1()
        {
            if (a.b.c is { d: n, a: n })
            {
            }

            if (this is { P1: < 1, P2: <= 2 })
            {
            }

            if (this is { B1: false, B2: true })
            {
            }
        }
    }

    class D : C
    {
        void M2()
        {
            _ = this switch
            {
                { a: var x } when x is { b: n } => 0
            };

            switch (this)
            {
                case { a: var x } when x is { b: n }:
                    break;
            }
        }
    }

    class B
    {
        public const C n = null;
        public C a, b, c, d;
        public int P1, P2, P3;
        public bool B1, B2;
        public C CP1, CP2;
        public static C SCP1, SCP2;
        public static int SP1, SP2;
        public C m() { return null; }
    }
}");
        }

        [Fact]
        public async Task UseRecursivePatterns_FixAllInContainingType()
        {
            await TestInRegularAndScriptAsync(@"
namespace NS
{
    class C : B
    {
        void M1()
        {
            if (n == a.b.c.d {|FixAllInContainingType:|}&& a.b.c.a == n)
            {
            }
        }

        void M2()
        {
            if (this.P1 < 1 && 2 >= this.P2)
            {
            }

            if (!B1 && B2)
            {
            }
        }
    }

    class D : C
    {
        void M3()
        {
            _ = this switch
            {
                { a: var x } when x is { b: n } => 0
            };

            switch (this)
            {
                case { a: var x } when x is { b: n }:
                    break;
            }
        }
    }

    class B
    {
        public const C n = null;
        public C a, b, c, d;
        public int P1, P2, P3;
        public bool B1, B2;
        public C CP1, CP2;
        public static C SCP1, SCP2;
        public static int SP1, SP2;
        public C m() { return null; }
    }
}", @"
namespace NS
{
    class C : B
    {
        void M1()
        {
            if (a.b.c is { d: n, a: n })
            {
            }
        }

        void M2()
        {
            if (this is { P1: < 1, P2: <= 2 })
            {
            }

            if (this is { B1: false, B2: true })
            {
            }
        }
    }

    class D : C
    {
        void M3()
        {
            _ = this switch
            {
                { a: var x } when x is { b: n } => 0
            };

            switch (this)
            {
                case { a: var x } when x is { b: n }:
                    break;
            }
        }
    }

    class B
    {
        public const C n = null;
        public C a, b, c, d;
        public int P1, P2, P3;
        public bool B1, B2;
        public C CP1, CP2;
        public static C SCP1, SCP2;
        public static int SP1, SP2;
        public C m() { return null; }
    }
}");
        }
    }
}
