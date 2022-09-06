using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Core.Modules.HttpMethodValidation
{
    internal class HttpMethodValidationModuleInfo : HttpMethodValidationModuleBase
    {
        internal override void Log(IRequestContext requestContext, string message, string method) =>
            requestContext.Log.Info(message, method);
    }
}