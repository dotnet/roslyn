Hooray, hoorah! We're excited that you are joining us on our journey! Before starting out, it's important to **make sure you are working in the right branch**. Once you figure out what branch you are going to work in, get specific instructions on how to build/test/debug by following the respective links in the branch descriptions in the table below. 

## Picking Your Branch
Here are the main branches you should know about:

| Branch |       |
| ------ | ----- | 
| [**main**](https://github.com/dotnet/roslyn/tree/main) | Our primary branch. Changes here currently target Visual Studio 2022 17.0. In the absence of other guidance, this is where you should work, and submit pull requests. <br/>[Instructions for Building on Windows](https://github.com/dotnet/roslyn/blob/main/docs/contributing/Building,%20Debugging,%20and%20Testing%20on%20Windows.md) <br/>[Instructions for Building on Linux and Mac](https://github.com/dotnet/roslyn/blob/main/docs/infrastructure/cross-platform.md) |
| [**community**](https://github.com/dotnet/roslyn/tree/community) | If you see a **pinned issue** or similar announcement referring to the **community** branch, then, you may need to base your work on it instead of main. If clicking the link to this branch gives you HTTP 404, you can assume there is no need to use it at this time. This branch is sometimes used when the Roslyn team needs to take dependencies on unreleased components, which makes it so our main branch doesn't work with the publicly released version of Visual Studio.  |
## Known Issues
Please see the [known contributor issues](https://github.com/dotnet/roslyn/labels/Contributor%20Pain) that you might encounter contributing to Roslyn. If you issue isn't listed, please file it.
