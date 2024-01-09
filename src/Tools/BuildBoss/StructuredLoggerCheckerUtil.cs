// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Logging.StructuredLogger;

namespace BuildBoss
{
    /// <summary>
    /// This type invokes the analyzer here:
    /// 
    ///   https://github.com/KirillOsenkov/MSBuildStructuredLog/blob/master/src/StructuredLogger/Analyzers/DoubleWritesAnalyzer.cs
    ///
    /// </summary>
    internal sealed class StructuredLoggerCheckerUtil : ICheckerUtil
    {
        private readonly string _logFilePath;

        internal StructuredLoggerCheckerUtil(string logFilePath)
        {
            _logFilePath = logFilePath;
        }

        public bool Check(TextWriter textWriter)
        {
            try
            {
                var build = Serialization.Read(_logFilePath);

                var allGood = true;
                allGood &= CheckDoubleWrites(build);
                allGood &= CheckDoubleTargetFramework(build);
                return allGood;
            }
            catch (Exception ex)
            {
                textWriter.WriteLine($"Error processing binary log file: {ex.Message}");
                return false;
            }

            bool CheckDoubleWrites(Build build)
            {
                var doubleWrites = DoubleWritesAnalyzer.GetDoubleWrites(build).ToArray();

                // Issue https://github.com/dotnet/roslyn/issues/62372
                if (doubleWrites.Any(doubleWrite => Path.GetFileName(doubleWrite.Key) != "Microsoft.VisualStudio.Text.Internal.dll"))
                {
                    foreach (var doubleWrite in doubleWrites)
                    {
                        textWriter.WriteLine($"Multiple writes to {doubleWrite.Key}");
                        foreach (var source in doubleWrite.Value)
                        {
                            textWriter.WriteLine($"\t{source}");
                        }

                        textWriter.WriteLine();
                    }

                    return false;
                }

                return true;
            }

            // Check to see if any TargetFrameworks entry includes the same TFM multiple times.
            // Our use of MSBuild properties to set TFM can lead to this particularly when moving
            // the base TFMs forward.
            bool CheckDoubleTargetFramework(Build build)
            {
                var set = new HashSet<string>();
                var allGood = true;
                build.VisitAllChildren<TimedNode>(node =>
                {
                    if (node is IProjectOrEvaluation e &&
                        e.TargetFramework is not null &&
                        e.TargetFramework.Contains(';'))
                    {
                        set.Clear();
                        var tfms = e.TargetFramework.Split([';'], StringSplitOptions.RemoveEmptyEntries);
                        foreach (var tfm in tfms)
                        {
                            if (!set.Add(tfm.Trim()))
                            {
                                var name = e switch
                                {
                                    Project p => p.ProjectFile,
                                    ProjectEvaluation p => p.ProjectFile,
                                    _ => $"<unknown> {e.GetType()}",
                                };

                                textWriter.WriteLine($"{name} <TargetFrameworks> includes {tfm} multiple times");
                                allGood = false;
                            }
                        }
                    }
                });

                return allGood;
            }
        }
    }
}
