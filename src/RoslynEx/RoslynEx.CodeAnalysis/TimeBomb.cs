using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace RoslynEx
{
    static class TimeBomb
    {
        [AttributeUsage(AttributeTargets.Assembly)]
        public class AssemblyBuildDate : Attribute
        {
            public string BuildDateString { get; set; }

            public DateTime BuildDate => DateTime.Parse(BuildDateString, CultureInfo.InvariantCulture);

            public AssemblyBuildDate(string buildDateString) => BuildDateString = buildDateString;
        }

        private const int ExplosionTimeDays = 90;
        private const int ExplosionWarningDays = 60;

        public static IEnumerable<Diagnostic> GetExplosionDiagnostics()
        {
            var attributes = typeof(TimeBomb).Assembly.GetCustomAttributes(typeof(AssemblyBuildDate), inherit: false);

            if (!attributes.Any())
                return Array.Empty<Diagnostic>();

            var buildDate = ((AssemblyBuildDate)attributes[0]).BuildDate;

            int buildAgeDays = (int)(DateTime.UtcNow - buildDate).TotalDays;

            IEnumerable<Diagnostic> CreateDiagnostic(ErrorCode errorCode) =>
                new[] { Diagnostic.Create(RoslynExMessageProvider.Instance, (int)errorCode, buildAgeDays, ExplosionTimeDays) };

            if (buildAgeDays > ExplosionTimeDays)
                return CreateDiagnostic(ErrorCode.ERR_TimeBombExploded);

            if (buildAgeDays >= ExplosionWarningDays)
                return CreateDiagnostic(ErrorCode.WRN_TimeBombAboutToExplode);

            return Array.Empty<Diagnostic>();
        }
    }
}
