using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style.NamingPreferences
{
    public class EnforcementLevel
    {
        public EnforcementLevel(DiagnosticSeverity severity)
        {
            Value = severity;
            switch (severity)
            {
                case DiagnosticSeverity.Hidden:
                    Name = "None";
                    Moniker = KnownMonikers.None;
                    return;
                case DiagnosticSeverity.Info:
                    Name = "Info";
                    Moniker = KnownMonikers.StatusInformation;
                    return;
                case DiagnosticSeverity.Warning:
                    Name = "Warning";
                    Moniker = KnownMonikers.StatusWarning;
                    return;
                case DiagnosticSeverity.Error:
                    Name = "Error";
                    Moniker = KnownMonikers.StatusError;
                    return;
                default:
                    throw new ArgumentException("Unexpected DiagnosticSeverity", nameof(severity));
            }
        }

        public EnforcementLevel(string name, DiagnosticSeverity value, ImageMoniker moniker)
        {
            Name = name;
            Value = value;
            Moniker = moniker;
        }

        public ImageMoniker Moniker { get; private set; }
        public string Name { get; private set; }
        public DiagnosticSeverity Value { get; private set; }
    }
}
