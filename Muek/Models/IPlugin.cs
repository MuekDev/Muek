namespace Muek.Models;

public interface IPlugin
{
    void ShowEditor();
    void CloseEditor();
    string GetPluginName();
}