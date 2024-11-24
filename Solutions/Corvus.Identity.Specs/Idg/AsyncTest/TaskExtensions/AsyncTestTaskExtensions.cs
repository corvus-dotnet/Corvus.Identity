// <copyright file="AsyncTestTaskExtensions.cs" company="IDG">
// Copyright (c) IDG. All rights reserved.
// </copyright>

namespace Idg.AsyncTest.TaskExtensions
{
    using System;
    using System.Threading.Tasks;

    public static class AsyncTestTaskExtensions
    {
        /// <summary>
        /// Produces a task that completes without errors when the input task completes, even
        /// if the input task faults or is canceled.
        /// </summary>
        /// <param name="t">The task to wait for.</param>
        /// <returns>
        /// A task that completes when the input tasks completes, but which will always complete
        /// successfully.
        /// </returns>
        /// <remarks>
        /// This is useful if a test needs to wait for a task to complete in scenarios where
        /// exceptions are expected.
        /// </remarks>
        public static Task WhenCompleteIgnoringErrors(this Task t)
        {
            return t.ContinueWith(
                ot =>
                {
                    if (ot.IsFaulted)
                    {
                        // Observe the exception to avoid deferred reports of
                        // unhandled exceptions.
                        GC.KeepAlive(ot.Exception);
                    }
                },
                TaskContinuationOptions.ExecuteSynchronously);
        }

        /// <summary>
        /// Produces a task that completes when the input task completes, unless the specified
        /// timeout elapses, in which case it faults with a <see cref="TimeoutException"/>.
        /// </summary>
        /// <param name="t">The task to wait for.</param>
        /// <param name="timeout">The maximum time to wait before timing out.</param>
        /// <returns>
        /// A task that represents the outcome of the input task, unless the input task did not
        /// complete within the specified time, in which case the returned task faults with
        /// <see cref="TimeoutException"/>.
        /// </returns>
        public static async Task WithTimeout(this Task t, TimeSpan timeout)
        {
            await Task.WhenAny(t, Task.Delay(timeout));

            // Attempt to work around an unhelpful characteristic of the xunit test runner,
            // which attempts to limit the degree of concurrency for parallel test runs by
            // creating a customs synchronization context with a limited number of threads,
            // and running all tests through a TPL TaskScheduler bound to that context.
            // Unfortunately, it makes no attempt to throttle the number of tests queued
            // up - it just dumps the entire test set into the TPL's lap as fast as it can.
            // The problem with this is that it tends to result in thread pool starvation.
            // If all tests are synchronous that has the desired effect, but as soon as you
            // have any tests that use 'await' and cannot (or at any rate, do not) use
            // ConfigureAwait, continuations end up sitting behind all the tests already
            // queued up (because unlike the standard TPL thread pool, which uses a LIFO
            // queue to avoid this, and related problems) xunit's custom synchronization
            // context uses a FIFO queue. The result is typically that by the time their
            // thread pool actually gets to the work needed to complete the test, we've
            // already timed out here.
            for (int i = 0; !t.IsCompleted && i < 100; ++i)
            {
                await Task.Yield();
            }

            if (!t.IsCompleted)
            {
                throw new TimeoutException();
            }

            await t.ConfigureAwait(false);
        }

        /// <summary>
        /// Produces a task that completes when the input task completes, unless two seconds
        /// pass, in which case it faults with a <see cref="TimeoutException"/>.
        /// </summary>
        /// <param name="t">The task to wait for.</param>
        /// <returns>
        /// A task that represents the outcome of the input task, unless the input task did not
        /// complete within the specified time, in which case the returned task faults with
        /// <see cref="TimeoutException"/>.
        /// </returns>
        public static Task WithTimeout(this Task t) => t.WithTimeout(TimeSpan.FromSeconds(2));

        /// <summary>
        /// Produces a task that completes when the input task completes, unless the specified
        /// timeout elapses, in which case it faults with a <see cref="TimeoutException"/>.
        /// </summary>
        /// <param name="t">The task to wait for.</param>
        /// <param name="timeout">The maximum time to wait before timing out.</param>
        /// <typeparam name="TResult">The type of the result produced by the input task.</typeparam>
        /// <returns>
        /// A task that represents the outcome of the input task, unless the input task did not
        /// complete within the specified time, in which case the returned task faults with
        /// <see cref="TimeoutException"/>.
        /// </returns>
        public static async Task<TResult?> WithTimeout<TResult>(this Task<TResult?> t, TimeSpan timeout)
        {
            Task asTaskWithNoResult = t;
            await asTaskWithNoResult.WithTimeout(timeout);
            return t.Result;
        }

        /// <summary>
        /// Produces a task that completes when the input task completes, unless two seconds
        /// pass, in which case it faults with a <see cref="TimeoutException"/>.
        /// </summary>
        /// <param name="t">The task to wait for.</param>
        /// <typeparam name="TResult">The type of the result produced by the input task.</typeparam>
        /// <returns>
        /// A task that represents the outcome of the input task, unless the input task did not
        /// complete within the specified time, in which case the returned task faults with
        /// <see cref="TimeoutException"/>.
        /// </returns>
        public static Task<TResult?> WithTimeout<TResult>(this Task<TResult?> t) => t.WithTimeout(TimeSpan.FromSeconds(2));
    }
}