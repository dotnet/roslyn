# Razor Visual Studio Integration Tests

## Introduction

The legacy Razor editor has a series of integration tests inside VS, using the Integration test framework known as APEX. We found the framework had significant drawbacks since it can't run against PRs, or even VS insertions, so it is a trailing indicator indicator of quality. By the time a test would fail, the bad code was already checked into a version of VS that potentially is shipping. Due to the unreliability we were unable to ever get the tests turned on as "Required", so failures were not very visible.

Instead for the modern LSP based editor we are trying the [vs-extension-testing](https://github.com/microsoft/vs-extension-testing) framework. Check out their documentation for a bit of a primer.

## Where is the code?

[Code lives here](https://github.com/dotnet/razor/tree/main/src/Razor/test/Microsoft.VisualStudio.Razor.IntegrationTests).
The pipeline that runs tests against VS main are [here](https://devdiv.visualstudio.com/DevDiv/_build?definitionId=15591) (Microsoft internal).
The pipelines for [PR tests](https://dev.azure.com/dnceng-public/public/_build?definitionId=103&_a=summary) and tests in [razor-CI-official](https://dev.azure.com/dnceng/internal/_build?definitionId=262&_a=summary) run against VS Preview. The exact version of VS Preview used is set by the image selected in `azure-pipelines.yml`.

## Authoring Tests

The general strategy for an individual test is:

1. Open a file.
1. Get yourself in the proper state. It's generally not recommended to use content from the default files because they may change when templates are updated.
1. Trigger your scenario.
1. Almost certainly wait somehow for your thing to happen. It will usually cause an un-reliable test if you don't wait at all or if you use something like Task.Sleep(timeSpan). Whilst not perfect, we tend to rely on polling, assuming that the tests are to detect regressions and so we expect them to pass most of the time. Given the number of moving parts to a Razor operation, which could involve up to 3 different LSP servers in multiple processes, doing anything more involved is assumed to have poor ROI. Check out an example of [how we poll for a result](https://github.com/dotnet/razor/blob/main/src/Razor/test/Microsoft.VisualStudio.Razor.IntegrationTests/InProcess/EditorInProcess_Outlinning.cs#L26).
1. Verify your scenario. Sometimes the act of waiting is enough to verify the scenario, because there is a timeout we use which will prevent a test waiting forever, and also result it a failure.

## Troubleshooting

- When adding a new test it's absolutely vital that you run it multiple times (5-10+) before even sending the PR since the most frequent issue in our Integration tests is flakyness. If you see any "random" failures while testing check `artifacts\log\Debug\Screenshots` for relevant logs and diagnostics.
- Ensure you have deployed `Microsoft.VisualStudio.RazorExtension.Dependencies` if you're using a Preview version of VS, or have not deployed it if you're running VS IntPreview or main. If you've any doubt I recommend just checking the hive folder under `Extensions\Microsoft\Razor Extension Dependencies`.
- If you're suddenly seeing strange packaging issues it's likely your "Hive" is corrupted somehow. I recommend deleting all the folders containing `RoslynDev` from `AppData\Local\Microsoft\VisualStudio`
- If the test did not produce enough logs/diagnostics to know why it failed it's time to [add/capture some more](https://github.com/dotnet/razor/blob/main/src/Razor/test/Microsoft.VisualStudio.Razor.IntegrationTests/VisualStudioLogging.cs).
- When in doubt, ask for help!

## Adding New Capabilities

If you're creating the first test in a given area chances are that no helper functions exist to get information out of VS, so you'll have to add stuff to our ["InProcess" classes](https://github.com/dotnet/razor/tree/main/src/Razor/test/Microsoft.VisualStudio.Razor.IntegrationTests/InProcess).

These are code-generated classes which get injected onto the "TestServices" object in your tests.

The real trick usually lies in identifying the specific VS APIs and Services to use when adding capabilities to the [Integration framework](https://github.com/dotnet/razor/blob/main/src/Razor/test/Microsoft.VisualStudio.Razor.IntegrationTests/InProcess/EditorInProcess_Outlinning.cs). A good first start is usually to check the [Roslyn Integration tests](https://github.com/dotnet/roslyn/tree/main/src/VisualStudio/IntegrationTest/New.IntegrationTests) and see if they do something similar. When in doubt "rip them off" (this is actually good since it may mean that some functionality gets promoted up into vs-extension-testing). If that doesn't find you what you need check some of the old APEX tests for hints. As a general rule we avoid using `EnvDTE`, in favor of more modern `IVS*` services.

## Limitations

Generally development in Razor targets the VS main branch but we can't build against VS main for public repos due to policy. We must therefore be able to run the Integration tests against both VS preview and VS main at any given time, which can lead to to package versioning issues. We try to work around them with the `Microsoft.VisualStudio.RazorExtension.Dependencies` package, by forcibly deploying specific dependencies to the VS experimental hive that we will use for testing. That can be finicky and can break when there is a new VS preview released or one of our dependencies makes a breaking change in a DLL we are not redirecting.

## Running

If you run the integration tests from the command line using something like "dotnet test" they may fail because the Extension was not deployed. To ensure the extension was deployed you can either launch tests through Visual Studio or first run a command like `eng\cibuild.cmd -configuration Debug -msbuildEngine vs -prepareMachine`
