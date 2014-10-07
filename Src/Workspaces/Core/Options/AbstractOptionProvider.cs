using System.Collections.Generic;

namespace Roslyn.Services.OptionService
{
    internal abstract class AbstractOptionProvider : IOptionProvider
    {
        protected abstract IEnumerable<string> AllOptionNames { get; }
        protected abstract object GetOptionDefaultValue(string optionName);

        public IEnumerable<OptionDescription> GetAllOptionDescriptions()
        {
            foreach (var optionName in this.AllOptionNames)
            {
                yield return CreateFeatureOptionDescription(optionName);
            }
        }

        public T GetOptionDefaultValue<T>(OptionKey<T> key)
        {
            return (T)GetOptionDefaultValue(key.Name);
        }

        private OptionDescription CreateFeatureOptionDescription(string optionName)
        {
            return new OptionDescription(optionName, GetOptionDefaultValue(optionName).GetType());
        }
    }
}