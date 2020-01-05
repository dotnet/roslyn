// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    [ExportOptionProvider, Shared]
    internal sealed class CSharpFormattingOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        public CSharpFormattingOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = CSharpFormattingOptions.AllOptions;
    }
}
