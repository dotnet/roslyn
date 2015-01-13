
Building this project will produce an analyzer .dll, as well as the
following two ways you may wish to package that analyzer:
 * A NuGet package (.nupkg file) that will add your assembly as a
   project-local analyzer that participates in builds.
 * A VSIX extension (.vsix file) that will apply your analyzer to all projects
   and works just in the IDE.

Starting this project will deploy the analyzer as a VSIX into another copy of
Visual Studio, which is useful for debugging, even if you intend to produce a
NuGet package.


GETTING STARTED WITH NUGET

Before you can produce a NuGet package, you must enable NuGet Package Restore:
 * Right-click the solution in Solution Explorer and choose Enable NuGet
   Package Restore.

This downloads the NuGet binaries that will be used to pack your analyzer into
a NuGet package.  For more information, see
http://go.microsoft.com/fwlink/?LinkID=322105.

If you do not wish to produce a NuGet package, you can delete the
Diagnostic.nuspec file to silence the Package Restore warning.


TRYING OUT YOUR NUGET PACKAGE

To try out the NuGet package:
 1. Create a local NuGet feed by following the instructions here:
    > http://docs.nuget.org/docs/creating-packages/hosting-your-own-nuget-feeds
 2. Copy the .nupkg file into that folder.
 3. Open the target project in Visual Studio "14".
 4. Right-click on the project node in Solution Explorer and choose Manage
    NuGet Packages.
 5. Select the NuGet feed you created on the left.
 6. Choose your analyzer from the list and click Install.

If you want to automatically deploy the .nupkg file to the local feed folder
when you build this project, follow these steps:
 1. Right-click on this project in Solution Explorer and choose Properties.
 2. Go to the Build Events tab.
 3. In the "Post-build event command line" box, change the -OutputDirectory
    path to point to your local NuGet feed folder.
