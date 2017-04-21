// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly IEnumerable<string> _headerLines;
        private readonly string _visualStudioVersionLineOpt;
        private readonly string _minimumVisualStudioVersionLineOpt;
        private readonly IEnumerable<ProjectBlock> _projectBlocks;
        private readonly IEnumerable<SectionBlock> _globalSectionBlocks;

        public SolutionFile(
            IEnumerable<string> headerLines,
            string visualStudioVersionLineOpt,
            string minimumVisualStudioVersionLineOpt,
            IEnumerable<ProjectBlock> projectBlocks,
            IEnumerable<SectionBlock> globalSectionBlocks)
        {
            if (headerLines == null)
            {
                throw new ArgumentNullException(nameof(headerLines));
            }

            if (projectBlocks == null)
            {
                throw new ArgumentNullException(nameof(projectBlocks));
            }

            if (globalSectionBlocks == null)
            {
                throw new ArgumentNullException(nameof(globalSectionBlocks));
            }

            _headerLines = headerLines.ToList().AsReadOnly();
            _visualStudioVersionLineOpt = visualStudioVersionLineOpt;
            _minimumVisualStudioVersionLineOpt = minimumVisualStudioVersionLineOpt;
            _projectBlocks = projectBlocks.ToList().AsReadOnly();
            _globalSectionBlocks = globalSectionBlocks.ToList().AsReadOnly();
        }

        public IEnumerable<string> HeaderLines
        {
            get { return _headerLines; }
        }

        public string VisualStudioVersionLineOpt
        {
            get { return _visualStudioVersionLineOpt; }
        }

        public string MinimumVisualStudioVersionLineOpt
        {
            get { return _minimumVisualStudioVersionLineOpt; }
        }

        public IEnumerable<ProjectBlock> ProjectBlocks
        {
            get { return _projectBlocks; }
        }

        public IEnumerable<SectionBlock> GlobalSectionBlocks
        {
            get { return _globalSectionBlocks; }
        }

        public string GetText()
        {
            var builder = new StringBuilder();

            builder.AppendLine();

            foreach (var headerLine in _headerLines)
            {
                builder.AppendLine(headerLine);
            }

            foreach (var block in _projectBlocks)
            {
                builder.Append(block.GetText());
            }

            builder.AppendLine("Global");

            foreach (var block in _globalSectionBlocks)
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
            if (headerLine1 == null || !headerLine1.StartsWith("Microsoft Visual Studio Solution File", StringComparison.Ordinal))
            {
                throw new Exception(string.Format(WorkspacesResources.Expected_header_colon_0, "Microsoft Visual Studio Solution File"));
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
                if (!visualStudioVersionLineOpt.StartsWith("VisualStudioVersion", StringComparison.Ordinal))
                {
                    throw new Exception(string.Format(WorkspacesResources.Expected_header_colon_0, "VisualStudioVersion"));
                }
            }

            string minimumVisualStudioVersionLineOpt = null;
            if (reader.Peek() == 'M')
            {
                minimumVisualStudioVersionLineOpt = GetNextNonEmptyLine(reader);
                if (!minimumVisualStudioVersionLineOpt.StartsWith("MinimumVisualStudioVersion", StringComparison.Ordinal))
                {
                    throw new Exception(string.Format(WorkspacesResources.Expected_header_colon_0, "MinimumVisualStudioVersion"));
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
                throw new Exception(WorkspacesResources.Expected_end_of_file);
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
                throw new Exception(string.Format(WorkspacesResources.Expected_0_line, "Global"));
            }

            var globalSectionBlocks = new List<SectionBlock>();

            // The blocks inside here are indented
            while (reader.Peek() != -1 && char.IsWhiteSpace((char)reader.Peek()))
            {
                globalSectionBlocks.Add(SectionBlock.Parse(reader));
            }

            if (GetNextNonEmptyLine(reader) != "EndGlobal")
            {
                throw new Exception(string.Format(WorkspacesResources.Expected_0_line, "EndGlobal"));
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
