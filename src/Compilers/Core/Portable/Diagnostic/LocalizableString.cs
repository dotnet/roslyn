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
        /// If the exception handler itself throwns an exception, that exception is ignored.
        /// </summary>
        public event EventHandler<Exception> OnException;

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

        public abstract override int GetHashCode();
        public abstract bool Equals(LocalizableString other);

        public override bool Equals(object other)
        {
            return Equals(other as LocalizableString);
        }

        internal LocalizableString MakeExceptionSafe()
        {
            if (this is FixedLocalizableString || this is LocalizableResourceString)
            {
                // These are already sealed types which have exception safe implementations.
                return this;
            }

            // Wrap the localizableString within an ExceptionSafeLocalizableString.
            return new ExceptionSafeLocalizableString(this);
        }

        internal T ExecuteAndCatchIfThrows<T>(Func<T> action, T defaulValueOnException)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                RaiseOnException(ex);
                return defaulValueOnException;
            }
        }

        internal T ExecuteAndCatchIfThrows<U, T>(Func<U, T> action, U argument, T defaulValueOnException)
        {
            try
            {
                return action(argument);
            }
            catch (Exception ex)
            {
                RaiseOnException(ex);
                return defaulValueOnException;
            }
        }

        private void RaiseOnException(Exception ex)
        {
            try
            {
                OnException?.Invoke(this, ex);
            }
            catch
            {
                // Ignore exceptions from the exception handlers themselves.
            }
        }
    }
}
