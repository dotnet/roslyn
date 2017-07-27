// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
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
            var parentMap = GetParentOperationsMap(model);

            // go through all foundings to see whether parent is correct
            foreach (var kv in parentMap)
            {
                var child = kv.Key;
                var parent = kv.Value;

                // check parent property returns same parent we gathered by walking down operation tree
                Assert.Equal(child.Parent, parent);

                // check SearchparentOperation return same parent
                Assert.Equal(((Operation)child).SearchParentOperation(), parent);
            }
        }
    }
}
