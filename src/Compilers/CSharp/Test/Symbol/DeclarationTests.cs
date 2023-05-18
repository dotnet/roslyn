// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
            var decl1 = DeclarationTreeBuilder.ForTree(tree1, TestOptions.DebugExe.ScriptClassName, isSubmission: false);
            var decl2 = DeclarationTreeBuilder.ForTree(tree2, TestOptions.DebugExe.ScriptClassName, isSubmission: false);
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
            var decl1 = Lazy(DeclarationTreeBuilder.ForTree(tree1, TestOptions.DebugExe.ScriptClassName, isSubmission: false));
            var decl2 = Lazy(DeclarationTreeBuilder.ForTree(tree2, TestOptions.DebugExe.ScriptClassName, isSubmission: false));

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

        [ConditionalFact(typeof(NoIOperationValidation), typeof(NoUsedAssembliesValidation))]
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

            var compilation = CreateCompilation(new SyntaxTree[] { underlyingTree, countedTree }, skipUsesIsNullable: true, options: TestOptions.ReleaseDll);

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

        [ConditionalFact(typeof(NoIOperationValidation), typeof(NoUsedAssembliesValidation))]
        public void OnlyOneParse_WithReservedTypeName()
        {
            var underlyingTree = SyntaxFactory.ParseSyntaxTree(@"
using System;

class c
{
    public b X(b b1) { return b1; }
    c(){}
}
");
            var foreignType = SyntaxFactory.ParseSyntaxTree(@"
public class b
{
  public int member(string s) { return s.Length; }
  b(){}
}
");

            var countedTree = new CountedSyntaxTree(foreignType);

            var compilation = CreateCompilation(new SyntaxTree[] { underlyingTree, countedTree }, skipUsesIsNullable: true, options: TestOptions.ReleaseDll);

            var type = compilation.Assembly.GlobalNamespace.GetTypeMembers().First();
            Assert.Equal(1, countedTree.AccessCount);

            var memberNames = type.MemberNames;
            Assert.Equal(1, countedTree.AccessCount);

            var interfaces = type.Interfaces();
            Assert.Equal(1, countedTree.AccessCount);

            var method = (MethodSymbol)type.GetMembers().First();
            Assert.Equal(1, countedTree.AccessCount);

            var returnType = method.ReturnTypeWithAnnotations;
            Assert.Equal(1, countedTree.AccessCount);

            var parameterType = method.Parameters.Single();
            Assert.Equal(1, countedTree.AccessCount);
        }

        [Fact]
        public void TestMemberNamesReused1()
        {
            var firstTree = SyntaxFactory.ParseSyntaxTree(@"
using System;

namespace N1
{
    namespace N2.N3
    {
        class C
        {
            int x, y, z;
        }
    }
}
");
            var secondTree = SyntaxFactory.ParseSyntaxTree(@"
using System;

namespace N1
{
    namespace N2.N3
    {
        class C
        {
            int z, y, x;
        }
    }
}
");
            var thirdTree = SyntaxFactory.ParseSyntaxTree(@"
using System;

namespace N1
{
    namespace N2.N3
    {
        class C
        {
            int z, y, x, w;
        }
    }
}
");

            var compilation = CreateCompilation(new SyntaxTree[] { firstTree });

            var type1 = (SourceNamedTypeSymbol)compilation.GetTypeByMetadataName("N1.N2.N3.C");
            Assert.True(type1.MergedDeclaration.Declarations[0].MemberNames.Value.SetEquals(new[] { "x", "y", "z" }));

            compilation = compilation.ReplaceSyntaxTree(firstTree, secondTree);

            var type2 = (SourceNamedTypeSymbol)compilation.GetTypeByMetadataName("N1.N2.N3.C");
            Assert.True(type2.MergedDeclaration.Declarations[0].MemberNames.Value.SetEquals(new[] { "x", "y", "z" }));

            // We should have the exact same set for the names.
            Assert.Same(type1.MergedDeclaration.Declarations[0].MemberNames, type2.MergedDeclaration.Declarations[0].MemberNames);

            compilation = compilation.ReplaceSyntaxTree(secondTree, thirdTree);

            var type3 = (SourceNamedTypeSymbol)compilation.GetTypeByMetadataName("N1.N2.N3.C");
            Assert.True(type3.MergedDeclaration.Declarations[0].MemberNames.Value.SetEquals(new[] { "w", "x", "y", "z" }));

            Assert.NotSame(type1.MergedDeclaration.Declarations[0].MemberNames, type3.MergedDeclaration.Declarations[0].MemberNames);
        }

        [Fact]
        public void TestMemberNamesReused_UnaffectedByTypeNameChange()
        {
            var firstTree = SyntaxFactory.ParseSyntaxTree(@"
using System;

namespace N1
{
    namespace N2.N3
    {
        class C
        {
            int x, y, z;
        }
    }
}
");
            var secondTree = SyntaxFactory.ParseSyntaxTree(@"
using System;

namespace N1
{
    namespace N2.N3
    {
        class D
        {
            int z, y, x;
        }
    }
}
");

            var compilation = CreateCompilation(new SyntaxTree[] { firstTree });

            var type1 = (SourceNamedTypeSymbol)compilation.GetTypeByMetadataName("N1.N2.N3.C");
            Assert.True(type1.MergedDeclaration.Declarations[0].MemberNames.Value.SetEquals(new[] { "x", "y", "z" }));

            compilation = compilation.ReplaceSyntaxTree(firstTree, secondTree);

            var type2 = (SourceNamedTypeSymbol)compilation.GetTypeByMetadataName("N1.N2.N3.D");
            Assert.True(type2.MergedDeclaration.Declarations[0].MemberNames.Value.SetEquals(new[] { "x", "y", "z" }));

            // We should have the exact same set for the names.
            Assert.Same(type1.MergedDeclaration.Declarations[0].MemberNames, type2.MergedDeclaration.Declarations[0].MemberNames);
        }

        [Fact]
        public void TestMemberNamesReused_UnaffectedByNamespaceChange()
        {
            var firstTree = SyntaxFactory.ParseSyntaxTree(@"
using System;

namespace N1
{
    namespace N2.N3
    {
        class C
        {
            int x, y, z;
        }
    }
}
");
            var secondTree = SyntaxFactory.ParseSyntaxTree(@"
using System;

namespace N1
{
    class C
    {
        int z, y, x;
    }
}
");

            var compilation = CreateCompilation(new SyntaxTree[] { firstTree });

            var type1 = (SourceNamedTypeSymbol)compilation.GetTypeByMetadataName("N1.N2.N3.C");
            Assert.True(type1.MergedDeclaration.Declarations[0].MemberNames.Value.SetEquals(new[] { "x", "y", "z" }));

            compilation = compilation.ReplaceSyntaxTree(firstTree, secondTree);

            var type2 = (SourceNamedTypeSymbol)compilation.GetTypeByMetadataName("N1.C");
            Assert.True(type2.MergedDeclaration.Declarations[0].MemberNames.Value.SetEquals(new[] { "x", "y", "z" }));

            // We should have the exact same set for the names.
            Assert.Same(type1.MergedDeclaration.Declarations[0].MemberNames, type2.MergedDeclaration.Declarations[0].MemberNames);
        }

        [Fact]
        public void TestMemberNamesReused_SupportsMultipleTypes()
        {
            var firstTree = SyntaxFactory.ParseSyntaxTree(@"
using System;

namespace N1
{
    namespace N2.N3
    {
        class C
        {
            int x, y, z;
        }

        class D
        {
            int a, b, c;
        }
    }
}
");
            var secondTree = SyntaxFactory.ParseSyntaxTree(@"
using System;

namespace N1
{
    class C
    {
        int z, y, x;
    }

    class D
    {
        int a, b, c;
    }
}
");

            var compilation = CreateCompilation(new SyntaxTree[] { firstTree });

            var type1a = (SourceNamedTypeSymbol)compilation.GetTypeByMetadataName("N1.N2.N3.C");
            Assert.True(type1a.MergedDeclaration.Declarations[0].MemberNames.Value.SetEquals(new[] { "x", "y", "z" }));

            var type1b = (SourceNamedTypeSymbol)compilation.GetTypeByMetadataName("N1.N2.N3.D");
            Assert.True(type1b.MergedDeclaration.Declarations[0].MemberNames.Value.SetEquals(new[] { "a", "b", "c" }));

            compilation = compilation.ReplaceSyntaxTree(firstTree, secondTree);

            var type2a = (SourceNamedTypeSymbol)compilation.GetTypeByMetadataName("N1.C");
            Assert.True(type2a.MergedDeclaration.Declarations[0].MemberNames.Value.SetEquals(new[] { "x", "y", "z" }));

            var type2b = (SourceNamedTypeSymbol)compilation.GetTypeByMetadataName("N1.D");
            Assert.True(type2b.MergedDeclaration.Declarations[0].MemberNames.Value.SetEquals(new[] { "a", "b", "c" }));

            // We should have the exact same set for the names.
            Assert.Same(type1a.MergedDeclaration.Declarations[0].MemberNames, type2a.MergedDeclaration.Declarations[0].MemberNames);
            Assert.Same(type1b.MergedDeclaration.Declarations[0].MemberNames, type2b.MergedDeclaration.Declarations[0].MemberNames);
        }

        [Fact]
        public void TestMemberNamesReused_SupportsMultipleTypes_TypeRemoved()
        {
            var firstTree = SyntaxFactory.ParseSyntaxTree(@"
using System;

namespace N1
{
    namespace N2.N3
    {
        class C
        {
            int x, y, z;
        }

        class D
        {
            int a, b, c;
        }
    }
}
");
            var secondTree = SyntaxFactory.ParseSyntaxTree(@"
using System;

namespace N1
{
    class C
    {
        int z, y, x;
    }
}
");

            var compilation = CreateCompilation(new SyntaxTree[] { firstTree });

            var type1a = (SourceNamedTypeSymbol)compilation.GetTypeByMetadataName("N1.N2.N3.C");
            Assert.True(type1a.MergedDeclaration.Declarations[0].MemberNames.Value.SetEquals(new[] { "x", "y", "z" }));

            var type1b = (SourceNamedTypeSymbol)compilation.GetTypeByMetadataName("N1.N2.N3.D");
            Assert.True(type1b.MergedDeclaration.Declarations[0].MemberNames.Value.SetEquals(new[] { "a", "b", "c" }));

            compilation = compilation.ReplaceSyntaxTree(firstTree, secondTree);

            var type2a = (SourceNamedTypeSymbol)compilation.GetTypeByMetadataName("N1.C");
            Assert.True(type2a.MergedDeclaration.Declarations[0].MemberNames.Value.SetEquals(new[] { "x", "y", "z" }));

            var type2b = (SourceNamedTypeSymbol)compilation.GetTypeByMetadataName("N1.D");
            Assert.Null(type2b);

            // We should have the exact same set for the names.
            Assert.Same(type1a.MergedDeclaration.Declarations[0].MemberNames, type2a.MergedDeclaration.Declarations[0].MemberNames);
        }

        [Fact]
        public void TestMemberNamesReused_SupportsEnums()
        {
            var firstTree = SyntaxFactory.ParseSyntaxTree(@"
using System;

namespace N1
{
    namespace N2.N3
    {
        enum E
        {
            x, y, z;
        }
    }
}
");
            var secondTree = SyntaxFactory.ParseSyntaxTree(@"
using System;

namespace N1
{
    enum E
    {
        z, y, x;
    }
}
");

            var compilation = CreateCompilation(new SyntaxTree[] { firstTree });

            var type1a = (SourceNamedTypeSymbol)compilation.GetTypeByMetadataName("N1.N2.N3.E");
            Assert.True(type1a.MergedDeclaration.Declarations[0].MemberNames.Value.SetEquals(new[] { "x", "y", "z" }));

            compilation = compilation.ReplaceSyntaxTree(firstTree, secondTree);

            var type2a = (SourceNamedTypeSymbol)compilation.GetTypeByMetadataName("N1.E");
            Assert.True(type2a.MergedDeclaration.Declarations[0].MemberNames.Value.SetEquals(new[] { "x", "y", "z" }));

            // We should have the exact same set for the names.
            Assert.Same(type1a.MergedDeclaration.Declarations[0].MemberNames, type2a.MergedDeclaration.Declarations[0].MemberNames);
        }

        [Fact]
        public void TestMemberNamesReused_SupportsStructs()
        {
            var firstTree = SyntaxFactory.ParseSyntaxTree(@"
using System;

namespace N1
{
    namespace N2.N3
    {
        struct S
        {
            int x, y, z;
        }
    }
}
");
            var secondTree = SyntaxFactory.ParseSyntaxTree(@"
using System;

namespace N1
{
    struct S
    {
        int z, y, x;
    }
}
");

            var compilation = CreateCompilation(new SyntaxTree[] { firstTree });

            var type1a = (SourceNamedTypeSymbol)compilation.GetTypeByMetadataName("N1.N2.N3.S");
            Assert.True(type1a.MergedDeclaration.Declarations[0].MemberNames.Value.SetEquals(new[] { "x", "y", "z" }));

            compilation = compilation.ReplaceSyntaxTree(firstTree, secondTree);

            var type2a = (SourceNamedTypeSymbol)compilation.GetTypeByMetadataName("N1.S");
            Assert.True(type2a.MergedDeclaration.Declarations[0].MemberNames.Value.SetEquals(new[] { "x", "y", "z" }));

            // We should have the exact same set for the names.
            Assert.Same(type1a.MergedDeclaration.Declarations[0].MemberNames, type2a.MergedDeclaration.Declarations[0].MemberNames);
        }

        [Fact]
        public void TestMemberNamesReused_SupportsInterfaces()
        {
            var firstTree = SyntaxFactory.ParseSyntaxTree(@"
using System;

namespace N1
{
    namespace N2.N3
    {
        interface I
        {
            int x { get; }
            int y { get; }
            int z { get; }
        }
    }
}
");
            var secondTree = SyntaxFactory.ParseSyntaxTree(@"
using System;

namespace N1
{
    interface I
    {
        int z { get; }
        int y { get; }
        int x { get; }
    }
}
");

            var compilation = CreateCompilation(new SyntaxTree[] { firstTree });

            var type1a = (SourceNamedTypeSymbol)compilation.GetTypeByMetadataName("N1.N2.N3.I");
            Assert.True(type1a.MergedDeclaration.Declarations[0].MemberNames.Value.SetEquals(new[] { "x", "y", "z" }));

            compilation = compilation.ReplaceSyntaxTree(firstTree, secondTree);

            var type2a = (SourceNamedTypeSymbol)compilation.GetTypeByMetadataName("N1.I");
            Assert.True(type2a.MergedDeclaration.Declarations[0].MemberNames.Value.SetEquals(new[] { "x", "y", "z" }));

            // We should have the exact same set for the names.
            Assert.Same(type1a.MergedDeclaration.Declarations[0].MemberNames, type2a.MergedDeclaration.Declarations[0].MemberNames);
        }

        [Fact]
        public void TestMemberNamesReused_UnaffectedByDelegate1()
        {
            var firstTree = SyntaxFactory.ParseSyntaxTree(@"
using System;

namespace N1
{
    namespace N2.N3
    {
        delegate void D();

        enum E
        {
            x, y, z;
        }
    }
}
");
            var secondTree = SyntaxFactory.ParseSyntaxTree(@"
using System;

namespace N1
{
    enum E
    {
        z, y, x;
    }
}
");

            var compilation = CreateCompilation(new SyntaxTree[] { firstTree });

            var type1a = (SourceNamedTypeSymbol)compilation.GetTypeByMetadataName("N1.N2.N3.E");
            Assert.True(type1a.MergedDeclaration.Declarations[0].MemberNames.Value.SetEquals(new[] { "x", "y", "z" }));

            compilation = compilation.ReplaceSyntaxTree(firstTree, secondTree);

            var type2a = (SourceNamedTypeSymbol)compilation.GetTypeByMetadataName("N1.E");
            Assert.True(type2a.MergedDeclaration.Declarations[0].MemberNames.Value.SetEquals(new[] { "x", "y", "z" }));

            // We should have the exact same set for the names.
            Assert.Same(type1a.MergedDeclaration.Declarations[0].MemberNames, type2a.MergedDeclaration.Declarations[0].MemberNames);
        }

        [Fact]
        public void TestMemberNamesReused_UnaffectedByDelegate2()
        {
            var firstTree = SyntaxFactory.ParseSyntaxTree(@"
using System;

namespace N1
{
    namespace N2.N3
    {
        enum E
        {
            x, y, z;
        }
    }
}
");
            var secondTree = SyntaxFactory.ParseSyntaxTree(@"
using System;

namespace N1
{
    delegate void D();

    enum E
    {
        z, y, x;
    }
}
");

            var compilation = CreateCompilation(new SyntaxTree[] { firstTree });

            var type1a = (SourceNamedTypeSymbol)compilation.GetTypeByMetadataName("N1.N2.N3.E");
            Assert.True(type1a.MergedDeclaration.Declarations[0].MemberNames.Value.SetEquals(new[] { "x", "y", "z" }));

            compilation = compilation.ReplaceSyntaxTree(firstTree, secondTree);

            var type2a = (SourceNamedTypeSymbol)compilation.GetTypeByMetadataName("N1.E");
            Assert.True(type2a.MergedDeclaration.Declarations[0].MemberNames.Value.SetEquals(new[] { "x", "y", "z" }));

            // We should have the exact same set for the names.
            Assert.Same(type1a.MergedDeclaration.Declarations[0].MemberNames, type2a.MergedDeclaration.Declarations[0].MemberNames);
        }

        [Fact]
        public void TestMemberNamesReused_SameTreeDifferentCompilations()
        {
            var firstTree = SyntaxFactory.ParseSyntaxTree(@"
using System;

namespace N1
{
    namespace N2.N3
    {
        enum E
        {
            x, y, z;
        }
    }
}
");

            var compilation1 = CreateCompilation(new SyntaxTree[] { firstTree });
            var compilation2 = CreateCompilation(new SyntaxTree[] { firstTree });

            var type1 = (SourceNamedTypeSymbol)compilation1.GetTypeByMetadataName("N1.N2.N3.E");
            var type2 = (SourceNamedTypeSymbol)compilation2.GetTypeByMetadataName("N1.N2.N3.E");
            Assert.True(type1.MergedDeclaration.Declarations[0].MemberNames.Value.SetEquals(new[] { "x", "y", "z" }));
            Assert.True(type2.MergedDeclaration.Declarations[0].MemberNames.Value.SetEquals(new[] { "x", "y", "z" }));

            // We should have the exact same set for the names.
            Assert.Same(type1.MergedDeclaration.Declarations[0].MemberNames, type2.MergedDeclaration.Declarations[0].MemberNames);
        }

        /// <remarks>
        /// When using this type, make sure to pass an explicit CompilationOptions to CreateCompilation, as the check
        /// to see whether the syntax tree has top-level statements will increment the counter.
        /// </remarks>
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
                    var nodeInUnderlying = _underlyingSyntaxReference.GetSyntax(cancellationToken);

                    // Note: GetCompilationUnitRoot increments AccessCount
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

            [Obsolete]
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
        }
    }
}
