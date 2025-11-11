using GoogleConnectorService.Core.Application;
using GoogleConnectorService.Core.Domain;

namespace GoogleConnectorService.Infrastructure.Persistence;

public class MockGoogleReviewRepository : IGoogleReviewRepository
{
    // Use static storage to persist across instances
    private static readonly Dictionary<string, GoogleReview> _reviews = new();
    private static readonly List<GoogleReview> _allReviews = new();
    private static bool _initialized = false;
    private static readonly object _lock = new object();

    public MockGoogleReviewRepository()
    {
        // Initialize with some mock data only once
        lock (_lock)
        {
            if (!_initialized)
            {
                InitializeMockData();
                _initialized = true;
            }
        }
    }

    private void InitializeMockData()
    {
        // Initialize with some mock data
        var mockReviews = new[]
        {
            new GoogleReview
            {
                Id = "review-1",
                BusinessId = "test-business-id",
                ReviewerName = "John Doe",
                Rating = 5,
                Comment = "Great service! Really happy with the experience.",
                ReviewDate = DateTime.UtcNow.AddDays(-5),
                Reply = "Thank you for your feedback!",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new GoogleReview
            {
                Id = "review-2",
                BusinessId = "test-business-id",
                ReviewerName = "Jane Smith",
                Rating = 4,
                Comment = "Good experience overall, but could be improved.",
                ReviewDate = DateTime.UtcNow.AddDays(-3),
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = DateTime.UtcNow.AddDays(-3)
            },
            new GoogleReview
            {
                Id = "review-3",
                BusinessId = "test-business-id",
                ReviewerName = "Bob Johnson",
                Rating = 5,
                Comment = "Excellent! Highly recommend.",
                ReviewDate = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        foreach (var review in mockReviews)
        {
            _reviews[review.Id] = review;
            _allReviews.Add(review);
        }
    }

    public Task<GoogleReview?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _reviews.TryGetValue(id, out var review);
            return Task.FromResult<GoogleReview?>(review);
        }
    }

    public Task<List<GoogleReview>> GetByBusinessIdAsync(string businessId, int skip, int take, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var reviews = _allReviews
                .Where(r => r.BusinessId == businessId)
                .OrderByDescending(r => r.ReviewDate)
                .Skip(skip)
                .Take(take)
                .ToList();

            return Task.FromResult(reviews);
        }
    }

    public Task<int> GetCountByBusinessIdAsync(string businessId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var count = _allReviews.Count(r => r.BusinessId == businessId);
            return Task.FromResult(count);
        }
    }

    public Task UpsertAsync(GoogleReview review, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (string.IsNullOrEmpty(review.Id))
            {
                review.Id = $"review-{Guid.NewGuid()}";
            }

            review.CreatedAt = _reviews.ContainsKey(review.Id) ? _reviews[review.Id].CreatedAt : DateTime.UtcNow;
            review.UpdatedAt = DateTime.UtcNow;

            _reviews[review.Id] = review;
            
            // Remove old entry from list if exists
            var existing = _allReviews.FirstOrDefault(r => r.Id == review.Id);
            if (existing != null)
            {
                _allReviews.Remove(existing);
            }
            _allReviews.Add(review);
        }

        return Task.CompletedTask;
    }
}

