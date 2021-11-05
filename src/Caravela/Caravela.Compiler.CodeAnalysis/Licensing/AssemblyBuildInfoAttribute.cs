using System;
using System.Globalization;

namespace Caravela.Compiler.Licensing
{
    [AttributeUsage(AttributeTargets.Assembly)]
    internal class AssemblyBuildInfoAttribute : Attribute
    {
        public string BuildDateString { get; set; }

        public string VersionString { get; set; }

        public string VersionSuffix { get; set; }

        private DateTime? _buildDate;

        public DateTime BuildDate =>
            _buildDate ?? (_buildDate = DateTime.Parse(BuildDateString, CultureInfo.InvariantCulture)).Value;

        private Version? _version;

        public Version Version => _version ??= Version.Parse(VersionString);

        private bool? _isPrerelease;

        public bool IsPrerelease => _isPrerelease ?? (_isPrerelease = string.IsNullOrEmpty(VersionSuffix)).Value; 

        public AssemblyBuildInfoAttribute(string buildDateString, string versionString, string versionSuffix)
        {
            BuildDateString = buildDateString;
            VersionString = versionString;
            VersionSuffix = versionSuffix;
        }
    }
}
