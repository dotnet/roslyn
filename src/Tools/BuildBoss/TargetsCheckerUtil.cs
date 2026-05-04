// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BuildBoss
{
    internal sealed class TargetsCheckerUtil : ICheckerUtil
    {
        private readonly string _targetDir;

        internal TargetsCheckerUtil(string targetDir)
        {
            _targetDir = targetDir;
        }

        public bool Check(TextWriter textWriter)
        {
            var allGood = true;

            foreach (var filePath in Directory.GetFiles(_targetDir))
            {
                var fileName = Path.GetFileName(filePath);
                if (fileName == "README.md")
                {
                    continue;
                }

                textWriter.WriteLine($"Checking {fileName}");
                if (SharedUtil.IsPropsFile(filePath))
                {
                    allGood &= CheckProps(new ProjectUtil(filePath), textWriter);
                }
                else if (SharedUtil.IsTargetsFile(filePath))
                {
                    allGood &= CheckTargets(new ProjectUtil(filePath), textWriter);
                }
                else if (SharedUtil.IsXslt(filePath))
                {
                    // Nothing to verify
                }
                else
                {
                    textWriter.WriteLine("Unrecognized file type");
                    allGood = false;
                }
            }

            return allGood;
        }

        private bool CheckProps(ProjectUtil util, TextWriter textWriter)
        {
            var allGood = true;
            foreach (var project in GetImportProjects(util))
            {
                if (!SharedUtil.IsPropsFile(project))
                {
                    textWriter.WriteLine($"Props files should only Import other props files");
                    allGood = false;
                }
            }

            if (util.GetTargets().Any())
            {
                textWriter.WriteLine($"Props files should not contain <Target> elements");
                allGood = false;
            }

            return allGood;
        }

        private bool CheckTargets(ProjectUtil util, TextWriter textWriter)
        {
            var allGood = true;
            foreach (var project in GetImportProjects(util))
            {
                if (SharedUtil.IsPropsFile(project))
                {
                    textWriter.WriteLine($"Targets files should not Import props files");
                    allGood = false;
                }
            }

            return allGood;
        }

        private static IEnumerable<string> GetImportProjects(ProjectUtil util)
        {
            foreach (var project in util.GetImportProjects())
            {
                if (IsReplacedValue(project))
                {
                    continue;
                }

                yield return project;
            }
        }

        /// <summary>
        /// Is this a whole replaced MSBuild value like $(Trick)
        /// </summary>
        private static bool IsReplacedValue(string value)
        {
            if (value.Length <= 3)
            {
                return false;
            }

            return
                value[0] == '$' &&
                value[1] == '(' &&
                value[value.Length - 1] == ')';
        }
    }
}
