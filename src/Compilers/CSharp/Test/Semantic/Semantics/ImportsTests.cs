// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class ImportsTest : CSharpTestBase
    {
        [Fact]
        public void ConcatEmpty()
        {
            Imports empty = Imports.Empty;
            Imports nonEmpty = GetImports("using System;").Single();

            Assert.Same(empty, empty.Concat(empty));
            Assert.Same(nonEmpty, nonEmpty.Concat(empty));
            Assert.Same(nonEmpty, empty.Concat(nonEmpty));
        }

        [Fact]
        public void ConcatDistinct()
        {
            Imports[] imports = GetImports(@"
extern alias A;
using System;
using C = System;
", @"
extern alias B;
using System.IO;
using D = System.IO;
");

            Imports concat1 = imports[0].Concat(imports[1]);
            Assert.Equal(new[] { "A", "B" }, concat1.ExternAliases.Select(e => e.Alias.Name));
            Assert.Equal(new[] { "System", "System.IO" }, concat1.Usings.Select(u => u.NamespaceOrType.ToTestDisplayString()));
            ImmutableDictionary<string, AliasAndUsingDirective> usingAliases1 = concat1.UsingAliases;
            AssertEx.SetEqual(new[] { "C", "D" }, usingAliases1.Select(a => a.Key));
            Assert.Equal("System", usingAliases1["C"].Alias.Target.ToTestDisplayString());
            Assert.Equal("System.IO", usingAliases1["D"].Alias.Target.ToTestDisplayString());

            Imports concat2 = imports[1].Concat(imports[0]);
            Assert.Equal(new[] { "B", "A" }, concat2.ExternAliases.Select(e => e.Alias.Name));
            Assert.Equal(new[] { "System.IO", "System" }, concat2.Usings.Select(u => u.NamespaceOrType.ToTestDisplayString()));
            ImmutableDictionary<string, AliasAndUsingDirective> usingAliases2 = concat2.UsingAliases;
            AssertEx.SetEqual(new[] { "C", "D" }, usingAliases2.Select(a => a.Key));
            Assert.Equal("System", usingAliases2["C"].Alias.Target.ToTestDisplayString());
            Assert.Equal("System.IO", usingAliases2["D"].Alias.Target.ToTestDisplayString());
        }

        [Fact]
        public void ConcatColliding()
        {
            Imports[] imports = GetImports(@"
extern alias A;
extern alias B;
using System;
using System.Collections;
using D = System;
using E = System;
", @"
extern alias A;
extern alias C;
using System;
using System.IO;
using D = System.IO;
using F = System.IO;
");

            Imports concat1 = imports[0].Concat(imports[1]);
            Assert.Equal(new[] { "B", "A", "C" }, concat1.ExternAliases.Select(e => e.Alias.Name));
            Assert.Equal(new[] { "System", "System.Collections", "System.IO" }, concat1.Usings.Select(u => u.NamespaceOrType.ToTestDisplayString()));
            ImmutableDictionary<string, AliasAndUsingDirective> usingAliases1 = concat1.UsingAliases;
            AssertEx.SetEqual(new[] { "D", "E", "F" }, usingAliases1.Select(a => a.Key));
            Assert.Equal("System.IO", usingAliases1["D"].Alias.Target.ToTestDisplayString()); // Last one wins
            Assert.Equal("System", usingAliases1["E"].Alias.Target.ToTestDisplayString());
            Assert.Equal("System.IO", usingAliases1["F"].Alias.Target.ToTestDisplayString());

            Imports concat2 = imports[1].Concat(imports[0]);
            Assert.Equal(new[] { "C", "A", "B" }, concat2.ExternAliases.Select(e => e.Alias.Name));
            Assert.Equal(new[] { "System", "System.IO", "System.Collections" }, concat2.Usings.Select(u => u.NamespaceOrType.ToTestDisplayString()));
            ImmutableDictionary<string, AliasAndUsingDirective> usingAliases2 = concat2.UsingAliases;
            AssertEx.SetEqual(new[] { "D", "E", "F" }, usingAliases2.Select(a => a.Key));
            Assert.Equal("System", usingAliases2["D"].Alias.Target.ToTestDisplayString()); // Last one wins
            Assert.Equal("System", usingAliases2["E"].Alias.Target.ToTestDisplayString());
            Assert.Equal("System.IO", usingAliases2["F"].Alias.Target.ToTestDisplayString());
        }

        [Fact]
        public void ConcatCollidingExternAliases()
        {
            CSharpCompilation comp = CreateCompilation(
                "extern alias A; extern alias B;",
                new[]
                {
                    SystemCoreRef.WithAliases(new[] { "A" }),
                    SystemDataRef.WithAliases(new[] { "B" }),
                });

            SyntaxTree tree = comp.SyntaxTrees.Single();
            InContainerBinder binder = comp.GetBinderFactory(tree).GetImportsBinder((CSharpSyntaxNode)tree.GetRoot(), inUsing: false);
            Imports scratchImports = binder.GetImports(basesBeingResolved: null);
            ImmutableArray<AliasAndExternAliasDirective> scratchExternAliases = scratchImports.ExternAliases;
            Assert.Equal(2, scratchExternAliases.Length);

            AliasAndExternAliasDirective externAlias1 = scratchExternAliases[0];
            var externAlias2 = new AliasAndExternAliasDirective(
                AliasSymbol.CreateCustomDebugInfoAlias(scratchExternAliases[1].Alias.Target, externAlias1.ExternAliasDirective.Identifier, binder),
                 externAlias1.ExternAliasDirective);

            var imports1 = Imports.FromCustomDebugInfo(
                comp,
                ImmutableDictionary<string, AliasAndUsingDirective>.Empty,
                ImmutableArray<NamespaceOrTypeAndUsingDirective>.Empty,
                ImmutableArray.Create(externAlias1));

            var imports2 = Imports.FromCustomDebugInfo(
                comp,
                ImmutableDictionary<string, AliasAndUsingDirective>.Empty,
                ImmutableArray<NamespaceOrTypeAndUsingDirective>.Empty,
                ImmutableArray.Create(externAlias2));

            Imports concat1 = imports1.Concat(imports2);
            Assert.Equal(externAlias2.Alias.Target, concat1.ExternAliases.Single().Alias.Target);

            Imports concat2 = imports2.Concat(imports1);
            Assert.Equal(externAlias1.Alias.Target, concat2.ExternAliases.Single().Alias.Target);
        }

        private static Imports[] GetImports(params string[] sources)
        {
            SyntaxTree[] trees = sources.Select(source => Parse(source)).ToArray();
            IEnumerable<CompilationUnitSyntax> compilationUnits = trees.Select(tree => (CompilationUnitSyntax)tree.GetRoot());
            IEnumerable<string> externAliases = compilationUnits.SelectMany(cu => cu.Externs).Select(e => e.Identifier.ValueText).Distinct();

            CSharpCompilation comp = CreateCompilation(trees, new[] { SystemCoreRef.WithAliases(externAliases) });
            comp.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Verify();

            IEnumerable<BinderFactory> factories = trees.Select(tree => comp.GetBinderFactory(tree));
            IEnumerable<InContainerBinder> binders = factories.Select(factory => factory.GetImportsBinder((CSharpSyntaxNode)factory.SyntaxTree.GetRoot(), inUsing: false));
            IEnumerable<Imports> imports = binders.Select(binder => binder.GetImports(basesBeingResolved: null));
            Assert.DoesNotContain(Imports.Empty, imports);
            return imports.ToArray();
        }
    }
}
