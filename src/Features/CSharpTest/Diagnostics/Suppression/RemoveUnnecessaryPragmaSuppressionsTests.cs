// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using Microsoft.CodeAnalysis.CSharp.UseAutoProperty;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.RemoveUnnecessarySuppressions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnnecessarySuppressions;

[Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessarySuppressions)]
[WorkItem("https://github.com/dotnet/roslyn/issues/44177")]
public abstract class RemoveUnnecessaryInlineSuppressionsTests : AbstractUnncessarySuppressionDiagnosticTest
{
    protected RemoveUnnecessaryInlineSuppressionsTests(ITestOutputHelper logger)
        : base(logger)
    {
    }

    #region Helpers

    internal sealed override CodeFixProvider CodeFixProvider
        => new RemoveUnnecessaryInlineSuppressionsCodeFixProvider();
    internal sealed override AbstractRemoveUnnecessaryInlineSuppressionsDiagnosticAnalyzer SuppressionAnalyzer
        => new CSharpRemoveUnnecessaryInlineSuppressionsDiagnosticAnalyzer();

    protected sealed override ParseOptions GetScriptOptions() => Options.Script;
    protected internal sealed override string GetLanguage() => LanguageNames.CSharp;

    protected override TestParameters SetParameterDefaults(TestParameters parameters)
        => parameters.WithCompilationOptions((parameters.compilationOptions ?? TestOptions.DebugDll).WithReportSuppressedDiagnostics(true));

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
            new DiagnosticDescriptor("CompilationEndId", "Title", "Message", "Category", DiagnosticSeverity.Warning, isEnabledByDefault: true,
                customTags: [WellKnownDiagnosticTags.CompilationEnd]);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);
        public override void Initialize(AnalysisContext context)
            => context.RegisterCompilationStartAction(context => context.RegisterCompilationEndAction(_ => { }));
    }

    #endregion

    #region Single analyzer tests (Compiler OR Analyzer)

    public abstract class CompilerOrAnalyzerTests : RemoveUnnecessaryInlineSuppressionsTests
    {
        protected CompilerOrAnalyzerTests(ITestOutputHelper logger)
            : base(logger)
        {
        }

        protected abstract bool IsCompilerDiagnosticsTest { get; }
        protected abstract string VariableDeclaredButNotUsedDiagnosticId { get; }
        protected abstract string VariableAssignedButNotUsedDiagnosticId { get; }
        protected abstract ImmutableArray<string> UnsupportedDiagnosticIds { get; }

        public sealed class CompilerTests : CompilerOrAnalyzerTests
        {
            public CompilerTests(ITestOutputHelper logger)
                : base(logger)
            {
            }

            internal override ImmutableArray<DiagnosticAnalyzer> OtherAnalyzers
                => ImmutableArray.Create<DiagnosticAnalyzer>(new CSharpCompilerDiagnosticAnalyzer());

            protected override bool IsCompilerDiagnosticsTest => true;
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
                            var errorCodeString = errorCode.ToString();
                            var errorCodeD4String = errorCode.ToString("D4");
                            builder.Add(errorCodeString);
                            if (errorCodeD4String != errorCodeString)
                                builder.Add(errorCodeD4String);
                            builder.Add("CS" + errorCodeD4String);
                        }
                    }

                    return builder.ToImmutableAndClear();
                }
            }
        }

        public sealed class AnalyzerTests : CompilerOrAnalyzerTests
        {
            public AnalyzerTests(ITestOutputHelper logger)
                : base(logger)
            {
            }

            internal override ImmutableArray<DiagnosticAnalyzer> OtherAnalyzers
                => ImmutableArray.Create<DiagnosticAnalyzer>(new UserDiagnosticAnalyzer(), new CompilationEndDiagnosticAnalyzer());
            protected override bool IsCompilerDiagnosticsTest => false;
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
        public async Task TestDoNotRemoveRequiredDiagnosticSuppression_Pragma()
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
        public async Task TestDoNotRemoveRequiredDiagnosticSuppression_Pragma_02()
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

        [Fact]
        public async Task TestDoNotRemoveRequiredDiagnosticSuppression_Attribute_Method()
        {
            var code = $@"
class Class
{{
    [|[System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""{VariableDeclaredButNotUsedDiagnosticId}"")]|]
    void M()
    {{
        int y;
    }}
}}";
            // Compiler diagnostics cannot be suppressed with SuppressMessageAttribute.
            // Hence, attribute suppressions for compiler diagnostics are always unnecessary.
            if (!IsCompilerDiagnosticsTest)
            {
                await TestMissingInRegularAndScriptAsync(code);
            }
            else
            {
                await TestInRegularAndScript1Async(code, @"
class Class
{
    void M()
    {
        int y;
    }
}");
            }
        }

        [Fact]
        public async Task TestDoNotRemoveRequiredDiagnosticSuppression_Attribute_02()
        {
            var code = $@"
[|[System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""{VariableDeclaredButNotUsedDiagnosticId}"")]|]
class Class
{{
    void M()
    {{
        int y;
    }}
}}";
            // Compiler diagnostics cannot be suppressed with SuppressMessageAttribute.
            // Hence, attribute suppressions for compiler diagnostics are always unnecessary.
            if (!IsCompilerDiagnosticsTest)
            {
                await TestMissingInRegularAndScriptAsync(code);
            }
            else
            {
                await TestInRegularAndScript1Async(code, @"
class Class
{
    void M()
    {
        int y;
    }
}");
            }
        }

        public enum TestKind
        {
            Pragmas,
            SuppressMessageAttributes,
            PragmasAndSuppressMessageAttributes
        }

        [Theory, CombinatorialData]
        [WorkItem("https://github.com/dotnet/roslyn/issues/46047")]
        public async Task TestDoNotRemoveUnsupportedDiagnosticSuppression(bool disable, TestKind testKind)
        {
            var disableOrRestore = disable ? "disable" : "restore";
            var pragmas = new StringBuilder();
            var suppressMessageAttribtes = new StringBuilder();
            foreach (var id in UnsupportedDiagnosticIds)
            {
                if (testKind is TestKind.Pragmas or TestKind.PragmasAndSuppressMessageAttributes)
                    pragmas.AppendLine($@"#pragma warning {disableOrRestore} {id}");

                if (testKind is TestKind.SuppressMessageAttributes or TestKind.PragmasAndSuppressMessageAttributes)
                    suppressMessageAttribtes.AppendLine($@"[System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""{id}"")]");
            }

            var source = $@"{{|FixAllInDocument:{pragmas}{suppressMessageAttribtes}|}}class Class {{ }}";

            // Compiler diagnostics cannot be suppressed with SuppressMessageAttribute.
            // Hence, attribute suppressions for compiler diagnostics are always unnecessary.
            if (!IsCompilerDiagnosticsTest || testKind == TestKind.Pragmas)
            {
                await TestMissingInRegularAndScriptAsync(source);
            }
            else
            {
                var fixedSource = $@"{pragmas}class Class {{ }}";
                await TestInRegularAndScriptAsync(source, fixedSource);
            }
        }

        [Fact]
        public async Task TestDoNotRemoveInactiveDiagnosticSuppression()
        {
            await TestMissingInRegularAndScriptAsync(
    $@"
#if false
[|
class Class
{{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""{VariableDeclaredButNotUsedDiagnosticId}"")]
    void M()
    {{
#pragma warning disable {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used - Inactive
        int y;
#pragma warning restore {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used - Inactive
        y = 1;
    }}
}}
|]
#endif");
        }

        [Fact]
        public async Task TestDoNotRemoveDiagnosticSuppressionsInCodeWithSyntaxErrors()
        {
            await TestMissingInRegularAndScriptAsync(
    $@"
[|
class Class
{{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""{VariableDeclaredButNotUsedDiagnosticId}"")]
    void M()
    {{
#pragma warning disable {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used
        int y   // CS1002: ; expected
#pragma warning restore {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used
        y = 1;
    }}
}}
|]");
        }

        [Fact]
        public async Task TestDoNotRemoveDiagnosticSuppressionWhenAnalyzerSuppressed()
        {
            await TestMissingInRegularAndScriptAsync(
    $@"
#pragma warning disable {IDEDiagnosticIds.RemoveUnnecessarySuppressionDiagnosticId}
[|
class Class
{{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""{VariableDeclaredButNotUsedDiagnosticId}"")]
    void M()
    {{
#pragma warning disable {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used - Unnecessary, but suppressed
        int y;
#pragma warning restore {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used - Unnecessary, but suppressed
        y = 1;
    }}
}}
|]");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46075")]
        public async Task TestDoNotRemoveDiagnosticSuppressionInGeneratedCode()
        {
            await TestMissingInRegularAndScriptAsync(
    $@"
// <autogenerated>
[|
class Class
{{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""{VariableDeclaredButNotUsedDiagnosticId}"")] // Variable is declared but never used - Unnecessary, but not reported in generated code
    void M()
    {{
#pragma warning disable {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used - Unnecessary, but not reported in generated code
        int y;
#pragma warning restore {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used - Unnecessary, but not reported in generated code
        y = 1;
    }}
}}
|]");
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
[|
class Class
{{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""{VariableDeclaredButNotUsedDiagnosticId}"")]
    void M()
    {{
#pragma warning disable {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used - Unnecessary, but suppressed
        int y;
#pragma warning restore {VariableDeclaredButNotUsedDiagnosticId} // Variable is declared but never used - Unnecessary, but suppressed
        y = 1;
    }}
}}
|]", new TestParameters(options: options));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47288")]
        public async Task TestDoNotRemoveExcludedDiagnosticCategorySuppression()
        {
            var options = new OptionsCollection(LanguageNames.CSharp)
            {
                { CodeStyleOptions2.RemoveUnnecessarySuppressionExclusions, "category: ExcludedCategory" }
            };

            await TestMissingInRegularAndScriptAsync(
    $@"
[|
class Class
{{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""ExcludedCategory"", ""{VariableDeclaredButNotUsedDiagnosticId}"")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""ExcludedCategory"", ""{VariableAssignedButNotUsedDiagnosticId}"")]
    void M()
    {{
        int y;
        y = 1;

        int z = 1;
        z++;
    }}
}}
|]", new TestParameters(options: options));
        }

        [Fact]
        public async Task TestDoNotRemoveDiagnosticSuppression_Attribute_OnPartialDeclarations()
        {
            await TestMissingInRegularAndScriptAsync(
    $@"
[|
// Unnecessary, but we do not perform analysis for SuppressMessageAttributes on partial declarations.
[System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""{VariableDeclaredButNotUsedDiagnosticId}"")]
partial class Class
{{
}}

partial class Class
{{
    void M()
    {{
        int y;
        y = 1;
    }}
}}
|]");
        }

        [Theory, CombinatorialData]
        public async Task TestRemoveDiagnosticSuppression_Pragma(bool testFixFromDisable)
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
        public async Task TestRemoveDiagnosticSuppression_Attribute()
        {
            await TestInRegularAndScript1Async(
    $@"
class Class
{{
    [|[System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""{VariableDeclaredButNotUsedDiagnosticId}"")]|] // Variable is declared but never used - Unnecessary
    void M()
    {{
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
        public async Task TestRemoveDiagnosticSuppression_Attribute_Trivia()
        {
            await TestInRegularAndScript1Async(
    $@"
class Class
{{
    // Comment1
    /// <summary>
    /// DocComment
    /// </summary>
    // Comment2
    [|[System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""{VariableDeclaredButNotUsedDiagnosticId}"")]|] // Comment3
    // Comment4
    void M()
    {{
        int y;
        y = 1;
    }}
}}",
    @"
class Class
{
    // Comment1
    /// <summary>
    /// DocComment
    /// </summary>
    // Comment2
    // Comment4
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
        public async Task TestRemoveDiagnosticSuppression_DuplicatePragmaSuppression(bool testFixFromDisable)
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

        [Fact]
        public async Task TestRemoveDiagnosticSuppression_DuplicateAttributeSuppression()
        {
            // Compiler diagnostics cannot be suppressed with SuppressMessageAttribute.
            // Hence, attribute suppressions for compiler diagnostics are always unnecessary.
            var retainedAttributesInFixCode = IsCompilerDiagnosticsTest
                ? string.Empty
                : $@"[System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""{VariableDeclaredButNotUsedDiagnosticId}"")] // Variable is declared but never used - Necessary
    ";

            await TestInRegularAndScript1Async(
    $@"
class Class
{{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""{VariableDeclaredButNotUsedDiagnosticId}"")] // Variable is declared but never used - Necessary
    {{|FixAllInDocument:[System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""{VariableDeclaredButNotUsedDiagnosticId}"")] // Variable is declared but never used - Unnecessary|}}
    void M()
    {{
        int y;
    }}
}}",
    $@"
class Class
{{
    {retainedAttributesInFixCode}void M()
    {{
        int y;
    }}
}}");
        }

        [Fact]
        public async Task TestRemoveDiagnosticSuppression_DuplicateAttributeSuppression_OnContainingSymbol()
        {
            // Compiler diagnostics cannot be suppressed with SuppressMessageAttribute.
            // Hence, attribute suppressions for compiler diagnostics are always unnecessary.
            var retainedAttributesInFixCode = IsCompilerDiagnosticsTest
                ? string.Empty
                : $@"[System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""{VariableDeclaredButNotUsedDiagnosticId}"")] // Variable is declared but never used - Necessary
    ";

            await TestInRegularAndScript1Async(
    $@"
{{|FixAllInDocument:[System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""{VariableDeclaredButNotUsedDiagnosticId}"")] // Variable is declared but never used - Unnecessary|}}
class Class
{{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""{VariableDeclaredButNotUsedDiagnosticId}"")] // Variable is declared but never used - Necessary
    void M()
    {{
        int y;
    }}
}}",
    $@"
class Class
{{
    {retainedAttributesInFixCode}void M()
    {{
        int y;
    }}
}}");
        }

        [Fact]
        public async Task TestRemoveDiagnosticSuppression_DuplicatePragmaAndAttributeSuppression()
        {
            var source = $@"
class Class
{{
    [|[System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""{VariableDeclaredButNotUsedDiagnosticId}"")]
    void M()
    {{
#pragma warning disable {VariableDeclaredButNotUsedDiagnosticId}|]
        int y;
#pragma warning restore {VariableDeclaredButNotUsedDiagnosticId}
    }}
}}";
            string fixedSource;
            if (IsCompilerDiagnosticsTest)
            {
                // Compiler diagnostics cannot be suppressed with SuppressMessageAttribute.
                // Hence, attribute suppressions for compiler diagnostics are always unnecessary.
                fixedSource = $@"
class Class
{{
    void M()
    {{
#pragma warning disable {VariableDeclaredButNotUsedDiagnosticId}
        int y;
#pragma warning restore {VariableDeclaredButNotUsedDiagnosticId}
    }}
}}";
            }
            else
            {
                // Analyzer diagnostics can be suppressed with both SuppressMessageAttribute and pragmas.
                // SuppressMessageAttribute takes precedence over pragmas for duplicate suppressions,
                // hence duplicate pragmas are considered unnecessary.
                fixedSource = $@"
class Class
{{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""{VariableDeclaredButNotUsedDiagnosticId}"")]
    void M()
    {{
        int y;
    }}
}}";
            }

            await TestInRegularAndScript1Async(source, fixedSource);
        }

        [Theory, CombinatorialData]
        public async Task TestRemoveDiagnosticSuppression_Pragma_InnerValidSuppression(bool testFixFromDisable)
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

        [Fact]
        public async Task TestRemoveDiagnosticSuppression_Attribute_InnerValidSuppression()
        {
            await TestInRegularAndScript1Async(
    $@"
class Class
{{
    [|[System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""{VariableDeclaredButNotUsedDiagnosticId}"")]|] // Variable is declared but never used - Unnecessary
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""{VariableAssignedButNotUsedDiagnosticId}"")] // Variable is assigned but its value is never used - Necessary
    void M()
    {{
        int y = 0;
    }}
}}",
    $@"
class Class
{{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""{VariableAssignedButNotUsedDiagnosticId}"")] // Variable is assigned but its value is never used - Necessary
    void M()
    {{
        int y = 0;
    }}
}}");
        }

        [Theory, CombinatorialData]
        public async Task TestRemoveDiagnosticSuppression_Pragma_OuterValidSuppression(bool testFixFromDisable)
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

        [Fact]
        public async Task TestRemoveDiagnosticSuppression_Attribute_OuterValidSuppression()
        {
            await TestInRegularAndScript1Async(
    $@"
class Class
{{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""{VariableAssignedButNotUsedDiagnosticId}"")] // Variable is assigned but its value is never used - Necessary
    [|[System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""{VariableDeclaredButNotUsedDiagnosticId}"")]|] // Variable is declared but never used - Unnecessary
    void M()
    {{
        int y = 0;
    }}
}}",
    $@"
class Class
{{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""{VariableAssignedButNotUsedDiagnosticId}"")] // Variable is assigned but its value is never used - Necessary
    void M()
    {{
        int y = 0;
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
        public async Task TestRemoveUnknownDiagnosticSuppression_Pragma(bool testFixFromDisable)
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

        [Fact]
        public async Task TestRemoveUnknownDiagnosticSuppression_Attribute()
        {
            await TestInRegularAndScript1Async(
    @"
[|[System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""UnknownId"")]|]
class Class
{
}",
    @"
class Class
{
}");
        }
    }

    #endregion

    #region Multiple analyzer tests (Compiler AND Analyzer)

    public sealed class CompilerAndAnalyzerTests : RemoveUnnecessaryInlineSuppressionsTests
    {
        public CompilerAndAnalyzerTests(ITestOutputHelper logger)
            : base(logger)
        {
        }

        internal override ImmutableArray<DiagnosticAnalyzer> OtherAnalyzers
            => ImmutableArray.Create<DiagnosticAnalyzer>(new CSharpCompilerDiagnosticAnalyzer(), new UserDiagnosticAnalyzer());

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
[|class Class
{{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""CS0168"")] // Variable is declared but never used - Unnecessary, but suppressed
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""{UserDiagnosticAnalyzer.Descriptor0168.Id}"")] // Variable is declared but never used - Unnecessary, but suppressed
    void M()
    {{
#pragma warning disable CS0168 // Variable is declared but never used - Unnecessary, but suppressed
#pragma warning disable {UserDiagnosticAnalyzer.Descriptor0168.Id} // Variable is declared but never used - Unnecessary, but suppressed
        int y;
#pragma warning restore {UserDiagnosticAnalyzer.Descriptor0168.Id} // Variable is declared but never used - Unnecessary, but suppressed
#pragma warning restore CS0168 // Variable is declared but never used - Unnecessary, but suppressed
        y = 1;
    }}
}}|]";
            var parameters = new TestParameters();
            using var workspace = CreateWorkspaceFromOptions(source, parameters);

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
[|class Class
{{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""CS0168"")] // Variable is declared but never used - Unnecessary, but suppressed
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""{UserDiagnosticAnalyzer.Descriptor0168.Id}"")] // Variable is declared but never used - Unnecessary, but suppressed
    void M()
    {{
#pragma warning disable CS0168 // Variable is declared but never used - Unnecessary, but suppressed
#pragma warning disable {UserDiagnosticAnalyzer.Descriptor0168.Id} // Variable is declared but never used - Unnecessary, but suppressed
        int y;
#pragma warning restore {UserDiagnosticAnalyzer.Descriptor0168.Id} // Variable is declared but never used - Unnecessary, but suppressed
#pragma warning restore CS0168 // Variable is declared but never used - Unnecessary, but suppressed
        y = 1;
    }}
}}|]", new TestParameters(options: options));
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
[System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""CS0168"")]
[System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""{UserDiagnosticAnalyzer.Descriptor0168.Id}"")]
class Class
{{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""CS0168"")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""{UserDiagnosticAnalyzer.Descriptor0168.Id}"")]
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

    [Fact]
    public async Task TestRemoveDiagnosticSuppression_Attribute_Field()
    {
        await TestInRegularAndScript1Async(
$@"
class Class
{{
    [|[System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""UnknownId"")]|]
    private int f;
}}", @"
class Class
{
    private int f;
}");
    }

    [Fact]
    public async Task TestRemoveDiagnosticSuppression_Attribute_Property()
    {
        await TestInRegularAndScript1Async(
$@"
class Class
{{
    [|[System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""UnknownId"")]|]
    public int P {{ get; }}
}}", @"
class Class
{
    public int P { get; }
}");
    }

    [Fact]
    public async Task TestRemoveDiagnosticSuppression_Attribute_Event()
    {
        await TestInRegularAndScript1Async(
$@"
class Class
{{
    [|[System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""UnknownId"")]|]
    private event System.EventHandler SampleEvent;
}}", @"
class Class
{
    private event System.EventHandler SampleEvent;
}");
    }

    public sealed class NonLocalDiagnosticsAnalyzerTests : RemoveUnnecessaryInlineSuppressionsTests
    {
        public NonLocalDiagnosticsAnalyzerTests(ITestOutputHelper logger)
            : base(logger)
        {
        }

        private sealed class NonLocalDiagnosticsAnalyzer : DiagnosticAnalyzer
        {
            public const string DiagnosticId = "NonLocalDiagnosticId";
            public static readonly DiagnosticDescriptor Descriptor =
                new(DiagnosticId, "NonLocalDiagnosticTitle", "NonLocalDiagnosticMessage", "NonLocalDiagnosticCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSymbolAction(context =>
                {
                    if (!context.Symbol.ContainingNamespace.IsGlobalNamespace)
                    {
                        var diagnostic = Diagnostic.Create(Descriptor, context.Symbol.ContainingNamespace.Locations[0]);
                        context.ReportDiagnostic(diagnostic);
                    }
                }, SymbolKind.NamedType);
            }
        }

        internal override ImmutableArray<DiagnosticAnalyzer> OtherAnalyzers
            => ImmutableArray.Create<DiagnosticAnalyzer>(new NonLocalDiagnosticsAnalyzer());

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50203")]
        public async Task TestDoNotRemoveInvalidDiagnosticSuppression()
        {
            await TestMissingInRegularAndScriptAsync(
    $@"
[|#pragma warning disable {NonLocalDiagnosticsAnalyzer.DiagnosticId}
namespace N
#pragma warning restore {NonLocalDiagnosticsAnalyzer.DiagnosticId}|]
{{
    class Class
    {{
    }}
}}");
        }
    }

    public sealed class UseAutoPropertyAnalyzerTests : RemoveUnnecessaryInlineSuppressionsTests
    {
        public UseAutoPropertyAnalyzerTests(ITestOutputHelper logger)
            : base(logger)
        {
        }

        internal override ImmutableArray<DiagnosticAnalyzer> OtherAnalyzers
            => ImmutableArray.Create<DiagnosticAnalyzer>(new CSharpUseAutoPropertyAnalyzer());

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55529")]
        public async Task TestDoNotRemoveAutoPropertySuppression()
        {
            await TestMissingInRegularAndScriptAsync(
    $@"
public class Test2
{{
        // Message IDE0079 Remove unnecessary suppression
        [|[System.Diagnostics.CodeAnalysis.SuppressMessage(""Style"", ""IDE0032: Use auto property"", Justification = ""<Pending >"")]|]
        private readonly int i;
            public int I => i;
}}
", new TestParameters(options: Option(CodeStyleOptions2.PreferAutoProperties, true, NotificationOption2.Warning)));
        }
    }

    #endregion
}
