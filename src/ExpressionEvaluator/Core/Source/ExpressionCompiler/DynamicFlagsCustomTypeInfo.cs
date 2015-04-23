// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal struct DynamicFlagsCustomTypeInfo
    {
        /// <remarks>Internal for testing.</remarks>
        internal static readonly Guid PayloadTypeId = new Guid("826D6EC1-DC4B-46AF-BE05-CD3F1A1FD4AC");

        private readonly BitArray _bits;

        public DynamicFlagsCustomTypeInfo(BitArray bits)
        {
            _bits = bits;
        }

        public DynamicFlagsCustomTypeInfo(DkmClrCustomTypeInfo typeInfo)
        {
            if (typeInfo == null || typeInfo.PayloadTypeId != PayloadTypeId)
            {
                _bits = null;
            }
            else
            {
                var builder = ArrayBuilder<byte>.GetInstance();
                builder.AddRange(typeInfo.Payload);
                _bits = new BitArray(builder.ToArrayAndFree());
            }
        }

        public bool this[int i]
        {
            get
            {
                Debug.Assert(i >= 0);
                return _bits != null &&
                    i < _bits.Length &&
                    _bits[i];
            }
        }

        /// <remarks>
        /// Not guarantee to add the same number of flags as would
        /// appear in a <see cref="System.Runtime.CompilerServices.DynamicAttribute"/>.
        /// It may have more (for padding) or fewer (for compactness) falses.
        /// It is, however, guaranteed to include the last true.
        /// </remarks>
        internal void CopyTo(ArrayBuilder<bool> builder)
        {
            if (_bits == null)
            {
                return;
            }

            for (int b = 0; b < _bits.Length; b++)
            {
                builder.Add(_bits[b]);
            }
        }

        internal byte[] GetCustomTypeInfoPayload()
        {
            if (!Any())
            {
                return null;
            }

            var numBits = _bits.Length;
            var numBytes = (numBits + 7) / 8;
            var bytes = new byte[numBytes];

            // Unfortunately, BitArray.CopyTo is not portable.
            for (int b = 0; b < numBytes; b++)
            {
                for (int i = 0; i < 8; i++)
                {
                    var index = 8 * b + i;
                    if (index < numBits && _bits[index])
                    {
                        bytes[b] |= (byte)(1 << i);
                    }
                }
            }

            return bytes;
        }

        public DkmClrCustomTypeInfo GetCustomTypeInfo()
        {
            var payload = GetCustomTypeInfoPayload();
            return payload == null ? null : DkmClrCustomTypeInfo.Create(PayloadTypeId, new ReadOnlyCollection<byte>(payload));
        }

        public DynamicFlagsCustomTypeInfo SkipOne()
        {
            if (_bits == null)
            {
                return this;
            }

            var numBits = _bits.Length;
            var newBits = new BitArray(numBits - 1);
            for (int b = 0; b < numBits - 1; b++)
            {
                newBits[b] = _bits[b + 1];
            }

            return new DynamicFlagsCustomTypeInfo(newBits);
        }

        public bool Any()
        {
            if (_bits == null)
            {
                return false;
            }

            for (int b = 0; b < _bits.Length; b++)
            {
                if (_bits[b])
                {
                    return true;
                }
            }

            return false;
        }
    }
}
