// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Editing
{
    [ExportOptionProvider, Shared]
    internal class GenerationOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public GenerationOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = GenerationOptions.AllOptions.As<IOption>();
    }
}
