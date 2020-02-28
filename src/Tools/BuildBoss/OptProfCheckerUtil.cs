using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace BuildBoss
{
    internal sealed class OptProfCheckerUtil : ICheckerUtil
    {
        private readonly string RepositoryDirectory;
        private readonly string ArtifactsDirectory;
        private readonly string Configuration;
        private readonly string OptProfFile;

        public OptProfCheckerUtil(string repositoryDirectory, string artifactsDirectory, string configuration)
        {
            RepositoryDirectory = repositoryDirectory;
            ArtifactsDirectory = artifactsDirectory;
            Configuration = configuration;
            OptProfFile = Path.Combine(RepositoryDirectory, "eng", "config", "OptProf.json");
        }

        public bool Check(TextWriter textWriter)
        {
            try
            {
                // check the optprof file exists, and load it if so
                if (!File.Exists(OptProfFile))
                {
                    textWriter.WriteLine($"Couldn't find OptProf file: {OptProfFile}");
                    return false;
                }
                var content = File.ReadAllText(OptProfFile);
                var optProfFile = JObject.Parse(content);

                // validate that each vsix listed in it is valid
                bool allGood = true;
                foreach (var product in optProfFile["products"])
                {
                    allGood &= CheckVsix(textWriter, product["name"].ToString(), product["tests"]);
                }
                return allGood;
            }
            catch (Exception ex)
            {
                textWriter.WriteLine($"Error verifying: {ex.Message}");
                return false;
            }
        }

        private bool CheckVsix(TextWriter textWriter, string vsixName, JToken tests)
        {
            string vsixRoot = Path.Combine(ArtifactsDirectory, "VSSetup", Configuration, "Insertion");
            string vsixFullPath = Path.Combine(vsixRoot, vsixName);

            // check vsix exists
            if (!File.Exists(vsixFullPath))
            {
                textWriter.WriteLine($"Product '{vsixName}' does not exist in '{vsixRoot}'");
                return false;
            }

            // read the manifest and extract the file list
            var manifestFileNames = GetManifestFileNames(textWriter, vsixFullPath);

            // check any filtered tests match what's in the vsix
            bool allGood = true;
            foreach (var test in tests)
            {
                string container = test["container"].ToString();
                var filteredTests = test["filteredTestCases"];
                if (filteredTests is object)
                {
                    foreach (var filteredTest in filteredTests)
                    {
                        var filename = filteredTest["filename"].ToString();
                        if (!manifestFileNames.Contains(filename))
                        {
                            allGood = false;
                            textWriter.WriteLine($"Failed to find file '{filename}' in '{vsixName}' (Container: {container})");
                        }
                    }
                }
            }
            return allGood;
        }

        private static HashSet<string> GetManifestFileNames(TextWriter textWriter, string vsixFullPath)
        {
            try
            {
                using (var archive = new ZipArchive(File.Open(vsixFullPath, FileMode.Open), ZipArchiveMode.Read))
                {
                    var entry = archive.GetEntry("manifest.json");
                    using var stream = entry.Open();
                    using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 2048, leaveOpen: true);
                    var content = reader.ReadToEnd();
                    var manifest = JObject.Parse(content);
                    return manifest["files"].Select(f => f["fileName"].ToString()).ToHashSet();
                }
            }
            catch (Exception)
            {
                textWriter.WriteLine($"Failed to read manifest from '{vsixFullPath}'");
                throw;
            }
        }
    }
}
