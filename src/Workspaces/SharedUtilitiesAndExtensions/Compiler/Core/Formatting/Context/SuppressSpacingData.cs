// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Formatting
{
    /// <summary>
    /// data that will be used in an interval tree related to suppressing spacing operations.
    /// </summary>
    internal class SuppressSpacingData(TextSpan textSpan)
    {
        public TextSpan TextSpan { get; } = textSpan;
    }
}
