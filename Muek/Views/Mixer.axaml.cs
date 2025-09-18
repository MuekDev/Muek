using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Muek.Services;
using Muek.ViewModels;

namespace Muek.Views;

public partial class Mixer : UserControl
{
    //TODO
    //Name
    //Color

    public Mixer()
    {
        InitializeComponent();
        Console.WriteLine("Mixer Initialized");
    }

    private void HideMixer(object? sender, RoutedEventArgs e)
    {
        this.IsVisible = false;
    }
}