﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
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
                Assert.NotEqual(default, typeInfo);
                Assert.NotNull(typeInfo.Type);
                Assert.Equal(cType.GetPublicSymbol(), typeInfo.Type);
                Assert.Equal(cType.GetPublicSymbol(), typeInfo.ConvertedType);

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
            Assert.NotEqual(default, whenNullTypeInfo);
            Assert.Equal(dType.GetPublicSymbol(), whenNullTypeInfo.Type);
            Assert.Equal(cType, whenNullTypeInfo.ConvertedType.GetSymbol());

            void assertTypeInfo(SyntaxNode syntax)
            {
                var typeInfo = semanticModel.GetTypeInfo(syntax);
                Assert.NotEqual(default, typeInfo);
                Assert.NotNull(typeInfo.Type);
                Assert.Equal(cType, typeInfo.Type.GetSymbol());
                Assert.Equal(cType.GetPublicSymbol(), typeInfo.ConvertedType);

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
            Assert.NotEqual(default, whenNullTypeInfo);
            Assert.Equal(dType, whenNullTypeInfo.Type.GetSymbol());
            Assert.Equal(cType.GetPublicSymbol(), whenNullTypeInfo.ConvertedType);

            assertTypeInfo(coalesceAssignment.Right);
            assertTypeInfo(coalesceAssignment.Left);

            void assertTypeInfo(SyntaxNode syntax)
            {
                var typeInfo = semanticModel.GetTypeInfo(syntax);
                Assert.NotEqual(default, typeInfo);
                Assert.NotNull(typeInfo.Type);
                Assert.Equal(dType.GetPublicSymbol(), typeInfo.Type);
                Assert.Equal(dType, typeInfo.ConvertedType.GetSymbol());

            }
        }

        [Fact]
        public void CoalesceAssignment_ConvertedToNonNullable()
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

            var int32 = comp.GetSpecialType(SpecialType.System_Int32);
            var coalesceType = semanticModel.GetTypeInfo(coalesceAssignment).Type;

            Assert.Equal(int32.GetPublicSymbol(), coalesceType);
        }

        [Fact]
        public void CoalesceAssignment_DefaultConvertedToNonNullable()
        {
            var source = @"
class C
{
    void M(int? a)
    {
        a ??= default;
    }
}";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees.Single();
            var syntaxRoot = syntaxTree.GetRoot();
            var semanticModel = comp.GetSemanticModel(syntaxTree);
            var defaultLiteral = syntaxRoot.DescendantNodes().OfType<LiteralExpressionSyntax>().Where(expr => expr.IsKind(SyntaxKind.DefaultLiteralExpression)).Single();

            Assert.Equal(SpecialType.System_Int32, semanticModel.GetTypeInfo(defaultLiteral).Type.SpecialType);
            Assert.Equal(SpecialType.System_Int32, semanticModel.GetTypeInfo(defaultLiteral).ConvertedType.SpecialType);
        }
    }
}
