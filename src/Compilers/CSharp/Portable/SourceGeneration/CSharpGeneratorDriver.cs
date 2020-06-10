// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

#nullable enable
namespace Microsoft.CodeAnalysis.CSharp
{
    public sealed class CSharpGeneratorDriver : GeneratorDriver
    {
        public CSharpGeneratorDriver(ParseOptions parseOptions, ImmutableArray<ISourceGenerator> generators, AnalyzerConfigOptionsProvider optionsProvider, ImmutableArray<AdditionalText> additionalTexts)
            : base(parseOptions, generators, optionsProvider, additionalTexts)
        {
        }

        private CSharpGeneratorDriver(GeneratorDriverState state)
            : base(state)
        {
        }

        internal override SyntaxTree ParseGeneratedSourceText(GeneratedSourceText input, string fileName, CancellationToken cancellationToken)
            => SyntaxFactory.ParseSyntaxTree(input.Text, _state.ParseOptions, fileName, cancellationToken);

        internal override GeneratorDriver FromState(GeneratorDriverState state) => new CSharpGeneratorDriver(state);

        internal override CommonMessageProvider MessageProvider => CSharp.MessageProvider.Instance;
    }
}
