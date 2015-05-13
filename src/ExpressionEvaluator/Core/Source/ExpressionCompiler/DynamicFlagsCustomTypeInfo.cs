// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal struct DynamicFlagsCustomTypeInfo
    {
        /// <remarks>Internal for testing.</remarks>
        internal static readonly Guid PayloadTypeId = new Guid("826D6EC1-DC4B-46AF-BE05-CD3F1A1FD4AC");

        private readonly ReadOnlyCollection<byte> _bytes;

        private DynamicFlagsCustomTypeInfo(ReadOnlyCollection<byte> bytes)
        {
            _bytes = bytes;
        }

        public DynamicFlagsCustomTypeInfo(Guid payloadTypeId, ReadOnlyCollection<byte> payload)
            : this(payloadTypeId == PayloadTypeId ? payload : null)
        {
        }

        public DynamicFlagsCustomTypeInfo(DkmClrCustomTypeInfo typeInfo)
            : this(typeInfo != null && typeInfo.PayloadTypeId == PayloadTypeId ? typeInfo.Payload : null)
        {
        }

        internal DynamicFlagsCustomTypeInfo(bool[] dynamicFlags)
        {
            if (dynamicFlags == null || dynamicFlags.Length == 0)
            {
                _bytes = null;
                return;
            }

            int numFlags = dynamicFlags.Length;
            int numBytes = ((numFlags - 1) / 8) + 1;
            byte[] bytes = new byte[numBytes];
            for (int b = 0; b < numBytes; b++)
            {
                for (int i = 0; i < 8; i++)
                {
                    var f = b * 8 + i;
                    if (f >= numFlags)
                    {
                        Debug.Assert(f == numFlags);
                        goto ALL_FLAGS_READ;
                    }

                    if (dynamicFlags[f])
                    {
                        bytes[b] |= (byte)(1 << i);
                    }
                }
            }

            ALL_FLAGS_READ:

            _bytes = new ReadOnlyCollection<byte>(bytes);
        }

        public bool this[int i]
        {
            get
            {
                Debug.Assert(i >= 0);
                var b = i / 8;
                return _bytes != null &&
                    b < _bytes.Count &&
                    (_bytes[b] & (1 << (i % 8))) != 0;
            }
        }

        /// <remarks>
        /// Not guaranteed to add the same number of flags as would
        /// appear in a <see cref="System.Runtime.CompilerServices.DynamicAttribute"/>.
        /// It may have more (for padding) or fewer (for compactness) falses.
        /// It is, however, guaranteed to include the last true.
        /// </remarks>
        internal void CopyTo(ArrayBuilder<bool> builder)
        {
            if (_bytes == null)
            {
                return;
            }

            foreach (byte b in _bytes)
            {
                for (int i = 0; i < 8; i++)
                {
                    builder.Add((b & (1 << i)) != 0);
                }
            }
        }

        internal ReadOnlyCollection<byte> GetCustomTypeInfoPayload() => Any() ? _bytes : null;

        public DkmClrCustomTypeInfo GetCustomTypeInfo() => Any() ? DkmClrCustomTypeInfo.Create(PayloadTypeId, _bytes) : null;

        public DynamicFlagsCustomTypeInfo SkipOne()
        {
            if (_bytes == null)
            {
                return this;
            }

            var numBytes = _bytes.Count;
            var newBytes = new byte[numBytes]; // CONSIDER: In some cases, we could shrink the array.
            for (int b = 0; b < numBytes; b++)
            {
                newBytes[b] = (byte)(_bytes[b] >> 1);
                if (b + 1 < numBytes && (_bytes[b + 1] & 1) != 0)
                {
                    newBytes[b] |= 1 << 7;
                }
            }

            return new DynamicFlagsCustomTypeInfo(new ReadOnlyCollection<byte>(newBytes));
        }

        public bool Any()
        {
            if (_bytes == null)
            {
                return false;
            }

            for (int b = 0; b < _bytes.Count; b++)
            {
                if (_bytes[b] != 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
