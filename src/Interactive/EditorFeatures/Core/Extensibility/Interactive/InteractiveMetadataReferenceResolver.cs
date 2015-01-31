// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis.Scripting;

namespace Microsoft.CodeAnalysis.Editor.Interactive
{
#if TODO
    internal sealed class InteractiveMetadataReferenceResolver : MetadataReferencePathResolver
    {
        private readonly GacFileResolver gacResolver;

        internal InteractiveMetadataReferenceResolver(ImmutableArray<string> searchPaths, string baseDirectory)
        {
            this.gacResolver = new GacFileResolver(
                searchPaths,
                baseDirectory: baseDirectory,
                architectures: GacFileResolver.Default.Architectures,  // TODO (tomat)
                preferredCulture: System.Globalization.CultureInfo.CurrentCulture); // TODO (tomat)
        }

        public override string ResolveReference(string reference, string baseFilePath)
        {
            return gacResolver.ResolveReference(reference, baseFilePath);
        }

        public override bool Equals(object obj)
        {
            var other = obj as InteractiveMetadataReferenceResolver;
            return other != null && 
                this.gacResolver.Equals(other.gacResolver);
        }

        public override int GetHashCode()
        {
            return gacResolver.GetHashCode();
        }

        public ImmutableArray<string> SearchPaths
        {
            get { return gacResolver.SearchPaths; }
        }

        public string BaseDirectory
        {
            get { return gacResolver.BaseDirectory; }
        }
    }
#endif
}
