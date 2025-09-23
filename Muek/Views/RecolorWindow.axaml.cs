using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Muek.Models;

namespace Muek.Views;

public partial class RecolorWindow : Window
{
    public event EventHandler<Color>? Submit;
    public RecolorWindow()
    {
        InitializeComponent();
        // MyColorView.Palette = new FlatColorPalette();
    }
    private void SubmitBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        Submit?.Invoke(this,MyColorView.SelectedColor);
        
        var mainWindow = ViewHelper.GetMainWindow();
        mainWindow.Mixer.Refresh();
        
        Close();
    }

    private void CancelBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            this.BeginMoveDrag(e);
        }
    }
}