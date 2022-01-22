// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel.Extenders
{
    [ComVisible(true)]
    [ComDefaultInterface(typeof(ICSExtensionMethodExtender))]
    public class ExtensionMethodExtender : ICSExtensionMethodExtender
    {
        internal static ICSExtensionMethodExtender Create(bool isExtension)
        {
            var result = new ExtensionMethodExtender(isExtension);
            return (ICSExtensionMethodExtender)ComAggregate.CreateAggregatedObject(result);
        }

        private readonly bool _isExtension;

        private ExtensionMethodExtender(bool isExtension)
            => _isExtension = isExtension;

        public bool IsExtension
        {
            get { return _isExtension; }
        }
    }
}
