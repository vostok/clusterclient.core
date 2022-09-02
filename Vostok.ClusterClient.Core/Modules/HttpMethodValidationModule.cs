using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vostok.Clusterclient.Core.Model;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Core.Modules
{
    internal class HttpMethodValidationModule : IRequestModule
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
        
        private readonly Func<IRequestContext, Action<string, string>> logFunctionProvider;

        public HttpMethodValidationModule() : this(LogLevel.Error)
        {
            
        }

        public HttpMethodValidationModule(LogLevel logLevel)
        {
            logFunctionProvider = GetLogFunction(logLevel);
        }

        public Task<ClusterResult> ExecuteAsync(IRequestContext context, Func<IRequestContext, Task<ClusterResult>> next)
        {
            var method = context.Request.Method;

            if (All.Contains(method))
                return next(context);

            var logFunction = logFunctionProvider(context);
            logFunction("Request HTTP method '{Method}' is not valid.", method);
            
            return Task.FromResult(ClusterResult.IncorrectArguments(context.Request));
        }
        
        private Func<IRequestContext, Action<string, string>> GetLogFunction(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Debug => r => r.Log.Debug,
                LogLevel.Info => r => r.Log.Info,
                LogLevel.Warn => r => r.Log.Warn,
                LogLevel.Error => r => r.Log.Error,
                LogLevel.Fatal => r => r.Log.Fatal,
                _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null)
            };
        }
    }
}