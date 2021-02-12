// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class SemanticModelTests : CSharpTestBase
    {
        [Fact]
        public void NamespaceBindingInInteractiveCode()
        {
            var compilation = CreateCompilation(@"
using Z = Goo.Bar.Script.C;

class C { }

namespace Goo.Bar
{
    class B : Z { }
}
",
                parseOptions: TestOptions.Script,
                options: TestOptions.ReleaseExe.WithScriptClassName("Goo.Bar.Script")
            );

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var classB = (root.Members[1] as NamespaceDeclarationSyntax).Members[0] as TypeDeclarationSyntax;
            var model = compilation.GetSemanticModel(tree);
            var symbol = model.GetDeclaredSymbol(classB);
            var baseType = symbol?.BaseType;
            Assert.NotNull(baseType);
            Assert.Equal(TypeKind.Error, baseType.TypeKind);
            Assert.Equal(LookupResultKind.Inaccessible, baseType.GetSymbol<ErrorTypeSymbol>().ResultKind); // Script class members are private.
        }

        [Fact]
        public void CompilationChain_OverloadsWithParams()
        {
            CompileAndVerifyBindInfo(@"
public static string[] str = null;
public static void Goo(string[] r, string i) { str = r;}
public static void Goo(params string[] r) { str = r;}
/*<bind>*/ Goo(""1"", ""2"") /*</bind>*/;",
"Goo(params string[])");
        }

        [Fact]
        public void CompilationChain_NestedTypesClass()
        {
            CompileAndVerifyBindInfo(@"
class InnerClass
{
   public string innerStr = null;
   public string Goo() { return innerStr;}       
}
InnerClass iC = new InnerClass();
/*<bind>*/ iC.Goo(); /*</bind>*/",
"InnerClass.Goo()");
        }

        [Fact]
        public void MethodCallBinding()
        {
            var testSrc = @"
void Goo() {};
/*<bind>*/Goo()/*</bind>*/;
";
            // Get the bind info for the text identified within the commented <bind> </bind> tags
            var bindInfo = GetBindInfoForTest(testSrc);

            Assert.NotNull(bindInfo.Type);
            Assert.Equal("System.Void", bindInfo.Type.ToTestDisplayString());
        }

        [Fact]
        public void BindNullLiteral()
        {
            var testSrc = @"string s = /*<bind>*/null/*</bind>*/;";
            // Get the bind info for the text identified within the commented <bind> </bind> tags
            var bindInfo = GetBindInfoForTest(testSrc);
            Assert.Null(bindInfo.Type);
        }

        [Fact]
        public void BindBooleanField()
        {
            var testSrc = @"
bool result = true ;
/*<bind>*/ result /*</bind>*/= false;
";
            // Get the bind info for the text identified within the commented <bind> </bind> tags
            var bindInfo = GetBindInfoForTest(testSrc);
            Assert.NotNull(bindInfo.Type);
            Assert.Equal("System.Boolean", bindInfo.Type.ToTestDisplayString());
        }

        [Fact]
        public void BindLocals()
        {
            var testSrc = @"
const int constantField = 1;
int field = constantField;
{
    int local1 = field;
    int local2 = /*<bind>*/local1/*</bind>*/;
}
{
    int local2 = constantField;
}";
            // Get the bind info for the text identified within the commented <bind> </bind> tags
            var bindInfo = GetBindInfoForTest(testSrc);
            Assert.Equal(SpecialType.System_Int32, bindInfo.Type.SpecialType);
            var symbol = bindInfo.Symbol;
            Assert.Equal("System.Int32 local1", symbol.ToTestDisplayString());
            Assert.IsAssignableFrom<SourceLocalSymbol>(symbol.GetSymbol());
        }

        [WorkItem(540513, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540513")]
        [Fact]
        public void BindVariableInGlobalStatement()
        {
            var testSrc = @"
int i = 2;
++/*<bind>*/i/*</bind>*/;";
            // Get the bind info for the text identified within the commented <bind> </bind> tags
            var bindInfo = GetBindInfoForTest(testSrc);
            Assert.Equal(SpecialType.System_Int32, bindInfo.Type.SpecialType);
            var symbol = bindInfo.Symbol;
            Assert.Equal("System.Int32 Script.i", symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, symbol.Kind);
        }

        [WorkItem(543860, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543860")]
        [Fact]
        public void BindVarKeyword()
        {
            var testSrc = @"
/*<bind>*/var/*</bind>*/ rand = new System.Random();";

            // Get the bind info for the text identified within the commented <bind> </bind> tags
            var semanticInfo = GetBindInfoForTest(testSrc);

            Assert.Equal("System.Random", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Random", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Random", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(543860, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543860")]
        [Fact]
        public void BindVarKeyword_MultipleDeclarators()
        {
            string testSrc = @"
/*<bind>*/var/*</bind>*/ i = new int(), j = new char();
";
            // Get the bind info for the text identified within the commented <bind> </bind> tags
            var semanticInfo = GetBindInfoForTest(testSrc);

            Assert.Equal("var", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind);
            Assert.Equal("var", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(543860, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543860")]
        [Fact]
        public void BindVarNamedType()
        {
            string testSrc = @"
public class var { }
/*<bind>*/var/*</bind>*/ x = new var();
";
            // Get the bind info for the text identified within the commented <bind> </bind> tags
            var semanticInfo = GetBindInfoForTest(testSrc);

            Assert.Equal("Script.var", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("Script.var", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("Script.var", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(543860, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543860")]
        [Fact]
        public void BindVarNamedType_Ambiguous()
        {
            string testSrc = @"
using System;
public class var { }
public struct var { }
/*<bind>*/var/*</bind>*/ x = new var();
";
            // Get the bind info for the text identified within the commented <bind> </bind> tags
            var semanticInfo = GetBindInfoForTest(testSrc);

            Assert.Equal("Script.var", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind);
            Assert.Equal("Script.var", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, semanticInfo.CandidateReason);
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString()).ToArray();
            Assert.Equal("Script.var", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[0].Kind);
            Assert.Equal("Script.var", sortedCandidates[1].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[1].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(543864, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543864")]
        [Fact]
        public void BindQueryVariable()
        {
            string testSrc = @"
using System.Linq;

var x = from c in ""goo"" select /*<bind>*/c/*</bind>*/";
            // Get the bind info for the text identified within the commented <bind> </bind> tags
            var semanticInfo = GetBindInfoForTest(testSrc);
            Assert.Equal("c", semanticInfo.Symbol.Name);
            Assert.Equal(SymbolKind.RangeVariable, semanticInfo.Symbol.Kind);
            Assert.Equal(SpecialType.System_Char, semanticInfo.Type.SpecialType);
        }

        #region helpers

        private List<ExpressionSyntax> GetExprSyntaxList(SyntaxTree syntaxTree)
        {
            return GetExprSyntaxList(syntaxTree.GetCompilationUnitRoot(), null);
        }

        private List<ExpressionSyntax> GetExprSyntaxList(SyntaxNode node, List<ExpressionSyntax> exprSynList)
        {
            if (exprSynList == null)
                exprSynList = new List<ExpressionSyntax>();

            if (node is ExpressionSyntax)
            {
                exprSynList.Add(node as ExpressionSyntax);
            }

            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.IsNode)
                    exprSynList = GetExprSyntaxList(child.AsNode(), exprSynList);
            }

            return exprSynList;
        }

        private ExpressionSyntax GetExprSyntaxForBinding(List<ExpressionSyntax> exprSynList)
        {
            foreach (var exprSyntax in exprSynList)
            {
                string exprFullText = exprSyntax.ToFullString();
                exprFullText = exprFullText.Trim();

                if (exprFullText.StartsWith("/*<bind>*/", StringComparison.Ordinal))
                {
                    if (exprFullText.Contains("/*</bind>*/"))
                    {
                        if (exprFullText.EndsWith("/*</bind>*/", StringComparison.Ordinal))
                        {
                            return exprSyntax;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        return exprSyntax;
                    }
                }

                if (exprFullText.EndsWith("/*</bind>*/", StringComparison.Ordinal))
                {
                    if (exprFullText.Contains("/*<bind>*/"))
                    {
                        if (exprFullText.StartsWith("/*<bind>*/", StringComparison.Ordinal))
                        {
                            return exprSyntax;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        return exprSyntax;
                    }
                }
            }

            return null;
        }

        private void CompileAndVerifyBindInfo(string testSrc, string expected)
        {
            // Get the bind info for the text identified within the commented <bind> </bind> tags
            var bindInfo = GetBindInfoForTest(testSrc);
            Assert.NotNull(bindInfo.Type);
            var method = bindInfo.Symbol.ToDisplayString();
            Assert.Equal(expected, method);
        }

        private CompilationUtils.SemanticInfoSummary GetBindInfoForTest(string testSrc)
        {
            var compilation = CreateCompilation(testSrc, parseOptions: TestOptions.Script);
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var exprSyntaxToBind = GetExprSyntaxForBinding(GetExprSyntaxList(tree));

            return model.GetSemanticInfoSummary(exprSyntaxToBind);
        }

        #endregion helpers
    }
}
