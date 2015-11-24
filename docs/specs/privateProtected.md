# New protection level private protected

https://github.com/dotnet/roslyn/issues/1384

The C# and VB compilers now accept the combination of modifiers `private protected` (for C#) and `Private Protected` (for VB).  This corresponds to the CLR protection level "FamilyANDAssembly". A member with this access level is accessible only in derived types within the same assembly. It has essentially the same meaning as `private protected` in [C++/CLI](https://msdn.microsoft.com/en-us/library/ke3a209d.aspx#BKMK_Member_visibility). 
