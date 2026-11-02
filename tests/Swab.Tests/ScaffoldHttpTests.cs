using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Swab.Tests;

// T-001 kabul kriteri: herkese açık rota HTTP 200 döner; olmayan veya kaldırılmış sayfalar ayrıntı sızdırmaz.
public class ScaffoldHttpTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ScaffoldHttpTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PublicHomePage_IsAnonymousAndReturnsHtmlOk()
    {
        // Anonim istemci: giriş sayfasına yönlendirme olmamalı.
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.False(response.Headers.Contains("Location"));
    }

    [Theory]
    [InlineData("/lib/bootstrap/dist/css/bootstrap.min.css")]
    [InlineData("/lib/bootstrap/dist/js/bootstrap.bundle.min.js")]
    [InlineData("/lib/jquery/dist/jquery.min.js")]
    [InlineData("/lib/jquery-validation/dist/jquery.validate.min.js")]
    [InlineData("/lib/jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.min.js")]
    public async Task VendoredAssets_AreServedLocally(string path)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SampleTemplatePage_IsRemoved()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/Home/Privacy");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UnknownRoute_ReturnsNotFoundWithoutInternalDetails()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/bu-sayfa-yok-12345");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("Exception", body);
        Assert.DoesNotContain("StackTrace", body);
        Assert.DoesNotContain("   at ", body);
    }
}
