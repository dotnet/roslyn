// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Information decoded from early well-known custom attributes applied on an event.
    /// </summary>
    internal class CommonEventEarlyWellKnownAttributeData : EarlyWellKnownAttributeData
    {
        #region ObsoleteAttribute
        private ObsoleteAttributeData _obsoleteAttributeData = ObsoleteAttributeData.Uninitialized;
        public ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                VerifySealed(expected: true);
                return _obsoleteAttributeData.IsUninitialized ? null : _obsoleteAttributeData;
            }
            set
            {
                VerifySealed(expected: false);
                Debug.Assert(value != null);
                Debug.Assert(!value.IsUninitialized);

                _obsoleteAttributeData = value;
                SetDataStored();
            }
        }
        #endregion
    }
}
