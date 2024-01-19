// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

public class LockTests : CSharpTestBase
{
    private const string LockTypeDefinition = """
        namespace System.Threading
        {
            public class Lock
            {
                public Scope EnterLockScope()
                {
                    Console.Write("E");
                    return new Scope();
                }

                public ref struct Scope
                {
                    public void Dispose()
                    {
                        Console.Write("D");
                    }
                }
            }
        }
        """;

    [Fact]
    public void LockVsUsing()
    {
        var source = """
            using System;
            using System.Threading;

            static class C
            {
                static readonly Lock _lock = new();

                static void Main()
                {
                    M1();
                    M2();
                }

                static void M1()
                {
                    Console.Write("1");
                    lock (_lock)
                    {
                        Console.Write("2");
                    }
                    Console.Write("3");
                }

                static void M2()
                {
                    Console.Write("1");
                    using (_lock.EnterLockScope())
                    {
                        Console.Write("2");
                    }
                    Console.Write("3");
                }
            }
            """;
        var verifier = CompileAndVerify([source, LockTypeDefinition], expectedOutput: "1E2D31E2D3",
            verify: Verification.FailsILVerify);
        verifier.VerifyDiagnostics();
        var il = """
            {
              // Code size       52 (0x34)
              .maxstack  1
              .locals init (System.Threading.Lock.Scope V_0)
              IL_0000:  ldstr      "1"
              IL_0005:  call       "void System.Console.Write(string)"
              IL_000a:  ldsfld     "System.Threading.Lock C._lock"
              IL_000f:  callvirt   "System.Threading.Lock.Scope System.Threading.Lock.EnterLockScope()"
              IL_0014:  stloc.0
              .try
              {
                IL_0015:  ldstr      "2"
                IL_001a:  call       "void System.Console.Write(string)"
                IL_001f:  leave.s    IL_0029
              }
              finally
              {
                IL_0021:  ldloca.s   V_0
                IL_0023:  call       "void System.Threading.Lock.Scope.Dispose()"
                IL_0028:  endfinally
              }
              IL_0029:  ldstr      "3"
              IL_002e:  call       "void System.Console.Write(string)"
              IL_0033:  ret
            }
            """;
        verifier.VerifyIL("C.M2", il);
        verifier.VerifyIL("C.M1", il);
    }

    [Fact]
    public void MissingEnterLockScope()
    {
        var source = """
            System.Threading.Lock l = new();
            lock (l) { }

            namespace System.Threading
            {
                public class Lock { }
            }
            """;
        CreateCompilation(source).VerifyEmitDiagnostics(
            // (2,1): error CS0656: Missing compiler required member 'System.Threading.Lock.EnterLockScope'
            // lock (l) { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "lock (l) { }").WithArguments("System.Threading.Lock", "EnterLockScope").WithLocation(2, 1));
    }

    [Fact]
    public void EnterLockScopeReturnsVoid()
    {
        var source = """
            System.Threading.Lock l = new();
            lock (l) { }

            namespace System.Threading
            {
                public class Lock
                {
                    public void EnterLockScope() { }
                }
            }
            """;
        CreateCompilation(source).VerifyEmitDiagnostics(
            // (2,1): error CS0656: Missing compiler required member 'System.Threading.Lock.EnterLockScope'
            // lock (l) { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "lock (l) { }").WithArguments("System.Threading.Lock", "EnterLockScope").WithLocation(2, 1));
    }

    [Fact]
    public void EnterLockScopeTakesArguments()
    {
        var source = """
            System.Threading.Lock l = new();
            lock (l) { }

            namespace System.Threading
            {
                public class Lock
                {
                    public Scope EnterLockScope(int arg) => new Scope();

                    public ref struct Scope
                    {
                        public void Dispose() { }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyEmitDiagnostics(
            // (2,1): error CS0656: Missing compiler required member 'System.Threading.Lock.EnterLockScope'
            // lock (l) { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "lock (l) { }").WithArguments("System.Threading.Lock", "EnterLockScope").WithLocation(2, 1));
    }

    [Fact]
    public void MissingScopeDispose()
    {
        var source = """
            System.Threading.Lock l = new();
            lock (l) { }

            namespace System.Threading
            {
                public class Lock
                {
                    public Scope EnterLockScope() => new Scope();

                    public struct Scope { }
                }
            }
            """;
        CreateCompilation(source).VerifyEmitDiagnostics(
            // (2,1): error CS0656: Missing compiler required member 'System.Threading.Lock+Scope.Dispose'
            // lock (l) { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "lock (l) { }").WithArguments("System.Threading.Lock+Scope", "Dispose").WithLocation(2, 1));
    }

    [Fact]
    public void ScopeDisposeReturnsNonVoid()
    {
        var source = """
            System.Threading.Lock l = new();
            lock (l) { }

            namespace System.Threading
            {
                public class Lock
                {
                    public Scope EnterLockScope() => new Scope();

                    public ref struct Scope
                    {
                        public int Dispose() => 1;
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyEmitDiagnostics(
            // (2,1): error CS0656: Missing compiler required member 'System.Threading.Lock+Scope.Dispose'
            // lock (l) { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "lock (l) { }").WithArguments("System.Threading.Lock+Scope", "Dispose").WithLocation(2, 1));
    }

    [Fact]
    public void ScopeDisposeTakesArguments()
    {
        var source = """
            System.Threading.Lock l = new();
            lock (l) { }

            namespace System.Threading
            {
                public class Lock
                {
                    public Scope EnterLockScope() => new Scope();

                    public ref struct Scope
                    {
                        public void Dispose(int x) { }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyEmitDiagnostics(
            // (2,1): error CS0656: Missing compiler required member 'System.Threading.Lock+Scope.Dispose'
            // lock (l) { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "lock (l) { }").WithArguments("System.Threading.Lock+Scope", "Dispose").WithLocation(2, 1));
    }

    [Fact]
    public void ExternalAssembly()
    {
        var lib = CreateCompilation(LockTypeDefinition)
            .VerifyDiagnostics()
            .EmitToImageReference();
        var source = """
            using System;
            using System.Threading;
            
            Lock l = new Lock();
            lock (l) { Console.Write("L"); }
            """;
        var verifier = CompileAndVerify(source, [lib], expectedOutput: "ELD");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void InPlace()
    {
        var source = """
            using System;
            using System.Threading;

            lock (new Lock())
            {
                Console.Write("L");
            }
            """;
        var verifier = CompileAndVerify([source, LockTypeDefinition], verify: Verification.FailsILVerify,
           expectedOutput: "ELD");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void EmbeddedStatement()
    {
        var source = """
            using System;
            using System.Threading;
            
            lock (new Lock()) Console.Write("L");
            """;
        var verifier = CompileAndVerify([source, LockTypeDefinition], verify: Verification.FailsILVerify,
           expectedOutput: "ELD");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void EmptyStatement()
    {
        var source = """
            using System.Threading;
            
            lock (new Lock()) ;
            """;
        var verifier = CompileAndVerify([source, LockTypeDefinition], verify: Verification.FailsILVerify,
           expectedOutput: "ED");
        verifier.VerifyDiagnostics(
            // (3,19): warning CS0642: Possible mistaken empty statement
            // lock (new Lock()) ;
            Diagnostic(ErrorCode.WRN_PossibleMistakenNullStatement, ";").WithLocation(3, 19));
    }

    [Fact]
    public void Nullable()
    {
        var source = """
            #nullable enable
            using System;
            using System.Threading;

            static class C
            {
                static void Main()
                {
                    M(new Lock());
                }

                static void M(Lock? l)
                {
                    lock (l) { Console.Write("1"); }
                    lock (l) { Console.Write("2"); }
                }
            }
            """;
        var verifier = CompileAndVerify([source, LockTypeDefinition], verify: Verification.FailsILVerify,
           expectedOutput: "E1DE2D");
        verifier.VerifyDiagnostics(
            // (14,15): warning CS8602: Dereference of a possibly null reference.
            //         lock (l) { Console.Write("1"); }
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "l").WithLocation(14, 15));
    }

    [Theory, CombinatorialData]
    public void Null([CombinatorialValues("null", "default")] string expr)
    {
        var source = $$"""
            #nullable enable
            static class C
            {
                static void Main()
                {
                    try
                    {
                        M();
                    }
                    catch (System.NullReferenceException)
                    {
                        System.Console.Write("caught");
                    }
                }
                static void M()
                {
                    lock ((System.Threading.Lock){{expr}}) { }
                }
            }
            """;
        var verifier = CompileAndVerify([source, LockTypeDefinition], verify: Verification.FailsILVerify,
            expectedOutput: "caught");
        verifier.VerifyDiagnostics(
            // (17,15): warning CS8600: Converting null literal or possible null value to non-nullable type.
            //         lock ((System.Threading.Lock)null) { }
            Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, $"(System.Threading.Lock){expr}").WithLocation(17, 15),
            // (17,15): warning CS8602: Dereference of a possibly null reference.
            //         lock ((System.Threading.Lock)null) { }
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, $"(System.Threading.Lock){expr}").WithLocation(17, 15));
        verifier.VerifyIL("C.M", """
            {
              // Code size       18 (0x12)
              .maxstack  1
              .locals init (System.Threading.Lock.Scope V_0)
              IL_0000:  ldnull
              IL_0001:  callvirt   "System.Threading.Lock.Scope System.Threading.Lock.EnterLockScope()"
              IL_0006:  stloc.0
              .try
              {
                IL_0007:  leave.s    IL_0011
              }
              finally
              {
                IL_0009:  ldloca.s   V_0
                IL_000b:  call       "void System.Threading.Lock.Scope.Dispose()"
                IL_0010:  endfinally
              }
              IL_0011:  ret
            }
            """);
    }

    [Fact]
    public void Await()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;

            lock (new Lock())
            {
                await Task.Yield();
            }
            """;
        CreateCompilation([source, LockTypeDefinition]).VerifyDiagnostics(
            // (4,7): error CS9215: A lock statement scope type 'Lock.Scope' cannot be used in async methods or async lambda expressions.
            // lock (new Lock())
            Diagnostic(ErrorCode.ERR_BadSpecialByRefLock, "new Lock()").WithArguments("System.Threading.Lock.Scope").WithLocation(4, 7),
            // (6,5): error CS1996: Cannot await in the body of a lock statement
            //     await Task.Yield();
            Diagnostic(ErrorCode.ERR_BadAwaitInLock, "await Task.Yield()").WithLocation(6, 5));
    }

    [Fact]
    public void AsyncMethod()
    {
        var source = """
            #pragma warning disable 1998 // async method lacks 'await' operators
            using System.Threading;

            class C
            {
                async void M()
                {
                    lock (new Lock()) { }
                }
            }
            """;
        CreateCompilation([source, LockTypeDefinition]).VerifyDiagnostics(
            // (8,15): error CS9215: A lock statement scope type 'Lock.Scope' cannot be used in async methods or async lambda expressions.
            //         lock (new Lock()) { }
            Diagnostic(ErrorCode.ERR_BadSpecialByRefLock, "new Lock()").WithArguments("System.Threading.Lock.Scope").WithLocation(8, 15));
    }

    [Fact]
    public void AsyncMethod_WithAwait()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;

            class C
            {
                async void M()
                {
                    await Task.Yield();
                    lock (new Lock()) { }
                }
            }
            """;
        CreateCompilation([source, LockTypeDefinition]).VerifyDiagnostics(
            // (9,15): error CS9215: A lock statement scope type 'Lock.Scope' cannot be used in async methods or async lambda expressions.
            //         lock (new Lock()) { }
            Diagnostic(ErrorCode.ERR_BadSpecialByRefLock, "new Lock()").WithArguments("System.Threading.Lock.Scope").WithLocation(9, 15));
    }

    [Fact]
    public void AsyncLocalFunction()
    {
        var source = """
            #pragma warning disable 1998 // async method lacks 'await' operators
            using System.Threading;

            async void local()
            {
                lock (new Lock()) { }
            }

            local();
            """;
        CreateCompilation([source, LockTypeDefinition]).VerifyDiagnostics(
            // (6,11): error CS9215: A lock statement scope type 'Lock.Scope' cannot be used in async methods or async lambda expressions.
            //     lock (new Lock()) { }
            Diagnostic(ErrorCode.ERR_BadSpecialByRefLock, "new Lock()").WithArguments("System.Threading.Lock.Scope").WithLocation(6, 11));
    }

    [Fact]
    public void AsyncLocalFunction_WithAwait()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;

            async void local()
            {
                await Task.Yield();
                lock (new Lock()) { }
            }

            local();
            """;
        CreateCompilation([source, LockTypeDefinition]).VerifyDiagnostics(
            // (7,11): error CS9215: A lock statement scope type 'Lock.Scope' cannot be used in async methods or async lambda expressions.
            //     lock (new Lock()) { }
            Diagnostic(ErrorCode.ERR_BadSpecialByRefLock, "new Lock()").WithArguments("System.Threading.Lock.Scope").WithLocation(7, 11));
    }

    [Fact]
    public void AsyncLambda()
    {
        var source = """
            #pragma warning disable 1998 // async method lacks 'await' operators
            using System.Threading;

            var lam = async () =>
            {
                lock (new Lock()) { }
            };
            """;
        CreateCompilation([source, LockTypeDefinition]).VerifyDiagnostics(
            // (6,11): error CS9215: A lock statement scope type 'Lock.Scope' cannot be used in async methods or async lambda expressions.
            //     lock (new Lock()) { }
            Diagnostic(ErrorCode.ERR_BadSpecialByRefLock, "new Lock()").WithArguments("System.Threading.Lock.Scope").WithLocation(6, 11));
    }

    [Fact]
    public void AsyncLambda_WithAwait()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;

            var lam = async () =>
            {
                await Task.Yield();
                lock (new Lock()) { }
            };
            """;
        CreateCompilation([source, LockTypeDefinition]).VerifyDiagnostics(
            // (7,11): error CS9215: A lock statement scope type 'Lock.Scope' cannot be used in async methods or async lambda expressions.
            //     lock (new Lock()) { }
            Diagnostic(ErrorCode.ERR_BadSpecialByRefLock, "new Lock()").WithArguments("System.Threading.Lock.Scope").WithLocation(7, 11));
    }

    [Fact]
    public void Yield()
    {
        var source = """
            using System.Collections.Generic;
            using System.Threading;

            class C
            {
                IEnumerable<int> M()
                {
                    yield return 1;
                    lock (new Lock())
                    {
                        yield return 2;
                    }
                    yield return 3;
                }
            }
            """;
        CreateCompilation([source, LockTypeDefinition]).VerifyEmitDiagnostics(
            // (9,15): error CS4013: Instance of type 'Lock.Scope' cannot be used inside a nested function, query expression, iterator block or async method
            //         lock (new Lock())
            Diagnostic(ErrorCode.ERR_SpecialByRefInLambda, "new Lock()").WithArguments("System.Threading.Lock.Scope").WithLocation(9, 15));
    }

    [Theory, CombinatorialData]
    public void CastToObject([CombinatorialValues("object ", "dynamic")] string type)
    {
        var source = $$"""
            using System;
            using System.Threading;

            Lock l = new();

            {{type}} o = l;
            lock (o) { Console.Write("1"); }
            
            lock (({{type}})l) { Console.Write("2"); }

            lock (l as {{type}}) { Console.Write("3"); }
            
            o = l as {{type}};
            lock (o) { Console.Write("4"); }

            static {{type}} Cast1<T>(T t) => t;
            lock (Cast1(l)) { Console.Write("5"); }

            static {{type}} Cast2<T>(T t) where T : class => t;
            lock (Cast2(l)) { Console.Write("6"); }

            static {{type}} Cast3<T>(T t) where T : Lock => t;
            lock (Cast3(l)) { Console.Write("7"); }
            """;
        var verifier = CompileAndVerify([source, LockTypeDefinition], verify: Verification.FailsILVerify,
           expectedOutput: "1234567");
        verifier.VerifyDiagnostics(
            // 0.cs(6,13): warning CS9214: A value of type 'System.Threading.Lock' converted to another type will use likely unintended monitor-based locking in 'lock' statement.
            // object  o = l;
            Diagnostic(ErrorCode.WRN_ConvertingLock, "l").WithLocation(6, 13),
            // 0.cs(9,16): warning CS9214: A value of type 'System.Threading.Lock' converted to another type will use likely unintended monitor-based locking in 'lock' statement.
            // lock ((object )l) { Console.Write("2"); }
            Diagnostic(ErrorCode.WRN_ConvertingLock, "l").WithLocation(9, 16),
            // 0.cs(11,7): warning CS9214: A value of type 'System.Threading.Lock' converted to another type will use likely unintended monitor-based locking in 'lock' statement.
            // lock (l as object ) { Console.Write("3"); }
            Diagnostic(ErrorCode.WRN_ConvertingLock, "l").WithLocation(11, 7),
            // 0.cs(13,5): warning CS9214: A value of type 'System.Threading.Lock' converted to another type will use likely unintended monitor-based locking in 'lock' statement.
            // o = l as object ;
            Diagnostic(ErrorCode.WRN_ConvertingLock, "l").WithLocation(13, 5));
    }

    [Fact]
    public void CommonType()
    {
        var source = """
            using System;
            using System.Threading;

            var array1 = new[] { new Lock(), new Lock() };
            Console.WriteLine(array1.GetType());

            var array2 = new[] { new Lock(), new object() };
            Console.WriteLine(array2.GetType());
            """;
        var verifier = CompileAndVerify([source, LockTypeDefinition], verify: Verification.FailsILVerify, expectedOutput: """
            System.Threading.Lock[]
            System.Object[]
            """);
        verifier.VerifyDiagnostics(
            // 0.cs(7,22): warning CS9214: A value of type 'System.Threading.Lock' converted to another type will use likely unintended monitor-based locking in 'lock' statement.
            // var array2 = new[] { new Lock(), new object() };
            Diagnostic(ErrorCode.WRN_ConvertingLock, "new Lock()").WithLocation(7, 22));
    }

    [Theory, CombinatorialData]
    public void CastToBase([CombinatorialValues("interface", "class")] string baseKind)
    {
        var source = $$"""
            using System;
            using System.Threading;

            ILock l1 = new Lock();
            lock (l1) { Console.Write("1"); }

            ILock l2 = new Lock();
            lock ((Lock)l2) { Console.Write("2"); }

            namespace System.Threading
            {
                public {{baseKind}} ILock { }

                public class Lock : ILock
                {
                    public Scope EnterLockScope()
                    {
                        Console.Write("E");
                        return new Scope();
                    }

                    public ref struct Scope
                    {
                        public void Dispose()
                        {
                            Console.Write("D");
                        }
                    }
                }
            }
            """;
        var verifier = CompileAndVerify(source, verify: Verification.FailsILVerify,
           expectedOutput: "1E2D");
        verifier.VerifyDiagnostics(
            // (4,12): warning CS9214: A value of type 'System.Threading.Lock' converted to another type will use likely unintended monitor-based locking in 'lock' statement.
            // ILock l1 = new Lock();
            Diagnostic(ErrorCode.WRN_ConvertingLock, "new Lock()").WithLocation(4, 12),
            // (7,12): warning CS9214: A value of type 'System.Threading.Lock' converted to another type will use likely unintended monitor-based locking in 'lock' statement.
            // ILock l2 = new Lock();
            Diagnostic(ErrorCode.WRN_ConvertingLock, "new Lock()").WithLocation(7, 12));
    }

    [Fact]
    public void DerivedLock()
    {
        var source = """
            using System;
            using System.Threading;

            DerivedLock l1 = new DerivedLock();
            lock (l1) { Console.Write("1"); }

            Lock l2 = l1;
            lock (l2) { Console.Write("2"); }

            DerivedLock l3 = (DerivedLock)l2;
            lock (l3) { Console.Write("3"); }

            namespace System.Threading
            {
                public class Lock
                {
                    public Scope EnterLockScope()
                    {
                        Console.Write("E");
                        return new Scope();
                    }

                    public ref struct Scope
                    {
                        public void Dispose()
                        {
                            Console.Write("D");
                        }
                    }
                }

                public class DerivedLock : Lock { }
            }
            """;
        var verifier = CompileAndVerify(source, verify: Verification.FailsILVerify,
           expectedOutput: "1E2D3");
        // Note: no warnings here as we don't expect `Lock` to be unsealed,
        // so this doesn't warrant a warning in spec and implementation.
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void Downcast()
    {
        var source = """
            using System;
            using System.Threading;

            object o = new Lock();
            lock ((Lock)o) { Console.Write("L"); }
            """;
        var verifier = CompileAndVerify([source, LockTypeDefinition], verify: Verification.FailsILVerify,
           expectedOutput: "ELD");
        verifier.VerifyDiagnostics(
            // 0.cs(4,12): warning CS9214: A value of type 'System.Threading.Lock' converted to another type will use likely unintended monitor-based locking in 'lock' statement.
            // object o = new Lock();
            Diagnostic(ErrorCode.WRN_ConvertingLock, "new Lock()").WithLocation(4, 12));
    }

    [Fact]
    public void OtherConversions()
    {
        var source = """
            #nullable enable
            using System.Threading;

            I i = new Lock();
            Lock? l = new Lock();
            C c = new Lock();
            D d = new Lock();
            d = (D)new Lock();

            interface I { }

            class C
            {
                public static implicit operator C(Lock l) => new C();
            }

            class D
            {
                public static explicit operator D(Lock l) => new D();
            }
            """;
        // No warnings about converting `Lock` expected.
        CreateCompilation([source, LockTypeDefinition]).VerifyEmitDiagnostics(
            // 0.cs(4,7): error CS0266: Cannot implicitly convert type 'System.Threading.Lock' to 'I'. An explicit conversion exists (are you missing a cast?)
            // I i = new Lock();
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "new Lock()").WithArguments("System.Threading.Lock", "I").WithLocation(4, 7),
            // 0.cs(7,7): error CS0266: Cannot implicitly convert type 'System.Threading.Lock' to 'D'. An explicit conversion exists (are you missing a cast?)
            // D d = new Lock();
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "new Lock()").WithArguments("System.Threading.Lock", "D").WithLocation(7, 7));
    }

    [Theory]
    [InlineData("")]
    [InlineData("where T : class")]
    [InlineData("where T : Lock")]
    public void GenericParameter(string constraint)
    {
        var source = $$"""
            using System;
            using System.Threading;

            M(new Lock());

            static void M<T>(T t) {{constraint}}
            {
                lock (t) { Console.Write("L"); }
            }
            """;
        var verifier = CompileAndVerify([source, LockTypeDefinition], verify: Verification.FailsILVerify,
           expectedOutput: "L");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void GenericParameter_Object()
    {
        var source = """
            using System;
            using System.Threading;

            M<object>(new Lock());

            static void M<T>(T t)
            {
                lock (t) { Console.Write("L"); }
            }
            """;
        var verifier = CompileAndVerify([source, LockTypeDefinition], verify: Verification.FailsILVerify,
           expectedOutput: "L");
        verifier.VerifyDiagnostics(
            // 0.cs(4,11): warning CS9214: A value of type 'System.Threading.Lock' converted to another type will use likely unintended monitor-based locking in 'lock' statement.
            // M<object>(new Lock());
            Diagnostic(ErrorCode.WRN_ConvertingLock, "new Lock()").WithLocation(4, 11));
    }

    [Fact]
    public void UseSiteError_EnterLockScope()
    {
        // namespace System.Threading
        // {
        //     public class Lock
        //     {
        //         [System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute("Test")]
        //         public Scope EnterLockScope() => throw null;
        //
        //         public ref struct Scope
        //         {
        //             public void Dispose() { }
        //         }
        //     }
        // }
        var ilSource = """
            .class public auto ansi sealed beforefieldinit System.Threading.Lock extends System.Object
            {
                .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
                {
                    .maxstack 8
                    ret
                }

                .method public hidebysig instance class System.Threading.Lock/Scope EnterLockScope () cil managed
                {
                    .custom instance void System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::.ctor(string) = (
                        01 00 04 54 65 73 74 00 00
                    )
                    .maxstack 8
                    ldnull
                    throw
                }

                .class nested public sequential ansi sealed beforefieldinit Scope extends System.ValueType
                {
                    .custom instance void System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (
                        01 00 00 00
                    )

                    .pack 0
                    .size 1

                    .method public hidebysig instance void Dispose () cil managed
                    {
                        .maxstack 8
                        ret
                    }
                }
            }

            .class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute extends System.Object
            {
                .method public hidebysig specialname rtspecialname instance void .ctor(string s) cil managed
                {
                    .maxstack 8
                    ret
                }
            }

            .class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsByRefLikeAttribute extends System.Object
            {
                .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
                {
                    .maxstack 8
                    ret
                }
            }
            """;
        var source = """
            class C
            {
                void M(System.Threading.Lock l)
                {
                    lock (l) { }
                }
            }
            """;
        CreateCompilationWithIL(source, ilSource).VerifyEmitDiagnostics(
            // (5,9): error CS9041: 'Lock.EnterLockScope()' requires compiler feature 'Test', which is not supported by this version of the C# compiler.
            //         lock (l) { }
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "lock (l) { }").WithArguments("System.Threading.Lock.EnterLockScope()", "Test").WithLocation(5, 9));
    }

    [Fact]
    public void UseSiteError_Scope()
    {
        // namespace System.Threading
        // {
        //     public class Lock
        //     {
        //         public Scope EnterLockScope() => throw null;
        //
        //         [System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute("Test")]
        //         public ref struct Scope
        //         {
        //             public void Dispose() { }
        //         }
        //     }
        // }
        var ilSource = """
            .class public auto ansi sealed beforefieldinit System.Threading.Lock extends System.Object
            {
                .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
                {
                    .maxstack 8
                    ret
                }

                .method public hidebysig instance class System.Threading.Lock/Scope EnterLockScope () cil managed
                {
                    .maxstack 8
                    ldnull
                    throw
                }

                .class nested public sequential ansi sealed beforefieldinit Scope extends System.ValueType
                {
                    .custom instance void System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (
                        01 00 00 00
                    )

                    .custom instance void System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::.ctor(string) = (
                        01 00 04 54 65 73 74 00 00
                    )

                    .pack 0
                    .size 1

                    .method public hidebysig instance void Dispose () cil managed
                    {
                        .maxstack 8
                        ret
                    }
                }
            }

            .class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute extends System.Object
            {
                .method public hidebysig specialname rtspecialname instance void .ctor(string s) cil managed
                {
                    .maxstack 8
                    ret
                }
            }

            .class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsByRefLikeAttribute extends System.Object
            {
                .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
                {
                    .maxstack 8
                    ret
                }
            }
            """;
        var source = """
            class C
            {
                void M(System.Threading.Lock l)
                {
                    lock (l) { }
                }
            }
            """;
        CreateCompilationWithIL(source, ilSource).VerifyEmitDiagnostics(
            // (5,9): error CS9041: 'Lock.Scope' requires compiler feature 'Test', which is not supported by this version of the C# compiler.
            //         lock (l) { }
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "lock (l) { }").WithArguments("System.Threading.Lock.Scope", "Test").WithLocation(5, 9),
            // (5,9): error CS9041: 'Lock.Scope' requires compiler feature 'Test', which is not supported by this version of the C# compiler.
            //         lock (l) { }
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "lock (l) { }").WithArguments("System.Threading.Lock.Scope", "Test").WithLocation(5, 9));
    }

    [Fact]
    public void UseSiteError_Dispose()
    {
        // namespace System.Threading
        // {
        //     public class Lock
        //     {
        //         public Scope EnterLockScope() => throw null;
        //
        //         public ref struct Scope
        //         {
        //             [System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute("Test")]
        //             public void Dispose() { }
        //         }
        //     }
        // }
        var ilSource = """
            .class public auto ansi sealed beforefieldinit System.Threading.Lock extends System.Object
            {
                .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
                {
                    .maxstack 8
                    ret
                }

                .method public hidebysig instance class System.Threading.Lock/Scope EnterLockScope () cil managed
                {
                    .maxstack 8
                    ldnull
                    throw
                }

                .class nested public sequential ansi sealed beforefieldinit Scope extends System.ValueType
                {
                    .custom instance void System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (
                        01 00 00 00
                    )

                    .pack 0
                    .size 1

                    .method public hidebysig instance void Dispose () cil managed
                    {
                        .custom instance void System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::.ctor(string) = (
                            01 00 04 54 65 73 74 00 00
                        )
                        .maxstack 8
                        ret
                    }
                }
            }

            .class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute extends System.Object
            {
                .method public hidebysig specialname rtspecialname instance void .ctor(string s) cil managed
                {
                    .maxstack 8
                    ret
                }
            }

            .class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsByRefLikeAttribute extends System.Object
            {
                .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
                {
                    .maxstack 8
                    ret
                }
            }
            """;
        var source = """
            class C
            {
                void M(System.Threading.Lock l)
                {
                    lock (l) { }
                }
            }
            """;
        CreateCompilationWithIL(source, ilSource).VerifyEmitDiagnostics(
            // (5,9): error CS9041: 'Lock.Scope.Dispose()' requires compiler feature 'Test', which is not supported by this version of the C# compiler.
            //         lock (l) { }
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "lock (l) { }").WithArguments("System.Threading.Lock.Scope.Dispose()", "Test").WithLocation(5, 9));
    }
}
