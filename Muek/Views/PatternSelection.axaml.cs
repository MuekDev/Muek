using Avalonia.Controls;
using Muek.ViewModels;

namespace Muek.Views;

public partial class PatternSelection : UserControl
{
    public PatternSelectionViewModel ViewModel => (DataContext as PatternSelectionViewModel)!;
    public PatternSelection()
    {
        InitializeComponent();
        DataContext = new PatternSelectionViewModel();
    }
}