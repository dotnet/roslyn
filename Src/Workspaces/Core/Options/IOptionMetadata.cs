using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Services.OptionService
{
    public interface IOptionMetadata
    {
        object DefaultValue { get; }
        bool HasPerLanguageValue { get; }
        IEnumerable<string> LimitToLanguageNames { get; }
    }
}
