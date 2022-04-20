// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public class GetImportScopesTests : SemanticModelTestBase
{
    private ImmutableArray<IImportScope> GetImportsScopes(string text)
    {
        var tree = Parse(text);
        var comp = CreateCompilation(tree);
        var model = comp.GetSemanticModel(tree);
        var scopes = model.GetImportScopes(GetPositionForBinding(text));
        return scopes;
    }

    [Fact]
    public void TestEmptyFile()
    {
        var text = @"/*pos*/";
        var scopes = GetImportsScopes(text);
        Assert.Empty(scopes);
    }

    [Fact]
    public void TestNoImportsBeforeMemberDeclaration()
    {
        var text = @"/*pos*/
class C {}";
        var scopes = GetImportsScopes(text);
        Assert.Empty(scopes);
    }

    #region normal imports

    [Fact]
    public void TestBeforeImports()
    {
        var text = @"/*pos*/
using System;";
        var scopes = GetImportsScopes(text);
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
        var scopes = GetImportsScopes(text);
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
        var scopes = GetImportsScopes(text);
        Assert.Empty(scopes);
    }

    [Fact]
    public void TestBeforeImportsTopLevelStatements()
    {
        var text = @"
/*pos*/
using System;

return;";
        var scopes = GetImportsScopes(text);
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
        var scopes = GetImportsScopes(text);
        Assert.Empty(scopes);
    }

    [Fact]
    public void TestAfterImportsTopLevelStatements2()
    {
        var text = @"
using System;

return /*pos*/;";
        var scopes = GetImportsScopes(text);
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
        var scopes = GetImportsScopes(text);
        Assert.Single(scopes);
        Assert.Equal(2, scopes.Single().Imports.Length);
        Assert.True(scopes.Single().Imports.Any(i => i.NamespaceOrType is INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: true, Name: nameof(System) }));
        Assert.True(scopes.Single().Imports.Any(i => i.NamespaceOrType is INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: true, Name: nameof(Microsoft) }));
        Assert.True(scopes.Single().Imports.Any(i => i.DeclaringSyntaxReference!.GetSyntax() is UsingDirectiveSyntax { Name: IdentifierNameSyntax { Identifier.Text: nameof(System) } }));
        Assert.True(scopes.Single().Imports.Any(i => i.DeclaringSyntaxReference!.GetSyntax() is UsingDirectiveSyntax { Name: IdentifierNameSyntax { Identifier.Text: nameof(Microsoft) } }));
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
        var scopes = GetImportsScopes(text);
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
        var scopes = GetImportsScopes(text);
        Assert.Equal(2, scopes.Length);
        Assert.Single(scopes[0].Imports);
        Assert.Single(scopes[1].Imports);
        Assert.True(scopes[0].Imports.Single().NamespaceOrType is INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: true, Name: nameof(Microsoft) });
        Assert.True(scopes[0].Imports.Single().DeclaringSyntaxReference!.GetSyntax() is UsingDirectiveSyntax { Name: IdentifierNameSyntax { Identifier.Text: nameof(Microsoft) } });
        Assert.True(scopes[1].Imports.Single().NamespaceOrType is INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: true, Name: nameof(System) });
        Assert.True(scopes[1].Imports.Single().DeclaringSyntaxReference!.GetSyntax() is UsingDirectiveSyntax { Name: IdentifierNameSyntax { Identifier.Text: nameof(System) } });
    }

    [Fact]
    public void TestNestedNamespaceInnerPositionIntermediaryEmptyNamespace()
    {
        var text = @"
using System;

namespace Outer
{
    namespace N
    {
        using Microsoft;
        class C
        {
            /*pos*/
        }
    }
}
";
        var scopes = GetImportsScopes(text);
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
        var scopes = GetImportsScopes(text);
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
        var scopes = GetImportsScopes(text);
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
        var scopes = GetImportsScopes(text);
        Assert.Empty(scopes);
    }

    [Fact]
    public void TestBeforeAliasTopLevelStatements()
    {
        var text = @"
/*pos*/
using S = System;

return;";
        var scopes = GetImportsScopes(text);
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
        var scopes = GetImportsScopes(text);
        Assert.Empty(scopes);
    }

    [Fact]
    public void TestAfterAliasTopLevelStatements2()
    {
        var text = @"
using S = System;

return /*pos*/;";
        var scopes = GetImportsScopes(text);
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
        var scopes = GetImportsScopes(text);
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
        var scopes = GetImportsScopes(text);
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
        var scopes = GetImportsScopes(text);
        Assert.Equal(2, scopes.Length);
        Assert.Single(scopes[0].Aliases);
        Assert.Single(scopes[1].Aliases);
        Assert.True(scopes[0].Aliases.Single() is { Name: "M", Target: INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: true, Name: nameof(Microsoft) } });
        Assert.True(scopes[0].Aliases.Single().DeclaringSyntaxReferences.Single().GetSyntax() is UsingDirectiveSyntax { Name: IdentifierNameSyntax { Identifier.Text: nameof(Microsoft) } });
        Assert.True(scopes[1].Aliases.Single() is { Name: "S", Target: INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: true, Name: nameof(System) } });
        Assert.True(scopes[1].Aliases.Single().DeclaringSyntaxReferences.Single().GetSyntax() is UsingDirectiveSyntax { Name: IdentifierNameSyntax { Identifier.Text: nameof(System) } });
    }

    #endregion

    #region extern aliases

    private static CSharpCompilation CreateCompilationWithExternAlias(CSharpTestSource source, params string[] aliases)
    {
        if (aliases.Length == 0)
            aliases = new[] { "CORE" };

        var comp = CreateCompilation(source);
        var reference = comp.References.First(r => r.Display!.StartsWith("System.Core"));
        return comp.ReplaceReference(reference, reference.WithAliases(ImmutableArray.CreateRange(aliases)));
    }

    [Fact]
    public void TestBeforeExternAlias()
    {
        var text = @"/*pos*/
extern alias CORE;";
        var tree = Parse(text);
        var comp = CreateCompilationWithExternAlias(tree);
        var model = comp.GetSemanticModel(tree);
        var scopes = model.GetImportScopes(GetPositionForBinding(text));
        Assert.Single(scopes);
        Assert.Single(scopes.Single().ExternAliases);
        Assert.True(scopes.Single().ExternAliases.Single() is { Name: "CORE" });
        Assert.True(scopes.Single().ExternAliases.Single().DeclaringSyntaxReferences.Single().GetSyntax() is ExternAliasDirectiveSyntax);
        Assert.Empty(scopes.Single().Imports);
        Assert.Empty(scopes.Single().Aliases);
        Assert.Empty(scopes.Single().XmlNamespaces);
    }

    [Fact]
    public void TestAfterExternAliasNoContent()
    {
        var text = @"
extern alias CORE;
/*pos*/";
        var tree = Parse(text);
        var comp = CreateCompilationWithExternAlias(tree);
        var model = comp.GetSemanticModel(tree);
        var scopes = model.GetImportScopes(GetPositionForBinding(text));
        Assert.Single(scopes);
        Assert.Single(scopes.Single().ExternAliases);
        Assert.True(scopes.Single().ExternAliases.Single() is { Name: "CORE", Target: INamespaceSymbol { IsGlobalNamespace: true } });
        Assert.True(scopes.Single().ExternAliases.Single().DeclaringSyntaxReferences.Single().GetSyntax() is ExternAliasDirectiveSyntax);
        Assert.Empty(scopes.Single().Imports);
        Assert.Empty(scopes.Single().Aliases);
        Assert.Empty(scopes.Single().XmlNamespaces);
    }

    [Fact]
    public void TestAfterExternAliasBeforeMemberDeclaration()
    {
        var text = @"
extern alias CORE;
/*pos*/
class C
{
}";
        var tree = Parse(text);
        var comp = CreateCompilationWithExternAlias(tree);
        var model = comp.GetSemanticModel(tree);
        var scopes = model.GetImportScopes(GetPositionForBinding(text));
        Assert.Single(scopes);
        Assert.Single(scopes.Single().ExternAliases);
        Assert.True(scopes.Single().ExternAliases.Single() is { Name: "CORE", Target: INamespaceSymbol { IsGlobalNamespace: true } });
        Assert.True(scopes.Single().ExternAliases.Single().DeclaringSyntaxReferences.Single().GetSyntax() is ExternAliasDirectiveSyntax);
        Assert.Empty(scopes.Single().Imports);
        Assert.Empty(scopes.Single().Aliases);
        Assert.Empty(scopes.Single().XmlNamespaces);
    }

    [Fact]
    public void TestBeforeExternAliasTopLevelStatements()
    {
        var text = @"
/*pos*/
extern alias CORE;

return;";
        var tree = Parse(text);
        var comp = CreateCompilationWithExternAlias(tree);
        var model = comp.GetSemanticModel(tree);
        var scopes = model.GetImportScopes(GetPositionForBinding(text));
        Assert.Single(scopes);
        Assert.Single(scopes.Single().ExternAliases);
        Assert.True(scopes.Single().ExternAliases.Single() is { Name: "CORE", Target: INamespaceSymbol { IsGlobalNamespace: true } });
        Assert.True(scopes.Single().ExternAliases.Single().DeclaringSyntaxReferences.Single().GetSyntax() is ExternAliasDirectiveSyntax);
        Assert.Empty(scopes.Single().Imports);
        Assert.Empty(scopes.Single().Aliases);
        Assert.Empty(scopes.Single().XmlNamespaces);
    }

    [Fact]
    public void TestAfterExternAliasTopLevelStatements1()
    {
        var text = @"
extern alias CORE;
/*pos*/
return;";
        var tree = Parse(text);
        var comp = CreateCompilationWithExternAlias(tree);
        var model = comp.GetSemanticModel(tree);
        var scopes = model.GetImportScopes(GetPositionForBinding(text));
        Assert.Single(scopes.Single().ExternAliases);
        Assert.True(scopes.Single().ExternAliases.Single() is { Name: "CORE", Target: INamespaceSymbol { IsGlobalNamespace: true } });
        Assert.True(scopes.Single().ExternAliases.Single().DeclaringSyntaxReferences.Single().GetSyntax() is ExternAliasDirectiveSyntax);
    }

    [Fact]
    public void TestAfterExternAliasTopLevelStatements2()
    {
        var text = @"
extern alias CORE;

return /*pos*/;";
        var tree = Parse(text);
        var comp = CreateCompilationWithExternAlias(tree);
        var model = comp.GetSemanticModel(tree);
        var scopes = model.GetImportScopes(GetPositionForBinding(text));
        Assert.Single(scopes);
        Assert.Single(scopes.Single().ExternAliases);
        Assert.True(scopes.Single().ExternAliases.Single() is { Name: "CORE", Target: INamespaceSymbol { IsGlobalNamespace: true } });
        Assert.True(scopes.Single().ExternAliases.Single().DeclaringSyntaxReferences.Single().GetSyntax() is ExternAliasDirectiveSyntax);
        Assert.Empty(scopes.Single().Imports);
        Assert.Empty(scopes.Single().Aliases);
        Assert.Empty(scopes.Single().XmlNamespaces);
    }

    [Fact]
    public void TestAfterMultipleExternAliasesNoContent()
    {
        var text = @"
extern alias CORE1;
extern alias CORE2;
/*pos*/";
        var tree = Parse(text);
        var comp = CreateCompilationWithExternAlias(tree, "CORE1", "CORE2");
        var model = comp.GetSemanticModel(tree);
        var scopes = model.GetImportScopes(GetPositionForBinding(text));
        Assert.Single(scopes);
        Assert.Equal(2, scopes.Single().ExternAliases.Length);
        Assert.True(scopes.Single().ExternAliases.Any(a => a is { Name: "CORE1", Target: INamespaceSymbol { IsGlobalNamespace: true } }));
        Assert.True(scopes.Single().ExternAliases.Any(a => a is { Name: "CORE2", Target: INamespaceSymbol { IsGlobalNamespace: true } }));
        Assert.True(scopes.Single().ExternAliases.Any(a => a.DeclaringSyntaxReferences.Single().GetSyntax() is ExternAliasDirectiveSyntax { Identifier.Text: "CORE1" }));
        Assert.True(scopes.Single().ExternAliases.Any(a => a.DeclaringSyntaxReferences.Single().GetSyntax() is ExternAliasDirectiveSyntax { Identifier.Text: "CORE2" }));
        Assert.Empty(scopes.Single().Imports);
        Assert.Empty(scopes.Single().Aliases);
        Assert.Empty(scopes.Single().XmlNamespaces);
    }

    [Fact]
    public void TestExternAliasNestedNamespaceOuterPosition()
    {
        var text = @"
extern alias CORE1;

class C
{
    /*pos*/
}

namespace N
{
    extern alias CORE2;
}
";
        var tree = Parse(text);
        var comp = CreateCompilationWithExternAlias(tree, "CORE1", "CORE2");
        var model = comp.GetSemanticModel(tree);
        var scopes = model.GetImportScopes(GetPositionForBinding(text));
        Assert.Single(scopes);
        Assert.Single(scopes.Single().ExternAliases);
        Assert.True(scopes.Single().ExternAliases.Single() is { Name: "CORE1", Target: INamespaceSymbol { IsGlobalNamespace: true } });
        Assert.True(scopes.Single().ExternAliases.Single().DeclaringSyntaxReferences.Single().GetSyntax() is ExternAliasDirectiveSyntax { Identifier.Text: "CORE1" });
    }

    [Fact]
    public void TestExternAliasNestedNamespaceInnerPosition()
    {
        var text = @"
extern alias CORE1;

namespace N
{
    extern alias CORE2;
    class C
    {
        /*pos*/
    }
}
";
        var tree = Parse(text);
        var comp = CreateCompilationWithExternAlias(tree, "CORE1", "CORE2");
        var model = comp.GetSemanticModel(tree);
        var scopes = model.GetImportScopes(GetPositionForBinding(text));
        Assert.Equal(2, scopes.Length);
        Assert.Single(scopes[0].ExternAliases);
        Assert.Single(scopes[1].ExternAliases);
        Assert.True(scopes[0].ExternAliases.Single() is { Name: "CORE2", Target: INamespaceSymbol { IsGlobalNamespace: true } });
        Assert.True(scopes[0].ExternAliases.Single().DeclaringSyntaxReferences.Single().GetSyntax() is ExternAliasDirectiveSyntax { Identifier.Text: "CORE2" });
        Assert.True(scopes[1].ExternAliases.Single() is { Name: "CORE1", Target: INamespaceSymbol { IsGlobalNamespace: true } });
        Assert.True(scopes[1].ExternAliases.Single().DeclaringSyntaxReferences.Single().GetSyntax() is ExternAliasDirectiveSyntax { Identifier.Text: "CORE1" });
    }

    #endregion

    #region global imports

    [Fact]
    public void TestEmptyFile_WithGlobalImports()
    {
        var globalImportsText = @"
extern alias CORE;
global using System;
global using M = Microsoft;";
        var text = @"
/*pos*/";
        var tree = Parse(text);
        var comp = CreateCompilationWithExternAlias(new[] { tree, Parse(globalImportsText) });
        var model = comp.GetSemanticModel(tree);
        var scopes = model.GetImportScopes(GetPositionForBinding(text));
        Assert.Single(scopes);
        Assert.Single(scopes.Single().Aliases);
        Assert.Single(scopes.Single().Imports);
        Assert.Empty(scopes.Single().ExternAliases);
        Assert.Empty(scopes.Single().XmlNamespaces);

        Assert.True(scopes.Single().Aliases.Single() is { Name: "M", Target: INamespaceSymbol { Name: nameof(Microsoft) } });
        Assert.True(scopes.Single().Aliases.Single().DeclaringSyntaxReferences.Single().GetSyntax() is UsingDirectiveSyntax { Alias.Name.Identifier.Text: "M" });

        Assert.True(scopes.Single().Imports.Single().NamespaceOrType is INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: true, Name: nameof(System) });
        Assert.True(scopes.Single().Imports.Single().DeclaringSyntaxReference!.GetSyntax() is UsingDirectiveSyntax { Name: IdentifierNameSyntax { Identifier.Text: nameof(System) } });
    }

    [Fact]
    public void TestInsideDeclaration_WithGlobalImports()
    {
        var globalImportsText = @"
extern alias CORE;
global using System;
global using M = Microsoft";
        var text = @"
class C
{
    /*pos*/
}";
        var tree = Parse(text);
        var comp = CreateCompilationWithExternAlias(new[] { tree, Parse(globalImportsText) });
        var model = comp.GetSemanticModel(tree);
        var scopes = model.GetImportScopes(GetPositionForBinding(text));
        Assert.Single(scopes);

        Assert.Single(scopes.Single().Imports);
        Assert.True(scopes.Single().Imports.Single().NamespaceOrType is INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: true, Name: nameof(System) });
        Assert.True(scopes.Single().Imports.Single().DeclaringSyntaxReference!.GetSyntax() is UsingDirectiveSyntax);

        Assert.Single(scopes.Single().Aliases);
        Assert.True(scopes.Single().Aliases.Single() is { Name: "M", Target: INamespaceSymbol { Name: nameof(Microsoft) } });
        Assert.True(scopes.Single().Aliases.Single().DeclaringSyntaxReferences.Single().GetSyntax() is UsingDirectiveSyntax { Alias.Name.Identifier.Text: "M" });

        Assert.Empty(scopes.Single().ExternAliases);
        Assert.Empty(scopes.Single().XmlNamespaces);
    }

    [Fact]
    public void TestGlobalImportsAndFileImports()
    {
        var globalImportsText = @"
extern alias CORE;
global using System;
global using M = Microsoft";
        var text = @"
using System.IO;
using T = System.Threading;

class C
{
    /*pos*/
}";
        var tree = Parse(text);
        var comp = CreateCompilationWithExternAlias(new[] { tree, Parse(globalImportsText) });
        var model = comp.GetSemanticModel(tree);
        var scopes = model.GetImportScopes(GetPositionForBinding(text));

        Assert.Single(scopes);

        Assert.Equal(2, scopes.Single().Imports.Length);
        Assert.True(scopes.Single().Imports.Any(i => i.NamespaceOrType is INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: true, Name: nameof(System) }));
        Assert.True(scopes.Single().Imports.Any(i => i.DeclaringSyntaxReference!.GetSyntax() is UsingDirectiveSyntax { Name: IdentifierNameSyntax { Identifier.Text: nameof(System) } }));
        Assert.True(scopes.Single().Imports.Any(i => i.NamespaceOrType is INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: false, Name: nameof(System.IO) }));
        Assert.True(scopes.Single().Imports.Any(i => i.DeclaringSyntaxReference!.GetSyntax() is UsingDirectiveSyntax { Name: QualifiedNameSyntax { Right: IdentifierNameSyntax { Identifier.Text: nameof(System.IO) } } }));

        Assert.Equal(2, scopes.Single().Aliases.Length);
        Assert.True(scopes.Single().Aliases.Any(i => i is { Name: "M", Target: INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: true, Name: nameof(Microsoft) } }));
        Assert.True(scopes.Single().Aliases.Any(i => i.DeclaringSyntaxReferences.Single().GetSyntax() is UsingDirectiveSyntax { Name: IdentifierNameSyntax { Identifier.Text: nameof(Microsoft) } }));
        Assert.True(scopes.Single().Aliases.Any(i => i is { Name: "T", Target: INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: false, Name: nameof(System.Threading) } }));
        Assert.True(scopes.Single().Aliases.Any(i => i.DeclaringSyntaxReferences.Single().GetSyntax() is UsingDirectiveSyntax { Name: QualifiedNameSyntax { Right: IdentifierNameSyntax { Identifier.Text: nameof(System.Threading) } } }));

        Assert.Empty(scopes.Single().ExternAliases);
        Assert.Empty(scopes.Single().XmlNamespaces);
    }

    #endregion
}
