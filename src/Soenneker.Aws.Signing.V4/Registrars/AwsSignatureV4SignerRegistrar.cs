using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Aws.Signing.V4.Abstract;

namespace Soenneker.Aws.Signing.V4.Registrars;

/// <summary>
/// A dependency-free .NET implementation of AWS Signature Version 4 request signing.
/// </summary>
public static class AwsSignatureV4SignerRegistrar
{
    /// <summary>
    /// Adds <see cref="IAwsSignatureV4Signer"/> as a singleton service. <para/>
    /// </summary>
    /// <param name="services">Service collection that receives the registration.</param>
    /// <returns>The same service collection, so additional registrations can be chained.</returns>
    public static IServiceCollection AddAwsSignatureV4SignerAsSingleton(this IServiceCollection services)
    {
        services.TryAddSingleton<IAwsSignatureV4Signer, AwsSignatureV4Signer>();

        return services;
    }

    /// <summary>
    /// Adds <see cref="IAwsSignatureV4Signer"/> as a scoped service. <para/>
    /// </summary>
    /// <param name="services">Service collection that receives the registration.</param>
    /// <returns>The same service collection, so additional registrations can be chained.</returns>
    public static IServiceCollection AddAwsSignatureV4SignerAsScoped(this IServiceCollection services)
    {
        services.TryAddScoped<IAwsSignatureV4Signer, AwsSignatureV4Signer>();

        return services;
    }
}
