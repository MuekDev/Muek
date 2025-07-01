using System.Threading.Tasks;

namespace Muek.Commands;

public interface IRpcCommand
{
    Task Execute();
}