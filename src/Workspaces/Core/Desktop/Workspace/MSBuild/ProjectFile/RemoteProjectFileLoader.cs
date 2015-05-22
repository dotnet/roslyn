using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.MSBuild
{
    internal class RemoteProjectFileLoader
    {
        public static IProjectFileLoader CreateProjectLoader(Type loaderType)
        {
            return (IProjectFileLoader)GetBuildDomain().CreateInstanceAndUnwrap(loaderType.Assembly.FullName, loaderType.FullName);
        }

        // a separate app domain used for running msbuild
        private static AppDomain s_BuildDomain;
        private static object gate = new object();

        private static AppDomain GetBuildDomain()
        {
            if (s_BuildDomain == null)
            {
                lock (gate)
                {
                    if (s_BuildDomain == null)
                    {
                        var thisAssembly = typeof(RemoteProjectFileLoader).Assembly.Location;
                        var configFile = CreateTempFile(GetResourceText("msbuild.config"));

                        s_BuildDomain = AppDomain.CreateDomain("MSBuildDomain",
                            new System.Security.Policy.Evidence(),
                            new AppDomainSetup
                            {
                                ApplicationBase = Path.GetDirectoryName(thisAssembly),
                                ConfigurationFile = configFile
                            });
                    }
                }
            }

            return s_BuildDomain;
        }

        private static string CreateTempFile(string content)
        {
            var filename = Path.GetTempFileName();
            using (var stream = File.OpenWrite(filename))
            {
                using (var writer = new StreamWriter(stream, System.Text.Encoding.UTF8))
                {
                    writer.Write(content);
                }
            }

            return filename;
        }

        private static string GetResourceText(string fileName)
        {
            var fullName = @"Microsoft.CodeAnalysis.Workspace.MSBuild." + fileName;
            var resourceStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(fullName);
            if (resourceStream != null)
            {
                using (var streamReader = new StreamReader(resourceStream))
                {
                    return streamReader.ReadToEnd();
                }
            }
            else
            {
                throw new InvalidOperationException(string.Format("Cannot find resource named: '{0}'", fullName));
            }
        }
    }
}