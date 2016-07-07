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
            var signTool = new TestSignTool(AppContext.BaseDirectory, binariesPath, sourcePath);
#else

            var sourcePath = Environment.GetEnvironmentVariable("BUILD_SOURCESDIRECTORY");
            var binariesPath = Path.Combine(sourcePath, @"Binaries\Release");
            var signTool = new RealSignTool(AppContext.BaseDirectory, binariesPath, sourcePath);
#endif

            var signData = ReadSignData(binariesPath);
            var util = new RunSignUtil(signTool, signData);
            util.Go();
        }

        internal static SignData ReadSignData(string rootBinaryPath)
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "BinaryData.json");
            using (var file = File.OpenText(filePath))
            {
                var serializer = new JsonSerializer();
                var fileJson = (FileJson)serializer.Deserialize(file, typeof(FileJson));
                return new SignData(rootBinaryPath, fileJson.SignList, fileJson.ExcludeList);
            }
        }
    }
}
