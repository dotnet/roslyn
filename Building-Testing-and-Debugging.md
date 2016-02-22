Hooray, hoorah! We're excited that you are joining us on our journey! Before starting out, it's important to **make sure you are working in the right branch**. Once you figure out what branch you are going to work in, get specific instructions on how to build/test/debug by following the respective links in the branch descriptions in the table below. 

## Picking Your Branch
Here are the main branches you should know about:

| Branch |       |
| ------ | ----- | 
| [**master**](//github.com/dotnet/roslyn/tree/master) | Our primary branch. Changes here currently target Visual Studio 2015 Update 3. If in doubt, this is where you should work, and submit pull requests. <br/>[Instructions for Building on Windows](//github.com/dotnet/roslyn/blob/master/docs/contributing/Building, Debugging, and Testing on Windows.md) <br/>[Instructions for Building on Linux and Mac](//github.com/dotnet/roslyn/blob/master/docs/infrastructure/cross-platform.md) |
| [**future**](//github.com/dotnet/roslyn/tree/future) | Changes here will target the next major version of Visual Studio. This branch is primarily used for doing long-term language development. <br/>[Instructions for Building on Windows](//github.com/dotnet/roslyn/blob/future/docs/contributing/Building, Debugging, and Testing on Windows.md) <br/>[Instructions for Building on Linux and Mac](//github.com/dotnet/roslyn/blob/future/docs/infrastructure/cross-platform.md) |
| [**stabilization**](//github.com/dotnet/roslyn/tree/stabilization) | When we're getting close to a release of a Visual Studio update, we use this branch to stabilize and limit the churn. You generally shouldn't use this branch unless you have a good reason to do so -- you'll know it if you need it. |
| [**update-1**](//github.com/dotnet/roslyn/tree/update-1) | This branch is what we shipped in Visual Studio 2015 Update 1, plus some changes to allow you to build experimental extensions. Pull requests will not be accepted to this branch (they should go to master instead). If you want to contribute a new feature or a bug fix, you can just branch off of master. You can use this branch as a base if you are looking to build a private build that matches Update 1 as closely as possible. For example, if you're making a "hotfix" and want to ensure as little is changing from the "official" bits as possible, start here. If you make a change from here, send the pull request to master. <br />[Instructions for Building on Windows](//github.com/dotnet/roslyn/blob/update-1/docs/contributing/Building, Debugging, and Testing on Windows.md) |

## Known Issues
Please see the [known contributor issues](https://github.com/dotnet/roslyn/labels/Contributor%20Pain) that you might encounter contributing to Roslyn. If you issue isn't listed, please file it.
