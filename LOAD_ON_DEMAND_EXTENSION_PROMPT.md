# C# Extension Implementation Prompt: Load Projects On Demand

## Feature Overview

The Roslyn LSP server now supports **load-projects-on-demand** mode, which defers project loading until a file requires project-backed semantics. This reduces startup time by avoiding loading all projects upfront.

### Key Points for Extension Implementation

**Option:** `dotnet.projects.loadOnDemand` (type: `boolean`, default: `true`)
- **Description:** When enabled, the server scans workspace folders for .csproj files at startup and loads projects on demand when files require project-backed semantics

## Extension Implementation Requirements

### When LoadProjectsOnDemand is TRUE

**DO NOT:**
- Call `OpenProject()` or `OpenSolution()` during initialization
- Attempt to load all projects in workspace folders
- Block startup waiting for project discovery

**DO:**
- Skip automatic project loading
- Let the LSP server discover and load projects on demand
- Handle requests for files that may not yet have projects loaded
- Trust that `IOnDemandProjectLoader` will handle discovery when needed

### When LoadProjectsOnDemand is FALSE

**DO:**
- Restore traditional behavior: call `OpenProject()` or `OpenSolution()` as usual
- Load all configured solutions/projects at startup
- This is the opt-out path for users who prefer upfront loading

## Testing Considerations

The extension should test:
1. Startup performance with option enabled (no upfront project loading)
2. File editing and IntelliSense work correctly for on-demand loaded projects
3. Multiple projects in same workspace are discovered and loaded correctly
4. Disabling the option restores traditional upfront loading behavior
5. Mixed scenarios: manually opened projects + on-demand discovered projects

## LSP Server Guarantees

When `LoadProjectsOnDemand` is enabled, the LSP server:
- ✅ Discovers workspace `.csproj` files efficiently
- ✅ Finds the correct project for any file path
- ✅ Loads projects on first document access
- ✅ Deduplicates project loads
- ✅ Handles files with no matching project (miscellaneous files)
- ✅ Works correctly with nested workspaces

## Summary

Implement the extension to:
1. Read the `LoadProjectsOnDemand` option at startup
2. Skip `OpenProject`/`OpenSolution` calls when the option is `true`
3. Let the LSP server handle project discovery and loading automatically
4. Preserve traditional behavior when the option is `false`

The key insight: **When on-demand mode is enabled, don't initiate project loading—let the server do it in response to file access.**
