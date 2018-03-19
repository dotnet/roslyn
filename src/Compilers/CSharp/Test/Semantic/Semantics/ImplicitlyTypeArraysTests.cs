// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class ImplicitlyTypeArraysTests : SemanticModelTestBase
    {
        #region "Functionality tests"

        [Fact]
        public void ImplicitlyTypedArrayLocal()
        {
            CSharpCompilation compilation = CreateCompilation(@"
class M {}

class C 
{ 
     public void F()
     {
        var a = new[] { new M() };
     }
}
");

            compilation.VerifyDiagnostics();

            var method = (SourceMemberMethodSymbol)compilation.GlobalNamespace.GetTypeMembers("C").Single().GetMembers("F").Single();
            var diagnostics = new DiagnosticBag();
            BoundBlock block = MethodCompiler.BindMethodBody(method, new TypeCompilationState(method.ContainingType, compilation, null), diagnostics);

            var locDecl = (BoundLocalDeclaration)block.Statements.Single();
            var localA = (ArrayTypeSymbol)locDecl.DeclaredType.Display;

            TypeSymbol typeM = compilation.GlobalNamespace.GetMember<TypeSymbol>("M");

            Assert.Equal(typeM, localA.ElementType);
        }

        [Fact]
        public void ImplicitlyTypedArray_BindArrayInitializer()
        {
            var text = @"
class C 
{ 
     public void F()
     {
         var a = "";
         var b = new[] { ""hello"", /*<bind>*/ a /*</bind>*/, null}; 
     }
}
";

            SyntaxTree tree = Parse(text);
            CSharpCompilation comp = CreateCompilation(tree);
            SemanticModel model = comp.GetSemanticModel(tree);

            ExpressionSyntax expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            SymbolInfo sym = model.GetSymbolInfo(expr);
            Assert.Equal(SymbolKind.Local, sym.Symbol.Kind);

            TypeInfo info = model.GetTypeInfo(expr);
            Assert.NotNull(info.Type);
            Assert.NotNull(info.ConvertedType);
        }

        [Fact]
        public void ImplicitlyTypedArray_BindImplicitlyTypedLocal()
        {
            var text = @"
class C 
{ 
     public void F()
     {
        /*<bind>*/ var a /*</bind>*/ = new[] { ""hello"", "", null}; 
     }
}
";

            SyntaxTree tree = Parse(text);
            CSharpCompilation comp = CreateCompilation(tree);
            SemanticModel model = comp.GetSemanticModel(tree);

            ExpressionSyntax expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            SymbolInfo symInfo = model.GetSymbolInfo(expr);

            Assert.Equal("System.String[]", symInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.ArrayType, symInfo.Symbol.Kind);

            TypeInfo typeInfo = model.GetTypeInfo(expr);
            Assert.NotNull(typeInfo.Type);
            Assert.NotNull(typeInfo.ConvertedType);
        }

        #endregion
    }
}
