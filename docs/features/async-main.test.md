
# Public interface of compiler APIs
 -N/A?

# General functionality
- `Task` and `Task<int>` returns are allowed. Do we require `async`.
- Multiple valid/invalid async candidates.
- process exit code on success (void and int) and on exception
- `/main:<type>` cmd switch is still functional

# Old versions, compat
 - langver
 - when both async and regular Main avaialble. With old/new compiler versions.
 - async void Main. With old.new langver. With another applicable Main present.

# Type and members
- Access modifiers (public, protected, internal, protected internal, private), static modifier
- Parameter modifiers (ref, out, params)
- STA attribute
- Partial method
- Named and optional parameters
- `Task<dynamic>`
- Task-Like types
 
# Debug
- F11 works
- stepping through Main
