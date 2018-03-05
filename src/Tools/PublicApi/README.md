Mark Shipped Tool
========

This tool should be run after every supported release that has API changes. It will 
merge the collection of PublicApi.Shipped.txt files with the PublicApi.Unshipped.txt 
versions.  This will take into account `*REMOVED*` elements when updating the files.

Usage:

``` cmd
mark-shipped.cmd
```
