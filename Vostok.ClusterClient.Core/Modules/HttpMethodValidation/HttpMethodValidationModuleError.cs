using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Core.Modules.HttpMethodValidation
{
    internal class HttpMethodValidationModuleError : HttpMethodValidationModuleBase
    {
        internal override void Log(IRequestContext requestContext, string message, string method) =>
            requestContext.Log.Error(message, method);
    }
}