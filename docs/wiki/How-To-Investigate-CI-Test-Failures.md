# Determine Which Tests Are Failing

1. Click details to see more information about the failing run. 
![GitHub Checks Show Unsuccessful Roslyn-Integration-CI Run](images/how-to-investigate-ci-test-failures-figure1.png)
2. Click on the number of errors and warnings to open Azure DevOps.  
![GitHub Pipeline Details Of Unsuccessful Rosyln-Integration-CI Run](images/how-to-investigate-ci-test-failures-figure2.png)
3. Click on the Tests tabs to see a list of failing tests.  
![Azure DevOps Logs For Roslyn-Integration-CI Pipeline](images/how-to-investigate-ci-test-failures-figure3.png)
4. Expand each run to see which tests failed.  
![Test Results Grouped By Run](images/how-to-investigate-ci-test-failures-figure4.png)

# Check for Open Issues

1. Open https://github.com/dotnet/roslyn/issues
2. Enter the name of the failing test and search for open issues that match the failure.  
![GitHub Issue Search Results](images/how-to-investigate-ci-test-failures-figure5.png)

# Add New Occurrence to Existing Issue

1. Add a new comment and include relevent information.  
![GitHub Issue Comment](images/how-to-investigate-ci-test-failures-figure6.png)  
Good information to include in this comment:
    - A link to the Azure DevOps tests results.
    - Which attempt the failure occurred on.
    - Which test run failed.  
For instance the Debug_Async run failed on the 3rd attempt.  
![Azure DevOps Logs](images/how-to-investigate-ci-test-failures-figure7.png)  

# Creating a New Issue for Failed Integration Test

1. Open the Screenshot artifacts for one of the test runs.  
![Azure DevOps Artifacts Dropdown](images/how-to-investigate-ci-test-failures-figure8.png)  
![Azure DevOps Artifact Explorer](images/how-to-investigate-ci-test-failures-figure9.png)  
2. Focus on the first reported integration test failure. Often these failures cascade and it can be misleading.
3. Create a new issue and include the failed test name in the title
4. Good information to include in the issue body:
    - A link to the Azure DevOps tests results.
    - Which attempt the failure occurred on.
    - Which test run failed.  
    - Additional details outlined below.
3. First check to see if a new entry appears in the *.DotNet.log file for the test. There is a MissingMethodException thrown at the beginning of every test run that will log to *.DotNet.log anything thrown by a previous test run.  
![*.DotNet.log](images/how-to-investigate-ci-test-failures-figure10.png)  
If the file is new for the test you are creating (check file timestamp compared to when your tests ran) or contains a different exception than the one given in the MissingMethodException entry, then treat the root cause of the test failure as whatever that exception is.  
![Pipeline Run Started Timestamp](images/how-to-investigate-ci-test-failures-figure11.png)  
4. If the screenshot shows an interesting state (OS window, etc.), or if the failure is not identified by (2), make sure to include a screenshot from the time of failure in the issue.  
![Pipeline Artifacts List Displaying .png](images/how-to-investigate-ci-test-failures-figure12.png)  
5. If the cause is not identified by (2), include the stack trace from the TargetInvocationException leading to the test failure.  
![Pipeline Artifacts List Displaying TargetInvocationException.log](images/how-to-investigate-ci-test-failures-figure13.png)  
6. Add them to the Flaky Tests columns in the [Test Improvements project](https://github.com/dotnet/roslyn/projects/2)

Here is an example from a test failure on Jenkins - it's reviewable even though the CI link is broken
 https://github.com/dotnet/roslyn/issues/26041

# Troubleshooting Integration Test Run Failures

 1. Failure during **Checkout** because of locked file  
![Azure DevOps Pipeline Task List](images/how-to-investigate-ci-test-failures-figure14.png)  
`[error] One or more errors occurred. (The process cannot access the file '...\Some.File' because it is being used by another process.)`  
    1. Take note of the machine name  
    ![Azure DevOps Pipeline Pool and Agent Infromation](images/how-to-investigate-ci-test-failures-figure15.png)  
    * note - if you can't see pool name, make sure you change this to Attempt 1 
    ![Pool name attemp 1](images/pool-name-attempt1.png)
    2. Click on the Pool name (dotnet-external-vs2019-preview) in this case.  
    3. Click on the Agents tab  
    ![Azure DevOps Jobs List](images/how-to-investigate-ci-test-failures-figure16.png)
    4. Find the machine with the matching name and disable it.  
    ![Azure DevOps Agent Pool List](images/how-to-investigate-ci-test-failures-figure17.png)
    5. From the Azure DevOps Pipeline page choose to Retry the test run.  
    ![Azure DevOps Pipeline Dropdown](images/how-to-investigate-ci-test-failures-figure18.png)
    
 2. ServiceHub crash

    If you see integration tests failing for your PR with screenshots that show info-bar like so:
    
    ![ServiceHub failure info-bar](images/how-to-investigate-ci-test-failures-servicehub-failure-infobar.png) 
    
    This means the ServiceHub process crashed. Check the ServiceHub logs to see why. They are available in the log artifacts:
    
    ![ServiceHub log artifacts](images/how-to-investigate-ci-test-failures-servicehub-log-artifacts.png)
 
