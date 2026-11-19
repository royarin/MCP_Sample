function Resolve-DemoBaseName([string]$BaseName, [string]$UniqueSuffix = "") {
  $pattern = '\A[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\z'
  if ($BaseName -cnotmatch $pattern) {
    throw "BaseName must contain lowercase letters, digits or hyphens, and start and end with a letter or digit."
  }
  if ($UniqueSuffix -ne "" -and $UniqueSuffix -cnotmatch $pattern) {
    throw "UniqueSuffix must contain lowercase letters, digits or hyphens, and start and end with a letter or digit."
  }
  $name = if ($UniqueSuffix -eq "") { $BaseName } else { "$BaseName-$UniqueSuffix" }
  if ($name.Length -gt 45) {
    throw "The combined BaseName and UniqueSuffix must be at most 45 characters."
  }
  return $name
}
