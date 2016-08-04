// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeLens
{
    /// <summary>
    /// Holds information required to display and navigate to individual references
    /// </summary>
    internal sealed class ReferenceLocationDescriptor
    {
        public int LineNumber { get; }

        public int ColumnNumber { get; }

        public DocumentId Document { get; }

        public string Language { get; }

        public string LongDescription { get; }

        public Glyph? Glyph { get; }

        public string ReferenceLineText { get; }

        public int ReferenceStart { get; }

        public int ReferenceLength { get; }

        public string BeforeReferenceText1 { get; }

        public string BeforeReferenceText2 { get; }

        public string AfterReferenceText1 { get; }

        public string AfterReferenceText2 { get; }

        public ReferenceLocationDescriptor(Solution solution, Location location, DisplayInfo displayInfo)
        {
            Language = displayInfo.Language;
            LongDescription = displayInfo.LongName;
            Glyph = displayInfo.Glyph;
            LinePosition sourceText = location.GetLineSpan().StartLinePosition;
            LineNumber = sourceText.Line;
            ColumnNumber = sourceText.Character;
            // We want to keep track of the location's document if it comes from a file in your solution.
            var document = solution.GetDocument(location.SourceTree);
            Document = document?.Id;
            ReferenceLineText = displayInfo.ReferenceLineText;
            ReferenceStart = displayInfo.ReferenceStart;
            ReferenceLength = displayInfo.ReferenceLength;
            BeforeReferenceText1 = displayInfo.BeforeReferenceText1;
            BeforeReferenceText2 = displayInfo.BeforeReferenceText2;
            AfterReferenceText1 = displayInfo.AfterReferenceText1;
            AfterReferenceText2 = displayInfo.AfterReferenceText2;
        }
    }
}
