// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SymbolId;

public abstract class SymbolKeyTestBase : CSharpTestBase
{
    [Flags]
    internal enum SymbolKeyComparison
    {
        None = 0x0,
        IgnoreCase = 0x1,
        IgnoreAssemblyIds = 0x2
    }

    [Flags]
    internal enum SymbolCategory
    {
        All = 0,
        DeclaredNamespace = 2,
        DeclaredType = 4,
        NonTypeMember = 8,
        Parameter = 16,
    }

    #region "Verification"

    internal static void ResolveAndVerifySymbolList(IEnumerable<ISymbol> newSymbols, IEnumerable<ISymbol> originalSymbols, CSharpCompilation originalComp)
    {
        var newlist = newSymbols.OrderBy(s => s.Name).ToList();
        var origlist = originalSymbols.OrderBy(s => s.Name).ToList();

        Assert.Equal(origlist.Count, newlist.Count);

        for (var i = 0; i < newlist.Count; i++)
        {
            ResolveAndVerifySymbol(newlist[i], origlist[i], originalComp);
        }
    }

    internal static void ResolveAndVerifyTypeSymbol(ExpressionSyntax node, ITypeSymbol sourceSymbol, SemanticModel model, CSharpCompilation sourceComp)
    {
        var typeinfo = model.GetTypeInfo(node);
        ResolveAndVerifySymbol(typeinfo.Type ?? typeinfo.ConvertedType, sourceSymbol, sourceComp);
    }

    internal static void ResolveAndVerifySymbol(ExpressionSyntax node, ISymbol sourceSymbol, SemanticModel model, CSharpCompilation sourceComp, SymbolKeyComparison comparison = SymbolKeyComparison.None)
    {
        var syminfo = model.GetSymbolInfo(node);
        ResolveAndVerifySymbol(syminfo.Symbol, sourceSymbol, sourceComp, comparison);
    }

    internal static void ResolveAndVerifySymbol(ISymbol symbol1, ISymbol symbol2, Compilation compilation2, SymbolKeyComparison comparison = SymbolKeyComparison.None)
    {
        // same ID
        AssertSymbolKeysEqual(symbol1, symbol2, comparison);

        var resolvedSymbol = ResolveSymbol(symbol1, compilation2, comparison);

        Assert.NotNull(resolvedSymbol);

        // same Symbol
        Assert.Equal(symbol2, resolvedSymbol);
        Assert.Equal(symbol2.GetHashCode(), resolvedSymbol.GetHashCode());
    }

    internal static ISymbol ResolveSymbol(ISymbol originalSymbol, Compilation targetCompilation, SymbolKeyComparison comparison)
    {
        var sid = SymbolKey.Create(originalSymbol, CancellationToken.None);

        // Verify that serialization works.
        var serialized = sid.ToString();
        var deserialized = new SymbolKey(serialized);
        var comparer = SymbolKey.GetComparer(ignoreCase: false, ignoreAssemblyKeys: false);
        Assert.True(comparer.Equals(sid, deserialized));

        var symInfo = sid.Resolve(targetCompilation, (comparison & SymbolKeyComparison.IgnoreAssemblyIds) == SymbolKeyComparison.IgnoreAssemblyIds);
        return symInfo.Symbol;
    }

    internal static void AssertSymbolKeysEqual(ISymbol symbol1, ISymbol symbol2, SymbolKeyComparison comparison, bool expectEqual = true)
    {
        var sid1 = SymbolKey.Create(symbol1, CancellationToken.None);
        var sid2 = SymbolKey.Create(symbol2, CancellationToken.None);

        // default is Insensitive
        var ignoreCase = (comparison & SymbolKeyComparison.IgnoreCase) == SymbolKeyComparison.IgnoreCase;

        // default is NOT ignore
        var ignoreAssemblyIds = (comparison & SymbolKeyComparison.IgnoreAssemblyIds) == SymbolKeyComparison.IgnoreAssemblyIds;
        var message = string.Concat(
            ignoreCase ? "SymbolID IgnoreCase" : "SymbolID",
            ignoreAssemblyIds ? " IgnoreAssemblyIds " : " ",
            "Compare");

        var ret = CodeAnalysis.SymbolKey.GetComparer(ignoreCase, ignoreAssemblyIds).Equals(sid2, sid1);
        if (expectEqual)
        {
            Assert.True(ret, message);
        }
        else
        {
            Assert.False(ret, message);
        }
    }

    #endregion

    #region "Utilities"

    internal static List<BlockSyntax> GetBlockSyntaxList(IMethodSymbol symbol)
    {
        var list = new List<BlockSyntax>();

        foreach (var node in symbol.DeclaringSyntaxReferences.Select(d => d.GetSyntax()))
        {
            BlockSyntax body = null;
            if (node is BaseMethodDeclarationSyntax baseMethod)
            {
                body = baseMethod.Body;
            }
            else if (node is AccessorDeclarationSyntax accessor)
            {
                body = accessor.Body;
            }

            if (body != null && body.Statements.Any())
            {
                list.Add(body);
            }
        }

        return list;
    }

    internal static IEnumerable<ISymbol> GetSourceSymbols(Microsoft.CodeAnalysis.CSharp.CSharpCompilation compilation, SymbolCategory category)
    {
        // NYI for local symbols
        var list = GetSourceSymbols(compilation, includeLocal: false);

        var kinds = new List<SymbolKind>();
        if ((category & SymbolCategory.DeclaredNamespace) != 0)
        {
            kinds.Add(SymbolKind.Namespace);
        }

        if ((category & SymbolCategory.DeclaredType) != 0)
        {
            kinds.Add(SymbolKind.NamedType);
            kinds.Add(SymbolKind.TypeParameter);
        }

        if ((category & SymbolCategory.NonTypeMember) != 0)
        {
            kinds.Add(SymbolKind.Field);
            kinds.Add(SymbolKind.Event);
            kinds.Add(SymbolKind.Property);
            kinds.Add(SymbolKind.Method);
        }

        if ((category & SymbolCategory.Parameter) != 0)
        {
            kinds.Add(SymbolKind.Parameter);
        }

        return list.Where(s =>
        {
            if (s.IsImplicitlyDeclared)
            {
                return false;
            }

            foreach (var k in kinds)
            {
                if (s.Kind == k)
                {
                    return true;
                }
            }

            return false;
        });
    }

    internal static IList<ISymbol> GetSourceSymbols(CSharpCompilation compilation, bool includeLocal)
    {
        var list = new List<ISymbol>();
        var localDumper = includeLocal ? new LocalSymbolDumper(compilation) : null;
        GetSourceMemberSymbols(compilation.SourceModule.GlobalNamespace.GetPublicSymbol(), list, localDumper);

        // ??
        // if (includeLocal)
        GetSourceAliasSymbols(compilation, list);
        Compilation c = compilation;
        list.Add(c.Assembly);
        list.AddRange(c.Assembly.Modules);

        return list;
    }

    #endregion

    #region "Private Helpers"

    private static void GetSourceMemberSymbols(INamespaceOrTypeSymbol symbol, List<ISymbol> list, LocalSymbolDumper localDumper)
    {
        foreach (var memberSymbol in symbol.GetMembers())
        {
            list.Add(memberSymbol);

            switch (memberSymbol.Kind)
            {
                case SymbolKind.NamedType:
                case SymbolKind.Namespace:
                    GetSourceMemberSymbols((INamespaceOrTypeSymbol)memberSymbol, list, localDumper);
                    break;
                case SymbolKind.Method:
                    var method = (IMethodSymbol)memberSymbol;
                    foreach (var parameter in method.Parameters)
                    {
                        list.Add(parameter);
                    }

                    localDumper?.GetLocalSymbols(method.GetSymbol(), list);

                    break;
                case SymbolKind.Field:
                    localDumper?.GetLocalSymbols(memberSymbol.GetSymbol<FieldSymbol>(), list);

                    break;
            }
        }
    }

    private static void GetSourceAliasSymbols(CSharpCompilation comp, List<ISymbol> list)
    {
        foreach (var tree in comp.SyntaxTrees)
        {
            var usingNodes = tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>();
            var model = comp.GetSemanticModel(tree);
            foreach (var u in usingNodes)
            {
                if (u.Alias != null)
                {
                    // var sym = model.GetSymbolInfo(u.Alias.Identifier).Symbol;
                    var sym = model.GetDeclaredSymbol(u);
                    if (sym != null && !list.Contains(sym))
                    {
                        list.Add(sym);
                    }
                }
            }
        }
    }

    #endregion

    private class LocalSymbolDumper
    {
        private readonly CSharpCompilation _compilation;
        public LocalSymbolDumper(CSharpCompilation compilation)
            => _compilation = compilation;

        public void GetLocalSymbols(FieldSymbol symbol, List<ISymbol> list)
        {
            foreach (var node in symbol.DeclaringSyntaxReferences.Select(d => d.GetSyntax()))
            {
                if (node is VariableDeclaratorSyntax declarator && declarator.Initializer != null)
                {
                    var model = _compilation.GetSemanticModel(declarator.SyntaxTree);

                    // Expression
                    var df = model.AnalyzeDataFlow(declarator.Initializer.Value);
                    GetLocalAndType(df, list);

                    GetAnonymousExprSymbols(declarator.Initializer.Value, model, list);
                }
            }
        }

        public void GetLocalSymbols(MethodSymbol symbol, List<ISymbol> list)
        {
            foreach (var node in symbol.DeclaringSyntaxReferences.Select(d => d.GetSyntax()))
            {
                BlockSyntax body = null;
                if (node is BaseMethodDeclarationSyntax baseMethod)
                {
                    body = baseMethod.Body;
                }
                else if (node is AccessorDeclarationSyntax accessor)
                {
                    body = accessor.Body;
                }

                var model = _compilation.GetSemanticModel(node.SyntaxTree);

                if (body != null && body.Statements.Any())
                {
                    var df = model.AnalyzeDataFlow(body);
                    GetLocalAndType(df, list);

                    GetAnonymousTypeOrFuncSymbols(body, model, list);

                    GetLabelSymbols(body, model, list);
                }

                // C# specific (this|base access)
                if (node is ConstructorDeclarationSyntax ctor && ctor.Initializer != null)
                {
                    foreach (var a in ctor.Initializer.ArgumentList.Arguments)
                    {
                        var df = model.AnalyzeDataFlow(a.Expression);

                        // VisitLocals(arg, df);
                        list.AddRange(df.VariablesDeclared);

                        GetAnonymousExprSymbols(a.Expression, model, list);
                    }
                }
            }
        }

        private static void GetLocalAndType(DataFlowAnalysis df, List<ISymbol> list)
        {
            foreach (var v in df.VariablesDeclared)
            {
                list.Add(v);
                if (v is ILocalSymbol local && (local.Type.Kind == SymbolKind.ArrayType || local.Type.Kind == SymbolKind.PointerType))
                {
                    list.Add(local.Type);
                }
            }
        }

        private static void GetLabelSymbols(BlockSyntax body, SemanticModel model, List<ISymbol> list)
        {
            var labels = body.DescendantNodes().OfType<LabeledStatementSyntax>();
            foreach (var n in labels)
            {
                // Label: -> 'Label' is token
                var sym = model.GetDeclaredSymbol(n);
                list.Add(sym);
            }

            var swlabels = body.DescendantNodes().OfType<SwitchLabelSyntax>();
            foreach (var n in swlabels)
            {
                // label value has NO symbol, Type is expr's type
                // e.g. case "A": -> string type
                // var info1 = model.GetTypeInfo(n.Value);
                // var info2 = model.GetSymbolInfo(n.Value);
                var sym = model.GetDeclaredSymbol(n);
                list.Add(sym);
            }
        }

        private static void GetAnonymousTypeOrFuncSymbols(BlockSyntax body, SemanticModel model, List<ISymbol> list)
        {
            IEnumerable<ExpressionSyntax> exprs = body.DescendantNodes().OfType<SimpleLambdaExpressionSyntax>();
            IEnumerable<ExpressionSyntax> tmp = body.DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>();
            exprs = exprs.Concat(tmp);
            tmp = body.DescendantNodes().OfType<AnonymousMethodExpressionSyntax>();
            exprs = exprs.Concat(tmp);

            tmp = body.DescendantNodes().OfType<AnonymousObjectCreationExpressionSyntax>();
            exprs = exprs.Concat(tmp);

            foreach (var expr in exprs)
            {
                GetAnonymousExprSymbols(expr, model, list);
            }
        }

        private static void GetAnonymousExprSymbols(ExpressionSyntax expr, SemanticModel model, List<ISymbol> list)
        {
            var kind = expr.Kind();
            if (kind is not SyntaxKind.AnonymousObjectCreationExpression and
                not SyntaxKind.AnonymousMethodExpression and
                not SyntaxKind.ParenthesizedLambdaExpression and
                not SyntaxKind.SimpleLambdaExpression)
            {
                return;
            }

            var tinfo = model.GetTypeInfo(expr);
            var conv = model.GetConversion(expr);
            if (conv.IsAnonymousFunction)
            {
                // Lambda has no Type unless in part of case expr (C# specific)
                // var f = (Func<int>)(() => { return 1; }); Type is delegate
                // method symbol
                var sinfo = model.GetSymbolInfo(expr);
                list.Add(sinfo.Symbol);
            }
            else if (tinfo.Type != null && tinfo.Type.TypeKind != TypeKind.Delegate)
            {
                // bug#12625
                // GetSymbolInfo -> .ctor (part of members)
                list.Add(tinfo.Type); // NamedType with empty name
                foreach (var m in tinfo.Type.GetMembers())
                {
                    list.Add(m);
                }
            }
        }
    }
}
