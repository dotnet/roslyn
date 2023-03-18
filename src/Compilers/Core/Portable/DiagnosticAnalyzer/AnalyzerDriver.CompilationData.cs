// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal abstract partial class AnalyzerDriver : IDisposable
    {
        internal sealed class CompilationData
        {
            public CompilationData(Compilation compilation)
            {
                SemanticModelProvider = (CachingSemanticModelProvider)compilation.SemanticModelProvider!;
                SuppressMessageAttributeState = new SuppressMessageAttributeState(compilation);
            }

            public CachingSemanticModelProvider SemanticModelProvider { get; }
            public SuppressMessageAttributeState SuppressMessageAttributeState { get; }
        }
    }
}
