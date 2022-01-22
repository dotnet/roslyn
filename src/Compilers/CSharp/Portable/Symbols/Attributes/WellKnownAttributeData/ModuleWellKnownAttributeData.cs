// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Information decoded from well-known custom attributes applied on a module.
    /// </summary>
    internal sealed class ModuleWellKnownAttributeData : CommonModuleWellKnownAttributeData, ISkipLocalsInitAttributeTarget
    {
        #region SkipLocalsInitAttribute
        private bool _hasSkipLocalsInitAttribute;
        public bool HasSkipLocalsInitAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasSkipLocalsInitAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasSkipLocalsInitAttribute = value;
                SetDataStored();
            }
        }
        #endregion
    }
}
