using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TimeTrek.Domain.Organization;
using TimeTrek.Domain.Timing;
using TimeTrek.Presentation.WinUI.Common;
using TimeTrek.Presentation.WinUI.Timing;

namespace TimeTrek.Presentation.WinUI.Home;

public sealed partial class HomePage : UserControl
{
    private readonly TransportViewModel transport;

    public HomePage(HomeViewModel viewModel, TransportViewModel transport)
    {
        ViewModel = viewModel;
        this.transport = transport;
        InitializeComponent();
    }

    public HomeViewModel ViewModel { get; }

    private async void OnAddStream(object sender, RoutedEventArgs e)
    {
        ViewModel.NewStreamName = NewStreamName.Text;
        await ViewModel.CreateStreamCommand.ExecuteAsync(null);
        NewStreamName.Text = ViewModel.NewStreamName;
        ErrorBar.IsOpen = !string.IsNullOrWhiteSpace(ViewModel.ErrorMessage);
        ErrorBar.Title = ViewModel.ErrorMessage ?? string.Empty;
    }

    private async void OnStartTimer(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: StreamCard card })
        {
            return;
        }

        TextBox duration = new()
        {
            Header = "Required duration",
            Text = transport.DurationText,
            PlaceholderText = "25, 1.5h, 1h 30m, 90m, or 01:30",
        };
        ContentDialog dialog = new()
        {
            XamlRoot = XamlRoot,
            Title = "Start Session",
            Content = duration,
            PrimaryButtonText = "Start",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
        };
        dialog.Opened += (_, _) =>
        {
            duration.Focus(FocusState.Programmatic);
            duration.SelectAll();
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            transport.SelectAssociations(card.Id, card.SelectedCategoryIds, card.SelectedProjectId);
            transport.DurationText = duration.Text;
            await transport.StartAsync();
            ErrorBar.IsOpen = !string.IsNullOrWhiteSpace(transport.ErrorMessage);
            ErrorBar.Title = transport.ErrorMessage ?? string.Empty;
        }
    }

    private async void OnChooseCategories(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: StreamCard card })
        {
            return;
        }

        ListView list = new()
        {
            ItemsSource = card.CategoryOptions,
            DisplayMemberPath = nameof(CategoryOption.Name),
            SelectionMode = ListViewSelectionMode.Multiple,
            MaxHeight = 360,
        };
        list.Loaded += (_, _) =>
        {
            foreach (CategoryOption option in card.CategoryOptions.Where(item => card.SelectedCategoryIds.Contains(item.Id)))
            {
                list.SelectedItems.Add(option);
            }
        };
        ContentDialog dialog = new()
        {
            XamlRoot = XamlRoot,
            Title = $"Categories for {card.Name}",
            Content = list,
            PrimaryButtonText = "Use selected",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            card.SelectedCategoryIds.Clear();
            card.SelectedCategoryIds.UnionWith(list.SelectedItems.OfType<CategoryOption>().Select(item => item.Id));
            await ViewModel.SaveStreamDefaultsAsync(card);
        }
    }

    private async void OnProjectChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox { Tag: StreamCard card })
        {
            await ViewModel.SaveStreamDefaultsAsync(card);
        }
    }

    private async void OnStreamSettings(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: StreamCard card })
        {
            return;
        }

        TextBox name = new() { Header = "Stream name", Text = card.Name, MaxLength = 200 };
        TextBox color = new() { Header = "Color (#RRGGBB)", Text = card.Color, MaxLength = 7 };
        ToggleSwitch budgetEnabled = new() { Header = "Set a time budget", IsOn = card.Budget is not null };
        TextBox budgetDuration = new()
        {
            Header = "Budget duration",
            Text = card.Budget is null ? "" : Math.Max(1, card.Budget.DurationMilliseconds / 60_000L)
                .ToString(System.Globalization.CultureInfo.CurrentCulture),
            PlaceholderText = "600, 10h, or 10:00",
        };
        ComboBox reset = new() { Header = "Budget resets" };
        foreach ((string label, BudgetResetPeriod value) in new[]
        {
            ("Never", BudgetResetPeriod.None),
            ("Weekly", BudgetResetPeriod.Weekly),
            ("Monthly", BudgetResetPeriod.Monthly),
        })
        {
            reset.Items.Add(new ComboBoxItem { Content = label, Tag = value });
        }
        reset.SelectedItem = reset.Items.OfType<ComboBoxItem>().First(item => Equals(item.Tag, card.Budget?.ResetPeriod ?? BudgetResetPeriod.None));
        StackPanel content = new() { Spacing = 10, MaxWidth = 520 };
        content.Children.Add(name); content.Children.Add(color); content.Children.Add(budgetEnabled);
        content.Children.Add(budgetDuration); content.Children.Add(reset);
        TextBlock guidance = new()
        {
            Text = "Budgets show progress but never block tracking or send alerts.",
            TextWrapping = TextWrapping.Wrap,
        };
        content.Children.Add(guidance);
        ContentDialog dialog = new()
        {
            XamlRoot = XamlRoot,
            Title = "Stream settings",
            Content = content,
            PrimaryButtonText = "Save",
            SecondaryButtonText = "Archive",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
        };
        ContentDialogResult result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Secondary)
        {
            await ConfirmArchiveAsync(card.Id, card.Name);
            return;
        }

        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        TimeBudget? budget = null;
        if (budgetEnabled.IsOn)
        {
            DurationParseResult parsed = DurationParser.Parse(budgetDuration.Text);
            if (!parsed.IsValid)
            {
                ShowError(parsed.Error);
                return;
            }
            BudgetResetPeriod period = reset.SelectedItem is ComboBoxItem { Tag: BudgetResetPeriod value }
                ? value
                : BudgetResetPeriod.None;
            budget = new TimeBudget(parsed.Milliseconds, period);
        }

        ShowError(await ViewModel.UpdateStreamAsync(card, name.Text, color.Text, budget));
    }

    private async void OnAddSession(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Guid streamId })
        {
            await ShowHistoryEntryAsync(streamId, adjustment: false);
        }
    }

    private async void OnAdjust(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Guid streamId })
        {
            await ShowHistoryEntryAsync(streamId, adjustment: true);
        }
    }

    private async Task ShowHistoryEntryAsync(Guid streamId, bool adjustment)
    {
        TextBox duration = new()
        {
            Header = adjustment ? "Time to subtract" : "Duration",
            Text = "25",
            PlaceholderText = "25, 1.5h, 1h 30m, 90m, or 01:30",
        };
        TextBox description = new() { Header = "Description (optional)", MaxLength = 4000 };
        StackPanel content = new() { Spacing = 10 };
        content.Children.Add(duration);
        content.Children.Add(description);
        ContentDialog dialog = new()
        {
            XamlRoot = XamlRoot,
            Title = adjustment ? "Subtract time" : "Add Session",
            Content = content,
            PrimaryButtonText = adjustment ? "Subtract" : "Add",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            string? error = adjustment
                ? await ViewModel.AddAdjustmentAsync(streamId, duration.Text, description.Text)
                : await ViewModel.AddManualAsync(streamId, duration.Text, description.Text);
            ErrorBar.IsOpen = error is not null;
            ErrorBar.Title = error ?? string.Empty;
        }
    }

    private async void OnAddCategory(object sender, RoutedEventArgs e) => await ShowOrganizationDialogAsync(category: true);

    private async void OnAddProject(object sender, RoutedEventArgs e) => await ShowOrganizationDialogAsync(category: false);

    private async Task ShowOrganizationDialogAsync(bool category)
    {
        TextBox name = new() { Header = category ? "Category name" : "Project name", MaxLength = 200 };
        ComboBox owner = new()
        {
            Header = category ? "Scope" : "Owning Stream",
            ItemsSource = ViewModel.Streams,
            DisplayMemberPath = nameof(StreamCard.Name),
            SelectedValuePath = nameof(StreamCard.Id),
            PlaceholderText = category ? "Global Category" : "Standalone Project",
        };
        ToggleSwitch billable = new() { Header = "Billable by default", Visibility = category ? Visibility.Visible : Visibility.Collapsed };
        NumberBox rate = new() { Header = "Hourly rate", Minimum = 0, Maximum = 10_000_000, Visibility = category ? Visibility.Visible : Visibility.Collapsed };
        ComboBox currency = new()
        {
            Header = "Currency",
            ItemsSource = CurrencyCatalog.All,
            DisplayMemberPath = nameof(CurrencyOption.Label),
            SelectedValuePath = nameof(CurrencyOption.Code),
            SelectedValue = CurrencyCatalog.RegionalCode,
            IsTextSearchEnabled = true,
            Visibility = category ? Visibility.Visible : Visibility.Collapsed,
        };
        StackPanel content = new() { Spacing = 8 };
        content.Children.Add(name);
        content.Children.Add(owner);
        content.Children.Add(billable);
        content.Children.Add(rate);
        content.Children.Add(currency);
        ContentDialog dialog = new()
        {
            XamlRoot = XamlRoot,
            Title = category ? "Add Category" : "Add Project",
            Content = content,
            PrimaryButtonText = "Add",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            Guid? streamId = owner.SelectedValue is Guid id ? id : null;
            CurrencyOption? selectedCurrency = currency.SelectedItem as CurrencyOption;
            long? hourlyRate = billable.IsOn && selectedCurrency is not null && !double.IsNaN(rate.Value)
                ? CurrencyCatalog.ToMinorUnits(rate.Value, selectedCurrency)
                : null;
            string? error = category
                ? await ViewModel.CreateCategoryAsync(
                    name.Text, streamId, billable.IsOn,
                    hourlyRate,
                    billable.IsOn ? selectedCurrency?.Code : null)
                : await ViewModel.CreateProjectAsync(name.Text, streamId);
            ErrorBar.IsOpen = error is not null;
            ErrorBar.Title = error ?? string.Empty;
        }
    }

    private async void OnArchive(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: Guid streamId })
        {
            return;
        }

        ContentDialog dialog = new()
        {
            XamlRoot = XamlRoot,
            Title = "Archive this Stream?",
            Content = "Its historical Sessions remain intact and it can be restored from Archive.",
            PrimaryButtonText = "Archive",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            await ViewModel.ArchiveStreamAsync(streamId);
        }
    }

    private async ValueTask ConfirmArchiveAsync(Guid streamId, string name)
    {
        ContentDialog dialog = new()
        {
            XamlRoot = XamlRoot,
            Title = $"Archive {name}?",
            Content = "Its historical Sessions remain available, and you can restore the Stream later.",
            PrimaryButtonText = "Archive",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            await ViewModel.ArchiveStreamAsync(streamId);
        }
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        bool narrow = e.NewSize.Width < 980;
        if (narrow)
        {
            HeaderGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            HeaderGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
            HeaderGrid.ColumnDefinitions[2].Width = new GridLength(0);
            Grid.SetColumn(HeaderTitle, 0); Grid.SetColumnSpan(HeaderTitle, 2);
            Grid.SetRow(NewStreamName, 1); Grid.SetColumn(NewStreamName, 0);
            Grid.SetRow(HeaderActions, 1); Grid.SetColumn(HeaderActions, 1);
        }
        else
        {
            HeaderGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            HeaderGrid.ColumnDefinitions[1].Width = new GridLength(280);
            HeaderGrid.ColumnDefinitions[2].Width = GridLength.Auto;
            Grid.SetRow(HeaderTitle, 0); Grid.SetColumn(HeaderTitle, 0); Grid.SetColumnSpan(HeaderTitle, 1);
            Grid.SetRow(NewStreamName, 0); Grid.SetColumn(NewStreamName, 1);
            Grid.SetRow(HeaderActions, 0); Grid.SetColumn(HeaderActions, 2);
        }
    }

    private void ShowError(string? error)
    {
        ErrorBar.IsOpen = !string.IsNullOrWhiteSpace(error);
        ErrorBar.Title = error ?? string.Empty;
    }
}
