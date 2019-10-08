// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using ICSharpCode.Decompiler.IL;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AnonymousFunctionTestBase : CSharpTestBase
    {
        internal static readonly CSharpParseOptions DefaultParseOptions = TestOptions.Regular;

        internal static void VerifyDiagnostics(string source, params DiagnosticDescription[] expected)
        {
            var comp = CreateCompilationWithMscorlib45AndCSharp(source, options: TestOptions.ReleaseDll, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics(expected);
        }
    }

    [WorkItem(275, "https://github.com/dotnet/csharplang/issues/275")]
    [CompilerTrait(CompilerFeature.AnonymousFunctions)]
    public class AnonymousFunctionTests : AnonymousFunctionTestBase
    {
        [Fact]
        public void StaticLambdaCanCaptureStaticField()
        {
            var source = @"
using System;

public class C
{
    public static int a;

    public void Goo()
    {
        Func<int> f = static () => a;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (10,23): error CS8652: The feature 'static anonymous function' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         Func<int> f = static () => a;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "static").WithArguments("static anonymous function").WithLocation(10, 23));
        }

        [Fact]
        public void StaticLambdaCanCaptureStaticProperty()
        {
            var source = @"
using System;

public class C
{
    static int A { get; }

    public void Goo()
    {
        Func<int> f = static () => A;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (10,23): error CS8652: The feature 'static anonymous function' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         Func<int> f = static () => A;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "static").WithArguments("static anonymous function").WithLocation(10, 23));
        }

        [Fact]
        public void StaticLambdaCannotCaptureInstanceField()
        {
            var source = @"
using System;

public class C
{
    public int a;

    public void Goo()
    {
        Func<int> f = static () => a;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (10,23): error CS8652: The feature 'static anonymous function' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         Func<int> f = static () => a;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "static").WithArguments("static anonymous function").WithLocation(10, 23),
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

    public void Goo()
    {
        Func<int> f = static () => A;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (10,23): error CS8652: The feature 'static anonymous function' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         Func<int> f = static () => A;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "static").WithArguments("static anonymous function").WithLocation(10, 23),
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
    public void Goo(int a)
    {
        Func<int> f = static () => a;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,23): error CS8652: The feature 'static anonymous function' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         Func<int> f = static () => a;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "static").WithArguments("static anonymous function").WithLocation(8, 23),
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
    public void Goo()
    {
        int a;
        Func<int> f = static () => a;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (9,23): error CS8652: The feature 'static anonymous function' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         Func<int> f = static () => a;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "static").WithArguments("static anonymous function").WithLocation(9, 23),
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
    public void Goo()
    {
        Func<int> f = static () =>
        {
            int a = 0;
            return a;
        };
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,23): error CS8652: The feature 'static anonymous function' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         Func<int> f = static () =>
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "static").WithArguments("static anonymous function").WithLocation(8, 23));
        }

        [Fact]
        public void StaticLambdaCannotReferenceThis()
        {
            var source = @"
using System;

public class C
{
    public void Goo()
    {
        Func<int> f = static () =>
        {
            this.Goo();
            return 0;
        };
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,23): error CS8652: The feature 'static anonymous function' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         Func<int> f = static () =>
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "static").WithArguments("static anonymous function").WithLocation(8, 23),
                // (10,13): error CS8428: A static anonymous function cannot contain a reference to 'this' or 'base'.
                //             this.Goo();
                Diagnostic(ErrorCode.ERR_StaticAnonymousFunctionCannotCaptureThis, "this").WithLocation(10, 13));
        }

        [Fact]
        public void StaticLambdaCannotReferenceInstanceLocalFunction()
        {
            var source = @"
using System;

public class C
{
    public void Goo()
    {
        Func<int> f = static () =>
        {
            Goo();
            return 0;
        };

        void Goo() {}
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,23): error CS8652: The feature 'static anonymous function' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         Func<int> f = static () =>
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "static").WithArguments("static anonymous function").WithLocation(8, 23),
                // (10,13): error CS8427: A static anonymous function cannot contain a reference to 'Goo'.
                //             Goo();
                Diagnostic(ErrorCode.ERR_StaticAnonymousFunctionCannotCaptureVariable, "Goo()").WithArguments("Goo").WithLocation(10, 13));
        }

        [Fact]
        public void StaticLambdaCanReferenceStaticLocalFunction()
        {
            var source = @"
using System;

public class C
{
    public void Goo()
    {
        Func<int> f = static () =>
        {
            Goo();
            return 0;
        };

        static void Goo() {}
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,23): error CS8652: The feature 'static anonymous function' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         Func<int> f = static () =>
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "static").WithArguments("static anonymous function").WithLocation(8, 23));
        }

        [Fact]
        public void StaticLambdaCanHaveLocalsCapturedByInnerInstanceLambda()
        {
            var source = @"
using System;

public class C
{
    public void Goo()
    {
        Func<int> f = static () =>
        {
            int i = 0;
            Func<int> g = () => i;
            return 0;
        };
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,23): error CS8652: The feature 'static anonymous function' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         Func<int> f = static () =>
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "static").WithArguments("static anonymous function").WithLocation(8, 23));
        }

        [Fact]
        public void StaticLambdaCannotHaveLocalsCapturedByInnerStaticLambda()
        {
            var source = @"
using System;

public class C
{
    public void Goo()
    {
        Func<int> f = static () =>
        {
            int i = 0;
            Func<int> g = static () => i;
            return 0;
        };
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,23): error CS8652: The feature 'static anonymous function' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         Func<int> f = static () =>
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "static").WithArguments("static anonymous function").WithLocation(8, 23),
                // (11,27): error CS8652: The feature 'static anonymous function' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //             Func<int> g = static () => i;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "static").WithArguments("static anonymous function").WithLocation(11, 27),
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
    public void Goo()
    {
        Func<int> f = () =>
        {
            int i = 0;
            Func<int> g = static () => i;
            return 0;
        };
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (11,27): error CS8652: The feature 'static anonymous function' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //             Func<int> g = static () => i;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "static").WithArguments("static anonymous function").WithLocation(11, 27),
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
    public void Goo()
    {
        Func<int> f = static () =>
        {
            int i = 0;
            int g() => i;
            return g();
        };
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,23): error CS8652: The feature 'static anonymous function' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         Func<int> f = static () =>
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "static").WithArguments("static anonymous function").WithLocation(8, 23));
        }

        [Fact]
        public void StaticLambdaCannotHaveLocalsCapturedByInnerStaticLocalFunction()
        {
            var source = @"
using System;

public class C
{
    public void Goo()
    {
        Func<int> f = static () =>
        {
            int i = 0;
            static int g() => i;
            return g();
        };
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,23): error CS8652: The feature 'static anonymous function' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         Func<int> f = static () =>
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "static").WithArguments("static anonymous function").WithLocation(8, 23),
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
    public void Goo()
    {
        Func<int> f = () =>
        {
            int i = 0;
            static int g() => i;
            return g();
        };
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (11,31): error CS8421: A static local function cannot contain a reference to 'i'.
                //             static int g() => i;
                Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureVariable, "i").WithArguments("i").WithLocation(11, 31));
        }

        [Fact]
        public void StaticLocalFunctionCanHaveLocalsCapturedByInnerInstanceLocalFunction()
        {
            var source = @"


public class C
{
    public void Goo()
    {
        static int f()
        {
            int i = 0;
            int g() => i;
            return g();
        };
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,20): warning CS8321: The local function 'f' is declared but never used
                //         static int f()
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "f").WithArguments("f").WithLocation(8, 20));
        }

        [Fact]
        public void StaticLocalFunctionCannotHaveLocalsCapturedByInnerStaticLocalFunction()
        {
            var source = @"


public class C
{
    public void Goo()
    {
        static int f()
        {
            int i = 0;
            static int g() => i;
            return g();
        };
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
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
    public void Goo()
    {
        int f()
        {
            int i = 0;
            static int g() => i;
            return g();
        };
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
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
    public void Goo()
    {
        static int f()
        {
            int i = 0;
            Func<int> g = () => i;
            return g();
        };
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
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
    public void Goo()
    {
        static int f()
        {
            int i = 0;
            Func<int> g = static () => i;
            return g();
        };
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,20): warning CS8321: The local function 'f' is declared but never used
                //         static int f()
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "f").WithArguments("f").WithLocation(8, 20),
                // (11,27): error CS8652: The feature 'static anonymous function' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //             Func<int> g = static () => i;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "static").WithArguments("static anonymous function").WithLocation(11, 27),
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
    public void Goo()
    {
        int f()
        {
            int i = 0;
            Func<int> g = static () => i;
            return g();
        };
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,13): warning CS8321: The local function 'f' is declared but never used
                //         int f()
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "f").WithArguments("f").WithLocation(8, 13),
                // (11,27): error CS8652: The feature 'static anonymous function' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //             Func<int> g = static () => i;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "static").WithArguments("static anonymous function").WithLocation(11, 27),
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
    public void Goo()
    {
        Func<int> f = static () =>
        {
            M();
            return 0;
        };
    }

    static void M()
    {
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,23): error CS8652: The feature 'static anonymous function' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         Func<int> f = static () =>
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "static").WithArguments("static anonymous function").WithLocation(8, 23));
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

    public void Goo()
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
            CreateCompilation(source).VerifyDiagnostics(
                // (11,23): error CS8652: The feature 'static anonymous function' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         Func<int> f = static () =>
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "static").WithArguments("static anonymous function").WithLocation(11, 23),
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

public class C
{
    public static string[] args;

    public void Goo()
    {
        Func<int> f = static () =>
        {
            var q = from a in args
                    select M(a);
            return 0;
        };
    }

    static int M(string a) => 0;
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (11,23): error CS8652: The feature 'static anonymous function' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         Func<int> f = static () =>
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "static").WithArguments("static anonymous function").WithLocation(11, 23));
        }

        [Fact]
        public void InstanceLambdaInStaticLambdaCannotReferenceThis()
        {
            var source = @"
using System;

public class C
{
    public void Goo()
    {
        Func<int> f = static () =>
        {
            Func<int> g = () =>
            {
                this.Goo();
                return 0;
            };

            return g();
        };
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,23): error CS8652: The feature 'static anonymous function' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         Func<int> f = static () =>
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "static").WithArguments("static anonymous function").WithLocation(8, 23),
                // (12,17): error CS8428: A static anonymous function cannot contain a reference to 'this' or 'base'.
                //                 this.Goo();
                Diagnostic(ErrorCode.ERR_StaticAnonymousFunctionCannotCaptureThis, "this").WithLocation(12, 17));
        }

        [Fact]
        public void TestSymbols()
        {
            var source = @"
using System;

public class C
{
    public void Goo()
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

            var anonymousMethod = (IMethodSymbol)semanticModel.GetSymbolInfo(anonymousMethodSyntax).Symbol;
            var simpleLambda = (IMethodSymbol)semanticModel.GetSymbolInfo(simpleLambdaSyntax).Symbol;
            var parenthesizedLambda = (IMethodSymbol)semanticModel.GetSymbolInfo(parenthesizedLambdaSyntax).Symbol;

            Assert.True(anonymousMethod.IsStatic);
            Assert.True(simpleLambda.IsStatic);
            Assert.True(parenthesizedLambda.IsStatic);
        }
    }
}
