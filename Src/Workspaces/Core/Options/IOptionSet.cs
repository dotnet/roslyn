using System;

namespace Roslyn.Services.OptionService
{
    public interface IOptionSet
    {
        object GetOption(OptionKey option);
        object GetOption(OptionKey option, string languageName);
        T GetOption<T>(OptionKey<T> option);
        T GetOption<T>(OptionKey<T> option, string languageName);

        IOptionSet WithChangedOption(OptionKey option, object value);
        IOptionSet WithChangedOption(OptionKey option, string languageName, object value);
        IOptionSet WithChangedOption<T>(OptionKey<T> option, T value);
        IOptionSet WithChangedOption<T>(OptionKey<T> option, string languageName, T value);
    }
}
