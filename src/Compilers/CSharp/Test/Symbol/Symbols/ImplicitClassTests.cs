// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class ImplicitClassTests : CSharpTestBase
    {
        [Fact]
        public void ImplicitClassSymbol()
        {
            var c = CreateEmptyCompilation(@"
namespace N
{
    void Goo()
    {
    }
}
", new[] { MscorlibRef });
            var n = ((NamespaceSymbol)c.Assembly.GlobalNamespace.GetMembers("N").Single());
            var implicitClass = ((NamedTypeSymbol)n.GetMembers().Single());
            Assert.Equal(0, implicitClass.GetAttributes().Length);
            Assert.Equal(0, implicitClass.Interfaces().Length);
            Assert.Equal(c.ObjectType, implicitClass.BaseType());
            Assert.Equal(0, implicitClass.Arity);
            Assert.True(implicitClass.IsImplicitlyDeclared);
            Assert.Equal(SyntaxKind.NamespaceDeclaration, implicitClass.DeclaringSyntaxReferences.Single().GetSyntax().Kind());
            Assert.False(implicitClass.IsSubmissionClass);
            Assert.False(implicitClass.IsScriptClass);

            var c2 = CreateCompilationWithMscorlib45("", new[] { c.ToMetadataReference() });

            n = ((NamespaceSymbol)c2.GlobalNamespace.GetMembers("N").Single());
            implicitClass = ((NamedTypeSymbol)n.GetMembers().Single());
            Assert.IsType<CSharp.Symbols.Retargeting.RetargetingNamedTypeSymbol>(implicitClass);
            Assert.Equal(0, implicitClass.Interfaces().Length);
            Assert.Equal(c2.ObjectType, implicitClass.BaseType());
        }

        [Fact]
        public void ScriptClassSymbol()
        {
            var c = CreateCompilation(@"
base.ToString();
void Goo()
{
}
", parseOptions: TestOptions.Script);

            var scriptClass = (NamedTypeSymbol)c.Assembly.GlobalNamespace.GetMember("Script");
            Assert.Equal(0, scriptClass.GetAttributes().Length);
            Assert.Equal(0, scriptClass.Interfaces().Length);
            Assert.Null(scriptClass.BaseType());
            Assert.Equal(0, scriptClass.Arity);
            Assert.True(scriptClass.IsImplicitlyDeclared);
            Assert.Equal(SyntaxKind.CompilationUnit, scriptClass.DeclaringSyntaxReferences.Single().GetSyntax().Kind());
            Assert.False(scriptClass.IsSubmissionClass);
            Assert.True(scriptClass.IsScriptClass);

            var tree = c.SyntaxTrees.Single();
            var model = c.GetSemanticModel(tree);

            IEnumerable<IdentifierNameSyntax> identifiers = tree.GetCompilationUnitRoot().DescendantNodes().OfType<IdentifierNameSyntax>();
            var toStringIdentifier = identifiers.Where(node => node.Identifier.ValueText.Equals("ToString")).Single();

            Assert.Null(model.GetSymbolInfo(toStringIdentifier).Symbol);
        }

        [Fact, WorkItem(531535, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531535")]
        public void Events()
        {
            var c = CreateCompilationWithMscorlib45(@"
event System.Action e;
", parseOptions: TestOptions.Script);

            c.VerifyDiagnostics();

            var @event = c.ScriptClass.GetMember<EventSymbol>("e");
            Assert.False(@event.TypeWithAnnotations.IsDefault);
        }

        [WorkItem(598860, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/598860")]
        [Fact]
        public void AliasQualifiedNamespaceName()
        {
            var comp = CreateCompilation(@"
namespace N::A
{
    void Goo()
    {
    }
}
");
            // Used to assert.
            comp.VerifyDiagnostics(
                // (2,11): error CS7000: Unexpected use of an aliased name
                // namespace N::A
                Diagnostic(ErrorCode.ERR_UnexpectedAliasedName, "N::A"),
                // (4,10): error CS0116: A namespace does not directly contain members such as fields or methods
                //     void Goo()
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "Goo"));

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var namespaceDecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType<NamespaceDeclarationSyntax>().Single();
            var methodDecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();

            Assert.Equal("A", model.GetDeclaredSymbol(namespaceDecl).Name);
            Assert.Equal("Goo", model.GetDeclaredSymbol(methodDecl).Name);
        }
    }
}
