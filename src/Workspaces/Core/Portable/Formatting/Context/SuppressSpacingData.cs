// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Formatting
{
    /// <summary>
    /// data that will be used in an interval tree related to suppressing spacing operations.
    /// </summary>
    internal class SuppressSpacingData
    {
        public SuppressSpacingData(TextSpan textSpan)
        {
            this.TextSpan = textSpan;
        }

        public TextSpan TextSpan { get; }
    }
}
