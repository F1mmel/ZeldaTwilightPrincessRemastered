using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public static class EnumeratorHelper
{
    public static void Create(float delay, Action action)
    {
        ZeldaManager.Instance.StartCoroutine(ExecuteAfterDelay(delay, action));
    }

    private static IEnumerator ExecuteAfterDelay(float delay, Action action)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }
    
    public static async Task CreateAsync(float delay, Action action)
    {
        await ExecuteAfterDelayAsync(delay, action);
    }

    private static async Task ExecuteAfterDelayAsync(float delay, Action action)
    {
        var taskCompletionSource = new TaskCompletionSource<bool>();

        // Start Coroutine
        ZeldaManager.Instance.StartCoroutine(WaitAndComplete(taskCompletionSource, delay, action));

        // Wait for the Coroutine to complete
        await taskCompletionSource.Task;
    }

    private static IEnumerator WaitAndComplete(TaskCompletionSource<bool> tcs, float delay, Action action)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();

        // Signal the Task that the Coroutine is done
        tcs.SetResult(true);
    }
}