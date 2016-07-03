using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignRoslyn
{
    internal static class Program
    {
        internal static void Main(string[] args)
        {
#if DEBUG
            var binariesPath = @"e:\dd\roslyn\Binaries\Debug";
            var sourcePath = @"e:\dd\roslyn";
            var ignoreFailures = true;
#else

            var sourcePath = Environment.GetEnvironmentVariable("BUILD_SOURCESDIRECTORY");
            var binariesPath = Path.Combine(sourcePath, @"Binaries\Release");
            var ignoreFailures = false;
#endif

            var filePath = Path.Combine(AppContext.BaseDirectory, "BinaryData.json");
            using (var file = File.OpenText(filePath))
            {
                var serializer = new JsonSerializer();
                var fileJson = (FileJson)serializer.Deserialize(file, typeof(FileJson));
                var tool = new SignTool(
                    AppContext.BaseDirectory,
                    binariesPath: binariesPath,
                    sourcePath: sourcePath,
                    ignoreFailures: ignoreFailures);
                var util = new RunSignUtil(tool, binariesPath, fileJson.SignList, fileJson.ExcludeList);
                util.Go();
            }
        }
    }
}
