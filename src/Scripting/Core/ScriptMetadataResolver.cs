// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable 436 // The type 'RelativePathResolver' conflicts with imported type

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Scripting
{
    using static ParameterValidationHelpers;

    public sealed class ScriptMetadataResolver : MetadataReferenceResolver, IEquatable<ScriptMetadataResolver>
    {
        public static ScriptMetadataResolver Default { get; } = new ScriptMetadataResolver(
            RuntimeMetadataReferenceResolver.CreateCurrentPlatformResolver(ImmutableArray<string>.Empty, baseDirectory: null));

        private readonly RuntimeMetadataReferenceResolver _resolver;

        public ImmutableArray<string> SearchPaths => _resolver.PathResolver.SearchPaths;
        public string? BaseDirectory => _resolver.PathResolver.BaseDirectory;

        internal ScriptMetadataResolver(RuntimeMetadataReferenceResolver resolver)
        {
            _resolver = resolver;
        }

        public ScriptMetadataResolver WithSearchPaths(params string[] searchPaths)
            => WithSearchPaths(searchPaths.AsImmutableOrEmpty());

        public ScriptMetadataResolver WithSearchPaths(IEnumerable<string> searchPaths)
            => WithSearchPaths(searchPaths.AsImmutableOrEmpty());

        public ScriptMetadataResolver WithSearchPaths(ImmutableArray<string> searchPaths)
        {
            if (SearchPaths == searchPaths)
            {
                return this;
            }

            return new ScriptMetadataResolver(_resolver.WithRelativePathResolver(
                _resolver.PathResolver.WithSearchPaths(ToImmutableArrayChecked(searchPaths, nameof(searchPaths)))));
        }

        public ScriptMetadataResolver WithBaseDirectory(string? baseDirectory)
        {
            if (BaseDirectory == baseDirectory)
            {
                return this;
            }

            if (baseDirectory != null)
            {
                CompilerPathUtilities.RequireAbsolutePath(baseDirectory, nameof(baseDirectory));
            }

            return new ScriptMetadataResolver(_resolver.WithRelativePathResolver(
                _resolver.PathResolver.WithBaseDirectory(baseDirectory)));
        }

        public override bool ResolveMissingAssemblies => _resolver.ResolveMissingAssemblies;

        public override PortableExecutableReference? ResolveMissingAssembly(MetadataReference definition, AssemblyIdentity referenceIdentity)
            => _resolver.ResolveMissingAssembly(definition, referenceIdentity);

        public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string? baseFilePath, MetadataReferenceProperties properties)
            => _resolver.ResolveReference(reference, baseFilePath, properties);

        public bool Equals(ScriptMetadataResolver? other) => _resolver.Equals(other);
        public override bool Equals(object? other) => Equals(other as ScriptMetadataResolver);
        public override int GetHashCode() => _resolver.GetHashCode();
    }
}
