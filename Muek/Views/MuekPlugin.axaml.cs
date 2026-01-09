using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Remote.Protocol.Input;
using Avalonia.Themes.Neumorphism;
using Avalonia.Themes.Simple;
using DynamicData;
using Muek.Services;
using NAudio.Midi;
using Projektanker.Icons.Avalonia;

namespace Muek.Views;

public partial class MuekPlugin : UserControl
{
    public enum MuekPluginType
    {
        Empty,
        Oscillator,
        Sampler,
        Granular,
        Equalizer,
        Filter,
        Compressor,
        Limiter,
        Clipper,
        MultibandCompressor,
        MultibandLimiter,
        MultibandClipper,
        Delay,
        Reverb,
        Phaser,
        Flanger,
        Resonator,
        PitchShifter,
        FrequencyShifter,
        Vocoder,
        Distortion,
        Amplifier,
        Disperser,
        Stereo,
        Channel,
        Modulator,
        Meter
    }

    private MuekPluginType PluginType { get; } = MuekPluginType.Equalizer;

    private readonly BoxShadows _boxShadows = BoxShadows.Parse("0 0 10 #000000");
    private readonly BoxShadows _insetBoxShadow = BoxShadows.Parse("inset 0 0 10 #88000000");
    private readonly CornerRadius _cornerRadius = CornerRadius.Parse("4");

    private readonly IBrush _stroke = Brush.Parse("#88000000");
    private readonly Thickness _thickness = Thickness.Parse("4");
    
    public MuekPlugin()
    {
        InitializeComponent();
        SwitchPluginType();
    }
    public MuekPlugin(MuekPluginType pluginType)
    {
        PluginType = pluginType;
        InitializeComponent();
        SwitchPluginType();
    }

    private void SwitchPluginType()
    {
        switch (PluginType)
        {
            case MuekPluginType.Empty:
                InitEmpty();
                break;
            case MuekPluginType.Oscillator:
                InitOscillator();
                break;
            case MuekPluginType.Sampler:
                InitSampler();
                break;
            case MuekPluginType.Granular:
                InitGranular();
                break;
            case MuekPluginType.Equalizer:
                InitEqualizer();
                break;
        }
    }

    private void InitEmpty()
    {
        PluginColor.Background = Brushes.White;
        PluginName.Text = "Empty";
        var text = new TextBlock()
        {
            Text = "This is an empty plugin which will bypass the audio signal",
            Foreground = Brushes.DimGray
        };
        PluginContent.Child = text;
    }


    private void InitOscillator()
    {
        PluginColor.Background = DataStateService.MuekColorBrush;
        PluginName.Text = "Oscillator";
        var waveType = new ListBox()
        {
            SelectionMode = SelectionMode.AlwaysSelected,
            Background = Brushes.Transparent,
            FontSize = 16,
            VerticalAlignment = VerticalAlignment.Center,
            Items =
            {
                new Icon()
                {
                    Value = "mdi-sine-wave",
                },
                new Icon()
                {
                    Value = "mdi-sawtooth-wave"
                },
                new Icon()
                {
                    Value = "mdi-square-wave"
                }
            }
        };
        var osc = new Grid()
        {
            ColumnDefinitions = ColumnDefinitions.Parse("auto,auto,*"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ColumnSpacing = 8,
            Children =
            {
                new Border
                {
                    Width = 45,
                    Padding = Thickness.Parse("5"),
                    Background = Brush.Parse("#232323"),
                    CornerRadius = _cornerRadius,
                    BoxShadow = _boxShadows,
                    Child = waveType
                },
            }
        };
        var adsrWidth = 50;
        var attack = new Slider()
        {
            Width = adsrWidth,
            Height = 10,
            Minimum = 0,
            Maximum = 5000,
            Value = 500,
            Foreground = DataStateService.MuekColorBrush,
            Styles =
            {
                new NeumorphismTheme()
            }
        };
        var decay = new Slider()
        {
            Width = adsrWidth,
            Height = 10,
            Minimum = 0,
            Maximum = 5000,
            Value = 1000,
            Foreground = DataStateService.MuekColorBrush,
            Styles =
            {
                new NeumorphismTheme()
            }
        };
        var sustain = new Slider()
        {
            Width = adsrWidth * 1.5,
            Height = 10,
            Minimum = 0,
            Maximum = 1,
            Value = 0.8,
            Foreground = DataStateService.MuekColorBrush,
            Styles =
            {
                new NeumorphismTheme()
            }
        };
        var release = new Slider()
        {
            Width = adsrWidth,
            Height = 10,
            Minimum = 0,
            Maximum = 5000,
            Value = 500,
            Foreground = DataStateService.MuekColorBrush,
            Styles =
            {
                new NeumorphismTheme()
            }
        };
        var attackText = new TextBox()
        {
            Width = 80,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            Text = attack.Value.ToString("0.0"),
        };
        var decayText = new TextBox()
        {
            Width = 80,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            Text = decay.Value.ToString("0.0"),
        };
        var sustainText = new TextBox()
        {
            Width = 80,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            Text = sustain.Value.ToString("0.0"),
        };
        var releaseText = new TextBox()
        {
            Width = 80,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            Text = release.Value.ToString("0.0"),
        };
        
        attackText.TextChanged += (s, e) =>
        {
            attack.Value = double.Parse(attackText.Text);
            attackText.Text = attack.Value.ToString("0.0");
        };
        decayText.TextChanged += (s, e) =>
        {
            decay.Value = double.Parse(decayText.Text);
            decayText.Text = decay.Value.ToString("0.0");
        };
        sustainText.TextChanged += (s, e) =>
        {
            sustain.Value = double.Parse(sustainText.Text);
            sustainText.Text = sustain.Value.ToString("0.0");
        };
        releaseText.TextChanged += (s, e) =>
        {
            release.Value = double.Parse(releaseText.Text);
            releaseText.Text = release.Value.ToString("0.0");
        };


        var attackBend = new MuekValuer()
        {
            ValuerColor = DataStateService.MuekColorBrush,
            Layout = MuekValuer.LayoutEnum.Knob,
            Width = 15,
            Height = 15,
            Margin = Thickness.Parse("10"),
            Minimum = -1,
            Maximum = 1,
            DefaultValue = 0,
        };
        var decayBend = new MuekValuer()
        {
            ValuerColor = DataStateService.MuekColorBrush,
            Layout = MuekValuer.LayoutEnum.Knob,
            Width = 15,
            Height = 15,
            Margin = Thickness.Parse("10"),
            Minimum = -1,
            Maximum = 1,
            DefaultValue = 0,
        };
        var releaseBend = new MuekValuer()
        {
            ValuerColor = DataStateService.MuekColorBrush,
            Layout = MuekValuer.LayoutEnum.Knob,
            Width = 15,
            Height = 15,
            Margin = Thickness.Parse("10"),
            Minimum = -1,
            Maximum = 1,
            DefaultValue = 0,
        };
        
        
        var adsr = new Grid()
        {
            ColumnDefinitions = ColumnDefinitions.Parse("auto,*"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ColumnSpacing = 8,
            Children =
            {
                new Border
                {
                    Padding = Thickness.Parse("5"),
                    Background = Brush.Parse("#232323"),
                    CornerRadius = _cornerRadius,
                    BoxShadow = _boxShadows,
                    Child = new StackPanel()
                    {
                        Children =
                        {
                            new StackPanel()
                            {
                                Orientation = Orientation.Vertical,
                                Spacing = 10,
                                Children =
                                {
                                    new StackPanel()
                                    {
                                        Orientation = Orientation.Horizontal,
                                        Children =
                                        {
                                            new Label()
                                            {
                                                Content = "Attack",
                                                FontSize = 10,
                                                Width = 50,
                                                VerticalAlignment = VerticalAlignment.Center
                                            },
                                            attack,
                                            attackText,
                                            attackBend
                                        }
                                    },
                                    new StackPanel()
                                    {
                                        Orientation = Orientation.Horizontal,
                                        Children =
                                        {
                                            new Label()
                                            {
                                                Content = "Decay",
                                                FontSize = 10,
                                                Width = 50,
                                                VerticalAlignment = VerticalAlignment.Center
                                            },
                                            decay,
                                            decayText,
                                            decayBend
                                        }
                                    },
                                    new StackPanel()
                                    {
                                        Orientation = Orientation.Horizontal,
                                        Children =
                                        {
                                            new Label()
                                            {
                                                Content = "Sustain",
                                                FontSize = 10,
                                                Width = 50,
                                                VerticalAlignment = VerticalAlignment.Center
                                            },
                                            sustain,
                                            sustainText
                                        }
                                    },
                                    new StackPanel()
                                    {
                                        Orientation = Orientation.Horizontal,
                                        Children =
                                        {
                                            new Label()
                                            {
                                                Content = "Release",
                                                FontSize = 10,
                                                Width = 50,
                                                VerticalAlignment = VerticalAlignment.Center
                                            },
                                            release,
                                            releaseText,
                                            releaseBend
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
            }
        };
        var waveHeight = 150;
        var adsrWave = new Polyline()
        {
            Stroke = DataStateService.MuekColorBrush,
            StrokeThickness = 1,
            Height = waveHeight
        };
        var adsrFill = new Polygon()
        {
            Fill = new SolidColorBrush(DataStateService.MuekColor, 0.1),
            Height = waveHeight
        };


        var radius = 6;

        var attackPoint = new Ellipse()
        {
            Fill = DataStateService.MuekColorBrush,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Left,
            Height = radius,
            Width = radius,
            Margin = new Thickness(attack.Value/50-radius/2d,-radius/2d,0,0),
        };

        
        var sustainPoint = new Ellipse()
        {
            Fill = DataStateService.MuekColorBrush,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Left,
            Height = radius,
            Width = radius,
            Margin = new Thickness((attack.Value + decay.Value)/50-radius/2d,waveHeight - waveHeight * sustain.Value - radius/2d,0,0),
        };

        var releasePoint = new Ellipse()
        {
            Fill = DataStateService.MuekColorBrush,
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Left,
            Height = radius,
            Width = radius,
            Margin = new Thickness((attack.Value + decay.Value + release.Value)/50-radius/2d,0,0,-radius/2d),
        };

        var attackPointPressed = false;

        var sustainPointPressed = false;

        var releasePointPressed = false;

        attackPoint.PointerPressed += (sender, args) => { attackPointPressed = true; args.Handled = true; };

        attackPoint.PointerReleased += (sender, args) => { attackPointPressed = false; args.Handled = true; };

        sustainPoint.PointerPressed += (sender, args) => { sustainPointPressed = true; args.Handled = true; };

        sustainPoint.PointerReleased += (sender, args) => { sustainPointPressed = false; args.Handled = true; };

        releasePoint.PointerPressed += (sender, args) => { releasePointPressed = true; args.Handled = true; };

        releasePoint.PointerReleased += (sender, args) => { releasePointPressed = false; args.Handled = true; };

        attackPoint.PointerMoved += (sender, args) =>
        {
            if (!attackPointPressed) return;
            var position = args.GetPosition((sender as Visual)!.Parent as Visual);
            attack.Value = position.X * 50;
            args.Handled = true;
        };

        sustainPoint.PointerMoved += (sender, args) =>
        {
            if (!sustainPointPressed) return;
            var position = args.GetPosition((sender as Visual)!.Parent as Visual);
            decay.Value = position.X * 50 - attack.Value;
            sustain.Value = (waveHeight - position.Y) / waveHeight;
            args.Handled = true;
        };
        
        releasePoint.PointerMoved += (sender, args) =>
        {
            if (!releasePointPressed) return;
            var position = args.GetPosition((sender as Visual)!.Parent as Visual);
            release.Value = position.X * 50 - attack.Value - decay.Value;
            args.Handled = true;
        };

        attackPoint.PointerEntered += (sender, args) =>
        {
            attackPoint.Fill = Brushes.White;
            args.Handled = true;
        };
        sustainPoint.PointerEntered += (sender, args) =>
        {
            sustainPoint.Fill = Brushes.White;
            args.Handled = true;
        };
        releasePoint.PointerEntered += (sender, args) =>
        {
            releasePoint.Fill = Brushes.White;
            args.Handled = true;
        };
        attackPoint.PointerExited += (sender, args) =>
        {
            attackPoint.Fill = DataStateService.MuekColorBrush;
            args.Handled = true;
        };
        sustainPoint.PointerExited += (sender, args) =>
        {
            sustainPoint.Fill = DataStateService.MuekColorBrush;
            args.Handled = true;
        };
        releasePoint.PointerExited += (sender, args) =>
        {
            releasePoint.Fill = DataStateService.MuekColorBrush;
            args.Handled = true;
        };

        var attackBendPoint = new Ellipse()
        {
            Fill = Brushes.Transparent,
            Stroke = DataStateService.MuekColorBrush,
            StrokeThickness = 1,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Left,
            Height = radius,
            Width = radius,
        };
        var sustainBendPoint = new Ellipse()
        {
            Fill = Brushes.Transparent,
            Stroke = DataStateService.MuekColorBrush,
            StrokeThickness = 1,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Left,
            Height = radius,
            Width = radius,
        };
        var releaseBendPoint = new Ellipse()
        {
            Fill = Brushes.Transparent,
            Stroke = DataStateService.MuekColorBrush,
            StrokeThickness = 1,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Left,
            Height = radius,
            Width = radius,
        };

        var attackBendPointPressed = false;
        var sustainBendPointPressed = false;
        var releaseBendPointPressed = false;

        Point attackBendPointPressedPosition = default;
        Point sustainBendPointPressedPosition = default;
        Point releaseBendPointPressedPosition = default;

        double attackBendPressedValue = attackBend.Value;
        double sustainBendPressedValue = decayBend.Value;
        double releaseBendPressedValue = releaseBend.Value;
        
        attackBendPoint.PointerPressed += (sender, args) =>
        {
            attackBendPointPressed = true;
            attackBendPointPressedPosition = args.GetPosition((sender as Visual)!.Parent as Visual);
            attackBendPressedValue = attackBend.Value;
            args.Handled = true;
        };
        sustainBendPoint.PointerPressed += (sender, args) =>
        {
            sustainBendPointPressed = true;
            sustainBendPointPressedPosition = args.GetPosition((sender as Visual)!.Parent as Visual);
            sustainBendPressedValue = decayBend.Value;
            args.Handled = true;
        };
        releaseBendPoint.PointerPressed += (sender, args) =>
        {
            releaseBendPointPressed = true;
            releaseBendPointPressedPosition = args.GetPosition((sender as Visual)!.Parent as Visual);
            releaseBendPressedValue = releaseBend.Value;
            args.Handled = true;
        };
        attackBendPoint.PointerReleased += (sender, args) =>
        {
            attackBendPointPressed = false;
            args.Handled = true;
        };
        sustainBendPoint.PointerReleased += (sender, args) =>
        {
            sustainBendPointPressed = false;
            args.Handled = true;
        };
        releaseBendPoint.PointerReleased += (sender, args) =>
        {
            releaseBendPointPressed = false;
            args.Handled = true;
        };
        
        attackBendPoint.PointerMoved += (sender, args) =>
        {
            if (!attackBendPointPressed) return;
            var position = args.GetPosition((sender as Visual)!.Parent as Visual);
            var offset = (position - attackBendPointPressedPosition).Y;
            attackBend.Value = attackBendPressedValue - offset / waveHeight;
            args.Handled = true;
        };
        sustainBendPoint.PointerMoved += (sender, args) =>
        {
            if (!sustainBendPointPressed) return;
            var position = args.GetPosition((sender as Visual)!.Parent as Visual);
            var offset = (position - sustainBendPointPressedPosition).Y;
            decayBend.Value = sustainBendPressedValue - offset / waveHeight;
            args.Handled = true;
        };
        releaseBendPoint.PointerMoved += (sender, args) =>
        {
            if (!releaseBendPointPressed) return;
            var position = args.GetPosition((sender as Visual)!.Parent as Visual);
            var offset = (position - releaseBendPointPressedPosition).Y;
            releaseBend.Value = releaseBendPressedValue - offset / waveHeight;
            args.Handled = true;
        };

        attackBendPoint.PointerEntered += (sender, args) =>
        {
            attackBendPoint.Stroke = Brushes.White;
            args.Handled = true;
        };
        sustainBendPoint.PointerEntered += (sender, args) =>
        {
            sustainBendPoint.Stroke = Brushes.White;
            args.Handled = true;
        };
        releaseBendPoint.PointerEntered += (sender, args) =>
        {
            releaseBendPoint.Stroke = Brushes.White;
            args.Handled = true;
        };
        attackBendPoint.PointerExited += (sender, args) =>
        {
            attackBendPoint.Stroke = DataStateService.MuekColorBrush;
            args.Handled = true;
        };
        sustainBendPoint.PointerExited += (sender, args) =>
        {
            sustainBendPoint.Stroke = DataStateService.MuekColorBrush;
            args.Handled = true;
        };
        releaseBendPoint.PointerExited += (sender, args) =>
        {
            releaseBendPoint.Stroke = DataStateService.MuekColorBrush;
            args.Handled = true;
        };
        

        UpdateAdsr();
        attack.ValueChanged += (sender, args) =>
        {
            UpdateAdsr();
            attackText.Text = attack.Value.ToString("0.0");
        };
        decay.ValueChanged += (sender, args) =>
        {
            UpdateAdsr();
            decayText.Text = decay.Value.ToString("0.0");
        };
        sustain.ValueChanged += (sender, args) =>
        {
            UpdateAdsr(); 
            sustainText.Text = sustain.Value.ToString("0.0");
        };
        release.ValueChanged += (sender, args) =>
        {
            UpdateAdsr(); 
            releaseText.Text = release.Value.ToString("0.0");
        };

        attackBend.ValueChanged += (sender, args) => { UpdateAdsr(); };
        decayBend.ValueChanged += (sender, args) => { UpdateAdsr(); };
        releaseBend.ValueChanged += (sender, args) => { UpdateAdsr(); };


        void UpdateAdsr()
        {
            Point[] points = 
            [
                new(0, waveHeight),
                new(attack.Value / 50, 0),
                new((attack.Value + decay.Value) / 50, waveHeight - waveHeight * sustain.Value),
                new((attack.Value + decay.Value + release.Value) / 50, waveHeight)
            ];

            var bendCount = 20;

            var attackPoints = Enumerable.Range(1, bendCount).Select(i =>
            {
                var index = (double)i /  bendCount;
                var x = index * attack.Value / 50;
                var value = -attackBend.Value * 10;
                var y = value == 0 ? index
                    :(double.Exp(value * index) - 1) / (double.Exp(value) - 1);
                var point = new Point(x, waveHeight - waveHeight * y);
                return point;
            }).ToList();
            var decayPoints = Enumerable.Range(1, bendCount).Select(i =>
            {
                var index = (double)i /  bendCount;
                var x = attack.Value / 50 + index * decay.Value / 50;
                var value = decayBend.Value * 10;
                var y = value == 0 ? index
                    :(double.Exp(value * index) - 1) / (double.Exp(value) - 1);
                var point = new Point(x, (1 - sustain.Value) * waveHeight * y);
                return point;
            }).ToList();
            var releasePoints = Enumerable.Range(1, bendCount).Select(i =>
            {
                var index = (double)i /  bendCount;
                var x = (attack.Value + decay.Value) / 50 + index * release.Value / 50;
                var value = releaseBend.Value * 10;
                var y = value == 0 ? index
                    :(double.Exp(value * index) - 1) / (double.Exp(value) - 1);
                var point = new Point(x, (1 - sustain.Value) * waveHeight + sustain.Value * waveHeight * y);
                return point;
            }).ToList();
            
            adsrWave.Points =
                new List<Point>()
                {
                    points[0]
                }
                .Concat(
                    attackPoints)
                .Concat(new List<Point>()
                {
                    points[1]
                })
                .Concat(
                    decayPoints)
                .Concat(new List<Point>() 
                {
                    points[2] 
                })
                .Concat(
                    releasePoints)
                .Concat(new List<Point>() 
                {
                    points[3] 
                }).ToList();
            adsrFill.Points = adsrWave.Points;
            
            attackPoint.Margin = new Thickness(attack.Value / 50 - radius / 2d, -radius / 2d, 0, 0);
            sustainPoint.Margin = new Thickness((attack.Value + decay.Value) / 50 - radius / 2d,
                waveHeight - waveHeight * sustain.Value - radius/2d, 0, 0);
            releasePoint.Margin =
                new Thickness((attack.Value + decay.Value + release.Value) / 50 - radius / 2d, 0, 0, -radius/2d);

            var attackBendPointPos = attackPoints[attackPoints.Count / 2 - 1];
            var sustainBendPointPos = decayPoints[decayPoints.Count / 2 - 1];
            var releaseBendPointPos = releasePoints[releasePoints.Count / 2 - 1];
            attackBendPoint.Margin =
                new Thickness(attackBendPointPos.X - radius / 2d, attackBendPointPos.Y - radius / 2d, 0, 0);
            sustainBendPoint.Margin = new Thickness(sustainBendPointPos.X - radius / 2d,
                sustainBendPointPos.Y - radius / 2d, 0, 0);
            releaseBendPoint.Margin = new Thickness(releaseBendPointPos.X - radius / 2d,
                releaseBendPointPos.Y - radius / 2d, 0, 0);
        }


        var adsrWaveBorder =
            new Grid()
            {
                Height = 150,
                Children =
                {
                    new Grid()
                    {
                        VerticalAlignment = VerticalAlignment.Top,
                        Children =
                        {
                            adsrWave,
                            adsrFill,
                    
                            attackBendPoint,
                            sustainBendPoint,
                            releaseBendPoint,

                            attackPoint,
                            sustainPoint,
                            releasePoint
                        }
                    }
                }
            };

        var adsrBorder = new Border()
        {
            CornerRadius = _cornerRadius,
            BoxShadow = _boxShadows,
            Background = Brush.Parse("#232323"),
            Padding = _thickness,
            Child = new Grid()
            {
                Children =
                {
                    new Border()
                    {
                        Background = Brush.Parse("#23000000"),
                        Margin = Thickness.Parse("20 0 0 0"),
                        CornerRadius = _cornerRadius,
                        BoxShadow = _insetBoxShadow,
                        ClipToBounds = true,
                        Child = new Border()
                        {
                            VerticalAlignment = VerticalAlignment.Bottom,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            Margin = new Thickness(0, 0, 0, radius / 2d),
                            ClipToBounds = false,
                            Child = adsrWaveBorder
                        }
                    },
                }
            }
        };
        
        var grid = new Grid()
        {
            RowDefinitions = RowDefinitions.Parse("auto,auto"),
            RowSpacing = 8,
            
        };
        adsr.Children.Add(adsrBorder);
        Grid.SetColumn(adsrBorder,1);
        grid.Children.Add(osc);
        grid.Children.Add(adsr);
        Grid.SetRow(adsr, 1);
        var pitch = new NumericUpDown()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Minimum = -24,
            Maximum = 24,
            Value = 0,
            Increment = 1,
            Height = 25,
            Padding = Thickness.Parse("0"),
            BorderBrush = Brushes.Transparent,
            Styles =
            {
                new SimpleTheme()
            },
            CornerRadius = CornerRadius.Parse("5")
        };
        var cents = new NumericUpDown()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Minimum = -50,
            Maximum = 50,
            Value = 0,
            Increment = 1,
            Height = 25,
            Padding = Thickness.Parse("0"),
            BorderBrush = Brushes.Transparent,
            Styles =
            {
                new SimpleTheme()
            },
            CornerRadius = CornerRadius.Parse("5")
        };
        var pan = new Slider
        {
            Foreground = DataStateService.MuekColorBrush,
            Minimum = -1,
            Maximum = 1,
            Value = 0,
            Height = 10,
            TickPlacement = TickPlacement.BottomRight,
            TickFrequency = 1,
            Styles =
            {
                new NeumorphismTheme()
            },
            Margin = Thickness.Parse("0 0 0 20"),
            Width = 100,
        };
        var level = new Slider
        {
            Foreground = DataStateService.MuekColorBrush,
            Minimum = 0,
            Maximum = 100,
            Value = 100,
            Height = 10,
            TickPlacement = TickPlacement.BottomRight,
            TickFrequency = 10,
            Styles =
            {
                new NeumorphismTheme()
            },
            Margin = Thickness.Parse("0 0 0 20"),
            Width = 100,
        };
        var panText = new TextBox()
        {
            Width = 50,
            Height = 10,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            Text = pan.Value.ToString("0.0"),
            Margin = Thickness.Parse("0 -20 0 0")
        };
        panText.TextChanged += (_, _) =>
        {
            if (panText.Text == "-")
                panText.Text = "-0";
            pan.Value = double.Parse(panText.Text);
            panText.Text = pan.Value.ToString("0.0");
        };
        var levelText = new TextBox()
        {
            Width = 50,
            Height = 10,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            Text = level.Value.ToString("0.0"),
            Margin = Thickness.Parse("0 -20 0 0")
        };
        level.ValueChanged += (_, _) =>
        {
            level.Value = double.Round(level.Value,2);
            levelText.Text = level.Value.ToString("0.0");
        };
        levelText.TextChanged += (_, _) =>
        {
            level.Value = double.Parse(levelText.Text);
            levelText.Text = level.Value.ToString("0.0");
        };

        var settingsBorder = new Border()
        {
            Background = Brush.Parse("#232323"),
            Padding = Thickness.Parse("5"),
            CornerRadius = _cornerRadius,
            BoxShadow = _boxShadows,
            Child = new StackPanel()
            {
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new Label()
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        FontSize = 10,
                        Content = "Pitch",
                    },
                    pitch,
                    new Label()
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        FontSize = 10,
                        Content = "Cents",
                    },
                    cents,
                    new Label()
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        FontSize = 10,
                        Content = "Pan",
                    },
                    new StackPanel()
                    {
                        Orientation = Orientation.Horizontal,
                        Children =
                        {
                            pan,
                            panText
                        }
                    },
                    new Label()
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        FontSize = 10,
                        Content = "Level",
                    },
                    new StackPanel()
                    {
                        Orientation = Orientation.Horizontal,
                        Children =
                        {
                            level,
                            levelText
                        }
                    },
                }
            }
        };
        
        var wave = new Polyline()
        {
            Height = 100,
            Width = 100,
            Stroke = DataStateService.MuekColorBrush,
            StrokeThickness = 0.7,
        };
        var waveFill = new Polygon()
        {
            Fill = new SolidColorBrush(DataStateService.MuekColor, 0.1),
            Height = 100,
            Width = 100,
        };
        var panFill = new Rectangle()
        {
            Fill = new LinearGradientBrush()
            {
                Opacity = 0.1,
                StartPoint = new RelativePoint(0,0,RelativeUnit.Relative),
                EndPoint = new RelativePoint(1,0, RelativeUnit.Relative),
                GradientStops = [
                    new GradientStop(DataStateService.MuekColor, 0),
                    new GradientStop(Colors.Transparent, 1)
                ]
            },
            Height = 100,
            Width = 0,
            Margin = Thickness.Parse("50 0 0 0"),
            HorizontalAlignment = HorizontalAlignment.Left,
            RenderTransform = new RotateTransform(0)
        };
        pan.ValueChanged += (_, _) =>
        {
            pan.Value = double.Clamp(pan.Value, -1, 1);
            pan.Value = double.Round(pan.Value, 2);
            panText.Text = pan.Value.ToString("0.0");
            panFill.Width = pan.Value == 0 ? 0 : 50;
            panFill.Fill = new LinearGradientBrush()
            {
                Opacity = 0.1,
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
                GradientStops =
                [
                    new GradientStop(DataStateService.MuekColor, 0),
                    new GradientStop(Colors.Transparent, double.Abs(pan.Value))
                ]
            };
            panFill.Margin = pan.Value >= 0 ? Thickness.Parse("50 0 0 0") : Thickness.Parse("0");
            panFill.RenderTransform = pan.Value >= 0 ? new RotateTransform(0) : new RotateTransform(180);
        };
        WavetableChange();
        waveType.SelectionChanged += (sender, args) =>
        {
            WavetableChange();
        };
        level.ValueChanged += (sender, args) =>
        {
            WavetableChange();
        };

        void WavetableChange()
        {
            switch (waveType.SelectedIndex)
            {
                case 0:
                    wave.Points = Enumerable.Range(0, 101).Select(i =>
                        new Point(i, -50 * level.Value / 100d * Math.Sin(2 * Math.PI * i / 100) + 50)
                    ).ToList();
                    break;
                case 1:
                    wave.Points = Enumerable.Range(0, 101).Select(i =>
                        new Point(i, 100-(i + 50)%100 * level.Value / 100d - 50 * (100-level.Value) / 100d)
                    ).ToList();
                    break;
                case 2:
                    wave.Points = Enumerable.Range(0, 101).Select(i =>
                        new Point(i,i is 0 or 100 ? 50 : i < 50 ? 0 + (50-level.Value/2d) : 50 + level.Value/2d)
                    ).ToList();
                    break;
            }
            waveFill.Points = wave.Points;
            
        }

        var waveBorder = new Border()
        {
            Background = Brush.Parse("#23000000"),
            CornerRadius = _cornerRadius,
            Margin = Thickness.Parse("20 0 0 0"),
            BoxShadow = _insetBoxShadow,
            ClipToBounds = true,
            Child = new Viewbox()
            {
                ClipToBounds = false,
                Height = 180,
                StretchDirection = StretchDirection.UpOnly,
                Stretch = Stretch.Fill,
                Child = new Grid()
                {
                    Children =
                    {
                        panFill,
                        waveFill,
                        wave
                    }
                }
            }
        };

        var waveBorderPressed = false;
        var waveBorderPressedPosition = new Point();
        var waveBorderPressedValue = level.Value;
        waveBorder.PointerPressed += (sender, args) =>
        {
            waveBorderPressed = true;
            waveBorderPressedPosition = args.GetPosition(sender as Visual);
            waveBorderPressedValue = level.Value;
            args.Handled = true;
        };
        waveBorder.PointerMoved += (sender, args) =>
        {
            if (!waveBorderPressed) return;
            var position = args.GetPosition(sender as Visual);
            level.Value = waveBorderPressedValue - position.Y + waveBorderPressedPosition.Y;
            args.Handled = true;
        };
        waveBorder.PointerReleased += (sender, args) =>
        {
            waveBorderPressed = false;
            args.Handled = true;
        };

        var waveReference = new Grid()
        {
            Children =
            {
                new Line()
                {
                    Stroke = Brush.Parse("#23ffffff"),
                    StartPoint = Point.Parse("8 94"),
                    EndPoint = Point.Parse("15 94")
                },
                new Label()
                {
                    Content  = "-inf",
                    Margin = Thickness.Parse("0 80 0 0"),
                    Foreground = Brush.Parse("#23ffffff"),
                    FontSize = 8
                },
                new Line()
                {
                    Stroke = Brush.Parse("#23ffffff"),
                    StartPoint = Point.Parse("12 49"),
                    EndPoint = Point.Parse("15 49"),
                },
                new Line()
                {
                    Stroke = Brush.Parse("#23ffffff"),
                    StartPoint = Point.Parse("12 139"),
                    EndPoint = Point.Parse("15 139"),
                },
                new Line()
                {
                    Stroke = Brush.Parse("#23ffffff"),
                    StartPoint = Point.Parse("8 4"),
                    EndPoint = Point.Parse("15 4"),
                },
                new Label()
                {
                    Content  = "-6dB",
                    Margin = Thickness.Parse("-4 4 0 0"),
                    Foreground = Brush.Parse("#23ffffff"),
                    FontSize = 8
                },
                new Line()
                {
                    Stroke = Brush.Parse("#23ffffff"),
                    StartPoint = Point.Parse("8 184"),
                    EndPoint = Point.Parse("15 184"),
                },
                new Label()
                {
                    Content  = "-6dB",
                    Margin = Thickness.Parse("-4 170 0 0"),
                    Foreground = Brush.Parse("#23ffffff"),
                    FontSize = 8
                },
            }
        };
        
        var viewBorder = new Border()
        {
            CornerRadius = _cornerRadius,
            BoxShadow = _boxShadows,
            Background = Brush.Parse("#232323"),
            Padding = _thickness,
            Child = new Grid(){
                Children =
                {
                    waveBorder,
                    waveReference
                }
            }
        };

        viewBorder.SizeChanged += (sender, args) =>
        {
            waveReference.IsVisible = viewBorder.Bounds.Width >= 20;
        };
        
        osc.Children.Add(settingsBorder);
        osc.Children.Add(viewBorder);
        Grid.SetColumn(settingsBorder, 1);
        Grid.SetColumn(viewBorder, 2);
        PluginContent.Child = grid;
    }

    private void InitSampler()
    {
        PluginName.Text = "Sampler";
        var grid = new Grid()
        {
            ColumnDefinitions = ColumnDefinitions.Parse("auto,auto,*")
        };
        var border = new Border()
        {
            Height = 50,
            Width = 50,
            Background = Brush.Parse("#232323"),
            Margin = Thickness.Parse("0 0 8 0"),
        };
        grid.Children.Add(border);
        PluginContent.Child = grid;
        throw new NotImplementedException();
    }

    private void InitGranular()
    {
        throw new NotImplementedException();
    }
    
    
    private void InitEqualizer()
    {
        PluginName.Text = "Equalizer";
        PluginColor.Background = Brushes.DeepSkyBlue;
        var maximum = 48;
        var minimum = -maximum;
        var sliderHeight = 100;
        var sliderWidth = 5;
        var maximumFreq = 30000;
        var minimumFreq = 10;
        var scale = 20;

        var height = 150;

        double FreqMapping(double freq)
        {
            if(freq < 0) throw new ArgumentOutOfRangeException(nameof(freq));
            // var freqMapping = freq <= 10 ? double.Log(freq) - double.Log(minimumFreq)
            //     : freq <= 100 ? double.Log(freq-10) + double.Log(10) - double.Log(minimumFreq)
            //     : freq <= 1000 ? double.Log(freq-100) + double.Log(100) - double.Log(minimumFreq)
            //     : freq <= 10000 ? double.Log(freq-1000) + double.Log(1000) - double.Log(minimumFreq)
            //     : freq <= 100000 ? double.Log(freq-10000) + double.Log(10000) - double.Log(minimumFreq)
            //     : double.NaN;
            var freqMapping = 20 *
                (double.Log(freq) - double.Log(minimumFreq)) / 
                (double.Log(maximumFreq) - double.Log(minimumFreq));
            return freqMapping;
        }

        var bandCount = 7;
        
        
        var knobRadius = 10;
        var qMaximum = 40;
        var qMinimum = 0.025;

        List<MuekValuer> pointLevels;
        List<MuekValuer> pointFreqs;
        List<double> defaultFreqs;
        List<MuekValuer> qs;
        List<StackPanel> bandParams;
        List<bool> pointPressed;
        List<bool> pointHovered;
        var radius = 8;
        List<Ellipse> points;
        var curve = new Polyline()
        {
            Stroke = Brushes.DeepSkyBlue,
            Height = height,
        };
        void InitPoints()
        {
            pointLevels = Enumerable.Range(0, bandCount).Select(_ => new MuekValuer()
            {
                ValuerColor = Brushes.DeepSkyBlue,
                Layout = MuekValuer.LayoutEnum.Slider,
                Height = sliderHeight,
                Minimum = minimum,
                Maximum = maximum,
                DefaultValue = 0,
                Width = sliderWidth,
            }).ToList();

            defaultFreqs = Enumerable.Range(1, bandCount).Select(i =>
                double.Exp(
                    ((FreqMapping(maximumFreq) - FreqMapping(minimumFreq)) / (bandCount + 1) * i +
                     FreqMapping(minimumFreq))
                    * (double.Log(maximumFreq) - double.Log(minimumFreq)) / 20 + double.Log(minimumFreq))
            ).ToList();

            pointFreqs = Enumerable.Range(0, bandCount).Select(i => new MuekValuer()
            {
                ValuerColor = Brushes.DeepSkyBlue,
                Layout = MuekValuer.LayoutEnum.Knob,
                Minimum = FreqMapping(minimumFreq),
                Maximum = FreqMapping(maximumFreq),
                Width = knobRadius,
                Height = knobRadius,
                DefaultValue = FreqMapping(defaultFreqs[i]),
            }).ToList();
            
            qs = Enumerable.Range(0, bandCount).Select(_ => new MuekValuer()
            {
                ValuerColor = Brushes.DeepSkyBlue,
                Layout = MuekValuer.LayoutEnum.Knob,
                Height = knobRadius,
                Width = knobRadius,
                LogMaximum = qMaximum,
                LogMinimum = qMinimum,
                DefaultValue = double.Log(1),
            }).ToList();
            
                bandParams = Enumerable.Range(0, bandCount).Select(i => new StackPanel()
                {
                    Spacing = 12,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Children =
                    {
                        pointLevels[i],
                        pointFreqs[i],
                        qs[i],
                    }
                }
            ).ToList();
                
            points = Enumerable.Range(0, bandCount).Select(_ => new Ellipse()
            {
                Fill = Brushes.DeepSkyBlue,
                Width = radius,
                Height = radius,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            }).ToList();
            pointPressed = Enumerable.Range(0, bandCount).Select(_ => false).ToList();
            pointHovered = Enumerable.Range(0, bandCount).Select(_ => false).ToList();
            for (int i = 0; i < pointLevels.Count; i++)
            {
                pointLevels[i].ValueChanged += (sender, args) => { UpdateCurve(); };
            }
            for (int i = 0; i < pointFreqs.Count; i++)
            {
                pointFreqs[i].ValueChanged += (sender, args) => { UpdateCurve(); };
            }

            for (int i = 0; i < qs.Count; i++)
            {
                qs[i].ValueChanged += (sender, args) => { UpdateCurve(); };
            }
            for (int i = 0; i < points.Count; i++)
            {
                var index = i;
                points[index].PointerPressed += (sender, args) =>
                {
                    pointPressed[index] = true; args.Handled = true;
                };
                points[index].PointerMoved += (sender, args) =>
                {
                    ShowInfo(args.GetPosition((sender as Visual)!.Parent as Visual).X,
                        args.GetPosition((sender as Visual)!.Parent as Visual).Y);
                    if(!pointPressed[index]) return;
                    var position = args.GetPosition(curve);
                    pointFreqs[index].Value = position.X / scale;
                    pointLevels[index].Value = (-position.Y + height / 2d) / height * maximum * 2;
                    args.Handled = true;
                };
                points[index].PointerReleased += (sender, args) => { pointPressed[index] = false; args.Handled = true; };
                points[index].PointerEntered += (sender, args) => { points[index].Fill = Brushes.White; pointHovered[index] = true; args.Handled = true; };
                points[index].PointerExited += (sender, args) => { points[index].Fill = Brushes.DeepSkyBlue; pointHovered[index] = false; args.Handled = true; };
                points[index].PointerWheelChanged += (sender, args) =>
                {
                    ShowInfo(args.GetPosition((sender as Visual)!.Parent as Visual).X,
                        args.GetPosition((sender as Visual)!.Parent as Visual).Y);
                    if(!pointHovered[index]) return;
                    var offset = args.Delta.Y * 0.2;
                    qs[index].Value  += offset;
                    args.Handled = true;
                };
            }
        }

        void AddNewPoint()
        {
            bandCount++;
            pointLevels.Add(new MuekValuer()
            {
                ValuerColor = Brushes.DeepSkyBlue,
                Layout = MuekValuer.LayoutEnum.Slider,
                Height = sliderHeight,
                Minimum = minimum,
                Maximum = maximum,
                DefaultValue = 0,
                Width = sliderWidth,
            });

            defaultFreqs.Add(100d);

            pointFreqs.Add(new MuekValuer()
            {
                ValuerColor = Brushes.DeepSkyBlue,
                Layout = MuekValuer.LayoutEnum.Knob,
                Minimum = FreqMapping(minimumFreq),
                Maximum = FreqMapping(maximumFreq),
                Width = knobRadius,
                Height = knobRadius,
                DefaultValue = FreqMapping(defaultFreqs[^1]),
            });
            
            qs.Add(new MuekValuer()
            {
                ValuerColor = Brushes.DeepSkyBlue,
                Layout = MuekValuer.LayoutEnum.Knob,
                Height = knobRadius,
                Width = knobRadius,
                LogMaximum = qMaximum,
                LogMinimum = qMinimum,
                DefaultValue = double.Log(1),
            });
            
                bandParams.Add(new StackPanel()
                {
                    Spacing = 12,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Children =
                    {
                        pointLevels[^1],
                        pointFreqs[^1],
                        qs[^1],
                    }
                }
            );
                
            points.Add(new Ellipse()
            {
                Fill = Brushes.DeepSkyBlue,
                Width = radius,
                Height = radius,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            });
            pointPressed.Add(false);
            pointHovered.Add(false);
            
                pointLevels[^1].ValueChanged += (sender, args) => { UpdateCurve(); };
                
                pointFreqs[^1].ValueChanged += (sender, args) => { UpdateCurve(); };
            
                qs[^1].ValueChanged += (sender, args) => { UpdateCurve(); };
                
            
                var index = ^1;
                points[index].PointerPressed += (sender, args) =>
                {
                    pointPressed[index] = true; args.Handled = true;
                };
                points[index].PointerMoved += (sender, args) =>
                {
                    ShowInfo(args.GetPosition((sender as Visual)!.Parent as Visual).X,
                        args.GetPosition((sender as Visual)!.Parent as Visual).Y);
                    if(!pointPressed[index]) return;
                    var position = args.GetPosition(curve);
                    pointFreqs[index].Value = position.X / scale;
                    pointLevels[index].Value = (-position.Y + height / 2d) / height * maximum * 2;
                    args.Handled = true;
                };
                points[index].PointerReleased += (sender, args) => { pointPressed[index] = false; args.Handled = true; };
                points[index].PointerEntered += (sender, args) => { points[index].Fill = Brushes.White; pointHovered[index] = true; args.Handled = true; };
                points[index].PointerExited += (sender, args) => { points[index].Fill = Brushes.DeepSkyBlue; pointHovered[index] = false; args.Handled = true; };
                points[index].PointerWheelChanged += (sender, args) =>
                {
                    ShowInfo(args.GetPosition((sender as Visual)!.Parent as Visual).X,
                        args.GetPosition((sender as Visual)!.Parent as Visual).Y);
                    if(!pointHovered[index]) return;
                    var offset = args.Delta.Y * 0.2;
                    qs[index].Value  += offset;
                    args.Handled = true;
                };
        }

        var wrapPanel = new WrapPanel()
        {
            Orientation = Orientation.Horizontal,
            ItemSpacing = 40,
            LineSpacing = 40,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        var addButton = new Button()
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = Thickness.Parse("0 40 0 0"),
            Background = Brushes.Transparent,
            Content = new Icon()
            {
                Value = "mdi-plus"
            }
        };
        
        var bands = new Border()
        {
            
            Background = Brush.Parse("#232323"),
            CornerRadius = _cornerRadius,
            BoxShadow = _boxShadows,
            Child = new Border(){
                Padding = Thickness.Parse("15"),
                ClipToBounds = true,
            Child = new StackPanel()
            {
                Children =
                {
                    wrapPanel,
                    addButton
                }
            }
            }
        };
        
        
        
        var mainGain = new MuekValuer()
        {
            ValuerColor = Brushes.DeepSkyBlue,
            Layout = MuekValuer.LayoutEnum.Slider,
            Width = sliderWidth,
            Height = sliderHeight + knobRadius * 2,
            Minimum = minimum,
            Maximum = maximum,
            Margin = Thickness.Parse("10 0"),
            DefaultValue = 0,
        };
        
        var info = new Label();
        
        InitPoints();
        UpdateCurve();
        
        wrapPanel.Children.AddRange(bandParams);
        
        mainGain.ValueChanged += (sender, args) => { UpdateCurve(); };
        

        void ShowInfo(double x,double y)
        {
            info.IsVisible = false;
            for (int i = 0; i < points.Count; i++)
            {
                if(!pointHovered[i] && !pointPressed[i]) continue;
                info.Content =
                    $"Freq: {double.Exp(pointFreqs[i].Value * (double.Log(maximumFreq) - double.Log(minimumFreq)) / 20 + double.Log(minimumFreq)):F2}Hz, Gain: {pointLevels[i].Value:F2}dB, Q: {qs[i].LogValue:F2}";
                info.IsVisible = true;
            }
        }


        int[] lineArray = [10,20,30,40,50,60,70,80,90,
            100,200,300,400,500,600,700,800,900,
            1000,2000,3000,4000,5000,6000,7000,8000,9000,
            10000,20000,30000,
        ];

        List<Line> line = [];
        for (int i = 0; i < lineArray.Length; i++)
        {
            line.Add(new Line()
            {
                StartPoint = new Point(FreqMapping(lineArray[i]) * scale, 0),
                EndPoint = new Point(FreqMapping(lineArray[i]) * scale, height),
                Stroke = new SolidColorBrush(Colors.White,0.1),
                StrokeThickness = 0.5,
            });
        }

        var lineGrid = new Grid()
        {
        };
        lineGrid.Children.AddRange(line);

        var viewBorder = new Grid()
        {
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                // new Border()
                // {
                //     BorderBrush = Brushes.White,
                //     BorderThickness = Thickness.Parse("1")
                // },
                lineGrid,
                curve,
                info,
            }
        };
        
        viewBorder.Children.AddRange(points);
        
        viewBorder.PointerMoved += (sender, args) =>
        {
            ShowInfo(args.GetPosition(sender as Visual).X,
                args.GetPosition(sender as Visual).Y);
        };
        
        var view = new Border()
        {
            Background = Brush.Parse("#232323"),
            Padding = _thickness,
            CornerRadius = _cornerRadius,
            BoxShadow = _boxShadows,
            Child = new Border(){
                Background = Brush.Parse("#23000000"),
                CornerRadius = _cornerRadius,
                BoxShadow = _insetBoxShadow,
                Height = height + 20,
                Child = new Border()
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    ClipToBounds = true,
                    Height = height,
                    Child = viewBorder
                }
            },
            
        };

        addButton.Click += (sender, args) =>
        {
            AddNewPoint();
            wrapPanel.Children.Add(bandParams[^1]);
            viewBorder.Children.Add(points[^1]);
            args.Handled = true;
        };
        
        void UpdateCurve()
        {
            var curvePoints = Enumerable.Range((int)(FreqMapping(minimumFreq) * scale), 
                    (int)(FreqMapping(maximumFreq) * scale) - (int)(FreqMapping(minimumFreq) * scale))
                .Select(i => new Point(i, height/2d)).ToList();

            for (int i = 0; i < qs.Count; i++)
            {
                for (int p = 0; p < curvePoints.Count; p++)
                {
                    var omega = 
                        double.Exp((curvePoints[p].X / scale) * (double.Log(maximumFreq) - double.Log(minimumFreq)) / 20 + double.Log(minimumFreq)) /
                                double.Exp(pointFreqs[i].Value * (double.Log(maximumFreq) - double.Log(minimumFreq)) / 20 + double.Log(minimumFreq));
                    var q = qs[i].LogValue;
                    var a = double.Pow(10,pointLevels[i].Value / 40);
                    var gain = 20 * double.Log10(
                        double.Sqrt(
                            (double.Pow(1 - double.Pow(omega, 2), 2) + double.Pow(a * omega / q, 2)) /
                            (double.Pow(1 - double.Pow(omega, 2), 2) + double.Pow(omega / q / a, 2))
                        ));
                    var offset = gain / maximum / 2 * height;
                    curvePoints[p] = new Point(curvePoints[p].X, curvePoints[p].Y - offset);
                }
            }
            
            for (int i = 0; i < curvePoints.Count; i++)
            {
                curvePoints[i] = new Point(curvePoints[i].X, curvePoints[i].Y - mainGain.Value);
            }

            for (int i = 0; i < points.Count; i++)
                points[i].Margin = new Thickness(pointFreqs[i].Value * scale - radius / 2d,
                    -pointLevels[i].Value / maximum / 2 * height + height/2d - radius / 2d, 0, 0);
            curve.Points = curvePoints;
        }
        
        var misc = new Border()
        {
            Padding = Thickness.Parse("8"),
            Background = Brush.Parse("#232323"),
            CornerRadius = _cornerRadius,
            BoxShadow = _boxShadows,
            Child = new StackPanel()
            {
                Children =
                {
                    mainGain
                }
            }
        };
        var parameters = new Grid()
        {
            ColumnDefinitions = ColumnDefinitions.Parse("*,auto"),
            ColumnSpacing = 8,
            Children =
            {
                bands,
                misc,
            }
        };
        PluginContent.Child = new Grid()
        {
            RowDefinitions = RowDefinitions.Parse("*,auto"),
            RowSpacing = 8,
            Children =
            {
                view,
                parameters
            }
        };
        Grid.SetColumn(misc,1);
        Grid.SetRow(parameters, 1);
    }

    private void RemovePlugin(object? sender, RoutedEventArgs e)
    {
        var parent = Parent as StackPanel;
        parent?.Children.Remove(this);
    }
}