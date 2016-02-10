// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Base class for storing information decoded from well-known custom attributes.
    /// </summary>
    internal abstract class WellKnownAttributeData
    {
        /// <summary>
        /// Used to distinguish cases when attribute is applied with null value and when attribute is not applied.
        /// For some well-known attributes, the latter case will return string stored in <see cref="StringMissingValue"/>
        /// field.
        /// </summary>
        public static readonly string StringMissingValue = nameof(StringMissingValue);

#if DEBUG
        private bool _isSealed;
        private bool _anyDataStored;
#endif

        public WellKnownAttributeData()
        {
#if DEBUG
            _isSealed = false;
            _anyDataStored = false;
#endif
        }

        [Conditional("DEBUG")]
        protected void VerifySealed(bool expected = true)
        {
#if DEBUG
            Debug.Assert(_isSealed == expected);
#endif
        }

        [Conditional("DEBUG")]
        internal void VerifyDataStored(bool expected = true)
        {
#if DEBUG
            Debug.Assert(_anyDataStored == expected);
#endif
        }

        [Conditional("DEBUG")]
        protected void SetDataStored()
        {
#if DEBUG
            _anyDataStored = true;
#endif
        }

        [Conditional("DEBUG")]
        internal static void Seal(WellKnownAttributeData data)
        {
#if DEBUG
            if (data != null)
            {
                Debug.Assert(!data._isSealed);
                Debug.Assert(data._anyDataStored);
                data._isSealed = true;
            }
#endif
        }
    }
}
