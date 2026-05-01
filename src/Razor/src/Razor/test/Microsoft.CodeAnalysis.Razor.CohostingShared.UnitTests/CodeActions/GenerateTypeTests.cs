// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.CodeActions;

public class GenerateTypeTests(ITestOutputHelper testOutputHelper) : CohostCodeActionsEndpointTestBase(testOutputHelper)
{
    [Fact]
    public Task GenerateType_FromCodeBlock_ExistingCodeBlock()
        => VerifyCodeActionAsync(
            input: """
                @code
                {
                    private object M()
                    {
                        return new [||]MissingType();
                    }
                }
                """,
            expected: """
                @code
                {
                    private object M()
                    {
                        return new MissingType();
                    }

                    private class MissingType
                    {
                        public MissingType()
                        {
                        }
                    }
                }
                """,
            codeActionName: RazorPredefinedCodeFixProviderNames.GenerateType,
            codeActionIndex: 1,
            makeDiagnosticsRequest: true);

    [Fact]
    public Task GenerateType_FromCodeBlock_ExistingCodeBlock_Struct()
        => VerifyCodeActionAsync(
            input: """
                @code
                {
                    private static void M<T>() where T : struct
                    {
                        M<[||]MissingType>();
                    }
                }
                """,
            expected: """
                @code
                {
                    private static void M<T>() where T : struct
                    {
                        M<MissingType>();
                    }

                    private struct MissingType
                    {
                    }
                }
                """,
            codeActionName: RazorPredefinedCodeFixProviderNames.GenerateType,
            codeActionIndex: 1,
            makeDiagnosticsRequest: true);

    [Fact]
    public Task GenerateType_FromCodeBlock_ExistingCodeBlock_Interface()
        => VerifyCodeActionAsync(
            input: """
                @code
                {
                    private class C : [||]IMissingType
                    {
                    }
                }
                """,
            expected: """
                @code
                {
                    private class C : IMissingType
                    {
                    }

                    private interface IMissingType
                    {
                    }
                }
                """,
            codeActionName: RazorPredefinedCodeFixProviderNames.GenerateType,
            codeActionIndex: 1,
            makeDiagnosticsRequest: true);

    [Fact]
    public Task GenerateType_FromCodeBlock_InNewFile()
        => VerifyCodeActionAsync(
            input: """
                @code
                {
                    private object M()
                    {
                        return new [||]MissingType();
                    }
                }
                """,
            expected: """
                @code
                {
                    private object M()
                    {
                        return new MissingType();
                    }
                }
                """,
            additionalExpectedFiles: [
                (FileUri("MissingType.cs"), """
                    namespace SomeProject
                    {
                        internal class MissingType
                        {
                            public MissingType()
                            {
                            }
                        }
                    }
                    """)],
            codeActionName: RazorPredefinedCodeFixProviderNames.GenerateType,
            codeActionIndex: 0,
            makeDiagnosticsRequest: true);

    [Fact]
    public Task GenerateType_WithoutCodeBlock()
        => VerifyCodeActionAsync(
            input: """
                @{
                    var item = new [||]MissingType();
                }
                """,
            expected: """
                @{
                    var item = new MissingType();
                }
                @code {
                    private class MissingType
                    {
                        public MissingType()
                        {
                        }
                    }
                }
                """,
            codeActionName: RazorPredefinedCodeFixProviderNames.GenerateType,
            codeActionIndex: 1,
            makeDiagnosticsRequest: true);

    [Fact]
    public async Task GenerateType_WithoutCodeBlock_CodeBlockBraceOnNextLine()
    {
        ClientSettingsManager.Update(ClientSettingsManager.GetClientSettings().AdvancedSettings with { CodeBlockBraceOnNextLine = true });

        await VerifyCodeActionAsync(
            input: """
                @{
                    var item = new [||]MissingType();
                }
                """,
            expected: """
                @{
                    var item = new MissingType();
                }
                @code
                {
                    private class MissingType
                    {
                        public MissingType()
                        {
                        }
                    }
                }
                """,
            codeActionName: RazorPredefinedCodeFixProviderNames.GenerateType,
            codeActionIndex: 1,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public Task GenerateType_Legacy_WithoutFunctionsBlock()
        => VerifyCodeActionAsync(
            input: """
                @{
                    var item = new [||]MissingType();
                }
                """,
            expected: """
                @{
                    var item = new MissingType();
                }
                @functions {
                    private class MissingType
                    {
                        public MissingType()
                        {
                        }
                    }
                }
                """,
            codeActionName: RazorPredefinedCodeFixProviderNames.GenerateType,
            codeActionIndex: 1,
            fileKind: RazorFileKind.Legacy,
            makeDiagnosticsRequest: true);
}
