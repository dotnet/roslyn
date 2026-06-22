// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.CodeFixes;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.CodeActions;

public class ImplementInterfaceTests(ITestOutputHelper testOutputHelper) : CohostCodeActionsEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task ImplementInterface_ExistingCodeBlock()
    {
        await VerifyCodeActionAsync(
            input: """
                @implements IMyInter[||]face

                @code {
                }
                """,
            expected: """
                @implements IMyInterface

                @code {
                    public void M()
                    {
                        throw new NotImplementedException();
                    }
                }
                """,
            additionalFiles:
            [
                (FilePath("IMyInterface.cs"), """
                    public interface IMyInterface
                    {
                        void M();
                    }
                    """)
            ],
            codeActionName: PredefinedCodeFixProviderNames.ImplementInterface,
            codeActionIndex: 0,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task ImplementInterface_WithoutCodeBlock()
    {
        await VerifyCodeActionAsync(
            input: """
                @implements IMyInter[||]face
                """,
            expected: """
                @implements IMyInterface
                @code {
                    public void M()
                    {
                        throw new NotImplementedException();
                    }
                }
                """,
            additionalFiles:
            [
                (FilePath("IMyInterface.cs"), """
                    public interface IMyInterface
                    {
                        void M();
                    }
                    """)
            ],
            codeActionName: PredefinedCodeFixProviderNames.ImplementInterface,
            codeActionIndex: 0,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task ImplementInterface_Legacy_ClassInFunctionsBlock()
    {
        await VerifyCodeActionAsync(
            input: """
                @functions {
                    class C : IMyInter[||]face
                    {
                    }
                }
                """,
            expected: """
                @functions {
                    class C : IMyInterface
                    {
                        public void M()
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
                """,
            additionalFiles:
            [
                (FilePath("IMyInterface.cs"), """
                    public interface IMyInterface
                    {
                        void M();
                    }
                    """)
            ],
            codeActionName: PredefinedCodeFixProviderNames.ImplementInterface,
            codeActionIndex: 0,
            fileKind: RazorFileKind.Legacy,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task ImplementInterface_Legacy_ImplementsDirective_WithFunctionsBlock()
    {
        await VerifyCodeActionAsync(
            input: """
                @implements IMyInter[||]face

                @functions {
                }
                """,
            expected: """
                @implements IMyInterface

                @functions {
                    public void M()
                    {
                        throw new NotImplementedException();
                    }
                }
                """,
            additionalFiles:
            [
                (FilePath("IMyInterface.cs"), """
                    public interface IMyInterface
                    {
                        void M();
                    }
                    """)
            ],
            codeActionName: PredefinedCodeFixProviderNames.ImplementInterface,
            codeActionIndex: 0,
            fileKind: RazorFileKind.Legacy,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task ImplementInterface_Legacy_ImplementsDirective_WithoutFunctionsBlock()
    {
        await VerifyCodeActionAsync(
            input: """
                @implements IMyInter[||]face
                """,
            expected: """
                @implements IMyInterface
                @functions {
                    public void M()
                    {
                        throw new NotImplementedException();
                    }
                }
                """,
            additionalFiles:
            [
                (FilePath("IMyInterface.cs"), """
                    public interface IMyInterface
                    {
                        void M();
                    }
                    """)
            ],
            codeActionName: PredefinedCodeFixProviderNames.ImplementInterface,
            codeActionIndex: 0,
            fileKind: RazorFileKind.Legacy,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task ImplementInterface_Explicitly_ExistingCodeBlock()
    {
        await VerifyCodeActionAsync(
            input: """
                @implements IMyInter[||]face

                @code {
                }
                """,
            expected: """
                @implements IMyInterface

                @code {
                    void IMyInterface.Method1()
                    {
                        throw new NotImplementedException();
                    }
                }
                """,
            additionalFiles:
            [
                (FilePath("IMyInterface.cs"), """
                    public interface IMyInterface
                    {
                        void Method1();
                    }
                    """)
            ],
            codeActionName: PredefinedCodeFixProviderNames.ImplementInterface,
            codeActionIndex: 1,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task ImplementInterface_Explicitly_WithInheritedPropertyEventAndIndexerNameCollisions()
    {
        await VerifyCodeActionAsync(
            input: """
                @using System
                @implements IDeri[||]ved

                @code {
                }
                """,
            expected: """
                @using System
                @implements IDerived

                @code {
                    int IDerived.this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

                    int IBase.this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

                    string IDerived.Property1 { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

                    string IBase.Property1 { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

                    event EventHandler IDerived.Event1
                    {
                        add
                        {
                            throw new NotImplementedException();
                        }

                        remove
                        {
                            throw new NotImplementedException();
                        }
                    }

                    event EventHandler IBase.Event1
                    {
                        add
                        {
                            throw new NotImplementedException();
                        }

                        remove
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
                """,
            additionalFiles:
            [
                (FilePath("IMyInterface.cs"), """
                    using System;

                    public interface IBase
                    {
                        string Property1 { get; set; }
                        event EventHandler Event1;
                        int this[int index] { get; set; }
                    }

                    public interface IDerived : IBase
                    {
                        new string Property1 { get; set; }
                        new event EventHandler Event1;
                        new int this[int index] { get; set; }
                    }
                    """)
            ],
            codeActionName: PredefinedCodeFixProviderNames.ImplementInterface,
            codeActionIndex: 1,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task ImplementInterface_Explicitly_PartialBaseImplementations_AddsDerivedMembers()
    {
        await VerifyCodeActionAsync(
            input: """
                @using System
                @implements IDeri[||]ved

                @code {
                    int IBase.this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

                    string IBase.Property1 { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

                    event EventHandler IBase.Event1
                    {
                        add
                        {
                            throw new NotImplementedException();
                        }

                        remove
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
                """,
            expected: """
                @using System
                @implements IDerived

                @code {
                    int IBase.this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

                    string IBase.Property1 { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
                    string IDerived.Property1 { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

                    int IDerived.this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

                    event EventHandler IBase.Event1
                    {
                        add
                        {
                            throw new NotImplementedException();
                        }

                        remove
                        {
                            throw new NotImplementedException();
                        }
                    }

                    event EventHandler IDerived.Event1
                    {
                        add
                        {
                            throw new NotImplementedException();
                        }

                        remove
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
                """,
            additionalFiles:
            [
                (FilePath("IMyInterface.cs"), """
                    using System;

                    public interface IBase
                    {
                        string Property1 { get; set; }
                        event EventHandler Event1;
                        int this[int index] { get; set; }
                    }

                    public interface IDerived : IBase
                    {
                        new string Property1 { get; set; }
                        new event EventHandler Event1;
                        new int this[int index] { get; set; }
                    }
                    """)
            ],
            codeActionName: PredefinedCodeFixProviderNames.ImplementInterface,
            codeActionIndex: 1,
            makeDiagnosticsRequest: true);
    }

    [ConditionalFact(typeof(IsEnglishLocal))]
    public async Task ImplementInterface_IDisposableDisposePattern()
    {
        await VerifyCodeActionAsync(
            input: """
                @implements IDi[||]sposable

                @code {
                }
                """,
            expected: """
                @implements IDisposable

                @code {
                    private bool disposedValue;

                    protected virtual void Dispose(bool disposing)
                    {
                        if (!disposedValue)
                        {
                            if (disposing)
                            {
                                // TODO: dispose managed state (managed objects)
                            }

                            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                            // TODO: set large fields to null
                            disposedValue = true;
                        }
                    }

                    public void Dispose()
                    {
                        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
                        Dispose(disposing: true);
                        GC.SuppressFinalize(this);
                    }
                }
                """,
            codeActionName: PredefinedCodeFixProviderNames.ImplementInterface,
            codeActionIndex: 1,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task ImplementInterface_PartiallyImplemented_RemainingMethod()
    {
        await VerifyCodeActionAsync(
            input: """
                @implements IMyInter[||]face

                @code {
                    public void Method1()
                    {
                    }
                }
                """,
            expected: """
                @implements IMyInterface

                @code {
                    public void Method1()
                    {
                    }

                    public void Method2()
                    {
                        throw new NotImplementedException();
                    }
                }
                """,
            additionalFiles:
            [
                (FilePath("IMyInterface.cs"), """
                    public interface IMyInterface
                    {
                        void Method1();
                        void Method2();
                    }
                    """)
            ],
            codeActionName: PredefinedCodeFixProviderNames.ImplementInterface,
            codeActionIndex: 0,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task ImplementInterface_PartiallyImplemented_IndexerOverload()
    {
        await VerifyCodeActionAsync(
            input: """
                @using System
                @implements IMyInter[||]face

                @code {
                    public int this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
                }
                """,
            expected: """
                @using System
                @implements IMyInterface

                @code {
                    public int this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

                    public int this[string index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
                }
                """,
            additionalFiles:
            [
                (FilePath("IMyInterface.cs"), """
                    public interface IMyInterface
                    {
                        int this[int index] { get; set; }
                        int this[string index] { get; set; }
                    }
                    """)
            ],
            codeActionName: PredefinedCodeFixProviderNames.ImplementInterface,
            codeActionIndex: 0,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task ImplementInterface_WithMethodPropertyEventAndIndexer()
    {
        await VerifyCodeActionAsync(
            input: """
                @implements IMyInter[||]face

                @code {
                }
                """,
            expected: """
                @using System
                @implements IMyInterface

                @code {
                    public int this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

                    public string Property1 { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

                    public event EventHandler Event1;

                    public void Method1()
                    {
                        throw new NotImplementedException();
                    }
                }
                """,
            additionalFiles:
            [
                (FilePath("IMyInterface.cs"), """
                    using System;

                    public interface IMyInterface
                    {
                        void Method1();
                        string Property1 { get; set; }
                        event EventHandler Event1;
                        int this[int index] { get; set; }
                    }
                    """)
            ],
            codeActionName: PredefinedCodeFixProviderNames.ImplementInterface,
            codeActionIndex: 0,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task ImplementInterface_WithMultipleMembers_WithoutCodeBlock()
    {
        await VerifyCodeActionAsync(
            input: """
                @implements IMyInter[||]face
                """,
            expected: """
                @using System
                @implements IMyInterface
                @code {
                    public int this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

                    public string Property1 { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

                    public event EventHandler Event1;

                    public void Method1()
                    {
                        throw new NotImplementedException();
                    }
                }
                """,
            additionalFiles:
            [
                (FilePath("IMyInterface.cs"), """
                    using System;

                    public interface IMyInterface
                    {
                        void Method1();
                        string Property1 { get; set; }
                        event EventHandler Event1;
                        int this[int index] { get; set; }
                    }
                    """)
            ],
            codeActionName: PredefinedCodeFixProviderNames.ImplementInterface,
            codeActionIndex: 0,
            makeDiagnosticsRequest: true);
    }
}
