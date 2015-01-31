// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A program location in metadata.
    /// </summary>
    internal sealed class MetadataLocation : Location, IEquatable<MetadataLocation>
    {
        private readonly IModuleSymbol _module;

        internal MetadataLocation(IModuleSymbol module)
        {
            Debug.Assert(module != null);
            _module = module;
        }

        public override LocationKind Kind
        {
            get { return LocationKind.MetadataFile; }
        }

        public override IModuleSymbol MetadataModule
        {
            get { return _module; }
        }

        public override int GetHashCode()
        {
            return _module.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MetadataLocation);
        }

        public bool Equals(MetadataLocation other)
        {
            return other != null && other._module == _module;
        }
    }
}
