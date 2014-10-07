using System.Collections.Generic;

namespace Roslyn.Services
{
    /// <summary>
    /// Encapsulates all the options available to a specific feature.
    /// </summary>
    public interface IFeatureOptions
    {
        /// <summary>
        /// The feature that this provides options for.
        /// </summary>
        string Feature { get; }

        /// <summary>
        /// get all option descriptions belong to this feature
        /// </summary>
        IEnumerable<OptionDescription> GetAllOptionDescriptions();

        /// <summary>
        /// get current value for the option
        /// </summary>
        T GetOption<T>(OptionKey<T> key);

        /// <summary>
        /// set new value for the option
        /// </summary>
        T SetOption<T>(OptionKey<T> key, T value);

        /// <summary>
        /// reset the option to its default value
        /// </summary>
        void ResetOption<T>(OptionKey<T> key);

        /// <summary>
        /// reset all options to its default value
        /// </summary>
        void ResetOptions();
    }
}
