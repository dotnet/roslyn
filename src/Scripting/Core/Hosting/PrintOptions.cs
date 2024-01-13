// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using static Microsoft.CodeAnalysis.Scripting.Hosting.ObjectFormatterHelpers;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    public class PrintOptions
    {
        private int _numberRadix = NumberRadixDecimal;
        private MemberDisplayFormat _memberDisplayFormat;
        private int _maximumOutputLength = 1024;

        public string Ellipsis { get; set; } = "...";
        public bool EscapeNonPrintableCharacters { get; set; } = true;

        public int NumberRadix
        {
            get
            {
                return _numberRadix;
            }

            set
            {
                if (!IsValidRadix(value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _numberRadix = value;
            }
        }

        /// <remarks>
        /// Virtual so that extenders can support other radices.
        /// </remarks>
        protected virtual bool IsValidRadix(int radix)
        {
            switch (radix)
            {
                case NumberRadixDecimal:
                case NumberRadixHexadecimal:
                    return true;
                default:
                    return false;
            }
        }

        public MemberDisplayFormat MemberDisplayFormat
        {
            get
            {
                return _memberDisplayFormat;
            }

            set
            {
                if (!value.IsValid())
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _memberDisplayFormat = value;
            }
        }

        public int MaximumOutputLength
        {
            get
            {
                return _maximumOutputLength;
            }

            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _maximumOutputLength = value;
            }
        }
    }
}
