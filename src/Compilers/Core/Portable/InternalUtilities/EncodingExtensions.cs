// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    internal static partial class EncodingExtensions
    {
        internal static bool TryGetMaxCharCount(this Encoding encoding, long length, out int maxCharCount)
        {
            maxCharCount = 0;

            if (length <= int.MaxValue)
            {
                try
                {
                    maxCharCount = encoding.GetMaxCharCount((int)length);
                    return true;
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Encoding does not provide a way to predict that max byte count would not
                    // fit in Int32 and we must therefore catch ArgumentOutOfRange to handle that
                    // case.
                }
            }

            return false;
        }
    }
}
