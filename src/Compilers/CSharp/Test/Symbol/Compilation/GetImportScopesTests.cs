// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class GetImportScopesTests : SemanticModelTestBase
    {
        [Fact]
        public void TestEmptyFile()
        {
            var text = @"/*pos*/";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var scopes = model.GetImportScopes(GetPositionForBinding(text));
            Assert.Empty(scopes);
        }

        [Fact]
        public void TestNoImportsBeforeMemberDeclaration()
        {
            var text = @"/*pos*/
class C {}";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var scopes = model.GetImportScopes(GetPositionForBinding(text));
            Assert.Empty(scopes);
        }

        #region normal imports

        [Fact]
        public void TestBeforeImports()
        {
            var text = @"/*pos*/
using System;";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var scopes = model.GetImportScopes(GetPositionForBinding(text));
            Assert.Single(scopes);
            Assert.Single(scopes.Single().Imports);
            Assert.True(scopes.Single().Imports.Single().NamespaceOrType is INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: true, Name: nameof(System) });
            Assert.True(scopes.Single().Imports.Single().DeclaringSyntaxReference!.GetSyntax() is UsingDirectiveSyntax);
            Assert.Empty(scopes.Single().Aliases);
            Assert.Empty(scopes.Single().ExternAliases);
            Assert.Empty(scopes.Single().XmlNamespaces);
        }

        [Fact]
        public void TestAfterImportsNoContent()
        {
            var text = @"
using System;
/*pos*/";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var scopes = model.GetImportScopes(GetPositionForBinding(text));
            Assert.Single(scopes);
            Assert.Single(scopes.Single().Imports);
            Assert.True(scopes.Single().Imports.Single().NamespaceOrType is INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: true, Name: nameof(System) });
            Assert.True(scopes.Single().Imports.Single().DeclaringSyntaxReference!.GetSyntax() is UsingDirectiveSyntax);
            Assert.Empty(scopes.Single().Aliases);
            Assert.Empty(scopes.Single().ExternAliases);
            Assert.Empty(scopes.Single().XmlNamespaces);
        }

        [Fact]
        public void TestAfterImportsBeforeMemberDeclaration()
        {
            var text = @"
using System;
/*pos*/
class C
{
}";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var scopes = model.GetImportScopes(GetPositionForBinding(text));
            Assert.Empty(scopes);
        }

        [Fact]
        public void TestBeforeImportsTopLevelStatements()
        {
            var text = @"
/*pos*/
using System;

return;";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var scopes = model.GetImportScopes(GetPositionForBinding(text));
            Assert.Single(scopes);
            Assert.Single(scopes.Single().Imports);
            Assert.True(scopes.Single().Imports.Single().NamespaceOrType is INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: true, Name: nameof(System) });
            Assert.True(scopes.Single().Imports.Single().DeclaringSyntaxReference!.GetSyntax() is UsingDirectiveSyntax);
            Assert.Empty(scopes.Single().Aliases);
            Assert.Empty(scopes.Single().ExternAliases);
            Assert.Empty(scopes.Single().XmlNamespaces);
        }

        [Fact]
        public void TestAfterImportsTopLevelStatements1()
        {
            var text = @"
using System;
/*pos*/
return;";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var scopes = model.GetImportScopes(GetPositionForBinding(text));
            Assert.Empty(scopes);
        }

        [Fact]
        public void TestAfterImportsTopLevelStatements2()
        {
            var text = @"
using System;

return /*pos*/;";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var scopes = model.GetImportScopes(GetPositionForBinding(text));
            Assert.Single(scopes);
            Assert.Single(scopes.Single().Imports);
            Assert.True(scopes.Single().Imports.Single().NamespaceOrType is INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: true, Name: nameof(System) });
            Assert.True(scopes.Single().Imports.Single().DeclaringSyntaxReference!.GetSyntax() is UsingDirectiveSyntax);
            Assert.Empty(scopes.Single().Aliases);
            Assert.Empty(scopes.Single().ExternAliases);
            Assert.Empty(scopes.Single().XmlNamespaces);
        }

        [Fact]
        public void TestAfterMultipleImportsNoContent()
        {
            var text = @"
using System;
using Microsoft;
/*pos*/";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var scopes = model.GetImportScopes(GetPositionForBinding(text));
            Assert.Single(scopes);
            Assert.Equal(2, scopes.Single().Imports.Length);
            Assert.True(scopes.Single().Imports.First().NamespaceOrType is INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: true, Name: nameof(System) });
            Assert.True(scopes.Single().Imports.Last().NamespaceOrType is INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: true, Name: nameof(Microsoft) });
            Assert.True(scopes.Single().Imports.First().DeclaringSyntaxReference!.GetSyntax() is UsingDirectiveSyntax { Name: IdentifierNameSyntax { Identifier.Text: nameof(System) } });
            Assert.True(scopes.Single().Imports.Last().DeclaringSyntaxReference!.GetSyntax() is UsingDirectiveSyntax { Name: IdentifierNameSyntax { Identifier.Text: nameof(Microsoft) } });
            Assert.Empty(scopes.Single().Aliases);
            Assert.Empty(scopes.Single().ExternAliases);
            Assert.Empty(scopes.Single().XmlNamespaces);
        }

        [Fact]
        public void TestNestedNamespaceOuterPosition()
        {
            var text = @"
using System;

class C
{
    /*pos*/
}

namespace N
{
    using Microsoft;
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var scopes = model.GetImportScopes(GetPositionForBinding(text));
            Assert.Single(scopes);
            Assert.Single(scopes.Single().Imports);
            Assert.True(scopes.Single().Imports.Single().NamespaceOrType is INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: true, Name: nameof(System) });
            Assert.True(scopes.Single().Imports.Single().DeclaringSyntaxReference!.GetSyntax() is UsingDirectiveSyntax { Name: IdentifierNameSyntax { Identifier.Text: nameof(System) } });
        }

        [Fact]
        public void TestNestedNamespaceInnerPosition()
        {
            var text = @"
using System;

namespace N
{
    using Microsoft;
    class C
    {
        /*pos*/
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var scopes = model.GetImportScopes(GetPositionForBinding(text));
            Assert.Equal(2, scopes.Length);
            Assert.Single(scopes[0].Imports);
            Assert.Single(scopes[1].Imports);
            Assert.True(scopes[0].Imports.Single().NamespaceOrType is INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: true, Name: nameof(Microsoft) });
            Assert.True(scopes[0].Imports.Single().DeclaringSyntaxReference!.GetSyntax() is UsingDirectiveSyntax { Name: IdentifierNameSyntax { Identifier.Text: nameof(Microsoft) } });
            Assert.True(scopes[1].Imports.Single().NamespaceOrType is INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: true, Name: nameof(System) });
            Assert.True(scopes[1].Imports.Single().DeclaringSyntaxReference!.GetSyntax() is UsingDirectiveSyntax { Name: IdentifierNameSyntax { Identifier.Text: nameof(System) } });
        }

        #endregion

        #region aliases

        [Fact]
        public void TestBeforeAlias()
        {
            var text = @"/*pos*/
using S = System;";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var scopes = model.GetImportScopes(GetPositionForBinding(text));
            Assert.Single(scopes);
            Assert.Single(scopes.Single().Aliases);
            Assert.True(scopes.Single().Aliases.Single() is { Name: "S", Target: INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: true, Name: nameof(System) } });
            Assert.True(scopes.Single().Aliases.Single().DeclaringSyntaxReferences.Single().GetSyntax() is UsingDirectiveSyntax);
            Assert.Empty(scopes.Single().Imports);
            Assert.Empty(scopes.Single().ExternAliases);
            Assert.Empty(scopes.Single().XmlNamespaces);
        }

        [Fact]
        public void TestAfterAliasNoContent()
        {
            var text = @"
using S = System;
/*pos*/";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var scopes = model.GetImportScopes(GetPositionForBinding(text));
            Assert.Single(scopes);
            Assert.Single(scopes.Single().Aliases);
            Assert.True(scopes.Single().Aliases.Single() is { Name: "S", Target: { ContainingNamespace.IsGlobalNamespace: true, Name: nameof(System) } });
            Assert.True(scopes.Single().Aliases.Single().DeclaringSyntaxReferences.Single().GetSyntax() is UsingDirectiveSyntax);
            Assert.Empty(scopes.Single().Imports);
            Assert.Empty(scopes.Single().ExternAliases);
            Assert.Empty(scopes.Single().XmlNamespaces);
        }

        [Fact]
        public void TestAfterAliasBeforeMemberDeclaration()
        {
            var text = @"
using S = System;
/*pos*/
class C
{
}";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var scopes = model.GetImportScopes(GetPositionForBinding(text));
            Assert.Empty(scopes);
        }

        [Fact]
        public void TestBeforeAliasTopLevelStatements()
        {
            var text = @"
/*pos*/
using S = System;

return;";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var scopes = model.GetImportScopes(GetPositionForBinding(text));
            Assert.Single(scopes);
            Assert.Single(scopes.Single().Aliases);
            Assert.True(scopes.Single().Aliases.Single() is { Name: "S", Target: INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: true, Name: nameof(System) } });
            Assert.True(scopes.Single().Aliases.Single().DeclaringSyntaxReferences.Single().GetSyntax() is UsingDirectiveSyntax);
            Assert.Empty(scopes.Single().Imports);
            Assert.Empty(scopes.Single().ExternAliases);
            Assert.Empty(scopes.Single().XmlNamespaces);
        }

        [Fact]
        public void TestAfterAliasTopLevelStatements1()
        {
            var text = @"
using S = System;
/*pos*/
return;";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var scopes = model.GetImportScopes(GetPositionForBinding(text));
            Assert.Empty(scopes);
        }

        [Fact]
        public void TestAfterAliasTopLevelStatements2()
        {
            var text = @"
using S = System;

return /*pos*/;";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var scopes = model.GetImportScopes(GetPositionForBinding(text));
            Assert.Single(scopes);
            Assert.Single(scopes.Single().Aliases);
            Assert.True(scopes.Single().Aliases.Single() is { Name: "S", Target: { ContainingNamespace.IsGlobalNamespace: true, Name: nameof(System) } });
            Assert.True(scopes.Single().Aliases.Single().DeclaringSyntaxReferences.Single().GetSyntax() is UsingDirectiveSyntax);
            Assert.Empty(scopes.Single().Imports);
            Assert.Empty(scopes.Single().ExternAliases);
            Assert.Empty(scopes.Single().XmlNamespaces);
        }

        [Fact]
        public void TestAfterMultipleAliasesNoContent()
        {
            var text = @"
using S = System;
using M = Microsoft;
/*pos*/";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var scopes = model.GetImportScopes(GetPositionForBinding(text));
            Assert.Single(scopes);
            Assert.Equal(2, scopes.Single().Aliases.Length);
            Assert.True(scopes.Single().Aliases.Any(a => a is { Name: "S", Target: INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: true, Name: nameof(System) } }));
            Assert.True(scopes.Single().Aliases.Any(a => a is { Name: "M", Target: INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: true, Name: nameof(Microsoft) } }));
            Assert.True(scopes.Single().Aliases.Any(a => a.DeclaringSyntaxReferences.Single().GetSyntax() is UsingDirectiveSyntax { Name: IdentifierNameSyntax { Identifier.Text: nameof(System) } }));
            Assert.True(scopes.Single().Aliases.Any(a => a.DeclaringSyntaxReferences.Single().GetSyntax() is UsingDirectiveSyntax { Name: IdentifierNameSyntax { Identifier.Text: nameof(Microsoft) } }));
            Assert.Empty(scopes.Single().Imports);
            Assert.Empty(scopes.Single().ExternAliases);
            Assert.Empty(scopes.Single().XmlNamespaces);
        }

        [Fact]
        public void TestAliasNestedNamespaceOuterPosition()
        {
            var text = @"
using S = System;

class C
{
    /*pos*/
}

namespace N
{
    using M = Microsoft;
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var scopes = model.GetImportScopes(GetPositionForBinding(text));
            Assert.Single(scopes);
            Assert.Single(scopes.Single().Aliases);
            Assert.True(scopes.Single().Aliases.Single() is { Name: "S", Target: INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: true, Name: nameof(System) } });
            Assert.True(scopes.Single().Aliases.Single().DeclaringSyntaxReferences.Single().GetSyntax() is UsingDirectiveSyntax { Name: IdentifierNameSyntax { Identifier.Text: nameof(System) } });
        }

        [Fact]
        public void TestAliasNestedNamespaceInnerPosition()
        {
            var text = @"
using S = System;

namespace N
{
    using M = Microsoft;
    class C
    {
        /*pos*/
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var scopes = model.GetImportScopes(GetPositionForBinding(text));
            Assert.Equal(2, scopes.Length);
            Assert.Single(scopes[0].Aliases);
            Assert.Single(scopes[1].Aliases);
            Assert.True(scopes[0].Aliases.Single() is { Name: "M", Target: INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: true, Name: nameof(Microsoft) } });
            Assert.True(scopes[0].Aliases.Single().DeclaringSyntaxReferences.Single().GetSyntax() is UsingDirectiveSyntax { Name: IdentifierNameSyntax { Identifier.Text: nameof(Microsoft) } });
            Assert.True(scopes[1].Aliases.Single() is { Name: "S", Target: INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: true, Name: nameof(System) } });
            Assert.True(scopes[1].Aliases.Single().DeclaringSyntaxReferences.Single().GetSyntax() is UsingDirectiveSyntax { Name: IdentifierNameSyntax { Identifier.Text: nameof(System) } });
        }

        #endregion
    }
}
