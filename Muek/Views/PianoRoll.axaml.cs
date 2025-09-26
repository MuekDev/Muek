using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Muek.Models;

namespace Muek.Views;

public partial class PianoRoll : UserControl
{

    public static readonly StyledProperty<double> NoteHeightProperty = AvaloniaProperty.Register<PianoRoll, double>(
        nameof(NoteHeight));

    public double NoteHeight
    {
        get => GetValue(NoteHeightProperty);
        set => SetValue(NoteHeightProperty, value);
    }

    public static readonly StyledProperty<bool> IsPianoBarProperty = AvaloniaProperty.Register<PianoRoll, bool>(
        nameof(IsPianoBar));

    
    //为了同步一些属性，把两边的钢琴写在一个类里了
    public bool IsPianoBar
    {
        get => GetValue(IsPianoBarProperty);
        set => SetValue(IsPianoBarProperty, value);
    }

    public static readonly StyledProperty<double> ScrollOffsetProperty = AvaloniaProperty.Register<PianoRoll, double>(
        nameof(ScrollOffset));

    public double ScrollOffset
    {
        get => GetValue(ScrollOffsetProperty);
        set => SetValue(ScrollOffsetProperty, value);
    }

    public static readonly StyledProperty<double> ClampValueProperty = AvaloniaProperty.Register<PianoRoll, double>(
        nameof(ClampValue));

    public double ClampValue
    {
        get => GetValue(ClampValueProperty);
        set => SetValue(ClampValueProperty, value);
    }

    private double _widthOfBeat;

    
    private int _noteRangeMax = 9;
    private int _noteRangeMin = 0;

    private int _Temperament = 12;

    
    //TODO 需要获取目前窗口大小
    private double _renderSize = 2000;
    
    
    
    private IBrush _noteColor1;
    private IBrush _noteColor2;
    private IBrush _noteColor3;

    private IBrush _noteHoverColor;

    private string _currentHoverNote = "";
    
    private bool _isDrawing = false;
    private bool _isDragging = false;
    private bool _isEditing = false;
    
    private double _currentNoteStartTime;
    private double _currentNoteEndTime;
    private double _dragPos;
    private double _dragStartTime;
    private double _dragEndTime;
    
    private string _editingNote = "";

    public struct Note
    {
        public string Name;
        public double StartTime;
        public double EndTime;
        public Color Color;
    }
    
    private Note _currentHoverDrawedNote = new Note();
    
    private Point _currentMousePosition;
    
    public List<Note> Notes = new();


    public Point ScalingSensitivity = new Point(5, 2);
    
    
    public PianoRoll()
    {
        InitializeComponent();
        
        NoteHeight = 15;
        ClampValue = 0;
        
        
        // IsVisible = true;
        Height = NoteHeight * (_noteRangeMax - _noteRangeMin + 1) * _Temperament;
        Console.WriteLine(Height);

        
        
        _noteColor1 = Brushes.White;
        _noteColor2 = Brushes.Black;
        _noteColor3 = Brushes.YellowGreen;
        
        
        _noteHoverColor = Brush.Parse("#40232323");

        _widthOfBeat = 50;
        if (!IsPianoBar)
        {
            Width = 20000;
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        
        // context.FillRectangle(Brushes.DimGray, new Rect(0, 0, Width, Height));
        
        //左侧钢琴Bar
        if (IsPianoBar)
        {
            var noteColor = _noteColor1;
            var noteName = "";

            //左侧钢琴
            {
                //十二平均律
                for (int i = _noteRangeMin; i <= _noteRangeMax; i++)
                {
                    for (int note = 0; note < _Temperament; note++)
                    {
                        switch (note)
                        {
                            case 0:
                                noteName = $"C{i}";
                                noteColor = _noteColor3;
                                break;
                            case 1:
                                noteName = $"C#{i}";
                                noteColor = _noteColor2;
                                break;
                            case 2:
                                noteName = $"D{i}";
                                noteColor = _noteColor1;
                                break;
                            case 3:
                                noteName = $"D#{i}";
                                noteColor = _noteColor2;
                                break;
                            case 4:
                                noteName = $"E{i}";
                                noteColor = _noteColor1;
                                break;
                            case 5:
                                noteName = $"F{i}";
                                noteColor = _noteColor1;
                                break;
                            case 6:
                                noteName = $"F#{i}";
                                noteColor = _noteColor2;
                                break;
                            case 7:
                                noteName = $"G{i}";
                                noteColor = _noteColor1;
                                break;
                            case 8:
                                noteName = $"G#{i}";
                                noteColor = _noteColor2;
                                break;
                            case 9:
                                noteName = $"A{i}";
                                noteColor = _noteColor1;
                                break;
                            case 10:
                                noteName = $"A#{i}";
                                noteColor = _noteColor2;
                                break;
                            case 11:
                                noteName = $"B{i}";
                                noteColor = _noteColor1;
                                break;
                        }

                        // Console.WriteLine(noteName);
                        context.FillRectangle(noteColor,
                            new Rect(0, Height - (i * _Temperament + note + 1) * NoteHeight, Width * .9, NoteHeight * .9),5F);
                        context.FillRectangle(noteColor,
                            new Rect(0, Height - (i * _Temperament + note + 1) * NoteHeight, Width * .4, NoteHeight * .9));
                        // Console.WriteLine(new Rect(0, Height - (i * _Temperament + note +1) * NoteHeight,NoteWidth,NoteHeight));
                        context.DrawText(new FormattedText(noteName,
                                CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default,
                                NoteHeight * .6, (noteColor == _noteColor2 ? _noteColor1 : _noteColor2)),
                            new Point(0, Height - (i * _Temperament + note + 1) * NoteHeight)
                        );

                        if (noteName.Equals(_currentHoverNote))
                        {
                            context.FillRectangle(_noteHoverColor,
                                new Rect(0, Height - (i * _Temperament + note + 1) * NoteHeight, Width * .9,
                                    NoteHeight * .9));
                        }
                    }
                }
            }
        }
        
        //绘制区域
        else
        {
            var noteColor = _noteColor1;
            var noteName = "";
            for (int i = _noteRangeMin; i <= _noteRangeMax; i++)
            {
                for (int note = 0; note < _Temperament; note++)
                {
                    switch (note)
                    {
                        case 0:
                            noteName = $"C{i}";
                            noteColor = _noteColor3;
                            break;
                        case 1:
                            noteName = $"C#{i}";
                            noteColor = _noteColor2;
                            break;
                        case 2:
                            noteName = $"D{i}";
                            noteColor = _noteColor1;
                            break;
                        case 3:
                            noteName = $"D#{i}";
                            noteColor = _noteColor2;
                            break;
                        case 4:
                            noteName = $"E{i}";
                            noteColor = _noteColor1;
                            break;
                        case 5:
                            noteName = $"F{i}";
                            noteColor = _noteColor1;
                            break;
                        case 6:
                            noteName = $"F#{i}";
                            noteColor = _noteColor2;
                            break;
                        case 7:
                            noteName = $"G{i}";
                            noteColor = _noteColor1;
                            break;
                        case 8:
                            noteName = $"G#{i}";
                            noteColor = _noteColor2;
                            break;
                        case 9:
                            noteName = $"A{i}";
                            noteColor = _noteColor1;
                            break;
                        case 10:
                            noteName = $"A#{i}";
                            noteColor = _noteColor2;
                            break;
                        case 11:
                            noteName = $"B{i}";
                            noteColor = _noteColor1;
                            break;
                    }
                    // Console.WriteLine(noteName);
                    context.DrawRectangle(Brush.Parse("#40232323"),new Pen(new SolidColorBrush(new HslColor(1,0,0,0).ToRgb(),.1)),
                        new Rect(ClampValue, Height - (i * _Temperament + note + 1) * NoteHeight, _renderSize, NoteHeight));
                    // Console.WriteLine(new Rect(0, Height - (i * _Temperament + note +1) * NoteHeight,NoteWidth,NoteHeight));
                    
                    if (noteName.Equals(_currentHoverNote))
                    {
                        context.FillRectangle(_noteHoverColor,
                            new Rect(ClampValue, Height - (i * _Temperament + note + 1) * NoteHeight, _renderSize,
                                NoteHeight * .9));
                    }
                    
                }
            }
            
            
            for (int i = 0; i < Width / _widthOfBeat; i++)
            {
                var gridLinePen = new Pen(Brushes.White, 1);
                if(i * _widthOfBeat > ClampValue && i *  _widthOfBeat < _renderSize + ClampValue)
                {
                    if (i % 4 == 0)
                    {
                        gridLinePen.Brush = Brushes.White;
                
                        //小节数
                        context.DrawText(new FormattedText(i.ToString(),
                                CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 15,
                                Brushes.White),
                            new Point(6 + i * _widthOfBeat, ScrollOffset));
                    }
                    else
                    {
                        gridLinePen.Brush = Brushes.DimGray;
                    }
                
                    context.DrawLine(gridLinePen,
                        new Point(i * _widthOfBeat, 0),
                        new Point(i * _widthOfBeat, Height));
                }
            }
            
            for (int i = _noteRangeMin; i <= _noteRangeMax; i++)
            {
                for (int note = 0; note < _Temperament; note++)
                {
                    switch (note)
                    {
                        case 0:
                            noteName = $"C{i}";
                            noteColor = _noteColor3;
                            break;
                        case 1:
                            noteName = $"C#{i}";
                            noteColor = _noteColor2;
                            break;
                        case 2:
                            noteName = $"D{i}";
                            noteColor = _noteColor1;
                            break;
                        case 3:
                            noteName = $"D#{i}";
                            noteColor = _noteColor2;
                            break;
                        case 4:
                            noteName = $"E{i}";
                            noteColor = _noteColor1;
                            break;
                        case 5:
                            noteName = $"F{i}";
                            noteColor = _noteColor1;
                            break;
                        case 6:
                            noteName = $"F#{i}";
                            noteColor = _noteColor2;
                            break;
                        case 7:
                            noteName = $"G{i}";
                            noteColor = _noteColor1;
                            break;
                        case 8:
                            noteName = $"G#{i}";
                            noteColor = _noteColor2;
                            break;
                        case 9:
                            noteName = $"A{i}";
                            noteColor = _noteColor1;
                            break;
                        case 10:
                            noteName = $"A#{i}";
                            noteColor = _noteColor2;
                            break;
                        case 11:
                            noteName = $"B{i}";
                            noteColor = _noteColor1;
                            break;
                    }
                    
                    //渲染音符
                    foreach (Note existNote in Notes)
                    {
                        var start = existNote.StartTime * _widthOfBeat;
                        var end = existNote.EndTime * _widthOfBeat;
                        var color = existNote.Color;
                        if (noteName.Equals(existNote.Name))
                        {
                            context.DrawRectangle(new SolidColorBrush(color),new Pen(Brushes.Black,1),
                                new Rect(start, Height - (i * _Temperament + note + 1) * NoteHeight,end-start,NoteHeight * .9));
                            
                            context.DrawText(new FormattedText(existNote.Name,
                                    CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, NoteHeight * .6,
                                    Brushes.Black),
                                new Point(start, Height - (i * _Temperament + note + 1) * NoteHeight));

                            
                            //Hover音符
                            if (existNote.Name.Equals(_currentHoverNote) &&
                                _currentMousePosition.X > existNote.StartTime * _widthOfBeat &&
                                _currentMousePosition.X < existNote.EndTime * _widthOfBeat)
                            {
                                if (double.Abs(_currentMousePosition.X - existNote.EndTime * _widthOfBeat) < 5)
                                {
                                    context.DrawLine(new Pen(Brushes.White,2),
                                        new Point(end, Height - (i * _Temperament + note + 1) * NoteHeight),
                                        new Point(end, Height - (i * _Temperament + note + 1) * NoteHeight + NoteHeight * .9));
                                }
                                else
                                {
                                    context.DrawRectangle(null,new Pen(Brushes.White,1),
                                        new Rect(start, Height - (i * _Temperament + note + 1) * NoteHeight,end-start,NoteHeight * .9));

                                }
                                
                               
                            }
                        }
                        
                    }

                    //渲染绘制中的音符
                    if (_isDrawing || _isDragging || _isEditing)
                    {
                        var start = _currentNoteStartTime * _widthOfBeat;
                        var end = _currentNoteEndTime * _widthOfBeat;
                        var color = Colors.DimGray;
                        if(!_isEditing)
                        {
                            if (noteName.Equals(_currentHoverNote))
                            {
                                context.FillRectangle(new SolidColorBrush(color),
                                    new Rect(start, Height - (i * _Temperament + note + 1) * NoteHeight, end - start,
                                        NoteHeight * .9));
                            }
                        }
                        else
                        {
                            if (noteName.Equals(_editingNote))
                            {
                                context.FillRectangle(new SolidColorBrush(color),
                                    new Rect(start, Height - (i * _Temperament + note + 1) * NoteHeight, end - start,
                                        NoteHeight * .9));
                            }
                        }
                    }
                }
            }
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        
        
        {
            var noteColor = _noteColor1;
            var noteName = "";

            for (int i = _noteRangeMin; i <= _noteRangeMax; i++)
            {
                for (int note = 0; note < _Temperament; note++)
                {
                    switch (note)
                    {
                        case 0:
                            noteName = $"C{i}";
                            noteColor = _noteColor1;
                            break;
                        case 1:
                            noteName = $"C#{i}";
                            noteColor = _noteColor2;
                            break;
                        case 2:
                            noteName = $"D{i}";
                            noteColor = _noteColor1;
                            break;
                        case 3:
                            noteName = $"D#{i}";
                            noteColor = _noteColor2;
                            break;
                        case 4:
                            noteName = $"E{i}";
                            noteColor = _noteColor1;
                            break;
                        case 5:
                            noteName = $"F{i}";
                            noteColor = _noteColor1;
                            break;
                        case 6:
                            noteName = $"F#{i}";
                            noteColor = _noteColor2;
                            break;
                        case 7:
                            noteName = $"G{i}";
                            noteColor = _noteColor1;
                            break;
                        case 8:
                            noteName = $"G#{i}";
                            noteColor = _noteColor2;
                            break;
                        case 9:
                            noteName = $"A{i}";
                            noteColor = _noteColor1;
                            break;
                        case 10:
                            noteName = $"A#{i}";
                            noteColor = _noteColor2;
                            break;
                        case 11:
                            noteName = $"B{i}";
                            noteColor = _noteColor1;
                            break;
                    }

                    var relativePos = e.GetPosition(this) -
                                      new Point(0, Height - (i * _Temperament + note + 1) * NoteHeight);
                    Console.WriteLine(relativePos);
                    
                    if (relativePos.Y > 0 && relativePos.Y < NoteHeight)
                    {
                        _currentHoverNote = noteName;
                        InvalidateVisual();
                        Console.WriteLine(_currentHoverNote);
                    }
                }
            }
        }
        {
            if (!IsPianoBar)
            {
                _currentMousePosition = e.GetPosition(this);
                if(_isDrawing)
                {
                    _currentNoteEndTime =
                        (e.GetPosition(this).X - (e.GetPosition(this).X) % _widthOfBeat + _widthOfBeat) / _widthOfBeat;
                }

                if (_isDragging)
                {
                    _currentNoteStartTime = _dragStartTime + ((e.GetPosition(this).X - _dragPos) -
                                                              (e.GetPosition(this).X - _dragPos) % _widthOfBeat) / _widthOfBeat;
                    _currentNoteEndTime = _dragEndTime + ((e.GetPosition(this).X - _dragPos) -
                                                          (e.GetPosition(this).X - _dragPos) % _widthOfBeat) / _widthOfBeat;
                }

                if (_isEditing)
                {
                    _currentNoteEndTime =
                        (e.GetPosition(this).X - (e.GetPosition(this).X) % _widthOfBeat + _widthOfBeat) / _widthOfBeat;
                }

                foreach (Note note in Notes)
                {
                    if (e.GetPosition(this).X > note.StartTime * _widthOfBeat &&
                        e.GetPosition(this).X < note.EndTime * _widthOfBeat)
                    {
                        _currentHoverDrawedNote.StartTime = note.StartTime;
                        _currentHoverDrawedNote.EndTime = note.EndTime;
                        _currentHoverDrawedNote.Name = _currentHoverNote;
                    }
                }
            }
        }
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _currentHoverNote =  null;
        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!IsPianoBar)
        {
            if (e.KeyModifiers == KeyModifiers.Control && e.Properties.IsLeftButtonPressed)
            {
                _currentNoteStartTime = (e.GetPosition(this).X - (e.GetPosition(this).X) % _widthOfBeat) / _widthOfBeat;
                _isDrawing = true;
            }

            Note? removedNote = null;
            foreach (Note existNote in Notes)
            {
                if (existNote.Name.Equals(_currentHoverNote) &&
                    _currentMousePosition.X > existNote.StartTime * _widthOfBeat &&
                    _currentMousePosition.X < existNote.EndTime * _widthOfBeat)
                {
                    if (double.Abs(_currentMousePosition.X - existNote.EndTime * _widthOfBeat) < 5)
                    {
                        _isEditing = true;
                        _currentNoteStartTime = existNote.StartTime;
                        removedNote = existNote;
                        _editingNote = existNote.Name;
                        e.Handled = true;
                    }
                    else
                    {
                        //删除单个音符
                        if (e.Properties.IsRightButtonPressed)
                        {
                            removedNote = existNote;
                            e.Handled = true;
                            break;
                        }

                        //拖动单个音符
                        if (e.Properties.IsLeftButtonPressed)
                        {
                            _isDragging = true;

                            _dragPos = e.GetPosition(this).X;
                            _dragStartTime = existNote.StartTime;
                            _dragEndTime = existNote.EndTime;
                            removedNote = existNote;
                            e.Handled = true;
                            break;
                        }
                    }
                }
            }

            if (removedNote != null)
            {
                Notes.Remove((Note)removedNote);
                InvalidateVisual();
            }
            
        }
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        
        
        
        if (_currentNoteStartTime < _currentNoteEndTime && (_isDrawing || _isDragging || _isEditing))
        {
            Note removableNote = new Note();
            foreach (Note note in Notes)
            {
                if (Notes.Contains(note) && _currentHoverNote.Equals(note.Name))
                {
                    if (_currentNoteStartTime > note.StartTime && _currentNoteStartTime < note.EndTime ||
                        _currentNoteEndTime > note.StartTime && _currentNoteEndTime < note.EndTime ||
                        note.StartTime > _currentNoteStartTime &&  note.StartTime < _currentNoteEndTime ||
                        note.EndTime > _currentNoteStartTime && note.EndTime < _currentNoteEndTime)
                    {
                        removableNote = note;
                        break;
                    }
                }
            }

            Notes.Remove(removableNote);

            if(e.InitialPressMouseButton == MouseButton.Left)
            {
                if (!_isEditing)
                {
                    Notes.Add(new Note
                    {
                        StartTime = _currentNoteStartTime,
                        EndTime = _currentNoteEndTime,
                        Name = _currentHoverNote,
                        Color = Colors.YellowGreen
                    });
                }
                else
                {
                    Notes.Add(new Note
                    {
                        StartTime = _currentNoteStartTime,
                        EndTime = _currentNoteEndTime,
                        Name = _editingNote,
                        Color = Colors.YellowGreen
                    });
                }
            }
        }
        _isDrawing = false;
        _isDragging = false;
        _isEditing = false;
        e.Handled = true;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                double currentPosition = e.GetPosition(this).Y / NoteHeight;
                NoteHeight = double.Clamp(NoteHeight + e.Delta.Y * ScalingSensitivity.Y, 10, 30);
                Height = NoteHeight * (_noteRangeMax - _noteRangeMin + 1) * _Temperament;
                ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRight.Offset = new Vector(
                    ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRight.Offset.X,
                    currentPosition * NoteHeight -
                    e.GetPosition(ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRight).Y);
                
                
                Console.WriteLine($"CurrentPositionY: {currentPosition}");
                
                Console.WriteLine(ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRight.Offset);
                Console.WriteLine(e.GetPosition(ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRight));
            }
            else
            {
                double currentPosition = e.GetPosition(this).X / _widthOfBeat;

                _widthOfBeat = double.Clamp(_widthOfBeat + e.Delta.Y * ScalingSensitivity.X, 10, _renderSize * .1);

                ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRight.Offset = new Vector(
                    currentPosition * _widthOfBeat -
                    e.GetPosition(ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRight).X,
                    ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRight.Offset.Y);

                // Console.WriteLine(ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRight.Offset);
                // Console.WriteLine(e.GetPosition(ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRight));
            }

            InvalidateVisual();
            e.Handled = true;
        }
    }
}