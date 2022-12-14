// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.Options;

[Collection(nameof(SharedIntegrationHostFixture))]
public sealed class GlobalOptionsTest : AbstractIntegrationTest
{
    public GlobalOptionsTest(VisualStudioInstanceFactory instanceFactory)
        : base(instanceFactory)
    {
    }

    [WpfFact]
    public void ValidateAllOptions()
    {
        VisualStudio.GlobalOptions.ValidateAllOptions();
    }
}
