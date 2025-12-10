// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class NullableContextTests : CSharpTestBase
    {
        [InlineData("#nullable enable", NullableContextOptions.Disable, NullableContext.Enabled)]
        [InlineData("#nullable enable", NullableContextOptions.Annotations, NullableContext.Enabled)]
        [InlineData("#nullable enable", NullableContextOptions.Warnings, NullableContext.Enabled)]
        [InlineData("#nullable enable", NullableContextOptions.Enable, NullableContext.Enabled)]

        [InlineData("#nullable enable warnings", NullableContextOptions.Disable, NullableContext.WarningsEnabled | NullableContext.AnnotationsContextInherited)]
        [InlineData("#nullable enable warnings", NullableContextOptions.Warnings, NullableContext.WarningsEnabled | NullableContext.AnnotationsContextInherited)]
        [InlineData("#nullable enable warnings", NullableContextOptions.Annotations, NullableContext.Enabled | NullableContext.AnnotationsContextInherited)]
        [InlineData("#nullable enable warnings", NullableContextOptions.Enable, NullableContext.Enabled | NullableContext.AnnotationsContextInherited)]

        [InlineData("#nullable enable annotations", NullableContextOptions.Disable, NullableContext.AnnotationsEnabled | NullableContext.WarningsContextInherited)]
        [InlineData("#nullable enable annotations", NullableContextOptions.Warnings, NullableContext.Enabled | NullableContext.WarningsContextInherited)]
        [InlineData("#nullable enable annotations", NullableContextOptions.Annotations, NullableContext.AnnotationsEnabled | NullableContext.WarningsContextInherited)]
        [InlineData("#nullable enable annotations", NullableContextOptions.Enable, NullableContext.Enabled | NullableContext.WarningsContextInherited)]

        [InlineData("#nullable disable", NullableContextOptions.Disable, NullableContext.Disabled)]
        [InlineData("#nullable disable", NullableContextOptions.Annotations, NullableContext.Disabled)]
        [InlineData("#nullable disable", NullableContextOptions.Warnings, NullableContext.Disabled)]
        [InlineData("#nullable disable", NullableContextOptions.Enable, NullableContext.Disabled)]

        [InlineData("#nullable disable warnings", NullableContextOptions.Disable, NullableContext.Disabled | NullableContext.AnnotationsContextInherited)]
        [InlineData("#nullable disable warnings", NullableContextOptions.Warnings, NullableContext.Disabled | NullableContext.AnnotationsContextInherited)]
        [InlineData("#nullable disable warnings", NullableContextOptions.Annotations, NullableContext.AnnotationsEnabled | NullableContext.AnnotationsContextInherited)]
        [InlineData("#nullable disable warnings", NullableContextOptions.Enable, NullableContext.AnnotationsEnabled | NullableContext.AnnotationsContextInherited)]

        [InlineData("#nullable disable annotations", NullableContextOptions.Disable, NullableContext.Disabled | NullableContext.WarningsContextInherited)]
        [InlineData("#nullable disable annotations", NullableContextOptions.Warnings, NullableContext.WarningsEnabled | NullableContext.WarningsContextInherited)]
        [InlineData("#nullable disable annotations", NullableContextOptions.Annotations, NullableContext.Disabled | NullableContext.WarningsContextInherited)]
        [InlineData("#nullable disable annotations", NullableContextOptions.Enable, NullableContext.WarningsEnabled | NullableContext.WarningsContextInherited)]
        [Theory]
        public void NullableContextExplicitlySpecifiedAndRestoredInFile(string pragma, NullableContextOptions globalContext, NullableContext expectedContext)
        {
            var source = $@"
{pragma}
class C
{{
#nullable restore
    void M() {{}}
}}";

            var comp = CreateCompilation(source, options: WithNullable(globalContext));
            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);

            var classDeclPosition = syntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single().SpanStart;
            var methodDeclPosition = syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single().SpanStart;

            Assert.Equal(expectedContext, model.GetNullableContext(classDeclPosition));

            // The context at the start of the file should always be inherited and match the global context
            var restoredContext = ((NullableContext)globalContext) | NullableContext.ContextInherited;
            Assert.Equal(restoredContext, model.GetNullableContext(0));
            Assert.Equal(restoredContext, model.GetNullableContext(methodDeclPosition));
        }

        [Fact]
        public void NullableContextMultipleFiles()
        {
            var source1 = @"
#nullable enable
partial class C
{
    void M1() {};
}";

            var source2 = @"
partial class C
{
#nullable enable
    void M2();
}";

            var comp = CreateCompilation(new[] { source1, source2 }, options: WithNullableEnable());

            var syntaxTree1 = comp.SyntaxTrees[0];
            var model1 = comp.GetSemanticModel(syntaxTree1);
            var syntaxTree2 = comp.SyntaxTrees[1];
            var model2 = comp.GetSemanticModel(syntaxTree2);

            var classDecl1 = syntaxTree1.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single().SpanStart;
            var classDecl2 = syntaxTree2.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single().SpanStart;

            Assert.Equal(NullableContext.Enabled, model1.GetNullableContext(classDecl1));
            Assert.Equal(NullableContext.Enabled | NullableContext.ContextInherited, model2.GetNullableContext(classDecl2));
        }

        [Fact]
        public void NullableContextOptionsFlags()
        {
            Assert.True(NullableContextOptions.Enable.AnnotationsEnabled());
            Assert.True(NullableContextOptions.Enable.WarningsEnabled());

            Assert.True(NullableContextOptions.Annotations.AnnotationsEnabled());
            Assert.False(NullableContextOptions.Annotations.WarningsEnabled());

            Assert.False(NullableContextOptions.Warnings.AnnotationsEnabled());
            Assert.True(NullableContextOptions.Warnings.WarningsEnabled());

            Assert.False(NullableContextOptions.Disable.AnnotationsEnabled());
            Assert.False(NullableContextOptions.Disable.WarningsEnabled());
        }

        [Fact]
        public void NullableContextFlags()
        {
            AssertEnabledForInheritance(NullableContext.Disabled, warningsEnabled: false, annotationsEnabled: false);
            AssertEnabledForInheritance(NullableContext.WarningsEnabled, warningsEnabled: true, annotationsEnabled: false);
            AssertEnabledForInheritance(NullableContext.AnnotationsEnabled, warningsEnabled: false, annotationsEnabled: true);
            AssertEnabledForInheritance(NullableContext.Enabled, warningsEnabled: true, annotationsEnabled: true);

            void AssertEnabledForInheritance(NullableContext context, bool warningsEnabled, bool annotationsEnabled)
            {
                Assert.Equal(warningsEnabled, context.WarningsEnabled());
                Assert.Equal(annotationsEnabled, context.AnnotationsEnabled());
                Assert.False(context.WarningsInherited());
                Assert.False(context.AnnotationsInherited());

                var warningsInherited = context | NullableContext.WarningsContextInherited;
                Assert.Equal(warningsEnabled, warningsInherited.WarningsEnabled());
                Assert.Equal(annotationsEnabled, warningsInherited.AnnotationsEnabled());
                Assert.True(warningsInherited.WarningsInherited());
                Assert.False(warningsInherited.AnnotationsInherited());

                var annotationsInherited = context | NullableContext.AnnotationsContextInherited;
                Assert.Equal(warningsEnabled, annotationsInherited.WarningsEnabled());
                Assert.Equal(annotationsEnabled, annotationsInherited.AnnotationsEnabled());
                Assert.False(annotationsInherited.WarningsInherited());
                Assert.True(annotationsInherited.AnnotationsInherited());

                var contextInherited = context | NullableContext.ContextInherited;
                Assert.Equal(warningsEnabled, contextInherited.WarningsEnabled());
                Assert.Equal(annotationsEnabled, contextInherited.AnnotationsEnabled());
                Assert.True(contextInherited.WarningsInherited());
                Assert.True(contextInherited.AnnotationsInherited());
            }
        }

        // See also CommandLineTests.NullableAnalysisFlags().
        [ConditionalFact(typeof(NoUsedAssembliesValidation), Reason = "GetEmitDiagnostics affects result")]
        public void NullableAnalysisFlags_01()
        {
            var source =
@"#nullable enable
class Program
{
    const object? C1 = null;
    const object? C2 = null;
#nullable enable
    static object F1() => C1;
#nullable disable
    static object F2() => C2;
}";

            var expectedAnalyzedKeysAll = new[] { ".cctor", ".ctor", "F1", "F2" };
            var expectedAnalyzedKeysDefault =
#if DEBUG
                new[] { ".cctor", ".ctor", "F1", "F2" };
#else
                new[] { ".cctor", "F1" };
#endif

            Assert.Equal("run-nullable-analysis", Feature.RunNullableAnalysis);
            verify(parseOptions: TestOptions.Regular, expectedAnalyzedKeysDefault);
            verify(parseOptions: TestOptions.Regular.WithFeature(Feature.RunNullableAnalysis, null), expectedAnalyzedKeysDefault);
            verify(parseOptions: TestOptions.Regular.WithFeature(Feature.RunNullableAnalysis, "always"), expectedAnalyzedKeysAll);
            verify(parseOptions: TestOptions.Regular.WithFeature(Feature.RunNullableAnalysis, "never"));
            verify(parseOptions: TestOptions.Regular.WithFeature(Feature.RunNullableAnalysis, "ALWAYS"), expectedAnalyzedKeysDefault); // unrecognized value (incorrect case) ignored
            verify(parseOptions: TestOptions.Regular.WithFeature(Feature.RunNullableAnalysis, "NEVER"), expectedAnalyzedKeysDefault); // unrecognized value (incorrect case) ignored
            verify(parseOptions: TestOptions.Regular.WithFeature(Feature.RunNullableAnalysis, "true"), expectedAnalyzedKeysDefault); // unrecognized value ignored
            verify(parseOptions: TestOptions.Regular.WithFeature(Feature.RunNullableAnalysis, "false"), expectedAnalyzedKeysDefault); // unrecognized value ignored
            verify(parseOptions: TestOptions.Regular.WithFeature(Feature.RunNullableAnalysis, "unknown"), expectedAnalyzedKeysDefault); // unrecognized value ignored

            void verify(CSharpParseOptions parseOptions, params string[] expectedAnalyzedKeys)
            {
                var comp = CreateCompilation(source, parseOptions: parseOptions);
                comp.TestOnlyCompilationData = new NullableWalker.NullableAnalysisData();
                if (expectedAnalyzedKeys.Length > 0)
                {
                    comp.VerifyDiagnostics(
                        // (7,27): warning CS8603: Possible null reference return.
                        //     static object F1() => C1;
                        Diagnostic(ErrorCode.WRN_NullReferenceReturn, "C1").WithLocation(7, 27));
                }
                else
                {
                    comp.VerifyDiagnostics();
                }

                var actualAnalyzedKeys = GetNullableDataKeysAsStrings(comp.TestOnlyCompilationData);
                AssertEx.Equal(expectedAnalyzedKeys, actualAnalyzedKeys);
            }
        }

        [ConditionalFact(typeof(NoUsedAssembliesValidation), Reason = "GetEmitDiagnostics affects result")]
        public void NullableAnalysisFlags_02()
        {
            var source =
@"#nullable enable
class Program
{
    const object? C1 = null;
    const object? C2 = null;
#nullable enable
    static void F1(object obj = C1) { }
#nullable disable
    static void F2(object obj = C2) { }
}";

            var expectedAnalyzedKeysAll = new[] { ".cctor", ".ctor", "= C1", "= C2", "F1", "F2" };
            var expectedAnalyzedKeysDefault =
#if DEBUG
                new[] { ".cctor", ".ctor", "= C1", "= C2", "F1", "F2" };
#else
                new[] { ".cctor", "= C1", "F1" };
#endif

            verify(parseOptions: TestOptions.Regular, expectedAnalyzedKeysDefault);
            verify(parseOptions: TestOptions.Regular.WithFeature(Feature.RunNullableAnalysis, null), expectedAnalyzedKeysDefault);
            verify(parseOptions: TestOptions.Regular.WithFeature(Feature.RunNullableAnalysis, "always"), expectedAnalyzedKeysAll);
            verify(parseOptions: TestOptions.Regular.WithFeature(Feature.RunNullableAnalysis, "never"));

            void verify(CSharpParseOptions parseOptions, params string[] expectedAnalyzedKeys)
            {
                var comp = CreateCompilation(source, parseOptions: parseOptions);
                comp.TestOnlyCompilationData = new NullableWalker.NullableAnalysisData();
                if (expectedAnalyzedKeys.Length > 0)
                {
                    comp.VerifyDiagnostics(
                        // (7,33): warning CS8625: Cannot convert null literal to non-nullable reference type.
                        //     static void F1(object obj = C1) { }
                        Diagnostic(ErrorCode.WRN_NullAsNonNullable, "C1").WithLocation(7, 33));
                }
                else
                {
                    comp.VerifyDiagnostics();
                }

                var actualAnalyzedKeys = GetNullableDataKeysAsStrings(comp.TestOnlyCompilationData);
                AssertEx.Equal(expectedAnalyzedKeys, actualAnalyzedKeys);
            }
        }

        [ConditionalFact(typeof(NoUsedAssembliesValidation), Reason = "GetEmitDiagnostics affects result")]
        public void NullableAnalysisFlags_03()
        {
            var sourceA =
@"#nullable enable
public class A : System.Attribute
{
    public A(object obj) { }
    public const object? C1 = null;
    public const object? C2 = null;
}";
            var refA = CreateCompilation(sourceA).EmitToImageReference();

            var sourceB =
@"#nullable enable
[A(A.C1)]
struct B1
{
}
#nullable disable
[A(A.C2)]
struct B2
{
}";

            var expectedAnalyzedKeysAll = new[] { ".cctor", ".cctor", "A(A.C1)", "A(A.C2)" };
            var expectedAnalyzedKeysDefault =
#if DEBUG
                new[] { ".cctor", ".cctor", "A(A.C1)", "A(A.C2)" };
#else
                new[] { "A(A.C1)" };
#endif

            verify(parseOptions: TestOptions.Regular, expectedAnalyzedKeysDefault);
            verify(parseOptions: TestOptions.Regular.WithFeature(Feature.RunNullableAnalysis, null), expectedAnalyzedKeysDefault);
            verify(parseOptions: TestOptions.Regular.WithFeature(Feature.RunNullableAnalysis, "always"), expectedAnalyzedKeysAll);
            verify(parseOptions: TestOptions.Regular.WithFeature(Feature.RunNullableAnalysis, "never"));

            void verify(CSharpParseOptions parseOptions, params string[] expectedAnalyzedKeys)
            {
                var comp = CreateCompilation(sourceB, references: new[] { refA }, parseOptions: parseOptions);
                comp.TestOnlyCompilationData = new NullableWalker.NullableAnalysisData();
                if (expectedAnalyzedKeys.Length > 0)
                {
                    comp.VerifyDiagnostics(
                        // (2,4): warning CS8625: Cannot convert null literal to non-nullable reference type.
                        // [A(A.C1)]
                        Diagnostic(ErrorCode.WRN_NullAsNonNullable, "A.C1").WithLocation(2, 4));
                }
                else
                {
                    comp.VerifyDiagnostics();
                }

                var actualAnalyzedKeys = GetNullableDataKeysAsStrings(comp.TestOnlyCompilationData);
                AssertEx.Equal(expectedAnalyzedKeys, actualAnalyzedKeys);
            }
        }

        [Fact]
        public void NullableAnalysisFlags_MethodBodySemanticModel()
        {
            var source =
@"#nullable enable
class Program
{
    static object F(object? obj)
    {
        if (obj == null) return null;
        return obj;
    }
}";

            var expectedAnalyzedKeysAll = new[] { "F" };

            verify(parseOptions: TestOptions.Regular, expectedFlowState: true, expectedAnalyzedKeysAll);
            verify(parseOptions: TestOptions.Regular.WithFeature(Feature.RunNullableAnalysis, null), expectedFlowState: true, expectedAnalyzedKeysAll);
            verify(parseOptions: TestOptions.Regular.WithFeature(Feature.RunNullableAnalysis, "always"), expectedFlowState: true, expectedAnalyzedKeysAll);
            verify(parseOptions: TestOptions.Regular.WithFeature(Feature.RunNullableAnalysis, "never"), expectedFlowState: false);

            void verify(CSharpParseOptions parseOptions, bool expectedFlowState, params string[] expectedAnalyzedKeys)
            {
                var comp = CreateCompilation(source, parseOptions: parseOptions);
                comp.TestOnlyCompilationData = new NullableWalker.NullableAnalysisData();
                var syntaxTree = comp.SyntaxTrees[0];
                var model = comp.GetSemanticModel(syntaxTree);
                var syntax = syntaxTree.GetRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Skip(1).Single();
                Assert.Equal("return obj;", syntax.ToString());
                var typeInfo = model.GetTypeInfo(syntax.Expression);
                var expectedNullability = expectedFlowState ? Microsoft.CodeAnalysis.NullableFlowState.NotNull : Microsoft.CodeAnalysis.NullableFlowState.None;
                Assert.Equal(expectedNullability, typeInfo.Nullability.FlowState);

                var actualAnalyzedKeys = GetNullableDataKeysAsStrings(comp.TestOnlyCompilationData);
                AssertEx.Equal(expectedAnalyzedKeys, actualAnalyzedKeys);
            }
        }

        [Fact]
        public void NullableAnalysisFlags_AttributeSemanticModel()
        {
            var source =
@"#nullable enable
class A : System.Attribute
{
    public A(object obj) { }
    public static object F;
}
[A(A.F = null)]
class B
{
}";

            var expectedAnalyzedKeysAll = new[] { "A(A.F = null)" };

            verify(parseOptions: TestOptions.Regular, expectedFlowState: true, expectedAnalyzedKeysAll);
            verify(parseOptions: TestOptions.Regular.WithFeature(Feature.RunNullableAnalysis, null), expectedFlowState: true, expectedAnalyzedKeysAll);
            verify(parseOptions: TestOptions.Regular.WithFeature(Feature.RunNullableAnalysis, "always"), expectedFlowState: true, expectedAnalyzedKeysAll);
            verify(parseOptions: TestOptions.Regular.WithFeature(Feature.RunNullableAnalysis, "never"), expectedFlowState: false);

            void verify(CSharpParseOptions parseOptions, bool expectedFlowState, params string[] expectedAnalyzedKeys)
            {
                var comp = CreateCompilation(source, parseOptions: parseOptions);
                comp.TestOnlyCompilationData = new NullableWalker.NullableAnalysisData();
                var syntaxTree = comp.SyntaxTrees[0];
                var model = comp.GetSemanticModel(syntaxTree);
                var syntax = syntaxTree.GetRoot().DescendantNodes().OfType<AttributeArgumentSyntax>().Single();
                Assert.Equal("A.F = null", syntax.ToString());
                var typeInfo = model.GetTypeInfo(syntax.Expression);
                var expectedNullability = expectedFlowState ? Microsoft.CodeAnalysis.NullableFlowState.MaybeNull : Microsoft.CodeAnalysis.NullableFlowState.None;
                Assert.Equal(expectedNullability, typeInfo.Nullability.FlowState);

                var actualAnalyzedKeys = GetNullableDataKeysAsStrings(comp.TestOnlyCompilationData);
                AssertEx.Equal(expectedAnalyzedKeys, actualAnalyzedKeys);
            }
        }

        [Fact]
        public void NullableAnalysisFlags_InitializerSemanticModel()
        {
            var source =
@"#nullable enable
class Program
{
    static object F;
    static void M(object arg = (F = null)) { }
}";

            var expectedAnalyzedKeysAll = new[] { "arg" };

            verify(parseOptions: TestOptions.Regular, expectedFlowState: true, expectedAnalyzedKeysAll);
            verify(parseOptions: TestOptions.Regular.WithFeature(Feature.RunNullableAnalysis, null), expectedFlowState: true, expectedAnalyzedKeysAll);
            verify(parseOptions: TestOptions.Regular.WithFeature(Feature.RunNullableAnalysis, "always"), expectedFlowState: true, expectedAnalyzedKeysAll);
            verify(parseOptions: TestOptions.Regular.WithFeature(Feature.RunNullableAnalysis, "never"), expectedFlowState: false);

            void verify(CSharpParseOptions parseOptions, bool expectedFlowState, params string[] expectedAnalyzedKeys)
            {
                var comp = CreateCompilation(source, parseOptions: parseOptions);
                comp.TestOnlyCompilationData = new NullableWalker.NullableAnalysisData();
                var syntaxTree = comp.SyntaxTrees[0];
                var model = comp.GetSemanticModel(syntaxTree);
                var syntax = syntaxTree.GetRoot().DescendantNodes().OfType<EqualsValueClauseSyntax>().First().Value;
                Assert.Equal("(F = null)", syntax.ToString());
                var typeInfo = model.GetTypeInfo(syntax);
                var expectedNullability = expectedFlowState ? Microsoft.CodeAnalysis.NullableFlowState.MaybeNull : Microsoft.CodeAnalysis.NullableFlowState.None;
                Assert.Equal(expectedNullability, typeInfo.Nullability.FlowState);

                var actualAnalyzedKeys = GetNullableDataKeysAsStrings(comp.TestOnlyCompilationData);
                AssertEx.Equal(expectedAnalyzedKeys, actualAnalyzedKeys);
            }
        }

        public readonly struct NullableDirectives
        {
            internal readonly string[] Directives;
            internal readonly NullableContextState.State ExpectedWarningsState;
            internal readonly NullableContextState.State ExpectedAnnotationsState;

            internal NullableDirectives(string[] directives, NullableContextState.State expectedWarningsState, NullableContextState.State expectedAnnotationsState)
            {
                Directives = directives;
                ExpectedWarningsState = expectedWarningsState;
                ExpectedAnnotationsState = expectedAnnotationsState;
            }

            public override string ToString()
            {
                var builder = new StringBuilder();
                foreach (var str in Directives) builder.AppendLine(str);
                return builder.ToString();
            }
        }

        private static readonly NullableDirectives[] s_nullableDirectives = new[]
        {
            new NullableDirectives(new string[0], NullableContextState.State.Unknown, NullableContextState.State.Unknown),
            new NullableDirectives(new[] { "#nullable disable" }, NullableContextState.State.Disabled, NullableContextState.State.Disabled),
            new NullableDirectives(new[] { "#nullable enable" }, NullableContextState.State.Enabled, NullableContextState.State.Enabled),
            new NullableDirectives(new[] { "#nullable restore" }, NullableContextState.State.ExplicitlyRestored, NullableContextState.State.ExplicitlyRestored),
            new NullableDirectives(new[] { "#nullable disable annotations" }, NullableContextState.State.Unknown, NullableContextState.State.Disabled),
            new NullableDirectives(new[] { "#nullable enable warnings", "#nullable disable annotations", }, NullableContextState.State.Enabled, NullableContextState.State.Disabled),
            new NullableDirectives(new[] { "#nullable restore warnings", "#nullable disable annotations" }, NullableContextState.State.ExplicitlyRestored, NullableContextState.State.Disabled),
            new NullableDirectives(new[] { "#nullable enable annotations" }, NullableContextState.State.Unknown, NullableContextState.State.Enabled),
            new NullableDirectives(new[] { "#nullable disable warnings", "#nullable enable annotations" }, NullableContextState.State.Disabled, NullableContextState.State.Enabled),
            new NullableDirectives(new[] { "#nullable restore warnings", "#nullable enable annotations" }, NullableContextState.State.ExplicitlyRestored, NullableContextState.State.Enabled),
            new NullableDirectives(new[] { "#nullable restore annotations" }, NullableContextState.State.Unknown, NullableContextState.State.ExplicitlyRestored),
            new NullableDirectives(new[] { "#nullable enable warnings" , "#nullable restore annotations" }, NullableContextState.State.Enabled, NullableContextState.State.ExplicitlyRestored),
            new NullableDirectives(new[] { "#nullable disable warnings", "#nullable restore annotations" }, NullableContextState.State.Disabled, NullableContextState.State.ExplicitlyRestored),
            new NullableDirectives(new[] { "#nullable disable warnings" }, NullableContextState.State.Disabled, NullableContextState.State.Unknown),
            new NullableDirectives(new[] { "#nullable enable warnings" }, NullableContextState.State.Enabled, NullableContextState.State.Unknown),
            new NullableDirectives(new[] { "#nullable restore warnings" }, NullableContextState.State.ExplicitlyRestored, NullableContextState.State.Unknown),
        };

        // AnalyzeMethodsInEnabledContextOnly_01_Data is split due to https://github.com/dotnet/roslyn/issues/50337
        public static IEnumerable<object[]> AnalyzeMethodsInEnabledContextOnly_01_Data1()
        {
            var projectSettings = new[]
            {
                (NullableContextOptions?)null,
                NullableContextOptions.Disable,
            };

            foreach (var projectSetting in projectSettings)
            {
                foreach (var classDirectives in s_nullableDirectives)
                {
                    foreach (var methodDirectives in s_nullableDirectives)
                    {
                        yield return new object[] { projectSetting, classDirectives, methodDirectives };
                    }
                }
            }
        }

        public static IEnumerable<object[]> AnalyzeMethodsInEnabledContextOnly_01_Data2()
        {
            var projectSettings = new[]
            {
                NullableContextOptions.Warnings,
                NullableContextOptions.Annotations,
                NullableContextOptions.Enable,
            };

            foreach (var projectSetting in projectSettings)
            {
                foreach (var classDirectives in s_nullableDirectives)
                {
                    foreach (var methodDirectives in s_nullableDirectives)
                    {
                        yield return new object[] { projectSetting, classDirectives, methodDirectives };
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(AnalyzeMethodsInEnabledContextOnly_01_Data1))]
        [WorkItem(49746, "https://github.com/dotnet/roslyn/issues/49746")]
        public void AnalyzeMethodsInEnabledContextOnly_01A(NullableContextOptions? projectContext, NullableDirectives classDirectives, NullableDirectives methodDirectives)
        {
            AnalyzeMethodsInEnabledContextOnly_01_Execute(projectContext, classDirectives, methodDirectives);
        }

        [Theory]
        [MemberData(nameof(AnalyzeMethodsInEnabledContextOnly_01_Data2))]
        [WorkItem(49746, "https://github.com/dotnet/roslyn/issues/49746")]
        public void AnalyzeMethodsInEnabledContextOnly_01B(NullableContextOptions? projectContext, NullableDirectives classDirectives, NullableDirectives methodDirectives)
        {
            AnalyzeMethodsInEnabledContextOnly_01_Execute(projectContext, classDirectives, methodDirectives);
        }

        private static void AnalyzeMethodsInEnabledContextOnly_01_Execute(NullableContextOptions? projectContext, NullableDirectives classDirectives, NullableDirectives methodDirectives)
        {
            var sourceA =
@"#nullable enable
public class A
{
    public static void M(object obj) { }
}";
            var refA = CreateCompilation(sourceA).EmitToImageReference();

            var sourceB =
$@"{classDirectives}
static class B
{{
{methodDirectives}
    static void Main() {{ A.M(null); }}
}}";

            var expectedWarningsStateForMethod = CombineState(methodDirectives.ExpectedWarningsState, classDirectives.ExpectedWarningsState);
            var expectedAnnotationsStateForMethod = CombineState(methodDirectives.ExpectedAnnotationsState, classDirectives.ExpectedAnnotationsState);

            bool isNullableEnabledForProject = projectContext != null && (projectContext.Value & NullableContextOptions.Warnings) != 0;
            bool isNullableEnabledForMethod = IsNullableEnabled(expectedWarningsStateForMethod, isNullableEnabledForProject);

            var options = TestOptions.ReleaseDll;
            if (projectContext != null) options = options.WithNullableContextOptions(projectContext.Value);
            var comp = CreateCompilation(sourceB, options: options, references: new[] { refA });
            comp.TestOnlyCompilationData = new NullableWalker.NullableAnalysisData();

            if (isNullableEnabledForMethod)
            {
                comp.VerifyDiagnostics(
                    // (4,30): warning CS8625: Cannot convert null literal to non-nullable reference type.
                    //     static void Main() { A.M(null); }
                    Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null"));
            }
            else
            {
                comp.VerifyDiagnostics();
            }

            var actualAnalyzedKeys = GetNullableDataKeysAsStrings(comp.TestOnlyCompilationData, requiredAnalysis: true);
            Assert.Equal(isNullableEnabledForMethod, actualAnalyzedKeys.Contains("Main"));

            var tree = (CSharpSyntaxTree)comp.SyntaxTrees[0];
            var syntaxNodes = tree.GetRoot().DescendantNodes();
            verifyContextState(tree, syntaxNodes.OfType<ClassDeclarationSyntax>().Single(), classDirectives.ExpectedWarningsState, classDirectives.ExpectedAnnotationsState);
            verifyContextState(tree, syntaxNodes.OfType<MethodDeclarationSyntax>().Single(), expectedWarningsStateForMethod, expectedAnnotationsStateForMethod);

            static void verifyContextState(CSharpSyntaxTree tree, CSharpSyntaxNode syntax, NullableContextState.State expectedWarningsState, NullableContextState.State expectedAnnotationsState)
            {
                var actualState = tree.GetNullableContextState(syntax.SpanStart);
                Assert.Equal(expectedWarningsState, actualState.WarningsState);
                Assert.Equal(expectedAnnotationsState, actualState.AnnotationsState);
            }
        }

        private static NullableContextState.State CombineState(NullableContextState.State currentState, NullableContextState.State previousState)
        {
            return currentState == NullableContextState.State.Unknown ? previousState : currentState;
        }

        private static bool IsNullableEnabled(NullableContextState.State state, bool isNullableEnabledForProject)
        {
            return state switch
            {
                NullableContextState.State.Enabled => true,
                NullableContextState.State.Disabled => false,
                _ => isNullableEnabledForProject,
            };
        }

        [Fact]
        [WorkItem(49746, "https://github.com/dotnet/roslyn/issues/49746")]
        public void AnalyzeMethodsInEnabledContextOnly_02()
        {
            var source =
@"#pragma warning disable 219
static class Program
{
#nullable disable
    static void M1
#nullable enable
        ()
    {
        object obj = null;
    }
}";
            verify(source, expectedAnalyzedKeys: new[] { "M1" },
                // (9,22): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         object obj = null;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(9, 22));

            source =
@"#pragma warning disable 219
static class Program
{
#nullable disable
    static void M2()
#nullable enable
    {
        object obj = null;
    }
}";
            verify(source, expectedAnalyzedKeys: new[] { "M2" },
                // (8,22): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         object obj = null;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(8, 22));

            source =
@"#pragma warning disable 219
static class Program
{
#nullable disable
    static void M3()
    {
#nullable enable
        object obj = null;
    }
}";
            verify(source, expectedAnalyzedKeys: new[] { "M3" },
                // (8,22): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         object obj = null;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(8, 22));

            source =
@"#pragma warning disable 219
static class Program
{
#nullable disable
    static void M4()
    {
#nullable disable
        object obj = null;
    }
#nullable enable
}";
            verify(source, expectedAnalyzedKeys: new string[0]);

            source =
@"#pragma warning disable 219
static class Program
{
#nullable enable
    static void M5
#nullable disable
        ()
    {
        object obj = null;
    }
}";
            verify(source, expectedAnalyzedKeys: new[] { "M5" });

            source =
@"#pragma warning disable 219
static class Program
{
    static void M6
#nullable enable
        ()
#nullable disable
    {
        object obj = null;
    }
}";
            verify(source, expectedAnalyzedKeys: new[] { "M6" });

            source =
@"static class Program
{
    static object M7()
#nullable enable
        => default(object);
}";
            verify(source, expectedAnalyzedKeys: new[] { "M7" });

            source =
@"static class Program
{
#nullable disable
    static object M8() =>
        default(
#nullable enable
            object);
}";
            verify(source, expectedAnalyzedKeys: new[] { "M8" });

            static void verify(string source, string[] expectedAnalyzedKeys, params DiagnosticDescription[] expectedDiagnostics)
            {
                var comp = CreateCompilation(source);
                comp.TestOnlyCompilationData = new NullableWalker.NullableAnalysisData();
                comp.VerifyDiagnostics(expectedDiagnostics);

                AssertEx.Equal(expectedAnalyzedKeys, GetNullableDataKeysAsStrings(comp.TestOnlyCompilationData, requiredAnalysis: true));
                AssertEx.Equal(expectedAnalyzedKeys, GetIsNullableEnabledMethods(comp.TestOnlyCompilationData));

                var tree = (CSharpSyntaxTree)comp.SyntaxTrees[0];
                var methodDeclarations = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().ToArray();
                foreach (var methodDeclaration in methodDeclarations)
                {
                    bool expectedAnalysis = expectedAnalyzedKeys.Contains(methodDeclaration.Identifier.Text);
                    bool actualAnalysis = tree.IsNullableAnalysisEnabled(methodDeclaration.Span).Value;
                    Assert.Equal(expectedAnalysis, actualAnalysis);
                }
            }
        }

        [Fact]
        [WorkItem(49746, "https://github.com/dotnet/roslyn/issues/49746")]
        public void AnalyzeMethodsInEnabledContextOnly_03()
        {
            var source =
@"class Program
{
    Program(object o) { o = null; }
}
#nullable enable
";
            verify(source, expectedAnalyzedKeys: new string[0]);

            source =
@"#nullable enable
#nullable disable
class Program
{
    Program(object o) { o = null; }
}";
            verify(source, expectedAnalyzedKeys: new string[0]);

            source =
@"class Program
{
    Program(object o) { o = null; }
#nullable enable
}";
            verify(source, expectedAnalyzedKeys: new string[0]);

            source =
@"#pragma warning disable 414
class Program
{
#nullable disable
    object F1 = null;
#nullable enable
}";
            verify(source, expectedAnalyzedKeys: new string[0]);

            source =
@"#pragma warning disable 414
class Program
{
#nullable disable
    object F1 = null;
#nullable enable
    static object F2 = null;
}";
            verify(source, expectedAnalyzedKeys: new[] { ".cctor" },
                // (7,24): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //     static object F2 = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(7, 24));

            source =
@"#pragma warning disable 414
class Program
{
#nullable enable
    object F1 = null;
#nullable disable
    static object F2 = null;
}";
            verify(source, expectedAnalyzedKeys: new[] { ".ctor" },
                // (5,17): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //     object F1 = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(5, 17));

            source =
@"#pragma warning disable 414
static class Program
{
#nullable disable
    static object F1 = null;
#nullable enable
    static Program() { F1 = null; }
}";
            verify(source, expectedAnalyzedKeys: new[] { ".cctor" });

            source =
@"#pragma warning disable 414
static class Program
{
#nullable enable
    static object F1 = null;
#nullable disable
    static Program() { F1 = null; }
}";
            verify(source, expectedAnalyzedKeys: new[] { ".cctor" },
                // (5,24): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //     static object F1 = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(5, 24));

            source =
@"#pragma warning disable 414
class Program
{
#nullable disable
    object F1 = null;
#nullable enable
    Program() { F1 = null; }
}";
            verify(source, expectedAnalyzedKeys: new[] { ".ctor" });

            source =
@"#pragma warning disable 414
class Program
{
#nullable enable
    object F1 = null;
#nullable disable
    Program() { F1 = null; }
}";
            verify(source, expectedAnalyzedKeys: new[] { ".ctor" },
                // (5,17): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //     object F1 = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(5, 17));

            source =
@"#pragma warning disable 414
class Program
{
#nullable enable
    object F1 = null;
#nullable disable
    object F2 = null;
#nullable enable
    object F3 = null;
}";
            verify(source, expectedAnalyzedKeys: new[] { ".ctor" },
                // (5,17): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //     object F1 = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(5, 17),
                // (9,17): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //     object F3 = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(9, 17));

            source =
@"#pragma warning disable 414
class Program
{
#nullable disable
    object F1 = null;
#nullable enable
    object F2() => null;
#nullable disable
    object F3 = null;
}";
            verify(source, expectedAnalyzedKeys: new[] { "F2" },
                // (7,20): warning CS8603: Possible null reference return.
                //     object F2() => null;
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "null").WithLocation(7, 20));

            source =
@"#pragma warning disable 169
class Program
{
#nullable enable
    object F1;
#nullable disable
    Program() { }
    Program(object obj) : base() { }
}";
            verify(source, expectedAnalyzedKeys: new[] { ".ctor", ".ctor" });

            source =
@"#pragma warning disable 169
class Program
{
#nullable enable
    object F1;
#nullable disable
    Program() { }
    Program(object obj) : this() { }
}";
            verify(source, expectedAnalyzedKeys: new[] { ".ctor" });

            source =
@"#pragma warning disable 169
class Program
{
#nullable enable
    object F1;
#nullable disable
    Program() { }
#nullable enable
    Program(object obj) : this() { }
}";
            verify(source, expectedAnalyzedKeys: new[] { ".ctor", ".ctor" });

            source =
@"#pragma warning disable 169
class Program
{
#nullable disable
    object F1;
    Program() { }
#nullable enable
    Program(object obj) : this() { }
}";
            verify(source, expectedAnalyzedKeys: new[] { ".ctor" });

            source =
@"#pragma warning disable 169
class Program
{
#nullable disable
    object F1;
#nullable enable
    Program() { }
#nullable disable
    Program(object obj) : this() { }
}";
            verify(source, expectedAnalyzedKeys: new[] { ".ctor" });

            source =
@"#pragma warning disable 169
struct S
{
#nullable enable
    object F1;
    S(object obj) : this() { }
}";
            verify(source, expectedAnalyzedKeys: new[] { ".ctor" },
                // (6,5): warning CS8618: Non-nullable field 'F1' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
                //     S(object obj) : this() { }
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "S").WithArguments("field", "F1").WithLocation(6, 5)
                );

            source =
@"#pragma warning disable 169
struct S
{
#nullable enable
    object F1;
#nullable disable
    S(object obj) : this() { }
}";
            verify(source, expectedAnalyzedKeys: new string[0]);

            static void verify(string source, string[] expectedAnalyzedKeys, params DiagnosticDescription[] expectedDiagnostics)
            {
                var comp = CreateCompilation(source);
                comp.TestOnlyCompilationData = new NullableWalker.NullableAnalysisData();
                comp.VerifyDiagnostics(expectedDiagnostics);

                AssertEx.Equal(expectedAnalyzedKeys, GetNullableDataKeysAsStrings(comp.TestOnlyCompilationData, requiredAnalysis: true));
                AssertEx.Equal(expectedAnalyzedKeys, GetIsNullableEnabledMethods(comp.TestOnlyCompilationData));
            }
        }

        [Fact]
        [WorkItem(49746, "https://github.com/dotnet/roslyn/issues/49746")]
        public void AnalyzeMethodsInEnabledContextOnly_04()
        {
            var source1 =
@"#pragma warning disable 414
partial class Program
{
    object F1 = null;
}";
            var source2 =
@"#pragma warning disable 414
partial class Program
{
#nullable disable
    object F2 = null;
}";
            var source3 =
@"#pragma warning disable 414
partial class Program
{
#nullable enable
    object F3 = null;
}";
            var source4 =
@"#pragma warning disable 414
partial class Program
{
#nullable restore
    object F4 = null;
}";
            var options = TestOptions.ReleaseDll.WithNullableContextOptions(NullableContextOptions.Disable);

            verify(new[] { source1, source2 }, options, new string[0]);

            verify(new[] { source1, source3 }, options, new[] { ".ctor" },
                // (5,17): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //     object F3 = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(5, 17));

            verify(new[] { source1, source4 }, options, new string[0]);

            verify(new[] { source2, source3 }, options, new[] { ".ctor" },
                // (5,17): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //     object F3 = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(5, 17));

            verify(new[] { source2, source4 }, options, new string[0]);

            verify(new[] { source3, source4 }, options, new[] { ".ctor" },
                // (5,17): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //     object F3 = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(5, 17));

            verify(new[] { source1, source2, source3, source4 }, options, new[] { ".ctor" },
                // (5,17): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //     object F3 = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(5, 17));

            verify(new[] { source1, source2, source3, source4 }, TestOptions.ReleaseDll.WithNullableContextOptions(NullableContextOptions.Enable), new[] { ".ctor" },
                // (4,17): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //     object F1 = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(4, 17),
                // (5,17): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //     object F3 = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(5, 17),
                // (5,17): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //     object F4 = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(5, 17));

            static void verify(string[] source, CSharpCompilationOptions options, string[] expectedAnalyzedKeys, params DiagnosticDescription[] expectedDiagnostics)
            {
                var comp = CreateCompilation(source, options: options);
                comp.TestOnlyCompilationData = new NullableWalker.NullableAnalysisData();
                comp.VerifyDiagnostics(expectedDiagnostics);

                AssertEx.Equal(expectedAnalyzedKeys, GetNullableDataKeysAsStrings(comp.TestOnlyCompilationData, requiredAnalysis: true));
                AssertEx.Equal(expectedAnalyzedKeys, GetIsNullableEnabledMethods(comp.TestOnlyCompilationData));
            }
        }

        [ConditionalFact(typeof(NoUsedAssembliesValidation), Reason = "GetEmitDiagnostics affects result")]
        [WorkItem(49746, "https://github.com/dotnet/roslyn/issues/49746")]
        public void AnalyzeMethodsInEnabledContextOnly_05()
        {
            var source =
@"class Program
{
#nullable disable
    object P1 { get; set; }
#nullable enable
    static object P2 { get; set; }
}";
            verify(source, expectedAnalyzedKeys: new[] { ".cctor" },
                // (6,19): warning CS8618: Non-nullable property 'P2' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
                //     static object P2 { get; set; }
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "P2").WithArguments("property", "P2").WithLocation(6, 19));

            source =
@"class Program
{
#nullable enable
    object P1 { get; set; }
#nullable disable
    static object P2 { get; set; }
}";
            verify(source, expectedAnalyzedKeys: new[] { ".ctor" },
                // (4,12): warning CS8618: Non-nullable property 'P1' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
                //     object P1 { get; set; }
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "P1").WithArguments("property", "P1").WithLocation(4, 12));

            source =
@"class Program
{
#nullable enable
    object P1 { get; }
    object P2 { get { return 2; } set { } }
    object P3 => 3;
}";
            verify(source, expectedAnalyzedKeys: new[] { ".ctor", "get_P2", "get_P3", "set_P2" },
                // (4,12): warning CS8618: Non-nullable property 'P1' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
                //     object P1 { get; }
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "P1").WithArguments("property", "P1").WithLocation(4, 12));

            source =
@"#pragma warning disable 67
delegate void D();
class Program
{
#nullable disable
    event D E1;
#nullable enable
    static event D E2;
}";
            verify(source, expectedAnalyzedKeys: new[] { ".cctor" },
                // (8,20): warning CS8618: Non-nullable event 'E2' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the event as nullable.
                //     static event D E2;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "E2").WithArguments("event", "E2").WithLocation(8, 20));

            source =
@"#pragma warning disable 67
delegate void D();
class Program
{
#nullable enable
    event D E1;
#nullable disable
    static event D E2;
}";
            verify(source, expectedAnalyzedKeys: new[] { ".ctor" },
                // (6,13): warning CS8618: Non-nullable event 'E1' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the event as nullable.
                //     event D E1;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "E1").WithArguments("event", "E1").WithLocation(6, 13));

            static void verify(string source, string[] expectedAnalyzedKeys, params DiagnosticDescription[] expectedDiagnostics)
            {
                var comp = CreateCompilation(source);
                comp.TestOnlyCompilationData = new NullableWalker.NullableAnalysisData();
                comp.VerifyDiagnostics(expectedDiagnostics);

                AssertEx.Equal(expectedAnalyzedKeys, GetNullableDataKeysAsStrings(comp.TestOnlyCompilationData, requiredAnalysis: true));
                AssertEx.Equal(expectedAnalyzedKeys, GetIsNullableEnabledMethods(comp.TestOnlyCompilationData));
            }
        }

        [Fact]
        [WorkItem(49746, "https://github.com/dotnet/roslyn/issues/49746")]
        public void AnalyzeMethodsInEnabledContextOnly_06()
        {
            var source =
@"#pragma warning disable 414
class Program
{
#nullable enable
    object F = 1;
#nullable disable
    ~Program()
    {
        F = null;
    }
}";
            verify(source, expectedAnalyzedKeys: new[] { ".ctor" });

            source =
@"#pragma warning disable 414
class Program
{
#nullable disable
    object F = 1;
#nullable enable
    ~Program()
    {
        F = null;
    }
}";
            verify(source, expectedAnalyzedKeys: new[] { "Finalize" });

            source =
@"#pragma warning disable 414
class Program
{
#nullable enable
    object F = 1;
    ~Program()
    {
        F = null;
    }
}";
            verify(source, expectedAnalyzedKeys: new[] { ".ctor", "Finalize" },
                // (8,13): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //         F = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(8, 13));

            static void verify(string source, string[] expectedAnalyzedKeys, params DiagnosticDescription[] expectedDiagnostics)
            {
                var comp = CreateCompilation(source);
                comp.TestOnlyCompilationData = new NullableWalker.NullableAnalysisData();
                comp.VerifyDiagnostics(expectedDiagnostics);

                AssertEx.Equal(expectedAnalyzedKeys, GetNullableDataKeysAsStrings(comp.TestOnlyCompilationData, requiredAnalysis: true));
                AssertEx.Equal(expectedAnalyzedKeys, GetIsNullableEnabledMethods(comp.TestOnlyCompilationData));
            }
        }

        [Fact]
        [WorkItem(49746, "https://github.com/dotnet/roslyn/issues/49746")]
        public void AnalyzeMethodsInEnabledContextOnly_07()
        {
            var source =
@"class Program
{
#nullable enable
    object P
#nullable disable
#nullable enable
        => null;
    object Q
#nullable disable
        => null;
}";
            verify(source, expectedAnalyzedKeys: new[] { "get_P" },
                // (7,12): warning CS8603: Possible null reference return.
                //         => null;
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "null").WithLocation(7, 12));

            source =
@"class Program
{
#nullable enable
    object P
    {
        get { return null; }
#nullable disable
        set { value = null; }
    }
#nullable enable
    object Q
    {
#nullable disable
        get => null;
#nullable enable
        set => value = null;
    }
}";
            verify(source, expectedAnalyzedKeys: new[] { "get_P", "set_Q" },
                // (6,22): warning CS8603: Possible null reference return.
                //         get { return null; }
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "null").WithLocation(6, 22),
                // (16,24): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         set => value = null;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(16, 24));

            source =
@"#pragma warning disable 414
delegate void D();
class Program
{
#nullable enable
    D _e = null!;
    D _f = null!;
#nullable disable
    event D E
    {
#nullable enable
        add { _e = null; }
#nullable disable
        remove { _e = null; }
    }
    event D F
    {
#nullable disable
        add => _f = null;
#nullable enable
        remove => _f = null;
    }
}";
            verify(source, expectedAnalyzedKeys: new[] { ".ctor", "add_E", "remove_F" },
                // (12,20): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //         add { _e = null; }
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(12, 20),
                // (21,24): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //         remove => _f = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(21, 24));

            static void verify(string source, string[] expectedAnalyzedKeys, params DiagnosticDescription[] expectedDiagnostics)
            {
                var comp = CreateCompilation(source);
                comp.TestOnlyCompilationData = new NullableWalker.NullableAnalysisData();
                comp.VerifyDiagnostics(expectedDiagnostics);

                AssertEx.Equal(expectedAnalyzedKeys, GetNullableDataKeysAsStrings(comp.TestOnlyCompilationData, requiredAnalysis: true));
                AssertEx.Equal(expectedAnalyzedKeys, GetIsNullableEnabledMethods(comp.TestOnlyCompilationData));
            }
        }

        [Fact]
        [WorkItem(49746, "https://github.com/dotnet/roslyn/issues/49746")]
        public void AnalyzeMethodsInEnabledContextOnly_08()
        {
            var source =
@"class A
{
}
class B
{
    public static explicit operator B(A a) => null;
}";
            verify(source, expectedAnalyzedKeys: new string[0]);

            source =
@"class A
{
}
class B
{
#nullable enable
    public static explicit operator B(A a) => null;
}";
            verify(source, expectedAnalyzedKeys: new[] { "op_Explicit" },
                // (7,47): warning CS8603: Possible null reference return.
                //     public static explicit operator B(A a) => null;
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "null").WithLocation(7, 47));

            source =
@"class C
{
    public static C operator~(C c) => null;
}";
            verify(source, expectedAnalyzedKeys: new string[0]);

            source =
@"class C
{
#nullable enable
    public static C operator~(C c) => null;
}";
            verify(source, expectedAnalyzedKeys: new[] { "op_OnesComplement" },
                // (4,39): warning CS8603: Possible null reference return.
                //     public static C operator~(C c) => null;
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "null").WithLocation(4, 39));

            static void verify(string source, string[] expectedAnalyzedKeys, params DiagnosticDescription[] expectedDiagnostics)
            {
                var comp = CreateCompilation(source);
                comp.TestOnlyCompilationData = new NullableWalker.NullableAnalysisData();
                comp.VerifyDiagnostics(expectedDiagnostics);

                AssertEx.Equal(expectedAnalyzedKeys, GetNullableDataKeysAsStrings(comp.TestOnlyCompilationData, requiredAnalysis: true));
                AssertEx.Equal(expectedAnalyzedKeys, GetIsNullableEnabledMethods(comp.TestOnlyCompilationData));
            }
        }

        [Fact]
        [WorkItem(49746, "https://github.com/dotnet/roslyn/issues/49746")]
        public void AnalyzeMethodsInEnabledContextOnly_09()
        {
            var source =
@"#nullable enable
record R;
";
            verify(source, expectedAnalyzedKeys: new string[0]);

            source =
@"#nullable enable
record R();
";
            verify(source, expectedAnalyzedKeys: new[] { "R..ctor()" });

            source =
@"record R(object P
#nullable enable
    );
";
            verify(source, expectedAnalyzedKeys: new[] { "R..ctor(System.Object P)" });

            source =
@"record A;
#nullable disable
record B0 : A
#nullable enable
{
}
#nullable disable
record B1() : A
#nullable enable
{
}
#nullable disable
record B2() : A
#nullable enable
    ();
";
            verify(source, expectedAnalyzedKeys: new[] { "B2..ctor()" });

            source =
@"record A;
#nullable disable
record B0
#nullable enable
    :
#nullable disable
    A;
#nullable disable
record B1 :
#nullable enable
    A
#nullable disable
    ;
#nullable disable
record B2() :
#nullable enable
    A
#nullable disable
    ();
";
            verify(source, expectedAnalyzedKeys: new string[0]);

            source =
@"record A(object P)
{
#nullable enable
    internal static object F(object obj) => obj;
#nullable disable
}
#nullable disable
record B1() : A(
    F(null));
record B2() : A(
#nullable enable
    F(null));
";
            verify(source, expectedAnalyzedKeys: new[] { "B2..ctor()", "System.Object A.F(System.Object obj)" },
                // (12,7): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //     F(null));
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(12, 7));

            static void verify(string source, string[] expectedAnalyzedKeys, params DiagnosticDescription[] expectedDiagnostics)
            {
                var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });
                comp.TestOnlyCompilationData = new NullableWalker.NullableAnalysisData();
                comp.VerifyDiagnostics(expectedDiagnostics);

                var actualAnalyzedKeys = GetIsNullableEnabledMethods(comp.TestOnlyCompilationData, key => ((MethodSymbol)key).ToTestDisplayString());
                AssertEx.Equal(expectedAnalyzedKeys, actualAnalyzedKeys);
            }
        }

        [Fact]
        [WorkItem(49746, "https://github.com/dotnet/roslyn/issues/49746")]
        public void AnalyzeMethodsInEnabledContextOnly_10()
        {
            var source1 =
@"partial class Program
{
#nullable enable
    partial void F1(ref object o1) { o1 = null; }
    partial void F2(ref object o2);
#nullable disable
    partial void F3(ref object o3) { o3 = null; }
    partial void F4(ref object o4);
}";
            var source2 =
@"partial class Program
{
#nullable disable
    partial void F1(ref object o1);
    partial void F2(ref object o2) { o2 = null; }
#nullable enable
    partial void F3(ref object o3);
    partial void F4(ref object o4) { o4 = null; }
}";

            var comp = CreateCompilation(new[] { source1, source2 });
            comp.TestOnlyCompilationData = new NullableWalker.NullableAnalysisData();
            comp.VerifyDiagnostics(
                // (4,43): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //     partial void F1(ref object o1) { o1 = null; }
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(4, 43),
                // (8,43): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //     partial void F4(ref object o4) { o4 = null; }
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(8, 43));

            var expectedAnalyzedKeys = new[] { "F1", "F4" };
            AssertEx.Equal(expectedAnalyzedKeys, GetNullableDataKeysAsStrings(comp.TestOnlyCompilationData, requiredAnalysis: true));
            AssertEx.Equal(expectedAnalyzedKeys, GetIsNullableEnabledMethods(comp.TestOnlyCompilationData));
        }

        [Fact]
        [WorkItem(49746, "https://github.com/dotnet/roslyn/issues/49746")]
        public void AnalyzeMethodsInEnabledContextOnly_11()
        {
            var source =
@"partial class Program
{
#nullable enable
    partial void F1(object x = null);
    partial void F2(object y = null) { }
#nullable disable
    partial void F1(object x = null) { }
    partial void F2(object y = null);
}";

            var comp = CreateCompilation(source);
            comp.TestOnlyCompilationData = new NullableWalker.NullableAnalysisData();
            comp.VerifyDiagnostics(
                // (4,32): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //     partial void F1(object x = null);
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(4, 32),
                // (5,28): warning CS1066: The default value specified for parameter 'y' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //     partial void F2(object y = null) { }
                Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "y").WithArguments("y").WithLocation(5, 28),
                // (5,32): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //     partial void F2(object y = null) { }
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(5, 32),
                // (7,28): warning CS1066: The default value specified for parameter 'x' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //     partial void F1(object x = null) { }
                Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "x").WithArguments("x").WithLocation(7, 28));

            var actualAnalyzedKeys = GetNullableDataKeysAsStrings(comp.TestOnlyCompilationData, requiredAnalysis: true);
            AssertEx.Equal(new[] { "= null", "= null", "F2" }, actualAnalyzedKeys);
        }

        [ConditionalFact(typeof(NoUsedAssembliesValidation), Reason = "GetEmitDiagnostics affects result")]
        [WorkItem(49746, "https://github.com/dotnet/roslyn/issues/49746")]
        public void AnalyzeMethodsInEnabledContextOnly_12()
        {
            var source =
@"using System.Runtime.InteropServices;
class Program
{
#nullable enable
    const object? C1 = null;
    const object? C2 = null;
    const object? C3 = null;
    const object? C4 = null;
#nullable disable
    static void F1(
        [DefaultParameterValue(
#nullable enable
        C1
#nullable disable
        )]
        [A] object x)
    {
    }
    static void F2(
#nullable enable
        [DefaultParameterValue(C2)]
#nullable disable
        [A] object x)
    {
    }
#nullable disable
    static void F3(
        [DefaultParameterValue(C3)]
#nullable enable
        [A] object x
#nullable disable
        )
    {
    }
    static void F4(
        [DefaultParameterValue(C4)]
        [A] object x)
#nullable enable
#nullable disable
    {
    }
}
class A : System.Attribute
{
}";

            var comp = CreateCompilation(source);
            comp.TestOnlyCompilationData = new NullableWalker.NullableAnalysisData();
            comp.VerifyDiagnostics();

            var actualAnalyzedKeys = GetNullableDataKeysAsStrings(comp.TestOnlyCompilationData, requiredAnalysis: true);
            var expectedAnalyzedKeys = new[]
            {
                ".cctor",
                @"[DefaultParameterValue(
#nullable enable
        C1
#nullable disable
        )]
        [A] object x",
                @"[DefaultParameterValue(C2)]
#nullable disable
        [A] object x",
                @"[DefaultParameterValue(C3)]
#nullable enable
        [A] object x",
                "A",
                @"DefaultParameterValue(
#nullable enable
        C1
#nullable disable
        )",
                "DefaultParameterValue(C2)",
                "F1",
                "F2",
                "F3",
                "F4",
            };
            AssertEx.Equal(expectedAnalyzedKeys, actualAnalyzedKeys);
        }

        [Fact]
        [WorkItem(49746, "https://github.com/dotnet/roslyn/issues/49746")]
        public void AnalyzeMethodsInEnabledContextOnly_13()
        {
            var source =
@"#nullable disable
object x = typeof(string);
if (x == null) { }
_ = x.ToString();
#nullable enable
";
            verify(new[] { source }, projectContext: null, expectedAnalyzedKeys: new string[0]);

            source =
@"#nullable disable
object x = typeof(string);
if (x == null) { }
#nullable enable
_ = x.ToString();
";
            verify(new[] { source }, projectContext: null, expectedAnalyzedKeys: new[] { "<Main>$" },
                // (5,5): warning CS8602: Dereference of a possibly null reference.
                // _ = x.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(5, 5));

            source =
@"#nullable disable
object x = typeof(string);
if (x == null) { }
#nullable restore
_ = x.ToString();
";
            verify(new[] { source }, projectContext: NullableContextOptions.Warnings, expectedAnalyzedKeys: new[] { "<Main>$" },
                // (5,5): warning CS8602: Dereference of a possibly null reference.
                // _ = x.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(5, 5));

            source =
@"object x = typeof(string);
if (x == null) { }
#nullable enable
#nullable disable
_ = x.ToString();
";
            verify(new[] { source }, projectContext: null, expectedAnalyzedKeys: new string[0]);

            source =
@"object x = A.F();
if (x == null) { }
_ = x.ToString();
static class A
{
#nullable enable
    internal static object F() => new object();
}";
            verify(new[] { source }, projectContext: null, expectedAnalyzedKeys: new[] { "F" });

            source =
@"object x = typeof(A);
class A
{
#nullable enable
#nullable disable
}
if (x == null) { }
_ = x.ToString();
";
            verify(new[] { source }, projectContext: null, expectedAnalyzedKeys: new string[0],
                // (7,1): error CS8803: Top-level statements must precede namespace and type declarations.
                // if (x == null) { }
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "if (x == null) { }").WithLocation(7, 1));

            source =
@"#nullable enable
object x = A.F();
#nullable disable
static class A
{
    internal static object F() => new object();
}
if (x == null) { }
_ = x.ToString();
";
            verify(new[] { source }, projectContext: null, expectedAnalyzedKeys: new[] { "<Main>$" },
                // (8,1): error CS8803: Top-level statements must precede namespace and type declarations.
                // if (x == null) { }
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "if (x == null) { }").WithLocation(8, 1));

            source =
@"object x = F();
if (x == null) { }
_ = x.ToString();
static object F()
{
#nullable enable
    return new object();
#nullable disable
}";
            verify(new[] { source }, projectContext: null, expectedAnalyzedKeys: new[] { "<Main>$" });

            var sourceA =
@"static class A
{
#nullable enable
    internal static object F() => new object();
}";
            var sourceB =
@"object x = A.F();
if (x == null) { }
_ = x.ToString();
";
            verify(new[] { sourceA, sourceB }, projectContext: null, expectedAnalyzedKeys: new[] { "F" });

            static void verify(string[] source, NullableContextOptions? projectContext, string[] expectedAnalyzedKeys, params DiagnosticDescription[] expectedDiagnostics)
            {
                var options = TestOptions.ReleaseExe;
                if (projectContext != null) options = options.WithNullableContextOptions(projectContext.GetValueOrDefault());
                var comp = CreateCompilation(source, options: options);
                comp.TestOnlyCompilationData = new NullableWalker.NullableAnalysisData();
                comp.VerifyDiagnostics(expectedDiagnostics);

                AssertEx.Equal(expectedAnalyzedKeys, GetNullableDataKeysAsStrings(comp.TestOnlyCompilationData, requiredAnalysis: true));
                AssertEx.Equal(expectedAnalyzedKeys, GetIsNullableEnabledMethods(comp.TestOnlyCompilationData));
            }
        }

        [Fact]
        [WorkItem(49746, "https://github.com/dotnet/roslyn/issues/49746")]
        public void AnalyzeMethodsInEnabledContextOnly_MethodBodySemanticModel_01()
        {
            var source =
@"class Program
{
#nullable enable
    static object F1(object o1)
    {
        if (o1 == null) { }
        return o1;
    }
#nullable disable
    static object F2(object o2)
    {
        if (o2 == null) { }
        return o2;
    }
}";

            var comp = CreateCompilation(source);
            comp.TestOnlyCompilationData = new NullableWalker.NullableAnalysisData();
            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);
            var returnStatements = syntaxTree.GetRoot().DescendantNodes().OfType<ReturnStatementSyntax>().ToArray();

            var syntax = returnStatements[0];
            Assert.Equal("return o1;", syntax.ToString());
            var typeInfo = model.GetTypeInfo(syntax.Expression);
            Assert.Equal(Microsoft.CodeAnalysis.NullableFlowState.MaybeNull, typeInfo.Nullability.FlowState);

            syntax = returnStatements[1];
            Assert.Equal("return o2;", syntax.ToString());
            typeInfo = model.GetTypeInfo(syntax.Expression);
            Assert.Equal(Microsoft.CodeAnalysis.NullableFlowState.None, typeInfo.Nullability.FlowState);

            var expectedAnalyzedKeys = new[] { "F1" };
            AssertEx.Equal(expectedAnalyzedKeys, GetNullableDataKeysAsStrings(comp.TestOnlyCompilationData, requiredAnalysis: true));
            AssertEx.Equal(expectedAnalyzedKeys, GetIsNullableEnabledMethods(comp.TestOnlyCompilationData));
        }

        [Fact]
        [WorkItem(49746, "https://github.com/dotnet/roslyn/issues/49746")]
        public void AnalyzeMethodsInEnabledContextOnly_MethodBodySemanticModel_02()
        {
            var source =
@"class Program
{
#nullable enable
    object F;
    Program(object obj)
#nullable disable
    {
        if (obj == null) { }
        F = obj;
    }
}";
            verify(source, Microsoft.CodeAnalysis.NullableFlowState.MaybeNull, ".ctor");

            source =
@"class Program
{
#nullable enable
    object F;
#nullable disable
    Program(object obj)
    {
        if (obj == null) { }
        F = obj;
    }
}";
            verify(source, Microsoft.CodeAnalysis.NullableFlowState.MaybeNull, ".ctor");

            source =
@"class Program
{
#nullable enable
    object F = new object();
#nullable disable
    Program(object obj)
    {
        if (obj == null) { }
        F = obj;
    }
}";
            verify(source, Microsoft.CodeAnalysis.NullableFlowState.MaybeNull, ".ctor");

            static void verify(string source, Microsoft.CodeAnalysis.NullableFlowState expectedFlowState, params string[] expectedAnalyzedKeys)
            {
                var comp = CreateCompilation(source);
                comp.TestOnlyCompilationData = new NullableWalker.NullableAnalysisData();

                var syntaxTree = comp.SyntaxTrees[0];
                var model = comp.GetSemanticModel(syntaxTree);
                var syntax = syntaxTree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().Single().Right;
                Assert.Equal("obj", syntax.ToString());
                var typeInfo = model.GetTypeInfo(syntax);
                Assert.Equal(expectedFlowState, typeInfo.Nullability.FlowState);

                AssertEx.Equal(expectedAnalyzedKeys, GetNullableDataKeysAsStrings(comp.TestOnlyCompilationData, requiredAnalysis: true));
                AssertEx.Equal(expectedAnalyzedKeys, GetIsNullableEnabledMethods(comp.TestOnlyCompilationData));
            }
        }

        [Fact]
        [WorkItem(49746, "https://github.com/dotnet/roslyn/issues/49746")]
        public void AnalyzeMethodsInEnabledContextOnly_AttributeSemanticModel_01()
        {
            var source =
@"class A : System.Attribute
{
#nullable enable
    public A(object obj) { }
    public static object F1;
    public static object F2;
}
#nullable enable
[A(A.F1 = null)]
class B1
{
}
#nullable disable
[A(A.F2 = null)]
class B2
{
}";

            var comp = CreateCompilation(source);
            comp.TestOnlyCompilationData = new NullableWalker.NullableAnalysisData();
            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);
            var attributeArguments = syntaxTree.GetRoot().DescendantNodes().OfType<AttributeArgumentSyntax>().ToArray();

            verify(attributeArguments[0], "A.F1 = null", Microsoft.CodeAnalysis.NullableFlowState.MaybeNull);
            verify(attributeArguments[1], "A.F2 = null", Microsoft.CodeAnalysis.NullableFlowState.None);

            var actualAnalyzedKeys = GetNullableDataKeysAsStrings(comp.TestOnlyCompilationData, requiredAnalysis: true);
            AssertEx.Equal(new[] { "A(A.F1 = null)" }, actualAnalyzedKeys);

            void verify(AttributeArgumentSyntax syntax, string expectedText, Microsoft.CodeAnalysis.NullableFlowState expectedFlowState)
            {
                Assert.Equal(expectedText, syntax.ToString());
                var typeInfo = model.GetTypeInfo(syntax.Expression);
                Assert.Equal(expectedFlowState, typeInfo.Nullability.FlowState);
            }
        }

        [Fact]
        [WorkItem(49746, "https://github.com/dotnet/roslyn/issues/49746")]
        public void AnalyzeMethodsInEnabledContextOnly_AttributeSemanticModel_02()
        {
            var source =
@"using System.Runtime.InteropServices;
class Program
{
#nullable enable
    const object? C1 = null;
    const object? C2 = null;
#nullable disable
    static void F1([DefaultParameterValue(
#nullable enable
        C1
#nullable disable
        )] object x)
    {
    }
#nullable disable
    static void F2([DefaultParameterValue(C2)]
#nullable enable
        object x
#nullable disable
        )
    {
    }
}";

            var comp = CreateCompilation(source);
            comp.TestOnlyCompilationData = new NullableWalker.NullableAnalysisData();
            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);
            var attributeArguments = syntaxTree.GetRoot().DescendantNodes().OfType<AttributeArgumentSyntax>().ToArray();

            verify(attributeArguments[0], "C1", Microsoft.CodeAnalysis.NullableFlowState.MaybeNull);
            verify(attributeArguments[1], "C2", Microsoft.CodeAnalysis.NullableFlowState.None);

            var actualAnalyzedKeys = GetNullableDataKeysAsStrings(comp.TestOnlyCompilationData, requiredAnalysis: true);
            var expectedAnalyzedKey =
@"DefaultParameterValue(
#nullable enable
        C1
#nullable disable
        )";
            AssertEx.Equal(new[] { expectedAnalyzedKey }, actualAnalyzedKeys);

            void verify(AttributeArgumentSyntax syntax, string expectedText, Microsoft.CodeAnalysis.NullableFlowState expectedFlowState)
            {
                Assert.Equal(expectedText, syntax.ToString());
                var typeInfo = model.GetTypeInfo(syntax.Expression);
                Assert.Equal(expectedFlowState, typeInfo.Nullability.FlowState);
            }
        }

        [Fact]
        [WorkItem(49746, "https://github.com/dotnet/roslyn/issues/49746")]
        public void AnalyzeMethodsInEnabledContextOnly_InitializerSemanticModel_01()
        {
            var source =
@"class Program
{
#nullable enable
    static object F1;
    static object F2;
#nullable enable
    static void M1(object o1 = (F1 = null)) { }
#nullable disable
    static void M2(object o2 = (F2 = null)) { }
}";

            var comp = CreateCompilation(source);
            comp.TestOnlyCompilationData = new NullableWalker.NullableAnalysisData();
            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);
            var equalsValueClauses = syntaxTree.GetRoot().DescendantNodes().OfType<EqualsValueClauseSyntax>().ToArray();

            verify(equalsValueClauses[0], "(F1 = null)", Microsoft.CodeAnalysis.NullableFlowState.MaybeNull);
            verify(equalsValueClauses[1], "(F2 = null)", Microsoft.CodeAnalysis.NullableFlowState.None);

            var actualAnalyzedKeys = GetNullableDataKeysAsStrings(comp.TestOnlyCompilationData, requiredAnalysis: true);
            AssertEx.Equal(new[] { "o1" }, actualAnalyzedKeys);

            void verify(EqualsValueClauseSyntax syntax, string expectedText, Microsoft.CodeAnalysis.NullableFlowState expectedFlowState)
            {
                var value = syntax.Value;
                Assert.Equal(expectedText, value.ToString());
                var typeInfo = model.GetTypeInfo(value);
                Assert.Equal(expectedFlowState, typeInfo.Nullability.FlowState);
            }
        }

        [Fact]
        [WorkItem(49746, "https://github.com/dotnet/roslyn/issues/49746")]
        public void AnalyzeMethodsInEnabledContextOnly_InitializerSemanticModel_02()
        {
            var source =
@"#pragma warning disable 414
class A
{
#nullable disable
    object F1 = null;
}
class B
{
#nullable disable
    object F2 = null;
#nullable enable
    object F3 = null;
}";

            var comp = CreateCompilation(source);
            comp.TestOnlyCompilationData = new NullableWalker.NullableAnalysisData();
            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);
            var declarations = syntaxTree.GetRoot().DescendantNodes().OfType<FieldDeclarationSyntax>().Select(f => f.Declaration.Variables[0]).ToArray();

            verify(declarations[0], "F1", Microsoft.CodeAnalysis.NullableFlowState.None);
            verify(declarations[1], "F2", Microsoft.CodeAnalysis.NullableFlowState.MaybeNull);
            verify(declarations[2], "F3", Microsoft.CodeAnalysis.NullableFlowState.MaybeNull);

            var actualAnalyzedKeys = GetNullableDataKeysAsStrings(comp.TestOnlyCompilationData, requiredAnalysis: true);
            AssertEx.Equal(new[] { "F2", "F3" }, actualAnalyzedKeys);

            void verify(VariableDeclaratorSyntax syntax, string expectedText, Microsoft.CodeAnalysis.NullableFlowState expectedFlowState)
            {
                Assert.Equal(expectedText, syntax.Identifier.ValueText);
                var typeInfo = model.GetTypeInfo(syntax.Initializer.Value);
                Assert.Equal(expectedFlowState, typeInfo.Nullability.FlowState);
            }
        }

        [Fact]
        [WorkItem(49746, "https://github.com/dotnet/roslyn/issues/49746")]
        public void AnalyzeMethodsInEnabledContextOnly_InitializerSemanticModel_03()
        {
            var source =
@"class A
{
#nullable disable
    object P1 { get; set; } = null;
}
class B
{
#nullable disable
    object P2 { get; set; } = null;
#nullable enable
    object P3 { get; set; } = null;
}";

            var comp = CreateCompilation(source);
            comp.TestOnlyCompilationData = new NullableWalker.NullableAnalysisData();
            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);
            var declarations = syntaxTree.GetRoot().DescendantNodes().OfType<PropertyDeclarationSyntax>().ToArray();

            verify(declarations[0], "P1", Microsoft.CodeAnalysis.NullableFlowState.None);
            verify(declarations[1], "P2", Microsoft.CodeAnalysis.NullableFlowState.MaybeNull);
            verify(declarations[2], "P3", Microsoft.CodeAnalysis.NullableFlowState.MaybeNull);

            var actualAnalyzedKeys = GetNullableDataKeysAsStrings(comp.TestOnlyCompilationData, requiredAnalysis: true);
            AssertEx.Equal(new[] { "P2", "P3" }, actualAnalyzedKeys);

            void verify(PropertyDeclarationSyntax syntax, string expectedText, Microsoft.CodeAnalysis.NullableFlowState expectedFlowState)
            {
                Assert.Equal(expectedText, syntax.Identifier.ValueText);
                var typeInfo = model.GetTypeInfo(syntax.Initializer.Value);
                Assert.Equal(expectedFlowState, typeInfo.Nullability.FlowState);
            }
        }

        [Fact]
        [WorkItem(49746, "https://github.com/dotnet/roslyn/issues/49746")]
        public void AnalyzeMethodsInEnabledContextOnly_SpeculativeSemanticModel_MethodBody()
        {
            var source =
@"class Program
{
#nullable disable
    static void Main()
    {
        object obj = typeof(object);
    }
#nullable enable
}";
            VerifySpeculativeSemanticModel(source, null, "string", Microsoft.CodeAnalysis.NullableAnnotation.None);

            source =
@"class Program
{
#nullable disable
    static void Main()
    {
        object obj =
#nullable enable
            typeof(object);
    }
}";
            VerifySpeculativeSemanticModel(source, null, "string", Microsoft.CodeAnalysis.NullableAnnotation.NotAnnotated);
        }

        [Fact]
        [WorkItem(49746, "https://github.com/dotnet/roslyn/issues/49746")]
        public void AnalyzeMethodsInEnabledContextOnly_SpeculativeSemanticModel_Initializer()
        {
            var source =
@"class Program
{
#nullable disable
    static object F = typeof(object);
#nullable enable
}";
            VerifySpeculativeSemanticModel(source, null, "string", Microsoft.CodeAnalysis.NullableAnnotation.None);

            source =
@"class Program
{
#nullable disable
    static object F =
#nullable enable
        typeof(object);
}";
            VerifySpeculativeSemanticModel(source, null, "string", Microsoft.CodeAnalysis.NullableAnnotation.NotAnnotated);

            source =
@"class Program
{
#nullable disable
    static object P { get; } = typeof(object);
#nullable enable
}";
            VerifySpeculativeSemanticModel(source, null, "string", Microsoft.CodeAnalysis.NullableAnnotation.None);

            source =
@"class Program
{
#nullable disable
    static object P { get; } =
#nullable enable
        typeof(object);
}";
            VerifySpeculativeSemanticModel(source, null, "string", Microsoft.CodeAnalysis.NullableAnnotation.NotAnnotated);
        }

        [Fact]
        [WorkItem(49746, "https://github.com/dotnet/roslyn/issues/49746")]
        public void AnalyzeMethodsInEnabledContextOnly_SpeculativeSemanticModel_Attribute()
        {
            var source =
@"class A : System.Attribute
{
    internal A(object obj) { }
}
class Program
{
#nullable disable
    [A(typeof(object)]
    static void Main()
    {
    }
#nullable enable
}";
            VerifySpeculativeSemanticModel(source, null, "string", Microsoft.CodeAnalysis.NullableAnnotation.None);

            source =
@"class A : System.Attribute
{
    internal A(object obj) { }
}
class Program
{
#nullable disable
    [A(
#nullable enable
        typeof(object)]
    static void Main()
    {
    }
}";
            VerifySpeculativeSemanticModel(source, null, "string", Microsoft.CodeAnalysis.NullableAnnotation.NotAnnotated);
        }

        [Theory]
        [MemberData(nameof(AnalyzeMethodsInEnabledContextOnly_01_Data1))]
        [WorkItem(49746, "https://github.com/dotnet/roslyn/issues/49746")]
        public void AnalyzeMethodsInEnabledContextOnly_SpeculativeSemanticModel_A(NullableContextOptions? projectContext, NullableDirectives sourceDirectives, NullableDirectives speculativeDirectives)
        {
            AnalyzeMethodsInEnabledContextOnly_SpeculativeSemanticModel_Execute(projectContext, sourceDirectives, speculativeDirectives);
        }

        [Theory]
        [MemberData(nameof(AnalyzeMethodsInEnabledContextOnly_01_Data2))]
        [WorkItem(49746, "https://github.com/dotnet/roslyn/issues/49746")]
        public void AnalyzeMethodsInEnabledContextOnly_SpeculativeSemanticModel_B(NullableContextOptions? projectContext, NullableDirectives sourceDirectives, NullableDirectives speculativeDirectives)
        {
            AnalyzeMethodsInEnabledContextOnly_SpeculativeSemanticModel_Execute(projectContext, sourceDirectives, speculativeDirectives);
        }

        private static void AnalyzeMethodsInEnabledContextOnly_SpeculativeSemanticModel_Execute(NullableContextOptions? projectContext, NullableDirectives sourceDirectives, NullableDirectives speculativeDirectives)
        {
            // https://github.com/dotnet/roslyn/issues/50234: SyntaxTreeSemanticModel.IsNullableAnalysisEnabledAtSpeculativePosition()
            // does not handle '#nullable restore'.
            if (speculativeDirectives.ExpectedWarningsState == NullableContextState.State.ExplicitlyRestored) return;

            var source =
$@"class Program
{{
{sourceDirectives}
    static void Main()
    {{
        object obj = typeof(object);
    }}
}}";
            var typeName =
$@"{speculativeDirectives}
string";

            var expectedWarningsState = CombineState(speculativeDirectives.ExpectedWarningsState, sourceDirectives.ExpectedWarningsState);

            bool isNullableEnabledForProject = projectContext != null && (projectContext.Value & NullableContextOptions.Warnings) != 0;
            Microsoft.CodeAnalysis.NullableAnnotation expectedAnnotation = IsNullableEnabled(expectedWarningsState, isNullableEnabledForProject) ?
                Microsoft.CodeAnalysis.NullableAnnotation.NotAnnotated :
                Microsoft.CodeAnalysis.NullableAnnotation.None;

            VerifySpeculativeSemanticModel(source, projectContext, typeName, expectedAnnotation);
        }

        private static void VerifySpeculativeSemanticModel(string source, NullableContextOptions? projectContext, string typeName, Microsoft.CodeAnalysis.NullableAnnotation expectedAnnotation)
        {
            var options = TestOptions.ReleaseDll;
            if (projectContext != null) options = options.WithNullableContextOptions(projectContext.GetValueOrDefault());
            var comp = CreateCompilation(source, options: options);
            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);
            var typeOf = syntaxTree.GetRoot().DescendantNodes().OfType<TypeOfExpressionSyntax>().Single();
            var type = SyntaxFactory.ParseTypeName(typeName);
            Assert.True(model.TryGetSpeculativeSemanticModel(typeOf.Type.SpanStart, type, out model, SpeculativeBindingOption.BindAsTypeOrNamespace));
            var typeInfo = model.GetTypeInfo(type);
            Assert.Equal(expectedAnnotation, typeInfo.Nullability.Annotation);
        }

        private static string[] GetNullableDataKeysAsStrings(object compilationData, bool requiredAnalysis = false)
        {
            return ((NullableWalker.NullableAnalysisData)compilationData).Data.
                Where(pair => !requiredAnalysis || pair.Value.RequiredAnalysis).
                Select(pair => GetNullableDataKeyAsString(pair.Key)).
                OrderBy(key => key).
                ToArray();
        }

        private static string[] GetIsNullableEnabledMethods(object compilationData, Func<object, string> toString = null)
        {
            toString ??= GetNullableDataKeyAsString;
            return ((NullableWalker.NullableAnalysisData)compilationData).Data.
                Where(pair => pair.Value.RequiredAnalysis && pair.Key is MethodSymbol method && method.IsNullableAnalysisEnabled()).
                Select(pair => toString(pair.Key)).
                OrderBy(key => key).
                ToArray();
        }

        private static string GetNullableDataKeyAsString(object key) =>
            key is Symbol symbol ? symbol.MetadataName : ((SyntaxNode)key).ToString();
    }
}
