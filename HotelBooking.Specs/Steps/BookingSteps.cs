using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TechTalk.SpecFlow;

namespace HotelBooking.Specs.Steps;

[Binding]
public class BookingSteps
{
    private readonly HttpClient _client;
    private HttpResponseMessage? _response;

    public BookingSteps()
    {
        var baseUri = Environment.GetEnvironmentVariable("BASE_URI") ?? "http://localhost:5000";
        _client = new HttpClient { BaseAddress = new Uri(baseUri) };
    }

    [When(@"I send a POST request to ""(.*)"" with JSON body:")]
    public async Task WhenISendPostRequestWithJsonBody(string endpoint, string jsonBody)
    {
        var content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");
        _response = await _client.PostAsync(endpoint, content);
    }

    [Then(@"the response status should be 201 or 409")]
    public void ThenTheResponseStatusShouldBe201Or409()
    {
        _response.Should().NotBeNull("a response should have been received");
        _response!.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.Conflict);
    }
}
