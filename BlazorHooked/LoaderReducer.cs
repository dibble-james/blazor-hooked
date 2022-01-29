using System.Runtime.CompilerServices;

namespace BlazorHooked;

public static class LoaderReducer
{
    public static (LoaderState<T>? state, Dispatch dispatch) UseLoaderReducer<T>(this HookContext context, [CallerLineNumber] int caller = 0)
        => context.UseReducer(Reducer, new LoaderState<T>(true, default, null), caller);

    private static LoaderState<T> Reducer<T>(LoaderState<T> state, object action) => action switch
    {
        LoaderActions.Load => state with { Loading = true, Result = default, Failure = null },
        LoaderActions.Loaded<T> loaded => state with { Loading = false, Result = loaded.Result },
        LoaderActions.Failed failed => state with { Loading = false, Failure = failed.Failure },
        _ => state
    };

}

public static class LoaderActions
{
    public record Load();

    public record Loaded<T>(T Result);

    public record Failed(Exception Failure);
}

public record LoaderState<T>(bool Loading, T? Result, Exception? Failure)
{
    public bool TryGetResult(out T? result)
    {
        result = default;

        if (Loading || Failure is not null)
        {
            return false;
        }

        result = Result;
        return true;
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