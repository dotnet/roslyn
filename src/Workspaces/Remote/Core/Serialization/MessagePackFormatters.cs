// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Runtime.Serialization;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Defines MessagePack formatters for public types without a public constructor suitable for deserialization.
    /// Roslyn internal types should always be annotated with <see cref="DataContractAttribute"/> and have the right constructor.
    /// </summary>
    internal sealed class MessagePackFormatters
    {
        internal static readonly ImmutableArray<IMessagePackFormatter> Formatters = ImmutableArray.Create<IMessagePackFormatter>(
            // ForceTypelessFormatter<T> needs to be listed here for each Roslyn abstract type T that is being serialized OOP.
            // TODO: add a resolver that provides these https://github.com/dotnet/roslyn/issues/60724
            new ForceTypelessFormatter<SimplifierOptions>(),
            new ForceTypelessFormatter<SyntaxFormattingOptions>(),
            new ForceTypelessFormatter<CodeGenerationOptions>(),
            new ForceTypelessFormatter<IdeCodeStyleOptions>());

        private static readonly ImmutableArray<IFormatterResolver> s_resolvers = ImmutableArray.Create<IFormatterResolver>(
            StandardResolverAllowPrivate.Instance);

        internal static readonly IFormatterResolver DefaultResolver = CompositeResolver.Create(Formatters, s_resolvers);

        internal static IFormatterResolver CreateResolver(ImmutableArray<IMessagePackFormatter> additionalFormatters, ImmutableArray<IFormatterResolver> additionalResolvers)
            => (additionalFormatters.IsEmpty && additionalResolvers.IsEmpty) ? DefaultResolver : CompositeResolver.Create(Formatters.AddRange(additionalFormatters), s_resolvers.AddRange(additionalResolvers));
    }
}
