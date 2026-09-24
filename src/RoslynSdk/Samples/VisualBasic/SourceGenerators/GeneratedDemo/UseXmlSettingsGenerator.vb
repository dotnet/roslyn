Imports AutoSettings

Public Module UseXmlSettingsGenerator

    Public Sub Run()

        ' This XmlSettings generator makes a static property in the XmlSettings class for each .xmlsettings file

        ' here we have the 'Main' settings file from MainSettings.xmlsettings
        ' the name is determined by the 'name' attribute of the root settings element
        Dim main As XmlSettings.MainSettings = XmlSettings.Main
        Console.WriteLine($"Reading settings from {main.GetLocation()}")

        ' settings are strongly typed and can be read directly from the static instance
        Dim firstRun As Boolean = XmlSettings.Main.FirstRun
        Console.WriteLine($"Setting firstRun = {firstRun}")

        Dim cacheSize As Integer = XmlSettings.Main.CacheSize
        Console.WriteLine($"Setting cacheSize = {cacheSize}")

        ' Try adding some keys to the settings file and see the settings become available to read from

    End Sub

End Module
