# Target Framework Strategy

## Layers

The roslyn repository produces components for a number of different products that push varying ship and TFM constraints on us. A summary of some of our dependencies are : 

- Build Tools: requires us to ship compilers on `net472`
- .NET SDK: requires us to ship compilers on current servicing target framework (presently `net9.0`)
- Repository Source build: requires us to ship `$(NetCurrent)` and `$(NetPrevious)` in workspaces and below (presently `net10.0` and `net9.0` respectively). This is because the output of repository source build is an input to other repository source build and those could be targeting either `$(NetCurrent)` or `$(NetPrevious)`.
- Full Source build: requires us to ship `$(NetCurrent)`
- Visual Studio: requires us to ship `net472` for base IDE components and `$(NetVisualStudio)` (presently `net8.0`) for private runtime components.
- Visual Studio Code: expects us to ship against the same runtime as DevKit (presently `net10.0`) to avoid two runtime downloads.
- MSBuildWorkspace: requires to ship a process that must be usable on the lowest supported SDK (presently `net6.0`)

It is not reasonable for us to take the union of all TFM and multi-target every single project to them. That would add several hundred compilations to any build operation which would in turn negatively impact our developer throughput. Instead we attempt to use the TFM where needed. That keeps our builds smaller but increases complexity a bit as we end up shipping a mix of TFM for binaries across our layers.

## Picking the right TargetFramework

Projects in our repository should include the following values in `<TargetFramework(s)>` based on the rules below:

1. `$(NetRoslynSourceBuild)`: code that needs to be part of source build. This property will change based on whether the code is building in a source build context or official builds. 
  a. In official builds this will include the TFMs for `$(NetVSShared)`
  b. In source builds this will include `$(NetRoslyn)`
2. `$(NetVS)`: code that needs to execute on the private runtime of Visual Studio.
3. `$(NetVSCode)`: code that needs to execute in DevKit host
4. `$(NetVSShared)`: code that needs to execute in both Visual Studio and VS Code but does not need to be source built.
5. `$(NetRoslyn)`: code that needs to execute on .NET but does not have any specific product deployment requirements. For example utilities that are used by our infra, compiler unit tests, etc ... This property also controls which of the frameworks the compiler builds against are shipped in the toolset packages. This value will potentially change in source builds.
6. `$(NetRoslynAll)`: code, generally test utilities, that need to build for all .NET runtimes that we support.
7. `$(NetRoslynBuildHostNetCoreVersion)`: the target used for the .NET Core BuildHost process used by MSBuildWorkspace.
8. `$(NetRoslynNext)`: code that needs to run on the next .NET Core version. This is used during the transition to a new .NET Core version where we need to move forward but don't want to hard code a .NET Core TFM into the build files.

This properties `$(NetCurrent)`, `$(NetPrevious)` and `$(NetMinimum)` are not used in our project files because they change in ways that make it hard for us to maintain corect product deployments. Our product ships on VS and VS Code which are not captured by arcade `$(Net...)` macros. Further as the arcade properties change it's very easy for us to end up with duplicate entries in a `<TargetFarmeworks>` setting. Instead our repo uses the above values and when inside source build or VMR our properties are initialized with arcade properties.

**DO NOT** hard code .NET Core TFMs in project files. Instead use the properties above as that lets us centrally manage them and structure the properties to avoid duplication. It is fine to hard code other TFMs like `netstandard2.0` or `net472` as those are not expected to change.

**DO NOT** use `$(NetCurrent)` or `$(NetPrevious)` in project files. These should only be used inside of `TargetFrameworks.props` to initialize the above values in certain configurations.

## Require consistent API across Target Frameworks

It is important that our shipping APIs maintain consistent API surface area across target frameworks. That is true whether the API is `public` or `internal`.

The reason for `public` is standard design pattern. The reason for `internal` is a combination of the following problems:

- Our repository makes use of `InternalsVisibleTo` which allows other assemblies to directly reference signatures of `internal` members.
- Our repository ships a mix of target frameworks. Typically workspaces and below will ship more recent TFMs than the layers above it. Compiler has to ship newer TFM for source build while IDE is constrained by Visual Studio's private runtime hence adopts newer TFM slower.
- Our repository invests in polyfill APIs to make compiling against multiple TFMs in the same project a seamless experience.

Taken together though this means that our `internal` surface area in many cases is effectively `public` when it comes to binary compatibility. For example a consuming project can end up with `net7.0` binaries from workspaces layer and `net6.0` binaries from IDE layer. Because there is `InternalsVisibleTo` between these binaries the `internal` API surface area is effectively `public`. This requires us to have a consistent strategy for achieving binary compatibility across TFM combinations.

Consider a specific example of what goes wrong when our `internal` APIs are not consistent across TFM:

- Workspaces today targets `net6.0` and `net7.0` and it contains our `EnumerableExtensions.cs` which polyfills many extensions methods on `IEnumerable<T>`. In `net7.0` the `Order` extension method is not needed because it was put into the .NET core libraries.
- Language Server Protocol targets `net6.0` and consumes the `Order` polyfill from Workspaces

Let's assume for a second that we `#if` the `Order` method such that it's not present in `net7.0`.  Locally this all builds because we compile the `net6.0` versions against each other so they're consistent. However if an external project which targets `net7.0` and consumes both Workspaces and Language Server Protocol then it will be in a broken state. The Protocol binary is expecting Workspaces to contain a polyfill method for `Order` but it does not since it's at `net7.0` and it was `#if` out. As a result this will fail at runtime with missing method exceptions.

This problem primarily comes from our use of polyfill APIs. To avoid this we employ the following rule:

> When there is a `#if` directive that matches the regex `#if !?NET.*` that declares a non-private member, there must be an `#else` that defines an equivalent binary compatible symbol

This comes up in two forms:

### Pattern for types

When creating a polyfill for a type use the `#if !NET...` to declare the type and in the `#else` use a `TypeForwardedTo` for the actual type.

Example: 

```csharp
#if NET6_0_OR_GREATER

using System.Runtime.CompilerServices;

#pragma warning disable RS0016 // Add public types and members to the declared API (this is a supporting forwarder for an internal polyfill API)
[assembly: TypeForwardedTo(typeof(IsExternalInit))]
#pragma warning restore RS0016 // Add public types and members to the declared API

#else

using System.ComponentModel;

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit
    {
    }
}

#endif
```

### Pattern for extension methods

When creating a polyfill for an extension use the `#if NET...` to declare the extension method and the `#else` to declare the same method without `this`. That will put a method with the expected signature in the binary but avoids it appearing as an extension method within that target framework.

```csharp
#if NET7_0_OR_GREATER
        public static IOrderedEnumerable<T> Order<T>(IEnumerable<T> source) where T : IComparable<T>
#else
        public static IOrderedEnumerable<T> Order<T>(this IEnumerable<T> source) where T : IComparable<T>
#endif
```

## Transitioning to new .NET SDK

As the .NET team approaches releasing a new .NET SDK the Roslyn team will begin using preview versions of that SDK in our build. This will often lead to test failures in our CI system due to underlying behavior changes in the runtime. These failures will often not show up when running in Visual Studio due to the way the runtime for the test runner is chosen.

To ensure we have a simple developer environment such project should be moved to to the `$(NetRoslynNext)` target framework. That ensures the new runtime is loaded when running tests locally.

When the .NET SDK RTMs and Roslyn adopts it all occurrences of `$(NetRoslynNext)` will be moved to simply `$(NetRoslyn)`.

**DO NOT** include both `$(NetRoslyn)` and `$(NetRoslynNext)` in the same project unless there is a very specific reason that both tests are adding value. The most common case is that the runtime has changed behavior and we simply need to update our baselines to match it. Adding extra TFMs for this just increases test time for very little gain.

## Checklist for updating TFMs (once a year)

- Update `TargetFrameworks.props`.
  - Ensure we have the correct TFM for VS / VSCode (usually not the latest TFM).
- Might need updating `MicrosoftNetCompilersToolsetVersion` in `eng\Versions.props` to consume latest compiler features.
- Change `$(NetRoslynNext)` references to `$(NetRoslyn)` in project files.
- Update TFMs in code/scripts/pipelines (search for the old `netX.0` and replace with the new `netY.0`).
- Replace `#if NET7_0_OR_GREATER` corresponding to the no longer used TFM (`net7.0` in this example) with the lowest used TFM (e.g., `net8.0`: `#if NET8_0_OR_GREATER`) and similarly the negated variant `#if !NET7_0_OR_GREATER` with `#if !NET8_0_OR_GREATER`. See https://github.com/dotnet/roslyn/issues/75453#issuecomment-2405500584.
- Check that the same number of tests still run in CI (they are not unintentionally filtered out by TFM).
- Try an official build, a VS insertion.
