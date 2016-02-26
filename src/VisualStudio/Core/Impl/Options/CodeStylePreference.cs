using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal class CodeStylePreference
    {
        //string _name;
        //bool _isChecked;

        public CodeStylePreference()
        {

        }

        public CodeStylePreference(string name, bool isChecked)
        {
            Name = name;
            IsChecked = isChecked;
        }

        public string Name { get; set; }
        public bool IsChecked { get; set; }
    }
}
