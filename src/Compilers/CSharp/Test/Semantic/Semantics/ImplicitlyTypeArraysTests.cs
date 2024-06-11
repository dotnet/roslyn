// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class ImplicitlyTypeArraysTests : SemanticModelTestBase
    {
        [Fact]
        public void ImplicitlyTypedArrayLocal()
        {
            var compilation = CreateCompilation(@"
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
            var diagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics: true, withDependencies: false);
            var block = MethodCompiler.BindSynthesizedMethodBody(method, new TypeCompilationState(method.ContainingType, compilation, null), diagnostics);
            diagnostics.Free();

            var locDecl = (BoundLocalDeclaration)block.Statements.Single();
            var localA = (ArrayTypeSymbol)locDecl.DeclaredTypeOpt.Display;

            var typeM = compilation.GlobalNamespace.GetMember<TypeSymbol>("M");

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

            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            var sym = model.GetSymbolInfo(expr);
            Assert.Equal(SymbolKind.Local, sym.Symbol.Kind);

            var info = model.GetTypeInfo(expr);
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

            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            var symInfo = model.GetSymbolInfo(expr);

            Assert.Equal("System.String[]", symInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.ArrayType, symInfo.Symbol.Kind);

            var typeInfo = model.GetTypeInfo(expr);
            Assert.NotNull(typeInfo.Type);
            Assert.NotNull(typeInfo.ConvertedType);
        }
    }
}
