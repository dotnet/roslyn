// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit
{
    using System;

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class IdeSettingsAttribute : Attribute, IIdeSettingsAttribute
    {
        public IdeSettingsAttribute()
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

        public string RootSuffix
        {
            get;
            set;
        }
    }
}
