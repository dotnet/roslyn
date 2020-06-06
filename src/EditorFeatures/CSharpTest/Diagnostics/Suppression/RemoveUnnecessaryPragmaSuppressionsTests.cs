// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessarySuppressions;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.RemoveUnnecessarySuppressions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnnecessarySuppressions
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessarySuppressions)]
    [WorkItem(44177, "https://github.com/dotnet/roslyn/issues/44177")]
    public abstract class RemoveUnnecessaryPragmaSuppressionsTests : AbstractUnncessarySuppressionDiagnosticTest
    {
        #region Helpers

        internal sealed override CodeFixProvider CodeFixProvider
            => new RemoveUnnecessaryPragmaSuppressionsCodeFixProvider();
        internal sealed override AbstractRemoveUnnecessaryPragmaSuppressionsDiagnosticAnalyzer SuppressionAnalyzer
            => new CSharpRemoveUnnecessaryPragmaSuppressionsDiagnosticAnalyzer();

        protected sealed override ParseOptions GetScriptOptions() => Options.Script;
        protected internal sealed override string GetLanguage() => LanguageNames.CSharp;
        protected sealed override TestWorkspace CreateWorkspaceFromFile(string initialMarkup, TestParameters parameters)
            => TestWorkspace.CreateCSharp(initialMarkup, parameters.parseOptions,
                (parameters.compilationOptions ?? TestOptions.DebugDll).WithReportSuppressedDiagnostics(true));

        protected sealed class UserDiagnosticAnalyzer : DiagnosticAnalyzer
        {
            public static readonly DiagnosticDescriptor Descriptor0168 =
                new DiagnosticDescriptor("Analyzer0168", "Variable is declared but never used", "Message", "Category", DiagnosticSeverity.Warning, isEnabledByDefault: true);
            public static readonly DiagnosticDescriptor Descriptor0219 =
                new DiagnosticDescriptor("Analyzer0219", "Variable is assigned but its value is never used", "Message", "Category", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor0168, Descriptor0219);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterOperationBlockStartAction(
                    context =>
                    {
                        var localsToIsAssignedMap = new ConcurrentDictionary<ILocalSymbol, bool>();
                        var usedLocals = new HashSet<ILocalSymbol>();
                        context.RegisterOperationAction(
                            context =>
                            {
                                var declarator = (IVariableDeclaratorOperation)context.Operation;
                                var hasInitializer = declarator.GetVariableInitializer() != null;
                                localsToIsAssignedMap.GetOrAdd(declarator.Symbol, hasInitializer);
                            }, OperationKind.VariableDeclarator);

                        context.RegisterOperationAction(
                            context =>
                            {
                                var localReference = (ILocalReferenceOperation)context.Operation;
                                if (localReference.Parent is ISimpleAssignmentOperation simpleAssignment &&
                                    simpleAssignment.Target == localReference)
                                {
                                    localsToIsAssignedMap.AddOrUpdate(localReference.Local, true, (_1, _2) => true);
                                }
                                else
                                {
                                    usedLocals.Add(localReference.Local);
                                }
                            }, OperationKind.LocalReference);

                        context.RegisterOperationBlockEndAction(
                            context =>
                            {
                                foreach (var (local, isAssigned) in localsToIsAssignedMap)
                                {
                                    if (usedLocals.Contains(local))
                                    {
                                        continue;
                                    }

                                    var rule = !isAssigned ? Descriptor0168 : Descriptor0219;
                                    context.ReportDiagnostic(Diagnostic.Create(rule, local.Locations[0]));
                                }
                            });
                    });
            }
        }

        protected sealed class CompilationEndDiagnosticAnalyzer : DiagnosticAnalyzer
        {
            public static readonly DiagnosticDescriptor Descriptor =
                new DiagnosticDescriptor("CompilationEndId", "Title", "Message", "Category", DiagnosticSeverity.Warning, isEnabledByDefault: true);
            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);
            public override void Initialize(AnalysisContext context) =>
                context.RegisterCompilationStartAction(context => context.RegisterCompilationEndAction(_ => { }));
        }

        #endregion

        #region Single analyzer tests (Compiler OR Analyzer)

        public abstract class CompilerOrAnalyzerTests : RemoveUnnecessaryPragmaSuppressionsTests
        {
            protected abstract string VariableDeclaredButNotUsedDiagnosticId { get; }
            protected abstract string VariableAssignedButNotUsedDiagnosticId { get; }
            protected abstract ImmutableArray<string> UnsupportedDiagnosticIds { get; }

            public sealed class CompilerTests : CompilerOrAnalyzerTests
            {
                internal override ImmutableArray<DiagnosticAnalyzer> OtherAnalyzers
                    => ImmutableArray.Create<DiagnosticAnalyzer>(new CSharpCompilerDiagnosticAnalyzer());
                protected override string VariableDeclaredButNotUsedDiagnosticId => "CS0168";
                protected override string VariableAssignedButNotUsedDiagnosticId => "CS0219";
                protected override ImmutableArray<string> UnsupportedDiagnosticIds
                {
                    get
                    {
                        var errorCodes = Enum.GetValues(typeof(ErrorCode));
                        var supported = ((CSharpCompilerDiagnosticAnalyzer)OtherAnalyzers[0]).GetSupportedErrorCodes();
                        using var _ = ArrayBuilder<string>.GetInstance(out var builder);
                        foreach (int errorCode in errorCodes)
                        {
                            if (!supported.Contains(errorCode) && errorCode > 0)
                            {
                                // Add all 3 supported formats for suppressions: integer, integer with leading zeros, "CS" prefix
                                builder.Add(errorCode.ToString());
                                builder.Add(errorCode.ToString("D4"));
                                builder.Add("CS" + errorCode.ToString("D4"));
                            }
                        }

                        return builder.ToImmutable();
                    }
                }
            }

            public sealed class AnalyzerTests : CompilerOrAnalyzerTests
            {
                internal override ImmutableArray<DiagnosticAnalyzer> OtherAnalyzers
                    => ImmutableArray.Create<DiagnosticAnalyzer>(new UserDiagnosticAnalyzer(), new CompilationEndDiagnosticAnalyzer());
                protected override string VariableDeclaredButNotUsedDiagnosticId => UserDiagnosticAnalyzer.Descriptor0168.Id;
                protected override string VariableAssignedButNotUsedDiagnosticId => UserDiagnosticAnalyzer.Descriptor0219.Id;
                protected override ImmutableArray<string> UnsupportedDiagnosticIds
                    => ImmutableArray.Create(
                        CompilationEndDiagnosticAnalyzer.Descriptor.Id,
                        IDEDiagnosticIds.RemoveUnnecessarySuppressionDiagnosticId,
                        IDEDiagnosticIds.FormattingDiagnosticId,
                        "format");
            }

            [Fact]
            public async Task TestDoNotRemoveRequiredDiagnosticSuppression()
            {
                await TestMissingInRegularAndScriptAsync(
        $@"
class Class
{{
    void M()
    {{
[|#pragma warning disable {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used - Necessary
        int y;
#pragma warning restore {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used - Necessary|]
    }}
}}");
            }

            [Fact]
            public async Task TestDoNotRemoveRequiredDiagnosticSuppression_02()
            {
                await TestMissingInRegularAndScriptAsync(
        $@"
[|#pragma warning disable {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used - Necessary|]
class Class
{{
    void M()
    {{
        int y;
    }}
}}");
            }

            [Theory, CombinatorialData]
            public async Task TestDoNotRemoveUnsupportedDiagnosticSuppression(bool disable)
            {
                var disableOrRestore = disable ? "disable" : "restore";
                var pragmas = new StringBuilder();
                foreach (var id in UnsupportedDiagnosticIds)
                {
                    pragmas.AppendLine($@"#pragma warning {disableOrRestore} {id}");
                }

                await TestMissingInRegularAndScriptAsync($"[|{pragmas}|]");
            }

            [Fact]
            public async Task TestDoNotRemoveInactiveDiagnosticSuppression()
            {
                await TestMissingInRegularAndScriptAsync(
        $@"
#if false

class Class
{{
    void M()
    {{
[|#pragma warning disable {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used - Inactive
        int y;
#pragma warning restore {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used - Inactive|]
        y = 1;
    }}
}}

#endif");
            }

            [Fact]
            public async Task TestDoNotRemoveDiagnosticSuppressionsInCodeWithSyntaxErrors()
            {
                await TestMissingInRegularAndScriptAsync(
        $@"
class Class
{{
    void M()
    {{
[|#pragma warning disable {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used
        int y   // CS1002: ; expected
#pragma warning restore {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used|]
        y = 1;
    }}
}}

#endif");
            }

            [Fact]
            public async Task TestDoNotRemoveDiagnosticSuppressionWhenAnalyzerSuppressed()
            {
                await TestMissingInRegularAndScriptAsync(
        $@"
#pragma warning disable {IDEDiagnosticIds.RemoveUnnecessarySuppressionDiagnosticId}

class Class
{{
    void M()
    {{
[|#pragma warning disable {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used - Unnecessary, but suppressed
        int y;
#pragma warning restore {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used - Unnecessary, but suppressed|]
        y = 1;
    }}
}}");
            }

            [Theory, CombinatorialData]
            public async Task TestDoNotRemoveExcludedDiagnosticSuppression(bool excludeAll)
            {
                var options = new OptionsCollection(LanguageNames.CSharp)
                {
                    { CodeStyleOptions2.RemoveUnnecessarySuppressionExclusions, excludeAll ? "all" : VariableDeclaredButNotUsedDiagnosticId }
                };

                await TestMissingInRegularAndScriptAsync(
        $@"
class Class
{{
    void M()
    {{
[|#pragma warning disable {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used - Unnecessary, but suppressed
        int y;
#pragma warning restore {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used - Unnecessary, but suppressed|]
        y = 1;
    }}
}}", new TestParameters(options: options));
            }

            [Theory, CombinatorialData]
            public async Task TestRemoveDiagnosticSuppression(bool testFixFromDisable)
            {
                var (disablePrefix, disableSuffix, restorePrefix, restoreSuffix) = testFixFromDisable
                    ? ("[|", "|]", "", "")
                    : ("", "", "[|", "|]");

                await TestInRegularAndScript1Async(
        $@"
class Class
{{
    void M()
    {{
{disablePrefix}#pragma warning disable {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used - Unnecessary{disableSuffix}
        int y;
{restorePrefix}#pragma warning restore {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used - Unnecessary{restoreSuffix}
        y = 1;
    }}
}}",
        @"
class Class
{
    void M()
    {
        int y;
        y = 1;
    }
}");
            }

            [Fact]
            public async Task TestRemoveDiagnosticSuppression_OnlyDisableDirective()
            {
                await TestInRegularAndScript1Async(
        $@"
class Class
{{
    void M()
    {{
[|#pragma warning disable {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used - Unnecessary|]
        int y;
        y = 1;
    }}
}}",
        @"
class Class
{
    void M()
    {
        int y;
        y = 1;
    }
}");
            }

            [Fact]
            public async Task TestRemoveDiagnosticSuppression_OnlyRestoreDirective()
            {
                await TestInRegularAndScript1Async(
        $@"
class Class
{{
    void M()
    {{
        int y;
[|#pragma warning restore {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used - Unnecessary|]
        y = 1;
    }}
}}",
        @"
class Class
{
    void M()
    {
        int y;
        y = 1;
    }
}");
            }

            [Theory, CombinatorialData]
            public async Task TestRemoveDiagnosticSuppression_DuplicateSuppression(bool testFixFromDisable)
            {
                var (disablePrefix, disableSuffix, restorePrefix, restoreSuffix) = testFixFromDisable
                    ? ("[|", "|]", "", "")
                    : ("", "", "[|", "|]");

                await TestInRegularAndScript1Async(
        $@"
class Class
{{
{disablePrefix}#pragma warning disable {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used - Unnecessary{disableSuffix}
    void M()
    {{
#pragma warning disable {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used - Necessary
        int y;
#pragma warning restore {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used - Necessary
    }}
{restorePrefix}#pragma warning restore {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used - Unnecessary{restoreSuffix}
}}",
        $@"
class Class
{{
    void M()
    {{
#pragma warning disable {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used - Necessary
        int y;
#pragma warning restore {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used - Necessary
    }}
}}");
            }

            [Theory, CombinatorialData]
            public async Task TestRemoveDiagnosticSuppression_InnerValidSuppression(bool testFixFromDisable)
            {
                var (disablePrefix, disableSuffix, restorePrefix, restoreSuffix) = testFixFromDisable
                    ? ("[|", "|]", "", "")
                    : ("", "", "[|", "|]");

                await TestInRegularAndScript1Async(
        $@"
class Class
{{
    void M()
    {{
{disablePrefix}#pragma warning disable {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used - Unnecessary{disableSuffix}
#pragma warning disable {VariableAssignedButNotUsedDiagnosticId} // Variable is assigned but its value is never used - Necessary
        int y = 0;
#pragma warning restore {VariableAssignedButNotUsedDiagnosticId} // Variable is assigned but its value is never used - Necessary
{restorePrefix}#pragma warning restore {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used - Unnecessary{restoreSuffix}
    }}
}}",
        $@"
class Class
{{
    void M()
    {{
#pragma warning disable {VariableAssignedButNotUsedDiagnosticId} // Variable is assigned but its value is never used - Necessary
        int y = 0;
#pragma warning restore {VariableAssignedButNotUsedDiagnosticId} // Variable is assigned but its value is never used - Necessary
    }}
}}");
            }

            [Theory, CombinatorialData]
            public async Task TestRemoveDiagnosticSuppression_OuterValidSuppression(bool testFixFromDisable)
            {
                var (disablePrefix, disableSuffix, restorePrefix, restoreSuffix) = testFixFromDisable
                    ? ("[|", "|]", "", "")
                    : ("", "", "[|", "|]");

                await TestInRegularAndScript1Async(
        $@"
class Class
{{
    void M()
    {{
#pragma warning disable {VariableAssignedButNotUsedDiagnosticId} // Variable is assigned but its value is never used - Necessary
{disablePrefix}#pragma warning disable {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used - Unnecessary{disableSuffix}
        int y = 0;
{restorePrefix}#pragma warning restore {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used - Unnecessary{restoreSuffix}
#pragma warning restore {VariableAssignedButNotUsedDiagnosticId} // Variable is assigned but its value is never used - Necessary
    }}
}}",
        $@"
class Class
{{
    void M()
    {{
#pragma warning disable {VariableAssignedButNotUsedDiagnosticId} // Variable is assigned but its value is never used - Necessary
        int y = 0;
#pragma warning restore {VariableAssignedButNotUsedDiagnosticId} // Variable is assigned but its value is never used - Necessary
    }}
}}");
            }

            [Theory, CombinatorialData]
            public async Task TestRemoveDiagnosticSuppression_OverlappingDirectives(bool testFixFromDisable)
            {
                var (disablePrefix, disableSuffix, restorePrefix, restoreSuffix) = testFixFromDisable
                    ? ("[|", "|]", "", "")
                    : ("", "", "[|", "|]");

                await TestInRegularAndScript1Async(
        $@"
class Class
{{
    void M()
    {{
{disablePrefix}#pragma warning disable {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used - Unnecessary{disableSuffix}
#pragma warning disable {VariableAssignedButNotUsedDiagnosticId} // Variable is assigned but its value is never used - Necessary
        int y = 0;
{restorePrefix}#pragma warning restore {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used - Unnecessary{restoreSuffix}
#pragma warning restore {VariableAssignedButNotUsedDiagnosticId} // Variable is assigned but its value is never used - Necessary
    }}
}}",
        $@"
class Class
{{
    void M()
    {{
#pragma warning disable {VariableAssignedButNotUsedDiagnosticId} // Variable is assigned but its value is never used - Necessary
        int y = 0;
#pragma warning restore {VariableAssignedButNotUsedDiagnosticId} // Variable is assigned but its value is never used - Necessary
    }}
}}");
            }

            [Fact]
            public async Task TestRemoveDiagnosticSuppression_DuplicateDisableWithoutMatchingRestoreDirective()
            {
                await TestInRegularAndScript1Async(
        $@"
class Class
{{
    void M()
    {{
[|#pragma warning disable {VariableAssignedButNotUsedDiagnosticId} // Variable is assigned but its value is never used - Unnecessary|]
#pragma warning disable {VariableAssignedButNotUsedDiagnosticId} // Variable is assigned but its value is never used - Necessary
        int y = 0;
#pragma warning restore {VariableAssignedButNotUsedDiagnosticId} // Variable is assigned but its value is never used - Necessary
    }}
}}",
        $@"
class Class
{{
    void M()
    {{
#pragma warning disable {VariableAssignedButNotUsedDiagnosticId} // Variable is assigned but its value is never used - Necessary
        int y = 0;
#pragma warning restore {VariableAssignedButNotUsedDiagnosticId} // Variable is assigned but its value is never used - Necessary
    }}
}}");
            }

            [Fact]
            public async Task TestRemoveDiagnosticSuppression_DuplicateRestoreWithoutMatchingDisableDirective()
            {
                await TestInRegularAndScript1Async(
        $@"
class Class
{{
    void M()
    {{
#pragma warning disable {VariableAssignedButNotUsedDiagnosticId} // Variable is assigned but its value is never used - Necessary
        int y = 0;
#pragma warning restore {VariableAssignedButNotUsedDiagnosticId} // Variable is assigned but its value is never used - Necessary
[|#pragma warning restore {VariableAssignedButNotUsedDiagnosticId} // Variable is assigned but its value is never used - Unnecessary|]
    }}
}}",
        $@"
class Class
{{
    void M()
    {{
#pragma warning disable {VariableAssignedButNotUsedDiagnosticId} // Variable is assigned but its value is never used - Necessary
        int y = 0;
#pragma warning restore {VariableAssignedButNotUsedDiagnosticId} // Variable is assigned but its value is never used - Necessary
    }}
}}");
            }

            [Theory, CombinatorialData]
            public async Task TestRemoveUnknownDiagnosticSuppression(bool testFixFromDisable)
            {
                var (disablePrefix, disableSuffix, restorePrefix, restoreSuffix) = testFixFromDisable
                    ? ("[|", "|]", "", "")
                    : ("", "", "[|", "|]");

                await TestInRegularAndScript1Async(
        $@"
{disablePrefix}#pragma warning disable UnknownId{disableSuffix}
class Class
{restorePrefix}#pragma warning restore UnknownId{restoreSuffix}
{{
}}",
        @"
class Class
{
}");
            }
        }

        #endregion

        #region Multiple analyzer tests (Compiler AND Analyzer)

        public sealed class CompilerAndAnalyzerTests : RemoveUnnecessaryPragmaSuppressionsTests
        {
            internal override ImmutableArray<DiagnosticAnalyzer> OtherAnalyzers =>
                ImmutableArray.Create<DiagnosticAnalyzer>(new CSharpCompilerDiagnosticAnalyzer(), new UserDiagnosticAnalyzer());

            [Fact]
            public async Task TestDoNotRemoveInvalidDiagnosticSuppression()
            {
                await TestMissingInRegularAndScriptAsync(
        $@"
class Class
{{
    void M()
    {{
[|#pragma warning disable
        int y;
#pragma warning restore |]
        y = 1;
    }}
}}
");
            }

            [Fact]
            public async Task TestDoNotRemoveDiagnosticSuppressionsForSuppressedAnalyzer()
            {
                var source = $@"
class Class
{{
    void M()
    {{
[|#pragma warning disable CS0168 // Variable is declared but never used - Unnecessary, but suppressed
#pragma warning disable {UserDiagnosticAnalyzer.Descriptor0168.Id} // Variable is declared but never used - Unnecessary, but suppressed
        int y;
#pragma warning restore {UserDiagnosticAnalyzer.Descriptor0168.Id} // Variable is declared but never used - Unnecessary, but suppressed
#pragma warning restore CS0168 // Variable is declared but never used - Unnecessary, but suppressed|]
        y = 1;
    }}
}}";
                var parameters = new TestParameters();
                using var workspace = CreateWorkspaceFromFile(source, parameters);

                // Suppress the diagnostic in options.
                var projectId = workspace.Projects[0].Id;
                var compilationOptions = TestOptions.DebugDll.WithSpecificDiagnosticOptions(
                ImmutableDictionary<string, ReportDiagnostic>.Empty
                    .Add(IDEDiagnosticIds.RemoveUnnecessarySuppressionDiagnosticId, ReportDiagnostic.Suppress));
                workspace.SetCurrentSolution(s => s.WithProjectCompilationOptions(projectId, compilationOptions), WorkspaceChangeKind.ProjectChanged, projectId);

                var (actions, _) = await GetCodeActionsAsync(workspace, parameters);
                Assert.True(actions.Length == 0, "An action was offered when none was expected");
            }

            [Theory, CombinatorialData]
            public async Task TestDoNotRemoveCompilerDiagnosticSuppression_IntegerId(bool leadingZero)
            {
                var id = leadingZero ? "0168" : "168";
                await TestMissingInRegularAndScriptAsync(
        $@"
class Class
{{
    void M()
    {{
[|#pragma warning disable {id} // Variable is declared but never used - Necessary
        int y;
#pragma warning restore {id} // Variable is declared but never used - Necessary|]
    }}
}}");
            }

            [Theory, CombinatorialData]
            public async Task TestRemoveCompilerDiagnosticSuppression_IntegerId(bool leadingZero)
            {
                var id = leadingZero ? "0168" : "168";
                await TestInRegularAndScript1Async(
        $@"
class Class
{{
    void M()
    {{
[|#pragma warning disable {id} // Variable is declared but never used - Unnecessary|]
        int y;
#pragma warning restore {id} // Variable is declared but never used - Unnecessary
        y = 1;
    }}
}}",
        @"
class Class
{
    void M()
    {
        int y;
        y = 1;
    }
}");
            }

            [Theory, CombinatorialData]
            public async Task TestDoNotRemoveExcludedDiagnosticSuppression_Multiple(bool excludeAll)
            {
                var options = new OptionsCollection(LanguageNames.CSharp)
                {
                    { CodeStyleOptions2.RemoveUnnecessarySuppressionExclusions, excludeAll ? "all" : $"CS0168, {UserDiagnosticAnalyzer.Descriptor0168.Id}" }
                };

                await TestMissingInRegularAndScriptAsync(
        $@"
class Class
{{
    void M()
    {{
[|#pragma warning disable CS0168 // Variable is declared but never used - Unnecessary, but suppressed
#pragma warning disable {UserDiagnosticAnalyzer.Descriptor0168.Id} // Variable is declared but never used - Unnecessary, but suppressed
        int y;
#pragma warning restore {UserDiagnosticAnalyzer.Descriptor0168.Id} // Variable is declared but never used - Unnecessary, but suppressed
#pragma warning restore CS0168 // Variable is declared but never used - Unnecessary, but suppressed|]
        y = 1;
    }}
}}", new TestParameters(options: options));
            }

            [Theory, CombinatorialData]
            public async Task TestDoNotRemoveExcludedDiagnosticSuppression_Subset(bool suppressCompilerDiagnostic, bool testDisableDirective)
            {
                var (disabledId, enabledId) = suppressCompilerDiagnostic
                    ? ("CS0168", UserDiagnosticAnalyzer.Descriptor0168.Id)
                    : (UserDiagnosticAnalyzer.Descriptor0168.Id, "CS0168");

                var options = new OptionsCollection(LanguageNames.CSharp)
                {
                    { CodeStyleOptions2.RemoveUnnecessarySuppressionExclusions, disabledId }
                };

                var (disablePrefix, disableSuffix, restorePrefix, restoreSuffix) = testDisableDirective
                    ? ("[|", "|]", "", "")
                    : ("", "", "[|", "|]");

                // Verify disabled ID is not marked unnecessary.
                await TestMissingInRegularAndScriptAsync(
        $@"
class Class
{{
    void M()
    {{
{disablePrefix}#pragma warning disable {disabledId} // Variable is declared but never used - Unnecessary, but suppressed{disableSuffix}
#pragma warning disable {enabledId} // Variable is declared but never used - Unnecessary, not suppressed
        int y;
#pragma warning restore {enabledId} // Variable is declared but never used - Unnecessary, not suppressed
{restorePrefix}#pragma warning restore {disabledId} // Variable is declared but never used - Unnecessary, but suppressed{restoreSuffix}
        y = 1;
    }}
}}", new TestParameters(options: options));

                // Verify enabled ID is marked unnecessary and removed with code fix.
                await TestInRegularAndScriptAsync(
        $@"
class Class
{{
    void M()
    {{
#pragma warning disable {disabledId} // Variable is declared but never used - Unnecessary, but suppressed
{disablePrefix}#pragma warning disable {enabledId} // Variable is declared but never used - Unnecessary, not suppressed{disableSuffix}
        int y;
{restorePrefix}#pragma warning restore {enabledId} // Variable is declared but never used - Unnecessary, not suppressed{restoreSuffix}
#pragma warning restore {disabledId} // Variable is declared but never used - Unnecessary, but suppressed
        y = 1;
    }}
}}", $@"
class Class
{{
    void M()
    {{
#pragma warning disable {disabledId} // Variable is declared but never used - Unnecessary, but suppressed
        int y;
#pragma warning restore {disabledId} // Variable is declared but never used - Unnecessary, but suppressed
        y = 1;
    }}
}}", options: options);
            }

            [Theory, CombinatorialData]
            public async Task TestRemoveDiagnosticSuppression_FixAll(bool testFixFromDisable)
            {
                var (disablePrefix, disableSuffix, restorePrefix, restoreSuffix) = testFixFromDisable
                    ? ("{|FixAllInDocument:", "|}", "", "")
                    : ("", "", "{|FixAllInDocument:", "|}");

                await TestInRegularAndScript1Async(
        $@"
#pragma warning disable CS0168 // Variable is declared but never used - Unnecessary
#pragma warning disable {UserDiagnosticAnalyzer.Descriptor0168.Id}
class Class
{{
    void M()
    {{
{disablePrefix}#pragma warning disable {UserDiagnosticAnalyzer.Descriptor0168.Id} // Variable is declared but never used - Unnecessary{disableSuffix}
#pragma warning disable CS0168 // Variable is declared but never used - Unnecessary
        int y;
{restorePrefix}#pragma warning restore {UserDiagnosticAnalyzer.Descriptor0168.Id} // Variable is declared but never used - Unnecessary{restoreSuffix}
#pragma warning restore CS0168 // Variable is declared but never used
        y = 1;
    }}
}}",
        @"
class Class
{
    void M()
    {
        int y;
        y = 1;
    }
}");
            }
        }

        #endregion
    }
}
