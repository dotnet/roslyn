// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic
{
    [Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
    public class BasicSquigglesDesktop : BasicSquigglesCommon
    {
        public BasicSquigglesDesktop()
            : base(WellKnownProjectTemplates.ClassLibrary)
        {
        }
    }
}
