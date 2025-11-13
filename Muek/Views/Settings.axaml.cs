using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Muek.Services;

namespace Muek.Views;

public partial class Settings : UserControl
{
    public bool IsShowing { get; set; } = false;
    public double MaxWidth { get; set; } = 400;

    public Settings()
    {
        InitializeComponent();
        Width = 0;
    }

    public void Show()
    {
        if (Width <= 0)
        {
            new Animation
            {
                Duration = TimeSpan.FromMilliseconds(UiStateService.AnimationDuration),
                FillMode = FillMode.Forward,
                Easing = Easing.Parse("CubicEaseOut"),
                Delay = TimeSpan.FromMilliseconds(UiStateService.AnimationDelay),
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(1),
                        Setters =
                        {
                            new Setter
                            {
                                Property = WidthProperty,
                                Value = MaxWidth
                            }
                        }
                    }
                }
            }.RunAsync(this);
            IsShowing = true;
        }
    }

    public void Hide()
    {
        if (Width >= MaxWidth)
        {
            new Animation
            {
                Duration = TimeSpan.FromMilliseconds(UiStateService.AnimationDuration + UiStateService.AnimationDelay),
                FillMode = FillMode.Forward,
                Easing = Easing.Parse("CubicEaseOut"),
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(1),
                        Setters =
                        {
                            new Setter
                            {
                                Property = WidthProperty,
                                Value = 0.0
                            }
                        }
                    }
                }
            }.RunAsync(this);
            IsShowing = false;
        }
    }
}