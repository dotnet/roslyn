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
        public void TestExtensionMethod()
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
        public void TestAfterStatement()
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

            CreateStandardCompilation(source)
                .VerifyDiagnostics(
                // (7,15): error CS1003: Syntax error, '(' expected
                //         using System;
                Diagnostic(ErrorCode.ERR_SyntaxError, "System").WithArguments("(", "").WithLocation(7, 15),
                // (7,21): error CS1026: ) expected
                //         using System;
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ";").WithLocation(7, 21),
                // (7,15): error CS0118: 'System' is a namespace but is used like a variable
                //         using System;
                Diagnostic(ErrorCode.ERR_BadSKknown, "System").WithArguments("System", "namespace", "variable").WithLocation(7, 15),
                // (7,21): warning CS0642: Possible mistaken empty statement
                //         using System;
                Diagnostic(ErrorCode.WRN_PossibleMistakenNullStatement, ";").WithLocation(7, 21)
                );
        }
    }
}
