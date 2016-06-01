Hooray, hoorah! We're excited that you are joining us on our journey! Before starting out, it's important to **make sure you are working in the right branch**. Once you figure out what branch you are going to work in, get specific instructions on how to build/test/debug by following the respective links in the branch descriptions in the table below. 

## Picking Your Branch
Here are the main branches you should know about:

| Branch |       |
| ------ | ----- | 
| [**master**](//github.com/dotnet/roslyn/tree/master) | Our primary branch. Changes here currently target Visual Studio "15". If in doubt, this is where you should work, and submit pull requests. <br/>[Instructions for Building on Windows](//github.com/dotnet/roslyn/blob/master/docs/contributing/Building, Debugging, and Testing on Windows.md) <br/>[Instructions for Building on Linux and Mac](//github.com/dotnet/roslyn/blob/master/docs/infrastructure/cross-platform.md) |
| [**stabilization**](//github.com/dotnet/roslyn/tree/stabilization) | Changes here target updates of Visual Studio 2015, and is only taking bug fixes at this point. <br/>[Instructions for Building on Windows](//github.com/dotnet/roslyn/blob/future/docs/contributing/Building, Debugging, and Testing on Windows.md) <br/>[Instructions for Building on Linux and Mac](//github.com/dotnet/roslyn/blob/future/docs/infrastructure/cross-platform.md) |
| [**future-stabilization**](//github.com/dotnet/roslyn/tree/future-stabilization) | When we're getting close to a release of a Visual Studio "15", we use this branch to stabilize and limit the churn. You generally shouldn't use this branch unless you have a good reason to do so -- you'll know it if you need it. |

## Known Issues
Please see the [known contributor issues](https://github.com/dotnet/roslyn/labels/Contributor%20Pain) that you might encounter contributing to Roslyn. If you issue isn't listed, please file it.
