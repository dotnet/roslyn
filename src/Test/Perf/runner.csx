#load ".\util\runner_util.csx"
#load ".\util\test_util.csx"

using System.Collections.Generic;
using System.IO;

InitUtilities();

var myDir = MyWorkingDirectory();
var skip = new HashSet<string> {
    Path.Combine(myDir, "runner.csx"),
    Path.Combine(myDir, "util"),
};

foreach (var script in AllCsiRecursive(myDir, skip)) {
    Log("Running " + script);
    await RunFile(script);
}
