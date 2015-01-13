// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A string that may possibly be formatted differently depending on culture.
    /// NOTE: Types implementing <see cref="LocalizableString"/> must be serializable.
    /// </summary>
    public abstract class LocalizableString : IFormattable
    {
        /// <summary>
        /// Formats the value of the current instance using the optionally specified format. 
        /// </summary>
        public abstract string ToString(IFormatProvider formatProvider);

        public static explicit operator string(LocalizableString localizableResource)
        {
            return localizableResource.ToString(null);
        }

        public static implicit operator LocalizableString(string fixedResource)
        {
            return new FixedLocalizableString(fixedResource);
        }

        public sealed override string ToString()
        {
            return ToString(null);
        }

        string IFormattable.ToString(string ignored, IFormatProvider formatProvider)
        {
            return ToString(formatProvider);
        }

        private sealed class FixedLocalizableString : LocalizableString
        {
            private readonly string fixedString;

            public FixedLocalizableString(string fixedResource)
            {
                this.fixedString = fixedResource;
            }

            public override string ToString(IFormatProvider formatProvider)
            {
                return this.fixedString;
            }
        }
    }
}