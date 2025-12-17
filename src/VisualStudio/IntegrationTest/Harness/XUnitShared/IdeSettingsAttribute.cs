// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            MaxAttempts = 0;
            EnvironmentVariables = new string[0];
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

        public int MaxAttempts
        {
            get;
            set;
        }

        public string[] EnvironmentVariables
        {
            get;
            set;
        }
    }
}
