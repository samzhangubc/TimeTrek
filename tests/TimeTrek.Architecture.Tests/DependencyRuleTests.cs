using System.Reflection;
using TimeTrek.Application;
using TimeTrek.Domain;
using TimeTrek.Infrastructure;
using TimeTrek.Platform.Windows;
using TimeTrek.Presentation.WinUI;

namespace TimeTrek.Architecture.Tests;

public sealed class DependencyRuleTests
{
    [Fact]
    public void DomainHasNoOutwardProjectDependency()
    {
        AssertDoesNotReference(
            typeof(DomainAssembly).Assembly,
            "TimeTrek.Application",
            "TimeTrek.Infrastructure",
            "TimeTrek.Platform.Windows",
            "TimeTrek.Presentation.WinUI",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.UI.Xaml");
    }

    [Fact]
    public void ApplicationDependsOnNoImplementationProject()
    {
        AssertDoesNotReference(
            typeof(ApplicationAssembly).Assembly,
            "TimeTrek.Infrastructure",
            "TimeTrek.Platform.Windows",
            "TimeTrek.Presentation.WinUI",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.UI.Xaml");
    }

    [Fact]
    public void InfrastructureDoesNotDependOnPresentationOrWindowsPlatform()
    {
        AssertDoesNotReference(
            typeof(InfrastructureAssembly).Assembly,
            "TimeTrek.Platform.Windows",
            "TimeTrek.Presentation.WinUI",
            "Microsoft.UI.Xaml");
    }

    [Fact]
    public void WindowsPlatformDoesNotDependOnPresentationOrInfrastructure()
    {
        AssertDoesNotReference(
            typeof(WindowsPlatformAssembly).Assembly,
            "TimeTrek.Infrastructure",
            "TimeTrek.Presentation.WinUI");
    }

    [Fact]
    public void PresentationDoesNotDependOnImplementationProjects()
    {
        AssertDoesNotReference(
            typeof(PresentationAssembly).Assembly,
            "TimeTrek.Infrastructure",
            "TimeTrek.Platform.Windows",
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
