// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Lsif.Generator.LsifGraph
{
    /// <summary>
    /// The base class of an element in the LSIF format.
    /// </summary>
    internal abstract class Element
    {
        public Id<Element> Id { get; }
        public string Type { get; }
        public string Label { get; }

        protected Element(string type, string label)
        {
            this.Id = Id<Element>.Create();
            this.Label = label;
            this.Type = type;
        }
    }
}
