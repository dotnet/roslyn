Compiler Queries
-------------

This is a page tracking the queries the compiler team uses to manage our GitHub issues

## General

- [Triage](https://github.com/dotnet/roslyn/issues?utf8=✓&q=is%3Aopen+-label%3AArea-IDE+-label%3Aarea-performance+-label%3Aarea-interactive+-label%3A"Area-SDK+and+Samples"+-label%3AArea-external+-label%3Aarea-infrastructure+-label%3A"Area-Language+Design"+-label%3Aarea-Analyzers+is%3Aissue+-label%3Abug+-label%3Atest+-label%3Astory+-label%3Adocumentation+-label%3A"feature+request"++-label%3Adocumentation+-label%3Aquestion+-milestone%3Abacklog+-label%3Ainvestigating+-label%3A"area-project+system"++-label%3A"Investigation+Required"++-label%3Adiscussion+-label%3A"Need+More+Info"+-label%3Adisccussion+)
- [Pull Requests](https://github.com/dotnet/roslyn/pulls?utf8=%E2%9C%93&q=is%3Aopen+is%3Apr+-label%3A%22PR+For+Personal+Review+Only%22+label%3Aarea-compilers++)

## Phased Triage
- [Phase 1 - Issues not assigned to an area](https://github.com/dotnet/roslyn/issues?utf8=%E2%9C%93&q=is%3Aopen%20is%3Aissue%20-label%3A%22Area-Dynamic%20Analysis%22%20-label%3A%22Area-Project%20System%22%20%20-label%3AArea-Performance%20-label%3AArea-Analyzers%20-label%3AArea-Compilers%20-label%3AArea-Debugging%20-label%3A%22Area-Design%20Notes%22%20-label%3AArea-External%20-label%3AArea-IDE%20-label%3AArea-Infrastructure%20-label%3AArea-Interactive%20-label%3A%22Area-Language%20Design%22%20-label%3A%22Area-SDK%20and%20Samples%22%20-label%3A%22Sprint%20Summary%22%20)
- [Phase 2 - Compiler Issues not "triaged"
  as Bug/Test/Story/Documentation/Feature/Question/Investigating/Investigation Required/Need More Info/Blocked/Tenet-Performance/Code Gen Quality](https://github.com/dotnet/roslyn/issues?utf8=%E2%9C%93&q=is%3Aopen+is%3Aissue+label%3AArea-Compilers+-label%3ABug+-label%3ATest+-label%3AStory+-label%3ADocumentation+-label%3A%22Feature+Request%22+-label%3AQuestion+-label%3A%22Investigation+Required%22+-label%3A%22Need+More+Info%22+-label%3ABlocked+-label%3AInvestigating+-label%3ATenet-Performance+-label%3A%22Code+Gen+Quality%22+)
   - [2a Other than Nullable](https://github.com/dotnet/roslyn/issues?q=is%3Aopen+is%3Aissue+label%3AArea-Compilers+-label%3ABug+-label%3ATest+-label%3AStory+-label%3ADocumentation+-label%3A"Feature+Request"+-label%3AQuestion+-label%3A"Investigation+Required"+-label%3A"Need+More+Info"+-label%3ABlocked+-label%3AInvestigating+-label%3ATenet-Performance+-label%3A"Code+Gen+Quality"+-label%3A"New+Language+Feature+-+Nullable+Reference+Types")
   - [2b Nullable Only](https://github.com/dotnet/roslyn/issues?q=is%3Aopen+is%3Aissue+label%3AArea-Compilers+-label%3ABug+-label%3ATest+-label%3AStory+-label%3ADocumentation+-label%3A"Feature+Request"+-label%3AQuestion+-label%3A"Investigation+Required"+-label%3A"Need+More+Info"+-label%3ABlocked+-label%3AInvestigating+-label%3ATenet-Performance+-label%3A"Code+Gen+Quality"+label%3A"New+Language+Feature+-+Nullable+Reference+Types")
- [Phase 3 - Compiler issues not yet assigned to a release](https://github.com/dotnet/roslyn/issues?q=is%3Aopen+is%3Aissue+label%3AArea-Compilers+no%3Amilestone)
- [Phase 4 - Compiler issues in release 16.4 not yet assigned](https://github.com/dotnet/roslyn/issues?q=is%3Aopen+is%3Aissue+label%3AArea-Compilers+milestone%3A16.4+no%3Aassignee)
- [Language design issues that should probably be closed or moved to a language spec repo](https://github.com/dotnet/roslyn/issues?utf8=%E2%9C%93&q=is%3Aopen+is%3Aissue+label%3A%22Area-Language+Design%22+-label%3AArea-Compilers)
- [Oldest compiler issues that "need more info".  If no information is offered we close](https://github.com/dotnet/roslyn/issues?q=is%3Aopen+is%3Aissue+label%3AArea-Compilers+label%3A%22Need+More+Info%22+sort%3Acreated-asc)
- [Oldest compiler issues that are blocked](https://github.com/dotnet/roslyn/issues?q=is%3Aopen+is%3Aissue+label%3AArea-Compilers+sort%3Acreated-asc+label%3ABlocked)
- [Oldest compiler questions - please answer and close](https://github.com/dotnet/roslyn/issues?q=is%3Aopen+is%3Aissue+label%3AArea-Compilers+sort%3Acreated-asc+label%3AQuestion)

## Triaged Issues
- [Bug](https://github.com/dotnet/roslyn/issues?q=is%3Aopen+is%3Aissue+label%3AArea-Compilers+label%3ABug)
- [Tenet-Performance](https://github.com/dotnet/roslyn/issues?q=is%3Aopen+is%3Aissue+label%3AArea-Compilers+label%3ATenet-Performance)
- [Code Gen Quality](https://github.com/dotnet/roslyn/issues?q=is%3Aopen+is%3Aissue+label%3AArea-Compilers+label%3A%22Code+Gen+Quality%22)
- [Investigation Required](https://github.com/dotnet/roslyn/issues?q=is%3Aopen+is%3Aissue+label%3AArea-Compilers+label%3A%22Investigation+Required%22)
- [Investigating](https://github.com/dotnet/roslyn/issues?q=is%3Aopen+is%3Aissue+label%3AArea-Compilers+label%3AInvestigating)
- [Feature](https://github.com/dotnet/roslyn/issues?q=is%3Aopen+is%3Aissue+label%3AArea-Compilers+label%3A%22Feature+Request%22)
- [Question](https://github.com/dotnet/roslyn/issues?q=is%3Aopen+is%3Aissue+label%3AArea-Compilers+label%3AQuestion)
- [Test](https://github.com/dotnet/roslyn/issues?q=is%3Aopen+is%3Aissue+label%3AArea-Compilers+label%3ATest)
- [Story](https://github.com/dotnet/roslyn/issues?q=is%3Aopen+is%3Aissue+label%3AArea-Compilers+label%3AStory)
- [Documentation](https://github.com/dotnet/roslyn/issues?q=is%3Aopen+is%3Aissue+label%3AArea-Compilers+label%3ADocumentation)
- [Need More Info](https://github.com/dotnet/roslyn/issues?q=is%3Aopen+is%3Aissue+label%3AArea-Compilers+label%3A%22Need+More+Info%22)
- [Blocked](https://github.com/dotnet/roslyn/issues?q=is%3Aopen+is%3Aissue+label%3AArea-Compilers+label%3ABlocked)

## Release Specific
- [16.4](https://github.com/dotnet/roslyn/issues?utf8=✓&q=is%3Aopen+is%3Aissue+milestone%3A16.4+label%3AArea-Compilers+-label%3Adocumentation)
