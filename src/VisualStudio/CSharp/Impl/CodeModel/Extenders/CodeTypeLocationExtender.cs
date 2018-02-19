// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel.Extenders
{
    [ComVisible(true)]
    [ComDefaultInterface(typeof(ICSCodeTypeLocation))]
    public class CodeTypeLocationExtender : ICSCodeTypeLocation
    {
        internal static ICSCodeTypeLocation Create(string externalLocation)
        {
            var result = new CodeTypeLocationExtender(externalLocation);
            return (ICSCodeTypeLocation)ComAggregate.CreateAggregatedObject(result);
        }

        private readonly string _externalLocation;

        private CodeTypeLocationExtender(string externalLocation)
        {
            _externalLocation = externalLocation;
        }

        public string ExternalLocation
        {
            get
            {
                return _externalLocation;
            }
        }
    }
}
