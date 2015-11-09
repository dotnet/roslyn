// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            var c = CreateCompilationWithMscorlib(@"
namespace N
{
    void Foo()
    {
    }
}
");
            var n = ((NamespaceSymbol)c.Assembly.GlobalNamespace.GetMembers("N").Single());
            var implicitClass = ((NamedTypeSymbol)n.GetMembers().Single());
            Assert.Equal(0, implicitClass.GetAttributes().Length);
            Assert.Equal(0, implicitClass.Interfaces.Length);
            Assert.Equal(c.ObjectType, implicitClass.BaseType);
            Assert.Equal(0, implicitClass.Arity);
            Assert.True(implicitClass.IsImplicitlyDeclared);
            Assert.Equal(SyntaxKind.NamespaceDeclaration, implicitClass.DeclaringSyntaxReferences.Single().GetSyntax().Kind());
            Assert.False(implicitClass.IsSubmissionClass);
            Assert.False(implicitClass.IsScriptClass);

            var c2 = CreateCompilationWithMscorlib45("", new[] { c.ToMetadataReference() });

            n = ((NamespaceSymbol)c2.GlobalNamespace.GetMembers("N").Single());
            implicitClass = ((NamedTypeSymbol)n.GetMembers().Single());
            Assert.IsType<CSharp.Symbols.Retargeting.RetargetingNamedTypeSymbol>(implicitClass);
            Assert.Equal(0, implicitClass.Interfaces.Length);
            Assert.Equal(c2.ObjectType, implicitClass.BaseType);
        }

        [Fact]
        public void ScriptClassSymbol()
        {
            var c = CreateCompilationWithMscorlib(@"
void Foo()
{
}
", parseOptions: TestOptions.Script);

            var scriptClass = ((NamedTypeSymbol)c.Assembly.GlobalNamespace.GetMembers().Single());
            Assert.Equal(0, scriptClass.GetAttributes().Length);
            Assert.Equal(0, scriptClass.Interfaces.Length);
            Assert.Equal(c.ObjectType, scriptClass.BaseType);
            Assert.Equal(0, scriptClass.Arity);
            Assert.True(scriptClass.IsImplicitlyDeclared);
            Assert.Equal(SyntaxKind.CompilationUnit, scriptClass.DeclaringSyntaxReferences.Single().GetSyntax().Kind());
            Assert.False(scriptClass.IsSubmissionClass);
            Assert.True(scriptClass.IsScriptClass);
        }

        [Fact, WorkItem(531535, "DevDiv")]
        public void Events()
        {
            var c = CreateCompilationWithMscorlib45(@"
event System.Action e;
", parseOptions: TestOptions.Script);

            c.VerifyDiagnostics();

            var evnt = c.ScriptClass.GetMember<EventSymbol>("e");
            Assert.NotNull(evnt.Type);
        }

        [WorkItem(598860, "DevDiv")]
        [Fact]
        public void AliasQualifiedNamespaceName()
        {
            var comp = CreateCompilationWithMscorlib(@"
namespace N::A
{
    void Foo()
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
                //     void Foo()
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "Foo"));

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var namespaceDecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType<NamespaceDeclarationSyntax>().Single();
            var methodDecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();

            Assert.Equal("A", model.GetDeclaredSymbol(namespaceDecl).Name);
            Assert.Equal("Foo", model.GetDeclaredSymbol(methodDecl).Name);
        }
    }
}
