using Google.Apis.Auth.OAuth2;
using GoogleConnectorService.Core.Application;
using GoogleConnectorService.Core.Domain;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;

namespace GoogleConnectorService.Infrastructure.Persistence;

public class GoogleApiClient : IGoogleApiClient
{
    private readonly ILogger<GoogleApiClient> _logger;
    private readonly string _credentialsJson;
    private readonly HttpClient _httpClient;
    private ServiceAccountCredential? _credential;

    public GoogleApiClient(ILogger<GoogleApiClient> logger, string credentialsJson, HttpClient? httpClient = null)
    {
        _logger = logger;
        _credentialsJson = credentialsJson;
        _httpClient = httpClient ?? new HttpClient();
    }

    private async Task<ServiceAccountCredential> GetCredentialAsync(CancellationToken cancellationToken = default)
    {
        if (_credential != null)
        {
            // Check if token is stale/expired and refresh if needed
            if (_credential.Token.IsStale)
            {
                await _credential.RequestAccessTokenAsync(cancellationToken);
            }
            return _credential;
        }

        try
        {
            var googleCredential = GoogleCredential.FromJson(_credentialsJson)
                .CreateScoped(new[]
                {
                    "https://www.googleapis.com/auth/business.manage",
                    "https://www.googleapis.com/auth/businessprofileperformance"
                });

            var credentialParameters = googleCredential.UnderlyingCredential as ServiceAccountCredential;

            if (credentialParameters == null)
            {
                throw new InvalidOperationException("Failed to create service account credential from JSON. Ensure the credentials JSON is for a service account.");
            }

            // Request initial access token
            await credentialParameters.RequestAccessTokenAsync(cancellationToken);

            _credential = credentialParameters;
            return _credential;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Google credential");
            throw;
        }
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var credential = await GetCredentialAsync(cancellationToken);
        
        // Request access token if stale/expired or not available
        if (credential.Token.IsStale)
        {
            await credential.RequestAccessTokenAsync(cancellationToken);
        }

        return credential.Token.AccessToken;
    }

    public async Task<List<GoogleReview>> GetReviewsAsync(string businessId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching reviews for business {BusinessId}", businessId);

            // Get access token
            var accessToken = await GetAccessTokenAsync(cancellationToken);

            // Google Business Profile API endpoint for reviews
            // Note: The businessId should be the location ID from Google Business Profile
            // Format: accounts/{accountId}/locations/{locationId}/reviews
            // If businessId is just the locationId, we need to get the account first
            // For now, assuming businessId is in format: {accountId}/{locationId} or just {locationId}
            
            var reviews = new List<GoogleReview>();
            var pageToken = string.Empty;
            var hasMorePages = true;

            while (hasMorePages)
            {
                // Parse businessId to extract account and location
                // Expected format: "accounts/{accountId}/locations/{locationId}" or just locationId
                string apiPath;
                if (businessId.Contains("/"))
                {
                    apiPath = $"{businessId}/reviews";
                }
                else
                {
                    // If only locationId is provided, we need to find the account
                    // This is a simplified approach - in production, you might want to store the full path
                    _logger.LogWarning("BusinessId appears to be locationId only. Full path format recommended: accounts/{{accountId}}/locations/{{locationId}}");
                    // For now, try to use it directly (this may not work without proper account context)
                    apiPath = $"locations/{businessId}/reviews";
                }

                var url = $"https://mybusiness.googleapis.com/v4/{apiPath}";
                
                if (!string.IsNullOrEmpty(pageToken))
                {
                    url += $"?pageToken={Uri.EscapeDataString(pageToken)}";
                }

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Google API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _logger.LogWarning("Business location not found: {BusinessId}", businessId);
                        break;
                    }
                    
                    response.EnsureSuccessStatusCode();
                }

                var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var jsonDoc = JsonDocument.Parse(jsonContent);
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("reviews", out var reviewsElement) && reviewsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var reviewElement in reviewsElement.EnumerateArray())
                    {
                        var review = ParseReview(reviewElement, businessId);
                        if (review != null)
                        {
                            reviews.Add(review);
                        }
                    }
                }

                // Check for next page token
                if (root.TryGetProperty("nextPageToken", out var nextPageTokenElement))
                {
                    pageToken = nextPageTokenElement.GetString() ?? string.Empty;
                    hasMorePages = !string.IsNullOrEmpty(pageToken);
                }
                else
                {
                    hasMorePages = false;
                }
            }

            _logger.LogInformation("Successfully fetched {Count} reviews for business {BusinessId}", reviews.Count, businessId);

            return reviews;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching reviews for business {BusinessId}", businessId);
            throw;
        }
    }

    private GoogleReview? ParseReview(JsonElement reviewElement, string businessId)
    {
        try
        {
            var review = new GoogleReview
            {
                BusinessId = businessId,
                CreatedAt = DateTime.UtcNow
            };

            // Parse review ID
            if (reviewElement.TryGetProperty("reviewId", out var reviewIdElement))
            {
                review.Id = reviewIdElement.GetString() ?? Guid.NewGuid().ToString();
            }
            else
            {
                review.Id = Guid.NewGuid().ToString();
            }

            // Parse reviewer name
            if (reviewElement.TryGetProperty("reviewer", out var reviewerElement))
            {
                if (reviewerElement.TryGetProperty("displayName", out var displayNameElement))
                {
                    review.ReviewerName = displayNameElement.GetString() ?? "Anonymous";
                }
            }

            // Parse star rating
            if (reviewElement.TryGetProperty("starRating", out var starRatingElement))
            {
                if (starRatingElement.ValueKind == JsonValueKind.String)
                {
                    if (int.TryParse(starRatingElement.GetString(), out var rating))
                    {
                        review.Rating = rating;
                    }
                }
                else if (starRatingElement.ValueKind == JsonValueKind.Number)
                {
                    review.Rating = starRatingElement.GetInt32();
                }
            }

            // Parse comment
            if (reviewElement.TryGetProperty("comment", out var commentElement))
            {
                review.Comment = commentElement.GetString() ?? string.Empty;
            }

            // Parse review date
            if (reviewElement.TryGetProperty("createTime", out var createTimeElement))
            {
                var createTimeStr = createTimeElement.GetString();
                if (!string.IsNullOrEmpty(createTimeStr) && DateTime.TryParse(createTimeStr, out var createTime))
                {
                    review.ReviewDate = createTime;
                }
                else
                {
                    review.ReviewDate = DateTime.UtcNow;
                }
            }
            else
            {
                review.ReviewDate = DateTime.UtcNow;
            }

            // Parse reply (if business has responded)
            if (reviewElement.TryGetProperty("reviewReply", out var reviewReplyElement))
            {
                if (reviewReplyElement.TryGetProperty("comment", out var replyCommentElement))
                {
                    review.Reply = replyCommentElement.GetString();
                }
            }

            return review;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing review element");
            return null;
        }
    }
}



