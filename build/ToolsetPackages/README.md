# Toolset packages

Our repo needs a number of tools that are delivered by NuGet packages.  These packages shouldn't be added to project.json files in shipping projects becausee the assets aren't actually referenced.  Additionally in some cases the package references can complicate the package graph and lead to developer headaches.  

For such toolset packages we use the project.json files in this directory to restore / download them.  In some ways it's just an efficient way of using NuGet.exe to download tools, SDKs, etc ... into known locations.  The files are broken down into the following uses:

- project.json: General file for referencing SDK and toolsets.
- dev14.project.json: The Dev14 SDK and toolsets
- dev15rc.project.json: The Dev15 RC SDK and toolsets.  
- closed.project.json: Contains all of the NuGet packages contained in our closed repo that aren't in Open.  This is done only to give visibility to these packages in our build verification tools. 

In general we try and keep the number of files here low.  New ones are added only when the contents can potentially conflict with packages listed in the existing files.
