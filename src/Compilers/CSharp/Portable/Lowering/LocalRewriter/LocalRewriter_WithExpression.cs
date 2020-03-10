// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
            RoslynDebug.AssertNotNull(withExpr.WithMethod);
            Debug.Assert(withExpr.WithMembers.Length == withExpr.WithMethod.ParameterCount);

            // for a with expression of the form
            //
            //      receiver with { P1 = e1, P2 = e2 }
            //
            // we want to lower it to a call to the receiver's `With` method, filling
            // in any "missing" parameters with a call to a matching field/property on
            // the receiver
            var F = _factory;
            var tempsCount = withExpr.Arguments.Length + 1;
            var locals = ArrayBuilder<LocalSymbol>.GetInstance(tempsCount);
            var stores = ArrayBuilder<BoundExpression>.GetInstance(tempsCount);

            // First lower the receiver and arguments to temps. This preserves lexical evaluation order
            var receiverLocal = F.StoreToTemp(
                VisitExpression(withExpr.Receiver),
                out var receiverStore);
            locals.Add(receiverLocal.LocalSymbol);
            stores.Add(receiverStore);

            var nameToLocal = PooledDictionary<string, BoundLocal>.GetInstance();
            foreach (var arg in withExpr.Arguments)
            {
                RoslynDebug.AssertNotNull(arg.Member);
                var local = F.StoreToTemp(VisitExpression(arg.Expression), out var store);
                locals.Add(local.LocalSymbol);
                stores.Add(store);
                nameToLocal.Add(arg.Member.Name, local);
            }

            // Now construct call to the WithMethod
            var methodArgs = ArrayBuilder<BoundExpression>.GetInstance(withExpr.WithMembers.Length);
            foreach (var m in withExpr.WithMembers)
            {
                RoslynDebug.AssertNotNull(m);
                if (nameToLocal.TryGetValue(m.Name, out var local))
                {
                    methodArgs.Add(local);
                }
                else
                {
                    // receiver.With(..., receiver.P_n);
                    BoundExpression access;
                    if (m is PropertySymbol p)
                    {
                        access = F.Property(receiverLocal, p);
                    }
                    else
                    {
                        var field = (FieldSymbol)m; 
                        access = F.Field(receiverLocal, field);
                    }
                    methodArgs.Add(access);
                }
            }
            nameToLocal.Free();

            return new BoundSequence(
                withExpr.Syntax,
                locals.ToImmutableAndFree(),
                stores.ToImmutableAndFree(),
                F.Call(receiverLocal, withExpr.WithMethod, methodArgs.ToImmutableAndFree()),
                withExpr.Type);
        }
    }
}