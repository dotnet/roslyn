using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Roslyn.Services.OptionService
{
    internal class ChangedOptionSet : AbstractOptionSet
    {
        private readonly BaseOptionSet baseOptionSet;
        internal readonly ImmutableMap<OptionKeyAndLanguage, object> ChangedMap;

        public ChangedOptionSet(BaseOptionSet baseOptionSet, ImmutableMap<OptionKeyAndLanguage, object> oldChangedMap, OptionKey option, string languageName, object value)
        {
            // We must ensure that the value matches the underlying type of the option
            if (value == null)
            {
                if (option.Type.IsValueType)
                {
                    throw new ArgumentException("The option is a value type and cannot be assigned a value of null.".NeedsLocalization(), "value");
                }
            }
            else if (!option.Type.IsAssignableFrom(value.GetType()))
            {
                throw new ArgumentException("The value doesn't match the underlying type of the option.".NeedsLocalization(), "value");
            }

            this.baseOptionSet = baseOptionSet;
            this.ChangedMap = oldChangedMap.Add(new OptionKeyAndLanguage(option, languageName), value);
        }

        public override object GetOption(OptionKey option, string languageName)
        {
            object value;

            if (ChangedMap.TryGetValue(new OptionKeyAndLanguage(option, languageName), out value))
            {
                return value;
            }
            else
            {
                return baseOptionSet.GetOption(option, languageName);
            }
        }

        public override IOptionSet WithChangedOption(OptionKey option, string languageName, object value)
        {
            baseOptionSet.OptionService.VerifyLanguageNameIsAppropriate(option, languageName);
            return new ChangedOptionSet(baseOptionSet, ChangedMap, option, languageName, value);
        }
    }
}
