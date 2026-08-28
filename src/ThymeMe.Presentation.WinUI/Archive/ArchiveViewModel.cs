using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ThymeMe.Application.Organization;
using ThymeMe.Domain.Organization;

namespace ThymeMe.Presentation.WinUI.Archive;

public sealed record ArchivedRow(OrganizationKind Kind, Guid Id, string Name, string Type, string Owner);

public sealed partial class ArchiveViewModel(IOrganizationStore store, TimeProvider timeProvider) : ObservableObject
{
    public ObservableCollection<ArchivedRow> Items { get; } = [];

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    public async ValueTask LoadAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<StreamDefinition> streams = await store.ListStreamsAsync(true, cancellationToken);
        IReadOnlyList<CategoryDefinition> categories = await store.ListCategoriesAsync(true, cancellationToken);
        IReadOnlyList<ProjectDefinition> projects = await store.ListProjectsAsync(true, cancellationToken);
        Dictionary<Guid, string> streamNames = streams.ToDictionary(item => item.Id, item => item.Name);
        string search = Search.Trim();
        Items.Clear();
        foreach (ArchivedRow row in streams.Where(item => item.IsArchived).Select(item => new ArchivedRow(OrganizationKind.Stream, item.Id, item.Name, "Stream", string.Empty))
                     .Concat(categories.Where(item => item.IsArchived).Select(item => new ArchivedRow(OrganizationKind.Category, item.Id, item.Name, "Category", item.StreamId is Guid id ? streamNames.GetValueOrDefault(id, "Unknown") : "Global")))
                     .Concat(projects.Where(item => item.IsArchived).Select(item => new ArchivedRow(OrganizationKind.Project, item.Id, item.Name, "Project", item.StreamId is Guid id ? streamNames.GetValueOrDefault(id, "Unknown") : "Standalone")))
                     .Where(item => search.Length == 0 || item.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase)))
        {
            Items.Add(row);
        }
    }

    public async ValueTask RestoreAsync(ArchivedRow row, CancellationToken cancellationToken = default)
    {
        await store.RestoreAsync(row.Kind, row.Id, timeProvider.GetUtcNow().ToUnixTimeMilliseconds(), cancellationToken);
        await LoadAsync(cancellationToken);
    }
}
