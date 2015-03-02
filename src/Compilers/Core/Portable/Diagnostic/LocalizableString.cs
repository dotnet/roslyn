// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A string that may possibly be formatted differently depending on culture.
    /// NOTE: Types implementing <see cref="LocalizableString"/> must be serializable.
    /// </summary>
    public abstract class LocalizableString : IFormattable, IEquatable<LocalizableString>
    {
        /// <summary>
        /// Formats the value of the current instance using the optionally specified format. 
        /// </summary>
        public abstract string ToString(IFormatProvider formatProvider);

        public static explicit operator string (LocalizableString localizableResource)
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

        public abstract override int GetHashCode();
        public abstract bool Equals(LocalizableString other);

        public override bool Equals(object other)
        {
            return Equals(other as LocalizableString);
        }

        private sealed class FixedLocalizableString : LocalizableString
        {
            private readonly string _fixedString;

            public FixedLocalizableString(string fixedResource)
            {
                _fixedString = fixedResource;
            }

            public override string ToString(IFormatProvider formatProvider)
            {
                return _fixedString;
            }

            public override bool Equals(LocalizableString other)
            {
                var fixedStr = other as FixedLocalizableString;
                return fixedStr != null && string.Equals(_fixedString, fixedStr.ToString());
            }

            public override int GetHashCode()
            {
                return _fixedString == null ? 0 : _fixedString.GetHashCode();
            }
        }
    }
}
