// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Formatting
{
    /// <summary>
    /// data that will be used in an interval tree related to suppressing wrapping operations.
    /// </summary>
    internal class SuppressWrappingData
    {
        public SuppressWrappingData(TextSpan textSpan, bool noWrapping)
        {
            this.TextSpan = textSpan;
            this.NoWrapping = noWrapping;
        }

        public TextSpan TextSpan { get; }
        public bool NoWrapping { get; }
    }
}
