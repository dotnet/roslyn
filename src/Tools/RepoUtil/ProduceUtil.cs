using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepoUtil
{
    internal class ProduceUtil
    {
        private readonly RepoConfig _repoConfig;
        private readonly string _sourcesPath;

        internal ProduceUtil(RepoConfig repoConfig, string sourcesPath)
        {
            _repoConfig = repoConfig;
            _sourcesPath = sourcesPath;
        }

        internal void Go()
        {
            foreach (var fileName in NuSpecUtil.GetNuSpecFiles(_sourcesPath))
            {
                if (_repoConfig.NuSpecExcludes.Any(x => x.IsMatch(fileName.RelativePath)))
                {
                    continue;
                }

                var id = NuSpecUtil.GetId(fileName.FullPath);
                Console.WriteLine(id);
            }
        }
    }
}
