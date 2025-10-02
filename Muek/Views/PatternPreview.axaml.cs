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
    public int Index;
    
    public List<PianoRoll.Note> Notes = new();

    private bool _isHover = false;
    
    public PatternPreview()
    {
        InitializeComponent();
        Width = 150;
        Height = 50;
        ClipToBounds = false;
    }
    
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.DrawRectangle(Brushes.YellowGreen, null, new Rect(0, 0, Width, Height),10D,15D);
        
        if (Notes.Count > 0)
        {
            double noteHeight = Height;
            double noteWidth = Width;
            int noteMax = new PianoRoll().NoteNameToIndex(Notes[0].Name);
            int noteMin = noteMax;
            double noteFirst = Notes[0].StartTime;
            double noteLast = Notes[0].EndTime;
            foreach (var note in Notes)
            {
                var position = new PianoRoll().NoteNameToIndex(note.Name);
                noteMin = int.Min(position, noteMin);
                noteMax = int.Max(position, noteMax);
                noteFirst = double.Min(noteFirst, note.StartTime);
                noteLast = double.Max(noteLast, note.EndTime);
            }

            noteHeight = Height / (noteMax + 1 - noteMin);
            noteWidth = Width / (noteLast - noteFirst);
            Console.WriteLine($"noteWidth:{noteWidth}");
            Console.WriteLine($"noteHeight:{noteHeight}");
            
            foreach (var note in Notes)
            {
                var position = new PianoRoll().NoteNameToIndex(note.Name);
                var background = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0.9,0.5, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1.0,0.5, RelativeUnit.Relative),
                };
                background.GradientStops.Add(new GradientStop(Colors.White, 0.0));
                background.GradientStops.Add(new GradientStop(Colors.Transparent, 1.0));
                context.FillRectangle(background,
                    new Rect(
                        noteWidth * .6 * (note.StartTime - noteFirst) + Width*.2,
                        Height*.6 - (position - noteMin + 1) * noteHeight * .6 + Height*.2,
                        noteWidth * (note.EndTime - note.StartTime) * .6,
                        noteHeight * .6),
                    (float)(noteHeight * .1));
                Console.WriteLine("Drew");
                Console.WriteLine(new Rect(noteWidth * (note.StartTime - noteFirst), Height - (position - noteMin) * noteHeight, noteWidth *
                    (note.EndTime - note.StartTime), noteHeight));
            }
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Console.WriteLine("Pattern Notes:");
        foreach (var note in Notes)
        {
            Console.WriteLine(note.Name);
        }
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

    public void OpenPianoRoll()
    {
        ViewHelper.GetMainWindow().PianoRollWindow.EditArea.Notes = Notes;
    }
    
    
    
}