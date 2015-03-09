## Install and build Roslyn

1. Install the latest Visual Studio 14 preview from (http://www.visualstudio.com/en-us/downloads/visual-studio-14-ctp-vs.aspx)
2. Install the corresponding  VSSDK from the same page 
3. Clone the repo using: git clone https://github.com/dotnet/roslyn.git
4. Checkout the branch that matches the downloaded preview E.g: Git checkout releases/Dev14CTP5
5. Start VS, Load the Roslyn Solution 
6. Set the Tools\OpenSourceDebug project to the default project 
7. Use the menu to build the solution 

## Build and Debug Roslyn with Visual Studio

1. Install and Configure VS as above
2. Start VS, Load the Roslyn Solution 
3. Press F5 to start debugging 
4. The solution will build and start a new instance of Visual Studio 
5. In the new instance of VS create a new C# or VB project 
6. In the VS with the Roslyn solution open, add a breakpoint to the file: Workspaces\workspace\workspace.cs at line 142 
7. Add an interface to the project created earlier and see the breakpoint hit.

## Build Project with OSS compilers within Visual Studio

1. Install and Configure VS as above
2. Press F5 to start a new instance of VS
3. Create a new C# or VB project
4. Use Tools/Options/Projects and Solutions/Build and Run to set the 'Build Project Verbosity' to 'Normal' so that we can ensure the correct compiler was used
5. Build the solution you built above.
6. Look in the build 0utput window and observe the compiler used is similar to: `%USERPROFILE%\APPDATA\LOCAL\MICROSOFT\VISUALSTUDIO\14.0ROSLYN\EXTENSIONS\MSOPENTECH\OPENSOURCEDEBUG\1.0\csc.exe`

## Build with OSS Roslyn compilers using MSBUILD

1.  In a Visual Studio Command Shell, type the following command:
2.  MSBUILD ConsoleApplication01.csproj /t:Rebuild /p:RoslynHive=VisualStudio\14.0Roslyn
3.  In the build output from this command observe a compiler command line similar to: `%USERPROFILE%\APPDATA\LOCAL\MICROSOFT\VISUALSTUDIO\14.0ROSLYN\EXTENSIONS\MSOPENTECH\OPENSOURCEDEBUG\1.0\csc.exe`
