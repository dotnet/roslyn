using System;
using System.Collections.Generic;

namespace Roslyn.Compilers
{
    public interface IOptions
    {
        /// <summary>
        /// Gets a list of the option names.
        /// </summary>
        IEnumerable<string> GetOptionNames();

        /// <summary>
        /// True if the compilation options has the specified option.
        /// </summary>
        bool HasOption(string name);

        /// <summary>
        /// Gets an individual option value.
        /// </summary>
        string GetOption(string name);

        /// <summary>
        /// Creates a new options instance including the new option value.
        /// </summary>
        IOptions SetOption(string name, string value);
    }
}
