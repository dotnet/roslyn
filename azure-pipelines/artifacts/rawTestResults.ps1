if ($env:AGENT_TEMPDIRECTORY) {
    $xunitRoot = "$($env:AGENT_TEMPDIRECTORY)\xUnitResults"
} elseif ($env:RUNNER_TEMP) {
    $xunitRoot = "$($env:RUNNER_TEMP)\xUnitResults"
} else {
    return
}

@{
    "$xunitRoot" = (
        (Get-ChildItem "$xunitRoot" -Recurse)
    );
}
