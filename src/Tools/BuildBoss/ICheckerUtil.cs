using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildBoss
{
    internal interface ICheckerUtil
    {
        bool Check(TextWriter textWriter);
    }
}
