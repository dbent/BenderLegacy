#l "utilities.cake"

var target = Argument<string>("target", "Build");
var configuration = Argument<string>("configuration", "Debug");
var release = Argument<bool>("release", false);

var outputDirectory = "Output";
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
    BuildVersionInfo("Bend");
    BuildVersionInfo("Bender");
});

Task("BuildAssemblyInfo")
    .IsDependentOn("BuildVersionInfo")
    .Does(() =>
{
    BuildAssemblyInfo("Bend");
    BuildAssemblyInfo("Bender");
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
    Information($"Bend:   {GetVersion("Bend")}");
    Information($"Bender: {GetVersion("Bender")}");
});

Task("ChangeLog")
    .Does(() =>
{
    Information($"# Bend{Environment.NewLine}{GetChangeLog("Bend").LatestChanges}");
    Information($"# Bender{Environment.NewLine}{GetChangeLog("Bender").LatestChanges}");
});

RunTarget(target);

private void BuildVersionInfo(string project)
{
    SemVer buildVersion;

    var changeLog = GetChangeLog(project);
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
            throw new Exception($"{project} ChangeLog already contains build metadata.");
        }
    }
    else
    {
        buildVersion = version;
    }

    System.IO.File.WriteAllText($"Output/{project}.version", buildVersion);
    System.IO.File.WriteAllText($"Output/{project}.prelease", (buildVersion.Pre != null).ToString().ToLower());
    System.IO.File.WriteAllText($"Output/{project}.changelog", changeLog.LatestChanges);
}

private void BuildAssemblyInfo(string project)
{
    var inFile = $"Source/{project}/Properties/AssemblyInfo.cs.in";
    var outFile = $"Source/{project}/Properties/AssemblyInfo.cs";

    var version = GetBuildVersion(project);

    var output = TransformTextFile(inFile)
        .WithToken("VERSION", version)
        .WithToken("VERSION.MAJOR", version.Major)
        .WithToken("VERSION.MINOR", version.Minor)
        .WithToken("VERSION.PATCH", version.Patch)
        .WithToken("VERSION.PRE", version.Pre)
        .WithToken("VERSION.BUILD", version.Build)
        .ToString();

    System.IO.File.WriteAllText(outFile, output);
}

private SemVer GetBuildVersion(string project)
{
    return new SemVer(System.IO.File.ReadAllText($"Output/{project}.version"));
}
