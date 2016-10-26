
Building this project will produce an analyzer .dll, as well as the
following two ways you may wish to package that analyzer:
 * A NuGet package (.nupkg file) that will add your assembly as a
   project-local analyzer that participates in builds.
 * A VSIX extension (.vsix file) that will apply your analyzer to all projects
   and works just in the IDE.

To debug your analyzer, make sure the default project is the VSIX project and
start debugging.  This will deploy the analyzer as a VSIX into another instance
of Visual Studio, which is useful for debugging, even if you intend to produce
a NuGet package.


TRYING OUT YOUR NUGET PACKAGE

To try out the NuGet package:
 1. Create a local NuGet feed by following the instructions here:
    > http://docs.nuget.org/docs/creating-packages/hosting-your-own-nuget-feeds
 2. Copy the .nupkg file into that folder.
 3. Open the target project in Visual Studio 2015.
 4. Right-click on the project node in Solution Explorer and choose Manage
    NuGet Packages.
 5. Select the NuGet feed you created on the left.
 6. Choose your analyzer from the list and click Install.

If you want to automatically deploy the .nupkg file to the local feed folder
when you build this project, follow these steps:
 1. Right-click on this project in Solution Explorer and choose 'Unload Project'.
 2. Right-click on this project and click "Edit".
 3. Scroll down to the "AfterBuild" target.
 4. In the "Exec" task, change the value inside "Command" after the -OutputDirectory
    path to point to your local NuGet feed folder.