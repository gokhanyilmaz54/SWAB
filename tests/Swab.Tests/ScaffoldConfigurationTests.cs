using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Swab.Tests;

// T-001 kabul kriterleri: kalite ayarları, CI YAML ve onaylı paket listesi dosya düzeyinde doğrulanır.
public class ScaffoldConfigurationTests
{
    // docs/PLAN.md bölüm 3'teki onaylı uygulama paketleri.
    private static readonly string[] ApprovedAppPackages =
    [
        "Dapper",
        "Microsoft.Data.SqlClient",
        "Serilog.AspNetCore",
        "MailKit",
        "QRCoder",
    ];

    // docs/PLAN.md bölüm 3'teki onaylı test paketleri; "xunit" ailesi ayrıca prefix ile kabul edilir.
    private static readonly string[] ApprovedTestPackages =
    [
        "Microsoft.NET.Test.Sdk",
        "Microsoft.AspNetCore.Mvc.Testing",
    ];

    [Fact]
    public void DirectoryBuildProps_EnforcesQualitySettings()
    {
        var root = XDocument.Load(RepoPaths.Of("Directory.Build.props")).Root!;
        var values = root.Descendants().ToDictionary(e => e.Name.LocalName, e => e.Value.Trim());

        Assert.Equal("enable", values["Nullable"]);
        Assert.Equal("true", values["TreatWarningsAsErrors"]);
        Assert.Equal("true", values["NuGetAudit"]);
        Assert.Equal("true", values["RestorePackagesWithLockFile"]);
    }

    [Theory]
    [InlineData("src/Swab.Web/Swab.Web.csproj")]
    [InlineData("tests/Swab.Tests/Swab.Tests.csproj")]
    public void ProjectFiles_DoNotWeakenQualitySettings(string path)
    {
        var text = File.ReadAllText(RepoPaths.Of(path));

        Assert.DoesNotContain("<TreatWarningsAsErrors>false", text);
        Assert.DoesNotContain("<NuGetAudit>false", text);
        Assert.DoesNotContain("<NoWarn>", text);
        Assert.DoesNotContain("<Nullable>disable", text);
    }

    [Fact]
    public void GlobalJson_PinsSdkWithLatestFeatureRollForward()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(RepoPaths.Of("global.json")));
        var sdk = doc.RootElement.GetProperty("sdk");

        Assert.Equal("10.0.100", sdk.GetProperty("version").GetString());
        Assert.Equal("latestFeature", sdk.GetProperty("rollForward").GetString());
    }

    [Theory]
    [InlineData("src/Swab.Web/packages.lock.json")]
    [InlineData("tests/Swab.Tests/packages.lock.json")]
    public void LockFiles_AreCommitted(string path)
    {
        Assert.True(File.Exists(RepoPaths.Of(path)), $"Kilit dosyası eksik: {path}");
    }

    [Fact]
    public void CiWorkflow_BuildsWithWarningsAsErrorsAndRunsTests()
    {
        var ci = File.ReadAllText(RepoPaths.Of(".github/workflows/ci.yml"));

        Assert.Contains("dotnet build --no-restore -warnaserror", ci);
        Assert.Contains("dotnet test --no-build", ci);
        Assert.Contains("dotnet list package --vulnerable", ci);
    }

    [Fact]
    public void CiWorkflow_ReferencesNoSecretValues()
    {
        var ci = File.ReadAllText(RepoPaths.Of(".github/workflows/ci.yml"));

        // Depoda tanımlı secret ifadesi olmamalı; parola yalnızca çalışma anında üretilmeli.
        Assert.DoesNotContain("secrets.", ci);
        Assert.Contains("openssl rand", ci);
        Assert.Contains("::add-mask::", ci);

        // "password=" veya "PASSWORD=" gibi bir atamanın değeri sabit metin olmamalı (ortam değişkeni başvurusu hariç).
        var hardcoded = Regex.Matches(ci, @"(?i)password\s*=\s*(?!\$)[^\s""';$]");
        Assert.Empty(hardcoded);
    }

    [Fact]
    public void CiWorkflow_UsesReadOnlyPermissionsAndNoPullRequestTarget()
    {
        var ci = File.ReadAllText(RepoPaths.Of(".github/workflows/ci.yml"));

        Assert.Matches(new Regex(@"(?m)^permissions:\s*\r?\n\s+contents:\s*read\s*$"), ci);
        Assert.DoesNotContain("pull_request_target", ci);
    }

    [Fact]
    public void CiWorkflow_ThirdPartyActionsArePinnedToCommitSha()
    {
        var ci = File.ReadAllText(RepoPaths.Of(".github/workflows/ci.yml"));

        var usesLines = Regex.Matches(ci, @"(?m)^\s*-?\s*uses:\s*(\S+)")
            .Select(m => m.Groups[1].Value)
            .Where(u => !u.StartsWith("./", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(usesLines);
        foreach (var action in usesLines)
        {
            Assert.Matches(new Regex(@"@[0-9a-f]{40}$"), action);
        }
    }

    [Theory]
    [InlineData("src/Swab.Web/packages.lock.json", false)]
    [InlineData("tests/Swab.Tests/packages.lock.json", true)]
    public void LockFiles_DirectPackagesAreApproved(string path, bool testProject)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(RepoPaths.Of(path)));

        foreach (var framework in doc.RootElement.GetProperty("dependencies").EnumerateObject())
        {
            foreach (var package in framework.Value.EnumerateObject())
            {
                if (package.Value.GetProperty("type").GetString() == "Direct")
                {
                    Assert.True(IsApproved(package.Name, testProject), $"Onaylanmamış paket: {package.Name}");
                }
            }
        }
    }

    [Fact]
    public void ProjectFiles_PackageReferencesAreApproved()
    {
        var projects = Directory.EnumerateFiles(RepoPaths.Of("src"), "*.csproj", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(RepoPaths.Of("tests"), "*.csproj", SearchOption.AllDirectories))
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                        !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .ToList();

        Assert.NotEmpty(projects);
        foreach (var project in projects)
        {
            var testProject = project.Contains($"{Path.DirectorySeparatorChar}tests{Path.DirectorySeparatorChar}");
            var doc = XDocument.Load(project);
            foreach (var reference in doc.Descendants().Where(e => e.Name.LocalName == "PackageReference"))
            {
                var id = reference.Attribute("Include")?.Value ?? string.Empty;
                Assert.True(IsApproved(id, testProject), $"Onaylanmamış paket {id}: {Path.GetFileName(project)}");
            }
        }
    }

    [Fact]
    public void Repository_HasNoNpmPackageManifest()
    {
        // Bootstrap, jQuery ve doğrulama betikleri wwwroot/lib altında sabit olarak tutulur; npm bağımlılığı onaylı değildir.
        Assert.False(File.Exists(RepoPaths.Of("package.json")));
        Assert.False(File.Exists(RepoPaths.Of("src/Swab.Web/package.json")));
    }

    [Theory]
    [InlineData("Newtonsoft.Json", false, false)]
    [InlineData("Newtonsoft.Json", true, false)]
    [InlineData("Microsoft.EntityFrameworkCore", false, false)]
    [InlineData("xunit", false, false)]
    [InlineData("xunit.runner.visualstudio", true, true)]
    [InlineData("Dapper", false, true)]
    [InlineData("MailKit", true, true)]
    public void PackageAllowlist_RejectsUnlistedPackages(string id, bool testProject, bool expected)
    {
        Assert.Equal(expected, IsApproved(id, testProject));
    }

    private static bool IsApproved(string packageId, bool testProject)
    {
        if (ApprovedAppPackages.Contains(packageId, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        return testProject &&
               (ApprovedTestPackages.Contains(packageId, StringComparer.OrdinalIgnoreCase) ||
                packageId.StartsWith("xunit", StringComparison.OrdinalIgnoreCase));
    }
}

internal static class RepoPaths
{
    private static readonly string Root = FindRoot();

    // Forward slash ile verilen göreli yolu işletim sistemine uygun tam yola çevirir.
    public static string Of(string relative) =>
        Path.Combine(Root, relative.Replace('/', Path.DirectorySeparatorChar));

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !System.IO.File.Exists(Path.Combine(dir.FullName, "Swab.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Swab.sln bulunamadı.");
    }
}
