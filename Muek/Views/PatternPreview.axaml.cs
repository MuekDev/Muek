using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Muek.Models;
using Muek.Services;

namespace Muek.Views;

public partial class PatternPreview : UserControl
{
    // public static readonly StyledProperty<IBrush> BackgroundColorProperty =
    //     AvaloniaProperty.Register<PatternPreview, IBrush>(
    //         nameof(BackgroundColor));

    public IBrush BackgroundColor => ViewHelper.GetMainWindow().PianoRollWindow.PatternColor.Background;
    // {
    //     get => GetValue(BackgroundColorProperty);
    //     set => SetValue(BackgroundColorProperty, value);
    // }

    public List<PianoRoll.Note> Notes => ViewHelper.GetMainWindow().PianoRollWindow.EditArea.Notes;

    private bool _isHover = false;
    private bool _isPressed = false;
    private bool _isDragging = false;

    public PatternPreview()
    {
        InitializeComponent();
        ClipToBounds = false;
        // BackgroundColor = new SolidColorBrush(DataStateService.MuekColor);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime)
        {
            return;
        }

        if (!ViewHelper.GetMainWindow().PianoRollWindow.IsShowing)
        {
            context.DrawRectangle(BackgroundColor, null, new Rect(0, 0, Bounds.Width, Bounds.Height));
            if (_isHover)
            {
                context.DrawRectangle(new SolidColorBrush(Colors.Black, .1), null,
                    new Rect(0, 0, Bounds.Width, Bounds.Height), 10D, 15D);
            }
        }
        else
        {
            context.DrawRectangle(new SolidColorBrush(Colors.White, .05), null,
                new Rect(ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Offset.X /
                         ViewHelper.GetMainWindow().PianoRollWindow.EditArea.Width
                         * Bounds.Width
                    , 0,
                    ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Bounds.Width /
                    ViewHelper.GetMainWindow().PianoRollWindow.EditArea.Width
                    * Bounds.Width,
                    Bounds.Height));
            if (_isHover)
            {
                context.DrawRectangle(null, new Pen(new SolidColorBrush(Colors.White)),
                    new Rect(ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Offset.X /
                             ViewHelper.GetMainWindow().PianoRollWindow.EditArea.Width
                             * Bounds.Width
                        , 0,
                        ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Bounds.Width /
                        ViewHelper.GetMainWindow().PianoRollWindow.EditArea.Width
                        * Bounds.Width,
                        Bounds.Height));
            }
        }

        if (_isDragging)
        {
            //TODO 拖拽功能
        }

        if (Notes.Count > 0)
        {
            double noteHeight;
            double noteWidth;
            int noteMax = Notes[0].Name;
            int noteMin = noteMax;
            // double noteFirst = Notes[0].StartTime;
            double noteLast = Notes[0].EndTime;
            foreach (var note in Notes)
            {
                var position = note.Name;
                noteMin = int.Min(position, noteMin); //最低的音符
                noteMax = int.Max(position, noteMax); //最高的音符
                // noteFirst = double.Min(noteFirst, note.StartTime); //最左边的音符
                noteLast = double.Max(noteLast, note.EndTime); //最右边的音符
            }

            noteHeight = Bounds.Height / (noteMax + 1 - noteMin);
            // noteWidth = Bounds.Width / (noteLast - noteFirst);
            noteWidth = Bounds.Width /
                        (noteLast + ViewHelper.GetMainWindow().PianoRollWindow.EditArea.LengthIncreasement);
            // Console.WriteLine($"noteWidth:{noteWidth}");
            // Console.WriteLine($"noteHeight:{noteHeight}");

            foreach (var note in Notes)
            {
                var position = note.Name;
                var background = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0.9, 0.5, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1.0, 0.5, RelativeUnit.Relative),
                };
                background.GradientStops.Add(new GradientStop(Colors.White, 0.0));
                background.GradientStops.Add(new GradientStop(Colors.Transparent, 1.0));
                // context.FillRectangle(background,
                //     new Rect(
                //         noteWidth * .6 * (note.StartTime - noteFirst) + Bounds.Width*.2,
                //         Bounds.Height*.6 - (position - noteMin + 1) * noteHeight * .6 + Bounds.Height*.2,
                //         noteWidth * (note.EndTime - note.StartTime) * .6,
                //         noteHeight * .6),
                //     (float)(noteHeight * .1));
                context.FillRectangle(background,
                    new Rect(
                        note.StartTime * noteWidth,
                        Bounds.Height * .6 - (position - noteMin + 1) * noteHeight * .6 + Bounds.Height * .2,
                        noteWidth * (note.EndTime - note.StartTime),
                        noteHeight * .55),
                    (float)(noteHeight * .1));
                // Console.WriteLine("Drew");
                // Console.WriteLine(new Rect(noteWidth * (note.StartTime - noteFirst), Height - (position - noteMin) * noteHeight, noteWidth *
                //     (note.EndTime - note.StartTime), noteHeight));
            }
        }
    }

    private double _pressedPosition;
    private double _pressedScroll;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime)
        {
            return;
        }
        
        // Console.WriteLine("Pattern Notes:");
        // foreach (var note in Notes)
        // {
        // Console.WriteLine(note.Name);
        // }
        if (!ViewHelper.GetMainWindow().PianoRollWindow.IsShowing)
        {
            ScrollToNoteFirst();
        }

        _pressedPosition = e.GetPosition(this).X;
        _pressedScroll = ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Offset.X;
        _isPressed = true;
        if (ViewHelper.GetMainWindow().PianoRollWindow.IsShowing)
        {
            e.Handled = true;
        }
    }

    public void ScrollToNoteFirst()
    {
        if (Notes.Count > 0)
        {
            PianoRoll.Note noteFirst = Notes[0];
            foreach (var note in Notes)
            {
                var position = note.Name;
                // noteMin = int.Min(position, noteMin); //最低的音符
                // noteMax = int.Max(position, noteMax); //最高的音符
                // noteFirst = double.Min(noteFirst, note.StartTime); //最左边的音符
                // noteLast = double.Max(noteLast, note.EndTime); //最右边的音符
                noteFirst = noteFirst.StartTime < note.StartTime ? noteFirst : note;
            }

            ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Offset = new Vector(
                noteFirst.StartTime * ViewHelper.GetMainWindow().PianoRollWindow.EditArea.WidthOfBeat,
                ViewHelper.GetMainWindow().PianoRollWindow.EditArea.Height - (noteFirst.Name + 1) *
                ViewHelper.GetMainWindow().PianoRollWindow.EditArea.NoteHeight);
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isPressed = false;
        e.Handled = true;
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        _isHover = true;
        InvalidateVisual();

        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime)
        {
            return;
        }
        
        if (_isPressed)
        {
            ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Offset = new Vector(
                (e.GetPosition(this).X - _pressedPosition) / Bounds.Width
                * ViewHelper.GetMainWindow().PianoRollWindow.EditArea.Width + _pressedScroll
                , ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Offset.Y
            );
            if (!_isDragging)
            {
            }

            _isDragging = true;
            e.Handled = true;
            InvalidateVisual();
        }
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _isHover = false;
        InvalidateVisual();
        e.Handled = true;
    }

    // public void OpenPianoRoll()
    // {
    //     ViewHelper.GetMainWindow().PianoRollWindow.EditArea.Notes = Notes;
    // }
}