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
    [ComDefaultInterface(typeof(ICSPartialMethodExtender))]
    public class PartialMethodExtender : ICSPartialMethodExtender
    {
        internal static ICSPartialMethodExtender Create(bool isPartial, bool isDeclaration, bool hasOtherPart)
        {
            var result = new PartialMethodExtender(isPartial, isDeclaration, hasOtherPart);
            return (ICSPartialMethodExtender)ComAggregate.CreateAggregatedObject(result);
        }

        private readonly bool _hasOtherPart;

        private PartialMethodExtender(bool isPartial, bool isDeclaration, bool hasOtherPart)
        {
            IsPartial = isPartial;
            IsDeclaration = isDeclaration;
            _hasOtherPart = hasOtherPart;
        }

        public bool IsPartial { get; }

        public bool IsDeclaration { get; }

        public bool HasOtherPart
        {
            get { return _hasOtherPart; }
        }
    }
}
