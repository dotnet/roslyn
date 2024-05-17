# Target Framework Strategy

## Layers

The roslyn repository produces components for a number of different products that push varying ship and TFM constraints on us. A summary of some of our dependencies are : 

- Build Tools: requires us to ship compilers on `net472`
- .NET SDK: requires us to ship compilers on current servicing target framework (presently `net8.0`)
- Source build: requires us to ship `$(NetCurrent)` and `$(NetPrevious)` in workspaces and below (presently `net9.0` and `net8.0` respectively)
- Visual Studio: requires us to ship `net472` for base IDE components and `$(NetVisualStudio)` (presently `net8.0`) for private runtime components.
- Visual Studio Code: expects us to ship against the same runtime as DevKit (presently `net7.0`) to avoid two runtime downloads.
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




