Win32 Resources
===============

The compiler accepts these native resource types:
 - Arbitrary data in a file (presumed to be text) via /win32manifest. This data is added to the output binary as a resource with type #24 and number #2 for a DLL and #1 for an EXE.
 - A resource file (.RES) whose format is defined [here](http://msdn.microsoft.com/en-us/library/ms648007(VS.85).aspx) via /win32res
 - An icon whose format is described [here](http://msdn.microsoft.com/en-us/library/ms997538.aspx) via /win32icon
	
Specifying either /win32icon or /win32manifest along with /win32res is an error. The user is expected to either supply all of the resources (via /win32res) or parts of them (with the other switches), but the compiler doesn't combine the contents of a user-supplied .RES file with individual resources.

A version resource is added to the output unless /win32res is specified. In the absence of the attributes that affect version numbers, the compiler constructs a default version number.
