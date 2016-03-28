// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.VisualStudio.Testing
{
    public class ProjectSystemTraitDiscoverer : ITraitDiscoverer
    {
        public ProjectSystemTraitDiscoverer()
        {
        }

        public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
        {
            yield return new KeyValuePair<string, string>("ProjectSystem", "UnitTest");
        }
    }
}
