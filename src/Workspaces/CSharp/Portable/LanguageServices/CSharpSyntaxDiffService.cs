// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Differencing;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.CSharp
{
    [ExportLanguageService(typeof(ISyntaxDiffService), LanguageNames.CSharp), Shared]
    internal class CSharpSyntaxDiffService : ISyntaxDiffService
    {
        public IEnumerable<Edit<TreeNode>> Diff(SyntaxNode oldRoot, SyntaxNode newRoot)
        {
            // TODO: we probably don't want to expose TreeNode to out side.
            var script = CSharpTreeNodeComparer.Instance.ComputeEditScript(new TreeNode(oldRoot), new TreeNode(newRoot));

            // *NOTE* we only care about leaf change. not non-leaf change that is caused by leaf nodes.
            // currently we can't use it as it is since TreeComparer doesn't expose enough information to convert old tree to new tree.
            // for example, insert or re-order doesnt let one know where it is inserted or where one is re-ordered to. it just
            // let one know plain diff. not what edit is needed to make that transformation.
            return script.Edits.Where(e => e.NewNode.IsLeaf || e.OldNode.IsLeaf);
        }
    }
}
