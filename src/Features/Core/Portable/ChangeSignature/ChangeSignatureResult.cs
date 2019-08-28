// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    internal sealed class ChangeSignatureResult
    {
        public bool Succeeded { get; }
        public Solution UpdatedSolution { get; }
        public string Name { get; }
        public Glyph? Glyph { get; }
        public bool PreviewChanges { get; }

        public ChangeSignatureResult(bool succeeded, Solution updatedSolution = null, string name = null, Glyph? glyph = null, bool previewChanges = false)
        {
            Succeeded = succeeded;
            UpdatedSolution = updatedSolution;
            Name = name;
            Glyph = glyph;
            PreviewChanges = previewChanges;
        }
    }
}
