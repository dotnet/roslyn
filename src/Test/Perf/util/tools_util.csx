// ShellOut()
#load "test_util.csx"

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

void CopyDirectory(string source, string destination, string argument = @"/mir")
{
    var result = ShellOut("Robocopy", $"{argument} {source} {destination}", "");

    // Robocopy has a success exit code from 0 - 7
    if (result.Code > 7)
    {
        throw new IOException($"Failed to copy \"{source}\" to \"{destination}\".");
    }
}
