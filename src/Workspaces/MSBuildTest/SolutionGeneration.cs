// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.UnitTests.TestFiles;

namespace Microsoft.CodeAnalysis.MSBuild.UnitTests
{
    /// <summary>
    /// Flexible and extensible API to generate MSBuild projects and solutions without external files or resources.
    /// </summary>
    public static class SolutionGeneration
    {
        public const string NS = "http://schemas.microsoft.com/developer/msbuild/2003";

        private const string CSharpProjectTemplate =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""12.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Import Project=""$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"" Condition=""Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')"" />
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">
  </PropertyGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>";

        private const string SolutionTemplate =
@"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 2013
VisualStudioVersion = 12.0.30110.0
MinimumVisualStudioVersion = 10.0.40219.1
{0}Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
EndGlobal
";

        public const string PublicKey = "00240000048000009400000006020000002400005253413100040000010001003bb5de1b79bee9bf5ba44bdb42974c6f40fdc4b329c8e1b833fa798cf0859529485b2bfc359a08e16f025fe57efd293c4dc3541cb2e0929b1c4a92db87eed7a9454dbd08beb7c7308941384b3bfb088de781b51caef23677f8f6defb671e97e1fc5e0979858e52828c86aca1d4ea1797f1f1254bf64073a28e5be520d5397fb0";
        public const string PublicKeyToken = "39d7e8ec38707fde";
        public static readonly byte[] KeySnk = Resources.Key_snk;

        public static IEnumerable<(string fileName, object fileContent)> GetSolutionFiles(params IBuilder[] inputs)
        {
            var list = new List<(string, object)>();
            var projectBuilders = inputs.OfType<ProjectBuilder>();
            var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var fileIndex = 1;
            var projectIndex = 1;

            // first make sure all projects have names, as a separate loop
            foreach (var project in projectBuilders)
            {
                if (project.Name == null)
                {
                    project.Name = "Project" + projectIndex;
                    projectIndex++;
                }
            }

            foreach (var project in projectBuilders)
            {
                foreach (var document in project.Documents)
                {
                    if (document.FilePath == null)
                    {
                        document.FilePath = "Document" + fileIndex + (project.Language == LanguageNames.VisualBasic ? ".vb" : ".cs");
                        fileIndex++;
                    }
                }

                foreach (var projectReference in project.ProjectReferences)
                {
                    if (projectReference.Guid == Guid.Empty)
                    {
                        var referencedProject = projectBuilders.First(p => p.Name == projectReference.ProjectName);
                        projectReference.Guid = referencedProject.Guid;
                        projectReference.ProjectFileName = referencedProject.Name + referencedProject.Extension;
                    }
                }

                foreach (var (fileName, fileContent) in project.Files)
                {
                    if (files.Add(fileName + fileContent))
                    {
                        list.Add((fileName, fileContent));
                    }
                }
            }

            list.Add(("Solution.sln", GetSolutionContent(projectBuilders)));

            return list;
        }

        public static IBuilder Project(params IBuilder[] inputs)
        {
            var projectReferences = inputs.OfType<ProjectReferenceBuilder>();
            var documents = inputs.OfType<DocumentBuilder>().ToList();
            var projectName = inputs.OfType<ProjectNameBuilder>().FirstOrDefault();
            var properties = inputs.OfType<PropertyBuilder>().ToList();
            var sign = inputs.OfType<SignBuilder>();
            if (sign != null)
            {
                properties.Add((PropertyBuilder)Property("SignAssembly", "true"));
                properties.Add((PropertyBuilder)Property("AssemblyOriginatorKeyFile", "key.snk"));
                documents.Add((DocumentBuilder)Document(KeySnk, "key.snk", "None"));
            }

            return new ProjectBuilder
            {
                Name = projectName?.Name,
                Documents = documents,
                ProjectReferences = projectReferences,
                Properties = properties
            };
        }

        public static IBuilder ProjectReference(string projectName)
        {
            return new ProjectReferenceBuilder
            {
                ProjectName = projectName
            };
        }

        public static IBuilder ProjectName(string projectName)
        {
            return new ProjectNameBuilder
            {
                Name = projectName
            };
        }

        public static IBuilder Property(string propertyName, string propertyValue)
        {
            return new PropertyBuilder
            {
                Name = propertyName,
                Value = propertyValue
            };
        }

        public static IBuilder Sign
        {
            get
            {
                return new SignBuilder();
            }
        }

        public static IBuilder Document(
            object content = null,
            string filePath = null,
            string itemType = "Compile")
        {
            return new DocumentBuilder
            {
                FilePath = filePath,
                Content = content,
                ItemType = itemType
            };
        }

        private static string GetSolutionContent(IEnumerable<ProjectBuilder> projects)
        {
            var sb = new StringBuilder();
            foreach (var project in projects)
            {
                var fileName = project.Name + project.Extension;
                var languageGuid = project.Language == LanguageNames.VisualBasic ?
                    "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}" :
                    "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";
                sb.AppendLine(
                    string.Format(
                        @"Project(""{0}"") = ""{1}"", ""{2}"", ""{3}""",
                        languageGuid,
                        project.Name,
                        fileName,
                        project.Guid.ToString("B")));
                sb.AppendLine("EndProject");
            }

            return string.Format(SolutionTemplate, sb.ToString());
        }

        public interface IBuilder
        {
        }

        private class ProjectBuilder : IBuilder
        {
            public string Name { get; set; }
            public string Language { get; set; }
            public Guid Guid { get; set; }
            public string OutputType { get; set; }
            public string OutputPath { get; set; }
            public IEnumerable<DocumentBuilder> Documents { get; set; }
            public IEnumerable<ProjectReferenceBuilder> ProjectReferences { get; set; }
            public IEnumerable<PropertyBuilder> Properties { get; set; }

            public string Extension
            {
                get
                {
                    return Language == LanguageNames.VisualBasic ? ".vbproj" : ".csproj";
                }
            }

            public IEnumerable<(string fileName, object fileContent)> Files
            {
                get
                {
                    foreach (var document in Documents)
                    {
                        yield return (document.FilePath, document.Content);
                    }

                    yield return (Name + Extension, GetProjectContent());
                }
            }

            private string GetProjectContent()
            {
                if (Language == LanguageNames.VisualBasic)
                {
                    throw new NotImplementedException("Need VB support");
                }

                if (Guid == Guid.Empty)
                {
                    Guid = Guid.NewGuid();
                }

                if (string.IsNullOrEmpty(OutputType))
                {
                    OutputType = "Library";
                }

                if (string.IsNullOrEmpty(OutputPath))
                {
                    OutputPath = ".";
                }

                var document = XDocument.Parse(CSharpProjectTemplate);
                var propertyGroup = document.Root.Descendants(XName.Get("PropertyGroup", NS)).First();
                AddXElement(propertyGroup, "ProjectGuid", Guid.ToString("B"));
                AddXElement(propertyGroup, "OutputType", OutputType);
                AddXElement(propertyGroup, "OutputPath", OutputPath);
                AddXElement(propertyGroup, "AssemblyName", Name);

                if (Properties != null)
                {
                    foreach (var property in Properties)
                    {
                        AddXElement(propertyGroup, property.Name, property.Value);
                    }
                }

                var importTargets = document.Root.Elements().Last();

                if (ProjectReferences != null && ProjectReferences.Any())
                {
                    AddItemGroup(
                        importTargets,
                        _ => "ProjectReference",
                        ProjectReferences,
                        i => i.ProjectFileName,
                        (projectReference, xmlElement) =>
                        {
                            if (projectReference.Guid != Guid.Empty)
                            {
                                AddXElement(xmlElement, "Project", projectReference.Guid.ToString("B"));
                            }

                            AddXElement(xmlElement, "Name", Path.GetFileNameWithoutExtension(projectReference.ProjectName));
                        });
                }

                if (Documents != null)
                {
                    AddItemGroup(
                        importTargets,
                        i => i.ItemType,
                        Documents,
                        i => i.FilePath);
                }

                return document.ToString();
            }

            private void AddItemGroup<T>(
                XElement addBefore,
                Func<T, string> itemTypeSelector,
                IEnumerable<T> items,
                Func<T, string> attributeValueGetter,
                Action<T, XElement> elementModifier = null)
            {
                var itemGroup = CreateXElement("ItemGroup");
                addBefore.AddBeforeSelf(itemGroup);

                foreach (var item in items)
                {
                    var itemElement = CreateXElement(itemTypeSelector(item));
                    itemElement.SetAttributeValue("Include", attributeValueGetter(item));
                    elementModifier?.Invoke(item, itemElement);

                    itemGroup.Add(itemElement);
                }
            }

            private XElement CreateXElement(string name)
            {
                return new XElement(XName.Get(name, NS));
            }

            private void AddXElement(XElement element, string elementName, string elementValue)
            {
                element.Add(new XElement(XName.Get(elementName, NS), elementValue));
            }
        }

        private class ProjectReferenceBuilder : IBuilder
        {
            public string ProjectName { get; set; }
            public Guid Guid { get; set; }
            public string ProjectFileName { get; set; }
        }

        private class ProjectNameBuilder : IBuilder
        {
            public string Name { get; set; }
        }

        private class PropertyBuilder : IBuilder
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        private class SignBuilder : IBuilder { }

        private class DocumentBuilder : IBuilder
        {
            public string FilePath { get; set; }
            public object Content { get; set; }
            public string ItemType { get; set; }
        }
    }
}
