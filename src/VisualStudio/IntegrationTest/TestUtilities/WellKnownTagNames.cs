// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
