using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Muek.Models;
using Muek.Services;

namespace Muek.Views;

public partial class NoteVelocity : UserControl
{
    private List<PianoRoll.Note> Notes
    {
        get => ViewHelper.GetMainWindow().PianoRollWindow.EditArea.Notes;
        set => ViewHelper.GetMainWindow().PianoRollWindow.EditArea.Notes = value;
    }

    private List<PianoRoll.Note> SelectedNotes
    {
        get => ViewHelper.GetMainWindow().PianoRollWindow.EditArea.SelectedNotes;
        set => ViewHelper.GetMainWindow().PianoRollWindow.EditArea.SelectedNotes = value;
    }
    
    private double WidthOfBeat
    {
        get => ViewHelper.GetMainWindow().PianoRollWindow.EditArea.WidthOfBeat;
        set => ViewHelper.GetMainWindow().PianoRollWindow.EditArea.WidthOfBeat = value;
    }

    private double LengthIncreasement => ViewHelper.GetMainWindow().PianoRollWindow.EditArea.LengthIncreasement;
    private Point ScalingSensitivity => ViewHelper.GetMainWindow().PianoRollWindow.EditArea.ScalingSensitivity;
    private Color Color => ViewHelper.GetMainWindow().PianoRollWindow.EditArea.Pattern?.Color ?? DataStateService.MuekColor;
    private Pen NotePen => new Pen(new SolidColorBrush(Color),2);

    private static readonly IBrush WhiteBrush = new SolidColorBrush(Colors.White,.2);
    private readonly Pen _whitePen = new Pen(new SolidColorBrush(Colors.White,.2));

    private readonly FormattedText _maxVelocityText = new FormattedText("Velocity: 127",CultureInfo.CurrentCulture, FlowDirection.LeftToRight,Typeface.Default, 10,WhiteBrush);
    private readonly FormattedText _minVelocityText = new FormattedText("Velocity: 0",CultureInfo.CurrentCulture, FlowDirection.LeftToRight,Typeface.Default, 10,WhiteBrush);
    
    private bool _isPressed;
    private readonly Pen _blackPen = new Pen(Brushes.Black,2);


    public NoteVelocity()
    {
        InitializeComponent();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _isPressed = true;
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isPressed = false;
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isPressed)
        {
            var position = e.GetPosition(this);
            var newNotes = new List<PianoRoll.Note>();
            var changedNotes = new List<PianoRoll.Note>();
            double distance = WidthOfBeat;
            foreach (var note in Notes)
            {
                // var velocity = note.Velocity;
                if (double.Abs(position.X - note.StartTime * WidthOfBeat) < WidthOfBeat)
                {
                    // velocity = int.Clamp((int)((Bounds.Height - position.Y) / Bounds.Height * 127),0,127);
                    // Console.WriteLine($"VelocityChange: {note.Velocity}");
                    // InvalidateVisual();
                    if(double.Abs(position.X - note.StartTime * WidthOfBeat) <= distance)
                    {
                        if(double.Abs(position.X - note.StartTime * WidthOfBeat) < distance)
                            changedNotes.Clear();
                        distance = double.Abs(position.X - note.StartTime * WidthOfBeat);
                        changedNotes.Add(note);
                    }
                }
                // newNotes.Add(note with { Velocity = velocity });
                // Console.WriteLine($"{position.X},{note.StartTime * WidthOfBeat}");
            }

            foreach (var note in Notes)
            {
                var velocity = note.Velocity;
                if(SelectedNotes.Count == 0 || SelectedNotes.Contains(note))
                    if (changedNotes.Contains(note))
                    {
                        if(e.KeyModifiers == KeyModifiers.Control)
                            velocity = int.Clamp((int)((Bounds.Height - position.Y - 10) / (Bounds.Height-20) * 127) /
                                (128/8) * (128/8),0,127);
                        else
                            velocity = int.Clamp((int)((Bounds.Height - position.Y - 10) / (Bounds.Height-20) * 127),0,127);
                        InvalidateVisual();
                    }

                if (SelectedNotes.Contains(note))
                {
                    SelectedNotes.Remove(note);
                    SelectedNotes.Add(note with { Velocity = velocity });
                }
                newNotes.Add(note with { Velocity = velocity });
            }
            
            if (!Notes.Equals(newNotes))
            {
                Notes = newNotes;
                ViewHelper.GetMainWindow().PianoRollWindow.EditArea.InvalidateVisual();
            }
        }
        e.Handled = true;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.DrawRectangle(Brush.Parse("#232323"), null, new Rect(0, 0, Bounds.Width, Bounds.Height));
        
        
        context.DrawLine(_whitePen,new Point(
            ViewHelper.GetMainWindow().PianoRollWindow.NoteVelocityScroll.Offset.X + 10
            ,10),new Point(
            ViewHelper.GetMainWindow().PianoRollWindow.NoteVelocityScroll.Bounds.Width + 
            ViewHelper.GetMainWindow().PianoRollWindow.NoteVelocityScroll.Offset.X - 100
            ,10));
        context.DrawLine(_whitePen,new Point(
            ViewHelper.GetMainWindow().PianoRollWindow.NoteVelocityScroll.Offset.X + 10
            ,Bounds.Height - 10),new Point(
            ViewHelper.GetMainWindow().PianoRollWindow.NoteVelocityScroll.Bounds.Width + 
            ViewHelper.GetMainWindow().PianoRollWindow.NoteVelocityScroll.Offset.X - 100
            ,Bounds.Height - 10));
        context.DrawLine(_whitePen,new Point(
            ViewHelper.GetMainWindow().PianoRollWindow.NoteVelocityScroll.Offset.X + 10
            ,Bounds.Height/2),new Point(
            ViewHelper.GetMainWindow().PianoRollWindow.NoteVelocityScroll.Bounds.Width + 
            ViewHelper.GetMainWindow().PianoRollWindow.NoteVelocityScroll.Offset.X - 100
            ,Bounds.Height/2));
        
        context.DrawText(
            _maxVelocityText,
            new Point(
                ViewHelper.GetMainWindow().PianoRollWindow.NoteVelocityScroll.Bounds.Width + 
                ViewHelper.GetMainWindow().PianoRollWindow.NoteVelocityScroll.Offset.X - 80
                ,4)
        );
        
        context.DrawText(
            _minVelocityText,
            new Point(
                ViewHelper.GetMainWindow().PianoRollWindow.NoteVelocityScroll.Bounds.Width + 
                ViewHelper.GetMainWindow().PianoRollWindow.NoteVelocityScroll.Offset.X - 80
                ,Bounds.Height - 16)
            );
        foreach (var note in Notes)
        {
            if (SelectedNotes.Contains(note))
            {
                context.DrawLine(_blackPen,
                    new Point(note.StartTime * WidthOfBeat,Bounds.Height - 10),
                    new Point(note.StartTime * WidthOfBeat, (Bounds.Height - 20) * (1-note.Velocity/127f) + 10 + 2));
                context.DrawEllipse(null,_blackPen, new Point(note.StartTime * WidthOfBeat, (Bounds.Height - 20) * (1-note.Velocity/127f) + 10),2,2);
            }
            else
            {
                // Console.WriteLine(note.Velocity);
                context.DrawLine(NotePen,
                    new Point(note.StartTime * WidthOfBeat, Bounds.Height - 10),
                    new Point(note.StartTime * WidthOfBeat,
                        (Bounds.Height - 20) * (1 - note.Velocity / 127f) + 10 + 2));
                // Console.WriteLine($"Point1: {new Point(note.StartTime * WidthOfBeat,Bounds.Height)}" +
                //                   $"Point2: {new Point(note.StartTime * WidthOfBeat, Bounds.Height * (1-note.Velocity/128f))}");
                context.DrawEllipse(null, NotePen,
                    new Point(note.StartTime * WidthOfBeat, (Bounds.Height - 20) * (1 - note.Velocity / 127f) + 10), 2,
                    2);
            }
        }
    }
    
    
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Bounds.Width >= Width)
        {
            ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Offset = new Vector(
                Width - ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Bounds.Width,
                ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Offset.Y);
        }
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            {
                double trackEnd = 0;
                foreach (var note in Notes)
                {
                    trackEnd = double.Max(trackEnd, note.EndTime);
                }
                trackEnd += LengthIncreasement;
                double currentPosition = e.GetPosition(this).X / WidthOfBeat;

                WidthOfBeat = double.Clamp(WidthOfBeat + e.Delta.Y * WidthOfBeat / 50d * ScalingSensitivity.X,
                    double.Max(1, ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Bounds.Width / trackEnd), 500);

                ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Offset = new Vector(
                    currentPosition * WidthOfBeat -
                    e.GetPosition(ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll).X,
                    ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Offset.Y);

                // Console.WriteLine(ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Offset);
                // Console.WriteLine(e.GetPosition(ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll));
            }

            ViewHelper.GetMainWindow().PianoRollWindow.EditArea.SaveNotes();
            InvalidateVisual();
            e.Handled = true;
        }
    }
}