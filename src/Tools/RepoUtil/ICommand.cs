using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepoUtil
{
    internal interface ICommand
    {
        bool Run(TextWriter writer, string[] args);
    }
}
