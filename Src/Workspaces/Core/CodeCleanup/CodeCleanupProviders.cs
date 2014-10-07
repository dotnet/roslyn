using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.CodeCleanup
{
    public static class CodeCleanupProviders
    {
        /// <summary>
        /// return default code cleaners
        /// 
        /// this can be modified and given to Cleanup method to provide different cleaners
        /// </summary>
        public static IEnumerable<ICodeCleanupProvider> GetDefaultProviders(string language)
        {
            var cleanerService = CodeCleaner.GetCodeCleanerService(language);
            if (cleanerService == null)
            {
                throw new ArgumentException("language");
            }

            return cleanerService.GetDefaultProviders();
        }
    }
}