﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Represents region analysis context attributes such as compilation, region, etc...
    /// </summary>
    internal struct RegionAnalysisContext
    {
        /// <summary> Compilation to use </summary>
        public readonly CSharpCompilation Compilation;
        /// <summary> Containing symbol if available, null otherwise </summary>
        public readonly Symbol Member;
        /// <summary> Bound node, not null </summary>
        public readonly BoundNode BoundNode;
        /// <summary> Region to be used </summary>
        public readonly BoundNode FirstInRegion, LastInRegion;
        /// <summary> True if the input was bad, such as no first and last nodes </summary>
        public readonly bool Failed;

        /// <summary>
        /// Construct context
        /// </summary>
        public RegionAnalysisContext(CSharpCompilation compilation, Symbol member, BoundNode boundNode, BoundNode firstInRegion, BoundNode lastInRegion)
        {
            this.Compilation = compilation;
            this.Member = member;
            this.BoundNode = boundNode;
            this.FirstInRegion = firstInRegion;
            this.LastInRegion = lastInRegion;
            this.Failed =
                boundNode == null ||
                firstInRegion == null ||
                lastInRegion == null ||
                firstInRegion.Syntax.SpanStart > lastInRegion.Syntax.Span.End;

            if (!this.Failed && ReferenceEquals(firstInRegion, lastInRegion))
            {
                switch (firstInRegion.Kind)
                {
                    case BoundKind.NamespaceExpression:
                    case BoundKind.TypeExpression:

                        // Some bound nodes are still considered to be invalid for flow analysis
                        this.Failed = true;
                        break;
                }
            }
        }
    }
}
