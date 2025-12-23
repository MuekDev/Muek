using System;
using System.Collections.Generic;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Muek.Models;
using Muek.Services;
using Muek.Views;

namespace Muek.ViewModels;

public partial class PatternViewModel : ViewModelBase
{
    [ObservableProperty] private Color _color;
    [ObservableProperty] private string _name;
    [ObservableProperty] private List<PianoRoll.Note>[] _notes;
    [ObservableProperty] private IBrush _background;
    
    public IBrush Brush => new SolidColorBrush(Color);

    public PatternViewModel()
    {
        _color = DataStateService.MuekColor;
        _name = "New Pattern";
        _notes = [[],[],[],[],[],[],[],[],[],[],[],[],[],[],[],[]];
        _background = new SolidColorBrush(Colors.Black, 0);
    }
    
    [RelayCommand]
    public void SelectPattern()
    {
        var pianoRoll =  ViewHelper.GetMainWindow().PianoRollWindow.EditArea;
        var pianoBar = ViewHelper.GetMainWindow().PianoRollWindow.PianoBar;
        var button = ViewHelper.GetMainWindow().PianoRollWindow.PatternSelectButton;
        var color = ViewHelper.GetMainWindow().PianoRollWindow.PatternColor;
        pianoRoll.Pattern = this;
        pianoRoll.InvalidateVisual();
        pianoRoll.SaveNotes();
        pianoBar.Pattern = this;
        pianoBar.InvalidateVisual();
        button.Content = Name;
        color.Background = Brush;
        try
        {
            ViewHelper.GetMainWindow().PianoRollWindow.Channel.SelectedItem = 1;
        }
        catch (Exception e)
        {
            new DialogWindow().ShowError(e.Message);
        }
    }
    
    [RelayCommand]
    public void RemovePattern()
    {
        var pianoRoll =  ViewHelper.GetMainWindow().PianoRollWindow.EditArea;
        var pianoBar = ViewHelper.GetMainWindow().PianoRollWindow.PianoBar;
        var button = ViewHelper.GetMainWindow().PianoRollWindow.PatternSelectButton;
        var pattern = ViewHelper.GetMainWindow().PianoRollWindow.PatternSelection;
        if(pianoRoll.Pattern == this)
        {
            pianoRoll.Pattern = null;
            pianoRoll.InvalidateVisual();
            pianoRoll.SaveNotes();
            pianoBar.Pattern = null;
            pianoBar.InvalidateVisual();
            button.Content = "Pattern Undefined";
        }
        pattern.ViewModel.Patterns.Remove(this);
    }
    
    public void ShowRenameWindow()
    {
        var mainWindow = ViewHelper.GetMainWindow();
        var pianoRoll =  ViewHelper.GetMainWindow().PianoRollWindow.EditArea;
        var window = new RenameWindow();
        window.ShowDialog(mainWindow);
        window.NameBox.Text = Name;
        window.Submit += (sender, s) =>
        {
            Name = s;
            if (pianoRoll.Pattern == this)
                SelectPattern();
        };
    }
    
    public void ShowRecolorWindow()
    {
        var mainWindow = ViewHelper.GetMainWindow();
        var pianoRoll =  ViewHelper.GetMainWindow().PianoRollWindow.EditArea;
        var recolorWindow = new RecolorWindow();
        recolorWindow.MyColorView.SelectedColor = Color;
        recolorWindow.Show();
        recolorWindow.Submit += (sender, color) =>
        {
            Color = color;
            if (pianoRoll.Pattern == this)
                SelectPattern();
        };
    }
}