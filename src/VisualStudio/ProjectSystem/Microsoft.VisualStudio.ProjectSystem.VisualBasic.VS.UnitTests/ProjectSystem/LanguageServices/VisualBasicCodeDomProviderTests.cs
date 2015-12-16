// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    [ProjectSystemTrait]
    public class VisualBasicCodeDomProviderTests
    {
        [Fact]
        public void Constructor_DoesNotThrow()
        {
            new VisualBasicCodeDomProvider();
        }

        [Fact]
        public void UnconfiguredProject_CanGetSet()
        {
            var unconfiguedProject = IUnconfiguredProjectFactory.Create();
            var provider = CreateInstance();

            provider.UnconfiguredProject = unconfiguedProject;

            Assert.Same(unconfiguedProject, provider.UnconfiguredProject);
        }

        private static VisualBasicCodeDomProvider CreateInstance()
        {
            return new VisualBasicCodeDomProvider();
        }
    }
}
