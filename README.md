## Welcome to the .NET Compiler Platform ("Roslyn")

||Windows - Debug|Windows - Release|Linux|Mac OSX|
|:--:|:--:|:--:|:--:|:--:|
|**master**|[![Build Status](http://dotnet-ci.cloudapp.net/job/dotnet_roslyn_master_win_dbg/badge/icon)](http://dotnet-ci.cloudapp.net/job/dotnet_roslyn_master_win_dbg/)|[![Build Status](http://dotnet-ci.cloudapp.net/job/dotnet_roslyn_master_win_rel/badge/icon)](http://dotnet-ci.cloudapp.net/job/dotnet_roslyn_master_win_rel/)|[![Build Status](http://dotnet-ci.cloudapp.net/job/dotnet_roslyn_master_lin/badge/icon)](http://dotnet-ci.cloudapp.net/job/dotnet_roslyn_master_lin/)|[![Build Status](http://dotnet-ci.cloudapp.net/job/dotnet_roslyn_master_mac/badge/icon)](http://dotnet-ci.cloudapp.net/job/dotnet_roslyn_master_mac/)|
|**future**|[![Build Status](http://dotnet-ci.cloudapp.net/job/dotnet_roslyn_future_win_dbg/badge/icon)](http://dotnet-ci.cloudapp.net/job/dotnet_roslyn_future_win_dbg/)|[![Build Status](http://dotnet-ci.cloudapp.net/job/dotnet_roslyn_future_win_rel/badge/icon)](http://dotnet-ci.cloudapp.net/job/dotnet_roslyn_future_win_rel/)|[![Build Status](http://dotnet-ci.cloudapp.net/job/dotnet_roslyn_future_lin/badge/icon)](http://dotnet-ci.cloudapp.net/job/dotnet_roslyn_future_lin/)|[![Build Status](http://dotnet-ci.cloudapp.net/job/dotnet_roslyn_future_mac/badge/icon)](http://dotnet-ci.cloudapp.net/job/dotnet_roslyn_future_mac/)|
|**stabilization**|[![Build Status](http://dotnet-ci.cloudapp.net/job/dotnet_roslyn_stabil_win_dbg/badge/icon)](http://dotnet-ci.cloudapp.net/job/dotnet_roslyn_stabil_win_dbg/)|[![Build Status](http://dotnet-ci.cloudapp.net/job/dotnet_roslyn_stabil_win_rel/badge/icon)](http://dotnet-ci.cloudapp.net/job/dotnet_roslyn_stabil_win_rel/)|[![Build Status](http://dotnet-ci.cloudapp.net/job/dotnet_roslyn_stabil_lin/badge/icon)](http://dotnet-ci.cloudapp.net/job/dotnet_roslyn_stabil_lin/)|[![Build Status](http://dotnet-ci.cloudapp.net/job/dotnet_roslyn_stabil_mac/badge/icon)](http://dotnet-ci.cloudapp.net/job/dotnet_roslyn_stabil_mac/)|


[![Join the chat at https://gitter.im/dotnet/roslyn](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/dotnet/roslyn?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)


The .NET Compiler Platform ("Roslyn") provides open-source C# and Visual Basic compilers 
with rich code analysis APIs.  It enables building code analysis tools with the same APIs 
that are used by Visual Studio.

### Try new language and IDE features

Just want to provide feedback on new [language features](https://github.com/dotnet/roslyn/wiki/Languages-features-in-C%23-6-and-VB-14) 
and [IDE features](http://blogs.msdn.com/b/visualstudio/archive/2014/11/12/the-c-and-visual-basic-code-focused-ide-experience.aspx)? 

* Try out [Visual Studio 2015](https://www.visualstudio.com/en-us/downloads/visual-studio-2015-downloads-vs.aspx), 
which has the latest features built-in.

* Want to keep your box clean? You can use 
[prebuilt Azure VM images](http://blogs.msdn.com/b/visualstudioalm/archive/2014/06/04/visual-studio-14-ctp-now-available-in-the-virtual-machine-azure-gallery.aspx) 
with VS 2015 already installed.

### Build tools that understand C# and Visual Basic

Get started building diagnostics, code fixes, refactorings, and other code-aware tools!

- [**Getting Started On Visual Studio 2015**](https://github.com/dotnet/roslyn/wiki/Getting-Started-on-Visual-Studio-2015)

Or, you can grab the latest [NuGet Roslyn compiler package](http://www.nuget.org/packages/Microsoft.CodeAnalysis). 
From the NuGet package manager console:

    Install-Package Microsoft.CodeAnalysis

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

### Contribute!

Some of the best ways to contribute are to try things out, file bugs, and join in design conversations. 

* [How to Contribute](https://github.com/dotnet/roslyn/wiki/Contributing-Code)
* [Pull requests](https://github.com/dotnet/roslyn/pulls): [Open](https://github.com/dotnet/roslyn/pulls?q=is%3Aopen+is%3Apr)/[Closed](https://github.com/dotnet/roslyn/pulls?q=is%3Apr+is%3Aclosed)

Looking for something to work on? The list of [up for grabs issues](https://github.com/dotnet/roslyn/issues?q=is%3Aopen+is%3Aissue+label%3A%22Up+for+Grabs%22) is a great place to start.

This project has adopted a code of conduct adapted from the [Contributor Covenant](http://contributor-covenant.org/) to clarify expected behavior in our community. This code of conduct has been [adopted by many other projects](http://contributor-covenant.org/adopters/). For more information see the [Code of conduct](http://www.dotnetfoundation.org/code-of-conduct).


### .NET Foundation

This project is part of the [.NET Foundation](http://www.dotnetfoundation.org/projects) along with other
projects like [the class libraries for .NET Core](https://github.com/dotnet/corefx/).
