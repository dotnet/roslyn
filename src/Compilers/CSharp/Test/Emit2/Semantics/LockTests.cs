﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

public class LockTests : CSharpTestBase
{
    private const string LockTypeDefinition = """
        namespace System.Threading
        {
            public class Lock
            {
                public Scope EnterScope()
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
                    using (_lock.EnterScope())
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
              IL_000f:  callvirt   "System.Threading.Lock.Scope System.Threading.Lock.EnterScope()"
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
    public void SemanticModel()
    {
        var source = """
            using System.Threading;

            Lock l = null;
            lock (l)
            {
            }
            """;
        var compilation = CreateCompilation([source, LockTypeDefinition]);
        compilation.VerifyDiagnostics();

        var tree = compilation.SyntaxTrees[0];
        var model = compilation.GetSemanticModel(tree);

        var localDecl = tree.GetRoot().DescendantNodes().OfType<LocalDeclarationStatementSyntax>().Single();
        var localSymbol = (ILocalSymbol)model.GetDeclaredSymbol(localDecl.Declaration.Variables.Single())!;
        Assert.Equal("l", localSymbol.Name);
        Assert.Equal("System.Threading.Lock", localSymbol.Type.ToTestDisplayString());

        var lockStatement = tree.GetRoot().DescendantNodes().OfType<LockStatementSyntax>().Single();
        var lockExprInfo = model.GetSymbolInfo(lockStatement.Expression);
        Assert.Equal(localSymbol, lockExprInfo.Symbol);
    }

    [Fact]
    public void MissingEnterScope()
    {
        var source = """
            System.Threading.Lock l = new();
            lock (l) { }

            namespace System.Threading
            {
                public class Lock { }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (2,7): error CS0656: Missing compiler required member 'System.Threading.Lock.EnterScope'
            // lock (l) { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "l").WithArguments("System.Threading.Lock", "EnterScope").WithLocation(2, 7));
    }

    [Fact]
    public void EnterScopeReturnsVoid()
    {
        var source = """
            System.Threading.Lock l = new();
            lock (l) { }

            namespace System.Threading
            {
                public class Lock
                {
                    public void EnterScope() { }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (2,7): error CS0656: Missing compiler required member 'System.Threading.Lock.EnterScope'
            // lock (l) { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "l").WithArguments("System.Threading.Lock", "EnterScope").WithLocation(2, 7));
    }

    [Fact]
    public void EnterScopeStatic()
    {
        var source = """
            System.Threading.Lock l = new();
            lock (l) { }

            namespace System.Threading
            {
                public class Lock
                {
                    public static Scope EnterScope() => new Scope();

                    public ref struct Scope
                    {
                        public void Dispose() { }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (2,7): error CS0656: Missing compiler required member 'System.Threading.Lock.EnterScope'
            // lock (l) { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "l").WithArguments("System.Threading.Lock", "EnterScope").WithLocation(2, 7));
    }

    [Fact]
    public void EnterScopeTakesParameters()
    {
        var source = """
            System.Threading.Lock l = new();
            lock (l) { }

            namespace System.Threading
            {
                public class Lock
                {
                    public Scope EnterScope(int arg) => new Scope();

                    public ref struct Scope
                    {
                        public void Dispose() { }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (2,7): error CS0656: Missing compiler required member 'System.Threading.Lock.EnterScope'
            // lock (l) { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "l").WithArguments("System.Threading.Lock", "EnterScope").WithLocation(2, 7));
    }

    [Fact]
    public void EnterScopeTakesParameters_Optional()
    {
        var source = """
            System.Threading.Lock l = new();
            lock (l) { }

            namespace System.Threading
            {
                public class Lock
                {
                    public Scope EnterScope(int arg = 1) => new Scope();

                    public ref struct Scope
                    {
                        public void Dispose() { }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (2,7): error CS0656: Missing compiler required member 'System.Threading.Lock.EnterScope'
            // lock (l) { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "l").WithArguments("System.Threading.Lock", "EnterScope").WithLocation(2, 7));
    }

    [Fact]
    public void EnterScopeTakesParameters_ParamsArray()
    {
        var source = """
            System.Threading.Lock l = new();
            lock (l) { }

            namespace System.Threading
            {
                public class Lock
                {
                    public Scope EnterScope(params int[] args) => new Scope();

                    public ref struct Scope
                    {
                        public void Dispose() { }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (2,7): error CS0656: Missing compiler required member 'System.Threading.Lock.EnterScope'
            // lock (l) { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "l").WithArguments("System.Threading.Lock", "EnterScope").WithLocation(2, 7));
    }

    [Fact]
    public void EnterScopeMultipleOverloads_01()
    {
        var source = """
            System.Threading.Lock l = new();
            lock (l) { }

            namespace System.Threading
            {
                public class Lock
                {
                    public Scope EnterScope() => new Scope();
                    public Scope EnterScope() => new Scope();

                    public ref struct Scope
                    {
                        public void Dispose() { }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (2,7): error CS0656: Missing compiler required member 'System.Threading.Lock.EnterScope'
            // lock (l) { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "l").WithArguments("System.Threading.Lock", "EnterScope").WithLocation(2, 7),
            // (9,22): error CS0111: Type 'Lock' already defines a member called 'EnterScope' with the same parameter types
            //         public Scope EnterScope() => new Scope();
            Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "EnterScope").WithArguments("EnterScope", "System.Threading.Lock").WithLocation(9, 22));
    }

    [Fact]
    public void EnterScopeMultipleOverloads_02()
    {
        var source = """
            System.Threading.Lock l = new();
            lock (l) { System.Console.Write("L"); }

            namespace System.Threading
            {
                public class Lock
                {
                    public Scope EnterScope()
                    {
                        Console.Write("E");
                        return new Scope();
                    }

                    public Scope EnterScope(int x)
                    {
                        Console.Write("X");
                        return new Scope();
                    }

                    public ref struct Scope
                    {
                        public void Dispose() => Console.Write("D");
                    }
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "ELD", verify: Verification.FailsILVerify).VerifyDiagnostics();
    }

    [Fact]
    public void EnterScopeMultipleOverloads_03()
    {
        var source = """
            System.Threading.Lock l = new();
            lock (l) { System.Console.Write("L"); }

            namespace System.Threading
            {
                public class Lock
                {
                    public Scope EnterScope()
                    {
                        Console.Write("E");
                        return new Scope();
                    }

                    public Scope EnterScope<T>()
                    {
                        Console.Write("T");
                        return new Scope();
                    }

                    public ref struct Scope
                    {
                        public void Dispose() => Console.Write("D");
                    }
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "ELD", verify: Verification.FailsILVerify).VerifyDiagnostics();
    }

    [Fact]
    public void EnterScopeHidden()
    {
        var source = """
            System.Threading.Lock l = new();
            lock (l) { System.Console.Write("L"); }

            namespace System.Threading
            {
                public class LockBase
                {
                    public Lock.Scope EnterScope()
                    {
                        Console.Write("B");
                        return new();
                    }
                }

                public class Lock : LockBase
                {
                    public new Scope EnterScope()
                    {
                        Console.Write("E");
                        return new();
                    }

                    public ref struct Scope
                    {
                        public void Dispose() => Console.Write("D");
                    }
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "ELD", verify: Verification.FailsILVerify).VerifyDiagnostics();
    }

    [Fact]
    public void EnterScopeOverride()
    {
        var source = """
            System.Threading.Lock l = new();
            lock (l) { System.Console.Write("L"); }

            namespace System.Threading
            {
                public class LockBase
                {
                    public virtual Lock.Scope EnterScope()
                    {
                        Console.Write("B");
                        return new();
                    }
                }

                public class Lock : LockBase
                {
                    public override Scope EnterScope()
                    {
                        Console.Write("E");
                        return new();
                    }

                    public ref struct Scope
                    {
                        public void Dispose() => Console.Write("D");
                    }
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "ELD", verify: Verification.FailsILVerify).VerifyDiagnostics();
    }

    [Fact]
    public void EnterScopeVirtual()
    {
        var source = """
            System.Threading.Lock l = new System.Threading.LockDerived();
            lock (l) { System.Console.Write("L"); }

            namespace System.Threading
            {
                public class Lock
                {
                    public virtual Scope EnterScope()
                    {
                        Console.Write("E");
                        return new();
                    }

                    public ref struct Scope
                    {
                        public void Dispose() => Console.Write("D");
                    }
                }

                public class LockDerived : Lock
                {
                    public override Scope EnterScope()
                    {
                        Console.Write("O");
                        return new();
                    }
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "OLD", verify: Verification.FailsILVerify).VerifyDiagnostics();
    }

    [Fact]
    public void EnterScopeExplicitImplementation()
    {
        var source = """
            System.Threading.Lock l = new();
            lock (l) { }

            namespace System.Threading
            {
                public interface ILock
                {
                    Lock.Scope EnterScope();
                }

                public class Lock : ILock
                {
                    Scope ILock.EnterScope() => new Scope();

                    public ref struct Scope
                    {
                        public void Dispose() { }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (2,7): error CS0656: Missing compiler required member 'System.Threading.Lock.EnterScope'
            // lock (l) { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "l").WithArguments("System.Threading.Lock", "EnterScope").WithLocation(2, 7));
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
                    public Scope EnterScope() => new Scope();

                    public ref struct Scope { }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (2,7): error CS0656: Missing compiler required member 'System.Threading.Lock+Scope.Dispose'
            // lock (l) { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "l").WithArguments("System.Threading.Lock+Scope", "Dispose").WithLocation(2, 7));
    }

    [Fact]
    public void ScopeDisposeStatic()
    {
        var source = """
            System.Threading.Lock l = new();
            lock (l) { }

            namespace System.Threading
            {
                public class Lock
                {
                    public Scope EnterScope() => new Scope();

                    public ref struct Scope
                    {
                        public static void Dispose() { }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (2,7): error CS0656: Missing compiler required member 'System.Threading.Lock+Scope.Dispose'
            // lock (l) { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "l").WithArguments("System.Threading.Lock+Scope", "Dispose").WithLocation(2, 7));
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
                    public Scope EnterScope() => new Scope();

                    public ref struct Scope
                    {
                        public int Dispose() => 1;
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (2,7): error CS0656: Missing compiler required member 'System.Threading.Lock+Scope.Dispose'
            // lock (l) { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "l").WithArguments("System.Threading.Lock+Scope", "Dispose").WithLocation(2, 7));
    }

    [Fact]
    public void ScopeDisposeTakesParameters()
    {
        var source = """
            System.Threading.Lock l = new();
            lock (l) { }

            namespace System.Threading
            {
                public class Lock
                {
                    public Scope EnterScope() => new Scope();

                    public ref struct Scope
                    {
                        public void Dispose(int x) { }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (2,7): error CS0656: Missing compiler required member 'System.Threading.Lock+Scope.Dispose'
            // lock (l) { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "l").WithArguments("System.Threading.Lock+Scope", "Dispose").WithLocation(2, 7));
    }

    [Fact]
    public void ScopeDisposeTakesParameters_Optional()
    {
        var source = """
            System.Threading.Lock l = new();
            lock (l) { }

            namespace System.Threading
            {
                public class Lock
                {
                    public Scope EnterScope() => new Scope();

                    public ref struct Scope
                    {
                        public void Dispose(int x = 1) { }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (2,7): error CS0656: Missing compiler required member 'System.Threading.Lock+Scope.Dispose'
            // lock (l) { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "l").WithArguments("System.Threading.Lock+Scope", "Dispose").WithLocation(2, 7));
    }

    [Fact]
    public void ScopeDisposeTakesParameters_ParamsArray()
    {
        var source = """
            System.Threading.Lock l = new();
            lock (l) { }

            namespace System.Threading
            {
                public class Lock
                {
                    public Scope EnterScope() => new Scope();

                    public ref struct Scope
                    {
                        public void Dispose(params int[] xs) { }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (2,7): error CS0656: Missing compiler required member 'System.Threading.Lock+Scope.Dispose'
            // lock (l) { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "l").WithArguments("System.Threading.Lock+Scope", "Dispose").WithLocation(2, 7));
    }

    [Fact]
    public void ScopeDisposeMultipleOverloads_01()
    {
        var source = """
            System.Threading.Lock l = new();
            lock (l) { }

            namespace System.Threading
            {
                public class Lock
                {
                    public Scope EnterScope() => new Scope();

                    public ref struct Scope
                    {
                        public void Dispose() { }
                        public void Dispose() { }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (2,7): error CS0656: Missing compiler required member 'System.Threading.Lock+Scope.Dispose'
            // lock (l) { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "l").WithArguments("System.Threading.Lock+Scope", "Dispose").WithLocation(2, 7),
            // (13,25): error CS0111: Type 'Lock.Scope' already defines a member called 'Dispose' with the same parameter types
            //             public void Dispose() { }
            Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "Dispose").WithArguments("Dispose", "System.Threading.Lock.Scope").WithLocation(13, 25));
    }

    [Fact]
    public void ScopeDisposeMultipleOverloads_02()
    {
        var source = """
            System.Threading.Lock l = new();
            lock (l) { System.Console.Write("L"); }

            namespace System.Threading
            {
                public class Lock
                {
                    public Scope EnterScope()
                    {
                        Console.Write("E");
                        return new Scope();
                    }

                    public ref struct Scope
                    {
                        public void Dispose() => Console.Write("D");
                        public void Dispose(int x) => Console.Write("X");
                    }
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "ELD", verify: Verification.FailsILVerify).VerifyDiagnostics();
    }

    [Fact]
    public void ScopeDisposeMultipleOverloads_03()
    {
        var source = """
            System.Threading.Lock l = new();
            lock (l) { System.Console.Write("L"); }

            namespace System.Threading
            {
                public class Lock
                {
                    public Scope EnterScope()
                    {
                        Console.Write("E");
                        return new Scope();
                    }

                    public ref struct Scope
                    {
                        public void Dispose() => Console.Write("D");
                        public void Dispose<T>() => Console.Write("T");
                    }
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "ELD", verify: Verification.FailsILVerify).VerifyDiagnostics();
    }

    [Fact]
    public void InternalLock()
    {
        var source = """
            static class Program
            {
                static void Main()
                {
                    System.Threading.Lock l = new();
                    lock (l) { System.Console.Write("L"); }
                }
            }

            namespace System.Threading
            {
                internal class Lock
                {
                    public Scope EnterScope()
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
        var verifier = CompileAndVerify(source, expectedOutput: "ELD", verify: Verification.FailsILVerify);
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void InternalScope()
    {
        var source = """
            System.Threading.Lock l = new();
            lock (l) { }

            namespace System.Threading
            {
                public class Lock
                {
                    public Scope EnterScope() => new Scope();

                    internal ref struct Scope
                    {
                        public void Dispose() { }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (2,7): error CS0656: Missing compiler required member 'System.Threading.Lock.EnterScope'
            // lock (l) { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "l").WithArguments("System.Threading.Lock", "EnterScope").WithLocation(2, 7),
            // (8,22): error CS0050: Inconsistent accessibility: return type 'Lock.Scope' is less accessible than method 'Lock.EnterScope()'
            //         public Scope EnterScope() => new Scope();
            Diagnostic(ErrorCode.ERR_BadVisReturnType, "EnterScope").WithArguments("System.Threading.Lock.EnterScope()", "System.Threading.Lock.Scope").WithLocation(8, 22));
    }

    [Fact]
    public void Obsolete_EnterScope()
    {
        var source = """
            using System;
            using System.Threading;

            Lock l = new();
            lock (l) { Console.Write("1"); }
            using (l.EnterScope()) { Console.Write("2"); }

            namespace System.Threading
            {
                public class Lock
                {
                    [System.Obsolete]
                    public Scope EnterScope()
                    {
                        Console.Write("E");
                        return new Scope();
                    }

                    public ref struct Scope
                    {
                        public void Dispose() => Console.Write("D");
                    }
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "E1DE2D", verify: Verification.FailsILVerify).VerifyDiagnostics(
            // (6,8): warning CS0612: 'Lock.EnterScope()' is obsolete
            // using (l.EnterScope()) { Console.Write("2"); }
            Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "l.EnterScope()").WithArguments("System.Threading.Lock.EnterScope()").WithLocation(6, 8));
    }

    [Fact]
    public void Obsolete_Lock()
    {
        var source = """
            using System;
            using System.Threading;

            Lock l = new();
            lock (l) { Console.Write("1"); }
            using (l.EnterScope()) { Console.Write("2"); }

            namespace System.Threading
            {
                [System.Obsolete]
                public class Lock
                {
                    public Scope EnterScope()
                    {
                        Console.Write("E");
                        return new Scope();
                    }

                    public ref struct Scope
                    {
                        public void Dispose() => Console.Write("D");
                    }
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "E1DE2D", verify: Verification.FailsILVerify).VerifyDiagnostics(
            // (4,1): warning CS0612: 'Lock' is obsolete
            // Lock l = new();
            Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "Lock").WithArguments("System.Threading.Lock").WithLocation(4, 1));
    }

    [Fact]
    public void Obsolete_Scope()
    {
        var source = """
            using System;
            using System.Threading;

            Lock l = new();
            lock (l) { Console.Write("1"); }
            using (l.EnterScope()) { Console.Write("2"); }

            namespace System.Threading
            {
                public class Lock
                {
                    public Scope EnterScope()
                    {
                        Console.Write("E");
                        return new Scope();
                    }
            
                    [System.Obsolete]
                    public ref struct Scope
                    {
                        public void Dispose() => Console.Write("D");
                    }
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "E1DE2D", verify: Verification.FailsILVerify).VerifyDiagnostics(
            // (12,16): warning CS0612: 'Lock.Scope' is obsolete
            //         public Scope EnterScope()
            Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "Scope").WithArguments("System.Threading.Lock.Scope").WithLocation(12, 16),
            // (15,24): warning CS0612: 'Lock.Scope' is obsolete
            //             return new Scope();
            Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "Scope").WithArguments("System.Threading.Lock.Scope").WithLocation(15, 24));
    }

    [Fact]
    public void Obsolete_Dispose()
    {
        var source = """
            using System;
            using System.Threading;

            Lock l = new();
            lock (l) { Console.Write("1"); }
            using (l.EnterScope()) { Console.Write("2"); }

            namespace System.Threading
            {
                public class Lock
                {
                    public Scope EnterScope()
                    {
                        Console.Write("E");
                        return new Scope();
                    }
            
                    public ref struct Scope
                    {
                        [System.Obsolete]
                        public void Dispose() => Console.Write("D");
                    }
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "E1DE2D", verify: Verification.FailsILVerify).VerifyDiagnostics(
            // (6,8): warning CS0612: 'Lock.Scope.Dispose()' is obsolete
            // using (l.EnterScope()) { Console.Write("2"); }
            Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "l.EnterScope()").WithArguments("System.Threading.Lock.Scope.Dispose()").WithLocation(6, 8));
    }

    [Fact]
    public void GenericLock()
    {
        var source = """
            static class Program
            {
                static void Main()
                {
                    System.Threading.Lock<int> l = new();
                    lock (l) { System.Console.Write("L"); }
                }
            }

            namespace System.Threading
            {
                public class Lock<T>
                {
                    public Scope EnterScope()
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
        var verifier = CompileAndVerify(source, expectedOutput: "L", verify: Verification.FailsILVerify);
        verifier.VerifyDiagnostics();
        // Should use Monitor locking.
        verifier.VerifyIL("Program.Main", """
            {
              // Code size       39 (0x27)
              .maxstack  2
              .locals init (System.Threading.Lock<int> V_0,
                            bool V_1)
              IL_0000:  newobj     "System.Threading.Lock<int>..ctor()"
              IL_0005:  stloc.0
              IL_0006:  ldc.i4.0
              IL_0007:  stloc.1
              .try
              {
                IL_0008:  ldloc.0
                IL_0009:  ldloca.s   V_1
                IL_000b:  call       "void System.Threading.Monitor.Enter(object, ref bool)"
                IL_0010:  ldstr      "L"
                IL_0015:  call       "void System.Console.Write(string)"
                IL_001a:  leave.s    IL_0026
              }
              finally
              {
                IL_001c:  ldloc.1
                IL_001d:  brfalse.s  IL_0025
                IL_001f:  ldloc.0
                IL_0020:  call       "void System.Threading.Monitor.Exit(object)"
                IL_0025:  endfinally
              }
              IL_0026:  ret
            }
            """);
    }

    [Fact]
    public void GenericEnterScope()
    {
        var source = """
            System.Threading.Lock l = new();
            lock (l) { }

            namespace System.Threading
            {
                public class Lock
                {
                    public Scope EnterScope<T>() => new Scope();

                    public ref struct Scope
                    {
                        public void Dispose() { }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (2,7): error CS0656: Missing compiler required member 'System.Threading.Lock.EnterScope'
            // lock (l) { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "l").WithArguments("System.Threading.Lock", "EnterScope").WithLocation(2, 7));
    }

    [Fact]
    public void GenericScope()
    {
        var source = """
            System.Threading.Lock l = new();
            lock (l) { }

            namespace System.Threading
            {
                public class Lock
                {
                    public Scope<int> EnterScope() => new Scope<int>();

                    public ref struct Scope<T>
                    {
                        public void Dispose() { }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (2,7): error CS0656: Missing compiler required member 'System.Threading.Lock.EnterScope'
            // lock (l) { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "l").WithArguments("System.Threading.Lock", "EnterScope").WithLocation(2, 7));
    }

    [Fact]
    public void LockStruct()
    {
        var source = """
            System.Threading.Lock l = new();
            lock (l) { }

            namespace System.Threading
            {
                public struct Lock
                {
                    public Scope EnterScope() => new Scope();

                    public ref struct Scope
                    {
                        public void Dispose() { }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (2,7): error CS0185: 'Lock' is not a reference type as required by the lock statement
            // lock (l) { }
            Diagnostic(ErrorCode.ERR_LockNeedsReference, "l").WithArguments("System.Threading.Lock").WithLocation(2, 7));
    }

    [Theory, CombinatorialData]
    public void ScopeRegularStruct(bool implementsIDisposable)
    {
        var source = $$"""
            static class Program
            {
                static void Main()
                {
                    System.Threading.Lock l = new();
                    lock (l) { System.Console.Write("L"); }
                }
            }

            namespace System.Threading
            {
                public class Lock
                {
                    public Scope EnterScope()
                    {
                        Console.Write("E");
                        return new Scope();
                    }

                    public struct Scope {{(implementsIDisposable ? ": IDisposable" : "")}}
                    {
                        public void Dispose()
                        {
                            Console.Write("D");
                        }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (6,15): error CS0656: Missing compiler required member 'System.Threading.Lock.EnterScope'
            //         lock (l) { System.Console.Write("L"); }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "l").WithArguments("System.Threading.Lock", "EnterScope").WithLocation(6, 15));
    }

    [Fact]
    public void ScopeMisnamed()
    {
        var source = """
            System.Threading.Lock l = new();
            lock (l) { }

            namespace System.Threading
            {
                public class Lock
                {
                    public MyScope EnterScope() => new MyScope();

                    public ref struct MyScope
                    {
                        public void Dispose() { }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (2,7): error CS0656: Missing compiler required member 'System.Threading.Lock.EnterScope'
            // lock (l) { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "l").WithArguments("System.Threading.Lock", "EnterScope").WithLocation(2, 7));
    }

    [Fact]
    public void ScopeNotNested()
    {
        var source = """
            System.Threading.Lock l = new();
            lock (l) { }

            namespace System.Threading
            {
                public class Lock
                {
                    public Scope EnterScope() => new Scope();
                }

                public ref struct Scope
                {
                    public void Dispose() { }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (2,7): error CS0656: Missing compiler required member 'System.Threading.Lock.EnterScope'
            // lock (l) { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "l").WithArguments("System.Threading.Lock", "EnterScope").WithLocation(2, 7));
    }

    [Fact]
    public void ScopeClass()
    {
        var source = """
            System.Threading.Lock l = new();
            lock (l) { }

            namespace System.Threading
            {
                public class Lock
                {
                    public Scope EnterScope() => new Scope();

                    public class Scope
                    {
                        public void Dispose() { }
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (2,7): error CS0656: Missing compiler required member 'System.Threading.Lock.EnterScope'
            // lock (l) { }
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "l").WithArguments("System.Threading.Lock", "EnterScope").WithLocation(2, 7));
    }

    [Fact]
    public void LockInterface()
    {
        var source = """
            static class Program
            {
                static void Main()
                {
                    System.Threading.Lock l = new System.Threading.MyLock();
                    lock (l) { System.Console.Write("L"); }
                }
            }

            namespace System.Threading
            {
                public interface Lock
                {
                    Scope EnterScope();

                    public ref struct Scope
                    {
                        public void Dispose()
                        {
                            Console.Write("D");
                        }
                    }
                }
            
                public class MyLock : Lock
                {
                    public Lock.Scope EnterScope()
                    {
                        Console.Write("E");
                        return new();
                    }
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "ELD", verify: Verification.FailsILVerify).VerifyDiagnostics();
    }

    [Fact]
    public void LockInterface_DefaultImplementation()
    {
        var source = """
            static class Program
            {
                static void Main()
                {
                    System.Threading.Lock l = new System.Threading.MyLock();
                    lock (l) { System.Console.Write("L"); }
                }
            }

            namespace System.Threading
            {
                public interface Lock
                {
                    Scope EnterScope()
                    {
                        Console.Write("I");
                        return new();
                    }

                    public ref struct Scope
                    {
                        public void Dispose()
                        {
                            Console.Write("D");
                        }
                    }
                }
            
                public class MyLock : Lock
                {
                    public Lock.Scope EnterScope()
                    {
                        Console.Write("E");
                        return new();
                    }
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? "ELD" : null,
            verify: Verification.Fails, targetFramework: TargetFramework.Net60).VerifyDiagnostics();
    }

    [Fact]
    public void LockNested()
    {
        var source = """
            static class Program
            {
                static void Main()
                {
                    System.Threading.Container.Lock l = new();
                    lock (l) { System.Console.Write("L"); }
                }
            }

            namespace System.Threading
            {
                public class Container
                {
                    public class Lock
                    {
                        public Scope EnterScope()
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
            }
            """;
        var verifier = CompileAndVerify(source, expectedOutput: "L", verify: Verification.FailsILVerify);
        verifier.VerifyDiagnostics();
        // Should use Monitor locking.
        verifier.VerifyIL("Program.Main", """
            {
              // Code size       39 (0x27)
              .maxstack  2
              .locals init (System.Threading.Container.Lock V_0,
                            bool V_1)
              IL_0000:  newobj     "System.Threading.Container.Lock..ctor()"
              IL_0005:  stloc.0
              IL_0006:  ldc.i4.0
              IL_0007:  stloc.1
              .try
              {
                IL_0008:  ldloc.0
                IL_0009:  ldloca.s   V_1
                IL_000b:  call       "void System.Threading.Monitor.Enter(object, ref bool)"
                IL_0010:  ldstr      "L"
                IL_0015:  call       "void System.Console.Write(string)"
                IL_001a:  leave.s    IL_0026
              }
              finally
              {
                IL_001c:  ldloc.1
                IL_001d:  brfalse.s  IL_0025
                IL_001f:  ldloc.0
                IL_0020:  call       "void System.Threading.Monitor.Exit(object)"
                IL_0025:  endfinally
              }
              IL_0026:  ret
            }
            """);
    }

    [Fact]
    public void LockInWrongNamespace()
    {
        var source = """
            static class Program
            {
                static void Main()
                {
                    Threading.Lock l = new();
                    lock (l) { System.Console.Write("L"); }
                }
            }

            namespace Threading
            {
                public class Lock
                {
                    public Scope EnterScope()
                    {
                        System.Console.Write("E");
                        return new Scope();
                    }

                    public ref struct Scope
                    {
                        public void Dispose()
                        {
                            System.Console.Write("D");
                        }
                    }
                }
            }
            """;
        var verifier = CompileAndVerify(source, expectedOutput: "L", verify: Verification.FailsILVerify);
        verifier.VerifyDiagnostics();
        // Should use Monitor locking.
        verifier.VerifyIL("Program.Main", """
            {
              // Code size       39 (0x27)
              .maxstack  2
              .locals init (Threading.Lock V_0,
                            bool V_1)
              IL_0000:  newobj     "Threading.Lock..ctor()"
              IL_0005:  stloc.0
              IL_0006:  ldc.i4.0
              IL_0007:  stloc.1
              .try
              {
                IL_0008:  ldloc.0
                IL_0009:  ldloca.s   V_1
                IL_000b:  call       "void System.Threading.Monitor.Enter(object, ref bool)"
                IL_0010:  ldstr      "L"
                IL_0015:  call       "void System.Console.Write(string)"
                IL_001a:  leave.s    IL_0026
              }
              finally
              {
                IL_001c:  ldloc.1
                IL_001d:  brfalse.s  IL_0025
                IL_001f:  ldloc.0
                IL_0020:  call       "void System.Threading.Monitor.Exit(object)"
                IL_0025:  endfinally
              }
              IL_0026:  ret
            }
            """);
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
    public void MultipleLockTypes()
    {
        var source1 = """
            public static class C1
            {
                public static readonly System.Threading.Lock L = new();
            }

            namespace System.Threading
            {
                public class Lock
                {
                    public Scope EnterScope()
                    {
                        Console.Write("E1 ");
                        return new Scope();
                    }

                    public ref struct Scope
                    {
                        public void Dispose()
                        {
                            Console.Write("D1 ");
                        }
                    }
                }
            }
            """;
        var lib1 = CreateCompilation(source1)
            .VerifyDiagnostics()
            .EmitToImageReference();

        var source2 = """
            public static class C2
            {
                public static readonly System.Threading.Lock L = new();
            }


            namespace System.Threading
            {
                public class Lock
                {
                    public Scope EnterScope()
                    {
                        Console.Write("E2 ");
                        return new Scope();
                    }

                    public ref struct Scope
                    {
                        public void Dispose()
                        {
                            Console.Write("D2 ");
                        }
                    }
                }
            }
            """;
        var lib2 = CreateCompilation(source2)
            .VerifyDiagnostics()
            .EmitToImageReference();

        var source = """
            using System;

            static class Program
            {
                static void Main()
                {
                    M1();
                    M2();
                }

                static void M1()
                {
                    var l1 = C1.L;
                    lock (l1) { Console.Write("L1 "); }
                }

                static void M2()
                {
                    var l2 = C2.L;
                    lock (l2) { Console.Write("L2 "); }
                }
            }
            """;
        var verifier = CompileAndVerify(source, [lib1, lib2], expectedOutput: "E1 L1 D1 E2 L2 D2");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void LangVersion()
    {
        var source = """
            using System;
            using System.Threading;

            Lock l = new Lock();
            lock (l) { Console.Write("L"); }
            """;

        CSharpTestSource sources = [source, LockTypeDefinition];

        CreateCompilation(sources, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
            // (5,7): error CS8652: The feature 'Lock object' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // lock (l) { Console.Write("L"); }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "l").WithArguments("Lock object").WithLocation(5, 7));

        var expectedOutput = "ELD";

        CompileAndVerify(sources, parseOptions: TestOptions.RegularPreview, expectedOutput: expectedOutput,
            verify: Verification.FailsILVerify).VerifyDiagnostics();
        CompileAndVerify(sources, expectedOutput: expectedOutput,
            verify: Verification.FailsILVerify).VerifyDiagnostics();
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
    public void Nullable_01()
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

    [Fact]
    public void Nullable_02()
    {
        var source = """
            #nullable enable
            using System.Threading;

            static class C
            {
                static void Main()
                {
                    M(new Lock());
                }

                static void M(Lock? l)
                {
                    lock (l)
                    {
                        l.EnterScope();
                    }
                }
            }
            """;
        var verifier = CompileAndVerify([source, LockTypeDefinition], verify: Verification.FailsILVerify,
           expectedOutput: "EED");
        verifier.VerifyDiagnostics(
            // (13,15): warning CS8602: Dereference of a possibly null reference.
            //         lock (l)
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "l").WithLocation(13, 15));
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
              IL_0001:  callvirt   "System.Threading.Lock.Scope System.Threading.Lock.EnterScope()"
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
            using System.Threading.Tasks;

            class C
            {
                static async Task Main()
                {
                    lock (new Lock()) { System.Console.Write("L"); }
                }
            }
            """;
        var expectedOutput = "ELD";
        var verifier = CompileAndVerify([source, LockTypeDefinition], options: TestOptions.DebugExe,
            expectedOutput: expectedOutput, verify: Verification.FailsILVerify);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("C.<Main>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", """
            {
              // Code size       94 (0x5e)
              .maxstack  2
              .locals init (int V_0,
                            System.Threading.Lock.Scope V_1,
                            System.Exception V_2)
              IL_0000:  ldarg.0
              IL_0001:  ldfld      "int C.<Main>d__0.<>1__state"
              IL_0006:  stloc.0
              .try
              {
                IL_0007:  nop
                IL_0008:  newobj     "System.Threading.Lock..ctor()"
                IL_000d:  call       "System.Threading.Lock.Scope System.Threading.Lock.EnterScope()"
                IL_0012:  stloc.1
                .try
                {
                  IL_0013:  nop
                  IL_0014:  ldstr      "L"
                  IL_0019:  call       "void System.Console.Write(string)"
                  IL_001e:  nop
                  IL_001f:  nop
                  IL_0020:  leave.s    IL_002f
                }
                finally
                {
                  IL_0022:  ldloc.0
                  IL_0023:  ldc.i4.0
                  IL_0024:  bge.s      IL_002e
                  IL_0026:  ldloca.s   V_1
                  IL_0028:  call       "void System.Threading.Lock.Scope.Dispose()"
                  IL_002d:  nop
                  IL_002e:  endfinally
                }
                IL_002f:  leave.s    IL_0049
              }
              catch System.Exception
              {
                IL_0031:  stloc.2
                IL_0032:  ldarg.0
                IL_0033:  ldc.i4.s   -2
                IL_0035:  stfld      "int C.<Main>d__0.<>1__state"
                IL_003a:  ldarg.0
                IL_003b:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder"
                IL_0040:  ldloc.2
                IL_0041:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
                IL_0046:  nop
                IL_0047:  leave.s    IL_005d
              }
              IL_0049:  ldarg.0
              IL_004a:  ldc.i4.s   -2
              IL_004c:  stfld      "int C.<Main>d__0.<>1__state"
              IL_0051:  ldarg.0
              IL_0052:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder"
              IL_0057:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
              IL_005c:  nop
              IL_005d:  ret
            }
            """);

        verifier = CompileAndVerify([source, LockTypeDefinition], options: TestOptions.ReleaseExe,
            expectedOutput: expectedOutput, verify: Verification.FailsILVerify);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("C.<Main>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", """
            {
              // Code size       87 (0x57)
              .maxstack  2
              .locals init (int V_0,
                            System.Threading.Lock.Scope V_1,
                            System.Exception V_2)
              IL_0000:  ldarg.0
              IL_0001:  ldfld      "int C.<Main>d__0.<>1__state"
              IL_0006:  stloc.0
              .try
              {
                IL_0007:  newobj     "System.Threading.Lock..ctor()"
                IL_000c:  call       "System.Threading.Lock.Scope System.Threading.Lock.EnterScope()"
                IL_0011:  stloc.1
                .try
                {
                  IL_0012:  ldstr      "L"
                  IL_0017:  call       "void System.Console.Write(string)"
                  IL_001c:  leave.s    IL_002a
                }
                finally
                {
                  IL_001e:  ldloc.0
                  IL_001f:  ldc.i4.0
                  IL_0020:  bge.s      IL_0029
                  IL_0022:  ldloca.s   V_1
                  IL_0024:  call       "void System.Threading.Lock.Scope.Dispose()"
                  IL_0029:  endfinally
                }
                IL_002a:  leave.s    IL_0043
              }
              catch System.Exception
              {
                IL_002c:  stloc.2
                IL_002d:  ldarg.0
                IL_002e:  ldc.i4.s   -2
                IL_0030:  stfld      "int C.<Main>d__0.<>1__state"
                IL_0035:  ldarg.0
                IL_0036:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder"
                IL_003b:  ldloc.2
                IL_003c:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
                IL_0041:  leave.s    IL_0056
              }
              IL_0043:  ldarg.0
              IL_0044:  ldc.i4.s   -2
              IL_0046:  stfld      "int C.<Main>d__0.<>1__state"
              IL_004b:  ldarg.0
              IL_004c:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder"
              IL_0051:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
              IL_0056:  ret
            }
            """);
    }

    [Fact]
    public void AsyncMethod_WithAwait()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;

            class C
            {
                static async Task Main()
                {
                    await Task.Yield();
                    lock (new Lock()) { System.Console.Write("L"); }
                }
            }
            """;
        var expectedOutput = "ELD";
        var verifier = CompileAndVerify([source, LockTypeDefinition], options: TestOptions.DebugExe,
            expectedOutput: expectedOutput, verify: Verification.FailsILVerify);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("C.<Main>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", """
            {
              // Code size      199 (0xc7)
              .maxstack  3
              .locals init (int V_0,
                            System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                            System.Runtime.CompilerServices.YieldAwaitable V_2,
                            C.<Main>d__0 V_3,
                            System.Threading.Lock.Scope V_4,
                            System.Exception V_5)
              IL_0000:  ldarg.0
              IL_0001:  ldfld      "int C.<Main>d__0.<>1__state"
              IL_0006:  stloc.0
              .try
              {
                IL_0007:  ldloc.0
                IL_0008:  brfalse.s  IL_000c
                IL_000a:  br.s       IL_000e
                IL_000c:  br.s       IL_004a
                IL_000e:  nop
                IL_000f:  call       "System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()"
                IL_0014:  stloc.2
                IL_0015:  ldloca.s   V_2
                IL_0017:  call       "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()"
                IL_001c:  stloc.1
                IL_001d:  ldloca.s   V_1
                IL_001f:  call       "bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get"
                IL_0024:  brtrue.s   IL_0066
                IL_0026:  ldarg.0
                IL_0027:  ldc.i4.0
                IL_0028:  dup
                IL_0029:  stloc.0
                IL_002a:  stfld      "int C.<Main>d__0.<>1__state"
                IL_002f:  ldarg.0
                IL_0030:  ldloc.1
                IL_0031:  stfld      "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<Main>d__0.<>u__1"
                IL_0036:  ldarg.0
                IL_0037:  stloc.3
                IL_0038:  ldarg.0
                IL_0039:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder"
                IL_003e:  ldloca.s   V_1
                IL_0040:  ldloca.s   V_3
                IL_0042:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, C.<Main>d__0>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref C.<Main>d__0)"
                IL_0047:  nop
                IL_0048:  leave.s    IL_00c6
                IL_004a:  ldarg.0
                IL_004b:  ldfld      "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<Main>d__0.<>u__1"
                IL_0050:  stloc.1
                IL_0051:  ldarg.0
                IL_0052:  ldflda     "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<Main>d__0.<>u__1"
                IL_0057:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
                IL_005d:  ldarg.0
                IL_005e:  ldc.i4.m1
                IL_005f:  dup
                IL_0060:  stloc.0
                IL_0061:  stfld      "int C.<Main>d__0.<>1__state"
                IL_0066:  ldloca.s   V_1
                IL_0068:  call       "void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
                IL_006d:  nop
                IL_006e:  newobj     "System.Threading.Lock..ctor()"
                IL_0073:  call       "System.Threading.Lock.Scope System.Threading.Lock.EnterScope()"
                IL_0078:  stloc.s    V_4
                .try
                {
                  IL_007a:  nop
                  IL_007b:  ldstr      "L"
                  IL_0080:  call       "void System.Console.Write(string)"
                  IL_0085:  nop
                  IL_0086:  nop
                  IL_0087:  leave.s    IL_0096
                }
                finally
                {
                  IL_0089:  ldloc.0
                  IL_008a:  ldc.i4.0
                  IL_008b:  bge.s      IL_0095
                  IL_008d:  ldloca.s   V_4
                  IL_008f:  call       "void System.Threading.Lock.Scope.Dispose()"
                  IL_0094:  nop
                  IL_0095:  endfinally
                }
                IL_0096:  leave.s    IL_00b2
              }
              catch System.Exception
              {
                IL_0098:  stloc.s    V_5
                IL_009a:  ldarg.0
                IL_009b:  ldc.i4.s   -2
                IL_009d:  stfld      "int C.<Main>d__0.<>1__state"
                IL_00a2:  ldarg.0
                IL_00a3:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder"
                IL_00a8:  ldloc.s    V_5
                IL_00aa:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
                IL_00af:  nop
                IL_00b0:  leave.s    IL_00c6
              }
              IL_00b2:  ldarg.0
              IL_00b3:  ldc.i4.s   -2
              IL_00b5:  stfld      "int C.<Main>d__0.<>1__state"
              IL_00ba:  ldarg.0
              IL_00bb:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder"
              IL_00c0:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
              IL_00c5:  nop
              IL_00c6:  ret
            }
            """);

        verifier = CompileAndVerify([source, LockTypeDefinition], options: TestOptions.ReleaseExe,
            expectedOutput: expectedOutput, verify: Verification.FailsILVerify);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("C.<Main>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", """
            {
              // Code size      182 (0xb6)
              .maxstack  3
              .locals init (int V_0,
                            System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                            System.Runtime.CompilerServices.YieldAwaitable V_2,
                            System.Threading.Lock.Scope V_3,
                            System.Exception V_4)
              IL_0000:  ldarg.0
              IL_0001:  ldfld      "int C.<Main>d__0.<>1__state"
              IL_0006:  stloc.0
              .try
              {
                IL_0007:  ldloc.0
                IL_0008:  brfalse.s  IL_0041
                IL_000a:  call       "System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()"
                IL_000f:  stloc.2
                IL_0010:  ldloca.s   V_2
                IL_0012:  call       "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()"
                IL_0017:  stloc.1
                IL_0018:  ldloca.s   V_1
                IL_001a:  call       "bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get"
                IL_001f:  brtrue.s   IL_005d
                IL_0021:  ldarg.0
                IL_0022:  ldc.i4.0
                IL_0023:  dup
                IL_0024:  stloc.0
                IL_0025:  stfld      "int C.<Main>d__0.<>1__state"
                IL_002a:  ldarg.0
                IL_002b:  ldloc.1
                IL_002c:  stfld      "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<Main>d__0.<>u__1"
                IL_0031:  ldarg.0
                IL_0032:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder"
                IL_0037:  ldloca.s   V_1
                IL_0039:  ldarg.0
                IL_003a:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, C.<Main>d__0>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref C.<Main>d__0)"
                IL_003f:  leave.s    IL_00b5
                IL_0041:  ldarg.0
                IL_0042:  ldfld      "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<Main>d__0.<>u__1"
                IL_0047:  stloc.1
                IL_0048:  ldarg.0
                IL_0049:  ldflda     "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<Main>d__0.<>u__1"
                IL_004e:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
                IL_0054:  ldarg.0
                IL_0055:  ldc.i4.m1
                IL_0056:  dup
                IL_0057:  stloc.0
                IL_0058:  stfld      "int C.<Main>d__0.<>1__state"
                IL_005d:  ldloca.s   V_1
                IL_005f:  call       "void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
                IL_0064:  newobj     "System.Threading.Lock..ctor()"
                IL_0069:  call       "System.Threading.Lock.Scope System.Threading.Lock.EnterScope()"
                IL_006e:  stloc.3
                .try
                {
                  IL_006f:  ldstr      "L"
                  IL_0074:  call       "void System.Console.Write(string)"
                  IL_0079:  leave.s    IL_0087
                }
                finally
                {
                  IL_007b:  ldloc.0
                  IL_007c:  ldc.i4.0
                  IL_007d:  bge.s      IL_0086
                  IL_007f:  ldloca.s   V_3
                  IL_0081:  call       "void System.Threading.Lock.Scope.Dispose()"
                  IL_0086:  endfinally
                }
                IL_0087:  leave.s    IL_00a2
              }
              catch System.Exception
              {
                IL_0089:  stloc.s    V_4
                IL_008b:  ldarg.0
                IL_008c:  ldc.i4.s   -2
                IL_008e:  stfld      "int C.<Main>d__0.<>1__state"
                IL_0093:  ldarg.0
                IL_0094:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder"
                IL_0099:  ldloc.s    V_4
                IL_009b:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
                IL_00a0:  leave.s    IL_00b5
              }
              IL_00a2:  ldarg.0
              IL_00a3:  ldc.i4.s   -2
              IL_00a5:  stfld      "int C.<Main>d__0.<>1__state"
              IL_00aa:  ldarg.0
              IL_00ab:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder"
              IL_00b0:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
              IL_00b5:  ret
            }
            """);
    }

    [Fact]
    public void AsyncMethod_AwaitResource()
    {
        var source = """
            #pragma warning disable 1998 // async method lacks 'await' operators
            using System.Threading;
            using System.Threading.Tasks;

            class C
            {
                static async Task Main()
                {
                    lock (await GetLock()) { System.Console.Write("L"); }
                }

                static async Task<Lock> GetLock() => new Lock();
            }
            """;
        var expectedOutput = "ELD";
        var verifier = CompileAndVerify([source, LockTypeDefinition], options: TestOptions.DebugExe,
            expectedOutput: expectedOutput, verify: Verification.FailsILVerify);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("C.<Main>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", """
            {
              // Code size      211 (0xd3)
              .maxstack  3
              .locals init (int V_0,
                            System.Threading.Lock.Scope V_1,
                            System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Lock> V_2,
                            C.<Main>d__0 V_3,
                            System.Exception V_4)
              IL_0000:  ldarg.0
              IL_0001:  ldfld      "int C.<Main>d__0.<>1__state"
              IL_0006:  stloc.0
              .try
              {
                IL_0007:  ldloc.0
                IL_0008:  brfalse.s  IL_000c
                IL_000a:  br.s       IL_000e
                IL_000c:  br.s       IL_004a
                IL_000e:  nop
                IL_000f:  call       "System.Threading.Tasks.Task<System.Threading.Lock> C.GetLock()"
                IL_0014:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Lock> System.Threading.Tasks.Task<System.Threading.Lock>.GetAwaiter()"
                IL_0019:  stloc.2
                IL_001a:  ldloca.s   V_2
                IL_001c:  call       "bool System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Lock>.IsCompleted.get"
                IL_0021:  brtrue.s   IL_0066
                IL_0023:  ldarg.0
                IL_0024:  ldc.i4.0
                IL_0025:  dup
                IL_0026:  stloc.0
                IL_0027:  stfld      "int C.<Main>d__0.<>1__state"
                IL_002c:  ldarg.0
                IL_002d:  ldloc.2
                IL_002e:  stfld      "System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Lock> C.<Main>d__0.<>u__1"
                IL_0033:  ldarg.0
                IL_0034:  stloc.3
                IL_0035:  ldarg.0
                IL_0036:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder"
                IL_003b:  ldloca.s   V_2
                IL_003d:  ldloca.s   V_3
                IL_003f:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Lock>, C.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Lock>, ref C.<Main>d__0)"
                IL_0044:  nop
                IL_0045:  leave      IL_00d2
                IL_004a:  ldarg.0
                IL_004b:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Lock> C.<Main>d__0.<>u__1"
                IL_0050:  stloc.2
                IL_0051:  ldarg.0
                IL_0052:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Lock> C.<Main>d__0.<>u__1"
                IL_0057:  initobj    "System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Lock>"
                IL_005d:  ldarg.0
                IL_005e:  ldc.i4.m1
                IL_005f:  dup
                IL_0060:  stloc.0
                IL_0061:  stfld      "int C.<Main>d__0.<>1__state"
                IL_0066:  ldarg.0
                IL_0067:  ldloca.s   V_2
                IL_0069:  call       "System.Threading.Lock System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Lock>.GetResult()"
                IL_006e:  stfld      "System.Threading.Lock C.<Main>d__0.<>s__1"
                IL_0073:  ldarg.0
                IL_0074:  ldfld      "System.Threading.Lock C.<Main>d__0.<>s__1"
                IL_0079:  callvirt   "System.Threading.Lock.Scope System.Threading.Lock.EnterScope()"
                IL_007e:  stloc.1
                IL_007f:  ldarg.0
                IL_0080:  ldnull
                IL_0081:  stfld      "System.Threading.Lock C.<Main>d__0.<>s__1"
                .try
                {
                  IL_0086:  nop
                  IL_0087:  ldstr      "L"
                  IL_008c:  call       "void System.Console.Write(string)"
                  IL_0091:  nop
                  IL_0092:  nop
                  IL_0093:  leave.s    IL_00a2
                }
                finally
                {
                  IL_0095:  ldloc.0
                  IL_0096:  ldc.i4.0
                  IL_0097:  bge.s      IL_00a1
                  IL_0099:  ldloca.s   V_1
                  IL_009b:  call       "void System.Threading.Lock.Scope.Dispose()"
                  IL_00a0:  nop
                  IL_00a1:  endfinally
                }
                IL_00a2:  leave.s    IL_00be
              }
              catch System.Exception
              {
                IL_00a4:  stloc.s    V_4
                IL_00a6:  ldarg.0
                IL_00a7:  ldc.i4.s   -2
                IL_00a9:  stfld      "int C.<Main>d__0.<>1__state"
                IL_00ae:  ldarg.0
                IL_00af:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder"
                IL_00b4:  ldloc.s    V_4
                IL_00b6:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
                IL_00bb:  nop
                IL_00bc:  leave.s    IL_00d2
              }
              IL_00be:  ldarg.0
              IL_00bf:  ldc.i4.s   -2
              IL_00c1:  stfld      "int C.<Main>d__0.<>1__state"
              IL_00c6:  ldarg.0
              IL_00c7:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder"
              IL_00cc:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
              IL_00d1:  nop
              IL_00d2:  ret
            }
            """);

        verifier = CompileAndVerify([source, LockTypeDefinition], options: TestOptions.ReleaseExe,
            expectedOutput: expectedOutput, verify: Verification.FailsILVerify);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("C.<Main>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", """
            {
              // Code size      172 (0xac)
              .maxstack  3
              .locals init (int V_0,
                            System.Threading.Lock.Scope V_1,
                            System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Lock> V_2,
                            System.Exception V_3)
              IL_0000:  ldarg.0
              IL_0001:  ldfld      "int C.<Main>d__0.<>1__state"
              IL_0006:  stloc.0
              .try
              {
                IL_0007:  ldloc.0
                IL_0008:  brfalse.s  IL_003e
                IL_000a:  call       "System.Threading.Tasks.Task<System.Threading.Lock> C.GetLock()"
                IL_000f:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Lock> System.Threading.Tasks.Task<System.Threading.Lock>.GetAwaiter()"
                IL_0014:  stloc.2
                IL_0015:  ldloca.s   V_2
                IL_0017:  call       "bool System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Lock>.IsCompleted.get"
                IL_001c:  brtrue.s   IL_005a
                IL_001e:  ldarg.0
                IL_001f:  ldc.i4.0
                IL_0020:  dup
                IL_0021:  stloc.0
                IL_0022:  stfld      "int C.<Main>d__0.<>1__state"
                IL_0027:  ldarg.0
                IL_0028:  ldloc.2
                IL_0029:  stfld      "System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Lock> C.<Main>d__0.<>u__1"
                IL_002e:  ldarg.0
                IL_002f:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder"
                IL_0034:  ldloca.s   V_2
                IL_0036:  ldarg.0
                IL_0037:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Lock>, C.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Lock>, ref C.<Main>d__0)"
                IL_003c:  leave.s    IL_00ab
                IL_003e:  ldarg.0
                IL_003f:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Lock> C.<Main>d__0.<>u__1"
                IL_0044:  stloc.2
                IL_0045:  ldarg.0
                IL_0046:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Lock> C.<Main>d__0.<>u__1"
                IL_004b:  initobj    "System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Lock>"
                IL_0051:  ldarg.0
                IL_0052:  ldc.i4.m1
                IL_0053:  dup
                IL_0054:  stloc.0
                IL_0055:  stfld      "int C.<Main>d__0.<>1__state"
                IL_005a:  ldloca.s   V_2
                IL_005c:  call       "System.Threading.Lock System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Lock>.GetResult()"
                IL_0061:  callvirt   "System.Threading.Lock.Scope System.Threading.Lock.EnterScope()"
                IL_0066:  stloc.1
                .try
                {
                  IL_0067:  ldstr      "L"
                  IL_006c:  call       "void System.Console.Write(string)"
                  IL_0071:  leave.s    IL_007f
                }
                finally
                {
                  IL_0073:  ldloc.0
                  IL_0074:  ldc.i4.0
                  IL_0075:  bge.s      IL_007e
                  IL_0077:  ldloca.s   V_1
                  IL_0079:  call       "void System.Threading.Lock.Scope.Dispose()"
                  IL_007e:  endfinally
                }
                IL_007f:  leave.s    IL_0098
              }
              catch System.Exception
              {
                IL_0081:  stloc.3
                IL_0082:  ldarg.0
                IL_0083:  ldc.i4.s   -2
                IL_0085:  stfld      "int C.<Main>d__0.<>1__state"
                IL_008a:  ldarg.0
                IL_008b:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder"
                IL_0090:  ldloc.3
                IL_0091:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
                IL_0096:  leave.s    IL_00ab
              }
              IL_0098:  ldarg.0
              IL_0099:  ldc.i4.s   -2
              IL_009b:  stfld      "int C.<Main>d__0.<>1__state"
              IL_00a0:  ldarg.0
              IL_00a1:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder"
              IL_00a6:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
              IL_00ab:  ret
            }
            """);
    }

    [Fact]
    public void AsyncLocalFunction()
    {
        var source = """
            #pragma warning disable 1998 // async method lacks 'await' operators
            using System.Threading;

            async void local()
            {
                lock (new Lock()) { System.Console.Write("L"); }
            }

            local();
            """;
        var expectedOutput = "ELD";
        var verifier = CompileAndVerify([source, LockTypeDefinition], options: TestOptions.DebugExe,
            expectedOutput: expectedOutput, verify: Verification.FailsILVerify);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Program.<<<Main>$>g__local|0_0>d.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", """
            {
              // Code size       94 (0x5e)
              .maxstack  2
              .locals init (int V_0,
                            System.Threading.Lock.Scope V_1,
                            System.Exception V_2)
              IL_0000:  ldarg.0
              IL_0001:  ldfld      "int Program.<<<Main>$>g__local|0_0>d.<>1__state"
              IL_0006:  stloc.0
              .try
              {
                IL_0007:  nop
                IL_0008:  newobj     "System.Threading.Lock..ctor()"
                IL_000d:  call       "System.Threading.Lock.Scope System.Threading.Lock.EnterScope()"
                IL_0012:  stloc.1
                .try
                {
                  IL_0013:  nop
                  IL_0014:  ldstr      "L"
                  IL_0019:  call       "void System.Console.Write(string)"
                  IL_001e:  nop
                  IL_001f:  nop
                  IL_0020:  leave.s    IL_002f
                }
                finally
                {
                  IL_0022:  ldloc.0
                  IL_0023:  ldc.i4.0
                  IL_0024:  bge.s      IL_002e
                  IL_0026:  ldloca.s   V_1
                  IL_0028:  call       "void System.Threading.Lock.Scope.Dispose()"
                  IL_002d:  nop
                  IL_002e:  endfinally
                }
                IL_002f:  leave.s    IL_0049
              }
              catch System.Exception
              {
                IL_0031:  stloc.2
                IL_0032:  ldarg.0
                IL_0033:  ldc.i4.s   -2
                IL_0035:  stfld      "int Program.<<<Main>$>g__local|0_0>d.<>1__state"
                IL_003a:  ldarg.0
                IL_003b:  ldflda     "System.Runtime.CompilerServices.AsyncVoidMethodBuilder Program.<<<Main>$>g__local|0_0>d.<>t__builder"
                IL_0040:  ldloc.2
                IL_0041:  call       "void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetException(System.Exception)"
                IL_0046:  nop
                IL_0047:  leave.s    IL_005d
              }
              IL_0049:  ldarg.0
              IL_004a:  ldc.i4.s   -2
              IL_004c:  stfld      "int Program.<<<Main>$>g__local|0_0>d.<>1__state"
              IL_0051:  ldarg.0
              IL_0052:  ldflda     "System.Runtime.CompilerServices.AsyncVoidMethodBuilder Program.<<<Main>$>g__local|0_0>d.<>t__builder"
              IL_0057:  call       "void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetResult()"
              IL_005c:  nop
              IL_005d:  ret
            }
            """);

        verifier = CompileAndVerify([source, LockTypeDefinition], options: TestOptions.ReleaseExe,
            expectedOutput: expectedOutput, verify: Verification.FailsILVerify);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Program.<<<Main>$>g__local|0_0>d.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", """
            {
              // Code size       87 (0x57)
              .maxstack  2
              .locals init (int V_0,
                            System.Threading.Lock.Scope V_1,
                            System.Exception V_2)
              IL_0000:  ldarg.0
              IL_0001:  ldfld      "int Program.<<<Main>$>g__local|0_0>d.<>1__state"
              IL_0006:  stloc.0
              .try
              {
                IL_0007:  newobj     "System.Threading.Lock..ctor()"
                IL_000c:  call       "System.Threading.Lock.Scope System.Threading.Lock.EnterScope()"
                IL_0011:  stloc.1
                .try
                {
                  IL_0012:  ldstr      "L"
                  IL_0017:  call       "void System.Console.Write(string)"
                  IL_001c:  leave.s    IL_002a
                }
                finally
                {
                  IL_001e:  ldloc.0
                  IL_001f:  ldc.i4.0
                  IL_0020:  bge.s      IL_0029
                  IL_0022:  ldloca.s   V_1
                  IL_0024:  call       "void System.Threading.Lock.Scope.Dispose()"
                  IL_0029:  endfinally
                }
                IL_002a:  leave.s    IL_0043
              }
              catch System.Exception
              {
                IL_002c:  stloc.2
                IL_002d:  ldarg.0
                IL_002e:  ldc.i4.s   -2
                IL_0030:  stfld      "int Program.<<<Main>$>g__local|0_0>d.<>1__state"
                IL_0035:  ldarg.0
                IL_0036:  ldflda     "System.Runtime.CompilerServices.AsyncVoidMethodBuilder Program.<<<Main>$>g__local|0_0>d.<>t__builder"
                IL_003b:  ldloc.2
                IL_003c:  call       "void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetException(System.Exception)"
                IL_0041:  leave.s    IL_0056
              }
              IL_0043:  ldarg.0
              IL_0044:  ldc.i4.s   -2
              IL_0046:  stfld      "int Program.<<<Main>$>g__local|0_0>d.<>1__state"
              IL_004b:  ldarg.0
              IL_004c:  ldflda     "System.Runtime.CompilerServices.AsyncVoidMethodBuilder Program.<<<Main>$>g__local|0_0>d.<>t__builder"
              IL_0051:  call       "void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetResult()"
              IL_0056:  ret
            }
            """);
    }

    [Fact]
    public void AsyncLocalFunction_WithAwait()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;

            async Task local()
            {
                await Task.Yield();
                lock (new Lock()) { System.Console.Write("L"); }
            }

            await local();
            """;
        var expectedOutput = "ELD";
        var verifier = CompileAndVerify([source, LockTypeDefinition], options: TestOptions.DebugExe,
            expectedOutput: expectedOutput, verify: Verification.FailsILVerify);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Program.<<<Main>$>g__local|0_0>d.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", """
            {
              // Code size      199 (0xc7)
              .maxstack  3
              .locals init (int V_0,
                            System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                            System.Runtime.CompilerServices.YieldAwaitable V_2,
                            Program.<<<Main>$>g__local|0_0>d V_3,
                            System.Threading.Lock.Scope V_4,
                            System.Exception V_5)
              IL_0000:  ldarg.0
              IL_0001:  ldfld      "int Program.<<<Main>$>g__local|0_0>d.<>1__state"
              IL_0006:  stloc.0
              .try
              {
                IL_0007:  ldloc.0
                IL_0008:  brfalse.s  IL_000c
                IL_000a:  br.s       IL_000e
                IL_000c:  br.s       IL_004a
                IL_000e:  nop
                IL_000f:  call       "System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()"
                IL_0014:  stloc.2
                IL_0015:  ldloca.s   V_2
                IL_0017:  call       "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()"
                IL_001c:  stloc.1
                IL_001d:  ldloca.s   V_1
                IL_001f:  call       "bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get"
                IL_0024:  brtrue.s   IL_0066
                IL_0026:  ldarg.0
                IL_0027:  ldc.i4.0
                IL_0028:  dup
                IL_0029:  stloc.0
                IL_002a:  stfld      "int Program.<<<Main>$>g__local|0_0>d.<>1__state"
                IL_002f:  ldarg.0
                IL_0030:  ldloc.1
                IL_0031:  stfld      "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<<<Main>$>g__local|0_0>d.<>u__1"
                IL_0036:  ldarg.0
                IL_0037:  stloc.3
                IL_0038:  ldarg.0
                IL_0039:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<<<Main>$>g__local|0_0>d.<>t__builder"
                IL_003e:  ldloca.s   V_1
                IL_0040:  ldloca.s   V_3
                IL_0042:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<<<Main>$>g__local|0_0>d>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<<<Main>$>g__local|0_0>d)"
                IL_0047:  nop
                IL_0048:  leave.s    IL_00c6
                IL_004a:  ldarg.0
                IL_004b:  ldfld      "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<<<Main>$>g__local|0_0>d.<>u__1"
                IL_0050:  stloc.1
                IL_0051:  ldarg.0
                IL_0052:  ldflda     "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<<<Main>$>g__local|0_0>d.<>u__1"
                IL_0057:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
                IL_005d:  ldarg.0
                IL_005e:  ldc.i4.m1
                IL_005f:  dup
                IL_0060:  stloc.0
                IL_0061:  stfld      "int Program.<<<Main>$>g__local|0_0>d.<>1__state"
                IL_0066:  ldloca.s   V_1
                IL_0068:  call       "void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
                IL_006d:  nop
                IL_006e:  newobj     "System.Threading.Lock..ctor()"
                IL_0073:  call       "System.Threading.Lock.Scope System.Threading.Lock.EnterScope()"
                IL_0078:  stloc.s    V_4
                .try
                {
                  IL_007a:  nop
                  IL_007b:  ldstr      "L"
                  IL_0080:  call       "void System.Console.Write(string)"
                  IL_0085:  nop
                  IL_0086:  nop
                  IL_0087:  leave.s    IL_0096
                }
                finally
                {
                  IL_0089:  ldloc.0
                  IL_008a:  ldc.i4.0
                  IL_008b:  bge.s      IL_0095
                  IL_008d:  ldloca.s   V_4
                  IL_008f:  call       "void System.Threading.Lock.Scope.Dispose()"
                  IL_0094:  nop
                  IL_0095:  endfinally
                }
                IL_0096:  leave.s    IL_00b2
              }
              catch System.Exception
              {
                IL_0098:  stloc.s    V_5
                IL_009a:  ldarg.0
                IL_009b:  ldc.i4.s   -2
                IL_009d:  stfld      "int Program.<<<Main>$>g__local|0_0>d.<>1__state"
                IL_00a2:  ldarg.0
                IL_00a3:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<<<Main>$>g__local|0_0>d.<>t__builder"
                IL_00a8:  ldloc.s    V_5
                IL_00aa:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
                IL_00af:  nop
                IL_00b0:  leave.s    IL_00c6
              }
              IL_00b2:  ldarg.0
              IL_00b3:  ldc.i4.s   -2
              IL_00b5:  stfld      "int Program.<<<Main>$>g__local|0_0>d.<>1__state"
              IL_00ba:  ldarg.0
              IL_00bb:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<<<Main>$>g__local|0_0>d.<>t__builder"
              IL_00c0:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
              IL_00c5:  nop
              IL_00c6:  ret
            }
            """);

        verifier = CompileAndVerify([source, LockTypeDefinition], options: TestOptions.ReleaseExe,
            expectedOutput: expectedOutput, verify: Verification.FailsILVerify);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Program.<<<Main>$>g__local|0_0>d.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", """
            {
              // Code size      182 (0xb6)
              .maxstack  3
              .locals init (int V_0,
                            System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                            System.Runtime.CompilerServices.YieldAwaitable V_2,
                            System.Threading.Lock.Scope V_3,
                            System.Exception V_4)
              IL_0000:  ldarg.0
              IL_0001:  ldfld      "int Program.<<<Main>$>g__local|0_0>d.<>1__state"
              IL_0006:  stloc.0
              .try
              {
                IL_0007:  ldloc.0
                IL_0008:  brfalse.s  IL_0041
                IL_000a:  call       "System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()"
                IL_000f:  stloc.2
                IL_0010:  ldloca.s   V_2
                IL_0012:  call       "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()"
                IL_0017:  stloc.1
                IL_0018:  ldloca.s   V_1
                IL_001a:  call       "bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get"
                IL_001f:  brtrue.s   IL_005d
                IL_0021:  ldarg.0
                IL_0022:  ldc.i4.0
                IL_0023:  dup
                IL_0024:  stloc.0
                IL_0025:  stfld      "int Program.<<<Main>$>g__local|0_0>d.<>1__state"
                IL_002a:  ldarg.0
                IL_002b:  ldloc.1
                IL_002c:  stfld      "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<<<Main>$>g__local|0_0>d.<>u__1"
                IL_0031:  ldarg.0
                IL_0032:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<<<Main>$>g__local|0_0>d.<>t__builder"
                IL_0037:  ldloca.s   V_1
                IL_0039:  ldarg.0
                IL_003a:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<<<Main>$>g__local|0_0>d>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<<<Main>$>g__local|0_0>d)"
                IL_003f:  leave.s    IL_00b5
                IL_0041:  ldarg.0
                IL_0042:  ldfld      "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<<<Main>$>g__local|0_0>d.<>u__1"
                IL_0047:  stloc.1
                IL_0048:  ldarg.0
                IL_0049:  ldflda     "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<<<Main>$>g__local|0_0>d.<>u__1"
                IL_004e:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
                IL_0054:  ldarg.0
                IL_0055:  ldc.i4.m1
                IL_0056:  dup
                IL_0057:  stloc.0
                IL_0058:  stfld      "int Program.<<<Main>$>g__local|0_0>d.<>1__state"
                IL_005d:  ldloca.s   V_1
                IL_005f:  call       "void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
                IL_0064:  newobj     "System.Threading.Lock..ctor()"
                IL_0069:  call       "System.Threading.Lock.Scope System.Threading.Lock.EnterScope()"
                IL_006e:  stloc.3
                .try
                {
                  IL_006f:  ldstr      "L"
                  IL_0074:  call       "void System.Console.Write(string)"
                  IL_0079:  leave.s    IL_0087
                }
                finally
                {
                  IL_007b:  ldloc.0
                  IL_007c:  ldc.i4.0
                  IL_007d:  bge.s      IL_0086
                  IL_007f:  ldloca.s   V_3
                  IL_0081:  call       "void System.Threading.Lock.Scope.Dispose()"
                  IL_0086:  endfinally
                }
                IL_0087:  leave.s    IL_00a2
              }
              catch System.Exception
              {
                IL_0089:  stloc.s    V_4
                IL_008b:  ldarg.0
                IL_008c:  ldc.i4.s   -2
                IL_008e:  stfld      "int Program.<<<Main>$>g__local|0_0>d.<>1__state"
                IL_0093:  ldarg.0
                IL_0094:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<<<Main>$>g__local|0_0>d.<>t__builder"
                IL_0099:  ldloc.s    V_4
                IL_009b:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
                IL_00a0:  leave.s    IL_00b5
              }
              IL_00a2:  ldarg.0
              IL_00a3:  ldc.i4.s   -2
              IL_00a5:  stfld      "int Program.<<<Main>$>g__local|0_0>d.<>1__state"
              IL_00aa:  ldarg.0
              IL_00ab:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<<<Main>$>g__local|0_0>d.<>t__builder"
              IL_00b0:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
              IL_00b5:  ret
            }
            """);
    }

    [Fact]
    public void AsyncLambda()
    {
        var source = """
            #pragma warning disable 1998 // async method lacks 'await' operators
            using System.Threading;

            var lam = async () =>
            {
                lock (new Lock()) { System.Console.Write("L"); }
            };

            await lam();
            """;
        var expectedOutput = "ELD";
        var verifier = CompileAndVerify([source, LockTypeDefinition], options: TestOptions.DebugExe,
            expectedOutput: expectedOutput, verify: Verification.FailsILVerify);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Program.<>c.<<<Main>$>b__0_0>d.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", """
            {
              // Code size       94 (0x5e)
              .maxstack  2
              .locals init (int V_0,
                            System.Threading.Lock.Scope V_1,
                            System.Exception V_2)
              IL_0000:  ldarg.0
              IL_0001:  ldfld      "int Program.<>c.<<<Main>$>b__0_0>d.<>1__state"
              IL_0006:  stloc.0
              .try
              {
                IL_0007:  nop
                IL_0008:  newobj     "System.Threading.Lock..ctor()"
                IL_000d:  call       "System.Threading.Lock.Scope System.Threading.Lock.EnterScope()"
                IL_0012:  stloc.1
                .try
                {
                  IL_0013:  nop
                  IL_0014:  ldstr      "L"
                  IL_0019:  call       "void System.Console.Write(string)"
                  IL_001e:  nop
                  IL_001f:  nop
                  IL_0020:  leave.s    IL_002f
                }
                finally
                {
                  IL_0022:  ldloc.0
                  IL_0023:  ldc.i4.0
                  IL_0024:  bge.s      IL_002e
                  IL_0026:  ldloca.s   V_1
                  IL_0028:  call       "void System.Threading.Lock.Scope.Dispose()"
                  IL_002d:  nop
                  IL_002e:  endfinally
                }
                IL_002f:  leave.s    IL_0049
              }
              catch System.Exception
              {
                IL_0031:  stloc.2
                IL_0032:  ldarg.0
                IL_0033:  ldc.i4.s   -2
                IL_0035:  stfld      "int Program.<>c.<<<Main>$>b__0_0>d.<>1__state"
                IL_003a:  ldarg.0
                IL_003b:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<>c.<<<Main>$>b__0_0>d.<>t__builder"
                IL_0040:  ldloc.2
                IL_0041:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
                IL_0046:  nop
                IL_0047:  leave.s    IL_005d
              }
              IL_0049:  ldarg.0
              IL_004a:  ldc.i4.s   -2
              IL_004c:  stfld      "int Program.<>c.<<<Main>$>b__0_0>d.<>1__state"
              IL_0051:  ldarg.0
              IL_0052:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<>c.<<<Main>$>b__0_0>d.<>t__builder"
              IL_0057:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
              IL_005c:  nop
              IL_005d:  ret
            }
            """);

        verifier = CompileAndVerify([source, LockTypeDefinition], options: TestOptions.ReleaseExe,
            expectedOutput: expectedOutput, verify: Verification.FailsILVerify);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Program.<>c.<<<Main>$>b__0_0>d.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", """
            {
              // Code size       87 (0x57)
              .maxstack  2
              .locals init (int V_0,
                            System.Threading.Lock.Scope V_1,
                            System.Exception V_2)
              IL_0000:  ldarg.0
              IL_0001:  ldfld      "int Program.<>c.<<<Main>$>b__0_0>d.<>1__state"
              IL_0006:  stloc.0
              .try
              {
                IL_0007:  newobj     "System.Threading.Lock..ctor()"
                IL_000c:  call       "System.Threading.Lock.Scope System.Threading.Lock.EnterScope()"
                IL_0011:  stloc.1
                .try
                {
                  IL_0012:  ldstr      "L"
                  IL_0017:  call       "void System.Console.Write(string)"
                  IL_001c:  leave.s    IL_002a
                }
                finally
                {
                  IL_001e:  ldloc.0
                  IL_001f:  ldc.i4.0
                  IL_0020:  bge.s      IL_0029
                  IL_0022:  ldloca.s   V_1
                  IL_0024:  call       "void System.Threading.Lock.Scope.Dispose()"
                  IL_0029:  endfinally
                }
                IL_002a:  leave.s    IL_0043
              }
              catch System.Exception
              {
                IL_002c:  stloc.2
                IL_002d:  ldarg.0
                IL_002e:  ldc.i4.s   -2
                IL_0030:  stfld      "int Program.<>c.<<<Main>$>b__0_0>d.<>1__state"
                IL_0035:  ldarg.0
                IL_0036:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<>c.<<<Main>$>b__0_0>d.<>t__builder"
                IL_003b:  ldloc.2
                IL_003c:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
                IL_0041:  leave.s    IL_0056
              }
              IL_0043:  ldarg.0
              IL_0044:  ldc.i4.s   -2
              IL_0046:  stfld      "int Program.<>c.<<<Main>$>b__0_0>d.<>1__state"
              IL_004b:  ldarg.0
              IL_004c:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<>c.<<<Main>$>b__0_0>d.<>t__builder"
              IL_0051:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
              IL_0056:  ret
            }
            """);
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
                lock (new Lock()) { System.Console.Write("L"); }
            };

            await lam();
            """;
        var expectedOutput = "ELD";
        var verifier = CompileAndVerify([source, LockTypeDefinition], options: TestOptions.DebugExe,
            expectedOutput: expectedOutput, verify: Verification.FailsILVerify);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Program.<>c.<<<Main>$>b__0_0>d.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", """
            {
              // Code size      199 (0xc7)
              .maxstack  3
              .locals init (int V_0,
                            System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                            System.Runtime.CompilerServices.YieldAwaitable V_2,
                            Program.<>c.<<<Main>$>b__0_0>d V_3,
                            System.Threading.Lock.Scope V_4,
                            System.Exception V_5)
              IL_0000:  ldarg.0
              IL_0001:  ldfld      "int Program.<>c.<<<Main>$>b__0_0>d.<>1__state"
              IL_0006:  stloc.0
              .try
              {
                IL_0007:  ldloc.0
                IL_0008:  brfalse.s  IL_000c
                IL_000a:  br.s       IL_000e
                IL_000c:  br.s       IL_004a
                IL_000e:  nop
                IL_000f:  call       "System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()"
                IL_0014:  stloc.2
                IL_0015:  ldloca.s   V_2
                IL_0017:  call       "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()"
                IL_001c:  stloc.1
                IL_001d:  ldloca.s   V_1
                IL_001f:  call       "bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get"
                IL_0024:  brtrue.s   IL_0066
                IL_0026:  ldarg.0
                IL_0027:  ldc.i4.0
                IL_0028:  dup
                IL_0029:  stloc.0
                IL_002a:  stfld      "int Program.<>c.<<<Main>$>b__0_0>d.<>1__state"
                IL_002f:  ldarg.0
                IL_0030:  ldloc.1
                IL_0031:  stfld      "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<>c.<<<Main>$>b__0_0>d.<>u__1"
                IL_0036:  ldarg.0
                IL_0037:  stloc.3
                IL_0038:  ldarg.0
                IL_0039:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<>c.<<<Main>$>b__0_0>d.<>t__builder"
                IL_003e:  ldloca.s   V_1
                IL_0040:  ldloca.s   V_3
                IL_0042:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<>c.<<<Main>$>b__0_0>d>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<>c.<<<Main>$>b__0_0>d)"
                IL_0047:  nop
                IL_0048:  leave.s    IL_00c6
                IL_004a:  ldarg.0
                IL_004b:  ldfld      "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<>c.<<<Main>$>b__0_0>d.<>u__1"
                IL_0050:  stloc.1
                IL_0051:  ldarg.0
                IL_0052:  ldflda     "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<>c.<<<Main>$>b__0_0>d.<>u__1"
                IL_0057:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
                IL_005d:  ldarg.0
                IL_005e:  ldc.i4.m1
                IL_005f:  dup
                IL_0060:  stloc.0
                IL_0061:  stfld      "int Program.<>c.<<<Main>$>b__0_0>d.<>1__state"
                IL_0066:  ldloca.s   V_1
                IL_0068:  call       "void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
                IL_006d:  nop
                IL_006e:  newobj     "System.Threading.Lock..ctor()"
                IL_0073:  call       "System.Threading.Lock.Scope System.Threading.Lock.EnterScope()"
                IL_0078:  stloc.s    V_4
                .try
                {
                  IL_007a:  nop
                  IL_007b:  ldstr      "L"
                  IL_0080:  call       "void System.Console.Write(string)"
                  IL_0085:  nop
                  IL_0086:  nop
                  IL_0087:  leave.s    IL_0096
                }
                finally
                {
                  IL_0089:  ldloc.0
                  IL_008a:  ldc.i4.0
                  IL_008b:  bge.s      IL_0095
                  IL_008d:  ldloca.s   V_4
                  IL_008f:  call       "void System.Threading.Lock.Scope.Dispose()"
                  IL_0094:  nop
                  IL_0095:  endfinally
                }
                IL_0096:  leave.s    IL_00b2
              }
              catch System.Exception
              {
                IL_0098:  stloc.s    V_5
                IL_009a:  ldarg.0
                IL_009b:  ldc.i4.s   -2
                IL_009d:  stfld      "int Program.<>c.<<<Main>$>b__0_0>d.<>1__state"
                IL_00a2:  ldarg.0
                IL_00a3:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<>c.<<<Main>$>b__0_0>d.<>t__builder"
                IL_00a8:  ldloc.s    V_5
                IL_00aa:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
                IL_00af:  nop
                IL_00b0:  leave.s    IL_00c6
              }
              IL_00b2:  ldarg.0
              IL_00b3:  ldc.i4.s   -2
              IL_00b5:  stfld      "int Program.<>c.<<<Main>$>b__0_0>d.<>1__state"
              IL_00ba:  ldarg.0
              IL_00bb:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<>c.<<<Main>$>b__0_0>d.<>t__builder"
              IL_00c0:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
              IL_00c5:  nop
              IL_00c6:  ret
            }
            """);

        verifier = CompileAndVerify([source, LockTypeDefinition], options: TestOptions.ReleaseExe,
            expectedOutput: expectedOutput, verify: Verification.FailsILVerify);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Program.<>c.<<<Main>$>b__0_0>d.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", """
            {
              // Code size      182 (0xb6)
              .maxstack  3
              .locals init (int V_0,
                            System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                            System.Runtime.CompilerServices.YieldAwaitable V_2,
                            System.Threading.Lock.Scope V_3,
                            System.Exception V_4)
              IL_0000:  ldarg.0
              IL_0001:  ldfld      "int Program.<>c.<<<Main>$>b__0_0>d.<>1__state"
              IL_0006:  stloc.0
              .try
              {
                IL_0007:  ldloc.0
                IL_0008:  brfalse.s  IL_0041
                IL_000a:  call       "System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()"
                IL_000f:  stloc.2
                IL_0010:  ldloca.s   V_2
                IL_0012:  call       "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()"
                IL_0017:  stloc.1
                IL_0018:  ldloca.s   V_1
                IL_001a:  call       "bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get"
                IL_001f:  brtrue.s   IL_005d
                IL_0021:  ldarg.0
                IL_0022:  ldc.i4.0
                IL_0023:  dup
                IL_0024:  stloc.0
                IL_0025:  stfld      "int Program.<>c.<<<Main>$>b__0_0>d.<>1__state"
                IL_002a:  ldarg.0
                IL_002b:  ldloc.1
                IL_002c:  stfld      "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<>c.<<<Main>$>b__0_0>d.<>u__1"
                IL_0031:  ldarg.0
                IL_0032:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<>c.<<<Main>$>b__0_0>d.<>t__builder"
                IL_0037:  ldloca.s   V_1
                IL_0039:  ldarg.0
                IL_003a:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<>c.<<<Main>$>b__0_0>d>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<>c.<<<Main>$>b__0_0>d)"
                IL_003f:  leave.s    IL_00b5
                IL_0041:  ldarg.0
                IL_0042:  ldfld      "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<>c.<<<Main>$>b__0_0>d.<>u__1"
                IL_0047:  stloc.1
                IL_0048:  ldarg.0
                IL_0049:  ldflda     "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<>c.<<<Main>$>b__0_0>d.<>u__1"
                IL_004e:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
                IL_0054:  ldarg.0
                IL_0055:  ldc.i4.m1
                IL_0056:  dup
                IL_0057:  stloc.0
                IL_0058:  stfld      "int Program.<>c.<<<Main>$>b__0_0>d.<>1__state"
                IL_005d:  ldloca.s   V_1
                IL_005f:  call       "void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
                IL_0064:  newobj     "System.Threading.Lock..ctor()"
                IL_0069:  call       "System.Threading.Lock.Scope System.Threading.Lock.EnterScope()"
                IL_006e:  stloc.3
                .try
                {
                  IL_006f:  ldstr      "L"
                  IL_0074:  call       "void System.Console.Write(string)"
                  IL_0079:  leave.s    IL_0087
                }
                finally
                {
                  IL_007b:  ldloc.0
                  IL_007c:  ldc.i4.0
                  IL_007d:  bge.s      IL_0086
                  IL_007f:  ldloca.s   V_3
                  IL_0081:  call       "void System.Threading.Lock.Scope.Dispose()"
                  IL_0086:  endfinally
                }
                IL_0087:  leave.s    IL_00a2
              }
              catch System.Exception
              {
                IL_0089:  stloc.s    V_4
                IL_008b:  ldarg.0
                IL_008c:  ldc.i4.s   -2
                IL_008e:  stfld      "int Program.<>c.<<<Main>$>b__0_0>d.<>1__state"
                IL_0093:  ldarg.0
                IL_0094:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<>c.<<<Main>$>b__0_0>d.<>t__builder"
                IL_0099:  ldloc.s    V_4
                IL_009b:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
                IL_00a0:  leave.s    IL_00b5
              }
              IL_00a2:  ldarg.0
              IL_00a3:  ldc.i4.s   -2
              IL_00a5:  stfld      "int Program.<>c.<<<Main>$>b__0_0>d.<>1__state"
              IL_00aa:  ldarg.0
              IL_00ab:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<>c.<<<Main>$>b__0_0>d.<>t__builder"
              IL_00b0:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
              IL_00b5:  ret
            }
            """);
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

    [Fact]
    public void Yield_Async()
    {
        var source = """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            class C
            {
                async IAsyncEnumerable<int> M()
                {
                    yield return 1;
                    lock (new Lock())
                    {
                        yield return 2;
                    }
                    await Task.Yield();
                    yield return 3;
                }
            }
            """;
        CreateCompilationWithTasksExtensions([source, LockTypeDefinition, AsyncStreamsTypes]).VerifyEmitDiagnostics(
            // (10,15): error CS4013: Instance of type 'Lock.Scope' cannot be used inside a nested function, query expression, iterator block or async method
            //         lock (new Lock())
            Diagnostic(ErrorCode.ERR_SpecialByRefInLambda, "new Lock()").WithArguments("System.Threading.Lock.Scope").WithLocation(10, 15));
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
            // 0.cs(6,13): warning CS9216: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement.
            // object  o = l;
            Diagnostic(ErrorCode.WRN_ConvertingLock, "l").WithLocation(6, 13),
            // 0.cs(9,16): warning CS9216: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement.
            // lock ((object )l) { Console.Write("2"); }
            Diagnostic(ErrorCode.WRN_ConvertingLock, "l").WithLocation(9, 16),
            // 0.cs(11,7): warning CS9216: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement.
            // lock (l as object ) { Console.Write("3"); }
            Diagnostic(ErrorCode.WRN_ConvertingLock, "l").WithLocation(11, 7),
            // 0.cs(13,5): warning CS9216: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement.
            // o = l as object ;
            Diagnostic(ErrorCode.WRN_ConvertingLock, "l").WithLocation(13, 5));
    }

    [Fact]
    public void CastToSelf()
    {
        var source = """
            using System;
            using System.Threading;

            Lock l = new();

            Lock o = l;
            lock (o) { Console.Write("1"); }

            lock ((Lock)l) { Console.Write("2"); }

            lock (l as Lock) { Console.Write("3"); }

            o = l as Lock;
            lock (o) { Console.Write("4"); }

            static Lock Cast1<T>(T t) => (Lock)(object)t;
            lock (Cast1(l)) { Console.Write("5"); }

            static Lock Cast2<T>(T t) where T : class => (Lock)(object)t;
            lock (Cast2(l)) { Console.Write("6"); }

            static Lock Cast3<T>(T t) where T : Lock => t;
            lock (Cast3(l)) { Console.Write("7"); }
            """;
        var verifier = CompileAndVerify([source, LockTypeDefinition], verify: Verification.FailsILVerify,
           expectedOutput: "E1DE2DE3DE4DE5DE6DE7D");
        verifier.VerifyDiagnostics();
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
            // 0.cs(7,22): warning CS9216: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement.
            // var array2 = new[] { new Lock(), new object() };
            Diagnostic(ErrorCode.WRN_ConvertingLock, "new Lock()").WithLocation(7, 22));
    }

    [Theory, CombinatorialData]
    public void CastToBase([CombinatorialValues("interface", "class")] string baseKind)
    {
        var source = $$"""
            using System;
            using System.Threading;

            static class Program
            {
                static void Main()
                {
                    M1();
                    M2();
                }

                static void M1()
                {
                    ILock l1 = new Lock();
                    lock (l1) { Console.Write("1"); }
                }

                static void M2()
                {
                    ILock l2 = new Lock();
                    lock ((Lock)l2) { Console.Write("2"); }
                }
            }

            namespace System.Threading
            {
                public {{baseKind}} ILock { }

                public class Lock : ILock
                {
                    public Scope EnterScope()
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
            // (14,20): warning CS9216: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement.
            //         ILock l1 = new Lock();
            Diagnostic(ErrorCode.WRN_ConvertingLock, "new Lock()").WithLocation(14, 20),
            // (20,20): warning CS9216: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement.
            //         ILock l2 = new Lock();
            Diagnostic(ErrorCode.WRN_ConvertingLock, "new Lock()").WithLocation(20, 20));
        // Should use Monitor locking.
        verifier.VerifyIL("Program.M1", """
            {
              // Code size       39 (0x27)
              .maxstack  2
              .locals init (System.Threading.ILock V_0,
                            bool V_1)
              IL_0000:  newobj     "System.Threading.Lock..ctor()"
              IL_0005:  stloc.0
              IL_0006:  ldc.i4.0
              IL_0007:  stloc.1
              .try
              {
                IL_0008:  ldloc.0
                IL_0009:  ldloca.s   V_1
                IL_000b:  call       "void System.Threading.Monitor.Enter(object, ref bool)"
                IL_0010:  ldstr      "1"
                IL_0015:  call       "void System.Console.Write(string)"
                IL_001a:  leave.s    IL_0026
              }
              finally
              {
                IL_001c:  ldloc.1
                IL_001d:  brfalse.s  IL_0025
                IL_001f:  ldloc.0
                IL_0020:  call       "void System.Threading.Monitor.Exit(object)"
                IL_0025:  endfinally
              }
              IL_0026:  ret
            }
            """);
    }

    [Fact]
    public void DerivedLock()
    {
        var source = """
            using System;
            using System.Threading;

            static class Program
            {
                static void Main()
                {
                    var l1 = M1();
                    var l2 = M2(l1);
                    M3(l2);
                    M4(l2);
                }

                static DerivedLock M1()
                {
                    DerivedLock l1 = new DerivedLock();
                    lock (l1) { Console.Write("1"); }
                    return l1;
                }

                static Lock M2(DerivedLock l1)
                {
                    Lock l2 = l1;
                    lock (l2) { Console.Write("2"); }
                    return l2;
                }

                static void M3(Lock l2)
                {
                    DerivedLock l3 = (DerivedLock)l2;
                    lock (l3) { Console.Write("3"); }
                }
            
                static void M4(Lock l2)
                {
                    IDerivedLock l4 = (IDerivedLock)l2;
                    lock (l4) { Console.Write("4"); }
                }
            }

            namespace System.Threading
            {
                public class Lock
                {
                    public Scope EnterScope()
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

                public class DerivedLock : Lock, IDerivedLock { }

                public interface IDerivedLock { }
            }
            """;
        var verifier = CompileAndVerify(source, verify: Verification.FailsILVerify,
           expectedOutput: "1E2D34");
        verifier.VerifyDiagnostics(
            // (30,39): warning CS9216: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement.
            //         DerivedLock l3 = (DerivedLock)l2;
            Diagnostic(ErrorCode.WRN_ConvertingLock, "l2").WithLocation(30, 39),
            // (36,41): warning CS9216: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement.
            //         IDerivedLock l4 = (IDerivedLock)l2;
            Diagnostic(ErrorCode.WRN_ConvertingLock, "l2").WithLocation(36, 41));
        // Should use Monitor locking.
        verifier.VerifyIL("Program.M1", """
            {
              // Code size       42 (0x2a)
              .maxstack  2
              .locals init (System.Threading.DerivedLock V_0, //l1
                            System.Threading.DerivedLock V_1,
                            bool V_2)
              IL_0000:  newobj     "System.Threading.DerivedLock..ctor()"
              IL_0005:  stloc.0
              IL_0006:  ldloc.0
              IL_0007:  stloc.1
              IL_0008:  ldc.i4.0
              IL_0009:  stloc.2
              .try
              {
                IL_000a:  ldloc.1
                IL_000b:  ldloca.s   V_2
                IL_000d:  call       "void System.Threading.Monitor.Enter(object, ref bool)"
                IL_0012:  ldstr      "1"
                IL_0017:  call       "void System.Console.Write(string)"
                IL_001c:  leave.s    IL_0028
              }
              finally
              {
                IL_001e:  ldloc.2
                IL_001f:  brfalse.s  IL_0027
                IL_0021:  ldloc.1
                IL_0022:  call       "void System.Threading.Monitor.Exit(object)"
                IL_0027:  endfinally
              }
              IL_0028:  ldloc.0
              IL_0029:  ret
            }
            """);
        // Should use Monitor locking.
        verifier.VerifyIL("Program.M3", """
            {
              // Code size       40 (0x28)
              .maxstack  2
              .locals init (System.Threading.DerivedLock V_0,
                            bool V_1)
              IL_0000:  ldarg.0
              IL_0001:  castclass  "System.Threading.DerivedLock"
              IL_0006:  stloc.0
              IL_0007:  ldc.i4.0
              IL_0008:  stloc.1
              .try
              {
                IL_0009:  ldloc.0
                IL_000a:  ldloca.s   V_1
                IL_000c:  call       "void System.Threading.Monitor.Enter(object, ref bool)"
                IL_0011:  ldstr      "3"
                IL_0016:  call       "void System.Console.Write(string)"
                IL_001b:  leave.s    IL_0027
              }
              finally
              {
                IL_001d:  ldloc.1
                IL_001e:  brfalse.s  IL_0026
                IL_0020:  ldloc.0
                IL_0021:  call       "void System.Threading.Monitor.Exit(object)"
                IL_0026:  endfinally
              }
              IL_0027:  ret
            }
            """);
        // Should use Monitor locking.
        verifier.VerifyIL("Program.M4", """
            {
              // Code size       40 (0x28)
              .maxstack  2
              .locals init (System.Threading.IDerivedLock V_0,
                            bool V_1)
              IL_0000:  ldarg.0
              IL_0001:  castclass  "System.Threading.IDerivedLock"
              IL_0006:  stloc.0
              IL_0007:  ldc.i4.0
              IL_0008:  stloc.1
              .try
              {
                IL_0009:  ldloc.0
                IL_000a:  ldloca.s   V_1
                IL_000c:  call       "void System.Threading.Monitor.Enter(object, ref bool)"
                IL_0011:  ldstr      "4"
                IL_0016:  call       "void System.Console.Write(string)"
                IL_001b:  leave.s    IL_0027
              }
              finally
              {
                IL_001d:  ldloc.1
                IL_001e:  brfalse.s  IL_0026
                IL_0020:  ldloc.0
                IL_0021:  call       "void System.Threading.Monitor.Exit(object)"
                IL_0026:  endfinally
              }
              IL_0027:  ret
            }
            """);
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
            // 0.cs(4,12): warning CS9216: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement.
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
        CreateCompilation([source, LockTypeDefinition]).VerifyDiagnostics(
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

            C.M(new Lock());

            static class C
            {
                public static void M<T>(T t) {{constraint}}
                {
                    lock (t) { Console.Write("L"); }
                }
            }
            """;
        var verifier = CompileAndVerify([source, LockTypeDefinition], verify: Verification.FailsILVerify,
           expectedOutput: "L");
        verifier.VerifyDiagnostics();
        // Should use Monitor locking.
        verifier.VerifyIL("C.M<T>", """
            {
              // Code size       40 (0x28)
              .maxstack  2
              .locals init (object V_0,
                            bool V_1)
              IL_0000:  ldarg.0
              IL_0001:  box        "T"
              IL_0006:  stloc.0
              IL_0007:  ldc.i4.0
              IL_0008:  stloc.1
              .try
              {
                IL_0009:  ldloc.0
                IL_000a:  ldloca.s   V_1
                IL_000c:  call       "void System.Threading.Monitor.Enter(object, ref bool)"
                IL_0011:  ldstr      "L"
                IL_0016:  call       "void System.Console.Write(string)"
                IL_001b:  leave.s    IL_0027
              }
              finally
              {
                IL_001d:  ldloc.1
                IL_001e:  brfalse.s  IL_0026
                IL_0020:  ldloc.0
                IL_0021:  call       "void System.Threading.Monitor.Exit(object)"
                IL_0026:  endfinally
              }
              IL_0027:  ret
            }
            """);
    }

    [Fact]
    public void GenericParameter_Object()
    {
        var source = """
            using System;
            using System.Threading;

            C.M<object>(new Lock());

            static class C
            {
                public static void M<T>(T t)
                {
                    lock (t) { Console.Write("L"); }
                }
            }
            """;
        var verifier = CompileAndVerify([source, LockTypeDefinition], verify: Verification.FailsILVerify,
           expectedOutput: "L");
        verifier.VerifyDiagnostics(
            // (4,13): warning CS9216: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement.
            // C.M<object>(new Lock());
            Diagnostic(ErrorCode.WRN_ConvertingLock, "new Lock()").WithLocation(4, 13));
        // Should use Monitor locking.
        verifier.VerifyIL("C.M<T>", """
            {
              // Code size       40 (0x28)
              .maxstack  2
              .locals init (object V_0,
                            bool V_1)
              IL_0000:  ldarg.0
              IL_0001:  box        "T"
              IL_0006:  stloc.0
              IL_0007:  ldc.i4.0
              IL_0008:  stloc.1
              .try
              {
                IL_0009:  ldloc.0
                IL_000a:  ldloca.s   V_1
                IL_000c:  call       "void System.Threading.Monitor.Enter(object, ref bool)"
                IL_0011:  ldstr      "L"
                IL_0016:  call       "void System.Console.Write(string)"
                IL_001b:  leave.s    IL_0027
              }
              finally
              {
                IL_001d:  ldloc.1
                IL_001e:  brfalse.s  IL_0026
                IL_0020:  ldloc.0
                IL_0021:  call       "void System.Threading.Monitor.Exit(object)"
                IL_0026:  endfinally
              }
              IL_0027:  ret
            }
            """);
    }

    [Fact]
    public void UseSiteError_EnterScope()
    {
        // namespace System.Threading
        // {
        //     public class Lock
        //     {
        //         [System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute("Test")]
        //         public Scope EnterScope() => throw null;
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

                .method public hidebysig instance class System.Threading.Lock/Scope EnterScope () cil managed
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
        CreateCompilationWithIL(source, ilSource).VerifyDiagnostics(
            // (5,15): error CS9041: 'Lock.EnterScope()' requires compiler feature 'Test', which is not supported by this version of the C# compiler.
            //         lock (l) { }
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "l").WithArguments("System.Threading.Lock.EnterScope()", "Test").WithLocation(5, 15));
    }

    [Fact]
    public void UseSiteError_Scope()
    {
        // namespace System.Threading
        // {
        //     public class Lock
        //     {
        //         public Scope EnterScope() => throw null;
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

                .method public hidebysig instance class System.Threading.Lock/Scope EnterScope () cil managed
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
        CreateCompilationWithIL(source, ilSource).VerifyDiagnostics(
            // (5,15): error CS9041: 'Lock.Scope' requires compiler feature 'Test', which is not supported by this version of the C# compiler.
            //         lock (l) { }
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "l").WithArguments("System.Threading.Lock.Scope", "Test").WithLocation(5, 15));
    }

    [Fact]
    public void UseSiteError_Dispose()
    {
        // namespace System.Threading
        // {
        //     public class Lock
        //     {
        //         public Scope EnterScope() => throw null;
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

                .method public hidebysig instance class System.Threading.Lock/Scope EnterScope () cil managed
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
        CreateCompilationWithIL(source, ilSource).VerifyDiagnostics(
            // (5,15): error CS9041: 'Lock.Scope.Dispose()' requires compiler feature 'Test', which is not supported by this version of the C# compiler.
            //         lock (l) { }
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "l").WithArguments("System.Threading.Lock.Scope.Dispose()", "Test").WithLocation(5, 15));
    }
}
