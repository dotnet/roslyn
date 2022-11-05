// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class RefSafetyAnalysis : BoundTreeWalker
    {
        internal static void Analyze(CSharpCompilation compilation, Symbol symbol, BoundNode node, BindingDiagnosticBag diagnostics)
        {
            var visitor = new RefSafetyAnalysis(
                compilation,
                symbol,
                inUnsafeRegion: InUnsafeMethod(symbol),
                useUpdatedEscapeRules: symbol.ContainingModule.UseUpdatedEscapeRules,
                diagnostics);
            visitor.Visit(node);
        }

        // PROTOTYPE: Remove this method!
        internal static uint GetRefEscapeOnly(CSharpCompilation compilation, Symbol symbol, BoundExpression expr, uint scopeOfTheContainingExpression, BindingDiagnosticBag diagnostics)
        {
            var visitor = new RefSafetyAnalysis(
                compilation,
                symbol,
                inUnsafeRegion: InUnsafeMethod(symbol),
                useUpdatedEscapeRules: symbol.ContainingModule.UseUpdatedEscapeRules,
                diagnostics);
            return visitor.GetRefEscape(expr, scopeOfTheContainingExpression);
        }

        // PROTOTYPE: Remove this method!
        internal static uint GetValEscapeOnly(CSharpCompilation compilation, Symbol symbol, BoundExpression expr, uint scopeOfTheContainingExpression, BindingDiagnosticBag diagnostics)
        {
            var visitor = new RefSafetyAnalysis(
                compilation,
                symbol,
                inUnsafeRegion: InUnsafeMethod(symbol),
                useUpdatedEscapeRules: symbol.ContainingModule.UseUpdatedEscapeRules,
                diagnostics);
            return visitor.GetValEscape(expr, scopeOfTheContainingExpression);
        }

        // PROTOTYPE: Remove this method!
        internal static void ValidateEscapeOnly(CSharpCompilation compilation, Symbol symbol, BoundExpression expr, uint escapeTo, bool isByRef, BindingDiagnosticBag diagnostics)
        {
            var visitor = new RefSafetyAnalysis(
                compilation,
                symbol,
                inUnsafeRegion: InUnsafeMethod(symbol),
                useUpdatedEscapeRules: symbol.ContainingModule.UseUpdatedEscapeRules,
                diagnostics);
            visitor.ValidateEscape(expr, escapeTo, isByRef, diagnostics);
        }

        private static bool InUnsafeMethod(Symbol symbol)
        {
            if (symbol is SourceMemberMethodSymbol { IsUnsafe: true })
            {
                return true;
            }

            var type = symbol.ContainingType;
            while (type is { })
            {
                var def = type.OriginalDefinition;
                if (def is SourceMemberContainerTypeSymbol { IsUnsafe: true })
                {
                    return true;
                }
                type = def.ContainingType;
            }

            return false;
        }

        private readonly CSharpCompilation _compilation;
        private readonly Symbol _symbol;
        private readonly bool _useUpdatedEscapeRules;
        private readonly BindingDiagnosticBag _diagnostics;

        private bool _inUnsafeRegion;
        private uint _localScopeDepth;

        private RefSafetyAnalysis(
            CSharpCompilation compilation,
            Symbol symbol,
            bool inUnsafeRegion,
            bool useUpdatedEscapeRules,
            BindingDiagnosticBag diagnostics)
        {
            _compilation = compilation;
            _symbol = symbol;
            _useUpdatedEscapeRules = useUpdatedEscapeRules;
            _diagnostics = diagnostics;
            _inUnsafeRegion = inUnsafeRegion;
            _localScopeDepth = Binder.CurrentMethodScope;
        }

        protected override BoundExpression? VisitExpressionWithoutStackGuard(BoundExpression node)
        {
            throw ExceptionUtilities.Unreachable();
        }

        private ref struct LocalScope
        {
            private readonly RefSafetyAnalysis _analysis;
            private readonly uint _previousDepth;

            public LocalScope(RefSafetyAnalysis analysis, uint localScopeDepth)
            {
                Debug.Assert(analysis._localScopeDepth <= localScopeDepth);
                _analysis = analysis;
                _previousDepth = analysis._localScopeDepth;
                _analysis._localScopeDepth = localScopeDepth;
            }

            public void Dispose()
            {
                _analysis._localScopeDepth = _previousDepth;
            }
        }

        private ref struct UnsafeRegion
        {
            private readonly RefSafetyAnalysis _analysis;
            private readonly bool _previousRegion;

            public UnsafeRegion(RefSafetyAnalysis analysis, bool inUnsafeRegion)
            {
                _analysis = analysis;
                _previousRegion = analysis._inUnsafeRegion;
                _analysis._inUnsafeRegion = inUnsafeRegion;
            }

            public void Dispose()
            {
                _analysis._inUnsafeRegion = _previousRegion;
            }
        }

        public override BoundNode? VisitBlock(BoundBlock node)
        {
            using var _ = new LocalScope(this, node.LocalScopeDepth);
            return base.VisitBlock(node);
        }

        public override BoundNode? VisitLocalFunctionStatement(BoundLocalFunctionStatement node)
        {
            // PROTOTYPE: Test all combinations of { safe, unsafe } for { container, local function }.
            var localFunction = node.Symbol;
            // PROTOTYPE: Test local function within unsafe block.
            var analysis = new RefSafetyAnalysis(_compilation, localFunction, localFunction.IsUnsafe, _useUpdatedEscapeRules, _diagnostics);
            analysis.Visit(node.BlockBody);
            analysis.Visit(node.ExpressionBody);
            return null;
        }

        public override BoundNode? VisitLambda(BoundLambda node)
        {
            var lambda = node.Symbol;
            var analysis = new RefSafetyAnalysis(_compilation, lambda, _inUnsafeRegion, _useUpdatedEscapeRules, _diagnostics);
            analysis.Visit(node.Body);
            return null;
        }

        public override BoundNode? VisitForStatement(BoundForStatement node)
        {
            using var _ = new LocalScope(this, node.LocalScopeDepth);
            return base.VisitForStatement(node);
        }

        public override BoundNode? VisitUsingStatement(BoundUsingStatement node)
        {
            using var _ = new LocalScope(this, node.LocalScopeDepth);
            return base.VisitUsingStatement(node);
        }

        public override BoundNode? VisitFixedStatement(BoundFixedStatement node)
        {
            using var _ = new LocalScope(this, node.LocalScopeDepth);
            return base.VisitFixedStatement(node);
        }

        public override BoundNode? VisitSwitchSection(BoundSwitchSection node)
        {
            using var _ = new LocalScope(this, node.LocalScopeDepth);
            return base.VisitSwitchSection(node);
        }

        public override BoundNode? VisitLocalDeclaration(BoundLocalDeclaration node)
        {
            // Verify LocalScopeDepth is correct.
#if DEBUG
            if (node.LocalSymbol is SourceLocalSymbol local)
            {
                Debug.Assert(local.ScopeBinder.LocalScopeDepth == _localScopeDepth);
            }
#endif
            return base.VisitLocalDeclaration(node);
        }

        // PROTOTYPE: What about 'yield return ...'?
        public override BoundNode? VisitReturnStatement(BoundReturnStatement node)
        {
            if (node.ExpressionOpt is { } expr)
            {
                ValidateEscape(expr, Binder.ReturnOnlyScope, node.RefKind != RefKind.None, _diagnostics);
            }
            return base.VisitReturnStatement(node);
        }

        public override BoundNode? VisitAssignmentOperator(BoundAssignmentOperator node)
        {
            ValidateAssignment(node.Syntax, node.Left, node.Right, node.IsRef, _diagnostics);
            return base.VisitAssignmentOperator(node);
        }

        public override BoundNode? VisitConditionalOperator(BoundConditionalOperator node)
        {
            if (node.IsRef)
            {
                ValidateRefConditionalOperator(node.Syntax, node.Consequence, node.Alternative, _diagnostics);
            }
            return base.VisitConditionalOperator(node);
        }

        public override BoundNode? VisitCall(BoundCall node)
        {
            var method = node.Method;
            CheckInvocationArgMixing(
                node.Syntax,
                method,
                node.ReceiverOpt,
                method.Parameters,
                node.Arguments,
                node.ArgumentRefKindsOpt,
                node.ArgsToParamsOpt,
                _localScopeDepth,
                _diagnostics);
            return base.VisitCall(node);
        }

        public override BoundNode? VisitObjectCreationExpression(BoundObjectCreationExpression node)
        {
            var constructor = node.Constructor;
            CheckInvocationArgMixing(
                node.Syntax,
                constructor,
                receiverOpt: null,
                constructor.Parameters,
                node.Arguments,
                node.ArgumentRefKindsOpt,
                node.ArgsToParamsOpt,
                _localScopeDepth,
                _diagnostics);
            return base.VisitObjectCreationExpression(node);
        }

        public override BoundNode? VisitIndexerAccess(BoundIndexerAccess node)
        {
            var indexer = node.Indexer;
            CheckInvocationArgMixing(
                node.Syntax,
                indexer,
                node.ReceiverOpt,
                indexer.Parameters,
                node.Arguments,
                node.ArgumentRefKindsOpt,
                node.ArgsToParamsOpt,
                _localScopeDepth,
                _diagnostics);
            return base.VisitIndexerAccess(node);
        }

        public override BoundNode? VisitDeconstructionAssignmentOperator(BoundDeconstructionAssignmentOperator node)
        {
            var right = node.Right;
            var conversion = right.Conversion;
            Debug.Assert(conversion.Kind == ConversionKind.Deconstruction);
            if (conversion.DeconstructionInfo.Invocation is BoundCall deconstruct)
            {
                var method = deconstruct.Method;
                CheckInvocationArgMixing(
                    right.Syntax,
                    method,
                    deconstruct.ReceiverOpt,
                    method.Parameters,
                    deconstruct.Arguments,
                    deconstruct.ArgumentRefKindsOpt,
                    deconstruct.ArgsToParamsOpt,
                    _localScopeDepth,
                    _diagnostics);
            }
            return base.VisitDeconstructionAssignmentOperator(node);
        }

        private static void Error(BindingDiagnosticBag diagnostics, ErrorCode code, SyntaxNodeOrToken syntax, params object[] args)
        {
            var location = syntax.GetLocation();
            RoslynDebug.Assert(location is object);
            Error(diagnostics, code, location, args);
        }

        private static void Error(BindingDiagnosticBag diagnostics, ErrorCode code, Location location, params object[] args)
        {
            diagnostics.Add(new CSDiagnostic(new CSDiagnosticInfo(code, args), location));
        }
    }
}
