using CSharpFunctionalExtensions;
using System.Reflection;

namespace Adastral.Cockatoo.Common.AspNet;

public class ApplicationInfo : IApplicationInfo
{
    /// <summary>
    /// Name of the assembly (e.g: "Adastral.Cockatoo.WebApi")
    /// </summary>
    public required string AssemblyName { get; set; }

    /// <summary>
    /// Display Name of the application (e.g: "Cockatoo" or "Echelon")
    /// </summary>
    public required string DisplayName { get; set; }

    /// <summary>
    /// Version of the assembly.
    /// </summary>
    public Maybe<Version> Version { get; set; }

    /// <summary>
    /// Entry assembly for the application
    /// </summary>
    /// <remarks>
    /// <see cref="Version"/> is updated to the value of <see cref="AssemblyName.Version"/>
    /// when <see cref="AssemblyName.Version"/> and <see cref="Assembly.GetName()"/> is not null.
    /// </remarks>
    public required Assembly Assembly
    {
        get;
        set
        {
            field = value;
            var asmName = value.GetName();
            if (asmName != null)
            {
                Version = asmName.Version ?? Maybe<Version>.None;
            }
        }
    }

    /// <summary>
    /// When some, this will override output value of <see cref="FormatVersion"/>
    /// when <see cref="Version"/> is <see cref="Maybe{Version}.None"/>
    /// </summary>
    public Maybe<ApplicationInfoFormatVersionNoneFactory> FormatVersionNoneFactory { get; set; }

    /// <summary>
    /// Format the <see cref="Version"/> into a string.
    /// </summary>
    /// <returns>
    /// Possible values:
    /// <list type="bullet">
    /// <item><c>0</c> when <see cref="Version"/> is <see cref="Maybe{Version}.None"/></item>
    /// <item>Result from <see cref="Version.ToString()"/> when <see cref="Version.Major"/> is less than <c>1000</c></item>
    /// <item>Otherwise, it's formatted like <c>major.minor.build.revision</c> with trailing <c>.0</c> removed.
    /// Along with that, all parts of the version have padding applied to the left of them with the width of <c>2</c> and the value of <c>0</c>
    /// (via <c>.PadLeft(2, '0')</c>)</item>
    /// </list>
    /// </returns>
    public string FormatVersion()
    {
        return Version.Match(version =>
        {
            if (version.Major < 1000) return version.ToString();

            // format nicely if major is >= 1000
            // like "2025.10.01" ()
            var values = new[]
            {
                version.Major.ToString(),
                version.Minor.ToString(),
                version.Build.ToString(),
                version.Revision.ToString(),
            };
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = values[i].PadLeft(2, '0');
            }
            var result = string.Join('.', values);
            while (result.EndsWith(".00"))
            {
                result = result[..^3];
            }
            if (result == "00") return "0";
            return result;
        }, () => FormatVersionNoneFactory.Match(factory => factory(this), () => "0"));

    }
}

public interface IApplicationInfo
{
    public string AssemblyName { get; }
    public string DisplayName { get; }
    public Maybe<Version> Version { get; }
    public string FormatVersion();
}

public delegate string ApplicationInfoFormatVersionNoneFactory(ApplicationInfo caller);