// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A program location in metadata.
    /// </summary>
    internal sealed class MetadataLocation : Location, IEquatable<MetadataLocation>
    {
        private readonly IModuleSymbolInternal _module;

        internal MetadataLocation(IModuleSymbolInternal module)
        {
            Debug.Assert(module != null);
            _module = module;
        }

        public override LocationKind Kind
        {
            get { return LocationKind.MetadataFile; }
        }

        internal override IModuleSymbolInternal MetadataModuleInternal
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
