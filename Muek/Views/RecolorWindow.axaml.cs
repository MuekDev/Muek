using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Muek.Views;

public partial class RecolorWindow : Window
{
    public event EventHandler<string>? Submit;
    public RecolorWindow()
    {
        InitializeComponent();
    }
    private void SubmitBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        
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