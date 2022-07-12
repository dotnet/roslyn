// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Rename
{
    internal class MultipleSymbolRenameRewriter : CSharpAbstractRenameRewriter
    {
        public MultipleSymbolRenameRewriter(
            RenameRewriterParametersNextGen parameters)
            : base(parameters.Document,
                  parameters.OriginalSolution,
                  parameters.ConflictLocationSpans,
                  parameters.SemanticModel,
                  parameters.RenameSpansTracker,
                  parameters.RenameAnnotations,
                  parameters.CancellationToken)
        {
        }
    }
}
