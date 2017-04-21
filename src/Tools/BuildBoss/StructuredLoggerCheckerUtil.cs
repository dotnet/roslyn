using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace BuildBoss
{
    /// <summary>
    /// The logic from this type is largely copied from:
    /// 
    ///   https://github.com/KirillOsenkov/MSBuildStructuredLog/blob/master/src/StructuredLogViewer/BuildAnalyzer.cs
    ///
    /// Once structured logger has a command line verification mode this code should be
    /// deleted.  The caller should instead use the structured logging tool for 
    /// verification.  Until then this is simple enough to duplicate.  Feature request
    /// is here:
    /// 
    ///     https://github.com/KirillOsenkov/MSBuildStructuredLog/issues/42
    /// </summary>
    internal sealed class StructuredLoggerCheckerUtil : ICheckerUtil
    {
        internal static readonly StringComparer FilePathComparer = StringComparer.OrdinalIgnoreCase;
        private static readonly string s_copyingFileFrom = "Copying file from \"";
        private static readonly string s_creatingHardLink = "Creating hard link to copy \"";
        private static readonly string s_didNotCopy = "Did not copy from file \"";
        private static readonly string s_to = "\" to \"";

        private readonly XDocument _document;
        private readonly Dictionary<string, List<string>> _copyMap = new Dictionary<string, List<string>>(FilePathComparer);

        internal StructuredLoggerCheckerUtil(XDocument document)
        {
            _document = document;
        }

        public bool Check(TextWriter textWriter)
        {
            foreach (var element in _document.XPathSelectElements("//CopyTask"))
            {
                foreach (var message in element.Elements(XName.Get("Message")))
                {
                    ProcessMessage(message);
                }
            }

            var allGood = true;
            foreach (var pair in _copyMap.OrderBy(x => x.Key))
            {
                var list = pair.Value;
                if (list.Count > 1)
                {
                    textWriter.WriteLine($"Multiple writes to {pair.Key}");
                    foreach (var item in list)
                    {
                        textWriter.WriteLine($"\t{item}");
                    }
                    textWriter.WriteLine("");

                    allGood = false;
                }
            }

            return allGood;
        }

        private void ProcessMessage(XElement element)
        {
            var text = element.Value.Trim();
            if (text.StartsWith(s_copyingFileFrom))
            {
                ProcessCopyingFileFrom(text, s_copyingFileFrom, s_to);
            }
            else if (text.StartsWith(s_creatingHardLink))
            {
                ProcessCopyingFileFrom(text, s_creatingHardLink, s_to);
            }
            else if (text.StartsWith(s_didNotCopy))
            {
                // Ignore files which were not copied.  This logic comes from the original 
                // analysis.
            }
        }

        private void ProcessCopyingFileFrom(string text, string prefix, string infix)
        {
            var split = text.IndexOf(infix);
            var prefixLength = prefix.Length;
            int toLength = infix.Length;
            var source = text.Substring(prefixLength, split - prefixLength);
            var destination = text.Substring(split + toLength, text.Length - 2 - split - toLength);

            List<string> list;
            if (!_copyMap.TryGetValue(destination, out list))
            {
                list = new List<string>();
                _copyMap[destination] = list;
            }

            list.Add(source);
        }
    }
}
