// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis
{
    public abstract partial class LocalizableString
    {
        private sealed class ExceptionSafeLocalizableString : LocalizableString
        {
            private readonly LocalizableString _innerLocalizableString;
            private readonly Action<Exception> _onException;

            public ExceptionSafeLocalizableString(LocalizableString innerLocalizableString, Action<Exception> onException = null)
            {
                _innerLocalizableString = innerLocalizableString;
                _onException = onException;
            }

            public override bool Equals(LocalizableString other)
            {
                var otherExceptionSafe = other as ExceptionSafeLocalizableString;
                if (otherExceptionSafe != null)
                {
                    other = otherExceptionSafe._innerLocalizableString;
                }

                return ExecuteAndCatchIfThrows(_innerLocalizableString.Equals, other, false, _onException);
            }

            public override int GetHashCode()
            {
                return ExecuteAndCatchIfThrows(_innerLocalizableString.GetHashCode, 0, _onException);
            }

            public override string ToString(IFormatProvider formatProvider)
            {
                return ExecuteAndCatchIfThrows(_innerLocalizableString.ToString, formatProvider, string.Empty, _onException);
            }

            internal override LocalizableString WithOnException(Action<Exception> onException)
            {
                if (onException == _onException)
                {
                    return this;
                }

                return new ExceptionSafeLocalizableString(_innerLocalizableString, onException);
            }
        }
    }
}
