#l "utilities.cake"

var target = Argument<string>("target", "Build");
var configuration = Argument<string>("configuration", "Debug");
var release = Argument<bool>("release", false);

var assemblyInfoFile = "Source/Bender/Properties/AssemblyInfo.cs";
var outputDirectory = "Output";
var buildVersionFile = $"{outputDirectory}/VERSION";
var buildPreleaseFile = $"{outputDirectory}/PRELEASE";
var buildChangeLogFile = $"{outputDirectory}/CHANGELOG";
var buildDirectory = Directory($"{outputDirectory}/Build/{configuration}");

Task("CleanBuild")
    .Does(() =>
{
    CleanDirectories(new DirectoryPath[] { buildDirectory });
});

Task("Restore")
    .Does(() =>
{
    NuGetRestore(GetSolution());
});

Task("BuildVersionInfo")
    .Does(() =>
{
    SemVer buildVersion;

    var changeLog = GetChangeLog();
    var version = changeLog.LatestVersion;
    var rev = GetGitRevision(useShort: true);

    if (rev != null && !release)
    {
        if (version.Build == null)
        {
            buildVersion = new SemVer(version.Major, version.Minor, version.Patch, version.Pre, rev);
        }
        else
        {
            throw new Exception($"ChangeLog already contains build metadata.");
        }
    }
    else
    {
        buildVersion = version;
    }

    System.IO.File.WriteAllText(buildVersionFile, buildVersion);
    System.IO.File.WriteAllText(buildPreleaseFile, (buildVersion.Pre != null).ToString().ToLower());
    System.IO.File.WriteAllText(buildChangeLogFile, changeLog.LatestChanges);
});

Task("BuildAssemblyInfo")
    .IsDependentOn("BuildVersionInfo")
    .Does(() =>
{
    var version = GetBuildVersion();

    var output = TransformTextFile($"{assemblyInfoFile}.in")
        .WithToken("VERSION", version)
        .WithToken("VERSION.MAJOR", version.Major)
        .WithToken("VERSION.MINOR", version.Minor)
        .WithToken("VERSION.PATCH", version.Patch)
        .WithToken("VERSION.PRE", version.Pre)
        .WithToken("VERSION.BUILD", version.Build)
        .ToString();

    System.IO.File.WriteAllText(assemblyInfoFile, output);
});

Task("Build")
    .IsDependentOn("CleanBuild")
    .IsDependentOn("Restore")
    .IsDependentOn("BuildAssemblyInfo")
    .Does(() =>
{
    MSBuild(GetSolution(), s => { s.Configuration = configuration; });
});

Task("Version")
    .Does(() =>
{
    Information(GetVersion());
});

Task("ChangeLog")
    .Does(() =>
{
    Information(GetChangeLog().LatestChanges);
});

RunTarget(target);

private SemVer GetBuildVersion()
{
    return new SemVer(System.IO.File.ReadAllText(buildVersionFile));
}
