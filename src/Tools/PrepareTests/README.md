# Prepare Tests

## Usage
This tool is meant to prepare our unit tests for efficient upload and download 
in our CI pipeline. Our build and test legs run on different machines and hence
they must go through a single upload cycle and many download cycles (per test
scenario).

The output of our build is ~11GB and that does not really lend itself to being 
uploaded and downloaded as often as is needed in our tests. Some amount of 
curation is needed to make that a practical experience. Even if it's as simple
as deleting all of the non-unit test directories from our `bin` folder and 
using the results as the payload.

Our eventual goal though is to be running on Helix and that environment is 
optimized for a specific payload structure. Helix consists of Jobs which have 
a collection of associated Work Items attached to it. Helix will fan out 
Work Items to a series of machines but it will attempt to schedule many 
Work Items for the same Job in sequence on the same machine. 

To support that scheduling Helix wants the payloads structured in the following
manner:

1. Correlated payload: there is one per job and that is on disk for every 
Work Item in the Job. Given that the schedule attempts to re-use the same machine
for Work Items in a Job this means the correlated payload is only downloaded 
based on the number of machines used, not the number of Work Items scheduled.
1. Work Item payload: this is downloaded whenever a work item is executed. There
is no re-use here hence this should be as small as possible.

In Roslyn terms the Job is effectively a single unit test DLL and the Work Items
are the partitions that RunTests creates over them. Although in Helix there will
be a lot more partitions.

This tool effectively optimizes our payload for the Helix case. All of the 
duplicate files in our build are structured into a single payload. That will 
eventually become our correlation payload. What is left in the unit test 
directory is unique to that test and hence is about as minimum as it can get.

Given that the end goal is Helix, and we need some sort of test data 
manipulation now, I thought it best to just insert that tool here rather than 
having an intermediate step. 

## Implementation
This tool recognizes that a large number of the artifacts in our output 
directory are duplicates. For example consider how many times 
Microsoft.CodeAnalysis.dll gets copied around during our build (quite a bit).

The tool uses that information to the following effect:

1. Create a payload directory, `testPayload`, that the tool will populate
1. Crack every DLL in the `bin` directory, read it's MVID, and keep a list
of all file paths which are this MVID
1. Create a directory, `.duplicates`, in `testPayload`
1. For each MVID that has multiple copies create a hard link in `.duplicates` 
where the name is the MVID. 
1. For every other file in `bin` which is not a duplicate create a hard link
in `.duplicates` with the same relative path.
1. Create a file, `rehydrate.ps1`, that will restore all the duplicate files
by creating a hard link into `.duplicates`. This file will be run on the test
machine.

This reduces our test payload size to ~1.5GB.

*Note*: yes in many ways this is similar to hard linking during build. The 
difference being that this is much more effective because build hard linking 
isn't perfect and also it creates a handy correlation payload for us.