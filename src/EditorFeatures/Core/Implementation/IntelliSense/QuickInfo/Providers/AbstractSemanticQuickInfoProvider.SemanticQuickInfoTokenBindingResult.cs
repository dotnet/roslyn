// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
{
    internal abstract partial class AbstractSemanticQuickInfoProvider
    {
        internal class SemanticQuickInfoTokenBindingResult
        {
            public SemanticQuickInfoTokenBindingResult(
                SemanticModel semanticModel, 
                ImmutableArray<ISymbol> symbols, 
                ImmutableArray<SyntaxNode> captureFlowAnalysisNodes)
            {
                SemanticModel = semanticModel;
                Symbols = symbols;
                CaptureFlowAnalysisNodes = captureFlowAnalysisNodes;
            }

            public SemanticModel SemanticModel { get; }
            public ImmutableArray<ISymbol> Symbols { get; }
            public ImmutableArray<SyntaxNode> CaptureFlowAnalysisNodes { get; }
        }
    }
}
