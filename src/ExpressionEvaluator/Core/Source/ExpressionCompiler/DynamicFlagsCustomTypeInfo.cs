// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal partial struct DynamicFlagsCustomTypeInfo
    {
        /// <remarks>Internal for testing.</remarks>
        internal static readonly Guid PayloadTypeId = new Guid("826D6EC1-DC4B-46AF-BE05-CD3F1A1FD4AC");

        private readonly ReadOnlyCollection<byte> _bytes;

        private DynamicFlagsCustomTypeInfo(ReadOnlyCollection<byte> bytes)
        {
            _bytes = bytes;
        }

        private DynamicFlagsCustomTypeInfo(ArrayBuilder<bool> dynamicFlags, int startIndex)
        {
            Debug.Assert(dynamicFlags != null);
            Debug.Assert(startIndex >= 0);

            int numFlags = dynamicFlags.Count - startIndex;
            Debug.Assert(numFlags > 0);

            int numBytes = (numFlags + 7) / 8;
            byte[] bytes = new byte[numBytes];
            bool seenTrue = false;
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

                    if (dynamicFlags[startIndex + f])
                    {
                        seenTrue = true;
                        bytes[b] |= (byte)(1 << i);
                    }
                }
            }

        ALL_FLAGS_READ:

            _bytes = seenTrue ? new ReadOnlyCollection<byte>(bytes) : null;
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
        /// appear in a System.Runtime.CompilerServices.DynamicAttribute.
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

            var builder = ArrayBuilder<bool>.GetInstance();
            this.CopyTo(builder);
            var result = new DynamicFlagsCustomTypeInfo(builder, startIndex: 1);
            builder.Free();
            return result;
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
