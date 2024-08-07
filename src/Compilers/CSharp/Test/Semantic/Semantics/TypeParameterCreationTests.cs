// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

public class TypeParameterCreationTests : CompilingTestBase
{
    [Theory]
    [InlineData("new()")]
    [InlineData("class, new()")]
    [InlineData("struct")]
    public void CreateGenericTypeParameterObject_ExplicitCreation(string constraint)
    {
        var source = $$"""
            class C
            {
                void M<T>() where T : {{constraint}}
                {
                    T t = new T();
                }
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);

        var objectCreationNode = tree
            .GetCompilationUnitRoot()
            .DescendantNodes()
            .OfType<ObjectCreationExpressionSyntax>()
            .Single();

        var typeInfo = model.GetTypeInfo(objectCreationNode);
        Assert.Equal("T", typeInfo.Type.ToTestDisplayString());
        Assert.Equal("T", typeInfo.ConvertedType.ToTestDisplayString());

        var symbolInfo = model.GetSymbolInfo(objectCreationNode);
        Assert.True(symbolInfo.IsEmpty);

        var typeNode = objectCreationNode.Type;

        typeInfo = model.GetTypeInfo(typeNode);
        Assert.Null(typeInfo.Type);
        Assert.Null(typeInfo.ConvertedType);

        symbolInfo = model.GetSymbolInfo(typeNode);
        Assert.Equal("T", symbolInfo.Symbol?.ToTestDisplayString());
    }

    [Theory]
    [InlineData("new()")]
    [InlineData("class, new()")]
    [InlineData("struct")]
    public void CreateGenericTypeParameterObject_ImplicitCreation(string constraint)
    {
        var source = $$"""
            class C
            {
                void M<T>() where T : {{constraint}}
                {
                    T t = new();
                }
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);

        var objectCreationNode = tree
            .GetCompilationUnitRoot()
            .DescendantNodes()
            .OfType<ImplicitObjectCreationExpressionSyntax>()
            .Single();

        var typeInfo = model.GetTypeInfo(objectCreationNode);
        Assert.Equal("T", typeInfo.Type.ToTestDisplayString());
        Assert.Equal("T", typeInfo.ConvertedType.ToTestDisplayString());

        var symbolInfo = model.GetSymbolInfo(objectCreationNode);
        Assert.True(symbolInfo.IsEmpty);
    }

    [Theory]
    [InlineData("")]
    [InlineData("where T : class")]
    public void CreateGenericTypeParameterObject_ExplicitCreation_ErrorRecovery_NoNewConstraint(string constraintClause)
    {
        var source = $$"""
            class C
            {
                void M<T>() {{constraintClause}}
                {
                    T t = new T();
                }
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (5,15): error CS0304: Cannot create an instance of the variable type 'T' because it does not have the new() constraint
            //         T t = new T();
            Diagnostic(ErrorCode.ERR_NoNewTyvar, "new T()").WithArguments("T").WithLocation(5, 15));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);

        var objectCreationNode = tree
            .GetCompilationUnitRoot()
            .DescendantNodes()
            .OfType<ObjectCreationExpressionSyntax>()
            .Single();

        var typeInfo = model.GetTypeInfo(objectCreationNode);
        Assert.Equal("T", typeInfo.Type.ToTestDisplayString());
        Assert.Equal("T", typeInfo.ConvertedType.ToTestDisplayString());

        var symbolInfo = model.GetSymbolInfo(objectCreationNode);
        Assert.True(symbolInfo.IsEmpty);

        var typeNode = objectCreationNode.Type;

        typeInfo = model.GetTypeInfo(typeNode);
        Assert.Null(typeInfo.Type);
        Assert.Null(typeInfo.ConvertedType);

        symbolInfo = model.GetSymbolInfo(typeNode);
        Assert.Null(symbolInfo.Symbol);
        Assert.Equal(CandidateReason.NotCreatable, symbolInfo.CandidateReason);
        Assert.Collection(symbolInfo.CandidateSymbols,
            s => Assert.Equal("T", s.ToTestDisplayString()));
    }

    [Theory]
    [InlineData("new()")]
    [InlineData("class, new()")]
    [InlineData("struct")]
    public void CreateGenericTypeParameterObject_ExplicitCreation_ErrorRecovery_UnexpectedParameter(string constraint)
    {
        var source = $$"""
            class C
            {
                void M<T>() where T : {{constraint}}
                {
                    T t = new T(0);
                }
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (5,15): error CS0417: 'T': cannot provide arguments when creating an instance of a variable type
            //         T t = new T(0);
            Diagnostic(ErrorCode.ERR_NewTyvarWithArgs, "new T(0)").WithArguments("T").WithLocation(5, 15));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);

        var objectCreationNode = tree
            .GetCompilationUnitRoot()
            .DescendantNodes()
            .OfType<ObjectCreationExpressionSyntax>()
            .Single();

        var typeInfo = model.GetTypeInfo(objectCreationNode);
        Assert.Equal("T", typeInfo.Type.ToTestDisplayString());
        Assert.Equal("T", typeInfo.ConvertedType.ToTestDisplayString());

        var symbolInfo = model.GetSymbolInfo(objectCreationNode);
        Assert.True(symbolInfo.IsEmpty);

        var typeNode = objectCreationNode.Type;

        typeInfo = model.GetTypeInfo(typeNode);
        Assert.Null(typeInfo.Type);
        Assert.Null(typeInfo.ConvertedType);

        symbolInfo = model.GetSymbolInfo(typeNode);
        Assert.Null(symbolInfo.Symbol);
        Assert.Equal(CandidateReason.NotCreatable, symbolInfo.CandidateReason);
        Assert.Collection(symbolInfo.CandidateSymbols,
            s => Assert.Equal("T", s.ToTestDisplayString()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("where T : class")]
    public void CreateGenericTypeParameterObject_ImplicitCreation_ErrorRecovery_NoNewConstraint(string constraintClause)
    {
        var source = $$"""
            class C
            {
                void M<T>() {{constraintClause}}
                {
                    T t = new();
                }
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (5,15): error CS0304: Cannot create an instance of the variable type 'T' because it does not have the new() constraint
            //         T t = new();
            Diagnostic(ErrorCode.ERR_NoNewTyvar, "new()").WithArguments("T").WithLocation(5, 15));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);

        var objectCreationNode = tree
            .GetCompilationUnitRoot()
            .DescendantNodes()
            .OfType<ImplicitObjectCreationExpressionSyntax>()
            .Single();

        var typeInfo = model.GetTypeInfo(objectCreationNode);
        Assert.Equal("T", typeInfo.Type.ToTestDisplayString());
        Assert.Equal("T", typeInfo.ConvertedType.ToTestDisplayString());

        var symbolInfo = model.GetSymbolInfo(objectCreationNode);
        Assert.True(symbolInfo.IsEmpty);
    }

    [Theory]
    [InlineData("new()")]
    [InlineData("class, new()")]
    [InlineData("struct")]
    public void CreateGenericTypeParameterObject_ImplicitCreation_ErrorRecovery_UnexpectedParameter(string constraint)
    {
        var source = $$"""
            class C
            {
                void M<T>() where T : {{constraint}}
                {
                    T t = new(0);
                }
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (5,15): error CS0417: 'T': cannot provide arguments when creating an instance of a variable type
            //         T t = new(0);
            Diagnostic(ErrorCode.ERR_NewTyvarWithArgs, "new(0)").WithArguments("T").WithLocation(5, 15));

        var tree = comp.SyntaxTrees.First();
        var model = comp.GetSemanticModel(tree);

        var objectCreationNode = tree
            .GetCompilationUnitRoot()
            .DescendantNodes()
            .OfType<ImplicitObjectCreationExpressionSyntax>()
            .Single();

        var typeInfo = model.GetTypeInfo(objectCreationNode);
        Assert.Equal("T", typeInfo.Type.ToTestDisplayString());
        Assert.Equal("T", typeInfo.ConvertedType.ToTestDisplayString());

        var symbolInfo = model.GetSymbolInfo(objectCreationNode);
        Assert.True(symbolInfo.IsEmpty);
    }
}
