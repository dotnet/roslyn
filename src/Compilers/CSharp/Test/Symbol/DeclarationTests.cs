// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
//test

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class DeclarationTests : CSharpTestBase
    {
        private CompilationUnitSyntax ParseFile(string text)
        {
            return SyntaxFactory.ParseCompilationUnit(text);
        }

        [Fact]
        public void TestSimpleDeclarations()
        {
            var text1 = @"
namespace NA.NB
{
  partial class C<T>
  { 
    partial class D
    {
      int F;
    }
  }
  class C { }
}
";
            var text2 = @"
namespace NA
{
  namespace NB
  {
    partial class C<T>
    { 
      partial class D
      {
        void G() {};
      }
    }
  }
}
";
            var tree1 = SyntaxFactory.ParseSyntaxTree(text1);
            var tree2 = SyntaxFactory.ParseSyntaxTree(text2);
            Assert.NotNull(tree1);
            Assert.NotNull(tree2);
            var decl1 = DeclarationTreeBuilder.ForTree(tree1, new CSharpCompilationOptions(OutputKind.ConsoleApplication).ScriptClassName, isSubmission: false);
            var decl2 = DeclarationTreeBuilder.ForTree(tree2, new CSharpCompilationOptions(OutputKind.ConsoleApplication).ScriptClassName, isSubmission: false);
            Assert.NotNull(decl1);
            Assert.NotNull(decl2);
            Assert.Equal(string.Empty, decl1.Name);
            Assert.Equal(string.Empty, decl2.Name);
            Assert.Equal(1, decl1.Children.Length);
            Assert.Equal(1, decl2.Children.Length);
            var na1 = decl1.Children.Single();
            var na2 = decl2.Children.Single();
            Assert.NotNull(na1);
            Assert.NotNull(na2);
            Assert.Equal(DeclarationKind.Namespace, na1.Kind);
            Assert.Equal(DeclarationKind.Namespace, na2.Kind);
            Assert.Equal("NA", na1.Name);
            Assert.Equal("NA", na2.Name);
            Assert.Equal(1, na1.Children.Length);
            Assert.Equal(1, na2.Children.Length);
            var nb1 = na1.Children.Single();
            var nb2 = na2.Children.Single();
            Assert.NotNull(nb1);
            Assert.NotNull(nb2);
            Assert.Equal(DeclarationKind.Namespace, nb1.Kind);
            Assert.Equal(DeclarationKind.Namespace, nb2.Kind);
            Assert.Equal("NB", nb1.Name);
            Assert.Equal("NB", nb2.Name);
            Assert.Equal(2, nb1.Children.Length);
            Assert.Equal(1, nb2.Children.Length);
            var ct1 = (SingleTypeDeclaration)nb1.Children.First();
            var ct2 = (SingleTypeDeclaration)nb2.Children.Single();
            Assert.Equal(DeclarationKind.Class, ct1.Kind);
            Assert.Equal(DeclarationKind.Class, ct2.Kind);
            Assert.NotNull(ct1);
            Assert.NotNull(ct2);
            Assert.Equal("C", ct1.Name);
            Assert.Equal("C", ct2.Name);
            Assert.Equal(1, ct1.Arity);
            Assert.Equal(1, ct2.Arity);
            Assert.Equal(1, ct1.Children.Length);
            Assert.Equal(1, ct2.Children.Length);
            var c1 = (SingleTypeDeclaration)nb1.Children.Skip(1).Single();
            Assert.NotNull(c1);
            Assert.Equal(DeclarationKind.Class, c1.Kind);
            Assert.Equal("C", c1.Name);
            Assert.Equal(0, c1.Arity);
            var d1 = ct1.Children.Single();
            var d2 = ct2.Children.Single();
            Assert.NotNull(d1);
            Assert.NotNull(d2);
            Assert.Equal(DeclarationKind.Class, d1.Kind);
            Assert.Equal(DeclarationKind.Class, d2.Kind);
            Assert.Equal("D", d1.Name);
            Assert.Equal("D", d2.Name);
            Assert.Equal(0, d1.Arity);
            Assert.Equal(0, d2.Arity);
            Assert.Equal(0, d1.Children.Length);
            Assert.Equal(0, d2.Children.Length);

            var table = DeclarationTable.Empty;
            var mr = table.CalculateMergedRoot(null);
            Assert.NotNull(mr);
            Assert.True(mr.Declarations.IsEmpty);
            Assert.True(table.TypeNames.IsEmpty());

            table = table.AddRootDeclaration(Lazy(decl1));
            mr = table.CalculateMergedRoot(null);

            Assert.Equal(mr.Declarations, new[] { decl1 });
            Assert.True(table.TypeNames.OrderBy(s => s).SequenceEqual(new[] { "C", "D" }));

            Assert.Equal(DeclarationKind.Namespace, mr.Kind);
            Assert.Equal(string.Empty, mr.Name);

            var na = mr.Children.Single();
            Assert.Equal(DeclarationKind.Namespace, na.Kind);
            Assert.Equal("NA", na.Name);

            var nb = na.Children.Single();
            Assert.Equal(DeclarationKind.Namespace, nb.Kind);
            Assert.Equal("NB", nb.Name);

            var ct = nb.Children.OfType<MergedTypeDeclaration>().Single(x => x.Arity == 1);
            Assert.Equal(1, ct.Arity);
            Assert.Equal(DeclarationKind.Class, ct.Kind);
            Assert.Equal("C", ct.Name);

            var c = nb.Children.OfType<MergedTypeDeclaration>().Single(x => x.Arity == 0);
            Assert.Equal(0, c.Arity);
            Assert.Equal(DeclarationKind.Class, c.Kind);
            Assert.Equal("C", c.Name);

            var d = ct.Children.Single();
            Assert.Equal(0, d.Arity);
            Assert.Equal(DeclarationKind.Class, d.Kind);
            Assert.Equal("D", d.Name);

            table = table.AddRootDeclaration(Lazy(decl2));
            mr = table.CalculateMergedRoot(null);

            Assert.True(table.TypeNames.Distinct().OrderBy(s => s).SequenceEqual(new[] { "C", "D" }));

            Assert.Equal(mr.Declarations, new[] { decl1, decl2 });

            Assert.Equal(DeclarationKind.Namespace, mr.Kind);
            Assert.Equal(string.Empty, mr.Name);

            na = mr.Children.Single();
            Assert.Equal(DeclarationKind.Namespace, na.Kind);
            Assert.Equal("NA", na.Name);

            nb = na.Children.Single();
            Assert.Equal(DeclarationKind.Namespace, nb.Kind);
            Assert.Equal("NB", nb.Name);

            ct = nb.Children.OfType<MergedTypeDeclaration>().Single(x => x.Arity == 1);
            Assert.Equal(1, ct.Arity);
            Assert.Equal(DeclarationKind.Class, ct.Kind);
            Assert.Equal("C", ct.Name);

            c = nb.Children.OfType<MergedTypeDeclaration>().Single(x => x.Arity == 0);
            Assert.Equal(0, c.Arity);
            Assert.Equal(DeclarationKind.Class, c.Kind);
            Assert.Equal("C", c.Name);

            d = ct.Children.Single();
            Assert.Equal(0, d.Arity);
            Assert.Equal(DeclarationKind.Class, d.Kind);
            Assert.Equal("D", d.Name);
        }

        private Lazy<RootSingleNamespaceDeclaration> Lazy(RootSingleNamespaceDeclaration decl)
        {
            return new Lazy<RootSingleNamespaceDeclaration>(() => decl);
        }

        [Fact]
        public void TestTypeNames()
        {
            var text1 = @"
namespace NA.NB
{
  partial class A<T>
  { 
    partial class B
    {
      int F;
    }
  }
}
";
            var text2 = @"
namespace NA
{
  namespace NB
  {
    partial class C<T>
    { 
      partial class D
      {
        void G() {};
      }
    }
  }
}
";
            var tree1 = SyntaxFactory.ParseSyntaxTree(text1);
            var tree2 = SyntaxFactory.ParseSyntaxTree(text2);
            Assert.NotNull(tree1);
            Assert.NotNull(tree2);
            var decl1 = Lazy(DeclarationTreeBuilder.ForTree(tree1, new CSharpCompilationOptions(OutputKind.ConsoleApplication).ScriptClassName, isSubmission: false));
            var decl2 = Lazy(DeclarationTreeBuilder.ForTree(tree2, new CSharpCompilationOptions(OutputKind.ConsoleApplication).ScriptClassName, isSubmission: false));

            var table = DeclarationTable.Empty;
            table = table.AddRootDeclaration(decl1);

            Assert.True(table.TypeNames.OrderBy(s => s).SequenceEqual(new[] { "A", "B" }));

            table = table.AddRootDeclaration(decl2);
            Assert.True(table.TypeNames.OrderBy(s => s).SequenceEqual(new[] { "A", "B", "C", "D" }));

            table = table.RemoveRootDeclaration(decl2);
            Assert.True(table.TypeNames.OrderBy(s => s).SequenceEqual(new[] { "A", "B" }));

            table = table.AddRootDeclaration(decl2);
            Assert.True(table.TypeNames.OrderBy(s => s).SequenceEqual(new[] { "A", "B", "C", "D" }));

            table = table.RemoveRootDeclaration(decl1);
            Assert.True(table.TypeNames.OrderBy(s => s).SequenceEqual(new[] { "C", "D" }));

            table = table.RemoveRootDeclaration(decl2);
            Assert.True(table.TypeNames.IsEmpty());
        }


        [Fact]
        public void Bug2038()
        {
            string code = @"
                    public public interface testiface {}";

            var comp = CSharpCompilation.Create(
                "Test.dll",
                new[] { SyntaxFactory.ParseSyntaxTree(code) },
                options: TestOptions.ReleaseDll);

            Assert.Equal(SymbolKind.NamedType, comp.GlobalNamespace.GetMembers()[0].Kind);
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void OnlyOneParse()
        {
            var underlyingTree = SyntaxFactory.ParseSyntaxTree(@"
using System;

class C
{
    public B X(B b) { return b; }
    C(){}
}
");
            var foreignType = SyntaxFactory.ParseSyntaxTree(@"
public class B
{
  public int member(string s) { return s.Length; }
  B(){}
}
");

            var countedTree = new CountedSyntaxTree(foreignType);

            var compilation = CreateCompilation(new SyntaxTree[] { underlyingTree, countedTree }, skipUsesIsNullable: true);

            var type = compilation.Assembly.GlobalNamespace.GetTypeMembers().First();
            Assert.Equal(1, countedTree.AccessCount);   // parse once to build the decl table

            // We shouldn't need to go back to syntax to get info about the member names.
            var memberNames = type.MemberNames;
            Assert.Equal(1, countedTree.AccessCount);

            // Getting the interfaces will cause us to do some more binding of the current type.
            var interfaces = type.Interfaces();
            Assert.Equal(1, countedTree.AccessCount);

            // Now bind the members.
            var method = (MethodSymbol)type.GetMembers().First();
            Assert.Equal(1, countedTree.AccessCount);

            // Once we have the method, we shouldn't need to go back to syntax again.
            var returnType = method.ReturnTypeWithAnnotations;
            Assert.Equal(1, countedTree.AccessCount);

            var parameterType = method.Parameters.Single();
            Assert.Equal(1, countedTree.AccessCount);
        }

        private class CountedSyntaxTree : CSharpSyntaxTree
        {
            private class Reference : SyntaxReference
            {
                private readonly CountedSyntaxTree _countedSyntaxTree;
                private readonly SyntaxReference _underlyingSyntaxReference;

                public Reference(CountedSyntaxTree countedSyntaxTree, SyntaxReference syntaxReference)
                {
                    _countedSyntaxTree = countedSyntaxTree;
                    _underlyingSyntaxReference = syntaxReference;
                }

                public override SyntaxTree SyntaxTree
                {
                    get
                    {
                        return _countedSyntaxTree;
                    }
                }

                public override TextSpan Span
                {
                    get { return _underlyingSyntaxReference.Span; }
                }

                public override SyntaxNode GetSyntax(CancellationToken cancellationToken)
                {
                    // Note: It's important for us to maintain identity of nodes/trees, so we find
                    // the equivalent node in our CountedSyntaxTree.
                    _countedSyntaxTree.AccessCount++;
                    var nodeInUnderlying = _underlyingSyntaxReference.GetSyntax(cancellationToken);


                    var token = _countedSyntaxTree.GetCompilationUnitRoot(cancellationToken).FindToken(nodeInUnderlying.SpanStart);
                    for (var node = token.Parent; node != null; node = node.Parent)
                    {
                        if (node.Span == nodeInUnderlying.Span && node.RawKind == nodeInUnderlying.RawKind)
                        {
                            return (CSharpSyntaxNode)node;
                        }
                    }

                    throw new Exception("Should have found the node");
                }
            }

            private readonly SyntaxTree _underlyingTree;
            private readonly CompilationUnitSyntax _root;

            public int AccessCount;

            public CountedSyntaxTree(SyntaxTree underlying)
            {
                Debug.Assert(underlying != null);
                Debug.Assert(underlying.HasCompilationUnitRoot);

                _underlyingTree = underlying;
                _root = CloneNodeAsRoot(_underlyingTree.GetCompilationUnitRoot(CancellationToken.None));
            }

            public override string FilePath
            {
                get { return _underlyingTree.FilePath; }
            }

            public override CSharpParseOptions Options
            {
                get { return (CSharpParseOptions)_underlyingTree.Options; }
            }

            public override CSharpSyntaxNode GetRoot(CancellationToken cancellationToken = default(CancellationToken))
            {
                AccessCount++;
                return _root;
            }

            public override bool TryGetRoot(out CSharpSyntaxNode root)
            {
                root = _root;
                AccessCount++;
                return true;
            }

            public override bool HasCompilationUnitRoot
            {
                get { return true; }
            }

            public override SourceText GetText(CancellationToken cancellationToken)
            {
                return _underlyingTree.GetText(cancellationToken);
            }

            public override bool TryGetText(out SourceText text)
            {
                return _underlyingTree.TryGetText(out text);
            }

            public override Encoding Encoding
            {
                get { return _underlyingTree.Encoding; }
            }

            public override int Length
            {
                get { return _underlyingTree.Length; }
            }

            public override ImmutableDictionary<string, ReportDiagnostic> DiagnosticOptions => throw new NotImplementedException();

            public override SyntaxReference GetReference(SyntaxNode node)
            {
                return new Reference(this, _underlyingTree.GetReference(node));
            }

            public override SyntaxTree WithChangedText(SourceText newText)
            {
                return _underlyingTree.WithChangedText(newText);
            }

            public override SyntaxTree WithRootAndOptions(SyntaxNode root, ParseOptions options)
            {
                throw new NotImplementedException();
            }

            public override SyntaxTree WithFilePath(string path)
            {
                throw new NotImplementedException();
            }

            public override SyntaxTree WithDiagnosticOptions(ImmutableDictionary<string, ReportDiagnostic> options)
            {
                throw new NotImplementedException();
            }
        }
    }
}
