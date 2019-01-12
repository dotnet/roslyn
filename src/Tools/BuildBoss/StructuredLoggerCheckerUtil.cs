using System;
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
                var doubleWrites = DoubleWritesAnalyzer.GetDoubleWrites(build).ToArray();
                if (doubleWrites.Any())
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
            catch (Exception ex)
            {
                textWriter.WriteLine($"Error processing binary log file: {ex.Message}");
                return false;
            }
        }
    }
}
