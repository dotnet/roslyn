// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Formatting
{
    /// <summary>
    /// data that will be used in an interval tree related to suppressing spacing operations.
    /// </summary>
    internal class SuppressSpacingData
    {
        public SuppressSpacingData(TextSpan textSpan, bool noSpacing)
        {
            this.TextSpan = textSpan;
            this.NoSpacing = noSpacing;
        }

        public TextSpan TextSpan { get; }
        public bool NoSpacing { get; }
    }
}
