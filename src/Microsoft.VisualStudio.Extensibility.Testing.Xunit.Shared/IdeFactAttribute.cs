// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit
{
    using System;
    using Xunit.Sdk;

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [XunitTestCaseDiscoverer("Xunit.Threading.IdeFactDiscoverer", "Microsoft.VisualStudio.Extensibility.Testing.Xunit")]
    public class IdeFactAttribute : FactAttribute, IIdeSettingsAttribute
    {
        public IdeFactAttribute()
        {
            MinVersion = VisualStudioVersion.Unspecified;
            MaxVersion = VisualStudioVersion.Unspecified;
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
