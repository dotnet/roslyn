Hooray, hoorah! We're excited that you are joining us on our journey! Before starting out, it's important to **make sure you are working in the right branch**. Once you figure out what branch you are going to work in, get specific instructions on how to build/test/debug by following the respective links in the branch descriptions in the table below. 

## Picking Your Branch
Here are the main branches you should know about:

| Branch |       |
| ------ | ----- | 
| [**master**](//github.com/dotnet/roslyn/tree/master) | Our primary branch. Changes here currently target a future update of Visual Studio 2017. If in doubt, this is where you should work, and submit pull requests. <br/>[Instructions for Building on Windows](//github.com/dotnet/roslyn/blob/master/docs/contributing/Building, Debugging, and Testing on Windows.md) <br/>[Instructions for Building on Linux and Mac](//github.com/dotnet/roslyn/blob/master/docs/infrastructure/cross-platform.md) |
| [**dev16**](//github.com/dotnet/roslyn/tree/dev16) | Changes here target whatever version of Visual Studio comes after Visual Studio 2017. <br/>[Instructions for Building on Windows](//github.com/dotnet/roslyn/blob/dev16/docs/contributing/Building, Debugging, and Testing on Windows.md) <br/>[Instructions for Building on Linux and Mac](//github.com/dotnet/roslyn/blob/dev16/docs/infrastructure/cross-platform.md)
| [**microupdate**](//github.com/dotnet/roslyn/tree/microupdate) | If you're still trying to use Visual Studio 2015 to contribute, this is the only remaining branch that supports it. Fixes will not be taken into this branch except in extremely rare situations (security fixes, etc.) <br/>[Instructions for Building on Windows](//github.com/dotnet/roslyn/blob/microupdate/docs/contributing/Building, Debugging, and Testing on Windows.md) <br/>[Instructions for Building on Linux and Mac](//github.com/dotnet/roslyn/blob/microupdate/docs/infrastructure/cross-platform.md)

## Known Issues
Please see the [known contributor issues](https://github.com/dotnet/roslyn/labels/Contributor%20Pain) that you might encounter contributing to Roslyn. If you issue isn't listed, please file it.
