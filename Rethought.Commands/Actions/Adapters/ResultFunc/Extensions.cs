﻿using Rethought.Commands.Actions.Adapters.ResultFunc.AsyncResultFunc;

namespace Rethought.Commands.Actions.Adapters.ResultFunc
{
    public static class Extensions
    {
        public static IAsyncResultFunc<TContext> ToAsyncBlockingResultFunc<TContext>(this IResultFunc<TContext> func)
            => AsyncBlockingResultFunc<TContext>.Create(func);

        public static IAsyncResultFunc<TContext> ToAsyncBackgroundResultFunc<TContext>(this IResultFunc<TContext> func)
            => AsyncBackgroundResultFunc<TContext>.Create(func);
    }
}