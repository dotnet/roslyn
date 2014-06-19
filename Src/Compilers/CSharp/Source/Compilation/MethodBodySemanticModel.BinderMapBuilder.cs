using System.Diagnostics;
using Roslyn.Utilities;

namespace Roslyn.Compilers.CSharp
{
    partial class MethodBodySemanticModel
    {
        // The BinderMapBuilder is used to build up the map of all local Binders, and the associated
        // SyntaxNode. To do so it traverses all the statements, handling blocks and other
        // statements that create scopes. For efficiency reasons, it does not traverse into
        // expressions. This means that blocks within lambdas and queries are not created. 
        // Blocks within lambdas are bound by their own binder map builders when they are 
        // analyzed.
        private sealed class BinderMapBuilder : SyntaxVisitor<Binder, object>
        {
            private ImmutableMap<SyntaxNode, Binder> map;
            private readonly MethodSymbol method;

            public static ImmutableMap<SyntaxNode, Binder> BuildMap(MethodSymbol method, SyntaxNode syntax, Binder enclosing, ImmutableMap<SyntaxNode, Binder> map = null)
            {
                var builder = new BinderMapBuilder(method, map);
                builder.Visit(syntax, enclosing);
                return builder.map;
            }

            public static ImmutableMap<SyntaxNode, Binder> BuildGlobalStatementsMap(MethodSymbol method, CompilationUnitSyntax compilationUnitSyntax, Binder enclosing)
            {
                Debug.Assert(method.ContainingType.IsScriptClass && method.MethodKind == MethodKind.Constructor);

                var builder = new BinderMapBuilder(method, null);

                // This loop is inlined (instead of overriding VisitCompilationUnit) because
                // it is not a general implementation of building a map for a compilation unit -
                // it is specific to the global statement case.
                foreach (MemberDeclarationSyntax member in compilationUnitSyntax.Members)
                {
                    if (member.Kind == SyntaxKind.GlobalStatement)
                    {
                        builder.Visit(member, enclosing);
                    }
                }
                return builder.map;
            }

            private BinderMapBuilder(MethodSymbol method, ImmutableMap<SyntaxNode, Binder> map)
            {
                this.map = map ?? ImmutableMap<SyntaxNode, Binder>.Empty;
                this.method = method;
            }

            #region Starting points - these nodes contain statements

            protected internal override object VisitMethodDeclaration(MethodDeclarationSyntax node, Binder enclosing)
            {
                return VisitBlock(node.BodyOpt, enclosing);
            }

            protected internal override object VisitConstructorDeclaration(ConstructorDeclarationSyntax node, Binder enclosing)
            {
                return VisitBlock(node.BodyOpt, enclosing);
            }

            protected internal override object VisitAccessorDeclaration(AccessorDeclarationSyntax node, Binder enclosing)
            {
                return VisitBlock(node.BodyOpt, enclosing);
            }

            protected internal override object VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node, Binder enclosing)
            {
                BlockSyntax blockSyntax = node.Body as BlockSyntax;
                return blockSyntax == null ? null : VisitBlock(blockSyntax, enclosing);
            }

            protected internal override object VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node, Binder enclosing)
            {
                BlockSyntax blockSyntax = node.Body as BlockSyntax;
                return blockSyntax == null ? null : VisitBlock(blockSyntax, enclosing);
            }

            protected internal override object VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node, Binder enclosing)
            {
                return VisitBlock(node.Block, enclosing);
            }

            protected internal override object VisitGlobalStatement(GlobalStatementSyntax node, Binder enclosing)
            {
                return Visit(node.Statement, enclosing);
            }

            #endregion Starting points

            // Top-level block has an enclosing that is not a BinderContext. All others must (so that variables can be declared).
            protected internal override object VisitBlock(BlockSyntax node, Binder enclosing)
            {
                var blockBinder = new BlockBinder(this.method, enclosing, node.Statements);
                this.map = this.map.Add(node, blockBinder);

                // Visit all the statements inside this block
                foreach (StatementSyntax statement in node.Statements)
                {
                    Visit(statement, blockBinder);
                }

                return null;
            }

            protected internal override object VisitUsingStatement(UsingStatementSyntax node, Binder enclosing)
            {
                var usingBinder = new UsingStatementBinder(this.method, enclosing, node);
                this.map = this.map.Add(node, usingBinder);

                Visit(node.Statement, usingBinder);
                return null;
            }

            protected internal override object VisitWhileStatement(WhileStatementSyntax node, Binder enclosing)
            {
                var whileBinder = new WhileBinder(this.method, enclosing, node);
                this.map = this.map.Add(node, whileBinder);

                Visit(node.Statement, whileBinder);
                return null;
            }

            protected internal override object VisitDoStatement(DoStatementSyntax node, Binder enclosing)
            {
                var whileBinder = new WhileBinder(this.method, enclosing, node);
                this.map = this.map.Add(node, whileBinder);

                Visit(node.Statement, whileBinder);
                return null;
            }

            protected internal override object VisitForStatement(ForStatementSyntax node, Binder enclosing)
            {
                var binder = new ForLoopBinder(this.method, enclosing, node);
                this.map = this.map.Add(node, binder);

                Visit(node.Statement, binder);
                return null;
            }

            protected internal override object VisitForEachStatement(ForEachStatementSyntax node, Binder enclosing)
            {
                // UNDONE: Create special binder type for "foreach"?
                //var binder = new ForEachBinderContext(this.method, enclosing, node);
                //this.map = this.map.Add(node, binder);

                Visit(node.Statement, enclosing);
                return null;
            }

            protected internal override object VisitCheckedStatement(CheckedStatementSyntax node, Binder enclosing)
            {
                // UNDONE: Create a special binder for "checked"?
                Visit(node.Block, enclosing);
                return null;
            }

            protected internal override object VisitUnsafeStatement(UnsafeStatementSyntax node, Binder enclosing)
            {
                // UNDONE: Create a special binder for "unsafe"?
                Visit(node.Block, enclosing);
                return null;
            }

            protected internal override object VisitFixedStatement(FixedStatementSyntax node, Binder enclosing)
            {
                // UNDONE: Create a special binder for "fixed"?
                Visit(node.Statement, enclosing);
                return null;
            }

            protected internal override object VisitLockStatement(LockStatementSyntax node, Binder enclosing)
            {
                // UNDONE: Create special binder type for "lock"?
                //var binder = new LockBinderContext(this.method, enclosing, node);
                //this.map = this.map.Add(node, binder);

                Visit(node.Statement, enclosing);
                return null;
            }

            protected internal override object VisitSwitchStatement(SwitchStatementSyntax node, Binder enclosing)
            {
                // UNDONE: Create special binder type for switch.
                //var binder = new SwitchBinderContext(this.method, enclosing, node);
                //this.map = this.map.Add(node, binder);

                foreach (SwitchSectionSyntax section in node.Sections)
                {
                    Visit(section, enclosing);
                }

                return null;
            }

            protected internal override object VisitSwitchSection(SwitchSectionSyntax node, Binder enclosing)
            {
                foreach (StatementSyntax statement in node.Statements)
                {
                    Visit(statement, enclosing);
                }
                return null;
            }

            protected internal override object VisitIfStatement(IfStatementSyntax node, Binder enclosing)
            {
                Visit(node.Statement, enclosing);
                Visit(node.ElseOpt, enclosing);
                return null;
            }

            protected internal override object VisitElseClause(ElseClauseSyntax node, Binder enclosing)
            {
                Visit(node.Statement, enclosing);
                return null;
            }

            protected internal override object VisitLabeledStatement(LabeledStatementSyntax node, Binder enclosing)
            {
                Visit(node.Statement, enclosing);
                return null;
            }

            protected internal override object VisitTryStatement(TryStatementSyntax node, Binder enclosing)
            {
                Visit(node.Block, enclosing);

                foreach (var c in node.Catches)
                {
                    Visit(c, enclosing);
                }

                if (node.FinallyOpt != null)
                {
                    Visit(node.FinallyOpt, enclosing);
                }

                return null;
            }

            protected internal override object VisitCatchClause(CatchClauseSyntax node, Binder enclosing)
            {
                Visit(node.Block, enclosing);

                return null;
            }

            protected internal override object VisitFinallyClause(FinallyClauseSyntax node, Binder enclosing)
            {
                Visit(node.Block, enclosing);

                return null;
            }

            protected override object DefaultVisit(SyntaxNode node, Binder argument)
            {
                // We should only get here for statements that don't introduce new scopes.
                Debug.Assert(node is StatementSyntax);
                return base.DefaultVisit(node, argument);
            }
        }
    }
}