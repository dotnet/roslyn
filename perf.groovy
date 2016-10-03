// Groovy Script: http://www.groovy-lang.org/syntax.html
// Jenkins DSL: https://github.com/jenkinsci/job-dsl-plugin/wiki

import jobs.generation.*;

def generate(boolean isPr) {
    // The input project name (e.g. dotnet/corefx)
    def projectName = GithubProject
    // The input branch name (e.g. master)
    def branchName = GithubBranchName
    def defaultBranch = "*/${branchName}"

    def jobName = Utilities.getFullJobName(projectName, "perf_run", isPr)
    def myJob = job(jobName) {
        description('perf run')

        steps {
            batchFile("""powershell -File ./build/scripts/run_perf.ps1""")
        }

        publishers {
            postBuildScripts {
                steps {
                    batchFile("""powershell -File ./build/scripts/cleanup_perf.ps1 -ShouldArchive""")
                }
            }
        }
    }

    def archiveSettings = new ArchivalSettings()
    archiveSettings.addFiles('ToArchive/**/*.*')
    Utilities.addArchival(myJob, archiveSettings)
    Utilities.standardJobSetup(myJob, projectName, isPr, defaultBranch)
    Utilities.setMachineAffinity(myJob, 'Windows_NT', 'latest-or-auto-perf')
    Utilities.addGithubPushTrigger(myJob)

    if (isPr) {
        // Utilities.addGithubPRTriggerForBranch(newJob, branch, "Windows ${configuration}")
        TriggerBuilder prTrigger = TriggerBuilder.triggerOnPullRequest()
        prTrigger.permitOrg('Microsoft')
        prTrigger.permitOrg('dotnet')
        prTrigger.setCustomTriggerPhrase("(?i)^\\s*(@dotnet-bot\\s+)?test\\s+perf(\\s+please)?\\s*\$" )
        prTrigger.triggerForBranch('master');
        prTrigger.setGithubContext('Performance Test Run')
        prTrigger.emitTrigger(myJob)
    }
    else {
        Utilities.addGithubPushTrigger(myJob)
    }
}

generate(true)
generate(false)
