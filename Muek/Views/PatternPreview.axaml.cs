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
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Muek.Models;
using Muek.Services;

namespace Muek.Views;

public partial class PatternPreview : UserControl
{
    // public static readonly StyledProperty<IBrush> BackgroundColorProperty =
    //     AvaloniaProperty.Register<PatternPreview, IBrush>(
    //         nameof(BackgroundColor));

    private IBrush BackgroundColor => DataStateService.PianoRollWindow.PatternColor.Background;
    // {
    //     get => GetValue(BackgroundColorProperty);
    //     set => SetValue(BackgroundColorProperty, value);
    // }

    private List<PianoRoll.Note> Notes => DataStateService.PianoRollWindow.EditArea.Notes;

    private bool _isHover = false;
    private bool _isPressed = false;
    private bool _isDragging = false;
    
    Brush background = new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0.9, 0.5, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1.0, 0.5, RelativeUnit.Relative),
        GradientStops = [
            new GradientStop(Colors.White, 0.0),
            new GradientStop(Colors.Transparent, 1.0)
        ]
    };
    
    private RenderTargetBitmap _notesCache;
    private bool _isCacheDirty = true;
    private int _lastNotesCount = 0;
    private double _lastBoundsWidth = 0;
    private double _lastBoundsHeight = 0;

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

        if (!DataStateService.PianoRollWindow.IsShowing)
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
                new Rect(DataStateService.PianoRollWindow.PianoRollRightScroll.Offset.X /
                         DataStateService.PianoRollWindow.EditArea.Width
                         * Bounds.Width
                    , 0,
                    DataStateService.PianoRollWindow.PianoRollRightScroll.Bounds.Width /
                    DataStateService.PianoRollWindow.EditArea.Width
                    * Bounds.Width,
                    Bounds.Height));
            if (_isHover)
            {
                context.DrawRectangle(null, new Pen(new SolidColorBrush(Colors.White)),
                    new Rect(DataStateService.PianoRollWindow.PianoRollRightScroll.Offset.X /
                             DataStateService.PianoRollWindow.EditArea.Width
                             * Bounds.Width
                        , 0,
                        DataStateService.PianoRollWindow.PianoRollRightScroll.Bounds.Width /
                        DataStateService.PianoRollWindow.EditArea.Width
                        * Bounds.Width,
                        Bounds.Height));
            }
        }

        // 使用缓存的音符渲染
        RenderNotesWithCache(context);
    }

    private void RenderNotesWithCache(DrawingContext context)
    {
        if (Notes.Count == 0)
        {
            // 如果没有音符，清理缓存
            if (_notesCache != null)
            {
                _notesCache.Dispose();
                _notesCache = null;
            }
            _lastNotesCount = 0;
            return;
        }

        // 检查是否需要更新缓存
        bool needsUpdate = _isCacheDirty || 
                          _notesCache == null ||
                          _lastNotesCount != Notes.Count ||
                          Math.Abs(_lastBoundsWidth - Bounds.Width) > 0.1 ||
                          Math.Abs(_lastBoundsHeight - Bounds.Height) > 0.1;

        if (needsUpdate)
        {
            UpdateNotesCache();
            _isCacheDirty = false;
            _lastNotesCount = Notes.Count;
            _lastBoundsWidth = Bounds.Width;
            _lastBoundsHeight = Bounds.Height;
        }

        // 绘制缓存
        if (_notesCache != null)
        {
            context.DrawImage(_notesCache, new Rect(0, 0, Bounds.Width, Bounds.Height));
        }
    }

    private void UpdateNotesCache()
    {
        // 释放之前的缓存
        _notesCache?.Dispose();
        
        // 检查有效尺寸
        if (Bounds.Width <= 0 || Bounds.Height <= 0 || double.IsNaN(Bounds.Width) || double.IsNaN(Bounds.Height))
        {
            _notesCache = null;
            return;
        }

        try
        {
            // 创建新的渲染目标位图
            var pixelSize = new PixelSize((int)Math.Ceiling(Bounds.Width), (int)Math.Ceiling(Bounds.Height));
            if (pixelSize.Width <= 0 || pixelSize.Height <= 0)
            {
                _notesCache = null;
                return;
            }

            _notesCache = new RenderTargetBitmap(pixelSize);
            
            using (var cacheContext = _notesCache.CreateDrawingContext())
            {
                double noteHeight;
                double noteWidth;
                int noteMax = Notes[0].Name;
                int noteMin = noteMax;
                double noteLast = Notes[0].EndTime;
                
                foreach (var note in Notes)
                {
                    var position = note.Name;
                    noteMin = Math.Min(position, noteMin);
                    noteMax = Math.Max(position, noteMax);
                    noteLast = Math.Max(noteLast, note.EndTime);
                }

                noteHeight = Bounds.Height / (noteMax + 1 - noteMin);
                noteWidth = Bounds.Width /
                            (noteLast + PianoRoll.LengthIncreasement);

                foreach (var note in Notes)
                {
                    var position = note.Name;
                    
                    cacheContext.FillRectangle(background,
                        new Rect(
                            note.StartTime * noteWidth,
                            Bounds.Height * .6 - (position - noteMin + 1) * noteHeight * .6 + Bounds.Height * .2,
                            noteWidth * (note.EndTime - note.StartTime),
                            noteHeight * .55),
                        (float)(noteHeight * .1));
                }
            }
        }
        catch (Exception ex)
        {
            // 如果创建缓存失败，回退到直接渲染
            System.Diagnostics.Debug.WriteLine($"Failed to create notes cache: {ex.Message}");
            _notesCache = null;
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
        if (!DataStateService.PianoRollWindow.IsShowing)
        {
            ScrollToNoteFirst();
        }

        _pressedPosition = e.GetPosition(this).X;
        _pressedScroll = DataStateService.PianoRollWindow.PianoRollRightScroll.Offset.X;
        _isPressed = true;
        if (DataStateService.PianoRollWindow.IsShowing)
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

            DataStateService.PianoRollWindow.PianoRollRightScroll.Offset = new Vector(
                noteFirst.StartTime * DataStateService.PianoRollWindow.EditArea.WidthOfBeat,
                DataStateService.PianoRollWindow.EditArea.Height - (noteFirst.Name + 1) *
                DataStateService.PianoRollWindow.EditArea.NoteHeight);
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
            DataStateService.PianoRollWindow.PianoRollRightScroll.Offset = new Vector(
                (e.GetPosition(this).X - _pressedPosition) / Bounds.Width
                * DataStateService.PianoRollWindow.EditArea.Width + _pressedScroll
                , DataStateService.PianoRollWindow.PianoRollRightScroll.Offset.Y
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