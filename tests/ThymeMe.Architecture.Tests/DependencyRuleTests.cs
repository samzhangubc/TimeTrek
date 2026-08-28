using System.Reflection;
using ThymeMe.Application;
using ThymeMe.Domain;
using ThymeMe.Infrastructure;
using ThymeMe.Platform.Windows;
using ThymeMe.Presentation.WinUI;

namespace ThymeMe.Architecture.Tests;

public sealed class DependencyRuleTests
{
    [Fact]
    public void DomainHasNoOutwardProjectDependency()
    {
        AssertDoesNotReference(
            typeof(DomainAssembly).Assembly,
            "ThymeMe.Application",
            "ThymeMe.Infrastructure",
            "ThymeMe.Platform.Windows",
            "ThymeMe.Presentation.WinUI",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.UI.Xaml");
    }

    [Fact]
    public void ApplicationDependsOnNoImplementationProject()
    {
        AssertDoesNotReference(
            typeof(ApplicationAssembly).Assembly,
            "ThymeMe.Infrastructure",
            "ThymeMe.Platform.Windows",
            "ThymeMe.Presentation.WinUI",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.UI.Xaml");
    }

    [Fact]
    public void InfrastructureDoesNotDependOnPresentationOrWindowsPlatform()
    {
        AssertDoesNotReference(
            typeof(InfrastructureAssembly).Assembly,
            "ThymeMe.Platform.Windows",
            "ThymeMe.Presentation.WinUI",
            "Microsoft.UI.Xaml");
    }

    [Fact]
    public void WindowsPlatformDoesNotDependOnPresentationOrInfrastructure()
    {
        AssertDoesNotReference(
            typeof(WindowsPlatformAssembly).Assembly,
            "ThymeMe.Infrastructure",
            "ThymeMe.Presentation.WinUI");
    }

    [Fact]
    public void PresentationDoesNotDependOnImplementationProjects()
    {
        AssertDoesNotReference(
            typeof(PresentationAssembly).Assembly,
            "ThymeMe.Infrastructure",
            "ThymeMe.Platform.Windows",
            "Microsoft.EntityFrameworkCore");
    }

    private static void AssertDoesNotReference(Assembly assembly, params string[] forbiddenNames)
    {
        HashSet<string> references = assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        foreach (string forbiddenName in forbiddenNames)
        {
            Assert.DoesNotContain(forbiddenName, references);
        }
    }
}
