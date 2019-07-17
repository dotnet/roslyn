// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
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
    }
}
