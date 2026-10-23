using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

using NUnit.Framework;

namespace PersonalDataLogger.ArchitectureTests
{
    [TestFixture]
    public sealed class ProjectBoundaryTests
    {
        [Test]
        public void GivenProductionProjects_WhenLoadingArchitecture_ThenEveryProductionAssemblyIsIncluded()
        {
            string[] assemblyNames = GetProductionProjects()
                .Select(project => GetAssemblyName(project)).ToArray();
            Assert.That(assemblyNames, Is.Not.Empty);
            Assert.That(
                ProductionArchitecture.Assemblies.Select(assembly => assembly.GetName().Name),
                Is.EquivalentTo(assemblyNames),
                "Load every production assembly explicitly, and never load test assemblies.");
        }

        [Test]
        public void GivenProductionProjects_WhenCheckingDeclaredReferences_ThenTestDependenciesAreForbidden()
        {
            foreach (string projectPath in GetProductionProjects())
            {
                XElement project = XElement.Load(projectPath);
                string[] references = project.Descendants()
                    .Where(element => element.Name.LocalName is "ProjectReference" or "Reference" or "PackageReference")
                    .Select(element => (string?)element.Attribute("Include") ?? string.Empty)
                    .ToArray();
                Assert.That(references, Is.Not.Empty, projectPath);
                Assert.That(
                    references.Where(IsTestReference),
                    Is.Empty,
                    $"{projectPath} must not declare references to test projects or test frameworks.");
            }
        }

        [Test]
        public void GivenProductionAssemblies_WhenCheckingReferences_ThenTestsCannotBecomeRuntimeDependencies()
        {
            foreach (Assembly assembly in ProductionArchitecture.Assemblies)
            {
                Assert.That(
                    assembly.GetReferencedAssemblies().Select(reference => reference.Name!).Where(IsTestReference),
                    Is.Empty,
                    $"{assembly.GetName().Name} must remain independent of test assemblies.");
                Assert.That(
                    assembly.GetCustomAttributes<InternalsVisibleToAttribute>(),
                    Is.Empty,
                    $"{assembly.GetName().Name} must not expose internals to other assemblies.");
            }
        }

        [Test]
        public void GivenProductionProjects_WhenInspectingFriendDeclarations_ThenInternalsRemainPrivate()
        {
            foreach (string projectPath in GetProductionProjects())
            {
                XElement project = XElement.Load(projectPath);
                Assert.That(
                    project.Descendants().Where(element =>
                        string.Equals(element.Name.LocalName, "InternalsVisibleTo") ||
                        string.Equals((string?)element.Attribute("Include"), typeof(InternalsVisibleToAttribute).FullName)),
                    Is.Empty,
                    $"{projectPath} must not declare InternalsVisibleTo.");
            }
        }

        private static string[] GetProductionProjects()
        {
            DirectoryInfo? directory = new(AppContext.BaseDirectory);

            while (directory is not null &&
                !File.Exists(Path.Combine(directory.FullName, "personal-data-logger.slnx")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "The solution root must be available for project-reference checks.");
            string root = directory!.FullName;
            XElement solution = XElement.Load(Path.Combine(root, "personal-data-logger.slnx"));
            string[] projects = solution.Elements("Project")
                .Select(project => Path.GetFullPath(Path.Combine(root, (string)project.Attribute("Path")!)))
                .Where(project => !IsTestProject(project)).ToArray();
            Assert.That(projects, Is.Not.Empty, "The solution must contain production projects.");

            return projects;
        }

        private static bool IsTestProject(string projectPath) =>
            IsTestReference(Path.GetFileNameWithoutExtension(projectPath)) ||
            XElement.Load(projectPath).Descendants("IsTestProject")
                .Any(property => string.Equals(property.Value, "true", StringComparison.OrdinalIgnoreCase));

        private static bool IsTestReference(string reference) =>
            reference.Contains("Tests", StringComparison.OrdinalIgnoreCase) ||
            reference.Contains("Test.Sdk", StringComparison.OrdinalIgnoreCase) ||
            reference.Contains("NUnit", StringComparison.OrdinalIgnoreCase) ||
            reference.Contains("xunit", StringComparison.OrdinalIgnoreCase) ||
            reference.Contains("Moq", StringComparison.OrdinalIgnoreCase) ||
            reference.Contains("ArchUnitNET", StringComparison.OrdinalIgnoreCase);

        private static string GetAssemblyName(string projectPath) =>
            XElement.Load(projectPath).Descendants("AssemblyName").Select(property => property.Value)
                .FirstOrDefault() ?? Path.GetFileNameWithoutExtension(projectPath);
    }
}