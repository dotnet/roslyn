// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.AddImport;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.CodeActions
{
    public class CodeActionsTests : AbstractLanguageServerProtocolTests
    {
        public CodeActionsTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [WpfFact]
        public async Task TestCodeActionHandlerAsync()
        {
            var markup =
@"class A
{
    void M()
    {
        {|caret:|}int i = 1;
    }
}";
            await using var testLspServer = await CreateTestLspServerAsync(markup);

            var caretLocation = testLspServer.GetLocations("caret").Single();
            var expected = CreateCodeAction(
                title: CSharpAnalyzersResources.Use_implicit_type,
                kind: CodeActionKind.Refactor,
                children: Array.Empty<LSP.VSInternalCodeAction>(),
                data: CreateCodeActionResolveData(
                    CSharpAnalyzersResources.Use_implicit_type,
                    caretLocation,
                    customTags: new[] { PredefinedCodeRefactoringProviderNames.UseImplicitType }),
                priority: VSInternalPriorityLevel.Low,
                groupName: "Roslyn1",
                applicableRange: new LSP.Range { Start = new Position { Line = 4, Character = 8 }, End = new Position { Line = 4, Character = 11 } },
                diagnostics: null);

            var results = await RunGetCodeActionsAsync(testLspServer, CreateCodeActionParams(caretLocation, allowInHiddenCode: false));
            var useImplicitType = results.FirstOrDefault(r => r.Title == CSharpAnalyzersResources.Use_implicit_type);

            AssertJsonEquals(expected, useImplicitType);
        }

        [WpfFact]
        public async Task TestCodeActionHandlerAsync_NestedAction()
        {
            var markup =
@"class A
{
    void M()
    {
        int {|caret:|}i = 1;
    }
}";
            await using var testLspServer = await CreateTestLspServerAsync(markup, CapabilitiesWithVSExtensions);

            var caretLocation = testLspServer.GetLocations("caret").Single();
            var expected = CreateCodeAction(
                title: string.Format(FeaturesResources.Introduce_constant_for_0, "1"),
                kind: CodeActionKind.Refactor,
                children: Array.Empty<LSP.VSInternalCodeAction>(),
                data: CreateCodeActionResolveData(
                    FeaturesResources.Introduce_constant + '|' + string.Format(FeaturesResources.Introduce_constant_for_0, "1"),
                    caretLocation),
                priority: VSInternalPriorityLevel.Normal,
                groupName: "Roslyn3",
                applicableRange: new LSP.Range { Start = new Position { Line = 4, Character = 12 }, End = new Position { Line = 4, Character = 12 } },
                diagnostics: null);

            var results = await RunGetCodeActionsAsync(testLspServer, CreateCodeActionParams(caretLocation, allowInHiddenCode: false));

            var topLevelAction = Assert.Single(results.Where(action => action.Title == FeaturesResources.Introduce_constant));
            var expectedChildActionTitle = FeaturesResources.Introduce_constant + '|' + string.Format(FeaturesResources.Introduce_constant_for_0, "1");
            var introduceConstant = topLevelAction.Children.FirstOrDefault(
                r => ((JObject)r.Data).ToObject<CodeActionResolveData>().UniqueIdentifier == expectedChildActionTitle);

            AssertJsonEquals(expected, introduceConstant);
        }

        [WpfFact]
        public async Task TestCodeActionHasCorrectDiagnostics()
        {
            var markup =
@"class A
{
    void M()
    {
        {|caret:|}Task.Delay(1);
    }
}";
            await using var testLspServer = await CreateTestLspServerAsync(markup);

            var caret = testLspServer.GetLocations("caret").Single();
            var codeActionParams = new CodeActionParamsWithOptions
            {
                TextDocument = CreateTextDocumentIdentifier(caret.Uri),
                Range = caret.Range,
                Context = new LSP.CodeActionContext
                {
                    Diagnostics = new[]
                    {
                        new LSP.Diagnostic
                        {
                            Code = AddImportDiagnosticIds.CS0103
                        },
                        new LSP.Diagnostic
                        {
                            Code = "SomeCode"
                        }
                    }
                }
            };

            var results = await RunGetCodeActionsAsync(testLspServer, codeActionParams);
            var addImport = results.FirstOrDefault(r => r.Title.Contains($"using System.Threading.Tasks"));
            Assert.Equal(1, addImport.Diagnostics.Length);
            Assert.Equal(AddImportDiagnosticIds.CS0103, addImport.Diagnostics.Single().Code.Value);
        }

        [WpfFact]
        public async Task TestCodeActionHandlerAsync_GenerateMethod_AllowInHidden()
        {
            var markup = """
                #line hidden
                class A
                {
                    void M()
                    {
                        {|caret:|}Bar();
                    }
                }
                """;
            await using var testLspServer = await CreateTestLspServerAsync(markup);

            var caretLocation = testLspServer.GetLocations("caret").Single();
            var expected = CreateCodeAction(
                title: string.Format(FeaturesResources.Generate_method_0, "Bar"),
                kind: CodeActionKind.QuickFix,
                children: Array.Empty<LSP.VSInternalCodeAction>(),
                data: CreateCodeActionResolveData(
                    string.Format(FeaturesResources.Generate_method_0, "Bar"),
                    caretLocation,
                    customTags: new[] { PredefinedCodeFixProviderNames.GenerateMethod }),
                priority: VSInternalPriorityLevel.Normal,
                groupName: "Roslyn1",
                applicableRange: new LSP.Range { Start = new Position { Line = 5, Character = 8 }, End = new Position { Line = 5, Character = 11 } },
                diagnostics: null);

            var results = await RunGetCodeActionsAsync(testLspServer, CreateCodeActionParams(caretLocation, allowInHiddenCode: true));
            var generateMethod = results.Single(r => r.Title == expected.Title);

            AssertJsonEquals(expected, generateMethod);
        }

        [WpfFact]
        public async Task TestCodeActionHandlerAsync_GenerateMethod()
        {
            var markup = """
                #line hidden
                class A
                {
                    void M()
                    {
                        {|caret:|}Bar();
                    }
                }
                """;
            await using var testLspServer = await CreateTestLspServerAsync(markup);

            var caretLocation = testLspServer.GetLocations("caret").Single();
            var results = await RunGetCodeActionsAsync(testLspServer, CreateCodeActionParams(caretLocation, allowInHiddenCode: false));
            Assert.False(results.Any(r => r.Title == string.Format(FeaturesResources.Generate_method_0, "Bar")));
        }

        private static async Task<LSP.VSInternalCodeAction[]> RunGetCodeActionsAsync(
            TestLspServer testLspServer,
            CodeActionParamsWithOptions codeActionParams)
        {
            var result = await testLspServer.ExecuteRequestAsync<CodeActionParamsWithOptions, LSP.CodeAction[]>(
                LSP.Methods.TextDocumentCodeActionName, codeActionParams, CancellationToken.None);
            return result.Cast<LSP.VSInternalCodeAction>().ToArray();
        }

        internal static CodeActionParamsWithOptions CreateCodeActionParams(LSP.Location caret, bool allowInHiddenCode)
            => new CodeActionParamsWithOptions
            {
                AllowGenerateInHiddenCode = allowInHiddenCode,
                TextDocument = CreateTextDocumentIdentifier(caret.Uri),
                Range = caret.Range,
                Context = new LSP.CodeActionContext
                {
                    // TODO - Code actions should respect context.
                }
            };

        internal static LSP.VSInternalCodeAction CreateCodeAction(
            string title, LSP.CodeActionKind kind, LSP.VSInternalCodeAction[] children,
            CodeActionResolveData data, LSP.Diagnostic[] diagnostics,
            LSP.VSInternalPriorityLevel? priority, string groupName, LSP.Range applicableRange,
            LSP.WorkspaceEdit edit = null, LSP.Command command = null)
        {
            var action = new LSP.VSInternalCodeAction
            {
                Title = title,
                Kind = kind,
                Children = children,
                Data = JToken.FromObject(data),
                Diagnostics = diagnostics,
                Edit = edit,
                Group = groupName,
                Priority = priority,
                ApplicableRange = applicableRange,
                Command = command
            };

            return action;
        }
    }
}
