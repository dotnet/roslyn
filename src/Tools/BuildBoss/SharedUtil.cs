using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BuildBoss
{
    internal static class SharedUtil
    {
        internal static string MSBuildNamespaceUriRaw => "http://schemas.microsoft.com/developer/msbuild/2003";
        internal static XNamespace MSBuildNamespace { get; } = XNamespace.Get(MSBuildNamespaceUriRaw);

        internal static bool IsSolutionFile(string path) => Path.GetExtension(path) == ".sln";
        internal static bool IsPropsFile(string path) => Path.GetExtension(path) == ".props";
        internal static bool IsTargetsFile(string path) => Path.GetExtension(path) == ".targets";
        internal static bool IsXslt(string path) => Path.GetExtension(path) == ".xslt";
    }
}
