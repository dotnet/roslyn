// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public static ScriptMetadataResolver Default { get; } = new ScriptMetadataResolver(ImmutableArray<string>.Empty, null);

        private readonly RuntimeMetadataReferenceResolver _resolver;

        public ImmutableArray<string> SearchPaths => _resolver.PathResolver.SearchPaths;
        public string BaseDirectory => _resolver.PathResolver.BaseDirectory;

        private ScriptMetadataResolver(ImmutableArray<string> searchPaths, string baseDirectoryOpt)
        {
            _resolver = new RuntimeMetadataReferenceResolver(searchPaths, baseDirectoryOpt);
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

            return new ScriptMetadataResolver(ToImmutableArrayChecked(searchPaths, nameof(searchPaths)), BaseDirectory);
        }

        public ScriptMetadataResolver WithBaseDirectory(string baseDirectory)
        {
            if (BaseDirectory == baseDirectory)
            {
                return this;
            }

            if (baseDirectory != null)
            {
                CompilerPathUtilities.RequireAbsolutePath(baseDirectory, nameof(baseDirectory));
            }

            return new ScriptMetadataResolver(SearchPaths, baseDirectory);
        }

        public override bool ResolveMissingAssemblies => _resolver.ResolveMissingAssemblies;

        public override PortableExecutableReference ResolveMissingAssembly(MetadataReference definition, AssemblyIdentity referenceIdentity)
            => _resolver.ResolveMissingAssembly(definition, referenceIdentity);

        public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties)
            => _resolver.ResolveReference(reference, baseFilePath, properties);

        public bool Equals(ScriptMetadataResolver other) => _resolver.Equals(other);
        public override bool Equals(object other) => Equals(other as ScriptMetadataResolver);
        public override int GetHashCode() => _resolver.GetHashCode();
    }
}
