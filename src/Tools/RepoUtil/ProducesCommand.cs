using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepoUtil
{
    internal class ProducesCommand : ICommand
    {
        private readonly RepoConfig _repoConfig;
        private readonly string _sourcesPath;

        internal ProducesCommand(RepoConfig repoConfig, string sourcesPath)
        {
            _repoConfig = repoConfig;
            _sourcesPath = sourcesPath;
        }

        public bool Run(TextWriter writer, string[] args)
        {
            foreach (var fileName in NuSpecUtil.GetNuSpecFiles(_sourcesPath))
            {
                if (_repoConfig.NuSpecExcludes.Any(x => x.IsMatch(fileName.RelativePath)))
                {
                    continue;
                }

                var id = NuSpecUtil.GetId(fileName.FullPath);
                writer.WriteLine(id);
            }

            return true;
        }
    }
}
