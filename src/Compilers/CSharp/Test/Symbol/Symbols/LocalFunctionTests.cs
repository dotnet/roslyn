// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class LocalFunctionTests : CSharpTestBase
    {
        [Fact, WorkItem(27719, "https://github.com/dotnet/roslyn/issues/27719")]
        public void LocalFunctionIsNotStatic()
        {
            var source = @"
class C
{
    void M()
    {
        void local() {}
        local();
    }
}";
            var compilation = CreateCompilation(source).VerifyDiagnostics();
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);

            var localSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single();
            var local = semanticModel.GetDeclaredSymbol(localSyntax);
            Assert.False(local.IsStatic);
        }

        [Fact, WorkItem(27719, "https://github.com/dotnet/roslyn/issues/27719")]
        public void StaticLocalFunctionIsStatic()
        {
            var source = @"
class C
{
    void M()
    {
        static void local() {}
        local();
    }
}";
            var compilation = CreateCompilation(source).VerifyDiagnostics();
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);

            var localSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single();
            var local = semanticModel.GetDeclaredSymbol(localSyntax);
            Assert.True(local.IsStatic);
        }

        [Fact, WorkItem(27719, "https://github.com/dotnet/roslyn/issues/27719")]
        public void LocalFunctionInStaticMethodIsNotStatic()
        {
            var source = @"
class C
{
    static void M()
    {
        void local() {}
        local();
    }
}";
            var compilation = CreateCompilation(source).VerifyDiagnostics();
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);

            var localSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single();
            var local = semanticModel.GetDeclaredSymbol(localSyntax);
            Assert.False(local.IsStatic);
        }

        [Fact]
        public void LocalFunctionDoesNotRequireInstanceReceiver()
        {
            var source = @"
class C
{
    void M()
    {
        void local() {}
        static void staticLocal() {}
        local();
        staticLocal();
    }
}";
            var compilation = CreateCompilation(source).VerifyDiagnostics();
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);

            var localsSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LocalFunctionStatementSyntax>().ToArray();
            var local = (MethodSymbol)semanticModel.GetDeclaredSymbol(localsSyntax[0]);
            Assert.False(local.RequiresInstanceReceiver);
            var staticLocal = (MethodSymbol)semanticModel.GetDeclaredSymbol(localsSyntax[0]);
            Assert.False(staticLocal.RequiresInstanceReceiver);
        }
    }
}
