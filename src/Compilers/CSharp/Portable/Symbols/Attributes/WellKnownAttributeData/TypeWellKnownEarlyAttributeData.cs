// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Information decoded early from well-known custom attributes applied on a type.
    /// </summary>
    internal sealed class TypeEarlyWellKnownAttributeData : CommonTypeEarlyWellKnownAttributeData
    {
        #region InterpolatedStringHandlerAttribute
        private bool _hasInterpolatedStringHandlerAttribute;
        public bool HasInterpolatedStringHandlerAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasInterpolatedStringHandlerAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasInterpolatedStringHandlerAttribute = value;
                SetDataStored();
            }
        }
        #endregion
    }
}
