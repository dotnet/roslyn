// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Information decoded from well-known custom attributes applied on a property.
    /// </summary>
    internal class CommonPropertyWellKnownAttributeData : WellKnownAttributeData
    {
        #region SpecialNameAttribute
        private bool _hasSpecialNameAttribute;
        public bool HasSpecialNameAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasSpecialNameAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasSpecialNameAttribute = value;
                SetDataStored();
            }
        }
        #endregion

        #region ExcludeFromCodeCoverageAttribute

        private bool _hasExcludeFromCodeCoverageAttribute;
        public bool HasExcludeFromCodeCoverageAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasExcludeFromCodeCoverageAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasExcludeFromCodeCoverageAttribute = value;
                SetDataStored();
            }
        }

        #endregion
    }
}
