## Welcome to the .NET Compiler Platform ("Roslyn")

### Windows - Unit Tests
||Debug x86|Debug x64|Release x86|Release x64|Determinism|
|:--:|:--:|:--:|:--:|:--:|:--:|
|**master**|[![Build Status](http://dotnet-ci.cloudapp.net/job/roslyn_master_win_dbg_unit32/badge/icon)](http://dotnet-ci.cloudapp.net/job/roslyn_master_win_dbg_unit32/)|[![Build Status](http://dotnet-ci.cloudapp.net/job/roslyn_master_win_dbg_unit64/badge/icon)](http://dotnet-ci.cloudapp.net/job/roslyn_master_win_dbg_unit64/)|[![Build Status](http://dotnet-ci.cloudapp.net/job/roslyn_master_win_rel_unit32/badge/icon)](http://dotnet-ci.cloudapp.net/job/roslyn_master_win_rel_unit32/)|[![Build Status](http://dotnet-ci.cloudapp.net/job/roslyn_master_win_rel_unit64/badge/icon)](http://dotnet-ci.cloudapp.net/job/roslyn_master_win_rel_unit64/)|[![Build Status](http://dotnet-ci.cloudapp.net/job/roslyn_master_determinism/badge/icon)](http://dotnet-ci.cloudapp.net/job/roslyn_master_determinism/)|
|**future**|[![Build Status](http://dotnet-ci.cloudapp.net/job/roslyn_future_win_dbg_unit32/badge/icon)](http://dotnet-ci.cloudapp.net/job/roslyn_future_win_dbg_unit32/)|[![Build Status](http://dotnet-ci.cloudapp.net/job/roslyn_future_win_dbg_unit64/badge/icon)](http://dotnet-ci.cloudapp.net/job/roslyn_future_win_dbg_unit64/)|[![Build Status](http://dotnet-ci.cloudapp.net/job/roslyn_future_win_rel_unit32/badge/icon)](http://dotnet-ci.cloudapp.net/job/roslyn_future_win_rel_unit32/)|[![Build Status](http://dotnet-ci.cloudapp.net/job/roslyn_future_win_rel_unit64/badge/icon)](http://dotnet-ci.cloudapp.net/job/roslyn_future_win_rel_unit64/)|[![Build Status](http://dotnet-ci.cloudapp.net/job/roslyn_future_determinism/badge/icon)](http://dotnet-ci.cloudapp.net/job/roslyn_future_determinism/)|
|**stabilization**|[![Build Status](http://dotnet-ci.cloudapp.net/job/roslyn_stabil_win_dbg_unit32/badge/icon)](http://dotnet-ci.cloudapp.net/job/roslyn_stabil_win_dbg_unit32/)|[![Build Status](http://dotnet-ci.cloudapp.net/job/roslyn_stabil_win_dbg_unit64/badge/icon)](http://dotnet-ci.cloudapp.net/job/roslyn_stabil_win_dbg_unit64/)|[![Build Status](http://dotnet-ci.cloudapp.net/job/roslyn_stabil_win_rel_unit32/badge/icon)](http://dotnet-ci.cloudapp.net/job/roslyn_stabil_win_rel_unit32/)|[![Build Status](http://dotnet-ci.cloudapp.net/job/roslyn_stabil_win_rel_unit64/badge/icon)](http://dotnet-ci.cloudapp.net/job/roslyn_stabil_win_rel_unit64/)|[![Build Status](http://dotnet-ci.cloudapp.net/job/roslyn_stabil_determinism/badge/icon)](http://dotnet-ci.cloudapp.net/job/roslyn_stabil_determinism/)|

### Linux/Mac - Unit Tests
||Linux|Mac OSX|
|:--:|:--:|:--:|
|**master**|[![Build Status](http://dotnet-ci.cloudapp.net/job/roslyn_master_lin_dbg_unit32/badge/icon)](http://dotnet-ci.cloudapp.net/job/roslyn_master_lin_dbg_unit32/)|[![Build Status](http://dotnet-ci.cloudapp.net/job/roslyn_master_mac_dbg_unit32/badge/icon)](http://dotnet-ci.cloudapp.net/job/roslyn_master_mac_dbg_unit32/)|
|**future**|[![Build Status](http://dotnet-ci.cloudapp.net/job/roslyn_future_lin_dbg_unit32/badge/icon)](http://dotnet-ci.cloudapp.net/job/roslyn_future_lin_dbg_unit32/)|[![Build Status](http://dotnet-ci.cloudapp.net/job/roslyn_future_mac_dbg_unit32/badge/icon)](http://dotnet-ci.cloudapp.net/job/roslyn_future_mac_dbg_unit32/)|
|**stabilization**|[![Build Status](http://dotnet-ci.cloudapp.net/job/roslyn_stabil_lin_dbg_unit32/badge/icon)](http://dotnet-ci.cloudapp.net/job/roslyn_stabil_lin_dbg_unit32/)|[![Build Status](http://dotnet-ci.cloudapp.net/job/roslyn_stabil_mac_dbg_unit32/badge/icon)](http://dotnet-ci.cloudapp.net/job/roslyn_stabil_mac_dbg_unit32/)|


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

To get the latest "preview" drop, published about once per month, add the `-pre` switch to the nuget commands.

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
* [Roadmap](https://github.com/dotnet/roslyn/wiki/Roadmap) 
* [Language Feature Status](https://github.com/dotnet/roslyn/wiki/Languages-features-in-C%23-6-and-VB-14)
* [Language Design Notes](https://github.com/dotnet/roslyn/issues?q=label%3A%22Design+Notes%22+)
* [FAQ](https://github.com/dotnet/roslyn/wiki/FAQ)
* Also take a look at our [Wiki](https://github.com/dotnet/roslyn/wiki) for more information on how to contribute, what the labels on issue mean, etc.

### Contribute!

Some of the best ways to contribute are to try things out, file bugs, and join in design conversations. 

* [How to Contribute](https://github.com/dotnet/roslyn/wiki/Contributing-Code)
* [Pull requests](https://github.com/dotnet/roslyn/pulls): [Open](https://github.com/dotnet/roslyn/pulls?q=is%3Aopen+is%3Apr)/[Closed](https://github.com/dotnet/roslyn/pulls?q=is%3Apr+is%3Aclosed)

Looking for something to work on? The list of [up for grabs issues](https://github.com/dotnet/roslyn/issues?q=is%3Aopen+is%3Aissue+label%3A%22Up+for+Grabs%22) is a great place to start.

This project has adopted a code of conduct adapted from the [Contributor Covenant](http://contributor-covenant.org/) to clarify expected behavior in our community. This code of conduct has been [adopted by many other projects](http://contributor-covenant.org/adopters/). For more information see the [Code of conduct](http://www.dotnetfoundation.org/code-of-conduct).


### .NET Foundation

This project is part of the [.NET Foundation](http://www.dotnetfoundation.org/projects) along with other
projects like [the class libraries for .NET Core](https://github.com/dotnet/corefx/). 
