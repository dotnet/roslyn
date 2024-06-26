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

[Trait(Traits.Feature, Traits.Features.CodeActionsUsePrimaryConstructor)]
public sealed class UseSystemThreadingLockTests
{
    private const string s_systemThreadingLockType = """

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
                """ + s_systemThreadingLockType,
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
                """ + s_systemThreadingLockType,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }
}
