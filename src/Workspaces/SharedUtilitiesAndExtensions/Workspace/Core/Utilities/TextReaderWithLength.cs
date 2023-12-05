// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal abstract class TextReaderWithLength(int length) : TextReader
    {
        public int Length { get; } = length;

        public override string ReadToEnd()
        {
#if NETCOREAPP
            return string.Create(Length, this, static (chars, state) => state.Read(chars));
#else
            var chars = new char[Length];

            var read = base.Read(chars, 0, Length);

            return new string(chars, 0, read);
#endif                
        }
    }
}
