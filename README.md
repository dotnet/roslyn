## Welcome to the .NET Compiler Platform ("Roslyn")

[//]: # (Begin current test results)

### Windows - Unit Tests
||Debug x86|Debug x64|Release x86|Release x64|Determinism|
|:--:|:--:|:--:|:--:|:--:|:--:|
|**master**|[![Build Status](https://ci.dot.net/job/dotnet_roslyn/job/master/job/windows_debug_unit32/badge/icon)](https://ci.dot.net/job/dotnet_roslyn/job/master/job/windows_debug_unit32/)|[![Build Status](https://ci.dot.net/job/dotnet_roslyn/job/master/job/windows_debug_unit64/badge/icon)](https://ci.dot.net/job/dotnet_roslyn/job/master/job/windows_debug_unit64/)|[![Build Status](https://ci.dot.net/job/dotnet_roslyn/job/master/job/windows_release_unit32/badge/icon)](https://ci.dot.net/job/dotnet_roslyn/job/master/job/windows_release_unit32/)|[![Build Status](https://ci.dot.net/job/dotnet_roslyn/job/master/job/windows_release_unit64/badge/icon)](https://ci.dot.net/job/dotnet_roslyn/job/master/job/windows_release_unit64/)|[![Build Status](https://ci.dot.net/job/dotnet_roslyn/job/master/job/windows_determinism/badge/icon)](https://ci.dot.net/job/dotnet_roslyn/job/master/job/windows_determinism/)|
|**dev15-rc2**|[![Build Status](https://ci.dot.net/job/dotnet_roslyn/job/dev15-rc2/job/windows_debug_unit32/badge/icon)](https://ci.dot.net/job/dotnet_roslyn/job/dev15-rc2/job/windows_debug_unit32/)|[![Build Status](https://ci.dot.net/job/dotnet_roslyn/job/dev15-rc2/job/windows_debug_unit64/badge/icon)](https://ci.dot.net/job/dotnet_roslyn/job/dev15-rc2/job/windows_debug_unit64/)|[![Build Status](https://ci.dot.net/job/dotnet_roslyn/job/dev15-rc2/job/windows_release_unit32/badge/icon)](https://ci.dot.net/job/dotnet_roslyn/job/dev15-rc2/job/windows_release_unit32/)|[![Build Status](https://ci.dot.net/job/dotnet_roslyn/job/dev15-rc2/job/windows_release_unit64/badge/icon)](https://ci.dot.net/job/dotnet_roslyn/job/dev15-rc2/job/windows_release_unit64/)|[![Build Status](https://ci.dot.net/job/dotnet_roslyn/job/dev15-rc2/job/windows_determinism/badge/icon)](https://ci.dot.net/job/dotnet_roslyn/job/dev15-rc2/job/windows_determinism/)|

### Linux/Mac - Unit Tests
||Linux|Mac OSX|
|:--:|:--:|:--:|
|**master**|[![Build Status](https://ci.dot.net/job/dotnet_roslyn/job/master/job/linux_debug/badge/icon)](https://ci.dot.net/job/dotnet_roslyn/job/master/job/linux_debug/)|[![Build Status](https://ci.dot.net/job/dotnet_roslyn/job/master/job/mac_debug/badge/icon)](https://ci.dot.net/job/dotnet_roslyn/job/master/job/mac_debug/)|
|**dev15-rc2**|[![Build Status](https://ci.dot.net/job/dotnet_roslyn/job/dev15-rc2/job/linux_debug/badge/icon)](https://ci.dot.net/job/dotnet_roslyn/job/dev15-rc2/job/linux_debug/)|[![Build Status](https://ci.dot.net/job/dotnet_roslyn/job/dev15-rc2/job/mac_debug/badge/icon)](https://ci.dot.net/job/dotnet_roslyn/job/dev15-rc2/job/mac_debug/)|

[//]: # (End current test results)

[![Join the chat at https://gitter.im/dotnet/roslyn](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/dotnet/roslyn?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)


Roslyn provides open-source C# and Visual Basic compilers with rich code analysis APIs.  It enables building code analysis tools with the same APIs that are used by Visual Studio.

### Download C# and Visual Basic

Want to start developing in C# and Visual Basic? Download [Visual Studio 2015](https://www.visualstudio.com/en-us/downloads/visual-studio-2015-downloads-vs.aspx), 
which has the latest features built-in. There are also [prebuilt Azure VM images](https://azure.microsoft.com/en-us/marketplace/virtual-machines/all/?term=Visual+Studio+2015) available with VS 2015 already installed.

To install the latest release without Visual Studio, run one of the following [nuget](https://dist.nuget.org/index.html) command lines:

```
nuget install Microsoft.Net.Compilers   # Install C# and VB compilers
nuget install Microsoft.CodeAnalysis    # Install Language APIs and Services
```

Daily NuGet builds of the project are also available in our MyGet feed:

> [https://dotnet.myget.org/F/roslyn/api/v3/index.json](https://dotnet.myget.org/F/roslyn/api/v3/index.json)


### Source code

* Clone the sources: `git clone https://github.com/dotnet/roslyn.git`
* [Enhanced source view](http://source.roslyn.io/), powered by Roslyn 
* [Building, testing and debugging the sources](https://github.com/dotnet/roslyn/wiki/Building%20Testing%20and%20Debugging)

### Get started

* Tutorial articles by Alex Turner in MSDN Magazine
  - [Use Roslyn to Write a Live Code Analyzer for Your API](https://msdn.microsoft.com/en-us/magazine/dn879356)
  - [Adding a Code Fix to your Roslyn Analyzer](https://msdn.microsoft.com/en-us/magazine/dn904670.aspx)
* [Roslyn Overview](https://github.com/dotnet/roslyn/wiki/Roslyn%20Overview) 
* [API Changes between CTP 6 and RC](https://github.com/dotnet/roslyn/wiki/VS-2015-RC-API-Changes)
* [Samples and Walkthroughs](https://github.com/dotnet/roslyn/wiki/Samples-and-Walkthroughs)
* [Documentation](https://github.com/dotnet/roslyn/tree/master/docs)
* [Analyzer documentation](https://github.com/dotnet/roslyn/tree/master/docs/analyzers)
* [Syntax Visualizer Tool](https://github.com/dotnet/roslyn/wiki/Syntax%20Visualizer)
* [Syntax Quoter Tool](http://roslynquoter.azurewebsites.net)
* [Roadmap](https://github.com/dotnet/roslyn/wiki/Roadmap) 
* [Language Design Notes](https://github.com/dotnet/roslyn/issues?q=label%3A%22Design+Notes%22+)
* [FAQ](https://github.com/dotnet/roslyn/wiki/FAQ)
* Also take a look at our [Wiki](https://github.com/dotnet/roslyn/wiki) for more information on how to contribute, what the labels on issue mean, etc.

### Contribute!

Some of the best ways to contribute are to try things out, file bugs, and join in design conversations. 

* [How to Contribute](https://github.com/dotnet/roslyn/wiki/Contributing-Code)
* [Pull requests](https://github.com/dotnet/roslyn/pulls): [Open](https://github.com/dotnet/roslyn/pulls?q=is%3Aopen+is%3Apr)/[Closed](https://github.com/dotnet/roslyn/pulls?q=is%3Apr+is%3Aclosed)

Looking for something to work on? The list of [up for grabs issues](https://github.com/dotnet/roslyn/issues?q=is%3Aopen+is%3Aissue+label%3A%22Up+for+Grabs%22) is a great place to start.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).  For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

### .NET Foundation

This project is part of the [.NET Foundation](http://www.dotnetfoundation.org/projects) along with other
projects like [the class libraries for .NET Core](https://github.com/dotnet/corefx/). 
