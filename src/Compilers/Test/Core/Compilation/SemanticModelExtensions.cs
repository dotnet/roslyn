// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
