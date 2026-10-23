using System;
using System.Collections.Generic;
using System.Linq;

using ArchUnitNET.Domain;
using ArchUnitNET.NUnit;

using NUnit.Framework;

using PersonalDataLogger.Service;
using PersonalDataLogger.Service.Processors;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace PersonalDataLogger.ArchitectureTests
{
    [TestFixture]
    public sealed class DependencyTests
    {
        [TestCase("PersonalDataLogger.Client", "PersonalDataLogger.Service")]
        [TestCase("PersonalDataLogger.Client", "PersonalDataLogger.Service.Processors")]
        [TestCase("PersonalDataLogger.Client", "PersonalDataLogger.Service.Models")]
        [TestCase("PersonalDataLogger.Configuration", "PersonalDataLogger.Service")]
        [TestCase("PersonalDataLogger.Configuration", "PersonalDataLogger.Service.Processors")]
        [TestCase("PersonalDataLogger.Configuration", "PersonalDataLogger.Client")]
        [TestCase("PersonalDataLogger.Configuration", "PersonalDataLogger.Logging")]
        [TestCase("PersonalDataLogger.Configuration", "PersonalDataLogger.Service.Models")]
        [TestCase("PersonalDataLogger.Logging", "PersonalDataLogger.Service")]
        [TestCase("PersonalDataLogger.Logging", "PersonalDataLogger.Service.Processors")]
        [TestCase("PersonalDataLogger.Logging", "PersonalDataLogger.Client")]
        [TestCase("PersonalDataLogger.Logging", "PersonalDataLogger.Configuration")]
        [TestCase("PersonalDataLogger.Logging", "PersonalDataLogger.Service.Models")]
        [TestCase("PersonalDataLogger.Service.Models", "PersonalDataLogger.Service")]
        [TestCase("PersonalDataLogger.Service.Models", "PersonalDataLogger.Service.Processors")]
        [TestCase("PersonalDataLogger.Service.Models", "PersonalDataLogger.Client")]
        [TestCase("PersonalDataLogger.Service.Models", "PersonalDataLogger.Configuration")]
        [TestCase("PersonalDataLogger.Service.Models", "PersonalDataLogger.Logging")]
        [TestCase("PersonalDataLogger.Service.Processors", "PersonalDataLogger.Service")]
        public void GivenComponentBoundaries_WhenCheckingDependencies_ThenReverseDependenciesAreForbidden(
            string sourceNamespace,
            string forbiddenNamespace)
        {
            IObjectProvider<IType> source = Types().That().ResideInNamespace(sourceNamespace);
            IObjectProvider<IType> forbidden = Types().That().ResideInNamespace(forbiddenNamespace);
            Assert.That(source.GetObjects(ProductionArchitecture.Model), Is.Not.Empty, sourceNamespace);
            Assert.That(forbidden.GetObjects(ProductionArchitecture.Model), Is.Not.Empty, forbiddenNamespace);

            Types().That().Are(source).Should().NotDependOnAny(forbidden)
                .Because("Dependencies must follow composition -> workers -> processors/clients -> settings/logging.")
                .Check(ProductionArchitecture.Model);
        }

        [TestCaseSource(nameof(GetProcessorBoundaries))]
        public void GivenIndependentProcessors_WhenCheckingDependencies_ThenProcessorsRemainIndependent(
            string processorName,
            string forbiddenName)
            => AssertNoDependency(processorName, forbiddenName);

        [TestCase("PersonalDataLogger.Service.EmailWorker", "PersonalDataLogger.Service.TimedLogWorker")]
        [TestCase("PersonalDataLogger.Service.TimedLogWorker", "PersonalDataLogger.Service.EmailWorker")]
        [TestCase("PersonalDataLogger.Service.Processors", "PersonalDataLogger.Service.Models.EmailCheckpoint")]
        public void GivenIndependentWorkersAndCheckpointOwnership_WhenCheckingDependencies_ThenBoundariesHold(
            string sourceName,
            string forbiddenName)
        {
            if (string.Equals(sourceName, "PersonalDataLogger.Service.Processors", StringComparison.Ordinal))
            {
                IObjectProvider<IType> processors = Types().That().ResideInNamespace(sourceName);
                IObjectProvider<IType> checkpoint = Types().That().HaveFullName(forbiddenName);
                Assert.That(processors.GetObjects(ProductionArchitecture.Model), Is.Not.Empty);
                Assert.That(checkpoint.GetObjects(ProductionArchitecture.Model), Is.Not.Empty);

                Types().That().Are(processors).Should().NotDependOnAny(checkpoint)
                    .Because("Only the email worker owns checkpoint persistence.")
                    .Check(ProductionArchitecture.Model);

                return;
            }

            AssertNoDependency(sourceName, forbiddenName);
        }

        [Test]
        public void GivenTheCompositionRoot_WhenCheckingDependencies_ThenComponentsDoNotDependOnProgram()
        {
            IObjectProvider<IType> program = Types().That().HaveFullName("PersonalDataLogger.Program");
            Assert.That(program.GetObjects(ProductionArchitecture.Model), Is.Not.Empty);

            foreach (string componentNamespace in GetComponentNamespaces())
            {
                IObjectProvider<IType> component = Types().That().ResideInNamespace(componentNamespace);
                Assert.That(component.GetObjects(ProductionArchitecture.Model), Is.Not.Empty, componentNamespace);

                Types().That().Are(component).Should().NotDependOnAny(program)
                    .Because("Program is the composition root, not a component dependency.")
                    .Check(ProductionArchitecture.Model);
            }
        }

        [Test]
        public void GivenWorkers_WhenInspectingConstructors_ThenComponentDependenciesUseInterfaces()
        {
            Type[] workers = [typeof(EmailWorker), typeof(TimedLogWorker)];

            foreach (Type worker in workers)
            {
                Type[] componentDependencies = worker.GetConstructors()
                    .SelectMany(constructor => constructor.GetParameters())
                    .Select(parameter => parameter.ParameterType)
                    .SelectMany(GetDependencyTypes)
                    .Where(parameterType => parameterType.Namespace is not null &&
                        parameterType.Namespace.StartsWith("PersonalDataLogger.Service", StringComparison.Ordinal))
                    .ToArray();
                Assert.That(componentDependencies, Is.Not.Empty, worker.FullName);
                Assert.That(
                    componentDependencies.Where(dependency => !dependency.IsInterface),
                    Is.Empty,
                    $"{worker.FullName} must consume worker and processor contracts, not implementations.");
            }
        }

        private static void AssertNoDependency(string sourceName, string forbiddenName)
        {
            IObjectProvider<IType> source = Types().That().HaveFullName(sourceName);
            IObjectProvider<IType> forbidden = Types().That().HaveFullName(forbiddenName);
            Assert.That(source.GetObjects(ProductionArchitecture.Model), Is.Not.Empty, sourceName);
            Assert.That(forbidden.GetObjects(ProductionArchitecture.Model), Is.Not.Empty, forbiddenName);

            Types().That().Are(source).Should().NotDependOnAny(forbidden)
                .Because("Workers and processors are independent components; cross-dependencies introduce cycles.")
                .Check(ProductionArchitecture.Model);
        }

        private static IEnumerable<TestCaseData> GetProcessorBoundaries()
        {
            Type[] processors = ProductionArchitecture.Assemblies.SelectMany(assembly => assembly.GetTypes())
                .Where(type => !type.IsNested &&
                    string.Equals(type.Namespace, typeof(IEmailProcessor).Namespace))
                .ToArray();
            Assert.That(processors.Where(type => type.IsClass), Is.Not.Empty);

            foreach (Type processor in processors.Where(type => type.IsClass))
            {
                foreach (Type forbidden in processors.Where(type =>
                    type != processor && !type.IsAssignableFrom(processor)))
                {
                    yield return new TestCaseData(processor.FullName!, forbidden.FullName!);
                }
            }
        }

        private static Type[] GetDependencyTypes(Type parameterType)
        {
            if (parameterType.IsGenericType)
            {
                return parameterType.GetGenericArguments();
            }

            return [parameterType];
        }

        private static string[] GetComponentNamespaces() =>
            ["PersonalDataLogger.Service", "PersonalDataLogger.Service.Processors",
                "PersonalDataLogger.Service.Models", "PersonalDataLogger.Client",
                "PersonalDataLogger.Configuration", "PersonalDataLogger.Logging"];
    }
}