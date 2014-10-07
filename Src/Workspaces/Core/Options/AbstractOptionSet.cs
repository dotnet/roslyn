using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Roslyn.Services.OptionService
{
    internal abstract class AbstractOptionSet : IOptionSet
    {
        public object GetOption(OptionKey option)
        {
            return GetOption(option, languageName: null);
        }

        public abstract object GetOption(OptionKey option, string languageName);

        public T GetOption<T>(OptionKey<T> option)
        {
            return (T)GetOption((OptionKey)option);
        }

        public T GetOption<T>(OptionKey<T> option, string languageName)
        {
            return (T)GetOption((OptionKey)option, languageName);
        }

        public IOptionSet WithChangedOption(OptionKey option, object value)
        {
            return WithChangedOption(option, languageName: null, value: value);
        }

        public abstract IOptionSet WithChangedOption(OptionKey option, string languageName, object value);

        public IOptionSet WithChangedOption<T>(OptionKey<T> option, T value)
        {
            return WithChangedOption(option, languageName: null, value: value);
        }

        public IOptionSet WithChangedOption<T>(OptionKey<T> option, string languageName, T value)
        {
            return WithChangedOption((OptionKey)option, languageName, value);
        }
    }
}
