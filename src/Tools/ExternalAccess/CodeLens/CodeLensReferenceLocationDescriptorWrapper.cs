// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CodeLens;

namespace Microsoft.CodeAnalysis.ExternalAccess.CodeLens
{
    public readonly struct CodeLensReferenceLocationDescriptorWrapper
    {
        internal CodeLensReferenceLocationDescriptorWrapper(ReferenceLocationDescriptor underlyingObject)
        {
            UnderlyingObject = underlyingObject;
        }

        internal ReferenceLocationDescriptor UnderlyingObject { get; }

        public Guid ProjectGuid => UnderlyingObject.ProjectGuid;
        public Guid DocumentGuid => UnderlyingObject.DocumentGuid;
        public string FilePath => UnderlyingObject.FilePath;
        public int SpanStart => UnderlyingObject.SpanStart;
        public int SpanLength => UnderlyingObject.SpanLength;
        public int LineNumber => UnderlyingObject.LineNumber;
        public int ColumnNumber => UnderlyingObject.ColumnNumber;
        public string Language => UnderlyingObject.Language;
        public string LongDescription => UnderlyingObject.LongDescription;
        public CodeLensGlyph? Glyph => UnderlyingObject.Glyph != null ? (CodeLensGlyph)UnderlyingObject.Glyph.Value : default(CodeLensGlyph?);
        public string ReferenceLineText => UnderlyingObject.ReferenceLineText;
        public int ReferenceStart => UnderlyingObject.ReferenceStart;
        public int ReferenceLength => UnderlyingObject.ReferenceLength;
        public string BeforeReferenceText1 => UnderlyingObject.BeforeReferenceText1;
        public string BeforeReferenceText2 => UnderlyingObject.BeforeReferenceText2;
        public string AfterReferenceText1 => UnderlyingObject.AfterReferenceText1;
        public string AfterReferenceText2 => UnderlyingObject.AfterReferenceText2;

    }
}
