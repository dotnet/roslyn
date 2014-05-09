// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.CodeAnalysis.MSBuild
{
    internal sealed partial class SolutionFile
    {
        private readonly IEnumerable<string> headerLines;
        private readonly string visualStudioVersionLineOpt;
        private readonly string minimumVisualStudioVersionLineOpt;
        private readonly IEnumerable<ProjectBlock> projectBlocks;
        private readonly IEnumerable<SectionBlock> globalSectionBlocks;

        public SolutionFile(
            IEnumerable<string> headerLines,
            string visualStudioVersionLineOpt,
            string minimumVisualStudioVersionLineOpt,
            IEnumerable<ProjectBlock> projectBlocks,
            IEnumerable<SectionBlock> globalSectionBlocks)
        {
            if (headerLines == null)
            {
                throw new ArgumentNullException("headerLines");
            }

            if (projectBlocks == null)
            {
                throw new ArgumentNullException("projectBlocks");
            }

            if (globalSectionBlocks == null)
            {
                throw new ArgumentNullException("globalSectionBlocks");
            }

            this.headerLines = headerLines.ToList().AsReadOnly();
            this.visualStudioVersionLineOpt = visualStudioVersionLineOpt;
            this.minimumVisualStudioVersionLineOpt = minimumVisualStudioVersionLineOpt;
            this.projectBlocks = projectBlocks.ToList().AsReadOnly();
            this.globalSectionBlocks = globalSectionBlocks.ToList().AsReadOnly();
        }

        public IEnumerable<string> HeaderLines
        {
            get { return headerLines; }
        }

        public string VisualStudioVersionLineOpt
        {
            get { return visualStudioVersionLineOpt; }
        }

        public string MinimumVisualStudioVersionLineOpt
        {
            get { return minimumVisualStudioVersionLineOpt; }
        }

        public IEnumerable<ProjectBlock> ProjectBlocks
        {
            get { return projectBlocks; }
        }

        public IEnumerable<SectionBlock> GlobalSectionBlocks
        {
            get { return globalSectionBlocks; }
        }

        public string GetText()
        {
            var builder = new StringBuilder();

            builder.AppendLine();

            foreach (var headerLine in headerLines)
            {
                builder.AppendLine(headerLine);
            }

            foreach (var block in projectBlocks)
            {
                builder.Append(block.GetText());
            }

            builder.AppendLine("Global");

            foreach (var block in globalSectionBlocks)
            {
                builder.Append(block.GetText(indent: 1));
            }

            builder.AppendLine("EndGlobal");

            return builder.ToString();
        }

        public static SolutionFile Parse(TextReader reader)
        {
            var headerLines = new List<string>();

            var headerLine1 = GetNextNonEmptyLine(reader);
            if (headerLine1 == null || !headerLine1.StartsWith("Microsoft Visual Studio Solution File"))
            {
                throw new Exception(string.Format(WorkspacesResources.MissingHeaderInSolutionFile, "Microsoft Visual Studio Solution File"));
            }

            headerLines.Add(headerLine1);

            // skip comment lines and empty lines
            while (reader.Peek() != -1 && "#\r\n".Contains((char)reader.Peek()))
            {
                headerLines.Add(reader.ReadLine());
            }

            string visualStudioVersionLineOpt = null;
            if (reader.Peek() == 'V')
            {
                visualStudioVersionLineOpt = GetNextNonEmptyLine(reader);
                if (!visualStudioVersionLineOpt.StartsWith("VisualStudioVersion"))
                {
                    throw new Exception(string.Format(WorkspacesResources.MissingHeaderInSolutionFile, "VisualStudioVersion"));
                }
            }

            string minimumVisualStudioVersionLineOpt = null;
            if (reader.Peek() == 'M')
            {
                minimumVisualStudioVersionLineOpt = GetNextNonEmptyLine(reader);
                if (!minimumVisualStudioVersionLineOpt.StartsWith("MinimumVisualStudioVersion"))
                {
                    throw new Exception(string.Format(WorkspacesResources.MissingHeaderInSolutionFile, "MinimumVisualStudioVersion"));
                }
            }

            var projectBlocks = new List<ProjectBlock>();

            // Parse project blocks while we have them
            while (reader.Peek() == 'P')
            {
                projectBlocks.Add(ProjectBlock.Parse(reader));
                while (reader.Peek() != -1 && "#\r\n".Contains((char)reader.Peek()))
                {
                    // Comments and Empty Lines between the Project Blocks are skipped
                    reader.ReadLine();
                }
            }

            // We now have a global block
            var globalSectionBlocks = ParseGlobal(reader);

            // We should now be at the end of the file
            if (reader.Peek() != -1)
            {
                throw new Exception(WorkspacesResources.MissingEndOfFileInSolutionFile);
            }

            return new SolutionFile(headerLines, visualStudioVersionLineOpt, minimumVisualStudioVersionLineOpt, projectBlocks, globalSectionBlocks);
        }

        [SuppressMessage("", "RS0001")] // TODO: This suppression should be removed once we have rulesets in place for Roslyn.sln
        private static IEnumerable<SectionBlock> ParseGlobal(TextReader reader)
        {
            if (reader.Peek() == -1)
            {
                return Enumerable.Empty<SectionBlock>();
            }

            if (GetNextNonEmptyLine(reader) != "Global")
            {
                throw new Exception(string.Format(WorkspacesResources.MissingLineInSolutionFile, "Global"));
            }

            var globalSectionBlocks = new List<SectionBlock>();

            // The blocks inside here are indented
            while (reader.Peek() != -1 && char.IsWhiteSpace((char)reader.Peek()))
            {
                globalSectionBlocks.Add(SectionBlock.Parse(reader));
            }

            if (GetNextNonEmptyLine(reader) != "EndGlobal")
            {
                throw new Exception(string.Format(WorkspacesResources.MissingLineInSolutionFile, "EndGlobal"));
            }

            // Consume potential empty lines at the end of the global block
            while (reader.Peek() != -1 && "\r\n".Contains((char)reader.Peek()))
            {
                reader.ReadLine();
            }

            return globalSectionBlocks;
        }

        private static string GetNextNonEmptyLine(TextReader reader)
        {
            string line = null;

            do
            {
                line = reader.ReadLine();
            }
            while (line != null && line.Trim() == string.Empty);

            return line;
        }
    }
}