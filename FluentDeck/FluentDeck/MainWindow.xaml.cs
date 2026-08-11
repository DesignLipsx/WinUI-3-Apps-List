using FluentDeck.Helpers;
using FluentDeck.Models;
using FluentDeck.Pages.Apps;
using FluentDeck.Pages.Diagnostics;
using FluentDeck.Pages.Emoji;
using FluentDeck.Pages.Icons;
using FluentDeck.Pages.Publish;
using FluentDeck.Pages.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace FluentDeck;

public partial class MainWindow : Window
{
    private string? _readmePath;
    public ObservableCollection<AppItem> RecentApps { get; } = new();
    public ObservableCollection<CategoryNode> CategoryNodes { get; } = new();

    private List<FlatAppItem> _allFlatApps = new();
    private Dictionary<string, string> _logos = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, string> GetLogos() => _logos;

    public event EventHandler? DataLoaded;

    public MainWindow()
    {
        InitializeComponent();
        RootGrid.DataContext = this;
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        ConfigureWindow();

        // Load theme on startup
        RootGrid.RequestedTheme = SettingsPage.GetSavedTheme();

        _readmePath = FindReadmePath();

        // Set AppsPage as default
        MainNav.SelectedItem = AppsItem;
        NavigateTo("Apps");

        UpdateDeveloperModeVisibility();

        _ = LoadAllCategoriesAndAppsAsync();
    }

    public List<FlatAppItem> GetFlatApps() => _allFlatApps;

    public int UniqueAppsCount => _allFlatApps == null ? 0 : _allFlatApps
        .Where(a => !a.GroupKey.Contains("Newly Added") &&
                    !a.GroupKey.Contains("Best Implementation"))
        .Select(a => a.Url)
        .Distinct()
        .Count();

    private void ConfigureWindow()
    {
        try
        {
            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "WindowIcon.ico");
            if (File.Exists(iconPath)) appWindow.SetIcon(iconPath);

            // Maximize window by default
            if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter overlappedPresenter)
            {
                overlappedPresenter.Maximize();
            }
        }
        catch { }
    }

    public string? FindReadmePath() => FindDataJsonPath();

    public string? FindDataJsonPath()
    {
        string? exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath)) exePath = System.AppContext.BaseDirectory;

        string? dir = Path.GetDirectoryName(exePath);
        while (!string.IsNullOrEmpty(dir))
        {
            string assetJsonPath = Path.Combine(dir, "Assets", "data", "apps_data.json");
            if (File.Exists(assetJsonPath)) return assetJsonPath;

            string nestedAssetJsonPath = Path.Combine(dir, "FluentDeck", "Assets", "data", "apps_data.json");
            if (File.Exists(nestedAssetJsonPath)) return nestedAssetJsonPath;

            string rootJsonPath = Path.Combine(dir, "apps_data.json");
            if (File.Exists(rootJsonPath)) return rootJsonPath;

            string readmePath = Path.Combine(dir, "README.md");
            if (File.Exists(readmePath)) return readmePath;

            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }

    public async System.Threading.Tasks.Task LoadAllCategoriesAndAppsAsync()
    {
        string? dataPath = FindDataJsonPath();
        if (string.IsNullOrEmpty(dataPath) || !File.Exists(dataPath)) return;

        var (recent, flat, nodes, loadedLogos) = await System.Threading.Tasks.Task.Run(() =>
        {
            var resultRecent = new List<AppItem>();
            var resultFlat = new List<FlatAppItem>();
            var parsedNodes = new List<CategoryNode>();
            var logos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            string directory = Path.GetDirectoryName(dataPath) ?? "";
            string jsonPath = dataPath;

            if (File.Exists(jsonPath) && jsonPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                var root = AppsDataParser.LoadRoot(jsonPath);
                if (root != null)
                {
                    var (pNodes, _, pFlat, pLogos) = AppsDataParser.ParseData(root);
                    parsedNodes = pNodes;
                    resultFlat = pFlat;
                    logos = pLogos;

                    if (root.NewlyAdded != null)
                    {
                        foreach (var item in root.NewlyAdded)
                        {
                            resultRecent.Add(new AppItem
                            {
                                Name = item.Name,
                                Url = item.Url,
                                Indicator = item.Indicator,
                                IsFoss = item.IsFoss,
                                IsPaid = item.IsPaid,
                                IsPlanned = item.IsPlanned,
                                LogoUrl = item.Logo
                            });
                        }
                    }
                    return (resultRecent, resultFlat, parsedNodes, logos);
                }
            }

            return (resultRecent, resultFlat, parsedNodes, logos);
        });

        RecentApps.Clear();
        foreach (var a in recent) RecentApps.Add(a);
        _allFlatApps = flat;
        _logos = loadedLogos;
        CategoryNodes.Clear();
        foreach (var n in nodes) CategoryNodes.Add(n);

        DataLoaded?.Invoke(this, EventArgs.Empty);
    }

    private void MainNav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        string tag = "Apps";
        if (args.IsSettingsSelected)
        {
            tag = "Settings";
        }
        else if (args.SelectedItemContainer != null)
        {
            tag = args.SelectedItemContainer.Tag?.ToString() ?? "Apps";
        }

        NavigateTo(tag);

        UpdateIconGeometries(tag);
    }

    private void UpdateIconGeometries(string activeTag)
    {
        const string appsReg = "M18.4923 2.33088L21.671 5.50966C22.5497 6.38834 22.5497 7.81296 21.671 8.69164L19.0866 11.2756C20.1696 11.438 21 12.3723 21 13.5006V18.7506C21 19.9932 19.9926 21.0006 18.75 21.0006H5.25C4.00736 21.0006 3 19.9932 3 18.7506V5.25055C3 4.00791 4.00736 3.00055 5.25 3.00055H10.5C11.6289 3.00055 12.5637 3.83201 12.7253 4.91596L15.3103 2.33088C16.189 1.45221 17.6136 1.45221 18.4923 2.33088ZM4.5 18.7506C4.5 19.1648 4.83579 19.5006 5.25 19.5006L11.249 19.4999L11.25 12.7506L4.5 12.7499V18.7506ZM12.749 19.4999L18.75 19.5006C19.1642 19.5006 19.5 19.1648 19.5 18.7506V13.5006C19.5 13.0863 19.1642 12.7506 18.75 12.7506L12.749 12.7499V19.4999ZM10.5 4.50055H5.25C4.83579 4.50055 4.5 4.83634 4.5 5.25055V11.2499H11.25V5.25055C11.25 4.83634 10.9142 4.50055 10.5 4.50055ZM12.75 9.30988V11.2506L14.69 11.2499L12.75 9.30988ZM16.3709 3.39154L13.1922 6.57032C12.8993 6.86321 12.8993 7.33808 13.1922 7.63098L16.3709 10.8097C16.6638 11.1026 17.1387 11.1026 17.4316 10.8097L20.6104 7.63098C20.9033 7.33808 20.9033 6.86321 20.6104 6.57032L17.4316 3.39154C17.1387 3.09865 16.6638 3.09865 16.3709 3.39154Z";
        const string appsFill = "M18.4923 2.33034L21.671 5.50911C22.5497 6.38779 22.5497 7.81241 21.671 8.69109L19.0866 11.275C20.1696 11.4375 21 12.3718 21 13.5V18.75C21 19.9926 19.9926 21 18.75 21H5.25C4.00736 21 3 19.9926 3 18.75V5.25001C3 4.00736 4.00736 3.00001 5.25 3.00001H10.5C11.6289 3.00001 12.5637 3.83146 12.7253 4.91541L15.3103 2.33034C16.189 1.45166 17.6136 1.45166 18.4923 2.33034ZM4.5 18.75C4.5 19.1642 4.83579 19.5 5.25 19.5L11.249 19.4993L11.25 12.75L4.5 12.7493V18.75ZM12.749 19.4993L18.75 19.5C19.1642 19.5 19.5 19.1642 19.5 18.75V13.5C19.5 13.0858 19.1642 12.75 18.75 12.75L12.749 12.7493V19.4993ZM10.5 4.50001H5.25C4.83579 4.50001 4.5 4.83579 4.5 5.25001V11.2493H11.25V5.25001C11.25 4.83579 10.9142 4.50001 10.5 4.50001ZM12.75 9.30933V11.25L14.69 11.2493L12.75 9.30933Z";

        const string emojiReg = "M12 1.99805C17.5237 1.99805 22.0015 6.47589 22.0015 11.9996C22.0015 17.5233 17.5237 22.0011 12 22.0011C6.47626 22.0011 1.99841 17.5233 1.99841 11.9996C1.99841 6.47589 6.47626 1.99805 12 1.99805ZM12 3.49805C7.30469 3.49805 3.49841 7.30432 3.49841 11.9996C3.49841 16.6949 7.30469 20.5011 12 20.5011C16.6952 20.5011 20.5015 16.6949 20.5015 11.9996C20.5015 7.30432 16.6952 3.49805 12 3.49805ZM8.4617 14.7829C9.31084 15.8606 10.6019 16.5012 11.9999 16.5012C13.3962 16.5012 14.6856 15.8624 15.5349 14.7871C15.7916 14.462 16.2633 14.4066 16.5883 14.6634C16.9134 14.9201 16.9688 15.3917 16.712 15.7168C15.5813 17.1485 13.8601 18.0012 11.9999 18.0012C10.1373 18.0012 8.41408 17.1462 7.28348 15.7112C7.02713 15.3859 7.08307 14.9143 7.40843 14.658C7.73379 14.4016 8.20535 14.4576 8.4617 14.7829ZM9.00041 8.75024C9.69037 8.75024 10.2497 9.30956 10.2497 9.99953C10.2497 10.6895 9.69037 11.2488 9.00041 11.2488C8.31045 11.2488 7.75112 10.6895 7.75112 9.99953C7.75112 9.30956 8.31045 8.75024 9.00041 8.75024ZM15.0004 8.75024C15.6904 8.75024 16.2497 9.30956 16.2497 9.99953C16.2497 10.6895 15.6904 11.2488 15.0004 11.2488C14.3104 11.2488 13.7511 10.6895 13.7511 9.99953C13.7511 9.30956 14.3104 8.75024 15.0004 8.75024Z";
        const string emojiFill = "M12 1.99805C17.5237 1.99805 22.0015 6.47589 22.0015 11.9996C22.0015 17.5233 17.5237 22.0011 12 22.0011C6.47626 22.0011 1.99841 17.5233 1.99841 11.9996C1.99841 6.47589 6.47626 1.99805 12 1.99805ZM8.4617 14.7829C8.20535 14.4576 7.73379 14.4016 7.40843 14.658C7.08307 14.9143 7.02713 15.3859 7.28348 15.7112C8.41408 17.1462 10.1373 18.0012 11.9999 18.0012C13.8601 18.0012 15.5813 17.1485 16.712 15.7168C16.9688 15.3917 16.9134 14.9201 16.5883 14.6634C16.2633 14.4066 15.7916 14.462 15.5349 14.7871C14.6856 15.8624 13.3962 16.5012 11.9999 16.5012C10.6019 16.5012 9.31084 15.8606 8.4617 14.7829ZM9.00041 8.75024C8.31045 8.75024 7.75112 9.30956 7.75112 9.99953C7.75112 10.6895 8.31045 11.2488 9.00041 11.2488C9.69037 11.2488 10.2497 10.6895 10.2497 9.99953C10.2497 9.30956 9.69037 8.75024 9.00041 8.75024ZM15.0004 8.75024C14.3104 8.75024 13.7511 9.30956 13.7511 9.99953C13.7511 10.6895 14.3104 11.2488 15.0004 11.2488C15.6904 11.2488 16.2497 10.6895 16.2497 9.99953C16.2497 9.30956 15.6904 8.75024 15.0004 8.75024Z";

        const string iconsReg = "M13 5.25C13 4.00736 14.0074 3 15.25 3H18.75C19.9926 3 21 4.00736 21 5.25V8.75C21 9.99264 19.9926 11 18.75 11H15.25C14.0074 11 13 9.99264 13 8.75V5.25ZM15.25 4.5C14.8358 4.5 14.5 4.83579 14.5 5.25V8.75C14.5 9.16421 14.8358 9.5 15.25 9.5H18.75C19.1642 9.5 19.5 9.16421 19.5 8.75V5.25C19.5 4.83579 19.1642 4.5 18.75 4.5H15.25ZM8.44969 3.89843C7.84719 2.70052 6.15281 2.70052 5.55031 3.89843L3.17842 8.61429C2.629 9.70668 3.41489 11 4.62811 11L9.37189 11C10.5851 11 11.371 9.70668 10.8216 8.61429L8.44969 3.89843ZM6.89036 4.57242C6.91066 4.53207 6.92838 4.52044 6.93681 4.51545C6.95022 4.50751 6.97195 4.5 7 4.5C7.02805 4.5 7.04978 4.50751 7.06318 4.51545C7.07161 4.52044 7.08934 4.53207 7.10964 4.57242L9.48152 9.28828C9.49857 9.32217 9.50082 9.34594 9.4998 9.36416C9.49856 9.38629 9.49124 9.4125 9.47584 9.43785C9.46044 9.46319 9.44231 9.47894 9.42831 9.48716C9.41795 9.49324 9.4027 9.5 9.37189 9.5L4.62811 9.5C4.5973 9.5 4.58205 9.49324 4.57169 9.48716C4.55769 9.47894 4.53956 9.46319 4.52416 9.43785C4.50876 9.4125 4.50144 9.38629 4.5002 9.36416C4.49918 9.34594 4.50143 9.32217 4.51848 9.28829L6.89036 4.57242ZM4.5 17C4.5 15.6193 5.61929 14.5 7 14.5C8.38071 14.5 9.5 15.6193 9.5 17C9.5 18.3807 8.38071 19.5 7 19.5C5.61929 19.5 4.5 18.3807 4.5 17ZM7 13C4.79086 13 3 14.7909 3 17C3 19.2091 4.79086 21 7 21C9.20914 21 11 19.2091 11 17C11 14.7909 9.20914 13 7 13ZM17.625 12.7448C17.2382 12.5215 16.7618 12.5215 16.375 12.7448L13.6274 14.3311C13.2407 14.5544 13.0024 14.9671 13.0024 15.4137V18.5863C13.0024 19.0329 13.2407 19.4456 13.6274 19.6689L16.375 21.2552C16.7618 21.4785 17.2382 21.4785 17.625 21.2552L20.3726 19.6689C20.7593 19.4456 20.9976 19.0329 20.9976 18.5863V15.4137C20.9976 14.9671 20.7593 14.5544 20.3726 14.3311L17.625 12.7448ZM14.5024 15.558L17 14.116L19.4976 15.558V18.442L17 19.884L14.5024 18.442V15.558Z";
        const string iconsFill = "M15.25 3C14.0074 3 13 4.00736 13 5.25V8.75C13 9.99264 14.0074 11 15.25 11H18.75C19.9926 11 21 9.99264 21 8.75V5.25C21 4.00736 19.9926 3 18.75 3H15.25ZM8.44969 3.89843C7.84719 2.70052 6.15281 2.70052 5.55031 3.89843L3.17842 8.61429C2.629 9.70668 3.41489 11 4.62811 11L9.37189 11C10.5851 11 11.371 9.70668 10.8216 8.61429L8.44969 3.89843ZM3 17C3 14.7909 4.79086 13 7 13C9.20914 13 11 14.7909 11 17C11 19.2091 9.20914 21 7 21C4.79086 21 3 19.2091 3 17ZM16.375 12.7448C16.7618 12.5215 17.2383 12.5215 17.625 12.7448L20.3726 14.3311C20.7594 14.5544 20.9976 14.9671 20.9976 15.4137V18.5863C20.9976 19.0329 20.7594 19.4456 20.3726 19.6689L17.625 21.2552C17.2383 21.4785 16.7618 21.4785 16.375 21.2552L13.6274 19.6689C13.2407 19.4456 13.0024 19.0329 13.0024 18.5863V15.4137C13.0024 14.9671 13.2407 14.5544 13.6274 14.3311L16.375 12.7448Z";

        const string healthReg = "M18.7488 3C19.9915 3 20.9988 4.00736 20.9988 5.25V18.7523C20.9988 19.9949 19.9915 21.0023 18.7488 21.0023H5.25C4.00736 21.0023 3 19.9949 3 18.7523V5.25C3 4.00736 4.00736 3 5.25 3H18.7488ZM18.7488 4.5H5.25C4.83579 4.5 4.5 4.83579 4.5 5.25V18.7523C4.5 19.1665 4.83579 19.5023 5.25 19.5023H18.7488C19.163 19.5023 19.4988 19.1665 19.4988 18.7523V5.25C19.4988 4.83579 19.163 4.5 18.7488 4.5ZM8.25508 11.5004L9.81175 7.94933C10.0631 7.37605 10.8475 7.35474 11.1451 7.86895L11.1949 7.97163L13.5762 13.9183L14.5794 11.9146C14.6907 11.6925 14.9033 11.542 15.145 11.5078L15.2501 11.5004H17.25C17.6642 11.5004 18 11.8362 18 12.2504C18 12.6301 17.7178 12.9439 17.3518 12.9936L17.25 13.0004H15.7133L14.1706 16.0814C13.8981 16.6257 13.1407 16.6269 12.8521 16.1245L12.8037 16.0244L10.4674 10.1899L9.4321 12.5516C9.3275 12.7902 9.10803 12.9549 8.85531 12.9923L8.7452 13.0004H6.75C6.33579 13.0004 6 12.6647 6 12.2504C6 11.8707 6.28215 11.5569 6.64823 11.5073L6.75 11.5004H8.25508L9.81175 7.94933L8.25508 11.5004Z";
        const string healthFill = "M18.7488 3C19.9915 3 20.9988 4.00736 20.9988 5.25V18.7523C20.9988 19.9949 19.9915 21.0023 18.7488 21.0023H5.25C4.00736 21.0023 3 19.9949 3 18.7523V5.25C3 4.00736 4.00736 3 5.25 3H18.7488ZM9.81175 7.94933L8.25508 11.5004H6.75C6.33579 11.5004 6 11.8362 6 12.2504C6 12.6647 6.33579 13.0004 6.75 13.0004H8.7452C9.04298 13.0004 9.31255 12.8243 9.4321 12.5516L10.4674 10.1899L12.8037 16.0244C13.0442 16.6249 13.881 16.6598 14.1706 16.0814L15.7133 13.0004H17.25C17.6642 13.0004 18 12.6647 18 12.2504C18 11.8362 17.6642 11.5004 17.25 11.5004H15.2501C14.9661 11.5004 14.7066 11.6608 14.5794 11.9146L13.5762 13.9183L11.1949 7.97163C10.9477 7.35423 10.0788 7.34022 9.81175 7.94933Z";

        const string publishReg = "M12.2803 2.21967C11.9874 1.92678 11.5126 1.92678 11.2197 2.21967L6.21967 7.21967C5.92678 7.51256 5.92678 7.98744 6.21967 8.28033C6.51256 8.57322 6.98744 8.57322 7.28033 8.28033L11 4.56066V18.25C11 18.6642 11.3358 19 11.75 19C12.1642 19 12.5 18.6642 12.5 18.25V4.56066L16.2197 8.28033C16.5126 8.57322 16.9874 8.57322 17.2803 8.28033C17.5732 7.98744 17.5732 7.51256 17.2803 7.21967L12.2803 2.21967ZM5.25 20.5C4.83579 20.5 4.5 20.8358 4.5 21.25C4.5 21.6642 4.83579 22 5.25 22H18.25C18.6642 22 19 21.6642 19 21.25C19 20.8358 18.6642 20.5 18.25 20.5H5.25Z";
        const string publishFill = "M12.7071 2.29289C12.3166 1.90237 11.6834 1.90237 11.2929 2.29289L6.29289 7.29289C5.90237 7.68342 5.90237 8.31658 6.29289 8.70711C6.68342 9.09763 7.31658 9.09763 7.70711 8.70711L11 5.41421V18C11 18.5523 11.4477 19 12 19C12.5523 19 13 18.5523 13 18V5.41421L16.2929 8.70711C16.6834 9.09763 17.3166 9.09763 17.7071 8.70711C18.0976 8.31658 18.0976 7.68342 17.7071 7.29289L12.7071 2.29289ZM5.25 20.5C4.83579 20.5 4.5 20.8358 4.5 21.25C4.5 21.6642 4.83579 22 5.25 22H18.75C19.1642 22 19.5 21.6642 19.5 21.25C19.5 20.8358 19.1642 20.5 18.75 20.5H5.25Z";

        if (AppsIcon != null)
        {
            AppsIcon.Data = (Microsoft.UI.Xaml.Media.Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Microsoft.UI.Xaml.Media.Geometry), activeTag == "Apps" ? appsFill : appsReg);
        }
        if (EmojiIcon != null)
        {
            EmojiIcon.Data = (Microsoft.UI.Xaml.Media.Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Microsoft.UI.Xaml.Media.Geometry), activeTag == "Emoji" ? emojiFill : emojiReg);
        }
        if (IconsIcon != null)
        {
            IconsIcon.Data = (Microsoft.UI.Xaml.Media.Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Microsoft.UI.Xaml.Media.Geometry), activeTag == "Icons" ? iconsFill : iconsReg);
        }
        if (DiagnosticsIcon != null)
        {
            DiagnosticsIcon.Data = (Microsoft.UI.Xaml.Media.Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Microsoft.UI.Xaml.Media.Geometry), activeTag == "Diagnostics" ? healthFill : healthReg);
        }
        if (PublishIcon != null)
        {
            PublishIcon.Data = (Microsoft.UI.Xaml.Media.Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Microsoft.UI.Xaml.Media.Geometry), activeTag == "Publish" ? publishFill : publishReg);
        }
    }

    public void UpdateDeveloperModeVisibility()
    {
        bool devMode = FeatureManager.IsDeveloperMode;
        Visibility devVisibility = devMode ? Visibility.Visible : Visibility.Collapsed;

        if (DiagnosticsItem != null) DiagnosticsItem.Visibility = devVisibility;
        if (PublishItem != null) PublishItem.Visibility = devVisibility;

        if (!devMode && MainNav != null)
        {
            if (MainNav.SelectedItem is NavigationViewItem selectedItem &&
                (selectedItem == DiagnosticsItem || selectedItem == PublishItem))
            {
                MainNav.SelectedItem = AppsItem;
                NavigateTo("Apps");
            }
        }
    }

    private void NavigateTo(string tag)
    {
        if (tag == "Apps")
            ContentFrame.Navigate(typeof(AppsPage));
        else if (tag == "Diagnostics")
            ContentFrame.Navigate(typeof(DiagnosticsPage));
        else if (tag == "Publish")
            ContentFrame.Navigate(typeof(PublishPage));
        else if (tag == "Settings")
            ContentFrame.Navigate(typeof(SettingsPage));
        else if (tag == "Emoji")
            ContentFrame.Navigate(typeof(EmojiPage));
        else if (tag == "Icons")
            ContentFrame.Navigate(typeof(IconsPage));
    }
}
