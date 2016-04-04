// Groovy Script: http://www.groovy-lang.org/syntax.html
// Jenkins DSL: https://github.com/jenkinsci/job-dsl-plugin/wiki

import jobs.generation.Utilities;
def project = GithubProject
def buildTimeLimit = 120

static void addLogRotator(def myJob) {
  myJob.with {
    logRotator {
      daysToKeep(21)
      numToKeep(-1)
      artifactDaysToKeep(5)
      artifactNumToKeep(25)
    }
  }
}

static void addConcurrentBuild(def myJob, String category) {
  myJob.with {
    concurrentBuild(true)
    if (category != null)  {
      throttleConcurrentBuilds {
        throttleDisabled(false)
        maxTotal(0)
        maxPerNode(1)
        categories([category])
      }
    }
  }
}

static void addScm(def myJob, String branchName, String refspecName = '') {
  myJob.with {
    scm {
      git {
        remote {
          github('dotnet/roslyn', 'https', 'github.com')
          name('')
          refspec(refspecName)
        }
        branch(branchName)
        wipeOutWorkspace(true)
        shallowClone(true)
      }
    }
  }
}

static void addWrappers(def myJob) {
  myJob.with {
    wrappers {
      timeout {
        absolute(buildTimeLimit)
        abortBuild()
      }
      timestamps()
    }
  }
}

static void addArtifactArchiving(def myJob, String patternString, String excludeString) {
  myJob.with {
    publishers {
      flexiblePublish {
        conditionalAction {
          condition {
            status('ABORTED', 'FAILURE')
          }

          publishers {
            archiveArtifacts {
              allowEmpty(true)
              defaultExcludes(false)
              exclude(excludeString)
              fingerprint(false)
              onlyIfSuccessful(false)
              pattern(patternString)
            }
          }
        }
      }
    }
  }
}

static void addEmailPublisher(def myJob) {
  myJob.with {
    publishers {
      extendedEmail('$DEFAULT_RECIPIENTS, cc:mlinfraswat@microsoft.com', '$DEFAULT_SUBJECT', '$DEFAULT_CONTENT') {
        trigger('Aborted', '$PROJECT_DEFAULT_SUBJECT', '$PROJECT_DEFAULT_CONTENT', null, true, true, true, true)
        trigger('Failure', '$PROJECT_DEFAULT_SUBJECT', '$PROJECT_DEFAULT_CONTENT', null, true, true, true, true)
      }
    }
  }
}

static void addUnitPublisher(def myJob) {
  myJob.with {
    configure { node ->
      node / 'publishers' << {
      'xunit'('plugin': 'xunit@1.97') {
      'types' {
        'XUnitDotNetTestType' {
          'pattern'('**/xUnitResults/*.xml')
            'skipNoTestFiles'(false)
            'failIfNotNew'(true)
            'deleteOutputFiles'(true)
            'stopProcessingIfError'(true)
          }
        }
        'thresholds' {
          'org.jenkinsci.plugins.xunit.threshold.FailedThreshold' {
            'unstableThreshold'('')
            'unstableNewThreshold'('')
            'failureThreshold'('0')
            'failureNewThreshold'('')
          }
          'org.jenkinsci.plugins.xunit.threshold.SkippedThreshold' {
              'unstableThreshold'('')
              'unstableNewThreshold'('')
              'failureThreshold'('')
              'failureNewThreshold'('')
            }
          }
          'thresholdMode'('1')
          'extraConfiguration' {
            testTimeMargin('3000')
          }
        }
      }
    }
  }
}

static void addPushTrigger(def myJob) {
  myJob.with {
    triggers {
      githubPush()
    }
  }
}

// Generates the standard trigger phrases.  This is the regex which ends up matching lines like:
//  test win32 please
static String generateTriggerPhrase(String jobName, String opsysName, String triggerKeyword = 'this') {
    return "(?i).*test\\W+(${jobName.replace('_', '/').substring(7)}|${opsysName}|${triggerKeyword}|${opsysName}\\W+${triggerKeyword}|${triggerKeyword}\\W+${opsysName})\\W+please.*";
}

static void addPullRequestTrigger(def myJob, String jobName, String triggerPhraseText, Boolean triggerPhraseOnly = false) {
  myJob.with {
    triggers {
      pullRequest {
        admin('Microsoft')
        useGitHubHooks(true)
        triggerPhrase(triggerPhraseText)
        onlyTriggerPhrase(triggerPhraseOnly)
        autoCloseFailedPullRequests(false)
        orgWhitelist('Microsoft')
        allowMembersOfWhitelistedOrgsAsAdmin(true)
        permitAll(true)
        extensions {
          commitStatus {
            context(jobName.replace('_', '/').substring(7))
          }
        }
      }
    }
  }
}

static void addStandardJob(def myJob, String jobName, String branchName, String triggerPhrase, Boolean triggerPhraseOnly = false) {
  addLogRotator(myJob)
  addWrappers(myJob)

  def includePattern = "Binaries/**/*.pdb,Binaries/**/*.xml,Binaries/**/*.log,Binaries/**/*.dmp,Binaries/**/*.zip,Binaries/**/*.png,Binaries/**/*.xml"
  def excludePattern = "Binaries/Obj/**,Binaries/Bootstrap/**,Binaries/**/nuget*.zip"
  addArtifactArchiving(myJob, includePattern, excludePattern)

  if (branchName == 'prtest') {
    addPullRequestTrigger(myJob, jobName, triggerPhrase, triggerPhraseOnly);
    addScm(myJob, '${sha1}', '+refs/pull/*:refs/remotes/origin/pr/*')
  } else {
    addPushTrigger(myJob)
    addScm(myJob, "*/${branchName}")
    addEmailPublisher(myJob)
  }
}

def branchNames = []
['master', 'future', 'stabilization', 'future-stabilization', 'hotfixes', 'prtest'].each { branchName ->
  def shortBranchName = branchName.substring(0, 6)
  def jobBranchName = shortBranchName in branchNames ? branchName : shortBranchName
  branchNames << jobBranchName

  // folder("${jobBranchName}")
  ['win', 'linux', 'mac'].each { opsys ->
    // folder("${jobBranchName}/${opsys.substring(0, 3)}")
    ['dbg', 'rel'].each { configuration ->
      if ((configuration == 'dbg') || ((branchName != 'prtest') && (opsys == 'win'))) {
        // folder("${jobBranchName}/${opsys.substring(0, 3)}/${configuration}")
        ['unit32', 'unit64'].each { buildTarget ->
          if ((opsys == 'win') || (buildTarget == 'unit32')) {
            def jobName = "roslyn_${jobBranchName}_${opsys.substring(0, 3)}_${configuration}_${buildTarget}"
            def myJob = job(jobName) {
              description('')
            }

            // Generate the PR trigger phrase for this job.
            String triggerKeyword = '';
            switch (buildTarget) {
              case 'unit32':
                triggerKeyword =  '(unit|unit32|unit\\W+32)';
                break;
              case 'unit64':
                triggerKeyword = '(unit|unit64|unit\\W+64)';
                break;
            }
            String triggerPhrase = generateTriggerPhrase(jobName, opsys, triggerKeyword);
            Boolean triggerPhraseOnly = false;

            switch (opsys) {
              case 'win':
                myJob.with {
                  steps {
                    batchFile("""set TEMP=%WORKSPACE%\\Binaries\\Temp
mkdir %TEMP%
set TMP=%TEMP%
.\\cibuild.cmd ${(configuration == 'dbg') ? '/debug' : '/release'} ${(buildTarget == 'unit32') ? '/test32' : '/test64'} /buildTimeLimit ${buildTimeLimit}""")
                  }
                }
                Utilities.setMachineAffinity(myJob, 'Windows_NT', 'latest-or-auto')
                // Generic throttling for Windows, no category
                addConcurrentBuild(myJob, null)
                break;
              case 'linux':
                myJob.with {
                  label('ubuntu-fast')
                  steps {
                    shell("./cibuild.sh --nocache --debug")
                  }
                }
                addConcurrentBuild(myJob, 'roslyn/lin/unit')
                break;
              case 'mac':
                myJob.with {
                  label('mac-roslyn')
                  steps {
                    shell("./cibuild.sh --nocache --debug")
                  }
                }
                addConcurrentBuild(myJob, 'roslyn/mac/unit')
                triggerPhraseOnly = true;
                break;
            }

            addUnitPublisher(myJob)
            addStandardJob(myJob, jobName, branchName, triggerPhrase, triggerPhraseOnly);
          }
        }
      }
    }
  }

  def determinismJobName = "roslyn_${jobBranchName}_determinism"
  def determinismJob = job(determinismJobName) {
    description('')
  }

  determinismJob.with {
    label('windows-roslyn')
    steps {
      batchFile("""set TEMP=%WORKSPACE%\\Binaries\\Temp
mkdir %TEMP%
set TMP=%TEMP%
.\\cibuild.cmd /testDeterminism""")
    }
  }

  Utilities.setMachineAffinity(determinismJob, 'Windows_NT', 'latest-or-auto')
  addConcurrentBuild(determinismJob, null)
  addStandardJob(determinismJob, determinismJobName, branchName,  "(?i).*test\\W+determinism.*", true);
}

