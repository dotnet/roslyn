﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal partial class DiagnosticAnalyzerService
{
    private partial class DiagnosticIncrementalAnalyzer
    {
        /// <summary>
        /// EventArgs for <see cref="StateManager.ProjectAnalyzerReferenceChanged"/>
        /// 
        /// this event args contains information such as <see cref="Project"/> the <see cref="AnalyzerReference"/> has changed
        /// and what <see cref="StateSet"/> has changed.
        /// </summary>
        private sealed class ProjectAnalyzerReferenceChangedEventArgs : EventArgs
        {
            public readonly ImmutableArray<StateSet> Removed;

            public ProjectAnalyzerReferenceChangedEventArgs(ImmutableArray<StateSet> removed)
            {
                Removed = removed;
            }
        }
    }
}
