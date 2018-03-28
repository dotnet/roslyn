using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Linq;

namespace RepoUtil
{
    internal static class NuGetConfigUtil
    {
        internal static List<NuGetFeed> GetNuGetFeeds(string nugetConfigFile)
        {
            var nugetConfig = XElement.Load(nugetConfigFile);
            var nugetFeeds =
                from el in nugetConfig.Descendants("packageSources").Descendants("add")
                select new NuGetFeed(el.Attribute("key").Value, new Uri(el.Attribute("value").Value));

            return nugetFeeds.ToList();
        }

        internal static IEnumerable<string> GetNuGetConfigFiles(string sourcesPath)
        {
            return Directory.EnumerateFiles(sourcesPath, "nuget*config", SearchOption.AllDirectories);
        }
    }
}
