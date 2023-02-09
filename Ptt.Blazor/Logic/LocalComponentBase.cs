using Microsoft.AspNetCore.Components;

namespace Ptt.Blazor.Logic;

public class LocalComponentBase : ComponentBase, IDisposable
{
    IDisposable? subscription;

    [Inject]
    public InteractionManager InteractionManager { get; set; } = null!;

    protected void NotifyOnEscape(Action action)
    {
        subscription = InteractionManager.OnEscape.Subscribe(_ => action());
    }

    public virtual void Dispose()
    {
        subscription?.Dispose();
    }
}
