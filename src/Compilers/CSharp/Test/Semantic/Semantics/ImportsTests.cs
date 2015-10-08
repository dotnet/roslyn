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
            var empty = Imports.Empty;
            var nonEmpty = GetImports("using System;").Single();

            Assert.Same(empty, empty.Concat(empty));
            Assert.Same(nonEmpty, nonEmpty.Concat(empty));
            Assert.Same(nonEmpty, empty.Concat(nonEmpty));
        }

        [Fact]
        public void ConcatDistinct()
        {
            var imports = GetImports(@"
extern alias A;
using System;
using C = System;
", @"
extern alias B;
using System.IO;
using D = System.IO;
");

            var concat1 = imports[0].Concat(imports[1]);
            Assert.Equal(new[] { "A", "B" }, concat1.ExternAliases.Select(e => e.Alias.Name));
            Assert.Equal(new[] { "System", "System.IO" }, concat1.Usings.Select(u => u.NamespaceOrType.ToTestDisplayString()));
            var usingAliases1 = concat1.UsingAliases;
            AssertEx.SetEqual(new[] { "C", "D" }, usingAliases1.Select(a => a.Key));
            Assert.Equal("System", usingAliases1["C"].Alias.Target.ToTestDisplayString());
            Assert.Equal("System.IO", usingAliases1["D"].Alias.Target.ToTestDisplayString());

            var concat2 = imports[1].Concat(imports[0]);
            Assert.Equal(new[] { "B", "A" }, concat2.ExternAliases.Select(e => e.Alias.Name));
            Assert.Equal(new[] { "System.IO", "System" }, concat2.Usings.Select(u => u.NamespaceOrType.ToTestDisplayString()));
            var usingAliases2 = concat2.UsingAliases;
            AssertEx.SetEqual(new[] { "C", "D" }, usingAliases2.Select(a => a.Key));
            Assert.Equal("System", usingAliases2["C"].Alias.Target.ToTestDisplayString());
            Assert.Equal("System.IO", usingAliases2["D"].Alias.Target.ToTestDisplayString());
        }

        [Fact]
        public void ConcatColliding()
        {
            var imports = GetImports(@"
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

            var concat1 = imports[0].Concat(imports[1]);
            Assert.Equal(new[] { "B", "A", "C" }, concat1.ExternAliases.Select(e => e.Alias.Name));
            Assert.Equal(new[] { "System", "System.Collections", "System.IO" }, concat1.Usings.Select(u => u.NamespaceOrType.ToTestDisplayString()));
            var usingAliases1 = concat1.UsingAliases;
            AssertEx.SetEqual(new[] { "D", "E", "F" }, usingAliases1.Select(a => a.Key));
            Assert.Equal("System.IO", usingAliases1["D"].Alias.Target.ToTestDisplayString()); // Last one wins
            Assert.Equal("System", usingAliases1["E"].Alias.Target.ToTestDisplayString());
            Assert.Equal("System.IO", usingAliases1["F"].Alias.Target.ToTestDisplayString());

            var concat2 = imports[1].Concat(imports[0]);
            Assert.Equal(new[] { "C", "A", "B" }, concat2.ExternAliases.Select(e => e.Alias.Name));
            Assert.Equal(new[] { "System", "System.IO", "System.Collections" }, concat2.Usings.Select(u => u.NamespaceOrType.ToTestDisplayString()));
            var usingAliases2 = concat2.UsingAliases;
            AssertEx.SetEqual(new[] { "D", "E", "F" }, usingAliases2.Select(a => a.Key));
            Assert.Equal("System", usingAliases2["D"].Alias.Target.ToTestDisplayString()); // Last one wins
            Assert.Equal("System", usingAliases2["E"].Alias.Target.ToTestDisplayString());
            Assert.Equal("System.IO", usingAliases2["F"].Alias.Target.ToTestDisplayString());
        }

        [Fact]
        public void ConcatCollidingExternAliases()
        {
            var comp = CreateCompilationWithMscorlib(
                "extern alias A; extern alias B;", 
                new[] 
                {
                    SystemCoreRef.WithAliases(new[] { "A" }),
                    SystemDataRef.WithAliases(new[] { "B" }),
                });

            var tree = comp.SyntaxTrees.Single();
            var binder = comp.GetBinderFactory(tree).GetImportsBinder((CSharpSyntaxNode)tree.GetRoot(), inUsing: false);
            var scratchImports = binder.GetImports(basesBeingResolved: null);
            var scratchExternAliases = scratchImports.ExternAliases;
            Assert.Equal(2, scratchExternAliases.Length);

            var externAlias1 = scratchExternAliases[0];
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

            var concat1 = imports1.Concat(imports2);
            Assert.Equal(externAlias2.Alias.Target, concat1.ExternAliases.Single().Alias.Target);

            var concat2 = imports2.Concat(imports1);
            Assert.Equal(externAlias1.Alias.Target, concat2.ExternAliases.Single().Alias.Target);
        }

        private static Imports[] GetImports(params string[] sources)
        {
            var trees = sources.Select(source => Parse(source)).ToArray();
            var compilationUnits = trees.Select(tree => (CompilationUnitSyntax)tree.GetRoot());
            var externAliases = compilationUnits.SelectMany(cu => cu.Externs).Select(e => e.Identifier.ValueText).Distinct();

            var comp = CreateCompilationWithMscorlib(trees, new[] { SystemCoreRef.WithAliases(externAliases) });
            comp.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Verify();

            var factories = trees.Select(tree => comp.GetBinderFactory(tree));
            var binders = factories.Select(factory => factory.GetImportsBinder((CSharpSyntaxNode)factory.SyntaxTree.GetRoot(), inUsing: false));
            var imports = binders.Select(binder => binder.GetImports(basesBeingResolved: null));
            Assert.DoesNotContain(Imports.Empty, imports);
            return imports.ToArray();
        }
    }
}