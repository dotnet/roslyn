using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

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
    internal sealed class StructuredLoggerChecekerUtil : ICheckerUtil
    {
        private readonly XDocument _document;

        internal StructuredLoggerChecekerUtil(XDocument document)
        {
            _document = document;
        }

        public bool Check(TextWriter textWriter)
        {
            return true;
        }
    }
}
