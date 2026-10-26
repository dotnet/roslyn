# External Access Policies

## API Tracking

We track the APIs that we've exposed with a roslyn analyzer, which will fail the build if APIs are removed. Unlike our public APIs, we are allowed to have breaking changes here, but these will often cause insertion failures. Any removal of an existing API (whether the API is fully removed, or the parameters/return changed) needs to have sign off from the current infrastructure rotation, and will potentially require a test insertion before merging to ensure VS isn't broken, or coordinated insertion with the affected partner team.

Because "shipping" for EA projects occurs every time a change is checked in, we don't bother updating the `InternalAPI.Shipped.txt` file for these projects.

Every EA project can have an `Internal` namespace that is considered Roslyn implementation details. We do not track the APIs under these namespaces, and it's on the dependent projects to not reference anything from these namespaces. Not every EA project actually uses this namespace. If an EA project that doesn't currently use its `Internal` namespace wants to start, modify the corresponding `.editorconfig` to set the `dotnet_public_api_analyzer.skip_namespaces` key to that namespace for the affected files. Multiple namespaces can be included by using a `,` separator.
