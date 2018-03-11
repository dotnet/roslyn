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

        wrappers {
            credentialsBinding {
                string('BV_UPLOAD_SAS_TOKEN', 'Roslyn Perf BenchView Sas')
            }
        }

        steps {
            batchFile("""powershell -File ./build/scripts/run_perf.ps1""")
        }
    }

    def archiveSettings = new ArchivalSettings()
    archiveSettings.addFiles('ToArchive/**/*.*')
    Utilities.addArchival(myJob, archiveSettings)
    Utilities.standardJobSetup(myJob, projectName, isPr, defaultBranch)
    Utilities.setMachineAffinity(myJob, 'Windows_NT', 'latest-or-auto-perf')

    if (isPr) {
        TriggerBuilder prTrigger = TriggerBuilder.triggerOnPullRequest()
        prTrigger.permitOrg('Microsoft')
        prTrigger.permitOrg('dotnet')
        prTrigger.setCustomTriggerPhrase("(?im)^\\s*(@dotnet-bot\\s+)?(re)?test\\s+perf(\\s+please)?\\s*\$" )
        prTrigger.triggerForBranch(branchName);
        prTrigger.setGithubContext('Performance Test Run')
        prTrigger.emitTrigger(myJob)
    }
    else {
        Utilities.addGithubPushTrigger(myJob)
    }
}

generate(true)
generate(false)
