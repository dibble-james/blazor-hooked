using System.Runtime.CompilerServices;

namespace BlazorHooked;

public static class LoaderReducer
{
    public static (LoaderState<T>? state, Dispatch dispatch) UseLoaderReducer<T>(this HookContext context, [CallerLineNumber] int caller = 0)
        => context.UseReducer(Reducer, new LoaderState<T>(true, default, null), caller);

    private static LoaderState<T> Reducer<T>(LoaderState<T> state, object action) => action switch
    {
        Loaded<T> loaded => state with { Loading = false, Result = loaded.Result },
        Failed<T> failed => state with { Loading = false, Failure = failed.Failure },
        _ => state
    };

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

public record Loaded<T>(T Result);

public record Failed<T>(Exception Failure);