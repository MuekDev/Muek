using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;

namespace Muek.ViewModels;

public partial class PatternSelectionViewModel : ViewModelBase
{
    public ObservableCollection<PatternViewModel> Patterns { get; set; } = [];

    [RelayCommand]
    public void AddPattern()
    {
        Patterns.Add(new PatternViewModel());
    }

    public void AddPattern(PatternViewModel pattern)
    {
        Patterns.Add(pattern);
    }
}