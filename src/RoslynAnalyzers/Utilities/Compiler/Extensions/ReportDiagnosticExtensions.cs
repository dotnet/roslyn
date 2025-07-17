// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis
{
    internal static class ReportDiagnosticExtensions
    {
        public static string ToAnalyzerConfigString(this ReportDiagnostic reportDiagnostic)
        {
            return reportDiagnostic switch
            {
                ReportDiagnostic.Error => "error",
                ReportDiagnostic.Warn => "warning",
                ReportDiagnostic.Info => "suggestion",
                ReportDiagnostic.Hidden => "silent",
                ReportDiagnostic.Suppress => "none",
                _ => throw new NotImplementedException(),
            };
        }

        public static bool IsLessSevereThan(this ReportDiagnostic current, ReportDiagnostic other)
        {
            return current switch
            {
                ReportDiagnostic.Error => false,

                ReportDiagnostic.Warn =>
                    other switch
                    {
                        ReportDiagnostic.Error => true,
                        _ => false
                    },

                ReportDiagnostic.Info =>
                    other switch
                    {
                        ReportDiagnostic.Error => true,
                        ReportDiagnostic.Warn => true,
                        _ => false
                    },

                ReportDiagnostic.Hidden =>
                    other switch
                    {
                        ReportDiagnostic.Error => true,
                        ReportDiagnostic.Warn => true,
                        ReportDiagnostic.Info => true,
                        _ => false
                    },

                ReportDiagnostic.Suppress => true,

                _ => false
            };
        }
    }
}
