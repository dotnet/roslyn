// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit.Sdk;

namespace Microsoft.VisualStudio
{
    /// <summary>
    ///     Indicates that a test is a project system unit test.
    /// </summary>
    [TraitDiscoverer("Microsoft.VisualStudio.Testing.ProjectSystemTraitDiscoverer", "Microsoft.VisualStudio.ProjectSystem.Managed.UnitTests")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class UnitTestTraitAttribute : Attribute, ITraitAttribute
    {
        public UnitTestTraitAttribute()
        {
        }
    }
}
