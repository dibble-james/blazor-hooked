namespace BlazorHooked;

public delegate Action DispatchAction(object action);

public delegate T ReducerExecutor<T>(T state);

public delegate T ReducerCombiner<T, TSubState>(T state, ReducerExecutor<TSubState> reducer);

public static class CombinedReducer
{
    public static Reducer<T> CombineReducer<T, TSubState>(
        this Reducer<T> parentReducer, ReducerCombiner<T, TSubState> map, Reducer<TSubState> reducer) => (state, action) =>
        map(parentReducer(state, action), subState => reducer(subState, action));
}
