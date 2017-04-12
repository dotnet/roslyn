
# Adding optional parameters to public methods #

Roslyn has strict requirements for maintaining source and binary API compatibility across minor version releases.
Adding overloads of existing methods can be an effective way of adding to the public API without breaking
backwards compatibility. However, overloads involving optional parameters can be tricky to make both source
and binary compatible. The following steps should be followed when adding optional parameters to public methods.

## Steps for adding optional parameters to an existing method ##

1.	Check to see if there is already a method marked with the comment 
    
    ```
    // <Previous release> BACKCOMPAT OVERLOAD -- DO NOT TOUCH
    ```
2.	If it does not:
    1. Copy the method and make any existing optional parameters mandatory. 
    2. Change the body to call the original method. 
    3. Add the comment `// <Previous release> BACKCOMPAT OVERLOAD -- DO NOT TOUCH`. 
       This method provides binary compatibility with assemblies compiled against the previous APIs.
3.	Add your new parameter with a default value to the *end* of the original method. 
    This provides source compatibility to consumers of the previous API. 

## Examples ##

Before:
```csharp
    public O(string o1 = null, string o2 = null)
    {
    }
```

After:

```csharp
    public void O(string o1 = null, string o2 = null, bool o3 = false)
    {
    }

    // 1.0 BACKCOMPAT OVERLOAD -- DO NOT TOUCH
    public void O(string o1, string o2)
    {
        O(o1, o2, o3: false);
    }
```

## Things to avoid ##

**DO NOT add a parameter in the middle of existing optional parameters**

```csharp
    public void O(string o1 = null, bool o4 = false, string o2 = null, bool o3 = false, bool o5 = false)
    {
    }
```

This breaks source compat when certain arguments to a method call are skipped, like `O(null, null, o5: true);`

**DO NOT add multiple overloads with optional parameters**

```csharp
    public void O(string o1 = null, string o2 = null, bool o3 = false)
    {
    }

    // 1.0 BACKCOMPAT OVERLOAD -- DO NOT TOUCH
    public void O(string o1 = null, string o2 = null)
    {
         O(o1, o2, o3: false);
    }
```

Aside from the fact that this is often ambiguous and it won’t compile, it makes verifying
compatibility much harder.

## End Result ##

The resulting code should 1) have only one overload with optional parameters, which 2) has the 
most parameters of all the overloads, and 3) all previous overloads should still exist, 
properly commented with their release version, and contain only mandatory parameters.

## NOTE ##

After this change, if Public API Analyzer complains about it, you should copy the entry for your change from PublicAPI.Shipped.txt and then put that entry in PublicAPI.Unshipped.txt with `*REMOVED*` prefix.

PublicAPI.Shipped.txt

``` txt
Example.O(string o1 = null, string o2 = null) -> void
```

PublicAPI.Unshipped.txt

``` txt
*REMOVED*Example.O(string o1 = null, string o2 = null) -> void
```
