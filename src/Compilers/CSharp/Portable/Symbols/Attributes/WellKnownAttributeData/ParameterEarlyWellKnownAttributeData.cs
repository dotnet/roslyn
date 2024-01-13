// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Information early-decoded from well-known custom attributes applied on a parameter.
    /// </summary>
    internal sealed class ParameterEarlyWellKnownAttributeData : CommonParameterEarlyWellKnownAttributeData
    {
        private bool _hasUnscopedRefAttribute;
        public bool HasUnscopedRefAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasUnscopedRefAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasUnscopedRefAttribute = value;
                SetDataStored();
            }
        }
    }
}
