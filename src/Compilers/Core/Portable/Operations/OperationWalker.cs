// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a <see cref="OperationVisitor"/> that descends an entire <see cref="IOperation"/> tree
    /// visiting each IOperation and its child IOperation nodes in depth-first order.
    /// </summary>
    public abstract class OperationWalker : OperationVisitor
    {
        private int _recursionDepth;

        internal void VisitArray<T>(IEnumerable<T> operations) where T : IOperation
        {
            foreach (var operation in operations)
            {
                VisitOperationArrayElement(operation);
            }
        }

        internal void VisitOperationArrayElement<T>(T operation) where T : IOperation
        {
            Visit(operation);
        }

        public override void Visit(IOperation operation)
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
            VisitArray(operation.Children);
        }

        internal override void VisitNoneOperation(IOperation operation)
        {
            VisitArray(operation.Children);
        }
    }
}
