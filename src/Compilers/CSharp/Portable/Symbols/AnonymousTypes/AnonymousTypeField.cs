// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Describes anonymous type field in terms of its name, type and other attributes
    /// </summary>
    internal struct AnonymousTypeField
    {
        /// <summary>Anonymous type field name, not nothing and not empty</summary>
        public readonly string Name;

        /// <summary>Anonymous type field location</summary>
        public readonly Location Location;

        /// <summary>Anonymous type field type</summary>
        public readonly TypeSymbolWithAnnotations Type;

        public AnonymousTypeField(string name, Location location, TypeSymbolWithAnnotations type)
        {
            this.Name = name;
            this.Location = location;
            this.Type = type;
        }

        [Conditional("DEBUG")]
        internal void AssertIsGood()
        {
            Debug.Assert(this.Name != null && this.Location != null && (object)this.Type != null);
        }
    }
}
