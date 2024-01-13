// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests
{
    public abstract class AbstractUpgradeProjectTest : AbstractIntegrationTest
    {
        protected async Task<XElement> GetProjectFileElementAsync(string projectName, CancellationToken cancellationToken)
        {
            // Save the project file.
            await TestServices.SolutionExplorer.SaveAllAsync(cancellationToken);

            var projectFileContent = await TestServices.SolutionExplorer.GetFileContentsAsync(projectName, $"{ProjectName}.csproj", cancellationToken);
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

        private static string? GetPropertyValue(XElement propertyGroup, string propertyName)
            => propertyGroup.Elements().SingleOrDefault(e => e.Name.LocalName == propertyName)?.Value;
    }
}
