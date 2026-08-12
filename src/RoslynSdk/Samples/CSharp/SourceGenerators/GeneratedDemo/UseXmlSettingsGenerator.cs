using System;
using AutoSettings;

namespace GeneratedDemo
{
    public static class UseXmlSettingsGenerator
    {
        public static void Run()
        {
            // This XmlSettings generator makes a static property in the XmlSettings class for each .xmlsettings file

            // here we have the 'Main' settings file from MainSettings.xmlsettings
            // the name is determined by the 'name' attribute of the root settings element
            XmlSettings.MainSettings main = XmlSettings.Main;
            Console.WriteLine($"Reading settings from {main.GetLocation()}");

            // settings are strongly typed and can be read directly from the static instance
            bool firstRun = XmlSettings.Main.FirstRun;
            Console.WriteLine($"Setting firstRun = {firstRun}");

            int cacheSize = XmlSettings.Main.CacheSize;
            Console.WriteLine($"Setting cacheSize = {cacheSize}");

            // Try adding some keys to the settings file and see the settings become available to read from
        }
    }
}
