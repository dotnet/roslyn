Main areas to test:
* Signature acceptance / rejection
* Method body creation

# Signature acceptance / rejection

## Single Mains
Classes that contain only a single "main" method

### Single legal main
* Past: Ok
* New 7: Ok
* New 7.1 Ok

### Single async (void) main
* Past: ERR (Async can't be main)
* New 7: ERR (Update to get this to work)
* New 7.1: Ok

### Single async (Task) main
* Past: ERR (No entrypoints found), WARN (has the wrong signature to be an entry point)
* New 7: ERR (Update to get this to work)
* New 7.1 Ok

### Single async (Task<int>) main
* Past: ERR (No entrypoints found), WARN (has the wrong signature)
* New 7: ERR (Update to get this to work)
* New 7.1: Ok

## Multiple Mains
Classes that contain more than one main

### Multiple legal mains
* Past: Err
* New 7: Err
* New 7.1: Err

### Single legal main, single async (void) main
* Past: Err (an entrypoint cannot be marked with async)
* New 7: Err (new error here?)
* New 7.1: Err (new error here? "void is not an acceptable async return type")

### Single legal main, single legal Task main
* Past: Ok (warning: task main has wrong signature)
* New 7: Ok (new warning here)
* New 7.1: Ok (new warning here?)

### Single legal main, single legal Task<int> main
* Past: Ok (warning: task main has wrong signature)
* New 7: Ok (new warning here)
* New 7.1: Ok (new warning here?)

# Method body creation

* Inspect IL for correct codegen.
* Make sure that attributes are correctly applied to the synthesized mains.

## Broken methods on Task and Task<T>

* GetAwatier or GetResult are missing
* GetAwaiter or GetResult don't have the required signature
* Has the right shape, but types are missing

## Task or Task<T> is missing

This will be caught during entrypoint detection, should be a binding error.

## Void or int is missing

If Task can be found, but void or int can't be found, then the compiler should behave gracefully. 

# Vlad Test Plan

## Public interface of compiler APIs
  -N/A?
 
## General functionality
- `Task` and `Task<int>` returns are allowed. Do we require `async`.
- Multiple valid/invalid async candidates.
- process exit code on success (void and int) and on exception
- `/main:<type>` cmd switch is still functional
 
## Old versions, compat
- langver
- when both async and regular Main avaialble. With old/new compiler versions.
- async void Main. With old.new langver. With another applicable Main present.
 
## Type and members
- Access modifiers (public, protected, internal, protected internal, private), static modifier
- Parameter modifiers (ref, out, params)
- STA attribute
- Partial method
- Named and optional parameters
- `Task<dynamic>`
- Task-Like types
  
## Debug
- F11 works
- stepping through Main
