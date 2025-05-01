## For faster PR/CI builds localize only for 2 languages, ENU and JPN provide good enough coverage
if ($env:BUILD_REASON -eq 'PullRequest') {
  'ENU,JPN'
} else {
  'VS'
}
