
using System.Runtime.CompilerServices;

// specifies "high confidence" that this assembly is going to be used by whatever references it
// in theory it should work as a hint to ngen to skip some indirections when ngen-ing callers
[assembly: DefaultDependencyAttribute(LoadHint.Always)]