// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A program location in metadata.
    /// </summary>
    [Serializable]
    internal sealed class MetadataLocation : Location, IEquatable<MetadataLocation>
    {
        private readonly IModuleSymbol module;

        public MetadataLocation(IModuleSymbol module)
        {
            Debug.Assert(module != null);
            this.module = module;
        }

        public override LocationKind Kind
        {
            get { return LocationKind.MetadataFile; }
        }

        public override IModuleSymbol MetadataModule
        {
            get { return module; }
        }

        public override int GetHashCode()
        {
            return module.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MetadataLocation);
        }

        public bool Equals(MetadataLocation other)
        {
            return other != null && other.module == this.module;
        }
    }
}