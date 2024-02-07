// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.Semantics
{
    public class GlobalUsingDirectiveTests : CompilingTestBase
    {
        [Fact]
        public void MixingUsings_01()
        {
            var source = @"
#pragma warning disable CS8019 // Unnecessary using directive.

global using ns1;
using ns2;
global using ns3;
using ns4;

namespace ns1 {}
namespace ns2 {}
namespace ns3 {}
namespace ns4 {}
";
            CreateCompilation(source, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (4,1): error CS8773: Feature 'global using directive' is not available in C# 9.0. Please use language version 10.0 or greater.
                // global using ns1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "global").WithArguments("global using directive", "10.0").WithLocation(4, 1),
                // (6,1): error CS8915: A global using directive must precede all non-global using directives.
                // global using ns3;
                Diagnostic(ErrorCode.ERR_GlobalUsingOutOfOrder, "global").WithLocation(6, 1),
                // (6,1): error CS8773: Feature 'global using directive' is not available in C# 9.0. Please use language version 10.0 or greater.
                // global using ns3;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "global").WithArguments("global using directive", "10.0").WithLocation(6, 1));

            CreateCompilation(source, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (6,1): error CS8915: A global using directive must precede all non-global using directives.
                // global using ns3;
                Diagnostic(ErrorCode.ERR_GlobalUsingOutOfOrder, "global").WithLocation(6, 1)
                );
        }

        [Fact]
        public void MixingUsings_02()
        {
            var source = @"
#pragma warning disable CS8019 // Unnecessary using directive.

global using ns1;
global using ns3;

namespace ns1 {}
namespace ns3 {}
";
            CreateCompilation(source, parseOptions: TestOptions.Regular10).VerifyDiagnostics();
        }

        [Fact]
        public void MixingUsings_03()
        {
            var source = @"
#pragma warning disable CS8019 // Unnecessary using directive.

global using ns1;
global using ns3;

using ns4;
using ns2;

namespace ns1 {}
namespace ns2 {}
namespace ns3 {}
namespace ns4 {}
";
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
        }

        [Fact]
        public void InNamespace_01()
        {
            var source = @"
#pragma warning disable CS8019 // Unnecessary using directive.

namespace ns
{
    global using ns1;
    using ns2;
    global using ns3;
    using ns4;

    namespace ns1 {}
    namespace ns2 {}
    namespace ns3 {}
    namespace ns4 {}
}
";
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (6,5): error CS8914: A global using directive cannot be used in a namespace declaration.
                //     global using ns1;
                Diagnostic(ErrorCode.ERR_GlobalUsingInNamespace, "global").WithLocation(6, 5)
                );
        }

        [Fact]
        public void InNamespace_02()
        {
            var source = @"
#pragma warning disable CS8019 // Unnecessary using directive.

namespace ns.ns.ns
{
    global using ns1;
    using ns2;
    global using ns3;
    using ns4;

    namespace ns1 {}
    namespace ns2 {}
    namespace ns3 {}
    namespace ns4 {}
}
";
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (6,5): error CS8914: A global using directive cannot be used in a namespace declaration.
                //     global using ns1;
                Diagnostic(ErrorCode.ERR_GlobalUsingInNamespace, "global").WithLocation(6, 5)
                );
        }

        [Fact]
        public void ExternAliasScope_01()
        {
            var source1 = @"
public class C1
{
    public class C2 {}
}

namespace NS3
{
    public class C4 {}
}

public class C5
{
    public class C6 {}
}

namespace NS7
{
    public class C8 {}
}
";

            var comp1 = CreateCompilation(source1);
            var comp1Ref = comp1.ToMetadataReference().WithAliases(new[] { "alias1" });

            var source2 = @"
extern alias alias1;

global using A = alias1::C1;
global using B = alias1::NS3;
global using static alias1::C1;
global using alias1::NS3;

class Program
{
    static void Main()
    {
        System.Console.WriteLine(new A());
        System.Console.WriteLine(new C2());
        System.Console.WriteLine(new B.C4());
        System.Console.WriteLine(new C4());

        System.Console.WriteLine(new alias1::C1());
        System.Console.WriteLine(new alias1::NS3.C4());
    }
}
";
            var comp2 = CreateCompilation(source2, parseOptions: TestOptions.Regular10, options: TestOptions.DebugExe, references: new[] { comp1Ref });

            CompileAndVerify(comp2, expectedOutput: @"
C1
C1+C2
NS3.C4
NS3.C4
C1
NS3.C4
").VerifyDiagnostics();

            var source3 = @"
extern alias alias1;

global using A = alias1::C1;
global using B = alias1::NS3;
global using static alias1::C1;
global using alias1::NS3;

using C = alias1::C5;
using D = alias1::NS7;
using static alias1::C5;
using alias1::NS7;

class Program
{
    static void Main()
    {
        System.Console.WriteLine(new A());
        System.Console.WriteLine(new C2());
        System.Console.WriteLine(new B.C4());
        System.Console.WriteLine(new C4());

        System.Console.WriteLine(new alias1::C1());
        System.Console.WriteLine(new alias1::NS3.C4());

        System.Console.WriteLine(new C());
        System.Console.WriteLine(new C6());
        System.Console.WriteLine(new D.C8());
        System.Console.WriteLine(new C8());
    }
}
";
            var comp3 = CreateCompilation(source3, parseOptions: TestOptions.Regular10, options: TestOptions.DebugExe, references: new[] { comp1Ref });

            CompileAndVerify(comp3, expectedOutput: @"
C1
C1+C2
NS3.C4
NS3.C4
C1
NS3.C4
C5
C5+C6
NS7.C8
NS7.C8
").VerifyDiagnostics();
        }

        [Fact]
        public void ExternAliasScope_02()
        {
            var source1 = @"
public class C1
{
    public class C2 {}
}

namespace NS3
{
    public class C4 {}
}

public class C5
{
    public class C6 {}
}

namespace NS7
{
    public class C8 {}
}
";

            var comp1 = CreateCompilation(source1);
            var comp1Ref = comp1.ToMetadataReference().WithAliases(new[] { "alias1" });

            var source2 = @"
extern alias alias1;

global using A = alias1::C1;
global using B = alias1::NS3;
global using static alias1::C1;
global using alias1::NS3;

Program.Test();
System.Console.WriteLine(new alias1::C1());
System.Console.WriteLine(new alias1::NS3.C4());

partial class Program
{
    public static void Test()
    {
        System.Console.WriteLine(new A());
        System.Console.WriteLine(new C2());
        System.Console.WriteLine(new B.C4());
        System.Console.WriteLine(new C4());

        System.Console.WriteLine(new alias1::C1());
        System.Console.WriteLine(new alias1::NS3.C4());
    }
}
";
            var comp2 = CreateCompilation(source2, parseOptions: TestOptions.Regular10, options: TestOptions.DebugExe, references: new[] { comp1Ref });

            CompileAndVerify(comp2, expectedOutput: @"
C1
C1+C2
NS3.C4
NS3.C4
C1
NS3.C4
C1
NS3.C4
").VerifyDiagnostics();

            var source3 = @"
extern alias alias1;

global using A = alias1::C1;
global using B = alias1::NS3;
global using static alias1::C1;
global using alias1::NS3;

using C = alias1::C5;
using D = alias1::NS7;
using static alias1::C5;
using alias1::NS7;

Program.Test();
System.Console.WriteLine(new alias1::C1());
System.Console.WriteLine(new alias1::NS3.C4());

partial class Program
{
    public static void Test()
    {
        System.Console.WriteLine(new A());
        System.Console.WriteLine(new C2());
        System.Console.WriteLine(new B.C4());
        System.Console.WriteLine(new C4());

        System.Console.WriteLine(new alias1::C1());
        System.Console.WriteLine(new alias1::NS3.C4());

        System.Console.WriteLine(new C());
        System.Console.WriteLine(new C6());
        System.Console.WriteLine(new D.C8());
        System.Console.WriteLine(new C8());
    }
}
";
            var comp3 = CreateCompilation(source3, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe, references: new[] { comp1Ref });

            CompileAndVerify(comp3, expectedOutput: @"
C1
C1+C2
NS3.C4
NS3.C4
C1
NS3.C4
C5
C5+C6
NS7.C8
NS7.C8
C1
NS3.C4
").VerifyDiagnostics();
        }

        [Fact]
        public void ExternAliasScope_03()
        {
            var source1 = @"
public class C1
{
}

namespace NS3
{
    public class C4 {}
}
";

            var comp1 = CreateCompilation(source1);
            var comp1Ref = comp1.ToMetadataReference().WithAliases(new[] { "alias1" });

            var source2 = @"
extern alias alias1;
";
            var source3 = @"
global using A = alias1::C1;
global using B = alias1::NS3;

partial class Program
{
    static void Main()
    {
        System.Console.WriteLine(new A());
        System.Console.WriteLine(new B.C4());
    }
}
";
            var comp2 = CreateCompilation(new[] { source2, source3 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe, references: new[] { comp1Ref });

            var expected = new[]
            {
                // (2,1): hidden CS8020: Unused extern alias.
                // extern alias alias1;
                Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias alias1;").WithLocation(2, 1),
                // (2,18): error CS0432: Alias 'alias1' not found
                // global using A = alias1::C1;
                Diagnostic(ErrorCode.ERR_AliasNotFound, "alias1").WithArguments("alias1").WithLocation(2, 18),
                // (3,18): error CS0432: Alias 'alias1' not found
                // global using B = alias1::NS3;
                Diagnostic(ErrorCode.ERR_AliasNotFound, "alias1").WithArguments("alias1").WithLocation(3, 18)
            };

            comp2.VerifyDiagnostics(expected);

            var comp3 = CreateCompilation(new[] { source3, source2 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe, references: new[] { comp1Ref });

            comp2.VerifyDiagnostics(expected);
        }

        [Fact]
        public void GlobalUsingAliasScope_01()
        {
            var ext = @"
public class Extern
{
}
";
            var extComp = CreateCompilation(ext);
            var extCompRef = extComp.ToMetadataReference().WithAliases(new[] { "ext" });

            var extAlias = @"
extern alias ext;
";

            var source1 = @"
public class C1
{
    public class C2 {}
}

namespace NS3
{
    public class C4
    {
        public class C5
        {
        }
    }

    namespace NS7
    {
        public class C8 {}
    }
}
";

            var globalUsings1 = @"
global using A = C1;
";

            var globalUsings2 = @"
global using B = NS3;
";

            var source2 = @"
class Program
{
    static void Main()
    {
        System.Console.WriteLine(new A());
        System.Console.WriteLine(new B.C4());
    }
}
";

            test(source2, expectedOutput: @"
C1
NS3.C4
");

            var source4 = @"
namespace NS
{
    class Program
    {
        static void Main()
        {
            System.Console.WriteLine(new A());
            System.Console.WriteLine(new B.C4());
        }
    }
}
";

            test(source4, expectedOutput: @"
C1
NS3.C4
");

            var source5 = @"
namespace NS
{
    using C = A.C2;
    using D = B::NS7;
    using static B::C4;
    using B.NS7;

    class Program
    {
        static void Main()
        {
            System.Console.WriteLine(new A());
            System.Console.WriteLine(new B.C4());

            System.Console.WriteLine(new C());
            System.Console.WriteLine(new C5());
            System.Console.WriteLine(new D.C8());
            System.Console.WriteLine(new C8());
        }
    }
}
";

            test(source5, expectedOutput: @"
C1
NS3.C4
C1+C2
NS3.C4+C5
NS3.NS7.C8
NS3.NS7.C8
");

            void test(string source, string expectedOutput)
            {
                var comp = CreateCompilation(new[] { globalUsings1 + globalUsings2 + source, source1 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

                comp = CreateCompilation(new[] { globalUsings2 + source, globalUsings1 + source1 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

                comp = CreateCompilation(new[] { globalUsings1 + source1, globalUsings2 + source }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

                comp = CreateCompilation(new[] { source, globalUsings1 + globalUsings2 + source1 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

                comp = CreateCompilation(new[] { globalUsings1 + globalUsings2 + source1, source }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

                comp = CreateCompilation(new[] { extAlias + source, globalUsings1 + globalUsings2 + source1 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe, references: new[] { extCompRef });
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics(
                    // (2,1): hidden CS8020: Unused extern alias.
                    // extern alias ext;
                    Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias ext;").WithLocation(2, 1)
                    );

                comp = CreateCompilation(new[] { globalUsings1 + globalUsings2 + source1, extAlias + source }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe, references: new[] { extCompRef });
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics(
                    // (2,1): hidden CS8020: Unused extern alias.
                    // extern alias ext;
                    Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias ext;").WithLocation(2, 1)
                    );
            }
        }

        [Fact]
        public void GlobalUsingAliasScope_02()
        {
            var ext = @"
public class Extern
{
}
";
            var extComp = CreateCompilation(ext);
            var extCompRef = extComp.ToMetadataReference().WithAliases(new[] { "ext" });

            var extAlias = @"
extern alias ext;
";

            var source1 = @"
public class C1
{
    public class C2 {}
}

namespace NS3
{
    public class C4
    {
        public class C5
        {
        }
    }

    namespace NS7
    {
        public class C8 {}
    }
}
";

            var globalUsings1 = @"
global using A = C1;
";

            var globalUsings2 = @"
global using B = NS3;
";

            var source2 = @"
Program.Test();
System.Console.WriteLine(new A());
System.Console.WriteLine(new B.C4());

partial class Program
{
    public static void Test()
    {
        System.Console.WriteLine(new A());
        System.Console.WriteLine(new B.C4());
    }
}
";

            test(source2, expectedOutput: @"
C1
NS3.C4
C1
NS3.C4
");

            var source3 = @"
N.Program.Test();
System.Console.WriteLine(new A());
System.Console.WriteLine(new B.C4());

namespace N
{
    using C = A.C2;
    using D = B::NS7;
    using static B::C4;
    using B.NS7;

    class Program
    {
        public static void Test()
        {
            System.Console.WriteLine(new A());
            System.Console.WriteLine(new B.C4());

            System.Console.WriteLine(new C());
            System.Console.WriteLine(new C5());
            System.Console.WriteLine(new D.C8());
            System.Console.WriteLine(new C8());
        }
    }
}
";

            test(source3, expectedOutput: @"
C1
NS3.C4
C1+C2
NS3.C4+C5
NS3.NS7.C8
NS3.NS7.C8
C1
NS3.C4
");

            void test(string source, string expectedOutput)
            {
                var comp = CreateCompilation(new[] { globalUsings1 + globalUsings2 + source, source1 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

                comp = CreateCompilation(new[] { globalUsings2 + source, globalUsings1 + source1 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

                comp = CreateCompilation(new[] { globalUsings1 + source1, globalUsings2 + source }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

                comp = CreateCompilation(new[] { source, globalUsings1 + globalUsings2 + source1 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

                comp = CreateCompilation(new[] { globalUsings1 + globalUsings2 + source1, source }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

                comp = CreateCompilation(new[] { extAlias + source, globalUsings1 + globalUsings2 + source1 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe, references: new[] { extCompRef });
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics(
                    // (2,1): hidden CS8020: Unused extern alias.
                    // extern alias ext;
                    Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias ext;").WithLocation(2, 1)
                    );

                comp = CreateCompilation(new[] { globalUsings1 + globalUsings2 + source1, extAlias + source }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe, references: new[] { extCompRef });
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics(
                    // (2,1): hidden CS8020: Unused extern alias.
                    // extern alias ext;
                    Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias ext;").WithLocation(2, 1)
                    );
            }
        }

        [Fact]
        public void GlobalUsingAliasScope_03()
        {
            var source1 = @"
public class C1
{
    public class C2 {}
}

namespace NS3
{
    public class C4
    {
        public class C5
        {
        }
    }

    namespace NS7
    {
        public class C8 {}
    }
}
";

            var globalUsings1 = @"
global using A = C1;
";

            var globalUsings2 = @"
global using B = NS3;
";

            var source2 = @"
Program.Test();
System.Console.WriteLine(new A());
System.Console.WriteLine(new B.C4());
";

            test(source2,
                 @"
partial class Program
{
    public static void Test()
    {
        System.Console.WriteLine(new A());
        System.Console.WriteLine(new B.C4());
    }
}
",
                 expectedOutput: @"
C1
NS3.C4
C1
NS3.C4
");

            var source3 = @"
N.Program.Test();
System.Console.WriteLine(new A());
System.Console.WriteLine(new B.C4());
";

            test(source3,
                 @"
namespace N
{
    using C = A.C2;
    using D = B.NS7;
    using static B.C4;
    using B::NS7;

    class Program
    {
        public static void Test()
        {
            System.Console.WriteLine(new A());
            System.Console.WriteLine(new B.C4());

            System.Console.WriteLine(new C());
            System.Console.WriteLine(new C5());
            System.Console.WriteLine(new D.C8());
            System.Console.WriteLine(new C8());
        }
    }
}
",
                 expectedOutput: @"
C1
NS3.C4
C1+C2
NS3.C4+C5
NS3.NS7.C8
NS3.NS7.C8
C1
NS3.C4
");

            void test(string source, string program, string expectedOutput)
            {
                var comp = CreateCompilation(new[] { globalUsings1 + globalUsings2 + source, source1, program }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

                comp = CreateCompilation(new[] { globalUsings2 + source, globalUsings1 + source1, program }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

                comp = CreateCompilation(new[] { globalUsings1 + source1, globalUsings2 + source, program }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

                comp = CreateCompilation(new[] { source, globalUsings1 + globalUsings2 + source1, program }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

                comp = CreateCompilation(new[] { globalUsings1 + globalUsings2 + source1, source, program }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
            }
        }

        [Fact]
        public void GlobalUsingAliasScope_04()
        {
            var source1 = @"
public class C1
{
    public class C2 {}
}

namespace NS3
{
    public class C4
    {
        public class C5
        {
        }
    }

    namespace NS7
    {
        public class C8 {}
    }
}
";

            var globalUsings1 = @"
global using A = C1;
global using B = NS3;
";

            var globalUsings2 = @"
#line 1000
global using C = A.C2;
#line 2000
global using D = B.NS7;
#line 3000
global using static B.C4;
#line 4000
global using B.NS7;
";

            var comp = CreateCompilation(new[] { globalUsings1 + globalUsings2, source1 }, parseOptions: TestOptions.RegularPreview);

            var expected = new DiagnosticDescription[]
            {
                // (1000,18): error CS0246: The type or namespace name 'A' could not be found (are you missing a using directive or an assembly reference?)
                // global using C = A.C2;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A").WithArguments("A").WithLocation(1000, 18),
                // (2000,18): error CS0246: The type or namespace name 'B' could not be found (are you missing a using directive or an assembly reference?)
                // global using D = B.NS7;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "B").WithArguments("B").WithLocation(2000, 18),
                // (3000,21): error CS0246: The type or namespace name 'B' could not be found (are you missing a using directive or an assembly reference?)
                // global using static B.C4;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "B").WithArguments("B").WithLocation(3000, 21),
                // (4000,14): error CS0246: The type or namespace name 'B' could not be found (are you missing a using directive or an assembly reference?)
                // global using B.NS7;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "B").WithArguments("B").WithLocation(4000, 14)
            };

            comp.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected);

            comp = CreateCompilation(new[] { globalUsings2, globalUsings1 + source1 }, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected);

            comp = CreateCompilation(new[] { globalUsings1 + source1, globalUsings2 }, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected);

            comp = CreateCompilation(new[] { globalUsings2 + globalUsings1, source1 }, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected);
        }

        [Fact]
        public void GlobalUsingAliasScope_05()
        {
            var source1 = @"
public class C1
{
    public class C2 {}
}

namespace NS3
{
    public class C4
    {
        public class C5
        {
        }
    }

    namespace NS7
    {
        public class C8 {}
    }
}
";

            var globalUsings1 = @"
global using A = C1;
global using B = NS3;
";

            var usings2 = @"
#line 1000
using C = A.C2;
#line 2000
using D = B.NS7;
#line 3000
using static B.C4;
#line 4000
using B.NS7;
";

            var comp = CreateCompilation(new[] { globalUsings1 + usings2, source1 }, parseOptions: TestOptions.RegularPreview);

            var expected = new DiagnosticDescription[]
            {
                // (1000,11): error CS0246: The type or namespace name 'A' could not be found (are you missing a using directive or an assembly reference?)
                // using C = A.C2;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A").WithArguments("A").WithLocation(1000, 11),
                // (2000,11): error CS0246: The type or namespace name 'B' could not be found (are you missing a using directive or an assembly reference?)
                // using D = B.NS7;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "B").WithArguments("B").WithLocation(2000, 11),
                // (3000,14): error CS0246: The type or namespace name 'B' could not be found (are you missing a using directive or an assembly reference?)
                // using static B.C4;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "B").WithArguments("B").WithLocation(3000, 14),
                // (4000,7): error CS0246: The type or namespace name 'B' could not be found (are you missing a using directive or an assembly reference?)
                // using B.NS7;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "B").WithArguments("B").WithLocation(4000, 7)
            };

            comp.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected);

            comp = CreateCompilation(new[] { usings2, globalUsings1 + source1 }, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected);

            comp = CreateCompilation(new[] { globalUsings1 + source1, usings2 }, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected);
        }

        [Fact]
        public void GlobalUsingAliasScope_06()
        {
            var source1 = @"
global using A = C1;

Program1.Test();
NS1.Program2.Test();
NS2.Program3.Test();

class Program1
{
    public static void Test()
    {
        System.Console.WriteLine(new A());
    }
}

namespace NS1
{
    using A = C2;

    class Program2
    {
        public static void Test()
        {
            System.Console.WriteLine(new A());
        }
    }
}

namespace NS2
{
    using NS3;

    class Program3
    {
        public static void Test()
        {
            System.Console.WriteLine(new A());
        }
    }

    namespace NS3
    {
        class A {}
    }
}

public class C1 {}
public class C2 {}
";

            var comp = CreateCompilation(source1, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: @"
C1
C2
NS2.NS3.A
").VerifyDiagnostics();
        }

        [Fact]
        public void UsingAliasScope_01()
        {
            var source1 = @"
global using C = A.C2;
global using D = B::NS7;
global using static B::C4;
global using B.NS7;

using A = C1;
using B = NS3;

public class C1
{
    public class C2 {}
}

namespace NS3
{
    public class C4
    {
        public class C5
        {
        }
    }

    namespace NS7
    {
        public class C8 {}
    }
}
";

            var comp = CreateCompilation(source1, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(
                // (2,18): error CS0246: The type or namespace name 'A' could not be found (are you missing a using directive or an assembly reference?)
                // global using C = A.C2;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A").WithArguments("A").WithLocation(2, 18),
                // (3,18): error CS0432: Alias 'B' not found
                // global using D = B::NS7;
                Diagnostic(ErrorCode.ERR_AliasNotFound, "B").WithArguments("B").WithLocation(3, 18),
                // (4,21): error CS0432: Alias 'B' not found
                // global using static B::C4;
                Diagnostic(ErrorCode.ERR_AliasNotFound, "B").WithArguments("B").WithLocation(4, 21),
                // (5,14): error CS0246: The type or namespace name 'B' could not be found (are you missing a using directive or an assembly reference?)
                // global using B.NS7;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "B").WithArguments("B").WithLocation(5, 14)
                );
        }

        [Fact]
        public void UsingAliasScope_02()
        {
            var source1 = @"
using A = C1;
using B = NS3;

global using C = A.C2;
global using D = B::NS7;
global using static B::C4;
global using B.NS7;

public class C1
{
    public class C2 {}
}

namespace NS3
{
    public class C4
    {
        public class C5
        {
        }
    }

    namespace NS7
    {
        public class C8 {}
    }
}
";

            var comp = CreateCompilation(source1, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(
                // (5,1): error CS8915: A global using directive must precede all non-global using directives.
                // global using C = A.C2;
                Diagnostic(ErrorCode.ERR_GlobalUsingOutOfOrder, "global").WithLocation(5, 1),
                // (5,18): error CS0246: The type or namespace name 'A' could not be found (are you missing a using directive or an assembly reference?)
                // global using C = A.C2;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A").WithArguments("A").WithLocation(5, 18),
                // (6,18): error CS0432: Alias 'B' not found
                // global using D = B::NS7;
                Diagnostic(ErrorCode.ERR_AliasNotFound, "B").WithArguments("B").WithLocation(6, 18),
                // (7,21): error CS0432: Alias 'B' not found
                // global using static B::C4;
                Diagnostic(ErrorCode.ERR_AliasNotFound, "B").WithArguments("B").WithLocation(7, 21),
                // (8,14): error CS0246: The type or namespace name 'B' could not be found (are you missing a using directive or an assembly reference?)
                // global using B.NS7;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "B").WithArguments("B").WithLocation(8, 14)
                );
        }

        [Fact]
        public void GlobalUsingNamespaceOrTypeScope_01()
        {
            var ext = @"
public class Extern
{
}
";
            var extComp = CreateCompilation(ext);
            var extCompRef = extComp.ToMetadataReference().WithAliases(new[] { "ext" });

            var extAlias = @"
extern alias ext;
";

            var source1 = @"
public class C1
{
    public class C2 {}
}

namespace NS3
{
    public class C4
    {
        public class C5
        {
        }
    }
}
";

            var globalUsings1 = @"
global using static C1;
";

            var globalUsings2 = @"
global using NS3;
";

            var source2 = @"
class Program
{
    static void Main()
    {
        System.Console.WriteLine(new C2());
        System.Console.WriteLine(new C4());
    }
}
";

            test(source2, expectedOutput: @"
C1+C2
NS3.C4
");

            var source4 = @"
namespace NS
{
    class Program
    {
        static void Main()
        {
            System.Console.WriteLine(new C2());
            System.Console.WriteLine(new C4());
        }
    }
}
";

            test(source4, expectedOutput: @"
C1+C2
NS3.C4
");

            var source5 = @"
namespace NS
{
    using C = C2;
    using static C4;

    class Program
    {
        static void Main()
        {
            System.Console.WriteLine(new C2());
            System.Console.WriteLine(new C4());

            System.Console.WriteLine(new C());
            System.Console.WriteLine(new C5());
        }
    }
}
";

            test(source5, expectedOutput: @"
C1+C2
NS3.C4
C1+C2
NS3.C4+C5
");

            void test(string source, string expectedOutput)
            {
                var comp = CreateCompilation(new[] { globalUsings1 + globalUsings2 + source, source1 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

                comp = CreateCompilation(new[] { globalUsings2 + source, globalUsings1 + source1 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

                comp = CreateCompilation(new[] { globalUsings1 + source1, globalUsings2 + source }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

                comp = CreateCompilation(new[] { source, globalUsings1 + globalUsings2 + source1 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

                comp = CreateCompilation(new[] { globalUsings1 + globalUsings2 + source1, source }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

                comp = CreateCompilation(new[] { extAlias + source, globalUsings1 + globalUsings2 + source1 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe, references: new[] { extCompRef });
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics(
                    // (2,1): hidden CS8020: Unused extern alias.
                    // extern alias ext;
                    Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias ext;").WithLocation(2, 1)
                    );

                comp = CreateCompilation(new[] { globalUsings1 + globalUsings2 + source1, extAlias + source }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe, references: new[] { extCompRef });
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics(
                    // (2,1): hidden CS8020: Unused extern alias.
                    // extern alias ext;
                    Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias ext;").WithLocation(2, 1)
                    );
            }
        }

        [Fact]
        public void GlobalUsingNamespaceOrTypeScope_02()
        {
            var ext = @"
public class Extern
{
}
";
            var extComp = CreateCompilation(ext);
            var extCompRef = extComp.ToMetadataReference().WithAliases(new[] { "ext" });

            var extAlias = @"
extern alias ext;
";

            var source1 = @"
public class C1
{
    public class C2 {}
}

namespace NS3
{
    public class C4
    {
        public class C5
        {
        }
    }
}
";

            var globalUsings1 = @"
global using static C1;
";

            var globalUsings2 = @"
global using NS3;
";

            var source2 = @"
Program.Test();
System.Console.WriteLine(new C2());
System.Console.WriteLine(new C4());

partial class Program
{
    public static void Test()
    {
        System.Console.WriteLine(new C2());
        System.Console.WriteLine(new C4());
    }
}
";

            test(source2, expectedOutput: @"
C1+C2
NS3.C4
C1+C2
NS3.C4
");

            var source3 = @"
N.Program.Test();
System.Console.WriteLine(new C2());
System.Console.WriteLine(new C4());

namespace N
{
    using C = C2;
    using static C4;

    class Program
    {
        public static void Test()
        {
            System.Console.WriteLine(new C2());
            System.Console.WriteLine(new C4());

            System.Console.WriteLine(new C());
            System.Console.WriteLine(new C5());
        }
    }
}
";

            test(source3, expectedOutput: @"
C1+C2
NS3.C4
C1+C2
NS3.C4+C5
C1+C2
NS3.C4
");

            void test(string source, string expectedOutput)
            {
                var comp = CreateCompilation(new[] { globalUsings1 + globalUsings2 + source, source1 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

                comp = CreateCompilation(new[] { globalUsings2 + source, globalUsings1 + source1 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

                comp = CreateCompilation(new[] { globalUsings1 + source1, globalUsings2 + source }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

                comp = CreateCompilation(new[] { source, globalUsings1 + globalUsings2 + source1 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

                comp = CreateCompilation(new[] { globalUsings1 + globalUsings2 + source1, source }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

                comp = CreateCompilation(new[] { extAlias + source, globalUsings1 + globalUsings2 + source1 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe, references: new[] { extCompRef });
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics(
                    // (2,1): hidden CS8020: Unused extern alias.
                    // extern alias ext;
                    Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias ext;").WithLocation(2, 1)
                    );

                comp = CreateCompilation(new[] { globalUsings1 + globalUsings2 + source1, extAlias + source }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe, references: new[] { extCompRef });
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics(
                    // (2,1): hidden CS8020: Unused extern alias.
                    // extern alias ext;
                    Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias ext;").WithLocation(2, 1)
                    );
            }
        }

        [Fact]
        public void GlobalUsingNamespaceOrTypeScope_03()
        {
            var source1 = @"
public class C1
{
    public class C2 {}
}

namespace NS3
{
    public class C4
    {
        public class C5
        {
        }
    }
}
";

            var globalUsings1 = @"
global using static C1;
";

            var globalUsings2 = @"
global using NS3;
";

            var source2 = @"
Program.Test();
System.Console.WriteLine(new C2());
System.Console.WriteLine(new C4());
";

            test(source2,
                 @"
partial class Program
{
    public static void Test()
    {
        System.Console.WriteLine(new C2());
        System.Console.WriteLine(new C4());
    }
}
",
                 expectedOutput: @"
C1+C2
NS3.C4
C1+C2
NS3.C4
");

            var source3 = @"
N.Program.Test();
System.Console.WriteLine(new C2());
System.Console.WriteLine(new C4());
";

            test(source3,
                 @"
namespace N
{
    using C = C2;
    using static C4;

    class Program
    {
        public static void Test()
        {
            System.Console.WriteLine(new C2());
            System.Console.WriteLine(new C4());

            System.Console.WriteLine(new C());
            System.Console.WriteLine(new C5());
        }
    }
}
",
                 expectedOutput: @"
C1+C2
NS3.C4
C1+C2
NS3.C4+C5
C1+C2
NS3.C4
");

            void test(string source, string program, string expectedOutput)
            {
                var comp = CreateCompilation(new[] { globalUsings1 + globalUsings2 + source, source1, program }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

                comp = CreateCompilation(new[] { globalUsings2 + source, globalUsings1 + source1, program }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

                comp = CreateCompilation(new[] { globalUsings1 + source1, globalUsings2 + source, program }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

                comp = CreateCompilation(new[] { source, globalUsings1 + globalUsings2 + source1, program }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

                comp = CreateCompilation(new[] { globalUsings1 + globalUsings2 + source1, source, program }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
            }
        }

        [Fact]
        public void GlobalUsingNamespaceOrTypeScope_04()
        {
            var source1 = @"
public class C1
{
    public class C2 {}
}

namespace NS3
{
    public class C4
    {
        public class C5
        {
        }
    }
}
";

            var globalUsings1 = @"
global using static C1;
global using NS3;
";

            var globalUsings2 = @"
global using C = C2;
global using static C4;
";

            var comp = CreateCompilation(new[] { globalUsings1 + globalUsings2, source1 }, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(
                // (5,18): error CS0246: The type or namespace name 'C2' could not be found (are you missing a using directive or an assembly reference?)
                // global using C = C2;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C2").WithArguments("C2").WithLocation(5, 18),
                // (6,21): error CS0246: The type or namespace name 'C4' could not be found (are you missing a using directive or an assembly reference?)
                // global using static C4;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C4").WithArguments("C4").WithLocation(6, 21)
                );

            var expected = new DiagnosticDescription[]
            {
                // (2,18): error CS0246: The type or namespace name 'C2' could not be found (are you missing a using directive or an assembly reference?)
                // global using C = C2;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C2").WithArguments("C2").WithLocation(2, 18),
                // (3,21): error CS0246: The type or namespace name 'C4' could not be found (are you missing a using directive or an assembly reference?)
                // global using static C4;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C4").WithArguments("C4").WithLocation(3, 21)
            };

            comp = CreateCompilation(new[] { globalUsings2, globalUsings1 + source1 }, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected);

            comp = CreateCompilation(new[] { globalUsings1 + source1, globalUsings2 }, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected);

            comp = CreateCompilation(new[] { globalUsings2 + globalUsings1, source1 }, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected);
        }

        [Fact]
        public void GlobalUsingNamespaceOrTypeScope_05()
        {
            var source1 = @"
public class C1
{
    public class C2 {}
}

namespace NS3
{
    public class C4
    {
        public class C5
        {
        }
    }
}
";

            var globalUsings1 = @"
global using static C1;
global using NS3;
";

            var usings2 = @"
using C = C2;
using static C4;
";

            var comp = CreateCompilation(new[] { globalUsings1 + usings2, source1 }, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(
                // (5,11): error CS0246: The type or namespace name 'C2' could not be found (are you missing a using directive or an assembly reference?)
                // using C = C2;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C2").WithArguments("C2").WithLocation(5, 11),
                // (6,14): error CS0246: The type or namespace name 'C4' could not be found (are you missing a using directive or an assembly reference?)
                // using static C4;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C4").WithArguments("C4").WithLocation(6, 14)
                );

            var expected = new DiagnosticDescription[]
            {
                // (2,11): error CS0246: The type or namespace name 'C2' could not be found (are you missing a using directive or an assembly reference?)
                // using C = C2;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C2").WithArguments("C2").WithLocation(2, 11),
                // (3,14): error CS0246: The type or namespace name 'C4' could not be found (are you missing a using directive or an assembly reference?)
                // using static C4;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C4").WithArguments("C4").WithLocation(3, 14)
            };

            comp = CreateCompilation(new[] { usings2, globalUsings1 + source1 }, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected);

            comp = CreateCompilation(new[] { globalUsings1 + source1, usings2 }, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected);
        }

        [Fact]
        public void GlobalUsingNamespaceOrTypeScope_06()
        {
            var source1 = @"
global using static C1;
global using NS4;

Program1.Test();
NS1.Program2.Test();
NS2.Program3.Test();

class Program1
{
    public static void Test()
    {
        System.Console.WriteLine(new C2());
        System.Console.WriteLine(new C3());
    }
}

namespace NS1
{
    using static C4;
    using NS5;

    class Program2
    {
        public static void Test()
        {
            System.Console.WriteLine(new C2());
            System.Console.WriteLine(new C3());
        }
    }
}

namespace NS2
{
    using C2 = NS3.A;

    class Program3
    {
        public static void Test()
        {
            System.Console.WriteLine(new C2());
        }
    }

    namespace NS3
    {
        class A {}
    }
}

public class C1
{
    public class C2 {}
}

namespace NS4
{
    class C3 {}
}

public class C4
{
    public class C2 {}
}

namespace NS5
{
    class C3 {}
}
";

            var comp = CreateCompilation(source1, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: @"
C1+C2
NS4.C3
C4+C2
NS5.C3
NS2.NS3.A
").VerifyDiagnostics();
        }

        [Fact]
        public void GlobalUsingNamespaceOrTypeScope_07()
        {
            var source1 = @"
namespace NS0
{
    public static class C1
    {
        public static void M2(this int x) => System.Console.WriteLine(""NS0.C1.M2 {0}"", x);
    }
}

namespace NS3
{
    public static class C4
    {
        public static void M5(this int x) => System.Console.WriteLine(""NS3.C4.M5 {0}"", x);
    }
}

namespace NS6
{
    public static class C7
    {
        public static void M8(this int x) => System.Console.WriteLine(""NS6.C7.M8 {0}"", x);
    }
}

namespace NS9
{
    public static class C10
    {
        public static void M11(this int x) => System.Console.WriteLine(""NS9.C10.M11 {0}"", x);
    }
}
";

            var globalUsings1 = @"
global using static NS0.C1;
";

            var globalUsings2 = @"
global using NS3;
";

            var source2 = @"
class Program
{
    static void Main()
    {
        1.M2();
        2.M5();
    }
}
";

            test(source2, expectedOutput: @"
NS0.C1.M2 1
NS3.C4.M5 2
");

            var source3 = @"
using NS6;
using static NS9.C10;

class Program
{
    static void Main()
    {
        3.M2();
        4.M5();

        5.M8();
        6.M11();
    }
}
";

            test(source3, expectedOutput: @"
NS0.C1.M2 3
NS3.C4.M5 4
NS6.C7.M8 5
NS9.C10.M11 6
");

            var source4 = @"
namespace NS
{
    class Program
    {
        static void Main()
        {
            1.M2();
            2.M5();
        }
    }
}
";

            test(source4, expectedOutput: @"
NS0.C1.M2 1
NS3.C4.M5 2
");

            void test(string source, string expectedOutput)
            {
                var comp = CreateCompilation(new[] { globalUsings1 + globalUsings2 + source, source1 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

                comp = CreateCompilation(new[] { globalUsings2 + source, globalUsings1 + source1 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

                comp = CreateCompilation(new[] { globalUsings1 + source1, globalUsings2 + source }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

                comp = CreateCompilation(new[] { source, globalUsings1 + globalUsings2 + source1 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

                comp = CreateCompilation(new[] { globalUsings1 + globalUsings2 + source1, source }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
            }
        }

        [Fact]
        public void GlobalUsingNamespaceOrTypeScope_08()
        {
            var source1 = @"
namespace NS0
{
    public static class C1
    {
        public static void M2(this int x) => System.Console.WriteLine(""NS0.C1.M2 {0}"", x);
    }
}

namespace NS3
{
    public static class C4
    {
        public static void M5(this int x) => System.Console.WriteLine(""NS3.C4.M5 {0}"", x);
    }
}

namespace NS6
{
    public static class C7
    {
        public static void M5(this int x) => System.Console.WriteLine(""NS6.C7.M5 {0}"", x);
    }
}

namespace NS9
{
    public static class C10
    {
        public static void M2(this int x) => System.Console.WriteLine(""NS9.C10.M2 {0}"", x);
    }
}
";

            var source2 = @"
global using static NS0.C1;
global using NS3;

Program1.Test();
NS12.Program2.Test();

class Program1
{
    public static void Test()
    {
        1.M2();
        2.M5();
    }
}

namespace NS12
{
    using NS6;
    using static NS9.C10;

    class Program2
    {
        public static void Test()
        {
            3.M2();
            4.M5();
        }
    }
}
";

            var comp = CreateCompilation(new[] { source1, source2 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: @"
NS0.C1.M2 1
NS3.C4.M5 2
NS9.C10.M2 3
NS6.C7.M5 4
").VerifyDiagnostics();
        }

        [Fact]
        public void UsingNamespaceOrTypeScope_01()
        {
            var source1 = @"
global using C = C2;
global using static C4;

using static C1;
using NS3;

public class C1
{
    public class C2 {}
}

namespace NS3
{
    public class C4
    {
        public class C5
        {
        }
    }
}
";
            var comp = CreateCompilation(source1, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(
                // (2,18): error CS0246: The type or namespace name 'C2' could not be found (are you missing a using directive or an assembly reference?)
                // global using C = C2;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C2").WithArguments("C2").WithLocation(2, 18),
                // (3,21): error CS0246: The type or namespace name 'C4' could not be found (are you missing a using directive or an assembly reference?)
                // global using static C4;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C4").WithArguments("C4").WithLocation(3, 21)
                );
        }

        [Fact]
        public void UsingNamespaceOrTypeScope_02()
        {
            var source1 = @"
using static C1;
using NS3;

global using C = C2;
global using static C4;

public class C1
{
    public class C2 {}
}

namespace NS3
{
    public class C4
    {
        public class C5
        {
        }
    }
}
";
            var comp = CreateCompilation(source1, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(
                // (5,1): error CS8915: A global using directive must precede all non-global using directives.
                // global using C = C2;
                Diagnostic(ErrorCode.ERR_GlobalUsingOutOfOrder, "global").WithLocation(5, 1),
                // (5,18): error CS0246: The type or namespace name 'C2' could not be found (are you missing a using directive or an assembly reference?)
                // global using C = C2;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C2").WithArguments("C2").WithLocation(5, 18),
                // (6,21): error CS0246: The type or namespace name 'C4' could not be found (are you missing a using directive or an assembly reference?)
                // global using static C4;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C4").WithArguments("C4").WithLocation(6, 21)
                );
        }

        [Fact]
        public void GlobalUsingMerge_01()
        {
            var source1 = @"
public class C1
{
    public class C2 {};
}

namespace NS3
{
    public class C4 {}
}

namespace NS6
{
    public class C7 {}
}

namespace NS9
{
    public class C10 {}
}
";
            var source2 = @"
global using static C1;
";
            var source3 = @"
global using C4 = NS3.C4;
";
            var source4 = @"
global using NS6;
";
            var source5 = @"
global using C10 = NS9.C10;
";
            var source6 = @"
class Program1
{
    public static void Main()
    {
        System.Console.WriteLine(new C2());
        System.Console.WriteLine(new C4());
        System.Console.WriteLine(new C7());
        System.Console.WriteLine(new C10());
    }
}
";

            var comp = CreateCompilation(new[] { source2, source1, source3, source6, source4, "", source5 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: @"
C1+C2
NS3.C4
NS6.C7
NS9.C10
").VerifyDiagnostics();
        }

        [Fact]
        public void AliasConflictWithExternAlias_01()
        {
            var source1 = @"
public class C1
{
}
";

            var comp1 = CreateCompilation(source1);
            var comp1Ref = comp1.ToMetadataReference().WithAliases(new[] { "alias1", "alias2" });

            var source2 = @"
#line 1000
extern alias alias1;
#line 2000
extern alias alias2;
";
            var source3 = @"
#line 3000
global using alias1 = C2;
";
            var source4 = @"
#line 4000
global using alias1 = C3;
";
            var source5 = @"
#line 5000
using alias1 = C4;
#line 6000
using alias2 = C5;
";
            var source6 = @"
class C2 {}
class C3 {}
class C4 {}
class C5 {}
";
            var comp2 = CreateCompilation(new[] { source2 + source3, source5, source6 }, parseOptions: TestOptions.RegularPreview, references: new[] { comp1Ref });
            comp2.GetDiagnostics().Where(d => d.Code is not ((int)ErrorCode.HDN_UnusedUsingDirective or (int)ErrorCode.HDN_UnusedExternAlias)).Verify(
                // (5000,7): error CS1537: The using alias 'alias1' appeared previously in this namespace
                // using alias1 = C4;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "alias1").WithArguments("alias1").WithLocation(5000, 7),
                // (3000,1): error CS1537: The using alias 'alias1' appeared previously in this namespace
                // global using alias1 = C2;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "global using alias1 = C2;").WithArguments("alias1").WithLocation(3000, 1)
                );

            var comp3 = CreateCompilation(new[] { source2 + source3 + source4, source5, source6 }, parseOptions: TestOptions.RegularPreview, references: new[] { comp1Ref });
            comp3.GetDiagnostics().Where(d => d.Code is not ((int)ErrorCode.HDN_UnusedUsingDirective or (int)ErrorCode.HDN_UnusedExternAlias)).Verify(
                // (5000,7): error CS1537: The using alias 'alias1' appeared previously in this namespace
                // using alias1 = C4;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "alias1").WithArguments("alias1").WithLocation(5000, 7),
                // (3000,1): error CS1537: The using alias 'alias1' appeared previously in this namespace
                // global using alias1 = C2;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "global using alias1 = C2;").WithArguments("alias1").WithLocation(3000, 1),
                // (4000,14): error CS1537: The using alias 'alias1' appeared previously in this namespace
                // global using alias1 = C3;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "alias1").WithArguments("alias1").WithLocation(4000, 14)
                );

            var comp4 = CreateCompilation(new[] { source2 + source3 + source5, source6 }, parseOptions: TestOptions.RegularPreview, references: new[] { comp1Ref });
            comp4.GetDiagnostics().Where(d => d.Code is not ((int)ErrorCode.HDN_UnusedUsingDirective or (int)ErrorCode.HDN_UnusedExternAlias)).Verify(
                // (3000,1): error CS1537: The using alias 'alias1' appeared previously in this namespace
                // global using alias1 = C2;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "global using alias1 = C2;").WithArguments("alias1").WithLocation(3000, 1),
                // (5000,7): error CS1537: The using alias 'alias1' appeared previously in this namespace
                // using alias1 = C4;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "alias1").WithArguments("alias1").WithLocation(5000, 7),
                // (6000,1): error CS1537: The using alias 'alias2' appeared previously in this namespace
                // using alias2 = C5;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "using alias2 = C5;").WithArguments("alias2").WithLocation(6000, 1)
                );

            var comp5 = CreateCompilation(new[] { source2 + source3 + source4 + source5, source6 }, parseOptions: TestOptions.RegularPreview, references: new[] { comp1Ref });

            var expected1 = new[]
            {
                // (3000,1): error CS1537: The using alias 'alias1' appeared previously in this namespace
                // global using alias1 = C2;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "global using alias1 = C2;").WithArguments("alias1").WithLocation(3000, 1),
                // (4000,14): error CS1537: The using alias 'alias1' appeared previously in this namespace
                // global using alias1 = C3;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "alias1").WithArguments("alias1").WithLocation(4000, 14),
                // (5000,7): error CS1537: The using alias 'alias1' appeared previously in this namespace
                // using alias1 = C4;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "alias1").WithArguments("alias1").WithLocation(5000, 7),
                // (6000,1): error CS1537: The using alias 'alias2' appeared previously in this namespace
                // using alias2 = C5;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "using alias2 = C5;").WithArguments("alias2").WithLocation(6000, 1)
            };

            comp5.GetDiagnostics().Where(d => d.Code is not ((int)ErrorCode.HDN_UnusedUsingDirective or (int)ErrorCode.HDN_UnusedExternAlias)).Verify(expected1);

            var comp6 = CreateCompilation(new[] { source2, source3, source5, source6 }, parseOptions: TestOptions.RegularPreview, references: new[] { comp1Ref });
            comp6.GetDiagnostics().Where(d => d.Code is not ((int)ErrorCode.HDN_UnusedUsingDirective or (int)ErrorCode.HDN_UnusedExternAlias)).Verify(
                // (5000,7): error CS1537: The using alias 'alias1' appeared previously in this namespace
                // using alias1 = C4;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "alias1").WithArguments("alias1").WithLocation(5000, 7),
                // (1000,14): error CS1537: The using alias 'alias1' appeared previously in this namespace
                // extern alias alias1;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "alias1").WithArguments("alias1").WithLocation(1000, 14)
                );

            var comp7 = CreateCompilation(new[] { source2, source3, source4, source5, source6 }, parseOptions: TestOptions.RegularPreview, references: new[] { comp1Ref });
            comp7.GetDiagnostics().Where(d => d.Code is not ((int)ErrorCode.HDN_UnusedUsingDirective or (int)ErrorCode.HDN_UnusedExternAlias)).Verify(
                // (5000,7): error CS1537: The using alias 'alias1' appeared previously in this namespace
                // using alias1 = C4;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "alias1").WithArguments("alias1").WithLocation(5000, 7),
                // (4000,14): error CS1537: The using alias 'alias1' appeared previously in this namespace
                // global using alias1 = C3;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "alias1").WithArguments("alias1").WithLocation(4000, 14),
                // (1000,14): error CS1537: The using alias 'alias1' appeared previously in this namespace
                // extern alias alias1;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "alias1").WithArguments("alias1").WithLocation(1000, 14)
                );

            var comp8 = CreateCompilation(new[] { source2 + source3 + source5, source4, source6 }, parseOptions: TestOptions.RegularPreview, references: new[] { comp1Ref });
            comp8.GetDiagnostics().Where(d => d.Code is not ((int)ErrorCode.HDN_UnusedUsingDirective or (int)ErrorCode.HDN_UnusedExternAlias)).Verify(expected1);

            var comp9 = CreateCompilation(new[] { source2 + source5, source3, source6 }, parseOptions: TestOptions.RegularPreview, references: new[] { comp1Ref });
            comp9.GetDiagnostics().Where(d => d.Code is not ((int)ErrorCode.HDN_UnusedUsingDirective or (int)ErrorCode.HDN_UnusedExternAlias)).Verify(
                // (1000,14): error CS1537: The using alias 'alias1' appeared previously in this namespace
                // extern alias alias1;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "alias1").WithArguments("alias1").WithLocation(1000, 14),
                // (5000,7): error CS1537: The using alias 'alias1' appeared previously in this namespace
                // using alias1 = C4;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "alias1").WithArguments("alias1").WithLocation(5000, 7),
                // (6000,1): error CS1537: The using alias 'alias2' appeared previously in this namespace
                // using alias2 = C5;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "using alias2 = C5;").WithArguments("alias2").WithLocation(6000, 1)
                );

            var comp10 = CreateCompilation(new[] { source2 + source5, source3, source4, source6 }, parseOptions: TestOptions.RegularPreview, references: new[] { comp1Ref });
            comp10.GetDiagnostics().Where(d => d.Code is not ((int)ErrorCode.HDN_UnusedUsingDirective or (int)ErrorCode.HDN_UnusedExternAlias)).Verify(
                // (4000,14): error CS1537: The using alias 'alias1' appeared previously in this namespace
                // global using alias1 = C3;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "alias1").WithArguments("alias1").WithLocation(4000, 14),
                // (1000,14): error CS1537: The using alias 'alias1' appeared previously in this namespace
                // extern alias alias1;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "alias1").WithArguments("alias1").WithLocation(1000, 14),
                // (5000,7): error CS1537: The using alias 'alias1' appeared previously in this namespace
                // using alias1 = C4;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "alias1").WithArguments("alias1").WithLocation(5000, 7),
                // (6000,1): error CS1537: The using alias 'alias2' appeared previously in this namespace
                // using alias2 = C5;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "using alias2 = C5;").WithArguments("alias2").WithLocation(6000, 1)
                );
        }

        [Fact]
        public void AliasConflictWithExternAlias_02()
        {
            var source1 = @"
public class C1
{
}
";

            var comp1 = CreateCompilation(source1);
            var comp1Ref = comp1.ToMetadataReference().WithAliases(new[] { "alias1" });

            var source2 = @"
#line 1000
extern alias alias1;
";
            var source3 = @"
#line 2000
global using alias1 = C2;

class C2 {}
";
            var comp2 = CreateCompilation(new[] { source1, source2, source3 }, parseOptions: TestOptions.RegularPreview, references: new[] { comp1Ref });

            var expected = new[]
            {
                // (1000,14): error CS1537: The using alias 'alias1' appeared previously in this namespace
                // extern alias alias1;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "alias1").WithArguments("alias1").WithLocation(1000, 14)
            };

            comp2.GetSemanticModel(comp2.SyntaxTrees[1]).GetDeclarationDiagnostics().Where(d => d.Code is not ((int)ErrorCode.HDN_UnusedUsingDirective or (int)ErrorCode.HDN_UnusedExternAlias)).Verify(expected);

            comp2 = CreateCompilation(new[] { source1, source3, source2 }, parseOptions: TestOptions.RegularPreview, references: new[] { comp1Ref });
            comp2.GetSemanticModel(comp2.SyntaxTrees[2]).GetDeclarationDiagnostics().Where(d => d.Code is not ((int)ErrorCode.HDN_UnusedUsingDirective or (int)ErrorCode.HDN_UnusedExternAlias)).Verify(expected);
        }

        [Fact]
        public void AliasConflictWithGlobalAlias_01()
        {
            var source3 = @"
#line 1000
global using alias1 = C2;
";
            var source4 = @"
#line 2000
global using alias1 = C3;
";
            var source5 = @"
#line 3000
using alias1 = C4;
";
            var source6 = @"
class C2 {}
class C3 {}
class C4 {}
";
            var comp2 = CreateCompilation(new[] { source3, source5, source6 }, parseOptions: TestOptions.RegularPreview);

            var expected1 = new[]
            {
                // (3000,7): error CS1537: The using alias 'alias1' appeared previously in this namespace
                // using alias1 = C4;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "alias1").WithArguments("alias1").WithLocation(3000, 7)
            };

            comp2.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected1);

            var comp3 = CreateCompilation(new[] { source3 + source4, source5, source6 }, parseOptions: TestOptions.RegularPreview);

            var expected2 = new[]
            {
                // (2000,14): error CS1537: The using alias 'alias1' appeared previously in this namespace
                // global using alias1 = C3;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "alias1").WithArguments("alias1").WithLocation(2000, 14),
                // (3000,7): error CS1537: The using alias 'alias1' appeared previously in this namespace
                // using alias1 = C4;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "alias1").WithArguments("alias1").WithLocation(3000, 7)
            };

            comp3.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected2);

            var comp4 = CreateCompilation(new[] { source3 + source5, source6 }, parseOptions: TestOptions.RegularPreview);
            comp4.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected1);

            var comp5 = CreateCompilation(new[] { source3 + source4 + source5, source6 }, parseOptions: TestOptions.RegularPreview);
            comp5.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected2);

            var comp6 = CreateCompilation(new[] { source3, source4, source5, source6 }, parseOptions: TestOptions.RegularPreview);
            comp6.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected2);

            var comp7 = CreateCompilation(new[] { source3 + source5, source4, source6 }, parseOptions: TestOptions.RegularPreview);
            comp7.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected2);

            var comp8 = CreateCompilation(new[] { source5, source3, source6 }, parseOptions: TestOptions.RegularPreview);
            comp8.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected1);

            var comp9 = CreateCompilation(new[] { source5, source3, source4, source6 }, parseOptions: TestOptions.RegularPreview);
            comp9.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected2);
        }

        [Fact]
        public void AliasConflictWithGlobalAlias_02()
        {
            var source2 = @"
#line 1000
global using alias1 = C3;
";
            var source3 = @"
#line 2000
global using alias1 = C2;

class C2 {}
class C3 {}
";
            var comp2 = CreateCompilation(new[] { source2, source3 }, parseOptions: TestOptions.RegularPreview);
            comp2.GetSemanticModel(comp2.SyntaxTrees[1]).GetDeclarationDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(
                // (2000,14): error CS1537: The using alias 'alias1' appeared previously in this namespace
                // global using alias1 = C2;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "alias1").WithArguments("alias1").WithLocation(2000, 14)
                );

            comp2 = CreateCompilation(new[] { source3, source2 }, parseOptions: TestOptions.RegularPreview);
            comp2.GetSemanticModel(comp2.SyntaxTrees[1]).GetDeclarationDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(
                // (1000,14): error CS1537: The using alias 'alias1' appeared previously in this namespace
                // global using alias1 = C3;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "alias1").WithArguments("alias1").WithLocation(1000, 14)
                );
        }

        [Fact]
        public void AliasConflictWithGlobalAlias_03()
        {
            var source2 = @"
global using alias1 = C3;
";
            var source3 = @"
#line 1000
using alias1 = C2;
#line default

class C2 {}
class C3 {}
";
            var comp2 = CreateCompilation(new[] { source2, source3 }, parseOptions: TestOptions.RegularPreview);

            var expected = new[]
            {
                // (1000,7): error CS1537: The using alias 'alias1' appeared previously in this namespace
                // using alias1 = C2;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "alias1").WithArguments("alias1").WithLocation(1000, 7)
            };

            comp2.GetSemanticModel(comp2.SyntaxTrees[1]).GetDeclarationDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected);

            comp2 = CreateCompilation(new[] { source3, source2 }, parseOptions: TestOptions.RegularPreview);
            comp2.GetSemanticModel(comp2.SyntaxTrees[0]).GetDeclarationDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected);

            comp2 = CreateCompilation(source2 + source3, parseOptions: TestOptions.RegularPreview);
            comp2.GetSemanticModel(comp2.SyntaxTrees[0]).GetDeclarationDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected);
        }

        [Fact]
        public void TypeConflictWithGlobalAlias_01()
        {
            var source3 = @"
#line 1000
global using C2 = C2;
";
            var source4 = @"
#line 2000
global using C4 = C4;
";
            var source5 = @"
#line 3000
using C3 = C3;
#line 4000
using C = C4;
#line 5000
using static C4;

class Test
{
    void M()
    {
#line 6000
        _ = new C2();
#line 7000
        _ = new C3();
#line 8000
        _ = new C4();
    }
}
";
            var source6 = @"
class C2 {}
class C3 {}
class C4 {}
";
            var comp2 = CreateCompilation(new[] { source3, source5, source6 }, parseOptions: TestOptions.RegularPreview);

            var expected1 = new[]
            {
                // (6000,17): error CS0576: Namespace '<global namespace>' contains a definition conflicting with alias 'C2'
                //         _ = new C2();
                Diagnostic(ErrorCode.ERR_ConflictAliasAndMember, "C2").WithArguments("C2", "<global namespace>").WithLocation(6000, 17),
                // (7000,17): error CS0576: Namespace '<global namespace>' contains a definition conflicting with alias 'C3'
                //         _ = new C3();
                Diagnostic(ErrorCode.ERR_ConflictAliasAndMember, "C3").WithArguments("C3", "<global namespace>").WithLocation(7000, 17)
            };

            comp2.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected1);

            var comp3 = CreateCompilation(new[] { source3 + source4, source5, source6 }, parseOptions: TestOptions.RegularPreview);

            var expected2 = new[]
            {
                // (6000,17): error CS0576: Namespace '<global namespace>' contains a definition conflicting with alias 'C2'
                //         _ = new C2();
                Diagnostic(ErrorCode.ERR_ConflictAliasAndMember, "C2").WithArguments("C2", "<global namespace>").WithLocation(6000, 17),
                // (7000,17): error CS0576: Namespace '<global namespace>' contains a definition conflicting with alias 'C3'
                //         _ = new C3();
                Diagnostic(ErrorCode.ERR_ConflictAliasAndMember, "C3").WithArguments("C3", "<global namespace>").WithLocation(7000, 17),
                // (8000,17): error CS0576: Namespace '<global namespace>' contains a definition conflicting with alias 'C4'
                //         _ = new C4();
                Diagnostic(ErrorCode.ERR_ConflictAliasAndMember, "C4").WithArguments("C4", "<global namespace>").WithLocation(8000, 17)
            };

            comp3.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected2);

            var comp4 = CreateCompilation(new[] { source3 + source5, source6 }, parseOptions: TestOptions.RegularPreview);
            comp4.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected1);

            var comp5 = CreateCompilation(new[] { source3 + source4 + source5, source6 }, parseOptions: TestOptions.RegularPreview);
            comp5.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected2);

            var comp6 = CreateCompilation(new[] { source3, source4, source5, source6 }, parseOptions: TestOptions.RegularPreview);
            comp6.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected2);

            var comp7 = CreateCompilation(new[] { source3 + source5, source4, source6 }, parseOptions: TestOptions.RegularPreview);
            comp7.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected2);

            var comp8 = CreateCompilation(new[] { source5, source3, source6 }, parseOptions: TestOptions.RegularPreview);
            comp8.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected1);

            var comp9 = CreateCompilation(new[] { source5, source3, source4, source6 }, parseOptions: TestOptions.RegularPreview);
            comp9.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected2);
        }

        [Fact]
        public void NamespaceConflictWithGlobalAlias_01()
        {
            var source3 = @"
#line 1000
global using NS2 = NS2;
";
            var source4 = @"
#line 2000
global using NS4 = NS4;
";
            var source5 = @"
#line 3000
using NS3 = NS3;
#line 4000
using C = NS4;
#line 5000
using NS4;

class Test
{
    void M()
    {
#line 6000
        _ = new NS2.C2();
#line 7000
        _ = new NS3.C3();
#line 8000
        _ = new NS4.C4();
    }
}
";
            var source6 = @"
namespace NS2
{
    class C2 {}
}
namespace NS3
{
    class C3 {}
}
namespace NS4
{
    class C4 {}
}
";
            var comp2 = CreateCompilation(new[] { source3, source5, source6 }, parseOptions: TestOptions.RegularPreview);

            var expected1 = new[]
            {
                // (6000,17): error CS0576: Namespace '<global namespace>' contains a definition conflicting with alias 'NS2'
                //         _ = new NS2.C2();
                Diagnostic(ErrorCode.ERR_ConflictAliasAndMember, "NS2").WithArguments("NS2", "<global namespace>").WithLocation(6000, 17),
                // (7000,17): error CS0576: Namespace '<global namespace>' contains a definition conflicting with alias 'NS3'
                //         _ = new NS3.C3();
                Diagnostic(ErrorCode.ERR_ConflictAliasAndMember, "NS3").WithArguments("NS3", "<global namespace>").WithLocation(7000, 17)
            };

            comp2.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected1);

            var comp3 = CreateCompilation(new[] { source3 + source4, source5, source6 }, parseOptions: TestOptions.RegularPreview);

            var expected2 = new[]
            {
                // (6000,17): error CS0576: Namespace '<global namespace>' contains a definition conflicting with alias 'NS2'
                //         _ = new NS2.C2();
                Diagnostic(ErrorCode.ERR_ConflictAliasAndMember, "NS2").WithArguments("NS2", "<global namespace>").WithLocation(6000, 17),
                // (7000,17): error CS0576: Namespace '<global namespace>' contains a definition conflicting with alias 'NS3'
                //         _ = new NS3.C3();
                Diagnostic(ErrorCode.ERR_ConflictAliasAndMember, "NS3").WithArguments("NS3", "<global namespace>").WithLocation(7000, 17),
                // (8000,17): error CS0576: Namespace '<global namespace>' contains a definition conflicting with alias 'NS4'
                //         _ = new NS4.C4();
                Diagnostic(ErrorCode.ERR_ConflictAliasAndMember, "NS4").WithArguments("NS4", "<global namespace>").WithLocation(8000, 17)
            };

            comp3.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected2);

            var comp4 = CreateCompilation(new[] { source3 + source5, source6 }, parseOptions: TestOptions.RegularPreview);
            comp4.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected1);

            var comp5 = CreateCompilation(new[] { source3 + source4 + source5, source6 }, parseOptions: TestOptions.RegularPreview);
            comp5.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected2);

            var comp6 = CreateCompilation(new[] { source3, source4, source5, source6 }, parseOptions: TestOptions.RegularPreview);
            comp6.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected2);

            var comp7 = CreateCompilation(new[] { source3 + source5, source4, source6 }, parseOptions: TestOptions.RegularPreview);
            comp7.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected2);

            var comp8 = CreateCompilation(new[] { source5, source3, source6 }, parseOptions: TestOptions.RegularPreview);
            comp8.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected1);

            var comp9 = CreateCompilation(new[] { source5, source3, source4, source6 }, parseOptions: TestOptions.RegularPreview);
            comp9.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected2);
        }

        [Fact]
        public void UsingConflictWithGlobalUsing_01()
        {
            var source3 = @"
#line 1000
global using static C2;
";
            var source4 = @"
#line 2000
global using static C2;
";
            var source5 = @"
#line 3000
using static C2;
";
            var source6 = @"
class C2 {}
";
            var comp2 = CreateCompilation(new[] { source3, source5, source6 }, parseOptions: TestOptions.RegularPreview);

            var expected1 = new[]
            {
                // (3000,14): hidden CS8933: The using directive for 'C2' appeared previously as global using
                // using static C2;
                Diagnostic(ErrorCode.HDN_DuplicateWithGlobalUsing, "C2").WithArguments("C2").WithLocation(3000, 14)
            };

            CompileAndVerify(comp2).Diagnostics.Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected1);

            var comp3 = CreateCompilation(new[] { source3 + source4, source5, source6 }, parseOptions: TestOptions.RegularPreview);

            CompileAndVerify(comp3).Diagnostics.Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(
                // (2000,21): warning CS0105: The using directive for 'C2' appeared previously in this namespace
                // global using static C2;
                Diagnostic(ErrorCode.WRN_DuplicateUsing, "C2").WithArguments("C2").WithLocation(2000, 21),
                // (3000,14): hidden CS8933: The using directive for 'C2' appeared previously as global using
                // using static C2;
                Diagnostic(ErrorCode.HDN_DuplicateWithGlobalUsing, "C2").WithArguments("C2").WithLocation(3000, 14)
                );

            var comp4 = CreateCompilation(new[] { source3 + source5, source6 }, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(comp4).Diagnostics.Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(
                // (3000,14): warning CS0105: The using directive for 'C2' appeared previously in this namespace
                // using static C2;
                Diagnostic(ErrorCode.WRN_DuplicateUsing, "C2").WithArguments("C2").WithLocation(3000, 14)
                );

            var comp5 = CreateCompilation(new[] { source3 + source4 + source5, source6 }, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(comp5).Diagnostics.Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(
                // (2000,21): warning CS0105: The using directive for 'C2' appeared previously in this namespace
                // global using static C2;
                Diagnostic(ErrorCode.WRN_DuplicateUsing, "C2").WithArguments("C2").WithLocation(2000, 21),
                // (3000,14): warning CS0105: The using directive for 'C2' appeared previously in this namespace
                // using static C2;
                Diagnostic(ErrorCode.WRN_DuplicateUsing, "C2").WithArguments("C2").WithLocation(3000, 14)
                );

            var comp6 = CreateCompilation(new[] { source3, source4, source5, source6 }, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(comp6).Diagnostics.Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(
                // (2000,21): hidden CS8933: The using directive for 'C2' appeared previously as global using
                // global using static C2;
                Diagnostic(ErrorCode.HDN_DuplicateWithGlobalUsing, "C2").WithArguments("C2").WithLocation(2000, 21),
                // (3000,14): hidden CS8933: The using directive for 'C2' appeared previously as global using
                // using static C2;
                Diagnostic(ErrorCode.HDN_DuplicateWithGlobalUsing, "C2").WithArguments("C2").WithLocation(3000, 14)
                );

            var comp7 = CreateCompilation(new[] { source3 + source5, source4, source6 }, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(comp7).Diagnostics.Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(
                // (2000,21): hidden CS8933: The using directive for 'C2' appeared previously as global using
                // global using static C2;
                Diagnostic(ErrorCode.HDN_DuplicateWithGlobalUsing, "C2").WithArguments("C2").WithLocation(2000, 21),
                // (3000,14): warning CS0105: The using directive for 'C2' appeared previously in this namespace
                // using static C2;
                Diagnostic(ErrorCode.WRN_DuplicateUsing, "C2").WithArguments("C2").WithLocation(3000, 14)
                );

            var comp8 = CreateCompilation(new[] { source5, source3, source6 }, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(comp8).Diagnostics.Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected1);

            var comp9 = CreateCompilation(new[] { source5, source3, source4, source6 }, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(comp9).Diagnostics.Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(
                // (2000,21): hidden CS8933: The using directive for 'C2' appeared previously as global using
                // global using static C2;
                Diagnostic(ErrorCode.HDN_DuplicateWithGlobalUsing, "C2").WithArguments("C2").WithLocation(2000, 21),
                // (3000,14): hidden CS8933: The using directive for 'C2' appeared previously as global using
                // using static C2;
                Diagnostic(ErrorCode.HDN_DuplicateWithGlobalUsing, "C2").WithArguments("C2").WithLocation(3000, 14)
                );
        }

        [Fact]
        public void UsingConflictWithGlobalUsing_02()
        {
            var source2 = @"
#line 1000
global using static C2;
";
            var source3 = @"
#line 2000
global using static C2;

class C2 {}
";
            var comp2 = CreateCompilation(new[] { source2, source3 }, parseOptions: TestOptions.RegularPreview);
            comp2.GetSemanticModel(comp2.SyntaxTrees[1]).GetDeclarationDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(
                // (2000,21): hidden CS8933: The using directive for 'C2' appeared previously as global using
                // global using static C2;
                Diagnostic(ErrorCode.HDN_DuplicateWithGlobalUsing, "C2").WithArguments("C2").WithLocation(2000, 21)
                );

            comp2 = CreateCompilation(new[] { source3, source2 }, parseOptions: TestOptions.RegularPreview);
            comp2.GetSemanticModel(comp2.SyntaxTrees[1]).GetDeclarationDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(
                // (1000,21): hidden CS8933: The using directive for 'C2' appeared previously as global using
                // global using static C2;
                Diagnostic(ErrorCode.HDN_DuplicateWithGlobalUsing, "C2").WithArguments("C2").WithLocation(1000, 21)
                );
        }

        [Fact]
        public void UsingConflictWithGlobalUsing_03()
        {
            var source2 = @"
global using static C2;
";
            var source3 = @"
#line 1000
using static C2;
#line default

class C2 {}
";
            var comp2 = CreateCompilation(new[] { source2, source3 }, parseOptions: TestOptions.RegularPreview);

            var expected = new[]
            {
                // (1000,14): hidden CS8933: The using directive for 'C2' appeared previously as global using
                // using static C2;
                Diagnostic(ErrorCode.HDN_DuplicateWithGlobalUsing, "C2").WithArguments("C2").WithLocation(1000, 14)
            };

            comp2.GetSemanticModel(comp2.SyntaxTrees[1]).GetDeclarationDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected);

            comp2 = CreateCompilation(new[] { source3, source2 }, parseOptions: TestOptions.RegularPreview);
            comp2.GetSemanticModel(comp2.SyntaxTrees[0]).GetDeclarationDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected);

            comp2 = CreateCompilation(source2 + source3, parseOptions: TestOptions.RegularPreview);
            comp2.GetSemanticModel(comp2.SyntaxTrees[0]).GetDeclarationDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(
                // (1000,14): warning CS0105: The using directive for 'C2' appeared previously in this namespace
                // using static C2;
                Diagnostic(ErrorCode.WRN_DuplicateUsing, "C2").WithArguments("C2").WithLocation(1000, 14)
                );
        }

        [Fact]
        public void UsingConflictWithGlobalUsing_04()
        {
            var source3 = @"
#line 1000
global using N2;
";
            var source4 = @"
#line 2000
global using N2;
";
            var source5 = @"
#line 3000
using N2;
";
            var source6 = @"
namespace N2 { class C2 {} }
";
            var comp2 = CreateCompilation(new[] { source3, source5, source6 }, parseOptions: TestOptions.RegularPreview);

            var expected1 = new[]
            {
                // (3000,7): hidden CS8933: The using directive for 'N2' appeared previously as global using
                // using N2;
                Diagnostic(ErrorCode.HDN_DuplicateWithGlobalUsing, "N2").WithArguments("N2").WithLocation(3000, 7)
            };

            CompileAndVerify(comp2).Diagnostics.Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected1);

            var comp3 = CreateCompilation(new[] { source3 + source4, source5, source6 }, parseOptions: TestOptions.RegularPreview);

            CompileAndVerify(comp3).Diagnostics.Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(
                // (2000,14): warning CS0105: The using directive for 'N2' appeared previously in this namespace
                // global using N2;
                Diagnostic(ErrorCode.WRN_DuplicateUsing, "N2").WithArguments("N2").WithLocation(2000, 14),
                // (3000,7): hidden CS8933: The using directive for 'N2' appeared previously as global using
                // using N2;
                Diagnostic(ErrorCode.HDN_DuplicateWithGlobalUsing, "N2").WithArguments("N2").WithLocation(3000, 7)
                );

            var comp4 = CreateCompilation(new[] { source3 + source5, source6 }, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(comp4).Diagnostics.Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(
                // (3000,7): warning CS0105: The using directive for 'N2' appeared previously in this namespace
                // using N2;
                Diagnostic(ErrorCode.WRN_DuplicateUsing, "N2").WithArguments("N2").WithLocation(3000, 7)
                );

            var comp5 = CreateCompilation(new[] { source3 + source4 + source5, source6 }, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(comp5).Diagnostics.Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(
                // (2000,14): warning CS0105: The using directive for 'N2' appeared previously in this namespace
                // global using N2;
                Diagnostic(ErrorCode.WRN_DuplicateUsing, "N2").WithArguments("N2").WithLocation(2000, 14),
                // (3000,7): warning CS0105: The using directive for 'N2' appeared previously in this namespace
                // using N2;
                Diagnostic(ErrorCode.WRN_DuplicateUsing, "N2").WithArguments("N2").WithLocation(3000, 7)
                );

            var comp6 = CreateCompilation(new[] { source3, source4, source5, source6 }, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(comp6).Diagnostics.Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(
                // (2000,14): hidden CS8933: The using directive for 'N2' appeared previously as global using
                // global using N2;
                Diagnostic(ErrorCode.HDN_DuplicateWithGlobalUsing, "N2").WithArguments("N2").WithLocation(2000, 14),
                // (3000,7): hidden CS8933: The using directive for 'N2' appeared previously as global using
                // using N2;
                Diagnostic(ErrorCode.HDN_DuplicateWithGlobalUsing, "N2").WithArguments("N2").WithLocation(3000, 7)
                );

            var comp7 = CreateCompilation(new[] { source3 + source5, source4, source6 }, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(comp7).Diagnostics.Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(
                // (2000,14): hidden CS8933: The using directive for 'N2' appeared previously as global using
                // global using N2;
                Diagnostic(ErrorCode.HDN_DuplicateWithGlobalUsing, "N2").WithArguments("N2").WithLocation(2000, 14),
                // (3000,7): warning CS0105: The using directive for 'N2' appeared previously in this namespace
                // using N2;
                Diagnostic(ErrorCode.WRN_DuplicateUsing, "N2").WithArguments("N2").WithLocation(3000, 7)
                );

            var comp8 = CreateCompilation(new[] { source5, source3, source6 }, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(comp8).Diagnostics.Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected1);

            var comp9 = CreateCompilation(new[] { source5, source3, source4, source6 }, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(comp9).Diagnostics.Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(
                // (2000,14): hidden CS8933: The using directive for 'N2' appeared previously as global using
                // global using N2;
                Diagnostic(ErrorCode.HDN_DuplicateWithGlobalUsing, "N2").WithArguments("N2").WithLocation(2000, 14),
                // (3000,7): hidden CS8933: The using directive for 'N2' appeared previously as global using
                // using N2;
                Diagnostic(ErrorCode.HDN_DuplicateWithGlobalUsing, "N2").WithArguments("N2").WithLocation(3000, 7)
                );
        }

        [Fact]
        public void UsingConflictWithGlobalUsing_05()
        {
            var source2 = @"
#line 1000
global using N2;
";
            var source3 = @"
#line 2000
global using N2;

namespace N2 { class C2 {} }
";
            var comp2 = CreateCompilation(new[] { source2, source3 }, parseOptions: TestOptions.RegularPreview);
            comp2.GetSemanticModel(comp2.SyntaxTrees[1]).GetDeclarationDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(
                // (2000,14): hidden CS8933: The using directive for 'N2' appeared previously as global using
                // global using N2;
                Diagnostic(ErrorCode.HDN_DuplicateWithGlobalUsing, "N2").WithArguments("N2").WithLocation(2000, 14)
                );

            comp2 = CreateCompilation(new[] { source3, source2 }, parseOptions: TestOptions.RegularPreview);
            comp2.GetSemanticModel(comp2.SyntaxTrees[1]).GetDeclarationDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(
                // (1000,14): hidden CS8933: The using directive for 'N2' appeared previously as global using
                // global using N2;
                Diagnostic(ErrorCode.HDN_DuplicateWithGlobalUsing, "N2").WithArguments("N2").WithLocation(1000, 14)
                );
        }

        [Fact]
        public void UsingConflictWithGlobalUsing_06()
        {
            var source2 = @"
global using N2;
";
            var source3 = @"
#line 1000
using N2;
#line default

namespace N2 { class C2 {} }
";
            var comp2 = CreateCompilation(new[] { source2, source3 }, parseOptions: TestOptions.RegularPreview);

            var expected = new[]
            {
                // (1000,7): hidden CS8933: The using directive for 'N2' appeared previously as global using
                // using N2;
                Diagnostic(ErrorCode.HDN_DuplicateWithGlobalUsing, "N2").WithArguments("N2").WithLocation(1000, 7)
            };

            comp2.GetSemanticModel(comp2.SyntaxTrees[1]).GetDeclarationDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected);

            comp2 = CreateCompilation(new[] { source3, source2 }, parseOptions: TestOptions.RegularPreview);
            comp2.GetSemanticModel(comp2.SyntaxTrees[0]).GetDeclarationDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected);

            comp2 = CreateCompilation(source2 + source3, parseOptions: TestOptions.RegularPreview);
            comp2.GetSemanticModel(comp2.SyntaxTrees[0]).GetDeclarationDiagnostics().Where(d => d.Code != (int)ErrorCode.HDN_UnusedUsingDirective).Verify(
                // (1000,7): warning CS0105: The using directive for 'N2' appeared previously in this namespace
                // using N2;
                Diagnostic(ErrorCode.WRN_DuplicateUsing, "N2").WithArguments("N2").WithLocation(1000, 7)
                );
        }

        [Fact]
        public void UnusedGlobalAlias_01()
        {
            var source2 = @"
#line 1000
global using alias1 = C2;
#line 2000
global using alias2 = C3;
";
            var source3 = @"
#line 3000
global using alias3 = C4;
#line 3500
global using alias5 = C4;
#line 4000
using alias4 = C2;
#line 5000
using alias6 = C2;

class C1
{
    void M()
    {
        new alias2();
        new alias5();
        new alias6();
    }
}

class C2 {}
class C3 {}
class C4 {}
";
            var comp2 = CreateCompilation(new[] { source2, source3 }, parseOptions: TestOptions.RegularPreview);

            var expected1 = new[]
            {
                // (1000,1): hidden CS8019: Unnecessary using directive.
                // global using alias1 = C2;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "global using alias1 = C2;").WithLocation(1000, 1),
                // (3000,1): hidden CS8019: Unnecessary using directive.
                // global using alias3 = C4;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "global using alias3 = C4;").WithLocation(3000, 1),
                // (4000,1): hidden CS8019: Unnecessary using directive.
                // using alias4 = C2;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using alias4 = C2;").WithLocation(4000, 1)
            };

            comp2.VerifyDiagnostics(expected1);

            var expected2 = new[]
            {
                // (1000,1): hidden CS8019: Unnecessary using directive.
                // global using alias1 = C2;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "global using alias1 = C2;").WithLocation(1000, 1)
            };

            var expected3 = new[]
            {
                // (3000,1): hidden CS8019: Unnecessary using directive.
                // global using alias3 = C4;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "global using alias3 = C4;").WithLocation(3000, 1),
                // (4000,1): hidden CS8019: Unnecessary using directive.
                // using alias4 = C2;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using alias4 = C2;").WithLocation(4000, 1)
            };

            comp2 = CreateCompilation(new[] { source2, source3 }, parseOptions: TestOptions.RegularPreview);
            comp2.GetSemanticModel(comp2.SyntaxTrees[0]).GetDiagnostics().Verify(expected2);
            comp2.GetSemanticModel(comp2.SyntaxTrees[1]).GetDiagnostics().Verify(expected3);

            comp2 = CreateCompilation(new[] { source2, source3 }, parseOptions: TestOptions.RegularPreview);
            comp2.GetSemanticModel(comp2.SyntaxTrees[1]).GetDiagnostics().Verify(expected3);
            comp2.GetSemanticModel(comp2.SyntaxTrees[0]).GetDiagnostics().Verify(expected2);

            comp2 = CreateCompilation(new[] { source3, source2 }, parseOptions: TestOptions.RegularPreview);
            comp2.GetSemanticModel(comp2.SyntaxTrees[0]).GetDiagnostics().Verify(expected3);
            comp2.GetSemanticModel(comp2.SyntaxTrees[1]).GetDiagnostics().Verify(expected2);

            comp2 = CreateCompilation(source2 + source3, parseOptions: TestOptions.RegularPreview);
            comp2.GetSemanticModel(comp2.SyntaxTrees[0]).GetDiagnostics().Verify(expected1);
        }

        [Fact]
        public void UnusedGlobalStaticUsing_01()
        {
            var source2 = @"
#line 1000
global using static C2;
#line 2000
global using static C3;
";
            var source3 = @"
#line 3000
global using static C4;
#line 3500
global using static C6;
#line 4000
using static C5;
#line 5000
using static C7;

class C1
{
    void M()
    {
        new C33();
        new C66();
        new C77();
    }
}

class C2 {}
class C3 { public class C33 {} }
class C4 {}
class C5 {}
class C6 { public class C66 {} }
class C7 { public class C77 {} }
";
            var comp2 = CreateCompilation(new[] { source2, source3 }, parseOptions: TestOptions.RegularPreview);

            var expected1 = new[]
            {
                // (1000,1): hidden CS8019: Unnecessary using directive.
                // global using static C2;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "global using static C2;").WithLocation(1000, 1),
                // (3000,1): hidden CS8019: Unnecessary using directive.
                // global using static C4;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "global using static C4;").WithLocation(3000, 1),
                // (4000,1): hidden CS8019: Unnecessary using directive.
                // using static C5;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using static C5;").WithLocation(4000, 1)
            };

            comp2.VerifyDiagnostics(expected1);

            var expected2 = new[]
            {
                // (1000,1): hidden CS8019: Unnecessary using directive.
                // global using static C2;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "global using static C2;").WithLocation(1000, 1)
            };

            var expected3 = new[]
            {
                // (3000,1): hidden CS8019: Unnecessary using directive.
                // global using static C4;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "global using static C4;").WithLocation(3000, 1),
                // (4000,1): hidden CS8019: Unnecessary using directive.
                // using static C5;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using static C5;").WithLocation(4000, 1)
            };

            comp2 = CreateCompilation(new[] { source2, source3 }, parseOptions: TestOptions.RegularPreview);
            comp2.GetSemanticModel(comp2.SyntaxTrees[0]).GetDiagnostics().Verify(expected2);
            comp2.GetSemanticModel(comp2.SyntaxTrees[1]).GetDiagnostics().Verify(expected3);

            comp2 = CreateCompilation(new[] { source2, source3 }, parseOptions: TestOptions.RegularPreview);
            comp2.GetSemanticModel(comp2.SyntaxTrees[1]).GetDiagnostics().Verify(expected3);
            comp2.GetSemanticModel(comp2.SyntaxTrees[0]).GetDiagnostics().Verify(expected2);

            comp2 = CreateCompilation(new[] { source3, source2 }, parseOptions: TestOptions.RegularPreview);
            comp2.GetSemanticModel(comp2.SyntaxTrees[0]).GetDiagnostics().Verify(expected3);
            comp2.GetSemanticModel(comp2.SyntaxTrees[1]).GetDiagnostics().Verify(expected2);

            comp2 = CreateCompilation(source2 + source3, parseOptions: TestOptions.RegularPreview);
            comp2.GetSemanticModel(comp2.SyntaxTrees[0]).GetDiagnostics().Verify(expected1);
        }

        [Fact]
        public void UnusedGlobalUsingNamespace_01()
        {
            var source2 = @"
#line 1000
global using C2;
#line 2000
global using C3;
";
            var source3 = @"
#line 3000
global using C4;
#line 3500
global using C6;
#line 4000
using C5;
#line 5000
using C7;

class C1
{
    void M()
    {
        new C33();
        new C66();
        new C77();
    }
}

namespace C2 {}
namespace C3 { class C33 {} }
namespace C4 {}
namespace C5 {}
namespace C6 { class C66 {} }
namespace C7 { class C77 {} }
";
            var comp2 = CreateCompilation(new[] { source2, source3 }, parseOptions: TestOptions.RegularPreview);

            var expected1 = new[]
            {
                // (1000,1): hidden CS8019: Unnecessary using directive.
                // global using C2;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "global using C2;").WithLocation(1000, 1),
                // (3000,1): hidden CS8019: Unnecessary using directive.
                // global using C4;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "global using C4;").WithLocation(3000, 1),
                // (4000,1): hidden CS8019: Unnecessary using directive.
                // using C5;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using C5;").WithLocation(4000, 1)
            };

            comp2.VerifyDiagnostics(expected1);

            var expected2 = new[]
            {
                // (1000,1): hidden CS8019: Unnecessary using directive.
                // global using C2;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "global using C2;").WithLocation(1000, 1)
            };

            var expected3 = new[]
            {
                // (3000,1): hidden CS8019: Unnecessary using directive.
                // global using C4;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "global using C4;").WithLocation(3000, 1),
                // (4000,1): hidden CS8019: Unnecessary using directive.
                // using C5;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using C5;").WithLocation(4000, 1)
            };

            comp2 = CreateCompilation(new[] { source2, source3 }, parseOptions: TestOptions.RegularPreview);
            comp2.GetSemanticModel(comp2.SyntaxTrees[0]).GetDiagnostics().Verify(expected2);
            comp2.GetSemanticModel(comp2.SyntaxTrees[1]).GetDiagnostics().Verify(expected3);

            comp2 = CreateCompilation(new[] { source2, source3 }, parseOptions: TestOptions.RegularPreview);
            comp2.GetSemanticModel(comp2.SyntaxTrees[1]).GetDiagnostics().Verify(expected3);
            comp2.GetSemanticModel(comp2.SyntaxTrees[0]).GetDiagnostics().Verify(expected2);

            comp2 = CreateCompilation(new[] { source3, source2 }, parseOptions: TestOptions.RegularPreview);
            comp2.GetSemanticModel(comp2.SyntaxTrees[0]).GetDiagnostics().Verify(expected3);
            comp2.GetSemanticModel(comp2.SyntaxTrees[1]).GetDiagnostics().Verify(expected2);

            comp2 = CreateCompilation(source2 + source3, parseOptions: TestOptions.RegularPreview);
            comp2.GetSemanticModel(comp2.SyntaxTrees[0]).GetDiagnostics().Verify(expected1);
        }

        [Fact]
        public void UnusedGlobalUsingNamespace_02()
        {
            var source2 = @"
#line 1000
global using N2;
#line 2000
global using N3;
#line 3000
global using N10;

class C2
{
    void M()
    {
        new C1010();
#line 10000
        new C10000();
    }
}

#line 40000
partial class C0 : C0000
{
}
";
            var source3 = @"
#line 4000
using N5;
#line 5000
using N7;

class C3
{
    void M()
    {
        new C33();
        new C77();
#line 20000
        new C20000();
    }
}

namespace N2 {}
namespace N3 { class C33 {} }
namespace N5 {}
namespace N7 { class C77 {} }
namespace N8 {}
namespace N9 { class C99 {} }
namespace N10 { class C1010 {} }

#line 50000
partial class C0 : C0000
{
}
";
            var source4 = @"
#line 6000
using N8;
#line 7000
using N9;

class C4
{
    void M()
    {
        new C99();
#line 30000
        new C30000();
    }
}

#line 60000
partial class C0 : C0000
{
}
";
            var comp2 = CreateCompilation(new[] { source2, source3, source4 }, parseOptions: TestOptions.RegularPreview);

            Assert.Empty(comp2.UsageOfUsingsRecordedInTrees);

            comp2.VerifyDiagnostics(
                // (1000,1): hidden CS8019: Unnecessary using directive.
                // global using N2;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "global using N2;").WithLocation(1000, 1),
                // (4000,1): hidden CS8019: Unnecessary using directive.
                // using N5;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N5;").WithLocation(4000, 1),
                // (6000,1): hidden CS8019: Unnecessary using directive.
                // using N8;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N8;").WithLocation(6000, 1),
                // (10000,13): error CS0246: The type or namespace name 'C10000' could not be found (are you missing a using directive or an assembly reference?)
                //         new C10000();
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C10000").WithArguments("C10000").WithLocation(10000, 13),
                // (20000,13): error CS0246: The type or namespace name 'C20000' could not be found (are you missing a using directive or an assembly reference?)
                //         new C20000();
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C20000").WithArguments("C20000").WithLocation(20000, 13),
                // (30000,13): error CS0246: The type or namespace name 'C30000' could not be found (are you missing a using directive or an assembly reference?)
                //         new C30000();
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C30000").WithArguments("C30000").WithLocation(30000, 13),
                // (40000,20): error CS0246: The type or namespace name 'C0000' could not be found (are you missing a using directive or an assembly reference?)
                // partial class C0 : C0000
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C0000").WithArguments("C0000").WithLocation(40000, 20),
                // (50000,20): error CS0246: The type or namespace name 'C0000' could not be found (are you missing a using directive or an assembly reference?)
                // partial class C0 : C0000
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C0000").WithArguments("C0000").WithLocation(50000, 20),
                // (60000,20): error CS0246: The type or namespace name 'C0000' could not be found (are you missing a using directive or an assembly reference?)
                // partial class C0 : C0000
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C0000").WithArguments("C0000").WithLocation(60000, 20)
                );

            Assert.Null(comp2.UsageOfUsingsRecordedInTrees);

            var expected0 = new[]
            {
                // (1000,1): hidden CS8019: Unnecessary using directive.
                // global using N2;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "global using N2;").WithLocation(1000, 1),
                // (10000,13): error CS0246: The type or namespace name 'C10000' could not be found (are you missing a using directive or an assembly reference?)
                //         new C10000();
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C10000").WithArguments("C10000").WithLocation(10000, 13),
                // (40000,20): error CS0246: The type or namespace name 'C0000' could not be found (are you missing a using directive or an assembly reference?)
                // partial class C0 : C0000
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C0000").WithArguments("C0000").WithLocation(40000, 20)
            };

            var expected1 = new[]
            {
                // (4000,1): hidden CS8019: Unnecessary using directive.
                // using N5;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N5;").WithLocation(4000, 1),
                // (20000,13): error CS0246: The type or namespace name 'C20000' could not be found (are you missing a using directive or an assembly reference?)
                //         new C20000();
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C20000").WithArguments("C20000").WithLocation(20000, 13),
                // (50000,20): error CS0246: The type or namespace name 'C0000' could not be found (are you missing a using directive or an assembly reference?)
                // partial class C0 : C0000
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C0000").WithArguments("C0000").WithLocation(50000, 20)
            };

            var expected2 = new[]
            {
                // (6000,1): hidden CS8019: Unnecessary using directive.
                // using N8;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N8;").WithLocation(6000, 1),
                // (30000,13): error CS0246: The type or namespace name 'C30000' could not be found (are you missing a using directive or an assembly reference?)
                //         new C30000();
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C30000").WithArguments("C30000").WithLocation(30000, 13),
                // (60000,20): error CS0246: The type or namespace name 'C0000' could not be found (are you missing a using directive or an assembly reference?)
                // partial class C0 : C0000
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C0000").WithArguments("C0000").WithLocation(60000, 20)
            };

            comp2.GetSemanticModel(comp2.SyntaxTrees[1]).GetDiagnostics().Verify(expected1);
            Assert.Null(comp2.UsageOfUsingsRecordedInTrees);

            comp2.GetSemanticModel(comp2.SyntaxTrees[2]).GetDiagnostics().Verify(expected2);
            Assert.Null(comp2.UsageOfUsingsRecordedInTrees);

            comp2.GetSemanticModel(comp2.SyntaxTrees[0]).GetDiagnostics().Verify(expected0);
            Assert.Null(comp2.UsageOfUsingsRecordedInTrees);

            comp2 = CreateCompilation(new[] { source2, source3, source4 }, parseOptions: TestOptions.RegularPreview);

            comp2.GetSemanticModel(comp2.SyntaxTrees[0]).GetDiagnostics(TextSpan.FromBounds(20, comp2.SyntaxTrees[0].Length - 1)).Verify(
                // (10000,13): error CS0246: The type or namespace name 'C10000' could not be found (are you missing a using directive or an assembly reference?)
                //         new C10000();
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C10000").WithArguments("C10000").WithLocation(10000, 13),
                // (40000,20): error CS0246: The type or namespace name 'C0000' could not be found (are you missing a using directive or an assembly reference?)
                // partial class C0 : C0000
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C0000").WithArguments("C0000").WithLocation(40000, 20)
                );
            comp2.GetSemanticModel(comp2.SyntaxTrees[1]).GetDiagnostics(TextSpan.FromBounds(20, comp2.SyntaxTrees[1].Length - 1)).Verify(
                // (20000,13): error CS0246: The type or namespace name 'C20000' could not be found (are you missing a using directive or an assembly reference?)
                //         new C20000();
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C20000").WithArguments("C20000").WithLocation(20000, 13),
                // (50000,20): error CS0246: The type or namespace name 'C0000' could not be found (are you missing a using directive or an assembly reference?)
                // partial class C0 : C0000
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C0000").WithArguments("C0000").WithLocation(50000, 20)
                );
            comp2.GetSemanticModel(comp2.SyntaxTrees[2]).GetDiagnostics(TextSpan.FromBounds(20, comp2.SyntaxTrees[2].Length - 1)).Verify(
                // (30000,13): error CS0246: The type or namespace name 'C30000' could not be found (are you missing a using directive or an assembly reference?)
                //         new C30000();
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C30000").WithArguments("C30000").WithLocation(30000, 13),
                // (60000,20): error CS0246: The type or namespace name 'C0000' could not be found (are you missing a using directive or an assembly reference?)
                // partial class C0 : C0000
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C0000").WithArguments("C0000").WithLocation(60000, 20)
                );

            Assert.Empty(comp2.UsageOfUsingsRecordedInTrees);

            comp2 = CreateCompilation(new[] { source2, source3, source4 }, parseOptions: TestOptions.RegularPreview);

            comp2.GetSemanticModel(comp2.SyntaxTrees[1]).GetDiagnostics().Verify(expected1);
            AssertEx.SetEqual(new[] { comp2.SyntaxTrees[1] }, comp2.UsageOfUsingsRecordedInTrees);
            comp2.GetSemanticModel(comp2.SyntaxTrees[1]).GetDiagnostics().Verify(expected1);
            AssertEx.SetEqual(new[] { comp2.SyntaxTrees[1] }, comp2.UsageOfUsingsRecordedInTrees);

            comp2.GetSemanticModel(comp2.SyntaxTrees[2]).GetDiagnostics().Verify(expected2);
            AssertEx.SetEqual(new[] { comp2.SyntaxTrees[1], comp2.SyntaxTrees[2] }, comp2.UsageOfUsingsRecordedInTrees);
            comp2.GetSemanticModel(comp2.SyntaxTrees[2]).GetDiagnostics().Verify(expected2);
            AssertEx.SetEqual(new[] { comp2.SyntaxTrees[1], comp2.SyntaxTrees[2] }, comp2.UsageOfUsingsRecordedInTrees);

            comp2.GetSemanticModel(comp2.SyntaxTrees[0]).GetDiagnostics().Verify(expected0);
            Assert.Null(comp2.UsageOfUsingsRecordedInTrees);
            comp2.GetSemanticModel(comp2.SyntaxTrees[0]).GetDiagnostics().Verify(expected0);
            Assert.Null(comp2.UsageOfUsingsRecordedInTrees);

            comp2 = CreateCompilation(new[] { source2, source3, source4 }, parseOptions: TestOptions.RegularPreview);

            comp2.GetSemanticModel(comp2.SyntaxTrees[1]).GetDiagnostics().Verify(expected1);
            AssertEx.SetEqual(new[] { comp2.SyntaxTrees[1] }, comp2.UsageOfUsingsRecordedInTrees);

            comp2.GetSemanticModel(comp2.SyntaxTrees[0]).GetDiagnostics().Verify(expected0);
            Assert.Null(comp2.UsageOfUsingsRecordedInTrees);

            comp2.GetSemanticModel(comp2.SyntaxTrees[2]).GetDiagnostics().Verify(expected2);
            Assert.Null(comp2.UsageOfUsingsRecordedInTrees);

            comp2 = CreateCompilation(new[] { source2, source3, source4 }, parseOptions: TestOptions.RegularPreview);

            comp2.GetSemanticModel(comp2.SyntaxTrees[0]).GetDiagnostics().Verify(expected0);
            Assert.Null(comp2.UsageOfUsingsRecordedInTrees);

            comp2.GetSemanticModel(comp2.SyntaxTrees[1]).GetDiagnostics().Verify(expected1);
            Assert.Null(comp2.UsageOfUsingsRecordedInTrees);

            comp2.GetSemanticModel(comp2.SyntaxTrees[2]).GetDiagnostics().Verify(expected2);
            Assert.Null(comp2.UsageOfUsingsRecordedInTrees);
        }

        [Fact]
        public void QuickAttributeChecker_01()
        {
            var origLib_cs = @"public class C : System.Attribute { }";

            var alias1 = @"
global using alias1 = System.Runtime.CompilerServices.TypeForwardedToAttribute;
";
            var alias2 = @"
using alias2 = System;
";
            var alias3 = @"
global using alias2 = System;
";
            var alias4 = @"
using alias1 = System.Runtime.CompilerServices.TypeForwardedToAttribute;
";
            var newLib_cs = @"
[assembly: RefersToLib] // to bind this, we'll need to find type C in 'lib'
[assembly: alias1(typeof(C))] // but C is forwarded via alias
";

            var reference_cs =
@"
public class RefersToLibAttribute : C
{
    public RefersToLibAttribute() { }
}
";

            var origLibComp = CreateCompilation(origLib_cs, assemblyName: "lib");
            origLibComp.VerifyDiagnostics();

            var newComp = CreateCompilation(origLib_cs, assemblyName: "new");
            newComp.VerifyDiagnostics();

            var compWithReferenceToLib = CreateCompilation(reference_cs, references: new[] { origLibComp.EmitToImageReference() });
            compWithReferenceToLib.VerifyDiagnostics();

            MetadataReference newCompImageRef = newComp.EmitToImageReference();
            var newLibComp = CreateCompilation(alias1 + newLib_cs,
                references: new[] { compWithReferenceToLib.EmitToImageReference(), newCompImageRef }, assemblyName: "lib", parseOptions: TestOptions.RegularPreview);
            newLibComp.VerifyDiagnostics();

            CompilationReference newCompRef = newComp.ToMetadataReference();
            var newLibComp2 = CreateCompilation(alias1 + newLib_cs,
                references: new[] { compWithReferenceToLib.ToMetadataReference(), newCompRef }, assemblyName: "lib", parseOptions: TestOptions.RegularPreview);
            newLibComp2.VerifyDiagnostics();

            newLibComp = CreateCompilation(new[] { alias1, newLib_cs },
                references: new[] { compWithReferenceToLib.EmitToImageReference(), newCompImageRef }, assemblyName: "lib", parseOptions: TestOptions.RegularPreview);
            newLibComp.VerifyDiagnostics();

            newLibComp2 = CreateCompilation(new[] { alias1, newLib_cs },
                references: new[] { compWithReferenceToLib.ToMetadataReference(), newCompRef }, assemblyName: "lib", parseOptions: TestOptions.RegularPreview);
            newLibComp2.VerifyDiagnostics();

            newLibComp = CreateCompilation(alias1 + alias2 + newLib_cs,
                references: new[] { compWithReferenceToLib.EmitToImageReference(), newCompImageRef }, assemblyName: "lib", parseOptions: TestOptions.RegularPreview);
            newLibComp.VerifyDiagnostics(
                // (4,1): hidden CS8019: Unnecessary using directive.
                // using alias2 = System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using alias2 = System;").WithLocation(4, 1)
                );

            newLibComp2 = CreateCompilation(alias1 + alias2 + newLib_cs,
                references: new[] { compWithReferenceToLib.ToMetadataReference(), newCompRef }, assemblyName: "lib", parseOptions: TestOptions.RegularPreview);
            newLibComp2.VerifyDiagnostics(
                // (4,1): hidden CS8019: Unnecessary using directive.
                // using alias2 = System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using alias2 = System;").WithLocation(4, 1)
                );

            newLibComp = CreateCompilation(new[] { alias1, alias2 + newLib_cs },
                references: new[] { compWithReferenceToLib.EmitToImageReference(), newCompImageRef }, assemblyName: "lib", parseOptions: TestOptions.RegularPreview);
            newLibComp.VerifyDiagnostics(
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using alias2 = System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using alias2 = System;").WithLocation(2, 1)
                );

            newLibComp2 = CreateCompilation(new[] { alias1, alias2 + newLib_cs },
                references: new[] { compWithReferenceToLib.ToMetadataReference(), newCompRef }, assemblyName: "lib", parseOptions: TestOptions.RegularPreview);
            newLibComp2.VerifyDiagnostics(
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using alias2 = System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using alias2 = System;").WithLocation(2, 1)
                );

            newLibComp = CreateCompilation(alias3 + alias4 + newLib_cs,
                references: new[] { compWithReferenceToLib.EmitToImageReference(), newCompImageRef }, assemblyName: "lib", parseOptions: TestOptions.RegularPreview);
            newLibComp.VerifyDiagnostics(
                // (2,1): hidden CS8019: Unnecessary using directive.
                // global using alias2 = System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "global using alias2 = System;").WithLocation(2, 1)
                );

            newLibComp2 = CreateCompilation(alias3 + alias4 + newLib_cs,
                references: new[] { compWithReferenceToLib.ToMetadataReference(), newCompRef }, assemblyName: "lib", parseOptions: TestOptions.RegularPreview);
            newLibComp2.VerifyDiagnostics(
                // (2,1): hidden CS8019: Unnecessary using directive.
                // global using alias2 = System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "global using alias2 = System;").WithLocation(2, 1)
                );

            newLibComp = CreateCompilation(new[] { alias3, alias4 + newLib_cs },
                references: new[] { compWithReferenceToLib.EmitToImageReference(), newCompImageRef }, assemblyName: "lib", parseOptions: TestOptions.RegularPreview);
            newLibComp.VerifyDiagnostics(
                // (2,1): hidden CS8019: Unnecessary using directive.
                // global using alias2 = System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "global using alias2 = System;").WithLocation(2, 1)
                );

            newLibComp2 = CreateCompilation(new[] { alias3, alias4 + newLib_cs },
                references: new[] { compWithReferenceToLib.ToMetadataReference(), newCompRef }, assemblyName: "lib", parseOptions: TestOptions.RegularPreview);
            newLibComp2.VerifyDiagnostics(
                // (2,1): hidden CS8019: Unnecessary using directive.
                // global using alias2 = System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "global using alias2 = System;").WithLocation(2, 1)
                );
        }

        [Fact]
        public void ImportsInPdb_01()
        {
            var ext = @"
public class Extern
{
}
";
            var extComp = CreateCompilation(ext, assemblyName: "Extern");
            var extCompRef = extComp.ToMetadataReference().WithAliases(new[] { "ext" });

            var externAlias = @"
extern alias ext;
";
            var globalUsings1 = @"
global using alias1 = C1;
";
            var globalUsings2 = @"
global using alias2 = C1;
";
            var usings = @"
using alias3 = C1;
";

            var filler = @"

";

            var source = @"
class C1
{
    static void Main() 
    {
    }
}
";

            var expected1 = @"
<symbols>
  <methods>
    <method containingType=""C1"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""4"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""14"" startColumn=""5"" endLine=""14"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x1"">
        <extern alias=""ext"" />
        <alias name=""alias1"" target=""C1"" kind=""type"" />
        <alias name=""alias2"" target=""C1"" kind=""type"" />
        <alias name=""alias3"" target=""C1"" kind=""type"" />
        <externinfo alias=""ext"" assembly=""Extern, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" />
      </scope>
    </method>
  </methods>
</symbols>
";
            var parseOptions = TestOptions.RegularPreview.WithNoRefSafetyRulesAttribute();
            var comp = CreateCompilation(externAlias + globalUsings1 + globalUsings2 + usings + source, parseOptions: parseOptions, references: new[] { extCompRef });
            comp.VerifyPdb("C1.Main", expected1, options: PdbValidationOptions.ExcludeDocuments);

            comp = CreateCompilation(new[] { externAlias + globalUsings1 + filler + usings + source, globalUsings2 }, parseOptions: parseOptions, references: new[] { extCompRef });
            comp.VerifyPdb("C1.Main", expected1, options: PdbValidationOptions.ExcludeDocuments);

            comp = CreateCompilation(new[] { externAlias + usings + filler + filler + source, globalUsings1 + globalUsings2 }, parseOptions: parseOptions, references: new[] { extCompRef });
            comp.VerifyPdb("C1.Main", expected1, options: PdbValidationOptions.ExcludeDocuments);

            comp = CreateCompilation(new[] { externAlias + usings + filler + filler + source, globalUsings1, globalUsings2 }, parseOptions: parseOptions, references: new[] { extCompRef });
            comp.VerifyPdb("C1.Main", expected1, options: PdbValidationOptions.ExcludeDocuments);

            var expected2 = @"
<symbols>
  <methods>
    <method containingType=""C1"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""3"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x1"">
        <extern alias=""ext"" />
        <alias name=""alias1"" target=""C1"" kind=""type"" />
        <alias name=""alias2"" target=""C1"" kind=""type"" />
        <externinfo alias=""ext"" assembly=""Extern, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" />
      </scope>
    </method>
  </methods>
</symbols>
";
            comp = CreateCompilation(externAlias + globalUsings1 + globalUsings2 + source, parseOptions: parseOptions, references: new[] { extCompRef });
            comp.VerifyPdb("C1.Main", expected2, options: PdbValidationOptions.ExcludeDocuments);

            comp = CreateCompilation(new[] { externAlias + globalUsings1 + filler + source, globalUsings2 }, parseOptions: parseOptions, references: new[] { extCompRef });
            comp.VerifyPdb("C1.Main", expected2, options: PdbValidationOptions.ExcludeDocuments);

            comp = CreateCompilation(new[] { externAlias + filler + filler + source, globalUsings1 + globalUsings2 }, parseOptions: parseOptions, references: new[] { extCompRef });
            comp.VerifyPdb("C1.Main", expected2, options: PdbValidationOptions.ExcludeDocuments);

            comp = CreateCompilation(new[] { externAlias + filler + filler + source, globalUsings1, globalUsings2 }, parseOptions: parseOptions, references: new[] { extCompRef });
            comp.VerifyPdb("C1.Main", expected2, options: PdbValidationOptions.ExcludeDocuments);

            var expected3 = @"
<symbols>
  <methods>
    <method containingType=""C1"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""3"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x1"">
        <alias name=""alias1"" target=""C1"" kind=""type"" />
        <alias name=""alias2"" target=""C1"" kind=""type"" />
        <alias name=""alias3"" target=""C1"" kind=""type"" />
      </scope>
    </method>
  </methods>
</symbols>
";
            comp = CreateCompilation(globalUsings1 + globalUsings2 + usings + source, parseOptions: parseOptions);
            comp.VerifyPdb("C1.Main", expected3, options: PdbValidationOptions.ExcludeDocuments);

            comp = CreateCompilation(new[] { globalUsings1 + filler + usings + source, globalUsings2 }, parseOptions: parseOptions);
            comp.VerifyPdb("C1.Main", expected3, options: PdbValidationOptions.ExcludeDocuments);

            comp = CreateCompilation(new[] { usings + filler + filler + source, globalUsings1 + globalUsings2 }, parseOptions: parseOptions);
            comp.VerifyPdb("C1.Main", expected3, options: PdbValidationOptions.ExcludeDocuments);

            comp = CreateCompilation(new[] { usings + filler + filler + source, globalUsings1, globalUsings2 }, parseOptions: parseOptions);
            comp.VerifyPdb("C1.Main", expected3, options: PdbValidationOptions.ExcludeDocuments);

            var expected4 = @"
<symbols>
  <methods>
    <method containingType=""C1"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""2"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x1"">
        <alias name=""alias1"" target=""C1"" kind=""type"" />
        <alias name=""alias2"" target=""C1"" kind=""type"" />
      </scope>
    </method>
  </methods>
</symbols>
";
            comp = CreateCompilation(globalUsings1 + globalUsings2 + source, parseOptions: parseOptions);
            comp.VerifyPdb("C1.Main", expected4, options: PdbValidationOptions.ExcludeDocuments);

            comp = CreateCompilation(new[] { globalUsings1 + filler + source, globalUsings2 }, parseOptions: parseOptions);
            comp.VerifyPdb("C1.Main", expected4, options: PdbValidationOptions.ExcludeDocuments);

            comp = CreateCompilation(new[] { filler + filler + source, globalUsings1 + globalUsings2 }, parseOptions: parseOptions);
            comp.VerifyPdb("C1.Main", expected4, options: PdbValidationOptions.ExcludeDocuments);

            comp = CreateCompilation(new[] { filler + filler + source, globalUsings1, globalUsings2 }, parseOptions: parseOptions);
            comp.VerifyPdb("C1.Main", expected4, options: PdbValidationOptions.ExcludeDocuments);

            comp = CreateCompilation(source, parseOptions: parseOptions);
            comp.VerifyPdb("C1.Main", @"
<symbols>
  <methods>
    <method containingType=""C1"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>
", options: PdbValidationOptions.ExcludeDocuments);
        }

        [Fact]
        public void ImportsInPdb_02()
        {
            var ext = @"
public class Extern
{
}
";
            var extComp = CreateCompilation(ext, assemblyName: "Extern");
            var extCompRef = extComp.ToMetadataReference().WithAliases(new[] { "ext" });

            var externAlias = @"
extern alias ext;
";
            var globalUsings1 = @"
global using static C1;
";
            var globalUsings2 = @"
global using NS;
";
            var usings = @"
using static NS.C2;
";

            var filler = @"

";

            var source = @"
class C1
{
    static void Main() 
    {
    }
}

namespace NS
{
    class C2 {}
}
";

            var expected1 = @"
<symbols>
  <methods>
    <method containingType=""C1"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""4"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""14"" startColumn=""5"" endLine=""14"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x1"">
        <extern alias=""ext"" />
        <type name=""C1"" />
        <namespace name=""NS"" />
        <type name=""NS.C2"" />
        <externinfo alias=""ext"" assembly=""Extern, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" />
      </scope>
    </method>
  </methods>
</symbols>
";
            var parseOptions = TestOptions.RegularPreview.WithNoRefSafetyRulesAttribute();
            var comp = CreateCompilation(externAlias + globalUsings1 + globalUsings2 + usings + source, parseOptions: parseOptions, references: new[] { extCompRef });
            comp.VerifyPdb("C1.Main", expected1, options: PdbValidationOptions.ExcludeDocuments);

            comp = CreateCompilation(new[] { externAlias + globalUsings1 + filler + usings + source, globalUsings2 }, parseOptions: parseOptions, references: new[] { extCompRef });
            comp.VerifyPdb("C1.Main", expected1, options: PdbValidationOptions.ExcludeDocuments);

            comp = CreateCompilation(new[] { externAlias + usings + filler + filler + source, globalUsings1 + globalUsings2 }, parseOptions: parseOptions, references: new[] { extCompRef });
            comp.VerifyPdb("C1.Main", expected1, options: PdbValidationOptions.ExcludeDocuments);

            comp = CreateCompilation(new[] { externAlias + usings + filler + filler + source, globalUsings1, globalUsings2 }, parseOptions: parseOptions, references: new[] { extCompRef });
            comp.VerifyPdb("C1.Main", expected1, options: PdbValidationOptions.ExcludeDocuments);

            var expected2 = @"
<symbols>
  <methods>
    <method containingType=""C1"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""3"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x1"">
        <extern alias=""ext"" />
        <type name=""C1"" />
        <namespace name=""NS"" />
        <externinfo alias=""ext"" assembly=""Extern, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" />
      </scope>
    </method>
  </methods>
</symbols>
";
            comp = CreateCompilation(externAlias + globalUsings1 + globalUsings2 + source, parseOptions: parseOptions, references: new[] { extCompRef });
            comp.VerifyPdb("C1.Main", expected2, options: PdbValidationOptions.ExcludeDocuments);

            comp = CreateCompilation(new[] { externAlias + globalUsings1 + filler + source, globalUsings2 }, parseOptions: parseOptions, references: new[] { extCompRef });
            comp.VerifyPdb("C1.Main", expected2, options: PdbValidationOptions.ExcludeDocuments);

            comp = CreateCompilation(new[] { externAlias + filler + filler + source, globalUsings1 + globalUsings2 }, parseOptions: parseOptions, references: new[] { extCompRef });
            comp.VerifyPdb("C1.Main", expected2, options: PdbValidationOptions.ExcludeDocuments);

            comp = CreateCompilation(new[] { externAlias + filler + filler + source, globalUsings1, globalUsings2 }, parseOptions: parseOptions, references: new[] { extCompRef });
            comp.VerifyPdb("C1.Main", expected2, options: PdbValidationOptions.ExcludeDocuments);

            var expected3 = @"
<symbols>
  <methods>
    <method containingType=""C1"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""3"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x1"">
        <type name=""C1"" />
        <namespace name=""NS"" />
        <type name=""NS.C2"" />
      </scope>
    </method>
  </methods>
</symbols>
";
            comp = CreateCompilation(globalUsings1 + globalUsings2 + usings + source, parseOptions: parseOptions);
            comp.VerifyPdb("C1.Main", expected3, options: PdbValidationOptions.ExcludeDocuments);

            comp = CreateCompilation(new[] { globalUsings1 + filler + usings + source, globalUsings2 }, parseOptions: parseOptions);
            comp.VerifyPdb("C1.Main", expected3, options: PdbValidationOptions.ExcludeDocuments);

            comp = CreateCompilation(new[] { usings + filler + filler + source, globalUsings1 + globalUsings2 }, parseOptions: parseOptions);
            comp.VerifyPdb("C1.Main", expected3, options: PdbValidationOptions.ExcludeDocuments);

            comp = CreateCompilation(new[] { usings + filler + filler + source, globalUsings1, globalUsings2 }, parseOptions: parseOptions);
            comp.VerifyPdb("C1.Main", expected3, options: PdbValidationOptions.ExcludeDocuments);

            var expected4 = @"
<symbols>
  <methods>
    <method containingType=""C1"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""2"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x1"">
        <type name=""C1"" />
        <namespace name=""NS"" />
      </scope>
    </method>
  </methods>
</symbols>
";
            comp = CreateCompilation(globalUsings1 + globalUsings2 + source, parseOptions: parseOptions);
            comp.VerifyPdb("C1.Main", expected4, options: PdbValidationOptions.ExcludeDocuments);

            comp = CreateCompilation(new[] { globalUsings1 + filler + source, globalUsings2 }, parseOptions: parseOptions);
            comp.VerifyPdb("C1.Main", expected4, options: PdbValidationOptions.ExcludeDocuments);

            comp = CreateCompilation(new[] { filler + filler + source, globalUsings1 + globalUsings2 }, parseOptions: parseOptions);
            comp.VerifyPdb("C1.Main", expected4, options: PdbValidationOptions.ExcludeDocuments);

            comp = CreateCompilation(new[] { filler + filler + source, globalUsings1, globalUsings2 }, parseOptions: parseOptions);
            comp.VerifyPdb("C1.Main", expected4, options: PdbValidationOptions.ExcludeDocuments);
        }

        [Fact]
        public void GetDeclaredSymbol_01()
        {
            var externAlias = @"
extern alias alias1;
extern alias alias1;
";
            var globalUsings1 = @"
global using alias1 = C1;
";
            var globalUsings2 = @"
global using alias1 = C2;
";
            var usings = @"
using alias1 = C3;
using alias1 = C4;
";

            var source = @"
class C1 {}
class C2 {}
class C3 {}
class C4 {}
";

            var comp = CreateCompilation(new[] { externAlias + globalUsings1 + globalUsings2 + usings, source }, parseOptions: TestOptions.RegularPreview);
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var ext = tree.GetRoot().DescendantNodes().OfType<ExternAliasDirectiveSyntax>().ToArray();
            Assert.Equal(2, ext.Length);
            var aliases = tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().ToArray();
            Assert.Equal(4, aliases.Length);

            var ext1 = model.GetDeclaredSymbol(ext[0]);
            var ext2 = model.GetDeclaredSymbol(ext[1]);
            Assert.NotNull(ext1);
            Assert.NotNull(ext2);
            Assert.NotEqual(ext1, ext2);

            Assert.Equal("global using alias1 = C1;", aliases[0].ToString());
            Assert.Equal("C1", model.GetDeclaredSymbol(aliases[0]).Target.ToTestDisplayString());

            Assert.Equal("global using alias1 = C2;", aliases[1].ToString());
            Assert.Equal("C2", model.GetDeclaredSymbol(aliases[1]).Target.ToTestDisplayString());

            Assert.Equal("using alias1 = C3;", aliases[2].ToString());
            Assert.Equal("C3", model.GetDeclaredSymbol(aliases[2]).Target.ToTestDisplayString());

            Assert.Equal("using alias1 = C4;", aliases[3].ToString());
            Assert.Equal("C4", model.GetDeclaredSymbol(aliases[3]).Target.ToTestDisplayString());

            comp = CreateCompilation(new[] { externAlias + globalUsings1 + usings, globalUsings2, source }, parseOptions: TestOptions.RegularPreview);
            tree = comp.SyntaxTrees[0];
            model = comp.GetSemanticModel(tree);
            ext = tree.GetRoot().DescendantNodes().OfType<ExternAliasDirectiveSyntax>().ToArray();
            Assert.Equal(2, ext.Length);
            aliases = tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().ToArray();
            Assert.Equal(3, aliases.Length);

            ext1 = model.GetDeclaredSymbol(ext[0]);
            ext2 = model.GetDeclaredSymbol(ext[1]);
            Assert.NotNull(ext1);
            Assert.NotNull(ext2);
            Assert.NotEqual(ext1, ext2);

            Assert.Equal("global using alias1 = C1;", aliases[0].ToString());
            Assert.Equal("C1", model.GetDeclaredSymbol(aliases[0]).Target.ToTestDisplayString());

            Assert.Equal("using alias1 = C3;", aliases[1].ToString());
            Assert.Equal("C3", model.GetDeclaredSymbol(aliases[1]).Target.ToTestDisplayString());

            Assert.Equal("using alias1 = C4;", aliases[2].ToString());
            Assert.Equal("C4", model.GetDeclaredSymbol(aliases[2]).Target.ToTestDisplayString());

            tree = comp.SyntaxTrees[1];
            model = comp.GetSemanticModel(tree);
            aliases = tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().ToArray();
            Assert.Equal(1, aliases.Length);

            Assert.Equal("global using alias1 = C2;", aliases[0].ToString());
            Assert.Equal("C2", model.GetDeclaredSymbol(aliases[0]).Target.ToTestDisplayString());

            comp = CreateCompilation(new[] { externAlias + usings, globalUsings1 + globalUsings2, source }, parseOptions: TestOptions.RegularPreview);
            tree = comp.SyntaxTrees[0];
            model = comp.GetSemanticModel(tree);
            ext = tree.GetRoot().DescendantNodes().OfType<ExternAliasDirectiveSyntax>().ToArray();
            Assert.Equal(2, ext.Length);
            aliases = tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().ToArray();
            Assert.Equal(2, aliases.Length);

            ext1 = model.GetDeclaredSymbol(ext[0]);
            ext2 = model.GetDeclaredSymbol(ext[1]);
            Assert.NotNull(ext1);
            Assert.NotNull(ext2);
            Assert.NotEqual(ext1, ext2);

            Assert.Equal("using alias1 = C3;", aliases[0].ToString());
            Assert.Equal("C3", model.GetDeclaredSymbol(aliases[0]).Target.ToTestDisplayString());

            Assert.Equal("using alias1 = C4;", aliases[1].ToString());
            Assert.Equal("C4", model.GetDeclaredSymbol(aliases[1]).Target.ToTestDisplayString());

            tree = comp.SyntaxTrees[1];
            model = comp.GetSemanticModel(tree);
            aliases = tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().ToArray();
            Assert.Equal(2, aliases.Length);

            Assert.Equal("global using alias1 = C1;", aliases[0].ToString());
            Assert.Equal("C1", model.GetDeclaredSymbol(aliases[0]).Target.ToTestDisplayString());

            Assert.Equal("global using alias1 = C2;", aliases[1].ToString());
            Assert.Equal("C2", model.GetDeclaredSymbol(aliases[1]).Target.ToTestDisplayString());

            // -------------------------------------

            comp = CreateCompilation(new[] { externAlias + globalUsings1 + globalUsings2, source }, parseOptions: TestOptions.RegularPreview);
            tree = comp.SyntaxTrees[0];
            model = comp.GetSemanticModel(tree);
            ext = tree.GetRoot().DescendantNodes().OfType<ExternAliasDirectiveSyntax>().ToArray();
            Assert.Equal(2, ext.Length);
            aliases = tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().ToArray();
            Assert.Equal(2, aliases.Length);

            ext1 = model.GetDeclaredSymbol(ext[0]);
            ext2 = model.GetDeclaredSymbol(ext[1]);
            Assert.NotNull(ext1);
            Assert.NotNull(ext2);
            Assert.NotEqual(ext1, ext2);

            Assert.Equal("global using alias1 = C1;", aliases[0].ToString());
            Assert.Equal("C1", model.GetDeclaredSymbol(aliases[0]).Target.ToTestDisplayString());

            Assert.Equal("global using alias1 = C2;", aliases[1].ToString());
            Assert.Equal("C2", model.GetDeclaredSymbol(aliases[1]).Target.ToTestDisplayString());

            comp = CreateCompilation(new[] { externAlias + globalUsings1, globalUsings2, source }, parseOptions: TestOptions.RegularPreview);
            tree = comp.SyntaxTrees[0];
            model = comp.GetSemanticModel(tree);
            ext = tree.GetRoot().DescendantNodes().OfType<ExternAliasDirectiveSyntax>().ToArray();
            Assert.Equal(2, ext.Length);
            aliases = tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().ToArray();
            Assert.Equal(1, aliases.Length);

            ext1 = model.GetDeclaredSymbol(ext[0]);
            ext2 = model.GetDeclaredSymbol(ext[1]);
            Assert.NotNull(ext1);
            Assert.NotNull(ext2);
            Assert.NotEqual(ext1, ext2);

            Assert.Equal("global using alias1 = C1;", aliases[0].ToString());
            Assert.Equal("C1", model.GetDeclaredSymbol(aliases[0]).Target.ToTestDisplayString());

            tree = comp.SyntaxTrees[1];
            model = comp.GetSemanticModel(tree);
            aliases = tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().ToArray();
            Assert.Equal(1, aliases.Length);

            Assert.Equal("global using alias1 = C2;", aliases[0].ToString());
            Assert.Equal("C2", model.GetDeclaredSymbol(aliases[0]).Target.ToTestDisplayString());

            comp = CreateCompilation(new[] { externAlias, globalUsings1 + globalUsings2, source }, parseOptions: TestOptions.RegularPreview);
            tree = comp.SyntaxTrees[0];
            model = comp.GetSemanticModel(tree);
            ext = tree.GetRoot().DescendantNodes().OfType<ExternAliasDirectiveSyntax>().ToArray();
            Assert.Equal(2, ext.Length);
            aliases = tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().ToArray();
            Assert.Equal(0, aliases.Length);

            ext1 = model.GetDeclaredSymbol(ext[0]);
            ext2 = model.GetDeclaredSymbol(ext[1]);
            Assert.NotNull(ext1);
            Assert.NotNull(ext2);
            Assert.NotEqual(ext1, ext2);

            tree = comp.SyntaxTrees[1];
            model = comp.GetSemanticModel(tree);
            aliases = tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().ToArray();
            Assert.Equal(2, aliases.Length);

            Assert.Equal("global using alias1 = C1;", aliases[0].ToString());
            Assert.Equal("C1", model.GetDeclaredSymbol(aliases[0]).Target.ToTestDisplayString());

            Assert.Equal("global using alias1 = C2;", aliases[1].ToString());
            Assert.Equal("C2", model.GetDeclaredSymbol(aliases[1]).Target.ToTestDisplayString());

            // -------------------------------------

            comp = CreateCompilation(new[] { globalUsings1 + globalUsings2 + usings, source }, parseOptions: TestOptions.RegularPreview);
            tree = comp.SyntaxTrees[0];
            model = comp.GetSemanticModel(tree);
            ext = tree.GetRoot().DescendantNodes().OfType<ExternAliasDirectiveSyntax>().ToArray();
            Assert.Equal(0, ext.Length);
            aliases = tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().ToArray();
            Assert.Equal(4, aliases.Length);

            Assert.Equal("global using alias1 = C1;", aliases[0].ToString());
            Assert.Equal("C1", model.GetDeclaredSymbol(aliases[0]).Target.ToTestDisplayString());

            Assert.Equal("global using alias1 = C2;", aliases[1].ToString());
            Assert.Equal("C2", model.GetDeclaredSymbol(aliases[1]).Target.ToTestDisplayString());

            Assert.Equal("using alias1 = C3;", aliases[2].ToString());
            Assert.Equal("C3", model.GetDeclaredSymbol(aliases[2]).Target.ToTestDisplayString());

            Assert.Equal("using alias1 = C4;", aliases[3].ToString());
            Assert.Equal("C4", model.GetDeclaredSymbol(aliases[3]).Target.ToTestDisplayString());

            comp = CreateCompilation(new[] { globalUsings1 + usings, globalUsings2, source }, parseOptions: TestOptions.RegularPreview);
            tree = comp.SyntaxTrees[0];
            model = comp.GetSemanticModel(tree);
            ext = tree.GetRoot().DescendantNodes().OfType<ExternAliasDirectiveSyntax>().ToArray();
            Assert.Equal(0, ext.Length);
            aliases = tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().ToArray();
            Assert.Equal(3, aliases.Length);

            Assert.Equal("global using alias1 = C1;", aliases[0].ToString());
            Assert.Equal("C1", model.GetDeclaredSymbol(aliases[0]).Target.ToTestDisplayString());

            Assert.Equal("using alias1 = C3;", aliases[1].ToString());
            Assert.Equal("C3", model.GetDeclaredSymbol(aliases[1]).Target.ToTestDisplayString());

            Assert.Equal("using alias1 = C4;", aliases[2].ToString());
            Assert.Equal("C4", model.GetDeclaredSymbol(aliases[2]).Target.ToTestDisplayString());

            tree = comp.SyntaxTrees[1];
            model = comp.GetSemanticModel(tree);
            aliases = tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().ToArray();
            Assert.Equal(1, aliases.Length);

            Assert.Equal("global using alias1 = C2;", aliases[0].ToString());
            Assert.Equal("C2", model.GetDeclaredSymbol(aliases[0]).Target.ToTestDisplayString());

            comp = CreateCompilation(new[] { usings, globalUsings1 + globalUsings2, source }, parseOptions: TestOptions.RegularPreview);
            tree = comp.SyntaxTrees[0];
            model = comp.GetSemanticModel(tree);
            ext = tree.GetRoot().DescendantNodes().OfType<ExternAliasDirectiveSyntax>().ToArray();
            Assert.Equal(0, ext.Length);
            aliases = tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().ToArray();
            Assert.Equal(2, aliases.Length);

            Assert.Equal("using alias1 = C3;", aliases[0].ToString());
            Assert.Equal("C3", model.GetDeclaredSymbol(aliases[0]).Target.ToTestDisplayString());

            Assert.Equal("using alias1 = C4;", aliases[1].ToString());
            Assert.Equal("C4", model.GetDeclaredSymbol(aliases[1]).Target.ToTestDisplayString());

            tree = comp.SyntaxTrees[1];
            model = comp.GetSemanticModel(tree);
            aliases = tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().ToArray();
            Assert.Equal(2, aliases.Length);

            Assert.Equal("global using alias1 = C1;", aliases[0].ToString());
            Assert.Equal("C1", model.GetDeclaredSymbol(aliases[0]).Target.ToTestDisplayString());

            Assert.Equal("global using alias1 = C2;", aliases[1].ToString());
            Assert.Equal("C2", model.GetDeclaredSymbol(aliases[1]).Target.ToTestDisplayString());

            // -------------------------------------

            comp = CreateCompilation(new[] { globalUsings1 + globalUsings2, source }, parseOptions: TestOptions.RegularPreview);
            tree = comp.SyntaxTrees[0];
            model = comp.GetSemanticModel(tree);
            ext = tree.GetRoot().DescendantNodes().OfType<ExternAliasDirectiveSyntax>().ToArray();
            Assert.Equal(0, ext.Length);
            aliases = tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().ToArray();
            Assert.Equal(2, aliases.Length);

            Assert.Equal("global using alias1 = C1;", aliases[0].ToString());
            Assert.Equal("C1", model.GetDeclaredSymbol(aliases[0]).Target.ToTestDisplayString());

            Assert.Equal("global using alias1 = C2;", aliases[1].ToString());
            Assert.Equal("C2", model.GetDeclaredSymbol(aliases[1]).Target.ToTestDisplayString());

            comp = CreateCompilation(new[] { globalUsings1, globalUsings2, source }, parseOptions: TestOptions.RegularPreview);
            tree = comp.SyntaxTrees[0];
            model = comp.GetSemanticModel(tree);
            ext = tree.GetRoot().DescendantNodes().OfType<ExternAliasDirectiveSyntax>().ToArray();
            Assert.Equal(0, ext.Length);
            aliases = tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().ToArray();
            Assert.Equal(1, aliases.Length);

            Assert.Equal("global using alias1 = C1;", aliases[0].ToString());
            Assert.Equal("C1", model.GetDeclaredSymbol(aliases[0]).Target.ToTestDisplayString());

            tree = comp.SyntaxTrees[1];
            model = comp.GetSemanticModel(tree);
            aliases = tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().ToArray();
            Assert.Equal(1, aliases.Length);

            Assert.Equal("global using alias1 = C2;", aliases[0].ToString());
            Assert.Equal("C2", model.GetDeclaredSymbol(aliases[0]).Target.ToTestDisplayString());

            // -------------------------------------

            comp = CreateCompilation(new[] { externAlias + usings, source }, parseOptions: TestOptions.RegularPreview);
            tree = comp.SyntaxTrees[0];
            model = comp.GetSemanticModel(tree);
            ext = tree.GetRoot().DescendantNodes().OfType<ExternAliasDirectiveSyntax>().ToArray();
            Assert.Equal(2, ext.Length);
            aliases = tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().ToArray();
            Assert.Equal(2, aliases.Length);

            ext1 = model.GetDeclaredSymbol(ext[0]);
            ext2 = model.GetDeclaredSymbol(ext[1]);
            Assert.NotNull(ext1);
            Assert.NotNull(ext2);
            Assert.NotEqual(ext1, ext2);

            Assert.Equal("using alias1 = C3;", aliases[0].ToString());
            Assert.Equal("C3", model.GetDeclaredSymbol(aliases[0]).Target.ToTestDisplayString());

            Assert.Equal("using alias1 = C4;", aliases[1].ToString());
            Assert.Equal("C4", model.GetDeclaredSymbol(aliases[1]).Target.ToTestDisplayString());

            // -------------------------------------

            comp = CreateCompilation(new[] { externAlias, source }, parseOptions: TestOptions.RegularPreview);
            tree = comp.SyntaxTrees[0];
            model = comp.GetSemanticModel(tree);
            ext = tree.GetRoot().DescendantNodes().OfType<ExternAliasDirectiveSyntax>().ToArray();
            Assert.Equal(2, ext.Length);
            aliases = tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().ToArray();
            Assert.Equal(0, aliases.Length);

            ext1 = model.GetDeclaredSymbol(ext[0]);
            ext2 = model.GetDeclaredSymbol(ext[1]);
            Assert.NotNull(ext1);
            Assert.NotNull(ext2);
            Assert.NotEqual(ext1, ext2);

            // -------------------------------------

            comp = CreateCompilation(new[] { usings, source }, parseOptions: TestOptions.RegularPreview);
            tree = comp.SyntaxTrees[0];
            model = comp.GetSemanticModel(tree);
            ext = tree.GetRoot().DescendantNodes().OfType<ExternAliasDirectiveSyntax>().ToArray();
            Assert.Equal(0, ext.Length);
            aliases = tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().ToArray();
            Assert.Equal(2, aliases.Length);

            Assert.Equal("using alias1 = C3;", aliases[0].ToString());
            Assert.Equal("C3", model.GetDeclaredSymbol(aliases[0]).Target.ToTestDisplayString());

            Assert.Equal("using alias1 = C4;", aliases[1].ToString());
            Assert.Equal("C4", model.GetDeclaredSymbol(aliases[1]).Target.ToTestDisplayString());
        }

        [Fact]
        public void LookupAmbiguityInAliases_01()
        {
            var externAlias1 = @"
#line 1000
extern alias alias1;
";
            var externAlias2 = @"
#line 2000
extern alias alias1;
";
            var globalUsings1 = @"
#line 3000
global using alias1 = C1;
";
            var globalUsings2 = @"
#line 4000
global using alias1 = C2;
";
            var usings1 = @"
#line 5000
using alias1 = C3;
";
            var usings2 = @"
#line 6000
using alias1 = C4;
";

            var source = @"
using NS;

class Test
{
    void M()
    {
#line 7000
        new alias1();
    }
}

class C1 {}
class C2 {}
class C3 {}
class C4 {}

namespace NS
{
    class alias1 {}
}
";

            var comp = CreateCompilation(new[] { externAlias1 + globalUsings1 + source }, parseOptions: TestOptions.RegularPreview);

            var expected1 = new[]
            {
                // (7000,13): error CS0104: 'alias1' is an ambiguous reference between 'C1' and '<global namespace>'
                //         new alias1();
                Diagnostic(ErrorCode.ERR_AmbigContext, "alias1").WithArguments("alias1", "C1", "<global namespace>").WithLocation(7000, 13)
            };

            comp.GetDiagnostics().Where(d => d.Code is not ((int)ErrorCode.ERR_BadExternAlias or (int)ErrorCode.ERR_DuplicateAlias or (int)ErrorCode.HDN_UnusedUsingDirective)).Verify(expected1);

            comp = CreateCompilation(new[] { externAlias1 + source, globalUsings1 }, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code is not ((int)ErrorCode.ERR_BadExternAlias or (int)ErrorCode.ERR_DuplicateAlias or (int)ErrorCode.HDN_UnusedUsingDirective)).Verify(expected1);

            comp = CreateCompilation(new[] { externAlias1 + usings1 + source }, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code is not ((int)ErrorCode.ERR_BadExternAlias or (int)ErrorCode.ERR_DuplicateAlias or (int)ErrorCode.HDN_UnusedUsingDirective)).Verify(
                // (7000,13): error CS0104: 'alias1' is an ambiguous reference between 'C3' and '<global namespace>'
                //         new alias1();
                Diagnostic(ErrorCode.ERR_AmbigContext, "alias1").WithArguments("alias1", "C3", "<global namespace>").WithLocation(7000, 13)
                );

            var expected2 = new[]
            {
                // (5000,7): error CS1537: The using alias 'alias1' appeared previously in this namespace
                // using alias1 = C3;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "alias1").WithArguments("alias1").WithLocation(5000, 7)
            };

            comp = CreateCompilation(new[] { globalUsings1 + usings1 + source }, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code is not ((int)ErrorCode.ERR_BadExternAlias or (int)ErrorCode.HDN_UnusedUsingDirective)).Verify(expected2);
            var tree = comp.SyntaxTrees[0];
            var node = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Single();
            var model = comp.GetSemanticModel(tree);
            Assert.Equal("C1", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("alias1=C1", model.GetAliasInfo(node.Type).ToTestDisplayString());

            comp = CreateCompilation(new[] { usings1 + source, globalUsings1 }, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code is not ((int)ErrorCode.ERR_BadExternAlias or (int)ErrorCode.HDN_UnusedUsingDirective)).Verify(expected2);
            tree = comp.SyntaxTrees[0];
            node = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Single();
            model = comp.GetSemanticModel(tree);
            Assert.Equal("C1", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("alias1=C1", model.GetAliasInfo(node.Type).ToTestDisplayString());

            comp = CreateCompilation(new[] { externAlias1 + globalUsings1 + usings1 + source }, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code is not ((int)ErrorCode.ERR_BadExternAlias or (int)ErrorCode.ERR_DuplicateAlias or (int)ErrorCode.HDN_UnusedUsingDirective)).Verify(expected1);

            comp = CreateCompilation(new[] { externAlias1 + usings1 + source, globalUsings1 }, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code is not ((int)ErrorCode.ERR_BadExternAlias or (int)ErrorCode.ERR_DuplicateAlias or (int)ErrorCode.HDN_UnusedUsingDirective)).Verify(expected1);

            comp = CreateCompilation(new[] { externAlias1 + externAlias2 + source }, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code is not ((int)ErrorCode.ERR_BadExternAlias or (int)ErrorCode.ERR_DuplicateAlias or (int)ErrorCode.HDN_UnusedUsingDirective)).Verify(
                // (7000,13): error CS0104: 'alias1' is an ambiguous reference between '<global namespace>' and '<global namespace>'
                //         new alias1();
                Diagnostic(ErrorCode.ERR_AmbigContext, "alias1").WithArguments("alias1", "<global namespace>", "<global namespace>").WithLocation(7000, 13)
                );

            var expected3 = new[]
            {
                // (4000,14): error CS1537: The using alias 'alias1' appeared previously in this namespace
                // global using alias1 = C2;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "alias1").WithArguments("alias1").WithLocation(4000, 14)
            };

            comp = CreateCompilation(new[] { globalUsings1 + globalUsings2 + source }, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code is not ((int)ErrorCode.ERR_BadExternAlias or (int)ErrorCode.HDN_UnusedUsingDirective)).Verify(expected3);
            tree = comp.SyntaxTrees[0];
            node = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Single();
            model = comp.GetSemanticModel(tree);
            Assert.Equal("C1", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("alias1=C1", model.GetAliasInfo(node.Type).ToTestDisplayString());

            comp = CreateCompilation(new[] { globalUsings1 + source, globalUsings2 }, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code is not ((int)ErrorCode.ERR_BadExternAlias or (int)ErrorCode.HDN_UnusedUsingDirective)).Verify(expected3);
            tree = comp.SyntaxTrees[0];
            node = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Single();
            model = comp.GetSemanticModel(tree);
            Assert.Equal("C1", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("alias1=C1", model.GetAliasInfo(node.Type).ToTestDisplayString());

            comp = CreateCompilation(new[] { source, globalUsings1 + globalUsings2 }, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code is not ((int)ErrorCode.ERR_BadExternAlias or (int)ErrorCode.HDN_UnusedUsingDirective)).Verify(expected3);
            tree = comp.SyntaxTrees[0];
            node = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Single();
            model = comp.GetSemanticModel(tree);
            Assert.Equal("C1", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("alias1=C1", model.GetAliasInfo(node.Type).ToTestDisplayString());

            comp = CreateCompilation(new[] { source, globalUsings1, globalUsings2 }, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code is not ((int)ErrorCode.ERR_BadExternAlias or (int)ErrorCode.HDN_UnusedUsingDirective)).Verify(expected3);
            tree = comp.SyntaxTrees[0];
            node = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Single();
            model = comp.GetSemanticModel(tree);
            Assert.Equal("C1", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("alias1=C1", model.GetAliasInfo(node.Type).ToTestDisplayString());

            comp = CreateCompilation(new[] { usings1 + usings2 + source }, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code is not ((int)ErrorCode.ERR_BadExternAlias or (int)ErrorCode.HDN_UnusedUsingDirective)).Verify(
                // (6000,7): error CS1537: The using alias 'alias1' appeared previously in this namespace
                // using alias1 = C4;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "alias1").WithArguments("alias1").WithLocation(6000, 7)
                );
            tree = comp.SyntaxTrees[0];
            node = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Single();
            model = comp.GetSemanticModel(tree);
            Assert.Equal("C3", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("alias1=C3", model.GetAliasInfo(node.Type).ToTestDisplayString());
        }

        [Fact]
        public void LookupAmbiguityInUsedNamespacesOrTypes_01()
        {
            var globalUsings1 = @"
global using NS1;
";
            var globalUsings2 = @"
global using static C2;
";
            var usings1 = @"
using NS3;
";
            var usings2 = @"
using static C4;
";

            var source = @"
class Test
{
    void M()
    {
#line 7000
        new C5();
    }
}

class C2
{
    public class C5 {}
}
class C4
{
    public class C5 {}
}

namespace NS1
{
    public class C5 {}
}
namespace NS3
{
    public class C5 {}
}
";

            var comp = CreateCompilation(new[] { globalUsings1 + globalUsings2 + source }, parseOptions: TestOptions.RegularPreview);

            var expected1 = new[]
            {
                // (7000,13): error CS0104: 'C5' is an ambiguous reference between 'C2.C5' and 'NS1.C5'
                //         new C5();
                Diagnostic(ErrorCode.ERR_AmbigContext, "C5").WithArguments("C5", "C2.C5", "NS1.C5").WithLocation(7000, 13)
            };

            comp.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected1);

            comp = CreateCompilation(new[] { globalUsings1 + source, globalUsings2 }, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected1);

            comp = CreateCompilation(new[] { source, globalUsings1 + globalUsings2 }, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected1);

            comp = CreateCompilation(new[] { source, globalUsings1, globalUsings2 }, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected1);

            comp = CreateCompilation(new[] { globalUsings1 + usings2 + source }, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.HDN_UnusedUsingDirective).Verify(
                // (7000,13): error CS0104: 'C5' is an ambiguous reference between 'C4.C5' and 'NS1.C5'
                //         new C5();
                Diagnostic(ErrorCode.ERR_AmbigContext, "C5").WithArguments("C5", "C4.C5", "NS1.C5").WithLocation(7000, 13)
                );

            comp = CreateCompilation(new[] { usings1 + source, globalUsings2 }, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.HDN_UnusedUsingDirective).Verify(
                // (7000,13): error CS0104: 'C5' is an ambiguous reference between 'C2.C5' and 'NS3.C5'
                //         new C5();
                Diagnostic(ErrorCode.ERR_AmbigContext, "C5").WithArguments("C5", "C2.C5", "NS3.C5").WithLocation(7000, 13)
                );

            comp = CreateCompilation(new[] { usings1 + usings2 + source }, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.HDN_UnusedUsingDirective).Verify(
                // (7000,13): error CS0104: 'C5' is an ambiguous reference between 'C4.C5' and 'NS3.C5'
                //         new C5();
                Diagnostic(ErrorCode.ERR_AmbigContext, "C5").WithArguments("C5", "C4.C5", "NS3.C5").WithLocation(7000, 13)
                );
        }

        [Fact]
        public void LookupAmbiguityInUsedNamespacesOrTypes_02()
        {
            var globalUsings1 = @"
global using NS1;
";
            var globalUsings2 = @"
global using static NS.C2;
";
            var usings1 = @"
using NS3;
";
            var usings2 = @"
using static NS.C4;
";

            var source = @"
class Test
{
    void M()
    {
#line 7000
        1.M5();
    }
}

namespace NS
{
    static class C2
    {
        public static void M5(this int x) {}
    }
    static class C4
    {
        public static void M5(this int x) {}
    }
}

namespace NS1
{
    public static class C5
    {
        public static void M5(this int x) {}
    }
}
namespace NS3
{
    public static class C5
    {
        public static void M5(this int x) {}
    }
}
";

            var comp = CreateCompilation(new[] { globalUsings1 + globalUsings2 + source }, parseOptions: TestOptions.RegularPreview);

            var expected1 = new[]
            {
                // (7000,11): error CS0121: The call is ambiguous between the following methods or properties: 'NS1.C5.M5(int)' and 'NS.C2.M5(int)'
                //         1.M5();
                Diagnostic(ErrorCode.ERR_AmbigCall, "M5").WithArguments("NS1.C5.M5(int)", "NS.C2.M5(int)").WithLocation(7000, 11)
            };

            comp.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected1);

            comp = CreateCompilation(new[] { globalUsings1 + source, globalUsings2 }, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected1);

            comp = CreateCompilation(new[] { source, globalUsings1 + globalUsings2 }, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected1);

            comp = CreateCompilation(new[] { source, globalUsings1, globalUsings2 }, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected1);

            comp = CreateCompilation(new[] { globalUsings1 + usings2 + source }, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.HDN_UnusedUsingDirective).Verify(
                // (7000,11): error CS0121: The call is ambiguous between the following methods or properties: 'NS1.C5.M5(int)' and 'NS.C4.M5(int)'
                //         1.M5();
                Diagnostic(ErrorCode.ERR_AmbigCall, "M5").WithArguments("NS1.C5.M5(int)", "NS.C4.M5(int)").WithLocation(7000, 11)
                );

            comp = CreateCompilation(new[] { usings1 + source, globalUsings2 }, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.HDN_UnusedUsingDirective).Verify(
                // (7000,11): error CS0121: The call is ambiguous between the following methods or properties: 'NS.C2.M5(int)' and 'NS3.C5.M5(int)'
                //         1.M5();
                Diagnostic(ErrorCode.ERR_AmbigCall, "M5").WithArguments("NS.C2.M5(int)", "NS3.C5.M5(int)").WithLocation(7000, 11)
                );

            comp = CreateCompilation(new[] { usings1 + usings2 + source }, parseOptions: TestOptions.RegularPreview);
            comp.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.HDN_UnusedUsingDirective).Verify(
                // (7000,11): error CS0121: The call is ambiguous between the following methods or properties: 'NS3.C5.M5(int)' and 'NS.C4.M5(int)'
                //         1.M5();
                Diagnostic(ErrorCode.ERR_AmbigCall, "M5").WithArguments("NS3.C5.M5(int)", "NS.C4.M5(int)").WithLocation(7000, 11)
                );
        }

        [Fact]
        public void AliasHasPriority_01()
        {
            var source1 = @"
public class C1
{
}
";

            var comp1 = CreateCompilation(source1);
            var comp1Ref = comp1.ToMetadataReference().WithAliases(new[] { "A" });

            var externAlias = @"
extern alias A;
";
            var globalUsing1 = @"
#line 1000
global using NS;
";
            var globalUsing2 = @"
#line 2000
global using static C2;
";
            var using1 = @"
#line 3000
using NS;
";
            var using2 = @"
#line 4000
using static C2;
";

            var source2 = @"
class Program
{
    static void Main()
    {
        System.Console.WriteLine(new A.C1());
    }
}

namespace NS
{
    public class A
    {
        public class C1 {}
    }
}

class C2
{
    public class A
    {
        public class C1 {}
    }
}
";
            {
                var comp2 = CreateCompilation(externAlias + globalUsing1 + source2, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe, references: new[] { comp1Ref });

                var expected1 = new[]
                {
                    // (1000,1): hidden CS8019: Unnecessary using directive.
                    // global using NS;
                    Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "global using NS;").WithLocation(1000, 1)
                };

                CompileAndVerify(comp2, expectedOutput: @"C1").VerifyDiagnostics(expected1);

                comp2 = CreateCompilation(new[] { externAlias + source2, globalUsing1 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe, references: new[] { comp1Ref });
                CompileAndVerify(comp2, expectedOutput: @"C1").VerifyDiagnostics(expected1);

                comp2 = CreateCompilation(globalUsing1 + source2, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe, references: new[] { comp1Ref });
                CompileAndVerify(comp2, expectedOutput: @"NS.A+C1").VerifyDiagnostics();
            }
            {
                var comp2 = CreateCompilation(externAlias + globalUsing2 + source2, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe, references: new[] { comp1Ref });

                var expected1 = new[]
                {
                    // (2000,1): hidden CS8019: Unnecessary using directive.
                    // global using static C2;
                    Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "global using static C2;").WithLocation(2000, 1)
                };

                CompileAndVerify(comp2, expectedOutput: @"C1").VerifyDiagnostics(expected1);

                comp2 = CreateCompilation(new[] { externAlias + source2, globalUsing2 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe, references: new[] { comp1Ref });
                CompileAndVerify(comp2, expectedOutput: @"C1").VerifyDiagnostics(expected1);

                comp2 = CreateCompilation(globalUsing2 + source2, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe, references: new[] { comp1Ref });
                CompileAndVerify(comp2, expectedOutput: @"C2+A+C1").VerifyDiagnostics();
            }
            {
                var comp2 = CreateCompilation(externAlias + using1 + source2, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe, references: new[] { comp1Ref });
                CompileAndVerify(comp2, expectedOutput: @"C1").VerifyDiagnostics(
                    // (3000,1): hidden CS8019: Unnecessary using directive.
                    // using NS;
                    Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using NS;").WithLocation(3000, 1)
                    );

                comp2 = CreateCompilation(using1 + source2, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe, references: new[] { comp1Ref });
                CompileAndVerify(comp2, expectedOutput: @"NS.A+C1").VerifyDiagnostics();
            }
            {
                var comp2 = CreateCompilation(externAlias + using2 + source2, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe, references: new[] { comp1Ref });
                CompileAndVerify(comp2, expectedOutput: @"C1").VerifyDiagnostics(
                    // (4000,1): hidden CS8019: Unnecessary using directive.
                    // using static C2;
                    Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using static C2;").WithLocation(4000, 1)
                    );

                comp2 = CreateCompilation(using2 + source2, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe, references: new[] { comp1Ref });
                CompileAndVerify(comp2, expectedOutput: @"C2+A+C1").VerifyDiagnostics();
            }
        }

        [Fact]
        public void AliasHasPriority_02()
        {
            var globalAlias = @"
global using A = NS2;
";
            var regularAlias = @"
using A = NS2;
";
            var globalUsing1 = @"
#line 1000
global using NS;
";
            var globalUsing2 = @"
#line 2000
global using static C2;
";
            var using1 = @"
#line 3000
using NS;
";
            var using2 = @"
#line 4000
using static C2;
";

            var source2 = @"
class Program
{
    static void Main()
    {
        System.Console.WriteLine(new A.C1());
    }
}

namespace NS
{
    public class A
    {
        public class C1 {}
    }
}

class C2
{
    public class A
    {
        public class C1 {}
    }
}

namespace NS2
{
    public class C1 {}
}
";
            {
                var comp2 = CreateCompilation(globalAlias + globalUsing1 + source2, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);

                var expected1 = new[]
                {
                    // (1000,1): hidden CS8019: Unnecessary using directive.
                    // global using NS;
                    Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "global using NS;").WithLocation(1000, 1)
                };

                CompileAndVerify(comp2, expectedOutput: @"NS2.C1").VerifyDiagnostics(expected1);

                comp2 = CreateCompilation(new[] { globalAlias + source2, globalUsing1 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp2, expectedOutput: @"NS2.C1").VerifyDiagnostics(expected1);

                comp2 = CreateCompilation(new[] { source2, globalAlias + globalUsing1 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp2, expectedOutput: @"NS2.C1").VerifyDiagnostics(expected1);

                comp2 = CreateCompilation(new[] { source2, globalAlias, globalUsing1 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp2, expectedOutput: @"NS2.C1").VerifyDiagnostics(expected1);

                comp2 = CreateCompilation(new[] { source2, globalUsing1, globalAlias }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp2, expectedOutput: @"NS2.C1").VerifyDiagnostics(expected1);

                comp2 = CreateCompilation(globalUsing1 + regularAlias + source2, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp2, expectedOutput: @"NS2.C1").VerifyDiagnostics(expected1);

                comp2 = CreateCompilation(new[] { regularAlias + source2, globalUsing1 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp2, expectedOutput: @"NS2.C1").VerifyDiagnostics(expected1);

                comp2 = CreateCompilation(globalUsing1 + source2, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp2, expectedOutput: @"NS.A+C1").VerifyDiagnostics();
            }
            {
                var comp2 = CreateCompilation(globalAlias + globalUsing2 + source2, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);

                var expected1 = new[]
                {
                    // (2000,1): hidden CS8019: Unnecessary using directive.
                    // global using static C2;
                    Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "global using static C2;").WithLocation(2000, 1)
                };

                CompileAndVerify(comp2, expectedOutput: @"NS2.C1").VerifyDiagnostics(expected1);

                comp2 = CreateCompilation(new[] { globalAlias + source2, globalUsing2 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp2, expectedOutput: @"NS2.C1").VerifyDiagnostics(expected1);

                comp2 = CreateCompilation(new[] { source2, globalAlias + globalUsing2 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp2, expectedOutput: @"NS2.C1").VerifyDiagnostics(expected1);

                comp2 = CreateCompilation(new[] { source2, globalAlias, globalUsing2 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp2, expectedOutput: @"NS2.C1").VerifyDiagnostics(expected1);

                comp2 = CreateCompilation(new[] { source2, globalUsing2, globalAlias }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp2, expectedOutput: @"NS2.C1").VerifyDiagnostics(expected1);

                comp2 = CreateCompilation(globalUsing2 + regularAlias + source2, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp2, expectedOutput: @"NS2.C1").VerifyDiagnostics(expected1);

                comp2 = CreateCompilation(new[] { regularAlias + source2, globalUsing2 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp2, expectedOutput: @"NS2.C1").VerifyDiagnostics(expected1);

                comp2 = CreateCompilation(globalUsing2 + source2, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp2, expectedOutput: @"C2+A+C1").VerifyDiagnostics();
            }
            {
                var comp2 = CreateCompilation(globalAlias + using1 + source2, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);

                var expected1 = new[]
                {
                    // (3000,1): hidden CS8019: Unnecessary using directive.
                    // using NS;
                    Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using NS;").WithLocation(3000, 1)
                };

                CompileAndVerify(comp2, expectedOutput: @"NS2.C1").VerifyDiagnostics(expected1);

                comp2 = CreateCompilation(new[] { globalAlias, using1 + source2 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp2, expectedOutput: @"NS2.C1").VerifyDiagnostics(expected1);

                comp2 = CreateCompilation(regularAlias + using1 + source2, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp2, expectedOutput: @"NS2.C1").VerifyDiagnostics(expected1);

                comp2 = CreateCompilation(using1 + source2, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp2, expectedOutput: @"NS.A+C1").VerifyDiagnostics();
            }
            {
                var comp2 = CreateCompilation(globalAlias + using2 + source2, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);

                var expected1 = new[]
                {
                    // (4000,1): hidden CS8019: Unnecessary using directive.
                    // using static C2;
                    Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using static C2;").WithLocation(4000, 1)
                };

                CompileAndVerify(comp2, expectedOutput: @"NS2.C1").VerifyDiagnostics(expected1);

                comp2 = CreateCompilation(new[] { globalAlias, using2 + source2 }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp2, expectedOutput: @"NS2.C1").VerifyDiagnostics(expected1);

                comp2 = CreateCompilation(regularAlias + using2 + source2, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp2, expectedOutput: @"NS2.C1").VerifyDiagnostics(expected1);

                comp2 = CreateCompilation(using2 + source2, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
                CompileAndVerify(comp2, expectedOutput: @"C2+A+C1").VerifyDiagnostics();
            }
        }

        [Fact]
        public void ErrorRecoveryInNamespace_01()
        {
            var source = @"
namespace ns
{
    global using NS2;
    global using static C4;
    global using A2 = C5;

    using NS1;
    using static C2;
    using A1 = C3;

    class Test1
    {
        void M()
        {
#line 1000
            new NS1C1();
            M2();
            A1.M3();

            new NS2C2();
            M4();
            A2.M5();
        }
    }
}

namespace ns
{
    class Test2
    {
        void M()
        {
#line 2000
            new NS1C1();
            M2();
            A1.M3();

            new NS2C2();
            M4();
            A2.M5();
        }
    }
}

namespace NS1
{
    class NS1C1 {}
}

namespace NS2
{
    class NS2C2 {}
}

class C2
{
    public static void M2() {}
}

class C3 
{
    public static void M3() {}
}

class C4
{
    public static void M4() {}
}

class C5
{
    public static void M5() {}
}
";
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (4,5): error CS8914: A global using directive cannot be used in a namespace declaration.
                //     global using NS2;
                Diagnostic(ErrorCode.ERR_GlobalUsingInNamespace, "global").WithLocation(4, 5),
                // (2000,17): error CS0246: The type or namespace name 'NS1C1' could not be found (are you missing a using directive or an assembly reference?)
                //             new NS1C1();
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "NS1C1").WithArguments("NS1C1").WithLocation(2000, 17),
                // (2001,13): error CS0103: The name 'M2' does not exist in the current context
                //             M2();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "M2").WithArguments("M2").WithLocation(2001, 13),
                // (2002,13): error CS0103: The name 'A1' does not exist in the current context
                //             A1.M3();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "A1").WithArguments("A1").WithLocation(2002, 13),
                // (2004,17): error CS0246: The type or namespace name 'NS2C2' could not be found (are you missing a using directive or an assembly reference?)
                //             new NS2C2();
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "NS2C2").WithArguments("NS2C2").WithLocation(2004, 17),
                // (2005,13): error CS0103: The name 'M4' does not exist in the current context
                //             M4();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "M4").WithArguments("M4").WithLocation(2005, 13),
                // (2006,13): error CS0103: The name 'A2' does not exist in the current context
                //             A2.M5();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "A2").WithArguments("A2").WithLocation(2006, 13)
                );
        }

        [Fact]
        public void InvalidUsingTarget_01()
        {
            var globalUsing1 = @"
#line 1000
global using A1 = C<int>;
";
            var globalUsing2 = @"
#line 2000
global using static C<byte>;
";
            var regularUsings = @"
#line 3000
using A2 = C<long>;
using static C<short>;
";
            var source = @"
class C<T> where T : class {}
";
            var expected1 = new[]
            {
                // (1000,14): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type or method 'C<T>'
                // global using A1 = C<int>;
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "A1").WithArguments("C<T>", "T", "int").WithLocation(1000, 14),
                // (2000,21): error CS0452: The type 'byte' must be a reference type in order to use it as parameter 'T' in the generic type or method 'C<T>'
                // global using static C<byte>;
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "C<byte>").WithArguments("C<T>", "T", "byte").WithLocation(2000, 21),
                // (3000,7): error CS0452: The type 'long' must be a reference type in order to use it as parameter 'T' in the generic type or method 'C<T>'
                // using A2 = C<long>;
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "A2").WithArguments("C<T>", "T", "long").WithLocation(3000, 7),
                // (3001,14): error CS0452: The type 'short' must be a reference type in order to use it as parameter 'T' in the generic type or method 'C<T>'
                // using static C<short>;
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "C<short>").WithArguments("C<T>", "T", "short").WithLocation(3001, 14)
            };

            CreateCompilation(new[] { globalUsing1 + globalUsing2 + regularUsings + source }, parseOptions: TestOptions.RegularPreview).
                GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected1);

            CreateCompilation(new[] { globalUsing1 + regularUsings + source, globalUsing2 }, parseOptions: TestOptions.RegularPreview).
                GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected1);

            CreateCompilation(new[] { regularUsings + source, globalUsing1, globalUsing2 }, parseOptions: TestOptions.RegularPreview).
                GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected1);

            var expected2 = new[]
            {
                // (1000,14): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type or method 'C<T>'
                // global using A1 = C<int>;
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "A1").WithArguments("C<T>", "T", "int").WithLocation(1000, 14),
                // (2000,21): error CS0452: The type 'byte' must be a reference type in order to use it as parameter 'T' in the generic type or method 'C<T>'
                // global using static C<byte>;
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "C<byte>").WithArguments("C<T>", "T", "byte").WithLocation(2000, 21),
            };

            CreateCompilation(new[] { globalUsing1 + globalUsing2 + source }, parseOptions: TestOptions.RegularPreview).
                GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected2);

            CreateCompilation(new[] { globalUsing1 + source, globalUsing2 }, parseOptions: TestOptions.RegularPreview).
                GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.HDN_UnusedUsingDirective).Verify(expected2);

            CreateCompilation(new[] { regularUsings + source }, parseOptions: TestOptions.RegularPreview).
                GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.HDN_UnusedUsingDirective).Verify(
                // (3000,7): error CS0452: The type 'long' must be a reference type in order to use it as parameter 'T' in the generic type or method 'C<T>'
                // using A2 = C<long>;
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "A2").WithArguments("C<T>", "T", "long").WithLocation(3000, 7),
                // (3001,14): error CS0452: The type 'short' must be a reference type in order to use it as parameter 'T' in the generic type or method 'C<T>'
                // using static C<short>;
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "C<short>").WithArguments("C<T>", "T", "short").WithLocation(3001, 14)
                );
        }

        [Fact]
        public void GetSpeculativeAliasInfo_01()
        {
            var globalUsings1 = @"
global using alias1 = C1;

class C1 {}
";

            var source = @"
class C2 {}
";

            var comp = CreateCompilation(new[] { globalUsings1, source }, parseOptions: TestOptions.RegularPreview);
            var alias1 = SyntaxFactory.IdentifierName("alias1");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            Assert.Equal("alias1=C1", model.GetSpeculativeAliasInfo(tree.GetRoot().Span.End, alias1, SpeculativeBindingOption.BindAsExpression).ToTestDisplayString());
            Assert.Equal("alias1=C1", model.GetSpeculativeAliasInfo(tree.GetRoot().Span.End, alias1, SpeculativeBindingOption.BindAsTypeOrNamespace).ToTestDisplayString());

            tree = comp.SyntaxTrees[1];
            model = comp.GetSemanticModel(tree);
            Assert.Equal("alias1=C1", model.GetSpeculativeAliasInfo(tree.GetRoot().Span.End, alias1, SpeculativeBindingOption.BindAsExpression).ToTestDisplayString());
            Assert.Equal("alias1=C1", model.GetSpeculativeAliasInfo(tree.GetRoot().Span.End, alias1, SpeculativeBindingOption.BindAsTypeOrNamespace).ToTestDisplayString());
        }

        [Fact]
        public void GlobalAliasToType1()
        {
            var source1 = @"
global using X = int;
";
            var source2 = @"
class C
{
    X Goo(int i) => i;
}
";

            CreateCompilation(new[] { source1, source2 }, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // 0.cs(2,18): error CS9058: Feature 'using type alias' is not available in C# 11.0. Please use language version 12.0 or greater.
                // global using X = int;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "int").WithArguments("using type alias", "12.0").WithLocation(2, 18));

            CreateCompilation(new[] { source1, source2 }, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
        }

        [Fact]
        public void GlobalAliasToUnsafeType_CompilationOptionOff()
        {
            var source1 = @"
global using unsafe X = int*;
";
            var source2 = @"
class C
{
    unsafe X Goo() => default;
}
";

            CreateCompilation(new[] { source1, source2 }, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (2,14): error CS0227: Unsafe code may only appear if compiling with /unsafe
                // global using unsafe X = int*;
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "unsafe").WithLocation(2, 14),
                // (4,14): error CS0227: Unsafe code may only appear if compiling with /unsafe
                //     unsafe X Goo() => default;
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "Goo").WithLocation(4, 14));
        }

        [Fact]
        public void GlobalAliasToUnsafeType_CompilationOptionOn_CSharp11()
        {
            var source1 = @"
global using unsafe X = int*;
";
            var source2 = @"
class C
{
    unsafe X Goo() => default;
}
";

            CreateCompilation(new[] { source1, source2 }, options: TestOptions.UnsafeDebugDll, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // 0.cs(2,14): error CS9058: Feature 'using type alias' is not available in C# 11.0. Please use language version 12.0 or greater.
                // global using unsafe X = int*;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "unsafe").WithArguments("using type alias", "12.0").WithLocation(2, 14));
        }

        [Fact]
        public void GlobalAliasToUnsafeType1()
        {
            var source1 = @"
global using unsafe X = int*;
";
            var source2 = @"
class C
{
    X Goo() => default;
}
";

            CreateCompilation(new[] { source1, source2 }, options: TestOptions.UnsafeDebugDll, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (4,5): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //     X Goo() => default;
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "X").WithLocation(4, 5));
        }

        [Fact]
        public void GlobalAliasToUnsafeType2()
        {
            var source1 = @"
global using X = int*;
";
            var source2 = @"
class C
{
    unsafe X Goo() => default;
}
";

            CreateCompilation(new[] { source1, source2 }, options: TestOptions.UnsafeDebugDll, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (2,18): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                // global using X = int*;
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(2, 18));
        }

        [Fact]
        public void GlobalAliasToUnsafeType3()
        {
            var source1 = @"
global using unsafe X = int*;
";
            var source2 = @"
class C
{
    unsafe X Goo(int* p) => p;
}
";

            CreateCompilation(new[] { source1, source2 }, options: TestOptions.UnsafeDebugDll, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
        }
    }
}
