// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class NullableContextTests : CSharpTestBase
    {
        [InlineData("#nullable enable", NullableContextOptions.Disable, NullableContext.Enable)]
        [InlineData("#nullable enable", NullableContextOptions.Annotations, NullableContext.Enable)]
        [InlineData("#nullable enable", NullableContextOptions.Warnings, NullableContext.Enable)]
        [InlineData("#nullable enable", NullableContextOptions.Enable, NullableContext.Enable)]

        [InlineData("#nullable enable warnings", NullableContextOptions.Disable, NullableContext.WarningsEnable | NullableContext.AnnotationsContextInherited)]
        [InlineData("#nullable enable warnings", NullableContextOptions.Warnings, NullableContext.WarningsEnable | NullableContext.AnnotationsContextInherited)]
        [InlineData("#nullable enable warnings", NullableContextOptions.Annotations, NullableContext.Enable | NullableContext.AnnotationsContextInherited)]
        [InlineData("#nullable enable warnings", NullableContextOptions.Enable, NullableContext.Enable | NullableContext.AnnotationsContextInherited)]

        [InlineData("#nullable enable annotations", NullableContextOptions.Disable, NullableContext.AnnotationsEnable | NullableContext.WarningsContextInherited)]
        [InlineData("#nullable enable annotations", NullableContextOptions.Warnings, NullableContext.Enable | NullableContext.WarningsContextInherited)]
        [InlineData("#nullable enable annotations", NullableContextOptions.Annotations, NullableContext.AnnotationsEnable | NullableContext.WarningsContextInherited)]
        [InlineData("#nullable enable annotations", NullableContextOptions.Enable, NullableContext.Enable | NullableContext.WarningsContextInherited)]

        [InlineData("#nullable disable", NullableContextOptions.Disable, NullableContext.Disable)]
        [InlineData("#nullable disable", NullableContextOptions.Annotations, NullableContext.Disable)]
        [InlineData("#nullable disable", NullableContextOptions.Warnings, NullableContext.Disable)]
        [InlineData("#nullable disable", NullableContextOptions.Enable, NullableContext.Disable)]

        [InlineData("#nullable disable warnings", NullableContextOptions.Disable, NullableContext.Disable | NullableContext.AnnotationsContextInherited)]
        [InlineData("#nullable disable warnings", NullableContextOptions.Warnings, NullableContext.Disable | NullableContext.AnnotationsContextInherited)]
        [InlineData("#nullable disable warnings", NullableContextOptions.Annotations, NullableContext.AnnotationsEnable | NullableContext.AnnotationsContextInherited)]
        [InlineData("#nullable disable warnings", NullableContextOptions.Enable, NullableContext.AnnotationsEnable | NullableContext.AnnotationsContextInherited)]

        [InlineData("#nullable disable annotations", NullableContextOptions.Disable, NullableContext.Disable | NullableContext.WarningsContextInherited)]
        [InlineData("#nullable disable annotations", NullableContextOptions.Warnings, NullableContext.WarningsEnable | NullableContext.WarningsContextInherited)]
        [InlineData("#nullable disable annotations", NullableContextOptions.Annotations, NullableContext.Disable | NullableContext.WarningsContextInherited)]
        [InlineData("#nullable disable annotations", NullableContextOptions.Enable, NullableContext.WarningsEnable | NullableContext.WarningsContextInherited)]
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

            Assert.Equal(NullableContext.Enable, model1.GetNullableContext(classDecl1));
            Assert.Equal(NullableContext.Enable | NullableContext.ContextInherited, model2.GetNullableContext(classDecl2));
        }
    }
}
