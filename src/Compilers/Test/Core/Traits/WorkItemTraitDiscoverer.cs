// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.CodeAnalysis.Test.Utilities;

public sealed class WorkItemTraitDiscoverer : ITraitDiscoverer
{
    public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
    {
        var arguments = traitAttribute.GetConstructorArguments().ToArray();
        if (arguments is [int id, string url])
        {
            yield return new KeyValuePair<string, string>("WorkItemId", id.ToString());
            yield return new KeyValuePair<string, string>("WorkItemUrl", url);
        }
        else if (arguments is [string onlyUrl])
        {
            yield return new KeyValuePair<string, string>("WorkItemUrl", onlyUrl);
        }
    }
}
