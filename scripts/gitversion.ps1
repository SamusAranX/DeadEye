Param (
	[String]$ProjectDir,
	[String]$VersionFile="Version.cs"
)

$ErrorActionPreference = "Stop";

Push-Location -LiteralPath $ProjectDir;

$GitBranch = git rev-parse --abbrev-ref HEAD;
$GitCommit = git rev-parse HEAD;
$GitCommitShort = git rev-parse --short HEAD;
$GitVersion = git describe --tags --always --dirty;
$GitTag = git describe --tags --abbrev=0;
$BuildTime = Get-Date (Get-Date).ToUniversalTime() -Format 'o';

$NewVersionFileContent = Get-Content $VersionFile |
    %{$_ -replace 'false', 'true' } |
    %{$_ -replace 'GIT_BRANCH = ""', "GIT_BRANCH = `"$GitBranch`"" } |
    %{$_ -replace 'GIT_COMMIT = ""', "GIT_COMMIT = `"$GitCommit`"" } |
    %{$_ -replace 'GIT_COMMIT_SHORT = ""', "GIT_COMMIT_SHORT = `"$GitCommitShort`"" } |
    %{$_ -replace 'GIT_VERSION = ""', "GIT_VERSION = `"$GitVersion`"" } |
    %{$_ -replace 'GIT_TAG = ""', "GIT_TAG = `"$GitTag`"" } |
    %{$_ -replace 'BUILD_TIME = ""', "BUILD_TIME = `"$BuildTime`"" };

echo "Writing new $VersionFile…";

$NewVersionFileContent > $VersionFile;
# echo $NewVersionFileContent;

echo "Done.";

Pop-Location;