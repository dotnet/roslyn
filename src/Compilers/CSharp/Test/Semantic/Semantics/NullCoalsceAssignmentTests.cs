﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.NullCoalescingAssignment)]
    public partial class NullCoalesceAssignmentTests : SemanticModelTestBase
    {
        [Fact]
        public void CoalescingAssignment_NoConversion()
        {
            var source = @"
class C
{
    void M(C c1, C c2)
    {
        c1 ??= c2;
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
            var cType = comp.GetTypeByMetadataName("C");

            var syntaxTree = comp.SyntaxTrees.Single();
            var syntaxRoot = syntaxTree.GetRoot();
            var semanticModel = comp.GetSemanticModel(syntaxTree);
            var coalesceAssignment = syntaxRoot.DescendantNodes().OfType<AssignmentExpressionSyntax>().Single();

            assertTypeInfo(coalesceAssignment);
            assertTypeInfo(coalesceAssignment.Left);
            assertTypeInfo(coalesceAssignment.Right);

            void assertTypeInfo(SyntaxNode syntax)
            {
                var typeInfo = semanticModel.GetTypeInfo(syntax);
                Assert.NotNull(typeInfo);
                Assert.NotNull(typeInfo.Type);
                Assert.Equal(cType, typeInfo.Type);
                Assert.Equal(cType, typeInfo.ConvertedType);

            }
        }

        [Fact]
        public void CoalescingAssignment_ValueConversion()
        {
            var source = @"
class C
{
    void M(C c1, D d1)
    {
        c1 ??= d1;
    }
}
class D : C {}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
            var cType = comp.GetTypeByMetadataName("C");
            var dType = comp.GetTypeByMetadataName("D");

            var syntaxTree = comp.SyntaxTrees.Single();
            var syntaxRoot = syntaxTree.GetRoot();
            var semanticModel = comp.GetSemanticModel(syntaxTree);
            var coalesceAssignment = syntaxRoot.DescendantNodes().OfType<AssignmentExpressionSyntax>().Single();

            assertTypeInfo(coalesceAssignment);
            assertTypeInfo(coalesceAssignment.Left);

            var whenNullTypeInfo = semanticModel.GetTypeInfo(coalesceAssignment.Right);
            Assert.NotNull(whenNullTypeInfo);
            Assert.Equal(dType, whenNullTypeInfo.Type);
            Assert.Equal(cType, whenNullTypeInfo.ConvertedType);

            void assertTypeInfo(SyntaxNode syntax)
            {
                var typeInfo = semanticModel.GetTypeInfo(syntax);
                Assert.NotNull(typeInfo);
                Assert.NotNull(typeInfo.Type);
                Assert.Equal(cType, typeInfo.Type);
                Assert.Equal(cType, typeInfo.ConvertedType);

            }
        }

        [Fact]
        public void CoalescingAssignment_AsConvertedExpression()
        {
            var source = @"
class C
{
    void M(D d1, D d2)
    {
        M2(d1 ??= d1);
    }
    void M2(C c) {}
}
class D : C {}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
            var cType = comp.GetTypeByMetadataName("C");
            var dType = comp.GetTypeByMetadataName("D");

            var syntaxTree = comp.SyntaxTrees.Single();
            var syntaxRoot = syntaxTree.GetRoot();
            var semanticModel = comp.GetSemanticModel(syntaxTree);
            var coalesceAssignment = syntaxRoot.DescendantNodes().OfType<AssignmentExpressionSyntax>().Single();

            var whenNullTypeInfo = semanticModel.GetTypeInfo(coalesceAssignment);
            Assert.NotNull(whenNullTypeInfo);
            Assert.Equal(dType, whenNullTypeInfo.Type);
            Assert.Equal(cType, whenNullTypeInfo.ConvertedType);

            assertTypeInfo(coalesceAssignment.Right);
            assertTypeInfo(coalesceAssignment.Left);

            void assertTypeInfo(SyntaxNode syntax)
            {
                var typeInfo = semanticModel.GetTypeInfo(syntax);
                Assert.NotNull(typeInfo);
                Assert.NotNull(typeInfo.Type);
                Assert.Equal(dType, typeInfo.Type);
                Assert.Equal(dType, typeInfo.ConvertedType);

            }
        }

        [Fact]
        public void CoalesceAssignment_NotConvertedToNonNullable()
        {
            var source = @"
class C
{
    void M(int? a, int b)
    {
        a ??= b;
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees.Single();
            var syntaxRoot = syntaxTree.GetRoot();
            var semanticModel = comp.GetSemanticModel(syntaxTree);
            var coalesceAssignment = syntaxRoot.DescendantNodes().OfType<AssignmentExpressionSyntax>().Single();

            var nullable = comp.GetSpecialType(SpecialType.System_Nullable_T);
            var int32 = comp.GetSpecialType(SpecialType.System_Int32);

            var coalesceType = semanticModel.GetTypeInfo(coalesceAssignment).Type;
            var genericParameter = ((INamedTypeSymbol)coalesceType).TypeArguments.Single();

            Assert.Equal(nullable, coalesceType.OriginalDefinition);
            Assert.Equal(int32, genericParameter);
        }
    }
}
