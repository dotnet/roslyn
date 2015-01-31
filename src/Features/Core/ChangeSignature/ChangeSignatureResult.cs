// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    internal sealed class ChangeSignatureResult
    {
        public bool Succeeded { get; private set; }
        public Solution UpdatedSolution { get; private set; }
        public string Name { get; private set; }
        public Glyph? Glyph { get; private set; }
        public bool PreviewChanges { get; private set; }

        public ChangeSignatureResult(bool succeeded, Solution updatedSolution = null, string name = null, Glyph? glyph = null, bool previewChanges = false)
        {
            this.Succeeded = succeeded;
            this.UpdatedSolution = updatedSolution;
            this.Name = name;
            this.Glyph = glyph;
            this.PreviewChanges = previewChanges;
        }
    }
}
