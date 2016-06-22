// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

#if SRM
namespace System.Reflection
#else
namespace Roslyn.Reflection
#endif
{
#if SRM
    public
#endif
    struct Blob
    {
        internal readonly byte[] Buffer;
        internal readonly int Start;
        public int Length { get; }

        internal Blob(byte[] buffer, int start, int length)
        {
            Buffer = buffer;
            Start = start;
            Length = length;
        }

        public bool IsDefault => Buffer == null;

        public ArraySegment<byte> GetBytes() => new ArraySegment<byte>(Buffer, Start, Length);
    }
}
