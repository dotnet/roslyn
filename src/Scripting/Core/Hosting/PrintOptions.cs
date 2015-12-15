// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    public class PrintOptions
    {
        private NumberRadix _numberRadix = NumberRadix.Decimal;
        private MemberDisplayFormat _memberDisplayFormat;
        private bool _escapeNonPrintableCharacters;
        private int _maximumOutputLength = 1024;

        public NumberRadix NumberRadix
        {
            get
            {
                return _numberRadix;
            }

            set
            {
                if (!value.IsValid())
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _numberRadix = value;
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

        public bool EscapeNonPrintableCharacters
        {
            get
            {
                return _escapeNonPrintableCharacters;
            }

            set
            {
                _escapeNonPrintableCharacters = value;
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