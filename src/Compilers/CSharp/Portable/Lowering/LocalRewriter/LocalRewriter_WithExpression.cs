// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitWithExpression(BoundWithExpression withExpr)
        {
            RoslynDebug.AssertNotNull(withExpr.CloneMethod);
            Debug.Assert(withExpr.CloneMethod.ParameterCount == 0);

            // for a with expression of the form
            //
            //      receiver with { P1 = e1, P2 = e2 }
            //
            // we want to lower it to a call to the receiver's `Clone` method, then
            // set the given record properties. i.e.
            //
            //      var tmp = receiver.Clone();
            //      tmp.P1 = e1;
            //      tmp.P2 = e2;
            //      tmp
            var F = _factory;
            var stores = ArrayBuilder<BoundExpression>.GetInstance(withExpr.Arguments.Length + 1);

            // var tmp = receiver.Clone();
            var receiverLocal = F.StoreToTemp(
                F.InstanceCall(VisitExpression(withExpr.Receiver), "Clone"),
                out var receiverStore);
            stores.Add(receiverStore);

            // tmp.Pn = En;
            foreach (var arg in withExpr.Arguments)
            {
                RoslynDebug.AssertNotNull(arg.Member);
                var left = arg.Member switch
                {
                    PropertySymbol p => MakePropertyAccess(
                        withExpr.Syntax,
                        receiverLocal,
                        p,
                        receiverLocal.ResultKind,
                        p.Type,
                        isLeftOfAssignment: true),
                    FieldSymbol f => MakeFieldAccess(
                        withExpr.Syntax,
                        receiverLocal,
                        f,
                        constantValueOpt: null,
                        receiverLocal.ResultKind,
                        f.Type),
                    _ => throw ExceptionUtilities.UnexpectedValue(arg.Member.Kind)
                };
                stores.Add(MakeStaticAssignmentOperator(
                    arg.Expression.Syntax,
                    left,
                    VisitExpression(arg.Expression),
                    isRef: false,
                    left.Type!,
                    used: false));
            }

            return new BoundSequence(
                withExpr.Syntax,
                ImmutableArray.Create(receiverLocal.LocalSymbol),
                stores.ToImmutableAndFree(),
                receiverLocal,
                withExpr.Type);
        }
    }
}