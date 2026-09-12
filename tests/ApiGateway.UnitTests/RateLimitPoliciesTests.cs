using System.Net;
using System.Threading.RateLimiting;
using ApiGateway;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace ApiGateway.UnitTests;

public class RateLimitPoliciesTests
{
    [Fact]
    public async Task GlobalLimiter_RejectsWhenPermitLimitExceeded()
    {
        var limiter = RateLimitPolicies.CreateGlobalLimiter();
        var context = new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = IPAddress.Parse("10.0.0.10") }
        };

        for (var i = 0; i < 20; i++)
        {
            using var lease = await limiter.AcquireAsync(context, 1);
            lease.IsAcquired.Should().BeTrue();
        }

        using var rejected = await limiter.AcquireAsync(context, 1);
        rejected.IsAcquired.Should().BeFalse();
    }

    [Fact]
    public async Task GlobalLimiter_KeysByClientIp()
    {
        var limiter = RateLimitPolicies.CreateGlobalLimiter();
        var first = new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = IPAddress.Parse("10.0.0.20") }
        };
        var second = new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = IPAddress.Parse("10.0.0.21") }
        };

        var leases = new List<RateLimitLease>();
        for (var i = 0; i < 20; i++)
        {
            leases.Add(await limiter.AcquireAsync(first, 1));
        }

        using (var other = await limiter.AcquireAsync(second, 1))
        {
            other.IsAcquired.Should().BeTrue();
        }

        foreach (var lease in leases)
        {
            lease.Dispose();
        }
    }
}