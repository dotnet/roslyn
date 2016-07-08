# Check if the branch contains any bad commits we want to prevent reappearing

[string]$output = git branch --contains 29a0db828359046e61542c4b62fa72743a905a40
if ($output.Length -ne 0) {
    write-host "Error!!! Branch still contains something we got rid of in a force push."
    exit 1
}
