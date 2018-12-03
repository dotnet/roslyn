// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
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

        private const string _asyncDisposable = @"
namespace System
{
    public interface IAsyncDisposable
    {
        System.Threading.Tasks.ValueTask DisposeAsync();
    }
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
            var declaredLocal = (LocalSymbol)declaredSymbol;
            Assert.Equal("i", declaredLocal.Name);
            Assert.Equal(SpecialType.System_IDisposable, declaredLocal.Type.SpecialType);

            var memberAccessExpression = tree.GetCompilationUnitRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single();

            var info = model.GetSymbolInfo(memberAccessExpression.Expression);
            Assert.NotNull(info);
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
                // (6,16): error CS1674: 'method group': type used in a using statement must have a public void-returning Dispose() instance method.
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "Main").WithArguments("method group"));
        }

        [Fact]
        public void UsingPatternTest()
        {
            var source = @"
class C1
{
    public C1() { }
    public void Dispose() { }
}

class C2
{
    static void Main()
    {
        using (C1 c = new C1())
        {
        }
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UsingPatternSameSignatureAmbiguousTest()
        {
            var source = @"
class C1
{
    public C1() { }
    public void Dispose() { }
    public void Dispose() { }
}

class C2
{
    static void Main()
    {
        using (C1 c = new C1())
        {
        }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,17): error CS0111: Type 'C1' already defines a member called 'Dispose' with the same parameter types
                //     public void Dispose() { }
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "Dispose").WithArguments("Dispose", "C1").WithLocation(6, 17),
                // (13,16): error CS0121: The call is ambiguous between the following methods or properties: 'C1.Dispose()' and 'C1.Dispose()'
                //         using (C1 c = new C1())
                Diagnostic(ErrorCode.ERR_AmbigCall, "C1 c = new C1()").WithArguments("C1.Dispose()", "C1.Dispose()").WithLocation(13, 16),
                // (13,16): error CS1674: 'C1': type used in a using statement must have a public void-returning Dispose() instance method.
                //         using (C1 c = new C1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "C1 c = new C1()").WithArguments("C1").WithLocation(13, 16)
                );
        }

        [Fact]
        public void UsingPatternAmbiguousOverloadTest()
        {
            var source = @"
class C1
{
    public C1() { }
    public void Dispose() { }
    public bool Dispose() { return false; }
}

class C2
{
    static void Main()
    {
        using (C1 c = new C1())
        {
        }
        C1 c1b = new C1();
        using (c1b) { }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,17): error CS0111: Type 'C1' already defines a member called 'Dispose' with the same parameter types
                //     public bool Dispose() { return false; }
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "Dispose").WithArguments("Dispose", "C1").WithLocation(6, 17),
                // (13,16): error CS0121: The call is ambiguous between the following methods or properties: 'C1.Dispose()' and 'C1.Dispose()'
                //         using (C1 c = new C1())
                Diagnostic(ErrorCode.ERR_AmbigCall, "C1 c = new C1()").WithArguments("C1.Dispose()", "C1.Dispose()").WithLocation(13, 16),
                // (13,16): error CS1674: 'C1': type used in a using statement must be implicitly convertible to 'System.IDisposable' or have a public void-returning Dispose() instance method.
                //         using (C1 c = new C1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "C1 c = new C1()").WithArguments("C1").WithLocation(13, 16),
                // (17,16): error CS0121: The call is ambiguous between the following methods or properties: 'C1.Dispose()' and 'C1.Dispose()'
                //         using (c1b) { }
                Diagnostic(ErrorCode.ERR_AmbigCall, "c1b").WithArguments("C1.Dispose()", "C1.Dispose()").WithLocation(17, 16),
                // (17,16): error CS1674: 'C1': type used in a using statement must be implicitly convertible to 'System.IDisposable' or have a public void-returning Dispose() instance method.
                //         using (c1b) { }
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "c1b").WithArguments("C1").WithLocation(17, 16)
                );
        }

        [Fact]
        public void UsingPatternDifferentParameterOverloadTest()
        {
            var source = @"
class C1
{
    public C1() { }
    public void Dispose() { }
    public void Dispose(int x) { }
}

class C2
{
    static void Main()
    {
        using (C1 c = new C1())
        {
        }
        C1 c1b = new C1();
        using (c1b) { }
    }
}";
            // Shouldn't throw an error as the method signatures are different.
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UsingPatternInaccessibleAmbiguousTest()
        {
            var source = @"
class C1
{
    public C1() { }
    public void Dispose() { }
    internal void Dispose() { }
}

class C2
{
    static void Main()
    {
        using (C1 c = new C1())
        {
        }
        C1 c1b = new C1();
        using (c1b) { }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,19): error CS0111: Type 'C1' already defines a member called 'Dispose' with the same parameter types
                //     internal void Dispose() { }
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "Dispose").WithArguments("Dispose", "C1").WithLocation(6, 19),
                // (13,16): error CS0121: The call is ambiguous between the following methods or properties: 'C1.Dispose()' and 'C1.Dispose()'
                //         using (C1 c = new C1())
                Diagnostic(ErrorCode.ERR_AmbigCall, "C1 c = new C1()").WithArguments("C1.Dispose()", "C1.Dispose()").WithLocation(13, 16),
                // (13,16): error CS1674: 'C1': type used in a using statement must have a public void-returning Dispose() instance method.
                //         using (C1 c = new C1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "C1 c = new C1()").WithArguments("C1").WithLocation(13, 16),
                // (17,16): error CS0121: The call is ambiguous between the following methods or properties: 'C1.Dispose()' and 'C1.Dispose()'
                //         using (c1b) { }
                Diagnostic(ErrorCode.ERR_AmbigCall, "c1b").WithArguments("C1.Dispose()", "C1.Dispose()").WithLocation(17, 16),
                // (17,16): error CS1674: 'C1': type used in a using statement must have a public void-returning Dispose() instance method.
                //         using (c1b) { }
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "c1b").WithArguments("C1").WithLocation(17, 16)
                );
        }

        [Fact]
        public void UsingPatternInheritedTest()
        {
            var source = @"
class C1
{
    public C1() { }
    public void Dispose() { }
}

class C2 : C1
{
    internal void Dispose(int x) { } 
}

class C3
{
    static void Main()
    {
        using (C2 c = new C2())
        {
        }
        C2 c2b = new C2();
        using (c2b) { }
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UsingPatternLessDerivedAssignement()
        {
            var source = @"
class C1
{
    public C1() { }
    public void Dispose() { }
}

class C2
{
    static void Main()
    {
        using (object c1 = new C1())
        {
        }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (12,16): error CS1674: 'object': type used in a using statement must be implicitly convertible to 'System.IDisposable' or have a public void-returning Dispose() instance method.
                //         using (object c = new C1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "object c1 = new C1()").WithArguments("object").WithLocation(12, 16)
                );
        }

        [Fact]
        public void UsingPatternImplicitConversionToDisposable()
        {
            var source = @"
class C1
{
    public C1() { }
    public void Dispose() { }

    //  User-defined implicit conversion from C2 to C1
    public static implicit operator C1(C2 o)
    {
        return new C1();
    }
}

class C2
{
}

class C3
{
    static void Main()
    {
        using (C1 c = new C2())
        {
        }
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UsingPatternHidingInheritedWithNonMatchingMethodTest()
        {
            var source = @"
class C1
{
    public C1() { }
    public void Dispose() { }
}

class C2 : C1
{
    public new int Dispose() { return 0; } 
}

class C3
{
    static void Main()
    {
        using (C2 c = new C2())
        {
        }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (17,16): warning CS0280: 'C2' does not implement the 'disposable' pattern. 'C2.Dispose()' has the wrong signature.
                //         using (C2 c = new C2())
                Diagnostic(ErrorCode.WRN_PatternBadSignature, "C2 c = new C2()").WithArguments("C2", "disposable", "C2.Dispose()").WithLocation(17, 16),
                // (17,16): error CS1674: 'C2': type used in a using statement must be implicitly convertible to 'System.IDisposable' or have a public void-returning Dispose() instance method.
                //         using (C2 c = new C2())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "C2 c = new C2()").WithArguments("C2").WithLocation(17, 16)
                );
        }

        [Fact]
        public void UsingPatternHidingInheritedWithPropertyTest()
        {
            var source = @"
class C1
{
    public C1() { }
    public void Dispose() { }
}

class C2 : C1
{
    public new int Dispose { get; } 
}

class C3
{
    static void Main()
    {
        using (C2 c = new C2())
        {
        }
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UsingPatternHidingInvalidInheritedWithPropertyTest()
        {
            var source = @"
class C1
{
    public C1() { }
    public int Dispose() { return 0; }
}

class C2 : C1
{
    public new int Dispose { get; } 
}

class C3
{
    static void Main()
    {
        using (C2 c = new C2())
        {
        }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (17,16): warning CS0280: 'C2' does not implement the 'disposable' pattern. 'C1.Dispose()' has the wrong signature.
                //         using (C2 c = new C2())
                Diagnostic(ErrorCode.WRN_PatternBadSignature, "C2 c = new C2()").WithArguments("C2", "disposable", "C1.Dispose()").WithLocation(17, 16),
                // (17,16): error CS1674: 'C2': type used in a using statement must be implicitly convertible to 'System.IDisposable' or have a public void-returning Dispose() instance method.
                //         using (C2 c = new C2())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "C2 c = new C2()").WithArguments("C2").WithLocation(17, 16)
                );
        }

        [Fact]
        public void UsingPatternExtensionMethodTest()
        {
            var source = @"
class C1
{
    public C1() { }
}

static class C2 
{
    public static void Dispose(this C1 c1) { }
}

class C3
{
    static void Main()
    {
        using (C1 c = new C1())
        {
        }
        C1 c1b = new C1();
        using (c1b) { }
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UsingPatternExtensionMethodPropertyHidingTest()
        {
            var source = @"
class C1
{
    public C1() { }

    public int Dispose { get; }
}

static class C2 
{
    public static void Dispose(this C1 c1) { }
}

class C3
{
    static void Main()
    {
        using (C1 c = new C1())
        {
        }
        C1 c1b = new C1();
        using (c1b) { }
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UsingPatternExtensionMethodInaccessibleInstanceMethodTest()
        {
            var source = @"
using System;
class C1
{
    public C1() { }

    protected void Dispose() { }
}

static class C2 
{
    public static void Dispose(this C1 c1)
    {
        Console.WriteLine(""C2.Dispose(C1)"");
    }
}

class C3
{
    static void Main()
    {
        using (C1 c = new C1())
        {
        }
    }
}";
            CreateCompilation(source).VerifyDiagnostics();

            CompileAndVerify(source, expectedOutput: "C2.Dispose(C1)");
        }

        [Fact]
        public void UsingPatternStaticMethodTest()
        {
            var source = @"
class C1
{
    public C1() { }

    public static void Dispose() { }
}

class C2
{
    static void Main()
    {
        using (C1 c = new C1())
        {
        }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (14,16): error CS0176: Member 'C1.Dispose()' cannot be accessed with an instance reference; qualify it with a type name instead
                //         using (C1 c = new C1())
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "C1 c = new C1()").WithArguments("C1.Dispose()").WithLocation(13, 16),
                // (14,16): error CS1674: 'C1': type used in a using statement must be implicitly convertible to 'System.IDisposable' or have a public void-returning Dispose() instance method.
                //         using (C1 c = new C1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "C1 c = new C1()").WithArguments("C1").WithLocation(13, 16)
                );
        }


        [Fact]
        public void UsingPatternHidingInvalidInheritedWithPropertyAndValidExtensionMethodTest()
        {
            var source = @"
class C1
{
    public C1() { }
    public int Dispose() { return 0; }
}

class C2 : C1
{
    public new int Dispose { get; } 
}

static class C3
{
    public static void Dispose(this C1 c1){ }
}

class C4
{
    static void Main()
    {
        using (C2 c = new C2())
        {
        }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (22,16): warning CS0280: 'C2' does not implement the 'disposable' pattern. 'C1.Dispose()' has the wrong signature.
                //         using (C2 c = new C2())
                Diagnostic(ErrorCode.WRN_PatternBadSignature, "C2 c = new C2()").WithArguments("C2", "disposable", "C1.Dispose()").WithLocation(22, 16),
                // (22,16): error CS1674: 'C2': type used in a using statement must be implicitly convertible to 'System.IDisposable' or have a public void-returning Dispose() instance method.
                //         using (C2 c = new C2())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "C2 c = new C2()").WithArguments("C2").WithLocation(22, 16)
                );
        }

        [Fact]
        public void UsingPatternHidingInvalidInheritedWithPropertyAndInvalidExtensionMethodTest()
        {
            var source = @"
class C1
{
    public C1() { }
    public int Dispose() { return 0; }
}

class C2 : C1
{
    public new int Dispose { get; } 
}

static class C3
{
    public static int Dispose(this C1 c1){ return 0; }
}

class C4
{
    static void Main()
    {
        using (C2 c = new C2())
        {
        }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (22,16): warning CS0280: 'C2' does not implement the 'disposable' pattern. 'C1.Dispose()' has the wrong signature.
                //         using (C2 c = new C2())
                Diagnostic(ErrorCode.WRN_PatternBadSignature, "C2 c = new C2()").WithArguments("C2", "disposable", "C1.Dispose()").WithLocation(22, 16),
                // (22,16): error CS1674: 'C2': type used in a using statement must be implicitly convertible to 'System.IDisposable' or have a public void-returning Dispose() instance method.
                //         using (C2 c = new C2())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "C2 c = new C2()").WithArguments("C2").WithLocation(22, 16)
                );
        }

        [Fact]
        public void UsingPatternAmbiguousExtensionMethodTest()
        {
            var source = @"
class C1
{
    public C1() { }
}

static class C2 
{
    public static void Dispose(this C1 c1) { }
}

static class C3 
{
    public static void Dispose(this C1 c1) { }
}

class C4
{
    static void Main()
    {
        using (C1 c = new C1())
        {
        }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (21,16): error CS0121: The call is ambiguous between the following methods or properties: 'C2.Dispose(C1)' and 'C3.Dispose(C1)'
                //         using (C1 c = new C1())
                Diagnostic(ErrorCode.ERR_AmbigCall, "C1 c = new C1()").WithArguments("C2.Dispose(C1)", "C3.Dispose(C1)").WithLocation(21, 16),
                // (21,16): error CS1674: 'C1': type used in a using statement must be implicitly convertible to 'System.IDisposable' or have a public void-returning Dispose() instance method.
                //         using (C1 c = new C1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "C1 c = new C1()").WithArguments("C1").WithLocation(21, 16)
                );
        }

        [Fact]
        public void UsingPatternScopedExtensionMethodTest()
        {
            var source = @"
class C1
{
    public C1() { }
}

namespace N1
{
    static class C2 
    {
        public static void Dispose(this C1 c1) { }
    }
}

namespace N2
{
    static class C3 
    {
        public static void Dispose(this C1 c1) { }
    }
}

namespace N3
{
    static class C4 
    {
        public static int Dispose(this C1 c1) { return 0; }
    }
}


namespace N4
{
    partial class C5
    {
        static void M()
        {
            using (C1 c = new C1()) // error 1: no extension in scope
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
            using (C1 c = new C1()) // success: resolve against C2.Dispose
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
            using (C1 c = new C1()) // error 2: C4.Dispose does not match pattern
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
            using (C1 c = new C1())  // error 3: C2.Dispose and C4.Dispose are ambiguous
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
                using (C1 c = new C1())  // error 4: C4.Dispose does not match pattern
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
                    using (C1 c = new C1())  // success: resolve against C2.Dispose
                    { 
                    }
                }
            }
        }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (38,20): error CS1674: 'C1': type used in a using statement must be implicitly convertible to 'System.IDisposable' or have a public void-returning Dispose() instance method.
                //             using (C1 c = new C1()) // error 1: no extension in scope
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "C1 c = new C1()").WithArguments("C1").WithLocation(38, 20),
                // (64,20): warning CS0280: 'C1' does not implement the 'disposable' pattern. 'C4.Dispose(C1)' has the wrong signature.
                //             using (C1 c = new C1()) // error 2: C4.Dispose does not match pattern
                Diagnostic(ErrorCode.WRN_PatternBadSignature, "C1 c = new C1()").WithArguments("C1", "disposable", "N3.C4.Dispose(C1)").WithLocation(64, 20),
                // (64,20): error CS1674: 'C1': type used in a using statement must be implicitly convertible to 'System.IDisposable' or have a public void-returning Dispose() instance method.
                //             using (C1 c = new C1()) // error 2: C4.Dispose does not match pattern
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "C1 c = new C1()").WithArguments("C1").WithLocation(64, 20),
                // (78,20): error CS0121: The call is ambiguous between the following methods or properties: 'N1.C2.Dispose(C1)' and 'N3.C4.Dispose(C1)'
                //             using (C1 c = new C1())  // error 3: C2.Dispose and C4.Dispose are ambiguous
                Diagnostic(ErrorCode.ERR_AmbigCall, "C1 c = new C1()").WithArguments("N1.C2.Dispose(C1)", "N3.C4.Dispose(C1)").WithLocation(78, 20),
                // (78,20): error CS1674: 'C1': type used in a using statement must be implicitly convertible to 'System.IDisposable' or have a public void-returning Dispose() instance method.
                //             using (C1 c = new C1())  // error 3: C2.Dispose and C4.Dispose are ambiguous
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "C1 c = new C1()").WithArguments("C1").WithLocation(78, 20),
                // (93,24): warning CS0280: 'C1' does not implement the 'disposable' pattern. 'C4.Dispose(C1)' has the wrong signature.
                //                 using (C1 c = new C1())  // error 4: C4.Dispose does not match pattern
                Diagnostic(ErrorCode.WRN_PatternBadSignature, "C1 c = new C1()").WithArguments("C1", "disposable", "N3.C4.Dispose(C1)").WithLocation(93, 24),
                // (93,24): error CS1674: 'C1': type used in a using statement must be implicitly convertible to 'System.IDisposable' or have a public void-returning Dispose() instance method.
                //                 using (C1 c = new C1())  // error 4: C4.Dispose does not match pattern
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "C1 c = new C1()").WithArguments("C1").WithLocation(93, 24)
                );
        }

        [Fact]
        public void UsingPatternWithMultipleExtensionTargets()
        {
            var source = @"
class C1
{
}

class C2
{
}

static class C3 
{
    public static void Dispose(this C1 c1) { }

    public static void Dispose(this C2 c2) { }

}

class C4
{
    static void Main()
    {
        using (C1 c = new C1())
        {
        }
        C1 c1b = new C1();
        using (c1b) { }

        using (C2 c = new C2())
        {
        }
        C2 c2b = new C2();
        using (c2b) { }
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UsingPatternExtensionMethodWithLessDerivedTarget()
        {
            var source = @"
class C1
{
}

class C2 : C1
{
}

static class C3 
{
    public static void Dispose(this C1 c1) { }

}

class C4
{
    static void Main()
    {
        using (C2 c = new C2())
        {
        }
        C2 c2b = new C2();
        using (c2b) { }
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UsingPatternWithValidTargetAndLessDerivedTarget()
        {
            var source = @"
using System;
class C1
{
}

class C2 : C1
{
}

static class C3 
{
    public static void Dispose(this C1 c1)
    {
        Console.WriteLine(""C3.Dispose(C1)"");
    }

    public static void Dispose(this C2 c2)
    {
        Console.WriteLine(""C3.Dispose(C2)"");
    }
}

class C4
{
    static void Main()
    {
       using (C2 c = new C2())
       {
       }
    }
}";
            // ensure we bind without errors
            CreateCompilation(source).VerifyDiagnostics();

            // check we're calling the correct extension
            CompileAndVerify(source, expectedOutput: "C3.Dispose(C2)");
        }

        [Fact]
        public void UsingPatternExtensionMethodNonPublic()
        {
            var source = @"
class C1
{
}

static class C2 
{
   internal static void Dispose(this C1 c1) { }
}

class C3
{
    static void Main()
    {
       using (C1 c = new C1())
       {
       }
       C1 c1b = new C1();
       using (c1b) { }
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UsingPatternExtensionMethodGeneric()
        {
            var source = @"
class C1
{
}

static class C2 
{
   public static void Dispose<T>(this T c1) where T : C1 { }
}

class C3
{
    static void Main()
    {
       using (C1 c = new C1())
       {
       }
       C1 c1b = new C1();
       using (c1b) { }
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UsingPatternExtensionMethodWithDefaultArguments()
        {
            var source = @"
class C1
{
}

static class C2 
{
   internal static void Dispose(this C1 c1, int a = 1) { }
}

class C3
{
    static void Main()
    {
       using (C1 c = new C1())
       {
       }
       C1 c1b = new C1();
       using (c1b) { }
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UsingPatternExtensionMethodOnObject()
        {
            var source = @"
class C1
{
}

static class C2 
{
   internal static void Dispose(this object o) { }
}

class C3
{
    static void Main()
    {
        using (C1 c = new C1()) { }
        using (System.Object o = new System.Object()) { }
        using (System.Uri uri = new System.Uri(""http://example.com"")) { }
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UsingPatternExtensionMethodMixOfGeneric()
        {
            var source = @"
using System;
class C1
{
}

static class C2 
{
   public static void Dispose(this C1 c1)
    {
        Console.WriteLine(""C2.Dispose(C1)"");
    }

   public static void Dispose<T>(this T c1) where T : C1
    {
        Console.WriteLine(""C2.Dispose<T>(T)"");
    }

}

class C3
{
    static void Main()
    {
       using (C1 c = new C1())
       {
       }
    }
}";
            // ensure we bind without errors
            CreateCompilation(source).VerifyDiagnostics();

            // check we call the correct overload
            CompileAndVerify(source, expectedOutput: "C2.Dispose(C1)");
        }

        [Fact]
        public void UsingPatternExtensionMethodMixOfGenericAndMoreDerived()
        {
            var source = @"
using System;
class C1
{
}

class C2 : C1
{
}

class C3 : C1
{
}

static class C4
{
    public static void Dispose<T>(this T c1) where T : C1
    {
        Console.Write(""C4.Dispose<T>(T) "");
    }

    public static void Dispose(this C3 c3)
    {
        Console.Write(""C4.Dispose(C3) "");
    }
}

class C5
{
    static void Main()
    {
        using (C1 c = new C1()) { } // Dispose<T>

        using (C2 c = new C2()) { } // Dispose<T>
        
        using (C3 c = new C3()) { } // Dispose(C3)
    }
}";
            // ensure we bind without errors
            CreateCompilation(source).VerifyDiagnostics();

            // check we call the correct overload
            CompileAndVerify(source, expectedOutput: $"C4.Dispose<T>(T) C4.Dispose<T>(T) C4.Dispose(C3)");
        }

        [Fact]
        public void UsingPatternExtensionMethodOnInStruct()
        {
            var source = @"
struct S1
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
            var compilation = CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UsingPatternExtensionMethodRefParameterOnStruct()
        {
            var source = @"
struct S1
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
                // (15,15): error CS1674: 'S1': type used in a using statement must be implicitly convertible to 'System.IDisposable' or have a public void-returning Dispose() instance method.
                //        using (S1 s = new S1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "S1 s = new S1()").WithArguments("S1").WithLocation(15, 15),
                // (15,18): error CS1657: Cannot use 's' as a ref or out value because it is a 'using variable'
                //        using (S1 s = new S1())
                Diagnostic(ErrorCode.ERR_RefReadonlyLocalCause, "s = new S1()").WithArguments("s", "using variable").WithLocation(15, 18)
                );
        }

        [Fact]
        public void UsingPatternExtensionMethodInParameterOnStruct()
        {
            var source = @"
struct S1
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

            var compilation = CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UsingPatternWithParamsTest()
        {
            var source = @"
class C1
{
    public C1() { }
    public void Dispose(params int[] args){ }
}

class C2
{
    static void Main()
    {
        using (C1 c = new C1())
        {
        }
        C1 c1b = new C1();
        using (c1b) { }
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UsingPatternWithDefaultParametersTest()
        {
            var source = @"
class C1
{
    public C1() { }
    public void Dispose(int a = 4){ }
}

class C2
{
    static void Main()
    {
        using (C1 c = new C1())
        {
        }
        C1 c1b = new C1();
        using (c1b) { }
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UsingPatternWithRefStructTest()
        {
            var source = @"
public class Program
{
    static void Main(string[] args)
    {
        using (new S1())
        {
        }
    }

    public ref struct S1 
    {
        public void Dispose() { System.Console.WriteLine(""S1.Dispose()""); }
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UsingPatternWrongReturnTest()
        {
            var source = @"
class C1
{
    public C1() { }
    public bool Dispose() { return false; }
}

class C2
{
    static void Main()
    {
        using (C1 c = new C1())
        {
        }
        C1 c1b = new C1();
        using (c1b) { }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (12,16): warning CS0280: 'C1' does not implement the 'disposable' pattern. 'C1.Dispose()' has the wrong signature.
                //         using (C1 c = new C1())
                Diagnostic(ErrorCode.WRN_PatternBadSignature, "C1 c = new C1()").WithArguments("C1", "disposable", "C1.Dispose()").WithLocation(12, 16),
                // (12,16): error CS1674: 'C1': type used in a using statement must have a public void-returning Dispose() instance method.
                //         using (C1 c = new C1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "C1 c = new C1()").WithArguments("C1").WithLocation(12, 16),
                // (16,16): warning CS0280: 'C1' does not implement the 'disposable' pattern. 'C1.Dispose()' has the wrong signature.
                //         using (c1b) { }
                Diagnostic(ErrorCode.WRN_PatternBadSignature, "c1b").WithArguments("C1", "disposable", "C1.Dispose()").WithLocation(16, 16),
                // (16,16): error CS1674: 'C1': type used in a using statement must have a public void-returning Dispose() instance method.
                //         using (c1b) { }
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "c1b").WithArguments("C1").WithLocation(16, 16)
                );
        }

        [Fact]
        public void UsingPatternWrongAccessibilityTest()
        {
            var source = @"
class C1
{
    public C1() { }
    private void Dispose() { }
}

class C2
{
    static void Main()
    {
        using (C1 c = new C1())
        {
        }
        C1 c1b = new C1();
        using (c1b) { }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (12,16): error CS0122: 'C1.Dispose()' is inaccessible due to its protection level
                //         using (C1 c = new C1())
                Diagnostic(ErrorCode.ERR_BadAccess, "C1 c = new C1()").WithArguments("C1.Dispose()").WithLocation(12, 16),
                // (12,16): error CS1674: 'C1': type used in a using statement must be implicitly convertible to 'System.IDisposable' or have a public void-returning Dispose() instance method.
                //         using (C1 c = new C1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "C1 c = new C1()").WithArguments("C1").WithLocation(12, 16),
                // (16,16): error CS0122: 'C1.Dispose()' is inaccessible due to its protection level
                //         using (c1b) { }
                Diagnostic(ErrorCode.ERR_BadAccess, "c1b").WithArguments("C1.Dispose()").WithLocation(16, 16),
                // (16,16): error CS1674: 'C1': type used in a using statement must be implicitly convertible to 'System.IDisposable' or have a public void-returning Dispose() instance method.
                //         using (c1b) { }
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "c1b").WithArguments("C1").WithLocation(16, 16)
                );
        }

        [Fact]
        public void UsingPatternNonPublicAccessibilityTest()
        {
            var source = @"
class C1
{
    public C1() { }
    internal void Dispose() { }
}

class C2
{
    static void Main()
    {
        using (C1 c = new C1())
        {
        }
        C1 c1b = new C1();
        using (c1b) { }
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UsingPatternStaticMethod()
        {
            var source = @"
class C1
{
    public static void Dispose() { }
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
                // (11,16): error CS0176: Member 'C1.Dispose()' cannot be accessed with an instance reference; qualify it with a type name instead
                //         using (C1 c1 = new C1())
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "C1 c1 = new C1()").WithArguments("C1.Dispose()").WithLocation(11, 16),
                // (11,16): error CS1674: 'C1': type used in a using statement must be implicitly convertible to 'System.IDisposable' or have a public void-returning Dispose() instance method.
                //         using (C1 c1 = new C1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "C1 c1 = new C1()").WithArguments("C1").WithLocation(11, 16)
                );
        }

        [Fact]
        public void UsingPatternGenericMethodTest()
        {
            var source = @"
class C1
{
    public C1() { }
    public void Dispose<T>() { }
}

class C2
{
    static void Main()
    {
        using (C1 c = new C1())
        {
        }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (12,16): error CS0411: The type arguments for method 'C1.Dispose<T>()' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         using (C1 c = new C1())
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "C1 c = new C1()").WithArguments("C1.Dispose<T>()").WithLocation(12, 16),
                // (12,16): error CS1674: 'C1': type used in a using statement must be implicitly convertible to 'System.IDisposable' or have a public void-returning Dispose() instance method.
                //         using (C1 c = new C1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "C1 c = new C1()").WithArguments("C1").WithLocation(12, 16)
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
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UsingPatternAsyncTest()
        {
            var source = @"
using System.Threading.Tasks;
class C1
{
    public C1() { }
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
        await using (C1 c = new C1())
        {
        }
    }
}";
            var compilation = CreateCompilationWithTasksExtensions(source + _asyncDisposable, options: TestOptions.DebugExe).VerifyDiagnostics();

            CompileAndVerify(compilation, expectedOutput: "Dispose async");
        }

        [Fact]
        public void UsingPatternAsyncWithTaskLikeReturnTest()
        {
            var source = @"
using System;
using System.Runtime.CompilerServices;
class C1
{
    public C1() { }
    public System.Threading.Tasks.Task DisposeAsync() { return System.Threading.Tasks.Task.CompletedTask; }
}

class C2
{
    public C2() { }
    public MyTask DisposeAsync() { return new MyTask(); }
}

[AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
struct MyTask
{
    internal Awaiter GetAwaiter() => new Awaiter();
    internal class Awaiter : INotifyCompletion
    {
        public void OnCompleted(Action a) { }
        internal bool IsCompleted => true;
        internal void GetResult() { }
    }
}

struct MyTaskMethodBuilder
{
    private MyTask _task;
    public static MyTaskMethodBuilder Create() => new MyTaskMethodBuilder(new MyTask());
    internal MyTaskMethodBuilder(MyTask task)
    {
        _task = task;
    }
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
    {
        stateMachine.MoveNext();
    }
    public void SetException(Exception e) { }
    public void SetResult() { }
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public MyTask Task => _task;
}



class C3
{
    static async System.Threading.Tasks.Task Main()
    {
        await using (C1 c = new C1())
        {
        }

        await using (C2 c = new C2())
        {
        }
    }
}";
            CreateCompilationWithTasksExtensions(source + _asyncDisposable).VerifyDiagnostics();
        }

        [Fact]
        public void UsingPatternAsyncNoAwaitTest()
        {
            var source = @"
using System.Threading.Tasks;
class C1
{
    public ValueTask DisposeAsync()
    { 
        return new ValueTask(Task.CompletedTask); 
    }
}

class C2
{
    static async Task Main()
    {
        using (C1 c = new C1())
        {
        }
    }
}";
            CreateCompilationWithTasksExtensions(source + _asyncDisposable).VerifyDiagnostics(
                // (13,23): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     static async System.Threading.Tasks.Task Main()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "Main").WithLocation(13, 23),
                // (15,16): error CS1674: 'C1': type used in a using statement must be implicitly convertible to 'System.IDisposable' or have a public void-returning Dispose() instance method.
                //         using (C1 c = new C1())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "C1 c = new C1()").WithArguments("C1").WithLocation(15, 16)
                );
        }

        [Fact]
        public void UsingPatternAsyncAndSyncTest()
        {
            var source = @"
using System.Threading.Tasks;
class C1
{
    public ValueTask DisposeAsync()
    { 
        System.Console.Write(""Dispose async"");
        return new ValueTask(Task.CompletedTask); 
    }
    public void Dispose()
    { 
        System.Console.Write(""Dispose; "");
    }
}

class C2
{
    static async Task Main()
    {
        using (C1 c = new C1())
        {
        }
        await using (C1 c = new C1())
        {
        }
    }
}";
            var compilation = CreateCompilationWithTasksExtensions(source + _asyncDisposable, options: TestOptions.DebugExe).VerifyDiagnostics();

            CompileAndVerify(compilation, expectedOutput: "Dispose; Dispose async");
        }

        [Fact]
        public void UsingPatternAsyncExtensionMethodTest()
        {
            var source = @"
using System.Threading.Tasks;
class C1
{
}

static class C2 
{
    public static ValueTask DisposeAsync(this C1 c1) 
    { 
        System.Console.WriteLine(""Dispose async""); 
        return new ValueTask(Task.CompletedTask);
    }
}

class C3
{
    static async System.Threading.Tasks.Task Main()
    {
        await using (C1 c = new C1())
        {
        }
    }
}";
            var compilation = CreateCompilationWithTasksExtensions(source + _asyncDisposable, options: TestOptions.DebugExe).VerifyDiagnostics();

            CompileAndVerify(compilation, expectedOutput: "Dispose async");
        }

        [Fact]
        public void UsingPatternAsyncWrongReturnTypeTest()
        {
            var source = @"
class C1
{
    public bool DisposeAsync() { return false; }
}

class C2
{
    static async System.Threading.Tasks.Task Main()
    {
        await using (C1 c = new C1())
        {
        }
    }
}";
            CreateCompilationWithTasksExtensions(source + _asyncDisposable).VerifyDiagnostics(
                // (11,22): warning CS0280: 'C1' does not implement the 'disposable' pattern. 'C1.DisposeAsync()' has the wrong signature.
                //         await using (C1 c = new C1())
                Diagnostic(ErrorCode.WRN_PatternBadSignature, "C1 c = new C1()").WithArguments("C1", "disposable", "C1.DisposeAsync()").WithLocation(11, 22),
                // (11,22): error CS8410: 'C1': type used in an async using statement must be implicitly convertible to 'System.IAsyncDisposable'
                //         await using (C1 c = new C1())
                Diagnostic(ErrorCode.ERR_NoConvToIAsyncDisp, "C1 c = new C1()").WithArguments("C1").WithLocation(11, 22)
                );
        }

        [Fact]
        public void UsingPatternAsyncDefaultParametersTest()
        {
            var source = @"
using System.Threading.Tasks;
class C1
{
    public ValueTask DisposeAsync(int x = 4) 
    {
        return new ValueTask(Task.CompletedTask); 
    }
}

class C2
{
    static async Task Main()
    {
        await using (C1 c = new C1())
        {
        }
    }
}";
            CreateCompilationWithTasksExtensions(source + _asyncDisposable).VerifyDiagnostics();
        }

        [Fact]
        public void UsingPatternAsyncInterfaceDisposeTest()
        {
            var source = @"
using System.Threading.Tasks;
class C1 : System.IDisposable
{
    public ValueTask DisposeAsync() 
    { 
        System.Console.Write(""Dispose async; "");
        return new ValueTask(Task.CompletedTask);
    }

    public void Dispose()
    {
        System.Console.Write(""Dispose; "");
    }
}

class C2
{
    static async Task Main()
    {
        await using (C1 c = new C1())
        {
        }
        using (C1 c = new C1())
        {
        }
    }
}";
            var compilation = CreateCompilationWithTasksExtensions(source + _asyncDisposable, options: TestOptions.DebugExe).VerifyDiagnostics();

            CompileAndVerify(compilation, expectedOutput: "Dispose async; Dispose; ");
        }

        [Fact]
        public void UsingPatternDisposeInterfaceAsyncTest()
        {
            var source = @"
using System.Threading.Tasks;
class C1 : System.IAsyncDisposable
{
    public ValueTask DisposeAsync() 
    { 
        System.Console.Write(""Dispose async; "");
        return new ValueTask(Task.CompletedTask);
    }

    public void Dispose()
    {
        System.Console.Write(""Dispose; "");
    }
}

class C2
{
    static async Task Main()
    {
        await using (C1 c = new C1())
        {
        }
        using (C1 c = new C1())
        {
        }
    }
}";
            var compilation = CreateCompilationWithTasksExtensions(source + _asyncDisposable, options: TestOptions.DebugExe).VerifyDiagnostics();

            CompileAndVerify(compilation, expectedOutput: "Dispose async; Dispose; ");
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
                // (6,16): error CS1674: 'lambda expression': type used in a using statement must have a public void-returning Dispose() instance method.
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
            Assert.Equal(((LocalSymbol)declaredSymbol).Type.TypeSymbol, typeInfo.Symbol);
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

            var typeInfo = model.GetSymbolInfo(usingStatement.Declaration.Type);
            // lowest/last bound node with associated syntax is being picked up. Fine for now.
            Assert.Equal(((LocalSymbol)model.GetDeclaredSymbol(usingStatement.Declaration.Variables.Last())).Type.TypeSymbol, typeInfo.Symbol);
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
                VerifySymbolInfoForUsingStatements(compilation, ((LocalSymbol)x).Type.TypeSymbol);
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
                var localSymbol = (LocalSymbol)x;
                VerifyLookUpSymbolForUsingStatements(compilation, localSymbol, 2);
                VerifySymbolInfoForUsingStatements(compilation, ((LocalSymbol)x).Type.TypeSymbol, 2);
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
                var localSymbol = (LocalSymbol)x;
                VerifyLookUpSymbolForUsingStatements(compilation, localSymbol, 1);
                VerifySymbolInfoForUsingStatements(compilation, ((LocalSymbol)x).Type.TypeSymbol, 1);
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
                var localSymbol = (LocalSymbol)x;
                VerifyLookUpSymbolForUsingStatements(compilation, localSymbol, 1);
                VerifySymbolInfoForUsingStatements(compilation, ((LocalSymbol)x).Type.TypeSymbol, 1);
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
                var localSymbol = (LocalSymbol)x;
                VerifyLookUpSymbolForUsingStatements(compilation, localSymbol, 1);
                VerifySymbolInfoForUsingStatements(compilation, ((LocalSymbol)x).Type.TypeSymbol, 1);
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
                var localSymbol = (LocalSymbol)x;
                VerifyLookUpSymbolForUsingStatements(compilation, localSymbol, 1);
                VerifySymbolInfoForUsingStatements(compilation, ((LocalSymbol)x).Type.TypeSymbol, 1);
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
                // (16,16): error CS1674: 'T0': type used in a using statement must have a public void-returning Dispose() instance method.
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
                // (11,9): error CS0518: Predefined type 'System.IDisposable' is not defined or imported
                //         using (var v = null) ;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "using").WithArguments("System.IDisposable").WithLocation(11, 9),
                // (11,20): error CS0815: Cannot assign <null> to an implicitly-typed variable
                //         using (var v = null) ;
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "v = null").WithArguments("<null>").WithLocation(11, 20),
                // (11,30): warning CS0642: Possible mistaken empty statement
                //         using (var v = null) ;
                Diagnostic(ErrorCode.WRN_PossibleMistakenNullStatement, ";").WithLocation(11, 30));
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
        public void UsingVarInSwitchCase()
        {
            var source = @"
using System;
class C1 : IDisposable
    {
        public void Dispose() { }
    }
    class C2
    {
        public static void Main()
        {
            int x = 5;
            switch (x)
            {
                case 5:
                    using C1 o1 = new C1();
                    break;
            }
        }
    }";
            CreateCompilation(source).VerifyDiagnostics(
                // (15,21): error CS8389: A using variable cannot be used directly within a switch section (consider using braces). 
                //                     using C1 o1 = new C1();
                Diagnostic(ErrorCode.ERR_UsingVarInSwitchCase, "using C1 o1 = new C1();").WithLocation(15, 21)
            );
        }

        [Fact]
        public void DiagnosticsInUsingVariableDeclarationAreOnlyEmittedOnce()
        {
            var source = @"
using System;
class C1 : IDisposable
{
    public void Dispose() { }
}
class C2
{
    public static void Main()
    {
        using var c1 = new C1(), c2 = new C2();
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
              // (11,15): error CS0819: Implicitly-typed variables cannot have multiple declarators
              //         using (var c1 = new C1(), c2 = new C2())
              Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableMultipleDeclarator, "var c1 = new C1(), c2 = new C2()").WithLocation(11, 15)
            );
        }

        #region help method

        private UsingStatementSyntax GetUsingStatements(CSharpCompilation compilation, int index = 1)
        {
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);
            var usingStatements = tree.GetCompilationUnitRoot().DescendantNodes().OfType<UsingStatementSyntax>().ToList();
            return usingStatements[index - 1];
        }

        private IEnumerable VerifyDeclaredSymbolForUsingStatements(CSharpCompilation compilation, int index = 1, params string[] variables)
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
                yield return symbol;
            }
        }

        private SymbolInfo VerifySymbolInfoForUsingStatements(CSharpCompilation compilation, Symbol symbol, int index = 1)
        {
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var usingStatements = tree.GetCompilationUnitRoot().DescendantNodes().OfType<UsingStatementSyntax>().ToList();

            var type = model.GetSymbolInfo(usingStatements[index - 1].Declaration.Type);

            Assert.Equal(symbol, type.Symbol);

            return type;
        }

        private ISymbol VerifyLookUpSymbolForUsingStatements(CSharpCompilation compilation, Symbol symbol, int index = 1)
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
