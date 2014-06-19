// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--==

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Roslyn.Compilers.CSharp
{
#if false
    public class SyntaxTreeBinding : SyntaxBinding
    {
        private readonly SyntaxTree tree;
        private readonly SyntaxNode root;
        private readonly ContextBuilder builder;

        internal SyntaxTreeBinding(Compilation compilation, SyntaxTree tree)
        {
            this.tree = tree;
            this.root = tree.Root; // keep node tree alive
            this.builder = compilation.GetContextBuilder(tree);
        }

        public SyntaxTree Tree
        {
            get { return this.tree; }
        }

        public SymbolInfo LookupType(TypeSyntax type, int arity = 0)
        {
            return LookupType(GetEnclosingContext(type), type, arity);
        }

        private SymbolInfo LookupType(BinderContext underlying, TypeSyntax type, int arity = 0)
        {
            using (var context = new DiagnosticBufferBinderContext(underlying))
            {
                var result = context.BindType(type, arity);
                var err = result as ErrorTypeSymbol;
                if (err == null)
                {
                    return new SymbolInfo(result, ConsList.Singleton(result), context.Commit(false));
                }

                var info = err.ErrorInfo;
                var errors = context.Commit(false).ToList();
                if (info != null)
                {
                    errors.Add(new Diagnostic(info, context.Location(type)));
                }

                var syms = (info is CSDiagnosticInfo) ? (info as CSDiagnosticInfo).Symbols : Enumerable.Empty<Symbol>();
                return new SymbolInfo(result, syms, errors);
            }
        }

        /// <summary>
        /// Gets the enclosing BinderContext associated with the node
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private BinderContext GetEnclosingContext(SyntaxNode node)
        {
            // special case if node is from interior of a member declaration (method body, etc)
            var memberDecl = GetMemberDeclaration(node);
            if (memberDecl != null && memberDecl != node)
            {
                var memberContext = GetMemberContext(memberDecl);
                if (memberContext != null)
                {
                    return memberContext.GetEnclosingContext(node);
                }
            }
            // get the enclosing binder context (declaration level)
            return this.builder.GetContext(node);
        }

        private SyntaxNode GetMemberDeclaration(SyntaxNode node)
        {
            // TODO: may need to return other types like AccessorDeclarationSyntax
            return node.GetAncestor<MemberDeclarationSyntax>();
        }

        private ConcurrentDictionary<SyntaxNode, MemberBinderContext> memberContexts = new ConcurrentDictionary<SyntaxNode, MemberBinderContext>();
        private MemberBinderContext GetMemberContext(SyntaxNode node)
        {
            var memberDecl = GetMemberDeclaration(node);
            if (memberDecl != null)
            {
                if (this.fnCreateMemberContext == null)
                    this.fnCreateMemberContext = this.CreateMemberContext;

                return memberContexts.GetOrAdd(memberDecl, this.fnCreateMemberContext);
            }
            return null;
        }

        private Func<SyntaxNode, MemberBinderContext> fnCreateMemberContext;
        private MemberBinderContext CreateMemberContext(SyntaxNode node)
        {
            switch (node.Kind)
            {
                case SyntaxKind.MethodDeclaration:
                    var methodDecl = (MethodDeclarationSyntax)node;
                    var symbol = (SourceMethodSymbol)GetSymbolFromDeclaration(methodDecl);
                    var outer = this.GetEnclosingContext(node);
                    return new MethodBodyBinderContext(symbol, outer, methodDecl);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Given a namespace declaration, get the corresponding namespace symbol.
        /// </summary>
        public NamespaceSymbol GetNamespaceFromDeclaration(NamespaceDeclarationSyntax declarationSyntax)
        {
            var outer = GetEnclosingContext(declarationSyntax);
            return outer.BindNamespace(declarationSyntax.Name, outer);
        }

        /// <summary>
        /// Given a type declaration, get the corresponding type symbol.
        /// </summary>
        public NamedTypeSymbol GetTypeFromDeclaration(TypeDeclarationSyntax declarationSyntax)
        {
            var outer = GetEnclosingContext(declarationSyntax);
            var nameStart = declarationSyntax.Identifier.Span.Start;

            var result = LookupResult.GetInstance();
            outer.LookupType(result, declarationSyntax.Identifier.ValueText, declarationSyntax.Arity, outer, null);
            var symbols = result.Symbols;

            for (int i = 0; i < symbols.Count; i++)
            {
                var t = symbols[i];
                foreach (var l in t.Locations)
                {
                    if (l.SourceTree == tree && l.SourceSpan.Start == nameStart)
                    {
                        result.Free();
                        return (NamedTypeSymbol)t;
                    }
                }
            }
            result.Free();
            return null;
        }

        /// <summary>
        /// Given any member declaration, get the corresponding type symbol.
        /// </summary>
        public Symbol GetSymbolFromDeclaration(MemberDeclarationSyntax declarationSyntax)
        {
            var namespaceDeclarationSyntax = declarationSyntax as NamespaceDeclarationSyntax;
            if (namespaceDeclarationSyntax != null)
            {
                return GetNamespaceFromDeclaration(namespaceDeclarationSyntax);
            }
            var typeDeclarationSyntax = declarationSyntax as TypeDeclarationSyntax;
            if (typeDeclarationSyntax != null)
            {
                return GetTypeFromDeclaration(typeDeclarationSyntax);
            }
            typeDeclarationSyntax = (TypeDeclarationSyntax)declarationSyntax.Parent;
            var typeSymbol = GetTypeFromDeclaration(typeDeclarationSyntax);

            foreach (var symbol in typeSymbol.GetMembers())
            {
                if (symbol.Locations.Any(l => l.SourceTree == tree && declarationSyntax.Span.Contains(l.SourceSpan)))
                {
                    return symbol;
                }
            }
            return null;
        }

        internal override TypeSymbol GetExpressionType(ExpressionSyntax expression)
        {
            var context = this.GetMemberContext(expression);
            if (context != null)
            {
                return context.GetExpressionType(expression);
            }
            return null;
        }

        internal override Symbol GetExpressionSymbol(ExpressionSyntax expression)
        {
            var context = this.GetMemberContext(expression);
            if (context != null)
            {
                return context.GetExpressionSymbol(expression);
            }
            return null;
        }

        internal override Symbol GetAssociatedSymbol(SyntaxToken token)
        {
            var expr = token.Parent as ExpressionSyntax;
            if (expr != null)
            {
                return GetExpressionSymbol(expr);
            }
            // TODO: handle other cases
            return null;
        }

        /// <summary>
        /// Get a speculative binding for an expression as if it were
        /// declared in the scope identified by the location
        /// </summary>
        /// <returns></returns>
        internal SyntaxBinding BindExpression(SyntaxNode location, ExpressionSyntax expression)
        {
            var mbc = this.GetMemberContext(location);
            if (mbc != null)
            {
                return mbc.BindExpression(mbc.GetEnclosingContext(location), expression);
            }
            return null;
        }

        /// <summary>
        /// Get a speculative binding for a statement as if it were
        /// declared in the scope identified by the location
        /// </summary>
        /// <returns></returns>
        internal SyntaxBinding BindStatement(SyntaxNode location, StatementSyntax statement)
        {
            var mbc = this.GetMemberContext(location);
            if (mbc != null)
            {
                return mbc.BindStatement(mbc.GetEnclosingContext(location), statement);
            }
            return null;
        }

        /// <summary>
        /// Speculative binding: bind a type as if it appeared at a given point in the syntax
        /// of a program.  After this has been done, 
        /// the GetBinding methods can be used on parts of the "text" tree to get further information about
        /// the results of binding.
        /// </summary>
        /// <param name="location">The location where the type expression should be bound</param>
        /// <param name="typeName">The type name expression to be bound</param>
        /// <param name="arity">The arity of the type name to be bound</param>
        /// <returns></returns>
        public SymbolInfo BindType(SyntaxNode location, TypeSyntax typeName, int arity = 0)
        {
            return LookupType(GetEnclosingContext(location), typeName, arity);
        }

        // public abstract SymbolInfo Bind(SyntaxTree tree, SyntaxNode location, SyntaxNode text);
        // public abstract SymbolInfo BindNamespaceOrType(SyntaxTree tree, SyntaxNode location, TypeSyntax text);

        ///// <summary>
        ///// Get the definition points of the given symbol.
        ///// </summary>
        ///// <param name="symbol">The symbol whose definition points are to be returned.</param>
        ///// <returns></returns>
        // public abstract IEnumerable<Location> GetDefinitions(Symbol symbol);

        ///// <summary>
        ///// Get the points in the syntax that reference (not define) the given symbol.
        ///// </summary>
        ///// <param name="symbol">The symbol whose references are to be returned.</param>
        ///// <param name="within">The bounds of the tree within which references are to be found.
        ///// if within==null, then we find all uses within the compilation.</param>
        ///// <returns></returns>
        // public abstract IEnumerable<SyntaxTree> GetUsesWithin(Symbol symbol, SyntaxTree tree, SyntaxTree within);
        // public abstract IEnumerable<SyntaxTree> GetUsesGlobally(Symbol symbol);
        // public abstract IEnumerable<SyntaxTree> GetAllSymbolUses(SyntaxTree within);

        ///// <summary>
        ///// Bind an expression at a given point in the syntax of a program.  After this has been done, 
        ///// the GetBinding methods can be used on parts of the "text" tree to get further information about
        ///// the results of binding.
        ///// </summary>
        ///// <param name="location">The location where the expression should be bound</param>
        ///// <param name="text">The body of the expression to be bound</param>
        ///// <returns></returns>
        // public abstract Tuple<Expression/*??*/, IEnumerable<Diagnostic>> BindExpression(SyntaxTree tree, SyntaxNode location, ExpressionSyntax text);
        // TODO: LINQ's Expression might not be the right way to return a result

        ///// <summary>
        ///// Bind a statement at a given point in the syntax of a program.  After this has been done, 
        ///// the GetBinding methods can be used on parts of the "text" tree to get further information about
        ///// the results of binding.
        ///// </summary>
        ///// <param name="location">The location where the statement should be bound</param>
        ///// <param name="text">The body of the statement to be bound</param>
        ///// <returns></returns>
        // public abstract Tuple<Expression/*??*/, IEnumerable<Diagnostic>> BindStatement(SyntaxTree tree, SyntaxNode location, StatementSyntax text);
        // TODO: LINQ's Expression might not be the right way to return a result
    }
#endif
}
