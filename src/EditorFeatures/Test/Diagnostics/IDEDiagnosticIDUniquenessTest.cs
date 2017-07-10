// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    public class IDEDiagnosticIDUniquenessTest
    {
        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void UniqueIDEDiagnosticIds()
        {
            Type type = typeof(IDEDiagnosticIds);
            List<string> listOfIDEDiagnosticIds = type.GetFields().Select(x => x.GetValue(null).ToString()).ToList();
            Assert.Equal(listOfIDEDiagnosticIds.Count, listOfIDEDiagnosticIds.Distinct().Count());
        }
    }
}
