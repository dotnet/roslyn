// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.CodeFixes;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.CodeActions;

public class ImplementAbstractClassTests(ITestOutputHelper testOutputHelper) : CohostCodeActionsEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task ImplementAbstractClass_FromInheritsDirective_ExistingCodeBlock()
    {
        await VerifyBaseComponentCodeActionAsync(
            input: """
                @inherits [||]BaseComponent

                @code
                {
                }
                """,
            expected: """
                @inherits BaseComponent

                @code
                {
                    public override void Build()
                    {
                        throw new NotImplementedException();
                    }
                }
                """,
            fileKind: null);
    }

    [Fact]
    public async Task ImplementAbstractClass_FromInheritsDirective_WithoutCodeBlock()
    {
        await VerifyBaseComponentCodeActionAsync(
            input: """
                @inherits [||]BaseComponent

                <div>Hello</div>
                """,
            expected: """
                @inherits BaseComponent

                <div>Hello</div>
                @code {
                    public override void Build()
                    {
                        throw new NotImplementedException();
                    }
                }
                """,
            fileKind: null);
    }

    [Fact]
    public async Task ImplementAbstractClass_FromGenericInheritsDirective_ExistingCodeBlock()
    {
        await VerifyCodeActionAsync(
            input: """
                @inherits [||]GenericBase<int>

                @code
                {
                    private int value = 1;
                }
                """,
            expected: """
                @inherits GenericBase<int>

                @code
                {
                    private int value = 1;

                    protected override int Transform(int value)
                    {
                        throw new NotImplementedException();
                    }
                }
                """,
            additionalFiles:
            [
                (FilePath("GenericBase.cs"), """
                    namespace SomeProject;

                    public abstract class GenericBase<T>
                    {
                        protected abstract T Transform(T value);
                    }
                    """)
            ],
            codeActionName: PredefinedCodeFixProviderNames.ImplementAbstractClass,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task ImplementAbstractClass_FromInheritsDirective_WholeTypeRange_ExistingCodeBlock()
    {
        await VerifyBaseComponentCodeActionAsync(
            input: """
                @inherits [|BaseComponent|]

                @code
                {
                }
                """,
            expected: """
                @inherits BaseComponent

                @code
                {
                    public override void Build()
                    {
                        throw new NotImplementedException();
                    }
                }
                """,
            fileKind: null);
    }

    [Fact]
    public async Task ImplementAbstractClass_FromInheritsDirective_ZeroLengthRangeInMiddle_ExistingCodeBlock()
    {
        await VerifyBaseComponentCodeActionAsync(
            input: """
                @inherits Base$$Component

                @code
                {
                }
                """,
            expected: """
                @inherits BaseComponent

                @code
                {
                    public override void Build()
                    {
                        throw new NotImplementedException();
                    }
                }
                """,
            fileKind: null);
    }

    [Fact]
    public async Task ImplementAbstractClass_FromInheritsDirective_ZeroLengthRangeAtEnd_ExistingCodeBlock()
    {
        await VerifyBaseComponentCodeActionAsync(
            input: """
                @inherits BaseComponent$$

                @code
                {
                }
                """,
            expected: """
                @inherits BaseComponent

                @code
                {
                    public override void Build()
                    {
                        throw new NotImplementedException();
                    }
                }
                """,
            fileKind: null);
    }

    [Fact]
    public async Task ImplementAbstractClass_Legacy_FromInheritsDirective_WithoutFunctionsBlock()
    {
        await VerifyBaseComponentCodeActionAsync(
            input: """
                @using SomeProject
                @inherits Base$$Component

                <div>Hello</div>
                """,
            expected: """
                @using SomeProject
                @inherits BaseComponent

                <div>Hello</div>
                @functions {
                    public override void Build()
                    {
                        throw new NotImplementedException();
                    }
                }
                """,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task ImplementAbstractClass_Legacy_FromInheritsDirective_WithFunctionsBlock()
    {
        await VerifyBaseComponentCodeActionAsync(
            input: """
                @using SomeProject
                @inherits [|BaseComponent|]

                @functions
                {
                }
                """,
            expected: """
                @using SomeProject
                @inherits BaseComponent

                @functions
                {
                    public override void Build()
                    {
                        throw new NotImplementedException();
                    }
                }
                """,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task ImplementAbstractClass_FromInheritsDirective_AllAbstractMemberKinds()
    {
        await VerifyCodeActionAsync(
            input: """
                @inherits [||]AllMembersBase

                @code
                {
                }
                """,
            expected: """
                @using System
                @inherits AllMembersBase

                @code
                {
                    public override void Build()
                    {
                        throw new NotImplementedException();
                    }

                    public override int this[int index] { set => throw new NotImplementedException(); }

                    public override int Value { set => throw new NotImplementedException(); }

                    public override event Action Changed;
                }
                """,
            additionalFiles:
            [
                (FilePath("AllMembersBase.cs"), """
                    namespace SomeProject;

                    using System;

                    public abstract class AllMembersBase
                    {
                        public abstract event Action Changed;

                        public abstract int this[int index] { set; }

                        public abstract void Build();

                        public abstract int Value { set; }
                    }
                    """)
            ],
            codeActionName: PredefinedCodeFixProviderNames.ImplementAbstractClass,
            makeDiagnosticsRequest: true);
    }

    private Task VerifyBaseComponentCodeActionAsync(TestCode input, string expected, RazorFileKind? fileKind)
        => VerifyCodeActionAsync(
            input: input,
            expected: expected,
            additionalFiles:
            [
                (FilePath("BaseComponent.cs"), """
                    namespace SomeProject;

                    public abstract class BaseComponent
                    {
                        public abstract void Build();
                    }
                    """)
            ],
            codeActionName: PredefinedCodeFixProviderNames.ImplementAbstractClass,
            fileKind: fileKind,
            makeDiagnosticsRequest: true);
}
