using System;
using Avalonia.Controls.ApplicationLifetimes;
using Muek.Views;

namespace Muek.Models;

public class ViewHelper
{
    public static MainWindow GetMainWindow()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow as MainWindow ?? throw new NullReferenceException();
        }
        else
        {
            throw new NotSupportedException();
        }
    }
}