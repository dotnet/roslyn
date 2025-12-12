// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;

namespace BuildBoss
{
    internal static class SolutionUtil
    {
        internal static List<ProjectEntry> ParseProjects(string solutionPath)
        {
            using (var reader = new StreamReader(solutionPath))
            {
                var list = new List<ProjectEntry>();
                while (true)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                    {
                        break;
                    }

                    if (!line.StartsWith("Project"))
                    {
                        continue;

                    }

                    list.Add(ParseProjectLine(line));
                }
                return list;
            }
        }

        private static ProjectEntry ParseProjectLine(string line)
        {
            var index = 0;
            var typeGuid = ParseStringLiteral(line, ref index);
            var name = ParseStringLiteral(line, ref index);
            var filePath = ParseStringLiteral(line, ref index);
            var guid = ParseStringLiteral(line, ref index);
            return new ProjectEntry(
                relativeFilePath: filePath,
                name: name,
                projectGuid: Guid.Parse(guid),
                typeGuid: Guid.Parse(typeGuid));
        }

        private static string ParseStringLiteral(string line, ref int index)
        {
            var start = line.IndexOf('"', index);
            if (start < 0)
            {
                goto error;
            }

            start++;
            var end = line.IndexOf('"', start);
            if (end < 0)
            {
                goto error;
            }

            index = end + 1;
            return line.Substring(start, end - start);

error:
            throw new Exception($"Invalid project line {line}");
        }
    }
}
