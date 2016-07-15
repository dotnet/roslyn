// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitAwaitExpression(BoundAwaitExpression node)
        {
            _sawAwait = true;
            var getAwaiter = node.GetAwaiter;
            var getResult = node.GetResult;
            var isCompleted = node.IsCompleted;
            var changed = false;
            if ((object)getAwaiter != null && (getAwaiter.IsInExtensionClass || getAwaiter.IsExtensionMethod))
            {
                getAwaiter = getAwaiter.UnreduceExtensionMethod();
                changed = true;
            }
            if ((object)getResult != null && (getResult.IsInExtensionClass || getResult.IsExtensionMethod))
            {
                getResult = getResult.UnreduceExtensionMethod();
                changed = true;
            }
            if ((object)isCompleted != null && isCompleted.IsInExtensionClass)
            {
                isCompleted = isCompleted.UnreduceExtensionProperty();
                changed = true;
            }
            if (changed)
            {
                node = node.Update(node.Expression, getAwaiter, isCompleted, getResult, node.Type);
            }
            return base.VisitAwaitExpression(node);
        }
    }
}
