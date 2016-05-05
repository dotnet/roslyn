using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

Task.Run(() => StartWatching());
Console.WriteLine("Press any key to continue");
Console.Read();

void StartWatching()
{
    FileSystemWatcher watcher = new FileSystemWatcher();
    watcher.Path = Args[0];
    watcher.Created += new FileSystemEventHandler(OnChanged);
    watcher.IncludeSubdirectories = false;
    watcher.EnableRaisingEvents = true;
    while (true)
    {
        // Stay in loop
    }
}

void OnChanged(object sender, FileSystemEventArgs e)
{
    // Wait for 120 seconds to make sure all the contents are copied over to the share
    // If there is a better estimate then we can modify the wait time to be a little over the estimate.
    Thread.Sleep(120000);
    var processInfo = new ProcessStartInfo(@"TriggerAutomation.bat");
    processInfo.Arguments = Args[0];

    // Fire and forget
    var process = Process.Start(processInfo);
}