// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public static readonly string StringMissingValue = "StringMissingValue";

#if DEBUG
        private bool isSealed;
        private bool anyDataStored;
#endif

        public WellKnownAttributeData()
        {
#if DEBUG
            this.isSealed = false;
            this.anyDataStored = false;
#endif
        }

        [Conditional("DEBUG")]
        protected void VerifySealed(bool expected = true)
        {
#if DEBUG
            Debug.Assert(isSealed == expected);
#endif
        }

        [Conditional("DEBUG")]
        internal void VerifyDataStored(bool expected = true)
        {
#if DEBUG
            Debug.Assert(anyDataStored == expected);
#endif
        }

        [Conditional("DEBUG")]
        protected void SetDataStored()
        {
#if DEBUG
            anyDataStored = true;
#endif
        }

        [Conditional("DEBUG")]
        internal static void Seal(WellKnownAttributeData data)
        {
#if DEBUG
            if (data != null)
            {
                Debug.Assert(!data.isSealed);
                Debug.Assert(data.anyDataStored);
                data.isSealed = true;
            }
#endif
        }
    }
}