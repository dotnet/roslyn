// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AccessTests : CSharpTestBase
    {
        // Namespaces implicitly have public declared accessibility.
        [Fact]
        public void Access_3_5_1_a()
        {
            var text =
@"
namespace A {}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var global = comp.GlobalNamespace;
            var a = global.GetMembers("A").Single() as NamespaceSymbol;
            Assert.Equal(Accessibility.Public, a.DeclaredAccessibility);
            var errs = comp.GetSemanticModel(tree).GetDeclarationDiagnostics();
            Assert.Equal(0, errs.Count());
        }

        // No access modifiers are allowed on namespace declarations
        [Fact]
        public void Access_3_5_1_b()
        {
            var text =
@"
public namespace A {}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var global = comp.GlobalNamespace;
            var a = global.GetMembers("A").Single() as NamespaceSymbol;
            var errs = tree.GetDiagnostics();
            Assert.Equal(0, errs.Count());

            comp.VerifyDiagnostics(
                // (2,1): error CS1671: A namespace declaration cannot have modifiers or attributes
                // public namespace A {}
                Diagnostic(ErrorCode.ERR_BadModifiersOnNamespace, "public").WithLocation(2, 1));
        }

        // Types declared in compilation units or namespaces can have public or internal declared accessibility.
        [Fact]
        public void Access_3_5_1_c()
        {
            var text =
@"
public class A {}
internal class B {}
namespace X {
    public class A {}
    internal class B {}
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var global = comp.GlobalNamespace;
            var a = global.GetMembers("A").Single() as NamespaceSymbol;
            var errs = comp.GetSemanticModel(tree).GetDeclarationDiagnostics();
            Assert.Equal(0, errs.Count());
        }

        // Corollary: Types declared in compilation units or namespaces cannot have private, protected, or protected internal accessibility.
        [Fact]
        public void Access_3_5_1_d()
        {
            var text =
@"
private class A {}
protected class B {}
protected internal class C {}
namespace X {
    private class A {}
    protected class B {}
    protected internal class C {}
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var global = comp.GlobalNamespace;
            var a = global.GetMembers("A").Single() as NamespaceSymbol;
            var errs = comp.GetSemanticModel(tree).GetDeclarationDiagnostics();
            Assert.Equal(6, errs.Count());
        }

        // TODO: Types declared in compilation units or namespaces default to internal declared accessibility.
        // TODO: Class members can have any of the five kinds of declared accessibility. 
        // TODO: Class members default to private declared accessibility.
        // TODO: A type declared as a member of a namespace can have only public or internal declared accessibility.
        // TODO: Struct members can have public, internal, or private declared accessibility.
        // TODO: Struct members default to private declared accessibility.
        // TODO: Interface members implicitly have public declared accessibility.
        // TODO: No access modifiers are allowed on interface member declarations.
        // TODO: Enumeration members implicitly have public declared accessibility.
        // TODO: No access modifiers are allowed on enumeration member declarations.

        [WorkItem(538257, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538257")]
        [Fact]
        public void AccessInternalClassWithPublicConstructor()
        {
            var text = @"
class C1
{
    public void M1(C1 c) { }
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            Assert.False(comp.GetDeclarationDiagnostics().Any());
        }

        [WorkItem(538257, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538257")]
        [Fact]
        public void ProtectedAcrossClasses01()
        {
            var text = @"
public class C<T>
{
    protected class A { }
}
public class E
{
    protected class D : C<D>
    {
        public class B : A { }
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            Assert.Equal(1, comp.GetDeclarationDiagnostics().Count());
        }

        [WorkItem(539147, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539147")]
        [Fact]
        public void ProtectedAcrossClasses02()
        {
            var text = @"
class B
{
    protected class I
    {
    }
}
class D : B
{
    protected void M(I i)
    {
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            Assert.Equal(0, comp.GetDeclarationDiagnostics().Count());
        }
    }
}
