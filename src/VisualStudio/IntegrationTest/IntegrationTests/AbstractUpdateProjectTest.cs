// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests
{
    public abstract class AbstractUpdateProjectTest : AbstractIntegrationTest
    {
        protected AbstractUpdateProjectTest(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
        {
        }

        protected void VerifyPropertyOutsideConfiguration(XElement projectElement, string name, string value)
        {
            Assert.True(projectElement.Elements()
                .Where(e => e.Name.LocalName == "PropertyGroup" && !e.Attributes().Any(a => a.Name.LocalName == "Condition"))
                .Any(g => g.Elements().SingleOrDefault(e => e.Name.LocalName == name)?.Value == value));
        }

        protected void VerifyPropertyInEachConfiguration(XElement projectElement, string name, string value)
        {
            Assert.True(projectElement.Elements()
                .Where(e => e.Name.LocalName == "PropertyGroup" && e.Attributes().Any(a => a.Name.LocalName == "Condition"))
                .All(g => g.Elements().SingleOrDefault(e => e.Name.LocalName == name)?.Value == value));
        }
    }
}
