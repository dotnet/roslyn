// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace BuildBoss
{
    internal class ProjectUtil
    {
        internal ProjectKey Key { get; }
        internal XDocument Document { get; }
        internal XmlNamespaceManager Manager { get; }
        internal XNamespace Namespace { get; }
        internal string OutputType { get; }

        internal bool IsTestProject => IsUnitTestProject || IsIntegrationTestProject;
        internal bool IsUnitTestProject => Path.GetFileNameWithoutExtension(Key.FilePath).EndsWith(".UnitTests");
        internal bool IsIntegrationTestProject => Path.GetFileNameWithoutExtension(Key.FilePath).EndsWith(".IntegrationTests");

        internal ProjectUtil(string filePath)
            : this(new ProjectKey(filePath), XDocument.Load(filePath))
        {
        }

        internal ProjectUtil(ProjectKey key, XDocument document)
        {
            Key = key;
            Document = document;
            Namespace = document.Root.Name.Namespace;
            Manager = new XmlNamespaceManager(new NameTable());
            Manager.AddNamespace("mb", Namespace == XNamespace.None ? "" : SharedUtil.MSBuildNamespaceUriRaw);

            OutputType = FindSingleProperty("OutputType")?.Value.Trim().ToLowerInvariant();
        }

        internal XElement GetTargetFramework() => Document.XPathSelectElements("//mb:TargetFramework", Manager).FirstOrDefault();

        internal XElement GetTargetFrameworks() => Document.XPathSelectElements("//mb:TargetFrameworks", Manager).FirstOrDefault();

        public bool IsNewSdk()
        {
            if (GetTargetFramework() != null || GetTargetFrameworks() != null)
            {
                return true;
            }

            // If a project has a 'Project' element with an 'Sdk' attribute, then it's an SDK-style project.
            // https://github.com/dotnet/project-system/blob/main/docs/opening-with-new-project-system.md#sdks
            var hasProjectWithSdkAttribute = Document.XPathSelectElements("//mb:Project", Manager).FirstOrDefault()?.Attribute("Sdk") != null;
            return hasProjectWithSdkAttribute;
        }

        internal IEnumerable<string> GetAllTargetFrameworks()
        {
            var targetFramework = GetTargetFramework();
            if (targetFramework != null)
            {
                return new[] { targetFramework.Value.ToString() };
            }

            var targetFrameworks = GetTargetFrameworks();
            if (targetFrameworks != null)
            {
                var all = targetFrameworks.Value.ToString().Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                return all;
            }

            throw new InvalidOperationException($"Project {Key.FilePath} does not have a TargetFramework(s) element.");
        }

        internal IEnumerable<XElement> GetAllPropertyGroupElements()
        {
            var groups = Document.XPathSelectElements("//mb:PropertyGroup", Manager);
            foreach (var group in groups)
            {
                foreach (var element in group.Elements())
                {
                    yield return element;
                }
            }
        }

        internal IEnumerable<XElement> GetTargets()
        {
            return Document.XPathSelectElements("//mb:Target", Manager);
        }

        internal IEnumerable<XElement> GetImports()
        {
            return Document.XPathSelectElements("//mb:Import", Manager);
        }

        internal IEnumerable<string> GetImportProjects()
        {
            return GetImports()
                .Select(x => x.Attribute("Project")?.Value)
                .Where(x => !string.IsNullOrEmpty(x));
        }

        internal List<ProjectReferenceEntry> GetDeclaredProjectReferences()
        {
            var references = Document.XPathSelectElements("//mb:ItemGroup/mb:ProjectReference", Manager);
            var list = new List<ProjectReferenceEntry>();
            var directory = Path.GetDirectoryName(Key.FilePath);
            foreach (var r in references)
            {
                // Make sure to check for references that exist only for ordering purposes.  They don't count as 
                // actual references.
                var referenceOutputAssemblyValue = r.Element(Namespace.GetName("ReferenceOutputAssembly"))?.Value ?? r.Attribute(XName.Get("ReferenceOutputAssembly"))?.Value;
                if (referenceOutputAssemblyValue != null)
                {
                    if (bool.TryParse(referenceOutputAssemblyValue.Trim().ToLower(), out var isRealReference) && !isRealReference)
                    {
                        continue;
                    }
                }

                Guid? project = null;
                var projectElement = r.Element(Namespace.GetName("Project"));
                if (projectElement != null)
                {
                    project = Guid.Parse(projectElement.Value.Trim());
                }

                var relativePath = r.Attribute("Include").Value;
                var path = Path.Combine(directory, relativePath);
                list.Add(new ProjectReferenceEntry(path, project));
            }

            return list;
        }

        internal List<InternalsVisibleTo> GetInternalsVisibleTo()
        {
            var list = new List<InternalsVisibleTo>();
            foreach (var ivt in Document.XPathSelectElements("//mb:InternalsVisibleTo", Manager))
            {
                list.Add(GetInternalsVisibleTo(ivt));
            }

            return list;
        }

        internal InternalsVisibleTo GetInternalsVisibleTo(XElement element)
        {
            var targetAssembly = element.Attribute("Include")?.Value.Trim();
            var key = element.Attribute("Key")?.Value.Trim();
            var loadsWithinVisualStudio = element.Attribute("LoadsWithinVisualStudio")?.Value.Trim();
            var workItem = element.Attribute("WorkItem")?.Value.Trim();
            return new InternalsVisibleTo(targetAssembly, key, loadsWithinVisualStudio, workItem);
        }

        internal XElement FindSingleProperty(string localName) => GetAllPropertyGroupElements().SingleOrDefault(x => x.Name.LocalName == localName);
    }
}
