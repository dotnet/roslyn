// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal class DebuggerDiagnosticFormatter : DiagnosticFormatter
    {
        public override string Format(Diagnostic diagnostic, IFormatProvider? formatter = null)
        {
            if (diagnostic == null)
            {
                throw new ArgumentNullException(nameof(diagnostic));
            }

            var culture = formatter as CultureInfo;

            return string.Format(formatter, "{0}: {1}",
                GetMessagePrefix(diagnostic),
                diagnostic.GetMessage(culture));
        }

        internal static new readonly DebuggerDiagnosticFormatter Instance = new DebuggerDiagnosticFormatter();
    }
}
