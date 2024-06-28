// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.UseSystemThreadingLock;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.UnitTests.UsePrimaryConstructor;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpUseSystemThreadingLockDiagnosticAnalyzer,
    CSharpUseSystemThreadingLockCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseSystemThreadingLock)]
public sealed class UseSystemThreadingLockTests
{
    private const string SystemThreadingLockType = """

        namespace System.Threading
        {
            public sealed class Lock
            {
                public Lock() { }

                public bool IsHeldByCurrentThread { get; }

                public void Enter() { }
                public Scope EnterScope() => default;
                public void Exit() { }
                public bool TryEnter() => true;
                public bool TryEnter(int millisecondsTimeout) => true;
                public bool TryEnter(TimeSpan timeout) => true;

                public ref struct Scope
                {
                    public void Dispose() { }
                }
            }
        }
        """;

    [Fact]
    public async Task TestInCSharp13()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private object [|_gate|] = new object();

                    void M()
                    {
                        lock (_gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            FixedCode = """
                using System.Threading;

                class C
                {
                    private Lock _gate = new Lock();
                
                    void M()
                    {
                        lock (_gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithDocCommentReference()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private object [|_gate|] = new object();

                    /// Uses <see cref="_gate"/>
                    void M()
                    {
                        lock (_gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            FixedCode = """
                using System.Threading;

                class C
                {
                    private Lock _gate = new Lock();
                
                    /// Uses <see cref="_gate"/>
                    void M()
                    {
                        lock (_gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithFullType1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private System.Object [|_gate|] = new System.Object();

                    void M()
                    {
                        lock (_gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            FixedCode = """
                using System.Threading;

                class C
                {
                    private Lock _gate = new Lock();
                
                    void M()
                    {
                        lock (_gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithFullType2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    private Object [|_gate|] = new Object();

                    void M()
                    {
                        lock (_gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            FixedCode = """
                using System;
                using System.Threading;

                class C
                {
                    private Lock _gate = new Lock();
                
                    void M()
                    {
                        lock (_gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithParenthesizedInitializer()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private object _gate = (new object());

                    void M()
                    {
                        lock (_gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotInCSharp12()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private object _gate = new object();

                    void M()
                    {
                        lock (_gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithInnerYield()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    private object _gate = new object();

                    IEnumerable<int> M()
                    {
                        lock (_gate)
                        {
                            yield return 0;
                        }
                    }
                }
                """ + SystemThreadingLockType,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithYieldOutsideLock()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    private object [|_gate|] = new object();

                    IEnumerable<int> M()
                    {
                        lock (_gate)
                        {
                        }

                        yield return 0;
                    }
                }
                """ + SystemThreadingLockType,
            FixedCode = """
                using System.Collections.Generic;
                using System.Threading;

                class C
                {
                    private Lock _gate = new Lock();

                    IEnumerable<int> M()
                    {
                        lock (_gate)
                        {
                        }

                        yield return 0;
                    }
                }
                """ + SystemThreadingLockType,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithYieldInsideLocalFunctionInsideLock()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    private object [|_gate|] = new object();

                    void M()
                    {
                        lock (_gate)
                        {
                            var v = LocalFunction();

                            IEnumerable<int> LocalFunction()
                            {
                                yield return 0;
                            }
                        }
                    }
                }
                """ + SystemThreadingLockType,
            FixedCode = """
                using System.Collections.Generic;
                using System.Threading;

                class C
                {
                    private Lock _gate = new Lock();
                
                    void M()
                    {
                        lock (_gate)
                        {
                            var v = LocalFunction();
                
                            IEnumerable<int> LocalFunction()
                            {
                                yield return 0;
                            }
                        }
                    }
                }
                """ + SystemThreadingLockType,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithoutLock()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private object _gate = new object();

                    void M()
                    {
                    }
                }
                """ + SystemThreadingLockType,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithUnsupportedOperation1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private object _gate = new object();

                    void M()
                    {
                        lock (_gate)
                        {
                        }

                        Goo(_gate);
                    }

                    void Goo(object o) { }
                }
                """ + SystemThreadingLockType,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithUnsupportedOperation2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private object _gate = new object();

                    void M()
                    {
                        lock (_gate)
                        {
                        }

                        var v = _gate.GetType();
                    }
                }
                """ + SystemThreadingLockType,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithoutSystemThreadingLock()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private object _gate = new object();

                    void M()
                    {
                        lock (_gate)
                        {
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithPublicGate()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    public object _gate = new object();

                    void M()
                    {
                        lock (_gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithNonObjectInitializerValue1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    public object _gate = "";

                    void M()
                    {
                        lock (_gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithNonObjectInitializerValue_InField()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    public object _gate = new int();

                    void M()
                    {
                        lock (_gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithNonObjectInitializerValue_InConstructor()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    public object _gate;

                    public C()
                    {
                        _gate = new int();
                    }

                    void M()
                    {
                        lock (_gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithNonObjectAssignment()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    public object _gate;

                    public C()
                    {
                        _gate = "";
                    }

                    void M()
                    {
                        lock (_gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithMultipleDeclarators()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    public object _gate1, _gate2;

                    void M()
                    {
                        lock (_gate1)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithNonObjectType()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    public string _gate;

                    void M()
                    {
                        lock (_gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithinStruct1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                struct C
                {
                    private object [|_gate|] = new object();

                    public C()
                    {
                    }

                    void M()
                    {
                        lock (_gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            FixedCode = """
                using System.Threading;

                struct C
                {
                    private Lock _gate = new Lock();
                
                    public C()
                    {
                    }
                
                    void M()
                    {
                        lock (_gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithinStruct2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                struct C
                {
                    private object [|_gate|];

                    public C()
                    {
                        _gate = new object();
                    }

                    void M()
                    {
                        lock (_gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            FixedCode = """
                using System.Threading;

                struct C
                {
                    private Lock _gate;
                
                    public C()
                    {
                        _gate = new Lock();
                    }
                
                    void M()
                    {
                        lock (_gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithMemberAccess1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private object [|_gate|] = new object();

                    void M()
                    {
                        lock (this._gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            FixedCode = """
                using System.Threading;

                class C
                {
                    private Lock _gate = new Lock();
                
                    void M()
                    {
                        lock (this._gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithMemberAccess2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private object [|_gate|] = new object();

                    void M(C c)
                    {
                        lock (c._gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            FixedCode = """
                using System.Threading;

                class C
                {
                    private Lock _gate = new Lock();
                
                    void M(C c)
                    {
                        lock (c._gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithGenericMemberAccess()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C<T>
                {
                    private object [|_gate|] = new object();

                    void M(C<int> c)
                    {
                        lock (c._gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            FixedCode = """
                using System.Threading;

                class C<T>
                {
                    private Lock _gate = new Lock();
                
                    void M(C<int> c)
                    {
                        lock (c._gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithImplicitObjectCreation_InField()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private object [|_gate|] = new();

                    void M()
                    {
                        lock (_gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            FixedCode = """
                using System.Threading;

                class C
                {
                    private Lock _gate = new();
                
                    void M()
                    {
                        lock (_gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithImplicitObjectCreation_InConstructor()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                struct C
                {
                    private object [|_gate|];

                    public C()
                    {
                        _gate = new();
                    }

                    void M()
                    {
                        lock (_gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            FixedCode = """
                using System.Threading;

                struct C
                {
                    private Lock _gate;
                
                    public C()
                    {
                        _gate = new();
                    }
                
                    void M()
                    {
                        lock (_gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithObjectCreation_InConstructor1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private object [|_gate|];

                    public C()
                    {
                        _gate = new object();
                    }

                    void M()
                    {
                        lock (_gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            FixedCode = """
                using System.Threading;

                class C
                {
                    private Lock _gate;
                
                    public C()
                    {
                        _gate = new Lock();
                    }
                
                    void M()
                    {
                        lock (_gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithObjectCreation_InConstructor2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private object [|_gate|];

                    public C()
                    {
                        this._gate = new object();
                    }

                    void M()
                    {
                        lock (_gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            FixedCode = """
                using System.Threading;

                class C
                {
                    private Lock _gate;
                
                    public C()
                    {
                        this._gate = new Lock();
                    }
                
                    void M()
                    {
                        lock (_gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithObjectCreation_InConstructor3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private object _gate;

                    public C()
                    {
                        this._gate = (new object());
                    }

                    void M()
                    {
                        lock (_gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithNameOf()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private object [|_gate|] = new object();
                    private string s = nameof(_gate);

                    void M()
                    {
                        lock (_gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            FixedCode = """
                using System.Threading;

                class C
                {
                    private Lock _gate = new Lock();
                    private string s = nameof(_gate);
                
                    void M()
                    {
                        lock (_gate)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithMultipleFieldsNoneLocked()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private object _gate1 = new object();
                    private object _gate2 = new object();

                    void M()
                    {
                    }
                }
                """ + SystemThreadingLockType,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithMultipleFieldsOneLocked()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private object [|_gate1|] = new object();
                    private object _gate2 = new object();

                    void M()
                    {
                        lock (_gate1)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            FixedCode = """
                using System.Threading;

                class C
                {
                    private Lock _gate1 = new Lock();
                    private object _gate2 = new object();
                
                    void M()
                    {
                        lock (_gate1)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithMultipleFieldsBothLocked()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private object [|_gate1|] = new object();
                    private object [|_gate2|] = new object();

                    void M()
                    {
                        lock (_gate1)
                        {
                        }
                        lock (_gate2)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            FixedCode = """
                using System.Threading;

                class C
                {
                    private Lock _gate1 = new Lock();
                    private Lock _gate2 = new Lock();
                
                    void M()
                    {
                        lock (_gate1)
                        {
                        }
                        lock (_gate2)
                        {
                        }
                    }
                }
                """ + SystemThreadingLockType,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }
}
