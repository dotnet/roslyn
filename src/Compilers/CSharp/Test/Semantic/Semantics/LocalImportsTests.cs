// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class LocalImportsTests : CSharpTestBase
    {
        [Fact]
        public void TestLocalImport()
        {
            var source = @"
class Program
{   
    public static void Main()
    {
        using System;
        Console.Write(2);
    }
}
";

            CompileAndVerify(source, expectedOutput: "2");
        }

        [Fact]
        public void TestLocalStaticImport_1()
        {
            var source = @"
class Program
{   
    public static void Main()
    {
        using static System.Console;
        Write(2);
    }
}
";

            CompileAndVerify(source, expectedOutput: "2");
        }
        [Fact]
        public void TestLocalStaticImport_2()
        {
            var source = @"
using System;
class Program
{   
    public static void Main()
    {
        using static Console;
        Write(2);
    }
}
";

            CompileAndVerify(source, expectedOutput: "2");
        }

        [Fact]
        public void TestLocalStaticImport_3()
        {
            var source =
@"using static C1; // global
namespace Namespace
{
    using static C2; // namespace
    class Class
    {
        public static void Main()
        {
            using static C3; // method
            void Local()
            {
                using static C4; // local function
                {
                    using static C5; // block
                    System.Action action = () =>
                    {
                        using static C6; // lambda
                        {
                            using static C7; 
                            M();
                        }
                    };
                    action();
                }
            }
            Local();
        }
    }
}";
            var member = @"
public static void M()
{
    System.Console.Write(1);
}";

            var classes = Enumerable.Range(1, 7).Reverse()
                .Aggregate(member, (body, i) => $"public static class C{i} {{{body}}}");

            CompileAndVerify(source + classes, expectedOutput: "1");
        }

        [Fact]
        public void TestLocalImport_ExtensionMethod()
        {
            var source = @"
class Program
{   
    public static void Main()
    {
        using System;
        using System.Linq;
        foreach (var item in new [] {1}.Select(x => x * 2))
        {
            Console.Write(item);
        }
    }
}
";

            CompileAndVerify(source, expectedOutput: "2", additionalRefs: new[] { LinqAssemblyRef });
        }

        [Fact]
        public void TestLocalImport_AfterStatement()
        {
            var source = @"
class Program
{   
    public static void Main()
    {
        using (null) {}
        using System;
    }
}
";
            // TODO(local-imports): needs better diagnostic

            var comp = CreateStandardCompilation(source);
            comp.VerifyDiagnostics(
                // (7,9): error CS1529: A using clause must precede all other elements defined in the namespace except extern alias declarations
                //         using System;
                Diagnostic(ErrorCode.ERR_UsingAfterElements, "using System;").WithLocation(7, 9)
                );
        }

        [Fact]
        public void TestLocalImport_SwitchSection()
        {
            var source = @"
class Program
{   
    public static void Main()
    {
        int i = 0;
        switch (i)
        {
            case 1:
                using System;
                break;
        }

        switch (i)
        {
            case 1:
                System.Console.WriteLine();
                using System;
                break;
        }
    }
}
";
            // TODO(local-imports): needs better diagnostic

            var comp = CreateStandardCompilation(source);
            comp.VerifyDiagnostics(
                // (10,17): error CS1529: A using clause must precede all other elements defined in the namespace except extern alias declarations
                //                 using System;
                Diagnostic(ErrorCode.ERR_UsingAfterElements, "using System;").WithLocation(10, 17),
                // (18,17): error CS1529: A using clause must precede all other elements defined in the namespace except extern alias declarations
                //                 using System;
                Diagnostic(ErrorCode.ERR_UsingAfterElements, "using System;").WithLocation(18, 17)
                );
        }
    }
}
