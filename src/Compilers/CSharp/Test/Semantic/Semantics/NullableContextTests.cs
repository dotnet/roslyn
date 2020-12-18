// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
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

            var comp = CreateCompilation(source, options: WithNonNullTypes(globalContext));
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

            var comp = CreateCompilation(new[] { source1, source2 }, options: WithNonNullTypesTrue());

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
            AssertEnabledForInheritence(NullableContext.Disabled, warningsEnabled: false, annotationsEnabled: false);
            AssertEnabledForInheritence(NullableContext.WarningsEnabled, warningsEnabled: true, annotationsEnabled: false);
            AssertEnabledForInheritence(NullableContext.AnnotationsEnabled, warningsEnabled: false, annotationsEnabled: true);
            AssertEnabledForInheritence(NullableContext.Enabled, warningsEnabled: true, annotationsEnabled: true);

            void AssertEnabledForInheritence(NullableContext context, bool warningsEnabled, bool annotationsEnabled)
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
        [Fact]
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

            verify(parseOptions: TestOptions.Regular, expectedAnalyzedKeysDefault);
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", null), expectedAnalyzedKeysDefault);
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", "always"), expectedAnalyzedKeysAll);
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", "never"));
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", "ALWAYS"), expectedAnalyzedKeysDefault); // unrecognized value (incorrect case) ignored
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", "NEVER"), expectedAnalyzedKeysDefault); // unrecognized value (incorrect case) ignored
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", "true"), expectedAnalyzedKeysDefault); // unrecognized value ignored
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", "false"), expectedAnalyzedKeysDefault); // unrecognized value ignored
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", "unknown"), expectedAnalyzedKeysDefault); // unrecognized value ignored

            void verify(CSharpParseOptions parseOptions, params string[] expectedAnalyzedKeys)
            {
                var comp = CreateCompilation(source, parseOptions: parseOptions);
                comp.NullableAnalysisData = new ConcurrentDictionary<object, NullableWalker.Data>();
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

                var actualAnalyzedKeys = GetNullableDataKeysAsStrings(comp.NullableAnalysisData);
                AssertEx.Equal(expectedAnalyzedKeys, actualAnalyzedKeys);
            }
        }

        [Fact]
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
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", null), expectedAnalyzedKeysDefault);
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", "always"), expectedAnalyzedKeysAll);
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", "never"));

            void verify(CSharpParseOptions parseOptions, params string[] expectedAnalyzedKeys)
            {
                var comp = CreateCompilation(source, parseOptions: parseOptions);
                comp.NullableAnalysisData = new ConcurrentDictionary<object, NullableWalker.Data>();
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

                var actualAnalyzedKeys = GetNullableDataKeysAsStrings(comp.NullableAnalysisData);
                AssertEx.Equal(expectedAnalyzedKeys, actualAnalyzedKeys);
            }
        }

        [Fact]
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
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", null), expectedAnalyzedKeysDefault);
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", "always"), expectedAnalyzedKeysAll);
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", "never"));

            void verify(CSharpParseOptions parseOptions, params string[] expectedAnalyzedKeys)
            {
                var comp = CreateCompilation(sourceB, references: new[] { refA }, parseOptions: parseOptions);
                comp.NullableAnalysisData = new ConcurrentDictionary<object, NullableWalker.Data>();
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

                var actualAnalyzedKeys = GetNullableDataKeysAsStrings(comp.NullableAnalysisData);
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
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", null), expectedFlowState: true, expectedAnalyzedKeysAll);
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", "always"), expectedFlowState: true, expectedAnalyzedKeysAll);
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", "never"), expectedFlowState: false);

            void verify(CSharpParseOptions parseOptions, bool expectedFlowState, params string[] expectedAnalyzedKeys)
            {
                var comp = CreateCompilation(source, parseOptions: parseOptions);
                comp.NullableAnalysisData = new ConcurrentDictionary<object, NullableWalker.Data>();
                var syntaxTree = comp.SyntaxTrees[0];
                var model = comp.GetSemanticModel(syntaxTree);
                var syntax = syntaxTree.GetRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Skip(1).Single();
                Assert.Equal("return obj;", syntax.ToString());
                var typeInfo = model.GetTypeInfo(syntax.Expression);
                var expectedNullability = expectedFlowState ? Microsoft.CodeAnalysis.NullableFlowState.NotNull : Microsoft.CodeAnalysis.NullableFlowState.None;
                Assert.Equal(expectedNullability, typeInfo.Nullability.FlowState);

                var actualAnalyzedKeys = GetNullableDataKeysAsStrings(comp.NullableAnalysisData);
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
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", null), expectedFlowState: true, expectedAnalyzedKeysAll);
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", "always"), expectedFlowState: true, expectedAnalyzedKeysAll);
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", "never"), expectedFlowState: false);

            void verify(CSharpParseOptions parseOptions, bool expectedFlowState, params string[] expectedAnalyzedKeys)
            {
                var comp = CreateCompilation(source, parseOptions: parseOptions);
                comp.NullableAnalysisData = new ConcurrentDictionary<object, NullableWalker.Data>();
                var syntaxTree = comp.SyntaxTrees[0];
                var model = comp.GetSemanticModel(syntaxTree);
                var syntax = syntaxTree.GetRoot().DescendantNodes().OfType<AttributeArgumentSyntax>().Single();
                Assert.Equal("A.F = null", syntax.ToString());
                var typeInfo = model.GetTypeInfo(syntax.Expression);
                var expectedNullability = expectedFlowState ? Microsoft.CodeAnalysis.NullableFlowState.MaybeNull : Microsoft.CodeAnalysis.NullableFlowState.None;
                Assert.Equal(expectedNullability, typeInfo.Nullability.FlowState);

                var actualAnalyzedKeys = GetNullableDataKeysAsStrings(comp.NullableAnalysisData);
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
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", null), expectedFlowState: true, expectedAnalyzedKeysAll);
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", "always"), expectedFlowState: true, expectedAnalyzedKeysAll);
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", "never"), expectedFlowState: false);

            void verify(CSharpParseOptions parseOptions, bool expectedFlowState, params string[] expectedAnalyzedKeys)
            {
                var comp = CreateCompilation(source, parseOptions: parseOptions);
                comp.NullableAnalysisData = new ConcurrentDictionary<object, NullableWalker.Data>();
                var syntaxTree = comp.SyntaxTrees[0];
                var model = comp.GetSemanticModel(syntaxTree);
                var syntax = syntaxTree.GetRoot().DescendantNodes().OfType<EqualsValueClauseSyntax>().First().Value;
                Assert.Equal("(F = null)", syntax.ToString());
                var typeInfo = model.GetTypeInfo(syntax);
                var expectedNullability = expectedFlowState ? Microsoft.CodeAnalysis.NullableFlowState.MaybeNull : Microsoft.CodeAnalysis.NullableFlowState.None;
                Assert.Equal(expectedNullability, typeInfo.Nullability.FlowState);

                var actualAnalyzedKeys = GetNullableDataKeysAsStrings(comp.NullableAnalysisData);
                AssertEx.Equal(expectedAnalyzedKeys, actualAnalyzedKeys);
            }
        }

        private readonly struct NullableDirectives
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
        }

        [Fact]
        [WorkItem(49746, "https://github.com/dotnet/roslyn/issues/49746")]
        public void AnalyzeMethodsInEnabledContextOnly_01()
        {
            var projectSettings = new[]
            {
                (NullableContextOptions?)null,
                NullableContextOptions.Disable,
                NullableContextOptions.Warnings,
                NullableContextOptions.Annotations,
                NullableContextOptions.Enable,
            };
            var nullableDirectives = new[]
            {
                new NullableDirectives(new string[0], NullableContextState.State.Unknown, NullableContextState.State.Unknown),
                new NullableDirectives(new[] { "#nullable disable" }, NullableContextState.State.Disabled, NullableContextState.State.Disabled),
                new NullableDirectives(new[] { "#nullable enable" }, NullableContextState.State.Enabled, NullableContextState.State.Enabled),
                new NullableDirectives(new[] { "#nullable restore" }, NullableContextState.State.ExplicitlyRestored, NullableContextState.State.ExplicitlyRestored),
                new NullableDirectives(new[] { "#nullable disable annotations" }, NullableContextState.State.Unknown, NullableContextState.State.Disabled),
                new NullableDirectives(new[] { "#nullable disable annotations", "#nullable enable warnings" }, NullableContextState.State.Enabled, NullableContextState.State.Disabled),
                new NullableDirectives(new[] { "#nullable disable annotations", "#nullable restore warnings" }, NullableContextState.State.ExplicitlyRestored, NullableContextState.State.Disabled),
                new NullableDirectives(new[] { "#nullable enable annotations" }, NullableContextState.State.Unknown, NullableContextState.State.Enabled),
                new NullableDirectives(new[] { "#nullable enable annotations", "#nullable disable warnings" }, NullableContextState.State.Disabled, NullableContextState.State.Enabled),
                new NullableDirectives(new[] { "#nullable enable annotations", "#nullable restore warnings" }, NullableContextState.State.ExplicitlyRestored, NullableContextState.State.Enabled),
                new NullableDirectives(new[] { "#nullable restore annotations" }, NullableContextState.State.Unknown, NullableContextState.State.ExplicitlyRestored),
                new NullableDirectives(new[] { "#nullable restore annotations", "#nullable enable warnings" }, NullableContextState.State.Enabled, NullableContextState.State.ExplicitlyRestored),
                new NullableDirectives(new[] { "#nullable restore annotations", "#nullable disable warnings" }, NullableContextState.State.Disabled, NullableContextState.State.ExplicitlyRestored),
                new NullableDirectives(new[] { "#nullable disable warnings" }, NullableContextState.State.Disabled, NullableContextState.State.Unknown),
                new NullableDirectives(new[] { "#nullable enable warnings" }, NullableContextState.State.Enabled, NullableContextState.State.Unknown),
                new NullableDirectives(new[] { "#nullable restore warnings" }, NullableContextState.State.ExplicitlyRestored, NullableContextState.State.Unknown),
           };

            var source =
@"#nullable enable
public class A
{
    public static void M(object obj) { }
}";
            var refA = CreateCompilation(source).EmitToImageReference();

            foreach (var projectSetting in projectSettings)
            {
                foreach (var classDirectives in nullableDirectives)
                {
                    foreach (var methodDirectives in nullableDirectives)
                    {
                        analyzeMethods(refA, projectSetting, classDirectives, methodDirectives);
                    }
                }
            }

            static void analyzeMethods(MetadataReference refA, NullableContextOptions? projectContext, NullableDirectives classDirectives, NullableDirectives methodDirectives)
            {
                var sourceBuilder = new StringBuilder();
                foreach (var str in classDirectives.Directives) sourceBuilder.AppendLine(str);
                sourceBuilder.AppendLine("static class B");
                sourceBuilder.AppendLine("{");
                foreach (var str in methodDirectives.Directives) sourceBuilder.AppendLine(str);
                sourceBuilder.AppendLine("    static void Main() { A.M(null); }");
                sourceBuilder.AppendLine("}");
                var source = sourceBuilder.ToString();

                var expectedWarningsStateForMethod = combineState(methodDirectives.ExpectedWarningsState, classDirectives.ExpectedWarningsState);
                var expectedAnnotationsStateForMethod = combineState(methodDirectives.ExpectedAnnotationsState, classDirectives.ExpectedAnnotationsState);

                bool isNullableEnabledForProject = projectContext != null && (projectContext.Value & NullableContextOptions.Warnings) != 0;
                bool isNullableEnabledForClass = isNullableEnabled(classDirectives.ExpectedWarningsState, isNullableEnabledForProject);
                bool isNullableEnabledForMethod = isNullableEnabled(expectedWarningsStateForMethod, isNullableEnabledForProject);

                var options = TestOptions.ReleaseDll;
                if (projectContext != null) options = options.WithNullableContextOptions(projectContext.Value);
                var comp = CreateCompilation(source, options: options, references: new[] { refA });
                comp.NullableAnalysisData = new ConcurrentDictionary<object, NullableWalker.Data>();

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

                var actualAnalyzedKeys = GetNullableDataKeysAsStrings(comp.NullableAnalysisData, requiredAnalysis: true);
                Assert.Equal(actualAnalyzedKeys.Contains("Main"), isNullableEnabledForMethod);

                var tree = (CSharpSyntaxTree)comp.SyntaxTrees[0];
                var syntaxNodes = tree.GetRoot().DescendantNodes();
                verifyContextState(tree, syntaxNodes.OfType<ClassDeclarationSyntax>().Single(), classDirectives.ExpectedWarningsState, classDirectives.ExpectedAnnotationsState);
                verifyContextState(tree, syntaxNodes.OfType<MethodDeclarationSyntax>().Single(), expectedWarningsStateForMethod, expectedAnnotationsStateForMethod);

                static NullableContextState.State combineState(NullableContextState.State currentState, NullableContextState.State previousState)
                {
                    return currentState == NullableContextState.State.Unknown ? previousState : currentState;
                }

                static void verifyContextState(CSharpSyntaxTree tree, CSharpSyntaxNode syntax, NullableContextState.State expectedWarningsState, NullableContextState.State expectedAnnotationsState)
                {
                    var actualState = tree.GetNullableContextState(syntax.SpanStart);
                    Assert.Equal(expectedWarningsState, actualState.WarningsState);
                    Assert.Equal(expectedAnnotationsState, actualState.AnnotationsState);
                }

                static bool isNullableEnabled(NullableContextState.State state, bool isNullableEnabledForProject)
                {
                    return state switch
                    {
                        NullableContextState.State.Enabled => true,
                        NullableContextState.State.Disabled => false,
                        _ => isNullableEnabledForProject,
                    };
                }
            }
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
            verify(source, new[] { "M1" },
                // (9,22): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         object obj = null;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(9, 22));

            source =
@"#pragma warning disable 219
static class Program
{
#nullable disable
    static void M2 ()
#nullable enable
    {
        object obj = null;
    }
}";
            verify(source, new[] { "M2" },
                // (8,22): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         object obj = null;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(8, 22));

            source =
@"#pragma warning disable 219
static class Program
{
#nullable disable
    static void M3 ()
    {
#nullable enable
        object obj = null;
    }
}";
            verify(source, new[] { "M3" },
                // (8,22): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         object obj = null;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(8, 22));

            source =
@"#pragma warning disable 219
static class Program
{
#nullable disable
    static void M4 ()
    {
#nullable disable
        object obj = null;
    }
#nullable enable
}";
            verify(source, new string[0]);

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
            verify(source, new[] { "M5" });

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
            verify(source, new[] { "M6" });

            source =
@"static class Program
{
    static object M7()
#nullable enable
        => default(object);
}";
            verify(source, new[] { "M7" });

            source =
@"static class Program
{
#nullable disable
    static object M8() =>
        default(
#nullable enable
            object);
}";
            verify(source, new[] { "M8" });

            static void verify(string source, string[] expectedAnalyzedKeys, params DiagnosticDescription[] expectedDiagnostics)
            {
                var comp = CreateCompilation(source);
                comp.NullableAnalysisData = new ConcurrentDictionary<object, NullableWalker.Data>();
                comp.VerifyDiagnostics(expectedDiagnostics);

                var actualAnalyzedKeys = GetNullableDataKeysAsStrings(comp.NullableAnalysisData, requiredAnalysis: true);
                AssertEx.Equal(expectedAnalyzedKeys, actualAnalyzedKeys);

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
            verify(source, new string[0]);

            source =
@"#nullable enable
#nullable disable
class Program
{
    Program(object o) { o = null; }
}";
            verify(source, new string[0]);

            source =
@"class Program
{
    Program(object o) { o = null; }
#nullable enable
}";
            verify(source, new string[0]);

            source =
@"#pragma warning disable 414
class Program
{
#nullable disable
    object F1 = null;
#nullable enable
}";
            verify(source, new string[0]);

            source =
@"#pragma warning disable 414
class Program
{
#nullable disable
    object F1 = null;
#nullable enable
    static object F2 = null;
}";
            verify(source, new[] { ".cctor" },
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
            verify(source, new[] { ".ctor" },
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
            verify(source, new[] { ".cctor" });

            source =
@"#pragma warning disable 414
static class Program
{
#nullable enable
    static object F1 = null;
#nullable disable
    static Program() { F1 = null; }
}";
            verify(source, new[] { ".cctor" },
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
            verify(source, new[] { ".ctor" });

            source =
@"#pragma warning disable 414
class Program
{
#nullable enable
    object F1 = null;
#nullable disable
    Program() { F1 = null; }
}";
            verify(source, new[] { ".ctor" },
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
            verify(source, new[] { ".ctor" },
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
            verify(source, new[] { "F2" },
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
            verify(source, new[] { ".ctor", ".ctor" });

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
            verify(source, new[] { ".ctor" });

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
            verify(source, new[] { ".ctor", ".ctor" });

            static void verify(string source, string[] expectedAnalyzedKeys, params DiagnosticDescription[] expectedDiagnostics)
            {
                var comp = CreateCompilation(source);
                comp.NullableAnalysisData = new ConcurrentDictionary<object, NullableWalker.Data>();
                comp.VerifyDiagnostics(expectedDiagnostics);

                var actualAnalyzedKeys = GetNullableDataKeysAsStrings(comp.NullableAnalysisData, requiredAnalysis: true);
                AssertEx.Equal(expectedAnalyzedKeys, actualAnalyzedKeys);

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
                comp.NullableAnalysisData = new ConcurrentDictionary<object, NullableWalker.Data>();
                comp.VerifyDiagnostics(expectedDiagnostics);

                var actualAnalyzedKeys = GetNullableDataKeysAsStrings(comp.NullableAnalysisData, requiredAnalysis: true);
                AssertEx.Equal(expectedAnalyzedKeys, actualAnalyzedKeys);
            }
        }

        [Fact]
        [WorkItem(49746, "https://github.com/dotnet/roslyn/issues/49746")]
        public void AnalyzeMethodsInEnabledContextOnly_05()
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
            comp.NullableAnalysisData = new ConcurrentDictionary<object, NullableWalker.Data>();
            comp.VerifyDiagnostics(
                // (4,43): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //     partial void F1(ref object o1) { o1 = null; }
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(4, 43),
                // (8,43): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //     partial void F4(ref object o4) { o4 = null; }
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(8, 43));

            var actualAnalyzedKeys = GetNullableDataKeysAsStrings(comp.NullableAnalysisData, requiredAnalysis: true);
            AssertEx.Equal(new[] { "F1", "F4" }, actualAnalyzedKeys);
        }

        [Fact]
        [WorkItem(49746, "https://github.com/dotnet/roslyn/issues/49746")]
        public void AnalyzeMethodsInEnabledContextOnly_06()
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
            comp.NullableAnalysisData = new ConcurrentDictionary<object, NullableWalker.Data>();
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

            var actualAnalyzedKeys = GetNullableDataKeysAsStrings(comp.NullableAnalysisData, requiredAnalysis: true);
            AssertEx.Equal(new[] { "= null", "= null", "F2" }, actualAnalyzedKeys);
        }

        [Fact]
        [WorkItem(49746, "https://github.com/dotnet/roslyn/issues/49746")]
        public void AnalyzeMethodsInEnabledContextOnly_07()
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
#nullable enable
    static void F1(
        [DefaultParameterValue(C1)]
        object x)
#nullable disable
    {
    }
    static void F2(
#nullable enable
        [DefaultParameterValue(C2)]
#nullable disable
        object x)
    {
    }
    static void F3(
        [DefaultParameterValue(C3)]
#nullable enable
        object x)
#nullable disable
    {
    }
    static void F4(
        [DefaultParameterValue(C4)]
        object x)
#nullable enable
#nullable disable
    {
    }
}";

            var comp = CreateCompilation(source);
            comp.NullableAnalysisData = new ConcurrentDictionary<object, NullableWalker.Data>();
            comp.VerifyDiagnostics(
                // (12,9): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //         [DefaultParameterValue(C1)]
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, @"[DefaultParameterValue(C1)]
        object x").WithLocation(12, 9));

            var actualAnalyzedKeys = GetNullableDataKeysAsStrings(comp.NullableAnalysisData, requiredAnalysis: true);
            var expectedAnalyzedKeys = new[]
            {
                ".cctor",
                @"[DefaultParameterValue(C1)]
        object x",
                @"[DefaultParameterValue(C2)]
#nullable disable
        object x",
                @"[DefaultParameterValue(C3)]
#nullable enable
        object x",
                "DefaultParameterValue(C1)",
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
        public void AnalyzeMethodsInEnabledContextOnly_TopLevelStatements()
        {
            var sourceA =
@"object x = A.F();
if (x == null) { }
_ = x.ToString();
static class A
{
#nullable enable
    internal static object F() => new object();
}";
            verify(new[] { sourceA }, "<Main>$", "F");

            sourceA =
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
            verify(new[] { sourceA, sourceB }, "F");

            static void verify(string[] source, params string[] expectedAnalyzedKeys)
            {
                var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
                comp.NullableAnalysisData = new ConcurrentDictionary<object, NullableWalker.Data>();
                comp.VerifyDiagnostics();

                var actualAnalyzedKeys = GetNullableDataKeysAsStrings(comp.NullableAnalysisData, requiredAnalysis: true);
                AssertEx.Equal(expectedAnalyzedKeys, actualAnalyzedKeys);
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
            comp.NullableAnalysisData = new ConcurrentDictionary<object, NullableWalker.Data>();
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

            var actualAnalyzedKeys = GetNullableDataKeysAsStrings(comp.NullableAnalysisData, requiredAnalysis: true);
            AssertEx.Equal(new[] { "F1" }, actualAnalyzedKeys);
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
                comp.NullableAnalysisData = new ConcurrentDictionary<object, NullableWalker.Data>();

                var syntaxTree = comp.SyntaxTrees[0];
                var model = comp.GetSemanticModel(syntaxTree);
                var syntax = syntaxTree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().Single().Right;
                Assert.Equal("obj", syntax.ToString());
                var typeInfo = model.GetTypeInfo(syntax);
                Assert.Equal(expectedFlowState, typeInfo.Nullability.FlowState);

                var actualAnalyzedKeys = GetNullableDataKeysAsStrings(comp.NullableAnalysisData, requiredAnalysis: true);
                AssertEx.Equal(expectedAnalyzedKeys, actualAnalyzedKeys);
            }
        }

        [Fact]
        [WorkItem(49746, "https://github.com/dotnet/roslyn/issues/49746")]
        public void AnalyzeMethodsInEnabledContextOnly_AttributeSemanticModel()
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
            comp.NullableAnalysisData = new ConcurrentDictionary<object, NullableWalker.Data>();
            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);
            var attributeArguments = syntaxTree.GetRoot().DescendantNodes().OfType<AttributeArgumentSyntax>().ToArray();

            var syntax = attributeArguments[0];
            Assert.Equal("A.F1 = null", syntax.ToString());
            var typeInfo = model.GetTypeInfo(syntax.Expression);
            Assert.Equal(Microsoft.CodeAnalysis.NullableFlowState.MaybeNull, typeInfo.Nullability.FlowState);

            syntax = attributeArguments[1];
            Assert.Equal("A.F2 = null", syntax.ToString());
            typeInfo = model.GetTypeInfo(syntax.Expression);
            Assert.Equal(Microsoft.CodeAnalysis.NullableFlowState.None, typeInfo.Nullability.FlowState);

            var actualAnalyzedKeys = GetNullableDataKeysAsStrings(comp.NullableAnalysisData, requiredAnalysis: true);
            AssertEx.Equal(new[] { "A(A.F1 = null)" }, actualAnalyzedKeys);
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
            comp.NullableAnalysisData = new ConcurrentDictionary<object, NullableWalker.Data>();
            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);
            var equalsValueClauses = syntaxTree.GetRoot().DescendantNodes().OfType<EqualsValueClauseSyntax>().ToArray();

            var syntax = equalsValueClauses[0].Value;
            Assert.Equal("(F1 = null)", syntax.ToString());
            var typeInfo = model.GetTypeInfo(syntax);
            Assert.Equal(Microsoft.CodeAnalysis.NullableFlowState.MaybeNull, typeInfo.Nullability.FlowState);

            syntax = equalsValueClauses[1].Value;
            Assert.Equal("(F2 = null)", syntax.ToString());
            typeInfo = model.GetTypeInfo(syntax);
            Assert.Equal(Microsoft.CodeAnalysis.NullableFlowState.None, typeInfo.Nullability.FlowState);

            var actualAnalyzedKeys = GetNullableDataKeysAsStrings(comp.NullableAnalysisData, requiredAnalysis: true);
            AssertEx.Equal(new[] { "o1" }, actualAnalyzedKeys);
        }

        [Fact]
        [WorkItem(49746, "https://github.com/dotnet/roslyn/issues/49746")]
        public void AnalyzeMethodsInEnabledContextOnly_InitializerSemanticModel_02()
        {
            var source =
@"#pragma warning disable 414
class Program
{
#nullable disable
    object F1 = null;
#nullable enable
    object F2 = null;
}";

            var comp = CreateCompilation(source);
            comp.NullableAnalysisData = new ConcurrentDictionary<object, NullableWalker.Data>();
            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);
            var fieldDeclarations = syntaxTree.GetRoot().DescendantNodes().OfType<FieldDeclarationSyntax>().ToArray();

            var value = fieldDeclarations[0].Declaration.Variables[0].Initializer.Value;
            var typeInfo = model.GetTypeInfo(value);
            Assert.Equal(Microsoft.CodeAnalysis.NullableFlowState.None, typeInfo.Nullability.FlowState);

            value = fieldDeclarations[1].Declaration.Variables[0].Initializer.Value;
            typeInfo = model.GetTypeInfo(value);
            Assert.Equal(Microsoft.CodeAnalysis.NullableFlowState.MaybeNull, typeInfo.Nullability.FlowState);

            var actualAnalyzedKeys = GetNullableDataKeysAsStrings(comp.NullableAnalysisData, requiredAnalysis: true);
            AssertEx.Equal(new[] { "F2" }, actualAnalyzedKeys);
        }

        private static string[] GetNullableDataKeysAsStrings(ConcurrentDictionary<object, NullableWalker.Data> nullableData, bool requiredAnalysis = false) =>
            nullableData.Where(pair => !requiredAnalysis || pair.Value.RequiredAnalysis).Select(pair => GetNullableDataKeyAsString(pair.Key)).OrderBy(key => key).ToArray();

        private static string GetNullableDataKeyAsString(object key) =>
            key is Symbol symbol ? symbol.MetadataName : ((SyntaxNode)key).ToString();
    }
}
