// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

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

        #region InlineArrayAttribute

        private int _inlineArrayLength;
        public int InlineArrayLength
        {
            get
            {
                VerifySealed(expected: true);
                return _inlineArrayLength;
            }
            set
            {
                VerifySealed(expected: false);

                Debug.Assert(value is -1 or > 0);
                if (_inlineArrayLength == 0)
                {
                    _inlineArrayLength = value;
                }

                SetDataStored();
            }
        }

        #endregion

    }
}
