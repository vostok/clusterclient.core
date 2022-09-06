using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vostok.Clusterclient.Core.Model;

namespace Vostok.Clusterclient.Core.Modules.HttpMethodValidation
{
    internal abstract class HttpMethodValidationModuleBase : IRequestModule
    {
        internal static readonly HashSet<string> All = new HashSet<string>
        {
            RequestMethods.Get,
            RequestMethods.Post,
            RequestMethods.Put,
            RequestMethods.Head,
            RequestMethods.Patch,
            RequestMethods.Delete,
            RequestMethods.Options,
            RequestMethods.Trace
        };
        
        public Task<ClusterResult> ExecuteAsync(IRequestContext context, Func<IRequestContext, Task<ClusterResult>> next)
        {
            var method = context.Request.Method;

            if (All.Contains(method))
                return next(context);
            
            Log(context, "", method);

            return Task.FromResult(ClusterResult.IncorrectArguments(context.Request));
        }

        internal abstract void Log(IRequestContext requestContext, string message, string method);
    }
}