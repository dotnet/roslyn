// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.CodeAnalysis.MSBuild
{
    internal sealed partial class ProjectBlock
    {
        private Guid projectTypeGuid;
        private readonly string projectName;
        private readonly string projectPath;
        private Guid projectGuid;
        private readonly IEnumerable<SectionBlock> projectSections;

        public ProjectBlock(Guid projectTypeGuid, string projectName, string projectPath, Guid projectGuid, IEnumerable<SectionBlock> projectSections)
        {
            if (string.IsNullOrEmpty(projectName))
            {
                throw new ArgumentException(string.Format(WorkspacesResources.StringIsNullOrEmpty, "projectName"));
            }

            if (string.IsNullOrEmpty(projectPath))
            {
                throw new ArgumentException(string.Format(WorkspacesResources.StringIsNullOrEmpty, "projectPath"));
            }

            this.projectTypeGuid = projectTypeGuid;
            this.projectName = projectName;
            this.projectPath = projectPath;
            this.projectGuid = projectGuid;
            this.projectSections = projectSections.ToList().AsReadOnly();
        }

        public Guid ProjectTypeGuid
        {
            get { return projectTypeGuid; }
        }

        public string ProjectName
        {
            get { return projectName; }
        }

        public string ProjectPath
        {
            get { return projectPath; }
        }

        public Guid ProjectGuid
        {
            get { return projectGuid; }
        }

        public IEnumerable<SectionBlock> ProjectSections
        {
            get { return projectSections; }
        }

        internal string GetText()
        {
            var builder = new StringBuilder();

            builder.AppendFormat("Project(\"{0}\") = \"{1}\", \"{2}\", \"{3}\"", ProjectTypeGuid.ToString("B").ToUpper(), ProjectName, ProjectPath, ProjectGuid.ToString("B").ToUpper());
            builder.AppendLine();

            foreach (var block in projectSections)
            {
                builder.Append(block.GetText(indent: 1));
            }

            builder.AppendLine("EndProject");

            return builder.ToString();
        }

        internal static ProjectBlock Parse(TextReader reader)
        {
            var startLine = reader.ReadLine().TrimStart(null);
            var scanner = new LineScanner(startLine);

            if (scanner.ReadUpToAndEat("(\"") != "Project")
            {
                throw new Exception(string.Format(WorkspacesResources.InvalidProjectBlockInSolutionFile4, "Project"));
            }

            var projectTypeGuid = Guid.Parse(scanner.ReadUpToAndEat("\")"));

            // Read chars upto next quote, must contain "=" with optional leading/trailing whitespaces.
            if (scanner.ReadUpToAndEat("\"").Trim() != "=")
            {
                throw new Exception(WorkspacesResources.InvalidProjectBlockInSolutionFile);
            }

            var projectName = scanner.ReadUpToAndEat("\"");

            // Read chars upto next quote, must contain "," with optional leading/trailing whitespaces.
            if (scanner.ReadUpToAndEat("\"").Trim() != ",")
            {
                throw new Exception(WorkspacesResources.InvalidProjectBlockInSolutionFile2);
            }

            var projectPath = scanner.ReadUpToAndEat("\"");

            // Read chars upto next quote, must contain "," with optional leading/trailing whitespaces.
            if (scanner.ReadUpToAndEat("\"").Trim() != ",")
            {
                throw new Exception(WorkspacesResources.InvalidProjectBlockInSolutionFile3);
            }

            var projectGuid = Guid.Parse(scanner.ReadUpToAndEat("\""));

            var projectSections = new List<SectionBlock>();

            while (char.IsWhiteSpace((char)reader.Peek()))
            {
                projectSections.Add(SectionBlock.Parse(reader));
            }

            // Expect to see "EndProject" but be tolerant with missing tags as in Dev12. 
            // Instead, we may see either P' for "Project" or 'G' for "Global", which will be handled next.
            if (reader.Peek() != 'P' && reader.Peek() != 'G')
            {
                if (reader.ReadLine() != "EndProject")
                {
                    throw new Exception(string.Format(WorkspacesResources.InvalidProjectBlockInSolutionFile4, "EndProject"));
                }
            }

            return new ProjectBlock(projectTypeGuid, projectName, projectPath, projectGuid, projectSections);
        }
    }
}