using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Roslyn.Services
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    public class ExportOptionMigratorAttribute : ExportAttribute
    {
        public string ApplicableFeature { get; private set; }
        public IEnumerable<string> VersionsFrom { get; private set; }
        public string VersionTo { get; private set; }

        public ExportOptionMigratorAttribute(
            string applicableFeature,
            IEnumerable<string> versionsFrom,
            string versionTo)
            : base(typeof(IOptionMigrator))
        {
            this.ApplicableFeature = applicableFeature;
            this.VersionsFrom = versionsFrom;
            this.VersionTo = versionTo;
        }
    }
}