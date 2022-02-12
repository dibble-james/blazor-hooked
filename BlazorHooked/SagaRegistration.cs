namespace BlazorHooked;

public delegate Task Saga<TTrigger>(TTrigger action, Dispatch dispatch);

public static class SagaRegistration
{
    public static (T? state, Dispatch dispatch) Subscribe<T, TTrigger>(this (T? State, Dispatch Dispatch) reducer, Saga<TTrigger> saga)
        => (reducer.State, WrapDispatch(reducer.Dispatch, saga));

    private static Dispatch WrapDispatch<TTrigger>(Dispatch dispatch, Saga<TTrigger> saga) => action =>
    {
        dispatch(action);

        if (action is TTrigger trigger)
        {
            Task.Run(async () => await saga(trigger, dispatch));
        }
    };
}
