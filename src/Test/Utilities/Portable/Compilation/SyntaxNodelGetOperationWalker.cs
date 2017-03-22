// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public abstract class SyntaxNodeGetOperationWalker : SyntaxWalker
    {
        public SemanticModel SemanticModel { get; set; }

        // syntax nodes with null or OperationKind.None operation
        public List<(SyntaxNode node, IOperation operation)> SyntaxNodesWithNoOperation { get; }

        public SyntaxNodeGetOperationWalker()
        {
            SyntaxNodesWithNoOperation = new List<(SyntaxNode, IOperation)>();
        }

        public override void Visit(SyntaxNode node)
        {
            Debug.Assert(SemanticModel.SyntaxTree == node.SyntaxTree);

            if (!IsSyntaxNodeKindExcluded(node))
            {
                var operation = SemanticModel.GetOperationInternal(node);
                if (operation == null || operation.Kind == OperationKind.None)
                {
                    SyntaxNodesWithNoOperation.Add((node, operation));
                }
            }

            base.Visit(node);
        }

        protected override void VisitToken(SyntaxToken token)
        {
            // No need to check tokens.
        }

        protected abstract bool IsSyntaxNodeKindExcluded(SyntaxNode node);

        public void Report()
        {
            if (SyntaxNodesWithNoOperation.Any())
            {
                var sb = new StringBuilder();
                foreach ((var node, var operation) in SyntaxNodesWithNoOperation)
                {
                    var operationType = operation == null ? "null" : operation.GetType().Name;
                    sb.AppendLine($"{node.GetType().Name} : Operation - {operationType}");
                    sb.AppendLine($"{node.ToString()}");
                    sb.AppendLine("------------------------------------------");
                }
                Assert.True(false, sb.ToString());
            }
        }        

        public static void CheckSyntaxTrees(SyntaxNodeGetOperationWalker walker, Compilation compilation)
        {
            if (walker == null || compilation == null)
            {
                return;
            }

            foreach (var tree in compilation.SyntaxTrees)
            {
                walker.SemanticModel = compilation.GetSemanticModel(tree);
                walker.Visit(tree.GetRoot());
            }
            walker.Report();
        }
    }
}
