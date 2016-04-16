// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal class DebuggerDiagnosticFormatter : DiagnosticFormatter
    {
        public override string Format(Diagnostic diagnostic, IFormatProvider formatter = null)
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

        internal static readonly new DebuggerDiagnosticFormatter Instance = new DebuggerDiagnosticFormatter();
    }
}
