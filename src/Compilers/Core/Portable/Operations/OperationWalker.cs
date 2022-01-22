// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a <see cref="OperationVisitor"/> that descends an entire <see cref="IOperation"/> tree
    /// visiting each IOperation and its child IOperation nodes in depth-first order.
    /// </summary>
    public abstract class OperationWalker : OperationVisitor
    {
        private int _recursionDepth;

        private void VisitChildOperations(IOperation operation)
        {
            foreach (var child in ((Operation)operation).ChildOperations)
            {
                Visit(child);
            }
        }

        public override void Visit(IOperation? operation)
        {
            if (operation != null)
            {
                _recursionDepth++;
                try
                {
                    StackGuard.EnsureSufficientExecutionStack(_recursionDepth);
                    operation.Accept(this);
                }
                finally
                {
                    _recursionDepth--;
                }
            }
        }

        public override void DefaultVisit(IOperation operation)
        {
            VisitChildOperations(operation);
        }

        internal override void VisitNoneOperation(IOperation operation)
        {
            VisitChildOperations(operation);
        }
    }

    /// <summary>
    /// Represents a <see cref="OperationVisitor{TArgument, TResult}"/> that descends an entire <see cref="IOperation"/> tree
    /// visiting each IOperation and its child IOperation nodes in depth-first order. Returns null.
    /// </summary>
    public abstract class OperationWalker<TArgument> : OperationVisitor<TArgument, object?>
    {
        private int _recursionDepth;

        private void VisitChildrenOperations(IOperation operation, TArgument argument)
        {
            foreach (var child in ((Operation)operation).ChildOperations)
            {
                Visit(child, argument);
            }
        }

        public override object? Visit(IOperation? operation, TArgument argument)
        {
            if (operation != null)
            {
                _recursionDepth++;
                try
                {
                    StackGuard.EnsureSufficientExecutionStack(_recursionDepth);
                    operation.Accept(this, argument);
                }
                finally
                {
                    _recursionDepth--;
                }
            }

            return null;
        }

        public override object? DefaultVisit(IOperation operation, TArgument argument)
        {
            VisitChildrenOperations(operation, argument);
            return null;
        }

        internal override object? VisitNoneOperation(IOperation operation, TArgument argument)
        {
            VisitChildrenOperations(operation, argument);
            return null;
        }
    }
}
