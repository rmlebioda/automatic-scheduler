namespace PriceCheckWebScrapper.Core;

public static class TaskRepeater
{
    public static void Repeat(Action task, int maxAttempts, Action<int, Exception> onExceptionAction)
    {
        for (var i = 0; i < maxAttempts; i++)
            try
            {
                task();
            }
            catch (Exception e)
            {
                onExceptionAction(i + 1, e);
                if (i == maxAttempts - 1)
                    throw;
            }
    }
    
    public static T Repeat<T>(Func<T> task, int maxAttempts, Action<int, Exception> onExceptionAction)
    {
        for (var i = 0; i < maxAttempts; i++)
            try
            {
                return task();
            }
            catch (Exception e)
            {
                onExceptionAction(i + 1, e);
                if (i == maxAttempts - 1)
                    throw;
            }

        // it should never reach this code
        throw new ApplicationException();
    }

    public static async Task RepeatAsync(Func<Task> task, int maxAttempts, Action<int, Exception> onExceptionAction)
    {
        for (var i = 0; i < maxAttempts; i++)
            try
            {
                await task();
                return;
            }
            catch (Exception e)
            {
                onExceptionAction(i + 1, e);
                if (i == maxAttempts - 1)
                    throw;
            }
    }
}