﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel.Extenders
{
    [ComVisible(true)]
    [ComDefaultInterface(typeof(ICSAutoImplementedPropertyExtender))]
    public class AutoImplementedPropertyExtender : ICSAutoImplementedPropertyExtender
    {
        internal static ICSAutoImplementedPropertyExtender Create(bool isAutoImplemented)
        {
            var result = new AutoImplementedPropertyExtender(isAutoImplemented);
            return (ICSAutoImplementedPropertyExtender)ComAggregate.CreateAggregatedObject(result);
        }

        private readonly bool _isAutoImplemented;

        private AutoImplementedPropertyExtender(bool isAutoImplemented)
            => _isAutoImplemented = isAutoImplemented;

        public bool IsAutoImplemented
        {
            get { return _isAutoImplemented; }
        }
    }
}
