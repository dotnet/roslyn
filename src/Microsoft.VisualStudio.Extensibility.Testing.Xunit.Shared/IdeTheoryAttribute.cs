// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit
{
    using System;
    using Xunit.Sdk;

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [XunitTestCaseDiscoverer("Xunit.Threading.IdeTheoryDiscoverer", "Microsoft.VisualStudio.Extensibility.Testing.Xunit")]
    public class IdeTheoryAttribute : TheoryAttribute, IIdeSettingsAttribute
    {
        public IdeTheoryAttribute()
        {
            MinVersion = VisualStudioVersion.Unspecified;
            MaxVersion = VisualStudioVersion.Unspecified;
            RootSuffix = null;
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

        public string? RootSuffix
        {
            get;
            set;
        }
    }
}
