// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license 

using System;

namespace Microsoft.CodeAnalysis
{
    internal static class ReportDiagnosticExtensions
    {
        public static DiagnosticSeverity? ToDiagnosticSeverity(this ReportDiagnostic reportDiagnostic)
        {
            switch (reportDiagnostic)
            {
                case ReportDiagnostic.Error:
                    return DiagnosticSeverity.Error;

                case ReportDiagnostic.Warn:
                    return DiagnosticSeverity.Warning;

                case ReportDiagnostic.Info:
                    return DiagnosticSeverity.Info;

                case ReportDiagnostic.Hidden:
                    return DiagnosticSeverity.Hidden;

                case ReportDiagnostic.Suppress:
                case ReportDiagnostic.Default:
                    return null;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
