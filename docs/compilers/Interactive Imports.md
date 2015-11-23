# Imports in Scripts/Interactive

## Guiding principles

Files includes by ```#load``` are each in a separate lexical scope.
Declarations (including aliases) take precedence over usings (including using static) because they express intent.

## Process

Steps (top to bottom) | Binding Using | Binding Code | #load
----------------------|---------------|--------------|------
Look for declarations in the current submission/file (including aliases) | Yes, excluding aliases | Yes, in submission | Yes, in file
Look for declarations in preceding submissions (including aliases), in order | Yes | Yes | No
Look for members in the host object (if any) | Yes | Yes | Yes
Look for declarations in the global namespace | Yes | Yes | Yes
Look in usings (except aliases) in the current submission/file and all preceding submissions (all at once, not looping) | No | Yes, in all submissions | Yes, in file only
Look in the global imports of the current submission and all preceding submissions (all at once, not looping) | No | Yes | Yes
