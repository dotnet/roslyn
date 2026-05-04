// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.Harness
{
    using System;

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class RequireExtensionAttribute : Attribute
    {
        public RequireExtensionAttribute(string extensionFile)
        {
            ExtensionFile = extensionFile;
        }

        public string ExtensionFile
        {
            get;
        }
    }
}
