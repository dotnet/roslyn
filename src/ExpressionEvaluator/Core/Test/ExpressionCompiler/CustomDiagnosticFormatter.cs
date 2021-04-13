// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Globalization;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
{
    internal sealed class CustomDiagnosticFormatter : DiagnosticFormatter
    {
        internal static new readonly CustomDiagnosticFormatter Instance = new CustomDiagnosticFormatter();

        public override string Format(Diagnostic diagnostic, IFormatProvider formatter = null)
        {
            var cultureInfo = (CultureInfo)formatter;
            return string.Format("LCID={0}, Code={1}", cultureInfo.LCID, diagnostic.Code);
        }
    }
}
