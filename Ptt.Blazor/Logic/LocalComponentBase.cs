using Microsoft.AspNetCore.Components;

namespace Ptt.Blazor.Logic;

public class LocalComponentBase : ComponentBase, IDisposable
{
    IDisposable? eventSubscription;

    IDisposable? subscription;

    [Inject]
    public InteractionManager InteractionManager { get; set; } = null!;

    protected void SetUpdateSubscription(Object? target)
    {
        eventSubscription?.Dispose();
        if (target is null) return;
        eventSubscription = InteractionManager.GetExpressionObservable(target).Subscribe(_ => StateHasChanged());
    }

    protected void NotifyOnEvents(Action action)
    {
        subscription = InteractionManager.OnInteraction.Subscribe(_ => action());
    }

    public virtual void Dispose()
    {
        eventSubscription?.Dispose();
        subscription?.Dispose();
    }
}
