// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    [ExportOptionProvider, Shared]
    internal sealed class CodeStyleOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CodeStyleOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = CodeStyleOptions2.AllOptions.As<IOption>();
    }
}
