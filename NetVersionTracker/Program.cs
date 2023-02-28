using Microsoft.Build.Evaluation;
using Microsoft.Build.Locator;
using System;

namespace NetVersionTracker
{
    internal class Program
    {
        static string targetFolder = "";

        static void Main(string[] args)
        {
            MSBuildLocator.RegisterDefaults();

            GatherDependencies();
        }

        private static void GatherDependencies()
        {
            var netVersions = new Dictionary<string, string>();
            var packages = new List<PackageReference>();

            var projects = Directory.EnumerateFiles(targetFolder, "*.csproj", SearchOption.AllDirectories);

            foreach (var project in projects)
            {
                var projectName = Path.GetFileName(project);
                var buildEngine = new ProjectCollection();
                var projectBuild = buildEngine.LoadProject(project);

                var netVersion = GetNetVersion(projectBuild);

                Console.WriteLine($"{projectName} - {netVersion}");
                netVersions.TryAdd(projectName, netVersion);

                // Gather Nuget dependencies
                packages.AddRange(GetNugetDependencies(projectName, projectBuild));
            }

            Console.WriteLine("Nuget dependencies: ");
            foreach(var package in packages.GroupBy(x => x.PackageName))
            {
                Console.WriteLine($"{package.Key}");
                foreach(var reference in package)
                {
                    Console.WriteLine($"{reference.PackageVersion} ({reference.Project})");
                }
                Console.WriteLine();
            }
        }

        private static string GetNetVersion(Project projectBuild)
        {
            var targetFramework = projectBuild.GetPropertyValue("TargetFramework");

            // Old projects use TargetFrameworkVersion
            if (string.IsNullOrWhiteSpace(targetFramework))
            {
                targetFramework = projectBuild.GetPropertyValue("TargetFrameworkVersion");
            }

            return targetFramework;
        }

        private static IEnumerable<PackageReference> GetNugetDependencies(string projectName, Project projectBuild)
        {
            return projectBuild.Items
                .Where(item => item.ItemType == "PackageReference")
                .Select(item =>
                    new PackageReference
                    {
                        Project = projectName,
                        PackageName = item.EvaluatedInclude,
                        PackageVersion = item.GetMetadataValue("Version")
                    });
        }
    }

    internal class PackageReference
    {
        public string Project { get; set; }
        public string PackageName { get; set; }
        public string PackageVersion { get; set; }
    }
}