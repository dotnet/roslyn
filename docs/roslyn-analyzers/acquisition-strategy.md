# Roslyn Analyzer acquisition strategy

This document describes how consumers should acquire the **Microsoft.CodeAnalysis.* (Roslyn) analyzer packages** over time, and why the acquisition model changes across major versions.

## Goals

1. **Reliable analyzer execution**: analyzers should run on a compiler/host that they are compatible with.
2. **Lower maintenance cost**: reduce long-term burden of supporting extremely old compiler hosts.
3. **Simpler acquisition**: move from “add NuGet packages” toward “enable via MSBuild properties”, where possible.
4. **Version alignment**: analyzers should be **version-matched to the compiler** that executes them.

---

## Recommended guidance

### If you need maximum host compatibility
- Prefer **<= 3.11.0** analyzer packages built from `dotnet/roslyn-analyzer`, especially when builds must run on older compilers/hosts.

### If you are on modern SDKs and want the newest analyzer line
- Use **5.0+** analyzer packages, ensuring your build uses the **.NET 9 SDK or higher**.

### Plan for the SDK-based future
- Expect a transition from `PackageReference`-based acquisition to **SDK-provided analyzers** enabled by configuration properties.
- Prefer central configuration (for example, `Directory.Build.props`) to ease future migration.

---

## Background and timeline

### Analyzer packages <= 3.11.0

For package versions **less than or equal to 3.11.0**:

- The analyzer packages were built from the **`dotnet/roslyn-analyzer`** repository.
- These versions were designed to support running on **very old Roslyn compilers and compiler hosts** (for example, older Visual Studio / older SDK toolsets).
- This required significant **backwards-compatibility code and testing**, increasing maintenance overhead.

**Implication for consumers:** these packages were broadly compatible with older toolchains, making them a good choice when analyzer execution had to work across older environments.

---

### Analyzer packages 5.0+ (post-merge into dotnet/roslyn)

The **`dotnet/roslyn-analyzer`** repository was merged into **`dotnet/roslyn`**. As part of that consolidation:

- A large amount of the special-case compatibility logic for very old hosts was **removed** to reduce maintenance burden.
- The newer packages adopt a compatibility posture aligned with modern SDK/compiler shipping.

For the new **5.0** packages:

- They **require the compiler from Visual Studio 17.12/.NET 9 SDK or higher**.

**Implication for consumers:** using 5.0 analyzers assumes your build uses a sufficiently new compiler/SDK. If your build environment is older, these analyzers are not a supported choice.

---

## Current acquisition model (today)

Today, acquisition is typically done via **NuGet PackageReference**, for example:

- Add analyzer packages to a project (`.csproj`) or centrally in `Directory.Build.props`.
- Ensure the build environment uses a compiler/SDK new enough to run that analyzer version (notably for 5.0+).

This model works but has downsides:

- Consumers must discover and choose package versions.
- Version skew can happen: analyzers may be newer/older than the compiler executing them.
- Upgrading analyzers can implicitly require upgrading the SDK/compiler host.

---

## Future direction: ship analyzers in the .NET SDK

The long-term goal is to ship these analyzer packages **in the .NET SDK**. Currently Roslyn ships their CodeStyle analyzers in the SDK. Consumers opt-in to using them via the `EnforceCodeStyleInBuild` property.

### Why this is better

When analyzers ship in the SDK:

- The analyzers can be **version-matched to the compiler** automatically.
- The acquisition experience becomes simpler:
  - instead of adding `PackageReference`, you enable analyzers by setting **MSBuild properties** in your `.csproj` or `Directory.Build.props`.
- The SDK can ensure the analyzer/compiler pairing is known-good, reducing compatibility surprises.

### What this will look like for consumers

Conceptually, consumers will move from:

- “Add analyzer packages via NuGet”

to:

- “Enable analyzers via properties”

configured in one of:

- a project file (`.csproj`)
- `Directory.Build.props` (recommended for repository-wide policy)

*(Exact property names and final UX may evolve as the SDK integration is finalized.)*
