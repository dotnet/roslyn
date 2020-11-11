
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Information decoded from well-known custom attributes applied on a method.
    /// </summary>
    internal sealed class MethodEarlyWellKnownAttributeData : CommonMethodEarlyWellKnownAttributeData
    {
        private bool _unmanagedCallersOnlyAttributePresent;
        public bool UnmanagedCallersOnlyAttributePresent
        {
            get
            {
                VerifySealed(expected: true);
                return _unmanagedCallersOnlyAttributePresent;
            }
            set
            {
                VerifySealed(expected: false);
                _unmanagedCallersOnlyAttributePresent = value;
                SetDataStored();
            }
        }
    }
}
