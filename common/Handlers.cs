using System;

namespace Mimir.Common;

/// <summary>Raises an event one handler at a time; a failing handler is logged and the rest still run.</summary>
internal static class Handlers
{
    internal static void Invoke<T1>(Action<T1> handlers, string name, T1 a)
    {
        if (handlers == null)
        {
            return;
        }

        foreach (Delegate handler in handlers.GetInvocationList())
        {
            try
            {
                ((Action<T1>)handler)(a);
            }
            catch (Exception e)
            {
                Failed(name, e);
            }
        }
    }

    internal static void Invoke<T1, T2>(Action<T1, T2> handlers, string name, T1 a, T2 b)
    {
        if (handlers == null)
        {
            return;
        }

        foreach (Delegate handler in handlers.GetInvocationList())
        {
            try
            {
                ((Action<T1, T2>)handler)(a, b);
            }
            catch (Exception e)
            {
                Failed(name, e);
            }
        }
    }

    internal static void Invoke<T1, T2, T3>(Action<T1, T2, T3> handlers, string name, T1 a, T2 b, T3 c)
    {
        if (handlers == null)
        {
            return;
        }

        foreach (Delegate handler in handlers.GetInvocationList())
        {
            try
            {
                ((Action<T1, T2, T3>)handler)(a, b, c);
            }
            catch (Exception e)
            {
                Failed(name, e);
            }
        }
    }

    internal static void Invoke<T1, T2, T3, T4>(Action<T1, T2, T3, T4> handlers, string name, T1 a, T2 b, T3 c, T4 d)
    {
        if (handlers == null)
        {
            return;
        }

        foreach (Delegate handler in handlers.GetInvocationList())
        {
            try
            {
                ((Action<T1, T2, T3, T4>)handler)(a, b, c, d);
            }
            catch (Exception e)
            {
                Failed(name, e);
            }
        }
    }

    private static void Failed(string name, Exception e)
    {
        Log.LogError($"A handler for {name} failed: {e}");
    }
}
