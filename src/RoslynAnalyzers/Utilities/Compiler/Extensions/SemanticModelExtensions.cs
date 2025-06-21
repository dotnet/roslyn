//// Licensed to the .NET Foundation under one or more agreements.
//// The .NET Foundation licenses this file to you under the MIT license.
//// See the LICENSE file in the project root for more information.

//#nullable disable warnings

//using System.Threading;
//using Microsoft.CodeAnalysis;

//namespace Analyzer.Utilities.Extensions
//{
//    internal static class SemanticModelExtensions
//    {
//        public static IOperation? GetOperationWalkingUpParentChain(this SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
//        {
//            // Walk up the parent chain to fetch the first non-null operation.
//            do
//            {
//                var operation = semanticModel.GetOperation(node, cancellationToken);
//                if (operation != null)
//                {
//                    return operation;
//                }

//                node = node.Parent;
//            }
//            while (node != null);

//            return null;
//        }
//    }
//}
