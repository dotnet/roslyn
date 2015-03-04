// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis
{
    public abstract partial class LocalizableString
    {
        private sealed class ExceptionSafeLocalizableString : LocalizableString
        {
            private readonly LocalizableString _innerLocalizableString;
            
            public ExceptionSafeLocalizableString(LocalizableString innerLocalizableString)
            {
                _innerLocalizableString = innerLocalizableString;
            }

            public override bool Equals(LocalizableString other)
            {
                var otherExceptionSafe = other as ExceptionSafeLocalizableString;
                if (otherExceptionSafe != null)
                {
                    other = otherExceptionSafe._innerLocalizableString;
                }

                return ExecuteAndCatchIfThrows(_innerLocalizableString.Equals, other, false);
            }

            public override int GetHashCode()
            {
                return ExecuteAndCatchIfThrows(_innerLocalizableString.GetHashCode, 0);
            }

            public override string ToString(IFormatProvider formatProvider)
            {
                return ExecuteAndCatchIfThrows(_innerLocalizableString.ToString, formatProvider, string.Empty);
            }
        }
    }
}
