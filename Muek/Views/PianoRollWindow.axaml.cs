using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Muek.Models;
using Muek.Services;

namespace Muek.Views;

public partial class PianoRollWindow : UserControl
{
    private bool _isShowing = false;
    public bool IsShowing => _isShowing;
    private double _maxSize = 400.0;
    private bool _isDragging = false;
    public PianoRollWindow()
    {
        InitializeComponent();
        ClipToBounds = false;
        ResizePanel.PointerPressed += (sender, args) =>
        {
            _isDragging = true;
            if(_isShowing)
            {
                CloseButton.IsVisible = true;
                OpenButton.IsVisible = false;
            }
            else
            {
                _maxSize = 90;
            }
            args.Handled = true;
        };
        ResizePanel.PointerMoved += (sender, args) =>
        {
            if (_isDragging)
            {
                _maxSize = double.Clamp(Height - args.GetPosition(this).Y,90,600);
                Height = _maxSize;
                args.Handled = true;
                if(_maxSize > 150)
                    Show(sender, args);
                else
                {
                    _isShowing = false;
                }
            }
            else
            {
                if (_maxSize <= 90)
                {
                    CloseButton.IsVisible = false;
                    OpenButton.IsVisible = true;
                    _isShowing = false;
                    _maxSize = 400;
                }
                else
                if (_maxSize < 150)
                {
                    Hide(sender, args);
                    _maxSize = 400;
                }
            }
        };
        ResizePanel.PointerReleased += (sender, args) =>
        {
            _isDragging = false;
            
        };
        ResizePanel.PointerEntered += (sender, args) =>
        {
            ResizeBorder.IsVisible = true;
            Cursor = new Cursor(StandardCursorType.TopSide);
        };
        ResizePanel.PointerExited += (sender, args) =>
        {
            ResizeBorder.IsVisible = false;
            Cursor = new Cursor(StandardCursorType.Arrow);
        };
        DropDisplay.IsVisible = false;
        DropDisplay.Background = new SolidColorBrush(Colors.Black, .5);
        EditArea.SetValue(DragDrop.AllowDropProperty, true);
        EditArea.AddHandler(DragDrop.DragOverEvent,((sender, args) =>
        {
            DropDisplay.IsVisible = true;
        }));
        // EditArea.AddHandler(DragDrop.DropEvent, MidiDragDrop());
        DropDisplay.SetValue(DragDrop.AllowDropProperty, true);
        DropDisplay.AddHandler(DragDrop.DragOverEvent, MidiDragOver());
        DropDisplay.AddHandler(DragDrop.DragLeaveEvent, (sender, args) =>
        {
            DropDisplay.IsVisible = false;
        });
        DropDisplay.AddHandler(DragDrop.DropEvent, MidiDragDrop());
        OpenButton.Background = new SolidColorBrush(DataStateService.MuekColor);
    }

    private EventHandler<DragEventArgs>? MidiDragDrop()
    {
        return (sender, args) =>
        {
            if (args.Data.Contains(DataFormats.Files))
            {
                var files = args.Data.GetFileNames()?.ToList();
                if (files == null) return;
                foreach (var file in files)
                {
                    if (!Path.Exists(file)) continue;
                    if (Path.GetExtension(file).ToLower() != ".mid") continue;
                    EditArea.ImportMidi(file);
                }
            }
            DropDisplay.IsVisible = false;
        };
    }

    private EventHandler<DragEventArgs>? MidiDragOver()
    {
        return (sender, args)=>
        {
            args.DragEffects = DragDropEffects.Copy;
            DropDisplay.IsVisible = true;
            args.Handled = true;
            InvalidateVisual();
        };
    }

    private void Hide(object? sender, RoutedEventArgs e)
    {
        if(Height >= _maxSize)
        {
            _isShowing = false;
            CloseButton.IsVisible = false;
            OpenButton.IsVisible = true;
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
                                Property = HeightProperty,
                                Value = PatternPreview.Height + TopBar.Height
                            }
                        }
                    }
                }
            }.RunAsync(this);
        }
        else
        {
            _isShowing = false;
            CloseButton.IsVisible = false;
            OpenButton.IsVisible = true;
            _maxSize = 400;
        }
        PatternPreview.InvalidateVisual();
        e.Handled = true;
    }

    private void Show(object? sender, RoutedEventArgs e)
    {
        if (_isDragging)
        {
            _isShowing = true;
            CloseButton.IsVisible = true;
            OpenButton.IsVisible = false;
        }
        else if(Height <= PatternPreview.Height + TopBar.Height)
        {
            _isShowing = true;
            CloseButton.IsVisible = true;
            OpenButton.IsVisible = false;
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
                                Property = HeightProperty,
                                Value = _maxSize
                            }
                        }
                    }
                }
            }.RunAsync(this);
        }
        PatternPreview.InvalidateVisual();
        e.Handled = true;
    }

    private void ScrollChange(object? sender, ScrollChangedEventArgs e)
    {
        if (e.Source != null && e.Source.Equals(PianoRollLeft))
        {
            EditArea.Height = PianoBar.Height;
            PianoRollRight.Offset = new Vector(PianoRollRight.Offset.X,PianoRollLeft.Offset.Y);
            EditArea.NoteHeight = PianoBar.NoteHeight;
        }

        if (e.Source != null && e.Source.Equals(PianoRollRight))
        {
            PianoBar.Height = EditArea.Height;
            PianoRollLeft.Offset = new Vector(0,PianoRollRight.Offset.Y);
            PianoBar.NoteHeight = EditArea.NoteHeight;
            
        }
        EditArea.ScrollOffset = PianoRollRight.Offset.Y;
        EditArea.ClampValue = PianoRollRight.Offset.X;
        PatternPreview.InvalidateVisual();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            PianoRollRight.Offset = new Vector(PianoRollRight.Offset.X - e.Delta.Y * 44,PianoRollRight.Offset.Y );
            PianoRollRight.Offset = new Vector(Math.Max(0, PianoRollRight.Offset.X),PianoRollRight.Offset.Y) ; // 不允许左滚超过0
            
            InvalidateVisual();
            e.Handled = true;
        }
    }

    private void ImportMidiFile(object? sender, RoutedEventArgs e)
    {
        var files = new Window().StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import Midi file",
            FileTypeFilter =
            [
                new FilePickerFileType("MIDI File")
                {
                    Patterns = ["*.mid"]
                }
            ],
            AllowMultiple = false,
        }).GetAwaiter().GetResult();

        if (files.Count >= 1)
        {
            var file = files[0].Path.LocalPath;
            EditArea.ImportMidi(file);
        }

        e.Handled = true;
    }

    private void MagnetPropertyChange(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        EditArea.Magnet = MagnetSettingsWindow.SelectedGrid.Value;
        EditArea.InvalidateVisual();
    }

    private void ExportMidiFile(object? sender, RoutedEventArgs e)
    {
        
        EditArea.ExportMidi();
    }
}