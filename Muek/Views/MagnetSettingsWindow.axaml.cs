using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Muek.Services;

namespace Muek.Views;

public partial class MagnetSettingsWindow : UserControl
{
    public static readonly StyledProperty<Grid> SelectedGridProperty = AvaloniaProperty.Register<MagnetSettingsWindow, Grid>(
        nameof(SelectedGrid));

    public Grid SelectedGrid
    {
        get => GetValue(SelectedGridProperty);
        set => SetValue(SelectedGridProperty, value);
    }

    private List<Grid> _grids =
    [
        new Grid("2", 2),
        new Grid("1", 1),
        new Grid("1/2", 1 / 2.0),
        new Grid("1/3", 1 / 3.0),
        new Grid("1/4", 1 / 4.0),
        new Grid("1/6", 1 / 6.0),
        new Grid("1/8", 1 / 8.0),
        new Grid("1/16", 1 / 16.0),
    ];

    private bool _isPressed = false;
    private double _selectedGridPosition;
    
    public record struct Grid(string name, double value)
    {
        public readonly double Value = value;
        public readonly string Name = name;
    }
    public MagnetSettingsWindow()
    {
        SelectedGrid = _grids[1];
        InitializeComponent();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.DrawRectangle(new SolidColorBrush(new Color(255,255,255,255),.2),null,
            new Rect(0,0, Bounds.Width, Bounds.Height),5,5);
        var pen  = new Pen(new SolidColorBrush(Colors.White,.5), .5);
        foreach (var grid in _grids)
        {
            context.DrawLine(pen,
              new Point(Bounds.Width / _grids.Count *  _grids.IndexOf(grid) + Bounds.Width / _grids.Count / 2,0),
              new Point(Bounds.Width / _grids.Count *  _grids.IndexOf(grid) + Bounds.Width / _grids.Count / 2,Bounds.Height * .4)
              );
            context.DrawText(new FormattedText(grid.Name,
                    CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default,
                    12, new SolidColorBrush(DataStateService.MuekColor)),
                new Point(Bounds.Width / _grids.Count *  _grids.IndexOf(grid) + Bounds.Width / _grids.Count / 2 - grid.name.Length * 3,Bounds.Height - Bounds.Height * .6)
            );
        }

        if (!_isPressed)
        {
            _selectedGridPosition = Bounds.Width / _grids.Count * _grids.IndexOf(SelectedGrid) +
                                    Bounds.Width / _grids.Count / 2;
        }
        else
        {
            context.DrawLine(new Pen(Brushes.Orange),
                new Point(ClosestPosition(_selectedGridPosition), 0),
                new Point(ClosestPosition(_selectedGridPosition), Bounds.Height * .4));
        }
                
        
        var selectedPen = new Pen(new SolidColorBrush(DataStateService.MuekColor), 2);
        context.DrawLine(selectedPen,
            new Point(_selectedGridPosition,0),
            new Point(_selectedGridPosition,Bounds.Height * .4)
            );
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _isPressed = true;
        _selectedGridPosition = e.GetPosition(this).X;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isPressed)
        {
            _selectedGridPosition = e.GetPosition(this).X;
            // Console.WriteLine(_selectedGridPosition);
        }
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isPressed = false;
        _selectedGridPosition = ClosestPosition(_selectedGridPosition);
        InvalidateVisual();
    }

    private double ClosestPosition(double position)
    {
        double closestDistance = double.MaxValue;
        double closestPosition = position;

        foreach (var grid in _grids)
        {
            double gridPosition = Bounds.Width / _grids.Count * _grids.IndexOf(grid) + Bounds.Width / _grids.Count / 2;
            double distance = Math.Abs(position - gridPosition);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPosition = gridPosition;
                SelectedGrid = grid;
            }
        }
        return closestPosition;
    }
}