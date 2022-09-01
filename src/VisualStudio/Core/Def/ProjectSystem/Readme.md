# Locking and Synchronization

There are two core types in this folder, each of which has their own lock.

Each project from the project system is represented by a VisualStudioProject which has as SemaphoreSlim _gate. This lock is taken
any time a mutation happens to that individual project, to ensure that the type itself is safe to use concurrently. The expectation
though is that simultaneous use from multiple threads by a project system isn't common, so no real effort has been expended to try
to make the locking fine-grained there; each function just acquires the _gate and then does what it needs to do.

The workspace (VisualStudioWorkspaceImpl) also has it's own SemaphoreSlim _gate. This is acquired any time a change is being made
to the workspace. As much as possible, try to acquire this gate in an asynchronous fashion, since during solution load this lock
will have a lot of things trying to acquire it at once on a bunch of threads, and we may starve off the thread pool if we're not
careful. Unfortunately however, we still have legacy project systems that aren't async friendly; they still may apply changes or
batches synchronously so in those cases we still acquire the gate in a synchronous fashion.

There is a strict lock hierarchy: a VisualStudioProject may try to acquire the workspace lock while holding it's lock,
but to prevent deadlocks a holder of the VisualStudioWorkspaceImpl lock should never call a function on VisualStudioProject that
would acquire a project lock. To this end, a few bits of information that may seem to be "project specific" are actually stored
in maps in the VisualStudioWorkspaceImpl; specifically we maintain a list of the output paths of a project which we use to convert
metadata references to project references. This list is maintained in the workspace itself to avoid having to reach back to a
project and ask it for information which might violate this lock hierarchy.

When a VisualStudioProject needs to make a change to the workspace, there's a number of Apply methods that can be called that
acquire the global workspace lock and then call a lambda to do the work that's needed. In some cases there are public methods on
VisualStudioWorkspaceImpl which are suffixed wtih _NoLock; these exist to be called inside one of these Apply methods; they all
assert that the workspace lock is already being held.

There is a nested class of VisualStudioProject called BatchingDocumentCollection which manages all of logic around adding and removing
documents, and dealing with changes. The nested class exists simply because each project has multiple sets of documents (regular
documents, additional files, and .editorconfig files) that all behave the same way, so this allows for a common abstraction
to reuse most of the logic. A BatchingDocumentCollection does not have a lock of it's own, it just acquires the VisualStudioProject
lock whenever needed.

There's a few ancillary types that also have their own locks:

VisualStudioProjectOptionsTracker is a helper type which takes compiler command line strings and converts it to ParseOptions and
CompilationOptions. It holds onto a VisualStudioProject, and may call VisualStudioProject methods while holding it's lock. Nothing
else holds onto a VisualStudioProjectOptionsTracker that has a lock, so we avoid any deadlocks there.

VisualStudioWorkspaceImpl has a nested type OpenFileTracker that has it's own lock to guard it's own fields. It should call nothing
outside of itself while holding that lock.