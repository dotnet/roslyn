
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [WorkItem(275, "https://github.com/dotnet/csharplang/issues/275")]
    [CompilerTrait(CompilerFeature.AnonymousFunctions)]
    public class AnonymousFunctionTests : CSharpTestBase
    {
        public static CSharpCompilation VerifyInPreview(string source, params DiagnosticDescription[] expected)
            => CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(expected);
        internal CompilationVerifier VerifyInPreview(CSharpTestSource source, string expectedOutput, Action<ModuleSymbol>? symbolValidator = null, params DiagnosticDescription[] expected)
            => CompileAndVerify(
                    source,
                    options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All),
                    parseOptions: TestOptions.RegularPreview,
                    symbolValidator: symbolValidator,
                    expectedOutput: expectedOutput)
                .VerifyDiagnostics(expected);

        internal void VerifyInPreview(string source, string expectedOutput, string metadataName, string expectedIL)
        {
            verify(source);
            verify(source.Replace("static (", "("));

            void verify(string source)
            {
                var verifier = CompileAndVerify(
                        source,
                        options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All),
                        parseOptions: TestOptions.RegularPreview,
                        symbolValidator: symbolValidator,
                        expectedOutput: expectedOutput)
                    .VerifyDiagnostics();

                verifier.VerifyIL(metadataName, expectedIL);
            }

            void symbolValidator(ModuleSymbol module)
            {
                var method = module.GlobalNamespace.GetMember<MethodSymbol>(metadataName);
                // note that static anonymous functions do not guarantee that the lowered method will be static.
                Assert.False(method.IsStatic);
            }
        }

        [Fact]
        public void DisallowInNonPreview()
        {
            var source = @"
using System;

public class C
{
    public static int a;

    public void F()
    {
        Func<int> f = static () => a;
    }
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular8).VerifyDiagnostics(
                // (10,23): error CS8400: Feature 'static anonymous function' is not available in C# 8.0. Please use language version 9.0 or greater.
                //         Func<int> f = static () => a;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "static").WithArguments("static anonymous function", "9.0").WithLocation(10, 23));
        }

        [Fact]
        public void StaticLambdaCanReferenceStaticField()
        {
            var source = @"
using System;

public class C
{
    public static int a;

    public static void Main()
    {
        Func<int> f = static () => a;
        a = 42;
        Console.Write(f());
    }
}";
            VerifyInPreview(
                source,
                expectedOutput: "42",
                metadataName: "C.<>c.<Main>b__1_0",
                expectedIL: @"
{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  ldsfld     ""int C.a""
  IL_0005:  ret
}
");
        }

        [Fact]
        public void StaticLambdaCanReferenceStaticProperty()
        {
            var source = @"
using System;

public class C
{
    static int A { get; set; }

    public static void Main()
    {
        Func<int> f = static () => A;
        A = 42;
        Console.Write(f());
    }
}";
            VerifyInPreview(
                source,
                expectedOutput: "42",
                metadataName: "C.<>c.<Main>b__4_0",
                expectedIL: @"
{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  call       ""int C.A.get""
  IL_0005:  ret
}");
        }

        [Fact]
        public void StaticLambdaCanReferenceConstField()
        {
            var source = @"
using System;

public class C
{
    public const int a = 42;

    public static void Main()
    {
        Func<int> f = static () => a;
        Console.Write(f());
    }
}";
            VerifyInPreview(
                source,
                expectedOutput: "42",
                metadataName: "C.<>c.<Main>b__1_0",
                expectedIL: @"
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldc.i4.s   42
  IL_0002:  ret
}");
        }

        [Fact]
        public void StaticLambdaCanReferenceConstLocal()
        {
            var source = @"
using System;

public class C
{
    public static void Main()
    {
        const int a = 42;
        Func<int> f = static () => a;
        Console.Write(f());
    }
}";
            VerifyInPreview(
                source,
                expectedOutput: "42",
                metadataName: "C.<>c.<Main>b__0_0",
                expectedIL: @"
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldc.i4.s   42
  IL_0002:  ret
}");
        }

        [Fact]
        public void StaticLambdaCanReturnConstLocal()
        {
            var source = @"
using System;

public class C
{
    public static void Main()
    {
        Func<int> f = static () =>
        {
            const int a = 42;
            return a;
        };
        Console.Write(f());
    }
}";
            VerifyInPreview(source, expectedOutput: "42", metadataName: "C.<>c.<Main>b__0_0", expectedIL: @"
{
  // Code size        8 (0x8)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  nop
  IL_0001:  ldc.i4.s   42
  IL_0003:  stloc.0
  IL_0004:  br.s       IL_0006
  IL_0006:  ldloc.0
  IL_0007:  ret
}");
        }

        [Fact]
        public void StaticLambdaCannotCaptureInstanceField()
        {
            var source = @"
using System;

public class C
{
    public int a;

    public void F()
    {
        Func<int> f = static () => a;
    }
}";
            VerifyInPreview(source,
                // (10,36): error CS8428: A static anonymous function cannot contain a reference to 'this' or 'base'.
                //         Func<int> f = static () => a;
                Diagnostic(ErrorCode.ERR_StaticAnonymousFunctionCannotCaptureThis, "a").WithLocation(10, 36));
        }

        [Fact]
        public void StaticLambdaCannotCaptureInstanceProperty()
        {
            var source = @"
using System;

public class C
{
    int A { get; }

    public void F()
    {
        Func<int> f = static () => A;
    }
}";
            VerifyInPreview(source,
                // (10,36): error CS8428: A static anonymous function cannot contain a reference to 'this' or 'base'.
                //         Func<int> f = static () => A;
                Diagnostic(ErrorCode.ERR_StaticAnonymousFunctionCannotCaptureThis, "A").WithLocation(10, 36));
        }

        [Fact]
        public void StaticLambdaCannotCaptureParameter()
        {
            var source = @"
using System;

public class C
{
    public void F(int a)
    {
        Func<int> f = static () => a;
    }
}";
            VerifyInPreview(source,
                // (8,36): error CS8427: A static anonymous function cannot contain a reference to 'a'.
                //         Func<int> f = static () => a;
                Diagnostic(ErrorCode.ERR_StaticAnonymousFunctionCannotCaptureVariable, "a").WithArguments("a").WithLocation(8, 36));
        }

        [Fact]
        public void StaticLambdaCannotCaptureOuterLocal()
        {
            var source = @"
using System;

public class C
{
    public void F()
    {
        int a;
        Func<int> f = static () => a;
    }
}";
            VerifyInPreview(source,
                // (9,36): error CS8427: A static anonymous function cannot contain a reference to 'a'.
                //         Func<int> f = static () => a;
                Diagnostic(ErrorCode.ERR_StaticAnonymousFunctionCannotCaptureVariable, "a").WithArguments("a").WithLocation(9, 36),
                // (9,36): error CS0165: Use of unassigned local variable 'a'
                //         Func<int> f = static () => a;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "a").WithArguments("a").WithLocation(9, 36));
        }

        [Fact]
        public void StaticLambdaCanReturnInnerLocal()
        {
            var source = @"
using System;

public class C
{
    public static void Main()
    {
        Func<int> f = static () =>
        {
            int a = 42;
            return a;
        };
        Console.Write(f());
    }
}";
            VerifyInPreview(
                source,
                expectedOutput: "42",
                metadataName: "C.<>c.<Main>b__0_0",
                expectedIL: @"
{
  // Code size       10 (0xa)
  .maxstack  1
  .locals init (int V_0, //a
                int V_1)
  IL_0000:  nop
  IL_0001:  ldc.i4.s   42
  IL_0003:  stloc.0
  IL_0004:  ldloc.0
  IL_0005:  stloc.1
  IL_0006:  br.s       IL_0008
  IL_0008:  ldloc.1
  IL_0009:  ret
}");
        }

        [Fact]
        public void StaticLambdaCannotReferenceThis()
        {
            var source = @"
using System;

public class C
{
    public void F()
    {
        Func<int> f = static () =>
        {
            this.F();
            return 0;
        };
    }
}";
            VerifyInPreview(source,
                // (10,13): error CS8428: A static anonymous function cannot contain a reference to 'this' or 'base'.
                //             this.F();
                Diagnostic(ErrorCode.ERR_StaticAnonymousFunctionCannotCaptureThis, "this").WithLocation(10, 13));
        }

        [Fact]
        public void StaticLambdaCannotReferenceBase()
        {
            var source = @"
using System;

public class B
{
    public virtual void F() { }
}

public class C : B
{
    public override void F()
    {
        Func<int> f = static () =>
        {
            base.F();
            return 0;
        };
    }
}";
            VerifyInPreview(source,
                // (15,13): error CS8428: A static anonymous function cannot contain a reference to 'this' or 'base'.
                //             base.F();
                Diagnostic(ErrorCode.ERR_StaticAnonymousFunctionCannotCaptureThis, "base").WithLocation(15, 13));
        }

        [Fact]
        public void StaticLambdaCannotReferenceInstanceLocalFunction()
        {
            var source = @"
using System;

public class C
{
    public void F()
    {
        Func<int> f = static () =>
        {
            F();
            return 0;
        };

        void F() {}
    }
}";
            VerifyInPreview(source,
                // (10,13): error CS8427: A static anonymous function cannot contain a reference to 'F'.
                //             F();
                Diagnostic(ErrorCode.ERR_StaticAnonymousFunctionCannotCaptureVariable, "F()").WithArguments("F").WithLocation(10, 13));
        }

        [Fact]
        public void StaticLambdaCanReferenceStaticLocalFunction()
        {
            var source = @"
using System;

public class C
{
    public static void Main()
    {
        Func<int> f = static () => local();
        Console.WriteLine(f());

        static int local() => 42;
    }
}";
            VerifyInPreview(source, expectedOutput: "42", metadataName: "C.<>c.<Main>b__0_0", expectedIL: @"
{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  call       ""int C.<Main>g__local|0_1()""
  IL_0005:  ret
}");
        }

        [Fact]
        public void StaticLambdaCanHaveLocalsCapturedByInnerInstanceLambda()
        {
            var source = @"
using System;

public class C
{
    public static void Main()
    {
        Func<int> f = static () =>
        {
            int i = 42;
            Func<int> g = () => i;
            return g();
        };

        Console.Write(f());
    }
}";
            VerifyInPreview(source, expectedOutput: "42", metadataName: "C.<>c.<Main>b__0_0", expectedIL: @"
{
  // Code size       39 (0x27)
  .maxstack  2
  .locals init (C.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                System.Func<int> V_1, //g
                int V_2)
  IL_0000:  newobj     ""C.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  nop
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.s   42
  IL_000a:  stfld      ""int C.<>c__DisplayClass0_0.i""
  IL_000f:  ldloc.0
  IL_0010:  ldftn      ""int C.<>c__DisplayClass0_0.<Main>b__1()""
  IL_0016:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_001b:  stloc.1
  IL_001c:  ldloc.1
  IL_001d:  callvirt   ""int System.Func<int>.Invoke()""
  IL_0022:  stloc.2
  IL_0023:  br.s       IL_0025
  IL_0025:  ldloc.2
  IL_0026:  ret
}");
        }

        [Fact]
        public void StaticLambdaCannotHaveLocalsCapturedByInnerStaticLambda()
        {
            var source = @"
using System;

public class C
{
    public void F()
    {
        Func<int> f = static () =>
        {
            int i = 0;
            Func<int> g = static () => i;
            return 0;
        };
    }
}";
            VerifyInPreview(source,
                // (11,40): error CS8427: A static anonymous function cannot contain a reference to 'i'.
                //             Func<int> g = static () => i;
                Diagnostic(ErrorCode.ERR_StaticAnonymousFunctionCannotCaptureVariable, "i").WithArguments("i").WithLocation(11, 40));
        }

        [Fact]
        public void InstanceLambdaCannotHaveLocalsCapturedByInnerStaticLambda()
        {
            var source = @"
using System;

public class C
{
    public void F()
    {
        Func<int> f = () =>
        {
            int i = 0;
            Func<int> g = static () => i;
            return 0;
        };
    }
}";
            VerifyInPreview(source,
                // (11,40): error CS8427: A static anonymous function cannot contain a reference to 'i'.
                //             Func<int> g = static () => i;
                Diagnostic(ErrorCode.ERR_StaticAnonymousFunctionCannotCaptureVariable, "i").WithArguments("i").WithLocation(11, 40));
        }

        [Fact]
        public void StaticLambdaCanHaveLocalsCapturedByInnerInstanceLocalFunction()
        {
            var source = @"
using System;

public class C
{
    public static void Main()
    {
        Func<int> f = static () =>
        {
            int i = 42;
            int g() => i;
            return g();
        };

        Console.Write(f());
    }
}";
            VerifyInPreview(source, expectedOutput: "42", metadataName: "C.<>c.<Main>b__0_0", expectedIL: @"
{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (C.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                int V_1)
  IL_0000:  nop
  IL_0001:  ldloca.s   V_0
  IL_0003:  ldc.i4.s   42
  IL_0005:  stfld      ""int C.<>c__DisplayClass0_0.i""
  IL_000a:  nop
  IL_000b:  ldloca.s   V_0
  IL_000d:  call       ""int C.<Main>g__g|0_1(ref C.<>c__DisplayClass0_0)""
  IL_0012:  stloc.1
  IL_0013:  br.s       IL_0015
  IL_0015:  ldloc.1
  IL_0016:  ret
}");
        }

        [Fact]
        public void StaticLambdaCannotHaveLocalsCapturedByInnerStaticLocalFunction()
        {
            var source = @"
using System;

public class C
{
    public void F()
    {
        Func<int> f = static () =>
        {
            int i = 0;
            static int g() => i;
            return g();
        };
    }
}";
            VerifyInPreview(source,
                // (11,31): error CS8421: A static local function cannot contain a reference to 'i'.
                //             static int g() => i;
                Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureVariable, "i").WithArguments("i").WithLocation(11, 31));
        }

        [Fact]
        public void InstanceLambdaCannotHaveLocalsCapturedByInnerStaticLocalFunction()
        {
            var source = @"
using System;

public class C
{
    public void F()
    {
        Func<int> f = () =>
        {
            int i = 0;
            static int g() => i;
            return g();
        };
    }
}";
            VerifyInPreview(source,
                // (11,31): error CS8421: A static local function cannot contain a reference to 'i'.
                //             static int g() => i;
                Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureVariable, "i").WithArguments("i").WithLocation(11, 31));
        }

        [Fact]
        public void StaticLocalFunctionCanHaveLocalsCapturedByInnerInstanceLocalFunction()
        {
            var source = @"
using System;

public class C
{
    public static void Main()
    {
        static int f()
        {
            int i = 42;
            int g() => i;
            return g();
        };

        Console.Write(f());
    }
}";
            var verifier = VerifyInPreview(
                source,
                expectedOutput: "42",
                symbolValidator);

            const string metadataName = "C.<Main>g__f|0_0";
            if (RuntimeUtilities.IsCoreClrRuntime)
            {
                verifier.VerifyIL(metadataName, @"
{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (C.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                int V_1)
  IL_0000:  nop
  IL_0001:  ldloca.s   V_0
  IL_0003:  ldc.i4.s   42
  IL_0005:  stfld      ""int C.<>c__DisplayClass0_0.i""
  IL_000a:  nop
  IL_000b:  ldloca.s   V_0
  IL_000d:  call       ""int C.<Main>g__g|0_1(ref C.<>c__DisplayClass0_0)""
  IL_0012:  stloc.1
  IL_0013:  br.s       IL_0015
  IL_0015:  ldloc.1
  IL_0016:  ret
}");
            }

            void symbolValidator(ModuleSymbol module)
            {
                var method = module.GlobalNamespace.GetMember<MethodSymbol>(metadataName);
                Assert.True(method.IsStatic);
            }
        }

        [Fact]
        public void StaticLocalFunctionCannotHaveLocalsCapturedByInnerStaticLocalFunction()
        {
            var source = @"


public class C
{
    public void F()
    {
        static int f()
        {
            int i = 0;
            static int g() => i;
            return g();
        };
    }
}";
            VerifyInPreview(source,
                // (8,20): warning CS8321: The local function 'f' is declared but never used
                //         static int f()
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "f").WithArguments("f").WithLocation(8, 20),
                // (11,31): error CS8421: A static local function cannot contain a reference to 'i'.
                //             static int g() => i;
                Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureVariable, "i").WithArguments("i").WithLocation(11, 31));
        }

        [Fact]
        public void InstanceLocalFunctionCannotHaveLocalsCapturedByInnerStaticLocalFunction()
        {
            var source = @"


public class C
{
    public void F()
    {
        int f()
        {
            int i = 0;
            static int g() => i;
            return g();
        };
    }
}";
            VerifyInPreview(source,
                // (8,13): warning CS8321: The local function 'f' is declared but never used
                //         int f()
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "f").WithArguments("f").WithLocation(8, 13),
                // (11,31): error CS8421: A static local function cannot contain a reference to 'i'.
                //             static int g() => i;
                Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureVariable, "i").WithArguments("i").WithLocation(11, 31));
        }

        [Fact]
        public void StaticLocalFunctionCanHaveLocalsCapturedByInnerInstanceLambda()
        {
            var source = @"
using System;

public class C
{
    public void F()
    {
        static int f()
        {
            int i = 0;
            Func<int> g = () => i;
            return g();
        };
    }
}";
            VerifyInPreview(source,
                // (8,20): warning CS8321: The local function 'f' is declared but never used
                //         static int f()
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "f").WithArguments("f").WithLocation(8, 20));
        }

        [Fact]
        public void StaticLocalFunctionCannotHaveLocalsCapturedByInnerStaticLambda()
        {
            var source = @"
using System;

public class C
{
    public void F()
    {
        static int f()
        {
            int i = 0;
            Func<int> g = static () => i;
            return g();
        };
    }
}";
            VerifyInPreview(source,
                // (8,20): warning CS8321: The local function 'f' is declared but never used
                //         static int f()
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "f").WithArguments("f").WithLocation(8, 20),
                // (11,40): error CS8427: A static anonymous function cannot contain a reference to 'i'.
                //             Func<int> g = static () => i;
                Diagnostic(ErrorCode.ERR_StaticAnonymousFunctionCannotCaptureVariable, "i").WithArguments("i").WithLocation(11, 40));
        }

        [Fact]
        public void InstanceLocalFunctionCannotHaveLocalsCapturedByInnerStaticLambda()
        {
            var source = @"
using System;

public class C
{
    public void F()
    {
        int f()
        {
            int i = 0;
            Func<int> g = static () => i;
            return g();
        };
    }
}";
            VerifyInPreview(source,
                // (8,13): warning CS8321: The local function 'f' is declared but never used
                //         int f()
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "f").WithArguments("f").WithLocation(8, 13),
                // (11,40): error CS8427: A static anonymous function cannot contain a reference to 'i'.
                //             Func<int> g = static () => i;
                Diagnostic(ErrorCode.ERR_StaticAnonymousFunctionCannotCaptureVariable, "i").WithArguments("i").WithLocation(11, 40));
        }

        [Fact]
        public void StaticLambdaCanCallStaticMethod()
        {
            var source = @"
using System;

public class C
{
    public static void Main()
    {
        Func<int> f = static () => M();
        Console.Write(f());
    }

    static int M() => 42;
}";
            VerifyInPreview(source, expectedOutput: "42", metadataName: "C.<>c.<Main>b__0_0", expectedIL: @"
{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  call       ""int C.M()""
  IL_0005:  ret
}
");
        }

        [Fact]
        public void QueryInStaticLambdaCannotAccessThis()
        {
            var source = @"
using System;
using System.Linq;

public class C
{
    public static string[] args;

    public void F()
    {
        Func<int> f = static () =>
        {
            var q = from a in args
                    select M(a);
            return 0;
        };
    }

    int M(string a) => 0;
}";
            VerifyInPreview(source,
                // (14,28): error CS8428: A static anonymous function cannot contain a reference to 'this' or 'base'.
                //                     select M(a);
                Diagnostic(ErrorCode.ERR_StaticAnonymousFunctionCannotCaptureThis, "M").WithLocation(14, 28));
        }

        [Fact]
        public void QueryInStaticLambdaCanReferenceStatic()
        {
            var source = @"
using System;
using System.Linq;
using System.Collections.Generic;

public class C
{
    public static string[] args;

    public static void Main()
    {
        args = new[] { """" };
        Func<IEnumerable<int>> f = static () =>
        {
            var q = from a in args
                    select M(a);
            return q;
        };

        foreach (var x in f())
        {
            Console.Write(x);
        }
    }

    static int M(string a) => 42;
}";
            VerifyInPreview(source, expectedOutput: "42", metadataName: "C.<>c.<Main>b__1_0", expectedIL: @"
{
  // Code size       49 (0x31)
  .maxstack  3
  .locals init (System.Collections.Generic.IEnumerable<int> V_0, //q
                System.Collections.Generic.IEnumerable<int> V_1)
  IL_0000:  nop
  IL_0001:  ldsfld     ""string[] C.args""
  IL_0006:  ldsfld     ""System.Func<string, int> C.<>c.<>9__1_1""
  IL_000b:  dup
  IL_000c:  brtrue.s   IL_0025
  IL_000e:  pop
  IL_000f:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0014:  ldftn      ""int C.<>c.<Main>b__1_1(string)""
  IL_001a:  newobj     ""System.Func<string, int>..ctor(object, System.IntPtr)""
  IL_001f:  dup
  IL_0020:  stsfld     ""System.Func<string, int> C.<>c.<>9__1_1""
  IL_0025:  call       ""System.Collections.Generic.IEnumerable<int> System.Linq.Enumerable.Select<string, int>(System.Collections.Generic.IEnumerable<string>, System.Func<string, int>)""
  IL_002a:  stloc.0
  IL_002b:  ldloc.0
  IL_002c:  stloc.1
  IL_002d:  br.s       IL_002f
  IL_002f:  ldloc.1
  IL_0030:  ret
}
");
        }

        [Fact]
        public void InstanceLambdaInStaticLambdaCannotReferenceThis()
        {
            var source = @"
using System;

public class C
{
    public void F()
    {
        Func<int> f = static () =>
        {
            Func<int> g = () =>
            {
                this.F();
                return 0;
            };

            return g();
        };
    }
}";
            VerifyInPreview(source,
                // (12,17): error CS8428: A static anonymous function cannot contain a reference to 'this' or 'base'.
                //                 this.F();
                Diagnostic(ErrorCode.ERR_StaticAnonymousFunctionCannotCaptureThis, "this").WithLocation(12, 17));
        }

        [Fact]
        public void TestStaticAnonymousFunctions()
        {
            var source = @"
using System;

public class C
{
    public void F()
    {
        Action<int> a = static delegate(int i) { };
        Action<int> b = static a => { };
        Action<int> c = static (a) => { };
    }
}";
            var compilation = CreateCompilation(source);
            var syntaxTree = compilation.SyntaxTrees.Single();

            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = syntaxTree.GetRoot();

            var anonymousMethodSyntax = root.DescendantNodes().OfType<AnonymousMethodExpressionSyntax>().Single();
            var simpleLambdaSyntax = root.DescendantNodes().OfType<SimpleLambdaExpressionSyntax>().Single();
            var parenthesizedLambdaSyntax = root.DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().Single();

            var anonymousMethod = (IMethodSymbol)semanticModel.GetSymbolInfo(anonymousMethodSyntax).Symbol!;
            var simpleLambda = (IMethodSymbol)semanticModel.GetSymbolInfo(simpleLambdaSyntax).Symbol!;
            var parenthesizedLambda = (IMethodSymbol)semanticModel.GetSymbolInfo(parenthesizedLambdaSyntax).Symbol!;

            Assert.True(anonymousMethod.IsStatic);
            Assert.True(simpleLambda.IsStatic);
            Assert.True(parenthesizedLambda.IsStatic);
        }

        [Fact]
        public void TestNonStaticAnonymousFunctions()
        {
            var source = @"
using System;

public class C
{
    public void F()
    {
        Action<int> a = delegate(int i) { };
        Action<int> b = a => { };
        Action<int> c = (a) => { };
    }
}";
            var compilation = CreateCompilation(source);
            var syntaxTree = compilation.SyntaxTrees.Single();

            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = syntaxTree.GetRoot();

            var anonymousMethodSyntax = root.DescendantNodes().OfType<AnonymousMethodExpressionSyntax>().Single();
            var simpleLambdaSyntax = root.DescendantNodes().OfType<SimpleLambdaExpressionSyntax>().Single();
            var parenthesizedLambdaSyntax = root.DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().Single();

            var anonymousMethod = (IMethodSymbol)semanticModel.GetSymbolInfo(anonymousMethodSyntax).Symbol!;
            var simpleLambda = (IMethodSymbol)semanticModel.GetSymbolInfo(simpleLambdaSyntax).Symbol!;
            var parenthesizedLambda = (IMethodSymbol)semanticModel.GetSymbolInfo(parenthesizedLambdaSyntax).Symbol!;

            Assert.False(anonymousMethod.IsStatic);
            Assert.False(simpleLambda.IsStatic);
            Assert.False(parenthesizedLambda.IsStatic);
        }

        [Fact]
        public void TestStaticLambdaCallArgument()
        {
            var source = @"
using System;

public class C
{
    public static void F(Func<string> fn)
    {
        Console.WriteLine(fn());
    }

    public static void Main()
    {
        F(static () => ""hello"");
    }
}";
            VerifyInPreview(source, expectedOutput: "hello", metadataName: "C.<>c.<Main>b__1_0", expectedIL: @"
{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  ldstr      ""hello""
  IL_0005:  ret
}");
        }

        [Fact]
        public void TestStaticLambdaIndexerArgument()
        {
            var source = @"
using System;

public class C
{
    public object this[Func<object> fn]
    {
        get
        {
            Console.WriteLine(fn());
            return null;
        }
    }

    public static void Main()
    {
        _ = new C()[static () => ""hello""];
    }
}";
            VerifyInPreview(source, expectedOutput: "hello", metadataName: "C.<>c.<Main>b__2_0", expectedIL: @"
{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  ldstr      ""hello""
  IL_0005:  ret
}");
        }

        [Fact]
        public void TestStaticDelegateCallArgument()
        {
            var source = @"
using System;

public class C
{
    public static void F(Func<string> fn)
    {
        Console.WriteLine(fn());
    }

    public static void Main()
    {
        F(static delegate() { return ""hello""; });
    }
}";
            VerifyInPreview(source, expectedOutput: "hello", metadataName: "C.<>c.<Main>b__1_0", expectedIL: @"
{
  // Code size       11 (0xb)
  .maxstack  1
  .locals init (string V_0)
  IL_0000:  nop
  IL_0001:  ldstr      ""hello""
  IL_0006:  stloc.0
  IL_0007:  br.s       IL_0009
  IL_0009:  ldloc.0
  IL_000a:  ret
}");
        }

        [Fact]
        public void StaticLambdaNameof()
        {
            var source = @"
using System;

public class C
{
    public int w;
    public static int x;

    public static void F(Func<int, string> fn)
    {
        Console.WriteLine(fn(0));
    }

    public static void Main()
    {
        int y = 0;
        F(static (int z) => { return nameof(w) + nameof(x) + nameof(y) + nameof(z); });
    }
}";
            VerifyInPreview(source, expectedOutput: "wxyz", metadataName: "C.<>c.<Main>b__3_0", expectedIL: @"
{
  // Code size       11 (0xb)
  .maxstack  1
  .locals init (string V_0)
  IL_0000:  nop
  IL_0001:  ldstr      ""wxyz""
  IL_0006:  stloc.0
  IL_0007:  br.s       IL_0009
  IL_0009:  ldloc.0
  IL_000a:  ret
}");
        }

        [Fact]
        public void StaticLambdaTypeParams()
        {
            var source = @"
using System;

public class C<T>
{
    public static void F(Func<int, string> fn)
    {
        Console.WriteLine(fn(0));
    }

    public static void M<U>()
    {
        F(static (int x) => { return default(T).ToString() + default(U).ToString(); });
    }
}

public class Program
{
    public static void Main()
    {
        C<int>.M<bool>();
    }
}";
            verify(source);
            verify(source.Replace("static (", "("));

            void verify(string source)
            {
                var verifier = CompileAndVerify(
                        source,
                        options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All),
                        parseOptions: TestOptions.Regular9,
                        symbolValidator: symbolValidator,
                        expectedOutput: "0False")
                    .VerifyDiagnostics();

                verifier.VerifyIL("C<T>.<>c__1<U>.<M>b__1_0", @"
{
    // Code size       51 (0x33)
    .maxstack  3
    .locals init (T V_0,
                U V_1,
                string V_2)
    IL_0000:  nop
    IL_0001:  ldloca.s   V_0
    IL_0003:  dup
    IL_0004:  initobj    ""T""
    IL_000a:  constrained. ""T""
    IL_0010:  callvirt   ""string object.ToString()""
    IL_0015:  ldloca.s   V_1
    IL_0017:  dup
    IL_0018:  initobj    ""U""
    IL_001e:  constrained. ""U""
    IL_0024:  callvirt   ""string object.ToString()""
    IL_0029:  call       ""string string.Concat(string, string)""
    IL_002e:  stloc.2
    IL_002f:  br.s       IL_0031
    IL_0031:  ldloc.2
    IL_0032:  ret
}");
            }

            void symbolValidator(ModuleSymbol module)
            {
                var method = module.GlobalNamespace.GetMember<MethodSymbol>("C.<>c__1.<M>b__1_0");
                // note that static anonymous functions do not guarantee that the lowered method will be static.
                Assert.False(method.IsStatic);
            }
        }

        [Fact]
        public void StaticLambda_Nint()
        {
            var source = @"
using System;

local(static x => x + 1);

void local(Func<nint, nint> fn)
{
    Console.WriteLine(fn(0));
}";
            VerifyInPreview(source, expectedOutput: "1", metadataName: WellKnownMemberNames.TopLevelStatementsEntryPointTypeName + ".<>c.<" + WellKnownMemberNames.TopLevelStatementsEntryPointMethodName + ">b__0_0", expectedIL: @"
{
  // Code size        5 (0x5)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.1
  IL_0002:  conv.i
  IL_0003:  add
  IL_0004:  ret
}");
        }

        [Fact]
        public void StaticLambda_ExpressionTree()
        {
            var source = @"
using System;
using System.Linq.Expressions;

class C
{
    static void Main()
    {
        local(static x => x + 1);

        static void local(Expression<Func<int, int>> fn)
        {
            Console.WriteLine(fn.Compile()(0));
        }
    }
}";
            var verifier = VerifyInPreview(source, expectedOutput: "1");
            verifier.VerifyIL("C.Main", @"
{
  // Code size       72 (0x48)
  .maxstack  5
  .locals init (System.Linq.Expressions.ParameterExpression V_0)
  IL_0000:  nop
  IL_0001:  ldtoken    ""int""
  IL_0006:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000b:  ldstr      ""x""
  IL_0010:  call       ""System.Linq.Expressions.ParameterExpression System.Linq.Expressions.Expression.Parameter(System.Type, string)""
  IL_0015:  stloc.0
  IL_0016:  ldloc.0
  IL_0017:  ldc.i4.1
  IL_0018:  box        ""int""
  IL_001d:  ldtoken    ""int""
  IL_0022:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0027:  call       ""System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)""
  IL_002c:  call       ""System.Linq.Expressions.BinaryExpression System.Linq.Expressions.Expression.Add(System.Linq.Expressions.Expression, System.Linq.Expressions.Expression)""
  IL_0031:  ldc.i4.1
  IL_0032:  newarr     ""System.Linq.Expressions.ParameterExpression""
  IL_0037:  dup
  IL_0038:  ldc.i4.0
  IL_0039:  ldloc.0
  IL_003a:  stelem.ref
  IL_003b:  call       ""System.Linq.Expressions.Expression<System.Func<int, int>> System.Linq.Expressions.Expression.Lambda<System.Func<int, int>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_0040:  call       ""void C.<Main>g__local|0_1(System.Linq.Expressions.Expression<System.Func<int, int>>)""
  IL_0045:  nop
  IL_0046:  nop
  IL_0047:  ret
}");
        }

        [Fact]
        public void StaticLambda_FunctionPointer_01()
        {
            var source = @"
class C
{
    unsafe void M()
    {
        delegate*<void> ptr = &static () => { };
        ptr();
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeDebugDll, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (6,32): error CS1525: Invalid expression term 'static'
                //         delegate*<void> ptr = &static () => { };
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "static").WithArguments("static").WithLocation(6, 32),
                // (6,32): error CS1002: ; expected
                //         delegate*<void> ptr = &static () => { };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "static").WithLocation(6, 32),
                // (6,32): error CS0106: The modifier 'static' is not valid for this item
                //         delegate*<void> ptr = &static () => { };
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "static").WithArguments("static").WithLocation(6, 32),
                // (6,40): error CS8124: Tuple must contain at least two elements.
                //         delegate*<void> ptr = &static () => { };
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(6, 40),
                // (6,42): error CS1001: Identifier expected
                //         delegate*<void> ptr = &static () => { };
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "=>").WithLocation(6, 42),
                // (6,42): error CS1003: Syntax error, ',' expected
                //         delegate*<void> ptr = &static () => { };
                Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",").WithLocation(6, 42),
                // (6,45): error CS1002: ; expected
                //         delegate*<void> ptr = &static () => { };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(6, 45)
                );
        }

        [Fact]
        public void StaticLambda_FunctionPointer_02()
        {
            var source = @"
class C
{
    unsafe void M()
    {
        delegate*<void> ptr = static () => { };
        ptr();
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeDebugDll, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (6,41): error CS1660: Cannot convert lambda expression to type 'delegate*<void>' because it is not a delegate type
                //         delegate*<void> ptr = static () => { };
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "delegate*<void>").WithLocation(6, 41));
        }

        [Fact]
        public void StaticAnonymousMethod_FunctionPointer_01()
        {
            var source = @"
class C
{
    unsafe void M()
    {
        delegate*<void> ptr = &static delegate() { };
        ptr();
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeDebugDll, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (6,32): error CS0211: Cannot take the address of the given expression
                //         delegate*<void> ptr = &static delegate() { };
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "static delegate() { }").WithLocation(6, 32)
                );
        }

        [Fact]
        public void StaticAnonymousMethod_FunctionPointer_02()
        {
            var source = @"
class C
{
    unsafe void M()
    {
        delegate*<void> ptr = static delegate() { };
        ptr();
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeDebugDll, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (6,38): error CS1660: Cannot convert anonymous method to type 'delegate*<void>' because it is not a delegate type
                //         delegate*<void> ptr = static delegate() { };
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "delegate").WithArguments("anonymous method", "delegate*<void>").WithLocation(6, 38));
        }

        [Fact]
        public void ConditionalExpr()
        {
            var source = @"
using static System.Console;

class C
{
    static void M(bool b, System.Action a)
    {
        a = b ? () => Write(1) : a;
        a();

        a = b ? static () => Write(2) : a;
        a();

        a = b ? () => { Write(3); } : a;
        a();

        a = b ? static () => { Write(4); } : a;
        a();

        a = b ? a : () => { };
        a = b ? a : static () => { };

        a = b ? delegate() { Write(5); } : a;
        a();

        a = b ? static delegate() { Write(6); } : a;
        a();

        a = b ? a : delegate() { };
        a = b ? a : static delegate() { };
    }

    static void Main()
    {
        M(true, () => { });
    }
}
";
            CompileAndVerify(source, expectedOutput: "123456", parseOptions: TestOptions.Regular9);
        }

        [Fact]
        public void RefConditionalExpr()
        {
            var source = @"
using static System.Console;

class C
{
    static void M(bool b, ref System.Action a)
    {
        a = ref b ? ref () => Write(1) : ref a;
        a();

        a = ref b ? ref static () => Write(2) : ref a;
        a();

        a = ref b ? ref () => { Write(3); } : ref a;
        a();

        a = ref b ? ref static () => { Write(4); } : ref a;
        a();

        a = ref b ? ref a : ref () => { };
        a = ref b ? ref a : ref static () => { };

        a = ref b ? ref delegate() { Write(5); } : ref a;
        a();

        a = ref b ? ref static delegate() { Write(6); } : ref a;
        a();

        a = ref b ? ref a : ref delegate() { };
        a = b ? ref a : ref static delegate() { };
    }
}
";
            VerifyInPreview(source,
                // (8,25): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         a = ref b ? ref () => Write(1) : ref a;
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "() => Write(1)").WithLocation(8, 25),
                // (11,25): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         a = ref b ? ref static () => Write(2) : ref a;
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "static () => Write(2)").WithLocation(11, 25),
                // (14,25): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         a = ref b ? ref () => { Write(3); } : ref a;
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "() => { Write(3); }").WithLocation(14, 25),
                // (17,25): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         a = ref b ? ref static () => { Write(4); } : ref a;
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "static () => { Write(4); }").WithLocation(17, 25),
                // (20,33): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         a = ref b ? ref a : ref () => { };
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "() => { }").WithLocation(20, 33),
                // (21,33): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         a = ref b ? ref a : ref static () => { };
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "static () => { }").WithLocation(21, 33),
                // (23,25): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         a = ref b ? ref delegate() { Write(5); } : ref a;
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "delegate() { Write(5); }").WithLocation(23, 25),
                // (26,25): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         a = ref b ? ref static delegate() { Write(6); } : ref a;
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "static delegate() { Write(6); }").WithLocation(26, 25),
                // (29,33): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         a = ref b ? ref a : ref delegate() { };
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "delegate() { }").WithLocation(29, 33),
                // (30,29): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         a = b ? ref a : ref static delegate() { };
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "static delegate() { }").WithLocation(30, 29)
                );
        }

        [Fact]
        public void SwitchExpr()
        {
            var source = @"
using static System.Console;

class C
{
    static void M(bool b, System.Action a)
    {
        a = b switch { true => () => Write(1), false => a };
        a();

        a = b switch { true => static () => Write(2), false => a };
        a();

        a = b switch { true => () => { Write(3); }, false => a };
        a();

        a = b switch { true => static () => { Write(4); }, false => a };
        a();

        a = b switch { true => a , false => () => { Write(0); } };
        a = b switch { true => a , false => static () => { Write(0); } };

        a = b switch { true => delegate() { Write(5); }, false => a };
        a();

        a = b switch { true => static delegate() { Write(6); }, false => a };
        a();

        a = b switch { true => a , false => delegate() { Write(0); } };
        a = b switch { true => a , false => static delegate() { Write(0); } };
    }

    static void Main()
    {
        M(true, () => { });
    }
}
";
            CompileAndVerify(source, expectedOutput: "123456", parseOptions: TestOptions.Regular9);
        }

        [Fact]
        public void DiscardParams()
        {
            var source = @"
using System;

class C
{
    static void Main()
    {
        Action<int, int, string> fn = static (_, _, z) => Console.Write(z);
        fn(1, 2, ""hello"");

        fn = static delegate(int _, int _, string z) { Console.Write(z); };
        fn(3, 4, "" world"");
    }
}
";
            CompileAndVerify(source, expectedOutput: "hello world", parseOptions: TestOptions.Regular9);
        }

        [Fact]
        public void PrivateMemberAccessibility()
        {
            var source = @"
using System;

class C
{
    private static void M1(int i) { Console.Write(i); }

    class Inner
    {
        static void Main()
        {
            Action a = static () => M1(1);
            a();

            a = static delegate() { M1(2); };
            a();
        }
    }
}
";
            verify(source);
            verify(source.Replace("static (", "("));

            void verify(string source)
            {
                CompileAndVerify(source, expectedOutput: "12", parseOptions: TestOptions.Regular9);
            }
        }
    }
}
