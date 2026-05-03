// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A region analysis walker that records jumps into the region.  Works by overriding NoteBranch, which is
    /// invoked by a superclass when the two endpoints of a jump have been identified.
    /// </summary>
    internal class EntryPointsWalker : AbstractRegionControlFlowPass
    {
        internal static IEnumerable<LabeledStatementSyntax> Analyze(CSharpCompilation compilation, Symbol member, BoundNode node, BoundNode firstInRegion, BoundNode lastInRegion, out bool? succeeded)
        {
            var walker = new EntryPointsWalker(compilation, member, node, firstInRegion, lastInRegion);
            bool badRegion = false;
            try
            {
                walker.Analyze(ref badRegion);
                var result = walker._entryPoints;
                succeeded = !badRegion;
                return badRegion ? SpecializedCollections.EmptyEnumerable<LabeledStatementSyntax>() : result;
            }
            finally
            {
                walker.Free();
            }
        }

        private readonly HashSet<LabeledStatementSyntax> _entryPoints = new HashSet<LabeledStatementSyntax>();

        private void Analyze(ref bool badRegion)
        {
            // We only need to scan in a single pass.
            Scan(ref badRegion);
        }

        private EntryPointsWalker(CSharpCompilation compilation, Symbol member, BoundNode node, BoundNode firstInRegion, BoundNode lastInRegion)
            : base(compilation, member, node, firstInRegion, lastInRegion)
        {
        }

        protected override void Free()
        {
            base.Free();
        }

        protected override void NoteBranch(PendingBranch pending, BoundNode gotoStmt, BoundStatement targetStmt)
        {
            targetStmt.AssertIsLabeledStatement();
            if (!gotoStmt.WasCompilerGenerated && !targetStmt.WasCompilerGenerated && RegionContains(targetStmt.Syntax.Span) && !RegionContains(gotoStmt.Syntax.Span))
                _entryPoints.Add((LabeledStatementSyntax)targetStmt.Syntax);
        }
    }
}
