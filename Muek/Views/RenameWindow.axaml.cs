using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Muek.Views;

public partial class RenameWindow : Window
{
    public event EventHandler<string>? Submit;
    
    public RenameWindow()
    {
        InitializeComponent();
    }

    private void SubmitBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (NameBox.Text != null) Submit?.Invoke(this, NameBox.Text);
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