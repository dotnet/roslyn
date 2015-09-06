### High Level Language Proposal:  
Allow constant implicitly typed local variables.  i.e.

```c#
const var v = "Hello World";
```

#### Specification Change

> A local-constant-declaration declares one or more local constants.  
local-constant-declaration:  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;~~const   type   constant-declarators~~  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;+const   local-variable-type   constant-declarators  

> +The type of a local implicitly typed constant declaration a  must follow the same rules as those of an implicitly typed declaration (§8.5.1).
