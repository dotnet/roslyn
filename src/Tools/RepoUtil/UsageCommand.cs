using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepoUtil
{
    internal class UsageCommand : ICommand
    {
        public bool Run(TextWriter writer, string[] args)
        {
            Usage(writer);
            return true;
        }

        internal static void Usage(TextWriter writer = null)
        {
            writer = writer ?? Console.Out;
            var text = @"
  verify: check the state of the repo
  consumes: output the conent consumed by this repo
  produces: output the content produced by this repo
  change: change the dependencies.
";
            writer.WriteLine(text);
        }
    }
}
