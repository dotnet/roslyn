// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Threading
{
    using System;
    using Xunit.Harness;
    using Xunit.Sdk;

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [XunitTestCaseDiscoverer("Xunit.Threading.IdeFactDiscoverer", "Microsoft.VisualStudio.Extensibility.Testing.Xunit")]
    public class IdeFactAttribute : FactAttribute
    {
        public IdeFactAttribute()
        {
            MinVersion = VisualStudioVersion.VS2012;
            MaxVersion = VisualStudioVersion.VS2017;
        }

        public VisualStudioVersion MinVersion
        {
            get;
            set;
        }

        public VisualStudioVersion MaxVersion
        {
            get;
            set;
        }
    }
}
