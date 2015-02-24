## Welcome to the .NET Compiler Platform ("Roslyn")

[![Join the chat at https://gitter.im/dotnet/roslyn](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/dotnet/roslyn?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

[![Build Status](http://dotnet-ci.cloudapp.net/job/dotnet_roslyn/badge/icon)](http://dotnet-ci.cloudapp.net/job/dotnet_roslyn/)

The .NET Compiler Platform ("Roslyn") provides open-source C# and Visual Basic compilers 
with rich code analysis APIs.  It enables building code analysis tools with the same APIs 
that are used by Visual Studio.

### Try new language and IDE features

Just want to provide feedback on new [language features](https://github.com/dotnet/roslyn/wiki/Languages-features-in-C%23-6-and-VB-14) 
and [IDE features](http://blogs.msdn.com/b/visualstudio/archive/2014/11/12/the-c-and-visual-basic-code-focused-ide-experience.aspx)? 

* Try out [Visual Studio 2015 Preview](http://www.visualstudio.com/en-us/downloads/visual-studio-2015-downloads-vs), 
which has the latest features built-in.

    Want to keep your box clean? You can use 
[prebuilt Azure VM images](http://blogs.msdn.com/b/visualstudioalm/archive/2014/06/04/visual-studio-14-ctp-now-available-in-the-virtual-machine-azure-gallery.aspx) 
with VS 2015 Preview already installed.

* You can also try April's [End User Preview](http://go.microsoft.com/fwlink/?LinkId=394641), 
which installs on top of Visual Studio 2013. *(Note: The VS 2013 preview is quite out of date, and is no longer being updated.)*

### Build tools that understand C# and Visual Basic

Get started building diagnostics, code fixes, refactorings, and other code-aware tools!

To get started on **Visual Studio 2015 Preview**:

1. Set up a box with Visual Studio 2015 Preview. Either 
[install  Visual Studio 2015 Preview](http://www.visualstudio.com/en-us/downloads/visual-studio-2015-downloads-vs), 
or grab a [prebuilt Azure VM image](http://blogs.msdn.com/b/visualstudioalm/archive/2014/06/04/visual-studio-14-ctp-now-available-in-the-virtual-machine-azure-gallery.aspx).
2. Install the [Visual Studio 2015 Preview SDK](http://www.visualstudio.com/en-us/downloads/visual-studio-2015-downloads-vs). 
You'll need to do this even if you're using the Azure VM image. 
3. Install the [SDK Templates VSIX package](http://visualstudiogallery.msdn.microsoft.com/849f3ab1-05cf-4682-b4af-ef995e2aa1a5) 
to get the Visual Studio project templates. 
4. Install the [Syntax Visualizer VSIX package](http://visualstudiogallery.msdn.microsoft.com/70e184da-9b3a-402f-b210-d62a898e2887) 
to get a [Syntax Visualizer tool window](https://github.com/dotnet/roslyn/wiki/Syntax%20Visualizer) 
to help explore the syntax trees you'll be analyzing.

To get started on **Visual Studio 2015 CTP 5**:

1. Set up a box with Visual Studio 2015 CTP 5. Either 
[install  Visual Studio 2015 CTP 5](http://go.microsoft.com/fwlink/?LinkId=400496), 
or grab a [prebuilt Azure VM image](http://blogs.msdn.com/b/visualstudioalm/archive/2014/06/04/visual-studio-14-ctp-now-available-in-the-virtual-machine-azure-gallery.aspx).
2. Install the [Visual Studio 2015 CTP 5 SDK](http://go.microsoft.com/fwlink/?LinkId=400496). 
You'll need to do this even if you're using the Azure VM image. 
3. Install the [SDK Templates VSIX package](https://visualstudiogallery.msdn.microsoft.com/ae1cf421-54bf-4406-b48c-76a182819fb7) 
to get the Visual Studio project templates. 
4. Install the [Syntax Visualizer VSIX package](https://visualstudiogallery.msdn.microsoft.com/b5104545-29ed-46b2-beb0-351af9ca2d21) 
to get a [Syntax Visualizer tool window](https://github.com/dotnet/roslyn/wiki/Syntax%20Visualizer) 
to help explore the syntax trees you'll be analyzing.

Or, you can grab the latest [NuGet Roslyn compiler package](http://www.nuget.org/packages/Microsoft.CodeAnalysis). 
From the NuGet package manager console:

    Install-Package Microsoft.CodeAnalysis -Pre

### Source code

* Clone the sources: `git clone https://github.com/dotnet/roslyn.git`
* [Enhanced source view](http://source.roslyn.io/), powered by Roslyn 
* [Building, testing and debugging the sources](https://github.com/dotnet/roslyn/wiki/Building%20Testing%20and%20Debugging)

### Get started

* [Roslyn Overview](https://github.com/dotnet/roslyn/wiki/Roslyn%20Overview) 
* [Samples and Walkthroughs](https://github.com/dotnet/roslyn/wiki/Samples-and-Walkthroughs)
* [Syntax Visualizer Tool](https://github.com/dotnet/roslyn/wiki/Syntax%20Visualizer)
* [Roadmap](https://github.com/dotnet/roslyn/wiki/Roadmap) 
* [Language Feature Status](https://github.com/dotnet/roslyn/wiki/Languages-features-in-C%23-6-and-VB-14)
* [Language Design Notes](https://github.com/dotnet/roslyn/issues?q=label%3A%22Area-Design+Notes%22+)
* [FAQ](https://github.com/dotnet/roslyn/wiki/FAQ)

### Contribute!

Some of the best ways to contribute are to try things out, file bugs, and join in design conversations. 

* [How to Contribute](https://github.com/dotnet/roslyn/wiki/Contributing-Code)
* [Pull requests](https://github.com/dotnet/roslyn/pulls): [Open](https://github.com/dotnet/roslyn/pulls?q=is%3Aopen+is%3Apr)/[Closed](https://github.com/dotnet/roslyn/pulls?q=is%3Apr+is%3Aclosed)

Looking for something to work on? The list of [up for grabs issues](https://github.com/dotnet/roslyn/issues?q=is%3Aopen+is%3Aissue+label%3A%22Up+for+Grabs%22) is a great place to start.

### .NET Foundation

This project is part of the [.NET Foundation](http://www.dotnetfoundation.org/projects) along with other
projects like [the class libraries for .NET Core](https://github.com/dotnet/corefx/).
