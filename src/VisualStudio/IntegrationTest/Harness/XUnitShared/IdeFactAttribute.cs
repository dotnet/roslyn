// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit
{
    using System;
    using System.Runtime.CompilerServices;
    using Xunit.v3;

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [XunitTestCaseDiscoverer(typeof(Threading.IdeFactDiscoverer))]
    public class IdeFactAttribute : FactAttribute, IIdeSettingsAttribute
    {
        public IdeFactAttribute(
            [CallerFilePath] string? sourceFilePath = null,
            [CallerLineNumber] int sourceLineNumber = -1)
            : base(sourceFilePath, sourceLineNumber)
        {
            MinVersion = VisualStudioVersion.Unspecified;
            MaxVersion = VisualStudioVersion.Unspecified;
            RootSuffix = null;
            MaxAttempts = 0;
            EnvironmentVariables = Array.Empty<string>();
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
