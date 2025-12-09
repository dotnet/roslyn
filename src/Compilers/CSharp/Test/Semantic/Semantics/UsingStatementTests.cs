// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// Tests related to binding (but not lowering) using statements (not directives).
    /// </summary>
    public class UsingStatementTests : CompilingTestBase
    {
        private const string _managedClass = @"
class MyManagedType : System.IDisposable
{
    public void Dispose()
    { }
}";

        private const string _managedStruct = @"
struct MyManagedType : System.IDisposable
{
    public void Dispose()
    { }
}";

        [Fact]
        public void SemanticModel()
        {
            var source = @"
class C
{
    static void Main()
    {
        using (System.IDisposable i = null)
        {
            i.Dispose(); //this makes no sense, but we're only testing binding
        }
    }
}
";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var usingStatement = tree.GetCompilationUnitRoot().DescendantNodes().OfType<UsingStatementSyntax>().Single();

            var declaredSymbol = model.GetDeclaredSymbol(usingStatement.Declaration.Variables.Single());
            Assert.NotNull(declaredSymbol);
            Assert.Equal(SymbolKind.Local, declaredSymbol.Kind);
            var declaredLocal = (ILocalSymbol)declaredSymbol;
            Assert.Equal("i", declaredLocal.Name);
            Assert.Equal(SpecialType.System_IDisposable, declaredLocal.Type.SpecialType);

            var memberAccessExpression = tree.GetCompilationUnitRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single();

            var info = model.GetSymbolInfo(memberAccessExpression.Expression);
            Assert.NotEqual(default, info);
            Assert.Equal(declaredLocal, info.Symbol);

            var lookupSymbol = model.LookupSymbols(memberAccessExpression.SpanStart, name: declaredLocal.Name).Single();
            Assert.Equal(declaredLocal, lookupSymbol);
        }

        [Fact]
        public void MethodGroup()
        {
            var source = @"
class C
{
    static void Main()
    {
        using (Main)
        {
        }
    }
}
";

            CreateCompilation(source).VerifyDiagnostics(
                // (6,16): error CS1674: 'method group': type used in a using statement must implement 'System.IDisposable'.
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "Main").WithArguments("method group"));
        }

        [Fact]
        public void UsingPatternRefStructTest()
        {
            var source = @"
ref struct S1
{
    public void Dispose() { }
}

class C2
{
    static void Main()
    {
        using (S1 s = new S1())
        {
        }
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UsingPatternReferenceTypeTest()
        {
            var source = @"
class C1
{
    public void Dispose() { }
}

class C2
{
    static void Main()
    {
        using (C1 c1 = new C1())
        {
        }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (11,16): error CS1674: 'C1': type used in a using statement must implement 'System.IDisposable'.
                //         using (C1 c1 = new C1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "C1 c1 = new C1()").WithArguments("C1").WithLocation(11, 16)
                );
        }

        [Fact]
        public void UsingPatternSameSignatureAmbiguousTest()
        {
            var source = @"
ref struct S1
{
    public void Dispose() { }
    public void Dispose() { }
}

class C2
{
    static void Main()
    {
        using (S1 s = new S1())
        {
        }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (5,17): error CS0111: Type 'S1' already defines a member called 'Dispose' with the same parameter types
                //     public void Dispose() { }
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "Dispose").WithArguments("Dispose", "S1").WithLocation(5, 17),
                // (12,16): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //         using (S1 c = new S1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "S1 s = new S1()").WithArguments("S1").WithLocation(12, 16)
                );
        }

        [Fact]
        public void UsingPatternAmbiguousOverloadTest()
        {
            var source = @"
ref struct S1
{
    public void Dispose() { }
    public bool Dispose() { return false; }
}

class C2
{
    static void Main()
    {
        using (S1 s = new S1())
        {
        }
        S1 s1b = new S1();
        using (s1b) { }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (5,17): error CS0111: Type 'S1' already defines a member called 'Dispose' with the same parameter types
                //     public bool Dispose() { return false; }
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "Dispose").WithArguments("Dispose", "S1").WithLocation(5, 17),
                // (12,16): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //         using (S1 c = new S1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "S1 s = new S1()").WithArguments("S1").WithLocation(12, 16),
                // (16,16): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //         using (c1b) { }
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "s1b").WithArguments("S1").WithLocation(16, 16)
                );
        }

        [Fact]
        public void UsingPatternDifferentParameterOverloadTest()
        {
            var source = @"
ref struct S1
{
    public void Dispose() { }
    public void Dispose(int x) { }
}

class C2
{
    static void Main()
    {
        using (S1 s = new S1())
        {
        }
        S1 s1b = new S1();
        using (s1b) { }
    }
}";
            // Shouldn't throw an error as the method signatures are different.
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UsingPatternInaccessibleAmbiguousTest()
        {
            var source = @"
ref struct S1
{
    public void Dispose() { }
    internal void Dispose() { }
}

class C2
{
    static void Main()
    {
        using (S1 s = new S1())
        {
        }
        S1 s1b = new S1();
        using (s1b) { }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (5,19): error CS0111: Type 'S1' already defines a member called 'Dispose' with the same parameter types
                //     internal void Dispose() { }
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "Dispose").WithArguments("Dispose", "S1").WithLocation(5, 19),
                // (12,16): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //         using (S1 c = new S1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "S1 s = new S1()").WithArguments("S1").WithLocation(12, 16),
                // (16,16): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //         using (c1b) { }
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "s1b").WithArguments("S1").WithLocation(16, 16)
                );
        }

        [Fact]
        public void UsingPatternImplicitConversionToDisposable()
        {
            var source = @"
ref struct S1
{
    public void Dispose() { }

    //  User-defined implicit conversion from C2 to S1
    public static implicit operator S1(C2 o)
    {
        return new S1();
    }
}

class C2
{
}

class C3
{
    static void Main()
    {
        using (S1 s = new C2())
        {
        }
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UsingPatternExtensionMethodTest()
        {
            var source = @"
ref struct S1
{
}

static class C2 
{
    public static void Dispose(this S1 c1) { }
}

class C3
{
    static void Main()
    {
        using (S1 s = new S1())
        {
        }
        S1 s1b = new S1();
        using (s1b) { }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (15,16): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //         using (S1 s = new S1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "S1 s = new S1()").WithArguments("S1").WithLocation(15, 16),
                // (19,16): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //         using (s1b) { }
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "s1b").WithArguments("S1").WithLocation(19, 16)
                );
        }

        [Fact]
        public void UsingPatternExtensionMethodPropertyHidingTest()
        {
            var source = @"
ref struct S1
{
    public int Dispose { get; }
}

static class C2 
{
    public static void Dispose(this S1 s1) { }
}

class C3
{
    static void Main()
    {
        using (S1 s = new S1())
        {
        }
        S1 s1b = new S1();
        using (s1b) { }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (16,16): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //         using (S1 s = new S1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "S1 s = new S1()").WithArguments("S1").WithLocation(16, 16),
                // (20,16): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //         using (s1b) { }
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "s1b").WithArguments("S1").WithLocation(20, 16)
                );
        }

        [Fact]
        public void UsingPatternStaticMethodTest()
        {
            var source = @"
ref struct S1
{
    public static void Dispose() { }
}

class C2
{
    static void Main()
    {
        using (S1 s = new S1())
        {
        }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (11,16): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //         using (S1 s = new S1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "S1 s = new S1()").WithArguments("S1").WithLocation(11, 16)
                );
        }

        [Fact]
        public void UsingPatternAmbiguousExtensionMethodTest()
        {
            var source = @"
ref struct S1
{
}

static class C2 
{
    public static void Dispose(this S1 s1) { }
}

static class C3 
{
    public static void Dispose(this S1 s1) { }
}

class C4
{
    static void Main()
    {
        using (S1 s = new S1())
        {
        }
    }
}";
            // Extension methods should just be ignored, rather than rejected after-the-fact. So there should be no error
            // Tracked by https://github.com/dotnet/roslyn/issues/32767

            CreateCompilation(source).VerifyDiagnostics(
                // (20,16): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //         using (S1 c = new S1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "S1 s = new S1()").WithArguments("S1").WithLocation(20, 16)
                );
        }

        [Fact]
        public void UsingPatternScopedExtensionMethodTest()
        {
            var source = @"
ref struct S1
{
}

namespace N1
{
    static class C2
    {
        public static void Dispose(this S1 s1) { }
    }
}

namespace N2
{
    static class C3
    {
        public static void Dispose(this S1 s1) { }
    }
}

namespace N3
{
    static class C4
    {
        public static int Dispose(this S1 s1) { return 0; }
    }
}


namespace N4
{
    partial class C5
    {
        static void M()
        {
            using (S1 s = new S1()) // error 1
            {
            }
        }
    }
}
namespace N4
{
    using N1;
    partial class C5
    {
        static void M2()
        {
            using (S1 s = new S1()) // error 2
            {
            }
        }
    }
}
namespace N4
{
    using N3;
    partial class C5
    {
        static void M3()
        {
            using (S1 s = new S1()) // error 3
            {
            }
        }
    }
}
namespace N4
{
    using N1;
    using N3;
    partial class C5
    {
        static void M4()
        {
            using (S1 s = new S1())  // error 4
            {
            }
        }
    }
}
namespace N4
{
    using N3;
    namespace N5
    {
        partial class C5
        {
            static void M5()
            {
                using (S1 s = new S1())  // error 5
                {
                }
            }
        }

        namespace N6
        {
            using N1;
            partial class C5
            {
                static void M6()
                {
                    using (S1 s = new S1())  // error 6
                    {
                    }
                }
            }
        }
    }
}";
            // Extension methods should just be ignored, rather than rejected after-the-fact. So there should be no error
            // Tracked by https://github.com/dotnet/roslyn/issues/32767

            CreateCompilation(source).VerifyDiagnostics(
                // (37,20): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //             using (S1 s = new S1()) // error 1
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "S1 s = new S1()").WithArguments("S1").WithLocation(37, 20),
                // (50,20): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //             using (S1 s = new S1()) // error 2
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "S1 s = new S1()").WithArguments("S1").WithLocation(50, 20),
                // (63,20): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //             using (S1 s = new S1()) // error 3
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "S1 s = new S1()").WithArguments("S1").WithLocation(63, 20),
                // (77,20): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //             using (S1 s = new S1())  // error 4
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "S1 s = new S1()").WithArguments("S1").WithLocation(77, 20),
                // (92,24): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //                 using (S1 s = new S1())  // error 5
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "S1 s = new S1()").WithArguments("S1").WithLocation(92, 24),
                // (105,28): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //                     using (S1 s = new S1())  // error 6
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "S1 s = new S1()").WithArguments("S1").WithLocation(105, 28)
                );
        }

        [Fact]
        public void UsingPatternWithMultipleExtensionTargets()
        {
            var source = @"
ref struct S1
{
}

ref struct S2
{
}

static class C3 
{
    public static void Dispose(this S1 s1) { }

    public static void Dispose(this S2 s2) { }

}

class C4
{
    static void Main()
    {
        using (S1 s = new S1())
        {
        }
        S1 s1b = new S1();
        using (s1b) { }

        using (S2 s = new S2())
        {
        }
        S2 s2b = new S2();
        using (s2b) { }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (22,16): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //         using (S1 s = new S1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "S1 s = new S1()").WithArguments("S1").WithLocation(22, 16),
                // (26,16): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //         using (s1b) { }
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "s1b").WithArguments("S1").WithLocation(26, 16),
                // (28,16): error CS1674: 'S2': type used in a using statement must implement 'System.IDisposable'.
                //         using (S2 s = new S2())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "S2 s = new S2()").WithArguments("S2").WithLocation(28, 16),
                // (32,16): error CS1674: 'S2': type used in a using statement must implement 'System.IDisposable'.
                //         using (s2b) { }
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "s2b").WithArguments("S2").WithLocation(32, 16)
                );
        }

        [Fact]
        public void UsingPatternExtensionMethodWithDefaultArgumentsAmbiguous()
        {
            var source = @"
ref struct S1
{
}

static class C2 
{
    internal static void Dispose(this S1 s1, int a = 1) { }

}

static class C3
{
    internal static void Dispose(this S1 s1, int b = 2) { }
}

class C4
{
    static void Main()
    {
       using (S1 s = new S1())
       {
       }
    }
}";
            // Extension methods should just be ignored, rather than rejected after-the-fact. So there should be no error
            // Tracked by https://github.com/dotnet/roslyn/issues/32767

            CreateCompilation(source).VerifyDiagnostics(
                // (21,15): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //        using (S1 s = new S1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "S1 s = new S1()").WithArguments("S1").WithLocation(21, 15)
                );
        }

        [Fact]
        public void UsingPatternExtensionMethodNonPublic()
        {
            var source = @"
ref struct S1
{
}

static class C2 
{
   internal static void Dispose(this S1 s1) { }
}

class C3
{
    static void Main()
    {
       using (S1 s = new S1())
       {
       }
       S1 s1b = new S1();
       using (s1b) { }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (15,15): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //        using (S1 s = new S1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "S1 s = new S1()").WithArguments("S1").WithLocation(15, 15),
                // (19,15): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //        using (s1b) { }
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "s1b").WithArguments("S1").WithLocation(19, 15)
                );
        }

        [Fact]
        public void UsingPatternExtensionMethodWithInvalidInstanceMethod()
        {
            var source = @"
ref struct S1
{
    public int Dispose() 
    {
        return 0;
    }
}

static class C2 
{
   internal static void Dispose(this S1 s1) { }
}

class C3
{
    static void Main()
    {
       using (S1 s = new S1())
       {
       }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (19,15): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //        using (S1 s = new S1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "S1 s = new S1()").WithArguments("S1").WithLocation(19, 15)
                );
        }

        [Fact]
        public void UsingPatternExtensionMethodWithInvalidInstanceProperty()
        {
            var source = @"
ref struct S1
{
    public int Dispose => 0;
}

static class C2 
{
   internal static void Dispose(this S1 s1) { }
}

class C3
{
    static void Main()
    {
       using (S1 s = new S1())
       {
       }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (16,15): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //        using (S1 s = new S1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "S1 s = new S1()").WithArguments("S1").WithLocation(16, 15)
                );
        }

        [Fact]
        public void UsingPatternExtensionMethodWithDefaultArguments()
        {
            var source = @"
ref struct S1
{
}

static class C2 
{
   internal static void Dispose(this S1 s1, int a = 1) { }
}

class C3
{
    static void Main()
    {
       using (S1 s = new S1())
       {
       }
       S1 s1b = new S1();
       using (s1b) { }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (15,15): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //        using (S1 s = new S1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "S1 s = new S1()").WithArguments("S1").WithLocation(15, 15),
                // (19,15): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //        using (s1b) { }
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "s1b").WithArguments("S1").WithLocation(19, 15)
                );
        }

        [Fact]
        public void UsingPatternExtensionMethodOnInStruct()
        {
            var source = @"
ref struct S1
{
}

static class C1 
{
   public static void Dispose(in this S1 s1) { }
}

class C2
{
    static void Main()
    {
       using (S1 s = new S1())
       {
       }
    }
}";
            var compilation = CreateCompilation(source).VerifyDiagnostics(
                // (15,15): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //        using (S1 s = new S1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "S1 s = new S1()").WithArguments("S1").WithLocation(15, 15)
                );
        }

        [Fact]
        public void UsingPatternExtensionMethodRefParameterOnStruct()
        {
            var source = @"
ref struct S1
{
}

static class C1 
{
   public static void Dispose(ref this S1 s1) { }
}

class C2
{
    static void Main()
    {
       using (S1 s = new S1())
       {
       }
    }
}";

            var compilation = CreateCompilation(source).VerifyDiagnostics(
                // (15,15): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //        using (S1 s = new S1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "S1 s = new S1()").WithArguments("S1").WithLocation(15, 15)
                );
        }

        [Fact]
        public void UsingPatternWithParamsTest()
        {
            var source = @"
ref struct S1
{
    public void Dispose(params int[] args){ }
}

class C2
{
    static void Main()
    {
        using (S1 c = new S1())
        {
        }
        S1 c1b = new S1();
        using (c1b) { }
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UsingPatternWithDefaultParametersTest()
        {
            var source = @"
ref struct S1
{
    public void Dispose(int a = 4){ }
}

class C2
{
    static void Main()
    {
        using (S1 c = new S1())
        {
        }
        S1 c1b = new S1();
        using (c1b) { }
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UsingPatternWrongReturnTest()
        {
            var source = @"
ref struct S1
{
    public bool Dispose() { return false; }
}

class C2
{
    static void Main()
    {
        using (S1 c = new S1())
        {
        }
        S1 c1b = new S1();
        using (c1b) { }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (11,16): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //         using (S1 c = new S1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "S1 c = new S1()").WithArguments("S1").WithLocation(11, 16),
                // (15,16): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //         using (c1b) { }
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "c1b").WithArguments("S1").WithLocation(15, 16)
                );
        }

        [Fact]
        public void UsingPatternWrongAccessibilityTest()
        {
            var source = @"
ref struct S1
{
    private void Dispose() { }
}

class C2
{
    static void Main()
    {
        using (S1 c = new S1())
        {
        }
        S1 c1b = new S1();
        using (c1b) { }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (11,16): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //         using (S1 c = new S1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "S1 c = new S1()").WithArguments("S1").WithLocation(11, 16),
                // (15,16): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //         using (c1b) { }
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "c1b").WithArguments("S1").WithLocation(15, 16)
                );
        }

        [Fact]
        public void UsingPatternNonPublicAccessibilityTest()
        {
            var source = @"
ref struct S1
{
    internal void Dispose() { }
}

class C2
{
    static void Main()
    {
        using (S1 c = new S1())
        {
        }
        S1 c1b = new S1();
        using (c1b) { }
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UsingPatternStaticMethod()
        {
            var source = @"
ref struct S1
{
    public static void Dispose() { }
}

class C2
{
    static void Main()
    {
        using (S1 c1 = new S1())
        {
        }
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (11,16): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //         using (S1 c1 = new S1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "S1 c1 = new S1()").WithArguments("S1").WithLocation(11, 16)
                );
        }

        [Fact]
        public void UsingPatternGenericMethodTest()
        {
            var source = @"
ref struct S1
{
    public void Dispose<T>() { }
}

class C2
{
    static void Main()
    {
        using (S1 c = new S1())
        {
        }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (11,16): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable'.
                //         using (S1 c = new S1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "S1 c = new S1()").WithArguments("S1").WithLocation(11, 16)
                );
        }

        [Fact]
        public void UsingPatternDynamicArgument()
        {
            var source = @"
class C1
{
    public void Dispose(dynamic x = null) { }
}

class C2
{
    static void Main()
    {
        using (C1 c1 = new C1())
        {
        }
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (11,16): error CS1674: 'C1': type used in a using statement must implement 'System.IDisposable'.
                //         using (C1 c1 = new C1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "C1 c1 = new C1()").WithArguments("C1").WithLocation(11, 16)
                );
        }

        [Fact]
        public void UsingPatternAsyncTest()
        {
            var source = @"
using System.Threading.Tasks;
ref struct S1
{
    public ValueTask DisposeAsync() 
    { 
        System.Console.WriteLine(""Dispose async"");
        return new ValueTask(Task.CompletedTask);
    }
}

class C2
{
    static async Task Main()
    {
        await using (S1 c = new S1())
        {
        }
    }
}";
            var compilation = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: "Dispose async").VerifyDiagnostics();
        }

        [Fact]
        public void UsingPatternAsyncTest_02()
        {
            var source = """
                using System.Threading.Tasks;
                ref struct S1
                {
                    public ValueTask DisposeAsync() 
                    { 
                        System.Console.WriteLine("Dispose async");
                        return new ValueTask(Task.CompletedTask);
                    }
                }
                class C2
                {
                    static async Task Main()
                    {
                        await using (S1 c = new S1())
                        {
                            await Task.Yield();
                        }
                    }
                }
                """;
            var compilation = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition });
            compilation.VerifyEmitDiagnostics(
                // 0.cs(14,25): error CS4007: Instance of type 'S1' cannot be preserved across 'await' or 'yield' boundary.
                //         await using (S1 c = new S1())
                Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, "c = new S1()").WithArguments("S1").WithLocation(14, 25));
        }

        [Fact]
        public void UsingPatternAsyncTest_03()
        {
            var source = """
                using System.Threading.Tasks;
                ref struct S1
                {
                    public S1(int x) { }
                    public ValueTask DisposeAsync() 
                    { 
                        System.Console.WriteLine("Dispose async");
                        return new ValueTask(Task.CompletedTask);
                    }
                }
                class C2
                {
                    static async Task Main()
                    {
                        await using (S1 c = new S1(await Task.FromResult(1)))
                        {
                        }
                    }
                }
                """;
            var compilation = CreateCompilationWithTasksExtensions(new[] { source, IAsyncDisposableDefinition }, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: "Dispose async").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(32728, "https://github.com/dotnet/roslyn/issues/32728")]
        public void UsingPatternWithLangVer7_3()
        {
            var source = @"
ref struct S1
{
    public void Dispose() { }
}

class C2
{
    static void Main()
    {
        using (S1 s = new S1())
        {
        }
    }
}
";

            CreateCompilation(source, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(
                // (11,16): error CS8370: Feature 'pattern-based disposal' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         using (S1 s = new S1())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "S1 s = new S1()").WithArguments("pattern-based disposal", "8.0").WithLocation(11, 16)
                );

            CreateCompilation(source, parseOptions: TestOptions.Regular8).VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(32728, "https://github.com/dotnet/roslyn/issues/32728")]
        public void UsingInvalidPatternWithLangVer7_3()
        {
            var source = @"
ref struct S1
{
    public int Dispose() { return 0; }
}

class C2
{
    static void Main()
    {
        using (S1 s = new S1())
        {
        }
    }
}
";
            CreateCompilation(source, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(
                // (11,16): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable' or implement a suitable 'Dispose' method.
                //         using (S1 s = new S1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "S1 s = new S1()").WithArguments("S1").WithLocation(11, 16)
            );

            CreateCompilation(source, parseOptions: TestOptions.Regular8).VerifyDiagnostics(
                // (11,16): error CS1674: 'S1': type used in a using statement must implement 'System.IDisposable' or implement a suitable 'Dispose' method.
                //         using (S1 s = new S1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "S1 s = new S1()").WithArguments("S1").WithLocation(11, 16)
                );
        }

        [Fact]
        public void Lambda()
        {
            var source = @"
class C
{
    static void Main()
    {
        using (x => x)
        {
        }
    }
}
";

            CreateCompilation(source).VerifyDiagnostics(
                // (6,16): error CS1674: 'lambda expression': type used in a using statement must implement 'System.IDisposable'.
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "x => x").WithArguments("lambda expression"));
        }

        [Fact]
        public void Null()
        {
            var source = @"
class C
{
    static void Main()
    {
        using (null)
        {
        }
    }
}
";

            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UnusedVariable()
        {
            var source = @"
class C
{
    static void Main()
    {
        using (System.IDisposable d = null)
        {
        }
    }
}
";

            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void EmbeddedStatement()
        {
            var source = @"
class C
{
    static void Main()
    {
        using (System.IDisposable a = null)
            using (System.IDisposable b = null)
                using (System.IDisposable c = null) ;
    }
}
";

            CreateCompilation(source).VerifyDiagnostics(
                // (8,53): warning CS0642: Possible mistaken empty statement
                Diagnostic(ErrorCode.WRN_PossibleMistakenNullStatement, ";"));
        }

        [Fact]
        public void ModifyUsingLocal()
        {
            var source = @"
using System;

class C
{
    static void Main()
    {
        using (IDisposable i = null)
        {
            i = null;
            Ref(ref i);
            Out(out i);
        }
    }

    static void Ref(ref IDisposable i) { }
    static void Out(out IDisposable i) { i = null; }
}
";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
    // (10,13): error CS1656: Cannot assign to 'i' because it is a 'using variable'
    //             i = null;
    Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "i").WithArguments("i", "using variable").WithLocation(10, 13),
    // (11,21): error CS1657: Cannot use 'i' as a ref or out value because it is a 'using variable'
    //             Ref(ref i);
    Diagnostic(ErrorCode.ERR_RefReadonlyLocalCause, "i").WithArguments("i", "using variable").WithLocation(11, 21),
    // (12,21): error CS1657: Cannot use 'i' as a ref or out value because it is a 'using variable'
    //             Out(out i);
    Diagnostic(ErrorCode.ERR_RefReadonlyLocalCause, "i").WithArguments("i", "using variable").WithLocation(12, 21)
    );
        }

        [Fact]
        public void ImplicitType1()
        {
            var source = @"
using System.IO;

class C
{
    static void Main()
    {
        using (var a = new StreamWriter(""""))
        {
        }
    }
}
";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var usingStatement = tree.GetCompilationUnitRoot().DescendantNodes().OfType<UsingStatementSyntax>().Single();

            var declaredSymbol = model.GetDeclaredSymbol(usingStatement.Declaration.Variables.Single());

            Assert.Equal("System.IO.StreamWriter a", declaredSymbol.ToTestDisplayString());

            var typeInfo = model.GetSymbolInfo(usingStatement.Declaration.Type);
            Assert.Equal(((ILocalSymbol)declaredSymbol).Type, typeInfo.Symbol);
        }

        [Fact]
        public void ImplicitType2()
        {
            var source = @"
using System.IO;

class C
{
    static void Main()
    {
        using (var a = new StreamWriter(""""), b = new StreamReader(""""))
        {
        }
    }
}
";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (8,16): error CS0819: Implicitly-typed variables cannot have multiple declarators
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableMultipleDeclarator, @"var a = new StreamWriter(""""), b = new StreamReader("""")"));

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var usingStatement = tree.GetCompilationUnitRoot().DescendantNodes().OfType<UsingStatementSyntax>().Single();

            var firstDeclaredSymbol = model.GetDeclaredSymbol(usingStatement.Declaration.Variables.First());

            Assert.Equal("System.IO.StreamWriter a", firstDeclaredSymbol.ToTestDisplayString());

            var secondDeclaredSymbol = model.GetDeclaredSymbol(usingStatement.Declaration.Variables.Last());

            Assert.Equal("System.IO.StreamReader b", secondDeclaredSymbol.ToTestDisplayString());

            var typeInfo = model.GetSymbolInfo(usingStatement.Declaration.Type);

            // the type info uses the type inferred for the first declared local
            Assert.Equal(((ILocalSymbol)model.GetDeclaredSymbol(usingStatement.Declaration.Variables.First())).Type, typeInfo.Symbol);
        }

        [Fact]
        public void ModifyLocalInUsingExpression()
        {
            var source = @"
using System;

class C
{
    void Main()
    {
        IDisposable i = null;
        using (i)
        {
            i = null; //CS0728
            Ref(ref i); //CS0728
            this[out i] = 1; //CS0728
        }
    }

    void Ref(ref IDisposable i) { }
    int this[out IDisposable i] { set { i = null; } } //this is illegal, so if we break this test, we may need a metadata indexer
}
";

            CreateCompilation(source).VerifyDiagnostics(
                // (18,14): error CS0631: ref and out are not valid in this context
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "out"),
                // (11,13): warning CS0728: Possibly incorrect assignment to local 'i' which is the argument to a using or lock statement. The Dispose call or unlocking will happen on the original value of the local.
                Diagnostic(ErrorCode.WRN_AssignmentToLockOrDispose, "i").WithArguments("i"),
                // (12,21): warning CS0728: Possibly incorrect assignment to local 'i' which is the argument to a using or lock statement. The Dispose call or unlocking will happen on the original value of the local.
                Diagnostic(ErrorCode.WRN_AssignmentToLockOrDispose, "i").WithArguments("i"),
                // (13,22): warning CS0728: Possibly incorrect assignment to local 'i' which is the argument to a using or lock statement. The Dispose call or unlocking will happen on the original value of the local.
                Diagnostic(ErrorCode.WRN_AssignmentToLockOrDispose, "i").WithArguments("i"));
        }

        [Fact]
        public void ModifyParameterInUsingExpression()
        {
            var source = @"
using System;

class C
{
    void M(IDisposable i)
    {
        using (i)
        {
            i = null; //CS0728
            Ref(ref i); //CS0728
            this[out i] = 1; //CS0728
        }
    }

    void Ref(ref IDisposable i) { }
    int this[out IDisposable i] { set { i = null; } } //this is illegal, so if we break this test, we may need a metadata indexer
}
";

            CreateCompilation(source).VerifyDiagnostics(
                // (17,14): error CS0631: ref and out are not valid in this context
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "out"),
                // (10,13): warning CS0728: Possibly incorrect assignment to local 'i' which is the argument to a using or lock statement. The Dispose call or unlocking will happen on the original value of the local.
                Diagnostic(ErrorCode.WRN_AssignmentToLockOrDispose, "i").WithArguments("i"),
                // (11,21): warning CS0728: Possibly incorrect assignment to local 'i' which is the argument to a using or lock statement. The Dispose call or unlocking will happen on the original value of the local.
                Diagnostic(ErrorCode.WRN_AssignmentToLockOrDispose, "i").WithArguments("i"),
                // (12,22): warning CS0728: Possibly incorrect assignment to local 'i' which is the argument to a using or lock statement. The Dispose call or unlocking will happen on the original value of the local.
                Diagnostic(ErrorCode.WRN_AssignmentToLockOrDispose, "i").WithArguments("i"));
        }

        // The object could be created outside the "using" statement 
        [Fact]
        public void ResourceCreatedOutsideUsing()
        {
            var source = @"
using System;
class Program
{
    static void Main(string[] args)
    {
        MyManagedType mnObj1 = null;
        using (mnObj1)
        {
        }
    }
}
" + _managedClass;

            var compilation = CreateCompilation(source);
            VerifyDeclaredSymbolForUsingStatements(compilation);
        }

        // The object created inside the "using" statement but declared no variable
        [Fact]
        public void ResourceCreatedInsideUsingWithNoVarDeclared()
        {
            var source = @"
using System;
class Program
{
    static void Main(string[] args)
    {
        using (new MyManagedType())
        {
        }
    }
}
" + _managedStruct;
            var compilation = CreateCompilation(source);
            VerifyDeclaredSymbolForUsingStatements(compilation);
        }

        // Multiple resource created inside Using
        /// <bug id="10509" project="Roslyn"/>
        [Fact()]
        public void MultipleResourceCreatedInsideUsing()
        {
            var source = @"
using System;
class Program
{
    static void Main(string[] args)
    {
        using (MyManagedType mnObj1 = null, mnObj2 = default(MyManagedType))
        {
        }
    }
}
" + _managedStruct;

            var compilation = CreateCompilation(source);
            var symbols = VerifyDeclaredSymbolForUsingStatements(compilation, 1, "mnObj1", "mnObj2");
            foreach (var x in symbols)
            {
                VerifySymbolInfoForUsingStatements(compilation, x.Type);
            }
        }

        [Fact]
        public void MultipleResourceCreatedInNestedUsing()
        {
            var source = @"
using System;
class Program
{
    static void Main(string[] args)
    {
        using (MyManagedType mnObj1 = null, mnObj2 = default(MyManagedType))
        {
            using (MyManagedType mnObj3 = null, mnObj4 = default(MyManagedType))
            {
                mnObj3.Dispose(); 
            }
        }
    }
}
" + _managedClass;

            var compilation = CreateCompilation(source);

            var symbols = VerifyDeclaredSymbolForUsingStatements(compilation, 2, "mnObj3", "mnObj4");
            foreach (var x in symbols)
            {
                var localSymbol = x;
                VerifyLookUpSymbolForUsingStatements(compilation, localSymbol, 2);
                VerifySymbolInfoForUsingStatements(compilation, x.Type, 2);
            }
        }

        [Fact]
        public void ResourceTypeDerivedFromClassImplementIdisposable()
        {
            var source = @"
using System;
class Program
{
    public static void Main(string[] args)
    {
        using (MyManagedTypeDerived mnObj = new MyManagedTypeDerived())
        {
        }
    }
}
class MyManagedTypeDerived : MyManagedType
{ }
" + _managedClass;

            var compilation = CreateCompilation(source);

            var symbols = VerifyDeclaredSymbolForUsingStatements(compilation, 1, "mnObj");
            foreach (var x in symbols)
            {
                var localSymbol = x;
                VerifyLookUpSymbolForUsingStatements(compilation, localSymbol, 1);
                VerifySymbolInfoForUsingStatements(compilation, x.Type, 1);
            }
        }

        [Fact]
        public void LinqInUsing()
        {
            var source = @"
using System;
using System.Linq;
class Program
{
    public static void Main(string[] args)
    {
        using (var mnObj = (from x in ""1"" select new MyManagedType()).First () )
        {
        }
    }
}
" + _managedClass;

            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);

            var symbols = VerifyDeclaredSymbolForUsingStatements(compilation, 1, "mnObj");
            foreach (var x in symbols)
            {
                var localSymbol = x;
                VerifyLookUpSymbolForUsingStatements(compilation, localSymbol, 1);
                VerifySymbolInfoForUsingStatements(compilation, x.Type, 1);
            }
        }

        [Fact]
        public void LambdaInUsing()
        {
            var source = @"
using System;
using System.Linq;
class Program
{
    public static void Main(string[] args)
    {
        MyManagedType[] mnObjs = { };
        using (var mnObj = mnObjs.Where(x => x.ToString() == "").First())
        {
        }
    }
}
" + _managedStruct;

            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);

            var symbols = VerifyDeclaredSymbolForUsingStatements(compilation, 1, "mnObj");
            foreach (var x in symbols)
            {
                var localSymbol = x;
                VerifyLookUpSymbolForUsingStatements(compilation, localSymbol, 1);
                VerifySymbolInfoForUsingStatements(compilation, x.Type, 1);
            }
        }

        [Fact]
        public void UsingForGenericType()
        {
            var source = @"
using System;
using System.Collections.Generic;
class Test<T>
{
    public static IEnumerator<T> M<U>(IEnumerable<T> items) where U : IDisposable, new()
    {
        using (U u = new U())
        {
        }
        return null;
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);

            var symbols = VerifyDeclaredSymbolForUsingStatements(compilation, 1, "u");
            foreach (var x in symbols)
            {
                var localSymbol = x;
                VerifyLookUpSymbolForUsingStatements(compilation, localSymbol, 1);
                VerifySymbolInfoForUsingStatements(compilation, x.Type, 1);
            }
        }

        [Fact]
        public void UsingForGenericTypeWithClassConstraint()
        {
            var source = @"using System;
class A { }
class B : A, IDisposable
{
    void IDisposable.Dispose() { }
}
class C
{
    static void M<T0, T1, T2, T3, T4>(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4)
        where T0 : A
        where T1 : A, IDisposable
        where T2 : B
        where T3 : T1
        where T4 : T2
    {
        using (t0) { }
        using (t1) { }
        using (t2) { }
        using (t3) { }
        using (t4) { }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (16,16): error CS1674: 'T0': type used in a using statement must implement 'System.IDisposable'.
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "t0").WithArguments("T0").WithLocation(16, 16));
        }

        [WorkItem(543168, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543168")]
        [Fact]
        public void EmbeddedDeclaration()
        {
            var source = @"
class C
{
    static void Main()
    {
        using(null) object o = new object();
    }
}
";

            CreateCompilation(source).VerifyDiagnostics(
                // (6,20): error CS1023: Embedded statement cannot be a declaration or labeled statement
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "object o = new object();"));
        }

        [WorkItem(529547, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529547")]
        [Fact]
        public void UnusedLocal()
        {
            var source = @"
using System;

class C : IDisposable
{
    public void Dispose()
    {
    }
}

struct S : IDisposable
{
    public void Dispose()
    {
    }
}

public class Test
{
    public static void Main()
    {
        using (S s = new S()) { } //fine
        using (C c = new C()) { } //fine
    }
}";

            CreateCompilation(source).VerifyDiagnostics();
        }

        [WorkItem(545331, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545331")]
        [Fact]
        public void MissingIDisposable()
        {
            var source = @"
namespace System
{
    public class Object { }
    public class Void { }
}
class C
{
    void M()
    {
        using (var v = null) ;
    }
}";

            CreateEmptyCompilation(source).VerifyDiagnostics(
                // (11,20): error CS0815: Cannot assign <null> to an implicitly-typed variable
                //         using (var v = null) ;
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "v = null").WithArguments("<null>").WithLocation(11, 20),
                // (11,30): warning CS0642: Possible mistaken empty statement
                //         using (var v = null) ;
                Diagnostic(ErrorCode.WRN_PossibleMistakenNullStatement, ";").WithLocation(11, 30));
        }

        [Fact]
        public void MissingIDisposable_NoLocal()
        {
            var source = @"
namespace System
{
    public class Object { }
    public class Void { }
}
class C
{
    void M()
    {
        using (null);
    }
}";

            CreateEmptyCompilation(source).VerifyDiagnostics(
                // (11,9): error CS0518: Predefined type 'System.IDisposable' is not defined or imported
                //         using (null);
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "using").WithArguments("System.IDisposable").WithLocation(11, 9),
                // (11,21): warning CS0642: Possible mistaken empty statement
                //         using (null);
                Diagnostic(ErrorCode.WRN_PossibleMistakenNullStatement, ";").WithLocation(11, 21)
                );
        }

        [WorkItem(9581, "https://github.com/dotnet/roslyn/issues/9581")]
        [Fact]
        public void TestCyclicInference()
        {
            var source = @"
class C
{
    void M()
    {
        using (var v = v) { }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,24): error CS0841: Cannot use local variable 'v' before it is declared
                //         using (var v = v) { }
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "v").WithArguments("v").WithLocation(6, 24)
                );
        }

        [Fact]
        public void SemanticModel_02()
        {
            var source = @"
class C
{
    static void Main()
    {
        System.IDisposable i = null;

        using (i)
        {
            int x;
            x = 1;
        }
    }
}
";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithFeature(FeatureFlag.RunNullableAnalysis, "never"));

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var node = tree.GetCompilationUnitRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().Single();

            Assert.Equal(SpecialType.System_Int32, model.GetTypeInfo(node).Type.SpecialType);
        }

        #region help method

        private IEnumerable<ILocalSymbol> VerifyDeclaredSymbolForUsingStatements(CSharpCompilation compilation, int index = 1, params string[] variables)
        {
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var usingStatements = tree.GetCompilationUnitRoot().DescendantNodes().OfType<UsingStatementSyntax>().ToList();
            var i = 0;
            foreach (var x in usingStatements[index - 1].Declaration.Variables)
            {
                var symbol = model.GetDeclaredSymbol(x);
                Assert.Equal(SymbolKind.Local, symbol.Kind);
                Assert.Equal(variables[i++].ToString(), symbol.ToDisplayString());
                yield return (ILocalSymbol)symbol;
            }
        }

        private SymbolInfo VerifySymbolInfoForUsingStatements(CSharpCompilation compilation, ISymbol symbol, int index = 1)
        {
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var usingStatements = tree.GetCompilationUnitRoot().DescendantNodes().OfType<UsingStatementSyntax>().ToList();

            var type = model.GetSymbolInfo(usingStatements[index - 1].Declaration.Type);

            Assert.Equal(symbol, type.Symbol);

            return type;
        }

        private ISymbol VerifyLookUpSymbolForUsingStatements(CSharpCompilation compilation, ISymbol symbol, int index = 1)
        {
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);
            var usingStatements = tree.GetCompilationUnitRoot().DescendantNodes().OfType<UsingStatementSyntax>().ToList();

            var actualSymbol = model.LookupSymbols(usingStatements[index - 1].SpanStart, name: symbol.Name).Single();
            Assert.Equal(SymbolKind.Local, actualSymbol.Kind);
            Assert.Equal(symbol, actualSymbol);
            return actualSymbol;
        }

        #endregion
    }
}
