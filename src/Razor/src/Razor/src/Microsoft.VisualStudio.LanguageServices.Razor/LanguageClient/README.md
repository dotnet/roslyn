# Running this extension as the "Experimental instance"

## Having a "proper" VS build

1. From an Administrator Powershell prompt run \\vspreinstall\PREINSTALL\Preinstall.cmd
1. Install the VS Enterprise->Branch Channel of Visual Studio from the latest successful build of <http://ddweb/dashboard/vsbuild/>.
1. Workloads: ASP.NET and web dev., VS extension dev., .NET Core cross-platform dev., .NET desktop Dev.

## Using the Razor LSP Editor

## On VS Master

Tools>Options>Environment>Preview Features>Enable LSP powered Razor editor.

## On VS public builds

The above bits aren't yet public though, so if you have to use a master build set an environment variable under the name `Razor.LSP.Editor` to `true`, making sure you launch the VS instance afterward, so it can pick upt the ENV change. To set the environment variable in powershell you can use the following syntax: `${env:Razor.LSP.Editor}="true"`

After doing either of the above running the `Microsoft.VisualStudio.RazorExtension` project will then result in `.razor` and `.cshtml` files being opened with our LSP editor.

## FAQ

### How do I view the logs?

Logs are written to the %temp%\vslogs folder, in `.svclog` files with "Razor" in the name. You can increase logging here by passing `/log` on the command line when launching VS, or by setting an environment variable called `LogLevel` to `All`.
Logs are also written to the "Razor Logger Output" category of the Output Window in VS. You can increase logging here by changing the "Log Level" option in Tools, Options, Text Editor, Razor, Advanced.

### Opening a project results in my Razor file saying "waiting for IntelliSense to initialize", why does it never stop?

This is a combo issue dealing with how Visual Studio serializes project state after a feature flag / environment variable has been set. Basically, prior to setting `Razor.LSP.Editor` Visual Studio will have serialized project state that says a Razor file was opened with the WTE editor. Therefore, when you first open a project that Razor file will attempt to be opened under the WTE editor but the core editor will conflict saying it should be opened by our editor. This results in the endless behavior of "waiting for IntelliSense to initialize".

Close and re-open the file and it shouldn't re-occur if you re-save the solution.

### VS isn't actually using my locally built LSP, what do?

1. Double-check that you have `Razor.LSP.Editor` set to true, have set the Preview feature switch, and that you have built the project.
1. Close and re-open the offending file, see if it works once re-opened (a known issue causes this behavior).
1. Put a breakpoint in the RazorEditorFactory.CreateEditorInstance when opening a new Razor doc, does it get hit?
    1. If so debugging through to figure out why should be pretty straight forward.
1. Put a breakpoint in the RazorPackage.InitializeAsync, when opening a new Razor doc for the first time (it gets called once per-IDE lifetime), does it get hit?
    1. If so and no exceptions get thrown then something haywire is happening and we'll have to dig deeper
    1. If not It means our editor factory is not being found
        1. Delete the ComponentModelCache folder in the experimental instance location, re-run and see if that fixes things
        1. Delete the entire %localappdata%\Microsoft\VisualStudio\16.0_55c115ffRoslynDev folder (after closing the experiemental instance). Your RoslynDev version at the end of that path will be different and retry all of the above.
            1. Occasionally the Experiemental instance gets corrupted due to updating VS, bad deploys etc. and it becomes necessary to remove the entire thing. It's not a great solution and ideally it's best to avoid it (takes a few mins for VS to rebuild it).
