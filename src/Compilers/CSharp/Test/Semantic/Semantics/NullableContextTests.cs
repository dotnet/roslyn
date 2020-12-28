// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Concurrent;
using System.Linq;
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

            // https://github.com/dotnet/roslyn/issues/49746: Currently, if we analyze any members, we analyze all.
            var expectedAnalyzedKeysAll = new[] { ".cctor", ".ctor", "F1", "F2" };

            verify(parseOptions: TestOptions.Regular, expectedAnalyzedKeysAll);
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", null), expectedAnalyzedKeysAll);
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", "always"), expectedAnalyzedKeysAll);
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", "never"));
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", "ALWAYS"), expectedAnalyzedKeysAll); // unrecognized value (incorrect case) ignored
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", "NEVER"), expectedAnalyzedKeysAll); // unrecognized value (incorrect case) ignored
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", "true"), expectedAnalyzedKeysAll); // unrecognized value ignored
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", "false"), expectedAnalyzedKeysAll); // unrecognized value ignored
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", "unknown"), expectedAnalyzedKeysAll); // unrecognized value ignored

            void verify(CSharpParseOptions parseOptions, params string[] expectedAnalyzedKeys)
            {
                var comp = CreateCompilation(source, parseOptions: parseOptions);
                comp.NullableAnalysisData = new ConcurrentDictionary<object, int>();
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

            // https://github.com/dotnet/roslyn/issues/49746: Currently, if we analyze any members, we analyze all.
            var expectedAnalyzedKeysAll = new[] { ".cctor", ".ctor", "= C1", "= C2", "F1", "F2" };

            verify(parseOptions: TestOptions.Regular, expectedAnalyzedKeysAll);
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", null), expectedAnalyzedKeysAll);
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", "always"), expectedAnalyzedKeysAll);
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", "never"));

            void verify(CSharpParseOptions parseOptions, params string[] expectedAnalyzedKeys)
            {
                var comp = CreateCompilation(source, parseOptions: parseOptions);
                comp.NullableAnalysisData = new ConcurrentDictionary<object, int>();
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

            // https://github.com/dotnet/roslyn/issues/49746: Currently, if we analyze any members, we analyze all.
            var expectedAnalyzedKeysAll = new[] { ".cctor", ".cctor", "A(A.C1)", "A(A.C2)" };

            verify(parseOptions: TestOptions.Regular, expectedAnalyzedKeysAll);
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", null), expectedAnalyzedKeysAll);
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", "always"), expectedAnalyzedKeysAll);
            verify(parseOptions: TestOptions.Regular.WithFeature("run-nullable-analysis", "never"));

            void verify(CSharpParseOptions parseOptions, params string[] expectedAnalyzedKeys)
            {
                var comp = CreateCompilation(sourceB, references: new[] { refA }, parseOptions: parseOptions);
                comp.NullableAnalysisData = new ConcurrentDictionary<object, int>();
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
                comp.NullableAnalysisData = new ConcurrentDictionary<object, int>();
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
                comp.NullableAnalysisData = new ConcurrentDictionary<object, int>();
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
                comp.NullableAnalysisData = new ConcurrentDictionary<object, int>();
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

        private static string[] GetNullableDataKeysAsStrings(ConcurrentDictionary<object, int> nullableData) =>
            nullableData.Keys.Select(key => GetNullableDataKeyAsString(key)).OrderBy(key => key).ToArray();

        private static string GetNullableDataKeyAsString(object key) =>
            key is Symbol symbol ? symbol.MetadataName : ((SyntaxNode)key).ToString();
    }
}
