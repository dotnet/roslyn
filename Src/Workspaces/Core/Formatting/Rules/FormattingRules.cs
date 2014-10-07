using System;
using System.Collections.Generic;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.Formatting.Rules
{
    public static class FormattingRules
    {
        /// <summary>
        /// return default formatting rules
        /// 
        /// this can be modified and given to Format method to provide different rules
        /// </summary>
        public static IEnumerable<IFormattingRule> GetDefaultRules(string language)
        {
            var formatterService = Formatter.GetDefaultFormattingService(language);
            if (formatterService == null)
            {
                throw new ArgumentException("language");
            }

            return formatterService.GetDefaultFormattingRules();
        }
    }
}