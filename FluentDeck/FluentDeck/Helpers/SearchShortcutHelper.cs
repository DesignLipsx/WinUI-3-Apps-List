using FluentDeck.Pages.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace FluentDeck.Helpers;

public static class SearchShortcutHelper
{
    public static void ApplySearchShortcut(Page page, AutoSuggestBox searchBox)
    {
        if (page == null || searchBox == null) return;

        page.KeyboardAccelerators.Clear();
        string shortcut = SettingsManager.GetSearchShortcut();

        var accelerator = new KeyboardAccelerator();
        if (shortcut == "F3")
        {
            accelerator.Key = VirtualKey.F3;
            accelerator.Modifiers = VirtualKeyModifiers.None;
        }
        else // Default: Ctrl+F
        {
            accelerator.Key = VirtualKey.F;
            accelerator.Modifiers = VirtualKeyModifiers.Control;
        }

        accelerator.Invoked += (s, e) =>
        {
            searchBox.Focus(FocusState.Programmatic);
            e.Handled = true;
        };

        page.KeyboardAccelerators.Add(accelerator);
    }
}
