using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace VesselQuery.Tests.Integration;

public class VesselsApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Sample_query_returns_18_vessels()
    {
        var response = await _client.PostAsJsonAsync("/api/vessels/query",
            new { query = "WHERE Z13_STATUS_CODE = 4 AND BUILDER_GROUP = ‘Guoyu Logistics’" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(18, body.GetProperty("total").GetInt32());

        var cvns = body.GetProperty("results").EnumerateArray()
            .Select(v => v.GetProperty("X01_CVN").GetDouble())
            .ToHashSet();
        Assert.True(cvns.SetEquals(VesselDatasetFixture.ExpectedQuery1Cvns));
    }

    [Fact]
    public async Task Results_are_paged()
    {
        var response = await _client.PostAsJsonAsync("/api/vessels/query",
            new { query = "WHERE Z13_STATUS_CODE = 4", skip = 10, take = 5 });

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(29_996, body.GetProperty("total").GetInt32());
        Assert.Equal(5, body.GetProperty("results").GetArrayLength());
    }

    [Theory]
    [InlineData("WHERE BUILDR_GROUP = 'x'", "Unknown field")]
    [InlineData("WHERE Z13_STATUS_CODE <", "Expected a number or a quoted string")]
    [InlineData("WHERE BUILDER_GROUP > 'x'", "can only be used with numbers")]
    public async Task Invalid_query_returns_400_problem_details(string query, string message)
    {
        var response = await _client.PostAsJsonAsync("/api/vessels/query", new { query });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Invalid query", problem.GetProperty("title").GetString());
        Assert.Contains(message, problem.GetProperty("detail").GetString());
    }

    [Theory]
    [InlineData("""{ "query": "" }""")]
    [InlineData("""{ "query": "A = 1", "take": 0 }""")]
    [InlineData("""{ "query": "A = 1", "take": 5000 }""")]
    [InlineData("""{ "query": "A = 1", "skip": -1 }""")]
    public async Task Invalid_request_body_returns_400(string json)
    {
        var response = await _client.PostAsync("/api/vessels/query",
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Fields_endpoint_lists_every_queryable_field()
    {
        var fields = await _client.GetFromJsonAsync<string[]>("/api/vessels/fields");

        Assert.NotNull(fields);
        Assert.Equal(63, fields.Length);
        Assert.Contains("Z13_STATUS_CODE", fields);
        Assert.Contains("BUILDER_GROUP", fields);
    }
}
