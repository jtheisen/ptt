using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Subjects;

namespace Ptt.Blazor.Logic;

public record InteractionEvent;

public record EscapeInteractionEvent : InteractionEvent;
public record SpaceInteractionEvent : InteractionEvent;

public class InteractionManager : IDisposable, IComponentNotifier
{
    private readonly DotNetObjectReference<InteractionManager> self;
    private readonly IJSRuntime js;
    private Boolean isInstalled;

    private Dictionary<Object, Subject<Unit>> listeners;

    UiReasoningState reasoningState;

    public UiReasoningState ReasoningState => reasoningState;

    public InteractionManager(IJSRuntime js)
	{
        this.reasoningState = new UiReasoningState(this);
        this.listeners = new Dictionary<Object, Subject<Unit>>();
        this.self = DotNetObjectReference.Create(this);
        this.js = js;
    }

    public async Task EnsureInstall()
    {
        if (!isInstalled)
        {
            await Install();

            isInstalled = true;
        }
    }

    Subject<InteractionEvent> onInteraction = new Subject<InteractionEvent>();
    public IObservable<InteractionEvent> OnInteraction => onInteraction;

    public IObservable<Unit> GetExpressionObservable(Object target)
    {
        if (!listeners.TryGetValue(target, out var subject))
        {
            listeners[target] = subject = new Subject<Unit>();
        }

        return subject;
    }

    async Task Install()
    {
        await js.InvokeVoidAsync("installGlobalInteractions", self);
    }

    [JSInvokable]
    public void HandleEscape() => reasoningState.HandleEscape();

    [JSInvokable]
    public void HandleSpace() => reasoningState.Cascade(true);

    public void Dispose()
    {
        self?.Dispose();
    }

    public void Notify(Object target)
    {
        if (listeners.TryGetValue(target, out var subject))
        {
            subject.OnNext(default);
        }
    }
}
