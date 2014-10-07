using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Roslyn.Services.CodeCleanup
{
    /// <summary>
    /// code cleanup options to tweak default behavior
    /// </summary>
    public class CodeCleanupOptions
    {
        // currently there is no option

        /// <summary>
        /// return default code cleanup options
        /// 
        /// this can be modified and given to Cleanup method to have different code cleanup behavior
        /// </summary>
        public static CodeCleanupOptions GetDefaultOptions(string language)
        {
            var cleanerService = CodeCleaner.GetCodeCleanerService(language);
            if (cleanerService == null)
            {
                throw new ArgumentException("language");
            }

            return cleanerService.GetDefaultOptions();
        }
    }
}