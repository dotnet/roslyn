﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    [ExportOptionProvider, Shared]
    internal class InternalDiagnosticsOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        public InternalDiagnosticsOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            InternalDiagnosticsOptions.CompilationEndCodeFix,
            InternalDiagnosticsOptions.UseCompilationEndCodeFixHeuristic,
            InternalDiagnosticsOptions.PreferLiveErrorsOnOpenedFiles,
            InternalDiagnosticsOptions.PreferBuildErrorsOverLiveErrors);
    }
}
