// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A string that may possibly be formatted differently depending on culture.
    /// NOTE: Types implementing <see cref="LocalizableString"/> must be serializable.
    /// </summary>
    public abstract partial class LocalizableString : IFormattable, IEquatable<LocalizableString>
    {
        /// <summary>
        /// Fired when an exception is raised by any of the public methods of <see cref="LocalizableString"/>.
        /// If the exception handler itself throws an exception, that exception is ignored.
        /// </summary>
        public event EventHandler<Exception> OnException;

        /// <summary>
        /// Formats the value of the current instance using the optionally specified format. 
        /// </summary>
        public string ToString(IFormatProvider formatProvider)
        {
            try
            {
                return GetText(formatProvider);
            }
            catch (Exception ex)
            {
                RaiseOnException(ex);
                return string.Empty;
            }
        }

        public static explicit operator string(LocalizableString localizableResource)
        {
            return localizableResource.ToString(null);
        }

        public static implicit operator LocalizableString(string fixedResource)
        {
            return FixedLocalizableString.Create(fixedResource);
        }

        public sealed override string ToString()
        {
            return ToString(null);
        }

        string IFormattable.ToString(string ignored, IFormatProvider formatProvider)
        {
            return ToString(formatProvider);
        }

        public sealed override int GetHashCode()
        {
            try
            {
                return GetHash();
            }
            catch (Exception ex)
            {
                RaiseOnException(ex);
                return 0;
            }
        }

        public sealed override bool Equals(object other)
        {
            try
            {
                return AreEqual(other);
            }
            catch (Exception ex)
            {
                RaiseOnException(ex);
                return false;
            }
        }

        public bool Equals(LocalizableString other)
        {
            return Equals((object)other);
        }

        /// <summary>
        /// Formats the value of the current instance using the optionally specified format.
        /// Provides the implementation of ToString. ToString will provide a default value
        /// if this method throws an exception.
        /// </summary>
        protected abstract string GetText(IFormatProvider formatProvider);

        /// <summary>
        /// Provides the implementation of GetHashCode. GetHashCode will provide a default value
        /// if this method throws an exception.
        /// </summary>
        /// <returns></returns>
        protected abstract int GetHash();

        /// <summary>
        /// Provides the implementation of Equals. Equals will provide a default value
        /// if this method throws an exception.
        /// </summary>
        /// <returns></returns>
        protected abstract bool AreEqual(object other);

        private void RaiseOnException(Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                return;
            }

            try
            {
                OnException?.Invoke(this, ex);
            }
            catch
            {
                // Ignore exceptions from the exception handlers themselves.
            }
        }

        /// <summary>
        /// Flag indicating if any methods on this type can throw exceptions from public entrypoints.
        /// </summary>
        internal virtual bool CanThrowExceptions => true;
    }
}
