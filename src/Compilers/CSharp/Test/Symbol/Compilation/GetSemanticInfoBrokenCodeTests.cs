// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class GetSemanticInfoTests : SemanticModelTestBase
    {
        [WorkItem(545639, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545639")]
        [Fact]
        public void Bug14200()
        {
            var text =
@"abstract class A
{
    public abstract object this[object x] = { get; }
}";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            VisitAllExpressions(model, tree.GetCompilationUnitRoot());
        }

        [WorkItem(546285, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546285")]
        [Fact]
        public void OmittedArraySize()
        {
            var text =
@"class A
{
    void M()
    {
        /*<bind>*/new object[
class/*</bind>*/ B
{
}";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            VisitAllExpressions(model, expr);
        }

        [Fact]
        public void NullableObjCreation()
        {
            var text =
@"class A
{
    void M()
    {
       var o = new /*<bind>*/ S1? /*</bind>*/() { i = 1 };
    }

    struct S1
    {
        int i;
    }
}";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            VisitAllExpressions(model, expr);
        }

        /// <summary>
        /// Constructed types from the same generic error
        /// type should compare equal.
        /// </summary>
        [Fact]
        public void ConstructedTypeWithConstructedErrorTypeArgument()
        {
            var text =
@"class A<T> { }
class C
{
    static void M()
    {
        var o = /*<bind>*/new A<B<object>>/*</bind>*/();
    }
}";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            VisitAllExpressions(model, expr);
        }

        [WorkItem(546332, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546332")]
        [Fact]
        public void ExpressionInStructuredTrivia()
        {
            var text =
@"#if e==true";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            foreach (var expr in GetAllExpressions(tree.GetCompilationUnitRoot()))
            {
                model.GetTypeInfo(expr);
            }
        }

        [WorkItem(546637, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546637")]
        [Fact()]
        public void Bug16411()
        {
            var text =
@"class C
{
    void M1()
    {
        M2(() => { x }

    [A]
    void M2()
    {
    }
}";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            VisitAllExpressions(model, tree.GetCompilationUnitRoot());
        }

        [WorkItem(547065, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547065")]
        [Fact]
        public void Bug17789()
        {
            var text =
@"delegate U D<T, U>(T t);
static class C
{
    static string[] M(string[] s)
    {
        return s.Select(arg => arg.F().G        
    }
    static string F(this string o)
    {
        return o;
    }
    static U[] Select<T, U>(this T[] t, D<T, U> d)
    {
        return null;
    }
}";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            foreach (var stmt in GetAllStatements(tree.GetCompilationUnitRoot()))
            {
                model.AnalyzeDataFlow(stmt);
            }
        }

        [WorkItem(578141, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578141")]
        [Fact]
        public void IsImplicitlyDeclared()
        {
            var text =
@"object F;
abstract object P { get; set; }
abstract void M();";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var diagnostics = model.GetDiagnostics().ToArray();
            Assert.NotEmpty(diagnostics);
            var type = comp.GlobalNamespace.GetMember<NamedTypeSymbol>(TypeSymbol.ImplicitTypeName);
            Assert.True(type.IsImplicitlyDeclared);
            Symbol member;
            member = type.GetMember<FieldSymbol>("F");
            Assert.False(member.IsImplicitlyDeclared);
            member = type.GetMember<PropertySymbol>("P");
            Assert.False(member.IsImplicitlyDeclared);
            member = type.GetMember<MethodSymbol>("M");
            Assert.False(member.IsImplicitlyDeclared);
        }

        [WorkItem(611177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/611177")]
        [Fact]
        public void Repro611177()
        {
            var source = @"[_<_[delegate using'";
            var comp = CreateCompilation(source);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var usingSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().Single();
            model.GetSymbolInfo(usingSyntax);

            var identifierSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Single();
            model.GetSymbolInfo(identifierSyntax);
        }

        [WorkItem(611177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/611177")]
        [Fact]
        public void UsingStatementInDelegateInArrayRankInType()
        {
            var source = @"
class C
{
    void Test()
    {
        int[delegate { using (Q); }] array;
    }
}";
            var comp = CreateCompilation(source);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var usingSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<UsingStatementSyntax>().Single();

            model.GetSymbolInfo(usingSyntax.Expression);
        }

        [WorkItem(611177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/611177")]
        [Fact]
        public void TypeOfInUnexpectedDelegate()
        {
            var source = @"
class C
{
    void Test()
    {
        int[delegate { typeof(int) }] array;
    }
}";
            var comp = CreateCompilation(source);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var typeOfSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TypeOfExpressionSyntax>().Single();

            var info = model.GetTypeInfo(typeOfSyntax); //Used to throw
            Assert.Equal(comp.GetWellKnownType(WellKnownType.System_Type), info.Type);
        }

        [WorkItem(611177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/611177")]
        [Fact]
        public void UnexpectedDelegateInTypeOf()
        {
            var source = @"
class C
{
    void Test()
    {
        var x = typeof(int[delegate { 1 }]);
    }
}";
            var comp = CreateCompilation(source);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var literalSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().Single();
            Assert.Equal(SyntaxKind.NumericLiteralExpression, literalSyntax.Kind());

            var info = model.GetTypeInfo(literalSyntax); //Used to throw
            Assert.Equal(SpecialType.System_Int32, info.Type.SpecialType);
        }

        [WorkItem(1014561, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1014561")]
        [Fact]
        public void Bug1014561()
        {
            var source = @"

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
        public class DeclarationCompletionProvider : AbstractCompletionProvider
        {
            public overrasync Task<CompletionItemGroup> GetGroupAsync(Document document, int position, CompletionTriggerInfo triggerInfo, CancellationToken cancellationToken = default(CancellationToken))
            {
                    return new CompletionItemGroup(
                        items: names.Select(n => new CompletionItem(this, n, textChangeSpan)).AsImmutable());
                }

                return null;
            }

        }
    }
";
            var comp = CreateCompilation(source);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var identifierSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Single(n => n.Identifier.ValueText == "CompletionItem");

            var info = model.GetSymbolInfo(identifierSyntax); //Used to throw
        }

        [WorkItem(754405, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754405")]
        [Fact]
        public void MissingNullableType()
        {
            var text =
@"class C
{
    object F = typeof(int?);
}";
            var tree = Parse(text);
            var comp = CreateEmptyCompilation(new[] { tree });
            var model = comp.GetSemanticModel(tree);
            VisitAllExpressions(model, tree.GetCompilationUnitRoot());
        }

        [WorkItem(754405, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754405")]
        [Fact]
        public void MissingPointerType()
        {
            var text =
@"class C
{
    object F = typeof(int*);
}";
            var tree = Parse(text);
            var comp = CreateEmptyCompilation(new[] { tree });
            var model = comp.GetSemanticModel(tree);
            VisitAllExpressions(model, tree.GetCompilationUnitRoot());
        }

        [WorkItem(754405, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754405")]
        [Fact]
        public void MissingArrayType()
        {
            var text =
@"class C
{
    object F = typeof(int[]);
}";
            var tree = Parse(text);
            var comp = CreateEmptyCompilation(new[] { tree });
            var model = comp.GetSemanticModel(tree);
            VisitAllExpressions(model, tree.GetCompilationUnitRoot());
        }

        [WorkItem(757789, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/757789")]
        [Fact]
        public void OmittedTypeArgument()
        {
            var text =
@"class C<T, U>
{
    static object F = new C<,>();
}";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            foreach (var expr in GetAllExpressions(tree.GetCompilationUnitRoot()))
            {
                var symbolInfo = model.GetSymbolInfo(expr);
                // https://github.com/dotnet/roslyn/issues/38509
                // Assert.NotEqual(default, symbolInfo);
                model.AnalyzeDataFlow(expr);
            }
        }

        private void VisitAllExpressions(SemanticModel model, SyntaxNode node)
        {
            foreach (var expr in GetAllExpressions(node))
            {
                var symbolInfo = model.GetSymbolInfo(expr);

                // https://github.com/dotnet/roslyn/issues/38509
                // Assert.NotEqual(default, symbolInfo);
            }
        }

        private static IEnumerable<ExpressionSyntax> GetAllExpressions(SyntaxNode node)
        {
            return node.DescendantNodesAndSelf(descendIntoTrivia: true).OfType<ExpressionSyntax>();
        }

        private static IEnumerable<StatementSyntax> GetAllStatements(SyntaxNode node)
        {
            return node.DescendantNodesAndSelf().OfType<StatementSyntax>();
        }
    }
}
