﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public abstract class AbstractTestHostProject
    {
        public abstract string Language { get; }
        public abstract ProjectId Id { get; }
        public abstract HostLanguageServices LanguageServiceProvider { get; }
        public abstract string AssemblyName { get; }
        public abstract string Name { get; }
    }
}
