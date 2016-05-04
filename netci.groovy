// Groovy Script: http://www.groovy-lang.org/syntax.html
// Jenkins DSL: https://github.com/jenkinsci/job-dsl-plugin/wiki

import jobs.generation.Utilities;

def project = GithubProject

// Email the results of aborted / failed jobs to our infrastructure alias
static void addEmailPublisher(def myJob) {
  myJob.with {
    publishers {
      extendedEmail('mlinfraswat@microsoft.com', '$DEFAULT_SUBJECT', '$DEFAULT_CONTENT') {
	// trigger(trigger name, subject, body, recipient list, send to developers, send to requester, include culprits, send to recipient list)
        trigger('Aborted', '$PROJECT_DEFAULT_SUBJECT', '$PROJECT_DEFAULT_CONTENT', null, false, false, false, true)
        trigger('Failure', '$PROJECT_DEFAULT_SUBJECT', '$PROJECT_DEFAULT_CONTENT', null, false, false, false, true)
      }
    }
  }
}

// Calls a web hook on Jenkins build events.  Allows our build monitoring jobs to be push notified
// vs. polling
static void addBuildEventWebHook(def myJob) {
  myJob.with {
    notifications {
      endpoint('https://jaredpar.azurewebsites.net/api/BuildEvent?code=tts2pvyelahoiliwu7lo6flxr8ps9kaip4hyr4m0ofa3o3l3di77tzcdpk22kf9gex5m6cbrcnmi') {
        event('all')
      }
    }
  }   
}

// Generates the standard trigger phrases.  This is the regex which ends up matching lines like:
//  test win32 please
static String generateTriggerPhrase(String jobName, String opsysName, String triggerKeyword = 'this') {
    return "(?i).*test\\W+(${jobName.replace('_', '/').substring(7)}|${opsysName}|${triggerKeyword}|${opsysName}\\W+${triggerKeyword}|${triggerKeyword}\\W+${opsysName})\\W+please.*";
}

static void addRoslynJob(def myJob, String jobName, String branchName, String triggerPhrase, Boolean triggerPhraseOnly = false) {
  def includePattern = "Binaries/**/*.pdb,Binaries/**/*.xml,Binaries/**/*.log,Binaries/**/*.dmp,Binaries/**/*.zip,Binaries/**/*.png,Binaries/**/*.xml"
  def excludePattern = "Binaries/Obj/**,Binaries/Bootstrap/**,Binaries/**/nuget*.zip"
  Utilities.addArchival(myJob, includePattern, excludePattern)

  // Create the standard job.  This will setup parameter, SCM, timeout, etc ...
  def projectName = 'dotnet/roslyn'
  def isPr = branchName == 'prtest'
  def defaultBranch = "*/${branchName}"
  Utilities.standardJobSetup(myJob, projectName, isPr, defaultBranch)

  // Need to setup the triggers for the job
  if (isPr) {
    def contextName = jobName.replace('_', '/').substring(7)
    Utilities.addGithubPRTrigger(myJob, contextName, triggerPhrase, triggerPhraseOnly)
  } else {
    Utilities.addGithubPushTrigger(myJob)
    addEmailPublisher(myJob)
  }

  addBuildEventWebHook(myJob)
}

def branchNames = []
['master', 'future', 'stabilization', 'future-stabilization', 'hotfixes', 'prtest', 'microupdate'].each { branchName ->
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
.\\cibuild.cmd ${(configuration == 'dbg') ? '/debug' : '/release'} ${(buildTarget == 'unit32') ? '/test32' : '/test64'}""")
                  }
                }
                Utilities.setMachineAffinity(myJob, 'Windows_NT', 'latest-or-auto')
                break;
              case 'linux':
                myJob.with {
                  steps {
                    shell("./cibuild.sh --nocache --debug")
                  }
                }
                Utilities.setMachineAffinity(myJob, 'Ubuntu14.04', 'latest-or-auto')
                break;
              case 'mac':
                myJob.with {
                  label('mac-roslyn')
                  steps {
                    shell("./cibuild.sh --nocache --debug")
                  }
                }
                triggerPhraseOnly = true;
                break;
            }

            Utilities.addXUnitDotNETResults(myJob, '**/xUnitResults/*.xml')
            addRoslynJob(myJob, jobName, branchName, triggerPhrase, triggerPhraseOnly)
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
  addRoslynJob(determinismJob, determinismJobName, branchName,  "(?i).*test\\W+determinism.*", true);
}

