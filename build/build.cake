#tool "nuget:?package=xunit.runner.console"
#addin "Cake.Json"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var uri = Argument("uri", "https://localhost:8081/");
var authKey = Argument("authKey", "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==");
var consistencyLevel = Argument("consistencyLevel", "BoundedStaleness");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
var solutionFile = "../src/SimpleEventStore/SimpleEventStore.sln";
var documentDbTestConfigFile = File("../src/SimpleEventStore/SimpleEventStore.AzureDocumentDb.Tests/bin/" + configuration + "/netcoreapp1.1/appSettings.json");

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Build")
    .Does(() =>
{
    // TODO: Use dotnet CLI tools
    MSBuild(solutionFile, settings => settings.SetConfiguration(configuration).UseToolVersion(MSBuildToolVersion.VS2017));
});

Task("Transform-Unit-Test-Config")
    .Does(() =>
{
	var configJson = ParseJsonFromFile(documentDbTestConfigFile);
	configJson["Uri"] = uri;
	configJson["AuthKey"] = authKey;
	configJson["ConsistencyLevel"] = consistencyLevel;

	SerializeJsonToFile(documentDbTestConfigFile, configJson);
});

Task("Run-Unit-Tests")
    .IsDependentOn("Build")
    .IsDependentOn("Transform-Unit-Test-Config")
    .Does(() =>
{
	// TODO: Use dotnet CLI tools
    XUnit2("../src/**/bin/" + configuration + "/*.Tests.dll");
});

Task("Package")
    .IsDependentOn("Run-Unit-Tests")
    .Does(() => 
{
    NuGetPack("./../src/SimpleEventStore/SimpleEventStore/SimpleEventStore.csproj", new NuGetPackSettings() {
        ArgumentCustomization = args => args.Append("-Prop Configuration=" + configuration)
    });

    NuGetPack("./../src/SimpleEventStore/SimpleEventStore.AzureDocumentDb/SimpleEventStore.AzureDocumentDb.csproj", new NuGetPackSettings() {
        ArgumentCustomization = args => args.Append("-Prop Configuration=" + configuration)
    });
});


Task("Deploy")
    .IsDependentOn("Package")
    .Does(() => 
{
    var nugetSource = Argument<string>("nugetSource");
    var nugetApiKey = Argument<string>("nugetApiKey");

    var package = GetFiles("./*.nupkg");

    NuGetPush(package, new NuGetPushSettings {
        Source = nugetSource,
        ApiKey = nugetApiKey
    });
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Run-Unit-Tests");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
