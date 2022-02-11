namespace BlazorHooked;

using System.Runtime.CompilerServices;
using BlazorHooked.Sagas;

public static class LoaderReducer
{
    public static (LoaderState<T>? state, Dispatch dispatch) UseLoaderReducer<T>(this HookContext context, Func<Task<T>> loader, [CallerLineNumber] int caller = 0)
        => context.UseReducer(Reducer, new LoaderState<T>(false, false, default, null), caller).WithSaga(Saga(loader));

    private static LoaderState<T> Reducer<T>(LoaderState<T> state, object action) => action switch
    {
        LoaderActions.Load<T> => state with { Loading = true, Result = default, Failure = null, Loaded = false },
        LoaderActions.Loaded<T> loaded => state with { Loading = false, Loaded = true, Result = loaded.Result },
        LoaderActions.Failed failed => state with { Loading = false, Failure = failed.Failure },
        _ => state
    };

    private static Saga<LoaderActions.Load<T>> Saga<T>(Func<Task<T>> loader) => async (action, dispatch) =>
    {
        try
        {
            var result = await loader();

            dispatch(new LoaderActions.Loaded<T>(result));
        }
        catch (Exception ex)
        {
            dispatch(new LoaderActions.Failed(ex));
        }
    };
}

public static class LoaderActions
{
    public record Load<T>();

    public record Loaded<T>(T Result);

    public record Failed(Exception Failure);
}

public record LoaderState<T>(bool Loading, bool Loaded, T? Result, Exception? Failure)
{
    public bool TryGetResult(out T? result)
    {
        result = Result;

        return Loaded;
    }

    public bool TryGetFailure(out Exception? failure)
    {
        failure = default;

        if (Loading || Result is not null)
        {
            return false;
        }

        failure = Failure;
        return true;
    }
}