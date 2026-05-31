using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LaunchForge.Models;
using LaunchForge.Services;

namespace LaunchForge.ViewModels;

public partial class SteamPickerViewModel : ObservableObject
{
    private readonly SteamRegistryService    _registry;
    private readonly SteamAppCatalogService  _catalog;

    [ObservableProperty] private string     _filterText    = string.Empty;
    [ObservableProperty] private SteamGame? _selectedGame;
    [ObservableProperty] private string     _statusText    = string.Empty;
    [ObservableProperty] private bool       _isRefreshing;
    [ObservableProperty] private string     _manualAppId   = string.Empty; // fallback when list has no match

    public ObservableCollection<SteamGame> AllGames { get; } = [];
    public ICollectionView                 FilteredGames  { get; }

    public SteamPickerViewModel(SteamRegistryService registry, SteamAppCatalogService catalog)
    {
        _registry = registry;
        _catalog  = catalog;

        FilteredGames = CollectionViewSource.GetDefaultView(AllGames);
        FilteredGames.Filter = obj =>
            obj is SteamGame g &&
            (string.IsNullOrEmpty(FilterText) ||
             g.Name.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
             g.AppId.Contains(FilterText, StringComparison.OrdinalIgnoreCase));
    }

    partial void OnFilterTextChanged(string value) => FilteredGames.Refresh();

    public void Load()
    {
        AllGames.Clear();

        // Installed games from registry (fast, always available)
        var installed = _registry.GetInstalledGames();
        foreach (var g in installed)
            AllGames.Add(g);

        // Merge catalog (if loaded): add catalog entries not already in installed list
        var installedIds = installed.Select(g => g.AppId).ToHashSet();
        foreach (var g in _catalog.GetCachedApps())
        {
            if (!installedIds.Contains(g.AppId))
                AllGames.Add(g);
        }

        UpdateStatusText();
    }

    [RelayCommand]
    private async Task RefreshCatalogAsync()
    {
        IsRefreshing = true;
        StatusText   = "Fetching Steam catalog...";
        try
        {
            await _catalog.RefreshAsync();
            Load();
        }
        catch (Exception ex)
        {
            StatusText = $"Refresh failed: {ex.Message}";
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private void UpdateStatusText()
    {
        if (_catalog.CachedAt is null)
            StatusText = $"{AllGames.Count} installed games shown. Click Refresh to load full catalog.";
        else
        {
            var age  = DateTime.UtcNow - _catalog.CachedAt.Value;
            var desc = age.TotalDays >= 1 ? $"{(int)age.TotalDays}d ago" : $"{(int)age.TotalHours}h ago";
            StatusText = $"{AllGames.Count} games. Catalog last updated {desc}.";
        }
    }
}
