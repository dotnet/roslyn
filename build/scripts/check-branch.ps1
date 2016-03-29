# This script is meant to guard against accidental merges of our core
# branches.  For instance it should guard against future -> master merges
# that weren't intended.  

if (${env:ghprbTargetBranch} -eq "master") {
    [string]$output = git branch --contains 00677493dc69ceac4e9e4c01a73be90c75e7d340 
    if ($output.Length -ne 0) {
        write-host "Error!!! Accidental merge of future into master"
        exit 1
    }
}

