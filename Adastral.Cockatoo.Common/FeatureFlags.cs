using Adastral.Cockatoo.Common.Helpers;

namespace Adastral.Cockatoo.Common;

public static class FeatureFlags
{
    #region Parsing
    /// <inheritdoc cref="EnvironmentHelper.ParseBool"/>
    private static bool ParseBool(string environmentKey, bool defaultValue)
    {
        return EnvironmentHelper.ParseBool(environmentKey, defaultValue);
    }

    /// <inheritdoc cref="EnvironmentHelper.ParseString"/>
    private static string ParseString(string environmentKey, string defaultValue)
    {
        return EnvironmentHelper.ParseString(environmentKey, defaultValue);
    }
    #endregion

    public const string RunningInDockerName = "_COCKATOO_RUNNING_IN_DOCKER";
    public static bool RunningInDocker => ParseBool(RunningInDockerName, false);

    /// <summary>
    /// <para>Config Location</para>
    /// <para>Key: <c>COCKATOO_CONFIG</c></para>
    /// <para>Default Value: <c>./config/cockatoo.xml</c></para>
    /// </summary>
    /// <remarks>
    /// When running in docker, the default config location will actually be in <c>/config/cockatoo.xml</c>
    /// </remarks>
    public static string ConfigLocation => ParseString("COCKATOO_CONFIG", RunningInDocker ? "/config/cockatoo.xml" : "./config/cockatoo.xml");

    /// <summary>
    /// <para>Sentry DSN</para>
    /// <para>Key: <c>COCKATOO_SENTRY_DSN</c></para>
    /// </summary>
    public static string SentryDSN => ParseString("COCKATOO_SENTRY_DSN", "");
    
    
    private const string AspNetEnvironmentName = "ASPNET_ENVIRONMENT";
    private const string DotNetEnvironmentName = "DOTNET_ENVIRONMENT";
    
    /// <summary>
    /// <para><b>Key:</b> <c>ASPNET_ENVIRONMENT</c></para>
    /// </summary>
    public static string AspNetEnvironment => ParseString(AspNetEnvironmentName, "");
    /// <summary>
    /// <para><b>Key:</b> <c>DOTNET_ENVIRONMENT</c></para>
    /// </summary>
    public static string DotNetEnvironment => ParseString(DotNetEnvironmentName, "");
    
    /// <summary>
    /// Check if either <see cref="AspNetEnvironment"/> or <see cref="DotNetEnvironment"/>
    /// equals <c>DEVELOPMENT</c> (case-insensitive)
    /// </summary>
    public static bool IsDevelopmentEnvironment
        => AspNetEnvironment.Trim().Equals("DEVELOPMENT", StringComparison.InvariantCultureIgnoreCase)
        || DotNetEnvironment.Trim().Equals("DEVELOPMENT", StringComparison.InvariantCultureIgnoreCase);
    public static void SetEnvironment(string value)
    {
        Environment.SetEnvironmentVariable(AspNetEnvironmentName, value);
        Environment.SetEnvironmentVariable(DotNetEnvironmentName, value);
    }
    
    /// <summary>
    /// <para>Make sure that <see cref="AspNetEnvironment"/> and <see cref="DotNetEnvironment"/>
    /// equal to whatever one is set, when the other isn't set.</para>
    ///
    /// When <see cref="AspNetEnvironment"/> is set, and <see cref="DotNetEnvironment"/> isn't set, then this will set the value for <see cref="DotNetEnvironment"/> to be equal to <see cref="AspNetEnvironment"/>.
    /// Same thing is done, but with the environment variables swapped.
    /// </summary>
    public static void EnsureEnvironmentValue()
    {
        const string prefix = $"[{nameof(FeatureFlags)}.{nameof(EnsureEnvironmentValue)}]";
        if (string.IsNullOrEmpty(DotNetEnvironment) &&
            !string.IsNullOrEmpty(AspNetEnvironment))
        {
            System.Diagnostics.Trace.WriteLine($"{prefix} Updated {DotNetEnvironmentName} to match {AspNetEnvironmentName} ({AspNetEnvironment})");
            SetEnvironment(AspNetEnvironment);
        }
        else if (!string.IsNullOrEmpty(DotNetEnvironment) &&
                 string.IsNullOrEmpty(AspNetEnvironment))
        {
            System.Diagnostics.Trace.WriteLine($"{prefix} Updated {AspNetEnvironmentName} to match {DotNetEnvironmentName} ({DotNetEnvironment})");
            SetEnvironment(DotNetEnvironment);
        }
        else if (string.IsNullOrEmpty(DotNetEnvironment) && string.IsNullOrEmpty(AspNetEnvironment))
        {
            System.Diagnostics.Trace.WriteLine($"{prefix} {AspNetEnvironmentName} and {DotNetEnvironmentName} aren't set.");
        }
        else if (!string.IsNullOrEmpty(DotNetEnvironment) && !string.IsNullOrEmpty(AspNetEnvironment) &&
                 !DotNetEnvironment.Equals(AspNetEnvironment, StringComparison.InvariantCultureIgnoreCase))
        {
            System.Diagnostics.Trace.WriteLine(string.Join(Environment.NewLine,
                $"{prefix} {AspNetEnvironmentName} and {DotNetEnvironmentName} are set to different values!!!",
                $"{AspNetEnvironmentName}: {AspNetEnvironment}",
                $"{DotNetEnvironmentName}: {DotNetEnvironment}"));
        }
    }
}