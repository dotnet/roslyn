class Test
{
    void Method()
    {
        ErrorCode code = overridingMemberIsObsolete
            ? ErrorCode.WRN_ObsoleteOverridingNonObsolete
            : ErrorCode.WRN_NonObsoleteOverridingObsolete;
    }
}
