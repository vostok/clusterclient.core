using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;

namespace Vostok.Clusterclient.Core.Modules;

/// <summary>
/// Build configuration for <see cref="AdaptiveThrottlingModule"/>, create new instance of <see cref="AdaptiveThrottlingOptionsPerPriority"/>. 
/// </summary>
[PublicAPI]
public interface IAdaptiveThrottlingOptionsBuilder
{
    
    /// <summary>
    /// <para>Set an <see cref="AdaptiveThrottlingOptions"/> instance to default options per priority.</para>
    /// <para>See <see cref="AdaptiveThrottlingOptions"/> class documentation for details.</para>
    /// </summary>
    /// <param name="adaptiveThrottlingOptions"> will be used as the adaptive throttling parameters for all priorities.</param>
    public IAdaptiveThrottlingOptionsBuilder WithDefaultOptions(AdaptiveThrottlingOptions adaptiveThrottlingOptions);

    /// <summary>
    /// <para>Set an <see cref="AdaptiveThrottlingOptions"/> instance to adaptive throttling parameters for given priority.</para>
    /// <para>See <see cref="AdaptiveThrottlingOptions"/> class documentation for details.</para>
    /// </summary>
    /// <param name="priority">Priority name see <see cref="RequestPriority" /> documentation for details.</param>
    /// <param name="adaptiveThrottlingOptions">Adaptive throttling parameters for given priority.</param>
    public IAdaptiveThrottlingOptionsBuilder WithPriorityParameters(RequestPriority priority, AdaptiveThrottlingOptions adaptiveThrottlingOptions);
}