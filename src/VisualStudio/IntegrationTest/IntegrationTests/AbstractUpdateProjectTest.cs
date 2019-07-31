// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Xunit;
using Xunit.Abstractions;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests
{
    public abstract class AbstractUpdateProjectTest : AbstractIntegrationTest
    {
        protected AbstractUpdateProjectTest(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper)
        {
        }

        protected XElement GetProjectFileElement(ProjectUtils.Project project)
        {
            // Save the project file.
            VisualStudio.SolutionExplorer.SaveAll();

            var projectFileContent = VisualStudio.SolutionExplorer.GetFileContents(project, Path.GetFileName(project.RelativePath));
            return XElement.Parse(projectFileContent);
        }

        protected static void VerifyPropertyOutsideConfiguration(XElement projectElement, string name, string value)
        {
            Assert.Contains(
                projectElement.Elements().Where(IsUnconditionalPropertyGroup),
                group => GetPropertyValue(group, name) == value);

            static bool IsUnconditionalPropertyGroup(XElement element)
                => element.Name.LocalName == "PropertyGroup" && !element.Attributes().Any(a => a.Name.LocalName == "Condition");
        }

        protected static void VerifyPropertyInEachConfiguration(XElement projectElement, string name, string value)
        {
            Assert.All(
                projectElement.Elements().Where(IsConditionalPropertyGroup),
                group => Assert.Equal(value, GetPropertyValue(group, name)));

            static bool IsConditionalPropertyGroup(XElement element)
                => element.Name.LocalName == "PropertyGroup" && element.Attributes().Any(a => a.Name.LocalName == "Condition");
        }

        private static string GetPropertyValue(XElement propertyGroup, string propertyName)
            => propertyGroup.Elements().SingleOrDefault(e => e.Name.LocalName == propertyName)?.Value;
    }
}
