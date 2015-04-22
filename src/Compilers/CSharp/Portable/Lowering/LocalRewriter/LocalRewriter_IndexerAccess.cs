// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        private BoundExpression MakeDynamicIndexerAccessReceiver(BoundDynamicIndexerAccess indexerAccess, BoundExpression loweredReceiver)
        {
            BoundExpression result;

            string indexedPropertyName = indexerAccess.TryGetIndexedPropertyName();
            if (indexedPropertyName != null)
            {
                // Dev12 forces the receiver to be typed to dynamic to workaround a bug in the runtime binder.
                // See DynamicRewriter::FixupIndexedProperty:
                // "If we don't do this, then the calling object is statically typed and we pass the UseCompileTimeType to the runtime binder."
                // However, with the cast the scenarios don't work either, so we don't mimic Dev12.
                // loweredReceiver = BoundConversion.Synthesized(loweredReceiver.Syntax, loweredReceiver, Conversion.Identity, false, false, null, DynamicTypeSymbol.Instance);

                result = _dynamicFactory.MakeDynamicGetMember(loweredReceiver, indexedPropertyName, resultIndexed: true).ToExpression();
            }
            else
            {
                result = loweredReceiver;
            }

            return result;
        }

        public override BoundNode VisitDynamicIndexerAccess(BoundDynamicIndexerAccess node)
        {
            Debug.Assert(node.ReceiverOpt != null);

            var loweredReceiver = VisitExpression(node.ReceiverOpt);
            var loweredArguments = VisitList(node.Arguments);

            return MakeDynamicGetIndex(node, loweredReceiver, loweredArguments, node.ArgumentNamesOpt, node.ArgumentRefKindsOpt);
        }

        private BoundExpression MakeDynamicGetIndex(
            BoundDynamicIndexerAccess node,
            BoundExpression loweredReceiver,
            ImmutableArray<BoundExpression> loweredArguments,
            ImmutableArray<string> argumentNames,
            ImmutableArray<RefKind> refKinds)
        {
            // If we are calling a method on a NoPIA type, we need to embed all methods/properties
            // with the matching name of this dynamic invocation.
            EmbedIfNeedTo(loweredReceiver, node.ApplicableIndexers, node.Syntax);

            return _dynamicFactory.MakeDynamicGetIndex(
                MakeDynamicIndexerAccessReceiver(node, loweredReceiver),
                loweredArguments,
                argumentNames,
                refKinds).ToExpression();
        }

        public override BoundNode VisitIndexerAccess(BoundIndexerAccess node)
        {
            Debug.Assert(node.Indexer.IsIndexer || node.Indexer.IsIndexedProperty);
            Debug.Assert((object)node.Indexer.GetOwnOrInheritedGetMethod() != null);

            return VisitIndexerAccess(node, isLeftOfAssignment: false);
        }

        private BoundExpression VisitIndexerAccess(BoundIndexerAccess node, bool isLeftOfAssignment)
        {
            PropertySymbol indexer = node.Indexer;
            Debug.Assert(indexer.IsIndexer || indexer.IsIndexedProperty);

            // Rewrite the receiver.
            BoundExpression rewrittenReceiver = VisitExpression(node.ReceiverOpt);

            // Rewrite the arguments.
            // NOTE: We may need additional argument rewriting such as generating a params array, re-ordering arguments based on argsToParamsOpt map, inserting arguments for optional parameters, etc.
            // NOTE: This is done later by MakeArguments, for now we just lower each argument.
            ImmutableArray<BoundExpression> rewrittenArguments = VisitList(node.Arguments);

            return MakeIndexerAccess(node.Syntax, rewrittenReceiver, indexer, rewrittenArguments, node.ArgumentNamesOpt,
                 node.ArgumentRefKindsOpt, node.Expanded, node.ArgsToParamsOpt, node.Type, node, isLeftOfAssignment);
        }

        private BoundExpression MakeIndexerAccess(
            CSharpSyntaxNode syntax,
            BoundExpression rewrittenReceiver,
            PropertySymbol indexer,
            ImmutableArray<BoundExpression> rewrittenArguments,
            ImmutableArray<string> argumentNamesOpt,
            ImmutableArray<RefKind> argumentRefKindsOpt,
            bool expanded,
            ImmutableArray<int> argsToParamsOpt,
            TypeSymbol type,
            BoundIndexerAccess oldNodeOpt,
            bool isLeftOfAssignment)
        {
            if (isLeftOfAssignment && indexer.RefKind == RefKind.None)
            {
                // This is an indexer set access. We return a BoundIndexerAccess node here.
                // This node will be rewritten with MakePropertyAssignment when rewriting the enclosing BoundAssignmentOperator.

                return oldNodeOpt != null ?
                    oldNodeOpt.Update(rewrittenReceiver, indexer, rewrittenArguments, argumentNamesOpt, argumentRefKindsOpt, expanded, argsToParamsOpt, type) :
                    new BoundIndexerAccess(syntax, rewrittenReceiver, indexer, rewrittenArguments, argumentNamesOpt, argumentRefKindsOpt, expanded, argsToParamsOpt, type);
            }
            else
            {
                var getMethod = indexer.GetOwnOrInheritedGetMethod();
                Debug.Assert((object)getMethod != null);

                // We have already lowered each argument, but we may need some additional rewriting for the arguments,
                // such as generating a params array, re-ordering arguments based on argsToParamsOpt map, inserting arguments for optional parameters, etc.
                ImmutableArray<LocalSymbol> temps;
                rewrittenArguments = MakeArguments(syntax, rewrittenArguments, indexer, getMethod, expanded, argsToParamsOpt, ref argumentRefKindsOpt, out temps, enableCallerInfo: ThreeState.True);

                BoundExpression call = MakePropertyGetAccess(syntax, rewrittenReceiver, indexer, rewrittenArguments, getMethod);

                if (temps.IsDefaultOrEmpty)
                {
                    return call;
                }
                else
                {
                    return new BoundSequence(
                        syntax,
                        temps,
                        ImmutableArray<BoundExpression>.Empty,
                        call,
                        type);
                }
            }
        }
    }
}
