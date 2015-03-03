using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    internal static class DiagnosticDescriptorExtensions
    {
        public static void SetOnLocalizableStringException(this DiagnosticDescriptor descriptor, DiagnosticAnalyzer owner, Action<Exception, DiagnosticAnalyzer, Diagnostic> onLocalizableStringException)
        {
            Action<Exception> onException = ex =>
            {
                if (onLocalizableStringException != null)
                {
                    var diagnostic = AnalyzerExecutor.GetAnalyzerDiagnostic(owner, ex);
                    onLocalizableStringException(ex, owner, diagnostic);
                }
            };

            ((IExceptionSafeLocalizableString)descriptor.Title).SetOnException(onException);
            ((IExceptionSafeLocalizableString)descriptor.MessageFormat).SetOnException(onException);
            ((IExceptionSafeLocalizableString)descriptor.Description).SetOnException(onException);
        }
    }
}
