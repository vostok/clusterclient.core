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
    /// <para>Set a <see cref="AdaptiveThrottlingOptions"/> instance to default options per priority.</para>
    /// <para>See <see cref="AdaptiveThrottlingOptions"/> class documentation for details.</para>
    /// </summary>
    /// <param name="adaptiveThrottlingOptions">Adaptive throttling parameters which set to default value.</param>
    public IAdaptiveThrottlingOptionsBuilder WithDefaultOptions(AdaptiveThrottlingOptions adaptiveThrottlingOptions);

    /// <summary>
    /// <para>Produces a new <see cref="AdaptiveThrottlingOptions"/> instance where adaptive throttling parameters by priority will have given value.</para>
    /// <para>See <see cref="AdaptiveThrottlingOptions"/> class documentation for details.</para>
    /// </summary>
    /// <param name="priority">Priority name <see cref="RequestPriority" /> for details</param>
    /// <param name="adaptiveThrottlingOptions">Adaptive throttling parameters by priority.</param>
    /// <returns>A new <see cref="AdaptiveThrottlingOptions"/> object with updated throttling parameters for given priority.</returns>
    public IAdaptiveThrottlingOptionsBuilder WithPriorityParameters(RequestPriority priority, AdaptiveThrottlingOptions adaptiveThrottlingOptions);
}