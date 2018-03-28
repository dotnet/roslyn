using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static class ITextBufferEditExtensions
    {
        private static Exception s_lastException = null;

        /// <summary>
        /// Logs exceptions thrown during <see cref="ITextBufferEdit.Apply"/> as we look for issues.
        /// </summary>
        /// <param name="edit"></param>
        /// <returns></returns>
        public static ITextSnapshot ApplyAndLogExceptions(this ITextBufferEdit edit)
        {
            try
            {
                return edit.Apply();
            }
            catch (Exception e) when (ErrorReporting.FatalError.ReportWithoutCrash(e))
            {
                s_lastException = e;

                // Since we don't know what is causing this yet, I don't feel safe that catching
                // will not cause some further downstream failure. So we'll continue to propagate.
                throw;
            }
        }
    }
}
