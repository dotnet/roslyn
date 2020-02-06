// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Editor.ReferenceHighlighting;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    public class WellKnownTagNames
    {
        public const string MarkerFormatDefinition_HighlightedReference = "MarkerFormatDefinition/HighlightedReference";
        public const string MarkerFormatDefinition_HighlightedDefinition = "MarkerFormatDefinition/HighlightedDefinition";
        public const string MarkerFormatDefinition_HighlightedWrittenReference = "MarkerFormatDefinition/HighlightedWrittenReference";

        public static Type GetTagTypeByName(string typeName)
        {
            switch (typeName)
            {
                case MarkerFormatDefinition_HighlightedReference: return typeof(ReferenceHighlightTag);
                case MarkerFormatDefinition_HighlightedDefinition: return typeof(DefinitionHighlightTag);
                case MarkerFormatDefinition_HighlightedWrittenReference: return typeof(WrittenReferenceHighlightTag);
                default: return null;
            }
        }
    }
}
