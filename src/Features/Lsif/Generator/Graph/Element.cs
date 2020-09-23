// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph
{
    /// <summary>
    /// The base class of an element in the LSIF format.
    /// </summary>
    internal abstract class Element
    {
        public Id<Element> Id { get; }
        public string Type { get; }
        public string Label { get; }

        protected Element(string type, string label, IdFactory idFactory)
        {
            this.Id = idFactory.Create<Element>();
            this.Label = label;
            this.Type = type;
        }
    }
}
