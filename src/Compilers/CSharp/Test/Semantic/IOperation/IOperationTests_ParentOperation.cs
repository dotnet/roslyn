// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Semantics;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [Fact]
        public void TestParentOperations()
        {
            var sourceCode = TestResource.AllInOneCSharpCode;

            var compilation = CreateStandardCompilation(sourceCode, new[] { SystemRef, SystemCoreRef, ValueTupleRef, SystemRuntimeFacadeRef }, sourceFileName: "file.cs");
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            // visit tree top down to gather child to parent map
            var parentMap = GetParentMap(model);

            // go through all foundings to see whether parent is correct
            foreach (var kv in parentMap)
            {
                var child = kv.Key;
                var parent = kv.Value;

                Assert.Equal(child.Parent, parent);
            }
        }

        private Dictionary<IOperation, IOperation> GetParentMap(SemanticModel model)
        {
            // get top operations first
            var topOperations = new HashSet<IOperation>();
            var root = model.SyntaxTree.GetRoot();

            CollectTopOperations(model, root, topOperations);

            // dig down the each operation tree to create the parent operation map
            var map = new Dictionary<IOperation, IOperation>();
            foreach (var topOperation in topOperations)
            {
                // this is top of the operation tree
                map.Add(topOperation, null);

                CollectParentOperation(topOperation, map);
            }

            return map;
        }

        private void CollectParentOperation(IOperation operation, Dictionary<IOperation, IOperation> map)
        {
            // walk down to collect all parent operation map for this tree
            foreach (var child in operation.Children.WhereNotNull())
            {
                map.Add(child, operation);

                CollectParentOperation(child, map);
            }
        }

        private static void CollectTopOperations(SemanticModel model, SyntaxNode node, HashSet<IOperation> topOperations)
        {
            foreach (var child in node.ChildNodes())
            {
                var operation = model.GetOperationInternal(child);
                if (operation != null)
                {
                    // found top operation
                    topOperations.Add(operation);

                    // don't dig down anymore
                    continue;
                }

                // sub tree might have the top operation
                CollectTopOperations(model, child, topOperations);
            }
        }
    }
}
