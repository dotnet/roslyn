// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Text;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    internal static class SemanticModelExtensions
    {
        public static void VerifyOperationTree(this SemanticModel model, SyntaxNode node, string expectedOperationTree)
        {
            var actualTextBuilder = new StringBuilder();
            AppendOperationTree(model, node, actualTextBuilder);
            OperationTreeVerifier.Verify(expectedOperationTree, actualTextBuilder.ToString());
        }

        public static void AppendOperationTree(this SemanticModel model, SyntaxNode node, StringBuilder actualTextBuilder, int initialIndent = 0)
        {
            IOperation operation = model.GetOperation(node);
            if (operation != null)
            {
                string operationTree = OperationTreeVerifier.GetOperationTree(model.Compilation, operation, initialIndent);
                actualTextBuilder.Append(operationTree);
            }
            else
            {
                actualTextBuilder.Append($"  SemanticModel.GetOperation() returned NULL for node with text: '{node.ToString()}'");
            }
        }
    }
}
