namespace Microsoft.CodeAnalysis.Debugging
{
    /// <summary>
    /// The kinds of custom debug info that we know how to interpret.
    /// The values correspond to possible values of the "kind" byte
    /// in the record header.
    /// </summary>
    internal enum CustomDebugInfoKind : byte
    {
        UsingGroups = 0,
        ForwardMethodInfo = 1,
        ForwardModuleInfo = 2,
        StateMachineHoistedLocalScopes = 3,
        ForwardIteratorInfo = 4,
        DynamicLocals = 5,
        EditAndContinueLocalSlotMap = 6,
        EditAndContinueLambdaMap = 7,
        TupleElementNames = 8,
    }
}
