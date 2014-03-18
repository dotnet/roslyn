// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public static class ErrorHelpers
    {
        public static string Stringize(this Diagnostic e)
        {
            var retVal = string.Empty;
            if (e.Location.IsInSource)
            {
                retVal = e.Location.SourceSpan.ToString() + ": ";
            }
            else if (e.Location.IsInMetadata)
            {
                return "metadata: ";
            }
            else
            {
                return "no location: ";
            }

            retVal = e.Severity.ToString() + " " + e.Id + ": " + e.GetMessage(CultureInfo.CurrentCulture);
            return retVal;
        }

        public static string Stringize(this XElement e)
        {
            return
                e.Attribute("Severity") + " " +
                e.Attribute("Code") + ": " +
                (e.Element("Message") != null ? e.Element("Message").Value + " " : string.Empty) +
                (e.Element("Span") != null
                    ? string.Format("[{0},{1}) Length: {2}", e.Element("Span").Attribute("Start"),
                                                             e.Element("Span").Attribute("End"),
                                                             e.Element("Span").Attribute("Length")) : string.Empty);
        }

        public static string DumpDiagnostics(this EmitResult result)
        {
            var stringBuilder = new StringBuilder();
            foreach (var diag in result.Diagnostics)
            {
                stringBuilder.AppendLine(diag.Stringize());
            }

            return stringBuilder.ToString();
        }
    }
}