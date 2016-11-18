// Groovy Script: http://www.groovy-lang.org/syntax.html
// Jenkins DSL: https://github.com/jenkinsci/job-dsl-plugin/wiki

import jobs.generation.*;

def generate_perf_test(boolean isPr) {
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

    if (isPr) {
        TriggerBuilder prTrigger = TriggerBuilder.triggerOnPullRequest()
        prTrigger.permitOrg('Microsoft')
        prTrigger.permitOrg('dotnet')
        prTrigger.setCustomTriggerPhrase("(?i)^\\s*(@dotnet-bot\\s+)?(re)?test\\s+perf(\\s+please)?\\s*\$" )
        prTrigger.triggerForBranch('master');
        prTrigger.setGithubContext('Performance Test Run')
        prTrigger.emitTrigger(myJob)
    }
    else {
        Utilities.addGithubPushTrigger(myJob)
    }
}

def generate_machine_test() {
    // The input project name (e.g. dotnet/corefx)
    def projectName = GithubProject
    def defaultBranch = "9bec6b5"

    def jobName = Utilities.getFullJobName(projectName, "perf_machine_test")
    def myJob = job(jobName) {
        description('perf machine test')

        labelParam('NODES_LABEL') {
            allNodes('allCases', 'AllNodeEligibility')
            defaultValue("windows-perf-internal")
            description('Nodes label expression to run the unix stability job across')
        }

        wrappers {
            credentialsBinding {
                string('BV_UPLOAD_SAS_TOKEN', 'Roslyn Perf BenchView Sas')
            }
        }

        steps {
            batchFile("""powershell -File ./build/scripts/run_perf.ps1 -TestPerfMachines""")
        }

        publishers {
            postBuildScripts {
                steps {
                    batchFile("""powershell -File ./build/scripts/cleanup_perf.ps1 -ShouldArchive""")
                }
            }
        }
    }

    Utilities.standardJobSetup(myJob, projectName, isPr, defaultBranch)
    Utilities.addPeriodicTrigger(myJob, "0 0 * * *", true /*always run*/)
}

generate_perf_test(true)
generate_perf_test(false)
generate_machine_test();
