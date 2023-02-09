using Microsoft.JSInterop;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Subjects;

namespace Ptt.Blazor.Logic;

public class InteractionManager : IDisposable
{
    private readonly DotNetObjectReference<InteractionManager> self;
    private readonly IJSRuntime js;
    private Boolean isInstalled;

    public InteractionManager(IJSRuntime js)
	{
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

    Subject<Unit> onEscape = new Subject<Unit>();
    public IObservable<Unit> OnEscape => onEscape;

    async Task Install()
    {
        await js.InvokeVoidAsync("installGlobalInteractions", self);
    }

    [JSInvokable]
    public void HandleEscape() => onEscape.OnNext(default);

    public void Dispose()
    {
        self?.Dispose();
    }
}
