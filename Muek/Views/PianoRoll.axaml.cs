using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Styling;
using Muek.Models;

namespace Muek.Views;

public partial class PianoRoll : UserControl
{
    
    public int Index = 0;

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
    private Point _dragPos;
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
    private Point _pressedMousePosition;
    
    public List<Note> Notes = new();


    public Point ScalingSensitivity = new Point(5, 2);
    
    
    //框选音符
    private Rect? _selectFrame;
    
    public List<Note> SelectedNotes = new();
    
    private bool _isShowingOptions = false;
    
    public PianoRoll()
    {
        InitializeComponent();
        
        
        NoteHeight = 15;
        ClampValue = 0;
        
        
        // IsVisible = true;
        Height = NoteHeight * (_noteRangeMax - _noteRangeMin + 1) * _Temperament;
        // Console.WriteLine(Height);

        
        
        _noteColor1 = Brushes.White;
        _noteColor2 = Brushes.Black;
        _noteColor3 = Brushes.YellowGreen;
        
        
        _noteHoverColor = new SolidColorBrush(Colors.YellowGreen,.1);

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
            _noteHoverColor = new SolidColorBrush(Colors.Black, .5);
            
            var noteColor = _noteColor1;
            var noteName = "";

            //左侧钢琴
            {
                //十二平均律
                for (int i = _noteRangeMin; i <= _noteRangeMax; i++)
                {
                    for (int note = 0; note < _Temperament; note++)
                    {
                        noteName = IndexToNoteName(i * _Temperament + note);
                        noteColor = NoteNameToBrush(noteName);
                        
                        // Console.WriteLine(noteName);
                        context.FillRectangle(noteColor,
                            new Rect(0, Height - (i * _Temperament + note + 1) * NoteHeight, Width * .9, NoteHeight),5);
                        context.FillRectangle(noteColor,
                            new Rect(0, Height - (i * _Temperament + note + 1) * NoteHeight, Width * .8, NoteHeight));
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
                                    NoteHeight));
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
                    noteName = IndexToNoteName(i * _Temperament + note);
                    
                    //绘制编辑区
                    // Console.WriteLine(noteName);
                    context.DrawRectangle(Brush.Parse("#40232323"),new Pen(new SolidColorBrush(new HslColor(1,0,0,0).ToRgb(),.1)),
                        new Rect(ClampValue, Height - (i * _Temperament + note + 1) * NoteHeight, _renderSize, NoteHeight));
                    // Console.WriteLine(new Rect(0, Height - (i * _Temperament + note +1) * NoteHeight,NoteWidth,NoteHeight));
                    if (!noteName.Contains('#'))
                    {
                        context.DrawRectangle(new SolidColorBrush(Colors.White,.05),null,
                            new Rect(ClampValue, Height - (i * _Temperament + note + 1) * NoteHeight, _renderSize, NoteHeight));
                    }
                    
                    //绘制Hover区
                    if (noteName.Equals(_currentHoverNote))
                    {
                        context.FillRectangle(_noteHoverColor,
                            new Rect(ClampValue, Height - (i * _Temperament + note + 1) * NoteHeight, _renderSize,
                                NoteHeight));
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
                    noteName = IndexToNoteName(i * _Temperament + note);
                    
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
                                    //更改音符长度
                                    context.DrawLine(new Pen(Brushes.Red,2),
                                        new Point(end-1, Height - (i * _Temperament + note + 1) * NoteHeight),
                                        new Point(end-1, Height - (i * _Temperament + note + 1) * NoteHeight + NoteHeight * .9));
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
                    if ((_isDrawing || _isDragging || _isEditing) && SelectedNotes.Count == 0)
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
                                context.DrawLine(new Pen(Brushes.Orange,1),
                                    new Point(_currentMousePosition.X - _currentMousePosition.X % _widthOfBeat + _widthOfBeat, 0),
                                    new Point(_currentMousePosition.X - _currentMousePosition.X % _widthOfBeat + _widthOfBeat, Width));
                            }
                        }
                    }
                }
            }
            //渲染框
            if (_selectFrame != null)
            {
                context.DrawRectangle(null,new Pen(Brushes.White),
                    (Rect)_selectFrame);
            }
            for (int i = _noteRangeMin; i <= _noteRangeMax; i++)
            {
                for (int note = 0; note < _Temperament; note++)
                {
                    noteName = IndexToNoteName(i * _Temperament + note);
                    
                    //渲染选中音符
                    foreach (Note existNote in SelectedNotes)
                    {
                        var start = existNote.StartTime * _widthOfBeat;
                        var end = existNote.EndTime * _widthOfBeat;
                        var color = existNote.Color;
                        if (noteName.Equals(existNote.Name))
                        {
                            context.DrawRectangle(Brushes.Black, new Pen(Brushes.White,1),
                                new Rect(start, Height - (i * _Temperament + note + 1) * NoteHeight,end-start,NoteHeight * .9));
                            
                            context.DrawText(new FormattedText(existNote.Name,
                                    CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, NoteHeight * .6,
                                    Brushes.White),
                                new Point(start, Height - (i * _Temperament + note + 1) * NoteHeight));
                            
                            //Hover音符
                            if (existNote.Name.Equals(_currentHoverNote) &&
                                _currentMousePosition.X > existNote.StartTime * _widthOfBeat &&
                                _currentMousePosition.X < existNote.EndTime * _widthOfBeat)
                            {
                                if (double.Abs(_currentMousePosition.X - existNote.EndTime * _widthOfBeat) < 5)
                                {
                                    context.DrawLine(new Pen(Brushes.Red,2),
                                        new Point(end-1, Height - (i * _Temperament + note + 1) * NoteHeight),
                                        new Point(end-1, Height - (i * _Temperament + note + 1) * NoteHeight + NoteHeight * .9));
                                }
                                else
                                {
                                    context.DrawRectangle(null,new Pen(Brushes.White,1),
                                        new Rect(start, Height - (i * _Temperament + note + 1) * NoteHeight,end-start,NoteHeight * .9));
                            
                                }
                                
                               
                            }
                        }
                        
                    }
                    //渲染拖动的选中音符
                    if (_isDrawing || _isDragging || _isEditing && SelectedNotes.Count != 0)
                    {
                        var start = _currentNoteStartTime * _widthOfBeat;
                        var end = _currentNoteEndTime * _widthOfBeat;
                        var color = Colors.DimGray;
                        if(!_isEditing)
                        {
                            List<Note> dragedSelectedNotes = new List<Note>();
                            foreach (Note selectedNote in SelectedNotes)
                            {
                                var dragRelativePos = 
                                    ((_currentMousePosition.X - _dragPos.X) -
                                     (_currentMousePosition.X - _dragPos.X) %
                                     _widthOfBeat) / _widthOfBeat;
                                dragedSelectedNotes.Add(new Note
                                {
                                    StartTime = selectedNote.StartTime + dragRelativePos,
                                    EndTime = dragRelativePos + selectedNote.EndTime,
                                    Name = IndexToNoteName(
                                        (int)(-(_currentMousePosition.Y - _dragPos.Y) / NoteHeight +
                                              NoteNameToIndex(selectedNote.Name))),
                                    Color = Colors.YellowGreen
                                });
                            }
                            foreach (Note selectedNote in dragedSelectedNotes)
                            {
                                if(selectedNote.Name.Equals(noteName))
                                {
                                    context.FillRectangle(new SolidColorBrush(color),
                                        new Rect(selectedNote.StartTime * _widthOfBeat,
                                            Height - (i * _Temperament + note + 1) * NoteHeight,
                                            (selectedNote.EndTime - selectedNote.StartTime) * _widthOfBeat,
                                            NoteHeight * .9));
                                }
                            }

                            dragedSelectedNotes.Clear();
                        }
                        else
                        {
                            context.DrawLine(new Pen(Brushes.Orange,1),
                                new Point(_currentMousePosition.X - _currentMousePosition.X % _widthOfBeat + _widthOfBeat, 0),
                                new Point(_currentMousePosition.X - _currentMousePosition.X % _widthOfBeat + _widthOfBeat, Width));
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
                    noteName = IndexToNoteName(i * _Temperament + note);

                    var relativePos = e.GetPosition(this) -
                                      new Point(0, Height - (i * _Temperament + note + 1) * NoteHeight);
                    // Console.WriteLine(relativePos);
                    
                    if (relativePos.Y > 0 && relativePos.Y < NoteHeight)
                    {
                        _currentHoverNote = noteName;
                        InvalidateVisual();
                        // Console.WriteLine(_currentHoverNote);
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

                if (_isDragging && SelectedNotes.Count == 0)
                {
                    _currentNoteStartTime = _dragStartTime + ((e.GetPosition(this).X - _dragPos.X) -
                                                              (e.GetPosition(this).X - _dragPos.X) % _widthOfBeat) / _widthOfBeat;
                    _currentNoteEndTime = _dragEndTime + ((e.GetPosition(this).X - _dragPos.X) -
                                                          (e.GetPosition(this).X - _dragPos.X) % _widthOfBeat) / _widthOfBeat;
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

                //框选逻辑
                if (_selectFrame != null)
                {
                    Rect _tempSelectFrame = (Rect)_selectFrame;

                    double selectStartX = _tempSelectFrame.X;
                    double selectStartY =  _tempSelectFrame.Y;
                    double selectWidth = double.Abs(selectStartX - e.GetPosition(this).X);
                    double selectHeight = double.Abs(selectStartY - e.GetPosition(this).Y);
                    
                    if (e.GetPosition(this).X < _pressedMousePosition.X)
                    {
                        selectStartX = e.GetPosition(this).X;
                        selectWidth =  _tempSelectFrame.Right - selectStartX;
                    }
                    if (e.GetPosition(this).Y < _pressedMousePosition.Y)
                    {
                        selectStartY = e.GetPosition(this).Y;
                        selectHeight = _tempSelectFrame.Bottom - selectStartY;
                    }
                    
                    
                    
                    _selectFrame = new Rect(selectStartX, selectStartY,
                        selectWidth, selectHeight);
                    

                    
                    var noteColor = _noteColor1;
                    var noteName = "";

                    for (int i = _noteRangeMin; i <= _noteRangeMax; i++)
                    {
                        for (int note = 0; note < _Temperament; note++)
                        {
                            
                            
                            foreach (Note existNote in Notes)
                            {
                                if (
                                    (((Rect)_selectFrame).X < existNote.StartTime * _widthOfBeat &&
                                     ((Rect)_selectFrame).X + ((Rect)_selectFrame).Width >
                                     existNote.EndTime * _widthOfBeat ||
                                     ((Rect)_selectFrame).X > existNote.StartTime * _widthOfBeat &&
                                     ((Rect)_selectFrame).X < existNote.EndTime * _widthOfBeat ||
                                     ((Rect)_selectFrame).X + ((Rect)_selectFrame).Width >
                                     existNote.StartTime * _widthOfBeat &&
                                     ((Rect)_selectFrame).X + ((Rect)_selectFrame).Width <
                                     existNote.EndTime * _widthOfBeat) &&
                                    ((Rect)_selectFrame).Y < (Height / NoteHeight - NoteNameToIndex(existNote.Name)) * NoteHeight &&
                                    ((Rect)_selectFrame).Y + ((Rect)_selectFrame).Height > (Height / NoteHeight - NoteNameToIndex(existNote.Name)) * NoteHeight
                                    )
                                {
                                    if (!SelectedNotes.Contains(existNote))
                                    {
                                        SelectedNotes.Add(existNote);
                                    }
                                }
                                else
                                {
                                    if (SelectedNotes.Contains(existNote))
                                    {
                                        SelectedNotes.Remove(existNote);
                                    }
                                }
                            }
                            
                        }
                    }
                    
                    InvalidateVisual();
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
        _pressedMousePosition =  e.GetPosition(this);
        if (!IsPianoBar)
        {
            if (e.Properties.IsLeftButtonPressed)
            {
                _selectFrame = new Rect(e.GetPosition(this).X, e.GetPosition(this).Y, 0, 0);
                if(e.KeyModifiers == KeyModifiers.Control)
                {
                    _currentNoteStartTime =
                        (e.GetPosition(this).X - (e.GetPosition(this).X) % _widthOfBeat) / _widthOfBeat;
                    _isDrawing = true;
                    _selectFrame = null;
                    SelectedNotes.Clear();
                }
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
                        _selectFrame = null;
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
                            SelectedNotes.Clear();
                            e.Handled = true;
                            break;
                        }

                        //拖动单个音符
                        if (e.Properties.IsLeftButtonPressed)
                        {
                            if (!SelectedNotes.Contains(existNote))
                            {
                                SelectedNotes.Clear();
                            }
                            _isDragging = true;
                            _selectFrame = null;
                            _dragPos = e.GetPosition(this);
                            _dragStartTime = existNote.StartTime;
                            _dragEndTime = existNote.EndTime;
                            removedNote = existNote;
                            e.Handled = true;
                            break;
                        }
                    }
                }
                else
                {
                    
                }
            }
            if (removedNote != null)
            {
                Notes.Remove((Note)removedNote);
            }
            
        }
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (e.GetPosition(this) == _pressedMousePosition)
        {
            SelectedNotes.Clear();
        }
        
        if (_currentNoteStartTime < _currentNoteEndTime && (_isDrawing || _isDragging || _isEditing))
        {
            Note draggingNote = new Note();
            foreach (Note note in Notes)
            {
                if (Notes.Contains(note) && _currentHoverNote.Equals(note.Name))
                {
                    if (_currentNoteStartTime > note.StartTime && _currentNoteStartTime < note.EndTime ||
                        _currentNoteEndTime > note.StartTime && _currentNoteEndTime < note.EndTime ||
                        note.StartTime > _currentNoteStartTime &&  note.StartTime < _currentNoteEndTime ||
                        note.EndTime > _currentNoteStartTime && note.EndTime < _currentNoteEndTime)
                    {
                        draggingNote = note;
                        break;
                    }
                }
            }

            List<Note> draggingNotes = new List<Note>();
            foreach (Note selectedNote in SelectedNotes)
            {
                if (Notes.Contains(selectedNote))
                {
                    draggingNotes.Add(selectedNote);
                }
            }

            foreach (Note draggingNote1 in draggingNotes)
            {
                Notes.Remove(draggingNote1);
            }

            Notes.Remove(draggingNote);
            draggingNotes.Clear();
            
            
            if(e.InitialPressMouseButton == MouseButton.Left)
            {
                if (!_isEditing)
                {
                    if(SelectedNotes.Count == 0)
                    {
                        Notes.Add(new Note
                        {
                            StartTime = _currentNoteStartTime >= 0 ? _currentNoteStartTime : 0,
                            EndTime = _currentNoteEndTime,
                            Name = _currentHoverNote,
                            Color = Colors.YellowGreen
                        });
                    }
                    else
                    {
                        List<Note> dragedSelectedNotes = new List<Note>();
                        foreach (Note selectedNote in SelectedNotes)
                        {
                            var dragRelativePos = 
                                                  ((e.GetPosition(this).X - _dragPos.X) -
                                                   (e.GetPosition(this).X - _dragPos.X) %
                                                   _widthOfBeat) / _widthOfBeat;
                            dragedSelectedNotes.Add(new Note
                            {
                              StartTime = selectedNote.StartTime + dragRelativePos >= 0 ? selectedNote.StartTime + dragRelativePos : 0,
                              EndTime = dragRelativePos + selectedNote.EndTime,
                              Name = IndexToNoteName(
                                  (int)(-(e.GetPosition(this).Y - _dragPos.Y) / NoteHeight +
                                             NoteNameToIndex(selectedNote.Name))),
                              Color = Colors.YellowGreen
                            });
                        }
                        SelectedNotes.Clear();
                        foreach (Note dragedSelectedNote in dragedSelectedNotes)
                        {
                            Notes.Add(dragedSelectedNote);
                            SelectedNotes.Add(dragedSelectedNote);
                        }
                        // Console.WriteLine($"Notes:{Notes.Count}");
                        // Console.WriteLine($"SelectedNotes:{SelectedNotes.Count}");
                        dragedSelectedNotes.Clear();
                    }
                    
                }
                else
                {
                    if(SelectedNotes.Count == 0)
                    {
                        Notes.Add(new Note
                        {
                            StartTime = _currentNoteStartTime >= 0 ? _currentNoteStartTime : 0,
                            EndTime = _currentNoteEndTime,
                            Name = _editingNote,
                            Color = Colors.YellowGreen
                        });
                    }
                    else
                    {
                        List<Note> dragedSelectedNotes = new List<Note>();
                        foreach (Note selectedNote in SelectedNotes)
                        {
                            dragedSelectedNotes.Add(new Note
                            {
                                StartTime = selectedNote.StartTime >= 0 ? selectedNote.StartTime : 0,
                                EndTime = (e.GetPosition(this).X - e.GetPosition(this).X % _widthOfBeat + _widthOfBeat) / _widthOfBeat,
                                Name = selectedNote.Name,
                                Color = Colors.YellowGreen
                            });
                        }
                        SelectedNotes.Clear();
                        foreach (Note dragedSelectedNote in dragedSelectedNotes)
                        {
                            Notes.Add(dragedSelectedNote);
                            SelectedNotes.Add(dragedSelectedNote);
                        }
                        // Console.WriteLine($"Notes:{Notes.Count}");
                        // Console.WriteLine($"SelectedNotes:{SelectedNotes.Count}");
                        dragedSelectedNotes.Clear();
                    }
                }
            }
        }
        _isDrawing = false;
        _isDragging = false;
        _isEditing = false;
        _selectFrame = null;

        if(!IsPianoBar)
        {
            if (SelectedNotes.Count > 0)
            {
                _isShowingOptions = true;
                _ = ShowOptions();
            }
            else
            {
                _isShowingOptions = false;
                _ = HideOptions();
            }

            
            
        }
        
        SaveNotes();
        InvalidateVisual();
        e.Handled = true;
        GC.Collect();
    }

    private async Task ShowOptions()
    {
        var positionLeft = SelectedNotes[0].StartTime * _widthOfBeat;
        var positionTop = Height - NoteNameToIndex(SelectedNotes[0].Name) * NoteHeight;
        foreach (Note selectedNote in SelectedNotes)
        {
            positionLeft = double.Min(positionLeft, selectedNote.StartTime * _widthOfBeat);
            positionTop = double.Min(positionTop, Height - NoteNameToIndex(selectedNote.Name) * NoteHeight);
        }

        Options.Margin = new Thickness(positionLeft, positionTop, 0, 0);
            
        while (Options.Opacity < 1 && _isShowingOptions)
        {
            Options.IsVisible = true;
            await Task.Delay(50);
            lock(Options) Options.Opacity += .5;
            Console.WriteLine("IsShowingOptions");
        }
        await Task.CompletedTask;
        Console.WriteLine("ShowOptionsCompleted");
    }
    private async Task HideOptions()
    {
        
    while (Options.Opacity > 0 && !_isShowingOptions)
    {
        
        await Task.Delay(50);
        lock(Options) Options.Opacity -= .5;
        Console.WriteLine("IsHidingOptions");
    }
    await Task.CompletedTask;
    Options.IsVisible = false;
    Console.WriteLine("HidingOptionsCompleted");
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

            _ = ShowOptions();
            InvalidateVisual();
            e.Handled = true;
        }
    }

    public int NoteNameToIndex(string name)
    {
        string noteName = "";
        for (int i = _noteRangeMin; i <= _noteRangeMax; i++)
        {
            for (int note = 0; note < _Temperament; note++)
            {
                switch (note)
                {
                    case 0:
                        noteName = $"C{i}";
                        break;
                    case 1:
                        noteName = $"C#{i}";
                        break;
                    case 2:
                        noteName = $"D{i}";
                        break;
                    case 3:
                        noteName = $"D#{i}";
                        break;
                    case 4:
                        noteName = $"E{i}";
                        break;
                    case 5:
                        noteName = $"F{i}";
                        break;
                    case 6:
                        noteName = $"F#{i}";
                        break;
                    case 7:
                        noteName = $"G{i}";
                        break;
                    case 8:
                        noteName = $"G#{i}";
                        break;
                    case 9:
                        noteName = $"A{i}";
                        break;
                    case 10:
                        noteName = $"A#{i}";
                        break;
                    case 11:
                        noteName = $"B{i}";
                        break;
                }
                
                if (name.Equals(noteName))
                {
                    return (i * _Temperament + note);
                }
                
                
            }
        }
        return -1;
    }

    private string IndexToNoteName(int index)
    {
        string noteName = "";
        for (int i = _noteRangeMin - 1; i <= _noteRangeMax + 1; i++)
        {
            for (int note = 0; note < _Temperament; note++)
            {
                switch (note)
                {
                    case 0:
                        noteName = $"C{i}";
                        break;
                    case 1:
                        noteName = $"C#{i}";
                        break;
                    case 2:
                        noteName = $"D{i}";
                        break;
                    case 3:
                        noteName = $"D#{i}";
                        break;
                    case 4:
                        noteName = $"E{i}";
                        break;
                    case 5:
                        noteName = $"F{i}";
                        break;
                    case 6:
                        noteName = $"F#{i}";
                        break;
                    case 7:
                        noteName = $"G{i}";
                        break;
                    case 8:
                        noteName = $"G#{i}";
                        break;
                    case 9:
                        noteName = $"A{i}";
                        break;
                    case 10:
                        noteName = $"A#{i}";
                        break;
                    case 11:
                        noteName = $"B{i}";
                        break;
                }
                if (index.Equals(i * _Temperament + note))
                {
                    return noteName;
                }
            }
        }
        return "";
    }

    private IBrush NoteNameToBrush(string name)
    {
        if (name.Contains('#'))
        {
            return _noteColor2;
        }
        if (name[0] == 'C')
        {
            return _noteColor3;
        }
        return _noteColor1;
    }

    public void SaveNotes()
    {
        ViewHelper.GetMainWindow().PatternPreview.Notes = Notes;
        ViewHelper.GetMainWindow().PatternPreview.InvalidateVisual();
    }

    private void SelectedNotesMoveUp(object? sender, RoutedEventArgs e)
    {
        for (int i = 0; i < SelectedNotes.Count; i++)
        {
            if(Notes.Contains(SelectedNotes[i]))
            {
                Notes.Remove(SelectedNotes[i]);
                SelectedNotes[i] = new Note
                {
                    Color = SelectedNotes[i].Color,
                    StartTime = SelectedNotes[i].StartTime,
                    EndTime = SelectedNotes[i].EndTime,
                    Name = SelectedNotes[i].Name.Substring(0, SelectedNotes[i].Name.Length - 1)
                    + (int.Parse(SelectedNotes[i].Name[^1].ToString()) + 1)
                };
                Console.WriteLine(SelectedNotes[i].Name);
                Notes.Add(SelectedNotes[i]);
            }
        }

        _ = ShowOptions();
        InvalidateVisual();
    }
    private void SelectedNotesMoveDown(object? sender, RoutedEventArgs e)
    {
        for (int i = 0; i < SelectedNotes.Count; i++)
        {
            if(Notes.Contains(SelectedNotes[i]))
            {
                Notes.Remove(SelectedNotes[i]);
                SelectedNotes[i] = new Note
                {
                    Color = SelectedNotes[i].Color,
                    StartTime = SelectedNotes[i].StartTime,
                    EndTime = SelectedNotes[i].EndTime,
                    Name = SelectedNotes[i].Name.Substring(0, SelectedNotes[i].Name.Length - 1)
                           + (int.Parse(SelectedNotes[i].Name[^1].ToString()) - 1)
                };
                Console.WriteLine(SelectedNotes[i].Name);
                Notes.Add(SelectedNotes[i]);
            }
        }

        _ = ShowOptions();
        InvalidateVisual();
    }
}