namespace BlazorHooked;

using OneOf;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

public delegate void Effect();

public delegate Task AsyncEffect();

public delegate Action CallbackEffect();

public delegate Task<Func<Task>> AsyncCallbackEffect();

public partial class HookContext : IAsyncDisposable
{
    private readonly ConcurrentDictionary<int, object[]> effects = new();
    private readonly ConcurrentQueue<Func<Task>> effectQueue = new();
    private readonly ConcurrentDictionary<int, OneOf<Action, Func<Task>>> effectCallbacks = new();

    public void UseEffect(Effect effect, [CallerLineNumber] int caller = 0)
        => this.UseEffectInternal(effect, Array.Empty<object>(), caller);

    public void UseEffect(Effect effect, object[] dependencies, [CallerLineNumber] int caller = 0)
        => this.UseEffectInternal(effect, dependencies, caller);

    public void UseEffect(AsyncEffect effect, [CallerLineNumber] int caller = 0)
        => this.UseEffectInternal(effect, Array.Empty<object>(), caller);

    public void UseEffect(AsyncEffect effect, object[] dependencies, [CallerLineNumber] int caller = 0)
        => this.UseEffectInternal(effect, dependencies, caller);

    public void UseEffect(CallbackEffect effect, [CallerLineNumber] int caller = 0)
        => this.UseEffectInternal(effect, Array.Empty<object>(), caller);

    public void UseEffect(CallbackEffect effect, object[] dependencies, [CallerLineNumber] int caller = 0)
        => this.UseEffectInternal(effect, dependencies, caller);

    public void UseEffect(AsyncCallbackEffect effect, [CallerLineNumber] int caller = 0)
        => this.UseEffectInternal(effect, Array.Empty<object>(), caller);

    public void UseEffect(AsyncCallbackEffect effect, object[] dependencies, [CallerLineNumber] int caller = 0)
        => this.UseEffectInternal(effect, dependencies, caller);

    private void UseEffectInternal(OneOf<Effect, AsyncEffect, CallbackEffect, AsyncCallbackEffect> effect, object[] dependecies, int caller)
    {
        if (this.effects.TryAdd(caller, dependecies))
        {
            this.effectQueue.Enqueue(RunEffect(caller, effect));
            return;
        }

        if (this.effects.TryGetValue(caller, out var registeredDependencies))
        {
            // The dependencies have not been updated so no-op
            if (dependecies.Length == registeredDependencies.Length && dependecies.Zip(registeredDependencies, (a, b) => a.Equals(b)).All(x => x))
            {
                return;
            }

            if (this.effects.TryUpdate(caller, dependecies, registeredDependencies))
            {
                if (this.effectCallbacks.TryRemove(caller, out var callback))
                {
                    this.effectQueue.Enqueue(RunCallback(callback));
                }

                this.effectQueue.Enqueue(RunEffect(caller, effect));
            }
        }
    }

    internal async Task RunEffects()
    {
        while (this.effectQueue.TryDequeue(out var effect))
        {
            await effect();
        }
    }

    private Func<Task> RunEffect(int caller, OneOf<Effect, AsyncEffect, CallbackEffect, AsyncCallbackEffect> effect) => () =>
        effect.Match(e =>
            {
                e();
                this.effectCallbacks.TryRemove(caller, out var _);
                return Task.CompletedTask;
            },
            async e =>
            {
                await e();
                this.effectCallbacks.TryRemove(caller, out var _);
            },
            e =>
            {
                this.effectCallbacks.AddOrUpdate(caller, e(), (_, _) => e());
                return Task.CompletedTask;
            },
            async e =>
            {
                var cb = await e();
                this.effectCallbacks.AddOrUpdate(caller, cb, (_, _) => cb);
            });

    private Func<Task> RunCallback(OneOf<Action, Func<Task>> callback) => () =>
        callback.Match(e =>
            {
                e();
                return Task.CompletedTask;
            },
            func => func());

    public async ValueTask DisposeAsync()
    {
        foreach (var callback in this.effectCallbacks.Values)
        {
            await callback.Match(
                cb =>
                {
                    cb();
                    return Task.CompletedTask;
                },
                cb => cb());
        }
    }
}
