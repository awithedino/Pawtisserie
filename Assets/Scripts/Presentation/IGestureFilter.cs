using Paw.Sim;

namespace Paw.Presentation
{
    /// <summary>Optionally restricts cards and targets for guided input.</summary>
    public interface IGestureFilter
    {
        bool AllowsCard(int handIndex);
        bool AllowsTarget(int handIndex, Cell target);
    }
}
