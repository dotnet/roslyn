// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable disable

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
