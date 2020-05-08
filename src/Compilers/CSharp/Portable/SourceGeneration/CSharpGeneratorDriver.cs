// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;

#nullable enable
namespace Microsoft.CodeAnalysis.CSharp
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0016:Add public types and members to the declared API", Justification = "In Progress")]
    public sealed class CSharpGeneratorDriver : GeneratorDriver
    {
        public CSharpGeneratorDriver(ParseOptions parseOptions, ImmutableArray<ISourceGenerator> generators, ImmutableArray<AdditionalText> additionalTexts)
            : base(parseOptions, generators, additionalTexts)
        {
        }

        private CSharpGeneratorDriver(GeneratorDriverState state)
            : base(state)
        {
        }

        internal override SyntaxTree ParseGeneratedSourceText(GeneratedSourceText input, CancellationToken cancellationToken)
            => SyntaxFactory.ParseSyntaxTree(input.Text, _state.ParseOptions, input.HintName, cancellationToken); // https://github.com/dotnet/roslyn/issues/42628: hint path/ filename uniqueness

        internal override GeneratorDriver FromState(GeneratorDriverState state) => new CSharpGeneratorDriver(state);

        internal override CommonMessageProvider MessageProvider => CSharp.MessageProvider.Instance;
    }
}
