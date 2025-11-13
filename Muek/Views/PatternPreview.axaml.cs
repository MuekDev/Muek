using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Muek.Models;

namespace Muek.Views;

public partial class PatternPreview : UserControl
{
    public static readonly StyledProperty<IBrush> BackgroundColorProperty = AvaloniaProperty.Register<PatternPreview, IBrush>(
        nameof(BackgroundColor));

    public IBrush BackgroundColor
    {
        get => GetValue(BackgroundColorProperty);
        set => SetValue(BackgroundColorProperty, value);
    }
    
    public int Index;
    
    public List<PianoRoll.Note> Notes = new();

    private bool _isHover = false;
    private bool _isDragging = false;
    
    public PatternPreview()
    {
        InitializeComponent();
        ClipToBounds = false;
        BackgroundColor = Brushes.YellowGreen;
    }
    
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.DrawRectangle(BackgroundColor, null, new Rect(0, 0, Bounds.Width, Bounds.Height));
        if (_isHover)
        {
            context.DrawRectangle(new SolidColorBrush(Colors.Black,.1), null, new Rect(0, 0, Bounds.Width, Bounds.Height), 10D, 15D);
        }

        if (_isDragging)
        {
            
        }
        
        if (Notes.Count > 0)
        {
            double noteHeight;
            double noteWidth;
            int noteMax = Notes[0].Name;
            int noteMin = noteMax;
            double noteFirst = Notes[0].StartTime;
            double noteLast = Notes[0].EndTime;
            foreach (var note in Notes)
            {
                var position = note.Name;
                noteMin = int.Min(position, noteMin);
                noteMax = int.Max(position, noteMax);
                noteFirst = double.Min(noteFirst, note.StartTime);
                noteLast = double.Max(noteLast, note.EndTime);
            }

            noteHeight = Bounds.Height / (noteMax + 1 - noteMin);
            noteWidth = Bounds.Width / (noteLast - noteFirst);
            // Console.WriteLine($"noteWidth:{noteWidth}");
            // Console.WriteLine($"noteHeight:{noteHeight}");
            
            foreach (var note in Notes)
            {
                var position = note.Name;
                var background = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0.9,0.5, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1.0,0.5, RelativeUnit.Relative),
                };
                background.GradientStops.Add(new GradientStop(Colors.White, 0.0));
                background.GradientStops.Add(new GradientStop(Colors.Transparent, 1.0));
                context.FillRectangle(background,
                    new Rect(
                        noteWidth * .6 * (note.StartTime - noteFirst) + Bounds.Width*.2,
                        Bounds.Height*.6 - (position - noteMin + 1) * noteHeight * .6 + Bounds.Height*.2,
                        noteWidth * (note.EndTime - note.StartTime) * .6,
                        noteHeight * .6),
                    (float)(noteHeight * .1));
                // Console.WriteLine("Drew");
                // Console.WriteLine(new Rect(noteWidth * (note.StartTime - noteFirst), Height - (position - noteMin) * noteHeight, noteWidth *
                //     (note.EndTime - note.StartTime), noteHeight));
            }
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        // Console.WriteLine("Pattern Notes:");
        // foreach (var note in Notes)
        // {
        // Console.WriteLine(note.Name);
        // }

        _isDragging = true;
        // e.Handled = true;
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        _isHover = true;
        InvalidateVisual();
        
        e.Handled = true;
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _isHover = false;
        InvalidateVisual();
            new Animation
            {
                Duration = TimeSpan.FromMilliseconds(500),
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
                                Property = TranslateTransform.XProperty,
                                Value = 0.0
                            }
                        }
                    }
                }
            }.RunAsync(this);
        e.Handled = true;
    }

    // public void OpenPianoRoll()
    // {
    //     ViewHelper.GetMainWindow().PianoRollWindow.EditArea.Notes = Notes;
    // }
    
    
    
}