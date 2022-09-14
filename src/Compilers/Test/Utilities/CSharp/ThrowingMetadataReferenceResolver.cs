// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities;

/// <summary>
/// This simulates our default command line compilation experience where the <see cref="MetadataReferenceResolver"/>
/// throws on equality checks via <see cref="CommonCompiler.LoggingMetadataFileReferenceResolver" />
/// </summary>
public sealed class ThrowingMetadataReferenceResolver : MetadataReferenceResolver
{
    public MetadataReferenceResolver? Resolver { get; }

    public ThrowingMetadataReferenceResolver(MetadataReferenceResolver? resolver = null)
    {
        Resolver = resolver;
    }

    public override bool Equals(object? other) => throw new NotImplementedException();

    public override int GetHashCode() => throw new NotImplementedException();

    public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string? baseFilePath, MetadataReferenceProperties properties)
    {
        if (Resolver is null)
        {
            throw new NotImplementedException();
        }

        return Resolver.ResolveReference(reference, baseFilePath, properties);
    }

}
