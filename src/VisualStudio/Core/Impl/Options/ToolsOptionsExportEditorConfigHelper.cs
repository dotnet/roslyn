using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal static class ToolsOptionsExportEditorConfigHelper
    {
        internal static void AppendName(string name, StringBuilder editorconfig)
        {
            editorconfig.Append(name + " = ");
        }

        internal static int JoinMultipleValues(Dictionary<Option<bool>, string> allOptions, OptionSet optionSet, StringBuilder ruleString)
        {
            var valuesApplied = 0;
            foreach (var curValue in allOptions)
            {
                if (optionSet.GetOption(curValue.Key))
                {
                    if (ruleString.Length != 0)
                    {
                        ruleString.Append(",");
                    }
                    ruleString.Append(curValue.Value);
                    ++valuesApplied;
                }
            }
            return valuesApplied;
        }

        internal static string NotificationOptionToString(NotificationOption notificationOption)
        {
            if (notificationOption == NotificationOption.Silent)
            {
                return nameof(NotificationOption.Silent).ToLowerInvariant();
            }
            else
            {
                return notificationOption.ToString().ToLowerInvariant();
            }
        }
    }
}
